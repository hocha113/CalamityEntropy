using CalamityEntropy.Common;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.SamsaraCasket;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;
namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkAuric : BookMark
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[ItemID.EmpressBlade] = Type;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Auric");
        public override Color tooltipColor => Color.Goldenrod;
        public override EBookProjectileEffect getEffect()
        {
            return new BookMarkAuricBMEffect();
        }
    }

    /// <summary>耀日书签(2026-08-31 平衡案重做):攻击时召唤3个黄金七彩矢攻击目标(固定基伤125)。</summary>
    public class BookMarkAuricBMEffect : EBookProjectileEffect
    {
        public override void OnShoot(EntropyBookHeldProjectile book)
        {
            Projectile proj = book.Projectile;
            Player owner = proj.GetOwner();
            for (int i = 0; i < 3; i++)
            {
                // 原版七彩矢(FairyQueenMagicItemShot),ai[1]锁金色色相
                int p = Projectile.NewProjectile(proj.GetSource_FromAI(), proj.Center,
                    (proj.rotation + Main.rand.NextFloat(-0.7f, 0.7f)).ToRotationVector2() * 10f,
                    ProjectileID.FairyQueenMagicItemShot, FixedDamage(owner, 125, proj.DamageType), proj.knockBack, proj.owner,
                    0, 0.12f + Main.rand.NextFloat(0.05f));
                if (p >= 0 && p < Main.maxProjectiles)
                {
                    Main.projectile[p].DamageType = proj.DamageType;
                }
            }
        }
    }
}
