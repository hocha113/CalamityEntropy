using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.StateMachines;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.States
{
    /// <summary>
    /// 相位激光:12 架装饰无人机绕本体出现 → 闪现到玩家头顶 → 纵列(每列 9 架,玩家左侧 35 格,P2 起列间隔 45→30 收缩)
    /// → 井字网(9 横 13 纵)。公平阀:每架无人机自带 60 帧预警线;自带闪现,不走 hub 的四角闪
    /// </summary>
    [VaultState((int)VDStateIndex.PhaseLaser, typeof(VDStateContext))]
    public class VDPhaseLaserState : VDStateBase
    {
        public override string StateName => "PhaseLaser";
        public override VDStateIndex StateIndex => VDStateIndex.PhaseLaser;

        private enum Beat { Decor, Columns, Grid }
        private Beat beat;
        private int columnsDone;

        public override void OnEnter(VDStateContext ctx) {
            base.OnEnter(ctx);
            beat = Beat.Decor;
            columnsDone = 0;
        }

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
                    ctx.CoreGlow = System.Math.Max(ctx.CoreGlow, Timer / (float)VDDirector.LaserDecorFrames);
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
                        ctx.WingPulse = System.Math.Max(ctx.WingPulse, 0.6f);
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
                        SwitchBeat(Beat.Grid);
                        MarkNetUpdate(ctx);
                    }
                    break;
                default:
                    DeclareHoldRelative(ctx, VDDirector.LaserHoverOffset, 0.08f, 0.25f, 30f);
                    if (Timer == 1) {
                        ctx.WingPulse = 1f;
                        ctx.CoreGlow = 1f;
                        ctx.RimFlash = 1f;
                        VDVfx.Sound("VoidAnticipation", 1.3f, ctx.Target.Center, 3, 0.9f);
                        if (IsServer) {
                            Vector2 c = ctx.Target.Center;
                            for (int i = 0; i < VDDirector.LaserGridRows; i++) {
                                Shoot<VDSporeDrone>(ctx, new Vector2(c.X - VDDirector.LaserGridOffset, c.Y + (i - VDDirector.LaserGridRows / 2) * VDDirector.LaserDroneSpacing), Vector2.Zero, VDDirector.DmgSporeLaser, VDSporeDrone.ModeFireRight, VDDirector.LaserWarnTime, VDDirector.LaserGridLength);
                            }
                            for (int j = 0; j < VDDirector.LaserGridCols; j++) {
                                Shoot<VDSporeDrone>(ctx, new Vector2(c.X + (j - VDDirector.LaserGridCols / 2) * VDDirector.LaserDroneSpacing, c.Y - VDDirector.LaserGridOffset), Vector2.Zero, VDDirector.DmgSporeLaser, VDSporeDrone.ModeFireDown, VDDirector.LaserWarnTime, VDDirector.LaserGridLength);
                            }
                        }
                    }
                    if (Timer >= VDDirector.LaserGridTail) {
                        return EndAttack(ctx);
                    }
                    break;
            }
            return null;
        }

        private void SwitchBeat(Beat next) {
            beat = next;
            ResetTimer();
        }
    }
}
