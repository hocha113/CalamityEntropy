using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityEntropy.Content.Items.Books
{
    public class AzafureCylinder : EntropyBook, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 17;
            Item.mana = 7;
            Item.rare = ModContent.RarityType<AzafureOrange>();
        }
        public override int HeldProjectileType => ModContent.ProjectileType<AzafureCylinderHeld>();
        public override int SlotCount => 2;
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/Azafure")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(10)
                .AddIngredient(ItemID.HellstoneBar, 10)
                .AddIngredient(ItemID.ManaCrystal, 10)
                .AddTile(TileID.Anvils)
                .Register();

        }
    }

    public class AzafureCylinderHeld : EntropyBookHeldProjectile
    {
        public override Texture2D[] OpenAnimations()
        {
            return null;
        }
        public override Texture2D[] PageAnimations()
        {
            return null;
        }
        public override void playTurnPageAnimation()
        {
        }
        //UI 开书动画只有一帧,贴图加载期就位
        [VaultLoaden("CalamityEntropy/Content/Items/Books/Textures/AzafureCylinder/AzafureCylinderUI")]
        internal static Asset<Texture2D> UIOpenTex;
        public override Texture2D[] UIOpenAnimations()
        {
            return new Texture2D[] { UIOpenTex.Value };
        }
        public override int baseProjectileType => ModContent.ProjectileType<MetalBall>();
        public int frC = 0;
        public override int frameChange => 1;
        public override void AI()
        {
            base.AI();
            Offset *= 0.92f;
            if (active)
            {
                frC++;
                if (frC > frameChange)
                {
                    frC = 0;
                    Projectile.frame++;
                    if (Projectile.frame > 8)
                    {
                        Projectile.frame = 0;
                    }
                }
            }
            if (UIOpen)
                Offset = 0;
        }
        public override bool Shoot()
        {
            Offset -= 6;
            return base.Shoot();
        }
        public float Offset = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = getTexture();
            int frameCount = 9;
            int frame = Projectile.frame;
            Rectangle? rect = new Rectangle(0, texture.Height / frameCount * frame, texture.Width, texture.Height / frameCount - 2);

            if (UIOpen)
                rect = null;
            Vector2 origin = UIOpen ? texture.Size() / 2f : new Vector2(texture.Width / 2, (texture.Height / frameCount - 2) / 2);
            float rotAdd = UIOpen ? 0 : MathHelper.PiOver2;
            Main.EntitySpriteDraw(texture, Projectile.Center + Projectile.rotation.ToRotationVector2() * Offset + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition, rect, lightColor, Projectile.rotation + rotAdd, origin, Projectile.scale, (Projectile.velocity.X > 0 || UIOpen ? SpriteEffects.None : SpriteEffects.FlipVertically), 0);
            return false;
        }
        public override Texture2D getTexture()
        {
            if (UIOpen)
                return UIOpenAnimations()[0];
            return Projectile.GetTexture();
        }
        public override string Texture => "CalamityEntropy/Content/Items/Books/AzafureCylinderHeld";

    }
    public class MetalBall : EBookBaseProjectile
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = Projectile.height = 13;
            Projectile.tileCollide = true;
            gravity = 0.8f;
            Projectile.timeLeft = 8 * 60;
        }
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {

            if (oldVelocity.X != 0 && Projectile.velocity.X == 0)
            {
                Projectile.velocity.X = oldVelocity.X * -0.5f;
            }
            if (oldVelocity.Y != 0 && Projectile.velocity.Y == 0)
            {
                Projectile.velocity.Y = oldVelocity.Y * -0.5f;
                Projectile.velocity.X *= 0.85f;
            }
            if (oldVelocity.Length() > 4)
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f }, Projectile.Center);
            return false;
        }
        public PRT_TrailParticle trail = null;
        public override void AI()
        {
            if (Projectile.localAI[2]++ == 0)
            {
                if (Projectile.GetOwner().AzafureEnhance())
                    Projectile.timeLeft *= 2;
                CEUtils.PlaySound("aprclaunch", Main.rand.NextFloat(2, 2.4f), Projectile.Center);
            }
            if (trail == null)
            {
                //PRT_TrailParticle maxLength=9短尾,ShouldDraw=false每帧AddPoint自己画
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Color.OrangeRed, 0.4f);
                trail.maxLength = 9;
                trail.ShouldDraw = false;
                trail.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
            }
            base.AI();
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (!n.friendly && !n.dontTakeDamage && n.Hitbox.Intersects(Projectile.Center.getRectCentered(140, 140)))
                {
                    Projectile.Kill();
                }
            }
            trail.Lifetime = 26;
            trail.AddPoint(Projectile.Center + Projectile.velocity);
            CEUtils.AddLight(Projectile.Center, new Color(150, 10, 30));
            int max = 6 + (Projectile.GetOwner().AzafureEnhance() ? 4 : 0);
            Projectile toKill = null;
            int timeleft = 99999;
            int sum = 0;
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (proj.type == Projectile.type && proj.owner == Projectile.owner)
                {
                    sum++;
                    if (proj.timeLeft < timeleft)
                    {
                        timeleft = proj.timeLeft;
                        toKill = proj;
                    }
                }
            }
            if (toKill != null && sum > max)
                toKill.Kill();
        }
        public override void OnKill(int timeLeft)
        {
            CEUtils.PlaySound("explosionbig", 1.6f, Projectile.Center, 8, 0.26f);
            CEUtils.PlaySound("pulseBlast", 0.8f, Projectile.Center, 8, 0.46f);
            if (Main.myPlayer == Projectile.owner)
                ((EntropyBookHeldProjectile)ShooterModProjectile).ShootSingleProjectile(ModContent.ProjectileType<AzafureMagicBlast>(), Projectile.Center, Vector2.Zero, 1, 1, 0);
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            if (trail != null) trail.DrawTrail(Main.spriteBatch);
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            for (int i = 0; i < 1; i++)
            {
                List<Vector2> lol = new List<Vector2>();
                for (int ii = 0; ii < 8; ii++)
                {
                    lol.Add(Projectile.Center + CEUtils.randomPointInCircle(16));
                }
                CEUtils.DrawLines(lol, Color.Red * 0.65f, 2);
            }
            return false;
        }
    }
}
