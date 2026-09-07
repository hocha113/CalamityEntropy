using CalamityEntropy.Common;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Dash
{
    /// <summary>
    /// 饰品冲刺引擎。全模组只有这一处读冲刺输入、写冲刺速度、判撞击与无敌。
    /// <para>帧内顺序(对齐原版 Player.Update):
    /// ResetEffects 清登记 → UpdateAccessory 登记 → PostUpdateEquips 压掉原版冲刺 →
    /// PostUpdateMiscEffects 读输入、起手、每帧旗标(先于接触伤害结算) → PostUpdateRunSpeeds 重力 →
    /// PreUpdateMovement 写本帧速度、扫撞击(紧贴位移) → PostUpdate 判墙、计时、视觉、结束。</para>
    /// <para>速度是"权威式"的:冲刺期间每帧按速度表直接写 velocity,原版跑动加减速被覆盖,
    /// 不再依赖 dashDelay 之类原版字段记状态,也就没有被原版归零、被后槽饰品盖掉的问题。</para>
    /// </summary>
    public class CEDashPlayer : ModPlayer
    {
        /// <summary>双击判定窗,对齐原版 dashTime 的 15 帧。</summary>
        public const int TapWindow = 15;
        /// <summary>锁定帧尾部的输入缓冲:此窗口内完成的双击会在解锁瞬间起手。</summary>
        public const int InputBuffer = 8;
        /// <summary>连续几帧位移被物块吃掉才判定撞墙,留一帧给门被撞开。</summary>
        private const int WallFramesToStop = 2;
        /// <summary>水平冲刺期间竖直速度每帧衰减系数。</summary>
        private const float VerticalDamping = 0.85f;

        private readonly List<CEDashEffect> offered = new();
        private CEDashEnhancer offeredEnhancer;

        /// <summary>进行中的冲刺(本地为完整模拟,远端为视觉复现),空表示空闲。</summary>
        public CEDashState State { get; private set; }
        private int lockout;
        private int blockedFrames;
        private Vector2 preMovePosition;
        private bool hasPreMove;

        private bool heldLeft, heldRight, heldUp, heldDown;
        private bool justLeft, justRight, justUp, justDown;
        private int tapX, tapY;
        private bool hotkeyPending;
        private CEDashEffect hotkeyEffectPending;
        private Vector2 bufferedDirection;
        private int bufferTimer;

        public bool IsDashing => State != null && !State.Remote;
        public bool IgnoresPlatforms => IsDashing && State.Effect.IgnorePlatforms;
        public bool InvincibleNow => IsDashing && State.Invincible;
        /// <summary>接触伤害是否被挡:穿敌冲刺与无敌冲刺都不吃接触伤害。</summary>
        public bool BlocksContact => IsDashing && (State.Invincible || State.Effect.HitsEnemies);

        /// <summary>饰品每帧登记一次;引擎只在起手瞬间读取。</summary>
        public void Offer(CEDashEffect effect)
        {
            if (effect != null && !offered.Contains(effect))
                offered.Add(effect);
        }

        /// <summary>登记强化器(暗影披风)。</summary>
        public void Offer(CEDashEnhancer enhancer)
        {
            if (enhancer != null)
                offeredEnhancer = enhancer;
        }

        public override void ResetEffects()
        {
            offered.Clear();
            offeredEnhancer = null;
        }

        public override void PostUpdateEquips()
        {
            // 原版 dashType 在 ResetEffects 归零、各饰品 UpdateAccessory 再写;这里是所有饰品之后、DashMovement 之前
            bool suppress = IsDashing && !State.Effect.ExternalMotion;
            if (!suppress)
            {
                foreach (CEDashEffect effect in offered)
                {
                    if (effect.UsesDoubleTap)
                    {
                        suppress = true;
                        break;
                    }
                }
            }
            if (suppress)
                Player.dashType = 0;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (Player.dead)
                return;
            if ((EModPlayer.DashHotkey != null && EModPlayer.DashHotkey.JustPressed) || AnyGenericDashKeybindJustPressed())
                hotkeyPending = true;
            // 用的是上一帧的登记表,起手时会按本帧登记重验
            foreach (CEDashEffect effect in offered)
            {
                if (effect.Hotkey != null && effect.Hotkey.JustPressed)
                    hotkeyEffectPending = effect;
            }
        }

        public override void PostUpdateMiscEffects()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (lockout > 0)
                lockout--;
            if (bufferTimer > 0)
                bufferTimer--;
            UpdateTapTracker();

            if (IsDashing)
            {
                if (ShouldAbort())
                    End();
                else
                    ApplyFrameFlags();
            }

            TryStartFromInput();
            hotkeyPending = false;
            hotkeyEffectPending = null;
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!IsDashing || State.Effect.ExternalMotion)
                return;
            Player.gravity *= State.Effect.GravityMult;
            if (!State.Horizontal)
                Player.maxFallSpeed = Math.Max(Player.maxFallSpeed, State.CurrentSpeed + 1f);
        }

        public override void PreUpdateMovement()
        {
            if (!IsDashing)
            {
                hasPreMove = false;
                return;
            }
            if (!State.Effect.ExternalMotion)
            {
                ApplyVelocity();
                // 原版 DashMovement 已跑完,这里写 -1 只供读原版字段的系统识别"冲刺中",下一帧原版会自己归零
                Player.dashDelay = -1;
                if (State.Effect.HitsEnemies)
                    SweepHits();
            }
            preMovePosition = Player.position;
            hasPreMove = true;
        }

        public override void PostUpdate()
        {
            if (State == null)
                return;

            if (!State.Remote)
            {
                if (ShouldAbort())
                {
                    End();
                    return;
                }
                if (!State.Effect.ExternalMotion && hasPreMove && WallBlocked())
                {
                    End();
                    return;
                }
            }

            State.Effect.OnVisuals(Player, State);
            State.Enhancer?.OnVisuals(Player, State);
            State.Timer++;

            bool finished;
            if (State.Effect.ExternalMotion)
                finished = State.Remote ? State.Timer >= State.Duration : Player.dashDelay >= 0;
            else
                finished = State.Timer >= State.Duration;
            if (finished)
                End();
        }

        public override void UpdateDead()
        {
            if (State != null)
                End(false);
            bufferTimer = 0;
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            return !BlocksContact;
        }

        public override bool CanBeHitByProjectile(Projectile proj)
        {
            return !InvincibleNow;
        }

        // ---------------------------------------------------------------- 输入

        private void UpdateTapTracker()
        {
            justRight = Player.controlRight && !heldRight;
            justLeft = Player.controlLeft && !heldLeft;
            justUp = Player.controlUp && !heldUp;
            justDown = Player.controlDown && !heldDown;
            heldRight = Player.controlRight;
            heldLeft = Player.controlLeft;
            heldUp = Player.controlUp;
            heldDown = Player.controlDown;

            if (tapX > 0) tapX--;
            else if (tapX < 0) tapX++;
            if (tapY > 0) tapY--;
            else if (tapY < 0) tapY++;
        }

        /// <summary>消费本帧输入,得到一个双击/冲刺键方向。窗口在这里维护,不论当前能不能起手。</summary>
        private bool TryResolveTapDirection(bool allowVertical, out Vector2 direction)
        {
            direction = Vector2.Zero;
            if (hotkeyPending)
            {
                direction = new Vector2(ResolveHotkeyHorizontal(), 0f);
                tapX = 0;
                return true;
            }

            if (justRight)
            {
                if (tapX > 0)
                {
                    direction = Vector2.UnitX;
                    tapX = 0;
                    return true;
                }
                tapX = TapWindow;
            }
            else if (justLeft)
            {
                if (tapX < 0)
                {
                    direction = -Vector2.UnitX;
                    tapX = 0;
                    return true;
                }
                tapX = -TapWindow;
            }

            if (!allowVertical)
                return false;

            if (justDown)
            {
                if (tapY > 0)
                {
                    direction = Vector2.UnitY;
                    tapY = 0;
                    return true;
                }
                tapY = TapWindow;
            }
            else if (justUp)
            {
                if (tapY < 0)
                {
                    direction = -Vector2.UnitY;
                    tapY = 0;
                    return true;
                }
                tapY = -TapWindow;
            }
            return false;
        }

        private int ResolveHotkeyHorizontal()
        {
            if (Player.controlRight && !Player.controlLeft)
                return 1;
            if (Player.controlLeft && !Player.controlRight)
                return -1;
            if (MathF.Abs(Player.velocity.X) > 0.01f)
                return Math.Sign(Player.velocity.X);
            return Player.direction;
        }

        /// <summary>
        /// 其它模组注册名为 Dash / DashHotkey / DashDoubleTapOverride 的键也算冲刺键。
        /// 这些模组把冲刺挂在原版 DoCommonDashHandle 上,而本引擎压掉了 dashType,那条路不会跑。
        /// </summary>
        private static bool AnyGenericDashKeybindJustPressed()
        {
            foreach (var pair in PlayerInput.Triggers.JustPressed.KeyStatus)
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
                || name.Equals("DashDoubleTapOverride", StringComparison.OrdinalIgnoreCase);
        }

        // ---------------------------------------------------------------- 起手

        private bool BodyBlocked()
            => Player.dead || Player.mount.Active || Player.CCed || Player.tongued || Player.shimmering;

        private void TryStartFromInput()
        {
            if (BodyBlocked())
            {
                bufferTimer = 0;
                return;
            }

            // 热键专属效果(翱翔符文)优先,允许打断进行中的常规冲刺
            if (hotkeyEffectPending != null && offered.Contains(hotkeyEffectPending))
            {
                CEDashEffect effect = hotkeyEffectPending;
                bool canInterrupt = State == null || (effect.CanInterrupt && State.Effect != effect);
                if (canInterrupt && effect.CanStart(Player))
                {
                    Vector2? aim = effect.HotkeyDirection(Player);
                    if (aim.HasValue && aim.Value != Vector2.Zero)
                    {
                        Start(effect, aim.Value.SafeNormalize(Vector2.UnitX));
                        return;
                    }
                }
            }

            bool anyVertical = false;
            foreach (CEDashEffect effect in offered)
            {
                if (effect.UsesDoubleTap && effect.Omnidirectional)
                {
                    anyVertical = true;
                    break;
                }
            }

            // 双击窗口每帧都要走,哪怕现在起不了手;起不了手时把结果放进短缓冲
            if (TryResolveTapDirection(anyVertical, out Vector2 tapped))
            {
                bufferedDirection = tapped;
                bufferTimer = InputBuffer;
            }

            if (State != null || lockout > 0 || bufferTimer <= 0)
                return;

            Vector2 direction = bufferedDirection;
            bufferTimer = 0;

            CEDashEffect best = null;
            foreach (CEDashEffect effect in offered)
            {
                if (!effect.UsesDoubleTap)
                    continue;
                if (direction.Y != 0f && !effect.Omnidirectional)
                    continue;
                if (!effect.CanStart(Player))
                    continue;
                if (best == null || effect.Priority >= best.Priority)
                    best = effect;
            }
            if (best != null)
                Start(best, direction);
        }

        private void Start(CEDashEffect effect, Vector2 direction)
        {
            if (State != null)
                End(false);

            var state = new CEDashState
            {
                Effect = effect,
                Direction = direction,
                Duration = Math.Max(1, effect.Duration),
            };

            float distance = effect.Distance;
            float endSpeed = effect.EndSpeed(Player, direction);
            if (offeredEnhancer != null && effect.CanBeEnhanced && offeredEnhancer.TryConsume(Player))
            {
                state.Enhancer = offeredEnhancer;
                distance *= offeredEnhancer.SpeedMult;
                endSpeed *= offeredEnhancer.SpeedMult;
            }
            state.Speeds = BuildSpeeds(state.Duration, distance, endSpeed, effect.Curve);
            State = state;
            blockedFrames = 0;
            hasPreMove = false;

            Player.RemoveAllGrapplingHooks();
            Player.pulley = false;
            Player.timeSinceLastDashStarted = 0;
            Player.doorHelper.AllowOpeningDoorsByVelocityAloneForATime(state.Duration * 3);
            Player.Entropy().LastUsedDashID = effect.ID;
            if (state.Horizontal)
                Player.ChangeDir(state.HorizontalSign(Player));

            ApplyVelocity();
            ApplyFrameFlags();
            effect.OnStart(Player, state);
            state.Enhancer?.OnStart(Player, state);
            SendStart(state);
        }

        /// <summary>
        /// 原版冲刺起手帧的特征:dash>0、dashDelay 刚写成 -1、timeSinceLastDashStarted 还是 0。
        /// 这一帧只能在 DashMovement 之后、接触伤害之前抓到,调用点见 <see cref="PostMovementVanillaCheck"/>。
        /// </summary>
        private void TryEnhanceVanillaDash()
        {
            if (State != null || offeredEnhancer == null)
                return;
            if (Player.dash <= 0 || Player.dashDelay >= 0 || Player.timeSinceLastDashStarted != 0)
                return;
            CEDashEffect vanilla = CEDashRegistry.GetEffect("VanillaDash");
            if (vanilla == null || !offeredEnhancer.TryConsume(Player))
                return;

            var state = new CEDashState
            {
                Effect = vanilla,
                Enhancer = offeredEnhancer,
                Direction = new Vector2(Player.velocity.X >= 0f ? 1f : -1f, 0f),
                Duration = vanilla.Duration,
            };
            Player.velocity.X *= offeredEnhancer.SpeedMult;
            State = state;
            Player.Entropy().LastUsedDashID = state.Effect.ID;
            ApplyFrameFlags();
            state.Enhancer.OnStart(Player, state);
            SendStart(state);
        }

        private void End(bool startLockout = true)
        {
            CEDashState state = State;
            if (state == null)
                return;
            State = null;
            hasPreMove = false;
            blockedFrames = 0;
            if (!state.Remote)
            {
                Player.eocDash = 0;
                if (startLockout)
                    lockout = Math.Max(lockout, state.Effect.Cooldown);
            }
            state.Effect.OnEnd(Player, state);
            state.Enhancer?.OnEnd(Player, state);
        }

        private bool ShouldAbort()
            => Player.dead || Player.mount.Active || Player.CCed || Player.tongued;

        // ---------------------------------------------------------------- 运动

        /// <summary>
        /// 生成每帧速度表:总位移精确等于 distance,首帧最快,按 (1-t/T)^curve 缓出到 endSpeed。
        /// distance 不够铺满 endSpeed 时退化为匀速。
        /// </summary>
        public static float[] BuildSpeeds(int duration, float distance, float endSpeed, float curve)
        {
            duration = Math.Max(1, duration);
            var speeds = new float[duration];
            float extra = distance - endSpeed * duration;
            if (extra <= 0f)
            {
                float flat = distance / duration;
                for (int t = 0; t < duration; t++)
                    speeds[t] = flat;
                return speeds;
            }
            float sum = 0f;
            for (int t = 0; t < duration; t++)
            {
                float w = MathF.Pow(1f - t / (float)duration, Math.Max(0.01f, curve));
                speeds[t] = w;
                sum += w;
            }
            for (int t = 0; t < duration; t++)
                speeds[t] = endSpeed + extra * speeds[t] / sum;
            return speeds;
        }

        private void ApplyVelocity()
        {
            float speed = State.CurrentSpeed;
            if (State.Horizontal)
            {
                Player.velocity.X = State.Direction.X * speed;
                if (State.Effect.DampVertical && !Player.controlJump)
                    Player.velocity.Y *= VerticalDamping;
                Player.ChangeDir(State.HorizontalSign(Player));
            }
            else
            {
                Player.velocity = State.Direction * speed;
                if (State.Direction.X != 0f)
                    Player.ChangeDir(State.HorizontalSign(Player));
            }
        }

        /// <summary>每帧旗标:无敌、原版残影。放在接触伤害结算之前。</summary>
        private void ApplyFrameFlags()
        {
            if (State.Invincible)
            {
                Player.immune = true;
                Player.immuneNoBlink = true;
                if (Player.immuneTime < 2)
                    Player.immuneTime = 2;
            }
            if (!State.Effect.ExternalMotion)
                Player.eocDash = 12;
        }

        private bool WallBlocked()
        {
            // 水里位移本来就减半,蜂蜜更多,别误判成撞墙
            if (Player.wet || Player.honeyWet || Player.shimmerWet)
            {
                blockedFrames = 0;
                return false;
            }
            float expected = State.CurrentSpeed;
            if (expected < 2f)
                return false;
            float actual = Vector2.Dot(Player.position - preMovePosition, State.Direction);
            if (actual < expected * 0.25f)
                blockedFrames++;
            else
                blockedFrames = 0;
            return blockedFrames >= WallFramesToStop;
        }

        // ---------------------------------------------------------------- 撞击

        /// <summary>从当前位置扫到本帧落点,命中的敌怪本次冲刺只结算一次。</summary>
        private void SweepHits()
        {
            Vector2 from = Player.Center;
            Vector2 to = from + Player.velocity;
            int lineWidth = State.Horizontal ? Player.height : Player.width;
            Rectangle playerRect = Player.getRect();

            foreach (NPC npc in Main.ActiveNPCs)
            {
                // 不排除 immortal:训练假人靶要能撞出伤害数字
                if (npc.friendly || npc.dontTakeDamage)
                    continue;
                if (State.HitNPCs.Contains(npc.whoAmI))
                    continue;
                if (!Player.CanNPCBeHitByPlayerOrPlayerProjectile(npc))
                    continue;

                Rectangle npcRect = npc.getRect();
                npcRect.Inflate(10, 8);
                if (!npcRect.Intersects(playerRect) && !CEUtils.LineThroughRect(from, to, npcRect, lineWidth))
                    continue;

                State.HitNPCs.Add(npc.whoAmI);
                State.HitCount++;

                var hit = new CEDashHit
                {
                    DamageClass = DamageClass.Generic,
                    Knockback = 6f,
                    PlayerImmuneFrames = 12,
                };
                State.Effect.OnHit(Player, npc, State, ref hit);

                if (hit.Damage > 0)
                {
                    DamageClass damageClass = hit.DamageClass ?? DamageClass.Generic;
                    int damage = (int)Player.GetTotalDamage(damageClass).ApplyTo(hit.Damage);
                    bool crit = Main.rand.Next(100) < Player.GetTotalCritChance(damageClass);
                    Player.ApplyDamageToNPC(npc, damage, hit.Knockback, State.HorizontalSign(Player), crit, damageClass);
                }
                if (hit.PlayerImmuneFrames > 0)
                    GiveImmunity(hit.PlayerImmuneFrames);
            }
        }

        private void GiveImmunity(int frames)
        {
            if (!Player.immune || Player.immuneTime < frames)
            {
                Player.immune = true;
                Player.immuneNoBlink = true;
                Player.immuneTime = frames;
            }
            for (int i = 0; i < Player.hurtCooldowns.Length; i++)
            {
                if (Player.hurtCooldowns[i] < frames)
                    Player.hurtCooldowns[i] = frames;
            }
        }

        // ---------------------------------------------------------------- 联机

        /// <summary>只广播起手一帧的"形":效果、方向、强化器。位置与速度走原版玩家同步,远端只复现视觉。</summary>
        private void SendStart(CEDashState state)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)CEMessageType.SyncDashStart);
            packet.Write((byte)Player.whoAmI);
            packet.Write(state.Effect.ID);
            packet.WriteVector2(state.Direction);
            packet.Write(state.Enhancer?.ID ?? string.Empty);
            packet.Send();
        }

        /// <summary>远端收到起手包:建立纯视觉状态,按效果时长自然结束。</summary>
        public void BeginRemote(string effectId, Vector2 direction, string enhancerId)
        {
            if (Player.whoAmI == Main.myPlayer || Main.dedServ)
                return;
            CEDashEffect effect = CEDashRegistry.GetEffect(effectId);
            if (effect == null)
                return;
            if (State != null)
                End(false);
            State = new CEDashState
            {
                Effect = effect,
                Enhancer = CEDashRegistry.GetEnhancer(enhancerId),
                Direction = direction == Vector2.Zero ? Vector2.UnitX : direction.SafeNormalize(Vector2.UnitX),
                Duration = Math.Max(1, effect.Duration),
                Remote = true,
            };
            State.Speeds = BuildSpeeds(State.Duration, effect.Distance, effect.EndSpeed(Player, State.Direction), effect.Curve);
            effect.OnStart(Player, State);
            State.Enhancer?.OnStart(Player, State);
        }

        /// <summary>
        /// 原版冲刺的暗影强化检测点。ModPlayer 没有落在 DashMovement 与 Update_NPCCollision 之间的钩子,
        /// 由 CalamityEntropy.update_npc_collision 那条 On_Player.Update_NPCCollision 细节在 orig 之前调用。
        /// </summary>
        internal void PostMovementVanillaCheck()
        {
            if (Player.whoAmI == Main.myPlayer && !BodyBlocked())
                TryEnhanceVanillaDash();
        }
    }
}
