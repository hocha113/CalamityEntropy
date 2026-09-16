using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AzafureFurnaceHoldout : ModProjectile
    {
        //描边贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/AzafureFurnaceOutline")]
        internal static Asset<Texture2D> OutlineTex;
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
        int maxTime;
        public float eRotSpeed = 0;
        public int EAnmTime = -1;
        public LoopSound chargeSnd = null;
        public List<PRT_ShineParticle> ps = new();

        public override void AI() {

            if (!Main.dedServ) {
                if (chargeSnd == null) {
                    chargeSnd = new LoopSound(CalamityEntropy.ofCharge);
                    chargeSnd.instance.Pitch = 0;
                    chargeSnd.instance.Volume = 0;
                    chargeSnd.play();
                }
                chargeSnd.setVolume_Dist(Projectile.Center, 100, 700, (Projectile.ai[0] / maxTime) * 0.46f);
                chargeSnd.instance.Pitch = Projectile.ai[0] / maxTime;
                if (EAnmTime == -1) {
                    chargeSnd.timeleft = 3;
                }
            }

            Player owner = Projectile.owner.ToPlayer();
            owner.heldProj = Projectile.whoAmI;
            if (Projectile.velocity.X > 0) {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            if (EAnmTime > 0) {
                EAnmTime--;
                if (EAnmTime <= 0) {
                    Projectile.Kill();
                }
                Projectile.Center = owner.MountedCenter + owner.gfxOffY * Vector2.UnitY;
                Projectile.rotation += eRotSpeed;
                eRotSpeed *= 0.85f;
                owner.itemTime = 2;
                owner.itemAnimation = 2;
                return;
            }
            if (!owner.channel) {
                if (Projectile.ai[0] > 16) {
                    CEUtils.PlaySound("ofshoot", 1, Projectile.Center);
                    owner.velocity += Projectile.velocity.normalize() * -6f * owner.Entropy().GetPressure();
                    if (Main.myPlayer == Projectile.owner) {
                        CEUtils.SetShake(Projectile.Center, 4);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 64 * Projectile.scale, Projectile.velocity.SafeNormalize(Vector2.Zero) * 5f, ModContent.ProjectileType<AzafureFurnaceEnergyBall>(), (int)(Projectile.damage * (Projectile.ai[0] >= maxTime ? 5f : 1)), Projectile.knockBack, Projectile.owner, 0, (Projectile.ai[0] >= maxTime ? 1 : 0));
                    }
                    eRotSpeed = owner.direction * -0.3f;
                    EAnmTime = 32;
                }
                else {
                    EAnmTime = 1;
                }
                return;
            }
            Color lightColor = Color.White;
            if (Projectile.ai[0] >= maxTime) {
                lightColor = Color.Lerp(Color.White, Color.Red, 0.5f + (float)Math.Cos(counter * 0.16f) * 0.5f);
                if (Main.rand.NextBool(8)) {
                    //EMediumSmoke holdout蒸汽,Configure尾参+PRT/EParticle系
                    PRTLoader.NewParticle<PRT_EMediumSmoke>(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 64 * Projectile.scale, new Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-2, -6)), Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()), Main.rand.NextFloat(0.8f, 1.4f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());
                }
            }
            Vector2 sPos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 64 * Projectile.scale + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(64 * Projectile.scale, 64 * Projectile.scale);
            var __prt = PRTLoader.NewParticle<PRT_ULineParticle>(sPos, (Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 64 * Projectile.scale - sPos) * 0.18f, lightColor, 0.4f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
            __prt.spd = 0.064f;
            __prt.w2 = 0.85f;
            __prt.w1 = 0.8f;
            __prt.len = 4;

            maxTime = 60;

            if (Projectile.ai[0] < maxTime) {
                Projectile.ai[0] += 1 * owner.GetAttackSpeed(DamageClass.Magic) * (1 + owner.Entropy().WeaponBoost * 0.6f + owner.AzafureDurability() * 0.6f);
                if (Projectile.ai[0] >= maxTime) {
                    SoundStyle s = SoundID.DD2_BetsyFireballShot;
                    SoundEngine.PlaySound(s, Projectile.Center);
                    float a = 0;
                    for (int i = 0; i < 36; i++) {
                        a += MathHelper.ToRadians(10);
                        Dust.NewDust(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 60 * Projectile.scale, 1, 1, DustID.Smoke, a.ToRotationVector2().X * 0.4f, a.ToRotationVector2().Y * 0.4f);
                        //ULineParticle furnace/lunar holdout装饰线,旧EParticle ULine
                        var __prt2 = PRTLoader.NewParticle<PRT_ULineParticle>(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 64 * Projectile.scale, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(7, 16), Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0, 1)), 1).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
                        __prt2.spd = 0.064f;
                        __prt2.w2 = 0.85f;
                        __prt2.w1 = 0.8f;
                        __prt2.len = 4;
                    }
                    ps.Add(PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 60 * Projectile.scale, Vector2.Zero, Color.White, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12));
                    ps.Add(PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 60 * Projectile.scale, Vector2.Zero, Color.OrangeRed, 0.9f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12));
                }
            }
            foreach (var pt in ps) {
                pt.Position = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 60 * Projectile.scale;
            }
            if (Main.myPlayer == Projectile.owner) {
                Vector2 nv = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.One);
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
            Player player = owner;
            Projectile.Center = owner.MountedCenter + player.gfxOffY * Vector2.UnitY;
            owner.itemRotation = Projectile.rotation * Projectile.direction;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(5);
            owner.itemTime = 2;
            owner.itemAnimation = 2;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public float counter = 0;
        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            lightColor = Color.White;
            if (Projectile.ai[0] >= maxTime) {
                lightColor = Color.Lerp(lightColor, Color.Red, 0.5f + ((float)Math.Sin((++counter) * 0.16f) * 0.5f));
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<Vector2> v2ss = new List<Vector2>() { new Vector2(-2, -2), new Vector2(-2, 2), new Vector2(2, -2), new Vector2(2, 2), new Vector2(2, 0), new Vector2(-2, 0), new Vector2(0, 2), new Vector2(0, -2) };
            Texture2D to = OutlineTex.Value;
            if (EAnmTime == -1) {
                foreach (Vector2 v in v2ss) {
                    Main.EntitySpriteDraw(to, Projectile.Center - Main.screenPosition + v, null, lightColor * (Projectile.ai[0] / (float)maxTime) * (Projectile.ai[0] >= maxTime ? 1f : 0.95f), Projectile.rotation + MathHelper.ToRadians(45), new Vector2(0, texture.Height), Projectile.scale, SpriteEffects.None);
                }
            }
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.ToRadians(45), new Vector2(0, texture.Height), Projectile.scale, SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (EAnmTime == -1) {
                Texture2D light = CEExtraAssets.Glow;
                Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(0.1f) * 60 * Projectile.scale * Projectile.scale, null, (Projectile.ai[0] >= maxTime ? Color.Lerp(Color.White, Color.Red, (0.5f + (float)Math.Cos((counter) * 0.1f) * 0.5f)) : Color.White) * (Projectile.ai[0] / (float)maxTime), 0, light.Size() / 2, 0.2f * Projectile.scale * (1 + (float)Math.Cos((counter) * 0.1f) * 0.2f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(light, Projectile.Center - Main.screenPosition + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(0.1f) * 60 * Projectile.scale * Projectile.scale, null, (Projectile.ai[0] >= maxTime ? Color.Lerp(Color.White, Color.Red, (0.5f + (float)Math.Cos((counter) * 0.1f) * 0.5f)) : Color.White) * (Projectile.ai[0] / (float)maxTime), 0, light.Size() / 2, 0.2f * Projectile.scale * (1 + (float)Math.Cos((counter) * 0.1f) * 0.2f), SpriteEffects.None, 0);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            return false;
        }
    }

}