using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.EmbraceWhip
{
    public class EmbraceWhip : BaseWhipItem
    {
        //鞭伤与挥鞭用时(帧):每秒两鞭约900,加上20%标记暴击带给10栏召唤的约260,单体约1160每秒(对50防约1110)
        private const int BaseDamage = 450;
        private const int UseFrames = 30;
        private const float Knockback = 3f;
        //出鞭速度(像素/帧),和弹幕的长度倍率一起决定鞭长
        private const float ShootSpeed = 4f;
        //标记暴击率,对应提示「增加20%召唤物暴击率」
        private const float TagCrit = 0.20f;
        private const int SellGold = 25;
        private const int SunlightCost = 14;

        public override int TagDamage => 0;
        public override float TagCritChance => TagCrit;

        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<EmbraceWhipProj>(), BaseDamage, Knockback, ShootSpeed, UseFrames);
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.autoReuse = true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<BlazingSunlight>(SunlightCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
