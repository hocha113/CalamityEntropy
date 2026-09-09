using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Content.Projectiles.Pets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Pets
{
    public class ConsecratedRune : ModItem, IDevItem
    {
        public string DevName => "锯角";
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Pooney>();
            Item.buffType = ModContent.BuffType<ConsecratedRefuge>();
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_EssenceofSunlight))
            {
                CreateRecipe().
                AddIngredient(ItemID.SoulofLight, 2).
                AddIngredient(ItemID.HallowedBar, 4).
                AddIngredient(CEID.Item_EssenceofSunlight).
                AddTile(TileID.WorkBenches).
                Register();
                return;
            }
            // 灾厄阳光精华并入光明之魂行
            CreateRecipe().
                AddIngredient(ItemID.SoulofLight, 3).
                AddIngredient(ItemID.HallowedBar, 4).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }
}