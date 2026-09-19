using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Integrations.BossLog;
using InnoVault.Rigs2D.Runtime;
using InnoVault.Rigs2D.Solvers;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    /// <summary>
    /// 无魂者图鉴沙盒:灰烬荒原上空的游弋。与战斗端同一副骨架(Apsychos.rig.json):本体为根,12 节尾骨 + 尾尖走 ChainFollow 跟随链。
    /// 一轮 9 秒:巡游 → 冲刺预告描边(四向加色轮廓)→ 沿朝向猛冲 → 尾尖火光炸亮 + 余烬四散 → 回到路径续巡;
    /// 每两轮以白化闪切一次阶段(一阶段焰红 ↔ 二阶段幽蓝),描边、尾光、余烬与天幕都跟阶段换色。
    /// 描边 / 白化用战斗端同一 WhiteTrans 着色器
    /// </summary>
    internal sealed class ApsychosPortraitActor : CEBossPortraitActor
    {
        public static ApsychosPortraitActor Instance => instance ??= new ApsychosPortraitActor();
        private static ApsychosPortraitActor instance;

        private const float RigScale = 0.5f;
        private const int SegCount = 12;
        //一轮编排(秒)
        private const float CycleLength = 9f;
        private const float ChargeAt = 5.4f;
        private const float DashAt = 6.2f;
        private const float DashEnd = 6.7f;
        private const float FlareEnd = 8.2f;
        private const float FlashHalf = 0.32f;

        private Rig2DInstance rig;
        private readonly int[] segPiecesA = new int[SegCount];
        private readonly int[] segPiecesB = new int[SegCount];
        private int tipBone = -1;
        private int bodyA = -1, bodyB = -1, tailA = -1, tailB = -1;
        private ChainFollowSolver followSolver;
        private BezierChainSolver bezierSolver;

        private readonly CEPortraitMotes motes = new();
        private Vector2 pos;
        private Vector2 prevPos;
        private float heading;
        private float pathTime;
        private float cycle;
        private int cycles;
        private bool phase2;
        /// <summary>阶段色混合量 0(焰红)..1(幽蓝),切换时平滑过渡</summary>
        private float phaseBlend;
        private float outline;
        private float highLight;
        private float tailLight;
        /// <summary>阶段闪切进行中:白化升到峰值时切件</summary>
        private bool swapPending;
        private float swapTimer;
        private bool swapped;
        private float emberTimer;
        private float ashTimer;

        public override Vector2 SceneHalfSize => new(300f, 250f);

        public override CEBossLogTheme Theme => ApsychosLogTheme.Instance;

        private ApsychosPortraitActor() { }

        /// <summary>当前阶段的主色(描边 / 尾光 / 余烬)</summary>
        private Color PhaseColor => Color.Lerp(ApsychosLogTheme.EmberOrange, ApsychosLogTheme.SoulBlue, phaseBlend);
        private Color PhaseHot => Color.Lerp(ApsychosLogTheme.EmberHot, ApsychosLogTheme.SoulPale, phaseBlend);

        private void EnsureRig() {
            if (rig != null || CERigAssets.Apsychos == null || !CERigAssets.Apsychos.IsValid) {
                return;
            }
            rig = CERigAssets.Apsychos.CreateInstance(2605);
            //舞台实例:世界坐标是场景局部量,调试叠层会画到屏幕外的无意义位置
            rig.DebugVisible = false;
            rig.Scale = RigScale;
            for (int i = 0; i < SegCount; i++) {
                segPiecesA[i] = rig.Piece($"segA{i}");
                segPiecesB[i] = rig.Piece($"segB{i}");
            }
            tipBone = rig.Bone("tip");
            bodyA = rig.Piece("bodyA");
            bodyB = rig.Piece("bodyB");
            tailA = rig.Piece("tailA");
            tailB = rig.Piece("tailB");
            followSolver = rig.Solver<ChainFollowSolver>("follow");
            bezierSolver = rig.Solver<BezierChainSolver>("bezier");
            //沙盒只走跟随链(战斗端的贝塞尔尾由状态按需启用)
            if (followSolver != null) {
                followSolver.Enabled = true;
            }
            if (bezierSolver != null) {
                bezierSolver.Enabled = false;
            }
        }

        /// <summary>巡游路径:横 8 字,长尾跟在身后自然弯折</summary>
        private static Vector2 Path(float t) => new(MathF.Sin(t * 0.45f) * 150f, MathF.Sin(t * 0.9f) * 68f - 12f);

        protected override void Reset() {
            EnsureRig();
            motes.Clear();
            pathTime = 0f;
            pos = Path(0f);
            prevPos = pos;
            heading = (Path(0.02f) - pos).ToRotation();
            cycle = 0f;
            cycles = 0;
            phase2 = false;
            phaseBlend = 0f;
            outline = highLight = tailLight = 0f;
            swapPending = swapped = false;
            swapTimer = 0f;
            emberTimer = ashTimer = 0f;
            if (rig != null) {
                ApplyPhase();
                rig.SetRoot(pos, heading);
                rig.Snap();
            }
        }

        protected override void Update(float dt) {
            float frames = dt * 60f;
            prevPos = pos;

            cycle += dt;
            if (cycle >= CycleLength) {
                cycle -= CycleLength;
                cycles++;
                if (cycles % 2 == 0) {
                    swapPending = true;
                    swapTimer = 0f;
                    swapped = false;
                }
            }

            //编排:巡游(向路径收敛)→ 预告描边(悬停后坐)→ 猛冲 → 尾光炸亮(减速漂回路径)
            Vector2 fwd = heading.ToRotationVector2();
            if (cycle < ChargeAt) {
                pathTime += dt;
                Converge(frames, 0.06f);
                outline = MathF.Max(0f, outline - dt * 3f);
                tailLight = MathF.Max(0f, tailLight - dt * 2f);
            }
            else if (cycle < DashAt) {
                float k = (cycle - ChargeAt) / (DashAt - ChargeAt);
                outline = CEBossLogSkin.Ease(k);
                pos -= fwd * 0.35f * frames;
                pos.Y += MathF.Sin(Time * 9f) * 0.25f * frames;
            }
            else if (cycle < DashEnd) {
                float k = (cycle - DashAt) / (DashEnd - DashAt);
                if (cycle - dt < DashAt) {
                    DashBurst();
                }
                pos += fwd * MathHelper.Lerp(15f, 6f, k) * frames;
                outline = MathF.Max(0f, outline - dt * 6f);
            }
            else if (cycle < FlareEnd) {
                float k = (cycle - DashEnd) / (FlareEnd - DashEnd);
                tailLight = MathF.Sin(k * MathHelper.Pi);
                pathTime += dt * 0.5f;
                Converge(frames, 0.035f);
            }
            else {
                pathTime += dt;
                Converge(frames, 0.05f);
                tailLight = MathF.Max(0f, tailLight - dt * 2f);
            }
            //冲刺后本体可能冲出场景边缘,把它夹回可见范围(冲刺方向由路径决定,幅度已限)
            pos.X = MathHelper.Clamp(pos.X, -SceneHalfSize.X + 70f, SceneHalfSize.X - 70f);
            pos.Y = MathHelper.Clamp(pos.Y, -SceneHalfSize.Y + 70f, SceneHalfSize.Y - 40f);

            Vector2 vel = pos - prevPos;
            bool holdHeading = cycle >= ChargeAt && cycle < DashEnd;
            if (!holdHeading && vel.LengthSquared() > 0.0004f) {
                heading = heading.AngleLerp(vel.ToRotation(), MathHelper.Clamp(0.1f * frames, 0f, 1f));
            }

            //阶段闪切:白化升到峰值时切件,再退回
            if (swapPending) {
                swapTimer += dt;
                if (swapTimer < FlashHalf) {
                    highLight = CEBossLogSkin.Ease(swapTimer / FlashHalf);
                }
                else if (swapTimer < FlashHalf * 2f) {
                    if (!swapped) {
                        swapped = true;
                        phase2 = !phase2;
                        ApplyPhase();
                        PhaseBurst();
                    }
                    highLight = 1f - CEBossLogSkin.Ease((swapTimer - FlashHalf) / FlashHalf);
                }
                else {
                    highLight = 0f;
                    swapPending = false;
                }
            }
            phaseBlend = MathHelper.Lerp(phaseBlend, phase2 ? 1f : 0f, MathHelper.Clamp(0.08f * frames, 0f, 1f));

            if (rig != null) {
                rig.SetRoot(pos, heading);
                rig.Step(frames);
            }

            //余烬:沿尾骨零星升起,阶段色
            emberTimer += dt;
            if (emberTimer > 0.07f && rig != null) {
                emberTimer = 0f;
                int seg = Main.rand.Next(SegCount);
                int bone = rig.Bone($"seg{seg}");
                if (bone >= 0) {
                    Vector2 c = rig.Bones[bone].Pos;
                    motes.Spawn(c + Main.rand.NextVector2Circular(8f, 8f), new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.4f, 1f)),
                        new Vector2(1.8f, 1.8f), Color.Lerp(PhaseColor, PhaseHot, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.6f, 1.3f), drag: 0.975f, additive: true);
                }
            }
            //尾光余烬:尾尖火光亮着时向后喷
            if (tailLight > 0.3f && rig != null && tipBone >= 0 && Main.rand.NextBool(2)) {
                Vector2 flare = TailFlarePos();
                Vector2 back = -rig.Bones[tipBone].Dir.ToRotationVector2();
                motes.Spawn(flare, back.RotatedBy(Main.rand.NextFloat(-0.8f, 0.8f)) * Main.rand.NextFloat(1.5f, 4f),
                    new Vector2(2.4f, 1.4f), PhaseHot, Main.rand.NextFloat(0.3f, 0.7f), drag: 0.95f, additive: true);
            }
            //荒原灰:自下缓升的暗灰小粒
            ashTimer += dt;
            if (ashTimer > 0.18f) {
                ashTimer = 0f;
                Vector2 half = SceneHalfSize;
                motes.Spawn(new Vector2(Main.rand.NextFloat(-half.X, half.X), half.Y + 6f),
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.3f, 0.8f)),
                    new Vector2(Main.rand.NextFloat(1.5f, 3f), Main.rand.NextFloat(1.5f, 3f)),
                    Color.Lerp(PhaseColor, ApsychosLogTheme.Ash, 0.6f) * 0.6f, Main.rand.NextFloat(3f, 5.5f), drag: 1f, additive: true);
            }
            motes.Update(frames);
        }

        /// <summary>向路径点收敛(冲刺离开路径后平滑接回,不硬跳)</summary>
        private void Converge(float frames, float rate) {
            Vector2 want = Path(pathTime);
            pos += (want - pos) * MathHelper.Clamp(rate * frames, 0f, 1f);
        }

        /// <summary>尾尖火光位置:尾尖骨沿「背离本体」方向推 110(战斗端 tail.rotation 指向后方)</summary>
        private Vector2 TailFlarePos() {
            ref Bone2D tip = ref rig.Bones[tipBone];
            return tip.Pos - tip.Dir.ToRotationVector2() * 110f * RigScale;
        }

        /// <summary>按阶段切两套贴图件可见性(与战斗端 SyncTailRigPhase 同式);跟随链模式下全部件无额外半圈</summary>
        private void ApplyPhase() {
            if (rig == null) {
                return;
            }
            for (int i = 0; i < SegCount; i++) {
                rig.Pieces[segPiecesA[i]].Visible = !phase2;
                rig.Pieces[segPiecesB[i]].Visible = phase2;
                rig.Pieces[segPiecesA[i]].ExtraRotation = 0f;
                rig.Pieces[segPiecesB[i]].ExtraRotation = 0f;
            }
            rig.Pieces[bodyA].Visible = !phase2;
            rig.Pieces[tailA].Visible = !phase2;
            rig.Pieces[bodyB].Visible = phase2;
            rig.Pieces[tailB].Visible = phase2;
            rig.Pieces[tailA].ExtraRotation = 0f;
            rig.Pieces[tailB].ExtraRotation = 0f;
        }

        /// <summary>起冲瞬间:身后一团余烬爆散</summary>
        private void DashBurst() {
            Vector2 back = -heading.ToRotationVector2();
            for (int i = 0; i < 18; i++) {
                Vector2 vel = back.RotatedBy(Main.rand.NextFloat(-0.9f, 0.9f)) * Main.rand.NextFloat(2f, 6f);
                motes.Spawn(pos + Main.rand.NextVector2Circular(16f, 16f), vel,
                    new Vector2(Main.rand.NextFloat(2f, 4f), Main.rand.NextFloat(1.4f, 2.2f)),
                    Color.Lerp(PhaseColor, PhaseHot, Main.rand.NextFloat()), Main.rand.NextFloat(0.4f, 0.9f),
                    drag: 0.94f, rot: vel.ToRotation(), additive: true);
            }
        }

        /// <summary>阶段切换:沿整条链每节向外洒一圈新阶段色的火星</summary>
        private void PhaseBurst() {
            if (rig == null) {
                return;
            }
            Color c = phase2 ? ApsychosLogTheme.SoulPale : ApsychosLogTheme.EmberHot;
            for (int i = 0; i < SegCount; i++) {
                int bone = rig.Bone($"seg{i}");
                if (bone < 0) {
                    continue;
                }
                Vector2 p = rig.Bones[bone].Pos;
                for (int k = 0; k < 3; k++) {
                    Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f);
                    motes.Spawn(p, vel, new Vector2(2.2f, 2.2f), c, Main.rand.NextFloat(0.4f, 0.8f), drag: 0.94f, additive: true);
                }
            }
            for (int k = 0; k < 14; k++) {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5f);
                motes.Spawn(pos, vel, new Vector2(3f, 3f), c, Main.rand.NextFloat(0.5f, 0.9f), drag: 0.93f, additive: true);
            }
        }

        //==================== 绘制 ====================

        public override void Draw(SpriteBatch sb, in CEPortraitFrame frame) {
            DrawSky(sb, in frame);
            if (rig != null && rig.Built) {
                Color ambient = frame.Masked ? Color.Black : Color.White;
                Rig2DDrawContext ctx = Rig2DDrawContext.Stage(frame.WorldMatrix, ambient, frame.Scissor);
                Effect white = CEEffectAssets.WhiteTrans;

                //冲刺预告描边:白化到底的整副骨架在四个旋转偏移上加色叠画(战斗端 DrawOutLine 同式)
                if (!frame.Masked && outline > 0.01f && white != null) {
                    sb.End();
                    sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                        DepthStencilState.None, frame.Scissor, white, frame.WorldMatrix);
                    white.Parameters["strength"]?.SetValue(1f);
                    white.CurrentTechnique.Passes[0].Apply();
                    for (int ir = 0; ir < 4; ir++) {
                        float r = ir * MathHelper.PiOver2 + Time * 10f;
                        Vector2 ofs = r.ToRotationVector2() * 8f * RigScale;
                        Rig2DDrawContext o = ctx.Flat(PhaseColor, outline);
                        //舞台坐标系里视口偏移为零,整副骨架平移 ofs = 反向挪视口
                        o.ViewOffset = -ofs;
                        Rig2DRenderer.Draw(sb, rig, in o);
                    }
                    sb.End();
                    CEBossPortraitStage.BeginAlpha(sb, in frame);
                }

                //本体 + 尾链:阶段闪切期间套 WhiteTrans 白化(骨架里没有带状件,批次不会被中途重开)
                bool useShader = !frame.Masked && highLight > 0.01f && white != null;
                if (useShader) {
                    sb.End();
                    CEBossPortraitStage.BeginShader(sb, in frame, white);
                    white.Parameters["strength"]?.SetValue(highLight);
                    white.CurrentTechnique.Passes[0].Apply();
                }
                Rig2DRenderer.Draw(sb, rig, in ctx);
                if (useShader) {
                    sb.End();
                    CEBossPortraitStage.BeginAlpha(sb, in frame);
                }

                if (!frame.Masked) {
                    DrawTailLight(sb);
                    //身周阶段色微光
                    CEPortraitDraw.Glow(sb, pos, 220f * RigScale, PhaseColor * (0.18f + 0.25f * outline));
                }
            }
            motes.Draw(sb, in frame);
        }

        /// <summary>尾尖火光:射线贴图十字加色 + 三层辉光(战斗端 TailLight 同式),强度随拍</summary>
        private void DrawTailLight(SpriteBatch sb) {
            if (tailLight <= 0.01f || tipBone < 0) {
                return;
            }
            Vector2 p = TailFlarePos();
            Texture2D ray = CEExtraAssets.Ray;
            Color band = PhaseHot with { A = 0 } * tailLight;
            if (ray != null) {
                Vector2 origin = ray.Size() * 0.5f;
                float s = RigScale * 1.6f;
                sb.Draw(ray, p, null, band, Time * 3f, origin, new Vector2(1f, 0.3f) * s, SpriteEffects.None, 0f);
                sb.Draw(ray, p, null, band, Time * 3f, origin, new Vector2(0.3f, 1f) * s, SpriteEffects.None, 0f);
            }
            CEPortraitDraw.Glow(sb, p, 70f * RigScale * 1.6f, Color.White * (0.6f * tailLight));
            CEPortraitDraw.Glow(sb, p, 120f * RigScale * 1.6f, PhaseColor * (0.7f * tailLight));
            CEPortraitDraw.Glow(sb, p, 170f * RigScale * 1.6f, PhaseColor * (0.4f * tailLight));
        }

        /// <summary>灰烬荒原:阶段渐变天幕 + 地平余火 + 热浪线 + 远处飘灰 + 焦土地带</summary>
        private void DrawSky(SpriteBatch sb, in CEPortraitFrame frame) {
            Vector2 half = frame.SceneHalf;
            Color top = Color.Lerp(ApsychosLogTheme.AshSkyTop, ApsychosLogTheme.SoulSkyTop, phaseBlend);
            Color bottom = Color.Lerp(ApsychosLogTheme.AshSkyBottom, ApsychosLogTheme.SoulSkyBottom, phaseBlend);
            float groundY = half.Y * 0.72f;
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, -half.Y, groundY, frame.Dim(top), frame.Dim(bottom), 22);
            //焦土地带:暗色不透明带,顶缘一线余火色
            Color soil = frame.Dim(Color.Lerp(new Color(30, 16, 12), new Color(10, 12, 28), phaseBlend));
            Color soilDeep = frame.Dim(new Color(10, 6, 5));
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, groundY, half.Y, soil, soilDeep, 10);
            CEPortraitDraw.Fill(sb, new Vector2(-half.X, groundY - 1.5f), new Vector2(half.X * 2f, 2f), frame.Dim(PhaseColor) * 0.6f);
            if (frame.Masked) {
                return;
            }

            //地平余火:地面线上一大团暗火背光,呼吸
            float breath = 0.85f + 0.15f * MathF.Sin(Time * 1.7f);
            CEPortraitDraw.Glow(sb, new Vector2(0f, groundY), new Vector2(half.X * 2.6f, half.Y * 1.1f), PhaseColor * (0.32f * breath));
            CEPortraitDraw.Glow(sb, new Vector2(-half.X * 0.55f, -half.Y * 0.45f), new Vector2(half.X * 1.2f, half.Y * 0.9f), Color.Lerp(new Color(120, 40, 20), new Color(40, 40, 120), phaseBlend) * 0.3f);

            //热浪线:自地面缓升、左右轻摆的横向细亮线,越高越淡
            for (int i = 0; i < 12; i++) {
                float ph = i * 61.3f;
                float rise = (Time * (18f + i % 4 * 6f) + ph) % (groundY + half.Y);
                float y = groundY - rise;
                float fade = 1f - rise / (groundY + half.Y);
                float x = MathF.Sin(Time * 0.9f + ph) * 40f + (CEPortraitDraw.Hash01(i, 2.6f) - 0.5f) * half.X * 1.6f;
                float len = 30f + i % 3 * 18f;
                CEPortraitDraw.Line(sb, new Vector2(x - len, y), new Vector2(x + len, y), 1.2f, PhaseHot with { A = 0 } * (0.16f * fade));
            }

            //远处飘灰:几团暗灰烟羽缓移
            for (int i = 0; i < 4; i++) {
                float ph = i * 1.7f;
                float x = (Time * (6f + i * 2f) + ph * 170f) % (half.X * 2f + 300f) - half.X - 150f;
                float y = groundY - 40f - 30f * MathF.Sin(ph) - i * 14f;
                CEPortraitDraw.Puff(sb, new Vector2(x, y), 260f + i * 30f, ApsychosLogTheme.Ash * 0.16f, ph + Time * 0.03f);
            }

            //余烬星点:慢闪
            for (int i = 0; i < 30; i++) {
                float hx = CEPortraitDraw.Hash01(i, 4.4f);
                float hy = CEPortraitDraw.Hash01(i, 9.1f);
                Vector2 p = new(-half.X + hx * half.X * 2f, -half.Y + hy * (groundY + half.Y) * 0.9f);
                float tw = 0.5f + 0.5f * MathF.Sin(Time * (1.2f + hx * 2f) + i * 2.7f);
                CEPortraitDraw.Fill(sb, p, new Vector2(1.4f, 1.4f), PhaseHot with { A = 0 } * (0.12f + 0.4f * tw));
            }
        }
    }
}
