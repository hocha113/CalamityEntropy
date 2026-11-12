using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.ManmadeCalamity
{
    public class ManmadeCalamity : ModItem
    {
        public const float HeadDamageMult = 3f;        //锤头打 300% 面板,策划
        public const float CellDamageMult = 0.25f;     //小细胞打 25% 面板,协调方定
        public const float CellKnockbackMult = 0.25f;  //小细胞击退占面板击退
        public const int VirusFrames = 600;            //虚空病变 10 秒,策划
        public const int HealAmount = 2;               //策划
        public const int HealCooldownFrames = 30;      //0.5 秒,策划
        private const int BaseDamage = 140;            //协调方建议面板
        private const float BaseKnockback = 7f;        //协调方建议
        private const int UseFrames = 20;              //同猪鲨链球,飞行中由锤头续上
        private const float ThrowSpeed = 20f;          //出手速度,像素每帧;原版 14
        private const int SpriteSize = 68;             //物品图边长,像素
        private const int SellGold = 30;               //BRIEF-B

        public override void SetDefaults() {
            Item.width = SpriteSize;
            Item.height = SpriteSize;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = BaseKnockback;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ManmadeCalamityHead>();
            Item.shootSpeed = ThrowSpeed;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool CanUseItem(Player player) => !ManmadeCalamityHead.HasFlyingHead(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            ManmadeCalamityHead.ClearResting(player);
            Projectile.NewProjectile(source, player.MountedCenter, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
    }

    public class ManmadeCalamityPlayer : ModPlayer
    {
        private int healCooldown;

        public bool TryStartHealCooldown() {
            if (healCooldown > 0)
                return false;
            healCooldown = ManmadeCalamity.HealCooldownFrames;
            return true;
        }

        public override void PostUpdate() {
            if (healCooldown > 0)
                healCooldown--;
        }
    }
}
