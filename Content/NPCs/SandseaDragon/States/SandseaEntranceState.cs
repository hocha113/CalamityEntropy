using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 登场:沙面震动、两侧边界旋风升起,随后从玩家一侧 520 像素处破沙,跃过玩家头顶,弧顶战吼,落回另一侧的沙里
    /// 全程不可受伤也没有接触伤害;弧顶在玩家正上方、高出沙面约 27 格,横向速度约 10,身体在弧顶拱成一道圆弧
    /// </summary>
    [VaultState((int)SandseaStateIndex.Entrance, typeof(SandseaStateContext))]
    public class SandseaEntranceState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.Entrance;
        public override int TimeoutFrames => int.MaxValue;
        public override bool RequiresTarget => false;

        /// <summary>弧顶战吼只放一次,纯本地</summary>
        private bool roared;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            roared = false;
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            int leapAt = SandseaDirector.EntranceLeapAt;
            ctx.Invulnerable = true;
            ctx.ContactActive = false;
            float breachX = ctx.Target.Center.X + ctx.SideDir * SandseaDirector.EntranceBreachDist;

            if (At(1) && IsServer) {
                for (int side = -1; side <= 1; side += 2) {
                    Shoot<SandseaArenaWall>(ctx, new Vector2(ctx.ArenaCenterX + side * ctx.ArenaHalfWidth, ctx.ArenaFloorY), Vector2.Zero,
                        SandseaDirector.DmgWall, npc.whoAmI, side);
                }
            }

            if (Timer < leapAt) {
                //沙下慢慢浮到破沙点正下方停住,沙面越喷越急
                Swim(ctx, new Vector2(breachX, SurfaceAt(ctx, breachX) + 170f), 6f);
                DeclareOmen(ctx, breachX, SandseaDirector.Progress(Timer, 6, leapAt - 6));
                if (At(24)) {
                    SandseaVfx.Roar(npc.Center, false);
                }
                return null;
            }

            if (At(leapAt)) {
                //弧顶对准玩家的横坐标:上升段走完正好到玩家头顶,下落段落向另一侧
                float depth = Math.Max(0f, npc.Center.Y - SurfaceAt(ctx, npc.Center.X));
                float vy = MathF.Sqrt(2f * SandseaDirector.Gravity * (SandseaDirector.EntranceApex + depth));
                float vx = (ctx.Target.Center.X - npc.Center.X) / (vy / SandseaDirector.Gravity);
                npc.velocity = new Vector2(MathHelper.Clamp(vx, -SandseaDirector.EntranceMaxHorizontalSpeed, SandseaDirector.EntranceMaxHorizontalSpeed), -vy);
                SandseaVfx.BreachSound(npc.Center, 1f);
                MarkNetUpdate(ctx);
            }

            Ballistic(ctx);
            ctx.Effort = 0.5f;
            ctx.Curl = 0.35f * SandseaDirector.Progress(Timer, leapAt + 10, 30);
            if (!roared && npc.velocity.Y > -4f && Timer > leapAt + 8) {
                roared = true;
                ctx.ShakeStrength = 0.8f;
                SandseaVfx.Roar(npc.Center, true);
                SandseaVfx.Shake(npc.Center, 8f);
            }
            ctx.JawOpen = roared && npc.velocity.Y < 9f ? 1f : 0.2f;

            bool landed = Timer > leapAt + 40 && npc.velocity.Y > 0f && InSand(ctx);
            if (IsServer && (landed || Timer >= SandseaDirector.EntranceDuration + 60)) {
                return new SandseaHubState();
            }
            return null;
        }
    }
}
