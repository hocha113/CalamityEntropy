using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Acropolis.Core;
using CalamityEntropy.Core.Integrations.BossLog;
using InnoVault;
using InnoVault.Rigs2D.Runtime;
using InnoVault.Rigs2D.Solvers;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 卫城机器图鉴沙盒:烟霾下的废墟前巡逻。与战斗端同一副骨架(Acropolis.rig.json):四条腿走 FootPlantGait 世界落足步态
    /// (探地换成虚拟地面线)、两条 PointAt 瞄准臂、鱼叉链带。机体在地面线上左右往返巡逻,到边缘掉头(朝向翻转 = 件镜像 + 挂点翻侧);
    /// 炮臂追着前上方一个漂移瞄点,每 2.4 秒开一炮(后坐 + 炮口闪 + 硝烟);每 9 秒鱼叉出膛拖着锁链飞出、悬停、回收归架。
    /// 落步扬尘、顶部排气,背景是被余火背光的卫城残柱
    /// </summary>
    internal sealed class AcropolisPortraitActor : CEBossPortraitActor
    {
        public static AcropolisPortraitActor Instance => instance ??= new AcropolisPortraitActor();
        private static AcropolisPortraitActor instance;

        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/Harpoon")]
        internal static Asset<Texture2D> HarpoonTex = null;

        private const float RigScale = 0.6f;
        /// <summary>地面线(场景坐标 Y)</summary>
        private const float GroundY = 152f;
        /// <summary>机体中心离地高度(髋 36 + 腿静息约 70,已含缩放)</summary>
        private const float StandHeight = 106f;
        private const float PatrolHalf = 150f;
        private const float WalkSpeed = 1.4f;
        private const float CannonPeriod = 2.4f;
        private const float HarpoonPeriod = 9f;
        private const float HarpoonFlySeconds = 0.45f;
        private const float HarpoonHoldSeconds = 0.25f;
        private const float HarpoonBackSeconds = 0.5f;
        private const float HarpoonSpeed = 9f;
        private const int LegCount = AcropolisMachine.LegCount;

        private enum HarpoonStage { Docked, Flying, Hold, Returning }

        private Rig2DInstance rig;
        private int facingBone = -1;
        /// <summary>cannonMount / harpoonMount / harpoonMuzzle / chainTail</summary>
        private readonly int[] armAnchorBones = new int[4];
        /// <summary>cannon1 / cannon2 / harpoon1 / harpoon2</summary>
        private readonly int[] armSegBones = new int[4];
        /// <summary>cannonAim1 / cannonAim2 / harpoonAim1 / harpoonAim2</summary>
        private readonly PointAtSolver[] armSolvers = new PointAtSolver[4];
        private readonly FootPlantGaitSolver[] gaits = new FootPlantGaitSolver[LegCount];
        private readonly int[] footPieces = new int[LegCount];
        private HangChainSolver chainSolver;
        private int chainRibbon = -1;
        /// <summary>body / shoulder / cannon / harpoonLauncher / harpoonDocked:随朝向镜像</summary>
        private readonly int[] facingPieces = new int[5];
        /// <summary>harpoonArm / cannonConnect:从不镜像</summary>
        private readonly int[] armLinkPieces = new int[2];
        private readonly bool[] legWasSwinging = new bool[LegCount];

        private readonly CEPortraitMotes motes = new();
        private Vector2 pos;
        private Vector2 prevPos;
        private float dir;
        private float bob;
        private float cannonTimer;
        private float muzzleFlash;
        private Vector2 muzzlePos;
        private float harpoonTimer;
        private HarpoonStage harpoonStage;
        private float harpoonK;
        private Vector2 harpoonPos;
        private float harpoonDir;
        private Vector2 harpoonReturnFrom;
        private float exhaustTimer;
        private float emberTimer;

        public override Vector2 SceneHalfSize => new(300f, 250f);

        public override CEBossLogTheme Theme => AcropolisLogTheme.Instance;

        private AcropolisPortraitActor() { }

        private void EnsureRig() {
            if (rig != null || CERigAssets.Acropolis == null || !CERigAssets.Acropolis.IsValid) {
                return;
            }
            rig = CERigAssets.Acropolis.CreateInstance(2606);
            //舞台实例:世界坐标是场景局部量,调试叠层会画到屏幕外的无意义位置
            rig.DebugVisible = false;
            rig.Scale = RigScale;

            facingBone = rig.Bone("facing");
            string[] anchors = ["cannonMount", "harpoonMount", "harpoonMuzzle", "chainTail"];
            string[] segs = ["cannon1", "cannon2", "harpoon1", "harpoon2"];
            string[] aims = ["cannonAim1", "cannonAim2", "harpoonAim1", "harpoonAim2"];
            for (int i = 0; i < 4; i++) {
                armAnchorBones[i] = rig.Bone(anchors[i]);
                armSegBones[i] = rig.Bone(segs[i]);
                armSolvers[i] = rig.Solver<PointAtSolver>(aims[i]);
            }
            chainSolver = rig.Solver<HangChainSolver>("chain");
            chainRibbon = rig.Ribbon("chain");
            string[] facing = ["body", "shoulder", "cannon", "harpoonLauncher", "harpoonDocked"];
            for (int i = 0; i < facing.Length; i++) {
                facingPieces[i] = rig.Piece(facing[i]);
            }
            armLinkPieces[0] = rig.Piece("harpoonArm");
            armLinkPieces[1] = rig.Piece("cannonConnect");

            //腿:探地换虚拟地面线,脚掌贴图倾角与镜像照战斗端 OnRigBound
            float footTilt = MathHelper.ToRadians(AcropolisDirector.FootTiltDegrees);
            for (int i = 0; i < LegCount; i++) {
                gaits[i] = rig.Solver<FootPlantGaitSolver>($"gait{i}");
                if (gaits[i] != null) {
                    gaits[i].Probe = FlatGroundProbe;
                    gaits[i].GroundDir = Vector2.UnitY;
                    gaits[i].AutoPhase = true;
                    gaits[i].Attached = true;
                }
                footPieces[i] = rig.Piece($"tibia{i}");
                if (footPieces[i] >= 0) {
                    ref Piece2DState foot = ref rig.PieceRef(footPieces[i]);
                    foot.ExtraRotation = AcropolisMachine.LegSide(i) * footTilt;
                    foot.Mirror = AcropolisMachine.LegSide(i) < 0;
                }
            }
        }

        /// <summary>虚拟地面探测:地面是一条水平线,射线向下命中即落点;起点已在地下时直接顶回地表</summary>
        private static bool FlatGroundProbe(Vector2 from, Vector2 dir, float maxDistance, out Vector2 hit) {
            if (from.Y >= GroundY) {
                hit = new Vector2(from.X, GroundY);
                return true;
            }
            if (dir.Y <= 0.0001f) {
                hit = from + dir * maxDistance;
                return false;
            }
            float t = (GroundY - from.Y) / dir.Y;
            if (t <= maxDistance) {
                hit = from + dir * t;
                return true;
            }
            hit = from + dir * maxDistance;
            return false;
        }

        protected override void Reset() {
            EnsureRig();
            motes.Clear();
            dir = 1f;
            pos = new Vector2(-PatrolHalf * 0.5f, GroundY - StandHeight);
            prevPos = pos;
            bob = 0f;
            cannonTimer = 0.8f;
            muzzleFlash = 0f;
            harpoonTimer = 3f;
            harpoonStage = HarpoonStage.Docked;
            harpoonK = 0f;
            exhaustTimer = emberTimer = 0f;
            Array.Clear(legWasSwinging);
            if (rig != null) {
                ApplyFacing();
                rig.SetRoot(pos, 0f);
                for (int i = 0; i < 4; i++) {
                    if (armSolvers[i] != null) {
                        armSolvers[i].Target = pos + new Vector2(dir * 200f, -60f);
                    }
                }
                SetHarpoonFlying(false);
                rig.Snap();
            }
        }

        protected override void Update(float dt) {
            float frames = dt * 60f;
            prevPos = pos;

            //巡逻:到边缘掉头
            pos.X += dir * WalkSpeed * frames;
            if (dir > 0f && pos.X > PatrolHalf) {
                dir = -1f;
            }
            else if (dir < 0f && pos.X < -PatrolHalf) {
                dir = 1f;
            }
            bob += 0.06f * frames;
            pos.Y = GroundY - StandHeight + MathF.Sin(bob) * 2.2f;
            Vector2 vel = pos - prevPos;

            //瞄点:前上方漂移
            Vector2 cannonAim = pos + new Vector2(dir * 230f, -70f + MathF.Sin(Time * 0.9f) * 45f);
            Vector2 harpoonAim = pos + new Vector2(dir * 210f, -24f + MathF.Sin(Time * 0.7f + 1f) * 30f);

            //炮击拍:后坐 + 炮口闪 + 硝烟 + 一发亮弹
            cannonTimer += dt;
            muzzleFlash = MathF.Max(0f, muzzleFlash - dt * 5f);
            if (cannonTimer > CannonPeriod && rig != null && rig.Built) {
                cannonTimer = 0f;
                FireCannon(cannonAim);
            }

            //鱼叉拍:出膛 → 悬停 → 回收
            UpdateHarpoon(dt, frames);

            if (rig != null) {
                rig.SetRoot(pos, 0f);
                ApplyFacing();
                for (int i = 0; i < LegCount; i++) {
                    if (gaits[i] != null) {
                        gaits[i].Velocity = vel;
                    }
                }
                if (armSolvers[0] != null && armSolvers[1] != null) {
                    armSolvers[0].Target = cannonAim;
                    armSolvers[1].Target = cannonAim;
                }
                if (armSolvers[2] != null && armSolvers[3] != null) {
                    armSolvers[2].Target = harpoonAim;
                    armSolvers[3].Target = harpoonAim;
                }
                bool flying = harpoonStage != HarpoonStage.Docked;
                if (chainSolver != null) {
                    chainSolver.Enabled = flying;
                    if (flying) {
                        chainSolver.Target = harpoonPos;
                    }
                }
                rig.Step(frames);
                SetHarpoonFlying(flying);
                if (armSegBones[1] >= 0) {
                    muzzlePos = rig.Bones[armSegBones[1]].Tip;
                }

                //落步扬尘:腿由摆越转入落地那一帧
                for (int i = 0; i < LegCount; i++) {
                    if (gaits[i] == null) {
                        continue;
                    }
                    FootPlantGaitSolver.LegState ls = gaits[i].Leg(0);
                    if (legWasSwinging[i] && !ls.Swinging && ls.Inited) {
                        Dust(ls.Foot, 5, 0.8f);
                    }
                    legWasSwinging[i] = ls.Swinging;
                }
            }

            //顶部排气:暗灰烟粒自机顶缓升
            exhaustTimer += dt;
            if (exhaustTimer > 0.1f) {
                exhaustTimer = 0f;
                Vector2 vent = pos + new Vector2(-dir * 34f, -60f) * RigScale;
                motes.Spawn(vent + Main.rand.NextVector2Circular(4f, 2f), new Vector2(-dir * Main.rand.NextFloat(0.1f, 0.4f), -Main.rand.NextFloat(0.5f, 1f)),
                    new Vector2(Main.rand.NextFloat(5f, 9f), Main.rand.NextFloat(4f, 7f)),
                    Color.Lerp(AcropolisLogTheme.Soot, AcropolisLogTheme.Smoke, Main.rand.NextFloat()) * 0.55f,
                    Main.rand.NextFloat(1.2f, 2f), gravity: -0.01f, drag: 0.985f, rot: Main.rand.NextFloat(MathHelper.TwoPi), rotVel: Main.rand.NextFloat(-0.05f, 0.05f));
            }
            //荒原余烬:自下缓升的橙亮小粒
            emberTimer += dt;
            if (emberTimer > 0.24f) {
                emberTimer = 0f;
                Vector2 half = SceneHalfSize;
                motes.Spawn(new Vector2(Main.rand.NextFloat(-half.X, half.X), GroundY + Main.rand.NextFloat(0f, 30f)),
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.4f, 0.9f)),
                    new Vector2(1.6f, 1.6f), Color.Lerp(AcropolisLogTheme.Ember, AcropolisLogTheme.Brass, Main.rand.NextFloat(0.5f)),
                    Main.rand.NextFloat(2.5f, 4.5f), drag: 1f, additive: true);
            }
            motes.Update(frames);
        }

        /// <summary>随朝向翻转的量:facing 骨局部半圈、臂挂点 x 乘 dir、枪口侧向偏移乘 dir、五件镜像(与战斗端 UpdateRig / SyncFacingPieces 同式)</summary>
        private void ApplyFacing() {
            bool left = dir < 0f;
            if (facingBone >= 0) {
                rig.SetBoneLocalRotation(facingBone, left ? MathHelper.Pi : 0f);
            }
            if (armAnchorBones[0] >= 0) {
                rig.SetBoneLocalOffset(armAnchorBones[0], new Vector2(AcropolisDirector.CannonMountX * dir, AcropolisDirector.CannonMountY));
            }
            if (armAnchorBones[1] >= 0) {
                rig.SetBoneLocalOffset(armAnchorBones[1], new Vector2(AcropolisDirector.HarpoonMountX * dir, AcropolisDirector.HarpoonMountY));
            }
            if (armAnchorBones[2] >= 0) {
                rig.SetBoneLocalOffset(armAnchorBones[2], new Vector2(0f, AcropolisDirector.HarpoonMuzzleSide * dir));
            }
            if (armAnchorBones[3] >= 0) {
                rig.SetBoneLocalOffset(armAnchorBones[3], new Vector2(AcropolisDirector.HarpoonMuzzleReach - AcropolisDirector.HarpoonChainTail, AcropolisDirector.HarpoonMuzzleSide * dir));
            }
            for (int i = 0; i < facingPieces.Length; i++) {
                if (facingPieces[i] >= 0) {
                    rig.Pieces[facingPieces[i]].Mirror = left;
                }
            }
            for (int i = 0; i < armLinkPieces.Length; i++) {
                if (armLinkPieces[i] >= 0) {
                    rig.Pieces[armLinkPieces[i]].Mirror = false;
                }
            }
        }

        /// <summary>鱼叉在飞:停靠件隐藏、锁链带显示</summary>
        private void SetHarpoonFlying(bool flying) {
            if (facingPieces[4] >= 0) {
                rig.Pieces[facingPieces[4]].Visible = !flying;
            }
            if (chainRibbon >= 0) {
                rig.Ribbons[chainRibbon].Visible = flying;
            }
        }

        private void UpdateHarpoon(float dt, float frames) {
            if (rig == null || !rig.Built || armAnchorBones[2] < 0 || armSegBones[3] < 0) {
                return;
            }
            Vector2 muzzle = rig.Bones[armAnchorBones[2]].Pos;
            switch (harpoonStage) {
                case HarpoonStage.Docked:
                    harpoonTimer += dt;
                    if (harpoonTimer > HarpoonPeriod) {
                        harpoonTimer = 0f;
                        harpoonStage = HarpoonStage.Flying;
                        harpoonK = 0f;
                        harpoonPos = muzzle;
                        harpoonDir = rig.Bones[armSegBones[3]].Dir;
                        armSolvers[2]?.Kick(-0.16f * dir);
                        Sparks(muzzle, harpoonDir, 10, 1f);
                    }
                    break;
                case HarpoonStage.Flying:
                    harpoonK += dt / HarpoonFlySeconds;
                    harpoonPos += harpoonDir.ToRotationVector2() * HarpoonSpeed * frames;
                    if (harpoonK >= 1f) {
                        harpoonStage = HarpoonStage.Hold;
                        harpoonK = 0f;
                        Sparks(harpoonPos, harpoonDir + MathHelper.Pi, 8, 0.6f);
                    }
                    break;
                case HarpoonStage.Hold:
                    harpoonK += dt / HarpoonHoldSeconds;
                    harpoonPos += new Vector2(0f, MathF.Sin(Time * 30f) * 0.3f * frames);
                    if (harpoonK >= 1f) {
                        harpoonStage = HarpoonStage.Returning;
                        harpoonK = 0f;
                        harpoonReturnFrom = harpoonPos;
                    }
                    break;
                case HarpoonStage.Returning:
                    harpoonK += dt / HarpoonBackSeconds;
                    harpoonPos = Vector2.Lerp(harpoonReturnFrom, muzzle, CEBossLogSkin.Ease(harpoonK));
                    harpoonDir = harpoonDir.AngleLerp(rig.Bones[armSegBones[3]].Dir, MathHelper.Clamp(0.2f * frames, 0f, 1f));
                    if (harpoonK >= 1f) {
                        harpoonStage = HarpoonStage.Docked;
                        harpoonTimer = 0f;
                        Dust(muzzle, 3, 0.4f);
                    }
                    break;
            }
        }

        /// <summary>开炮:第一节后坐、炮口闪、硝烟与火花、一发沿炮管飞出的亮弹</summary>
        private void FireCannon(Vector2 aim) {
            if (armSegBones[1] < 0) {
                return;
            }
            ref Bone2D barrel = ref rig.Bones[armSegBones[1]];
            Vector2 muzzle = barrel.Tip;
            Vector2 fwd = barrel.Dir.ToRotationVector2();
            muzzleFlash = 1f;
            muzzlePos = muzzle;
            armSolvers[0]?.Kick(-0.22f * dir);
            //亮弹
            motes.Spawn(muzzle, fwd * 11f, new Vector2(9f, 3f), AcropolisLogTheme.Ember, 0.55f, drag: 1f, rot: barrel.Dir, additive: true);
            motes.Spawn(muzzle, fwd * 11f, new Vector2(5f, 1.6f), Color.White, 0.55f, drag: 1f, rot: barrel.Dir, additive: true);
            Sparks(muzzle, barrel.Dir, 12, 1.1f);
            //硝烟
            for (int i = 0; i < 7; i++) {
                Vector2 v = fwd.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(1f, 2.6f) + new Vector2(0f, -0.4f);
                motes.Spawn(muzzle + Main.rand.NextVector2Circular(6f, 6f), v,
                    new Vector2(Main.rand.NextFloat(6f, 11f), Main.rand.NextFloat(5f, 9f)),
                    Color.Lerp(AcropolisLogTheme.Smoke, AcropolisLogTheme.Soot, Main.rand.NextFloat()) * 0.6f,
                    Main.rand.NextFloat(0.8f, 1.5f), gravity: -0.015f, drag: 0.94f, rot: Main.rand.NextFloat(MathHelper.TwoPi), rotVel: Main.rand.NextFloat(-0.08f, 0.08f));
            }
        }

        private void Sparks(Vector2 pos, float along, int count, float power) {
            for (int i = 0; i < count; i++) {
                Vector2 vel = (along + Main.rand.NextFloat(-0.7f, 0.7f)).ToRotationVector2() * Main.rand.NextFloat(2f, 6f) * power;
                motes.Spawn(pos, vel, new Vector2(Main.rand.NextFloat(2.5f, 4.5f), 1.2f),
                    Color.Lerp(AcropolisLogTheme.Ember, Color.White, Main.rand.NextFloat(0.5f)),
                    Main.rand.NextFloat(0.3f, 0.7f), gravity: 0.12f, drag: 0.95f, rot: vel.ToRotation(), additive: true);
            }
        }

        private void Dust(Vector2 pos, int count, float power) {
            for (int i = 0; i < count; i++) {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), -Main.rand.NextFloat(0.6f, 2f)) * power;
                motes.Spawn(pos + new Vector2(Main.rand.NextFloat(-8f, 8f), 0f), vel,
                    new Vector2(Main.rand.NextFloat(3f, 6f), Main.rand.NextFloat(2f, 4f)),
                    Color.Lerp(AcropolisLogTheme.DustWarm, AcropolisLogTheme.DustDark, Main.rand.NextFloat()) * 0.8f,
                    Main.rand.NextFloat(0.5f, 1f), gravity: 0.14f, drag: 0.97f, rot: Main.rand.NextFloat(MathHelper.TwoPi), rotVel: Main.rand.NextFloat(-0.1f, 0.1f));
            }
        }

        //==================== 绘制 ====================

        public override void Draw(SpriteBatch sb, in CEPortraitFrame frame) {
            DrawScene(sb, in frame);
            if (rig != null && rig.Built) {
                Color ambient = frame.Masked ? Color.Black : AcropolisLogTheme.MachineAmbient;
                Rig2DDrawContext ctx = Rig2DDrawContext.Stage(frame.WorldMatrix, ambient, frame.Scissor);
                //腿 → 鱼叉臂 → 停靠鱼叉 → 发射器 → 本体 → 炮臂 → 肩甲,锁链带(层 0)在飞行时垫最底;带状件自己切一轮批次并回到舞台批次
                Rig2DRenderer.DrawAll(sb, rig, in ctx);
                DrawHarpoon(sb, in frame);
                if (!frame.Masked && muzzleFlash > 0.01f) {
                    CEPortraitDraw.Glow(sb, muzzlePos, 110f * RigScale * (0.6f + muzzleFlash), AcropolisLogTheme.Ember * (0.9f * muzzleFlash));
                    CEPortraitDraw.Glow(sb, muzzlePos, 50f * RigScale, Color.White * (0.6f * muzzleFlash));
                }
            }
            motes.Draw(sb, in frame);
        }

        /// <summary>飞行中的鱼叉:与停靠件同一贴图与原点,沿飞行方向,朝左时沿轴翻面</summary>
        private void DrawHarpoon(SpriteBatch sb, in CEPortraitFrame frame) {
            if (harpoonStage == HarpoonStage.Docked) {
                return;
            }
            Texture2D tex = HarpoonTex?.Value;
            if (tex == null) {
                return;
            }
            SpriteEffects fx = dir > 0f ? SpriteEffects.None : SpriteEffects.FlipVertically;
            sb.Draw(tex, harpoonPos, null, frame.Tint(AcropolisLogTheme.MachineAmbient), harpoonDir, new Vector2(70f, 20f), RigScale, fx, 0f);
            if (!frame.Masked) {
                CEPortraitDraw.Glow(sb, harpoonPos + harpoonDir.ToRotationVector2() * 16f * RigScale, 40f * RigScale, AcropolisLogTheme.Ember * 0.5f);
            }
        }

        /// <summary>烟霾暮空 + 余火背光 + 卫城残柱剪影 + 焦土地带 + 飘烟</summary>
        private void DrawScene(SpriteBatch sb, in CEPortraitFrame frame) {
            Vector2 half = frame.SceneHalf;
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, -half.Y, GroundY,
                frame.Dim(AcropolisLogTheme.SmogTop), frame.Dim(AcropolisLogTheme.SmogBottom), 22);

            if (!frame.Masked) {
                //余火背光:地平线上一轮低垂的浊日与暗橙背光
                CEPortraitDraw.Glow(sb, new Vector2(-half.X * 0.3f, GroundY - 40f), new Vector2(half.X * 2.4f, 300f), AcropolisLogTheme.Ember * 0.28f);
                CEPortraitDraw.Glow(sb, new Vector2(-half.X * 0.3f, GroundY - 70f), 140f, AcropolisLogTheme.Brass * 0.45f);
            }

            //卫城残柱:远景一排断柱与残梁的剪影,底部略亮(被余火背光)
            Color pillar = frame.Dim(AcropolisLogTheme.Ruin);
            Color pillarLit = frame.Dim(AcropolisLogTheme.RuinLit);
            for (int i = 0; i < 7; i++) {
                float hx = CEPortraitDraw.Hash01(i, 1.3f);
                float hh = CEPortraitDraw.Hash01(i, 4.6f);
                float x = -half.X + 30f + hx * (half.X * 2f - 60f);
                float w = 14f + CEPortraitDraw.Hash01(i, 8.2f) * 10f;
                float h = 90f + hh * 80f;
                //柱身:上暗下微亮,顶端一截更宽的柱头
                CEPortraitDraw.VerticalGradient(sb, x - w * 0.5f, x + w * 0.5f, GroundY - h, GroundY, pillar, pillarLit, 6);
                CEPortraitDraw.Fill(sb, new Vector2(x - w * 0.5f - 4f, GroundY - h - 8f), new Vector2(w + 8f, 8f), pillar);
                //相邻两柱之间偶有残梁
                if (i % 3 == 1) {
                    float nx = -half.X + 30f + CEPortraitDraw.Hash01(i + 1, 1.3f) * (half.X * 2f - 60f);
                    float nh = 90f + CEPortraitDraw.Hash01(i + 1, 4.6f) * 80f;
                    float top = MathF.Min(GroundY - h, GroundY - nh) - 8f;
                    float x0 = MathF.Min(x, nx) - 6f;
                    float x1 = MathF.Max(x, nx) + 6f;
                    CEPortraitDraw.Fill(sb, new Vector2(x0, top - 10f), new Vector2(x1 - x0, 10f), pillar);
                }
            }

            //焦土地带:不透明,上亮下暗,顶缘一线亮土
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, GroundY, half.Y,
                frame.Dim(AcropolisLogTheme.SoilTop), frame.Dim(AcropolisLogTheme.SoilBottom), 12);
            CEPortraitDraw.Fill(sb, new Vector2(-half.X, GroundY - 1.5f), new Vector2(half.X * 2f, 2.2f), frame.Dim(AcropolisLogTheme.DustWarm));
            if (frame.Masked) {
                return;
            }
            //地面碎石:零星暗点
            for (int i = 0; i < 26; i++) {
                float hx = CEPortraitDraw.Hash01(i, 5.9f);
                float hy = CEPortraitDraw.Hash01(i, 3.1f);
                Vector2 p = new(-half.X + hx * half.X * 2f, GroundY + 6f + hy * (half.Y - GroundY - 10f));
                float s = 1.5f + hy * 2.5f;
                CEPortraitDraw.Fill(sb, p, new Vector2(s * 1.6f, s), AcropolisLogTheme.SoilBottom * 0.9f);
            }
            //飘烟:低空几团暗灰烟羽缓移
            for (int i = 0; i < 4; i++) {
                float ph = i * 1.6f;
                float x = (Time * (7f + i * 2.5f) + ph * 160f) % (half.X * 2f + 320f) - half.X - 160f;
                float y = GroundY - 60f - 40f * MathF.Sin(ph) - i * 18f;
                CEPortraitDraw.Puff(sb, new Vector2(x, y), 280f + i * 40f, AcropolisLogTheme.Smoke * 0.14f, ph + Time * 0.03f);
            }
        }
    }
}
