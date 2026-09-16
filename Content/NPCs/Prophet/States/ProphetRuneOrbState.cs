using CalamityEntropy.Content.NPCs.Prophet.Core;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.PRT;
using InnoVault.StateMachines;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Prophet.States
{
    /// <summary>
    /// 6 号 符文光球(原注释「符文光球」),142 帧。
    /// <para>
    /// 节拍:倒计时 140 闪到玩家周围 800;130 在自身周围半径 86 的环上布一圈静止符文;
    /// 二阶段在 120 与 110 再各布一圈(三圈同心,间隔 10 帧)。
    /// 布阵之外本体一直缓慢朝玩家漂移(推 0.2 / 阻尼 0.96)。
    /// </para>
    /// <para>
    /// 环上锚点的角度步进由阶段决定(一阶段 60° 六个,二阶段 40° 九个),
    /// 但<b>三个布阵拍里写的都是同一个 <c>phase == 1 ? 60 : 40</c></b>,
    /// 而后两拍只有二阶段才会执行,所以后两圈恒为九个。原代码如此,照搬
    /// </para>
    /// </summary>
    [VaultState((int)ProphetStateIndex.RuneOrb, typeof(ProphetStateContext))]
    public class ProphetRuneOrbState : ProphetStateBase
    {
        public override ProphetStateIndex StateIndex => ProphetStateIndex.RuneOrb;

        protected override void RunAttack(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            int phase = ctx.Phase;
            int cd = ctx.Countdown;

            if (cd == ProphetDirector.OrbBlinkBeat && IsServer)
            {
                Teleport(ctx, target.Center + CEUtils.randomRot().ToRotationVector2() * ProphetDirector.OrbBlinkRadius);
            }

            if (cd == ProphetDirector.OrbRingBeatA)
            {
                SpawnRing(ctx);
            }
            if (cd == ProphetDirector.OrbRingBeatB && phase > 1)
            {
                SpawnRing(ctx);
            }
            if (cd == ProphetDirector.OrbRingBeatC && phase > 1)
            {
                SpawnRing(ctx);
            }

            npc.rotation = npc.velocity.ToRotation();
            npc.velocity += (target.Center - npc.Center).normalize() * ProphetDirector.OrbThrust;
            npc.velocity *= ProphetDirector.OrbDrag;
        }

        /// <summary>一圈锚点:每个锚点两枚 SparkleCal(Calamity CometShard 原配)加一枚静止符文</summary>
        private static void SpawnRing(ProphetStateContext ctx)
        {
            NPC npc = ctx.Npc;
            CrystalCue(npc);

            int damage = ProjDamage(ctx);
            float step = ctx.Phase == 1 ? ProphetDirector.OrbAngleStepP1 : ProphetDirector.OrbAngleStepP2;
            for (float i = 0; i < 360; i += step)
            {
                float rot = MathHelper.ToRadians(i);
                float impactParticleScale = ProphetDirector.OrbSparkleScale;
                Vector2 anchor = npc.Center + rot.ToRotationVector2() * ProphetDirector.OrbRingRadius;
                PRTLoader.NewParticle<PRT_SparkleCal>(anchor, Vector2.Zero, Color.White, impactParticleScale * 1.2f)
                    .Configure(Color.SkyBlue, 12, 0, 4.5f);
                PRTLoader.NewParticle<PRT_SparkleCal>(anchor, Vector2.Zero, Color.SkyBlue, impactParticleScale)
                    .Configure(Color.SkyBlue, 10, 0, 3f);

                //贴图变体是掷骰,写在权威端守卫之内(原代码同样在 netMode 守卫里掷)
                if (IsServer)
                {
                    Shoot<ProphetRune>(ctx, anchor, Vector2.Zero, damage, 4, npc.whoAmI, rot,
                        Main.rand.Next(ProphetDirector.OrbRuneVariantMin, ProphetDirector.OrbRuneVariantMax));
                }
            }
        }
    }
}
