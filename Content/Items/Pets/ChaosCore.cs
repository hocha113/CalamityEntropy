using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Projectiles.Pets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Pets
{
    public class ChaosCore : ModItem, IDonatorItem
    {
        public string DonatorName => "ShadowWarrior";
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Sorensen>();
            Item.buffType = ModContent.BuffType<ChaosTyrant>();
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
            CreateRecipe().
                AddIngredient(1508, 5).
                AddIngredient(1729, 4).
                AddCalOrOwn(CEID.Item_EssenceofHavoc, ItemID.SoulofNight, 7).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}