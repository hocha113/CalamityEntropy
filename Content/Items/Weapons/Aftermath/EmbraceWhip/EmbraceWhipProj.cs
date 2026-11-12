using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.EmbraceWhip
{
    public class EmbraceWhipProj : BaseWhip
    {
        //鞭节数、长度倍率、每帧更新次数;鞭长约4×60×2/3×2.2≈352像素(22格),与万花筒同档
        private const int SegmentCount = 24;
        private const float LengthMult = 2.2f;
        private const int UpdatesPerFrame = 6;
        //贴图分段高度(像素):柄、珠节与细环、日轮鞭尖,与EmbraceWhipProj.png对应
        private const int HandlePx = 24;
        private const int SegPx = 12;
        private const int TipPx = 28;
        //鞭身向白色插值的自发光比例,鞭绳宽(像素)
        private const float SelfLight = 0.65f;
        private const float StringWidth = 2f;
        //日轮光晕:Glow2原图256像素,0.36倍约92像素
        private const float SunGlowScale = 0.36f;
        private const float SunGlowAlpha = 0.8f;
        //光晕随飞行进度亮起与熄灭的区间
        private const float GlowRiseFrom = 0.1f;
        private const float GlowRiseTo = 0.45f;
        private const float GlowFadeFrom = 0.75f;
        //鞭尖余烬:飞行进度区间、每帧1/N概率、寿命(帧)
        private const float EmberFrom = 0.2f;
        private const float EmberTo = 0.85f;
        private const int EmberChance = 2;
        private const int EmberLife = 24;
        //鞭响时鞭尖的闪光与光环
        private const float CrackShineScale = 0.55f;
        private const int CrackShineLife = 12;
        private const float CrackRingStart = 0.1f;
        private const float CrackRingEnd = 0.6f;
        private const int CrackRingLife = 14;

        private static readonly Color SunString = new(255, 170, 60);
        private static readonly Color SunGlow = new(255, 150, 40, 0);
        private static readonly Color EmberHot = new(255, 220, 110);
        private static readonly Color EmberDeep = new(255, 120, 30);
        private static readonly Vector3 TipLight = new(1.1f, 0.7f, 0.25f);

        public override Color StringColor => SunString;
        public override int handleHeight => HandlePx;
        public override int segHeight => SegPx;
        public override int endHeight => TipPx;

        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = UpdatesPerFrame;
            segments = SegmentCount;
            rangeMult = LengthMult;
        }

        //日轮亮度0到1;flyTime在首次PreAI前为0
        private float SwingGlow() {
            if (flyTime <= 0f)
                return 0f;
            return Utils.GetLerpValue(GlowRiseFrom, GlowRiseTo, FlyProgress, true) * Utils.GetLerpValue(1f, GlowFadeFrom, FlyProgress, true);
        }

        public override void WhipAI() {
            if (Main.dedServ)
                return;
            //鞭响那次更新BaseWhip刚填好碰撞点,末点即鞭尖
            if (Projectile.ai[0] == (int)(flyTime / 2f) && Projectile.WhipPointsForCollision.Count > 0) {
                Vector2 crack = Projectile.WhipPointsForCollision[^1];
                PRTLoader.NewParticle<PRT_ShineParticle>(crack, Vector2.Zero, EmberHot, CrackShineScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, CrackShineLife);
                PRTLoader.NewParticle<PRT_PulseRing>(crack, Vector2.Zero, EmberDeep, CrackRingStart).Configure(CrackRingEnd, CrackRingLife);
            }
            //一帧内多次更新,只在最后一次发光和落余烬
            if (Projectile.numUpdates != -1)
                return;
            List<Vector2> points = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, points);
            Vector2 tip = points[^1];
            Lighting.AddLight(tip, TipLight * SwingGlow());
            float progress = FlyProgress;
            if (progress > EmberFrom && progress < EmberTo && Main.rand.NextBool(EmberChance)) {
                Color color = Color.Lerp(EmberHot, EmberDeep, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_GlowLightParticle>(tip, Main.rand.NextVector2Circular(1.5f, 1.5f), color, Main.rand.NextFloat(0.45f, 0.7f)).Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, 0f, EmberLife);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            if (Projectile.owner == Main.myPlayer) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), HitPoint(target), Vector2.Zero, ModContent.ProjectileType<EmbraceWhipFlare>(), 0, 0f, Projectile.owner, Projectile.velocity.ToRotation());
            }
        }

        //离目标中心最近的鞭点夹进目标碰撞箱,当作命中点
        private Vector2 HitPoint(NPC target) {
            Vector2 best = target.Center;
            float bestDist = float.MaxValue;
            foreach (Vector2 point in Projectile.WhipPointsForCollision) {
                float dist = Vector2.DistanceSquared(point, target.Center);
                if (dist < bestDist) {
                    bestDist = dist;
                    best = point;
                }
            }
            return Vector2.Clamp(best, target.TopLeft, target.BottomRight);
        }

        public override Color modifySegColor(int segCount, Color color) => Color.Lerp(color, Color.White, SelfLight);

        public override void DrawStrings(List<Vector2> points) {
            for (int i = 1; i < points.Count; i++) {
                Vector2 mid = Vector2.Lerp(points[i - 1], points[i], 0.5f);
                Color lit = Lighting.GetColor((int)(mid.X / 16f), (int)(mid.Y / 16f)).MultiplyRGBA(SunString);
                CEUtils.drawLine(points[i - 1], points[i], Color.Lerp(lit, SunString, SelfLight), StringWidth * Projectile.scale);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            List<Vector2> points = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, points);
            float glow = SwingGlow();
            if (glow > 0f) {
                Texture2D tex = CEUtils.getExtraTex("Glow2");
                //A为0的颜色在常规混合下等同叠加发光,不必重开画布
                Main.EntitySpriteDraw(tex, points[^1] - Main.screenPosition, null, SunGlow * (SunGlowAlpha * glow), 0f, tex.Size() / 2f, SunGlowScale * (0.7f + 0.3f * glow), SpriteEffects.None);
            }
            DrawStrings(points);
            DrawSegs(points);
            return false;
        }
    }
}
