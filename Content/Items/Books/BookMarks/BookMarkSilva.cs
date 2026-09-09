using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;
namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkSilva : BookMark
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Silva");
        public override Color tooltipColor => Color.Green;
        public override EBookProjectileEffect getEffect()
        {
            return new SilvaBMEffect();
        }
    }

    /// <summary>苍绿书签(2026-08-31 平衡案重做):命中造成3秒酸性毒液,
    /// 持书期间每秒回复2点生命,命中时获得3秒树妖祝福。</summary>
    public class SilvaBMEffect : EBookProjectileEffect
    {
        public override void BookUpdate(Projectile projectile, bool ownerClient)
        {
            projectile.GetOwner().Entropy().bmSilvaRegenTime = 2;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 180);
            projectile.GetOwner().AddBuff(BuffID.DryadsWard, 180);
        }
    }
}