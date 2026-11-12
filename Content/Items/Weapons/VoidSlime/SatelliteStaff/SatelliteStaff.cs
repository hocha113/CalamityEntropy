using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SatelliteStaff
{
    public class SatelliteStaff : ModItem
    {
        #region 调参旋钮
        //面板伤害、使用时间(帧)、法力、击退;10只时约2180/秒,另加烈日灼烧(任务文件)
        public const int BaseDamage = 90;
        public const int UseFrames = 36;
        public const int ManaCost = 10;
        public const float Knockback = 2f;
        public const int SellGold = 32;
        //刚召唤时给增益的帧数,之后由增益自己续上
        public const int BuffFrames = 2;
        //配方:狱烬提灯1、超新星凝胶8、烈阳之光20(策划)
        public const int GelCount = 8;
        public const int SunlightCount = 20;
        #endregion

        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
        }

        public override void SetDefaults() {
            Item.width = 54;
            Item.height = 44;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Summon;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item44;
            Item.buffType = ModContent.BuffType<SatelliteStaffBuff>();
            Item.shoot = ModContent.ProjectileType<SatelliteStaffSlime>();
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, BuffFrames);
            Projectile slime = Projectile.NewProjectileDirect(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
            slime.originalDamage = Item.damage;
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<CinderLantern>()
                .AddIngredient<SupernovaGel>(GelCount)
                .AddIngredient<BlazingSunlight>(SunlightCount)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }
    }
}
