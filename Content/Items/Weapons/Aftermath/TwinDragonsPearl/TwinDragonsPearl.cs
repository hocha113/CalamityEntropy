using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.TwinDragonsPearl
{
    /// <summary>
    /// 双龙戏珠:手持时宝珠悬在身后,按住时宝珠跟到鼠标处,每次使用从宝珠周围召一只金色飞龙幻影弧线穿过宝珠
    /// </summary>
    public class TwinDragonsPearl : ModItem
    {
        #region 调参旋钮
        //面板伤害;使用间隔(帧),即每只飞龙的间隔;每只飞龙的魔力
        public const int BaseDamage = 190;
        public const int UseTime = 10;
        public const int ManaPerWyvern = 6;
        public const float Knockback = 3f;
        //宝珠离鼠标超过此距离(像素,5 格)时飞龙改为穿过鼠标处:刚按下时宝珠还在身后
        public const float PearlSnapRange = 5f * 16f;
        //配方:任意珍珠与烈阳之光的数量
        public const int PearlCost = 3;
        public const int SunlightCost = 10;
        //售价(金币)
        public const int SellGold = 25;
        #endregion

        //相邻两只飞龙共用入射角与弯向,第二只从对侧进入;只在所有者端读写
        private int wyvernCount;
        private float pairAngle;
        private float pairBend;

        public override void SetDefaults() {
            Item.width = 72;
            Item.height = 68;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaPerWyvern;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.shoot = ModContent.ProjectileType<TwinDragonsPearlWyvern>();
            Item.shootSpeed = 1f;
        }

        public override void HoldItem(Player player) {
            int orb = ModContent.ProjectileType<TwinDragonsPearlOrb>();
            if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[orb] < 1) {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, Vector2.Zero, orb, 0, 0f, player.whoAmI);
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 mouse = Main.MouseWorld;
            Vector2 pearl = TwinDragonsPearlOrb.TryGetPearl(player, out Vector2 at) && Vector2.Distance(at, mouse) < PearlSnapRange ? at : mouse;
            if (wyvernCount % 2 == 0) {
                pairAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                pairBend = Main.rand.NextBool() ? 1f : -1f;
            }
            float angle = wyvernCount % 2 == 0 ? pairAngle : pairAngle + MathHelper.Pi;
            wyvernCount++;
            Projectile.NewProjectile(source, pearl, Vector2.Zero, type, damage, knockback, player.whoAmI, angle, pairBend);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddRecipeGroup(CERecipeGroups.AnyPearl, PearlCost)
                .AddIngredient<BlazingSunlight>(SunlightCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
