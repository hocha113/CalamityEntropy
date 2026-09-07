using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books
{
    public class BloodCodex : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 17;
            Item.useAnimation = Item.useTime = 14;
            Item.crit = 6;
            Item.mana = 4;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(gold: 1);
            Item.width = 40;
            Item.height = 52;
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BCdx")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<BloodCodexHeld>();
        public override int SlotCount => 1;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Silk, 10)
                .AddIngredient(ItemID.TissueSample, 10)
                .AddIngredient(ItemID.MeteoriteBar, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public class BloodCodexHeld : EntropyBookDrawingAlt
    {
        public override int frameChange => 2;
        public override string OpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/BloodCodex/Open";
        public override string PageAnimationPath => $"{EntropyBook.BaseFolder}/Textures/BloodCodex/Page";
        public override string UIOpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/BloodCodex/UI";

        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            if (Main.rand.NextBool(6))
                m.lifeSteal++;
            else
                m.lifeSteal += 0.1f;
            m.armorPenetration += 32;
            return m;
        }
        public override bool Shoot()
        {
            base.Shoot();
            //PRT_Smoke timeleftmax/vd字段spawn后直赋,旧EParticle Smoke初始化器拆出来的
            var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center, Projectile.rotation.ToRotationVector2().RotatedByRandom(randomShootRotMax) * Main.rand.NextFloat(4, 20) * 1f, Color.Red, 0.5f);
            p.timeleftmax = 30;
            p.Lifetime = 30;
            p.scaleStart = 0.01f;
            p.scaleEnd = Main.rand.NextFloat(0.36f, 0.8f) * 0.6f;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);
            p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center, Projectile.rotation.ToRotationVector2().RotatedByRandom(randomShootRotMax) * Main.rand.NextFloat(4, 20) * 1f, Color.Red, 0.5f);
            p.timeleftmax = 30;
            p.Lifetime = 30;
            p.scaleStart = 0.01f;
            p.scaleEnd = Main.rand.NextFloat(0.36f, 0.8f) * 0.6f;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);
            p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center, Projectile.rotation.ToRotationVector2().RotatedByRandom(randomShootRotMax) * Main.rand.NextFloat(4, 20) * 1f, Color.Red, 0.5f);
            p.timeleftmax = 30;
            p.Lifetime = 30;
            p.scaleStart = 0.01f;
            p.scaleEnd = Main.rand.NextFloat(0.36f, 0.8f) * 0.6f;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);

            return true;
        }
        public override float randomShootRotMax => 0.05f;
        public override int baseProjectileType => ModContent.ProjectileType<BloodSpray>();
    }
    public class BloodSpray : EBookBaseProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = true;
            Projectile.light = 0.8f;
            Projectile.timeLeft = 400;
            Projectile.penetrate = 1;
        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            for (int i = 0; i < 12; i++)
            {
                PRTLoader.NewParticle<PRT_BloodCal>(Projectile.Center, CEUtils.randomPointInCircle(16), baseColor, Main.rand.NextFloat(0.6f, 1f)).Configure(26);
            }
        }
        public override void AI()
        {
            base.AI();
            if (Projectile.localAI[2]++ > 10)
                Projectile.velocity.Y += 0.26f;
            for (float i = 0; i < 1; i += 0.1f)
            {
                var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * i, Vector2.Zero, new Color(255, 30, 30), 0.04f);
                p.timeleftmax = 10;
                p.Lifetime = 10;
                p.scaleStart = 0.04f * (0.9f + i * 0.1f);
                p.scaleEnd = 0f;
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            }
        }
        public override Color baseColor => Color.Red;
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff<FlamingBlood>(10 * 60);
        }
    }
}
