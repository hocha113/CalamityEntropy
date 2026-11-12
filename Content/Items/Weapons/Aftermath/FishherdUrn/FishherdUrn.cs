using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.FishherdUrn
{
    public class FishherdUrn : ModItem
    {
        //仆从伤害:每只每秒4口,30穿甲;对50防目标10只约10×4×(40-20×0.5)=1200每秒,NPC防御各难度都按一半抵伤
        private const int BaseDamage = 40;
        private const int UseFrames = 30;
        private const float Knockback = 2f;
        private const int ManaCost = 10;
        private const int SellGold = 25;
        private const int BassCost = 10;
        private const int EssenceCost = 10;
        //物品贴图尺寸(像素)
        private const int IconWidth = 50;
        private const int IconHeight = 82;

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
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item44;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FishherdUrnPiranha>();
            Item.buffType = ModContent.BuffType<FishherdUrnBuff>();
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 2);
            int index = Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI, 0f, -1f);
            Main.projectile[index].originalDamage = Item.damage;
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Bass, BassCost)
                .AddIngredient<WraithSoulEssence>(EssenceCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
