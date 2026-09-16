using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class JailerWhipProjectile : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.IsAWhip[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.DefaultToWhip();

            Projectile.WhipSettings.Segments = 18;
            Projectile.WhipSettings.RangeMultiplier = 1.5f;
        }

        public override bool PreAI() {
            var owner = Projectile.owner.ToPlayer();
            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;

            float swingProgress = Timer / swingTime;
            if (true) {
                List<Vector2> points = Projectile.WhipPointsForCollision;
                points.Clear();
                Projectile.FillWhipControlPoints(Projectile, points);
                int pointIndex = Main.rand.Next(points.Count - 10, points.Count);
                Rectangle spawnArea = Utils.CenteredRectangle(points[pointIndex], new Vector2(30f, 30f));
                int dustType = DustID.Smoke;
                if (Main.rand.NextBool(5))
                    dustType = DustID.Blood;

                Dust dust; Vector2 spinningPoint;


                if (!Main.rand.NextBool(3) && Utils.GetLerpValue(0.1f, 0.7f, swingProgress, clamped: true) * Utils.GetLerpValue(0.9f, 0.7f, swingProgress, clamped: true) > 0.5f) {
                    dust = Dust.NewDustDirect(spawnArea.TopLeft(), spawnArea.Width, spawnArea.Height, dustType, 0f, 0f, 100, Color.White);
                    dust.position = points[pointIndex];
                    dust.fadeIn = 0.3f;
                    spinningPoint = points[pointIndex] - points[pointIndex - 1];
                    dust.noGravity = true;
                    dust.velocity *= 0.5f;
                    dust.velocity += spinningPoint.RotatedBy(owner.direction * ((float)Math.PI / 2f));
                    dust.velocity *= 0.5f;
                }

                spawnArea = Utils.CenteredRectangle(points[points.Count - 1], new Vector2(10f, 10));
                dustType = DustID.Blood;
                dust = Dust.NewDustDirect(spawnArea.TopLeft(), spawnArea.Width, spawnArea.Height, dustType, 0f, 0f, 100, Color.White);
                dust.position = points[pointIndex];
                dust.fadeIn = 0.1f;
                spinningPoint = points[pointIndex] - points[pointIndex - 1];
                dust.noGravity = true;
                dust.velocity *= 0.5f;
                dust.velocity += spinningPoint.RotatedBy(owner.direction * ((float)Math.PI / 2f));
                dust.velocity *= 0.5f;
            }
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.RemoveAllTags(target);
            target.AddBuff(ModContent.BuffType<JailerWhipDebuff>(), 240);
            target.AddBuff(ModContent.BuffType<FlamingBlood>(), 16 * 60);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
            Projectile.damage = (int)(Projectile.damage * 0.85f);
            target.AddBuff(ModContent.BuffType<BurningBlood>(), 240);
        }

        private void DrawLine(List<Vector2> list) {
            Texture2D texture = CEExtraAssets.white;
            Rectangle frame = texture.Frame();
            Vector2 origin = new Vector2(0, 0.5f);

            Vector2 pos = list[0];
            for (int i = 0; i < list.Count - 1; i++) {
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;

                float rotation = diff.ToRotation();
                Color color = Lighting.GetColor(element.ToTileCoordinates()).MultiplyRGBA(new Color(64, 54, 54));
                Vector2 scale = new Vector2(diff.Length() + 2, 2);
                if (i == list.Count - 2) {
                    scale.X -= 8;
                }

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

                pos += diff;
            }
        }
        private float Timer {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public override bool PreDraw(ref Color lightColor) {
            List<Vector2> list = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, list);

            DrawLine(list);


            SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Vector2 pos = list[0];

            for (int i = 0; i < list.Count - 1; i++) {
                Rectangle frame = new Rectangle(0, 0, 18, 32); Vector2 origin = new Vector2(9, 2); float scale = 1;

                if (i == list.Count - 2) {
                    frame.Y = 74; frame.Height = 18;
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                    origin = new Vector2(9, 0);

                }
                else if (i > 0) {
                    if (i % 2 == 0) {
                        frame.Y = 32;
                        frame.Height = 18;
                        origin = new Vector2(9, 0);
                    }
                    else {
                        frame.Y = 50;
                        frame.Height = 18;
                        origin = new Vector2(9, 0);
                    }

                }
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;

                float rotation = diff.ToRotation() - MathHelper.PiOver2; Color color = Lighting.GetColor(element.ToTileCoordinates());

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, flip, 0);

                pos += diff;
            }
            return false;
        }
    }
}
