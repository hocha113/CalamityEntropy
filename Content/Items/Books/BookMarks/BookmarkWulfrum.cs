using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkWulfrum : BookMark
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(gold: 5);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Wulfrum");
        public override EBookProjectileEffect getEffect()
        {
            return new BMWulfrumEffect();
        }

        public override Color tooltipColor => new Color(160, 170, 120);
        public override void AddRecipes()
        {
            // 铁锭用配方组兼容铅锭世界
            CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 4)
                .AddCalOrOwn(CEID.Item_EnergyCore, ModContent.ItemType<AzafureCircuitry>())
                .AddIngredient(ItemID.FallenStar, 2)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public class BMWulfrumEffect : EBookProjectileEffect
    {
        public override void BookUpdate(Projectile projectile, bool ownerClient)
        {
            if (ownerClient && CECooldowns.CheckCD("WulfrumBMDrone", 160))
            {
                if (projectile.ModProjectile is EntropyBookHeldProjectile eb)
                    eb.ShootSingleProjectile(ModContent.ProjectileType<WulfrumBMDrone>(), projectile.Center, projectile.rotation.ToRotationVector2(), 1, 1, 0.5f, (proj) => { proj.damage = proj.damage.Softlimitation(90); });
            }
        }
    }
    public class WulfrumBMDrone : EBookBaseProjectile
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = -1;
            Projectile.localNPCHitCooldown = 16;
            Projectile.timeLeft = 480;
            Projectile.tileCollide = true;
            Projectile.width = Projectile.height = 20;
            Projectile.light = 0.4f;
        }
        public override void ApplyHoming()
        {
            if (Projectile.localAI[1] > 100)
                base.ApplyHoming();
        }

        public override void AI()
        {
            base.AI();
            Projectile.localAI[1]++;
            if (Projectile.localAI[1] < 100)
            {
                if (Projectile.localAI[1] % 10 == 0)
                {
                    if (Projectile.owner == Main.myPlayer)
                    {
                        var rt = CEUtils.randomRot().ToRotationVector2();
                        NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 900);
                        if (target != null)
                            rt = (target.Center - Projectile.Center).normalize().RotatedByRandom(0.2f);

                        ((EntropyBookHeldProjectile)ShooterModProjectile).ShootSingleProjectile(ModContent.ProjectileType<WulfrumScrapProj>(), Projectile.Center, rt, 0.1f, 1, 0.65f, (proj) => { proj.damage = proj.damage.Softlimitation(16); });
                    }
                    CEUtils.PlaySound("wulfrumShoot", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
                }
                Projectile.velocity.Y *= 0.97f;
                Projectile.velocity.X *= (Math.Abs(Projectile.velocity.X) > 6) ? 0.99f : 1.01f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else
            {
                Projectile.penetrate = 1;
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.X * 0.16f + (Projectile.velocity.X < 0 ? MathHelper.Pi : 0), 0.06f, false);
                Projectile.velocity.Y += 0.3f;
            }
        }
        public override void OnKill(int timeLeft)
        {
            ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.Zero, 12), Projectile.Distance(Main.LocalPlayer.Center));
            if (Main.zenithWorld)
            {
                CEUtils.ExplotionParticleLOL(Projectile.Center);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                CEUtils.PlaySound("pulseBlast", 0.8f, Projectile.Center, 6, 0.26f);
                //Wulfrum核爆三颗PRT,PulseRing+ShineParticle,旧spawn参数照抄
                PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(1.5f, 8);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick, 3.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 2.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);

            }
            for (int i = 0; i < 16; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Stone);
            if (Projectile.owner == Main.myPlayer)
            {
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage * 2, 160, Projectile.DamageType);
            }
            for (int i = 0; i < 32; i++)
            {
                var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Firework_Yellow);
                d.scale = 0.8f;
                d.velocity = CEUtils.randomPointInCircle(14);
                d.position += d.velocity * 4;
            }
            if (Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < 12; i++)
                {
                    ((EntropyBookHeldProjectile)ShooterModProjectile).ShootSingleProjectile(ModContent.ProjectileType<WulfrumScrapProj>(), Projectile.Center, CEUtils.randomRot().ToRotationVector2(), 0.25f, 1, Main.rand.NextFloat(0.3f, 0.54f), (proj) => { proj.tileCollide = false; proj.damage = proj.damage.Softlimitation(16); });
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            return false;
        }
    }
    public class WulfrumScrapProj : EBookBaseProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = 1;
            Projectile.localNPCHitCooldown = 16;
            Projectile.timeLeft = 480;
            Projectile.tileCollide = true;
            Projectile.width = Projectile.height = 16;
        }

        public override void AI()
        {
            base.AI();

            if (Projectile.localAI[2]++ > 6)
            {
                Projectile.velocity.Y += 0.3f;
                Projectile.velocity *= 0.998f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.01f;
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 16; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Stone);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // 原灾厄废铁贴图改画铁锭 (与配方替换一致)
            Main.instance.LoadItem(ItemID.IronBar);
            Texture2D tex = TextureAssets.Item[ItemID.IronBar].Value;
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, tex));
            return false;
        }
    }
}