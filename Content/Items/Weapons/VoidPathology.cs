using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class VoidPathology : ModItem
    {
        public override void SetStaticDefaults() {
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults() {
            Item.width = 104;
            Item.height = 104;
            Item.damage = 225;
            Item.noMelee = true;
            Item.useAnimation = Item.useTime = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 0;
            Item.UseSound = CEUtils.GetSound("vp_use");
            Item.maxStack = 1;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.rare = ModContent.RarityType<NihilityBlue>();
            Item.shoot = ModContent.ProjectileType<NihilityVirus>();
            Item.shootSpeed = 16f;
            Item.mana = 6;
            Item.DamageType = DamageClass.Magic;
            Item.channel = true;
            Item.useTurn = false;
        }
        public override bool MagicPrefix() {
            return true;
        }
    }
}
