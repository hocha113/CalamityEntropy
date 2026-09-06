using CalamityEntropy.Content.Items.Vanity;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI
{
    public class EResourceOverlay : ModResourceOverlay
    {
        private static Dictionary<string, Asset<Texture2D>> vanillaAssetCache = new();
        public static string baseFolder = "CalamityEntropy/Content/UI/";

        public static string LifeTexturePath()
        {
            string folder = $"{baseFolder}MoonShield";
            return folder;
        }
        public static string ManaTexturePath()
        {
            if (Main.LocalPlayer.Entropy().HasEnhancedMana)
            {
                string folder = $"{baseFolder}AH";

                return folder;
            }
            else { return string.Empty; }
        }

        // 原先挂钩灾厄的 CalamityResourceOverlay 转调绘制，脱离灾厄后改为 tML 原生覆写
        public override void PostDrawResource(ResourceOverlayDrawContext context)
        {
            Asset<Texture2D> asset = context.texture;
            string fancyFolder = "Images/UI/PlayerResourceSets/FancyClassic/";
            string barsFolder = "Images/UI/PlayerResourceSets/HorizontalBars/";
            bool blackMask = Main.LocalPlayer.GetModPlayer<VanityModPlayer>().vanityEquipped == "KitsunesFan";

            if (LifeTexturePath() != string.Empty)
            {
                if (asset == TextureAssets.Heart || asset == TextureAssets.Heart2 || CompareAssets(asset, fancyFolder + "Heart_Fill") || CompareAssets(asset, fancyFolder + "Heart_Fill_B"))
                {
                    if ((context.resourceNumber + 1) * 30 <= Main.LocalPlayer.Entropy().MagiShield)
                    {
                        context.texture = ModContent.Request<Texture2D>(LifeTexturePath() + "Heart");
                        if (Main.LocalPlayer.Entropy().MagiShield - (context.resourceNumber + 1) * 30 < 30)
                        {
                            float s = ((float)(Main.LocalPlayer.Entropy().MagiShield - (context.resourceNumber + 1) * 30)) / 30f;
                            context.scale *= new Vector2(s, s);
                        }

                        context.Draw();
                    }
                    if (Main.LocalPlayer.Entropy().deusCoreBloodOut > 0)
                    {
                        if (context.resourceNumber > (Main.LocalPlayer.ConsumedLifeCrystals + 5f) * ((((float)Main.LocalPlayer.statLife) / ((float)Main.LocalPlayer.statLifeMax2)) - ((float)Main.LocalPlayer.Entropy().deusCoreBloodOut / (float)Main.LocalPlayer.statLifeMax2)))
                        {
                            context.texture = ModContent.Request<Texture2D>($"{baseFolder}Astr" + "Heart");
                            context.Draw();
                        }
                    }
                    if (blackMask)
                    {
                        context.color = Color.Black * 0.5f;
                        context.texture = ModContent.Request<Texture2D>($"{baseFolder}HeartWhite");
                        context.Draw();
                        context.color = Color.White;
                    }
                    if (context.resourceNumber == 0 && Main.LocalPlayer.Entropy().HolyShield)
                    {
                        context.texture = ModContent.Request<Texture2D>($"{baseFolder}mantle");
                        context.scale = new Vector2(2, 2);
                        context.origin += new Vector2(0.5f, 0.5f);
                        Main.spriteBatch.UseSampleState_UI(SamplerState.PointClamp);
                        context.Draw();
                    }


                }
                else if (CompareAssets(asset, barsFolder + "HP_Fill") || CompareAssets(asset, barsFolder + "HP_Fill_Honey"))
                {
                    if ((context.resourceNumber + 1) * 30 <= Main.LocalPlayer.Entropy().MagiShield)
                    {
                        context.texture = ModContent.Request<Texture2D>(LifeTexturePath() + "Bar");
                        context.Draw();
                    }
                    if (Main.LocalPlayer.Entropy().deusCoreBloodOut > 0)
                    {
                        if (context.resourceNumber > (Main.LocalPlayer.ConsumedLifeCrystals + 5f) * ((((float)Main.LocalPlayer.statLife) / ((float)Main.LocalPlayer.statLifeMax2)) - ((float)Main.LocalPlayer.Entropy().deusCoreBloodOut / (float)Main.LocalPlayer.statLifeMax2)))
                        {
                            context.texture = ModContent.Request<Texture2D>($"{baseFolder}Astr" + "Bar");
                            context.Draw();
                        }
                    }
                    if (blackMask)
                    {
                        context.color = Color.Black * 0.5f;
                        context.texture = ModContent.Request<Texture2D>($"{baseFolder}BarWhite");
                        context.Draw();
                        context.color = Color.White;
                    }
                    if (context.resourceNumber == 2 && Main.LocalPlayer.Entropy().HolyShield)
                    {
                        context.texture = ModContent.Request<Texture2D>($"{baseFolder}mantle");
                        context.scale = new Vector2(2, 2);
                        context.origin += new Vector2(0.5f, 0.5f);
                        Main.spriteBatch.UseSampleState_UI(SamplerState.PointClamp);
                        context.Draw();
                    }


                }
            }
            if (ManaTexturePath() != string.Empty)
            {
                if (asset == TextureAssets.Mana || CompareAssets(asset, fancyFolder + "Star_Fill"))
                {
                    if (Main.LocalPlayer.Entropy().HasEnhancedMana)
                    {
                        if ((context.resourceNumber + 1) * 20 > Main.LocalPlayer.Entropy().manaNorm)
                        {
                            context.texture = ModContent.Request<Texture2D>(ManaTexturePath() + "Star");
                            context.Draw();
                        }
                    }
                    if (blackMask)
                    {
                        context.color = Color.Black * 0.8f;
                        context.texture = ModContent.Request<Texture2D>($"{baseFolder}StarWhite");
                        context.Draw();
                        context.color = Color.White;
                    }

                }
                else if (CompareAssets(asset, barsFolder + "MP_Fill"))
                {
                    if (Main.LocalPlayer.Entropy().HasEnhancedMana)
                    {
                        if ((context.resourceNumber + 1) * 20 > Main.LocalPlayer.Entropy().manaNorm)
                        {
                            context.texture = ModContent.Request<Texture2D>(ManaTexturePath() + "Bar");
                            context.Draw();
                        }
                    }
                    if (blackMask)
                    {
                        context.color = Color.Black * 0.5f;
                        context.texture = ModContent.Request<Texture2D>($"{baseFolder}BarWhite");
                        context.Draw();
                        context.color = Color.White;
                    }
                }
            }
        }

        public override bool DisplayHoverText(PlayerStatsSnapshot snapshot, IPlayerResourcesDisplaySet displaySet, bool drawingLife)
        {
            if (!drawingLife && Main.LocalPlayer.Entropy().HasEnhancedMana)
            {
                string str = $"{snapshot.Mana}/{Main.LocalPlayer.Entropy().manaNorm}[c/f0af00:+{snapshot.ManaMax - Main.LocalPlayer.Entropy().manaNorm}]";
                Main.LocalPlayer.cursorItemIconEnabled = true;
                Main.LocalPlayer.cursorItemIconID = -1;
                Main.LocalPlayer.cursorItemIconText = str;
                return false;
            }
            return true;
        }

        private static bool CompareAssets(Asset<Texture2D> currentAsset, string compareAssetPath)
        {
            if (!vanillaAssetCache.TryGetValue(compareAssetPath, out var asset))
                asset = vanillaAssetCache[compareAssetPath] = Main.Assets.Request<Texture2D>(compareAssetPath);

            return currentAsset == asset;
        }
    }
}
