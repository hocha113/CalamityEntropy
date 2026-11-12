using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarBlessing
{
    public class StarBlessing : BaseWhipItem
    {
        #region 调参旋钮
        //鞭伤与挥鞭用时(帧):每秒两鞭约500,星团每秒约495,标记带给召唤物的约800,合计约1800每秒(任务文件建议值)
        public const int BaseDamage = 250;
        public const int UseFrames = 30;
        public const float Knockback = 3f;
        //出鞭速度(像素/帧),和弹幕的长度倍率一起决定鞭长
        public const float WhipSpeed = 4f;
        //标记伤害20、标记暴击12%(策划),对应提示前两行
        public const int Tag = 20;
        public const float TagCrit = 0.12f;
        public const int SellGold = 32;
        //配方:超新星凝胶8、聚星之炵20(策划)
        public const int GelCount = 8;
        public const int EssenceCount = 20;
        #endregion

        public override int TagDamage => Tag;
        public override float TagCritChance => TagCrit;

        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<StarBlessingProj>(), BaseDamage, Knockback, WhipSpeed, UseFrames);
            Item.width = 52;
            Item.height = 48;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.autoReuse = true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<SupernovaGel>(GelCount)
                .AddIngredient<WraithSoulEssence>(EssenceCount)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }
    }
}
