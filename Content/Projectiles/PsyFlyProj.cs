using CalamityEntropy.Content.Items.Weapons;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PsyFlyProj : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.DamageType = NoneTypeDamageClass.Instance;
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 4;
        }
        public override void AI() {
            Player player = Projectile.owner.ToPlayer();
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 1600) {
                Projectile.Center = player.Center;
            }
            if (!player.dead) {
                Projectile.timeLeft = 4;
            }
            bool hasproj = false;
            Projectile target = null;
            float distance = 1000;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.hostile && !p.friendly && p.damage > 0 && Math.Max(p.width, p.height) < 150 && CEUtils.getDistance(Projectile.Center, p.Center) < distance && CEUtils.getDistance(player.Center, p.Center) < 256) {
                    if (p.Colliding(p.getRect(), Projectile.getRect())) {
                        p.Kill();
                    }
                    else {
                        target = p;
                        distance = CEUtils.getDistance(Projectile.Center, p.Center);
                        hasproj = true;
                    }
                }
            }
            if (hasproj) {
                for (int i = 0; i < 4; i++) {
                    Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 4f;
                    Projectile.velocity *= 0.94f;
                }
            }
            else {
                Vector2 targetPosp;
                int index = 0;
                int maxFlies = 0;
                bool flag = true;
                foreach (Projectile proj in Main.ActiveProjectiles) {
                    if (proj.type == Projectile.type) {
                        if (proj.whoAmI == Projectile.whoAmI) {
                            flag = false;
                        }
                        if (flag) {
                            index++;
                        }
                        maxFlies++;

                    }
                }
                targetPosp = player.Center + MathHelper.ToRadians(((float)(index + 1) / (float)(maxFlies)) * 360).ToRotationVector2().RotatedBy(player.Entropy().CasketSwordRot * 0.4f) * 90;
                Projectile.velocity += (targetPosp - Projectile.Center).SafeNormalize(Vector2.Zero) * 3.5f;
                Projectile.velocity *= 0.9f;
                if (CEUtils.getDistance(targetPosp, Projectile.Center) < Projectile.velocity.Length() + 32) {
                    Projectile.Center = targetPosp;
                    Projectile.velocity *= 0f;
                }

            }
            oldPos.Insert(0, Projectile.Center);
            if (oldPos.Count > 26) {
                oldPos.RemoveAt(oldPos.Count - 1);
            }
            if (!hasproj) {
                if (oldPos.Count > 0) {
                    oldPos.RemoveAt(oldPos.Count - 1);
                }
                if (oldPos.Count > 0) {
                    oldPos.RemoveAt(oldPos.Count - 1);
                }
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            Vector2 fpos = Projectile.Center;
            for (int i = 0; i < oldPos.Count; i++) {
                CEUtils.drawLine(fpos, oldPos[i], Color.Purple * (((float)oldPos.Count - (float)i) / (float)oldPos.Count), 6f * ((float)oldPos.Count - (float)i) / (float)oldPos.Count, 0, true);
                fpos = oldPos[i];
            }
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame1 = new Rectangle(((Main.GameUpdateCount / 4) % 2 == 0 ? 0 : 64), 0, 64, 64);
            Rectangle frame2 = new Rectangle(0, 64 + (((int)Main.GameUpdateCount / 4) % 6) * 64, 64, 64);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame2, Color.White, Projectile.rotation, new Vector2(32, 32), Projectile.scale, SpriteEffects.None, 0); ;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame1, Color.White, Projectile.rotation, new Vector2(32, 32), Projectile.scale, SpriteEffects.None, 0); ;

            return false;
        }
    }

}