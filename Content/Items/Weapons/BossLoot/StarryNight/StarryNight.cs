using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.StarryNight
{
    /// <summary>
    /// 暗夜繁星:星月贤者掉落的魔法书,每 0.33 秒从玩家头顶 40 格的水平线上落下 4 颗彗星(2 大 2 小)
    /// 鼠标在屏幕左半边时整体往左斜落,右半边往右;彗星本体见 <see cref="StarryNightComet"/>
    /// </summary>
    public class StarryNight : ModItem
    {
        #region 调参旋钮
        //面板伤害:每秒 3 波,每波 2 大(200%)2 小(100%),满中每秒 18 倍面板,按 75% 命中约 1620,落在 B 档 1500~1800
        public const int BaseDamage = 120;
        //施法间隔(帧):策划 0.33 秒
        public const int UseTime = 20;
        //魔力消耗:策划 6
        public const int ManaCost = 6;
        public const float Knockback = 3f;
        //售价(金币):BRIEF-B 统一 30 金
        public const int SellGold = 30;
        //贴图边长(px)
        public const int SpriteSize = 32;
        //每波彗星数与其中大彗星数
        public const int CometCount = 4;
        public const int LargeCount = 2;
        //生成线:玩家头顶以上 40 格,左右各 35 格(px)
        public const float SpawnHeight = 640f;
        public const float SpawnHalfWidth = 560f;
        //下落偏角(弧度):朝鼠标所在半屏偏 15 度,每颗再随机 ±3 度
        public const float TiltAngle = MathHelper.Pi / 12f;
        public const float AngleJitter = MathHelper.Pi / 60f;
        //施法音效:音高中心(1 为原调)、浮动全幅、音量、同时实例上限
        public const float CastPitch = 1.15f;
        public const float CastPitchVariance = 0.3f;
        public const float CastVolume = 0.55f;
        public const int CastMaxInstances = 3;
        //施法星芒:离手距离(px)、缩放、寿命(帧)、自转(弧度/帧)、光晕倍率
        public const float CastSparkReach = 18f;
        public const float CastSparkScale = 0.5f;
        public const int CastSparkLife = 14;
        public const float CastSparkSpin = 0.1f;
        public const float CastSparkBloom = 1.4f;
        //施法光尘:数量、散布半径(px)、上飘速度范围与扰动(px/帧)、缩放范围、寿命(帧)
        public const int CastMoteCount = 3;
        public const float CastMoteRadius = 6f;
        public const float CastMoteMinRise = 0.6f;
        public const float CastMoteMaxRise = 1.6f;
        public const float CastMoteJitter = 0.5f;
        public const float CastMoteMinScale = 0.18f;
        public const float CastMoteMaxScale = 0.3f;
        public const int CastMoteLife = 18;
        #endregion

        public override void SetDefaults() {
            Item.width = Item.height = SpriteSize;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.UseSound = CEUtils.GetSound("LunarStar", CastPitch, CastMaxInstances, CastVolume) with { PitchVariance = CastPitchVariance };
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.shoot = ModContent.ProjectileType<StarryNightCometSmall>();
            Item.shootSpeed = StarryNightCometSmall.FlightSpeed;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            //Shoot 只在主人端跑:鼠标半屏、落点、初始目标都在这里定死,随生成包同步
            float fall = MathHelper.PiOver2 + (Main.MouseScreen.X < Main.screenWidth / 2f ? TiltAngle : -TiltAngle);
            //生成线等分成 CometCount 段,每段落一颗,大小随机分进各段
            bool[] large = new bool[CometCount];
            for (int i = 0; i < LargeCount; i++) {
                large[i] = true;
            }
            for (int i = CometCount - 1; i > 0; i--) {
                int j = Main.rand.Next(i + 1);
                (large[i], large[j]) = (large[j], large[i]);
            }
            float segment = SpawnHalfWidth * 2f / CometCount;
            float top = player.Top.Y - SpawnHeight;
            for (int i = 0; i < CometCount; i++) {
                Vector2 spawn = new(player.Center.X - SpawnHalfWidth + segment * (i + Main.rand.NextFloat()), top);
                int kind = large[i] ? ModContent.ProjectileType<StarryNightCometLarge>() : ModContent.ProjectileType<StarryNightCometSmall>();
                float speed = large[i] ? StarryNightCometLarge.FlightSpeed : StarryNightCometSmall.FlightSpeed;
                int dmg = large[i] ? (int)(damage * StarryNightCometLarge.DamageMult) : damage;
                Vector2 vel = (fall + Main.rand.NextFloat(-AngleJitter, AngleJitter)).ToRotationVector2() * speed;
                NPC target = CEUtils.FindTarget_HomingProj(player, spawn, StarryNightComet.HomingRange);
                Projectile.NewProjectile(source, spawn, vel, kind, dmg, knockback, player.whoAmI, target?.whoAmI ?? -1, player.Center.Y);
            }
            return false;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            //首帧 itemRotation 还没转向鼠标,第二帧再放星芒;各端都跑,队友也看得见
            if (Main.dedServ || player.itemAnimation != player.itemAnimationMax - 1) {
                return;
            }
            Vector2 tip = player.RotatedRelativePoint(player.MountedCenter) + player.itemRotation.ToRotationVector2() * player.direction * CastSparkReach;
            PRTLoader.NewParticle<PRT_SparkleCal>(tip, Vector2.Zero, SageVfx.StarWhite, CastSparkScale).Configure(SageVfx.StarBlue, CastSparkLife, CastSparkSpin, CastSparkBloom);
            for (int i = 0; i < CastMoteCount; i++) {
                Vector2 vel = new Vector2(0f, -Main.rand.NextFloat(CastMoteMinRise, CastMoteMaxRise)) + CEUtils.randomPointInCircle(CastMoteJitter);
                Color tint = i % 2 == 0 ? StarryNightCometLarge.GlowColor : StarryNightCometSmall.GlowColor;
                PRTLoader.NewParticle<PRT_Light>(tip + CEUtils.randomPointInCircle(CastMoteRadius), vel, tint,
                    Main.rand.NextFloat(CastMoteMinScale, CastMoteMaxScale)).Configure(1f, lifetime: CastMoteLife);
            }
        }
    }
}
