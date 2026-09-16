using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkCancer : BookMark
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
        public override Texture2D UITexture => BookMark.GetUITexture("Cancer");
        public override Color tooltipColor => Color.LightBlue;
        // 2026-08-31 平衡案重做:+50%弹幕大小,-10%魔力消耗,大幅提升击退(原减速敌方弹幕效果退役)
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.Size += 0.5f;
            modifer.ManaCost *= 0.9f;
            modifer.Knockback += 4f;
        }
    }
}
