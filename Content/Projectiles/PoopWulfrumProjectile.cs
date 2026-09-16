using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopWulfrumProjectile : PoopProj
    {
        public int shield = 100;
        public static int shieldDistance = 200;
        public int shieldMax = 100;
        public int shieldCd = 0;
        public int lastTickShield = 100;
        public override void SendExtraAI(BinaryWriter writer) {
            base.SendExtraAI(writer);
            writer.Write(shield);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            base.ReceiveExtraAI(reader);
            shield = reader.ReadInt32();
        }
        public override void AI() {
            base.AI();
            if (shield > 0) {
                shieldCd = 10 * 60;
                if (Projectile.timeLeft % 16 == 0) {
                    if (shield < shieldMax) {
                        shield += 1;
                    }
                }
                if (opc < 1) {
                    opc += 0.05f;
                }
            }
            else {
                shield = 0;
                shieldCd--;
                if (shieldCd <= 0) {
                    shield = shieldMax;
                }
                if (opc > 0) {
                    opc -= 0.05f;
                }
            }
            if (shield < lastTickShield) {
                SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/RoverDriveHit") { PitchVariance = 0.6f, Volume = 0.6f, MaxInstances = 0 }, Projectile.Center);
            }
            lastTickShield = shield;
        }
        public override bool BreakWhenHitNPC => false;
        float opc = 1;
        public override void PostDraw(Color lightColor) {
            if (shield <= 0) {
                return;
            }
            SpriteBatch spriteBatch = Main.spriteBatch;
            bool shieldExists = shield > 0;


            float scale = 0.8f * opc;


            float shieldStrength = (0.1f + 0.5f * ((float)shield / 100f)) * opc;


            float noiseScale = MathHelper.Lerp(0.4f, 0.8f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);

            Effect shieldEffect = Filters.Scene["CalamityEntropy:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            float baseShieldOpacity = (0.2f + 0.8f * ((float)shield / 100f)) * opc;
            shieldEffect.Parameters["shieldOpacity"].SetValue(baseShieldOpacity * (0.5f + 0.5f * shieldStrength));
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(2f);

            Color blueTint = new Color(51, 102, 255);
            Color cyanTint = new Color(71, 202, 255);
            Color wulfGreen = new Color(194, 255, 67) * 0.8f;
            Color edgeColor = CEUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, blueTint, cyanTint, wulfGreen);
            Color shieldColor = blueTint;


            shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.GameViewMatrix.TransformationMatrix);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D tex = NoiseTex.Value;
            spriteBatch.Draw(tex, pos, null, Color.White, 0, tex.Size() / 2f, scale, 0, 0);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

        }
        //原先在绘制里 ??= 惰性请求,改为加载期就位
        [VaultLoaden("CalamityEntropy/Assets/Extra/Ports/TechyNoise")]
        public static Asset<Texture2D> NoiseTex;
        public override int dustType => DustID.Poop;
    }

}