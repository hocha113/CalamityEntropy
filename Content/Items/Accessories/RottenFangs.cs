using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class RottenFangs : ModItem
    {
        public override void SetDefaults() {
            Item.width = 26;
            Item.height = 16;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 5);
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().rottenFangs = true;
        }
    }
    public class RFangGProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public float Delay = 60;
        public override void AI(Projectile projectile) {
            if (projectile.minion) {
                if (projectile.owner == Main.myPlayer && projectile.OwnerEntropy().rottenFangs) {
                    Delay -= 1f / projectile.MaxUpdates;
                    if (Delay <= 0) {
                        Delay = 60;
                        if (Main.rand.NextBool(3)) {
                            NPC target = projectile.FindMinionTarget(1400);
                            if (target != null) {
                                int dmg = ((int)(projectile.GetOwner().GetTotalDamage(DamageClass.Summon).ApplyTo(10))).ApplyAccArmorDamageBonus(projectile.GetOwner());
                                for (int i = 0; i < 3; i++) {
                                    Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, (target.Center - projectile.Center).normalize().RotatedByRandom(0.2f) * Main.rand.NextFloat(5, 8), ModContent.ProjectileType<RottenFangsBloodBullet>(), dmg, 5, projectile.owner);
                                }
                                CEUtils.PlaySound("ksLand", Main.rand.NextFloat(0.6f, 0.8f), projectile.Center, 60, 0.32f);
                            }
                        }
                    }
                }
            }
        }
    }
    public class RottenFangsBloodBullet : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Summon, true, 1);
            Projectile.width = Projectile.height = 16;
            Projectile.MaxUpdates = 4;
            Projectile.light = 0.42f;
            Projectile.timeLeft = 1000;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 8;
        }
        public override void AI() {
            if (!Main.dedServ) {
                if (Projectile.localAI[2]++ == 0) {
                    //ImpactCal,CalamityPorts的Configure(rot,life)签名
                    PRTLoader.NewParticle<PRT_ImpactCal>(Projectile.Center, Vector2.Zero, new Color(255, 80, 80), 0.6f).Configure(0, 12);
                }
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Blood);
                d.velocity = Projectile.velocity * Main.rand.NextFloat(0.3f, 0.5f);
                d.noGravity = true;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override void OnKill(int timeLeft) {
            PRTLoader.NewParticle<PRT_ImpactCal>(Projectile.Center, Vector2.Zero, new Color(255, 120, 120), 0.6f).Configure(0, 12);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.getDrawData(Color.Crimson));
            CEUtils.DrawGlow(Projectile.Center, Color.Crimson * 0.6f, 1);
            return false;
        }
    }
}

