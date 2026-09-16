using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class SlimeYoyo : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.Yoyo[Item.type] = true; ItemID.Sets.GamepadExtraRange[Item.type] = 16; ItemID.Sets.GamepadSmartQuickReach[Item.type] = true;
        }

        public override void SetDefaults() {
            Item.width = 10; Item.height = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = Item.useAnimation = 25;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.damage = 10;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.knockBack = 3f;
            Item.crit = 8;
            Item.channel = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(silver: 28);
            Item.shoot = ModContent.ProjectileType<SlimeYoyoProjectile>(); Item.shootSpeed = 16f;
        }

        public override bool MeleePrefix() {
            return true;
        }
    }
}
