using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Sage;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 星流扇,对应先知的符文洪流:每轮先锁向并亮出几道航线,再沿航线连发几波左右摆动的星魂,相邻两波反相,织成麻花
    /// 偶数轮 5 道正中对准玩家,奇数轮 4 道把玩家夹在正中空隙;锁向后本体停住,航线不再动
    /// </summary>
    [VaultState((int)SageStateIndex.StarTorrent, typeof(SageStateContext))]
    public class SageStarTorrentState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.StarTorrent;

        public override Vector2 AnchorFor(SageStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SageDirector.TorrentAnchor.X, SageDirector.TorrentAnchor.Y);

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            NPC npc = ctx.Npc;
            int bursts = SageDirector.TorrentBursts(ctx.Phase);
            int burst = Timer / SageDirector.TorrentBurstCycle;
            if (burst >= bursts) {
                Hover(ctx, AnchorFor(ctx), SageDirector.HoverSpeed * 0.5f, 0.06f, 140f);
                return Timer >= bursts * SageDirector.TorrentBurstCycle + SageDirector.TorrentTail ? EndAttack(ctx) : null;
            }

            int cycleStart = burst * SageDirector.TorrentBurstCycle;
            int local = Timer - cycleStart;
            int lanes = SageDirector.TorrentLanes(burst);
            int fireEnd = SageDirector.TorrentTelegraph + SageDirector.TorrentVolleys * SageDirector.TorrentVolleyInterval;

            //先刹停再锁向,锁定之后航线的起点和角度都不再变
            bool locked = local >= SageDirector.TorrentLockBeat;
            if (At(cycleStart + SageDirector.TorrentLockBeat)) {
                if (IsServer) {
                    ctx.LockedAngle = (ctx.Target.Center - npc.Center).ToRotation();
                    MarkNetUpdate(ctx);
                }
                SageVfx.Cue("bell", npc.Center, 1.1f, 0.7f);
            }

            if (local < fireEnd) {
                Direct(ctx);
                npc.velocity *= 0.8f;
                float charge = SageDirector.Progress(local, 0, SageDirector.TorrentTelegraph);
                if (locked) {
                    FaceTo(ctx, ctx.LockedAngle);
                    ctx.FanLanes = lanes;
                    ctx.FanStrength = SageDirector.Progress(local, SageDirector.TorrentLockBeat, 8)
                        * (local < SageDirector.TorrentTelegraph ? 0.5f + 0.5f * charge : 0.6f);
                }
                ctx.CastGlow = charge;
                ctx.WingSpread = 0.9f * charge;
                ctx.CrestGlow = Math.Max(ctx.CrestGlow, 0.5f + 0.4f * charge);
            }
            else {
                //两轮之间挪回锚点
                Hover(ctx, AnchorFor(ctx), SageDirector.HoverSpeed, 0.08f, 140f);
            }

            int since = local - SageDirector.TorrentTelegraph;
            if (since >= 0 && local < fireEnd && since % SageDirector.TorrentVolleyInterval == 0) {
                FireVolley(ctx, lanes, since / SageDirector.TorrentVolleyInterval);
            }
            return null;
        }

        private static void FireVolley(SageStateContext ctx, int lanes, int volley) {
            Vector2 muzzle = ctx.Npc.Center + ctx.LockedAngle.ToRotationVector2() * SageDirector.MuzzleDistance;
            float sway = SageDirector.TorrentWeaveAmplitude * (volley % 2 == 0 ? 1f : -1f);
            for (int i = 0; i < lanes; i++) {
                float angle = ctx.LockedAngle + (i - (lanes - 1) * 0.5f) * SageDirector.TorrentLaneSpread;
                Shoot<SageStarWisp>(ctx, muzzle, angle.ToRotationVector2() * SageDirector.TorrentSpeed, SageDirector.DmgWisp, sway);
            }
            ctx.CrestPulse = Math.Max(ctx.CrestPulse, 0.5f);
            SageVfx.Stardust(muzzle, 6, 2f, 6f);
            SageVfx.Cue("LunarStar", muzzle, 1f + 0.08f * volley, 0.6f, 4);
        }
    }
}