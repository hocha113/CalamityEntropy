using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 相位激光:12 架装饰无人机从 Z 2 的背景降入环阵绕本体一圈(装饰即深度预告)→ 闪现到玩家头顶 →
    /// 纵列(每列 9 架,玩家左侧 35 格,P2 起列间隔 45→30 收缩,平面扫线)→ 静默 60 帧 →
    /// 透视点阵(重拍):7 × 9 架无人机停在 Z 1.8 的背景里,各自朝镜头发一道 Z 射线,落点是以玩家为中心、格距 110 的一片半径 40 的圆;
    /// 60 帧预警(背景里每架到脚下格点的收敛细线 + 格点小环收紧)后同帧打下,P3 错半格再来一轮(上一轮的安全格心正好是新一轮的落点)。
    /// 公平阀:纵列无人机自带 60 帧预警线;点阵人站进格子中央离四周落点各 55px;自带闪现,不走 hub 的四角闪
    /// </summary>
    [VaultState((int)VDStateIndex.PhaseLaser, typeof(VDStateContext))]
    public class VDPhaseLaserState : VDStateBase
    {
        public override string StateName => "PhaseLaser";
        public override VDStateIndex StateIndex => VDStateIndex.PhaseLaser;

        private enum Beat { Decor, Columns, Lattice }
        private Beat beat;
        private int columnsDone;
        private int pulsesFired;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Decor;
            columnsDone = 0;
            pulsesFired = 0;
        }

        /// <summary>第 k 轮点阵的起手帧(拍内计时):一轮 = 预警 + 判定 + 轮间隔</summary>
        private static int PulseStart(int k) => 1 + k * (VDDirector.LatticeWarn + VDDirector.LatticeStrikeFrames + VDDirector.LatticePulseGap);

        public override IVDState OnUpdate(VDStateContext ctx) {
            Timer++;
            bool ex = ctx.Phase >= 2;
            int columns = VDDirector.LaserColumns(ctx.Phase);
            switch (beat) {
                case Beat.Decor:
                    if (Timer == 1) {
                        for (int i = 0; i < VDDirector.LaserDecorDrones; i++) {
                            SpawnVisual<VDSporeDrone>(ctx, ctx.Npc.Center, Vector2.Zero, VDSporeDrone.ModeDecor, MathHelper.TwoPi * i / VDDirector.LaserDecorDrones, ctx.Npc.whoAmI);
                        }
                        VDVfx.Sound("VoidAnticipation", 1.2f, ctx.Npc.Center, 3, 0.8f);
                    }
                    ctx.CoreGlow = Math.Max(ctx.CoreGlow, Timer / (float)VDDirector.LaserDecorFrames);
                    if (Timer >= VDDirector.LaserDecorFrames) {
                        if (IsServer) {
                            ctx.Owner.StartBlink(ctx.Target.Center + VDDirector.LaserHoverOffset);
                        }
                        SwitchBeat(Beat.Columns);
                    }
                    break;
                case Beat.Columns:
                    DeclareHoldRelative(ctx, VDDirector.LaserHoverOffset, 0.08f, 0.25f, 30f);
                    if (columnsDone < columns && Timer >= VDDirector.LaserColumnTime(columnsDone, ex)) {
                        ctx.WingPulse = Math.Max(ctx.WingPulse, 0.6f);
                        if (IsServer) {
                            float x = ctx.Target.Center.X - VDDirector.LaserColumnX;
                            for (int i = 0; i < VDDirector.LaserColumnDrones; i++) {
                                Vector2 pos = new Vector2(x, ctx.Target.Center.Y + (i - VDDirector.LaserColumnDrones / 2) * VDDirector.LaserDroneSpacing);
                                Shoot<VDSporeDrone>(ctx, pos, Vector2.Zero, VDDirector.DmgSporeLaser, VDSporeDrone.ModeFireRight, VDDirector.LaserWarnTime, VDDirector.LaserColumnLength);
                            }
                        }
                        columnsDone++;
                    }
                    if (columnsDone >= columns && Timer >= VDDirector.LaserColumnTime(columns, ex) + VDDirector.LaserColumnsToGrid) {
                        SwitchBeat(Beat.Lattice);
                        MarkNetUpdate(ctx);
                    }
                    break;
                default: {
                    DeclareHoldRelative(ctx, VDDirector.LaserHoverOffset, 0.08f, 0.25f, 30f);
                    int pulses = VDDirector.LatticePulses(ctx.Phase);
                    //起手:一片无人机在背景里亮起,本体翼张核心亮(它在指挥这片点阵)
                    if (pulsesFired < pulses && Timer == PulseStart(pulsesFired)) {
                        ctx.WingPulse = 1f;
                        ctx.CoreGlow = 1f;
                        ctx.RimFlash = 0.6f;
                        VDVfx.Sound("VoidAnticipation", 1.3f, ctx.Target.Center, 3, 0.9f);
                        //点阵以玩家当前位置为中心:第一轮人正踩在格点上,必须挪 55px 进格心;第二轮错半格,格心又成了落点
                        Shoot<VDLatticeField>(ctx, ctx.Target.Center, Vector2.Zero, VDDirector.DmgSporeLaser, VDDirector.LatticeWarn, VDDirector.LatticeDepth, pulsesFired % 2);
                        pulsesFired++;
                    }
                    //预警期核心随蓄力爬亮,打下那一帧爆闪 + 反冲
                    for (int k = 0; k < pulsesFired; k++) {
                        int since = Timer - PulseStart(k);
                        if (since >= 0 && since < VDDirector.LatticeWarn) {
                            ctx.CoreGlow = Math.Max(ctx.CoreGlow, 0.3f + 0.7f * since / VDDirector.LatticeWarn);
                            ctx.RimCharge = Math.Max(ctx.RimCharge, since / (float)VDDirector.LatticeWarn);
                        }
                        else if (since == VDDirector.LatticeWarn) {
                            MuzzleCue(ctx, Vector2.UnitY, 4f, null);
                            ctx.ShakeStrength = Math.Max(ctx.ShakeStrength, 0.5f);
                        }
                    }
                    int lastStrike = PulseStart(pulses - 1) + VDDirector.LatticeWarn + VDDirector.LatticeStrikeFrames;
                    if (pulsesFired >= pulses && Timer >= lastStrike + VDDirector.LatticeTail) {
                        return EndAttack(ctx);
                    }
                    break;
                }
            }
            return null;
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
