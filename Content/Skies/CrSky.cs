using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.AbyssalWraith;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class CrSky : CustomSky
    {
        private bool skyActive;
        private float opacity;


        public override void Deactivate(params object[] args)
        {
            skyActive = Main.LocalPlayer.Entropy().crSky > 0;
        }

        public override void Reset()
        {
            skyActive = false;
        }

        public override bool IsActive()
        {
            return skyActive || opacity > 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            skyActive = true;
        }
        public static Effect shader;
        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(inColor, new Color(255, 255, 255, inColor.A), opacity);
        }
        public int counter = 0;
        public int awtime = 0;
        public Effect skyEffect = null; public Effect skyEffect2 = null;
        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (this.skyEffect == null)
            {
                this.skyEffect = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/AWSkyEffect", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                this.skyEffect2 = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/awsky2", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            counter++;
            Texture2D txd = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/CrSky").Value;
            float pc = 1f;
            /*
            if (SubworldSystem.IsActive<VOIDSubworld>())
            {
                pc *= 0.2f;
            }*/
            Color ocolor = new Color((int)(30 * pc), (int)(20 * pc), (int)(60 * pc));
            /*if (NPC.AnyNPCs(ModContent.NPCType<AbyssalWraith>()))
            {
                awtime = 180;
                ocolor = new Color((int)(190 * pc), (int)(55 * pc), (int)(205 * pc));
                foreach (NPC n in Main.npc)
                {
                    if (n.active && n.type == ModContent.NPCType<AbyssalWraith>())
                    {
                        drawAWMask = true;
                        AWIndex = n.whoAmI;
                        break;

                    }
                }
            }*/
            if (awtime >= 0)
                awtime--;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            Vector2 dp = new Vector2((Main.screenPosition.X * -0.5f + counter * 0.3f) % txd.Width, (Main.screenPosition.Y * -0.5f * Main.LocalPlayer.gravDir + counter * -0.1f) % txd.Height);
            spriteBatch.Draw(txd, Main.ScreenSize.ToVector2() * 0.5f, new Rectangle((int)-dp.X, (int)-dp.Y, (int)(Main.screenWidth * 2), (int)(Main.screenHeight * 2)), ocolor * opacity, 0, Main.ScreenSize.ToVector2(), 1, SpriteEffects.None, 0);
            
            if(shader == null)
                shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/ColorLerp2", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            if (awtime > 0)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                ocolor = new Color((int)(36 * pc), (int)(36 * pc), (int)(36 * pc));
                if (Main.rand.NextBool(120))
                {
                    if (Main.rand.NextBool(6))
                    {
                        SoundStyle s = SoundID.Thunder;
                        s.Volume = Main.rand.NextFloat() * 0.4f;
                        SoundEngine.PlaySound(s);
                    }
                    LightningParticle.lightningParticles.Add(new LightningParticle());
                }
                ocolor = new Color((int)(40 * pc), (int)(40 * pc), (int)(40 * pc));

                foreach (LightningParticle p in LightningParticle.lightningParticles)
                {
                    p.draw(opacity);
                }
                for (int i = LightningParticle.lightningParticles.Count - 1; i >= 0; i--)
                {
                    if (LightningParticle.lightningParticles[i].timeleft <= 0)
                    {
                        LightningParticle.lightningParticles.RemoveAt(i);
                    }
                }
            }
            else
            {
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            ocolor = new Color((int)(66 * pc), (int)(60 * pc), (int)(94 * pc));
            float c = 1f;
            for (int i = 1; i <= 2; i++)
            {
                float mc = i % 2 == 0 ? 1 : -1;
                Vector2 vc = (i * 2.9f + 0.8f).ToRotationVector2();
                dp = new Vector2((Main.screenPosition.X * -0.5f * c + counter * vc.X * mc * c) % txd.Width, (Main.screenPosition.Y * -0.5f * Main.LocalPlayer.gravDir * c + counter * vc.Y) % txd.Height);
                spriteBatch.Draw(txd, Main.ScreenSize.ToVector2() * 0.5f, new Rectangle((int)-dp.X, (int)-dp.Y, (int)(Main.screenWidth * 2), (int)(Main.screenHeight * 2)), ocolor * opacity, 0, Main.ScreenSize.ToVector2(), 1.2f, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            spriteBatch.End();

            if(true)//(ModContent.GetInstance<Config>().ScreenWarpEffects)
            {
                var graphicsDevice = Main.graphics.GraphicsDevice;
                graphicsDevice.SetRenderTarget(EffectLoader.Screen0);
                graphicsDevice.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
                Main.spriteBatch.End();

                graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
                graphicsDevice.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                CEUtils.drawLine(new Vector2(-600, Main.screenHeight / 2 / 2), new Vector2(Main.ScreenSize.X + 1200, Main.screenHeight / 2), Color.White * 1f, 3000, 0, false);

                Main.spriteBatch.End();

                graphicsDevice.SetRenderTarget(Main.screenTarget);
                graphicsDevice.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                var fscreen = EffectLoader.fscreenCr;
                fscreen.CurrentTechnique = fscreen.Techniques["Technique1"];
                fscreen.CurrentTechnique.Passes[0].Apply();
                fscreen.Parameters["strengthMult"].SetValue(0.28f * opacity + 0.0001f);
                fscreen.Parameters["screen"].SetValue(Main.screenPosition * 0.5f / Main.ScreenSize.ToVector2() * new Vector2(1, Main.LocalPlayer.gravDir));
                fscreen.Parameters["iTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.03f);
                fscreen.Parameters["coordMult"].SetValue(new Vector2(1f, 1f) / Main.GameViewMatrix.Zoom);
                fscreen.Parameters["cStrength"].SetValue(3.2f);
                fscreen.Parameters["cNum"].SetValue(1.12f);
                graphicsDevice.Textures[0] = EffectLoader.Screen0;
                graphicsDevice.Textures[1] = Main.screenTargetSwap;
                graphicsDevice.Textures[2] = CEUtils.getExtraTex("VoidBack");
                Main.spriteBatch.Draw(EffectLoader.Screen0, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
            }

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }
        public class LightningParticle
        {
            public List<Vector2> points = new List<Vector2>();
            public List<Vector2> points2 = new List<Vector2>();
            public static List<LightningParticle> lightningParticles = new List<LightningParticle>();
            public LightningParticle()
            {
                Vector2 centerp = Main.screenPosition + new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) + new Vector2(Main.rand.Next(-1200, 1201), Main.rand.Next(-1200, 1201));
                float a1 = CEUtils.randomRot();
                float a2 = a1 + MathHelper.ToRadians(180);
                Vector2 p1 = centerp;
                Vector2 p2 = centerp;
                for (int i = 0; i < 20; i++)
                {
                    points.Add(p1);
                    points2.Add(p2);
                    a1 += ((float)Main.rand.NextDouble() - 0.5f) * 1f;
                    a2 += ((float)Main.rand.NextDouble() - 0.5f) * 1f;
                    p1 += a1.ToRotationVector2() * Main.rand.Next(50, 66);
                    p2 += a2.ToRotationVector2() * Main.rand.Next(50, 66);
                }
            }

            public int timeleft = 200;
            public int maxTime = 200;
            public float opc = 0;
            public void draw(float op)
            {
                opc = op;
                timeleft--;
                List<Vector2> pointsDraw = new List<Vector2>();

                for (int i = points.Count - 1; i >= 0; i--)
                {
                    pointsDraw.Add(points[i]);
                }

                for (int i = 0; i < points2.Count; i++)
                {
                    pointsDraw.Add(points2[i]);
                }
                Main.spriteBatch.EnterShaderRegion();
                GameShaders.Misc["CalamityMod:ArtAttack"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/Streak2"));
                GameShaders.Misc["CalamityMod:ArtAttack"].Apply();
                PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityMod:ArtAttack"]), 180);
                Main.spriteBatch.ExitShaderRegion();
                Main.spriteBatch.UseBlendState(BlendState.Additive);

            }
            public Color TrailColor(float completionRatio, Vector2 vertex)
            {
                Color result = Color.Lerp(Color.MediumPurple, Color.LightBlue, new Vector2(1, 0).RotatedBy(completionRatio * MathHelper.Pi).Y) * completionRatio * (opc * 1.4f);
                return result;
            }

            public float TrailWidth(float completionRatio, Vector2 vertex)
            {
                return 48 * new Vector2(1, 0).RotatedBy((timeleft / (float)maxTime) * MathHelper.Pi).Y * new Vector2(1, 0).RotatedBy(completionRatio * MathHelper.Pi).Y;
            }
        }


        public override void Update(GameTime gameTime)
        {
            if (Main.LocalPlayer.Entropy().crSky <= 0 || Main.gameMenu)
                skyActive = false;

            if (skyActive && opacity < 1f)
                opacity += 0.02f;
            else if (!skyActive && opacity > 0f)
                opacity -= 0.02f;

            Opacity = opacity;
        }

        public override float GetCloudAlpha()
        {
            return (1f - opacity) * 0.97f + 0.03f;
        }
    }
}
