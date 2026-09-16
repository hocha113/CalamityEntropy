using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkLibra : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.Entropy().stroke = true;
            Item.Entropy().NameColor = Color.LightBlue;
            Item.Entropy().strokeColor = Color.DarkBlue;
            Item.Entropy().tooltipStyle = 4;
            Item.value = Item.buyPrice(gold: 1);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Libra");
        public override Color tooltipColor => Color.LightBlue;
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.Damage += 0.1f;
            modifer.attackSpeed += 0.04f;
            modifer.Crit += 10;
            modifer.shotSpeed += 0.05f;
            modifer.armorPenetration += 4;
            modifer.PenetrateAddition += 1;
            if (modifer.Damage < modifer.attackSpeed) {
                modifer.Damage += 0.04f;
            }
            else {
                modifer.attackSpeed += 0.04f;
            }
        }
    }

}
