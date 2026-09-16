using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo
{
    public class HallowedMissile : ModItem
    {
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(copper: 14);
            Item.rare = ItemRarityID.Orange;
            Item.ammo = BaseMissileProj.AmmoType;
            Item.damage = 17;
            Item.shoot = ModContent.ProjectileType<HallowedMissileProj>();
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
        }

        public override void AddRecipes() {
            CreateRecipe(250)
                .AddIngredient(ItemID.HallowedBar)
                .AddIngredient(ModContent.ItemType<CharredMissile>(), 250)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    public class HallowedMissileProj : BaseMissileProj
    {
        public override float StickDamageAddition => 0.03f;
        public override void SetupStats() {
            Projectile.ai[1] += 100;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Donator/RocketLauncher/Ammo/HallowedMissile";
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            base.ModifyHitNPC(target, ref modifiers);
            modifiers.ArmorPenetration += 20;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff(BuffID.OnFire3, 10 * 60);
        }
        public override void ExplodeVisual() {
            CEUtils.PlaySound("metalhit", 0.4f, Projectile.Center, volume: 0.16f);
            CEUtils.PlaySound("explosionbig", 1.1f, Projectile.Center, volume: 0.98f);

            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            float scale = ExplodeRadius / 40f;
            //CustomPulse贴图路径现传,CalamityPorts走PRTPathTextures
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Gold * 1.2f, 0.005f).Configure("CalamityEntropy/Assets/Particles/DetailedExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.2f, 22);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Gold * 1.2f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.047f, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Gold, 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Gold * 0.8f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
            Vector2 BurstFXDirection = new Vector2(0, 6 * 0.16f).RotatedBy(MathHelper.PiOver4);
            for (int i = 0; i < 16; i++) {
                var randomColor = Main.rand.Next(4) switch {
                    0 => Color.Red,
                    1 => Color.MediumTurquoise,
                    2 => Color.Orange,
                    _ => Color.LawnGreen,
                };

                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (BurstFXDirection) * (i + 1f), randomColor, (0.25f - i * 0.02f) * 0.3f).Configure(false, 12, new Vector2(2.7f, 1.3f), true);
            }
            for (int k = 0; k < 25; k++) {
                var randomColor = Main.rand.Next(4) switch {
                    0 => Color.Red,
                    1 => Color.MediumTurquoise,
                    2 => Color.Orange,
                    _ => Color.LawnGreen,
                };

                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Main.rand.NextVector2Circular(30 * 0.16f, 30 * 0.16f), BurstFXDirection * Main.rand.NextFloat(1f, 20.5f), randomColor, Main.rand.NextFloat(0.01f, 0.02f)).Configure(false, Main.rand.Next(40, 50 + 1), new Vector2(0.3f, 1.6f));
            }
            for (int i = 0; i < 16; i++) {
                var randomColor = Main.rand.Next(4) switch {
                    0 => Color.Red,
                    1 => Color.MediumTurquoise,
                    2 => Color.Orange,
                    _ => Color.LawnGreen,
                };

                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (-BurstFXDirection) * (i + 1f), randomColor, (0.25f - i * 0.02f) * 0.3f).Configure(false, 12, new Vector2(2.7f, 1.3f), true);
            }
            for (int k = 0; k < 25; k++) {
                var randomColor = Main.rand.Next(4) switch {
                    0 => Color.Red,
                    1 => Color.MediumTurquoise,
                    2 => Color.Orange,
                    _ => Color.LawnGreen,
                };

                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Main.rand.NextVector2Circular(12, 12), -BurstFXDirection * Main.rand.NextFloat(1f, 20.5f), randomColor, Main.rand.NextFloat(0.01f, 0.02f)).Configure(false, Main.rand.Next(40, 50 + 1), new Vector2(0.3f, 1.6f));
            }
            Vector2 BurstFXDirection2 = new Vector2(6 * 0.16f, 0).RotatedBy(MathHelper.PiOver4);
            for (int i = 0; i < 16; i++) {
                var randomColor = Main.rand.Next(4) switch {
                    0 => Color.Red,
                    1 => Color.MediumTurquoise,
                    2 => Color.Orange,
                    _ => Color.LawnGreen,
                };

                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (BurstFXDirection2) * (i + 1f), randomColor, (0.25f - i * 0.02f) * 0.3f).Configure(false, 12, new Vector2(2.7f, 1.3f), true);
            }
            for (int k = 0; k < 25; k++) {
                var randomColor = Main.rand.Next(4) switch {
                    0 => Color.Red,
                    1 => Color.MediumTurquoise,
                    2 => Color.Orange,
                    _ => Color.LawnGreen,
                };

                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Main.rand.NextVector2Circular(12, 12), BurstFXDirection2 * Main.rand.NextFloat(1f, 20.5f), randomColor, Main.rand.NextFloat(0.01f, 0.02f)).Configure(false, Main.rand.Next(40, 50 + 1), new Vector2(0.3f, 1.6f));
            }
            for (int i = 0; i < 16; i++) {
                var randomColor = Main.rand.Next(4) switch {
                    0 => Color.Red,
                    1 => Color.MediumTurquoise,
                    2 => Color.Orange,
                    _ => Color.LawnGreen,
                };

                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (-BurstFXDirection2) * (i + 1f), randomColor, (0.25f - i * 0.02f) * 0.3f).Configure(false, 12, new Vector2(2.7f, 1.3f), true);
            }
            for (int k = 0; k < 25; k++) {
                var randomColor = Main.rand.Next(4) switch {
                    0 => Color.Red,
                    1 => Color.MediumTurquoise,
                    2 => Color.Orange,
                    _ => Color.LawnGreen,
                };

                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Main.rand.NextVector2Circular(12, 12), -BurstFXDirection2 * Main.rand.NextFloat(1f, 20.5f), randomColor, Main.rand.NextFloat(0.01f, 0.02f)).Configure(false, Main.rand.Next(40, 50 + 1), new Vector2(0.3f, 1.6f));
            }
        }
        public override void SpawnParticle(Vector2 vel) {
            PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center - vel * 0.5f, vel * 0.1f, Color.Gold, 0.024f).Configure(false, 12, new Vector2(0.8f, 0.9f), false, false);
            PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel * 0.1f, Color.Gold, 0.028f).Configure(false, 12, new Vector2(1f, 0.7f), false, false);
            CEUtils.AddLight(Projectile.Center, Color.Gold);
        }
    }
}
