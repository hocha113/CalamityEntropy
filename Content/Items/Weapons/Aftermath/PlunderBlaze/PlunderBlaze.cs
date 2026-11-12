using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PlunderBlaze
{
    /// <summary>
    /// 劫掠怒焰。按住时 1 秒内喷 10 发暗影火焰,歇 0.4 秒再喷,节奏在持握弹幕里
    /// 以凝胶为弹药,每次扣凝胶时有 50% 不扣;火焰无视 30 防御,打出最后一击的普通敌人有概率再掉一份战利品
    /// </summary>
    public class PlunderBlaze : ModItem
    {
        private const int SpriteWidth = 70;         //物品图尺寸,像素
        private const int SpriteHeight = 32;
        private const int BaseDamage = 150;
        private const float BaseKnockback = 0.5f;
        private const int UseFrames = 6;            //使用时间,帧,等于喷射间隔,只用于面板攻速与生成持握弹幕
        private const int SellGold = 25;            //售价,金币
        private const int BearingCount = 12;        //配方里虚空轴承的数量
        private const float AmmoSaveChance = 0.5f;  //每次该扣凝胶时不扣的概率

        public override void SetDefaults() {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = BaseKnockback;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PlunderBlazeHeld>();
            Item.shootSpeed = PlunderBlazeFlame.Speed;
            Item.useAmmo = AmmoID.Gel;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) {
            return Main.rand.NextFloat() >= AmmoSaveChance;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, player.Center, velocity.SafeNormalize(Vector2.UnitX * player.direction), Item.shoot, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<DimBearing>(BearingCount)
                .AddIngredient(ItemID.IllegalGunParts)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
