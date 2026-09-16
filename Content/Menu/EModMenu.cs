using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Menu
{
    public class EModMenu : ModMenu
    {
        public int counter = 0;
        public override Asset<Texture2D> SunTexture => ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white");
        public override Asset<Texture2D> MoonTexture => ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white");
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/startmenu");
        public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/Logo");
        public override string DisplayName => Mod.GetLocalization("Menu.Vortex").Value;
        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor) {
            Main.time = 27000;
            Main.dayTime = true;
            Texture2D mask = CEUtils.getExtraTex("menumask");
            Texture2D l1 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/VoidVortex").Value;
            Texture2D l2 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/layer2").Value;
            Texture2D l3 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/layer3").Value;
            Texture2D l4 = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/menu/layer4").Value;
            Texture2D pixel = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white").Value;
            drawColor = Color.White;
            counter++;
            logoScale = 0.8f;
            logoRotation = 0;
            logoDrawCenter += new Vector2(0, (float)Math.Cos(counter * 0.02f) * 24 + 8);
            spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), new Color(1, 2, 32));

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            spriteBatch.Draw(l1, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), null, Color.White * 0.92f, MathHelper.ToRadians(counter * 0.04f), l1.Size() / 2, 0.8f, SpriteEffects.None, 0);
            spriteBatch.Draw(l1, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), null, Color.White * 0.94f, MathHelper.ToRadians(counter * 0.08f), l1.Size() / 2, 0.6f, SpriteEffects.None, 0);
            spriteBatch.Draw(l1, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), null, Color.White * 0.96f, MathHelper.ToRadians(counter * 0.14f), l1.Size() / 2, 0.4f, SpriteEffects.None, 0);
            spriteBatch.Draw(l1, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), null, Color.White * 0.98f, MathHelper.ToRadians(counter * 0.22f), l1.Size() / 2, 0.2f, SpriteEffects.None, 0);
            spriteBatch.Draw(l1, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), null, Color.White * 1f, MathHelper.ToRadians(counter * 0.34f), l1.Size() / 2, 0.1f, SpriteEffects.None, 0);
            spriteBatch.Draw(l1, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), null, Color.White * 1f, MathHelper.ToRadians(counter * 0.42f), l1.Size() / 2, 0.045f, SpriteEffects.None, 0);
            spriteBatch.Draw(l1, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), null, Color.White * 1f, MathHelper.ToRadians(counter * 0.46f), l1.Size() / 2, 0.02f, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            spriteBatch.Draw(mask, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * 0.6f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            if (Main.rand.NextBool(8)) {
                MenuParticle particle = new MenuParticle(new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), (Main.rand.Next(6) / 6f * MathHelper.TwoPi).ToRotationVector2() * 1, new Vector2(1.5f, 1), 660);
                MenuParticle.particles.Add(particle);
                particle.pos += particle.velocity * 2;
            }
            if (counter % 35 == 0) {
                MenuParticle particle = new MenuParticle(new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), CEUtils.randomRot().ToRotationVector2() * 1, new Vector2(1.5f, 1), 660);
                MenuParticle.particles.Add(particle);
                particle.pos += particle.velocity * 2;
            }
            if (Main.rand.NextBool(120)) {

                LightningParticle.lightningParticles.Add(new LightningParticle());
            }
            foreach (MenuParticle p in MenuParticle.particles) {
                p.update();
                p.draw();
            }
            for (int i = MenuParticle.particles.Count - 1; i >= 0; i--) {
                if (MenuParticle.particles[i].timeleft <= 0) {
                    MenuParticle.particles.RemoveAt(i);
                }
            }
            foreach (LightningParticle p in LightningParticle.lightningParticles) {
                p.draw();
            }
            for (int i = LightningParticle.lightningParticles.Count - 1; i >= 0; i--) {
                if (LightningParticle.lightningParticles[i].timeleft <= 0) {
                    LightningParticle.lightningParticles.RemoveAt(i);
                }
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            spriteBatch.Draw(l4, Main.ScreenSize.ToVector2() / 2 + new Vector2(-280, 70), null, Color.Black, 0, l4.Size() / 2, 1, SpriteEffects.None, 0);


            spriteBatch.Draw(l2, new Rectangle(0, (int)((float)Math.Cos(counter * 0.01f) * 42), Main.screenWidth, Main.screenHeight), Color.White * (0.7f + (float)Math.Cos(counter * 0.01f) * 0.1f));

            spriteBatch.Draw(l3, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * (0.7f + (float)Math.Cos(counter * 0.01f) * 0.1f));

            Texture2D logo = Logo.Value;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            for (int i = 1; i < 19; i += 3) {
                float rot = counter * 0.008f;
                for (int j = 0; j < 16; j++) {
                    spriteBatch.Draw(ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/Logool").Value, logoDrawCenter + rot.ToRotationVector2() * ((float)i), null, Color.LightBlue * 0.15f, logoRotation, logo.Size() / 2, logoScale, SpriteEffects.None, 0);
                    rot += MathHelper.ToRadians(22.5f);
                }
            }


            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            spriteBatch.Draw(logo, logoDrawCenter, null, Color.White, logoRotation, logo.Size() / 2, logoScale, SpriteEffects.None, 0);

            return false;
        }

        public override bool IsAvailable => true;
        public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<MenuBack>();


    }

    public class MenuParticle
    {
        public Vector2 pos;
        public Vector2 center;
        public Vector2 velocity;
        public Vector2 size;
        public float timeleft;
        public float alpha = 0f;
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRot = new List<float>();
        public MenuParticle(Vector2 pos, Vector2 center, Vector2 vel, Vector2 size, float time) {
            this.center = center;
            this.pos = center + vel.RotatedBy(-MathHelper.PiOver2).normalize() * 660;
            dist = 660;
            this.velocity = vel;
            this.size = size * Main.rand.NextFloat(0.4f, 0.64f);
            this.timeleft = 600;
            rot = vel.ToRotation();
        }
        public Vector2 lastPos = Vector2.Zero;
        public float dist = 0;
        public float rot = 0;
        public void update() {
            oldPos.Add(pos);
            oldRot.Add(rot);
            if (oldPos.Count > 40) {
                oldPos.RemoveAt(0);
                oldRot.RemoveAt(0);
            }
            if (alpha < 1) {
                alpha += 0.005f;
            }
            timeleft--;
            if (dist < 12)
                if (timeleft > 2)
                    timeleft = 2;
            dist -= Utils.Remap(dist, 0, 660, 1.4f, 0.9f);
            rot += Utils.Remap(dist, 0, 660, 0.015f, 0.003f);
            pos = center + rot.ToRotationVector2() * dist;
            velocity = (pos - lastPos);
            lastPos = pos;
        }

        public void draw() {
            Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/lightball").Value;
            float op = 1;
            if (timeleft < 90) {
                op = float.Min(1, (float)timeleft / 90f);
            }
            op *= alpha;
            for (int i = 0; i < oldPos.Count; i++) {
                Main.spriteBatch.Draw(tx, oldPos[i], null, new Color(170, 180, 255) * op * 0.6f * ((i + 1f) / oldPos.Count), oldRot[i], tx.Size() / 2, this.size * 0.15f * (dist / 660f), SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(tx, pos, null, new Color(170, 180, 255) * op * 0.8f, this.velocity.ToRotation(), tx.Size() / 2, this.size * 0.15f * (dist / 660f), SpriteEffects.None, 0);
        }
        public void draw(float opc) {
            Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/lightball").Value;
            float op = 1;
            if (timeleft < 90) {
                op = (float)timeleft / 90f;
            }
            op *= alpha * opc;
            Main.spriteBatch.Draw(tx, pos, null, Color.AliceBlue * op * 0.8f, this.velocity.ToRotation(), tx.Size() / 2, this.size * 0.15f * (dist / 660f), SpriteEffects.None, 0);
        }
        public static List<MenuParticle> particles = new List<MenuParticle>();
    }

    public class LightningParticle
    {
        public List<Vector2> points = new List<Vector2>();
        public List<Vector2> points2 = new List<Vector2>();
        public static List<LightningParticle> lightningParticles = new List<LightningParticle>();
        public LightningParticle() {
            Vector2 centerp = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) + new Vector2(Main.rand.Next(-60, 61), Main.rand.Next(-60, 61));
            float a1 = CEUtils.randomRot();
            float a2 = a1 + MathHelper.ToRadians(180);
            Vector2 p1 = centerp;
            Vector2 p2 = centerp;
            for (int i = 0; i < 20; i++) {
                points.Add(p1);
                points2.Add(p2);
                a1 += ((float)Main.rand.NextDouble() - 0.5f) * 1f;
                a2 += ((float)Main.rand.NextDouble() - 0.5f) * 1f;
                p1 += a1.ToRotationVector2() * Main.rand.Next(8, 26);
                p2 += a2.ToRotationVector2() * Main.rand.Next(8, 26);
            }
        }

        public int timeleft = 20;

        public void draw() {

            timeleft--;
            Texture2D px = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white").Value;

            float jd = 1;
            float lw = 2f * ((float)timeleft / 20f);
            Color color = Color.White;
            for (int i = 1; i < points.Count; i++) {
                Vector2 jv = points[i] - points[i - 1];
                jv.Normalize();
                jv *= 2;
                CEUtils.drawLine(Main.spriteBatch, px, points[i - 1], points[i] + jv, color * jd, 2f * lw, 0, false);
                lw -= 2f * ((float)timeleft / 20f) / ((float)points.Count + 1);
            }

            jd = 1;
            lw = 2f * ((float)timeleft / 20f);
            for (int i = 1; i < points2.Count; i++) {
                Vector2 jv = points2[i] - points2[i - 1];
                jv.Normalize();
                jv *= 2;
                CEUtils.drawLine(Main.spriteBatch, px, points2[i - 1], points2[i] + jv, color * jd, 2f * lw, 0, false);
                lw -= 2f * ((float)timeleft / 20f) / ((float)points2.Count + 1);
            }
        }
    }
}