using System;
using System.Collections.Generic;
using CalamityEntropy.Common;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    /// <summary>
    /// 盾冲刺命中上下文：由各冲刺效果的 OnHitEffects 填写，驱动器据此结算冲撞伤害。
    /// 形状对齐灾厄 DashHitContext 的本模组使用面。
    /// </summary>
    public struct CEDashHitContext
    {
        public int HitDirection;
        public int PlayerImmunityFrames;
        public DamageClass damageClass;
        public int BaseDamage;
        public float BaseKnockback;
    }

    /// <summary>
    /// 盾冲刺效果基类（自研，替代灾厄 PlayerDashEffect 的本模组使用面：水平方向盾撞冲刺）。
    /// 实例为静态单例，Time/PostHit 等运行时字段仅服务本地玩家（与灾厄同约束）。
    /// </summary>
    public abstract class CEShieldDashEffect
    {
        /// <summary>冲刺进行帧计数，驱动器每帧递增。</summary>
        public int dashTime;

        /// <summary>冲刺 ID，写入 player.Entropy().LastUsedDashID（约定名见 player-api.md）。</summary>
        public abstract string DashID { get; }

        public abstract float CalculateDashSpeed(Player player);

        /// <summary>冲刺触发瞬间。</summary>
        public virtual void OnDashEffects(Player player) { }

        /// <summary>冲刺进行中每帧：可改写冲刺速度与减速系数。</summary>
        public virtual void MidDashEffects(Player player, ref float dashSpeed, ref float dashSpeedDecelerationFactor, ref float runSpeedDecelerationFactor) { }

        /// <summary>冲撞命中敌怪：填写 hitContext 以结算伤害。</summary>
        public virtual void OnHitEffects(Player player, NPC npc, ref CEDashHitContext hitContext) { }
    }

    /// <summary>
    /// 盾冲刺驱动器（自研，承接灾厄 ModDashMovement 的本模组使用面）。
    /// 饰品每帧在 UpdateAccessory 里设置 ActiveDash；驱动器负责冲刺键或双击触发、
    /// 冲刺移动衰减、冲撞判定与冷却。冲刺中状态自管，不借用原版 dashDelay。
    /// </summary>
    public class CEShieldDashPlayer : ModPlayer
    {
        /// <summary>本帧可用的冲刺效果，饰品侧每帧设置，ResetEffects 清空。</summary>
        public CEShieldDashEffect ActiveDash;

        /// <summary>正在进行的冲刺效果。</summary>
        private CEShieldDashEffect usedDash;

        /// <summary>冲撞后的敌怪受击间隔（对齐灾厄 dashImmunityTime，仅本地玩家维护）。</summary>
        private readonly Dictionary<int, int> npcHitGap = new();

        /// <summary>盾撞冲刺结束后的统一冷却（对齐灾厄 UniversalShieldSlamCooldown）。</summary>
        private const int ShieldSlamCooldown = 30;

        /// <summary>
        /// 冲刺进行中标志（自有状态）。不能借用原版 dashDelay 记录状态：
        /// 这些饰品每帧写 dashType=0，原版 DashMovement 对 dash==0 会把 dashDelay 强制归零，
        /// 借用它会让冲刺启动当帧即被打断（历史 bug：无法充能冲刺）。
        /// </summary>
        private bool dashing;

        /// <summary>盾撞冲刺自有冷却计时（替代原版 dashDelay>0 的冷却语义）。</summary>
        private int slamCooldown;

        /// <summary>上一帧是否按着左/右。PreUpdateMovement 里 releaseLeft/Right 已被原版清掉，只能自管边沿。</summary>
        private bool heldLeft;
        private bool heldRight;

        /// <summary>自管双击窗：&gt;0 右向待第二下，&lt;0 左向待第二下。对齐原版 dashTime 的 15 帧。</summary>
        private int dashTapWindow;

        /// <summary>冲刺结束后仍挡接触伤害的剩余帧,避免刚减速还叠在怪里就对撞。</summary>
        private int passThroughGuard;

        /// <summary>上一帧采样点,给扫过判定用。</summary>
        private Vector2 slamFrom;

        public bool IsDashing => dashing;

        /// <summary>冲刺中或刚结束的穿敌保护窗。</summary>
        public bool BlocksContact => dashing || passThroughGuard > 0;

        public override void ResetEffects()
        {
            ActiveDash = null;
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            // 接触伤害看这个钩,只写 immune 挡不住 specialHitSetter / 部分 Hurt 路径
            if (BlocksContact)
                return false;
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

        /// <summary>接触伤害结算前启动、给无敌、做扫过撞箱。放 PreUpdateMovement 会晚于 Update_NPCCollision。</summary>
        public static void PrepareForNpcCollision(Player player)
        {
            player.GetModPlayer<CEShieldDashPlayer>().PrepareForNpcCollision();
        }

        private void PrepareForNpcCollision()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (!dashing)
                TryStartDash();
            if (dashing || passThroughGuard > 0)
                ApplyPassThroughImmune();
            if (dashing && usedDash != null)
                TrySlamHits();
        }

        public override void PreUpdateMovement()
        {
            if (npcHitGap.Count > 0)
            {
                foreach (int key in new List<int>(npcHitGap.Keys))
                {
                    if (--npcHitGap[key] <= 0)
                        npcHitGap.Remove(key);
                }
            }

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (slamCooldown > 0)
                slamCooldown--;
            if (passThroughGuard > 0)
                passThroughGuard--;

            if (ActiveDash == null)
            {
                // 饰品被卸下或充能不足：终止进行中的冲刺并进入冷却
                if (dashing)
                {
                    dashing = false;
                    slamCooldown = ShieldSlamCooldown;
                }
                usedDash = null;
                // 充能未就绪时也要跟边沿，否则充能刚满会把「一直按着」误判成刚按下
                heldRight = Player.controlRight;
                heldLeft = Player.controlLeft;
                if (dashTapWindow > 0)
                    dashTapWindow--;
                else if (dashTapWindow < 0)
                    dashTapWindow++;
                return;
            }

            if (dashing && usedDash != null)
                UpdateActiveDash();
        }

        private void TryStartDash()
        {
            if (ActiveDash == null || slamCooldown != 0 || Player.mount.Active || Player.CCed)
                return;
            if (Player.Entropy().shadeDashExclusive)
                return;
            if (!TryGetHorizontalDashDirection(out int direction))
                return;
            StartDash(direction);
        }

        private void ApplyPassThroughImmune()
        {
            Player.immune = true;
            Player.immuneNoBlink = true;
            if (Player.immuneTime < 8)
                Player.immuneTime = 8;
            var mp = Player.Entropy();
            if (mp.immune < 8)
                mp.immune = 8;
        }

        private void StartDash(int direction)
        {
            usedDash = ActiveDash;
            usedDash.dashTime = 0;
            dashing = true;
            Player.velocity.X = direction * usedDash.CalculateDashSpeed(Player);

            // 前方贴墙时腰斩初速（对齐灾厄 DoADash 的物块检测）
            Point upwardTilePoint = (Player.Center + new Vector2(direction * Player.width / 2 + 2, Player.gravDir * -Player.height / 2f + Player.gravDir * 2f)).ToTileCoordinates();
            Point aheadTilePoint = (Player.Center + new Vector2(direction * Player.width / 2 + 2, 0f)).ToTileCoordinates();
            if (WorldGen.SolidOrSlopedTile(upwardTilePoint.X, upwardTilePoint.Y) || WorldGen.SolidOrSlopedTile(aheadTilePoint.X, aheadTilePoint.Y))
                Player.velocity.X /= 2f;

            Player.timeSinceLastDashStarted = 0;
            Player.Entropy().LastUsedDashID = usedDash.DashID;
            passThroughGuard = 36;
            slamFrom = Player.Center;
            ApplyPassThroughImmune();
            usedDash.OnDashEffects(Player);
        }

        public override void PostUpdate()
        {
            if (dashing && usedDash != null)
                TrySlamHits();
            slamFrom = Player.Center;
        }

        /// <summary>
        /// 从上一采样点扫到本帧落点。不读 CanHit:穿敌时隔墙/跨步都不该漏。
        /// </summary>
        private void TrySlamHits()
        {
            Vector2 slamTo = Player.Center + new Vector2(Player.velocity.X, 0f);
            int lineWidth = Player.height + 24;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (Player.dontHurtCritters && NPCID.Sets.CountsAsCritter[n.type])
                    continue;
                if (n.dontTakeDamage || n.friendly || npcHitGap.ContainsKey(n.whoAmI))
                    continue;

                Rectangle npcRect = n.getRect();
                npcRect.Inflate(12, 8);
                bool swept = CEUtils.LineThroughRect(slamFrom, slamTo, npcRect, lineWidth);
                if (!swept && !Player.getRect().Intersects(n.getRect()))
                    continue;

                CEDashHitContext hitContext = default;
                usedDash.OnHitEffects(Player, n, ref hitContext);
                if (hitContext.damageClass is null || hitContext.BaseDamage <= 0)
                    continue;

                int dashDamage = (int)Player.GetTotalDamage(hitContext.damageClass).ApplyTo(hitContext.BaseDamage);
                Player.ApplyDamageToNPC(n, dashDamage, hitContext.BaseKnockback, hitContext.HitDirection, false, hitContext.damageClass);
                npcHitGap[n.whoAmI] = 12;
                if (passThroughGuard < 16)
                    passThroughGuard = 16;
            }
        }

        private void UpdateActiveDash()
        {
            // 每帧对外镜像"冲刺中"（供读取原版字段的系统用）；自身逻辑不读它，原版帧尾会归零
            Player.dashDelay = -1;
            usedDash.dashTime++;

            float dashSpeed = 12f;
            float dashSpeedDecelerationFactor = 0.985f;
            float runSpeed = Math.Max(Player.accRunSpeed, Player.maxRunSpeed);
            float runSpeedDecelerationFactor = 0.94f;
            usedDash.MidDashEffects(Player, ref dashSpeed, ref dashSpeedDecelerationFactor, ref runSpeedDecelerationFactor);

            Player.vortexStealthActive = false;
            if (Player.velocity.X != 0f)
                Player.ChangeDir(Math.Sign(Player.velocity.X));

            // 水平速度逐级衰减，回落到跑速后进入冷却（对齐灾厄非全向冲刺分支）
            if (Player.velocity.X > dashSpeed || Player.velocity.X < -dashSpeed)
            {
                Player.velocity.X *= dashSpeedDecelerationFactor;
                return;
            }
            if (Player.velocity.X > runSpeed || Player.velocity.X < -runSpeed)
            {
                Player.velocity.X *= runSpeedDecelerationFactor;
                return;
            }

            dashing = false;
            slamCooldown = ShieldSlamCooldown;
            if (Player.velocity.X < 0f)
                Player.velocity.X = -runSpeed;
            else if (Player.velocity.X > 0f)
                Player.velocity.X = runSpeed;
        }

        /// <summary>
        /// 水平冲刺输入。暗影披风与盾冲刺共用，同一帧只消费一次。
        /// 不能读 releaseRight：原版在 DashMovement 之后、PreUpdateMovement 之前就把按住中的 release 清掉了。
        /// 自管边沿 + 15 帧双击窗；冲刺键认本模组 F，以及突变模组等其它模组的 Dash 键。
        /// </summary>
        public bool TryGetHorizontalDashDirection(out int direction)
        {
            direction = 0;

            bool justRight = Player.controlRight && !heldRight;
            bool justLeft = Player.controlLeft && !heldLeft;
            heldRight = Player.controlRight;
            heldLeft = Player.controlLeft;

            if (dashTapWindow > 0)
                dashTapWindow--;
            else if (dashTapWindow < 0)
                dashTapWindow++;

            if (AnyDashHotkeyJustPressed())
            {
                direction = ResolveHotkeyDashDirection(Player);
                dashTapWindow = 0;
                return true;
            }

            if (justRight)
            {
                if (dashTapWindow > 0)
                {
                    direction = 1;
                    dashTapWindow = 0;
                    return true;
                }
                dashTapWindow = 15;
                return false;
            }

            if (justLeft)
            {
                if (dashTapWindow < 0)
                {
                    direction = -1;
                    dashTapWindow = 0;
                    return true;
                }
                dashTapWindow = -15;
                return false;
            }

            return false;
        }

        private static int ResolveHotkeyDashDirection(Player player)
        {
            if (player.controlRight && !player.controlLeft)
                return 1;
            if (player.controlLeft && !player.controlRight)
                return -1;
            if (MathF.Abs(player.velocity.X) > 0.01f)
                return Math.Sign(player.velocity.X);
            return player.direction;
        }

        /// <summary>
        /// 通用冲刺键：本模组 Dash，以及其它模组注册名为 Dash / DashHotkey / DashDoubleTapOverride 的键。
        /// 突变模组把冲刺挂在原版 DoCommonDashHandle 上，而本模组饰品写 dashType=0，那条钩子根本不会跑。
        /// </summary>
        private static bool AnyDashHotkeyJustPressed()
        {
            var justPressed = PlayerInput.Triggers.JustPressed.KeyStatus;
            foreach (var pair in justPressed)
            {
                if (pair.Value && IsGenericDashKeybind(pair.Key))
                    return true;
            }
            return false;
        }

        private static bool IsGenericDashKeybind(string fullName)
        {
            int slash = fullName.LastIndexOf('/');
            string name = slash >= 0 ? fullName.Substring(slash + 1) : fullName;
            return name.Equals("Dash", StringComparison.OrdinalIgnoreCase)
                || name.Equals("DashHotkey", StringComparison.OrdinalIgnoreCase)
                || name.Equals("DashHotKey", StringComparison.OrdinalIgnoreCase)
                || name.Equals("DashDoubleTapOverride", StringComparison.OrdinalIgnoreCase);
        }
    }
}
