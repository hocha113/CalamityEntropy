using CalamityEntropy.Content.Items.Weapons.VoidDestroyer;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PrayerlightLance
{
    public class PrayerlightLance : ModItem
    {
        //面板:165 × 3.75 刺/秒 × (1 + 0.75 + 0.5) ≈ 1390/秒(贴身、光枪与激光全中)
        public const int BaseDamage = 165;
        public const int UseTime = 16;
        public const float Knockback = 5f;
        //长矛本体的局部无敌帧
        public const int StabHitCooldown = 8;

        //光枪:75% 伤害,穿透 7 次(第 8 次命中后消失),前 3 次命中各降一道激光
        public const float SpearDamageMult = 0.75f;
        public const float SpearKnockbackMult = 0.5f;
        public const int SpearPenetrate = 8;
        public const int SpearLaserHits = 3;
        //像素/帧
        public const float SpearSpeed = 26f;

        //神圣激光:50% 伤害
        public const float LaserDamageMult = 0.5f;

        //光之能量:累计攻击 12 秒
        public const int EnergyMax = 720;

        //圣光洗礼:3 秒,每 0.2 秒一道激光,锁定鼠标 12 格内的目标
        public const int BaptismFrames = 180;
        public const int BaptismInterval = 12;
        public const float BaptismRadius = 12 * 16f;

        //配方烈阳之光
        public const int SunlightCost = 12;
        public const int SellGold = 25;

        public override void SetDefaults() {
            Item.width = 64;
            Item.height = 64;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = UseTime;
            Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = Knockback;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.rare = ItemRarityID.Cyan;
            Item.shoot = ModContent.ProjectileType<PrayerlightLanceHeld>();
            Item.shootSpeed = 1f;
        }

        public override bool AltFunctionUse(Player player) => true;

        //刺击使用中的每一帧都算攻击时间,包括两刺之间的空档;洗礼期间 altFunctionUse 保持为 2,不计
        public override void HoldItem(Player player) {
            if (player.whoAmI != Main.myPlayer || player.itemAnimation <= 0 || player.altFunctionUse == 2) {
                return;
            }
            if (player.GetModPlayer<PrayerlightLancePlayer>().Charge()) {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<PrayerlightLanceReady>(), 0, 0f, player.whoAmI);
            }
        }

        public override bool MeleePrefix() => true;

        public override bool CanUseItem(Player player) {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<PrayerlightLanceHeld>()] > 0) {
                return false;
            }
            if (player.altFunctionUse == 2) {
                //能量只在拥有者端累计,远端不模拟右键
                return player.whoAmI == Main.myPlayer && player.GetModPlayer<PrayerlightLancePlayer>().Ready;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            PrayerlightLancePlayer energy = player.GetModPlayer<PrayerlightLancePlayer>();
            if (player.altFunctionUse == 2) {
                energy.Energy = 0;
                Projectile.NewProjectile(source, player.MountedCenter, dir, type, damage, knockback, player.whoAmI, PrayerlightLanceHeld.ModeBaptism, 1f);
                return false;
            }
            Projectile.NewProjectile(source, player.MountedCenter, dir, type, damage, knockback, player.whoAmI, PrayerlightLanceHeld.ModeStab, energy.Progress);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<BlazingSunlight>(SunlightCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    /// <summary>光之能量只在拥有者端累计,停手不清零,释放洗礼时归零;其他端看长矛弹幕出手时带的 ai[1]</summary>
    public class PrayerlightLancePlayer : ModPlayer
    {
        //帧
        public int Energy;

        public bool Ready => Energy >= PrayerlightLance.EnergyMax;

        public float Progress => MathHelper.Clamp(Energy / (float)PrayerlightLance.EnergyMax, 0f, 1f);

        /// <summary>攻击一帧,刚好积满的那帧返回 true</summary>
        public bool Charge() {
            if (Ready) {
                return false;
            }
            Energy++;
            return Ready;
        }
    }

    /// <summary>102×102,矛尖朝右上,坐标取 2×2 块中心</summary>
    public static class PrayerlightLanceGeometry
    {
        public const string TexturePath = "CalamityEntropy/Content/Items/Weapons/Aftermath/PrayerlightLance/PrayerlightLance";
        public static readonly Vector2 Butt = new Vector2(3f, 99f);
        public static readonly Vector2 Tip = new Vector2(99f, 3f);
        //握点在蓝色缠布中段
        public static readonly Vector2 Pivot = new Vector2(23f, 79f);
        //日徽中心
        public static readonly Vector2 Emblem = new Vector2(69f, 33f);
        public static readonly float AxisAngle = MathF.Atan2(Tip.Y - Butt.Y, Tip.X - Butt.X);
        public static readonly float TipFromPivot = Vector2.Distance(Pivot, Tip);
    }

    /// <summary>光柱与落点爆闪,配色取太阳女神那一套;Beam 必须在加法批次里调用</summary>
    public static class PrayerlightLanceFx
    {
        private static readonly Vector2[] column = new Vector2[3];

        /// <summary>从 top 到 bottom 的光柱:宽余烬晕、金色主体、白热芯,顶端渐入</summary>
        public static void Beam(Vector2 top, Vector2 bottom, float width, float opacity) {
            if (opacity <= 0.01f) {
                return;
            }
            column[0] = top;
            column[1] = Vector2.Lerp(top, bottom, 0.5f);
            column[2] = bottom;
            VDWeaponFx.Ribbon(column, _ => width * 2.4f, t => SunVfx.Ember * (0.35f * opacity * FadeIn(t)));
            VDWeaponFx.Ribbon(column, _ => width * 1.3f, t => SunVfx.Gold * (0.7f * opacity * FadeIn(t)));
            VDWeaponFx.Ribbon(column, _ => width * 0.45f, t => SunVfx.Core * (opacity * FadeIn(t)));
        }

        //光柱上段 25% 渐入
        private static float FadeIn(float t) => MathHelper.Clamp(t * 4f, 0f, 1f);

        /// <summary>落点爆闪:金环、星芒、向两侧上方溅开的火花(光柱自上而下打下来)</summary>
        public static void Strike(Vector2 at, float size) {
            if (Main.dedServ) {
                return;
            }
            PRTLoader.NewParticle<PRT_PulseRing>(at, Vector2.Zero, SunVfx.Gold, 0.1f).Configure(0.7f * size, 16);
            PRTLoader.NewParticle<PRT_SparkleCal>(at, Vector2.Zero, SunVfx.Core, 1.4f * size).Configure(SunVfx.Ember, 14, 0f, 2f);
            int sparks = (int)(8 * size);
            for (int i = 0; i < sparks; i++) {
                Vector2 v = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, -0.25f)).SafeNormalize(-Vector2.UnitY) * Main.rand.NextFloat(6f, 13f);
                Color c = Color.Lerp(SunVfx.Gold, SunVfx.Core, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_GlowSparkCal>(at, v, c, Main.rand.NextFloat(0.035f, 0.055f)).Configure(false, 12, new Vector2(2.4f, 0.8f), true);
            }
        }
    }
}
