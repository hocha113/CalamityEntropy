using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Sage;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage.States
{
    /// <summary>
    /// 新月斩,对应先知的符文剑:一手甩出几柄月刃,沿叶形轨迹飞出又折回,起飞前整条轨迹先画出来
    /// 正中那柄的去程穿过玩家生成时的位置,相邻月刃卷向相反;二阶段起追加整体反卷的第二手
    /// 月刃的轨迹钉在生成点上,预告期间本体停住
    /// </summary>
    [VaultState((int)SageStateIndex.CrescentSlash, typeof(SageStateContext))]
    public class SageCrescentSlashState : SageStateBase
    {
        public override SageStateIndex StateIndex => SageStateIndex.CrescentSlash;

        public override Vector2 AnchorFor(SageStateContext ctx)
            => ctx.Target.Center + new Vector2(ctx.SideDir * SageDirector.CrescentAnchor.X, SageDirector.CrescentAnchor.Y);

        protected override IVaultState<SageStateContext> Run(SageStateContext ctx) {
            NPC npc = ctx.Npc;
            int volleys = SageDirector.CrescentVolleys(ctx.Phase);
            int since = Timer - SageDirector.CrescentSpawnBeat;
            int volley = since < 0 ? -1 : Math.Min(since / SageDirector.CrescentVolleyInterval, volleys - 1);
            int local = volley < 0 ? -1 : since - volley * SageDirector.CrescentVolleyInterval;
            bool winding = volley >= 0 && local < SageDirector.CrescentTelegraph;

            if (winding || Timer < SageDirector.CrescentSpawnBeat) {
                Direct(ctx);
                npc.velocity *= 0.8f;
            }
            else {
                Hover(ctx, AnchorFor(ctx), SageDirector.HoverSpeed * 0.6f, 0.06f, 140f);
            }

            if (winding) {
                //蓄势时双翼后拉,起飞拍猛地甩开
                float p = local / (float)SageDirector.CrescentTelegraph;
                ctx.WingSpread = -0.7f * p;
                ctx.CastGlow = p;
                ctx.CrestGlow = Math.Max(ctx.CrestGlow, 0.5f + 0.5f * p);
            }
            else if (volley >= 0 && local < SageDirector.CrescentTelegraph + 20) {
                ctx.WingSpread = 1f;
                ctx.CrestGlow = 1f;
            }

            for (int v = 0; v < volleys; v++) {
                int spawnBeat = SageDirector.CrescentSpawnBeat + v * SageDirector.CrescentVolleyInterval;
                if (At(spawnBeat)) {
                    Throw(ctx, v);
                }
                if (At(spawnBeat + SageDirector.CrescentTelegraph)) {
                    ctx.CrestPulse = 1f;
                    SageVfx.Burst(npc.Center, 1.4f);
                    SageVfx.Shake(npc.Center, 3f, 2000f);
                }
            }
            int lastLaunch = SageDirector.CrescentSpawnBeat + (volleys - 1) * SageDirector.CrescentVolleyInterval + SageDirector.CrescentTelegraph;
            return Timer >= lastLaunch + SageDirector.CrescentTail ? EndAttack(ctx) : null;
        }

        private static void Throw(SageStateContext ctx, int volley) {
            NPC npc = ctx.Npc;
            SageVfx.Cue("bell", npc.Center, 1.2f + 0.15f * volley, 0.7f);
            if (!IsServer) {
                return;
            }
            int count = SageDirector.CrescentCount(ctx.Phase, volley);
            int middle = count / 2;
            int handed = ctx.SideDir * (volley % 2 == 0 ? 1 : -1);
            Vector2 toTarget = ctx.Target.Center - npc.Center;
            float distance = toTarget.Length();

            //按玩家距离求轨迹的横向偏移,出射角反向扭回,去程才穿过玩家
            float tau = MathF.Asin(MathHelper.Clamp(distance / SageDirector.CrescentReach, 0f, 1f)) / MathHelper.Pi;
            float lateral = SageDirector.CrescentBulge * MathF.Sin(MathHelper.TwoPi * tau);
            float middleCurl = Curl(middle, handed);
            float through = toTarget.ToRotation() - middleCurl * MathF.Atan2(lateral, Math.Max(distance, 1f));
            for (int i = 0; i < count; i++) {
                float angle = through + (i - middle) * SageDirector.CrescentSpread;
                Shoot<SageCrescent>(ctx, npc.Center, Vector2.Zero, SageDirector.DmgCrescent, angle, Curl(i, handed), SageDirector.CrescentTelegraph);
            }
        }

        private static float Curl(int index, int handed) => (index % 2 == 0 ? 1f : -1f) * handed;
    }
}