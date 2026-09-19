using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Integrations.BossLog;
using InnoVault.Rigs2D.Runtime;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.LuminarisMoth
{
    /// <summary>
    /// 月华之蛾图鉴沙盒:月夜里的扑翼巡游。与战斗端同一副骨架(Luminaris.rig.json):8 帧图集本体 + 两条 Verlet 垂尾,
    /// 本体沿 8 字飘飞、随横向速度侧倾,身后拖一条天蓝 / 白双层光尾,星芒常亮;
    /// 每 6.5 秒一拍「月华冲刺」:沿路径加速、沿尾迹回放残影、光尾放宽;每 14 秒切一次阶段附魔色(青 ↔ 紫),切换时星屑爆散。
    /// 本体件套战斗端同一 EnchantedPass 流光着色器,剪影模式跳过
    /// </summary>
    internal sealed class LuminarisPortraitActor : CEBossPortraitActor
    {
        public static LuminarisPortraitActor Instance => instance ??= new LuminarisPortraitActor();
        private static LuminarisPortraitActor instance;

        private const float RigScale = 0.78f;
        private const float PhasePeriod = 14f;
        private const float DashPeriod = 6.5f;
        private const float DashLength = 0.9f;
        private const int TrailLength = 26;
        /// <summary>本体件所在层序带(rig.json 里 layer 10)</summary>
        private const float BodyLayer = 10f;

        private Rig2DInstance rig;
        private int bodyPiece = -1;

        private readonly CEPortraitMotes motes = new();
        private readonly Vector2[] trail = new Vector2[TrailLength];
        private Vector2 pos;
        private Vector2 prevPos;
        private float tilt;
        private float pathTime;
        private float frameCounter;
        private bool phase2;
        /// <summary>阶段附魔色混合量 0(青)..1(紫),切换时平滑过渡</summary>
        private float phaseBlend;
        private float phaseTimer;
        private float dashTimer;
        /// <summary>冲刺拍强度 0..1</summary>
        private float dash;
        private float afterImage;
        private float dustTimer;
        private float mistTimer;

        public override Vector2 SceneHalfSize => new(300f, 250f);

        public override CEBossLogTheme Theme => LuminarisLogTheme.Instance;

        private LuminarisPortraitActor() { }

        /// <summary>当前阶段附魔色(与战斗端 DrawMyself 的两档同值)</summary>
        private Color PhaseColor => Color.Lerp(LuminarisLogTheme.PhaseCyan, LuminarisLogTheme.PhaseViolet, phaseBlend);

        private void EnsureRig() {
            if (rig != null || CERigAssets.Luminaris == null || !CERigAssets.Luminaris.IsValid) {
                return;
            }
            rig = CERigAssets.Luminaris.CreateInstance(2607);
            //舞台实例:世界坐标是场景局部量,调试叠层会画到屏幕外的无意义位置
            rig.DebugVisible = false;
            rig.Scale = RigScale;
            bodyPiece = rig.Piece("body");
        }

        /// <summary>飘飞路径:横 8 字,偏上,给垂尾留出下方空间</summary>
        private static Vector2 Path(float t) => new(MathF.Sin(t * 0.6f) * 182f, MathF.Sin(t * 1.2f + 0.8f) * 66f - 34f);

        protected override void Reset() {
            EnsureRig();
            motes.Clear();
            pathTime = 0f;
            pos = Path(0f);
            prevPos = pos;
            tilt = 0f;
            frameCounter = 0f;
            phase2 = false;
            phaseBlend = 0f;
            phaseTimer = 0f;
            dashTimer = 1.5f;
            dash = 0f;
            afterImage = 0f;
            dustTimer = mistTimer = 0f;
            for (int i = 0; i < trail.Length; i++) {
                trail[i] = pos;
            }
            if (rig != null) {
                rig.SetRoot(pos, 0f);
                rig.Snap();
            }
        }

        protected override void Update(float dt) {
            float frames = dt * 60f;
            prevPos = pos;

            //冲刺拍:沿路径加速,正弦包络
            dashTimer += dt;
            dash = 0f;
            if (dashTimer > DashPeriod) {
                float k = (dashTimer - DashPeriod) / DashLength;
                if (k >= 1f) {
                    dashTimer = 0f;
                }
                else {
                    dash = MathF.Sin(k * MathHelper.Pi);
                }
            }
            pathTime += dt * (1f + 2.4f * dash);
            pos = Path(pathTime);
            Vector2 vel = pos - prevPos;
            float speed = frames > 0.001f ? vel.Length() / frames : 0f;
            //侧倾:横向速度读作倾角(战斗端 AboveMovingTilt 同向,沙盒速度量级更小所以系数放大)
            float wantTilt = MathHelper.Clamp(vel.X / MathF.Max(frames, 0.001f) * 0.05f, -0.4f, 0.4f);
            tilt = MathHelper.Lerp(tilt, wantTilt, MathHelper.Clamp(0.12f * frames, 0f, 1f));
            afterImage = MathF.Max(dash, afterImage - dt * 2.5f);

            //尾迹采样:每步推一格
            for (int i = trail.Length - 1; i >= 1; i--) {
                trail[i] = trail[i - 1];
            }
            trail[0] = pos;

            //阶段:每 14 秒切色,切换瞬间星屑爆散,混合量平滑过渡
            phaseTimer += dt;
            if (phaseTimer >= PhasePeriod) {
                phaseTimer = 0f;
                phase2 = !phase2;
                PhaseBurst();
            }
            phaseBlend = MathHelper.Lerp(phaseBlend, phase2 ? 1f : 0f, MathHelper.Clamp(0.06f * frames, 0f, 1f));

            //动画帧:与战斗端同速(每 4 帧一格)
            frameCounter += frames;
            if (rig != null) {
                if (bodyPiece >= 0) {
                    rig.Pieces[bodyPiece].Frame = (int)(frameCounter / 4f) % 8;
                }
                rig.SetRoot(pos, tilt);
                rig.Step(frames);
            }

            //翅尘:两翼外侧零星迸出的附魔色亮粒,冲刺时更密
            dustTimer += dt;
            if (dustTimer > (dash > 0.2f ? 0.04f : 0.13f)) {
                dustTimer = 0f;
                float sign = Main.rand.NextBool() ? 1f : -1f;
                Vector2 wing = pos + new Vector2(sign * Main.rand.NextFloat(60f, 110f), Main.rand.NextFloat(-30f, 10f)).RotatedBy(tilt) * RigScale;
                motes.Spawn(wing, new Vector2(sign * Main.rand.NextFloat(0.2f, 0.7f), -Main.rand.NextFloat(0.2f, 0.6f)) - vel * 0.15f,
                    new Vector2(2f, 2f), Color.Lerp(PhaseColor, Color.White, Main.rand.NextFloat(0.5f)),
                    Main.rand.NextFloat(0.5f, 1.1f), drag: 0.965f, additive: true);
            }
            //月尘:自下缓升的柔亮小粒
            mistTimer += dt;
            if (mistTimer > 0.2f) {
                mistTimer = 0f;
                Vector2 half = SceneHalfSize;
                motes.Spawn(new Vector2(Main.rand.NextFloat(-half.X, half.X), half.Y + 6f),
                    new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -Main.rand.NextFloat(0.3f, 0.7f)),
                    new Vector2(Main.rand.NextFloat(1.2f, 2.4f), Main.rand.NextFloat(1.2f, 2.4f)),
                    LuminarisLogTheme.MoonWhite * 0.7f, Main.rand.NextFloat(3.5f, 6f), drag: 1f, additive: true);
            }
            motes.Update(frames);
        }

        /// <summary>阶段切换的星屑爆散:一圈附魔色亮粒自本体甩出</summary>
        private void PhaseBurst() {
            Color c = phase2 ? LuminarisLogTheme.PhaseViolet : LuminarisLogTheme.PhaseCyan;
            for (int i = 0; i < 28; i++) {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5.5f);
                motes.Spawn(pos + Main.rand.NextVector2Circular(14f, 14f), vel,
                    new Vector2(Main.rand.NextFloat(2f, 3.5f), Main.rand.NextFloat(1.4f, 2.2f)),
                    Color.Lerp(c, Color.White, Main.rand.NextFloat(0.6f)), Main.rand.NextFloat(0.5f, 1f),
                    drag: 0.94f, rot: vel.ToRotation(), additive: true);
            }
        }

        //==================== 绘制 ====================

        public override void Draw(SpriteBatch sb, in CEPortraitFrame frame) {
            DrawSky(sb, in frame);
            if (rig == null || !rig.Built) {
                motes.Draw(sb, in frame);
                return;
            }
            Color ambient = frame.Masked ? Color.Black : Color.White;
            Rig2DDrawContext ctx = Rig2DDrawContext.Stage(frame.WorldMatrix, ambient, frame.Scissor);

            if (!frame.Masked) {
                DrawTrail(sb);
                //残影:沿尾迹后半段回放本体件,越旧越淡(战斗端 AfterImage 同构)
                if (afterImage > 0.02f) {
                    for (int i = trail.Length - 1; i >= 3; i -= 2) {
                        float fade = 1f - i / (float)trail.Length;
                        Rig2DDrawContext ghost = ctx.Flat(Color.White, 0.22f * fade * afterImage).Layers(BodyLayer, BodyLayer);
                        //舞台坐标系里视口偏移为零,整副骨架平移 = 反向挪视口
                        ghost.ViewOffset = pos - trail[i];
                        Rig2DRenderer.Draw(sb, rig, in ghost);
                    }
                }
            }

            //尾带(层 0、1):带状件自己切一轮批次并回到舞台批次
            Rig2DRibbonRenderer.Draw(sb, rig, in ctx);

            //本体件(层 10):非剪影时套 EnchantedPass 流光(附魔纹理绑 s1),与战斗端 DrawMyself 同一着色器与参数
            Effect shader = CEEffectAssets.Transform3;
            Texture2D enchanted = CEExtraAssets.EnchantedAsset?.Value;
            bool useShader = !frame.Masked && shader != null && enchanted != null;
            Rig2DDrawContext bodyCtx = ctx.Layers(BodyLayer, BodyLayer);
            if (useShader) {
                shader.Parameters["uTime"]?.SetValue(Time * 0.2f);
                shader.Parameters["color"]?.SetValue(PhaseColor.ToVector4());
                shader.Parameters["strength"]?.SetValue(MathHelper.Lerp(0.2f, 1f, phaseBlend));
                sb.End();
                CEBossPortraitStage.BeginShader(sb, in frame, shader);
                sb.GraphicsDevice.Textures[1] = enchanted;
                shader.CurrentTechnique.Passes[0].Apply();
            }
            Rig2DRenderer.Draw(sb, rig, in bodyCtx);
            if (useShader) {
                sb.End();
                CEBossPortraitStage.BeginAlpha(sb, in frame);
            }

            if (!frame.Masked) {
                DrawStar(sb);
            }
            motes.Draw(sb, in frame);
        }

        /// <summary>光尾:沿尾迹采样点的两层加色折线(天蓝宽 + 白窄),越旧越细越淡;冲刺时整体放宽(战斗端 MegaTrail 的替身)</summary>
        private void DrawTrail(SpriteBatch sb) {
            float widen = 1f + afterImage * 1.6f;
            for (int i = 1; i < trail.Length; i++) {
                float k = 1f - i / (float)trail.Length;
                float a = k * k;
                CEPortraitDraw.Line(sb, trail[i - 1], trail[i], (18f * k + 2f) * widen, Color.SkyBlue with { A = 0 } * (0.45f * a));
                CEPortraitDraw.Line(sb, trail[i - 1], trail[i], (10f * k + 1f) * widen, Color.White with { A = 0 } * (0.5f * a));
            }
        }

        /// <summary>身心星芒:两张正交拉伸的白星贴图加色叠画,随时间轻微缩放,冲刺时放大(战斗端同式)</summary>
        private void DrawStar(SpriteBatch sb) {
            Texture2D star = CEExtraAssets.StarTexture_White;
            if (star == null) {
                return;
            }
            float starX = 1f + MathF.Cos(Time * 26f) * 0.4f;
            Vector2 starScale = new Vector2(starX, starX) * (1f + afterImage * 1.6f) * RigScale * 0.74f;
            Color c = Color.LightBlue with { A = 0 } * 0.9f;
            sb.Draw(star, pos, null, c, 0f, star.Size() * 0.5f, new Vector2(1f, 0.56f) * starScale, SpriteEffects.None, 0f);
            sb.Draw(star, pos, null, c, 0f, star.Size() * 0.5f, new Vector2(0.8f, 0.7f) * starScale, SpriteEffects.None, 0f);
            CEPortraitDraw.Glow(sb, pos, 160f * RigScale * (1f + afterImage * 0.5f), PhaseColor * 0.35f);
        }

        /// <summary>月夜:深靛渐变 + 满月(实心盘 + 大范围月晕)+ 星野 + 低处夜霭</summary>
        private void DrawSky(SpriteBatch sb, in CEPortraitFrame frame) {
            Vector2 half = frame.SceneHalf;
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, -half.Y, half.Y,
                frame.Dim(LuminarisLogTheme.NightTop), frame.Dim(LuminarisLogTheme.NightBottom), 24);

            //满月:右上一轮实心盘(横向扫描线堆出),剪影模式压暗保留
            Vector2 moon = new(half.X * 0.55f, -half.Y * 0.58f);
            const float MoonR = 46f;
            Color disc = frame.Dim(LuminarisLogTheme.MoonWhite);
            Color discEdge = frame.Dim(new Color(190, 205, 240));
            for (float y = -MoonR; y <= MoonR; y += 2f) {
                float k = y / MoonR;
                float hw = MoonR * MathF.Sqrt(MathF.Max(0f, 1f - k * k));
                if (hw < 0.5f) {
                    continue;
                }
                Color c = Color.Lerp(disc, discEdge, MathF.Abs(k) * 0.6f);
                CEPortraitDraw.Fill(sb, new Vector2(moon.X - hw, moon.Y + y), new Vector2(hw * 2f, 2.2f), c);
            }
            if (frame.Masked) {
                return;
            }
            //月晕:阶段色微染
            Color halo = Color.Lerp(new Color(150, 190, 255), PhaseColor, 0.35f);
            CEPortraitDraw.Glow(sb, moon, 520f, halo * 0.32f);
            CEPortraitDraw.Glow(sb, moon, 180f, LuminarisLogTheme.MoonWhite * 0.5f);

            //星野:确定性散布,慢闪;每 8 颗出一颗四芒星
            for (int i = 0; i < 64; i++) {
                float hx = CEPortraitDraw.Hash01(i, 5.1f);
                float hy = CEPortraitDraw.Hash01(i, 8.6f);
                Vector2 p = new(-half.X + hx * half.X * 2f, -half.Y + hy * half.Y * 1.7f);
                if (Vector2.Distance(p, moon) < MoonR + 6f) {
                    continue;
                }
                float tw = 0.5f + 0.5f * MathF.Sin(Time * (0.9f + hx * 1.6f) + i * 2.3f);
                float a = 0.22f + 0.5f * tw;
                if (i % 8 == 0) {
                    CEPortraitDraw.Star(sb, p, 2.5f + 1.5f * tw, LuminarisLogTheme.MoonWhite with { A = 0 } * (a * 0.8f));
                }
                else {
                    float s = 0.8f + hy;
                    CEPortraitDraw.Fill(sb, p, new Vector2(s, s), LuminarisLogTheme.MoonWhite with { A = 0 } * a);
                }
            }

            //夜霭:低处几团靛蓝薄雾缓移
            for (int i = 0; i < 4; i++) {
                float ph = i * 2.1f;
                float x = (Time * (6f + i * 2f) + ph * 150f) % (half.X * 2f + 320f) - half.X - 160f;
                float y = half.Y * 0.55f + 30f * MathF.Sin(ph) + i * 18f;
                CEPortraitDraw.Puff(sb, new Vector2(x, y), 280f + i * 40f, new Color(90, 120, 200) * 0.12f, ph + Time * 0.02f);
            }
        }
    }
}
