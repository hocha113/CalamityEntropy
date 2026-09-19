using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Integrations.BossLog;
using InnoVault.Rigs2D.Runtime;
using InnoVault.Rigs2D.Solvers;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    /// <summary>
    /// 虚无双子图鉴沙盒:深渊水体里的双子巡游。噬菌体本体沿 8 字滑行、三层触须随速张合,
    /// 混沌细胞在它身后绕行、八条触须摆头,两者之间一条 29 节 Verlet 绳(与战斗端同一副骨架、同一根绳);
    /// 每 8 秒一拍「口部激光」:本体换激光贴图、口前吐出一道青白光束,细胞触须同时切到加色发光带
    /// </summary>
    internal sealed class NihilityPortraitActor : CEBossPortraitActor
    {
        public static NihilityPortraitActor Instance => instance ??= new NihilityPortraitActor();
        private static NihilityPortraitActor instance;

        private const float BodyScale = 0.8f;
        private const float CellScale = 0.62f;
        private const int TentacleCount = 8;
        private const float LaserPeriod = 8f;
        private const float LaserLength = 1.3f;

        private Rig2DInstance bodyRig;
        private Rig2DInstance cellRig;
        private readonly int[] layerBones = new int[6];
        private int bodyPiece = -1, bodyAltPiece = -1, ropeRibbon = -1;
        private VerletStrandSolver ropeSolver;
        private readonly int[] tentacleRoots = new int[TentacleCount];
        private readonly int[] tentacleRibbons = new int[TentacleCount];
        private readonly int[] glowRibbons = new int[TentacleCount];

        private readonly CEPortraitMotes motes = new();
        private Vector2 bodyPos;
        private Vector2 prevBodyPos;
        private float bodyRot;
        private Vector2 cellPos;
        private float cellRot;
        private float pathTime;
        private float laserTimer;
        /// <summary>激光拍强度 0..1</summary>
        private float laser;
        private bool glowOn;
        private float sporeTimer;

        public override Vector2 SceneHalfSize => new(300f, 250f);

        public override CEBossLogTheme Theme => NihilityLogTheme.Instance;

        private NihilityPortraitActor() { }

        private void EnsureRigs() {
            if (bodyRig == null && CERigAssets.Nihility != null && CERigAssets.Nihility.IsValid) {
                bodyRig = CERigAssets.Nihility.CreateInstance(2602);
                bodyRig.DebugVisible = false;
                bodyRig.Scale = BodyScale;
                string[] names = ["backL", "backR", "midL", "midR", "frontL", "frontR"];
                for (int i = 0; i < names.Length; i++) {
                    layerBones[i] = bodyRig.Bone(names[i]);
                }
                bodyPiece = bodyRig.Piece("body");
                bodyAltPiece = bodyRig.Piece("bodyAlt");
                ropeRibbon = bodyRig.Ribbon("rope");
                ropeSolver = bodyRig.Solver<VerletStrandSolver>("rope");
            }
            if (cellRig == null && CERigAssets.ChaoticCell != null && CERigAssets.ChaoticCell.IsValid) {
                cellRig = CERigAssets.ChaoticCell.CreateInstance(2603);
                cellRig.DebugVisible = false;
                cellRig.Scale = CellScale;
                for (int k = 0; k < TentacleCount; k++) {
                    tentacleRoots[k] = cellRig.Bone($"tRoot{k}");
                    tentacleRibbons[k] = cellRig.Ribbon($"tent{k}");
                    glowRibbons[k] = cellRig.Ribbon($"glow{k}");
                }
            }
        }

        /// <summary>本体巡游路径:横 8 字,偏上</summary>
        private static Vector2 Path(float t) => new(MathF.Sin(t * 0.55f) * 170f, MathF.Sin(t * 1.1f) * 80f - 30f);

        protected override void Reset() {
            EnsureRigs();
            motes.Clear();
            pathTime = 0f;
            bodyPos = Path(0f);
            prevBodyPos = bodyPos;
            bodyRot = (Path(0.02f) - bodyPos).ToRotation();
            cellPos = bodyPos + new Vector2(-150f, 90f);
            cellRot = 0f;
            laserTimer = 2f;
            laser = 0f;
            glowOn = false;
            sporeTimer = 0f;
            if (bodyRig != null) {
                bodyRig.SetRoot(bodyPos, bodyRot);
                bodyRig.Snap();
            }
            if (cellRig != null) {
                cellRig.SetRoot(cellPos, cellRot);
                cellRig.Snap();
                SetTentacleGlow(false);
            }
        }

        protected override void Update(float dt) {
            float frames = dt * 60f;
            prevBodyPos = bodyPos;

            //激光拍:每 8 秒起手,持续 1.3 秒,期间本体减速悬停
            laserTimer += dt;
            float slow = 1f;
            if (laserTimer > LaserPeriod) {
                float k = (laserTimer - LaserPeriod) / LaserLength;
                if (k >= 1f) {
                    laserTimer = 0f;
                    laser = 0f;
                }
                else {
                    laser = MathF.Sin(k * MathHelper.Pi);
                    slow = 0.25f;
                }
            }
            pathTime += dt * slow;
            bodyPos = Path(pathTime);
            Vector2 vel = bodyPos - prevBodyPos;
            if (vel.LengthSquared() > 0.0004f) {
                bodyRot = bodyRot.AngleLerp(vel.ToRotation(), MathHelper.Clamp(0.2f * frames, 0f, 1f));
            }
            float speed = frames > 0.001f ? vel.Length() / frames : 0f;

            //细胞:绕着本体后方一点缓慢绕行,自身随横向速度慢转
            Vector2 orbitCenter = bodyPos - bodyRot.ToRotationVector2() * 60f;
            Vector2 wantCell = orbitCenter + new Vector2(MathF.Cos(Time * 0.7f) * 150f, MathF.Sin(Time * 0.7f) * 95f + 40f);
            Vector2 cellVel = (wantCell - cellPos) * MathHelper.Clamp(0.05f * frames, 0f, 1f);
            cellPos += cellVel;
            cellRot += cellVel.X * 0.006f * frames;

            if (bodyRig != null) {
                bodyRig.SetRoot(bodyPos, bodyRot);
                //触须张开量随速度饱和(战斗端同式),激光拍猛张
                float erot = ((1f - 1f / (1f + speed * 1.6f)) * 0.12f) + laser * 0.25f;
                bodyRig.SetBoneLocalRotation(layerBones[0], -erot);
                bodyRig.SetBoneLocalRotation(layerBones[1], erot);
                bodyRig.SetBoneLocalRotation(layerBones[2], -erot * 5f);
                bodyRig.SetBoneLocalRotation(layerBones[3], erot * 5f);
                bodyRig.SetBoneLocalRotation(layerBones[4], -erot);
                bodyRig.SetBoneLocalRotation(layerBones[5], erot);
                if (ropeSolver != null) {
                    ropeSolver.Enabled = true;
                    ropeSolver.EndTarget = cellPos;
                }
                if (bodyPiece >= 0 && bodyAltPiece >= 0) {
                    bool alt = laser > 0.05f;
                    bodyRig.Pieces[bodyPiece].Visible = !alt;
                    bodyRig.Pieces[bodyAltPiece].Visible = alt;
                }
                bodyRig.Step(frames);
            }
            if (cellRig != null) {
                cellRig.SetRoot(cellPos, cellRot);
                for (int k = 0; k < TentacleCount; k++) {
                    float wobble = MathF.Cos(cellRig.Time * 0.064f + k * 0.008f) * 0.6f;
                    cellRig.SetBoneLocalRotation(tentacleRoots[k], MathHelper.Pi + wobble);
                }
                bool wantGlow = laser > 0.3f;
                if (wantGlow != glowOn) {
                    SetTentacleGlow(wantGlow);
                }
                cellRig.Step(frames);
            }

            //激光口前火花
            if (laser > 0.2f && Main.rand.NextBool(2)) {
                Vector2 mouth = bodyPos + bodyRot.ToRotationVector2() * 70f * BodyScale;
                Vector2 v = bodyRot.ToRotationVector2().RotatedBy(Main.rand.NextFloat(-0.35f, 0.35f)) * Main.rand.NextFloat(3f, 7f);
                motes.Spawn(mouth, v, new Vector2(3f, 1.4f), NihilityLogTheme.SporeCyan, Main.rand.NextFloat(0.25f, 0.5f),
                    drag: 0.95f, rot: v.ToRotation(), additive: true);
            }
            //孢子:自下上浮的柔亮小粒
            sporeTimer += dt;
            if (sporeTimer > 0.09f) {
                sporeTimer = 0f;
                Vector2 half = SceneHalfSize;
                motes.Spawn(new Vector2(Main.rand.NextFloat(-half.X, half.X), half.Y + 6f),
                    new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), -Main.rand.NextFloat(0.4f, 1.1f)),
                    new Vector2(Main.rand.NextFloat(1.5f, 3f), Main.rand.NextFloat(1.5f, 3f)),
                    Color.Lerp(NihilityLogTheme.SporeCyan, NihilityLogTheme.CellViolet, Main.rand.NextFloat()) * 0.7f,
                    Main.rand.NextFloat(3f, 6f), drag: 1f, additive: true);
            }
            //本体尾迹:两侧交替吐出的青尘(战斗端 SpawnParticle 的走向)
            if (Main.rand.NextBool(2)) {
                Vector2 side = (bodyRot + MathHelper.PiOver2).ToRotationVector2() * MathF.Cos(Time * 18f) * 3f;
                motes.Spawn(bodyPos - bodyRot.ToRotationVector2() * 50f * BodyScale, side - vel * 0.3f,
                    new Vector2(1.8f, 1.8f), NihilityLogTheme.SporeCyan * 0.8f, Main.rand.NextFloat(0.3f, 0.6f), drag: 0.96f, additive: true);
            }
            motes.Update(frames);
        }

        private void SetTentacleGlow(bool glow) {
            glowOn = glow;
            if (cellRig == null) {
                return;
            }
            for (int k = 0; k < TentacleCount; k++) {
                if (tentacleRibbons[k] >= 0) {
                    cellRig.Ribbons[tentacleRibbons[k]].Visible = !glow;
                }
                if (glowRibbons[k] >= 0) {
                    cellRig.Ribbons[glowRibbons[k]].Visible = glow;
                }
            }
        }

        //==================== 绘制 ====================

        public override void Draw(SpriteBatch sb, in CEPortraitFrame frame) {
            DrawAbyss(sb, in frame);
            Color ambient = frame.Masked ? Color.Black : Color.White;
            Rig2DDrawContext ctx = Rig2DDrawContext.Stage(frame.WorldMatrix, ambient, frame.Scissor);

            //绳在最底,再细胞(件 + 触须带),本体压最上;带状件会自己切一轮批次并回到舞台批次
            if (bodyRig != null && bodyRig.Built && ropeRibbon >= 0) {
                Rig2DRibbonRenderer.DrawIndices(sb, bodyRig, bodyRig.Bones, in ctx, [ropeRibbon]);
            }
            if (cellRig != null && cellRig.Built) {
                Rig2DRenderer.DrawAll(sb, cellRig, in ctx);
            }
            if (bodyRig != null && bodyRig.Built) {
                Rig2DRenderer.Draw(sb, bodyRig, in ctx);
            }
            if (!frame.Masked) {
                DrawLaser(sb);
            }
            motes.Draw(sb, in frame);
        }

        /// <summary>深渊水体:深蓝渐变 + 背光 + 远处细胞环</summary>
        private void DrawAbyss(SpriteBatch sb, in CEPortraitFrame frame) {
            Vector2 half = frame.SceneHalf;
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, -half.Y, half.Y,
                frame.Dim(new Color(2, 6, 30)), frame.Dim(new Color(8, 30, 80)), 24);
            if (frame.Masked) {
                return;
            }
            CEPortraitDraw.Glow(sb, new Vector2(0f, half.Y * 0.6f), new Vector2(half.X * 2.2f, half.Y * 1.3f), new Color(20, 70, 150) * 0.4f);
            CEPortraitDraw.Glow(sb, new Vector2(half.X * 0.5f, -half.Y * 0.6f), new Vector2(half.X * 1.2f, half.Y * 0.9f), new Color(60, 40, 140) * 0.3f);
            for (int i = 0; i < 7; i++) {
                float ph = i * 37.1f;
                float r = 14f + i % 3 * 9f;
                float x = (Time * (5f + i * 1.5f) + ph * 17f) % (half.X * 2f + 80f) - half.X - 40f;
                float y = -half.Y + (ph * 5.3f) % (half.Y * 2f);
                float breath = 1f + 0.1f * MathF.Sin(Time * 1.3f + ph);
                CEPortraitDraw.Ellipse(sb, new Vector2(x, y), new Vector2(r * breath, r * 0.8f * breath), ph * 0.1f,
                    NihilityLogTheme.CellViolet with { A = 0 } * 0.16f, 1.2f, 22);
            }
        }

        /// <summary>口部激光:青白加色光束(宽紫外晕 + 白芯)自口前射出,长度随拍强度伸缩</summary>
        private void DrawLaser(SpriteBatch sb) {
            if (laser <= 0.05f) {
                return;
            }
            Vector2 dir = bodyRot.ToRotationVector2();
            Vector2 mouth = bodyPos + dir * 62f * BodyScale;
            float len = 420f * laser;
            Vector2 end = mouth + dir * len;
            float flicker = 0.85f + 0.15f * MathF.Sin(Time * 40f);
            CEPortraitDraw.Line(sb, mouth, end, 26f * laser, NihilityLogTheme.CellViolet with { A = 0 } * (0.35f * laser));
            CEPortraitDraw.Line(sb, mouth, end, 12f * laser, NihilityLogTheme.SporeCyan with { A = 0 } * (0.6f * laser * flicker));
            CEPortraitDraw.Line(sb, mouth, end, 4f * laser, Color.White with { A = 0 } * (0.8f * laser * flicker));
            CEPortraitDraw.Glow(sb, mouth, 120f * laser, NihilityLogTheme.SporeCyan * (0.6f * laser));
        }
    }
}
