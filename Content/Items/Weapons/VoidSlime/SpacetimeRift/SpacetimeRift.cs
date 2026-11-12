using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SpacetimeRift
{
    /// <summary>
    /// 时空裂隙: 照日暮的发射方式,每轮从身前一段弧上自下而上连射 3 支,箭矢一律换成 SpacetimeRiftArrow
    /// 第几支看 player.ItemUsesThisAnimation,ApplyItemTime 在 Shoot 之前把它加到 1、2、3;只有最后一支消耗弹药,同日暮
    /// </summary>
    public class SpacetimeRift : ModItem
    {
        //面板伤害、击退
        public const int BaseDamage = 85;
        private const float BaseKnockback = 2f;
        //一轮的动作时长、相邻两支的间隔(帧)与每轮支数;日暮是 30、2、5,每秒同样 10 支
        private const int RoundFrames = 18;
        private const int ShotGap = 2;
        private const int ShotsPerRound = 3;
        //出手速度(像素/更新),日暮是 10
        private const float ShootSpeed = 12f;
        //发射弧: 半径(像素)与相邻两个发射点的夹角(弧度);日暮是 40 像素、π/10 分 5 点
        private const float ArcRadius = 40f;
        private const float ArcStep = MathHelper.Pi / 8f;
        //鼠标离发射点 100 像素以内逐渐改成直射,40 像素以内完全直射,同日暮
        private const float AimBlendFar = 100f;
        private const float AimBlendNear = 40f;
        //售价(金币)、配方里超新星凝胶的数量
        private const int SellGold = 32;
        private const int GelCount = 8;

        public override void SetDefaults() {
            Item.width = 36;
            Item.height = 76;
            Item.damage = BaseDamage;
            Item.knockBack = BaseKnockback;
            Item.DamageType = DamageClass.Ranged;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = RoundFrames;
            Item.useTime = ShotGap;
            Item.useLimitPerAnimation = ShotsPerRound;
            Item.consumeAmmoOnLastShotOnly = true;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item5;
            Item.shoot = ModContent.ProjectileType<SpacetimeRiftArrow>();
            Item.useAmmo = AmmoID.Arrow;
            Item.shootSpeed = ShootSpeed;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int shot = Math.Clamp(player.ItemUsesThisAnimation - 1, 0, ShotsPerRound - 1);
            int slot = player.direction == 1 ? ShotsPerRound - 1 - shot : shot;
            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 forward = aim * ArcRadius;
            Vector2 offset = forward.RotatedBy((slot - (ShotsPerRound - 1) / 2f) * ArcStep);
            if (!Collision.CanHit(position, 0, 0, position + forward, 0, 0))
                offset -= forward;
            Vector2 origin = position + offset;
            Vector2 mouse = Main.MouseWorld;
            float straight = Utils.GetLerpValue(AimBlendFar, AimBlendNear, mouse.Distance(position), true);
            Vector2 direction = Vector2.Lerp((mouse - origin).SafeNormalize(aim), aim, straight).SafeNormalize(aim);
            Projectile.NewProjectile(source, origin, direction * velocity.Length(), ModContent.ProjectileType<SpacetimeRiftArrow>(), damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.FairyQueenRangedItem)
                .AddIngredient<SupernovaGel>(GelCount)
                .AddTile<GalaxySolidifierTile>()
                .Register();
        }
    }
}
