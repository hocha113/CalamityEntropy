using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Items.Armor.AzafureT3;
using CalamityEntropy.Content.Items.Armor.NihTwins;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Donator.Ratziel;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.GrassSword;
using CalamityEntropy.Content.NPCs.Prophet;
using CalamityEntropy.Content.NPCs.SpiritFountain;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Skies;
using CalamityEntropy.Content.UI;
using CalamityEntropy.Content.UI.EntropyBookUI;
using CalamityMod;
using CalamityMod.Items.Ammo;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.UI.CalamitasEnchants;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityEntropy.Common
{
    public class EModSys : ModSystem
    {
        private static Texture2D markTex => CEUtils.getExtraTex("EvMark");
        internal static int timer;
        public static bool noItemUse = false;
        public float counter = 0;
        public bool prd = true;
        public static bool mi = false;
        public bool escLast = true;
        public bool rCtrlLast = false;
        public bool eowLast = false;
        public int eowMaxLife = 0;
        public bool slimeGodLast = false;
        public int slimeGodMaxLife = 0;
        public Vector2 LastPlayerPos;
        public Vector2 LastPlayerVel;
        public static int AcropolisDontSpawn = 0;

        public override void OnLocalizationsLoaded()
        {
            string n = Language.GetTextValue($"Mods.CalamityEntropy.ModNameOverride");
            var ff = typeof(Mod).GetProperty("DisplayName", BindingFlags.Public |
        BindingFlags.Instance |
        BindingFlags.NonPublic);
            ff?.SetValue(Mod, n);
        }
        public static Color GetColorForNPCBossbarFromTexture(Color[] data)
        {
            int pixelCount = 0;
            Dictionary<Color, int> dict = new();
            foreach (Color clr in data)
            {
                if (clr.A != 0)
                {
                    if (dict.ContainsKey(clr))
                    {
                        dict[clr]++;
                    }
                    else
                    {
                        dict[clr] = 0;
                    }
                    if (clr.R + clr.G + clr.B >= 80)
                    {
                        dict[clr] += (clr.R + clr.G + clr.B) / 80;
                    }
                    pixelCount++;
                }
            }
            if (pixelCount > 0)
            {
                Color c = Color.White;
                int count = 0;
                foreach (Color color in dict.Keys)
                {
                    if (dict[color] > count)
                    {
                        count = dict[color];
                        c = color;
                    }
                }
                return c;
            }
            return Color.Transparent;

        }
        public override void PostSetupContent()
        {
            Fruitcake.ammoList = new();
            List<int> AmmoIds = new List<int>();
            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                if (i == ModContent.ItemType<MortarRound>() || i == ModContent.ItemType<RubberMortarRound>())
                {
                    continue;
                }
                if (ContentSamples.ItemsByType[i].ammo != AmmoID.None)
                {
                    if (!AmmoIds.Contains(ContentSamples.ItemsByType[i].ammo))
                    {
                        AmmoIds.Add(ContentSamples.ItemsByType[i].ammo);
                        Fruitcake.ammoList[ContentSamples.ItemsByType[i].ammo] = new();
                    }
                    Fruitcake.ammoList[ContentSamples.ItemsByType[i].ammo].Add(i);
                }
            }
        }

        public static void DrawDriverShield(Player player, float progress, bool active, Vector2 center)
        {
            if (!player.Entropy().DriverShieldVisual)
                return;

            center += player.gfxOffY * Vector2.UnitY;
            float scale = player.Entropy().DriverScale;
            Texture2D noise = CEUtils.getExtraTex("Noise_14");
            Texture2D tex = CEUtils.getExtraTex("RectShield");
            if (player.Entropy().AzafureDriverShieldItem == null && player.Entropy().DriverShieldVisual)
            {
                active = true;
                scale = 2.5f;
            }
            if (active)
            {
                float alpha = 0.5f + 0.5f * progress;
                Main.spriteBatch.Begin(0, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Color clr = Color.Lerp(new Color(190, 40, 40), Color.White, 0.5f + 0.5f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 10))) * 0.8f;
                int offset = (int)Main.GameUpdateCount;
                Vector2 so = new Vector2(12, 12) * scale;
                Main.spriteBatch.Draw(noise, center - so - Main.screenPosition, new Rectangle(offset, offset, tex.Width * 4, tex.Height * 4), Color.OrangeRed * alpha, 0, tex.Size() / 2f, scale / 4, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(noise, center - so - Main.screenPosition, new Rectangle(offset / 2, offset / 2, tex.Width * 4, tex.Height * 4), Color.OrangeRed * alpha, 0, tex.Size() / 2f, scale / 4, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, center - Main.screenPosition, null, clr * alpha, 0, tex.Size() / 2f, scale, SpriteEffects.None, 0);

                alpha *= player.Entropy().ShieldAlphaAdd;
                Main.spriteBatch.Draw(noise, center - so - Main.screenPosition, new Rectangle(offset, offset, tex.Width * 4, tex.Height * 4), Color.OrangeRed * alpha, 0, tex.Size() / 2f, scale / 4, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(noise, center - so - Main.screenPosition, new Rectangle(offset / 2, offset / 2, tex.Width * 4, tex.Height * 4), Color.OrangeRed * alpha, 0, tex.Size() / 2f, scale / 4, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, center - Main.screenPosition, null, clr * alpha, 0, tex.Size() / 2f, scale, SpriteEffects.None, 0);

                Main.spriteBatch.End();
            }
            else
            {
                float alpha = 0.72f * progress;
                Main.spriteBatch.Begin(0, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Color clr = Color.Lerp(new Color(255, 100, 100), Color.White, 0.5f + 0.5f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 36)));
                int offset = (int)Main.GameUpdateCount;
                Vector2 so = new Vector2(12, 12) * scale;
                Main.spriteBatch.Draw(noise, center - so - Main.screenPosition, new Rectangle(offset, offset, tex.Width * 4, tex.Height * 4), Color.OrangeRed * alpha * alpha, 0, tex.Size() / 2f, scale / 4, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(noise, center - so - Main.screenPosition, new Rectangle(offset / 2, offset / 2, tex.Width * 4, tex.Height * 4), Color.OrangeRed * alpha * alpha, 0, tex.Size() / 2f, scale / 4, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, center - Main.screenPosition, null, clr * alpha, 0, tex.Size() / 2f, scale, SpriteEffects.None, 0);

                Main.spriteBatch.End();
            }
        }
        public static void DrawNihShield(Player player, Vector2 pos, float alpha, float scale)
        {
            scale *= 0.5f;
            Vector2 center = pos + Vector2.UnitY * player.gfxOffY;
            Effect shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/NihShield", AssetRequestMode.ImmediateLoad).Value;
            shader.Parameters["offset"].SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["num"].SetValue(0.98f);
            shader.Parameters["OutlineColor"].SetValue((Color.Lerp(new Color(140, 140, 255), Color.White, 0.16f + 0.16f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 6)))).ToVector4() * alpha);
            shader.CurrentTechnique.Passes[0].Apply();
            var gd = Main.graphics.GraphicsDevice;
            gd.Textures[1] = CEUtils.getExtraTex("Noise_10");
            Texture2D tex = CEUtils.getExtraTex("Hexagon");
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tex, center - Main.screenPosition, null, new Color(200, 200, 255) * alpha, 0, tex.Size() / 2f, scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            shader.Parameters["OutlineColor"].SetValue((Color.Lerp(new Color(140, 140, 255), Color.White, 0.16f + 0.16f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 6)))).ToVector4() * alpha * player.Entropy().ShieldAlphaAdd);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tex, center - Main.screenPosition, null, new Color(200, 200, 255) * alpha * player.Entropy().ShieldAlphaAdd, 0, tex.Size() / 2f, scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
        }
        public static void DrawVoidShield(Player player, Vector2 pos, float alpha, float scale)
        {
            if (player.Entropy().VoidShieldVisual && player.Entropy().VoidCoreItem == null)
            {
                alpha = 1;
                scale = 0.6f;
            }
            scale *= 0.55f;
            Vector2 center = pos + Vector2.UnitY * player.gfxOffY;
            Effect shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/NihShield", AssetRequestMode.ImmediateLoad).Value;
            shader.Parameters["offset"].SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["num"].SetValue(0.98f);
            shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            var gd = Main.graphics.GraphicsDevice;
            gd.Textures[1] = CEUtils.getExtraTex("Noise_10");
            Texture2D tex = CEUtils.getExtraTex("Circle");
            for (int i = 0; i < 1; i++)
            {
                float rot = 0.8f;
                rot *= Main.GlobalTimeWrappedHourly * 2;
                Main.spriteBatch.End();
                shader.Parameters["OutlineColor"].SetValue((Color.Lerp(new Color(140, 140, 255), Color.White, 0.16f + 0.16f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 6)))).ToVector4() * alpha);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(tex, center - Main.screenPosition, null, new Color(200, 200, 255) * alpha, 0 + rot, tex.Size() / 2f, scale, SpriteEffects.None, 0);
                Main.spriteBatch.End();
                shader.Parameters["OutlineColor"].SetValue((Color.Lerp(new Color(140, 140, 255), Color.White, 0.16f + 0.16f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 6)))).ToVector4() * alpha * player.Entropy().ShieldAlphaAdd);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(tex, center - Main.screenPosition, null, new Color(200, 200, 255) * alpha * player.Entropy().ShieldAlphaAdd, 0 + rot, tex.Size() / 2f, scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.End();
        }
        public override void OnModLoad()
        {
            for (int index = 0; index < EnchantmentManager.EnchantmentList.Count; index++)
            {
                Enchantment enc = EnchantmentManager.EnchantmentList[index];
                if (enc.IconTexturePath == "CalamityMod/UI/CalamitasEnchantments/CurseIcon_Tainted")
                {
                    var method = typeof(Enchantment).GetField("ApplyRequirement", BindingFlags.Instance | BindingFlags.NonPublic);
                    Predicate<Item> pd = (Predicate<Item>)(item => item.IsEnchantable() && item.damage > 0 && item.CountsAsClass<MeleeDamageClass>() && ((!item.noUseGraphic && item.shoot > ProjectileID.None) || CELists.SpecialTaintedEnchantmentList.Contains(item.type)));
                    object boxed = enc;
                    method.SetValue(boxed, pd);
                    EnchantmentManager.EnchantmentList[index] = (Enchantment)boxed;
                    break;
                }
            }
        }
        public override void PostDrawTiles()
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.dead)
                    continue;
                EModPlayer mp = player.Entropy();
                {

                    float p = 1;
                    if (mp.AzafureDriverShieldItem != null)
                    {
                        p = (float)mp.DriverShield / AzafureDriverCore.MaxShield;
                        if (mp.DriverShield <= 0)
                        {
                            p = (float)mp.DriverRecharge / AzafureDriverCore.RechargeTime;
                        }
                    }
                    DrawDriverShield(player, p, mp.DriverShield > 0, player.Center);
                }
                if (mp.NihilityShieldEnabled)
                {
                    float p = mp.NihilityShield / (float)VoidEaterHelmet.MaxShield;
                    float c = mp.NihilityShield > 0 ? 1 : ((float)mp.NihilityRecharge / VoidEaterHelmet.ShieldRecharge) * 0.6f;
                    DrawNihShield(player, player.Center, c * (0.5f + p * 0.5f), mp.NihShieldScale * (0.7f + 0.3f * p));
                }
                if (mp.NihArmorRope != null && player.whoAmI < mp.NihTwinArmorConnetPlayer)
                {
                    Main.spriteBatch.begin_();
                    mp.DrawNihRope();
                    Main.spriteBatch.End();
                }
                if (mp.RatzielShieldTime > 0 && mp.RatzielShield > 0)
                {
                    Main.spriteBatch.begin_();
                    DrawForce(player, (player.Entropy().RatzielShield / (float)Ratziel.MaxShield(Ratziel.Level())) * 0.6f + 0.4f);
                    Main.spriteBatch.End();
                }
                if (mp.VoidShieldVisual)
                {
                    float p = 1;
                    if (mp.VoidCoreItem != null)
                    {
                        p = (float)mp.VoidShield / VoidCore.MaxShield;
                        if (mp.VoidShield <= 0)
                        {
                            p = (float)mp.VoidRecharge / VoidCore.ShieldRecharge;
                        }
                    }
                    float c = mp.VoidShield > 0 ? 1 : ((float)mp.VoidRecharge / VoidCore.ShieldRecharge) * 0.4f;

                    DrawVoidShield(player, player.Center, c * (0.5f + p * 0.5f), mp.VoidCoreShieldScale * (0.7f + 0.3f * p));
                }
            }

            if (CalamityEntropy.SetupBossbarClrAuto)
            {
                CalamityEntropy.SetupBossbarClrAuto = false;
                for (int i = 0; i < NPCLoader.NPCCount; i++)
                {
                    if (!EntropyBossbar.bossbarColor.ContainsKey(i) && ContentSamples.NpcsByNetId[i].boss)
                    {
                        Main.instance.LoadNPC(i);
                        Texture2D tex = TextureAssets.Npc[i].Value;
                        var pixData = new Color[tex.Width * tex.Height];
                        tex.GetData(pixData);
                        Color c = GetColorForNPCBossbarFromTexture(pixData);
                        if (c.A > 0)
                        {
                            EntropyBossbar.bossbarColor[i] = c;
                        }
                    }
                }
            }
            if (ptype == -1)
                ptype = ModContent.NPCType<TheProphet>();
            if (sftype == -1)
                sftype = ModContent.NPCType<SpiritFountain>();

            List<int> HighLightWallTypes = new List<int>() { 94, 98, 96, 95, 99, 97 };
            DWAlpha = float.Lerp(DWAlpha, NPC.AnyNPCs(sftype) ? 0.2f : (NPC.AnyNPCs(ptype) ? 1 : 0), 0.1f);
            if (DWAlpha > 0.02f && Config.Instance.TileEffect)
            {
                DrawWallsHL(HighLightWallTypes);
            }
            if (mi)
            {
                Main.instance.IsMouseVisible = true;
            }
            if (!Main.dedServ && Main.LocalPlayer.Entropy().hasAcc(SmartScope.ID))
            {
                if (SmartScope.target != null)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                    var tx = CEUtils.getExtraTex("SS_Target");
                    Main.spriteBatch.Draw(tx, SmartScope.target.Center - Main.screenPosition, null, Color.White, 0, tx.Size() / 2f, 1, SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                }
            }
            if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<EventideSniper>())
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.dontTakeDamage && !npc.friendly)
                    {
                        Main.spriteBatch.Draw(markTex, npc.Center - Main.screenPosition, null, Color.White, 0, markTex.Size() / 2, 1, SpriteEffects.None, 0f);
                    }
                }
                Main.spriteBatch.End();
            }
        }
        public void DrawForce(Player player, float alpha)
        {
            string key = "Nebula";
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Terraria.Graphics.Effects.Filters.Scene[key].GetShader().UseIntensity(2f).UseProgress(0f);
            DrawData value61 = new DrawData(Main.Assets.Request<Texture2D>("Images/Misc/Perlin", (AssetRequestMode)1).Value, player.Center - Main.screenPosition, new Microsoft.Xna.Framework.Rectangle(0, 0, 600, 600), Microsoft.Xna.Framework.Color.White * alpha, 0, new Vector2(300f, 300f), new Vector2(1.05f, 0.7f) * 0.32f, SpriteEffects.None);
            GameShaders.Misc["ForceField"].UseColor(Color.LightSkyBlue.ToVector3());
            GameShaders.Misc["ForceField"].Apply(value61);
            value61.Draw(Main.spriteBatch);
            value61.Draw(Main.spriteBatch);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
        public int ptype = -1;
        public int sftype = -1;
        public static List<int> NeedTiles = new List<int>() { 41, 43, 44 };
        public void DrawWallsHL(List<int> types)
        {
            Effect shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/WhiteTrans", AssetRequestMode.ImmediateLoad).Value;

            shader.CurrentTechnique.Passes[0].Apply();
            shader.Parameters["strength"].SetValue(0.2f);
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
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
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
                    if (!tile.HasUnactuatedTile && tile.WallType > 0 && types.Contains(wall) && NeedTiles.Contains(tile.TileType))
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
                        var pos = new Vector2(j * 16 - (int)screenPosition.X, i * 16 - (int)screenPosition.Y) + vector;
                        spriteBatch.Draw(tileDrawTexture, pos, value, Color.SkyBlue * DWAlpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

                    }
                }
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.End();
        }
        public float DWAlpha = 0;
        public static bool sayTip = true;
        public override void UpdateUI(GameTime gameTime)
        {
            lhBarTarget = float.Lerp(lhBarTarget, ((float)Main.LocalPlayer.statLife / (float)Main.LocalPlayer.statLifeMax2), 0.1f);
            lhBarTarget2 = float.Lerp(lhBarTarget2, lhBarTarget, 0.06f);

            if (Lighting.Mode != Terraria.Graphics.Light.LightMode.Color)
            {
                if (sayTip)
                {
                    sayTip = false;
                }
            }
            if (!ModContent.GetInstance<Config>().EnableRetroLighting)
            {
                if (Lighting.Mode == Terraria.Graphics.Light.LightMode.Retro || Lighting.Mode == Terraria.Graphics.Light.LightMode.Trippy)
                {
                    Lighting.Mode = Terraria.Graphics.Light.LightMode.Color;
                }
                Main.WaveQuality = 3;
            }

            noItemUse = false;
            counter += 1f;
            if (ArmorForgingStationUI.Visible)
            {
                CalamityEntropy.Instance.userInterface?.Update(gameTime);
            }
            if (ModContent.GetInstance<Config>().EnableRetroLighting && ModContent.GetInstance<Config>().EnablePixelEffect)
            {
                ModContent.GetInstance<Config>().EnablePixelEffect = false;
            }
        }

        public override void PostUpdateDusts()
        {
            //Debug
            /*{
                if (CEKeybinds.CommandMinions.JustPressed)
                {
                    OnModLoad();
                    foreach (Enchantment enc in EnchantmentManager.EnchantmentList)
                    {
                        if (enc.IconTexturePath == "CalamityMod/UI/CalamitasEnchantments/CurseIcon_Tainted")
                        {
                            var method = typeof(Enchantment).GetField("ApplyRequirement", BindingFlags.Instance | BindingFlags.NonPublic);
                        }
                    }
                }
            }*/
            ScreenShaker.Update();
            if (CalamityEntropy.FlashEffectStrength > 0)
            {
                CalamityEntropy.FlashEffectStrength -= 0.02f;
            }
            CalamityEntropy.blackMaskTime--;
            PixelParticle.Update();
            VoidParticles.Update();
            AbyssalParticles.Update();
            CalamityEntropy.cutScreen += CalamityEntropy.cutScreenVel;
            if (CalamityEntropy.cutScreen > 0)
            {
                CalamityEntropy.cutScreenVel -= 0.5f;
            }
            if (CalamityEntropy.cutScreen < 0)
            {
                CalamityEntropy.cutScreen = 0;
                CalamityEntropy.cutScreenVel = 0;
            }
        }

        public override void PostUpdatePlayers()
        {
            if (AcropolisDontSpawn > 0)
                AcropolisDontSpawn--;
            CECooldowns.Update();
            EBookUI.update();
            if (CalamityEntropy.noMusTime > 0)
            {
                CalamityEntropy.noMusTime--;
                Main.curMusic = 0;
                Main.newMusic = 0;
                for (int i = 0; i < Main.musicFade.Length; i++)
                {
                    Main.musicFade[i] = 0;
                }
            }

            bool rCtrl = Keyboard.GetState().IsKeyDown(Keys.RightControl);
            if (!rCtrlLast && rCtrl)
            {

            }
            rCtrlLast = rCtrl;


            if (!Main.playerInventory)
            {
                if (ArmorForgingStationUI.Visible)
                {
                    CalamityEntropy.Instance.armorForgingStationUI.close();
                }
                ArmorForgingStationUI.Visible = false;

            }
            escLast = Keyboard.GetState().IsKeyDown(Keys.Escape);
            if (!Main.dedServ)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.LeftControl) && Keyboard.GetState().IsKeyDown(Keys.N))
                {
                    if (!prd)
                    {
                        prd = true;
                        mi = !mi;
                    }
                }
                else
                {
                    prd = false;
                }
            }

            LoopSoundManager.update();
        }
        public void drawChargeBar(Vector2 center, float prog, Color color)
        {
            Texture2D bar = ModContent.Request<Texture2D>("CalamityEntropy/Content/UI/ui_chargebar").Value;
            Main.spriteBatch.Draw(bar, center, new Rectangle(0, 0, 54, 12), Color.White, 0, new Vector2(27, 6), 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(bar, center, new Rectangle(0, 12, (int)Math.Round(54 * prog), 12), color, 0, new Vector2(27, 6), 1, SpriteEffects.None, 0);
        }
        public void drawChargeBarNoback(Vector2 center, float prog, Color color)
        {
            Texture2D bar = ModContent.Request<Texture2D>("CalamityEntropy/Content/UI/ui_chargebar").Value;
            Main.spriteBatch.Draw(bar, center, new Rectangle(0, 12, (int)Math.Round(54 * prog), 12), color, 0, new Vector2(27, 6), 1, SpriteEffects.None, 0);
        }
        public override void PreUpdateDusts()
        {
            EParticle.updateAll();
        }
        public void drawXythBar()
        {
            if (Main.LocalPlayer.HeldItem.ModItem is Xytheron xr)
            {
                float prog = xr.charge / 20f;
                Vector2 Center = Main.ScreenSize.ToVector2() * 0.5f + new Vector2(0, 56);
                Texture2D bar = CEUtils.getExtraTex("XythBar");
                Main.spriteBatch.Draw(bar, Center, new Rectangle(0, 0, 64, 26), Color.White, 0, new Vector2(32, 13), 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(bar, Center, new Rectangle(0, 26, (int)(8 + 48 * prog), 6), Color.White, 0, new Vector2(32, 1), 1, SpriteEffects.None, 0);
                if (Main.MouseScreen.getRectCentered(2, 2).Intersects(Center.getRectCentered(64, 26)))
                {
                    Main.instance.MouseText(Mod.GetLocalization("XythCharge").Value + ": " + xr.charge.ToString() + "/20");
                }
            }
        }
        public static float lhBarTarget2 = 1;
        public static float lhBarTarget = 1;
        public static float lhRedLerp = 0;
        public static Vector2 bbarOffset = Vector2.Zero;
        private static void DrawLifeBarText(SpriteBatch spriteBatch, Vector2 topLeftAnchor)
        {
            Vector2 vector = topLeftAnchor + new Vector2(130f, -24f);
            Player localPlayer = Main.LocalPlayer;
            Color color = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
            string text = Lang.inter[0].Value + " " + localPlayer.statLifeMax2 + "/" + localPlayer.statLifeMax2;
            Vector2 vector2 = FontAssets.MouseText.Value.MeasureString(text);
            spriteBatch.DrawString(FontAssets.MouseText.Value, Lang.inter[0].Value, vector + new Vector2((0f - vector2.X) * 0.5f, 0f), color, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, localPlayer.statLife + "/" + localPlayer.statLifeMax2, vector + new Vector2(vector2.X * 0.5f, 0f), color, 0f, new Vector2(FontAssets.MouseText.Value.MeasureString(localPlayer.statLife + "/" + localPlayer.statLifeMax2).X, 0f), 1f, SpriteEffects.None, 0f);
        }
        public static float bbar = 0;

        public static void DrawBrambleBar()
        {
            Main.spriteBatch.UseSampleState_UI(SamplerState.PointClamp);
            Texture2D bar = CEUtils.getExtraTex("BrambleBar");
            bbar = float.Lerp(bbar, Main.LocalPlayer.Entropy().BrambleBarCharge, 0.16f);
            Vector2 center = new Vector2(Main.screenWidth / 2, Main.screenHeight / 16);
            if ((center + bbarOffset).getRectCentered(100, 46).Intersects(Main.MouseScreen.getRectCentered(2, 2)))
            {
                Main.instance.MouseText(CalamityEntropy.Instance.GetLocalization("BCBarInfo").Value);
                if (Mouse.GetState().MiddleButton == ButtonState.Pressed)
                    bbarOffset = Main.MouseScreen - center;
            }
            center += bbarOffset;
            Main.spriteBatch.Draw(bar, center, new Rectangle(0, 0, 66, 34), Color.White, 0, new Vector2(33, 17), 1.4f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(bar, center, new Rectangle(12, 36, (int)(42 * bbar), 8), Color.White, 0, new Vector2(21, 5), 1.4f, SpriteEffects.None, 0);
            Main.spriteBatch.UseSampleState_UI(SamplerState.AnisotropicClamp);
        }
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {

            for (int ei = 0; ei < layers.Count; ei++)
            {
                var l = layers[ei];

                if (l.Name == "Vanilla: Resource Bars")
                {
                    var drawLHB = new LegacyGameInterfaceLayer("Lost Heirloom HB", () =>
                    {
                        if (Main.LocalPlayer.dead || !(Main.LocalPlayer.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom)))
                        { return true; }
                        Texture2D t1 = CEUtils.getExtraTex("llBar1");
                        Texture2D t2 = CEUtils.getExtraTex("llBar2");
                        Texture2D t3 = CEUtils.getExtraTex("llBar3");
                        Vector2 offset = new Vector2(Main.screenWidth - 610, 10);
                        Main.spriteBatch.Draw(t3, offset, Color.White);
                        Main.spriteBatch.DrawString(CalamityEntropy.efont2, Main.LocalPlayer.statLife.ToString(), offset + new Vector2(30, 24), Color.White, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
                        Main.spriteBatch.Draw(t1, offset + new Vector2(100, 28), Color.White);
                        Main.spriteBatch.Draw(t2, offset + new Vector2(108, 34), new Rectangle(0, 0, (int)((float)t2.Width * lhBarTarget2), t2.Height), new Color(255, 160, 160));
                        Main.spriteBatch.Draw(t2, offset + new Vector2(108, 34), new Rectangle(0, 0, (int)((float)t2.Width * lhBarTarget), t2.Height), Color.White);
                        Vector2 vector = new Vector2(Main.screenWidth - 300 + 4, 15f);
                        vector.Y += 6f;
                        DrawLifeBarText(Main.spriteBatch, vector + new Vector2(-4f, 3f));
                        typeof(FancyClassicPlayerResourcesDisplaySet).GetMethod("PrepareFields", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, new Type[] { typeof(Player) }).Invoke(((FancyClassicPlayerResourcesDisplaySet)((Dictionary<string, IPlayerResourcesDisplaySet>)Main.ResourceSetsManager.GetType().GetField("_sets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Main.ResourceSetsManager))["New"]), new object[] { Main.LocalPlayer });
                        typeof(FancyClassicPlayerResourcesDisplaySet).GetMethod("DrawManaBar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, new Type[] { typeof(SpriteBatch) }).Invoke(((FancyClassicPlayerResourcesDisplaySet)((Dictionary<string, IPlayerResourcesDisplaySet>)Main.ResourceSetsManager.GetType().GetField("_sets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Main.ResourceSetsManager))["New"]), new object[] { Main.spriteBatch });
                        return true;
                    }, InterfaceScaleType.UI);
                    if (Main.LocalPlayer.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom))
                    {
                        l.Active = false;
                    }
                    layers.Insert(ei,
                        drawLHB
                        );
                    break;
                }
            }
            int mouseIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseIndex != -1)
            {
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("CalamityEntropy: Void Charge Bar", () =>
                {
                    DrawVoidChargeBar(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("CalamityEntropy: Other Charge Bars", () =>
                {
                    int baroffsety = 44;
                    if (Main.LocalPlayer.GetModPlayer<CapricornBookmarkRecordPlayer>().SandStormCharge > 0)
                    {
                        drawChargeBar(Main.ScreenSize.ToVector2() / 2 + new Vector2(0, baroffsety), Main.LocalPlayer.GetModPlayer<CapricornBookmarkRecordPlayer>().SandStormCharge, new Color(246, 201, 122));
                        baroffsety += 20;
                    }
                    if (Main.LocalPlayer.Entropy().SnowgraveCharge > 0)
                    {
                        drawChargeBar(Main.ScreenSize.ToVector2() / 2 + new Vector2(0, baroffsety), Main.LocalPlayer.Entropy().SnowgraveCharge, new Color(170, 170, 255));
                        baroffsety += 20;
                    }
                    if (Main.LocalPlayer.Entropy().mawOfVoidCharge > 0)
                    {
                        drawChargeBar(Main.ScreenSize.ToVector2() / 2 + new Vector2(0, baroffsety), Main.LocalPlayer.Entropy().mawOfVoidCharge, Color.Red);
                        baroffsety += 20;
                    }
                    if (Main.LocalPlayer.Entropy().revelationCharge > 0)
                    {
                        drawChargeBar(Main.ScreenSize.ToVector2() / 2 + new Vector2(0, baroffsety), Main.LocalPlayer.Entropy().revelationCharge, new Color(255, 255, 190));
                        baroffsety += 20;
                    }

                    return true;
                }, InterfaceScaleType.None)); 
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("CalamityEntropy: Poop UI", () =>
                {
                    if (Main.LocalPlayer.Entropy().brokenAnkh)
                    {
                        Main.spriteBatch.UseSampleState_UI(SamplerState.PointClamp);
                        PoopsUI.Draw();
                        Main.spriteBatch.UseSampleState_UI(SamplerState.AnisotropicClamp);
                    }
                    return true;
                }, InterfaceScaleType.UI));
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("CalamityEntropy: Xyth Bar", () =>
                {
                    if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<Bramblecleave>())
                    {
                        DrawBrambleBar();
                    }
                    drawXythBar();
                    return true;
                }, InterfaceScaleType.UI));
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("CalamityEntropy: Durability Bar", () =>
                {
                    if (!Main.LocalPlayer.dead)
                    {
                        if (Main.LocalPlayer.GetModPlayer<AzafureHeavyArmorPlayer>().ArmorSetBonus)
                        {
                            AzafureHeavyArmorPlayer.DrawDuraBar(Main.LocalPlayer.GetModPlayer<AzafureHeavyArmorPlayer>().durability);
                        }
                        if (Main.LocalPlayer.GetModPlayer<AzafureSteamKnightArmorPlayer>().ArmorSetBonus)
                        {
                            AzafureSteamKnightArmorPlayer.DrawDuraBar(Main.LocalPlayer.GetModPlayer<AzafureSteamKnightArmorPlayer>().durability);
                        }
                        if (Main.LocalPlayer.GetModPlayer<AcropolisArmorPlayer>().ArmorSetBonus)
                        {
                            AcropolisArmorPlayer.DrawDuraBar(Main.LocalPlayer.GetModPlayer<AcropolisArmorPlayer>().durability);
                        }
                    }
                    return true;
                }, InterfaceScaleType.UI));
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer(
                "CalamityEntropy: Armor Reforging Station",
                delegate
                {
                    if (ArmorForgingStationUI.Visible)
                        CalamityEntropy.Instance.armorForgingStationUI.Draw(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.UI)
            );
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer(
                "CalamityEntropy: EntropyBookItemUI",
                delegate
                {
                    EBookUI.draw();
                    return true;
                },
                InterfaceScaleType.UI)
            );
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer(
                "CalamityEntropy: Dialog UI",
                delegate
                {
                    if (Typer.activeTypers.Count > 0)
                    {
                        Typer.activeTypers[0].sound = new Terraria.Audio.SoundStyle($"CalamityMod/Sounds/Custom/Codebreaker/DraedonTalk{Main.rand.Next(1, 4)}");

                        var t = Typer.activeTypers[0];
                        t.update();
                        t.draw();

                        if (Typer.activeTypers[0].Finish() && Main.mouseLeft && !MLPrd && !Main.LocalPlayer.mouseInterface)
                        {
                            Typer.activeTypers.RemoveAt(0);
                        }
                        MLPrd = Main.mouseLeft;
                    }
                    return true;
                },
                InterfaceScaleType.UI));

            }
        }

        public bool MLPrd = false;

        public void DrawVoidChargeBar(SpriteBatch spriteBatch)
        {
            if (!Main.LocalPlayer.Entropy().VFSet)
            {
                return;
            }
            Texture2D bar = ModContent.Request<Texture2D>("CalamityEntropy/Content/UI/VoidChargeBar").Value;
            Texture2D prog = ModContent.Request<Texture2D>("CalamityEntropy/Content/UI/VoidChargeProgress").Value;
            Config config = ModContent.GetInstance<Config>();
            Vector2 offset = new Vector2(config.VoidChargeBarX, config.VoidChargeBarY);
            float p = Main.LocalPlayer.Entropy().VoidCharge;
            if (Main.LocalPlayer.Entropy().VoidInspire > 0)
            {
                p = Main.LocalPlayer.Entropy().VoidInspire / 600f;
                offset += new Vector2(Main.rand.Next(-2, 3), Main.rand.Next(-2, 3));
            }
            spriteBatch.Draw(bar, offset, null, Color.White, 0, bar.Size() / 2, 1, SpriteEffects.None, 0);
            spriteBatch.Draw(prog, offset, new Rectangle(0, 0, (int)(prog.Width * p), prog.Height), Color.White, 0, prog.Size() / 2, 1, SpriteEffects.None, 0);
            Rectangle mouse = new Rectangle((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 1, 1);
            if (new Rectangle((int)(offset - bar.Size() / 2).X, (int)((offset - bar.Size() / 2).Y), (int)bar.Size().X, (int)bar.Size().Y).Intersects(mouse))
            {
                string textToDisplay = Language.GetOrRegister("Mods.CalamityEntropy.VoidChargeBar").Value + " : " + ((int)(p * 100)).ToString() + "%";
                Main.instance.MouseText(textToDisplay, 0, 0, -1, -1, -1, -1);
            }
        }

        public override void PostUpdateNPCs()
        {
            if (ModLoader.HasMod("Fargowiltas"))
                foreach (NPC npc in Main.ActiveNPCs)
                    if (npc.type == 0)
                        npc.active = false;
            if (CalamityEntropy.Instance.screenShakeAmp > 0)
            {
                CalamityEntropy.Instance.screenShakeAmp -= 0.5f;
            }
            bool eow = false;
            int maxlifeEows = 0;
            bool sg = false;
            int maxlifeSg = 0;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsTail)
                {
                    eow = true;
                    maxlifeEows += n.lifeMax;
                }
                if (ModContent.NPCType<CrimulanPaladin>() == n.type || ModContent.NPCType<SplitCrimulanPaladin>() == n.type || ModContent.NPCType<EbonianPaladin>() == n.type || ModContent.NPCType<SplitEbonianPaladin>() == n.type)
                {
                    sg = true;
                    maxlifeSg += n.lifeMax;
                }
            }
            if (eow && !eowLast)
            {
                eowMaxLife = maxlifeEows;
            }
            eowLast = eow;

            if (sg && !slimeGodLast)
            {
                slimeGodMaxLife = maxlifeSg;
            }

            slimeGodLast = sg;

        }

        public static void RemoveItemInARecipe(Recipe recipe, int type)
        {
            for (int i = recipe.requiredItem.Count - 1; i >= 0; i--)
            {
                if (recipe.requiredItem[i].type == type)
                {
                    recipe.requiredItem.RemoveAt(i);
                }
            }
        }
        public static void RemoveItemInRecipes(int itemtype, int type)
        {
            for (int i = 0; i < Main.recipe.Length; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe.createItem.type == itemtype)
                {
                    RemoveItemInARecipe(recipe, type);
                }
            }
        }

        public override void PostAddRecipes()
        {
            Recipe.Create(ItemID.BloodMoonStarter)
                .AddRecipeGroup(CERecipeGroups.evilBar, 4)
                .AddIngredient(ItemID.Lens, 4)
                .AddTile(TileID.DemonAltar)
                .Register();
            foreach (var recipe in Main.recipe)
            {
                if (recipe.createItem.type == ModContent.ItemType<CalamityMod.Items.Placeables.FurnitureAuric.AuricToilet>())
                {
                    recipe.createItem.type = ModContent.ItemType<Content.Items.AuricToilet>();
                }
            }
            RemoveItemInRecipes(ModContent.ItemType<Sylvestaff>(), ItemID.GenderChangePotion);
        }

        public override void PreUpdateProjectiles()
        {
            timer++;
            if (!Main.dedServ)
            {
                LastPlayerPos = Main.LocalPlayer.Center;
            }
        }

        public override void PostUpdateProjectiles()
        {
            CEUtils.Update();
            if (EGlobalProjectile.SSCD < 3)
            {
                EGlobalProjectile.SSCD++;
            }

            if (!Main.dedServ)
            {
            }
        }
    }
}
