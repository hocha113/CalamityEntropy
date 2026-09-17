using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using InnoVault.Rigs2D.Runtime;
using InnoVault.Rigs2D.Solvers;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    /// <summary>
    /// 虚无双子的表现层:InnoVault Rigs2D 骨架(定义在 <c>Assets/Rigs/Nihility.rig.json</c>)。
    /// <para>
    /// 本体为根,<c>anchor</c> 骨在本体后方 64(原 <c>buttom</c>),三层触须对挂在它上面按速度张开;
    /// 通往细胞的绳是一条 29 节 <c>VerletStrand</c>(30 质点、15 次约束迭代、阻尼 0.994、节长贴合两端距离 × 29/35,
    /// 与原 <c>Utilities.Rope</c> 的参数逐项对应)加一条平铺绳索贴图的带状件。
    /// 全部纯本地,不回写任何 gameplay 状态;绳的末端每帧写已同步的细胞坐标
    /// </para>
    /// </summary>
    public partial class NihilityActeriophage
    {
        private Rig2DInstance rig;
        [Rig2DBone("backL", "backR", "midL", "midR", "frontL", "frontR")]
        private readonly int[] layerBones = new int[6];
        [Rig2DPiece("body")]
        private int bodyPiece;
        [Rig2DPiece("bodyAlt")]
        private int bodyAltPiece;
        [Rig2DRibbon("rope")]
        private int ropeRibbon;
        [Rig2DSolver("rope")]
        private VerletStrandSolver ropeSolver;

        /// <summary>绳索在本体那一端的挂点:中心沿朝向后退 64(即骨架里 <c>anchor</c> 骨的位置)</summary>
        public Vector2 buttom => NPC.Center + new Vector2(0, NihilityDirector.RopeAnchorOffset).RotatedBy(NPC.rotation + MathHelper.PiOver2);

        /// <summary>骨架是否可用</summary>
        public bool BodyRigReady => rig != null && rig.Bound && rig.Built;

        /// <summary>
        /// 尾迹尘。频率由 <c>localAI[0]</c> 的余弦给,所以两侧尘线会交替张合。
        /// <c>Dust.NewDust</c> 在服务端直接返回,不吃随机数
        /// </summary>
        public void SpawnParticle(Vector2 center) {
            Vector2 vel = (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * (float)Math.Cos(NPC.localAI[0] * 0.3f) * 16;
            Vector2 vel2 = vel * -1;
            vel -= NPC.velocity * 1f;
            vel2 -= NPC.velocity * 1f;
            Dust.NewDust(center, 1, 1, DustID.MagicMirror, vel.X, vel.Y);
            Dust.NewDust(center, 1, 1, DustID.MagicMirror, vel2.X, vel2.Y);
        }

        /// <summary>二阶段口部激光那一手要换本体贴图。读同步槽 <c>ai[3]</c>,客户端不必等本地换态</summary>
        private bool InLaserPose => Context != null && Context.Phase == 2
            && (int)NPC.ai[3] == (int)NihilityStateIndex.P2Laser;

        private void EnsureBodyRig() {
            if (rig != null) {
                return;
            }
            Vault2DRig asset = CERigAssets.Nihility;
            if (asset == null || !asset.IsValid) {
                return;
            }
            rig = asset.CreateInstance(NPC.whoAmI);
            rig.Bind(this, OnBodyRigBound);
        }

        private void OnBodyRigBound(Rig2DInstance r) {
            //逐帧写目标,无一次性配置
        }

        /// <summary>
        /// 骨架落地(各端都跑)。触须张开量 <c>erot</c> 随速度渐进饱和(纯本地推导量);
        /// 绳的末端 = <c>Lerp(buttom, cell.Center, ropeLerp)</c>,<c>ropeLerp</c> 归零(二阶段)时绳整体停机隐藏
        /// </summary>
        private void UpdateBodyRig() {
            EnsureBodyRig();
            if (rig == null || !rig.Bound) {
                return;
            }
            rig.Scale = NPC.scale;
            rig.SetRoot(NPC.Center, NPC.rotation);

            float erot = (1f - 1f / (1f + NPC.velocity.Length())) * NihilityDirector.TentacleSpreadFactor;
            rig.SetBoneLocalRotation(layerBones[0], -erot);
            rig.SetBoneLocalRotation(layerBones[1], erot);
            rig.SetBoneLocalRotation(layerBones[2], -erot * NihilityDirector.TentacleMidSpreadMul);
            rig.SetBoneLocalRotation(layerBones[3], erot * NihilityDirector.TentacleMidSpreadMul);
            rig.SetBoneLocalRotation(layerBones[4], -erot);
            rig.SetBoneLocalRotation(layerBones[5], erot);

            bool ropeOn = ropeLerp > 0 && cell != null;
            ropeSolver.Enabled = ropeOn;
            rig.Ribbons[ropeRibbon].Visible = ropeOn;
            if (ropeOn) {
                ropeSolver.EndTarget = Vector2.Lerp(buttom, cell.Center, ropeLerp);
            }
            rig.Step();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (spawnAnm > 0 || !BodyRigReady) {
                return false;
            }
            bool alt = InLaserPose;
            rig.Pieces[bodyPiece].Visible = !alt;
            rig.Pieces[bodyAltPiece].Visible = alt;
            //只画件:绳的带状件由细胞的绘制路径经 drawRope 画出,保持迁移前的压盖层次
            Rig2DDrawContext ctx = Rig2DDrawContext.World().Flat(Color.White);
            Rig2DRenderer.Draw(spriteBatch, rig, in ctx);
            return false;
        }

        /// <summary>
        /// 本体与细胞之间的绳索。由 <see cref="ChaoticCell"/> 的绘制路径回调,
        /// 两端读的都是未加平滑偏移的原始 <c>Center</c>(本体与细胞都已关掉 netOffset),不会出现根部跳动。
        /// 带状件会自己切一轮批次(Immediate → 回到 Deferred / AlphaBlend),调用方处在任意已 Begin 的批次内即可
        /// </summary>
        public void drawRope() {
            if (!BodyRigReady || cell == null || ropeLerp <= 0) {
                return;
            }
            Rig2DDrawContext ctx = Rig2DDrawContext.World().Flat(Color.White);
            Rig2DRibbonRenderer.DrawIndices(Main.spriteBatch, rig, rig.Bones, in ctx, [ropeRibbon]);
        }
    }
}
