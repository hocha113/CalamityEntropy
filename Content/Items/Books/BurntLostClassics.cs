using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books
{
    public class BurntLostClassics : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 33;
            Item.useAnimation = Item.useTime = 22;
            Item.crit = 5;
            Item.mana = 9;
            Item.shootSpeed = 15;
            Item.ArmorPenetration = 20;
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BLC")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<BurntLostClassicsHeld>();
        public override int SlotCount => 3;

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<RedemptionBible>()
                .AddIngredient<DarkScripture>()
                .AddIngredient(ItemID.BrokenHeroSword)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class BurntLostClassicsHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/BurntLostClassics/BurntLostClassicsOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/BurntLostClassics/BurntLostClassicsPage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/BurntLostClassics/BurntLostClassicsUI";

        public override float randomShootRotMax => 0.16f;
        public override int baseProjectileType => ModContent.ProjectileType<BurntBrimShot>();
        public override bool Shoot()
        {
            base.Shoot();
            base.Shoot();
            return base.Shoot();
        }
        public override EBookProjectileEffect getEffect()
        {
            return new BLCBookBaseEffect();
        }
        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            m.Size += 0.25f;
            return m;
        }
    }

    public class BLCBookBaseEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            //完整限定名会被入口类同名遮蔽(CalamityEntropy 先解析为类),改经 global:: 前缀
            target.AddBuff(ModContent.BuffType<global::CalamityEntropy.Content.Buffs.PortsDoT.BrimstoneFlames>(), 320);
        }
    }

    public class BurntBrimShot : EBookBaseProjectile
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.Resize(18, 18);
            Projectile.ignoreWater = true;
            Projectile.scale *= 1f;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.X != 0 && Projectile.velocity.X == 0)
            {
                Projectile.velocity.X = oldVelocity.X * -1;
            }
            if (oldVelocity.Y != 0 && Projectile.velocity.Y == 0)
            {
                Projectile.velocity.Y = oldVelocity.Y * -1f;
            }
            if (Main.rand.NextBool(3))
            {
                Projectile.penetrate -= 1;
            }
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            return false;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        //残影三态贴图,加载期就位,不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/Invisible")]
        internal static Asset<Texture2D> InvisibleTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/DrizzlefishFire")]
        internal static Asset<Texture2D> FireTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/DrizzlefishFire2")]
        internal static Asset<Texture2D> FireTex2;
        public int Time;
        public override void AI()
        {
            base.AI();
            Time++;
            Player player = Main.player[base.Projectile.owner];

            //每帧5颗尾烟,位置沿速度随机分布;timeleftmax/Lifetime跟旧Smoke初始化器一致
            for (float i = 0; i <= 1; i += 0.2f)
            {
                var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * Main.rand.NextFloat(), CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.02f, 0.04f));
                p.timeleftmax = 26;
                p.Lifetime = 26;
                p.Configure(0.5f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 26);
            }

            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);
            Projectile.rotation += 0.5f * (float)Projectile.direction;
            Projectile.velocity.Y += float.Min(0.6f, Time * 0.004f);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Time < 7)
            {
                CEUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1, InvisibleTex.Value);
            }
            else if (Projectile.ai[1] == 1f)
            {
                CEUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1, FireTex2.Value);
            }
            else
            {
                CEUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1, FireTex.Value);
            }

            if (Projectile.ai[1] == 1f)
            {
                Texture2D value = FireTex2.Value;
                Main.spriteBatch.Draw(value, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 16, 16), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2((float)value.Width / 2f, 10f), Projectile.scale, SpriteEffects.None, 0f);
                return false;
            }

            return true;
        }
    }
}
