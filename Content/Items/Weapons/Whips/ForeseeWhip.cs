using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class ForeseeWhip : BaseWhipItem
    {
        public override int TagDamage => 10;
        public override float TagDamageMult => 1.05f;
        public override float TagCritChance => 0.10f;
        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<ForeseeWhipProj>(), 100, 3, 4, 24);
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.buyPrice(gold: 60);
            Item.autoReuse = true;
        }
    }
}
