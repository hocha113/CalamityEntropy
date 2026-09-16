using CalamityEntropy.Content.NPCs.Acropolis.Core;
using System;
using System.IO;
using Terraria;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 一条腿。<b>锚定型部件</b>:它不是 NPC,没有自己的运动积分,
    /// 落脚点 <see cref="StandPoint"/> 每帧以固定速度向 <see cref="targetPos"/> 收敛,
    /// 而 <see cref="targetPos"/> 由本体位置与地形决定。所以它天然自愈,不进位置预测纠偏器。
    /// <para>
    /// 联机:落脚点、目标点、迈步冷却、收敛速度、着地标记、步数种子全部随包过线。
    /// 换落点的撒点搜索原本吃 <c>Main.rand</c>,那会让各端的着地腿数分叉
    /// (着地腿数决定本体朝向与落地判定),所以改成按<b>步数种子</b>播种的确定性随机——
    /// 分布不变,只是各端摇出同一串点
    /// </para>
    /// </summary>
    public class AcropolisLeg
    {
        public Vector2 StandPoint = Vector2.Zero;
        public float Scale = 1f;
        public NPC NPC;
        public Vector2 offset;
        public int NoMoveTime = 0;
        public Vector2 targetPos;
        /// <summary>落脚点收敛速度,换落点那一刻按距离重算</summary>
        public float ms;
        /// <summary>腿序号,参与确定性随机的播种</summary>
        public int Index;
        /// <summary>已经换过几次落点,确定性随机的种子源</summary>
        public int StepSeed;

        /// <summary>过线块:定长,写读顺序必须逐字对应</summary>
        public void NetSend(BinaryWriter writer) {
            writer.Write(NoMoveTime);
            writer.WriteVector2(targetPos);
            writer.WriteVector2(StandPoint);
            writer.Write(ms);
            writer.Write(StepSeed);
            writer.Write(o);
        }

        public void NetReceive(BinaryReader reader) {
            NoMoveTime = reader.ReadInt32();
            targetPos = reader.ReadVector2();
            StandPoint = reader.ReadVector2();
            ms = reader.ReadSingle();
            StepSeed = reader.ReadInt32();
            o = reader.ReadBoolean();
        }

        public AcropolisLeg(NPC npc, Vector2 offset, float scale = 1, int index = 0) {
            NPC = npc;
            this.offset = offset;
            Scale = scale;
            Index = index;
            StandPoint = npc.Center;
        }

        /// <summary>上一次搜索是否真的找到了落点</summary>
        private bool o = false;

        /// <summary>踩在实体上:落脚点处有方块,且上一次搜索确实命中</summary>
        public bool OnTile => !CEUtils.isAir(StandPoint, true) && o;

        /// <summary>返回 true 表示本帧换了落点(宿主据此压同侧腿的迈步冷却)</summary>
        public bool Update() {
            if (NPC.ModNPC is AcropolisMachine am) {
                //未晋升形态 / 还没有落点:腿直接贴着本体,不做地形搜索
                if (am.Dummy || targetPos == Vector2.Zero) {
                    StandPoint = Vector2.Lerp(StandPoint,
                        NPC.Center + ((offset * new Vector2(AcropolisDirector.LegDummySpreadX, AcropolisDirector.LegDummySpreadY))
                            .RotatedBy(am.Dummy ? NPC.rotation : 0) * NPC.scale),
                        AcropolisDirector.LegDummyLerp);
                    targetPos = StandPoint;
                    return false;
                }
            }

            //落脚点向目标收敛;下坠时收敛速度翻三倍(两处阈值不同,原代码如此)
            if (CEUtils.getDistance(StandPoint, targetPos) < ms * (NPC.velocity.Y > AcropolisDirector.LegFastConvergeFallSpeed ? AcropolisDirector.LegFastConvergeMultiplier : 1)) {
                StandPoint = targetPos;
            }
            else {
                StandPoint += (targetPos - StandPoint).normalize() * ms
                    * (NPC.velocity.Y > AcropolisDirector.LegFastConvergeFallSpeedAlt ? AcropolisDirector.LegFastConvergeMultiplier : 1);
            }
            NoMoveTime--;

            float distToMove = AcropolisDirector.LegStepDistance * NPC.scale;
            AcropolisMachine machine = (AcropolisMachine)NPC.ModNPC;
            if (machine.Jumping) {
                //腾空:强制收腿,不再找地面
                o = false;
                targetPos = NPC.Center + new Vector2(offset.X * AcropolisDirector.LegTuckSideFactor, AcropolisDirector.LegTuckDrop) * NPC.scale;
                ms = CEUtils.getDistance(targetPos, StandPoint) * AcropolisDirector.LegConvergeFactor;
                return false;
            }

            //挂点:本体位置按速度前瞻 16 帧,再旋转挂载偏移
            Vector2 anchor = NPC.Center + NPC.velocity * AcropolisDirector.LegLookAheadFrames
                + (offset * NPC.scale).RotatedBy(machine.dir > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi));
            float anchorDist = CEUtils.getDistance(StandPoint, anchor);
            if (!OnTile
                || (NPC.boss && NoMoveTime <= 0 && anchorDist > distToMove)
                || ((NoMoveTime <= 0 || NPC.boss) && anchorDist > distToMove * AcropolisDirector.LegStepRelaxMultiplier)) {
                if (!NPC.boss) {
                    NoMoveTime = AcropolisDirector.LegStepCooldown;
                }
                targetPos = FindStandPoint(
                    anchor + new Vector2(Math.Sign(NPC.velocity.X) == Math.Sign(offset.X)
                        ? (Math.Sign(NPC.velocity.X) * AcropolisDirector.LegLookAheadSpread) : 0, 0),
                    AcropolisDirector.LegSearchRadius * Scale * NPC.scale, AcropolisDirector.LegSearchTries);
                ms = CEUtils.getDistance(targetPos, StandPoint) * AcropolisDirector.LegConvergeFactor;
                if (NoMoveTime < AcropolisDirector.LegStepCooldownMin) {
                    NoMoveTime = AcropolisDirector.LegStepCooldownMin;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// 找落点:在挂点附近撒点,命中实体后沿 Y 轴上抬到贴地。
        /// 撒点用<b>按步数播种</b>的确定性随机,各端摇出同一串点
        /// </summary>
        public Vector2 FindStandPoint(Vector2 center, float MaxOffset, float MaxTry = 64) {
            o = false;
            if (!NPC.boss) {
                center.Y -= AcropolisDirector.LegSearchDummyRise;
            }
            UnifiedRandom rng = new UnifiedRandom(NPC.whoAmI * 7919 + Index * 131 + StepSeed);
            StepSeed++;
            for (int i = 0; i < MaxTry; i++) {
                //原式是 randomPointInCircle(MaxTry):半径直接拿尝试次数当数用,照搬
                float rot = (float)(rng.NextDouble() * MathHelper.Pi * 2);
                Vector2 pos = rot.ToRotationVector2() * rng.NextFloat(-MaxTry, MaxTry) + center;
                if (CEUtils.getDistance(pos, center) <= MaxOffset * 0.9f && AcropolisMachine.CanStandOn(pos)) {
                    o = true;
                    Vector2 orgPos = pos;
                    int c = AcropolisDirector.LegRaiseMaxSteps;
                    while (AcropolisMachine.CanStandOn(pos)) {
                        c--;
                        pos.Y -= AcropolisDirector.LegRaiseStep * NPC.scale;
                        if (c <= 0) {
                            return orgPos;
                        }
                    }
                    pos.Y += AcropolisDirector.LegRaiseStep * NPC.scale;
                    return pos;
                }
            }
            if (!NPC.boss) {
                return Vector2.Zero;
            }
            return NPC.Center + new Vector2(offset.X, AcropolisDirector.LegTuckDrop)
                .RotatedBy(((AcropolisMachine)NPC.ModNPC).dir > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi));
        }
    }
}
