using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.NecromancerPrimer
{
    /// <summary>
    /// 死灵法师见习手册:每次使用在玩家身后的半圆里召出两只粉色地牢幽魂,飞向鼠标
    /// </summary>
    public class NecromancerPrimer : ModItem
    {
        #region 调参旋钮
        //面板伤害;使用间隔(帧);每次使用的魔力
        public const int BaseDamage = 100;
        public const int UseTime = 10;
        public const int ManaCost = 8;
        public const float Knockback = 2f;
        //每次使用召出的幽魂数
        public const int SpiritsPerUse = 2;
        //生成点离玩家中心的距离范围(像素,4~8 格)
        public const float SpawnMinDist = 4f * 16f;
        public const float SpawnMaxDist = 8f * 16f;
        //找不被墙挡住的生成点的尝试次数,都不行就从玩家中心出发
        public const int SpawnTries = 6;
        //幽魂初速(像素/帧);初速朝向向「玩家指向鼠标」方向靠拢的比例
        public const float SpiritSpeed = 16f;
        public const float AimBlend = 0.25f;
        //使用音效:音高浮动与同时实例上限(每秒 6 次)
        public const float CastPitchVariance = 0.2f;
        public const int CastMaxInstances = 3;
        //配方:聚星之灵数量
        public const int WraithSoulCost = 10;
        //售价(金币)
        public const int SellGold = 25;
        #endregion

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 30;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item8 with { PitchVariance = CastPitchVariance, MaxInstances = CastMaxInstances };
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.shoot = ModContent.ProjectileType<NecromancerPrimerSpirit>();
            Item.shootSpeed = SpiritSpeed;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            float aimAngle = aim.ToRotation();
            for (int n = 0; n < SpiritsPerUse; n++) {
                Vector2 spawn = FindSpawn(player, aimAngle);
                Vector2 toMouse = (Main.MouseWorld - spawn).SafeNormalize(aim);
                Vector2 vel = Vector2.Lerp(toMouse, aim, AimBlend).SafeNormalize(aim) * SpiritSpeed;
                Projectile.NewProjectile(source, spawn, vel, type, damage, knockback, player.whoAmI);
            }
            return false;
        }

        //身后半圆:以瞄准方向的反方向为中心,左右各 90 度
        private static Vector2 FindSpawn(Player player, float aimAngle) {
            for (int i = 0; i < SpawnTries; i++) {
                float angle = aimAngle + MathHelper.Pi + Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                Vector2 spot = player.Center + angle.ToRotationVector2() * Main.rand.NextFloat(SpawnMinDist, SpawnMaxDist);
                if (Collision.CanHit(player.Center, 1, 1, spot, 1, 1)) {
                    return spot;
                }
            }
            return player.Center;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.SpellTome)
                .AddIngredient<WraithSoulEssence>(WraithSoulCost)
                .AddTile(TileID.Bookcases)
                .Register();
        }
    }
}
