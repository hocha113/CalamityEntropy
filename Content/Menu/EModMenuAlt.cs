using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Menu
{
    public class EModMenuAlt : ModMenu
    {
        public int counter = 0;
        public override Asset<Texture2D> SunTexture => ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white");
        public override Asset<Texture2D> MoonTexture => ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white");
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/startmenu");
        public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/Logo");
        public override string DisplayName => Mod.GetLocalization("Menu.Church").Value;
        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor) {
            Main.time = 27000;
            Main.dayTime = true;
            Texture2D l1 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/1").Value;
            Texture2D l2 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/2").Value;
            Texture2D l3 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/3").Value;
            Texture2D l4 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/4").Value;
            Texture2D l5 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/5").Value;
            Texture2D l6 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/6").Value;
            Texture2D l7 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/7").Value;
            Texture2D l8 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/8").Value;
            Texture2D l9 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/9").Value;
            Texture2D l10 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/10").Value;
            Texture2D l11 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/menu2/11").Value;
            Texture2D pixel = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white").Value;
            drawColor = Color.White;
            float fl = (float)Math.Sin(counter * 0.01f);
            float xoffset = (float)Math.Cos(counter * 0.004f);

            counter++;
            logoScale = 0.8f;
            logoRotation = 0;
            logoDrawCenter += new Vector2(0, (float)Math.Cos(counter * 0.02f) * 18 + 8);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), new Color(1, 2, 32));

            spriteBatch.Draw(l1, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), null, Color.White, 0, l1.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);

            spriteBatch.Draw(l5, new Vector2(Main.screenWidth / 2 - xoffset * 2, Main.screenHeight / 2 + fl * 8), null, Color.White, 0, l1.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);
            spriteBatch.Draw(l6, new Vector2(Main.screenWidth / 2 - xoffset * 2, Main.screenHeight / 2 + fl * 8), null, Color.White, 0, l1.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);

            spriteBatch.Draw(l3, new Vector2(Main.screenWidth / 2 - xoffset * 4, Main.screenHeight / 2 - fl * 14), null, Color.White, 0, l1.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);
            spriteBatch.Draw(l4, new Vector2(Main.screenWidth / 2 - xoffset * 4, Main.screenHeight / 2 - fl * 14), null, Color.White, 0, l1.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);
            Vector2 pa1 = new Vector2(-xoffset * 4, -fl * 14) + new Vector2(-340, -84) * (Main.screenWidth / 1920f) + Main.ScreenSize.ToVector2() / 2f;
            Vector2 pa2 = new Vector2(-xoffset * 6, fl * 18) + new Vector2(-260, 0) * (Main.screenWidth / 1920f) + Main.ScreenSize.ToVector2() / 2f;
            Vector2 pb1 = new Vector2(-xoffset * 4, -fl * 14) + new Vector2(314, -80) * (Main.screenWidth / 1920f) + Main.ScreenSize.ToVector2() / 2f;
            Vector2 pb2 = new Vector2(-xoffset * 6, fl * 18) + new Vector2(120, 0) * (Main.screenWidth / 1920f) + Main.ScreenSize.ToVector2() / 2f;

            spriteBatch.Draw(l10, pa1, null, Color.White, (pa2 - pa1).ToRotation(), l10.Size() / 2 * new Vector2(0, 1), CEUtils.getDistance(pa1, pa2) / l10.Width, SpriteEffects.None, 0);
            spriteBatch.Draw(l11, pb1, null, Color.White, (pb2 - pb1).ToRotation(), l10.Size() / 2 * new Vector2(0, 1), CEUtils.getDistance(pb1, pb2) / l11.Width, SpriteEffects.FlipVertically, 0);

            spriteBatch.Draw(l7, new Vector2(Main.screenWidth / 2 - xoffset * 6, Main.screenHeight / 2 + fl * 18), null, Color.White, 0, l1.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);
            spriteBatch.Draw(l8, new Vector2(Main.screenWidth / 2 - xoffset * 6, Main.screenHeight / 2 + fl * 18), null, Color.White, 0, l1.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);
            spriteBatch.Draw(l9, new Vector2(Main.screenWidth / 2 - xoffset * 6, Main.screenHeight / 2 + fl * 18), null, Color.White, 0, l1.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);


            spriteBatch.Draw(l2, new Vector2(Main.screenWidth / 2 + xoffset * 32, Main.screenHeight / 2), null, Color.White, 0, l2.Size() / 2, Main.ScreenSize.ToVector2() / l1.Size(), SpriteEffects.None, 0);

            Texture2D logo = Logo.Value;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            for (int i = 0; i < 6; i++) {
                var prt = new EMenuAltParticle() { pos = new Vector2(Main.rand.NextFloat(-400, Main.screenWidth), -20), vel = new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(0, 5)) };
                prt.vel = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f) + 7, 28);
                particles.Add(prt);
            }
            Texture2D ptex = CEUtils.getExtraTex("Circle");
            foreach (var p in particles) {
                p.Update();
                Main.spriteBatch.Draw(ptex, p.pos, new Rectangle(0, 0, ptex.Width / 2, ptex.Height), new Color(30, 30, 120) * p.alpha, p.vel.ToRotation(), ptex.Size() / 2f, new Vector2(100, 2) * p.scale * p.alpha * 0.004f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(ptex, p.pos, new Rectangle(ptex.Width / 2, 0, ptex.Width / 2, ptex.Height), new Color(30, 30, 120) * p.alpha, p.vel.ToRotation(), new Vector2(0, ptex.Height / 2), new Vector2(8, 2) * p.scale * p.alpha * 0.004f, SpriteEffects.None, 0);
            }
            Texture2D noise = CEUtils.getExtraTex("TransverseTwill");
            float rotn = 1.3258f;
            Main.spriteBatch.Draw(noise, new Vector2(Main.screenWidth / 2, -500), new Rectangle((int)(Main.GlobalTimeWrappedHourly * -400), (int)(Main.GlobalTimeWrappedHourly * 32), Main.screenWidth, Main.screenHeight * 2), new Color(3, 3, 7), rotn, new Vector2(0, Main.screenHeight), 2, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(noise, new Vector2(Main.screenWidth / 2, -500), new Rectangle((int)(Main.GlobalTimeWrappedHourly * -230), (int)(Main.GlobalTimeWrappedHourly * -32), Main.screenWidth, Main.screenHeight * 2), new Color(3, 3, 7), rotn, new Vector2(0, Main.screenHeight), 2, SpriteEffects.None, 0);
            for (int i = particles.Count - 1; i >= 0; i--) {
                if (particles[i].alpha <= 0 || particles[i].pos.X > Main.screenWidth + 20) {
                    particles.RemoveAt(i);
                }
            }
            for (int i = 1; i < 19; i += 3) {
                float rot = counter * 0.008f;
                for (int j = 0; j < 16; j++) {
                    spriteBatch.Draw(ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/Logool").Value, logoDrawCenter + rot.ToRotationVector2() * ((float)i * 0.5f), null, Color.LightBlue * 0.15f, logoRotation, logo.Size() / 2, logoScale, SpriteEffects.None, 0);
                    rot += MathHelper.ToRadians(22.5f);
                }
            }



            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            for (int i = 1; i < 10; i++) {
                float rot = 0;
                for (int j = 0; j < 8; j++) {
                    spriteBatch.Draw(ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/Logool").Value, logoDrawCenter + rot.ToRotationVector2() * ((float)i), null, Color.LightBlue * 0.15f, logoRotation, logo.Size() / 2, logoScale, SpriteEffects.None, 0);
                    rot += MathHelper.ToRadians(45);
                }
            }


            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            spriteBatch.Draw(logo, logoDrawCenter, null, Color.White, logoRotation, logo.Size() / 2, logoScale, SpriteEffects.None, 0);

            return false;
        }
        public class EMenuAltParticle
        {
            public Vector2 pos;
            public Vector2 vel;
            public float alpha = 1;
            public float scale = Main.rand.NextFloat(0.4f, 1.4f);
            public void Update() {
                //vel += new Vector2(0.2f, 0.2f);
                //vel *= new Vector2(0.99f, 0.98f);
                alpha -= 0.005f;
                pos += vel * scale;
            }
        }
        public List<EMenuAltParticle> particles = new();
        public override bool IsAvailable => true;
        public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<MenuBack>();

    }
}