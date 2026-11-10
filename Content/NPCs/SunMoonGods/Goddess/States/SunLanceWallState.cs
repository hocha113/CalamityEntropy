using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 日矛·天罗,对应原版二阶段枪墙:成排平行长枪从一侧扫过,每波换一个方向,上一波射出时下一波正好亮出航线
    /// 每波正中那支穿过玩家生成时的位置,要横移半格才站进空隙
    /// </summary>
    [VaultState((int)SunStateIndex.LanceWall, typeof(SunStateContext))]
    public class SunLanceWallState : SunStateBase
    {
        /// <summary>各波的飞行方向:右、左、右下、左下、正下</summary>
        private static readonly float[] Directions =
        {
            0f, MathHelper.Pi, MathHelper.PiOver4, MathHelper.Pi - MathHelper.PiOver4, MathHelper.PiOver2,
        };

        public override SunStateIndex StateIndex => SunStateIndex.LanceWall;

        public override Vector2 AnchorFor(SunStateContext ctx) => ctx.Target.Center + SunDirector.WallAnchor;

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            int waves = SunDirector.WallWaves(ctx.Phase);
            int lastWave = SunDirector.WallWindup + (waves - 1) * SunDirector.WallWaveInterval;

            Hover(ctx, AnchorFor(ctx), 12f, 0.08f, 140f);
            ctx.HaloGlow = Math.Max(ctx.HaloGlow, 0.45f + 0.4f * SunDirector.Progress(Timer, 0, SunDirector.WallWindup));

            int since = Timer - SunDirector.WallWindup;
            int wave = since < 0 ? -1 : Math.Min(since / SunDirector.WallWaveInterval, waves - 1);
            if (wave >= 0 && Timer < lastWave + SunDirector.WallWaveInterval) {
                //指向长枪来的那一侧,正下那波双臂高举
                float dirX = MathF.Cos(Directions[wave % Directions.Length]);
                int local = since - wave * SunDirector.WallWaveInterval;
                int frame = local < 5 ? 2 : 3;
                float cast = SunDirector.Progress(local, 0, 10);
                if (MathF.Abs(dirX) < 0.1f) {
                    ctx.ArmLeft = ctx.ArmRight = local < 5 ? 2 : 4;
                    ctx.CastLeft = ctx.CastRight = cast;
                }
                else if (dirX > 0f) {
                    ctx.ArmLeft = frame;
                    ctx.CastLeft = cast;
                }
                else {
                    ctx.ArmRight = frame;
                    ctx.CastRight = cast;
                }
                ctx.WingPose = -0.4f;
            }

            if (At(0)) {
                SunVfx.Sound(SoundID.Item162 with { Pitch = -0.2f }, ctx.Npc.Center);
            }
            for (int k = 0; k < waves; k++) {
                if (At(SunDirector.WallWindup + k * SunDirector.WallWaveInterval)) {
                    FireWave(ctx, k);
                }
            }
            return Timer >= lastWave + SunDirector.WallTail ? EndAttack(ctx) : null;
        }

        private static void FireWave(SunStateContext ctx, int wave) {
            float angle = Directions[wave % Directions.Length];
            Vector2 dir = angle.ToRotationVector2();
            Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);
            Vector2 origin = ctx.Target.Center - dir * SunDirector.WallDistance;
            float spacing = SunDirector.WallSpacing(ctx.Phase);
            int half = SunDirector.WallLances / 2;
            for (int j = -half; j <= half; j++) {
                Shoot<SunLance>(ctx, origin + normal * (j * spacing), Vector2.Zero, SunDirector.DmgLance, angle, SunDirector.WallWaveInterval);
            }
            ctx.HaloPulse = 0.6f;
            SunVfx.Sound(SoundID.Item162 with { Volume = 0.9f, MaxInstances = 3 }, ctx.Npc.Center);
        }
    }
}