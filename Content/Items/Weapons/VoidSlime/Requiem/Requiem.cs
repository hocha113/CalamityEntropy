using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    public class Requiem : ModItem
    {
        public const int BaseDamage = 120;          //五发一轮共 7.5 倍单发,10 发/秒,配约 +17 的弹药 (120 + 17) × 15 ≈ 2050/秒
        public const int UseTime = 6;               //帧/发
        public const float AmmoSaveChance = 0.5f;
        public const float SpreadMinDegrees = 1f;   //度,每发随机偏左或偏右
        public const float SpreadMaxDegrees = 2f;
        public const float PiercerSpeed = 15f;      //像素/更新,子弹自带 1 次额外更新
        public const float ChargerSpeed = 13f;
        public const float Knockback = 2f;
        public const float LifeCostRatio = 0.3f;    //右键代价,占最大生命
        public const int FractureTime = 36000;      //帧,10 分钟
        public const float RecoilPixels = 4f;       //像素,按本次使用进度的平方衰减
        public static readonly Vector2 Holdout = new(-2f, 0f);          //像素,旋转点在贴图 (2, 18)
        public static readonly Vector2 MuzzleFromPivot = new(62f, -9f); //像素,枪口在贴图 (64, 9)
        public const int GelCost = 16;
        public const int SellGold = 36;

        //五发一轮的排列,true 为银色充能弹
        private static readonly bool[] ChargerSlots = { false, true, false, true, false };

        public override void SetDefaults() {
            Item.width = 66;
            Item.height = 36;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = UseTime;
            Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = PiercerSpeed;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override Vector2? HoldoutOffset() => Holdout;

        public override bool AltFunctionUse(Player player) => true;

        public static int LifeCost(Player player) => (int)(player.statLifeMax2 * LifeCostRatio);

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse != 2)
                return true;
            return !player.HasBuff(ModContent.BuffType<RequiemFracture>()) && player.statLife > LifeCost(player);
        }

        public override bool NeedsAmmo(Player player) => player.altFunctionUse != 2;

        //右键不进 Shoot,朋友盒的复制入口在 Shoot 链上,黑枪因此不会被复制
        public override bool CanShoot(Player player) => player.altFunctionUse != 2;

        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextFloat() >= AmmoSaveChance;

        public override bool? UseItem(Player player) {
            if (player.altFunctionUse != 2)
                return null;
            if (player.whoAmI == Main.myPlayer && player.ItemAnimationJustStarted)
                Sacrifice(player);
            return true;
        }

        private void Sacrifice(Player player) {
            int cost = LifeCost(player);
            player.statLife -= cost;
            CombatText.NewText(player.Hitbox, CombatText.DamagedFriendly, cost);
            player.Entropy().SyncLife();
            player.AddBuff(ModContent.BuffType<RequiemFracture>(), FractureTime);
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<RequiemBlackGun>(), player.GetWeaponDamage(Item), player.GetWeaponKnockback(Item), player.whoAmI);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            //朋友盒副本调用时 twinSpawnIndex 不为 -1,副本与主射同一发,不推进循环
            bool copy = player.Entropy().twinSpawnIndex != -1;
            bool charger = ChargerSlots[player.GetModPlayer<RequiemPlayer>().TakeCycleSlot(!copy)];
            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 up = aim.RotatedBy(-MathHelper.PiOver2 * player.direction) * player.gravDir;
            Vector2 muzzle = position + aim * MuzzleFromPivot.X - up * MuzzleFromPivot.Y;
            if (!Collision.CanHit(position, 0, 0, muzzle, 0, 0))
                muzzle = position;
            float deviation = MathHelper.ToRadians(Main.rand.NextFloat(SpreadMinDegrees, SpreadMaxDegrees)) * (Main.rand.NextBool() ? 1f : -1f);
            Vector2 dir = aim.RotatedBy(deviation);
            int projType = charger ? ModContent.ProjectileType<RequiemCharger>() : ModContent.ProjectileType<RequiemPiercer>();
            Projectile.NewProjectile(source, muzzle, dir * (charger ? ChargerSpeed : PiercerSpeed), projType, damage, knockback, player.whoAmI);
            return false;
        }

        /// <summary>后坐跟着物品贴图走,各端都会执行;枪口火光由子弹首帧生成</summary>
        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            if (player.altFunctionUse == 2)
                return;
            Vector2 aim = player.itemRotation.ToRotationVector2() * player.direction;
            float k = player.itemAnimationMax > 0 ? player.itemAnimation / (float)player.itemAnimationMax : 0f;
            player.itemLocation -= aim * (RecoilPixels * k * k);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Handgun)
                .AddIngredient<SupernovaGel>(GelCost)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }
    }
}
