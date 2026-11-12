using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.PhoenixBreath
{
    /// <summary>
    /// 凤凰吐息。左键蓄力后喷出一道无限穿透的火柱,遇墙、遇水截断;右键按住连发烈焰火球,命中后顺着来向散出三发追踪火焰弹
    /// 以凝胶为弹药,每次该扣凝胶时有 75% 不扣;所有攻击施加 3 秒烈日灼烧;手持时翅膀飞行时间翻倍
    /// 蓄力、出弹与扣弹都在持握弹幕里;射程、穿墙与命中减益各收在下面一个静态方法里,供后续的喷火器饰品接入
    /// </summary>
    public class PhoenixBreath : ModItem
    {
        public const int UseFrames = 30;                //使用时间,帧;火柱每隔这么久判定一次扣弹,火球每隔这么久发一个,都再乘攻速
        private const int SpriteWidth = 96;             //物品图尺寸,像素
        private const int SpriteHeight = 48;
        private const int BaseDamage = 200;
        private const float BaseKnockback = 2f;
        private const int SellGold = 32;                //售价,金币
        private const float AmmoSaveChance = 0.75f;     //每次该扣凝胶时不扣的概率
        private const int SupernovaGelCount = 8;        //配方里超新星凝胶的数量
        private const int SunlightCount = 10;           //配方里烈阳之光的数量
        private const float ColumnReachTiles = 30f;     //火柱最长长度,格,从喷口量起
        private const int BurnFrames = 180;             //烈日灼烧时长,帧,3 秒

        public override void SetStaticDefaults() {
            //按住右键时火球模式收起后能接着再举枪
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

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
            Item.shoot = ModContent.ProjectileType<PhoenixBreathHeld>();
            Item.shootSpeed = PhoenixBreathFireball.LaunchSpeed;
            Item.useAmmo = AmmoID.Gel;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2 && player.GetModPlayer<PhoenixBreathPlayer>().FireballCooldown > 0) {
                return false;
            }
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) {
            return Main.rand.NextFloat() >= AmmoSaveChance;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            float mode = player.altFunctionUse == 2 ? PhoenixBreathHeld.ModeFireball : PhoenixBreathHeld.ModeColumn;
            Projectile.NewProjectile(source, player.Center, velocity.SafeNormalize(Vector2.UnitX * player.direction), Item.shoot, damage, knockback, player.whoAmI, mode);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Flamethrower)
                .AddIngredient<SupernovaGel>(SupernovaGelCount)
                .AddIngredient<BlazingSunlight>(SunlightCount)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }

        /// <summary>火柱最长长度,像素,从喷口量起</summary>
        public static float ColumnReach(Player player) {
            return ColumnReachTiles * 16f;
        }

        /// <summary>火柱是否穿过实心物块;遇水照样截断</summary>
        public static bool ColumnPassesWalls(Player player) {
            return false;
        }

        /// <summary>火柱、火球与火焰弹每次命中都走这里施加减益</summary>
        public static void ApplyHitDebuffs(Player player, NPC target) {
            target.AddBuff(ModContent.BuffType<SunblazeBurn>(), BurnFrames);
        }
    }

    /// <summary>凤凰吐息的火球冷却与飞行时间。冷却只在拥有者端有意义,连点右键快不过按住</summary>
    public class PhoenixBreathPlayer : ModPlayer
    {
        private const int WingTimeMultiplier = 2;

        public int FireballCooldown;    //还要多少帧才能再用右键,帧

        public override void PostUpdateEquips() {
            //装备结算完、翅膀定下飞行时间之后再翻倍,没有翅膀时仍是 0
            if (Player.HeldItem.type == ModContent.ItemType<PhoenixBreath>()) {
                Player.wingTimeMax *= WingTimeMultiplier;
            }
        }

        public override void PostUpdate() {
            if (FireballCooldown > 0) {
                FireballCooldown--;
            }
        }
    }
}
