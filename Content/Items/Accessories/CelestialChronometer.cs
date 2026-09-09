using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class CelestialChronometer : ModItem, IDonatorItem
    {
        public string DonatorName => "丰川祥子";
        // 2026-08-31 平衡案重做:8防,+75生命,神话护身符的-25%药水冷却,-33%减益持续,
        // 大幅自然再生(6hp/s)且直接回血(1hp/s),走过草地长出草药(shift右键开关),站在草药上+20防御。
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = CECal.RarityTurquoise(ModContent.RarityType<NihilityBlue>());
            Item.accessory = true;
            Item.defense = 8;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            Vector2 c = (player.Center + new Vector2(0, player.height / 2 - 2)) / 16;
            if (herbPlanting && Main.rand.NextBool(10))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && TileLoader.CanPlace((int)c.X, (int)c.Y, 84) && Main.tile[(int)c.X, (int)c.Y + 1].HasTile)
                {
                    List<int> CanPlace = new();
                    int t = Main.tile[(int)c.X, (int)c.Y + 1].TileType;
                    bool cpls = (!((Main.tile[(int)c.X, (int)c.Y + 1]).Get<TileWallWireStateData>().Slope != SlopeType.Solid || (Main.tile[(int)c.X, (int)c.Y + 1]).Get<TileWallWireStateData>().IsHalfBlock));
                    if (cpls)
                    {
                        if (t == 0 || t == 59)
                        {
                            CanPlace.Add(2);
                        }
                        if (t == 2 || t == 109 || t == 477 || t == 492)
                        {
                            CanPlace.Add(0);
                        }
                        if (t == 23 || t == 661 || t == 199 || t == 662 || t == 15 || t == 203)
                        {
                            CanPlace.Add(3);
                        }
                        if (t == 57 || t == 633)
                        {
                            CanPlace.Add(5);
                        }
                        if (t == 53 || t == 234)
                        {
                            CanPlace.Add(4);
                        }
                        if (t == 60)
                        {
                            CanPlace.Add(1);
                        }
                        if (t == 147 || t == 161 || t == 163 || t == 164 || t == 200)
                        {
                            CanPlace.Add(7);
                        }
                        if (CanPlace.Count > 0)
                        {
                            short fx = (short)(18 * CanPlace[Main.rand.Next(CanPlace.Count)]);
                            var tl = CEUtils.PlaceTile((int)c.X, (int)c.Y, 83);
                            tl.Get<TileWallWireStateData>().TileFrameX = fx;
                            tl.Get<TileWallWireStateData>().TileFrameY = 0;
                        }
                    }
                }
            }
            player.statLifeMax2 += 75;
            // 神话护身符效果(-25%治疗药水冷却与其生命再生)
            player.pStone = true;
            // 减少33%减益持续时间(与净化卡同一通道)
            player.Entropy().DebuffTime -= 0.33f;
            // 大幅自然再生(6hp/s)+直接回血(1hp/s)
            player.lifeRegen += 12;
            player.Entropy().lifeRegenPerSec += 1;
            // 站在草药上+20防御
            if (CEUtils.inWorld((int)c.X, (int)c.Y) && Main.tile[(int)c.X, (int)c.Y].HasTile)
            {
                int type = Main.tile[(int)c.X, (int)c.Y].TileType;
                if (type >= 82 && type <= 84)
                {
                    player.statDefense += 20;
                }
            }
        }

        #region 草药种植开关(shift右键)
        private bool herbPlanting = true;
        public override bool CanRightClick() => Main.keyState.PressingShift();
        public override void RightClick(Player player)
        {
            herbPlanting = !herbPlanting;
            Item.NetStateChanged();
        }
        public override bool ConsumeItem(Player player) => false;
        public override void SaveData(TagCompound tag)
        {
            tag.Add("herb", herbPlanting);
        }
        public override void LoadData(TagCompound tag)
        {
            herbPlanting = !tag.ContainsKey("herb") || tag.GetBool("herb");
        }
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(herbPlanting);
        }
        public override void NetReceive(BinaryReader reader)
        {
            herbPlanting = reader.ReadBoolean();
        }
        public override void PostDrawInInventory(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            CEUtils.DrawInventoryDot(spriteBatch, position, new Vector2(16, 16) * Main.inventoryScale, herbPlanting);
        }
        #endregion

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ChaliceOfTheBloodGod, CEID.Item_TheAbsorber, CEID.Item_Radiance))
            {
                CreateRecipe().
                    AddIngredient(CEID.Item_ChaliceOfTheBloodGod).
                    AddIngredient(CEID.Item_TheAbsorber).
                    AddIngredient(CEID.Item_Radiance).
                    AddIngredient(5295).
                    AddIngredient<FadingRunestone>(3).
                    AddTile<VoidWellTile>().
                    Register();
                return;
            }
            CreateRecipe().
                    AddIngredient(ItemID.ShinyStone).
                    AddIngredient(ModContent.ItemType<SilvasCrown>()).
                    AddIngredient(ItemID.CharmofMyths).
                    AddIngredient(ItemID.AcornAxe).
                    AddIngredient(ModContent.ItemType<ChaoticPiece>(), 10).
                    AddTile(TileID.LunarCraftingStation).
                    Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (var t in tooltips)
            {
                if (t.Mod == "Terraria")
                {
                    if (t.Text.Contains("$"))
                    {
                        t.OverrideColor = Color.Lerp(Color.White, Main.DiscoColor, (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 10) * 0.5f + 0.5f));
                    }
                }
            }
        }
    }
}
