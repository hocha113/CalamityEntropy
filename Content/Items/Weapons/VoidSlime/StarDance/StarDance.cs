using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Tiles;
using InnoVault.PRT;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarDance
{
    public class StarDance : ModItem
    {
        #region 调参旋钮
        //面板伤害、使用时间(帧)、法力、击退;每次约8倍面板,单体约2160/秒(任务文件建议值)
        public const int BaseDamage = 90;
        public const int UseFrames = 20;
        public const int ManaCost = 14;
        public const float Knockback = 3f;
        //只决定原版瞄准向量的长度,各色球的出手速度在各自类里
        public const float ShootSpeed = 10f;
        public const int SellGold = 36;
        //配方:法术书1、超新星凝胶8、聚星之灵20(策划)
        public const int GelCount = 8;
        public const int EssenceCount = 20;
        //各色伤害倍率(策划)
        public const float RedMult = 0.75f;
        public const float SilverMult = 1f;
        public const float YellowMult = 2f;
        public const float OrangeMult = 2f;
        public const float BlueMult = 0.4f;
        //银球一次3颗;橙、蓝相对黄球方向的偏角(度);均为策划
        public const int SilverCount = 3;
        public const float OrangeSpreadDeg = 10f;
        public const float BlueSpreadDeg = 20f;
        //黄、橙、蓝的出手点在书前方的距离(像素)
        public const float MuzzleReach = 20f;
        //出手闪光:书前方距离(像素)、星芒缩放与寿命(帧);五色火星速度(像素/帧)、缩放与寿命(帧)
        public const float FlashReach = 24f;
        public const float FlashScale = 0.55f;
        public const int FlashLife = 14;
        public const float SparkSpeed = 2.4f;
        public const float SparkScale = 0.3f;
        public const int SparkLife = 18;
        #endregion

        public override void SetDefaults() {
            Item.width = 34;
            Item.height = 38;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = Knockback;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item9 with { Volume = 0.8f, PitchVariance = 0.15f, MaxInstances = 3 };
            Item.shoot = ModContent.ProjectileType<StarDanceOrbYellow>();
            Item.shootSpeed = ShootSpeed;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        //一次放出五组:红1、银3、黄1、橙2、蓝2
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 aim = (Main.MouseWorld - position).SafeNormalize(velocity.SafeNormalize(Vector2.UnitX * player.direction));
            float aimRot = aim.ToRotation();
            Vector2 muzzle = position + aim * MuzzleReach;
            int owner = player.whoAmI;
            Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<StarDanceOrbRed>(), (int)(damage * RedMult), knockback, owner, aimRot);
            for (int i = 0; i < SilverCount; i++) {
                Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<StarDanceOrbSilver>(), (int)(damage * SilverMult), knockback, owner, aimRot + MathHelper.TwoPi * i / SilverCount);
            }
            Projectile.NewProjectile(source, muzzle, aim * StarDanceOrbYellow.StartSpeed, ModContent.ProjectileType<StarDanceOrbYellow>(), (int)(damage * YellowMult), knockback, owner);
            for (int side = -1; side <= 1; side += 2) {
                Vector2 orange = aim.RotatedBy(MathHelper.ToRadians(OrangeSpreadDeg) * side) * StarDanceOrbOrange.Speed;
                Projectile.NewProjectile(source, muzzle, orange, ModContent.ProjectileType<StarDanceOrbOrange>(), (int)(damage * OrangeMult), knockback, owner);
                Vector2 blue = aim.RotatedBy(MathHelper.ToRadians(BlueSpreadDeg) * side) * StarDanceOrbBlue.LaunchSpeed;
                Projectile.NewProjectile(source, muzzle, blue, ModContent.ProjectileType<StarDanceOrbBlue>(), (int)(damage * BlueMult), knockback, owner);
            }
            return false;
        }

        //每次使用的第一帧在书前闪一下,五色火星各一颗;各端都会跑到这里
        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            if (Main.dedServ || player.itemAnimation != player.itemAnimationMax - 1)
                return;
            Vector2 dir = player.itemRotation.ToRotationVector2() * player.direction;
            Vector2 tip = player.RotatedRelativePoint(player.MountedCenter) + dir * FlashReach;
            PRTLoader.NewParticle<PRT_SparkleCal>(tip, Vector2.Zero, Color.White, FlashScale).Configure(StarDanceOrbBlue.RimColor, FlashLife, 0f, 1.2f);
            Color[] colors = { StarDanceOrbRed.BodyColor, StarDanceOrbOrange.BodyColor, StarDanceOrbYellow.BodyColor, StarDanceOrbSilver.RimColor, StarDanceOrbBlue.BodyColor };
            for (int i = 0; i < colors.Length; i++) {
                Vector2 vel = dir.RotatedBy(MathHelper.Lerp(-1f, 1f, i / (colors.Length - 1f))) * SparkSpeed;
                PRTLoader.NewParticle<PRT_Light>(tip, vel, colors[i], SparkScale).Configure(1f, lifetime: SparkLife);
            }
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.SpellTome)
                .AddIngredient<SupernovaGel>(GelCount)
                .AddIngredient<WraithSoulEssence>(EssenceCount)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }
    }
}
