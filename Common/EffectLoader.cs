using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Donator.Ratziel;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.AbyssalWraith;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.AbyssalWraithProjs;
using CalamityEntropy.Content.Projectiles.Chainsaw;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Projectiles.Pets.Abyss;
using CalamityEntropy.Content.Projectiles.Prophet;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Content.UI.EntropyBookUI;
using InnoVault;
using InnoVault.PRT;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using static CalamityEntropy.CalamityEntropy;

namespace CalamityEntropy.Common
{
    [VaultLoaden("CalamityEntropy/Assets/Effects/")]
    internal class EffectLoader : RenderHandle
    {
        [VaultLoaden("CalamityEntropy/Assets/Extra/cvmask")]
        private static Asset<Texture2D> cvmask;
        [VaultLoaden("CalamityEntropy/Assets/Extra/StarrySky")]
        private static Asset<Texture2D> planetarium_blue_base;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/CruiserSlash")]
        private static Asset<Texture2D> cruiserSlash;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Cruiser/CruiserBlackholeBullet")]
        private static Asset<Texture2D> cruiserBlackholeBullet;
        [VaultLoaden("CalamityEntropy/Assets/Extra/ksc1")]
        private static Asset<Texture2D> ksc1;
        [VaultLoaden("CalamityEntropy/Assets/Extra/shockwave")]
        private static Asset<Texture2D> shockwave;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Cruiser/VoidStar")]
        private static Asset<Texture2D> voidStar;
        //以下为本批次补充的私有资产字段;类上挂了目录级标签,新字段必须带字段级标签,否则会按字段名去 Effects 目录找资源
        [VaultLoaden("CalamityEntropy/Assets/Extra/HollowCircleMask")]
        private static Asset<Texture2D> HollowCircleMaskTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/shield")]
        private static Asset<Texture2D> ShieldTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/cvdt")]
        private static Asset<Texture2D> CvdtTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/BlurryPerlinNoise")]
        private static Asset<Texture2D> BlurryPerlinNoiseTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/AwSky1")]
        private static Asset<Texture2D> AwSky1Tex;
        [VaultLoaden("CalamityEntropy/Assets/Effects/Pixel", AssetMode.EffectValue, "Pixel")]
        private static Effect PixelShader;
        [VaultLoaden("CalamityEntropy/Assets/Effects/blur", AssetMode.EffectValue, "P0")]
        private static Effect BlurShader;
        public static Asset<Effect> PowerSFShader;
        public static Asset<Effect> KnifeRendering;
        public static Asset<Effect> StarsTrail;
        public static Asset<Effect> RTShader;
        public static Asset<Effect> WarpShader;
        //以下这批 .fx 的 pass 名不是「文件名+Pass」,必须显式指明,
        //否则 VaultLoaden 自动注册的 Filters.Scene 项会带一个不存在的 pass 名
        [VaultLoaden("CalamityEntropy/Assets/Effects/Cylinder", AssetMode.Effects, "P0")]
        public static Asset<Effect> Cylinder;
        [VaultLoaden("CalamityEntropy/Assets/Effects/kscreen", AssetMode.EffectValue, "kscreen")]
        public static Effect kscreen;
        [VaultLoaden("CalamityEntropy/Assets/Effects/fscreen", AssetMode.EffectValue, "fscreen")]
        public static Effect fscreen;
        [VaultLoaden("CalamityEntropy/Assets/Effects/kscreen2", AssetMode.EffectValue, "kscreen2")]
        public static Effect kscreen2;
        [VaultLoaden("CalamityEntropy/Assets/Effects/cvoid", AssetMode.EffectValue, "cvoid")]
        public static Effect cvoid;
        [VaultLoaden("CalamityEntropy/Assets/Effects/cvoid2", AssetMode.EffectValue, "cvoid")]
        public static Effect cvoid2;
        [VaultLoaden("CalamityEntropy/Assets/Effects/cvoid3", AssetMode.EffectValue, "cvoid")]
        public static Effect cvoid3;
        [VaultLoaden("CalamityEntropy/Assets/Effects/cabyss", AssetMode.EffectValue, "cabyss")]
        public static Effect cabyss;
        [VaultLoaden("CalamityEntropy/Assets/Effects/cblood", AssetMode.EffectValue, "cabyss")]
        public static Effect cblood;
        internal static float twistStrength = 0f;
        public const string AssetPath = "CalamityEntropy/Assets/";
        public const string AssetPath2 = "Assets/";
        public const int MaxScreenSlot = 4;
        [VaultLoaden("CalamityEntropy/Assets/Effects/Outline", AssetMode.EffectValue, "Pass1")]
        public static Effect OutlineShader
        {
            get
            {
                return EBookUI.shader;
            }
            set
            {
                EBookUI.shader = value;
            }
        }
        public override int ScreenSlot => MaxScreenSlot;
        public static EffectLoader This { get; private set; }
        public static RenderTarget2D Screen0 => This.ScreenTargets[0];
        public static RenderTarget2D Screen1 => This.ScreenTargets[1];
        public static RenderTarget2D Screen2 => This.ScreenTargets[2];
        public static RenderTarget2D Screen3 => This.ScreenTargets[3];
        /// <summary>
        /// 如果没有必要，尽量避免直接访问这个屏幕中间值，可能会影响到与其他模组的交互效果，推荐在<see cref="EndCaptureDraw"/> 通过参数 screenSwap 使用它
        /// </summary>
        public static RenderTarget2D StaticScreenSwap => RenderHandleLoader.ScreenSwap;

        public override void Load() => This = this;


        public override void EndCaptureDraw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) => CE_EffectHandler(graphicsDevice);

