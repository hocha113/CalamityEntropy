using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo
{
    public class AzafureMissile : ModItem
    {
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(copper: 5);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.ammo = BaseMissileProj.AmmoType;
            Item.damage = 11;
            Item.shoot = ModContent.ProjectileType<AzafureMissileProj>();
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
        }

        public override void AddRecipes() {
            CreateRecipe(100)
                .AddIngredient(ModContent.ItemType<HellIndustrialComponents>())
                .AddIngredient(ModContent.ItemType<OsseousRemains>())
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class AzafureMissileProj : BaseMissileProj
    {
        public override float StickDamageAddition => 0.02f;
        public override float StickDamageMult => 0.16f;
        public override void SetupStats() {
            Projectile.ai[1] += 50;
            Projectile.ai[0]--;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Donator/RocketLauncher/Ammo/AzafureMissile";
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff<MechanicalTrauma>(5 * 60);
        }
        public override void ExplodeVisual() {
            CEUtils.PlaySound("metalhit", 0.4f, Projectile.Center, volume: 0.24f);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            float scale = ExplodeRadius / 40f;
            //PRT_ShineParticle FollowOwner等字段spawn后赋,Configure只管Additive
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Red * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
        }
        public override void SpawnParticle(Vector2 vel) {
            for (int i = 0; i < 8; i++) {
                var smoke = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + vel * (i / 8f), CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.025f, 0.04f));
                smoke.timeleftmax = 19;
                smoke.Lifetime = 19;
                smoke.scaleEnd = 0;
                smoke.Configure(0.8f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 19);
                var smoke2 = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + vel * (i / 8f), CEUtils.randomPointInCircle(0.5f), new Color(255, 160, 160), Main.rand.NextFloat(0.01f, 0.015f));
                smoke2.timeleftmax = 19;
                smoke2.Lifetime = 19;
                smoke2.scaleEnd = 0;
                smoke2.Configure(0.8f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 19);
            }
        }
    }
}
