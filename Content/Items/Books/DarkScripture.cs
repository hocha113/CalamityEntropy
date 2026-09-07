using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityEntropy.Content.Items.Books
{
    public class DarkScripture : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 38;
            Item.useAnimation = Item.useTime = 24;
            Item.mana = 13;
            Item.rare = ItemRarityID.Red;
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/DS")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<DarkScriptureHeld>();
        public override int SlotCount => 2;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<OuijaBoard>()
                .AddIngredient(ItemID.SoulofFright, 15)
                .AddIngredient(ItemID.SoulofNight, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class DarkScriptureHeld : EntropyBookDrawingAlt
    {
        public override string OpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/DarkScripture/Open";
        public override string PageAnimationPath => $"{EntropyBook.BaseFolder}/Textures/DarkScripture/Page";
        public override string UIOpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/DarkScripture/UI";
        public override int frameChange => 3;
        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            m.Homing = 0.5f;
            m.HomingRange = 0.625f;
            m.armorPenetration += 16;
            return m;
        }
        public override float randomShootRotMax => 0.16f;
        public override int baseProjectileType => ModContent.ProjectileType<DarkBullet>();
    }
    public class DarkBullet : EBookBaseProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = true;
            Projectile.light = 0.2f;
            Projectile.timeLeft = 800;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 2;
        }
        public void Explode()
        {
            CEUtils.PlaySound("blackholeEnd", Main.rand.NextFloat(1.2f, 1.6f), Projectile.Center, volume: 0.4f);
            //CustomPulse贴图路径现传,CalamityPorts走PRTPathTextures,三层爆炸+Shine是旧版粒子系统原样
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 80, 80), 0.02f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, 0, 0.02f, 0.1f, 16);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 80, 80), 0.02f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, 0, 0.02f, 0.09f, 16);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 80, 80), 0.02f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, 0, 0.02f, 0.08f, 16);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Red * 0.8f, 1.7f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            if (Projectile.owner == Main.myPlayer)
            {
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage, 120, Projectile.DamageType);
            }
            CEUtils.SetShake(Projectile.Center, 6);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Explode();
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return base.OnTileCollide(oldVelocity);
        }
        public PRT_TrailParticle t1;
        public PRT_TrailParticle t2;
        public override void AI()
        {
            base.AI();
            if (t1 == null || t2 == null)
            {
                t1 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, new Color(255, 16, 16), 1f);
                t1.maxLength = 20;
                t1.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
                t2 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, new Color(255, 16, 16), 1f);
                t2.maxLength = 20;
                t2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            }
            t1.AddPoint(Projectile.Center + Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * 12 * Projectile.scale);
            t2.AddPoint(Projectile.Center + Projectile.velocity.normalize().RotatedBy(-MathHelper.PiOver2) * 12 * Projectile.scale);
            t1.Lifetime = 12;
            t2.Lifetime = 12;
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 9)
            {
                oldPos.RemoveAt(0);
            }
            if (Projectile.timeLeft % 3 == 0)
            {
                PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Projectile.velocity * 0.1f, new Color(255, 42, 42), 0.46f * Projectile.scale).Configure(new Vector2(0.38f, 1f), Projectile.velocity.ToRotation(), 0.1f * Projectile.scale, 38);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float scale = 2.2f * Projectile.scale;
            List<Vector2> points = new();
            for (int i = 1; i < oldPos.Count; i++)
            {
                for (float j = 0.1f; j <= 1f; j += 0.1f)
                {
                    points.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }
            }
            for (int i = 0; i < points.Count; i++)
            {
                float c = (i + 1f) / points.Count;
                DrawDark(points[i], scale * c * 0.6f, Projectile.Opacity * c);
            }
            for (int i = 0; i < points.Count; i++)
            {
                float c = (i + 1f) / points.Count;
                DrawEnergyBall(points[i], scale * c, Projectile.Opacity * c);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public void DrawEnergyBall(Vector2 pos, float size, float alpha)
        {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(160, 90, 90) * alpha, Projectile.rotation, tex.Size() * 0.5f, new Vector2(1 + (Projectile.velocity.Length() * 0.01f), 1) * size * 0.25f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(60, 2, 2) * alpha, Projectile.rotation, tex.Size() * 0.5f, new Vector2(1 + (Projectile.velocity.Length() * 0.01f), 1) * size * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
        }
        public void DrawDark(Vector2 pos, float size, float alpha)
        {
            CEUtils.DrawGlow(pos, Color.Black * alpha * Projectile.Opacity, size, false);
        }
    }
}