        //首先纹理在使用前尽量缓存为静态的，Request函数并非性能的最佳选择，尤其是在每帧调用甚至循环调用中的高频访问
        //这不是最佳的选择，要我说EndCapture就应该去死，该他妈的沉没在历史的粪坑中。万物都有自己的道理唯独它没有
        //如果有机会，我会把Red绑上十字架然后用白磷火刑慢慢的把他净化，神皇会赞许我的行为的，因为那帮家伙全他妈的是异端邪祟
        //----HoCha113 2025-5-6
        private static void CE_EffectHandler(GraphicsDevice graphicsDevice)
        {
            if (!Main.gameMenu && ModContent.GetInstance<Config>().EnablePixelEffect)
            {
                //初始化
                InitializeEffectHandler();

                //绘制初始屏幕
                DrawInitialScreen(graphicsDevice);
                //绘制投射物特效
                DrawProjectileEffects(graphicsDevice);

                //绘制粒子效果
                DrawParticleEffects(graphicsDevice);   //虚空粒子shape4走RT合成,不进常规PRT桶

                //应用背景着色器
                ApplyBackgroundShader(graphicsDevice);

                DrawNonPixVoidEffects(graphicsDevice);

                //深渊类型Shader
                DrawAbyssalEffect(graphicsDevice);

                DrawBloodEffect(graphicsDevice);

                //我也不知道叫啥的特效 虚寂之翼用了
                DrawRandomEffect(graphicsDevice);
                //准备像素着色器
                PreparePixelShader(graphicsDevice);

                //绘制 NPC 和投射物
                DrawNPCsAndProjectiles(graphicsDevice);

                //应用像素着色器
                ApplyPixelShader(graphicsDevice);

                //绘制玩家和投射物特效
                DrawPlayerAndProjectileEffects(graphicsDevice);

                //绘制切片效果
                DrawSlashEffects(graphicsDevice);

                //绘制区域波动
                DrawFragEffects(graphicsDevice);

                //应用最终着色器
                ApplyFinalShader(graphicsDevice);

                /*PreparePixelShader(Main.graphics.GraphicsDevice);
                Vector2 v = new Vector2(67278, 5867) - Main.screenPosition;
                DrawCylinder(CEUtils.getExtraTex("vine/spr_towery_vines_0"), v, Color.White, 6, 3, 0.5f, Main.GlobalTimeWrappedHourly * 1.4f, startBatch: false);
                ApplyPixelShader(Main.graphics.GraphicsDevice);*/

                //处理屏幕切割效果
                HandleCutScreenEffect(graphicsDevice);

                //绘制黑色遮罩
                DrawBlackMask();

                //旋转屏幕
                DrawScreenRotation(graphicsDevice);
            }
        }
        public static void DrawCylinder(Texture2D tex, Vector2 pos, Color color, BlendState blend, float Height = 1, float scale = 1, float rad = 0.5f, float rot = 0, int tiles = 2, float FullRot = 0, bool inner = false, bool startBatch = true)
        {
            Effect shader = Cylinder.Value;
            if (shader == null)
                return;
            if (!startBatch)
                Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.ZoomMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            shader.Parameters["radius"].SetValue(rad);
            shader.Parameters["rotation"].SetValue(rot);
            shader.Parameters["tileCount"].SetValue(tiles);
            shader.Parameters["innerWall"].SetValue(inner ? 1 : 0);
            Main.spriteBatch.Draw(tex, pos, new Rectangle(0, 0, tex.Width, (int)(tex.Height * Height)), color, FullRot, new Vector2(tex.Width * 0.5f, tex.Height * Height * 0.5f), scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            if (!startBatch)
                Main.spriteBatch.begin_();

        }
        public static float ScreenRotAmp = 0;
        private static void DrawScreenRotation(GraphicsDevice graphicsDevice)
        {
            bool enabled = false;
            if (!ModContent.GetInstance<Config>().ScreenWarpEffects)
            {
                enabled = false;
            }
            if (enabled)
            {
                ScreenRotAmp += (1 - ScreenRotAmp) * 0.01f;
            }
            else
            {
                ScreenRotAmp *= 0.95f;
            }
            if (ScreenRotAmp > 0.001f)
            {
                Texture2D mask = HollowCircleMaskTex.Value;

                float rotation = (float)Math.Sin(Main.GameUpdateCount * 0.02f) * 0.6f;

                graphicsDevice.SetRenderTarget(Screen0);
                graphicsDevice.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
                Main.spriteBatch.End();


                graphicsDevice.SetRenderTarget(Main.screenTarget);
                graphicsDevice.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer);

                Vector2 v = Main.ScreenSize.ToVector2();
                Main.spriteBatch.Draw(Screen0, Main.ScreenSize.ToVector2() / 2 + (v * new Vector2(1, 0)).RotatedBy(rotation), null, Color.White, rotation * (ScreenRotAmp < 0.45f ? 0 : ((ScreenRotAmp - 0.45f) / 0.55f)), (Main.ScreenSize.ToVector2()) * 0.5f, 1, SpriteEffects.FlipHorizontally, 0);
                Main.spriteBatch.Draw(Screen0, Main.ScreenSize.ToVector2() / 2 + (v * new Vector2(-1, 0)).RotatedBy(rotation), null, Color.White, rotation * (ScreenRotAmp < 0.45f ? 0 : ((ScreenRotAmp - 0.45f) / 0.55f)), (Main.ScreenSize.ToVector2()) * 0.5f, 1, SpriteEffects.FlipHorizontally, 0);
                Main.spriteBatch.Draw(Screen0, Main.ScreenSize.ToVector2() / 2 + (v * new Vector2(0, 1)).RotatedBy(rotation), null, Color.White, rotation * (ScreenRotAmp < 0.45f ? 0 : ((ScreenRotAmp - 0.45f) / 0.55f)), (Main.ScreenSize.ToVector2()) * 0.5f, 1, SpriteEffects.FlipVertically, 0);
                Main.spriteBatch.Draw(Screen0, Main.ScreenSize.ToVector2() / 2 + (v * new Vector2(0, -1)).RotatedBy(rotation), null, Color.White, rotation * (ScreenRotAmp < 0.45f ? 0 : ((ScreenRotAmp - 0.45f) / 0.55f)), (Main.ScreenSize.ToVector2()) * 0.5f, 1, SpriteEffects.FlipVertically, 0);

                Main.spriteBatch.Draw(Screen0, Main.ScreenSize.ToVector2() / 2, null, Color.White, rotation * (ScreenRotAmp < 0.45f ? 0 : ((ScreenRotAmp - 0.45f) / 0.55f)), (Main.ScreenSize.ToVector2()) * 0.5f, 1, SpriteEffects.None, 0);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer);
                float a = (ScreenRotAmp < 0.5f) ? (ScreenRotAmp * 2) : 1;
                Main.spriteBatch.Draw(mask, Main.ScreenSize.ToVector2() / 2, null, new Color(210, 255, 255, (int)(255 * a)), 0, mask.Size() / 2f, (Main.GameViewMatrix.Zoom.X * 1.6f + (1 - a) * 2) * 0.75f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
        }

        private static void DrawRandomEffect(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile == null)
                {
                    continue;
                }
                if (p.ModProjectile is AbyssalLaser al)
                {
                    al.drawLaser();
                }
                if (p.ModProjectile is VoidStar)
                {
                    if (p.ai[0] >= 60 || p.ai[2] == 0)
                    {
                        VoidStar mp = (VoidStar)p.ModProjectile;
                        mp.odp.Add(p.Center);
                        if (mp.odp.Count > 2)
                        {
                            float size = 10;
                            float sizej = size / mp.odp.Count;
                            Color cl = new Color(200, 235, 255);
                            for (int i = mp.odp.Count - 1; i >= 1; i--)
                            {
                                CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, mp.odp[i], mp.odp[i - 1], cl * ((255 - p.alpha) / 255f), size * 0.25f);
                                size -= sizej;
                            }
                        }
                        mp.odp.RemoveAt(mp.odp.Count - 1);
                    }

                }
                if (p.ModProjectile is VoidStarF)
                {
                    VoidStarF mp = (VoidStarF)p.ModProjectile;
                    mp.odp.Add(p.Center);
                    if (mp.odp.Count > 2)
                    {
                        float size = 10;
                        float sizej = size / mp.odp.Count;
                        Color cl = new Color(200, 235, 255);
                        if (p.ai[2] > 0)
                        {
                            cl = new Color(255, 160, 160);
                        }
                        for (int i = mp.odp.Count - 1; i >= 1; i--)
                        {
                            CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, mp.odp[i], mp.odp[i - 1], cl * ((255 - p.alpha) / 255f), size * 0.25f);
                            size -= sizej;
                        }
                    }
                    mp.odp.RemoveAt(mp.odp.Count - 1);

                }
                if (p.ModProjectile is LightWisperFlame lwf)
                {
                    lwf.draw();
                }
            }
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (Player p in Main.ActivePlayers)
            {
                if (!p.dead && p.Entropy().MagiShield > 0 && p.Entropy().visualMagiShield)
                {
                    Texture2D shieldTexture = ShieldTex.Value;
                    Main.spriteBatch.Draw(shieldTexture, p.Center - Main.screenPosition, null, new Color(186, 120, 255), 0, shieldTexture.Size() / 2, 0.47f, SpriteEffects.None, 0);
                }
            }
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile != null && p.ModProjectile is MoonlightShieldBreak)
                {
                    Texture2D shieldTexture = ShieldTex.Value;
                    Main.spriteBatch.Draw(shieldTexture, p.Center - Main.screenPosition, null, new Color(186, 120, 255) * p.ai[2], 0, shieldTexture.Size() / 2, 0.47f * (1 + p.ai[1]), SpriteEffects.None, 0);
                }
                if (p.ModProjectile != null && p.ModProjectile is CruiserShadow aw)
                {
                    if (aw.alphaPor > 0)
                    {
                        float s = 0;
                        float sj = 1;
                        for (int i = 0; i <= 30; i++)
                        {
                            aw.DrawPortal(aw.spawnPos, new Color(50, 35, 240) * aw.alphaPor, aw.spawnRot, 270 * s, 0.3f, i * 3f);
                            s = s + (sj - s) * 0.05f;
                        }
                    }
                }
            }
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.ModNPC == null)
                {
                    continue;
                }
                if (n.ModNPC is AbyssalWraith aw)
                {
                    if (aw.portalAlpha > 0)
                    {
                        float s = 0;
                        float sj = 1;
                        for (int i = 0; i <= 30; i++)
                        {
                            aw.DrawPortal(aw.portalPos + new Vector2(0, 220 - i * 2.2f), new Color(50, 35, 240) * aw.portalAlpha, 270 * s, 0.3f, i * 3f);
                            s = s + (sj - s) * 0.05f;
                        }

                        s = 0;
                        sj = 1;
                        for (int i = 0; i <= 30; i++)
                        {
                            aw.DrawPortal(aw.portalTarget + new Vector2(0, 220 - i * 2.2f), new Color(50, 35, 240) * aw.portalAlpha, 270 * s, 0.3f, i * 3f);
                            s = s + (sj - s) * 0.05f;
                        }
                    }
                }
            }
            Main.spriteBatch.End();
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
            cvoid2.CurrentTechnique = cvoid2.Techniques["Technique1"];
            cvoid2.CurrentTechnique.Passes[0].Apply();
            cvoid2.Parameters["tex0"].SetValue(Main.screenTargetSwap);
            cvoid2.Parameters["tex1"].SetValue(CEExtraAssets.VoidBack);
            cvoid2.Parameters["time"].SetValue(Instance.cvcount / 50f);
            cvoid2.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(Instance.cvcount * 1.4f, Instance.cvcount * 1.4f)) / new Vector2(Main.screenWidth, Main.screenHeight));
            Main.spriteBatch.Draw(Screen0, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        private static void DrawBloodEffect(GraphicsDevice graphicsDevice)
        {
            if (!CEUtils.AnyActiveProj<BloodCrack>())
                return;
            bool f = false;
            foreach (var p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile != null && p.ModProjectile is BloodCrack)
                {
                    f = true;
                }
            }
            if (!f)
            {
                return;
            }
            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();


            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.ModProjectile is BloodCrack ac)
                {
                    ac.draw();
                }
            }

            Main.spriteBatch.End();
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            Main.spriteBatch.Draw(Screen0, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            cblood.CurrentTechnique = cblood.Techniques["Technique1"];
            cblood.CurrentTechnique.Passes[0].Apply();
            cblood.Parameters["clr"].SetValue(new Color(100, 0, 0).ToVector4());
            cblood.Parameters["tex1"].SetValue(BlurryPerlinNoiseTex.Value);
            cblood.Parameters["time"].SetValue(Instance.cvcount / 50f);
            cblood.Parameters["scrsize"].SetValue(Screen0.Size());
            cblood.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(Instance.cvcount * 1.4f, Instance.cvcount * 1.4f)) / new Vector2(Main.screenWidth, Main.screenHeight));
            Main.spriteBatch.Draw(Main.screenTargetSwap, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, Main.LocalPlayer.gravDir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);

            Main.spriteBatch.End();
        }
        //PRT_Abyssal mask绘制,粒子枚举在DrawParticleEffectsAlt,和PRT_Void那套RT分流
        private static void DrawAbyssalEffect(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();


            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.ModProjectile is AbyssalCrack ac)
                {
                    ac.draw();
                }
                if (proj.ModProjectile is AbyssalRift ar)
                    ar.draw();
                if (proj.ModProjectile is AbyssBookmarkCrack ac2)
                {
                    ac2.drawVoid();
                }
                if (proj.ModProjectile is NxCrack nc)
                {
                    nc.drawCrack();
                }
                if (proj.ModProjectile is YstralynProj yst)
                {
                    yst.draw_crack();
                }
            }
            DrawParticleEffectsAlt();

            Main.spriteBatch.End();
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            Main.spriteBatch.Draw(Screen0, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            cabyss.CurrentTechnique = cabyss.Techniques["Technique1"];
            cabyss.CurrentTechnique.Passes[0].Apply();
            cabyss.Parameters["clr"].SetValue(new Color(12, 50, 160).ToVector4());
            cabyss.Parameters["tex1"].SetValue(AwSky1Tex.Value);
            cabyss.Parameters["time"].SetValue(Instance.cvcount / 50f);
            cabyss.Parameters["scrsize"].SetValue(Screen0.Size());
            cabyss.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(Instance.cvcount * 1.4f, Instance.cvcount * 1.4f)) / new Vector2(Main.screenWidth, Main.screenHeight));
            Main.spriteBatch.Draw(Main.screenTargetSwap, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, Main.LocalPlayer.gravDir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);

            Main.spriteBatch.End();
        }

        private static void InitializeEffectHandler()
        {
            Instance.screenShakeAmp *= 0.9f;
        }

        private static void DrawInitialScreen(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
        public static void PreparePixelShader(GraphicsDevice graphicsDevice)
        {
            if (ModContent.GetInstance<Config>().EnablePixelEffect)
            {
                DrawInitialScreen(graphicsDevice);
                graphicsDevice.SetRenderTarget(Screen2);   //IPixelPassPRT画进这层,ApplyPixelShader再过Pixel shader
                graphicsDevice.Clear(Color.Transparent);
            }
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.Transform);

        }
        private static void DrawNPCsAndProjectiles(GraphicsDevice graphicsDevice)
        {

            int cruiserEnergyBallType = ModContent.ProjectileType<CruiserEnergyBall>();
            int runeTorrentType = ModContent.ProjectileType<RuneTorrent>();
            int runeTorrentRangerType = ModContent.ProjectileType<RuneTorrentRanger>();
            int prophetVoidSpikeType = ModContent.ProjectileType<ProphetVoidSpike>();

            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.type == cruiserEnergyBallType && proj.ModProjectile is CruiserEnergyBall ceb)
                {
                    ceb.Draw();
                }
                else if (proj.type == runeTorrentType && proj.ModProjectile is RuneTorrent rt)
                {
                    rt.Draw();
                }
                else if (proj.type == runeTorrentRangerType && proj.ModProjectile is RuneTorrentRanger rt_)
                {
                    rt_.Draw();
                }
                else if (proj.type == prophetVoidSpikeType && proj.ModProjectile is ProphetVoidSpike vs)
                {
                    vs.Draw();
                }
                /*if(proj.ModProjectile is OblivionThresherHoldout vt)
                {
                    vt.DrawSaw(Vector2.Zero);
                }
                if (proj.ModProjectile is OblivionThresherShoot vt2)
                {
                    vt2.DrawSaw();
                }
                if (proj.ModProjectile is OblivionThresherShootAlt vt3)
                {
                    vt3.DrawSaw();
                }*/
            }

            DrawPixelPassPRT();   //IPixelPassPRT→Screen2,三桶序见DrawPixelPassPRT
            Main.spriteBatch.End();

            //IAdditivePRT自己管绘制,PRT分桶对不上,攒一批单独开Additive画
            List<IAdditivePRT> prtAdditives = new List<IAdditivePRT>();
            foreach (var prt in PRTLoader.PRT_InGame_World_Inds)
            {
                if (!prt.active || prt.Mod != Instance)
                {
                    continue;
                }
                if (prt is IAdditivePRT additivePRT)
                {
                    prtAdditives.Add(additivePRT);
                }
            }
            if (prtAdditives.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp
                    , DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                foreach (var iaddDrawin in prtAdditives)
                {
                    iaddDrawin.Draw(Main.spriteBatch);
                }
                Main.spriteBatch.End();
            }
        }

        /// <summary>
        /// IPixelPassPRT 画进 Screen2 RT，之后 ApplyPixelShader 过 Pixel shader
        /// 三桶顺序 AlphaBlend → NonPremultiplied → Additive，对齐旧 PixelParticle 绘制序
        /// </summary>
        private static void DrawPixelPassPRT()
        {
            List<IPixelPassPRT> alphaBlendDraw = new();
            List<IPixelPassPRT> nonPremultipliedDraw = new();
            List<IPixelPassPRT> additiveDraw = new();

            foreach (var prt in PRTLoader.PRT_InGame_World_Inds)
            {
                if (!prt.active || prt.Mod != Instance)
                {
                    continue;
                }
                if (prt is not IPixelPassPRT { PixelPass: true } pixelPRT)
                {
                    continue;
                }

                switch (prt.PRTDrawMode)
                {
                    case PRTDrawModeEnum.AdditiveBlend:
                        additiveDraw.Add(pixelPRT);
                        break;
                    case PRTDrawModeEnum.AlphaBlend:
                        alphaBlendDraw.Add(pixelPRT);
                        break;
                    default:
                        nonPremultipliedDraw.Add(pixelPRT);   //非Additive非AlphaBlend全进这桶,旧EParticle三分支语义
                        break;
                }
            }

            if (alphaBlendDraw.Count == 0 && nonPremultipliedDraw.Count == 0 && additiveDraw.Count == 0)
            {
                return;
            }

            //PreparePixelShader开的批次得先End,再按桶重开画像素粒子
            Main.spriteBatch.End();
            if (alphaBlendDraw.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                foreach (var p in alphaBlendDraw)
                {
                    p.DrawPixelPass(Main.spriteBatch);
                }
                Main.spriteBatch.End();
            }
            if (nonPremultipliedDraw.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                foreach (var p in nonPremultipliedDraw)
                {
                    p.DrawPixelPass(Main.spriteBatch);
                }
                Main.spriteBatch.End();
            }
            if (additiveDraw.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                foreach (var p in additiveDraw)
                {
                    p.DrawPixelPass(Main.spriteBatch);
                }
                Main.spriteBatch.End();
            }
            //三桶画完接回NonPremultiplied,后面NPC绘制续上PreparePixelShader开的批次
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public static void ApplyPixelShader(GraphicsDevice graphicsDevice, int dye = 0, Entity dyeEnt = null, bool GameZoom = false, BlendState state = null)
        {
            if (ModContent.GetInstance<Config>().EnablePixelEffect)
            {
                if (state == null)
                {
                    state = BlendState.AlphaBlend;
                }
                graphicsDevice.SetRenderTarget(Screen1);
                graphicsDevice.Clear(Color.Transparent);
                Effect shader = PixelShader;
                shader.CurrentTechnique = shader.Techniques["Technique1"];
                shader.Parameters["scsize"].SetValue(Main.ScreenSize.ToVector2() / Main.GameViewMatrix.Zoom);
                if (GameZoom)
                    shader.Parameters["scsize"].SetValue(Main.ScreenSize.ToVector2());
                shader.CurrentTechnique.Passes[0].Apply();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shader);
                Main.spriteBatch.Draw(Screen2, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
                graphicsDevice.SetRenderTarget(Main.screenTarget);
                graphicsDevice.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(Screen0, Vector2.Zero, Color.White);

                Main.spriteBatch.End();
                Effect dyeShader = null;
                if (dye != 0)
                {
                    //GameShaders.Armor.Apply(GameShaders.Armor.GetShaderIdFromItemId(dye), dyeEnt, new Terraria.DataStructures.DrawData(Screen1, Vector2.Zero, Color.White));

                    dyeShader = GameShaders.Armor.GetShaderFromItemId(dye).Shader;
                }
                Matrix m = Main.GameViewMatrix.ZoomMatrix;
                if (GameZoom)
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, state, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, m);
                else
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, state, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

                if (dye != 0)
                {
                    GameShaders.Armor.GetShaderFromItemId(dye).Apply(null, new(Screen1, Vector2.Zero, Color.White));
                }
                Main.spriteBatch.Draw(Screen1, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
            }
        }
        public static Texture2D ScaleTexture(Texture2D originalTexture, float scale, GraphicsDevice graphicsDevice)
        {
            // 计算新尺寸
            int newWidth = (int)(originalTexture.Width * scale);
            int newHeight = (int)(originalTexture.Height * scale);

            // 创建渲染目标
            RenderTarget2D renderTarget = new RenderTarget2D(
                graphicsDevice,
                newWidth,
                newHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.None);

            // 保存当前渲染状态
            RenderTarget2D originalTarget = (RenderTarget2D)graphicsDevice.GetRenderTargets()[0].RenderTarget;
            Viewport originalViewport = graphicsDevice.Viewport;

            // 设置新的渲染目标
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);

            // 使用SpriteBatch绘制缩放后的纹理
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                             SamplerState.LinearClamp, DepthStencilState.None,
                             RasterizerState.CullCounterClockwise);

            spriteBatch.Draw(originalTexture, new Rectangle(0, 0, newWidth, newHeight), Color.White);

            spriteBatch.End();

            // 恢复原始状态
            graphicsDevice.SetRenderTarget(originalTarget);
            graphicsDevice.Viewport = originalViewport;

            return renderTarget;
        }
        private static void DrawProjectileEffects(GraphicsDevice graphicsDevice)
        {
            if (Screen0 == null)
            {
                return;
            }

            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            Instance.cvcount += 3;

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            int cruiserSlashType = ModContent.ProjectileType<CruiserSlash>();
            int cruiserBlackholeBulletType = ModContent.ProjectileType<CruiserBlackholeBullet>();
            int voidBulletType = ModContent.ProjectileType<VoidBullet>();
            int voidMonsterType = ModContent.ProjectileType<VoidMonster>();

            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == cruiserSlashType)
                {
                    if (p.ModProjectile is CruiserSlash cs && cs.ct > 60)
                    {
                        Main.spriteBatch.Draw(cruiserSlash.Value, p.Center - Main.screenPosition + new Vector2((p.ai[0] + p.ai[1]) / 2 - 300, 0).RotatedBy(p.rotation), null, Color.White, p.rotation, new Vector2(cruiserSlash.Value.Width, cruiserSlash.Value.Height) / 2, new Vector2((p.ai[0] - p.ai[1]) / cruiserSlash.Value.Width, 1.2f), SpriteEffects.None, 0);
                    }
                }
                else if (p.type == cruiserBlackholeBulletType)
                {
                    Main.spriteBatch.Draw(cruiserBlackholeBullet.Value, p.Center - Main.screenPosition, null, Color.White, p.rotation, new Vector2(cruiserBlackholeBullet.Value.Width, cruiserBlackholeBullet.Value.Height) / 2, p.scale, SpriteEffects.None, 0);
                }
                else if (p.type == voidBulletType)
                {
                    Main.spriteBatch.Draw(cruiserBlackholeBullet.Value, p.Center - Main.screenPosition, null, Color.White, p.rotation, new Vector2(cruiserBlackholeBullet.Value.Width, cruiserBlackholeBullet.Value.Height) / 2, p.scale, SpriteEffects.None, 0);
                }
                else if (p.type == voidMonsterType)
                {
                    if (p.ModProjectile is VoidMonster vmnpc)
                    {
                        vmnpc.draw();
                    }
                }
            }


            foreach (var pt in PRTLoader.PRT_InGame_World_Inds)
            {
                if (!pt.active || pt.Mod != Instance)
                {
                    continue;
                }
                //is PRT_Void && is not PRT_Abyssal:两套RT shader分流,条件写反就画错桶
                if (pt is not PRT_Void || pt is PRT_Abyssal)
                {
                    continue;
                }
                if (cvmask == null)
                {
                    continue;
                }
                Main.spriteBatch.Draw(cvmask.Value, pt.Position - Main.screenPosition, null, Color.White * 0.06f, pt.Rotation, cvmask.Value.Size() / 2, (5.4f * pt.Opacity) * 0.05f, SpriteEffects.None, 0);
            }

            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile != null)
                {
                    if (p.ModProjectile is Pioneer1 p1)
                    {
                        p1.drawVoid();
                    }
                }
            }

            Main.spriteBatch.End();
        }

        //PRT_Void shape4→Screen1(Additive)→kscreen2 shader→Screen2,不进常规PRT桶
        private static void DrawParticleEffects(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetRenderTarget(Screen1);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            foreach (var pt in PRTLoader.PRT_InGame_World_Inds)
            {
                if (!pt.active || pt.Mod != Instance)
                {
                    continue;
                }
                //过滤条件和DrawNonPixVoidEffects/DrawParticleEffectsAlt镜像,is PRT_Abyssal走另一套RT
                if (pt is not PRT_Void || pt is PRT_Abyssal)
                {
                    continue;
                }
                if (pt is PRT_Void voidPt && voidPt.shape != 4)   //只有shape4进kscreen2合成,别的走常规PRT桶
                {
                    continue;
                }
                Texture2D draw = CvdtTex.Value;
                Main.spriteBatch.Draw(draw, pt.Position - Main.screenPosition, null, Color.White, pt.Rotation, draw.Size() / 2, 2.2f * pt.Opacity, SpriteEffects.None, 0);
            }

            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Screen2);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            kscreen2.CurrentTechnique = kscreen2.Techniques["Technique1"];
            kscreen2.CurrentTechnique.Passes[0].Apply();
            kscreen2.Parameters["tex0"].SetValue(Screen1);
            kscreen2.Parameters["tex1"].SetValue(CEExtraAssets.EternityStreak);
            kscreen2.Parameters["offset"].SetValue(Main.screenPosition / Main.ScreenSize.ToVector2());
            kscreen2.Parameters["i"].SetValue(0.04f);
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
        private static void DrawNonPixVoidEffects(GraphicsDevice graphicsDevice)
        {
            if (Screen0 == null || !CEUtils.AnyActiveProj<WOHHeld>())
            {
                return;
            }

            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);


            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile != null)
                {
                    if (p.ModProjectile is WOHHeld woh)
                        woh.DrawVoid();
                }
            }

            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            Main.spriteBatch.Draw(Screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            cvoid3.CurrentTechnique.Passes[0].Apply();
            cvoid3.Parameters["tex1"].SetValue(planetarium_blue_base.Value);
            cvoid3.Parameters["time"].SetValue(Instance.cvcount / 50f);
            cvoid3.Parameters["scsize"].SetValue(Main.ScreenSize.ToVector2());
            cvoid3.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(-Instance.cvcount / 6f, Instance.cvcount / 6f)) / Main.ScreenSize.ToVector2());
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
        private static void DrawParticleEffectsAlt()
        {
            //深渊粒子单独走DrawAbyssalEffect,过滤条件和上面虚空那套镜像
            foreach (var prt in PRTLoader.PRT_InGame_World_Inds)
            {
                if (!prt.active || prt.Mod != Instance)
                {
                    continue;
                }
                if (prt is not PRT_Abyssal pt)
                {
                    continue;
                }
                if (cvmask == null)
                {
                    continue;
                }
                Main.spriteBatch.Draw(cvmask.Value, pt.Position - Main.screenPosition, null, Color.White * 0.06f, pt.Rotation, cvmask.Value.Size() / 2, (5.4f * pt.Opacity) * 0.05f, SpriteEffects.None, 0);
            }
        }

        private static void ApplyBackgroundShader(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            Main.spriteBatch.Draw(Screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            cvoid.CurrentTechnique = cvoid.Techniques["Technique1"];
            cvoid.CurrentTechnique.Passes[0].Apply();
            cvoid.Parameters["tex1"].SetValue(planetarium_blue_base.Value);
            cvoid.Parameters["tex2"].SetValue(CEExtraAssets.Empty);
            cvoid.Parameters["tex3"].SetValue(CEExtraAssets.Empty);
            cvoid.Parameters["tex4"].SetValue(CEExtraAssets.Empty);
            cvoid.Parameters["tex5"].SetValue(CEExtraAssets.Empty);
            cvoid.Parameters["tex6"].SetValue(CEExtraAssets.Empty);
            cvoid.Parameters["time"].SetValue(Instance.cvcount / 50f);
            cvoid.Parameters["scsize"].SetValue(Main.ScreenSize.ToVector2());
            cvoid.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(-Instance.cvcount / 6f, Instance.cvcount / 6f)) / Main.ScreenSize.ToVector2());
            Main.spriteBatch.Draw(Screen2, Main.ScreenSize.ToVector2() * 0.5f, null, Color.White, 0, Screen2.Size() * 0.5f, 1, Main.LocalPlayer.gravDir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        private static void DrawPlayerAndProjectileEffects(GraphicsDevice graphicsDevice)
        {
            if (Screen1 == null)
            {
                return;
            }

            graphicsDevice.SetRenderTarget(Screen1);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (Player player in Main.ActivePlayers)
            {
                if (!player.dead && player.Entropy().daPoints.Count > 2)
                {
                    float scj = 1f / player.Entropy().daPoints.Count;
                    float sc = scj;
                    Color color = player.Entropy().VaMoving > 0 ? Color.Blue : Color.Black;
                    for (int i = 1; i < player.Entropy().daPoints.Count; i++)
                    {
                        CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, CEUtils.Entropy(player).daPoints[i - 1], CEUtils.Entropy(player).daPoints[i], color * 0.6f, 12 * sc, 0);
                        sc += scj;
                    }
                }
            }

            //获取投射物类型 ID
            int voidBottleThrowType = ModContent.ProjectileType<VoidBottleThrow>();
            int cruiserShadowType = ModContent.ProjectileType<CruiserShadow>();
            int voidWraithType = ModContent.ProjectileType<VoidWraith>();
            int abyssPetType = ModContent.ProjectileType<AbyssPet>();
            int voidPalProjType = ModContent.ProjectileType<VoidPalProj>();
            int shadewindLanceThrowType = ModContent.ProjectileType<ShadewindLanceThrow>();
            int voidStarType = ModContent.ProjectileType<VoidStar>();
            int voidStarFType = ModContent.ProjectileType<VoidStarF>();

            //遍历投射物，使用类型 ID 判断
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile == null)
                {
                    continue;
                }

                if (p.type == voidBottleThrowType || p.type == cruiserShadowType)
                {
                    Color color = Color.White;
                    p.ModProjectile.PreDraw(ref color);
                }
                if (p.ModProjectile is CrossBorderPursuitProj cbp)
                {
                    cbp.DrawEye();
                }
                else if (p.type == voidWraithType)
                {
                    if (p.ModProjectile is VoidWraith vw)
                    {
                        vw.draw();
                    }
                }
                else if (p.type == abyssPetType || p.type == voidPalProjType)
                {
                    Color color = Color.White;
                    p.ModProjectile.PreDraw(ref color);
                }
                else if (p.type == shadewindLanceThrowType)
                {
                    if (p.ModProjectile is ShadewindLanceThrow sp)
                    {
                        sp.draw();
                    }
                }
                else if (p.type == voidStarType || p.type == voidStarFType)
                {
                    Color c = p.type == voidStarFType && p.ai[2] > 0 ? new Color(255, 100, 100) : Color.White;
                    Main.spriteBatch.Draw(voidStar.Value, p.Center - Main.screenPosition, null, c * ((255 - p.alpha) / 255f), p.rotation, voidStar.Value.Size() / 2, new Vector2(1.45f, 0.25f) * p.scale, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(voidStar.Value, p.Center - Main.screenPosition, null, c * ((255 - p.alpha) / 255f), p.rotation, voidStar.Value.Size() / 2, new Vector2(0.25f, 1.45f) * p.scale, SpriteEffects.None, 0);
                    CEUtils.DrawGlow(p.Center, Color.LightBlue * 0.8f, 1f);
                }
            }
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Screen1, Vector2.Zero, Color.White);
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        private static void DrawSlashEffects(GraphicsDevice graphicsDevice)
        {
            if (Screen0 == null || Screen1 == null) return;
            if (!ModContent.GetInstance<Config>().ScreenWarpEffects)
                return;
            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //获取投射物类型 ID
            int slashType = ModContent.ProjectileType<Slash>();
            int slash2Type = ModContent.ProjectileType<Slash2>();
            int voidExplodeType = ModContent.ProjectileType<VoidExplode>();
            int voidRExpType = ModContent.ProjectileType<VoidRExp>();
            int starlessNightProjType = ModContent.ProjectileType<StarlessNightProj>();

            //遍历投射物，使用类型 ID 判断
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile == null)
                {
                    continue;
                }

                if (p.type == slashType)
                {
                    Main.spriteBatch.Draw(ksc1.Value, p.Center - Main.screenPosition + new Vector2((p.ai[0] + p.ai[1]) / 2 - 165, 0).RotatedBy(p.rotation), null, Color.White, p.rotation + (float)Math.PI / 2, new Vector2(ksc1.Value.Width, ksc1.Value.Height) / 2, new Vector2((p.ai[0] - p.ai[1]) / ksc1.Value.Width * 0.4f, 0.1f), SpriteEffects.None, 0);
                }
                else if (p.type == slash2Type)
                {
                    Main.spriteBatch.Draw(ksc1.Value, p.Center - Main.screenPosition + new Vector2((p.ai[0] + p.ai[1]) / 2 - 300, 0).RotatedBy(p.rotation), null, Color.White, p.rotation + (float)Math.PI / 2, new Vector2(ksc1.Value.Width, ksc1.Value.Height) / 2, new Vector2((p.ai[0] - p.ai[1]) / ksc1.Value.Width * 1.4f, 1f), SpriteEffects.None, 0);
                }
                else if (p.type == voidExplodeType)
                {
                    if (p.ModProjectile is VoidExplode ve)
                    {
                        float ks = ve.Projectile.timeLeft * 0.1f;
                        if (ve.Projectile.timeLeft > 10) ks = (20 - (float)ve.Projectile.timeLeft) / 10f;
                        ks *= (1 + ve.Projectile.ai[1]);
                        Main.spriteBatch.Draw(ksc1.Value, ve.Projectile.Center - Main.screenPosition, null, Color.White, 0, new Vector2(ksc1.Value.Width, ksc1.Value.Height) / 2, ks * 2, SpriteEffects.None, 0);
                    }
                }
                else if (p.type == voidRExpType)
                {
                    if (p.ModProjectile is VoidRExp vre)
                    {
                        float ks = (90f - vre.Projectile.timeLeft) * 0.4f;
                        if (p.ai[0] == 1)
                            ks = p.timeLeft * 0.4f;
                        float a = p.ai[0] == 0 ? (vre.Projectile.timeLeft / 90f) : 1 - (vre.Projectile.timeLeft / 90f);
                        Main.spriteBatch.Draw(shockwave.Value, vre.Projectile.Center - Main.screenPosition, null, Color.White * a, 0, new Vector2(shockwave.Value.Width, shockwave.Value.Height) / 2, ks, SpriteEffects.None, 0);
                    }
                }
                else if (p.type == starlessNightProjType)
                {
                    if (p.ModProjectile is StarlessNightProj sl)
                    {
                        sl.drawSlash();
                    }
                }
            }

            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            kscreen.CurrentTechnique = kscreen.Techniques["Technique1"];
            kscreen.CurrentTechnique.Passes[0].Apply();
            kscreen.Parameters["tex0"].SetValue(Main.screenTargetSwap);
            kscreen.Parameters["i"].SetValue(0.1f);
            Main.spriteBatch.Draw(Screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
        public static int votype = -1;
        public static int cruiserEnergyBallType = -1;
        public static int voidRsType = -1;
        private static void DrawFragEffects(GraphicsDevice graphicsDevice)
        {
            if (voidRsType < 0)
                voidRsType = ModContent.ProjectileType<VoidResidue>();
            if (cruiserEnergyBallType < 0)
                cruiserEnergyBallType = ModContent.ProjectileType<CruiserEnergyBall>();
            if (Screen0 == null || Screen1 == null) return;
            if (!ModContent.GetInstance<Config>().ScreenWarpEffects)
                return;
            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            if (votype == -1)
                votype = ModContent.TileType<VoidOreTile>();
            if (Main.LocalPlayer.Entropy().voidOreNearby > 0 && Config.Instance.TileEffect)
                DrawVoidOres(votype);
            bool startBatch = false;
            int ratzielStype = ModContent.ProjectileType<RatzielSentry>();
            Texture2D rGlowTex = CEExtraAssets.Circle;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == cruiserEnergyBallType && p.ModProjectile is CruiserEnergyBall ceb)
                {
                    if (!startBatch)
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        startBatch = true;
                    }
                    CEUtils.DrawGlow(p.Center, Color.White * p.Opacity * 0.72f, 14 * ceb.Scale);
                }
                if (p.type == voidRsType)
                {
                    if (!startBatch)
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        startBatch = true;
                    }
                    CEUtils.DrawGlow(p.Center, Color.White * p.Opacity * 0.4f, 3);
                }
                if (p.type == ratzielStype)
                {
                    if (!startBatch)
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        startBatch = true;
                    }
                    CEUtils.DrawGlow(p.Center, Color.White * p.Opacity * 0.42f, 6, true, rGlowTex);
                }
            }
            if (startBatch)
                Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            fscreen.CurrentTechnique = fscreen.Techniques["Technique1"];
            fscreen.CurrentTechnique.Passes[0].Apply();
            fscreen.Parameters["strengthMult"].SetValue(0.1f);
            fscreen.Parameters["screen"].SetValue(Main.screenPosition * new Vector2(1, Main.LocalPlayer.gravDir) / Main.ScreenSize.ToVector2());
            fscreen.Parameters["iTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.034f);
            fscreen.Parameters["coordMult"].SetValue(new Vector2(1, (float)Main.screenHeight / Main.screenWidth) * 1.2f);
            graphicsDevice.Textures[0] = Screen0;
            graphicsDevice.Textures[1] = Main.screenTargetSwap;
            graphicsDevice.Textures[2] = CEExtraAssets.VoidBack;
            Main.spriteBatch.Draw(Screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
        public static void DrawVoidOres(int types)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            float gfxQuality = Main.gfxQuality;
            int offScreenRange = Main.offScreenRange;
            bool drawToScreen = Main.drawToScreen;
            Vector2 screenPosition = Main.screenPosition;
            int screenWidth = Main.screenWidth;
            int screenHeight = Main.screenHeight;
            int maxTilesX = Main.maxTilesX;
            int maxTilesY = Main.maxTilesY;
            int[] wallBlend = Main.wallBlend;
            SpriteBatch spriteBatch = Main.spriteBatch;
            var _tileArray = Main.tile;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int num = (int)(120f * (1f - gfxQuality) + 40f * gfxQuality);
            int num2 = (int)((float)num * 0.4f);
            int num3 = (int)((float)num * 0.35f);
            int num4 = (int)((float)num * 0.3f);
            Vector2 vector = new Vector2(offScreenRange, offScreenRange);
            if (true)
            {
                vector = Vector2.Zero;
            }
            int num5 = (int)((screenPosition.X - vector.X) / 16f - 1f);
            int num6 = (int)((screenPosition.X + (float)screenWidth + vector.X) / 16f) + 2;
            int num7 = (int)((screenPosition.Y - vector.Y) / 16f - 1f);
            int num8 = (int)((screenPosition.Y + (float)screenHeight + vector.Y) / 16f) + 5;
            int num9 = offScreenRange / 16;
            int num10 = offScreenRange / 16;
            if (num5 - num9 < 4)
            {
                num5 = num9 + 4;
            }
            if (num6 + num9 > maxTilesX - 4)
            {
                num6 = maxTilesX - num9 - 4;
            }
            if (num7 - num10 < 4)
            {
                num7 = num10 + 4;
            }
            if (num8 + num10 > maxTilesY - 4)
            {
                num8 = maxTilesY - num10 - 4;
            }
            VertexColors vertices = default(VertexColors);
            Rectangle value = new Rectangle(0, 0, 16, 16);
            int underworldLayer = Main.UnderworldLayer;
            Point screenOverdrawOffset = Main.GetScreenOverdrawOffset();
            for (int i = num7 - num10 + screenOverdrawOffset.Y; i < num8 + num10 - screenOverdrawOffset.Y; i++)
            {
                for (int j = num5 - num9 + screenOverdrawOffset.X; j < num6 + num9 - screenOverdrawOffset.X; j++)
                {
                    Tile tile = _tileArray[j, i];
                    ushort wall = tile.WallType;
                    if (tile.HasTile && tile.TileType == types)
                    {
                        value.X = tile.TileFrameX;
                        value.Y = tile.TileFrameY + Main.tileFrame[tile.TileType] * 0;

                        Texture2D GetTileDrawTexture(Tile tile, int tileX, int tileY)
                        {
                            Texture2D result = TextureAssets.Tile[tile.TileType].Value;
                            int wall = tile.TileType;
                            Texture2D texture2D = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(wall, 0, tile.TileColor);
                            if (texture2D != null)
                            {
                                result = texture2D;
                            }
                            return result;
                        }
                        Texture2D tileDrawTexture = GetTileDrawTexture(tile, j, i);
                        vertices = new VertexColors(Color.LightBlue);
                        var pos = new Vector2(j * 16, i * 16) + vector;
                        CEUtils.DrawGlow(pos + new Vector2(8, 8), Color.White * 0.24f, 3.6f, true, null, false);
                    }
                }
            }
            Main.spriteBatch.End();
        }
        private static void ApplyFinalShader(GraphicsDevice graphicsDevice)
        {
            if (FlashEffectStrength > 0)
            {
                graphicsDevice.SetRenderTarget(Screen0);
                graphicsDevice.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
                Main.spriteBatch.End();

                graphicsDevice.SetRenderTarget(Main.screenTarget);
                graphicsDevice.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(Screen0, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                for (float i = 1; i <= 16; i++)
                {
                    Main.spriteBatch.Draw(Screen0, Screen0.Size() / 2, null, Color.White * ((16f / i) * 0.1f * FlashEffectStrength), 0, Screen0.Size() / 2, 1 + FlashEffectStrength * 0.08f * i, SpriteEffects.None, 0);
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.End();
            }

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            int abyssalWraithType = ModContent.NPCType<AbyssalWraith>();
            int cruiserHeadType = ModContent.NPCType<CruiserHead>();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.type == abyssalWraithType && npc.ModNPC is AbyssalWraith aw)
                {
                    NPCLoader.PreDraw(npc, Main.spriteBatch, Main.screenPosition, Color.White);
                    aw.Draw();
                    NPCLoader.PostDraw(npc, Main.spriteBatch, Main.screenPosition, Color.White);
                }
                if (npc.type == cruiserHeadType && npc.ModNPC is CruiserHead ch && ch.phase == 2)
                {
                    ch.candraw = true;
                    NPCLoader.PreDraw(npc, Main.spriteBatch, Main.screenPosition, Color.White);
                    ch.PreDraw(Main.spriteBatch, Main.screenPosition, Color.White);
                    NPCLoader.PostDraw(npc, Main.spriteBatch, Main.screenPosition, Color.White);
                    ch.candraw = false;
                }
            }

            int starlessNightType = ModContent.ProjectileType<StarlessNightProj>();
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.ModProjectile != null && proj.ModProjectile is CruiserPhantomPet crp)
                    crp.draw();
                if (proj.type != starlessNightType)
                {
                    continue;
                }

                if (proj.ModProjectile is StarlessNightProj sl)
                {
                    sl.drawSword();
                }
            }

            Main.spriteBatch.End();
        }

        private static void HandleCutScreenEffect(GraphicsDevice graphicsDevice)
        {
            if (cutScreen <= 0)
            {
                return;
            }
            if (!ModContent.GetInstance<Config>().ScreenWarpEffects)
                return;

            graphicsDevice.SetRenderTarget(Screen0);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            CEUtils.drawLine(cutScreenCenter, cutScreenCenter + cutScreenRot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 9000, Color.Black, 9000);
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Screen1);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            CEUtils.drawLine(cutScreenCenter, cutScreenCenter + cutScreenRot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * -9000, Color.Black, 9000);
            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Black);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            Effect blur = BlurShader;
            blur.CurrentTechnique = blur.Techniques["GaussianBlur"];
            blur.Parameters["resolution"].SetValue(Main.ScreenSize.ToVector2());
            blur.Parameters["blurAmount"].SetValue(cutScreen * 0.036f);
            blur.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(Screen0, cutScreenRot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * -cutScreen * Main.GameViewMatrix.Zoom.X, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(Screen1, cutScreenRot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * cutScreen * Main.GameViewMatrix.Zoom.X, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        private static void DrawBlackMask()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            if (blackMaskTime > 0)
            {
                blackMaskAlpha = Math.Min(blackMaskAlpha + 0.05f, 1f);
            }
            else
            {
                blackMaskAlpha = Math.Max(blackMaskAlpha - 0.025f, 0f);
            }
            Main.spriteBatch.Draw(CEUtils.pixelTex, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * 0.5f * blackMaskAlpha);
            Main.spriteBatch.End();
        }
    }
}