using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.StarmoonShark
{
    public class StarmoonShark : ModItem
    {
        public const int BaseDamage = 32;           //10 次/秒 × 3 发,配 +17 的弹药约 (32 + 17) × 30 ≈ 1470/秒
        public const int UseTime = 6;               //帧/次
        public const int BulletsPerShot = 3;
        public const float SpreadDegrees = 2f;      //度,每发各自随机偏转的上限
        public const float AmmoSaveChance = 0.75f;
        public const float ShootSpeed = 14f;        //像素/帧
        public const float Knockback = 1.5f;
        public const float MuzzleOffset = 60f;      //像素,子弹出生点沿枪管前移
        public const float RecoilPixels = 3f;       //像素,按本次使用进度的平方衰减
        public static readonly Vector2 Holdout = new(-12f, -2f);           //像素,旋转点在贴图 (12, 19)
        public static readonly Vector2 MuzzleFromPivot = new(78f, 4f);    //像素,枪口在贴图 (90, 23)
        public const int EssenceCost = 10;          //配方聚星之灵
        public const int SellGold = 25;

        public override void SetDefaults() {
            Item.width = 88;
            Item.height = 34;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = UseTime;
            Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item40 with { Volume = 0.55f, PitchVariance = 0.12f, MaxInstances = 3 };
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = ShootSpeed;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override Vector2? HoldoutOffset() => Holdout;

        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextFloat() >= AmmoSaveChance;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 muzzle = position + velocity.SafeNormalize(Vector2.UnitX) * MuzzleOffset;
            if (!Collision.CanHit(position, 0, 0, muzzle, 0, 0))
                muzzle = position;
            float spread = MathHelper.ToRadians(SpreadDegrees);
            int starred = Main.rand.Next(BulletsPerShot);
            for (int i = 0; i < BulletsPerShot; i++) {
                Vector2 v = velocity.RotatedByRandom(spread);
                int index = Projectile.NewProjectile(source, muzzle, v, type, damage, knockback, player.whoAmI);
                //每次射击挑一发挂星光拖尾,即三分之一的子弹
                if (i == starred && index >= 0 && index < Main.maxProjectiles) {
                    Projectile bullet = Main.projectile[index];
                    Projectile.NewProjectile(source, muzzle, v, ModContent.ProjectileType<StarmoonSharkStarTrail>(), 0, 0f, player.whoAmI, bullet.identity, bullet.type);
                }
            }
            return false;
        }

        /// <summary>后坐与枪口火光跟着物品贴图走,各端都会执行</summary>
        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            Vector2 aim = player.itemRotation.ToRotationVector2() * player.direction;
            float k = player.itemAnimationMax > 0 ? player.itemAnimation / (float)player.itemAnimationMax : 0f;
            player.itemLocation -= aim * (RecoilPixels * k * k);
            if (Main.dedServ || player.itemAnimation != player.itemAnimationMax) {
                return;
            }
            //原版枪械的旋转点在 itemLocation 下方半个贴图高再加 Holdout.Y
            Vector2 pivot = player.itemLocation + new Vector2(0f, heldItemFrame.Height * 0.5f + Holdout.Y * player.gravDir);
            Vector2 muzzle = pivot + new Vector2(MuzzleFromPivot.X * player.direction, MuzzleFromPivot.Y * player.gravDir).RotatedBy(player.itemRotation);
            MuzzleFlash(player, muzzle, aim);
        }

        private static void MuzzleFlash(Player player, Vector2 muzzle, Vector2 aim) {
            PRT_ShineParticle flash = PRTLoader.NewParticle<PRT_ShineParticle>(muzzle, player.velocity, SageVfx.StarWhite, 0.14f);
            flash.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, aim.ToRotation(), 6);
            flash.drawScale = new Vector2(2.4f, 0.8f);
            for (int i = 0; i < 2; i++) {
                Color c = Color.Lerp(SageVfx.StarBlue, SageVfx.StarWhite, Main.rand.NextFloat());
                Vector2 v = aim.RotatedByRandom(0.3f) * Main.rand.NextFloat(6f, 12f) + player.velocity;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(muzzle, v, c, Main.rand.NextFloat(0.03f, 0.045f)).Configure(false, 8, new Vector2(2.4f, 0.7f), true);
            }
            Lighting.AddLight(muzzle, SageVfx.StarBlue.ToVector3() * 0.7f);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Megashark)
                .AddIngredient<WraithSoulEssence>(EssenceCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
