using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class MindCorruptor : BaseWhipItem
    {
        public override int TagDamage => 3;
        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<MindCorruptorProj>(), 32, 3, 4, 42);
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(gold: 1);
            Item.autoReuse = true;
            Item.UseSound = CEUtils.GetSound("corruptwhip_swing");
        }
        public override bool CanUseItem(Player player) {
            Item.UseSound = CEUtils.GetSound("corruptwhip_swing", Main.rand.NextFloat(0.6f, 1.4f));
            return true;
        }
    }
}
