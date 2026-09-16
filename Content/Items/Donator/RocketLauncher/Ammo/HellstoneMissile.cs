using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo
{
    public class HellstoneMissile : ModItem
    {
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 5);
            Item.rare = ItemRarityID.Orange;
            Item.ammo = BaseMissileProj.AmmoType;
            Item.damage = 10;
            Item.shoot = ModContent.ProjectileType<HellstoneMissileProj>();
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.shootSpeed = 4;
        }

        public override void AddRecipes() {
            CreateRecipe(100)
                .AddIngredient(ModContent.ItemType<OsseousRemains>())
                .AddIngredient(ItemID.HellstoneBar, 1)
                .AddIngredient(ItemID.Bomb, 1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class HellstoneMissileProj : BaseMissileProj
    {
        public override float StickDamageAddition => 0.02f;
        public override void SetupStats() {
            Projectile.ai[1] += 60;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Donator/RocketLauncher/Ammo/HellstoneMissile";
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff(BuffID.OnFire3, 3 * 60);
        }
        public override void StickUpdate(NPC target) {
            target.AddBuff(BuffID.OnFire3, 3 * 60);
        }
        public override void ExplodeVisual() {
            for (int i = 0; i < 36; i++) {
                var d = Dust.NewDustDirect(Projectile.Center, Projectile.width, Projectile.height, DustID.Flare);
                d.noGravity = true;
                d.scale = 1.5f;
                d.velocity = CEUtils.randomPointInCircle(16);
            }
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            float scale = ExplodeRadius / 40f;
            //PRT_ShineParticle FollowOwner等字段spawn后赋,Configure只管Additive
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Red * 0.3f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.3f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Firebrick * 1.6f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SmokeExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Firebrick * 1.6f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SmokeExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.045f, 22);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Firebrick * 1.6f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SmokeExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.04f, 20);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Firebrick * 1.6f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SmokeExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Firebrick * 1.6f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SmokeExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.03f, 16);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Firebrick * 1.6f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SmokeExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.025f, 14);

        }
        public override void SpawnParticle(Vector2 vel) {
            for (int i = 0; i < 8; i++) {
                var d = Dust.NewDustDirect(Projectile.position, 0, 0, DustID.Flare);
                d.noGravity = true;
                d.position = Projectile.Center + CEUtils.randomPointInCircle(6) + vel * (i / 8f);
                d.velocity = vel * 0.4f;
                d.scale = 1.15f;
            }
        }
    }
}
