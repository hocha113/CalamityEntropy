using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime.States
{
    /// <summary>
    /// 登场:苍白史莱姆从高空坠下,落地压扁弹起后开打
    /// 全程不可受伤,没有接触伤害;超时还没落地也照常开打
    /// </summary>
    [VaultState((int)VSlimeStateIndex.Entrance, typeof(VSlimeStateContext))]
    public class VSlimeEntranceState : VSlimeStateBase
    {
        public override VSlimeStateIndex StateIndex => VSlimeStateIndex.Entrance;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        /// <summary>落地那一帧的 Timer,-1 为还在空中,各端按自己的碰撞结果记</summary>
        private int landedAt = -1;

        public override void OnEnter(VSlimeStateContext ctx) {
            base.OnEnter(ctx);
            landedAt = -1;
        }

        protected override IVaultState<VSlimeStateContext> Run(VSlimeStateContext ctx) {
            NPC npc = ctx.Npc;
            ctx.Invulnerable = true;
            ctx.NoContact = true;
            Ground(ctx);
            npc.velocity.X *= 0.9f;

            //出生帧速度也是 0,前几帧不判落地
            bool grounded = Timer > 6 && npc.velocity.Y == 0f;
            if (landedAt < 0 && (grounded || Timer >= VSlimeDirector.EntranceMaxFrames)) {
                landedAt = Timer;
                ctx.ShakeStrength = 0.6f;
                VSlimeVfx.PaleShards(npc.Bottom, 24, 2f, 9f);
                VSlimeVfx.Shake(npc.Center, 9f);
                VSlimeVfx.Sound(SoundID.Item167 with { Pitch = -0.3f }, npc.Center);
                VSlimeVfx.Cue("ksLand", npc.Center, 0.8f, 0.9f, 2);
            }

            if (landedAt < 0) {
                ctx.Squash = -0.22f;
                if (!Main.dedServ && Timer % 2 == 0) {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(70f, 40f), DustID.Silver, new Vector2(0f, -3f), 80, default, Main.rand.NextFloat(1f, 1.5f));
                    d.noGravity = true;
                }
                if (At(1)) {
                    VSlimeVfx.Sound(SoundID.Item9 with { Pitch = -0.6f, Volume = 0.8f }, npc.Center);
                }
                return null;
            }

            //落地后先压扁再弹回,弹过头一次
            float t = VSlimeDirector.Progress(Timer, landedAt, 26);
            ctx.Squash = 0.5f * (1f - t) * System.MathF.Cos(t * MathHelper.Pi * 1.5f);
            return Timer >= landedAt + VSlimeDirector.EntranceSettle ? new VSlimePaleHubState() : null;
        }
    }
}
