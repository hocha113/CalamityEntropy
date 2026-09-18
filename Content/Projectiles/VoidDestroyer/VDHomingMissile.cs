using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 深空追踪导弹:从导弹环径向飞出的同时射入深处(30 帧退到 Z 1.5,环在背景里收成一圈向消失点收拢的小点,平面速度衰减到明显减速),
    /// 到顶掉头重新锁定玩家,40 帧扑回平面(越来越大,各自带一枚落点小环),只在穿过平面那几帧有判定,之后掠过镜头消失。
    /// 未带深度生成时(Z 恒 0)退化成旧版平面追踪导弹。命中 312,无减益。ai[0] 为目标玩家索引
    /// </summary>
    public class VDHomingMissile : VDDepthProjectile
    {
        public const float RetargetDistance = 15f * 16f;
        public const float MaxSpeed = 22f;
        public static readonly Color GlowColor = new Color(210, 90, 255);

        public override int DefaultTimeLeft => 240;
        public override float MarkerRadius => VDDirector.MissileMarkerRadius;
        /// <summary>一环十来发同帧掠过镜头,只让四分之一发声</summary>
        protected override float WhooshStrength => Projectile.identity % 4 == 0 ? 0.3f : 0f;

        public int Age => (int)Projectile.localAI[1];
        /// <summary>去程(射入深处)中</summary>
        private bool Outbound => HasDepth && ZVel > 0f;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 18;
            Projectile.height = 18;
        }

        /// <summary>回程预测落点:按剩余帧数的一半预判目标位置(全程预判会被反向走位晃掉,不预判追不上)</summary>
        public override Vector2 PredictedLanding() {
            Player target = TargetPlayer(0);
            if (target == null || !Approaching) {
                return base.PredictedLanding();
            }
            float frames = FramesToPlane();
            return target.Center + target.velocity * (frames * 0.5f);
        }

        protected override void DepthAI() {
            Projectile.localAI[1]++;
            if (HasDepth) {
                DepthFlight();
            }
            else {
                PlaneFlight();
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (!HasDepth || Math.Abs(Z) < 0.5f) {
                Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.35f);
            }

            if (Main.dedServ || (HasDepth && Math.Abs(Z) > 0.4f)) {
                return;
            }
            //尾烟只在平面附近放(粒子系统不分层)
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Dust d = Dust.NewDustPerfect(Projectile.Center + back * 16f, DustID.Smoke, back * Main.rand.NextFloat(1f, 3f), 120, default, Main.rand.NextFloat(1f, 1.6f));
            d.noGravity = true;
            if (Main.rand.NextBool(2)) {
                Vector2 v = back * Main.rand.NextFloat(2f, 5f) + CEUtils.randomPointInCircle(1f);
                VDVfx.Spark(Projectile.Center + back * 14f, v, GlowColor, Main.rand.NextFloat(0.4f, 0.8f), 0.9f, 16);
            }
        }

        /// <summary>旧版平面行为:径向飞出约 15 格后重新瞄准并加速扑去</summary>
        private void PlaneFlight() {
            //localAI[0] 累计飞行距离,全端由速度积分得到
            Projectile.localAI[0] += Projectile.velocity.Length();
            if (Projectile.localAI[0] <= RetargetDistance) {
                return;
            }
            Player target = TargetPlayer(0);
            if (target != null) {
                float cur = Projectile.velocity.ToRotation();
                float want = (target.Center - Projectile.Center).ToRotation();
                float diff = MathHelper.WrapAngle(want - cur);
                float turn = MathHelper.Clamp(diff, -0.105f, 0.105f);
                float speed = Math.Min(Projectile.velocity.Length() * 1.03f + 0.1f, MaxSpeed);
                Projectile.velocity = (cur + turn).ToRotationVector2() * speed;
            }
            else {
                Projectile.velocity *= 1.01f;
            }
        }

        /// <summary>深空行为:去程沿环向减速、Z 上升;到顶掉头,回程按剩余帧数收敛到预测落点;穿过平面后直飞出视野</summary>
        private void DepthFlight() {
            if (Outbound) {
                //去程:平面速度衰减到下限,导弹在背景里明显慢下来
                float speed = Math.Max(Projectile.velocity.Length() * 0.96f, VDDirector.MissileOutMinSpeed);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
                if (Age >= VDDirector.MissileOutFrames) {
                    //掉头:Z 速度反向,回程帧数固定
                    ZVel = -Z / VDDirector.MissileReturnFrames;
                    ZAccel = 0f;
                    if (!Main.dedServ && Projectile.identity % 3 == 0) {
                        CEUtils.PlaySound("VoidAnticipation", 1.4f, ProjectedCenter, 4, 0.4f);
                    }
                }
                return;
            }
            if (Z > 0f) {
                //回程:朝预测落点收敛,转向与速度都有上限,玩家可以横向甩
                Player target = TargetPlayer(0);
                if (target != null) {
                    float frames = Math.Max(FramesToPlane(), 1f);
                    Vector2 desired = (PredictedLanding() - Projectile.Center) / frames;
                    float cur = Projectile.velocity.ToRotation();
                    float diff = MathHelper.WrapAngle(desired.ToRotation() - cur);
                    float turn = MathHelper.Clamp(diff, -VDDirector.MissileReturnTurn, VDDirector.MissileReturnTurn);
                    float speed = Math.Min(desired.Length(), VDDirector.MissileReturnMaxSpeed);
                    Projectile.velocity = (cur + turn).ToRotationVector2() * speed;
                }
                return;
            }
            //穿过平面后:继续沿原方向掠向镜头,出视野即消失
            if (OutOfSight) {
                Projectile.Kill();
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            base.OnHitPlayer(target, info);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ || (HasDepth && Math.Abs(Z) > 0.5f)) {
                return;
            }
            CEUtils.PlaySound("VoidBomb", Main.rand.NextFloat(1.1f, 1.3f), Projectile.Center, 6, 0.45f);
            PRTLoader.NewParticle<PRT_EXPLOSION>(Projectile.Center, Vector2.Zero, Color.White, 0.35f)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 24);
            for (int i = 0; i < 10; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                VDVfx.Spark(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.5f, 1f), 1f, 22, gravity: true);
            }
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = ProjectedCenter - Main.screenPosition;
            float scale = DrawScale;
            float depthAlpha = VDDepth.Alpha(Z);
            Color glow = VDDepth.Fog(GlowColor, Z);

            spriteBatch.UseAdditive();
            if (HasDepth) {
                DrawDepthTrail(tex, origin, GlowColor, 0.3f);
            }
            else {
                for (int i = 1; i < Projectile.oldPos.Length; i++) {
                    float a = (1f - i / (float)Projectile.oldPos.Length) * 0.3f;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    spriteBatch.Draw(tex, pos, null, GlowColor * a, Projectile.oldRot[i], origin, Projectile.scale, SpriteEffects.None, 0f);
                }
            }
            Texture2D glowTex = CEUtils.getExtraTex("Glow");
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            spriteBatch.Draw(glowTex, drawPos + back * 18f * scale, null, VDDepth.Fog(new Color(255, 140, 255), Z) * (0.8f * depthAlpha), 0f, glowTex.Size() / 2f, 0.14f * scale, SpriteEffects.None, 0f);
            //回程的导弹核心更亮:在背景里一颗颗亮起来就是「它们要回来了」
            if (HasDepth && Approaching) {
                spriteBatch.Draw(glowTex, drawPos, null, glow * (0.5f * depthAlpha), 0f, glowTex.Size() / 2f, 0.12f * Math.Max(scale, 0.5f), SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();

            if (HasDepth) {
                VDDepthDraw.Draw(tex, drawPos, null, Color.White, 1f, Projectile.rotation, origin, scale, SpriteEffects.None, Z);
            }
            else {
                spriteBatch.Draw(tex, drawPos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
        }
    }
}
