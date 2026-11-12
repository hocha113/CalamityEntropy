using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StellarFury
{
    /// <summary>
    /// 星之怒,法杖,发射白光包裹的黑色魔法导弹,直线飞行;命中敌人时在命中点留下滞留 1.5 秒的迷你黑洞
    /// </summary>
    public class StellarFury : ModItem
    {
        #region 调参旋钮
        public const int BaseDamage = 230;              //任务建议
        public const int UseTime = 20;                  //帧,任务建议
        public const int ManaCost = 12;                 //任务建议
        public const float Knockback = 4f;
        public const int SellGold = 32;                 //策划 32 金
        public const int SpriteSize = 56;               //物品图边长,像素
        public const float MissileDamageMult = 1.5f;    //策划导弹 150%
        public const float HoleDamageMult = 0.8f;       //策划黑洞 80%
        public const float TipReach = 48f;              //像素,杖头离手,导弹从这里出
        public const int GelCost = 8;                   //策划 8 超新星凝胶
        public const float CastPitch = 1.2f;
        public const float CastPitchVariance = 0.15f;
        public const float CastVolume = 0.5f;
        public const int CastMaxInstances = 3;
        #endregion

        public override void SetStaticDefaults() {
            Item.staff[Type] = true;
        }

        public override void SetDefaults() {
            Item.width = Item.height = SpriteSize;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.UseSound = CEUtils.GetSound("shootMissileLarge", CastPitch, CastMaxInstances, CastVolume) with { PitchVariance = CastPitchVariance };
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.shoot = ModContent.ProjectileType<StellarFuryMissile>();
            Item.shootSpeed = StellarFuryMissile.Speed;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            //从杖头出,杖头卡进墙时退回手上
            Vector2 tip = position + velocity.SafeNormalize(Vector2.UnitX * player.direction) * TipReach;
            if (!Collision.CanHitLine(position, 0, 0, tip, 0, 0)) {
                tip = position;
            }
            Projectile.NewProjectile(source, tip, velocity, type, (int)(damage * MissileDamageMult), knockback, player.whoAmI, (int)(damage * HoleDamageMult));
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.InfernoFork)
                .AddIngredient<SupernovaGel>(GelCost)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }
    }
}
