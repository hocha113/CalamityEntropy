using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Integrations.BossLog;
using InnoVault.Rigs2D.Runtime;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    /// <summary>
    /// 先知图鉴沙盒:晨曦天穹里的巡游。与战斗端同一副骨架(Prophet.rig.json):本体为根,四片翅骨按鳍相位张合,
    /// 尾巴是自带摆动的 Verlet 绳带,尾环钉在第 8 节。本体沿缓 8 字滑行、始终朝行进方向;
    /// 每 8 秒一拍「预言符阵」:身周两圈符文环由内向外扩散淡出,同时甩出一圈符文光点,本体减速悬停
    /// </summary>
    internal sealed class ProphetPortraitActor : CEBossPortraitActor
    {
        public static ProphetPortraitActor Instance => instance ??= new ProphetPortraitActor();
        private static ProphetPortraitActor instance;

        private const float RigScale = 0.72f;
        private const float RunePeriod = 8f;
        private const float RuneLength = 1.7f;
        /// <summary>鳍相位分界:前 40% 余弦缓动升到 1,后 60% 落回 0(与战斗端 FinSwing 同式)</summary>
        private const float FinRise = 0.4f;

        private Rig2DInstance rig;
        private readonly int[] wingBones = new int[4];

        private readonly CEPortraitMotes motes = new();
        private Vector2 pos;
        private Vector2 prevPos;
        private float heading;
        private float pathTime;
        private float finPhase;
        private float runeTimer;
        /// <summary>符阵拍进度 0..1,0 = 未触发</summary>
        private float rune;
        private bool runeBurst;
        private float sparkleTimer;
        private float glyphTimer;

        public override Vector2 SceneHalfSize => new(300f, 250f);

        public override CEBossLogTheme Theme => ProphetLogTheme.Instance;

        private ProphetPortraitActor() { }

        private void EnsureRig() {
            if (rig != null || CERigAssets.Prophet == null || !CERigAssets.Prophet.IsValid) {
                return;
            }
            rig = CERigAssets.Prophet.CreateInstance(2604);
            //舞台实例:世界坐标是场景局部量,调试叠层会画到屏幕外的无意义位置
            rig.DebugVisible = false;
            rig.Scale = RigScale;
            string[] names = ["wing2L", "wing2R", "wing1L", "wing1R"];
            for (int i = 0; i < names.Length; i++) {
                wingBones[i] = rig.Bone(names[i]);
            }
        }

        /// <summary>巡游路径:缓 8 字,偏上留出符阵扩散的余地</summary>
        private static Vector2 Path(float t) => new(MathF.Sin(t * 0.5f) * 178f, MathF.Sin(t * 1.0f) * 76f - 18f);

        protected override void Reset() {
            EnsureRig();
            motes.Clear();
            pathTime = 0f;
            pos = Path(0f);
            prevPos = pos;
            heading = (Path(0.02f) - pos).ToRotation();
            finPhase = 0f;
            runeTimer = 2.5f;
            rune = 0f;
            runeBurst = false;
            sparkleTimer = glyphTimer = 0f;
            if (rig != null) {
                rig.SetRoot(pos, heading);
                ApplyWings(0f);
                rig.Snap();
            }
        }

        protected override void Update(float dt) {
            float frames = dt * 60f;
            prevPos = pos;

            //符阵拍:起手后本体减速悬停,环阵扩散完毕再续航
            runeTimer += dt;
            float slow = 1f;
            if (runeTimer > RunePeriod) {
                float k = (runeTimer - RunePeriod) / RuneLength;
                if (k >= 1f) {
                    runeTimer = 0f;
                    rune = 0f;
                    runeBurst = false;
                }
                else {
                    rune = k;
                    slow = 0.3f;
                    if (!runeBurst && k > 0.12f) {
                        runeBurst = true;
                        RuneBurst();
                    }
                }
            }
            pathTime += dt * slow;
            pos = Path(pathTime);
            Vector2 vel = pos - prevPos;
            if (vel.LengthSquared() > 0.0004f) {
                heading = heading.AngleLerp(vel.ToRotation(), MathHelper.Clamp(0.12f * frames, 0f, 1f));
            }
            float speed = frames > 0.001f ? vel.Length() / frames : 0f;

            //鳍相位:基础拍 + 随速加快(战斗端同式,速度量级按沙盒缩放)
            finPhase += (0.012f + speed * 0.003f) * frames;
            while (finPhase > 1f) {
                finPhase -= 1f;
            }

            if (rig != null) {
                rig.SetRoot(pos, heading);
                ApplyWings(FinSwing());
                rig.Step(frames);
            }

            //翅尖星屑:随扇动在两翼外侧零星迸出
            sparkleTimer += dt;
            if (sparkleTimer > 0.16f) {
                sparkleTimer = 0f;
                Vector2 side = (heading + MathHelper.PiOver2).ToRotationVector2();
                float sign = Main.rand.NextBool() ? 1f : -1f;
                Vector2 tip = pos + side * sign * 78f * RigScale - heading.ToRotationVector2() * 20f * RigScale;
                motes.Spawn(tip + Main.rand.NextVector2Circular(8f, 8f), side * sign * Main.rand.NextFloat(0.3f, 0.9f) - vel * 0.2f,
                    new Vector2(2f, 2f), Color.Lerp(ProphetLogTheme.RuneBlue, ProphetLogTheme.HolyWhite, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 1.1f), drag: 0.96f, additive: true);
            }
            //天穹浮符:自下缓升的淡金小粒
            glyphTimer += dt;
            if (glyphTimer > 0.22f) {
                glyphTimer = 0f;
                Vector2 half = SceneHalfSize;
                motes.Spawn(new Vector2(Main.rand.NextFloat(-half.X, half.X), half.Y + 6f),
                    new Vector2(Main.rand.NextFloat(-0.15f, 0.15f), -Main.rand.NextFloat(0.35f, 0.8f)),
                    new Vector2(Main.rand.NextFloat(1.4f, 2.6f), Main.rand.NextFloat(1.4f, 2.6f)),
                    ProphetLogTheme.DawnGold * 0.75f, Main.rand.NextFloat(3.5f, 6f), drag: 1f, additive: true);
            }
            motes.Update(frames);
        }

        /// <summary>鳍摆角 rotj(0 ~ 1 rad):相位前 40% 余弦缓动升到 1,后 60% 落回 0</summary>
        private float FinSwing() {
            return finPhase <= FinRise
                ? Ease01(finPhase / FinRise)
                : 1f - Ease01((finPhase - FinRise) / (1f - FinRise));
        }

        private static float Ease01(float t) => 0.5f - 0.5f * MathF.Cos(MathHelper.Clamp(t, 0f, 1f) * MathHelper.Pi);

        /// <summary>四片翅骨的局部旋转:内翅 ∓rotj,外翅 ∓静息角 ± rotj(与 TheProphet.Rig 同式)</summary>
        private void ApplyWings(float rotj) {
            rig.SetBoneLocalRotation(wingBones[0], -rotj);
            rig.SetBoneLocalRotation(wingBones[1], rotj);
            rig.SetBoneLocalRotation(wingBones[2], -1f + rotj);
            rig.SetBoneLocalRotation(wingBones[3], 1f - rotj);
        }

        /// <summary>符阵起手:一圈符文光点从身周甩出</summary>
        private void RuneBurst() {
            const int Count = 26;
            for (int i = 0; i < Count; i++) {
                float ang = MathHelper.TwoPi * i / Count + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 dir = ang.ToRotationVector2();
                Color c = i % 3 == 0 ? ProphetLogTheme.HolyWhite : ProphetLogTheme.RuneBlue;
                motes.Spawn(pos + dir * 30f, dir * Main.rand.NextFloat(2.4f, 4.2f),
                    new Vector2(4f, 1.6f), c, Main.rand.NextFloat(0.7f, 1.2f), drag: 0.955f, rot: ang, additive: true);
            }
        }

        //==================== 绘制 ====================

        public override void Draw(SpriteBatch sb, in CEPortraitFrame frame) {
            DrawSky(sb, in frame);
            if (!frame.Masked) {
                DrawRunes(sb);
            }
            if (rig != null && rig.Built) {
                Color ambient = frame.Masked ? Color.Black : Color.White;
                Rig2DDrawContext ctx = Rig2DDrawContext.Stage(frame.WorldMatrix, ambient, frame.Scissor);
                //尾带(0)→ 尾环(1)→ 内翅(2、3)→ 外翅(4、5)→ 本体(6),与战斗端 DrawRig 同序;带状件自己切一轮批次并回到舞台批次
                Rig2DRenderer.DrawAll(sb, rig, in ctx);
            }
            if (!frame.Masked) {
                //身周圣光:随符阵拍增亮
                float pulse = 0.7f + 0.3f * MathF.Sin(Time * 2.4f);
                float lit = 0.22f + 0.4f * MathF.Sin(rune * MathHelper.Pi);
                CEPortraitDraw.Glow(sb, pos, 200f * RigScale * (1f + rune * 0.6f), ProphetLogTheme.RuneBlue * (lit * pulse));
            }
            motes.Draw(sb, in frame);
        }

        /// <summary>晨曦天穹:深靛 → 晨蓝渐变 + 晨日 + 斜射光柱 + 云霭 + 浮符星点</summary>
        private void DrawSky(SpriteBatch sb, in CEPortraitFrame frame) {
            Vector2 half = frame.SceneHalf;
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, -half.Y, half.Y,
                frame.Dim(ProphetLogTheme.SkyDeep), frame.Dim(ProphetLogTheme.SkyDawn), 24);
            if (frame.Masked) {
                return;
            }

            //晨日:右上一轮暖白盘,外圈大范围金晕
            Vector2 sun = new(half.X * 0.62f, -half.Y * 0.66f);
            CEPortraitDraw.Glow(sb, sun, new Vector2(half.X * 1.5f, half.Y * 1.3f), ProphetLogTheme.DawnGold * 0.42f);
            CEPortraitDraw.Glow(sb, sun, 150f, ProphetLogTheme.HolyWhite * 0.6f);

            //斜射光柱:自晨日向左下扇开,缓慢摆动、明暗交替
            for (int i = 0; i < 7; i++) {
                float ang = MathHelper.ToRadians(112f + i * 8.5f + MathF.Sin(Time * 0.35f + i) * 2.2f);
                Vector2 dir = ang.ToRotationVector2();
                float a = 0.05f + 0.045f * MathF.Sin(Time * 0.8f + i * 1.9f);
                float w = 10f + i % 3 * 6f;
                CEPortraitDraw.Line(sb, sun + dir * 60f, sun + dir * 900f, w, ProphetLogTheme.HolyWhite with { A = 0 } * a);
            }

            //云霭:低处几团白雾缓移
            for (int i = 0; i < 5; i++) {
                float ph = i * 1.9f;
                float x = (Time * (7f + i * 2.5f) + ph * 140f) % (half.X * 2f + 360f) - half.X - 180f;
                float y = half.Y * 0.35f + 40f * MathF.Sin(ph) + i * 22f;
                CEPortraitDraw.Puff(sb, new Vector2(x, y), 300f + i * 40f, ProphetLogTheme.HolyWhite * 0.13f, ph + Time * 0.02f);
            }

            //浮符星点:确定性散布,慢闪
            for (int i = 0; i < 34; i++) {
                float hx = CEPortraitDraw.Hash01(i, 3.9f);
                float hy = CEPortraitDraw.Hash01(i, 7.4f);
                Vector2 p = new(-half.X + hx * half.X * 2f, -half.Y + hy * half.Y * 1.6f);
                float tw = 0.5f + 0.5f * MathF.Sin(Time * (0.8f + hx * 1.4f) + i * 2.1f);
                if (i % 5 == 0) {
                    CEPortraitDraw.Star(sb, p, 2.5f + 2f * tw, ProphetLogTheme.HolyWhite with { A = 0 } * (0.25f + 0.45f * tw));
                }
                else {
                    CEPortraitDraw.Fill(sb, p, new Vector2(1.4f, 1.4f), ProphetLogTheme.HolyWhite with { A = 0 } * (0.2f + 0.4f * tw));
                }
            }
        }

        /// <summary>预言符阵:两圈由内向外扩散淡出的符文环(蓝宽环 + 白细环)+ 身周常驻慢转符轮</summary>
        private void DrawRunes(SpriteBatch sb) {
            //常驻符轮:两圈反向慢转的细环,带一点亮弧
            float breath = 0.5f + 0.5f * MathF.Sin(Time * 1.6f);
            CEPortraitDraw.Ellipse(sb, pos, new Vector2(74f, 74f) * RigScale, 0f,
                ProphetLogTheme.RuneBlue with { A = 0 } * (0.18f + 0.1f * breath), 1.4f, 48, Time * 1.3f, ProphetLogTheme.HolyWhite with { A = 0 } * 0.5f);
            CEPortraitDraw.Ellipse(sb, pos, new Vector2(96f, 96f) * RigScale, 0f,
                ProphetLogTheme.RuneBlue with { A = 0 } * (0.12f + 0.08f * breath), 1f, 48, -Time * 0.9f, ProphetLogTheme.RuneBlue with { A = 0 } * 0.5f);

            if (rune <= 0.001f) {
                return;
            }
            //扩散环:与战斗端出生光环同一走向(蓝 360、白 420),半径随拍展开、亮度正弦包络
            float a = MathF.Sin(rune * MathHelper.Pi);
            float r1 = (40f + 330f * rune) * RigScale;
            float r2 = (30f + 400f * rune) * RigScale;
            CEPortraitDraw.Ellipse(sb, pos, new Vector2(r1, r1), 0f, ProphetLogTheme.RuneBlue with { A = 0 } * (0.55f * a), 7f, 64, Time * 3f, ProphetLogTheme.HolyWhite with { A = 0 } * (0.6f * a));
            CEPortraitDraw.Ellipse(sb, pos, new Vector2(r2, r2), 0f, ProphetLogTheme.HolyWhite with { A = 0 } * (0.5f * a), 2.2f, 64, -Time * 4f, ProphetLogTheme.HolyWhite with { A = 0 } * (0.7f * a));
            //环上符文刻点:沿蓝环等距 12 点,随环转动
            for (int i = 0; i < 12; i++) {
                float ang = MathHelper.TwoPi * i / 12f + Time * 1.1f;
                Vector2 p = pos + ang.ToRotationVector2() * r1;
                CEPortraitDraw.Star(sb, p, 4f * a, ProphetLogTheme.HolyWhite with { A = 0 } * (0.8f * a), 1.4f);
            }
            CEPortraitDraw.Glow(sb, pos, r2 * 1.4f, ProphetLogTheme.RuneBlue * (0.22f * a));
        }
    }
}
