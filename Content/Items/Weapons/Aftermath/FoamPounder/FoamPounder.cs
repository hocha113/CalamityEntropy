using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.FoamPounder
{
    public class FoamPounder : ModItem
    {
        public const int BaseDamage = 55;           //任务面板
        public const int UseFrames = 6;             //帧/发,每秒10发
        public const int ManaCost = 3;
        public const float ShootSpeed = 15f;        //像素/帧
        public const float Knockback = 2f;
        public const int BubblesPerShot = 2;        //每秒20个泡泡
        public const float Spread = 0.2f;           //弧度,单侧
        public const float SpeedMinMult = 0.6f;
        public const float SpeedMaxMult = 1.1f;
        public const float SizeMin = 0.75f;         //泡泡体积倍率
        public const float SizeMax = 1.25f;
        public const float MuzzleLength = 44f;      //像素,握点到喇叭口
        public static readonly Vector2 Holdout = new(-24f, 0f);   //像素,握把对手
        public const int EssenceCost = 8;           //配方怨灵精华
        public const int LunarBarCost = 5;
        public const int SellGold = 25;

        public override void SetDefaults() {
            Item.width = 70;
            Item.height = 38;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item85 with { Volume = 0.55f, PitchVariance = 0.2f, MaxInstances = 3 };
            Item.shoot = ModContent.ProjectileType<FoamPounderBubble>();
            Item.shootSpeed = ShootSpeed;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override Vector2? HoldoutOffset() => Holdout;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 muzzle = position + velocity.SafeNormalize(Vector2.UnitX) * MuzzleLength;
            if (!Collision.CanHit(position, 0, 0, muzzle, 0, 0))
                muzzle = position;
            for (int i = 0; i < BubblesPerShot; i++) {
                Vector2 v = velocity.RotatedByRandom(Spread) * Main.rand.NextFloat(SpeedMinMult, SpeedMaxMult);
                Projectile.NewProjectile(source, muzzle, v, type, damage, knockback, player.whoAmI, Main.rand.NextFloat(SizeMin, SizeMax));
            }
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.BubbleGun)
                .AddIngredient<WraithSoulEssence>(EssenceCost)
                .AddIngredient(ItemID.LunarBar, LunarBarCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class FoamPounderBubble : ModProjectile
    {
        public const int LifeFrames = 120;          //2秒后破裂
        public const int FadeFrames = 12;
        public const float BaseSize = 18f;          //像素,倍率1时判定边长
        public const float Drag = 0.965f;           //每帧,原版泡泡0.96
        public const float MinSpeed = 2.5f;         //像素/帧
        public const float Buoyancy = 0.04f;        //像素/帧²,缓慢上浮
        public const float HomingRange = 320f;      //像素,20格
        public const float HomingTurn = 0.06f;      //弧度/帧
        public const int HitsPerBubble = 2;
        public const int RegenFrames = 120;         //命中后再生2秒
        public const int RegenBonus = 2;            //lifeRegen,即每秒1生命
        public const float WobbleSpeed = 0.15f;     //弧度/帧
        public const float WobbleAmp = 0.08f;
        public const float BloomSize = 40f;         //像素,倍率1时
        public const float CurrentSpeedMult = 1.1f; //海啸水流相对泡泡速度
        public const int PopDust = 8;

        //ai[0] 体积倍率,随生成包同步
        private float wobble;

        public override void SetDefaults() {
            Projectile.width = (int)BaseSize;
            Projectile.height = (int)BaseSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = HitsPerBubble;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifeFrames;
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.scale = Projectile.ai[0] > 0f ? Projectile.ai[0] : 1f;
                int size = (int)(BaseSize * Projectile.scale);
                Projectile.Resize(size, size);
                wobble = Main.rand.NextFloat(MathHelper.TwoPi);
                CurrentEffect();
            }
            Projectile.velocity *= Drag;
            if (Projectile.velocity.Length() < MinSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY) * MinSpeed;
            Projectile.velocity.Y -= Buoyancy;
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange, (p, n) => p.localNPCImmunity[n] == 0);
            if (target != null) {
                float rot = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), HomingTurn);
                Projectile.velocity = rot.ToRotationVector2() * Projectile.velocity.Length();
            }
            wobble += WobbleSpeed;
            Lighting.AddLight(Projectile.Center, 0.08f, 0.2f, 0.35f);
        }

        private void CurrentEffect() {
            if (Main.dedServ)
                return;
            Color water = Color.Lerp(new Color(110, 190, 255), new Color(190, 235, 255), Main.rand.NextFloat());
            PRTLoader.NewParticle<PRT_LineCal>(Projectile.Center, Projectile.velocity * CurrentSpeedMult, water, Main.rand.NextFloat(0.6f, 1f))
                .Configure(false, Main.rand.Next(14, 20));
            if (Main.rand.NextBool(2)) {
                Vector2 side = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-12f, 12f);
                Dust d = Dust.NewDustPerfect(Projectile.Center + side, DustID.DungeonWater, Projectile.velocity * 0.6f, 0, default, 1.1f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Projectile.GetOwner().GetModPlayer<FoamPounderPlayer>().RegenTimer = RegenFrames;
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.45f, PitchVariance = 0.3f, MaxInstances = 4 }, Projectile.Center);
            float r = Projectile.width * 0.5f;
            for (int i = 0; i < PopDust; i++) {
                Vector2 dir = (MathHelper.TwoPi * i / PopDust).ToRotationVector2();
                Dust d = Dust.NewDustPerfect(Projectile.Center + dir * r, DustID.BubbleBurst_White, dir * Main.rand.NextFloat(1f, 2.5f), 0, default, 1f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float fade = Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);
            float s = MathF.Sin(wobble) * WobbleAmp;
            Vector2 squash = new Vector2(1f + s, 1f - s) * Projectile.scale;
            Color body = Color.Lerp(lightColor, Color.White, 0.5f) * fade;
            Main.EntitySpriteDraw(tex, pos, null, body, 0f, tex.Size() / 2f, squash, SpriteEffects.None);
            Texture2D bloom = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(bloom, pos, null, new Color(90, 160, 255, 0) * (0.35f * fade), 0f, bloom.Size() / 2f, BloomSize * Projectile.scale / bloom.Width, SpriteEffects.None);
            return false;
        }
    }

    public class FoamPounderPlayer : ModPlayer
    {
        //泡泡命中后的再生剩余帧
        public int RegenTimer;

        public override void UpdateDead() {
            RegenTimer = 0;
        }

        public override void UpdateLifeRegen() {
            if (RegenTimer <= 0)
                return;
            RegenTimer--;
            Player.lifeRegen += FoamPounderBubble.RegenBonus;
        }
    }
}
