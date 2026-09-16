using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class HolyBookmarkHolder : ModItem
    {
        //额外书签槽外观贴图,加载期就位;仅在 !Main.dedServ 分支读取
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/Extra3")]
        internal static Texture2D SlotTex;
        public static float MAGECRIT = 5;
        public static float MAGEDAMAGE = 0.1f;
        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 30;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(gold: 2);
            Item.accessory = true;
            Item.value = Item.buyPrice(0, 10, 0, 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().AdditionalBookmarkSlot += 2;
            player.GetCritChance(DamageClass.Magic) += MAGECRIT;
            player.GetDamage(DamageClass.Magic) += MAGEDAMAGE;
            if (!Main.dedServ)
                for (int i = 0; i < 2; i++)
                    player.Entropy().BookmarkHolderSpecialTextures.Add(SlotTex);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[D]", MAGEDAMAGE.ToPercent().ToString());
            tooltips.Replace("[S]", MAGECRIT.ToString());
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient<MagicBookmarkHolder>()
                .AddIngredient<ExquisiteBookmarkHolder>()
                .AddIngredient(ItemID.HallowedBar, 3)
                .AddIngredient(ItemID.SoulofMight, 3)
                .AddIngredient(ItemID.SoulofFright, 3)
                .AddIngredient(ItemID.SoulofSight, 3)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
