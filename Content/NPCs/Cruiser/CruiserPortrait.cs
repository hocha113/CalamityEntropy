using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Integrations.BossLog;
using InnoVault.Rigs2D.Data;
using InnoVault.Rigs2D.Runtime;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    /// <summary>
    /// 巡游者图鉴沙盒:虚空雷暴里的巡游循环。短链(9 节)走 <see cref="CruiserHead.BuildChainDefinition"/> 建的同一副骨架,
    /// 头沿横 8 字巡游、周期性张口冲刺(咬合拍)、鞭毛随速收拢;每 12 秒白化闪切一次阶段(一阶段整链 ↔ 二阶段巨首七节),
    /// 与战斗端的 WhiteTrans 阶段过渡同一着色器。背景是巡游者天幕的紫灰虚空 + 雷电
    /// </summary>
    internal sealed class CruiserPortraitActor : CEBossPortraitActor
    {
        public static CruiserPortraitActor Instance => instance ??= new CruiserPortraitActor();
        private static CruiserPortraitActor instance;

        /// <summary>链节数(含尾节):与二阶段专用件覆盖的节数一致,两阶段都不留空节</summary>
        private const int ChainNodes = 9;
        private const float RigScale = 0.6f;
        private const float PhasePeriod = 12f;
        private const float FlashHalf = 0.35f;
        private const float BitePeriod = 4.6f;

        private Rig2DInstance rig;
        private Vault2DRig rigAsset;
        private int[] segPieces = [];
        private int[] p2Pieces = [];
        private int[] flagPieces = [];
        private int[] flagABones = [];
        private int[] flagBBones = [];
        private int jawDownP1, jawUpP1, jawDownP2, jawUpP2;
        private int headP1Piece, headP2Piece, jawDownP1Piece, jawUpP1Piece, jawDownP2Piece, jawUpP2Piece;
        private int tailBone = -1;

        private readonly CEPortraitMotes motes = new();
        private Vector2 headPos;
        private Vector2 prevHeadPos;
        private float heading;
        /// <summary>路径时钟(咬合冲刺期走得更快)</summary>
        private float pathTime;
        private float mouthDeg;
        private float flagDeg;
        private bool phase2;
        private float phaseTimer;
        /// <summary>白化强度 0..1,峰值处切阶段</summary>
        private float flash;
        private bool flashSwapped;
        private float biteTimer;
        private float boltTimer;
        private float boltLife;
        private int boltSeed;
        private Vector2 boltOrigin;
        private float dustTimer;

        public override Vector2 SceneHalfSize => new(300f, 250f);

        public override CEBossLogTheme Theme => CruiserLogTheme.Instance;

        private CruiserPortraitActor() { }

        private void EnsureRig() {
            if (rig != null) {
                return;
            }
            Rig2DDefinition def = CruiserHead.BuildChainDefinition(ChainNodes, false, out int p2Count);
            if (def == null) {
                return;
            }
            rigAsset = Vault2DRig.FromDefinition(CalamityEntropy.Instance, def);
            rig = rigAsset.CreateInstance(2601);
            //舞台实例:世界坐标是场景局部量,调试叠层会画到屏幕外的无意义位置
            rig.DebugVisible = false;
            rig.Scale = RigScale;

            segPieces = new int[ChainNodes];
            for (int i = 0; i < ChainNodes; i++) {
                segPieces[i] = rig.Piece($"seg{i}");
            }
            tailBone = rig.Bone($"seg{ChainNodes - 1}");
            p2Pieces = new int[p2Count];
            for (int k = 1; k <= p2Count; k++) {
                p2Pieces[k - 1] = rig.Piece($"p2b{k}");
            }
            //常规链只有尾节带鞭毛
            int fa = rig.Bone($"flagA{ChainNodes - 1}");
            if (fa >= 0) {
                flagABones = [fa];
                flagBBones = [rig.Bone($"flagB{ChainNodes - 1}")];
                flagPieces = [rig.Piece($"flagA{ChainNodes - 1}"), rig.Piece($"flagB{ChainNodes - 1}")];
            }
            jawDownP1 = rig.Bone("jawDownP1");
            jawUpP1 = rig.Bone("jawUpP1");
            jawDownP2 = rig.Bone("jawDownP2");
            jawUpP2 = rig.Bone("jawUpP2");
            headP1Piece = rig.Piece("headP1");
            headP2Piece = rig.Piece("headP2");
            jawDownP1Piece = rig.Piece("jawDownP1");
            jawUpP1Piece = rig.Piece("jawUpP1");
            jawDownP2Piece = rig.Piece("jawDownP2");
            jawUpP2Piece = rig.Piece("jawUpP2");
        }

        /// <summary>巡游路径:横 8 字</summary>
        private static Vector2 Path(float t) => new(MathF.Sin(t * 0.7f) * 215f, MathF.Sin(t * 1.4f) * 128f);

        protected override void Reset() {
            EnsureRig();
            motes.Clear();
            pathTime = 0f;
            headPos = Path(0f);
            prevHeadPos = headPos;
            heading = (Path(0.02f) - headPos).ToRotation();
            mouthDeg = 0f;
            flagDeg = 30f;
            phase2 = false;
            phaseTimer = 0f;
            flash = 0f;
            flashSwapped = false;
            biteTimer = 1.5f;
            boltTimer = 1.2f;
            boltLife = 0f;
            dustTimer = 0f;
            if (rig != null) {
                ApplyPhaseVisibility();
                rig.SetRoot(headPos, heading);
                rig.Snap();
            }
        }

        protected override void Update(float dt) {
            float frames = dt * 60f;
            prevHeadPos = headPos;

            //咬合拍:张口冲刺 0.55 秒,随后合口回到巡航速
            biteTimer += dt;
            float lunge = 0f;
            if (biteTimer > BitePeriod) {
                float k = (biteTimer - BitePeriod) / 0.55f;
                if (k >= 1f) {
                    biteTimer = 0f;
                }
                else {
                    lunge = MathF.Sin(k * MathHelper.Pi);
                }
            }
            float wantMouth = lunge * 38f;
            mouthDeg += (wantMouth - mouthDeg) * MathHelper.Clamp(0.25f * frames, 0f, 1f);
            pathTime += dt * (1f + 1.6f * lunge);
            headPos = Path(pathTime);
            Vector2 vel = headPos - prevHeadPos;
            if (vel.LengthSquared() > 0.0001f) {
                heading = vel.ToRotation();
            }
            float speed = frames > 0.001f ? vel.Length() / frames : 0f;
            //鞭毛:越快张得越窄(战斗端静息值同一走向),咬合时猛张一下
            float wantFlag = MathHelper.Clamp(48f - speed * 6f, 8f, 48f) + lunge * 30f;
            flagDeg += (wantFlag - flagDeg) * MathHelper.Clamp(0.12f * frames, 0f, 1f);

            //阶段闪切:白化升到峰值时切件可见,再退回
            phaseTimer += dt;
            if (phaseTimer >= PhasePeriod) {
                float k = phaseTimer - PhasePeriod;
                if (k < FlashHalf) {
                    flash = CEBossLogSkin.Ease(k / FlashHalf);
                }
                else if (k < FlashHalf * 2f) {
                    if (!flashSwapped) {
                        flashSwapped = true;
                        phase2 = !phase2;
                        ApplyPhaseVisibility();
                        PhaseBurst();
                    }
                    flash = 1f - CEBossLogSkin.Ease((k - FlashHalf) / FlashHalf);
                }
                else {
                    flash = 0f;
                    flashSwapped = false;
                    phaseTimer = 0f;
                }
            }

            if (rig != null) {
                rig.SetRoot(headPos, heading);
                float mouth = MathHelper.ToRadians(mouthDeg);
                rig.SetBoneLocalRotation(jawDownP1, mouth);
                rig.SetBoneLocalRotation(jawUpP1, -mouth);
                rig.SetBoneLocalRotation(jawDownP2, mouth * 0.8f);
                rig.SetBoneLocalRotation(jawUpP2, -mouth * 0.8f);
                float baseAngle = MathHelper.Pi;
                float spread = MathHelper.ToRadians(flagDeg);
                for (int i = 0; i < flagABones.Length; i++) {
                    rig.SetBoneLocalRotation(flagABones[i], baseAngle - spread);
                    rig.SetBoneLocalRotation(flagBBones[i], baseAngle + spread);
                }
                rig.Step(frames);
            }

            //雷电:随机间隔一道,寿命 0.4 秒
            boltTimer -= dt;
            if (boltTimer <= 0f) {
                boltTimer = Main.rand.NextFloat(1.6f, 3.4f);
                boltLife = 0.4f;
                boltSeed = Main.rand.Next(100000);
                boltOrigin = new Vector2(Main.rand.NextFloat(-260f, 260f), Main.rand.NextFloat(-220f, 120f));
            }
            boltLife = MathF.Max(0f, boltLife - dt);

            //虚空尘:淡紫小粒缓浮
            dustTimer += dt;
            if (dustTimer > 0.1f) {
                dustTimer = 0f;
                Vector2 half = SceneHalfSize;
                motes.Spawn(new Vector2(Main.rand.NextFloat(-half.X, half.X), half.Y + 6f),
                    new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -Main.rand.NextFloat(0.3f, 0.9f)),
                    new Vector2(Main.rand.NextFloat(1.2f, 2.6f), Main.rand.NextFloat(1.2f, 2.6f)),
                    Color.Lerp(CruiserLogTheme.BoltHalo, CruiserLogTheme.VoidAdd, Main.rand.NextFloat()),
                    Main.rand.NextFloat(3f, 6f), drag: 1f, additive: true);
            }
            //尾后虚空星屑:尾节向后洒
            if (rig != null && tailBone >= 0 && Main.rand.NextBool(2)) {
                ref Bone2D tail = ref rig.Bones[tailBone];
                Vector2 back = tail.Dir.ToRotationVector2();
                Vector2 pos = tail.Pos - back * 30f * RigScale;
                motes.Spawn(pos + Main.rand.NextVector2Circular(6f, 6f), -back * Main.rand.NextFloat(0.4f, 1.4f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    new Vector2(1.6f, 1.6f), CruiserLogTheme.BoltHalo, Main.rand.NextFloat(0.5f, 1.1f), drag: 0.97f, additive: true);
            }
            motes.Update(frames);
        }

        private void ApplyPhaseVisibility() {
            if (rig == null) {
                return;
            }
            for (int i = 0; i < segPieces.Length; i++) {
                rig.Pieces[segPieces[i]].Visible = !phase2;
            }
            for (int i = 0; i < p2Pieces.Length; i++) {
                rig.Pieces[p2Pieces[i]].Visible = phase2;
            }
            for (int i = 0; i < flagPieces.Length; i++) {
                rig.Pieces[flagPieces[i]].Visible = !phase2;
            }
            rig.Pieces[headP1Piece].Visible = !phase2;
            rig.Pieces[jawDownP1Piece].Visible = !phase2;
            rig.Pieces[jawUpP1Piece].Visible = !phase2;
            rig.Pieces[headP2Piece].Visible = phase2;
            rig.Pieces[jawDownP2Piece].Visible = phase2;
            rig.Pieces[jawUpP2Piece].Visible = phase2;
        }

        /// <summary>阶段切换的星屑爆散:沿链每节向外洒一圈</summary>
        private void PhaseBurst() {
            if (rig == null) {
                return;
            }
            for (int i = 0; i < segPieces.Length; i++) {
                int bone = rig.Bone($"seg{i}");
                if (bone < 0) {
                    continue;
                }
                Vector2 c = rig.Bones[bone].Pos;
                for (int k = 0; k < 4; k++) {
                    Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f);
                    motes.Spawn(c, vel, new Vector2(2.2f, 2.2f), CruiserLogTheme.BoltCore, Main.rand.NextFloat(0.4f, 0.8f), drag: 0.94f, additive: true);
                }
            }
        }

        //==================== 绘制 ====================

        public override void Draw(SpriteBatch sb, in CEPortraitFrame frame) {
            DrawSky(sb, in frame);
            DrawChain(sb, in frame);
            motes.Draw(sb, in frame);
        }

        /// <summary>虚空天幕:紫灰渐变 + 星云背光 + 雷电</summary>
        private void DrawSky(SpriteBatch sb, in CEPortraitFrame frame) {
            Vector2 half = frame.SceneHalf;
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, -half.Y, half.Y,
                frame.Dim(new Color(16, 10, 34)), frame.Dim(CruiserLogTheme.VoidBase), 22);
            if (frame.Masked) {
                return;
            }
            CEPortraitDraw.Glow(sb, new Vector2(-half.X * 0.4f, -half.Y * 0.3f), new Vector2(half.X * 1.6f, half.Y * 1.3f), CruiserLogTheme.VoidAdd * 0.55f);
            CEPortraitDraw.Glow(sb, new Vector2(half.X * 0.5f, half.Y * 0.5f), new Vector2(half.X * 1.2f, half.Y * 1.0f), new Color(90, 60, 150) * 0.35f);

            //远处慢闪的虚空星点
            for (int i = 0; i < 40; i++) {
                float hx = CEPortraitDraw.Hash01(i, 4.2f);
                float hy = CEPortraitDraw.Hash01(i, 6.8f);
                Vector2 p = new(-half.X + hx * half.X * 2f, -half.Y + hy * half.Y * 2f);
                float tw = 0.5f + 0.5f * MathF.Sin(Time * (0.7f + hx) + i * 1.9f);
                CEPortraitDraw.Fill(sb, p, new Vector2(1.2f, 1.2f), CruiserLogTheme.BoltCore with { A = 0 } * (0.15f + 0.35f * tw));
            }

            if (boltLife > 0f) {
                float k = boltLife / 0.4f;
                float intensity = k * k;
                CEPortraitDraw.Fill(sb, -half, half * 2f, CruiserLogTheme.BoltHalo with { A = 0 } * (0.12f * intensity));
                CruiserLogTheme.DrawBolt(sb, boltSeed, boltOrigin, 0.7f, intensity, 14);
            }
        }

        /// <summary>整链:剪影模式给黑环境光;白化期间切到 WhiteTrans 着色器批(与战斗端阶段过渡同一 Effect)</summary>
        private void DrawChain(SpriteBatch sb, in CEPortraitFrame frame) {
            if (rig == null || !rig.Built) {
                return;
            }
            Color ambient = frame.Masked ? Color.Black : Color.White;
            Rig2DDrawContext ctx = Rig2DDrawContext.Stage(frame.WorldMatrix, ambient, frame.Scissor);
            Effect white = CEEffectAssets.WhiteTrans;
            bool useShader = !frame.Masked && flash > 0.01f && white != null;
            if (useShader) {
                sb.End();
                CEBossPortraitStage.BeginShader(sb, in frame, white);
                white.Parameters["strength"]?.SetValue(flash);
                white.CurrentTechnique.Passes[0].Apply();
            }
            Rig2DRenderer.Draw(sb, rig, in ctx);
            if (useShader) {
                sb.End();
                CEBossPortraitStage.BeginAlpha(sb, in frame);
            }

            //头前虚空光:张口越大越亮
            if (!frame.Masked && mouthDeg > 2f) {
                float k = mouthDeg / 38f;
                Vector2 mouthPos = headPos + heading.ToRotationVector2() * 40f * RigScale;
                CEPortraitDraw.Glow(sb, mouthPos, 90f * RigScale * (0.6f + k), CruiserLogTheme.BoltHalo * (0.45f * k));
            }
        }
    }
}
