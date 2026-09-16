using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AbyssalVortex : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 256;
            Projectile.height = 256;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 2f;
            Projectile.timeLeft = 80;
            Projectile.penetrate = -1;
            Projectile.ArmorPenetration = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }
        public int ct = 0;
        float scale = 0;
        float scalej = 0.25f;
        public override void AI() {
            scale += scalej;
            scalej -= 0.03f;
            if (scale < 0) {
                Projectile.Kill();
            }
            Projectile.rotation += 0.2f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return Projectile.Center.getRectCentered(188 * scale, 188 * scale).Intersects(targetHitbox);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }
        public void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f) {
            Main.spriteBatch.End();
            Effect effect = CEEffectAssets.Vortex;
            effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
            effect.Parameters["Strength"].SetValue(22);
            effect.Parameters["AspectRatio"].SetValue(1);
            effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
            float fadeOutDistance = 0.06f;
            float fadeOutWidth = 0.3f;
            effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
            effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
            effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(CEExtraAssets.VoronoiShapes, pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, CEExtraAssets.VoronoiShapes.Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
            CEUtils.DrawGlow(pos, Color.White * 0.4f * glow, 0.8f * Size * glow);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D t1 = TextureAssets.Projectile[Projectile.type].Value;
            //Main.spriteBatch.Draw(t1, Projectile.Center - Main.screenPosition, null, Color.DarkBlue, Projectile.rotation, new Vector2(t1.Width, t1.Height) / 2, 188f / 408f * scale, SpriteEffects.None, 0);
            DrawVortex(Projectile.Center, new Color(40, 40, 200), 3.8f * scale, 2);
            return false;
        }


    }


}