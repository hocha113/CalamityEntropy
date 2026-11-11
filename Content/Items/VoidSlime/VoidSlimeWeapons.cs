using CalamityEntropy.Content.Projectiles.VoidSlime;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.VoidSlime
{
    /// <summary>凝星刃,近战:挥砍甩出会弹跳的胶团,胶团消失时炸成追踪小辉星</summary>
    public class GelstarBlade : ModItem
    {
        public override void SetDefaults() {
            Item.width = 48;
            Item.height = 48;
            Item.damage = 620;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<GelstarBlob>();
            Item.shootSpeed = 13f;
            Item.value = Item.buyPrice(platinum: 1, gold: 80);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
    }

    /// <summary>环轨弓,远程:每箭附带两颗追踪小辉星</summary>
    public class OrbitBow : ModItem
    {
        public const float StarletDamage = 0.6f;
        public const float StarletSpread = 0.14f;

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 48;
            Item.damage = 210;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.shootSpeed = 14f;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = Item.buyPrice(platinum: 1, gold: 80);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int starlet = ModContent.ProjectileType<VSlimeFriendlyStarlet>();
            for (int side = -1; side <= 1; side += 2) {
                Projectile.NewProjectile(source, position, velocity.RotatedBy(StarletSpread * side) * 0.85f, starlet,
                    (int)(damage * StarletDamage), knockback * 0.5f, player.whoAmI, 0f, 12f, 1f);
            }
            return true;
        }
    }

    /// <summary>脉冲星杖,魔法:朝光标射出一道穿透的伽马射线</summary>
    public class PulsarStaff : ModItem
    {
        public override void SetStaticDefaults() {
            Item.staff[Type] = true;
        }

        public override void SetDefaults() {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 480;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PulsarBeam>();
            Item.shootSpeed = 1f;
            Item.value = Item.buyPrice(platinum: 1, gold: 80);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
    }

    /// <summary>奇点杖,召唤:在光标处放一个小黑洞哨兵</summary>
    public class SingularityRod : ModItem
    {
        public override void SetDefaults() {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 150;
            Item.DamageType = DamageClass.Summon;
            Item.sentry = true;
            Item.mana = 20;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 2f;
            Item.UseSound = SoundID.Item78;
            Item.shoot = ModContent.ProjectileType<MiniSingularity>();
            Item.shootSpeed = 1f;
            Item.value = Item.buyPrice(platinum: 1, gold: 80);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI);
            player.UpdateMaxTurrets();
            return false;
        }
    }
}
