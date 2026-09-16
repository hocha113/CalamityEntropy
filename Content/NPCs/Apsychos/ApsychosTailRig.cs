using CalamityEntropy.Content.NPCs.Apsychos.Core;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    public partial class Apsychos
    {
        /// <summary>
        /// 尾巴骨架落地。三档数字全部来自 Director,与重构前逐字对齐。
        /// 12 节虚拟骨节由已过线的朝向/位置确定性重算,不过线
        /// </summary>
        public void UpdateTail() {
            if (segs == null) {
                segs = new List<TailSeg>();
                for (int i = 0; i < ApsychosDirector.TailSegCount; i++) {
                    segs.Add(new TailSeg() { Center = NPC.Center, rotation = NPC.rotation + MathHelper.Pi });
                }
            }
            ApsychosTailStyle style = Context?.TailStyle ?? ApsychosTailStyle.Follow;
            if (style == ApsychosTailStyle.Follow) {
                for (int i = 0; i < segs.Count; i++) {
                    float fRot = i == 0 ? NPC.rotation + MathHelper.Pi : segs[i - 1].rotation;
                    Vector2 fPos = i == 0 ? NPC.velocity + NPC.Center - NPC.rotation.ToRotationVector2() * ApsychosDirector.NeckOffset * NPC.scale : segs[i - 1].Center;
                    float spacing = ApsychosDirector.SegSpacing * NPC.scale;
                    segs[i].rotation = CEUtils.RotateTowardsAngle((segs[i].Center - fPos).ToRotation(), fRot, ApsychosDirector.SegRotateRate, false);
                    segs[i].Center = fPos + segs[i].rotation.ToRotationVector2() * spacing;
                }
                if (tail != null) {
                    int c = segs.Count;
                    float fRot = segs[c - 1].rotation;
                    Vector2 fPos = segs[c - 1].Center;
                    float spacing = ApsychosDirector.TailSpacing * NPC.scale;
                    tail.rotation = CEUtils.RotateTowardsAngle((tail.Center - fPos).ToRotation(), fRot, ApsychosDirector.SegRotateRate, false);
                    tail.Center = fPos + tail.rotation.ToRotationVector2() * spacing;
                }
            }
            else {
                for (int i = 0; i < segs.Count; i++) {
                    segs[i].Center += NPC.velocity;
                }
            }
            if (style == ApsychosTailStyle.OnePoint) {
                Vector2 p1 = NPC.Center - NPC.rotation.ToRotationVector2() * ApsychosDirector.BezierCtrl1Offset * NPC.scale;
                for (int i = 0; i < segs.Count; i++) {
                    float pg = i / (segs.Count - 1f);
                    List<Vector2> lt = new List<Vector2> { NPC.Center - NPC.rotation.ToRotationVector2() * ApsychosDirector.NeckOffset * NPC.scale, p1, tail.Center };
                    Vector2 p = CEUtils.Bezier(lt, pg);
                    segs[i].Center = Vector2.Lerp(segs[i].Center, p, ApsychosDirector.BezierFollowLerp);
                    Vector2 fp = i == 0 ? NPC.Center + NPC.rotation.ToRotationVector2() * ApsychosDirector.SegFacingOffset : segs[i - 1].Center;
                    segs[i].rotation = (segs[i].Center - fp).ToRotation();
                }
                tail.rotation = segs[segs.Count - 1].rotation;
            }
            if (style == ApsychosTailStyle.TwoPoint) {
                Vector2 p1 = NPC.Center - NPC.rotation.ToRotationVector2() * ApsychosDirector.BezierCtrl1Offset * NPC.scale;
                Vector2 p2 = tail.Center - tail.rotation.ToRotationVector2() * ApsychosDirector.BezierCtrl2Offset * NPC.scale;
                for (int i = 0; i < segs.Count; i++) {
                    float pg = i / (segs.Count - 1f);
                    List<Vector2> lt = new List<Vector2> { NPC.Center - NPC.rotation.ToRotationVector2() * ApsychosDirector.NeckOffset * NPC.scale, p1, p2, tail.Center };
                    Vector2 p = CEUtils.Bezier(lt, pg);
                    segs[i].Center = Vector2.Lerp(segs[i].Center, p, ApsychosDirector.BezierFollowLerp);
                    Vector2 fp = i == 0 ? NPC.Center + NPC.rotation.ToRotationVector2() * ApsychosDirector.SegFacingOffset : segs[i - 1].Center;
                    segs[i].rotation = (segs[i].Center - fp).ToRotation();
                }
            }
        }
    }
}
