using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Donator;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class InkTrail : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 30;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 20, 1);
        }
        public override void AI() {
            var player = Projectile.GetOwner();
            if (player.mount.Active && player.mount.Type == ModContent.MountType<ReplicaPenMount>()) {
                Projectile.timeLeft = 3;
            }
            Projectile.Center = player.MountedCenter + new Vector2(-46 * player.direction, 24).RotatedBy(player.fullRotation);
            odp.Add(Projectile.Center);
            if (odp.Count > 64) {
                odp.RemoveAt(0);
            }
        }
        float trailOffset = 0;

        public override bool PreDraw(ref Color lightColor) {
            float c = 0;
            trailOffset += 0.04f;

            c = 0;
            if (odp.Count > 2) {
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.Black;
                float d = 0;
                for (int i = 1; i < odp.Count; i++) {
                    float width = 0;
                    width = 1;
                    c += 1f / odp.Count;
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + new Vector2(28 * width, 0).RotatedBy((odp[i] - odp[i - 1]).ToRotation() + MathHelper.PiOver2),
                          new Vector3(d + trailOffset, 1, 1),
                          b * ((float)i / (float)odp.Count)));
                    ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + new Vector2(-28 * width, 0).RotatedBy((odp[i] - odp[i - 1]).ToRotation() + MathHelper.PiOver2),
                          new Vector3(d + trailOffset, 0, 1),
                          b * ((float)i / (float)odp.Count)));
                    d += CEUtils.getDistance(odp[i - 1], odp[i]) / 256f;
                }
                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEUtils.getExtraTex("TrailInk");
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            for (int i = 1; i < odp.Count; i++) {
                if (CEUtils.LineThroughRect(odp[i - 1], odp[i], targetHitbox, 30)) {
                    return true;
                }
            }
            return false;
        }
    }

}