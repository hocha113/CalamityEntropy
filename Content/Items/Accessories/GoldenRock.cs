using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class GoldenRock : ModItem
    {
        public static float PriceMult = 10;
        public int price = 2000;
        public override void SaveData(TagCompound tag) {
            tag.Add("Price", price);
        }
        public override void LoadData(TagCompound tag) {
            if (tag.TryGet<int>("Price", out int p))
                price = p;
        }
        public override void NetSend(BinaryWriter writer) {
            writer.Write(price);
        }
        public override void NetReceive(BinaryReader reader) {
            price = reader.ReadInt32();
        }
        public override void SetDefaults() {
            Item.width = 46;
            Item.height = 46;
            Item.accessory = true;
            Item.value = price * 5;
            Item.rare = ItemRarityID.Blue;
        }
        public override void UpdateInventory(Player player) {
            Item.value = (int)(price * PriceMult * 5);
        }
        public override void UpdateAccessory(Player player, bool hideVisual) {
            Item.value = (int)(price * PriceMult * 5);
            player.GetDamage(DamageClass.Generic) += GetDamageBonus();
            player.Entropy().goldenRock = Item;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            Item.value = (int)(price * PriceMult * 5);
            tooltips.Replace("[A]", Main.ValueToCoins((int)(price * PriceMult)));
            tooltips.Replace("[B]", GetDamageBonus().ToPercent());
        }
        public float GetDamageBonus() {
            if (price <= 2000)
                return 0;
            return float.Min(0.1f, (float)Math.Pow((price - 2000) * 0.000015f, 0.25f) * 0.1f);
        }

        public override void AddRecipes() {
            CreateRecipe().
                AddIngredient(ItemID.GoldOre, 8).
                AddIngredient(ItemID.StoneBlock, 15).
                AddTile(TileID.Furnaces).
                Register();
            CreateRecipe().
                AddIngredient(ItemID.PlatinumOre, 8).
                AddIngredient(ItemID.StoneBlock, 15).
                AddTile(TileID.Furnaces).
                Register();
        }
    }
}
