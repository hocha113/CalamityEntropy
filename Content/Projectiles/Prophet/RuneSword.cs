using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Prophet
{
    public class RuneSword : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.width = Projectile.height = 38;
            Projectile.light = 1;
            Projectile.timeLeft = 360;
        }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 34;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public float rotSpeed = Main.rand.NextFloat(-0.4f, 0.4f);
        public override void AI() {
            if (Projectile.ai[0] < 80) {
                Projectile.velocity *= 0.97f;
                if (Projectile.ai[0] < 40) {
                    Projectile.rotation += rotSpeed;
                    rotSpeed *= 0.96f;
                }
                else {
                    int p = Player.FindClosest(Projectile.Center, 5000, 5000);
                    if (p >= 0) {
                        Player player = p.ToPlayer();
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (player.Center + player.velocity * 4 - Projectile.Center).ToRotation(), 0.1f, false);
                    }
                }
            }
            if (Projectile.ai[0] == 80) {
                int p = Player.FindClosest(Projectile.Center, 5000, 5000);
                if (p >= 0) {
                    Player player = p.ToPlayer();
                    Projectile.rotation = (player.Center + player.velocity * 12 - Projectile.Center).ToRotation();
                }
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 24;
            }
            Projectile.ai[0]++;
        }
        internal Color ColorFunction(float completionRatio, Vector2 vertex) {
            float fadeToEnd = MathHelper.Lerp(0.65f, 1f, (float)Math.Cos(-Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
            float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio, true) * 1;
            Color colorHue = Color.SkyBlue;

            Color endColor = Color.Lerp(colorHue, Color.Turquoise, (float)Math.Sin(completionRatio * MathHelper.Pi * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 8 * 60);
        }
        internal float WidthFunction(float completionRatio, Vector2 vertex) {
            float expansionCompletion = (float)Math.Pow(1 - completionRatio, 3);
            return MathHelper.Lerp(0f, 12 * Projectile.scale * 1, expansionCompletion);
        }
        public override bool PreDraw(ref Color lightColor) {
            // texture-map未落盘,暂用自有渐隐条带近似原ScarletDevilStreak(TrailStreak只采样红通道)
            GameShaders.Misc["CalamityEntropy:TrailStreak"].SetShaderTexture(CEExtraAssets.StreakFadedAsset);
            CEPrimitiveRenderer.RenderTrail(Projectile.oldPos, new CEPrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Projectile.Size * 0.5f, shader: GameShaders.Misc["CalamityEntropy:TrailStreak"]), 30);

            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            Texture2D t = Projectile.GetTexture();
            Main.EntitySpriteDraw(t, Projectile.Center - Main.screenPosition, null, Color.White * 1, Projectile.rotation, t.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }

}