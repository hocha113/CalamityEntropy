using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class UrnOfSouls : ModItem
    {
        public override void SetDefaults() {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 72;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.knockBack = 2.4f;
            Item.UseSound = CEUtils.GetSound("urnopen");
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<UrnOfSoulsHoldout>();
            Item.shootSpeed = 6f;
            Item.mana = 8;
            Item.ArmorPenetration = 32;
            Item.DamageType = DamageClass.Magic;
        }
        public override bool MagicPrefix() {
            return true;
        }
    }
}
