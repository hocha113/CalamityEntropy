using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ExquisiteBookmarkHolder : ModItem
    {
        //额外书签槽外观贴图,加载期就位;仅在 !Main.dedServ 分支读取
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/Extra2")]
        internal static Texture2D SlotTex;
        public static float MAGECRIT = 5;
        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 30;
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
            Item.value = Item.buyPrice(0, 8, 42, 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().AdditionalBookmarkSlot += 1;
            player.GetCritChance(DamageClass.Magic) += MAGECRIT;
            if (!Main.dedServ)
                player.Entropy().BookmarkHolderSpecialTextures.Add(SlotTex);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[S]", MAGECRIT.ToString());
        }
    }
}
