using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo
{
    public class WulfrumMissile : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(copper: 1);
            Item.rare = ItemRarityID.Orange;
            Item.ammo = BaseMissileProj.AmmoType;
            Item.damage = 6;
            Item.shoot = ModContent.ProjectileType<WulfrumMissileProj>();
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.shootSpeed = 4;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_WulfrumMetalScrap))
            {
                CreateRecipe(100)
                .AddIngredient(ModContent.ItemType<OsseousRemains>())
                .AddIngredient(CEID.Item_WulfrumMetalScrap, 1)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            // 灾厄原料按 material-map.md 替换：WulfrumMetalScrap→铁锭（另开铅锭平行配方）
            CreateRecipe(100)
                .AddIngredient(ModContent.ItemType<OsseousRemains>())
                .AddIngredient(ItemID.IronBar, 1)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe(100)
                .AddIngredient(ModContent.ItemType<OsseousRemains>())
                .AddIngredient(ItemID.LeadBar, 1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class WulfrumMissileProj : BaseMissileProj
    {
        public override float StickDamageAddition => 0.01f;
        public override string Texture => "CalamityEntropy/Content/Items/Donator/RocketLauncher/Ammo/WulfrumMissile";
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff(BuffID.OnFire3, 3 * 60);
        }
        public override float Gravity => 0.7f;
        public override void StickUpdate(NPC target)
        {
            target.AddBuff(BuffID.OnFire3, 3 * 60);
        }
        public override void SpawnParticle(Vector2 vel)
        {
            for (int i = 0; i < 4; i++)
            {
                var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Smoke);
                d.noGravity = true;
                d.position += vel * (i / 4f) + CEUtils.randomPointInCircle(6);
                d.velocity = vel * 0.2f;
                d.scale = 1f;
            }
        }
    }
}
