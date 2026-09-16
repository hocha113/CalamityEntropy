using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class WelkingShield : ModProjectile
    {
        public SoundStyle sound = new SoundStyle("CalamityEntropy/Assets/Sounds/flashback");
        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;

        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public bool flag = true;
        public int btime = 16;
        public float rp = 2;

        public override void AI() {
            if (flag) {
                flag = false;
                CEUtils.PlaySound("vshield", 1, Projectile.Center);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            rp *= 0.73f;
            btime--;
            Player plr = Projectile.GetOwner();
            Projectile.Center = plr.Center;
            if (btime < 0) {
                Projectile.Opacity -= 0.1f;
                if (Projectile.Opacity <= 0) {
                    Projectile.Kill();
                }
            }
            else {
                Vector2 p1 = Projectile.Center + new Vector2(50, -70).RotatedBy(Projectile.rotation);
                Vector2 p2 = Projectile.Center + new Vector2(50, 70).RotatedBy(Projectile.rotation);
                foreach (NPC n in Main.ActiveNPCs) {
                    if (!n.friendly && CEUtils.LineThroughRect(p1, p2, n.Hitbox, 56)) {
                        Projectile.GetOwner().Entropy().immune = 20;
                        if (!n.dontTakeDamage) {
                            n.SimpleStrikeNPC(56, Projectile.velocity.X > 0 ? 1 : -1, true, 20, DamageClass.Melee);
                            n.velocity = (n.Center - Projectile.GetOwner().Center).normalize() * (n.velocity.Length() * 2 + n.velocity.Length() > 0.01f ? 12 : 0);
                        }
                        Block();
                        break;
                    }
                }
            }
        }
        public void Block() {
            btime = 0;
            rp = 0;
            Projectile.GetOwner().velocity = Projectile.rotation.ToRotationVector2() * -4;
            Projectile.GetOwner().Entropy().immune = 46;
            CalamityEntropy.Instance.screenShakeAmp = 4;
            Projectile.GetOwner().Entropy().vShieldCD = VetrasylsEye.GetShieldCooldown().ApplyCdDec(Projectile.GetOwner());
            if (!Main.dedServ) {
                SoundEngine.PlaySound(sound, Projectile.Center);
            }

            //Sn粒子WelkingShield专用,旧PRT/EParticle Sn
            PRTLoader.NewParticle<PRT_Sn>(Projectile.Center, Projectile.rotation.ToRotationVector2() * 16, Color.SkyBlue, 0.66f)
                .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation);  //Sn粒子WelkingShield专用,旧EParticle Sn
            var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 66, Vector2.Zero, Color.SkyBlue, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation + MathHelper.PiOver2);
            __prt.xadd = 0.44f;
            __prt.lx = 0.44f;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/WelkinShield";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation + (Projectile.velocity.X > 0 ? -1 : 1) * rp, tex.Size() * 0.5f, Projectile.Opacity, SpriteEffects.None);
            return false;
        }
    }
    public class WelkingShieldGProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool friendly = false;
        public override void OnSpawn(Projectile projectile, IEntitySource source) {
            if (source is EntitySource_Parent ep && ep.Entity is Projectile pj) {
                if (pj.GetGlobalProjectile<WelkingShieldGProj>().friendly) {
                    friendly = true;
                    projectile.friendly = true;
                    projectile.hostile = false;
                    projectile.owner = pj.owner;
                }
            }
        }
        private static void ReflectProjectile(Projectile projectile, Projectile shield, Player owner) {
            projectile.velocity = shield.velocity.normalize() * projectile.velocity.Length();
            projectile.hostile = false;
            projectile.friendly = true;
            projectile.owner = owner.whoAmI;
            projectile.damage = (int)Math.Min(projectile.damage * VetrasylsEye.ReflectDamageRatio, VetrasylsEye.ReflectDamageCap);
            projectile.GetGlobalProjectile<WelkingShieldGProj>().friendly = true;
        }

        public override bool CanHitPlayer(Projectile projectile, Player target) {
            if (projectile.damage > 0 && projectile.hostile && projectile.Colliding(projectile.getRect(), target.getRect()) && target.ownedProjectileCounts[ModContent.ProjectileType<WelkingShield>()] > 0) {
                foreach (Projectile proj in Main.ActiveProjectiles) {
                    if (proj.ModProjectile is WelkingShield ws && ws.btime > 0) {
                        if (CEUtils.GetAngleBetweenVectors(proj.velocity, projectile.Center - proj.Center) < MathHelper.ToRadians(65)) {
                            ReflectProjectile(projectile, proj, target);
                            ws.Block();
                            return false;
                        }
                    }
                }
            }
            return base.CanHitPlayer(projectile, target);
        }

        public override bool PreAI(Projectile projectile) {
            foreach (var target in Main.ActivePlayers) {
                if (projectile.damage > 0 && projectile.hostile && projectile.Colliding(projectile.getRect(), target.getRect().Center.ToVector2().getRectCentered(136, 136)) && target.ownedProjectileCounts[ModContent.ProjectileType<WelkingShield>()] > 0) {
                    foreach (Projectile proj in Main.ActiveProjectiles) {
                        if (proj.ModProjectile is WelkingShield ws && ws.btime > 0) {
                            if (CEUtils.GetAngleBetweenVectors(proj.velocity, projectile.Center - proj.Center) < MathHelper.ToRadians(65)) {
                                ReflectProjectile(projectile, proj, target);
                                ws.Block();
                                break;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }

}