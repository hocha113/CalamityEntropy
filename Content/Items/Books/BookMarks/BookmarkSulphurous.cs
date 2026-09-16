using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkSulphurous : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(gold: 5);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Sulphurous");
        public override EBookProjectileEffect getEffect() {
            return new BookmarkSulphurousBMEffect();
        }

        public override Color tooltipColor => Color.LimeGreen;
        public override void AddRecipes() {
        }
    }

    public partial class BookmarkSulphurousBMEffect : EBookProjectileEffect
    {
        // 2026-08-31 平衡案:无泡泡期间改为造成诅咒狱火(原辐射减益退役)
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            if (projectile.GetOwner().Entropy().SulphurousBubbleRecharge < 3600) {
                target.AddBuff(BuffID.CursedInferno, Time);
            }
        }
    }
}
