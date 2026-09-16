using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class HadopelagicEchoIIProj : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public float shootCd = 0;
        public bool altShoot = false;
        public override void AI() {
            Player owner = Projectile.owner.ToPlayer();
            if (owner.HeldItem.type == ModContent.ItemType<HadopelagicEchoII>()) {
                Projectile.timeLeft = 3;
            }
            if (Main.myPlayer == Projectile.owner) {
                Vector2 newVec = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX);
                if (!Projectile.velocity.Equals(newVec)) {
                    Projectile.velocity = newVec;
                    Projectile.netUpdate = true;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            Projectile.Center = owner.MountedCenter + owner.gfxOffY * Vector2.UnitY - new Vector2(22, 0).RotatedBy(Projectile.rotation) + new Vector2(0, -12);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 2 == 0) {
                Projectile.frame++;
                if (Projectile.frame > 4) {
                    Projectile.frame = 0;
                }
            }
            if (Projectile.velocity.X >= 0) {
                owner.direction = 1;
                Projectile.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            }
            else {
                owner.direction = -1;
                Projectile.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            }
            Vector2 topPos = Projectile.Center + new Vector2(52, 0).RotatedBy(Projectile.rotation);
            shootCd -= 1 * owner.GetTotalAttackSpeed(Projectile.DamageType);
            if (active) {
                owner.manaRegenDelay = 16;
                if (shootCd <= 0) {
                    int cMana = int.Max(1, (int)(owner.HeldItem.mana * owner.manaCost));
                    if (owner.CheckMana(cMana, true)) {
                        PlayerLoader.OnConsumeMana(owner, owner.HeldItem, cMana);
                        if (altShoot) {
                            gfxXAdd = 4f;
                            if (Main.myPlayer == Projectile.owner) {
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), topPos, Projectile.velocity * 40, ModContent.ProjectileType<HadopelagicWail>(), (int)owner.GetTotalDamage(Projectile.DamageType).ApplyTo((int)(Projectile.damage * 1.50f)), Projectile.knockBack, Projectile.owner);
                            }
                            shootCd = 50;
                            CEUtils.PlaySound("he2", 1, Projectile.Center);

                        }
                        else {
                            gfxXAdd = 4f;
                            CEUtils.PlaySound("he" + (Main.rand.NextBool() ? 1 : 3).ToString(), 1, Projectile.Center);
                            if (Main.myPlayer == Projectile.owner) {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), topPos, Projectile.velocity * 30, ModContent.ProjectileType<HadopelagicLaser>(), (int)owner.GetTotalDamage(Projectile.DamageType).ApplyTo((int)(Projectile.damage * 1.50f)), Projectile.knockBack, Projectile.owner);
                            }
                            //PRT_DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
                            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(topPos + Projectile.velocity * 3, Vector2.Zero, new Color(170, 170, 255), 0.1f).Configure(new Vector2(2f, 2f), 0, 0.3f, 20);

                            PRTLoader.NewParticle<PRT_DetailedExplosionCal>(topPos + Projectile.velocity * 6, Vector2.Zero, new Color(140, 140, 255) * 0.6f, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 0.16f, 12);
                            if (Projectile.owner == Main.myPlayer) {
                                int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), topPos, Projectile.velocity, ModContent.ProjectileType<Impact>(), 0, 0, Projectile.owner, 0, 1.2f);
                            }
                            float sparkCount = 64;
                            for (int i = 0; i < sparkCount; i++) {
                                Vector2 sparkVelocity2 = new Vector2(Main.rand.NextFloat(4, 20), 0).RotateRandom(1f).RotatedBy(Projectile.velocity.ToRotation());
                                int sparkLifetime2 = Main.rand.Next(26, 35);
                                float sparkScale2 = sparkVelocity2.Length() * 0.16f;
                                Color sparkColor2 = Color.Lerp(Color.SkyBlue, Color.LightSkyBlue, Main.rand.NextFloat(0, 1));
                                //跟AltSpark成对出现时寿命/速度系数是旧代码原值
                                PRTLoader.NewParticle<PRT_LineCal>(topPos + Projectile.velocity * 3, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                            }
                            shootCd = 26;
                            owner.velocity -= Projectile.velocity * 9;
                        }
                        altShoot = !altShoot;
                    }
                }
            }
            gfxOffX += gfxXAdd;
            gfxOffX *= 0.87f;
            gfxXAdd *= 0.87f;
        }
        public float gfxOffX = 0;
        public float gfxXAdd = 0;
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public bool active { get { return Projectile.owner.ToPlayer().channel; } }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D glow = CEUtils.getExtraTex("HadopelagicEchoIIGlow");
            Rectangle frame = new Rectangle(active ? 126 : 0, Projectile.frame * 66, 126, 66);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(-gfxOffX + 40, 0).RotatedBy(Projectile.rotation), frame, lightColor, Projectile.rotation, (Projectile.velocity.X > 0 ? new Vector2(86, 42) : new Vector2(86, 66 - 42)), Projectile.scale, (Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically));
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition + new Vector2(-gfxOffX + 40, 0).RotatedBy(Projectile.rotation), frame, Color.White, Projectile.rotation, (Projectile.velocity.X > 0 ? new Vector2(86, 42) : new Vector2(86, 66 - 42)), Projectile.scale, (Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically));

            return false;
        }
    }

}
