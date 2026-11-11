using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.Projectiles.SandseaDragon;
using InnoVault.StateMachines;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SandseaDragon.States
{
    /// <summary>
    /// 鲨卷风(三阶段,打到战斗结束):本体在竞技场中线衔尾盘旋并变半透明,2.5 秒内以自身为中心卷起巨型龙卷风,圆心始终跟到玩家的高度
    /// 进入 3 秒后沙鲨从玩家屏幕上方慢慢斜落,方向背离龙卷风;两侧边界旋风每秒合拢 2 格
    /// 公平阀:龙卷风的判定随成形进度展开,成形要 150 帧;沙鲨每秒下落 10 格且不追踪,相邻两只生成点横向至少隔 150 像素
    /// 盘旋角由 Timer 算出,LockedAngle 存起始角,LockedPoint 存圆心
    /// </summary>
    [VaultState((int)SandseaStateIndex.Sharknado, typeof(SandseaStateContext))]
    public class SandseaSharknadoState : SandseaStateBase
    {
        public override SandseaStateIndex StateIndex => SandseaStateIndex.Sharknado;
        public override int TimeoutFrames => int.MaxValue;

        /// <summary>沙鲨雨的生成累加器与上一只的横坐标,只权威端用</summary>
        private float rainAccumulator;
        private float lastRainX = float.MinValue;

        public override void OnEnter(SandseaStateContext ctx) {
            base.OnEnter(ctx);
            rainAccumulator = 0f;
            lastRainX = float.MinValue;
            NPC npc = ctx.Npc;
            //客户端的换态包里已经带着这两个事实,不在本地重算
            if (IsServer) {
                //圆轨迹与当前航向相切地接上,不折角
                float start = Heading(ctx) - MathHelper.PiOver2;
                ctx.LockedAngle = start;
                ctx.LockedPoint = npc.Center - start.ToRotationVector2() * SandseaDirector.SpinRadius;
                Shoot<SandseaMaelstrom>(ctx, new Vector2(ctx.ArenaCenterX, ctx.ArenaFloorY), Vector2.Zero, SandseaDirector.DmgWall, npc.whoAmI);
                MarkNetUpdate(ctx);
            }
            SandseaVfx.Roar(npc.Center, true);
        }

        protected override IVaultState<SandseaStateContext> Run(SandseaStateContext ctx) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            ctx.AlphaDeclared = MathHelper.Lerp(1f, SandseaDirector.SpinAlpha, SandseaDirector.Progress(Timer, 0, 60));
            ctx.Effort = 0.25f;
            ctx.GlowBoost = 1f;

            //圆心横向收向场地中线,纵向跟玩家的高度
            float floor = ctx.ArenaFloorY;
            Vector2 want = new Vector2(ctx.ArenaCenterX,
                MathHelper.Clamp(target.Center.Y, floor - SandseaDirector.MaelstromHeight + 180f, floor - 190f));
            Vector2 step = want - ctx.LockedPoint;
            if (step.Length() > SandseaDirector.SpinFollowSpeed) {
                step = step.SafeNormalize(Vector2.Zero) * SandseaDirector.SpinFollowSpeed;
            }
            ctx.LockedPoint += step;

            float angle = ctx.LockedAngle + Timer * (SandseaDirector.SpinSpeed / SandseaDirector.SpinRadius);
            Vector2 desired = ctx.LockedPoint + angle.ToRotationVector2() * SandseaDirector.SpinRadius;
            Vector2 velocity = desired - npc.Center;
            if (velocity.Length() > 40f) {
                velocity = velocity.SafeNormalize(Vector2.Zero) * 40f;
            }
            npc.velocity = velocity;
            ctx.Mode = SandseaMoveMode.Direct;
            ctx.FrameVelocity = step;

            ctx.ArenaHalfWidth = Math.Max(SandseaDirector.ArenaMinHalfWidth, ctx.ArenaHalfWidth - SandseaDirector.ArenaCloseSpeed);

            //原版天气可能中途把沙尘暴收掉,每 5 秒查一次,没了就再开
            if (IsServer && Timer % SandseaDirector.WeatherCheckFrames == 0 && !Sandstorm.Happening) {
                SandseaWeather.Start();
            }

            if (IsServer && Timer >= SandseaDirector.RainStartAt) {
                float rate = MathHelper.Lerp(SandseaDirector.RainRateMin, SandseaDirector.RainRateMax,
                    SandseaDirector.Progress(Timer, SandseaDirector.RainStartAt, SandseaDirector.RainRampFrames));
                rainAccumulator += rate / 60f;
                while (rainAccumulator >= 1f) {
                    rainAccumulator -= 1f;
                    SpawnRain(ctx);
                }
            }
            return null;
        }

        /// <summary>每个在场且彼此相距较远的玩家头顶各落一只,聚在一起的玩家共用一片雨</summary>
        private void SpawnRain(SandseaStateContext ctx) {
            List<Player> anchors = new List<Player> { ctx.Target };
            foreach (Player player in Main.ActivePlayers) {
                if (player.dead || player.whoAmI == ctx.Target.whoAmI || Math.Abs(player.Center.X - ctx.ArenaCenterX) > SandseaDirector.ArenaHalfWidth + 400f) {
                    continue;
                }
                bool apart = true;
                foreach (Player anchor in anchors) {
                    if (Vector2.Distance(anchor.Center, player.Center) < 1200f) {
                        apart = false;
                        break;
                    }
                }
                if (apart) {
                    anchors.Add(player);
                }
            }

            foreach (Player anchor in anchors) {
                //斜落方向背离龙卷风,生成点朝龙卷风一侧偏,落到玩家高度时正好覆盖玩家周围
                float away = anchor.Center.X >= ctx.ArenaCenterX ? 1f : -1f;
                Vector2 velocity = new Vector2(away * MathF.Sin(SandseaDirector.RainSlant), MathF.Cos(SandseaDirector.RainSlant)) * SandseaDirector.RainSpeed;
                float drift = SandseaDirector.RainSpawnHeight * MathF.Tan(SandseaDirector.RainSlant);
                float x = anchor.Center.X - away * drift;
                for (int attempt = 0; attempt < 6; attempt++) {
                    x = anchor.Center.X - away * drift + Main.rand.NextFloat(-SandseaDirector.RainHalfSpan, SandseaDirector.RainHalfSpan);
                    if (Math.Abs(x - lastRainX) >= SandseaDirector.RainMinGap) {
                        break;
                    }
                }
                lastRainX = x;
                Vector2 pos = new Vector2(x, anchor.Center.Y - SandseaDirector.RainSpawnHeight);

                if (Main.zenithWorld && Main.rand.NextFloat() < SandseaDirector.RainCactusChance) {
                    Projectile.NewProjectile(ctx.Npc.GetSource_FromAI(), pos, velocity, ProjectileID.RollingCactus,
                        SandseaDirector.ProjDamage(SandseaDirector.DmgShark), 0f, Main.myPlayer);
                }
                else {
                    Shoot<SandseaRainShark>(ctx, pos, velocity, SandseaDirector.DmgShark, Main.rand.Next(4));
                }
            }
        }
    }
}
