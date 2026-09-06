using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class Typer
    {
        //????????????,???????????
        [VaultLoaden("CalamityEntropy/Assets/Extra/light")]
        internal static Asset<Texture2D> LightTex;
        public string text;
        public DynamicSpriteFont font;
        public float scale;
        public int width;
        public int height;
        public int charCount = 0;
        public int chrcounter = 0;
        public int speed = 2;
        public int lineAddWidth;
        public Color color;
        public SoundStyle? sound = null;
        public int xfloat;
        public int yfloat;
        public bool dispersion = false;
        public List<Color> colorList = new List<Color>();
        public List<Color> lightColorList = new List<Color>();
        public List<Vector2> lightSizeList = new List<Vector2>();
        public int counter = 0;
        public int shake = 0;
        public bool serious;
        public bool drawRect = false;
        public Vector2 position;
        public static List<Typer> activeTypers = null;
        public Typer(string text, DynamicSpriteFont font, float scale = 1, int width = 520, int speed = 2, int lineAddWidth = 0, Color? color = null, bool serious = false)
        {
            this.text = text;
            this.font = font;
            this.scale = scale;
            this.width = width;
            this.speed = speed;
            this.charCount = 0;
            this.counter = 0;
            this.xfloat = 0;
            this.yfloat = 0;
            this.lineAddWidth = lineAddWidth;
            this.chrcounter = 0;
            this.serious = serious;
            if (color != null)
            {
                this.color = (Color)color;
            }
            else
            {
                this.color = Color.White;
            }
            for (int i = 0; i <= this.text.Length; i++)
            {
                this.colorList.Add(this.color);
                this.lightColorList.Add(new Color(0, 0, 0, 0));
                this.lightSizeList.Add(new Vector2(1, 1));
            }
            if (this.lineAddWidth == -1)
            {
                this.lineAddWidth = ((int)((font.MeasureString("* ").X + 14) * this.scale));
            }
        }

        public Typer copy()
        {
            Typer rt = new Typer(this.text, this.font, this.scale, this.width, this.speed, this.lineAddWidth, this.color, this.serious);
            rt.xfloat = this.xfloat;
            rt.yfloat = this.yfloat;
            rt.shake = this.shake;
            rt.dispersion = this.dispersion;
            rt.colorList = this.colorList;
            rt.lightColorList = this.lightColorList;
            rt.lightSizeList = this.lightSizeList;
            rt.charCount = this.charCount;
            rt.counter = this.counter;
            rt.chrcounter = this.chrcounter;
            rt.sound = this.sound;
            return rt;

        }
        public void reset()
        {
            this.charCount = 0;
            this.counter = 0;
            this.colorList.Clear();
            this.lightColorList.Clear();
            this.lightSizeList.Clear();
            for (int i = 0; i <= this.text.Length; i++)
            {
                this.colorList.Add(this.color);
                this.lightColorList.Add(new Color(0, 0, 0, 0));
                this.lightSizeList.Add(new Vector2(1, 1));
            }
        }
        public float heightMult = 0;
        public void update()
        {
            if (this.charCount < this.text.Length)
            {
                if (this.chrcounter == 0 && charCount % 2 == 1)
                {
                    if (this.sound.HasValue)
                    {
                        char char_ = this.text[this.charCount];
                        string noSound = " ，。“”！,.\'\"/<>[]{}!@#$%^&*()+-*";
                        if (!noSound.Contains(char_))
                        {
                            SoundEngine.PlaySound(this.sound.Value);
                        }
                    }
                }
                this.chrcounter += 1;

                if (this.chrcounter >= this.speed)
                {

                    this.chrcounter = 0;
                    this.charCount += 1;

                }
            }
            this.counter++;
        }
        public bool Finish() => charCount >= text.Length;
        public void draw()
        {
            draw(Main.spriteBatch, this.position);
        }
        public void draw(SpriteBatch spriteBatch, Vector2 position)
        {
            if (drawRect)
            {
                heightMult = float.Lerp(heightMult, 1, 0.16f);
                int addw = 18;
                Vector2 center = position + new Vector2(this.width + 2, this.height) / 2f;
                int whadd = 4;
                spriteBatch.Draw(CEUtils.pixelTex, center.getRectCentered(width + addw + whadd, (height + addw + whadd) * heightMult), Color.LightSkyBlue);
                spriteBatch.Draw(CEUtils.pixelTex, center.getRectCentered(width + addw, (height + addw) * heightMult), new Color(18, 18, 30));
                position.Y += (height / 2f) * (1 - heightMult);
            }
            Vector2 posp = new Vector2(0, 0);
            Random random = new Random();
            for (int i = 0; i < this.charCount; i++)
            {
                if (i >= this.text.Length)
                {
                    return;
                }
                if (!(this.text[i] == '\n'))
                {
                    Vector2 px = new Vector2(random.Next(-this.shake, this.shake + 1), random.Next(-this.shake, this.shake + 1));
                    px += ((float)(this.counter - i * 2) / 8f).ToRotationVector2() * new Vector2(this.xfloat, this.yfloat);
                    if (this.serious)
                    {
                        if (random.Next(0, 340) == 0)
                        {
                            px += new Vector2(random.Next(-2, 3), random.Next(-2, 3));
                        }
                    }
                    Color colordraw = this.colorList[i];

                    Texture2D light = LightTex.Value;
                    Vector2 dsize = new Vector2(this.font.MeasureString(this.text[i].ToString()).X, this.font.MeasureString(this.text[i].ToString()).X);
                    if (this.lightSizeList[i] != Vector2.Zero)
                    {
                        spriteBatch.UseBlendState_UI(BlendState.Additive);
                        spriteBatch.Draw(light, position + posp + new Vector2(dsize.X + 4, dsize.Y / 2), null, this.lightColorList[i], 0, new Vector2(light.Height / 2, light.Height / 2), this.scale * this.lightSizeList[i] / new Vector2(light.Width, light.Height) * 4, SpriteEffects.None, 0);
                        spriteBatch.Draw(light, position + posp + new Vector2(dsize.X + 4, dsize.Y / 2), null, this.lightColorList[i], 0, new Vector2(light.Height / 2, light.Height / 2), this.scale * this.lightSizeList[i] / new Vector2(light.Width, light.Height) * 4, SpriteEffects.None, 0);
                        spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
                    }
                    if (this.dispersion)
                    {
                        spriteBatch.DrawString(this.font, this.text[i].ToString(), px + position + posp - new Vector2(4, 0), new Color((int)colordraw.R, 0, 0, colordraw.A / 3), 0, new Vector2(0, 0), new Vector2(this.scale, this.scale), SpriteEffects.None, 0);
                        spriteBatch.DrawString(this.font, this.text[i].ToString(), px + position + posp, new Color(0, (int)colordraw.G, 0, colordraw.A / 3), 0, new Vector2(0, 0), new Vector2(this.scale, this.scale), SpriteEffects.None, 0);
                        spriteBatch.DrawString(this.font, this.text[i].ToString(), px + position + posp + new Vector2(4, 0), new Color(0, 0, (int)colordraw.B, colordraw.A / 3), 0, new Vector2(0, 0), new Vector2(this.scale, this.scale), SpriteEffects.None, 0);

                    }
                    else
                    {
                        spriteBatch.DrawString(this.font, this.text[i].ToString(), px + position + posp, colordraw, 0, new Vector2(0, 0), new Vector2(this.scale, this.scale), SpriteEffects.None, 0);
                    }
                    posp.X += (this.font.MeasureString(this.text[i].ToString()).X) * this.scale;
                }
                if (posp.X > this.width || this.text[i] == '\n')
                {
                    posp.X = this.lineAddWidth;
                    posp.Y += 36 * this.scale;
                }
            }
            if (drawRect && Finish())
            {
                Vector2 rb = position + new Vector2(width - 6, height * heightMult - 10);
                spriteBatch.DrawString(this.font, ">", rb, Color.LightSkyBlue, 0, new Vector2(0, 0), new Vector2(this.scale, this.scale), SpriteEffects.None, 0);
            }
        }
    }
    public class DialogSystem : ModSystem
    {
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            /*if (CEKeybinds.RuneDashHotKey.JustPressed)
            {
                Typer.activeTypers.Clear();
                List<string> texts = new(){"你为啥跟我直接表白啊？！", "嘎啦game里不是这样！", "你应该多跟我聊天，然后提升我的好感度。偶尔给我送送礼物，然后在那个特殊节日时候跟我有特殊互动。", "最后在某个我内心神秘事件中，向我表白，我同意跟你在一起，然后我给你看我的特殊CG啊。", "你怎么直接上来跟我表白！？嘎啦game里根本不是这样！","我不接受！！"};
                List<int> shakeMap = new() { 2, 2, 1, 0, 2, 3 };
                bool flag = false;
                for (int i = 0; i < texts.Count; i++)
                {
                    string text = texts[i];
                    Typer.activeTypers.Add(new(text, FontAssets.MouseText.Value, 1f, 800, 2, 0, Color.White) { drawRect = true, height = 100, position = Main.ScreenSize.ToVector2() * new Vector2(0.5f, 0.75f) - new Vector2(400, 50), heightMult = flag ? 1 : 0, shake = shakeMap[i] });
                    flag = true;
                }
                Typer.activeTypers[3].colorList[8] = Color.Red;
                Typer.activeTypers[3].colorList[9] = Color.Red;
                Typer.activeTypers[3].colorList[10] = Color.Red;
                Typer.activeTypers[3].colorList[11] = Color.Red;
                Typer.activeTypers[3].colorList[16] = Color.Pink;
                Typer.activeTypers[3].colorList[17] = Color.Pink;

                Typer.activeTypers[3].colorList[24] = Color.BlueViolet;
                Typer.activeTypers[3].colorList[25] = Color.BlueViolet;
                Typer.activeTypers[3].colorList[26] = Color.BlueViolet;

                Typer.activeTypers[3].colorList[36] = Color.Yellow;
                Typer.activeTypers[3].colorList[37] = Color.Yellow;
                Typer.activeTypers[3].colorList[38] = Color.Yellow;
                Typer.activeTypers[3].colorList[39] = Color.Yellow;
            }*/
            MLPrd = Main.mouseLeft;
        }
        public bool MLPrd = false;
    }

}