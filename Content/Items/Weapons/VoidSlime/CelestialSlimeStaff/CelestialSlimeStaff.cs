using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.CelestialSlimeStaff
{
    public class CelestialSlimeStaff : ModItem
    {
        #region 调参旋钮
        //面板伤害、使用时间(帧)、法力、击退;10栏位时约2250/秒(任务文件)
        public const int BaseDamage = 45;
        public const int UseFrames = 36;
        public const int ManaCost = 10;
        public const float Knockback = 2f;
        public const int SellGold = 32;
        //刚召唤时给增益的帧数,之后由增益自己续上
        public const int BuffFrames = 2;
        //配方:史莱姆法杖1、超新星凝胶8(策划)
        public const int GelCount = 8;
        #endregion

        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
        }

        public override void SetDefaults() {
            Item.width = 54;
            Item.height = 52;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Summon;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item44;
            Item.buffType = ModContent.BuffType<CelestialSlimeStaffBuff>();
            Item.shoot = ModContent.ProjectileType<CelestialSlimeStaffCounter>();
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        //每次使用只加一个隐形计数弹幕;可见的史莱姆由增益在主人端补出
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, BuffFrames);
            Projectile counter = Projectile.NewProjectileDirect(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
            counter.originalDamage = Item.damage;
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.SlimeStaff)
                .AddIngredient<SupernovaGel>(GelCount)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }
    }
}
