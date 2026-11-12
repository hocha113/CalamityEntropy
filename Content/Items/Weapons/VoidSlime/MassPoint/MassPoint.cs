using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ConstellationItem = CalamityEntropy.Content.Items.Weapons.Aftermath.Constellation.Constellation;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.MassPoint
{
    //悠悠球,持续命中后在球心长出跟随的金边黑洞
    public class MassPoint : ModItem
    {
        //手柄智能光标的额外距离,单位格,按射程 45 格照星座球的 35(40 格)换算
        private const int GamepadRange = 39;

        public override void SetStaticDefaults() {
            ItemID.Sets.Yoyo[Type] = true;
            ItemID.Sets.GamepadExtraRange[Type] = GamepadRange;
            ItemID.Sets.GamepadSmartQuickReach[Type] = true;
        }

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.damage = 120;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.knockBack = 3f;
            Item.channel = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 32);
            Item.shoot = ModContent.ProjectileType<MassPointYoyo>();
            Item.shootSpeed = 16f;
        }

        public override bool MeleePrefix() => true;

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<ConstellationItem>()
                .AddIngredient<SupernovaGel>(8)
                .AddTile<GalaxySolidifierTile>()
                .Register();
        }
    }
}
