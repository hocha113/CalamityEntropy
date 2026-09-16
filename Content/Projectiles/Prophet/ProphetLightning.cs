using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Prophet
{
    public class ProphetLightning : ModProjectile
    {
        public SoundStyle sound = new SoundStyle("CalamityEntropy/Assets/Sounds/flashback");
        List<Vector2> points = new List<Vector2>();
        List<Vector2> pointVels = new List<Vector2>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
        }
        public override void SetDefaults() {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ArmorPenetration = 12;
            Projectile.timeLeft = 100;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 8 * 60);
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void AI() {
            if (points.Count == 0) {
                Projectile.Center -= Projectile.velocity;
                Vector2 p = Projectile.Center;
                for (int i = 0; i < 240; i++) {
                    Vector2 rd = CEUtils.randomPointInCircle(32);
                    points.Add(p + rd * 1.64f);
                    pointVels.Add(-rd * 0.1f);
                    p += Projectile.velocity * 0.8f;
                    Projectile.velocity = Projectile.velocity.RotatedByRandom(0.2f);
                }
            }
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 80 && !Main.dedServ) {
                SoundEngine.PlaySound(sound, Projectile.Center);
                ScreenShaker.AddShake(new ScreenShaker.NoDirQuickShake(6));
            }
            if (Projectile.ai[0] < 80) {
                for (int i = 0; i < points.Count; i++) {
                    points[i] += pointVels[i];
                    pointVels[i] *= 0.94f;
                }
            }

        }
        public override bool CanHitPlayer(Player target) {
            if (Projectile.ai[0] < 81) {
                return false;
            }
            return true;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (points.Count < 1) {
                return false;
            }
            for (int i = 1; i < points.Count; i++) {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, 4)) {
                    return true;
                }
            }
            return false;
        }
        public float width = 0;
        public override bool PreDraw(ref Color lightColor) {
            width = (20 - (Projectile.ai[0] - 80)) / 20f;
            if (points.Count < 1) {
                return false;
            }
            float lw = 0.7f * ((36f - Projectile.ai[0]) / 36f);
            Color color = Color.White;

            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
            if (Projectile.ai[0] > 80) {

                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.White;

                float a = 1 - ((Projectile.ai[0] - 80) / 20f);
                float lr = 0;
                for (int i = 1; i < points.Count; i++) {
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 26 * width,
                          new Vector3(i * 0.1f + -16 * Main.GlobalTimeWrappedHourly, 1, 1),
                        b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 26 * width,
                          new Vector3(i * 0.1f + -16 * Main.GlobalTimeWrappedHourly, 0, 1),
                          b));
                    lr = (points[i] - points[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.Streak2;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            else {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.White;

                float a = 0.6f * (Projectile.ai[0] / 80f);
                float lr = 0;
                for (int i = 1; i < points.Count; i++) {
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 1.6f,
                          new Vector3((float)(i + 1) / points.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 1.6f,
                          new Vector3((float)(i + 1) / points.Count, 0, 1),
                          b * a));
                    lr = (points[i] - points[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.white;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

}