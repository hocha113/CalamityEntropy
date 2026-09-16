using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class ZeratosHeart : SamsaraSword
    {
        public float yOffset = 0;
        public override void AI() {
            setDamage(1.2f);
            Projectile.timeLeft = 5;
            Player player = Projectile.owner.ToPlayer();
            var modPlayer = player.Entropy();
            if (modPlayer.samsaraCasketOpened) {
                yOffset += (140 - yOffset) * 0.1f;
                Vector2 vrec = Projectile.Center;
                Projectile.Center = player.Center;
                int range = getRange(player);


                if (target != null && !target.active) {
                    target = null;
                }
                if (target != null && target.dontTakeDamage) {
                    target = null;
                }
                if (target == null) {
                    target = Projectile.FindTargetWithinRange(range, modPlayer.sCasketLevel > 3);
                }
                if (target != null && CEUtils.getDistance(player.Center, target.Center) > Math.Min(range, 1400)) {
                    target = null;
                }
                Projectile.Center = vrec;
                if (player.MinionAttackTargetNPC >= 0 && player.MinionAttackTargetNPC.ToNPC().active) {
                    target = player.MinionAttackTargetNPC.ToNPC();
                }
                if (target != null) {
                    if (++Projectile.ai[0] % 100 == 0 && Main.myPlayer == Projectile.owner) {
                        for (int i = 0; i < 12; i++) {
                            float angle = MathHelper.ToRadians(i * 30);
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, angle.ToRotationVector2() * 12, ModContent.ProjectileType<FireDragonsRoar>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

                        }
                        SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/YharonFireball1"), Projectile.Center);

                    }
                }
            }
            else {
                yOffset += (-30 - yOffset) * 0.1f;
                if (yOffset < 0) {
                    if (casket.ToProj().ModProjectile is SamsaraCasketProj) {
                        ((SamsaraCasketProj)casket.ToProj().ModProjectile).swords[index] = true;
                    }
                    Projectile.Kill();
                }
            }
            Projectile.Center = player.Center + new Vector2(0, -yOffset);
            oldPos.Add(Projectile.Center);
            oldRot.Add(Projectile.rotation);
            if (oldPos.Count > 5) {
                oldPos.RemoveAt(0);
                oldRot.RemoveAt(0);
            }
            Projectile.rotation = MathHelper.PiOver2;
        }
    }
}
