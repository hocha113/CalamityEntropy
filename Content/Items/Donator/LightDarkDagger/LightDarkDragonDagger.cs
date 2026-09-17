using CalamityEntropy.Core.Weapons;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.LightDarkDagger
{
    /// <summary>
    /// 光暗龙匕(捐赠者:紫墨)。
    /// 左键交替掷出耀光/黯影三连飞刃(上半弧/下半弧),两种刃在同一目标上交替命中触发光影斩切并回血;
    /// 普攻命中累积蓄势,右键清空蓄势掷出双螺旋纠缠刃,螺旋刃在场时再按右键闪烁至刃位并留下织影裂隙。
    /// 每次暴击叠加基础伤害,一次未暴击即清空;收藏在背包中时召出朦胧的光暗之龙跟随,层数越高越清晰。
    /// </summary>
    public class LightDarkDragonDagger : ModItem, IDonatorItem, ICEChargeWeapon
    {
        public string DonatorName => "紫墨";

        //普攻真实命中 24 次充满蓄势(框架自动计数,螺旋刃自身命中不回充)
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(ChargeHits);

        #region 调参旋钮
        public const int ChargeHits = 24;
        //每次三连的三把刃相对瞄准线的起手偏角(度),越靠后的刃弧越大
        public static readonly float[] VolleyOffsetsDeg = { 20f, 42f, 64f };
        //弧线回摆倍率:大于 1 让飞刃越过瞄准线一点,三条弧在目标前交叉
        public const float ArcOvershoot = 1.35f;
        public const int ArcTurnFrames = 26;
        //暴击层数上限与每层基础伤害
        public const int MaxCritStack = 15;
        public const int DamagePerStack = 2;
        //螺旋刃:飘行速度、双股相位差由光暗各占一半,同股两把刃的间隔帧
        public const float HelixSpeed = 9f;
        public const int HelixLagFrames = 7;
        //闪烁斩击与织影裂隙的伤害倍率(相对物品面板)
        public const float BlinkDamageMult = 1.6f;
        public const float RiftDamageMult = 0.5f;
        //闪烁落点搜索:方向数与半径步进(像素)
        public const int BlinkSearchDirs = 8;
        public const float BlinkSearchStep = 24f;
        public const int BlinkSearchRings = 5;
        #endregion

        //左键交替:false 掷耀光,true 掷黯影
        private bool nextVolleyDark;

        public override void SetDefaults() {
            Item.width = 52;
            Item.height = 48;
            Item.damage = 38;
            Item.crit = 8;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.knockBack = 3f;
            Item.UseSound = null;
            Item.value = Item.buyPrice(gold: 15);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<LDKnifeLight>();
            Item.shootSpeed = 15f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse != 2) {
                return true;
            }
            LDDaggerPlayer mp = player.GetModPlayer<LDDaggerPlayer>();
            if (mp.HasHelixKnives()) {
                return TryGetBlinkTarget(player, out _);
            }
            return CEChargeWeapon.IsReady(Item);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            LDDaggerPlayer mp = player.GetModPlayer<LDDaggerPlayer>();
            mp.AttackAnimTime = 30;
            if (player.altFunctionUse == 2) {
                if (mp.HasHelixKnives()) {
                    if (TryGetBlinkTarget(player, out Vector2 dest)) {
                        Blink(player, source, dest, damage, knockback);
                    }
                    return false;
                }
                if (CEChargeWeapon.TryConsume(player, Item)) {
                    ThrowHelix(player, source, position, velocity, damage, knockback);
                }
                return false;
            }
            ThrowVolley(player, source, position, velocity, damage, knockback);
            nextVolleyDark = !nextVolleyDark;
            return false;
        }

        #region 左键三连弧刃
        private void ThrowVolley(Player player, IEntitySource source, Vector2 position, Vector2 velocity, int damage, float knockback) {
            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            //正向旋转是否朝屏幕上方:据此决定耀光走上半弧、黯影走下半弧
            float upSign = aim.RotatedBy(0.1f).Y < aim.Y ? 1f : -1f;
            float sideSign = nextVolleyDark ? -upSign : upSign;
            int type = nextVolleyDark ? ModContent.ProjectileType<LDKnifeDark>() : ModContent.ProjectileType<LDKnifeLight>();
            float speed = velocity.Length();
            for (int i = 0; i < VolleyOffsetsDeg.Length; i++) {
                float off = MathHelper.ToRadians(VolleyOffsetsDeg[i]) * sideSign;
                Vector2 vel = aim.RotatedBy(off) * speed;
                //ai[0] = 剩余回转总角(反向并略过冲),ai[1] = 剩余回转帧数
                Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI, -off * ArcOvershoot, ArcTurnFrames);
            }
            CEUtils.PlaySound("throw", nextVolleyDark ? 0.85f : 1.1f, player.Center, 6, 0.8f);
        }
        #endregion

        #region 右键螺旋刃
        private void ThrowHelix(Player player, IEntitySource source, Vector2 position, Vector2 velocity, int damage, float knockback) {
            Vector2 vel = velocity.SafeNormalize(Vector2.UnitX * player.direction) * HelixSpeed;
            int light = ModContent.ProjectileType<LDHelixKnifeLight>();
            int dark = ModContent.ProjectileType<LDHelixKnifeDark>();
            //光股相位 0、暗股相位 π,同股第二把刃落后 HelixLagFrames 帧
            SpawnHelix(source, position, vel, light, damage, knockback, player.whoAmI, 0f, 0);
            SpawnHelix(source, position, vel, light, damage, knockback, player.whoAmI, 0f, HelixLagFrames);
            SpawnHelix(source, position, vel, dark, damage, knockback, player.whoAmI, MathHelper.Pi, 0);
            SpawnHelix(source, position, vel, dark, damage, knockback, player.whoAmI, MathHelper.Pi, HelixLagFrames);
            CEUtils.PlaySound("throw", 0.7f, player.Center, 6, 1f);
            CEUtils.PlaySound("soulshine", 1.2f, player.Center, 4, 0.6f);
        }

        private static void SpawnHelix(IEntitySource source, Vector2 position, Vector2 vel, int type, int damage, float knockback, int owner, float phase, int lag) {
            int p = Projectile.NewProjectile(source, position, vel, type, damage, knockback, owner, phase, lag);
            CEChargeWeapon.Empower(p);
        }
        #endregion

        #region 右键闪烁
        /// <summary>螺旋刃在场时的闪烁落点:取所有螺旋刃的中心,再在附近找一处玩家能站下的空位。</summary>
        private bool TryGetBlinkTarget(Player player, out Vector2 dest) {
            dest = Vector2.Zero;
            int light = ModContent.ProjectileType<LDHelixKnifeLight>();
            int dark = ModContent.ProjectileType<LDHelixKnifeDark>();
            Vector2 sum = Vector2.Zero;
            int count = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == player.whoAmI && (p.type == light || p.type == dark)) {
                    sum += p.Center;
                    count++;
                }
            }
            if (count == 0) {
                return false;
            }
            Vector2 center = sum / count;
            if (IsStandable(player, center)) {
                dest = center;
                return true;
            }
            for (int ring = 1; ring <= BlinkSearchRings; ring++) {
                for (int d = 0; d < BlinkSearchDirs; d++) {
                    Vector2 candidate = center + (MathHelper.TwoPi * d / BlinkSearchDirs).ToRotationVector2() * BlinkSearchStep * ring;
                    if (IsStandable(player, candidate)) {
                        dest = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsStandable(Player player, Vector2 center) {
            Vector2 topLeft = center - player.Size / 2f;
            if (topLeft.X < 16 * 40 || topLeft.Y < 16 * 40 || topLeft.X + player.width > (Main.maxTilesX - 40) * 16 || topLeft.Y + player.height > (Main.maxTilesY - 40) * 16) {
                return false;
            }
            return !Collision.SolidCollision(topLeft, player.width, player.height);
        }

        private void Blink(Player player, IEntitySource source, Vector2 dest, int damage, float knockback) {
            Vector2 start = player.Center;
            //路径斩击与织影裂隙都以出发点为锚,落点写进 ai
            Projectile.NewProjectile(source, start, Vector2.Zero, ModContent.ProjectileType<LDBlinkStrike>(), (int)(damage * BlinkDamageMult), knockback, player.whoAmI, dest.X, dest.Y);
            Projectile.NewProjectile(source, start, Vector2.Zero, ModContent.ProjectileType<LDShadowRift>(), (int)(damage * RiftDamageMult), 0f, player.whoAmI, dest.X, dest.Y);
            Vector2 newPos = dest - player.Size / 2f;
            player.Teleport(newPos, 1);
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, newPos.X, newPos.Y, 1);
            }
            player.immune = true;
            player.immuneTime = Math.Max(player.immuneTime, 16);
            player.immuneNoBlink = true;
            CEUtils.PlaySound("teleport", 1.1f, dest, 4, 0.7f);
        }
        #endregion

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
            damage.Base += player.GetModPlayer<LDDaggerPlayer>().CritStack * DamagePerStack;
        }

        public override void UpdateInventory(Player player) {
            if (Item.favorited) {
                player.GetModPlayer<LDDaggerPlayer>().CompanionRequested = true;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            int stack = Main.LocalPlayer.GetModPlayer<LDDaggerPlayer>().CritStack;
            string text = this.GetLocalization("StackTooltip").Format(stack, MaxCritStack, stack * DamagePerStack);
            TooltipLine line = new TooltipLine(Mod, "LDDaggerStack", text);
            line.OverrideColor = Color.Lerp(new Color(200, 170, 255), new Color(255, 230, 140), stack / (float)MaxCritStack);
            tooltips.Add(line);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.LightShard)
                .AddIngredient(ItemID.DarkShard)
                .AddIngredient(ItemID.Ectoplasm, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    /// <summary>
    /// 光暗龙匕的玩家侧状态:暴击层数、龙的攻击演出计时、收藏召龙请求。
    /// 层数按玩家存而不是按物品存,弹幕命中处不必回查是哪一把匕首。
    /// </summary>
    public class LDDaggerPlayer : ModPlayer
    {
        public int CritStack;
        public int AttackAnimTime;
        public bool CompanionRequested;

        public override void ResetEffects() {
            CompanionRequested = false;
        }

        public override void PostUpdate() {
            if (AttackAnimTime > 0) {
                AttackAnimTime--;
            }
            if (Player.whoAmI != Main.myPlayer || Player.dead) {
                return;
            }
            int type = ModContent.ProjectileType<LightDarkDragon>();
            if (CompanionRequested && Player.ownedProjectileCounts[type] <= 0) {
                Projectile.NewProjectile(Player.GetSource_Misc("LightDarkDragon"), Player.Center, Vector2.Zero, type, 0, 0f, Player.whoAmI);
            }
        }

        /// <summary>飞刃命中回报:暴击 +1 层,未暴击清零。</summary>
        public void RegisterHit(bool crit) {
            if (crit) {
                CritStack = Math.Min(CritStack + 1, LightDarkDragonDagger.MaxCritStack);
            }
            else {
                CritStack = 0;
            }
        }

        public bool HasHelixKnives() {
            return Player.ownedProjectileCounts[ModContent.ProjectileType<LDHelixKnifeLight>()] + Player.ownedProjectileCounts[ModContent.ProjectileType<LDHelixKnifeDark>()] > 0;
        }
    }

    /// <summary>
    /// 光影印记:记录敌怪最近一次被耀光/黯影刃命中的剩余帧数。
    /// 命中处只在所有者端读写,印记消费后另一种刃需重新命中才能再次触发斩切。
    /// </summary>
    public class LDMarkNPC : GlobalNPC
    {
        public const int MarkDuration = 120;
        public override bool InstancePerEntity => true;

        public int LightMark;
        public int DarkMark;

        public override void PostAI(NPC npc) {
            if (LightMark > 0) {
                LightMark--;
            }
            if (DarkMark > 0) {
                DarkMark--;
            }
        }

        /// <summary>
        /// 一把刃命中:若目标短期内被另一种刃命中过,消费那枚印记并在目标身上追加一次光影斩切;随后落下本刃的印记。
        /// </summary>
        public void OnKnifeHit(NPC npc, Projectile knife, bool isLight, bool enhanced) {
            if (Main.myPlayer == knife.owner) {
                ref int other = ref (isLight ? ref DarkMark : ref LightMark);
                if (other > 0) {
                    other = 0;
                    int damage = (int)(knife.damage * (enhanced ? LDSlash.EnhancedDamageMult : 1f));
                    Projectile.NewProjectile(knife.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<LDSlash>(), damage, knife.knockBack, knife.owner, npc.whoAmI, enhanced ? 1f : 0f);
                }
            }
            if (isLight) {
                LightMark = MarkDuration;
            }
            else {
                DarkMark = MarkDuration;
            }
        }
    }
}
