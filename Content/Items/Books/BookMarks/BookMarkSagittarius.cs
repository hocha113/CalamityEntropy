using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkSagittarius : BookMark
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
        public override Texture2D UITexture => BookMark.GetUITexture("Sagittarius");
        public override Color tooltipColor => Color.LightBlue;
        // 2026-08-31 平衡案重做:+33%弹速,持书期间刷怪率翻倍
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.shotSpeed += 0.33f;
        }
        public override EBookProjectileEffect getEffect() {
            return new SagittariusBMEffect();
        }
    }

    public class SagittariusBMEffect : EBookProjectileEffect
    {
        public override void BookUpdate(Projectile projectile, bool ownerClient) {
            // 服务端也要看到这张票据,刷怪率在服务端结算
            projectile.GetOwner().Entropy().bmSagSpawnTime = 2;
        }
    }

    /// <summary>人马座书签:持书期间刷怪率翻倍。</summary>
    public class SagittariusSpawnGNPC : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
            if (player.Entropy().bmSagSpawnTime > 0) {
                spawnRate = int.Max(1, spawnRate / 2);
                maxSpawns *= 2;
            }
        }
    }
}