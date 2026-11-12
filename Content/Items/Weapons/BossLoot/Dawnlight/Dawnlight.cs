using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.Dawnlight
{
    //日之女神宝藏袋掉落,没有配方;仆从是橙红色的泰拉棱镜剑
    public class Dawnlight : ModItem
    {
        //伤害175为策划定值;其余照原版泰拉棱镜(ItemID 5005)
        private const int BaseDamage = 175;
        private const int UseFrames = 36;
        private const int ReuseDelay = 2;
        private const float Knockback = 4f;
        private const int ManaCost = 10;
        private const int SellGold = 30;
        //物品贴图尺寸(像素)
        private const int IconWidth = 52;
        private const int IconHeight = 52;

        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
        }

        public override void SetDefaults() {
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Summon;
            Item.mana = ManaCost;
            Item.width = IconWidth;
            Item.height = IconHeight;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.reuseDelay = ReuseDelay;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item82;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<DawnlightBlade>();
            Item.buffType = ModContent.BuffType<DawnlightBuff>();
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 2);
            Vector2 spawn = Main.MouseWorld;
            player.LimitPointToPlayerReachableArea(ref spawn);
            int index = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, damage, knockback, player.whoAmI);
            Main.projectile[index].originalDamage = Item.damage;
            return false;
        }
    }
}
