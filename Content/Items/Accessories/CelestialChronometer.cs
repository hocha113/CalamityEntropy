using CalamityEntropy.Content.Buffs.PortsDoT;
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
            // 配方与效果都随时代走,防御也一起:装灾厄时交的是 3.33 那三件成品饰品,回 3.33 的 28 防
            Item.defense = CERef.Has ? 28 : 8;
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
            // 两个时代整方法二分,不叠加。装灾厄时配方要交出血神圣杯、阴阳吸星石与辐辉
            // 三件成品饰品,效果若停在 4.0 平衡案那套,合成即降级。
            if (CERef.Has)
            {
                ApplyCalamityEraEffects(player, hideVisual, c);
                return;
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

        /// <summary>
        /// 3.33 装灾厄时的形态。那一版本体只有两条效果,其余全靠转调血神圣杯、
        /// 阴阳吸星石与辐辉三件的 UpdateAccessory。这里不转调那三个方法(它们会
        /// 引用灾厄类型,撞零编译期耦合),改为原版字段直接给 + 三个灾厄侧旗标经
        /// CECal 反射写入。三个旗标都由灾厄每帧在 ResetEffects 归位,写它们等价于
        /// 灾厄自家饰品在 UpdateAccessory 里做的事。
        /// <para>草药种植仍由 4.0 的 herbPlanting 开关把关而不是 3.33 的 !hideVisual:
        /// 那个开关连着存档、联机同步与格内小圆点,按 3.33 改回去只会让开关变成死的。</para>
        /// </summary>
        private static void ApplyCalamityEraEffects(Player player, bool hideVisual, Vector2 c)
        {
            // 3.33 本体的两条
            player.Entropy().lifeRegenPerSec += 4;
            if (CEUtils.inWorld((int)c.X, (int)c.Y) && Main.tile[(int)c.X, (int)c.Y].HasTile)
            {
                int type = Main.tile[(int)c.X, (int)c.Y].TileType;
                if (type >= 82 && type <= 84)
                {
                    player.endurance += 0.2f;
                }
            }
            // 血神圣杯:神话护身符、+4 生命再生、出血系免疫、+25%生命上限与伤害延迟结算
            player.pStone = true;
            player.lifeRegen += 4;
            player.buffImmune[BuffID.Bleeding] = true;
            SetBuffImmune(player, ModContent.BuffType<BurningBlood>());
            SetBuffImmune(player, ModContent.BuffType<HeavyBleeding>());
            SetBuffImmune(player, ModContent.BuffType<Laceration>());
            // 自有端口与灾厄本体是两套独立的减益类型,装灾厄时两套都会挂上,要一起免
            SetBuffImmune(player, CEID.Buff_BurningBlood);
            SetBuffImmune(player, CEID.Buff_HeavyBleeding);
            SetBuffImmune(player, CEID.Buff_Laceration);
            CECal.GrantChaliceOfTheBloodGod(player, !hideVisual);
            // 阴阳吸星石:免疫击退 + 受击光环、移速跳跃、荆棘与命中回血
            player.noKnockback = true;
            CECal.GrantAbsorber(player);
            // 辐辉:按缺失生命的动态再生与 DoT 削减
            CECal.GrantRadiance(player);
            if (!hideVisual)
            {
                Lighting.AddLight(player.Center, new Vector3(1.32f, 1.32f, 1.82f));
            }
        }

        private static void SetBuffImmune(Player player, int buffType)
        {
            if (buffType > 0 && buffType < player.buffImmune.Length)
            {
                player.buffImmune[buffType] = true;
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
            // 装灾厄整段换成灾厄时代键,无灾厄不碰,Items.CelestialChronometer.Tooltip 保持 4.0 原文
            if (CERef.Has)
            {
                ReplaceTooltipWithCalEra(tooltips);
            }
            foreach (var t in tooltips)
            {
                // 灾厄时代那段是本模组自己插的行,Mod 名不是 Terraria,所以只认 $ 标记
                if (t.Text.Contains("$"))
                {
                    t.OverrideColor = Color.Lerp(Color.White, Main.DiscoColor, (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 10) * 0.5f + 0.5f));
                }
            }
        }

        private void ReplaceTooltipWithCalEra(List<TooltipLine> tooltips)
        {
            string cal = Mod.GetLocalization("CelestialChronometerCal").Value;
            int insertAt = -1;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                {
                    insertAt = i;
                    break;
                }
            }
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                    tooltips.RemoveAt(i);
            }
            if (insertAt < 0)
                insertAt = tooltips.Count;
            string[] lines = cal.Replace("\r\n", "\n").Split('\n');
            int offset = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                tooltips.Insert(insertAt + offset, new TooltipLine(Mod, "TooltipCal" + offset, line));
                offset++;
            }
        }
    }
}
