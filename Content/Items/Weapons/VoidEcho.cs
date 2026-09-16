using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class VoidEcho : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 380;
            Item.DamageType = DamageClass.Magic;
            Item.width = 96;
            Item.noUseGraphic = true;
            Item.height = 96;
            Item.useTime = 1;
            Item.useAnimation = 0;
            Item.channel = true;
            Item.knockBack = 4;
            Item.value = Item.buyPrice(platinum: 2);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<VBSpawner>();
            Item.shootSpeed = 1f;
            Item.mana = 300;
            Item.useStyle = -1;
            Item.noMelee = true;
        }
        public override bool MagicPrefix() {
            return true;
        }
        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[ModContent.ProjectileType<VoidBlaster>()] <= 0;
        }
    }
}
