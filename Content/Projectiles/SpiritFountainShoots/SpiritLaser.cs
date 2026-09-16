using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Utilities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SpiritFountainShoots
{
    public class SpiritLaser : ModProjectile
    {
        public List<List<Vector2>> lasers;
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }
        public override void SetDefaults() {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 16;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void AI() {
            if (Projectile.ai[2] == 0) {
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/BaitReady") { Pitch = 2 }, Projectile.Center);
                lasers = new();
                Projectile.ai[2]++;
                for (int zz = 0; zz < 8; zz++) {
                    var p = LightningGenerator.GenerateLightning(Projectile.Center, Projectile.Center + Projectile.velocity * 500 + CEUtils.randomPointInCircle(64 * (Projectile.ai[0] + 1)), 16, 6);
                    for (int i = 1; i < p.Count; i++) {
                        p[i] += CEUtils.randomPointInCircle(50 * (Projectile.ai[0] + 1));
                    }
                    lasers.Add(p);
                }
            }
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity * 500, targetHitbox, 46);
        }

        public float PrimitiveWidthFunction(float completionRatio, Vector2 vertex) => (new Vector2(1, 0).RotatedBy((completionRatio + 0.14f) * MathHelper.Pi)).Y * Projectile.scale * 26 * ((36f - Projectile.ai[0]) / 36f) * CEUtils.Parabola(1 - Projectile.timeLeft / 16f, 1);

        public Color PrimitiveColorFunction(float completionRatio, Vector2 vertex) {
            float colorInterpolant = (float)Math.Sin(Projectile.identity / 3f + completionRatio * 20f + Main.GlobalTimeWrappedHourly * 1.1f) * 0.5f + 0.5f;
            Color color = CEUtils.MulticolorLerp(colorInterpolant, new Color(Main.rand.Next(20, 100), 204, 250), new Color(Main.rand.Next(20, 100), 204, 250));

            return color;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (lasers == null) {
                return false;
            }
            Texture2D lm = CEExtraAssets.Glow2;
            float lw = 0.7f * ((36f - Projectile.ai[0]) / 36f);
            Color color = Color.White;
            if (Projectile.ai[2] == 1) {
                color = Color.Red;
            }
            GameShaders.Misc["CalamityEntropy:HeavenlyGaleLightningArc"].UseImage1("Images/Misc/Perlin");
            GameShaders.Misc["CalamityEntropy:HeavenlyGaleLightningArc"].Apply();
            for (int i = 0; i < lasers.Count; i++) {
                var points = lasers[i];
                CEPrimitiveRenderer.RenderTrail(points, new CEPrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction, (_, _) => Projectile.Size * 0.2f * lw, false,
                    shader: GameShaders.Misc["CalamityEntropy:HeavenlyGaleLightningArc"]), 10);
            }

            return false;
        }
    }

}