using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 倾斜轨道小白龙(全息三模式之一):本体定住,30 帧放出绕本体做倾 55° 三维椭圆轨道的全息小白龙
    /// (下半圈退到远处小而雾化、上半圈压到镜头前巨大半透明,只有穿过平面的那几节有判定;玩家出圈即脱轨直冲),
    /// 60 帧起每当龙头穿过平面(θ 过 0 / π,P1/P2 每 60 帧、P3 每 50 帧)本体放一发 60 弹形状弹幕:三角 → 圆 → 方 → 圆 → 五角星 → 圆,
    /// 乘法外扩形状不变,持续 420 帧。节拍钉在龙的角速度上:看见龙从背景里绕到跟前那一瞬,弹幕就来
    /// </summary>
    [VaultState((int)VDStateIndex.BlueSky, typeof(VDStateContext))]
    public class VDBlueSkyState : VDStateBase
    {
        public override string StateName => "BlueSky";
        public override VDStateIndex StateIndex => VDStateIndex.BlueSky;
        /// <summary>小白龙绕本体转,本体停在玩家斜上方即可</summary>
        public override Vector2 AnchorFor(VDStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * VDDirector.ConnectorDefaultAnchor.X, VDDirector.ConnectorDefaultAnchor.Y);

        private int bursts;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            bursts = 0;
            if (IsServer) {
                //龙的起始角与状态同一颗骰子:形状弹要在龙头穿过平面那一帧释放,两边都得算得出 θ
                ctx.RolledAngles[0] = Main.rand.NextFloat(MathHelper.TwoPi);
                MarkNetUpdate(ctx);
            }
        }

        /// <summary>龙头此刻的轨道角(与 VDHoloWyvern 同一公式:起始角 + 角速度 × 龙龄)</summary>
        private float WyvernAngle(VDStateContext ctx, int timer)
            => ctx.RolledAngles[0] + VDDirector.WyvernAngularSpeed(ctx.Phase) * Math.Max(0, timer - VDDirector.SkyWyvernFrame);

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            NPC npc = ctx.Npc;
            ctx.CoreColorTarget = VDVfx.SkyBlue;
            DeclareDirect(ctx);
            npc.velocity *= 0.85f;

            ctx.CoreGlow = Math.Max(ctx.CoreGlow, MathHelper.Clamp(Timer / (float)VDDirector.SkyWyvernFrame, 0f, 1f));
            if (Timer == VDDirector.SkyWyvernFrame) {
                ctx.CoreGlow = 1f;
                ctx.WingPulse = 1f;
                ctx.RimFlash = 1f;
                VDVfx.Sound("VoidAnticipation", 0.75f, npc.Center, 3, 1f);
                if (IsServer) {
                    Shoot<VDHoloWyvern>(ctx, npc.Center, Vector2.Zero, VDDirector.DmgHoloBeast, npc.whoAmI, ctx.RolledAngles[0]);
                }
            }
            int burstEnd = VDDirector.SkyBurstStart + VDDirector.SkyBurstDuration;
            if (Timer >= VDDirector.SkyBurstStart && Timer < burstEnd) {
                //龙头穿过平面(sinθ 变号)那一帧放形状弹
                int prevHalf = (int)Math.Floor(WyvernAngle(ctx, Timer - 1) / MathHelper.Pi);
                int curHalf = (int)Math.Floor(WyvernAngle(ctx, Timer) / MathHelper.Pi);
                if (curHalf != prevHalf) {
                    FireShapeBurst(ctx, bursts % 6, bursts);
                    bursts++;
                }
                else {
                    //穿越前 10 帧核心先亮起来:节拍的可读预告
                    float toCross = (curHalf + 1) * MathHelper.Pi - WyvernAngle(ctx, Timer);
                    float framesToCross = toCross / VDDirector.WyvernAngularSpeed(ctx.Phase);
                    if (framesToCross < 10f) {
                        ctx.CoreGlow = Math.Max(ctx.CoreGlow, 1f - framesToCross / 10f);
                    }
                }
            }
            if (Timer >= burstEnd + VDDirector.SkyTail) {
                return EndAttack(ctx);
            }
            return null;
        }

        /// <summary>形状序列:三角 → 圆 → 方 → 圆 → 五角星 → 圆。60 发按周长布点,速度与半径向量成比例,外扩时形状不变</summary>
        private static void FireShapeBurst(VDStateContext ctx, int shapeIndex, int burstIndex) {
            ctx.WingPulse = Math.Max(ctx.WingPulse, 0.7f);
            ctx.CoreGlow = 1f;
            //连发形状弹是节拍器,描边半闪跟拍
            ctx.RimFlash = Math.Max(ctx.RimFlash, 0.6f);
            VDVfx.Sound("CruiserSpit", 1.1f, ctx.Npc.Center, 4, 0.7f);
            if (!IsServer) {
                return;
            }
            float radius = VDDirector.SkyBurstRadius;
            float baseRot = burstIndex * 0.35f;
            List<Vector2> points = shapeIndex switch {
                0 => PolygonPoints(3, radius, baseRot - MathHelper.PiOver2, VDDirector.SkyBurstCount),
                2 => PolygonPoints(4, radius, baseRot + MathHelper.PiOver4, VDDirector.SkyBurstCount),
                4 => StarPoints(radius, baseRot - MathHelper.PiOver2, VDDirector.SkyBurstCount),
                _ => CirclePoints(radius, baseRot, VDDirector.SkyBurstCount)
            };
            foreach (Vector2 p in points) {
                Shoot<VDVoidBolt>(ctx, ctx.Npc.Center + p, p * (VDDirector.SkyBurstSpeed / radius), VDDirector.DmgVoidBolt, VDVoidBolt.ModeShapeBurst);
            }
        }

        private static List<Vector2> CirclePoints(float radius, float rot, int count) {
            var list = new List<Vector2>(count);
            for (int i = 0; i < count; i++) {
                list.Add((rot + MathHelper.TwoPi * i / count).ToRotationVector2() * radius);
            }
            return list;
        }

        /// <summary>正多边形周长均匀布点</summary>
        private static List<Vector2> PolygonPoints(int sides, float radius, float rot, int count) {
            var verts = new Vector2[sides];
            for (int i = 0; i < sides; i++) {
                verts[i] = (rot + MathHelper.TwoPi * i / sides).ToRotationVector2() * radius;
            }
            var list = new List<Vector2>(count);
            int perEdge = count / sides;
            for (int e = 0; e < sides; e++) {
                Vector2 a = verts[e];
                Vector2 b = verts[(e + 1) % sides];
                for (int k = 0; k < perEdge; k++) {
                    list.Add(Vector2.Lerp(a, b, k / (float)perEdge));
                }
            }
            while (list.Count < count) {
                list.Add(verts[list.Count % sides]);
            }
            return list;
        }

        /// <summary>五角星:顶点 k 连到 k+2,五条边均匀布点</summary>
        private static List<Vector2> StarPoints(float radius, float rot, int count) {
            var verts = new Vector2[5];
            for (int i = 0; i < 5; i++) {
                verts[i] = (rot + MathHelper.TwoPi * i / 5f).ToRotationVector2() * radius;
            }
            var list = new List<Vector2>(count);
            int perEdge = count / 5;
            for (int e = 0; e < 5; e++) {
                Vector2 a = verts[e];
                Vector2 b = verts[(e + 2) % 5];
                for (int k = 0; k < perEdge; k++) {
                    list.Add(Vector2.Lerp(a, b, k / (float)perEdge));
                }
            }
            while (list.Count < count) {
                list.Add(verts[list.Count % 5]);
            }
            return list;
        }
    }
}
