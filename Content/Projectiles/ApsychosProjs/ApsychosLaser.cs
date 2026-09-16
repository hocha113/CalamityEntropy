using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.ApsychosProjs
{
    public class ApsychosLaser : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(BuffID.OnFire3, 180);
        }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 8000;
        }
        public int counter = 0;
        public int length = 3600;
        public float width = 0;
        public int aicounter = 0;
        public override void SetDefaults() {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 330;
        }
        public bool st = true;
        public override void AI() {
            NPC n = ((int)Projectile.ai[0]).ToNPC();
            if (!n.active) {
                Projectile.Kill();
                return;
            }
            Projectile.rotation = n.rotation;
            Projectile.Center = n.Center + n.rotation.ToRotationVector2() * 100 * n.scale;
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * 16;
            if (st) {
                //PRT_ShineParticle FollowOwner字段spawn后赋,Configure只管Additive和lifetime
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 255), 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 15);  //ShineParticle FollowOwner字段spawn后赋,Configure只管Additive和lifetime
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(160, 160, 255), 0.29f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 15);
                CEUtils.PlaySound("CrystalBallActive", 0.6f + Main.rand.NextFloat(-0.2f, 0.2f), Projectile.Center, 10, 0.4f);
                st = false;
            }
            if (Projectile.timeLeft < 16) {
                width -= 1f / 16f;
            }
            else {
                if (aicounter < 60) {
                    if (width < 0.1f)
                        width += 1f / 100f;
                }
                else {
                    if (width < 1)
                        width += 1f / 20f;
                }
            }
            float maxlength = 3800;
            for (float i = 550; i < maxlength; i += 8) {
                Vector2 v = Projectile.Center + Projectile.rotation.ToRotationVector2() * i;
                length = (int)i;
                if (!CEUtils.inWorld(v) || Main.tile[(int)(v.X / 16), (int)(v.Y / 16)].IsTileSolid()) {
                    break;
                }
            }

            aicounter++;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * length, targetHitbox, 50);
        }
        public override bool? CanDamage() {
            return width >= 0.8f;
        }
        public override bool PreDraw(ref Color lightColor) {
            counter++;
            Texture2D tex = CEExtraAssets.DeathRay2;
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearWrap);

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 18, 0, length, tex.Height), new Color(120, 120, 255), Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.8f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 26, 0, length, tex.Height), new Color(140, 140, 255), Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.6f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 40, 0, length, tex.Height), Color.White, Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.5f), SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);

            Texture2D star = CEExtraAssets.StarTexture;
            float num = 0.5f * (float)Math.Sin(Main.GameUpdateCount / 10f);
            float num2 = 0.5f * (float)Math.Sin(Main.GameUpdateCount / 10f + MathHelper.PiOver4);
            Vector2 pos = Projectile.Center;
            Color color = new Color(140, 140, 255);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, 0, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.5f * 3 * width, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, 0, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.5f * 3 * width, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.2f * 3 * width, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.2f * 3 * width, SpriteEffects.None, 0);

            float r = Main.GlobalTimeWrappedHourly * 4;
            Main.spriteBatch.Draw(star, pos + Projectile.rotation.ToRotationVector2() * length - Main.screenPosition, null, color * Projectile.Opacity, r, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.5f * 3 * width, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos + Projectile.rotation.ToRotationVector2() * length - Main.screenPosition, null, color * Projectile.Opacity, r, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.5f * 3 * width, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos + Projectile.rotation.ToRotationVector2() * length - Main.screenPosition, null, color * Projectile.Opacity, r + MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.2f * 3 * width, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos + Projectile.rotation.ToRotationVector2() * length - Main.screenPosition, null, color * Projectile.Opacity, r + MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.2f * 3 * width, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
    }
}