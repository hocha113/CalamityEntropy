using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.NebulaVortexStaff
{
    /// <summary>
    /// 星云漩涡圣杖:在鼠标处开一个缓慢追敌的星云涡旋,同时只存在一个,新的替掉旧的
    /// </summary>
    public class NebulaVortexStaff : ModItem
    {
        #region 调参旋钮
        //面板伤害;使用间隔(帧);魔力
        public const int BaseDamage = 110;
        public const int UseTime = 30;
        public const int ManaCost = 40;
        public const float Knockback = 2f;
        //配方:聚星之灵数量
        public const int WraithSoulCost = 5;
        //售价(金币)
        public const int SellGold = 25;
        #endregion

        public override void SetStaticDefaults() {
            Item.staff[Type] = true;
        }

        public override void SetDefaults() {
            Item.width = 60;
            Item.height = 62;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item117;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.shoot = ModContent.ProjectileType<NebulaVortexStaffVortex>();
            Item.shootSpeed = NebulaVortexStaffVortex.WanderSpeed;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            //提前移除的旧涡旋 timeLeft 仍大于 0,不会触发终结蓝焰
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == player.whoAmI && p.type == type) {
                    p.Kill();
                }
            }
            Vector2 dir = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(source, Main.MouseWorld, dir * NebulaVortexStaffVortex.WanderSpeed, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.NebulaArcanum)
                .AddIngredient<WraithSoulEssence>(WraithSoulCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
