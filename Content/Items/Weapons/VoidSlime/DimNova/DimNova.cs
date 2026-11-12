using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.DimNova
{
    /// <summary>
    /// 黯淡新星: 左键掷出 DimNovaGrenade,右键让场上闲置的 DimNovaRemnant 去追敌人
    /// 右键的 CanShoot 为 false,命令在 UseItem 里由主人端下达,不经过 Shoot,朋友盒复制不到
    /// </summary>
    public class DimNova : ModItem
    {
        //面板伤害、击退
        public const int BaseDamage = 140;
        private const float BaseKnockback = 6f;
        //投掷间隔(帧)
        private const int UseFrames = 40;
        //右键只是下命令,动作快一倍
        private const float CommandSpeedMult = 2f;
        //出手速度(像素/帧),原版弹力手榴弹是 6.5
        private const float ThrowSpeed = 11f;
        //售价(金币)
        private const int SellGold = 32;
        //配方: 弹力手榴弹、超新星凝胶的数量
        private const int GrenadeCount = 100;
        private const int GelCount = 8;

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 32;
            Item.damage = BaseDamage;
            Item.knockBack = BaseKnockback;
            Item.DamageType = DamageClass.Ranged;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<DimNovaGrenade>();
            Item.shootSpeed = ThrowSpeed;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override float UseSpeedMultiplier(Player player) => player.altFunctionUse == 2 ? CommandSpeedMult : 1f;

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2)
                return DimNovaRemnant.CountIdle(player.whoAmI) > 0;
            return true;
        }

        public override bool CanShoot(Player player) => player.altFunctionUse != 2;

        public override bool? UseItem(Player player) {
            if (player.altFunctionUse != 2)
                return null;
            if (player.whoAmI == Main.myPlayer)
                DimNovaRemnant.CommandAll(player);
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.BouncyGrenade, GrenadeCount)
                .AddIngredient<SupernovaGel>(GelCount)
                .AddTile<GalaxySolidifierTile>()
                .Register();
        }
    }
}
