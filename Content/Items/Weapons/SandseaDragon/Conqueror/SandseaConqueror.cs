using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Conqueror
{
    /// <summary>
    /// 沙海征服者,短剑,不吃近战攻速
    /// 刺击由 <see cref="SandseaConquerorStab"/> 执行,能量球与毒牙都从那里生成
    /// </summary>
    public class SandseaConqueror : ModItem
    {
        public const int BaseDamage = 48;       //面板,贴身每刺 48 + 2×24 + 4×34 = 232,约 870/秒
        public const int UseFrames = 16;        //使用间隔,帧
        public const float BaseKnockback = 4f;
        public const int SellGold = 8;          //售价,金币,策划原文
        public const float AimSpeed = 2.1f;     //只取方向,数值沿用原版短剑
        private const int SpriteWidth = 40;     //物品图宽高,像素
        private const int SpriteHeight = 42;

        public override void SetDefaults() {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.knockBack = BaseKnockback;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.shoot = ModContent.ProjectileType<SandseaConquerorStab>();
            Item.shootSpeed = AimSpeed;
        }

        public override bool MeleePrefix() {
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            int life = Math.Max(SandseaConquerorStab.MinLife, player.itemAnimationMax);
            Projectile.NewProjectile(source, position, dir, type, damage, knockback, player.whoAmI, 0f, life);
            return false;
        }
    }
}
