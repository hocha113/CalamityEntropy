using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class NihilityMinion : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 68;
            Projectile.height = 68;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.minion = true;
            Projectile.minionSlots = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 26;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public bool stickOnNPC = false;
        public int stickNPCIndex = -1;
        public int direction = 0;
        public Vector2 offset = Vector2.Zero;
        public void spawnParticle(Vector2 center) {/*
            Vector2 vel = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * (float)Math.Cos(Projectile.localAI[0] * 0.3f) * 14;
            Vector2 vel2 = vel * -1;
            vel -= Projectile.velocity * 0.7f;
            vel2 -= Projectile.velocity * 0.7f;
            Dust.NewDust(center, 1, 1, DustID.MagicMirror, vel.X, vel.Y);
            Dust.NewDust(center, 1, 1, DustID.MagicMirror, vel2.X, vel2.Y);*/

        }
        public NPC target = null;

        public override void AI() {
            if (Projectile.localAI[0] == 0) {
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.TwoPi * 0.05f) {
                    var dust = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.PurpleTorch);
                    dust.position = Projectile.Center;
                    dust.velocity = i.ToRotationVector2() * 9;
                    dust.scale = 1.4f;
                    dust.noGravity = true;
                }
            }
            Projectile.localAI[0]++;
            Player player = Main.player[Projectile.owner];
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 2600) {
                Projectile.Center = player.Center;
            }
            if (player.HasBuff(ModContent.BuffType<NihilityBacteriophageBuff>())) {
                Projectile.timeLeft = 3;
            }


            if (target == null || !target.active) {
                target = Projectile.FindMinionTarget();
            }
            if (player.MinionAttackTargetNPC >= 0 && player.MinionAttackTargetNPC.ToNPC().active) {
                target = player.MinionAttackTargetNPC.ToNPC();
            }
            if (Main.rand.NextBool(400)) {
                Projectile.ai[2] = Main.rand.Next(0, 220);
            }
            if (target != null) {
                bool needHealOwner = player.statLife < player.statLifeMax - 50 - Projectile.ai[2];
                if (Main.myPlayer == Projectile.owner && CEKeybinds.CommandMinions.Current)
                    needHealOwner = true;
                if (needHealOwner) {
                    Projectile.ai[1] = 0;
                    if (!stickOnNPC) {
                        Projectile.rotation = (Projectile.Center - target.Center).ToRotation();
                        Projectile.velocity *= 0.9f;
                        Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 6f;
                        if (CEUtils.getDistance(Projectile.Center, target.Center) < Projectile.velocity.Length() * 1.7f) {
                            stickOnNPC = true;
                            stickNPCIndex = target.whoAmI;
                            offset = target.Center - Projectile.Center;
                            offset *= -0.6f;
                            Projectile.velocity *= 0;

                        }
                    }
                    else {
                        if (stickOnNPC) {
                            Projectile.velocity *= 0;
                            Projectile.Center = stickNPCIndex.ToNPC().Center + offset;
                            Projectile.ai[0]++;
                            if (Projectile.ai[0] % 36 == 0) {
                                player.Heal(1);
                            }
                        }
                        if (!stickNPCIndex.ToNPC().active) {
                            stickOnNPC = false;
                        }

                        Projectile.rotation = (Projectile.Center - stickNPCIndex.ToNPC().Center).ToRotation();
                    }
                }
                else {
                    stickOnNPC = false;
                    Projectile.ai[1]--;
                    if (Projectile.ai[1] < -12) {
                        Projectile.ai[1] = 16 + Main.rand.Next(0, 8);
                    }
                    if (Projectile.ai[1]-- > 0) {
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 16f.ToRadians());
                        Projectile.velocity += Projectile.rotation.ToRotationVector2() * 8f;
                        for (int i = 0; i < 4; i++) {
                            spawnParticle(Projectile.Center + Projectile.velocity * ((float)i / 4f));
                        }
                    }
                    else {
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 16f.ToRadians());
                    }
                    Projectile.velocity *= 0.92f;
                }
            }
            else {
                Projectile.pushByOther(0.4f);
                Vector2 standPos = player.Center + new Vector2(0, -140);
                float dist = CEUtils.getDistance(Projectile.Center, standPos);
                if (dist > 100) {
                    Projectile.velocity += (standPos - Projectile.Center).SafeNormalize(Vector2.Zero);
                    if (dist > 160) {
                        Projectile.velocity *= 0.982f;
                    }
                }
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.12f, false);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 180);
            Player player = Projectile.owner.ToPlayer();
            if (player.statLife < player.statLifeMax - 80) {
                if (!stickOnNPC) {
                    stickOnNPC = true;
                    stickNPCIndex = target.whoAmI;
                    offset = target.Center - Projectile.Center;
                    offset *= -0.6f;
                    Projectile.velocity *= 0;
                }
            }
            else {
                if (Projectile.ai[1] > 2) {
                    Projectile.ai[1] = 2;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (stickOnNPC) {
                modifiers.SourceDamage /= 2;
            }
        }
    }
}

