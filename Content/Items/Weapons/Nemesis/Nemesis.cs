using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.CalamityRef;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Nemesis
{
    public class Nemesis : ModItem, IDevItem
    {
        public string DevName => "锯角";
        private int fireIndex;
        //普通挥砍的方向,每次左键交替下劈 / 上撩
        private int swingDir = 1;
        public override void SetDefaults() {
            Item.height = 154;
            Item.width = 154;
            Item.damage = 360;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 18;
            Item.scale = 1;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 5.5f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<NemesisHeld>();
            Item.shootSpeed = 18f;
            fireIndex = 0;
            swingDir = 1;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        //手持弹幕存活期间不许再次使用,蓄力时长可变,靠它而不是 useTime 收口
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position
            , Vector2 velocity, int type, int damage, float knockback) {
            int newLevel = 0;
            if (++fireIndex > 6) {
                newLevel = 1;
                fireIndex = 0;
            }
            if (player.altFunctionUse == 2) {
                newLevel = 2;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, newLevel, swingDir);
            if (newLevel == 0) {
                swingDir *= -1;
            }
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe().AddCalOrOwn(CEID.Item_GalactusBlade, ModContent.ItemType<FlowingLight>())
                .AddCalOrOwn(CEID.Item_TheBurningSky, ItemID.StarWrath)
                .AddIngredient<FadingRunestone>()
                .AddTile<VoidWellTile>()
                .Register();
        }
    }
}
