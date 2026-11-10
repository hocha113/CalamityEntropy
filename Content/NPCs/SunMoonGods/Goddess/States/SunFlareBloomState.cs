using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess;
using InnoVault.StateMachines;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.States
{
    /// <summary>
    /// 日珥绽放,对应原版永恒彩虹:一圈花瓣弹从日轮展开,沿固定圆弧飞行并拖出会灼伤的火带
    /// 花瓣生成后先画弧线预告再起飞;基准角按玩家距离补偏弧度,花瓣扫到玩家身位时玩家正在两瓣之间
    /// 二阶段起追加反卷的第二圈
    /// </summary>
    [VaultState((int)SunStateIndex.FlareBloom, typeof(SunStateContext))]
    public class SunFlareBloomState : SunStateBase
    {
        public override SunStateIndex StateIndex => SunStateIndex.FlareBloom;

        public override Vector2 AnchorFor(SunStateContext ctx) => ctx.Target.Center + SunDirector.BloomAnchor;

        protected override IVaultState<SunStateContext> Run(SunStateContext ctx) {
            bool second = ctx.Phase >= 2;
            int castEnd = (second ? SunDirector.BloomSecondBeat : SunDirector.BloomSpawnBeat) + SunDirector.BloomPetalDelay;

            //花瓣圈心是生成那一帧的日轮,预告期间本体停住,圈心和日轮不脱开
            bool rooted = Rooted(Timer, SunDirector.BloomSpawnBeat) || (second && Rooted(Timer, SunDirector.BloomSecondBeat));
            if (rooted) {
                Direct(ctx);
                ctx.Npc.velocity *= 0.8f;
            }
            else {
                Hover(ctx, AnchorFor(ctx), 8f, 0.06f, 120f);
            }
            float cast = Timer < castEnd ? SunDirector.Progress(Timer, 0, 20) : 0f;
            int arm = Timer < castEnd ? (Timer < 6 ? 2 : 4) : (Timer < castEnd + 6 ? 2 : 0);
            ctx.ArmLeft = arm;
            ctx.ArmRight = arm;
            ctx.CastLeft = cast;
            ctx.CastRight = cast;
            ctx.HaloGlow = Math.Max(ctx.HaloGlow, 0.5f + 0.5f * cast);
            ctx.WingPose = -0.6f * cast;

            if (At(SunDirector.BloomSpawnBeat)) {
                Bloom(ctx, ctx.SideDir);
            }
            if (second && At(SunDirector.BloomSecondBeat)) {
                Bloom(ctx, -ctx.SideDir);
            }
            //花瓣起飞拍
            if (At(SunDirector.BloomSpawnBeat + SunDirector.BloomPetalDelay) || (second && At(SunDirector.BloomSecondBeat + SunDirector.BloomPetalDelay))) {
                ctx.HaloPulse = 1f;
                SunVfx.Burst(ctx.Npc.Center + SunGoddess.HaloOffset, 1.6f);
                SunVfx.Shake(ctx.Npc.Center, 3f, 2000f);
                SunVfx.Sound(SoundID.DD2_BetsyFireballShot with { Volume = 0.8f, Pitch = 0.1f }, ctx.Npc.Center);
            }
            return Timer >= SunDirector.BloomDuration(ctx.Phase) ? EndAttack(ctx) : null;
        }

        private static bool Rooted(int timer, int spawnBeat)
            => timer >= spawnBeat - 4 && timer < spawnBeat + SunDirector.BloomPetalDelay;

        private static void Bloom(SunStateContext ctx, int curl) {
            NPC npc = ctx.Npc;
            Vector2 origin = npc.Center + SunGoddess.HaloOffset;
            SunVfx.Sound(SoundID.Item163, npc.Center);
            if (!IsServer) {
                return;
            }
            int petals = SunDirector.BloomPetals(ctx.Phase);
            Vector2 toTarget = ctx.Target.Center - origin;
            //花瓣飞到玩家距离时,弦方向比出射角多转了半个圆心角
            float reach = MathHelper.Clamp(toTarget.Length(), 100f, 900f);
            float bend = MathF.Asin(reach / (2f * SunDirector.BloomPetalRadius));
            float baseAngle = toTarget.ToRotation() - curl * bend + MathHelper.Pi / petals;
            for (int i = 0; i < petals; i++) {
                float angle = baseAngle + MathHelper.TwoPi * i / petals;
                Shoot<SunFlarePetal>(ctx, origin, Vector2.Zero, SunDirector.DmgFlare, angle, curl, SunDirector.BloomPetalDelay);
            }
        }
    }
}