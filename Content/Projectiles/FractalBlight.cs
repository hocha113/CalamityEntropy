using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FractalBlight : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
        }
        NPC homing = null;
        public override void AI() {
            if (homing == null) {
                homing = Projectile.FindTargetWithinRange(1200);
            }
            else {
                if (!homing.active) {
                    homing = null;
                }
            }
            if (homing != null) {
                float homingStrength = Projectile.ai[1] == 1 ? 1.6f : 1.12f;
                Projectile.velocity += (homing.Center - Projectile.Center).normalize() * homingStrength;
                Projectile.velocity *= 0.96f;
            }
            if (Projectile.timeLeft < 60) {
                Projectile.Opacity -= 1 / 60f;
            }
        }
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color color = (Projectile.ai[1] == 1 ? Color.Lerp(new Color(255, 120, 130), new Color(255, 160, 170), completionRatio) : Color.Lerp(Color.White, Color.Gold, completionRatio)) * (1 - completionRatio) * Projectile.Opacity;
            return color;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            float widthInterpolant = Utils.GetLerpValue(0f, 0.25f, completionRatio, true) * Utils.GetLerpValue(1.1f, 0.7f, completionRatio, true);
            return MathHelper.SmoothStep(8f, 20f, widthInterpolant);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.ai[1] == 1) {
                target.AddBuff(ModContent.BuffType<ElementalMix>(), 400);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.SylvestaffStreakAsset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(Projectile.oldPos, new(TrailWidth, TrailColor, (_, _) => Projectile.Size * 0.5f, shader: GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();

            Color color = Color.LightGoldenrodYellow;
            if (Projectile.ai[1] == 1) {
                color = new Color(255, 160, 185);
            }
            color.A = 0;
            Vector2 vector = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Texture2D value16 = Projectile.GetTexture();
            Microsoft.Xna.Framework.Color color36 = color;
            Vector2 origin7 = value16.Size() / 2f;
            Microsoft.Xna.Framework.Color color37 = color * 0.5f;
            float num165 = 18 * (1f + 0.2f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * ((float)Math.PI * 2f) * 3f)) * 0.8f;
            Vector2 vector31 = new Vector2(0.5f, 1.4f) * num165 * 0.02f;
            Vector2 vector32 = new Vector2(0.5f, 1f) * num165 * 0.02f;
            color36 *= num165;
            color37 *= num165;
            int num166 = 0;
            Vector2 position4 = vector + Projectile.velocity.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(0.5f, 1f, Projectile.localAI[0] / 60f) * num166;
            var dir = SpriteEffects.None;
            Main.EntitySpriteDraw(value16, position4, null, color36 * Projectile.Opacity, (float)Math.PI / 2f, origin7, vector31, dir);
            Main.EntitySpriteDraw(value16, position4, null, color36 * Projectile.Opacity, 0f, origin7, vector32, dir);
            Main.EntitySpriteDraw(value16, position4, null, color37 * Projectile.Opacity, (float)Math.PI / 2f, origin7, vector31 * 0.6f, dir);
            Main.EntitySpriteDraw(value16, position4, null, color37 * Projectile.Opacity, 0f, origin7, vector32 * 0.6f, dir);
            return false;
        }
    }


}