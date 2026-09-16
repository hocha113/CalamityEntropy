using CalamityEntropy.Content.NPCs.Acropolis.Core;
using System.IO;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 一条手臂(炮臂 / 鱼叉臂)。<b>锚定型部件</b>:不是 NPC,也没有位置字段,
    /// 根部由本体 <c>Center</c> 加挂载偏移每帧算出,只有两节的朝向是持久量。
    /// <para>
    /// 联机:两节朝向与反冲速度随包过线(原版就同步这三项)。
    /// 追瞄本身是确定性的,各端跑同一份数学,包只在被打断时纠正
    /// </para>
    /// </summary>
    public class AcropolisHand
    {
        /// <summary>第一节的角速度,开火反冲写它,每帧 ×0.96 衰减</summary>
        public float Seg1RotV = 0;
        public float Seg1Length = 0;
        public float Seg1Rot = 0;
        public float Seg2Rot = 0;
        /// <summary>第一节相对正上方的最大偏摆</summary>
        public float Seg1MaxRadians = MathHelper.ToRadians(AcropolisDirector.HandSeg1MaxDegrees);
        public Vector2 offset;
        public NPC npc;
        /// <summary>未晋升形态时手臂垂向的虚拟目标,纯本地</summary>
        public Vector2 DummyPos = Vector2.Zero;

        public AcropolisHand(NPC n, Vector2 offset, float seg1Length, float seg1Rot, float seg2Rot) {
            npc = n;
            Seg1Length = seg1Length;
            Seg1Rot = seg1Rot;
            Seg2Rot = seg2Rot;
            this.offset = offset;
            DummyPos = n.Center;
        }

        /// <summary>枪口位置</summary>
        public Vector2 TopPos => seg1end + Seg2Rot.ToRotationVector2() * AcropolisDirector.HandTopReach * npc.scale;

        /// <summary>第二节根部(第一节末端)</summary>
        public Vector2 seg1end => npc.Center
            + (offset * new Vector2(((AcropolisMachine)npc.ModNPC).dir, 1) * npc.scale)
                .RotatedBy(((AcropolisMachine)npc.ModNPC).dir > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi))
            + Seg1Rot.ToRotationVector2() * Seg1Length;

        /// <summary>两节一起转向目标点,第一节带偏摆上限</summary>
        public void PointAPos(Vector2 pos) {
            Seg1Rot = CEUtils.RotateTowardsAngle(Seg1Rot,
                (pos - (npc.Center + (offset * new Vector2(((AcropolisMachine)npc.ModNPC).dir, 1))
                    .RotatedBy(((AcropolisMachine)npc.ModNPC).dir > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi)))).ToRotation(),
                AcropolisDirector.HandAimRate, false);
            if (CEUtils.GetAngleBetweenVectors(Seg1Rot.ToRotationVector2(), -Vector2.UnitY) > Seg1MaxRadians * 2) {
                if (Seg1Rot > (MathHelper.PiOver2 + Seg1MaxRadians)) {
                    Seg1Rot = (MathHelper.PiOver2 + Seg1MaxRadians);
                }
                if (Seg1Rot < (MathHelper.PiOver2 - Seg1MaxRadians)) {
                    Seg1Rot = (MathHelper.PiOver2 - Seg1MaxRadians);
                }
            }
            Seg2Rot = CEUtils.RotateTowardsAngle(Seg2Rot, (pos - seg1end).ToRotation(), AcropolisDirector.HandAimRate, false);
        }

        /// <summary>过线块:定长</summary>
        public void NetSend(BinaryWriter writer) {
            writer.Write(Seg1Rot);
            writer.Write(Seg2Rot);
            writer.Write(Seg1RotV);
        }

        public void NetReceive(BinaryReader reader) {
            Seg1Rot = reader.ReadSingle();
            Seg2Rot = reader.ReadSingle();
            Seg1RotV = reader.ReadSingle();
        }

        /// <summary>每帧:未晋升形态自己垂下去,然后结算反冲</summary>
        public void Update() {
            if (!npc.boss) {
                PointAPos(DummyPos);
                DummyPos = Vector2.Lerp(DummyPos, npc.Center + offset + new Vector2(0, Seg1Length * npc.scale * 2), AcropolisDirector.HandDummyLerp);
            }
            Seg1Rot += Seg1RotV;
            Seg1RotV *= AcropolisDirector.HandRecoilDecay;
        }
    }
}
