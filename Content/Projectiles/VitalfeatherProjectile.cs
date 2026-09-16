using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VitalfeatherProjectile : BaseWhip
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = 10;
            this.segments = 36;
            this.rangeMult = 1.8f;
        }
        public Vector2 lastTop = Vector2.Zero;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (new Rectangle(((int)lastTop.X - 36), ((int)lastTop.Y - 36), 72, 72).Intersects(target.Hitbox)) {
                modifiers.SourceDamage *= 1.25f;
            }
        }

        public override bool PreAI() {
            var owner = Projectile.owner.ToPlayer();
            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;
            List<Vector2> points_ = Projectile.WhipPointsForCollision;
            points_.Clear();
            Projectile.FillWhipControlPoints(Projectile, points_);
            List<Vector2> points = points_;

            float swingProgress = Timer / swingTime;
            if (swingProgress > 0.04f) {
                Lighting.AddLight(lastTop, 1, 0.8f, 0.8f);
                int pointIndex = Main.rand.Next(points.Count - 10, points.Count);
                Rectangle spawnArea = Utils.CenteredRectangle(points[pointIndex], new Vector2(30f, 30f));
                int dustType = DustID.Smoke;
                if (Main.rand.NextBool(2))
                    dustType = DustID.FlameBurst;

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
                dustType = DustID.FlameBurst;
                dust = Dust.NewDustDirect(spawnArea.TopLeft(), spawnArea.Width, spawnArea.Height, dustType, 0f, 0f, 100, Color.White);
                dust.position = points[pointIndex];
                dust.fadeIn = 0.1f;
                spinningPoint = points[pointIndex] - points[pointIndex - 1];
                dust.noGravity = true;
                dust.velocity *= 0.5f;
                dust.velocity += spinningPoint.RotatedBy(owner.direction * ((float)Math.PI / 2f));
                dust.velocity *= 0.5f;
            }
            lastTop = points[points.Count - 1];
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.RemoveAllTags(target);
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff(ModContent.BuffType<DragonWhipDebuff>(), 240);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
            Projectile.damage = (int)(Projectile.damage * 0.9f);
            // 2026-08-31 平衡案:造成破晓而非龙焰
            target.AddBuff(BuffID.Daybreak, 180);
            SoundEngine.PlaySound(in SoundID.Item14, target.Center);
            for (int i = 0; i < 40; i++) {
                int num = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width, target.height, DustID.InfernoFork, 0f, 0f, 200, default(Color), 2);
                Dust obj = Main.dust[num];
                obj.position = target.Center + Vector2.UnitY.RotatedByRandom(3.1415927410125732) * (float)Main.rand.NextDouble() * target.width / 2f;
                obj.noGravity = true;
                obj.velocity = CEUtils.randomVec(8);
                num = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width, target.height, DustID.InfernoFork, 0f, 0f, 100, default(Color), 2);
                obj.position = target.Center;
                obj.velocity.Y -= 6f;
                obj.velocity *= 2f;
                obj.noGravity = true;
                obj.fadeIn = 1f;
                obj.color = Color.Crimson * 0.5f;
            }
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
                Color color = Color.OrangeRed;
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
            List<Vector2> list_ = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, list_);
            List<Vector2> list = list_;
            DrawLine(list);


            SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Vector2 pos = list[0];

            for (int i = 0; i < list.Count - 1; i++) {
                Rectangle frame = new Rectangle(0, 0, 22, 30); Vector2 origin = new Vector2(11, 24); float scale = 1.5f;

                if (i == list.Count - 2) {
                    frame.Y = 54; frame.Height = 20;
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true)) * 1.5f;
                    origin = new Vector2(11, 0);

                }
                else if (i > 0) {
                    if (i % 2 == 0) {
                        frame.Y = 30;
                        frame.Height = 12;
                        origin = new Vector2(11, 0);
                    }
                    else {
                        frame.Y = 42;
                        frame.Height = 12;
                        origin = new Vector2(11, 0);
                    }
                }

                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;

                float rotation = diff.ToRotation() - MathHelper.PiOver2; Color color = Color.White;

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, flip, 0);

                pos += diff;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D light = CEExtraAssets.lightball;
            Main.spriteBatch.Draw(light, lastTop - Main.screenPosition, null, Color.Gold * 0.2f, 0, light.Size() / 2, Projectile.scale * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        // 2026-08-31 平衡案:改用常规鞭子动画,自定义控制点曲线退役(走 BaseWhip 默认实现)
    }

    /// <summary>
    /// 沐生标记引爆:仆从命中标记目标时由 WhipDebuffNPC 触发,0.75秒一次。
    /// 固定基伤600(召唤),命中附加破晓减益。
    /// </summary>
    public class VitalfeatherBurst : ModProjectile
    {
        public const int BaseDamage = 600;
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source) {
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.4f }, Projectile.Center);
            for (int i = 0; i < 24; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + CEUtils.randomPointInCircle(60), DustID.SolarFlare, CEUtils.randomVec(7));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.4f, 2.2f);
            }
            for (int i = 0; i < 8; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, CEUtils.randomVec(4));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.6f, 2.4f);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Daybreak, 180);
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }
}
