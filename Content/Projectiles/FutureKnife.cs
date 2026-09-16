using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class FutureKnife : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            //盗贼并入近战(rogue-weapons: ProphecyFlyingKnife→近战)
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 80;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;

        }
        public PRT_TrailParticle trail;
        public override void AI() {
            if (!Main.dedServ) {
                if (trail == null) {
                    //TrailParticle不开CanPool,odp轨迹List池化会闪上一条
                    trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Color.AliceBlue * 0.6f, Projectile.scale * 0.6f * (Projectile.IsEmpowered() ? 2 : 1)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    trail.maxLength = 16;
                }
                trail.Lifetime = 16;
                trail.AddPoint(Projectile.Center + Projectile.velocity);
            }
            if (Projectile.IsEmpowered()) {
                Projectile.scale = 2.4f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft < 246 && !Projectile.IsEmpowered()) {
                var target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 800);
                if (target != null) {
                    Projectile.velocity += (target.Center - Projectile.Center).normalize() * 4f;
                    Projectile.velocity *= 0.92f;
                    if (Projectile.timeLeft < 30)
                        Projectile.timeLeft++;
                }
            }
            if (Projectile.timeLeft <= 78 && !Main.dedServ && CEUtils.getDistance(Projectile.Center, Main.LocalPlayer.Center) < 1600) {
                //RuneParticle字段(homing/target)旧初始化器拆成spawn后直赋
                PRTLoader.NewParticle<PRT_RuneParticle>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-0.6f, 0.6f), Color.White, 0.6f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            for (int i = 0; i < 6; i++) {
                PRTLoader.NewParticle<PRT_RuneParticle>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-5f, 5f), Color.White, 0.5f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
            }
            CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3).ToString(), Main.rand.NextFloat(0.7f, 1.3f), target.Center, 10, 0.4f);
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 300);
            if (Projectile.IsEmpowered()) {
                for (float j = 0; j < 360f; j += 60) {
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<ProphecyRune>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner, MathHelper.ToRadians(j), 0, Main.rand.Next(1, 12));
                    if (p.WithinBounds(Main.maxProjectiles)) {
                        Main.projectile[p].SetEmpowered();
                        p.ToProj().netUpdate = true;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }


}