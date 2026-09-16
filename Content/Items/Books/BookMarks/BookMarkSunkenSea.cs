using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkSunkenSea : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 5);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("SunkenSea");
        public override EBookProjectileEffect getEffect() {
            return new SunkenSeaBMEffect();
        }

        public override Color tooltipColor => Color.SkyBlue;
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Diamond, 10)
                .AddIngredient(ItemID.Glass, 20)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    public class SunkenSeaBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            Vector2 shotDir = CEUtils.randomRot().ToRotationVector2();
            Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center + shotDir * 32, shotDir * 6, ModContent.ProjectileType<SunkenAquashard>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 8, projectile.DamageType), projectile.knockBack / 3, projectile.owner).ToProj().DamageType = projectile.DamageType;
            SoundEngine.PlaySound(in SoundID.Item27, projectile.Center);
        }
    }

    // 原灾厄 AquashardSplit 的自有等效: 带重力的小水晶碎片
    public class SunkenAquashard : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 300;
        }
        public override void AI() {
            if (Projectile.localAI[0]++ > 10)
                Projectile.velocity.Y += 0.2f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            var p = PRTLoader.NewParticle<PRT_GlowLightParticle>(Projectile.Center, CEUtils.randomPointInCircle(1), Color.LightSkyBlue, Main.rand.NextFloat(0.4f, 0.7f));
            p.lightColor = Color.LightSkyBlue * 0.1f;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
        }
        public override void OnKill(int timeLeft) {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f }, Projectile.Center);
            for (int i = 0; i < 8; i++) {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.DungeonWater);
                d.noGravity = true;
                d.velocity = CEUtils.randomPointInCircle(4);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = CEExtraAssets.Diamond;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.LightSkyBlue, Projectile.rotation, tex.Size() / 2f, new Vector2(2.2f, 0.7f) * 0.1f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() / 2f, new Vector2(2.2f, 0.7f) * 0.06f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}