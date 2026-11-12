using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.NoontideRapier
{
    //按住持续刺击;本体不伤人,伤害在 NoontideRapierStab 与它发出的 NoontideRapierBolt 上
    public class NoontideRapier : ModItem
    {
        public const int BaseDamage = 85;          //每秒 15 刺 + 3 矢×150%,约 1657,不计暴击
        public const int BonusCrit = 6;            //显示 10%,游戏自带 4%
        public const float BaseKnockback = 4f;     //原版星光
        public const int UseFrames = 6;            //原版星光
        public const int AnimationFrames = 18;     //原版星光
        public const float AimSpeed = 15f;         //原版星光,px/帧,刺击弹幕每帧照此前移
        public const int SellGold = 30;
        public const int SpriteSize = 56;          //贴图边长,px

        public override void SetDefaults() {
            Item.width = SpriteSize;
            Item.height = SpriteSize;
            Item.damage = BaseDamage;
            Item.crit = BonusCrit;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = BaseKnockback;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.useTime = UseFrames;
            Item.useAnimation = AnimationFrames;
            Item.channel = true;
            Item.autoReuse = false;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<NoontideRapierStab>();
            Item.shootSpeed = AimSpeed;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool MeleePrefix() => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, NoontideRapierStab.GetStabScale(player, Item));
            return false;
        }
    }
}
