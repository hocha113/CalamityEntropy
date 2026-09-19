using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using System;
using Terraria;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 轨道轰炸的虚空光柱:以 Center 为落点、竖直贯穿上下各半长的光柱。ai[0] 寿命,ai[1] 宽度,ai[2] = 本体 whoAmI + 1(0 为无),velocity.X 为横扫速度(P3)。
    /// 出现 6 帧后开判定、末 10 帧收拢;出现帧落点炸一圈火花 + 震屏;本体在深处时前 8 帧沿瞄准线从它的投影核心跑一记亮脉冲到落点(炮弹从背景飞来的那一下)
    /// </summary>
    public class VDVoidPillar : VDHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => VDDirector.OrbitalPillarLife;

        public int Life => (int)Math.Max(Projectile.ai[0], 12f);
        public float Width => Projectile.ai[1] > 0 ? Projectile.ai[1] : VDDirector.OrbitalPillarWidth;
        public int Age => Life - Projectile.timeLeft;
        public float HalfLength => VDDirector.OrbitalPillarLength * 0.5f;
        /// <summary>脉冲跑完瞄准线的帧数</summary>
        private const int LanceFrames = 8;

        private VoidDestroyerNPC OwnerBoss {
            get {
                int idx = (int)Projectile.ai[2] - 1;
                if (idx < 0 || idx >= Main.maxNPCs || !Main.npc[idx].active || Main.npc[idx].ModNPC is not VoidDestroyerNPC boss) {
                    return null;
                }
                return boss;
            }
        }

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        //只横扫,不做常规位移积分
        public override bool ShouldUpdatePosition() => false;

        /// <summary>宽度包络:前 4 帧张开,末 10 帧收拢</summary>
        public float Envelope() {
            float open = MathHelper.Clamp(Age / 4f, 0f, 1f);
            float close = MathHelper.Clamp(Projectile.timeLeft / 10f, 0f, 1f);
            return Math.Min(open, close);
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = Life;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("void_laser", 0.75f, Projectile.Center, 4, 1f);
                    CEUtils.PlaySound("VoidBomb", 0.9f, Projectile.Center, 4, 0.9f);
                    CEUtils.SetShake(Projectile.Center, 7f, 2200f);
                    VDVfx.Explosion(Projectile.Center, 0.9f, 22);
                    for (int i = 0; i < 30; i++) {
                        Vector2 v = new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-9f, -1f));
                        VDVfx.SparkBurst(Projectile.Center, VDVfx.VoidPurple, 1, v.Length(), v.Length(), 30, 0.7f, 1.3f);
                    }
                }
            }
            Projectile.position.X += Projectile.velocity.X;
            Lighting.AddLight(Projectile.Center, VDVfx.VoidPurple.ToVector3() * 1.2f * Envelope());
            if (!Main.dedServ && Main.rand.NextBool(2)) {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-Width * 0.4f, Width * 0.4f), Main.rand.NextFloat(-HalfLength * 0.6f, HalfLength * 0.6f));
                Vector2 v = new Vector2(Math.Sign(pos.X - Projectile.Center.X) * Main.rand.NextFloat(1f, 4f), -Main.rand.NextFloat(2f, 6f));
                VDVfx.SparkBurst(pos, VDVfx.VoidPink, 1, v.Length(), v.Length(), 16, 0.4f, 0.8f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Age < VDDirector.OrbitalPillarDamageDelay || Projectile.timeLeft < 8) {
                return false;
            }
            Vector2 top = Projectile.Center + new Vector2(0, -HalfLength);
            Vector2 bottom = Projectile.Center + new Vector2(0, HalfLength);
            return CEUtils.LineThroughRect(top, bottom, targetHitbox, (int)(Width * 0.8f));
        }

        public override bool PreDraw(ref Color lightColor) {
            float env = Envelope();
            Vector2 top = Projectile.Center + new Vector2(0, -HalfLength);
            VDBeamDraw.Draw(top, Vector2.UnitY, HalfLength * 2f, Width, VDVfx.VoidPurple, VDVfx.CannonCore, env, 1f, Projectile.whoAmI * 0.41f);
            //落点冲击环:等比扩散淡出(灰度环贴图不许压椭圆),与落点标记同一语言
            Main.spriteBatch.UseAdditive();
            var ring = CEUtils.getExtraTex("BloomRing");
            float ringP = MathHelper.Clamp(Age / 16f, 0f, 1f);
            float ringScale = Width * 1.2f / ring.Width * (0.5f + 0.9f * ringP);
            Main.spriteBatch.Draw(ring, Projectile.Center - Main.screenPosition, null, VDVfx.VoidPurple * ((1f - ringP) * 0.7f), 0f, ring.Size() / 2f, ringScale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            //瞄准线上的亮脉冲:从背景里的船跑到落点,一段越来越粗的白热短光
            VoidDestroyerNPC boss = OwnerBoss;
            if (boss != null && boss.Depth > 0.5f && Age < LanceFrames) {
                float t = Age / (float)LanceFrames;
                float tA = Math.Max(0f, t - 0.25f);
                Vector2 from = boss.ProjectedCorePos;
                Vector2 a = Vector2.Lerp(from, Projectile.Center, tA);
                Vector2 b = Vector2.Lerp(from, Projectile.Center, t);
                //子段两端的 Z 按屏幕分数反算(远端那半屏幕塞着更多世界长度),脉冲一路上噪声与雾色都接得上整根射线
                float zA = VDDepth.ZAtScreenFraction(boss.Depth, 0f, tA);
                float zB = VDDepth.ZAtScreenFraction(boss.Depth, 0f, t);
                VDBeamDraw.DrawTapered(a, b, 3f, Width * 0.45f * (0.4f + 0.6f * t), VDVfx.VoidPurple, VDVfx.CannonCore, 1f, 1f, Projectile.whoAmI * 0.53f, endGlow: false, zStart: zA, zEnd: zB);
            }
            return false;
        }
    }
}
