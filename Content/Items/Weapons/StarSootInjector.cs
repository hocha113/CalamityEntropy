using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class StarSootInjector : ModItem
    {
        public override void SetDefaults() {
            // 弹药线接通：useAmmo 指向星辉鳞尘（ammo=自身、consumable=true 已在材料侧补齐）；
            // 鳞尘 shoot=0，发射物与伤害仍取本武器（PickAmmo 回落武器 shoot，弹药加伤为 0）
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<StarSootSmoke>(), ModContent.ItemType<StarlitScaleDust>(), singleShotTime: 16, shotVelocity: 18, hasAutoReuse: true);
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;
            Item.width = 152;
            Item.height = 48;
            Item.useTime = 7;
            Item.damage = 32;
            Item.knockBack = 4f;
            Item.crit = 10;
            Item.UseSound = SoundID.Item34;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ModContent.RarityType<Lunarblight>();
        }

        public override Vector2? HoldoutOffset() {
            return new Vector2(-8f, 0f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI).ToProj();
            return false;
        }
        public override void HoldItem(Player player) {
            if (player.itemTime > 0) {
                //PRT_GlowLightParticle 光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                //旧NewParticle第5参0.6~1.4是Scale,迁移时误放进Configure的Opacity槽,这里还原
                var p = PRTLoader.NewParticle<PRT_GlowLightParticle>(player.MountedCenter, (player.itemRotation + (player.direction > 0 ? 0 : MathHelper.Pi)).ToRotationVector2().RotatedByRandom(0.24f) * Main.rand.NextFloat(10, 20) * 2.5f, Color.LightBlue, Main.rand.NextFloat(0.6f, 1.4f));
                p.lightColor = Color.LightBlue * 0.55f;
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);
                for (int i = 0; i < 3; i++) {
                    //Smoke timeleft直赋+Configure Additive,跟GlowLight成对
                    var smoke = PRTLoader.NewParticle<PRT_Smoke>(player.MountedCenter, (player.itemRotation + (player.direction > 0 ? 0 : MathHelper.Pi)).ToRotationVector2().RotatedByRandom(0.24f) * Main.rand.NextFloat(10, 20) * 2.5f, Color.LightBlue, 1);
                    smoke.timeleftmax = 30;
                    smoke.Lifetime = 30;
                    smoke.scaleStart = 0f;
                    smoke.scaleEnd = Main.rand.NextFloat(0.6f, 1.4f) * 0.6f;
                    smoke.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);
                }
            }
        }
    }
    public class StarSootSmoke : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            Projectile.MaxUpdates = 2;
            Projectile.Opacity = 0;
        }
        public override void AI() {
            int s = (int)(Utils.Remap(Projectile.timeLeft, 40, 0, 10, 128));
            Projectile.Resize(s, s);
        }

    }
}
