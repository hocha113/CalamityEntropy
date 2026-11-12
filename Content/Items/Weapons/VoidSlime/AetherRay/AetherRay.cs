using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.AetherRay
{
    /// <summary>
    /// 以太射线,按住时每次使用发两道并排的伽玛射线,前后两对接成一道持续光束;使用时带出一架浮游炮,同频索敌开火
    /// 开火时按住右键过载,浮游炮回身边供能,射线变彩虹并加伤;持握弹幕与浮游炮在 HoldItem 里生成,朋友盒只复制射线
    /// </summary>
    public class AetherRay : ModItem
    {
        #region 调参旋钮
        public const int BaseDamage = 190;          //任务建议
        public const int UseTime = 36;              //帧,与射线判伤寿命相同才无缝
        public const int ManaCost = 10;             //任务建议约 10
        public const float Knockback = 2f;
        public const float ShootSpeed = 1f;         //射线不飞行,只取方向
        public const int SellGold = 32;             //策划 32 金
        public const int SpriteWidth = 76;          //物品图尺寸,像素
        public const int SpriteHeight = 34;
        public const int GelCost = 8;               //策划 8 超新星凝胶
        public const int NebulaCost = 8;            //策划 8 星云碎片
        #endregion

        public override void SetDefaults() {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.shoot = ModContent.ProjectileType<AetherRayBeam>();
            Item.shootSpeed = ShootSpeed;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        //原版先跑 HoldItem 再跑 Shoot,第一次使用当帧就有浮游炮接开火请求
        public override void HoldItem(Player player) {
            if (player.itemAnimation <= 0) {
                return;
            }
            player.CheckAndSpawnHeldProj(ModContent.ProjectileType<AetherRayHeld>());
            player.CheckAndSpawnHeldProj(ModContent.ProjectileType<AetherRayDrone>());
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            //朋友盒复制时 twinSpawnIndex 是双子编号,本体与双子的射线各自衔接
            int twin = player.Entropy().twinSpawnIndex;
            foreach (int side in AetherRayBeam.Sides) {
                float chained = AetherRayBeam.CanChain(player.whoAmI, side, twin) ? 1f : 0f;
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, side, chained);
            }
            if (twin == AetherRayBeam.NoTwin) {
                AetherRayDrone.RequestShot(player.whoAmI);
            }
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.ChargedBlasterCannon)
                .AddIngredient(ItemID.LaserMachinegun)
                .AddIngredient<SupernovaGel>(GelCost)
                .AddIngredient(ItemID.FragmentNebula, NebulaCost)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }
    }
}
