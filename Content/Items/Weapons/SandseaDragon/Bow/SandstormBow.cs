using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Bow
{
    /// <summary>
    /// 沙尘暴,弓,每次使用耗 1 支弹药射出 2 支箭,木箭换成 <see cref="SandstormBowGiantArrow"/>
    /// 弹药、攻速与连发走原版使用流程,拉弦与放箭的时机在 <see cref="SandstormBowHoldout"/>
    /// </summary>
    public class SandstormBow : ModItem
    {
        public const int BaseDamage = 55;               //面板,木箭全中每次 2×1.5 + 4×0.8 = 6.2 倍,约 790/秒
        public const int UseFrames = 26;                //使用间隔,帧
        public const float BaseKnockback = 3f;
        public const float ShootSpeed = 9f;             //弓自身弹速,像素每帧,弹药弹速另加
        public const int SellGold = 8;                  //售价,金币,策划原文
        public const int ArrowsPerShot = 2;             //每次射出的箭数,策划原文
        public const float ArrowPairOffset = 5f;        //普通箭离瞄准轴的距离,像素
        public const float GiantPairOffset = 8f;        //巨箭离瞄准轴的距离,像素
        public const float GiantDamageRatio = 1.5f;     //巨箭伤害倍率,策划 150%
        public const float GiantSpeedRatio = 0.7f;      //巨箭初速相对普通箭的倍率
        private const int SpriteWidth = 48;             //物品图宽高,像素
        private const int SpriteHeight = 76;

        public override void SetDefaults() {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.knockBack = BaseKnockback;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.shootSpeed = ShootSpeed;
            Item.useAmmo = AmmoID.Arrow;
        }

        public override bool RangedPrefix() {
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int arrowType = CEUtils.CheckWoodenAmmo(type, player) ? ModContent.ProjectileType<SandstormBowGiantArrow>() : type;
            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            float speed = velocity.Length();
            //扭曲孪生借本物品开火时不持弓,直接从它的位置齐射
            if (player.Entropy().twinSpawnIndex != -1) {
                FireVolley(source, player, position, aim, speed, arrowType, damage, knockback);
                return false;
            }
            SandstormBowHoldout.Arm(player, source, position, aim, arrowType, speed, damage, knockback);
            return false;
        }

        /// <summary>沿 aim 并排射出一轮,只在拥有者端调用。damage 是面板,巨箭的倍率在这里乘</summary>
        internal static void FireVolley(IEntitySource source, Player player, Vector2 origin, Vector2 aim, float speed, int arrowType, int damage, float knockback) {
            bool giant = arrowType == ModContent.ProjectileType<SandstormBowGiantArrow>();
            float offset = giant ? GiantPairOffset : ArrowPairOffset;
            Vector2 normal = aim.RotatedBy(MathHelper.PiOver2);
            Vector2 velocity = aim * (giant ? speed * GiantSpeedRatio : speed);
            for (int i = 0; i < ArrowsPerShot; i++) {
                //2i-(n-1) 把 n 支箭对称排在瞄准轴两侧
                Vector2 spawn = origin + normal * (offset * (2 * i - (ArrowsPerShot - 1)));
                if (giant) {
                    int giantDamage = Math.Max(1, (int)MathF.Round(damage * GiantDamageRatio));
                    Projectile.NewProjectile(source, spawn, velocity, arrowType, giantDamage, knockback, player.whoAmI, damage);
                }
                else {
                    Projectile.NewProjectile(source, spawn, velocity, arrowType, damage, knockback, player.whoAmI);
                }
            }
        }
    }
}
