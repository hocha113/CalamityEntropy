using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class EndlessAbyssHoldout : ModProjectile
    {
        public override void SetStaticDefaults() {
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }

        public override void AI() {
            Projectile.frameCounter++;
            Player owner = Projectile.owner.ToPlayer();
            if (owner.channel)
                Projectile.damage = owner.GetWeaponDamage(owner.HeldItem);
            if (Projectile.ai[0] == 0) {
                if (Main.myPlayer == Projectile.owner) {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<EndlessAbyssLaser>(), Projectile.damage, Projectile.knockBack, Projectile.owner).ToProj().DamageType = Projectile.DamageType;
                }
            }
            if (Projectile.ai[0]++ > 16 && (Projectile.ai[0] % 14 == 0)) {
                if (owner.CheckMana(30, true)) {
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedByRandom((Projectile.ai[0] % 30 == 8 ? 0.15f : 0.3f)) * 1.4f, ModContent.ProjectileType<VoidStarF>(), Projectile.damage / 4, Projectile.knockBack, Projectile.owner, 0, 0, 1).ToProj().DamageType = Projectile.DamageType;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedByRandom((Projectile.ai[0] % 30 == 8 ? 0.15f : 0.3f)) * 1.4f, ModContent.ProjectileType<VoidStarF>(), Projectile.damage / 4, Projectile.knockBack, Projectile.owner, 0, 0, 1).ToProj().DamageType = Projectile.DamageType;
                    }
                    if (!Main.dedServ) {
                        CEUtils.PlaySound("soulshine", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center, 8, 0.4f);
                        SpawnHoldoutSparkle(Color.IndianRed * 0.6f, Color.IndianRed);
                    }
                }
                else {
                    Projectile.Kill();
                }
            }
            if (!owner.channel) {
                Projectile.Kill();
                return;
            }
            else {
                Projectile.timeLeft = 3;
            }
            if (Main.myPlayer == Projectile.owner) {
                Vector2 nv = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.One) * 16;
                if (nv != Projectile.velocity) {
                    Projectile.netUpdate = true;
                }
                Projectile.velocity = nv;

            }
            if (Projectile.velocity.X > 0) {
                Projectile.direction = 1;
                owner.direction = 1;
            }
            else {
                Projectile.direction = -1;
                owner.direction = -1;
            }
            Player player = Projectile.owner.ToPlayer();
            Projectile.Center = owner.MountedCenter + player.gfxOffY * Vector2.UnitY + Projectile.velocity.SafeNormalize(Vector2.Zero) * 20;
            owner.itemRotation = Projectile.rotation * Projectile.direction;
            Projectile.rotation = Projectile.velocity.ToRotation();
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 12;
            owner.itemAnimation = 12;
            if (Projectile.velocity.X > 0) {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public float counter = 0;
        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            lightColor = Color.White;
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 52 * ((Projectile.frameCounter / 2) % 4), 58, 50), lightColor, Projectile.rotation + MathHelper.ToRadians(90), new Vector2(58, 50) / 2, Projectile.scale, SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D light = CEExtraAssets.Glow;
            Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition * Projectile.scale, null, Color.MediumVioletRed * 0.7f, 0, light.Size() / 2, 0.5f * Projectile.scale * (1 + (float)Math.Cos((counter) * 0.02f) * 0.2f), SpriteEffects.None, 0);

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            return false;
        }

        void SpawnHoldoutSparkle(Color color, Color bloom) {
            float sparkleScale = 0.28f * Projectile.scale;
            //PRT_SparkleCal bloom/color在Configure里,旧Calamity SparkleParticle两色构造
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, color, sparkleScale)
                .Configure(bloom, 17, Main.rand.NextFloat(-0.1f, 0.1f), 1.2f);  //holdout装饰sparkle,旧版粒子系统迁过来的,数值没动
        }
    }

}
