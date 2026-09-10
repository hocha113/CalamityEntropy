using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Tiles;
using InnoVault;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
namespace CalamityEntropy.Content.Items.Donator
{
    public class TlipocasScythe : ModItem, ICEChargeWeapon, IDevItem
    {
        // 充能条 5 秒（成长武器各阶段共用同一充能）；原潜伏乘数 击退2 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(5f, knockbackMult: 2f);

        //本体/异色两套刀身贴图,加载期就位,按玩家外观状态切换
        [VaultLoaden("CalamityEntropy/Content/Items/Donator/TlipocasScythe")]
        internal static Asset<Texture2D> ScytheTex;
        [VaultLoaden("CalamityEntropy/Content/Items/Donator/Scythe2")]
        internal static Asset<Texture2D> ScytheAltTex;
        public static Asset<Texture2D> GetTexture(Player player)
        {
            if (player != null && AltType(player))
                return ScytheAltTex;
            return ScytheTex;
        }
        public static bool AltType(Player player)
        {
            return ((player.name.ToLower() == "kanna" || StartBagGItem.NameContains(player, "akizukikanna")) || (player.TryGetModPlayer<VanityModPlayer>(out var vnp) && vnp.vanityEquippedLast.Equals("ReapersButterfly")));//(player.TryGetModPlayer<PGetPlayer>(out var mp) && mp.accVanity) || 
        }
        public static Color TrailColor(Projectile Projectile)
        {
            Player player = Projectile.GetOwner();
            if (player.HasBuff<VoidEmpowerment>())
                return Color.Purple;
            if (player != null && AltType(player))
                return new Color(217, 214, 255) * 0.7f;
            return Color.Firebrick;
        }
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public string DevName => "Kino";

        public int SpeedUpTime = 0;
        /// <summary>装灾厄走 3.33 的 16 段细档阶梯,无灾厄保持 4.0 常数 16 级</summary>
        public static int GetLevel()
        {
            if (!CERef.Has)
            {
                return 16;
            }
            int Level = 0;
            bool flag = true;
            void Check(bool f)
            {
                if (f && flag)
                {
                    Level++;
                }
                else
                {
                    flag = false;
                }
            }
            Check(NPC.downedBoss1);
            Check(NPC.downedBoss2 || CECal.DownedPerforator || CECal.DownedHiveMind);
            Check(CECal.DownedSlimeGod);
            Check(Main.hardMode);
            Check(CECal.DownedBrimstoneElemental);
            Check(CECal.DownedCalamitasClone(NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3));
            Check(EDownedBosses.downedProphet);
            Check(CECal.DownedRavager);
            Check(NPC.downedAncientCultist);
            Check(NPC.downedMoonlord);
            Check(CECal.DownedSignus);
            Check(CECal.DownedPolterghast);
            Check(CECal.DownedDoG(EDownedBosses.downedCruiser));
            Check(EDownedBosses.downedCruiser);
            Check(CECal.DownedCalamitas(EDownedBosses.downedCruiser) && CECal.DownedExoMechs(EDownedBosses.downedCruiser));
            Check(CECal.DownedPrimordialWyrm);
            return Level;
        }
        public int NowLevel = 0;
        public bool RecheckStats = true;
        private static float UpdatePos
        {
            get
            {
                return ((float)(MathF.Sin(Main.GlobalTimeWrappedHourly * 1f) * 1.2f + 1.4f)).ToClamp(1.0f, 2.4f);
            }
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D tex = TlipocasScythe.GetTexture(Main.LocalPlayer).Value;
            Vector2 position = Item.position - Main.screenPosition + tex.Size() / 2;
            Rectangle iFrame = tex.Frame();
            for (int i = 0; i < 16; i++)
                spriteBatch.Draw(tex, position + MathHelper.ToRadians(i * 60f).ToRotationVector2() * 4f, null, (!AltType(Main.LocalPlayer) ? Color.DarkRed : Color.Silver) with { A = 0 }, rotation, tex.Size() / 2, scale, 0, 0f);

            spriteBatch.Draw(tex, position, iFrame, Color.White, rotation, tex.Size() / 2, scale, 0, 0f);
            Lighting.AddLight(position, TorchID.Red);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!AltType(Main.LocalPlayer))
                tooltips.Add(new TooltipLine(Mod, "Description", Mod.GetLocalization(Main.zenithWorld ? "TScytheZenithDesc" : "TScytheDesc").Value) { OverrideColor = Color.Crimson });
            bool holdAlt = Keyboard.GetState().IsKeyDown(Keys.LeftAlt);
            bool holdShift = Keyboard.GetState().IsKeyDown(Keys.LeftShift);
            if (holdShift && holdAlt)
                holdAlt = false;
            #region 路径
            string pathPrefix = $"{CEUtils.LocalPrefix}.LegendaryAbility.";
            string lockedPath = pathPrefix + "General.Locked";
            string pathBase = pathPrefix + $"{GetType().Name}Legend.";
            string pathLore = pathBase + "Dialog.TScytheDia";
            string pathAbility = pathBase + "Ability.TScytheA";
            string pathCondition = pathBase + "Downed.TScytheU";
            #endregion
            string throwTooltip = AbilityThrowDesc(pathAbility, pathCondition, lockedPath);
            string teleportTooltip = AbilityTeleportDesc(pathAbility, pathCondition, lockedPath);
            string dashTooltip = AbilityDashDesc(pathAbility, pathCondition, lockedPath);
            string statTooltip = AbilityStat(pathAbility, pathCondition, lockedPath);

            bool isNeither = !holdShift && !holdAlt;
            bool shouldDrawHoldshift = !holdAlt && holdShift;
            bool shouldDrawPages = !holdShift && holdAlt;

            bool shouldDrawPagesTips = isNeither || shouldDrawHoldshift;
            bool shouldDrawHoldShiftTips = isNeither || shouldDrawPages;
            bool any = holdAlt || holdShift;

            if (shouldDrawHoldshift)
            {
                HandleHoldShift(tooltips);
            }
            if (shouldDrawPages)
                HandleSwapAbility(tooltips, throwTooltip, teleportTooltip, dashTooltip, statTooltip);
            if (shouldDrawPagesTips && !any)
                tooltips.QuickAddTooltip($"{pathPrefix}General.PagesTips", Color.Yellow, LineName: "PagesMoreInfo");
            if (shouldDrawHoldShiftTips && !any)
                tooltips.QuickAddTooltipDirect(Mod.GetLocalization("PressShiftForMoreInfo").Value, Color.Yellow, LineName: "ShiftMoreInfo");
            bool isKilledNPC = Main.LocalPlayer.GetModPlayer<TlipocasSingleHit>().isPressed;
            if (isKilledNPC)
                tooltips.QuickAddTooltip($"{pathBase}TlipocazKilledNPC", Color.Red, LineName: "CanKilledNPC");
            else
                tooltips.QuickAddTooltip($"{pathBase}TlipocazKilledNPCNot", Color.Gray, LineName: "CanNotKileldNPC");
            HandleLoreAndLevel(tooltips, pathLore);
        }
        /// <summary>Shift 详表:装灾厄读 *Cal 键(3.33 灾厄 Boss 名),无灾厄读 4.0 原键</summary>
        private string GetGate(string key)
        {
            if (CERef.Has)
            {
                return Mod.GetLocalization(key + "Cal").Value;
            }
            return Mod.GetLocalization(key).Value;
        }
        /// <summary>Alt 翻页锁定句同样按 CERef.Has 分发;无对应 Cal 键时保持 4.0</summary>
        private static string GateDowned(string pathCondition, string suffix)
        {
            bool useCal = CERef.Has && (suffix == "1B" || suffix == "2B" || suffix == "5" || suffix == "6" || suffix == "7" || suffix == "9");
            return (pathCondition + suffix + (useCal ? "Cal" : "")).ToLangValue();
        }
        private void HandleHoldShift(List<TooltipLine> tooltips)
        {
            string Get(string key)
            {
                return Mod.GetLocalization(key).Value;
            }
            bool flag = NPC.downedBoss1;
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA1") + (flag ? "" : Get("LOCKED") + " " + Get("TSU1")))
            { OverrideColor = flag ? Color.Yellow : Color.Gray });

            flag = CECal.DownedSlimeGod;
            tooltips.Add(new TooltipLine(Mod, "Ability Desc", GetGate("TSA1B"))
            { OverrideColor = flag ? Color.Yellow : Color.Gray });

            flag = AllowThrow();
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA2") + (flag ? "" : Get("LOCKED") + " " + Get("TSU2")))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });

            flag = CECal.DownedBrimstoneElemental;
            tooltips.Add(new TooltipLine(Mod, "Ability Desc", GetGate("TSA2B"))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });

            flag = EDownedBosses.downedProphet;
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA3") + (flag ? "" : Get("LOCKED") + " " + Get("TSU3")))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });

            flag = EDownedBosses.downedNihilityTwin;
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA4") + (flag ? "" : Get("LOCKED") + " " + Get("TSU4")))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });

            flag = CECal.DownedPolterghast;
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA5") + (flag ? "" : Get("LOCKED") + " " + GetGate("TSU5")))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });

            // 灾厄在场读神明吞噬者,缺席回落巡游者
            flag = CECal.DownedDoG(EDownedBosses.downedCruiser);
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA6") + (flag ? "" : Get("LOCKED") + " " + GetGate("TSU6")))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });

            flag = CECal.DownedYharon(EDownedBosses.downedCruiser);
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA7") + (flag ? "" : Get("LOCKED") + " " + GetGate("TSU7")))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });

            flag = EDownedBosses.downedCruiser;
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA8") + (flag ? "" : Get("LOCKED") + " " + Get("TSU8")))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });

            flag = CECal.DownedCalamitas(EDownedBosses.downedCruiser);
            tooltips.Add(new TooltipLine(Mod, "Ability Desc",
                Get("TSA9") + (flag ? "" : Get("LOCKED") + " " + GetGate("TSU9")))
            { OverrideColor = (flag ? Color.Yellow : Color.Gray) });
        }
        #region Lore，与等级
        private void HandleLoreAndLevel(List<TooltipLine> tooltips, string pathLore)
        {
            int LoreLevel = Math.Clamp(GetLevel() + 1, 1, 17);
            string curLore = pathLore + LoreLevel.ToString();
            tooltips.QuickAddTooltip(curLore, Color.HotPink);
            string curLevel = Mod.GetLocalization("NowLV").Value + " - " + GetLevel() + "/16";
            TooltipLine levelTooltip = new(Mod, "CurLevel", curLevel) { OverrideColor = Color.Yellow };
            tooltips.Add(levelTooltip);
        }
        #endregion
        #region 文本翻页
        private void HandleSwapAbility(List<TooltipLine> tooltips, string throwTooltip, string teleportTooltip, string dashTooltip, string statTooltip)
        {
            string selectedOne;
            selectedOne = SelectedDesc switch
            {
                AbilityDescSelect.Throw => throwTooltip,
                AbilityDescSelect.Dash => dashTooltip,
                AbilityDescSelect.Teleport => teleportTooltip,
                _ => statTooltip,
            };
            tooltips.QuickAddTooltipDirect(selectedOne);
        }

        private string AbilityThrowDesc(string pathAbility, string pathCondition, string lockedPath)
        {
            string titleText = $"{CEUtils.LocalPrefix}.LegendaryAbility.{GetType().Name}Legend.Conditions.ThrowTitle".ToLangValue();
            string lockedValue = lockedPath.ToLangValue();
            string baseThrowText = $"{pathAbility}2".ToLangValue();
            string downedEvilText = $"{lockedValue} {$"{pathCondition}2".ToLangValue()}";
            bool downedAnyEvil = NPC.downedBoss2 || CECal.DownedPerforator || CECal.DownedHiveMind;
            string pressThrowText = $"{pathAbility}3".ToLangValue();
            string downedProphetText = $"{lockedValue} {$"{pathCondition}3".ToLangValue()}";
            baseThrowText = downedAnyEvil ? DyeText(baseThrowText, Color.Yellow) : DyeText(downedEvilText + "\n" + baseThrowText, Color.Gray);
            pressThrowText = EDownedBosses.downedProphet ? DyeText(pressThrowText, Color.Yellow) : DyeText(downedProphetText + "\n" + pressThrowText, Color.Gray);
            string combination = DyeText(titleText, Color.Crimson)
       + "\n" + baseThrowText
       + "\n" + pressThrowText;
            return combination;
        }

        private string AbilityTeleportDesc(string pathAbility, string pathCondition, string lockedPath)
        {
            string titleText = $"{CEUtils.LocalPrefix}.LegendaryAbility.{GetType().Name}Legend.Conditions.TeleportTitle".ToLangValue();
            string lockedValue = lockedPath.ToLangValue();
            //冷却30/20秒已写进A2B文案本体;原ToFormatValue无占位符是no-op且携带错误数值(10/15),一并移除
            string allowTeleportSlice = $"{pathAbility}2B".ToLangValue();
            string downedBrimmyText = $"{lockedValue} {GateDowned(pathCondition, "2B")}";

            //错位修正:虚空赋能段应读A4(原误读A5影子斩击);赋能8/15秒与+20%已写进A4文案本体
            string enchanted = $"{pathAbility}4".ToLangValue();
            string downedPolterText = $"{lockedValue} {GateDowned(pathCondition, "5")}";

            bool downedDoG = CECal.DownedDoG(EDownedBosses.downedCruiser);
            string dogText = DyeText(downedDoG ? $"{pathAbility}6".ToLangValue() : $"{lockedValue} {GateDowned(pathCondition, "6")}", downedDoG ? Color.Yellow : Color.Gray);

            //与 Shift 页 TSA2B / 真实传送门槛对齐,不再用克眼给传送行上色
            allowTeleportSlice = CECal.DownedBrimstoneElemental ? DyeText(allowTeleportSlice, Color.Yellow) : DyeText(downedBrimmyText + "\n" + allowTeleportSlice, Color.Gray);
            enchanted = CECal.DownedPolterghast ? DyeText(enchanted, Color.Yellow) : DyeText(downedPolterText + "\n" + enchanted, Color.Gray);

            string combination = DyeText(titleText, Color.Crimson)
                   + "\n" + allowTeleportSlice
                   + "\n" + enchanted
                   + "\n" + dogText;
            return combination;
        }
        private string AbilityDashDesc(string pathAbility, string pathCondition, string lockedPath)
        {
            string titleText = $"{CEUtils.LocalPrefix}.LegendaryAbility.{GetType().Name}Legend.Conditions.DashTitle".ToLangValue();
            string lockedValue = lockedPath.ToLangValue();
            string allowDashText = $"{pathAbility}1".ToLangValue();
            string downedEoCText = $"{lockedValue} {GateDowned(pathCondition, "1")}";
            //错位修正:突刺第二段应读A6裂缝能力(原误读A5),与U6解锁条件对齐
            string tearDashText = $"{pathAbility}6".ToLangValue();
            string downedDoGText = $"{lockedValue} {GateDowned(pathCondition, "6")}";

            allowDashText = NPC.downedBoss1 ? DyeText(allowDashText, Color.Yellow) : DyeText(downedEoCText + "\n" + allowDashText, Color.Gray);
            tearDashText = CECal.DownedDoG(EDownedBosses.downedCruiser) ? DyeText(tearDashText, Color.Yellow) : DyeText(downedDoGText + "\n" + tearDashText, Color.Gray);

            string combination = DyeText(titleText, Color.Crimson)
                   + "\n" + allowDashText
                   + "\n" + tearDashText;
            return combination;
        }

        private string AbilityStat(string pathAbility, string pathCondition, string lockedPath)
        {
            string titleText = $"{CEUtils.LocalPrefix}.LegendaryAbility.{GetType().Name}Legend.Conditions.StatTitle".ToLangValue();
            string lockedValue = lockedPath.ToLangValue();

            string invinciDashText = $"{pathAbility}1B".ToLangValue();
            string downedSGLocked = $"{lockedValue} {GateDowned(pathCondition, "1B")}";
            string selfReviveText = $"{pathAbility}7".ToLangValue();
            string dowendYharonLocked = $"{lockedValue} {GateDowned(pathCondition, "7")}";
            string voidTouchText = $"{pathAbility}8".ToLangValue();
            string downedPurpleWormLocked = $"{lockedValue} {GateDowned(pathCondition, "8")}";
            string closeDamageText = $"{pathAbility}9".ToLangValue();
            string dowendScalLocked = $"{lockedValue} {GateDowned(pathCondition, "9")}";

            invinciDashText = CECal.DownedSlimeGod ? DyeText(invinciDashText, Color.Yellow) : DyeText(downedSGLocked + "\n" + invinciDashText, Color.Gray);
            selfReviveText = CECal.DownedYharon(EDownedBosses.downedCruiser) ? DyeText(selfReviveText, Color.Yellow) : DyeText(dowendYharonLocked + "\n" + selfReviveText, Color.Gray);
            voidTouchText = EDownedBosses.downedCruiser ? DyeText(voidTouchText, Color.Yellow) : DyeText(downedPurpleWormLocked + "\n" + voidTouchText, Color.Gray);
            closeDamageText = CECal.DownedCalamitas(EDownedBosses.downedCruiser) ? DyeText(closeDamageText, Color.Yellow) : DyeText(dowendScalLocked + "\n" + closeDamageText, Color.Gray);

            string combination = DyeText(titleText, Color.Crimson)
                   + "\n" + invinciDashText
                   + "\n" + selfReviveText
                   + "\n" + voidTouchText
                   + "\n" + closeDamageText;
            return combination;
        }
        private static string DyeText(string textValue, Color color)
        {
            string colorValue = $"{color.R:X2}{color.G:X2}{color.B:X2}";
            string[] lines = textValue.Split(['\n'], StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = $"[c/{colorValue}:{lines[i]}]";
            }
            string realValue = string.Join("\n", lines);
            return realValue;
        }
        private enum AbilityDescSelect
        {
            Stat,
            Throw,
            Dash,
            Teleport
        }
        private AbilityDescSelect SelectedDesc = AbilityDescSelect.Stat;
        public override bool CanRightClick() => Keyboard.GetState().IsKeyDown(Keys.LeftAlt);
        public override bool ConsumeItem(Player player) => false;
        public override void RightClick(Player player)
        {
            SelectedDesc++;
            if ((int)SelectedDesc % 4 is 0)
                SelectedDesc = AbilityDescSelect.Stat;
        }
        #endregion
        public override void UpdateInventory(Player player)
        {
            int lv = GetLevel();
            if (NowLevel != lv || RecheckStats)
            {
                RecheckStats = false;
                NowLevel = lv;
                int dmg = 10;
                switch (lv)
                {
                    case 0: dmg = 10; break;
                    case 1: dmg = 18; break;
                    case 2: dmg = 30; break;
                    case 3: dmg = 45; break;
                    case 4: dmg = 72; break;
                    case 5: dmg = 130; break;
                    case 6: dmg = 180; break;
                    case 7: dmg = 250; break;
                    case 8: dmg = 270; break;
                    case 9: dmg = 300; break;
                    case 10: dmg = 480; break;
                    case 11: dmg = 580; break;
                    case 12: dmg = 750; break;
                    case 13: dmg = 1000; break;
                    case 14: dmg = 1300; break;
                    case 15: dmg = 1800; break;
                    case 16: dmg = 2700; break;
                }
                Item.damage = dmg;
                Item.crit = lv;
                Item.knockBack = lv / 2;
                Item.scale = 1;
                Item.Prefix(Item.prefix);
            }
            else
            {
                Item.ClearNameOverride();
                Item.Entropy().NameColor = new Color(160, 0, 0);
                Item.Entropy().strokeColor = new Color(90, 0, 0);
                if (player != null && AltType(player))
                {
                    Item.Entropy().NameColor = new Color(60, 60, 60);
                    Item.Entropy().strokeColor = new Color(210, 210, 230);
                    Item.SetNameOverride(Mod.GetLocalization("TScytheSpecialName2").Value);
                }
                else if (player != null && (player.name.ToLower() is "tlipoca" or "kino" || (player.TryGetModPlayer<VanityModPlayer>(out var mp) && mp.vanityEquippedLast == "BlackFlower")))
                {
                    Item.SetNameOverride(Mod.GetLocalization("TScytheSpecialName").Value);
                }
                else if (Main.zenithWorld)
                {
                    Item.SetNameOverride(Mod.GetLocalization("TScytheZenithName").Value);
                }

                if (throwType == -1)
                    throwType = ModContent.ProjectileType<TlipocasScytheThrow>();
                FuncKilledTownNPC(player);
            }
            Item.useTime = Item.useAnimation = (40 - GetLevel()) / (SpeedUpTime > 0 ? 6 : 1);
        }
        private void FuncKilledTownNPC(Player player)
        {
            if (Item.favorited && Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                player.Entropy().CanSlainTownNPC = true;
        }
        public override void HoldItem(Player player)
        {
            UpdateInventory(player);
            if (SpeedUpTime > 0)
                SpeedUpTime--;
            if (SpeedUpTime < 0)
                SpeedUpTime = 0;
        }
        public override void SetDefaults()
        {
            Item.width = 132;
            Item.height = 116;
            Item.damage = 22;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shootSpeed = 8f;
            Item.ArmorPenetration = 15;
            Item.shoot = ModContent.ProjectileType<TlipocasScytheHeld>();
            Item.Entropy().tooltipStyle = 4;
            Item.Entropy().NameColor = new Color(160, 0, 0);
            Item.Entropy().strokeColor = new Color(90, 0, 0);
            Item.Entropy().stroke = true;
            Item.Entropy().HasCustomStrokeColor = true;
            Item.Entropy().HasCustomNameColor = true;
            Item.Entropy().Legend = true;
            RecheckStats = true;
            UpdateInventory(Main.LocalPlayer);
        }
        public int swing = 0;
        public static int throwType = -1;
        public override bool AllowPrefix(int pre) => true;

        public static bool AllowDash() => NPC.downedBoss1;
        public static bool DashImmune() => CECal.DownedSlimeGod;
        public static bool AllowThrow() => NPC.downedBoss2 || CECal.DownedPerforator || CECal.DownedHiveMind;
        public static bool AllowSpin() => EDownedBosses.downedProphet;
        public static bool DashUpgrade() => CECal.DownedSignus;
        public static bool AllowRevive() => CECal.DownedYharon(EDownedBosses.downedCruiser);
        public static bool AllowVoidEmpowerment() => EDownedBosses.downedNihilityTwin;
        public override bool AltFunctionUse(Player player) => AllowThrow();

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.HasBuff<VoidEmpowerment>())
            {
                damage = (int)(damage * 1.2f);
            }

            if (player.altFunctionUse == 2)
            {
                velocity *= 0.46f;
                type = throwType;
                damage = (int)(damage / 2.0f);
            }
            if (AllowDash() && player.controlUp && !player.HasCooldown(TlipocasScytheSlashCooldown.ID))
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<TlipocasScytheHeld>(), DashUpgrade() ? damage : damage / 4, knockback, player.whoAmI, swing == 0 ? 1 : -1, -1);
                player.AddCooldown(TlipocasScytheSlashCooldown.ID, 15 * 60);
                CalamityEntropy.FlashEffectStrength = 0.2f;
                int p = Projectile.NewProjectile(source, position, velocity.normalize() * 1000 * (DashUpgrade() ? 1.33f : 1), ModContent.ProjectileType<TSSlash>(), damage * 2, knockback, player.whoAmI);
                //瞬身斩沿用原逻辑：蓄势就绪时强化但不消耗
                if (CEChargeWeapon.IsReady(Item) && p >= 0 && p < Main.maxProjectiles)
                {
                    CEChargeWeapon.Empower(p);
                }
                if (CECal.DownedPolterghast)
                {
                    Projectile.NewProjectile(source, position + velocity.normalize() * 400 * (DashUpgrade() ? 1.33f : 1), velocity.normalize() * 1000 * (DashUpgrade() ? 1.33f : 1), ModContent.ProjectileType<TSSlash>(), damage * 2, knockback, player.whoAmI, 0, 1);
                }
            }
            else
            {
                if (player.ownedProjectileCounts[throwType] > 0)
                    return false;
                int p = Projectile.NewProjectile(source, position, velocity, type, damage * 1, knockback, player.whoAmI, swing == 0 ? 1 : -1);
                if (CEChargeWeapon.TryConsume(player, Item) && p >= 0 && p < Main.maxProjectiles)
                {
                    CEChargeWeapon.Empower(p);
                    int ut = 40 - GetLevel();
                    SpeedUpTime += ut + (int)(ut * 0.35f);
                    if (SpeedUpTime > 120)
                        SpeedUpTime = 120;
                }

                swing = 1 - swing;
            }
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Voidstone, CEID.Item_BloodOrb, CEID.Item_LoreAwakening))
            {
                CreateRecipe()
                    .AddIngredient(CEID.Item_Voidstone, 10)
                    .AddIngredient(CEID.Item_BloodOrb, 10)
                    .AddIngredient(ItemID.Deathweed)
                    .AddIngredient(CEID.Item_LoreAwakening)
                    .AddTile(TileID.Anvils)
                    .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Obsidian, 10)
                .AddIngredient(ItemID.Vertebrae, 10)
                .AddIngredient(ItemID.Deathweed)
                .AddIngredient<FadingRunestone>()
                .AddTile(ModContent.TileType<VoidWellTile>()).Register();
        }
    }

    public class TSSlash : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee);
            Projectile.timeLeft = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.penetrate = -1;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (!TlipocasScythe.DashImmune())
                return false;
            return null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("slice", 1, target.Center);
            //PRT_MultiSlash xadd spawn后赋,旧MultiSlash原值
            var p = PRTLoader.NewParticle<PRT_MultiSlash>(target.Center, Vector2.Zero, Color.IndianRed, 1);
            p.xadd = 1f;
            p.lx = 1f;
            p.endColor = Color.Red;
            p.spawnColor = Color.IndianRed;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
            if (TlipocasScythe.DashUpgrade())
            {
                target.AddBuff<MarkedforDeath>(8 * 60);
                target.AddBuff<WhisperingDeath>(8 * 60);
            }
        }
        public bool MovePlayer = true;
        public override void AI()
        {
            if (TlipocasScythe.DashImmune())
            {
                Projectile.GetOwner().Entropy().immune = 4;
            }
            if (Projectile.localAI[1]++ == 0)
            {
                CEUtils.PlaySound("AbyssalBladeLaunch", 1, Projectile.Center);
            }
            Player player = Projectile.GetOwner();
            CalamityEntropy.FlashEffectStrength = 0.3f;
            if (Projectile.ai[1] == 0 && MovePlayer)
            {
                Vector2 odp = player.Center;
                player.Center = Vector2.Lerp(Projectile.Center + Projectile.velocity, Projectile.Center, Projectile.timeLeft / 10f);
                if (CEUtils.IsPlayerStuck(player))
                {
                    MovePlayer = false;
                    player.Center = odp;
                }
            }
            if (Projectile.timeLeft == 10)
            {
                if (Projectile.ai[1] == 0)
                {
                    if (CECal.DownedDoG(EDownedBosses.downedCruiser))
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity / 16f, ModContent.ProjectileType<BloodCrack>(), Projectile.damage / 6, 0, Projectile.owner);
                    }
                    player.Entropy().screenShift = 1;
                    player.Entropy().screenPos = player.Center;
                    Vector2 top = Projectile.Center;
                    Vector2 sparkVelocity2 = Projectile.velocity * 0.08f;
                    Vector2 rd = Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2);
                    int sparkLifetime2 = 24;
                    float sparkScale2 = 1.5f;
                    for (float i = 0; i < 1; i += 0.01f)
                    {
                        Color sparkColor2 = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0, 1));
                        PRTLoader.NewParticle<PRT_AltSpark>(top + CEUtils.randomPointInCircle(32), sparkVelocity2 * Main.rand.NextFloat(), sparkColor2, sparkScale2 * Main.rand.NextFloat(0.6f, 1)).Configure(false, (int)(sparkLifetime2));
                    }
                }
                else
                {
                    if (CECal.DownedDoG(EDownedBosses.downedCruiser))
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy(MathHelper.PiOver2) / 16f / 2, ModContent.ProjectileType<BloodCrack>(), Projectile.damage / 6, 0, Projectile.owner);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy(-MathHelper.PiOver2) / 16f / 2, ModContent.ProjectileType<BloodCrack>(), Projectile.damage / 6, 0, Projectile.owner);
                    }
                    Vector2 top = Projectile.Center;
                    Vector2 sparkVelocity2 = Projectile.velocity * 0.04f;
                    Vector2 rd = Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2);
                    int sparkLifetime2 = 24;
                    float sparkScale2 = 1.5f;
                    for (float i = 0; i < 1; i += 0.01f)
                    {
                        Color sparkColor2 = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0, 1));
                        PRTLoader.NewParticle<PRT_AltSpark>(top + CEUtils.randomPointInCircle(32), sparkVelocity2.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1 : -1)) * Main.rand.NextFloat(), sparkColor2, sparkScale2 * Main.rand.NextFloat(0.6f, 1)).Configure(false, (int)(sparkLifetime2));
                    }
                }
            }
        }

        public override bool ShouldUpdatePosition() { return false; }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != 0)
            {
                return CEUtils.LineThroughRect(Projectile.Center - Projectile.velocity.RotatedBy(MathHelper.PiOver2 * 0.5f), Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2 * 0.5f), targetHitbox, 32);
            }
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity, targetHitbox, 32);
        }
    }

    public class TlipocasScytheHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/TlipocasScythe";
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 1.5f;
            if (CECal.DownedCalamitas(EDownedBosses.downedCruiser))
            {
                float dmgMult = Utils.Remap(CEUtils.getDistance(target.Center, Projectile.Center), 160, 300, 1.25f, 1);
                modifiers.FinalDamage *= dmgMult;
            }
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.light = 0.2f;
            Projectile.MaxUpdates = 30;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public List<float> oldRots = new List<float>();
        public List<float> oldScale = new List<float>();
        public float counter = 0;
        public bool flagS = true;
        public bool flag = true;
        public bool Canhit = false;
        public bool shake = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (shake)
                CEUtils.SetShake(target.Center, Projectile.IsEmpowered() ? 8 : 5);
            shake = false;
            if (Projectile.ai[1] == 1 && TlipocasScythe.AllowVoidEmpowerment())
            {
                int VETime = EDownedBosses.downedCruiser ? 15 : 8;
                VETime *= 60;
                Projectile.GetOwner().AddBuff(ModContent.BuffType<VoidEmpowerment>(), VETime);
            }
            if (target.townNPC && target.life <= 0)
            {
                //原灾厄穿孔者巢死亡音（血腥爆裂），用原版近似
                SoundEngine.PlaySound(SoundID.NPCDeath1, target.Center);
                var player = Projectile.GetOwner();
                player.QuickSpawnItem(target.GetSource_Death(), new Item(73, 20), 20);
                player.QuickSpawnItem(target.GetSource_Death(), new Item(ItemID.Vertebrae, 30), 30);
                if (NPC.downedPlantBoss)
                {
                    player.QuickSpawnItem(target.GetSource_Death(), new Item(1508, 20), 20);
                    player.QuickSpawnItem(target.GetSource_Death(), new Item(74, 514), 20);
                }
                if (NPC.downedMoonlord)
                {
                    player.QuickSpawnItem(target.GetSource_Death(), new Item(ModContent.ItemType<NihilityFragments>(), 20), 20);
                }
            }
            if (flagS)
            {
                if (TlipocasScythe.AllowSpin() && Projectile.IsEmpowered())
                {
                    Projectile.GetOwner().Heal(CECal.DownedPolterghast ? 10 : 7);
                }
                CEUtils.PlaySound("voidseekershort", 1, target.Center, 6, CEUtils.WeapSound);
                flagS = false;
                if (!target.Organic())
                {
                    CEUtils.PlaySound("metalhit", Main.rand.NextFloat(1.4f, 1.6f), target.Center, 6);
                }
            }
            if (EDownedBosses.downedCruiser)
            {
                EGlobalNPC.AddVoidTouch(target, 60, 3, 1000, 12);
            }
            // 2026-08-31 平衡案:不再造成孱弱巫咒/血管爆裂/燃烧/重度出血
            if (CECal.DownedProvidence(EDownedBosses.downedNihilityTwin))
            {
                if (CECal.DownedPrimordialWyrm)
                {
                    target.AddBuff<LifeOppress>(60 * 3);
                }
                else
                {
                    target.AddBuff<ArmorCrunch>(60 * 3);
                }
            }
            Color impactColor = Color.Red;
            float impactParticleScale = Main.rand.NextFloat(1.5f, 1.7f);

            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, TlipocasScythe.TrailColor(Projectile) * 8, impactParticleScale).Configure(Color.White, 12, 0, 2.5f);

            float sparkCount = 8 + TlipocasScythe.GetLevel();
            for (int i = 0; i < sparkCount; i++)
            {
                float p = Main.rand.NextFloat();
                Vector2 sparkVelocity2 = (target.Center - Projectile.Center).normalize().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(6, 20 * (2 - p)) * (1 + TlipocasScythe.GetLevel() * 0.1f);
                int sparkLifetime2 = (int)((2 - p) * 16);
                float sparkScale2 = 0.6f + (1 - p);
                sparkScale2 *= (1 + TlipocasScythe.GetLevel() * 0.06f);
                Color sparkColor2 = Color.Lerp(Color.DarkRed, Color.IndianRed, p);
                if (Projectile.GetOwner().HasBuff<VoidEmpowerment>())
                {
                    sparkColor2 = Color.Lerp(Color.DeepSkyBlue, Color.Purple, p);
                    if (Main.rand.NextBool())
                    {
                        PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                    }
                    else
                    {
                        PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (Projectile.frame == 7 ? 1f : 0.65f), Color.Purple, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2 * (Projectile.frame == 7 ? 1.2f : 1f)));
                    }
                }
                else
                {
                    if (TlipocasScythe.AltType(Projectile.GetOwner()))
                        sparkColor2 = TlipocasScythe.TrailColor(Projectile);
                    if (Main.rand.NextBool())
                    {
                        PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                    }
                    else
                    {
                        PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2, Main.rand.NextBool() ? Color.Red : Color.DarkRed, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2));
                    }
                }
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(255, 120, 120), 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);

        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool? CanDamage() => Canhit;
        public override bool? CanHitNPC(NPC target)
        {
            var togglePlayer = Projectile.GetOwner().GetModPlayer<TlipocasSingleHit>();
            bool canDealDamageToAll = (target.type != NPCID.DD2EterniaCrystal && !togglePlayer.isPressed);
            bool canDealDamageToNotFridly = !target.friendly && togglePlayer.isPressed;
            return canDealDamageToAll || canDealDamageToNotFridly;
        }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            player.itemTime = player.itemAnimation = 2;
            float progress = (counter / (player.itemAnimationMax * Projectile.MaxUpdates));
            counter++;
            if (Projectile.ai[2] == 0)
                player.heldProj = Projectile.whoAmI;

            int dir = (int)Projectile.ai[0] * Math.Sign(Projectile.velocity.X);

            float ySc = 0.6f;
            ProjScale = 1.56f + TlipocasScythe.GetLevel() * 0.04f;
            ProjScale *= (1 + Projectile.ai[1] * 0.5f);
            if (Projectile.ai[1] == 1)
            {
                if (progress < 0.1f)
                {
                    counter = (int)(0.1f * player.itemAnimationMax * Projectile.MaxUpdates + 1);
                }
            }
            if (Projectile.ai[1] == -1)
            {
                if (progress < 0.3f)
                {
                    counter = (int)(0.3f * player.itemAnimationMax * Projectile.MaxUpdates + 1);
                }
            }
            if (Projectile.IsEmpowered())
            {
                ySc = 0.5f;
                ProjScale *= 2f;
            }
            if (Projectile.ai[1] == -1)
            {
                ySc = 0.12f;
                ProjScale = 4.4f;
            }
            float r = 3.6f;
            float r1 = 0.5f;
            float r2 = 0.8f;
            float pn = 0.36f;
            if (progress >= pn && flag)
            {
                Canhit = true;
                flag = false;
                CEUtils.PlaySound("scytheswing", Main.rand.NextFloat(1.6f, 1.8f), Projectile.Center, 4, CEUtils.WeapSound);
            }
            if (progress < pn)
            {
                Projectile.rotation = (-r / 2f - CEUtils.GetRepeatedCosFromZeroToOne(progress / pn, 2) * r1) * dir;
            }
            else
            {
                Projectile.rotation = (-r / 2f - r1 + (CEUtils.GetRepeatedParaFromZeroToOne((progress - pn) / (1 - pn), 3) * 0.6f + 0.4f * CEUtils.GetRepeatedParaFromZeroToOne((progress - pn) / (1 - pn), 2)) * (r + r2)) * dir;
            }
            scale = (Projectile.rotation.ToRotationVector2() * new Vector2(1, ySc)).Length();
            if (progress > 0.7f)
            {
                ProjScale *= (1 - (progress - 0.7f) / 0.3f) * 0.2f + 0.8f;
            }
            Projectile.rotation = (Projectile.rotation.ToRotationVector2() * new Vector2(1, ySc)).ToRotation() + Projectile.velocity.ToRotation();
            if (progress > 1)
            {
                Projectile.Kill();
            }
            if (Projectile.ai[2] == 0)
                player.SetHandRotWithDir(Projectile.rotation, Math.Sign(Projectile.velocity.X));
            if (progress > pn)
            {
                oldScale.Add(scale);
                oldRots.Add(Projectile.rotation);
                if (oldRots.Count > 170)
                {
                    oldRots.RemoveAt(0);
                    oldScale.RemoveAt(0);
                }
            }
            if (Projectile.ai[2] == 0)
                Projectile.Center = player.MountedCenter + new Vector2(player.direction * -6, 0);

        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 146 * scale * ProjScale, targetHitbox, (int)(36 * ProjScale));
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 150 * Projectile.scale * scale * ProjScale, 54, DelegateMethods.CutTiles);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TlipocasScythe.GetTexture(Projectile.GetOwner()).Value;
            Texture2D trail = CEExtraAssets.StreakGoop;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            Effect _shader = CEEffectAssets.SwordTrail3;

            {
                for (int i = 0; i < oldRots.Count; i++)
                {
                    Color b = new Color(255, 255, 255);
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(168 * Projectile.scale * oldScale[i] * ProjScale, 0).RotatedBy(oldRots[i])),
                          new Vector3((i) / ((float)oldRots.Count - 1), 1, 1),
                          b));
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(90 * Projectile.scale * oldScale[i] * ProjScale, 0).RotatedBy(oldRots[i])),
                          new Vector3((i) / ((float)oldRots.Count - 1), 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    var gd = Main.graphics.GraphicsDevice;
                    SpriteBatch sb = Main.spriteBatch;

                    sb.End();
                    _shader.Parameters["color1"].SetValue(TlipocasScythe.TrailColor(Projectile).ToVector4());
                    _shader.Parameters["color2"].SetValue((Projectile.GetOwner().HasBuff<VoidEmpowerment>() ? new Color(190, 190, 255) : (TlipocasScythe.AltType(Projectile.GetOwner()) ? TlipocasScythe.TrailColor(Projectile) * 1.2f : new Color(255, 60, 60))).ToVector4());

                    _shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 2.4f);
                    _shader.Parameters["alpha"].SetValue(1);
                    sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, _shader, Main.GameViewMatrix.TransformationMatrix);
                    _shader.CurrentTechnique.Passes["EffectPass"].Apply();
                    gd.Textures[0] = CEExtraAssets.Streak2;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }
            ve.Clear();
            {
                for (int i = 0; i < oldRots.Count; i++)
                {
                    Color b = new Color(255, 255, 255);
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(158 * Projectile.scale * oldScale[i] * ProjScale, 0).RotatedBy(oldRots[i])),
                          new Vector3((i) / ((float)oldRots.Count - 1), 1, 1),
                          b));
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(100 * Projectile.scale * oldScale[i] * ProjScale, 0).RotatedBy(oldRots[i])),
                          new Vector3((i) / ((float)oldRots.Count - 1), 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    var gd = Main.graphics.GraphicsDevice;
                    SpriteBatch sb = Main.spriteBatch;
                    sb.End();
                    _shader.Parameters["color1"].SetValue(new Vector4(1, 1, 1, 0));
                    _shader.Parameters["color2"].SetValue((Color.White).ToVector4() * 0.82f);

                    _shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 3f);
                    _shader.Parameters["alpha"].SetValue(1);
                    sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, _shader, Main.GameViewMatrix.TransformationMatrix);
                    _shader.CurrentTechnique.Passes["EffectPass"].Apply();
                    gd.Textures[0] = CEExtraAssets.Streak1;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            int dir = (int)(Projectile.ai[0]) * Math.Sign(Projectile.velocity.X);
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            if (Projectile.GetOwner().HasBuff<VoidEmpowerment>())
            {
                Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, CEEffectAssets.RedTrans);
                CEEffectAssets.RedTrans.CurrentTechnique.Passes[0].Apply();
            }

            Main.spriteBatch.Draw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * ProjScale * scale, effect, 0);


            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public float alpha = 1;
        public float ProjScale = 1;
        public float scale = 1;
    }
    internal class TlipocasSingleHit : ModPlayer
    {
        public bool isToggle = false;
        public bool isPressed = false;
        public override void LoadData(TagCompound tag)
        {
            isPressed = tag.GetBool(nameof(isPressed));
        }
        public override void SaveData(TagCompound tag)
        {
            tag.Add(nameof(isPressed), isPressed);
        }
        //花
        public void SpawnFlower()
        {
            //花蕊的半径。
            float heartRads = 8f;
            //花蕊的粒子数量。
            int heartDustCounts = 20;
            for (int i = 0; i < heartDustCounts; i++)
            {
                //花蕊粒子与中心的向量差
                Vector2 heartPos = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(heartRads);
                //需要表现出从中心往外扩散的效果
                Vector2 dir = heartPos.SafeNormalize(Vector2.UnitX) * 1.6f;
                Dust d = Dust.NewDustPerfect(Player.Center + heartPos, DustID.TheDestroyer);
                d.scale *= 1.1f;
                d.velocity = dir;
                d.noGravity = true;
            }

            //尝试生成花瓣
            int petalCount = 5;
            float outerRadius = 20f;
            float innerRadius = 3.6f;
            List<List<Vector2>> petalVertexList = [];
            float randAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int k = 0; k < petalCount; k++)
            {
                //每次过来的时候都会新建一次顶点数据
                //然后放入到上面的顶点列表里的顶点列表内
                List<Vector2> petalVertexs = [];
                //五个花瓣，总共72°
                float petalStartAngle = randAngle + MathHelper.ToRadians(k * 72);
                float petalEndAngle = petalStartAngle + MathHelper.ToRadians(72);
                //生成花瓣内侧弧线顶点
                for (int i = 0; i <= 4; i++)
                {
                    float t = (float)i / 4;
                    float angle = MathHelper.Lerp(petalStartAngle, petalEndAngle, t);
                    Vector2 vertex = angle.ToRotationVector2() * innerRadius;
                    petalVertexs.Add(vertex);
                }

                //生成花瓣外侧弧线顶点，需逆向
                for (int i = 4; i >= 0; i--)
                {
                    float t = (float)i / 4;
                    float angle = MathHelper.Lerp(petalStartAngle, petalEndAngle, t);
                    //调整了一点弧度。
                    float dynamicOuterRadius = outerRadius * (1 + 0.1f * (float)Math.Sin(angle * 5));
                    Vector2 vertex = angle.ToRotationVector2() * dynamicOuterRadius;
                    petalVertexs.Add(vertex);
                }

                petalVertexList.Add(petalVertexs);
            }

            //连线。
            foreach (var petalVertex in petalVertexList)
            {
                for (int i = 0; i < petalVertex.Count - 1; i++)
                {
                    Vector2 startVertex = petalVertex[i];
                    Vector2 endVertex = petalVertex[i + 1];
                    int connectCounts = 8;
                    for (int j = 0; j < connectCounts; j++)
                    {
                        float t = (float)j / (connectCounts - 1);
                        Vector2 currentPos = Vector2.Lerp(startVertex, endVertex, t);
                        float lerpValue = MathHelper.Lerp(MathF.Abs(j % 10 - 5) / 5f, 1, 0.6f);
                        Vector2 dir2 = currentPos.SafeNormalize(Vector2.UnitX) * lerpValue;
                        Dust d = Dust.NewDustPerfect(Player.Center + currentPos, DustID.TheDestroyer);
                        d.scale *= 1.1f;
                        d.velocity = dir2;
                        d.noGravity = true;
                    }
                }
            }
        }
        public override void PostUpdate()
        {
            if (Main.mouseMiddle && Main.HoverItem.type == ModContent.ItemType<TlipocasScythe>() && Main.playerInventory)
            {
                if (Main.mouseMiddleRelease)
                {
                    isPressed = !isPressed;
                    SpawnFlower();
                    SoundEngine.PlaySound(SoundID.Item103 with { MaxInstances = 4, Pitch = 0.4f });
                }
            }
        }
    }

    public class TlipocasScytheThrow : ModProjectile
    {
        public float TeleportSlashDamageMult = 4f;
        public override string Texture => "CalamityEntropy/Content/Items/Donator/TlipocasScythe";
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (StickOnMouse)
                modifiers.SourceDamage /= 2.0f;

            if (CECal.DownedCalamitas(EDownedBosses.downedCruiser))
            {
                float dmgMult = Utils.Remap(CEUtils.getDistance(target.Center, Projectile.Center), 160, 300, 1.25f, 1);
                modifiers.FinalDamage *= dmgMult;
            }
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.light = 0.2f;
            Projectile.MaxUpdates = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 64;
        }

        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRots = new List<float>();
        public List<float> oldScale = new List<float>();
        public float counter = 0;
        public bool flagS = true;
        public bool flag = true;
        public bool StickOnMouse = false;
        public bool RightLast = true;
        public bool shake = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (shake || StickOnMouse)
                CEUtils.SetShake(target.Center, (Projectile.IsEmpowered() ? 8 : 4) * (StickOnMouse ? 0.5f : 1));
            shake = false;
            if (Projectile.numHits == 1)
            {
                if (TlipocasScythe.AllowSpin() && Projectile.IsEmpowered())
                {
                    Projectile.GetOwner().Heal(CECal.DownedPolterghast ? 10 : 7);
                }
            }
            if (counter < 16 * 10)
            {
                counter = 16 * 10;
            }
            CEUtils.PlaySound("voidseekershort", 1, target.Center, 6, CEUtils.WeapSound * 0.4f);
            if (!target.Organic())
            {
                CEUtils.PlaySound("metalhit", Main.rand.NextFloat(0.8f, 1.2f) / Projectile.ai[1], target.Center, 6, CEUtils.WeapSound * 0.4f);
            }
            if (EDownedBosses.downedCruiser)
            {
                EGlobalNPC.AddVoidTouch(target, 60, 3, 1000, 12);
            }
            // 2026-08-31 平衡案:不再造成孱弱巫咒/血管爆裂/燃烧/重度出血
            if (CECal.DownedProvidence(EDownedBosses.downedNihilityTwin))
            {
                if (CECal.DownedPrimordialWyrm)
                {
                    target.AddBuff<LifeOppress>(60 * 3);
                }
                else
                {
                    target.AddBuff<ArmorCrunch>(60 * 3);
                }
            }
            Color impactColor = Color.Red;
            float impactParticleScale = Main.rand.NextFloat(1.5f, 1.7f);

            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, TlipocasScythe.TrailColor(Projectile) * 12, impactParticleScale).Configure(Color.White, 8, 0, 2.5f);


            float sparkCount = 6 + TlipocasScythe.GetLevel();
            for (int i = 0; i < sparkCount; i++)
            {
                float p = Main.rand.NextFloat();
                Vector2 sparkVelocity2 = CEUtils.randomRot().ToRotationVector2().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(6, 20 * (2 - p)) * (1 + TlipocasScythe.GetLevel() * 0.1f);
                int sparkLifetime2 = (int)((2 - p) * 9);
                float sparkScale2 = 0.4f + (1 - p);
                sparkScale2 *= (1 + TlipocasScythe.GetLevel() * 0.03f);
                Color sparkColor2 = Color.Lerp(Color.DarkRed, Color.IndianRed, p);
                if (Projectile.GetOwner().HasBuff<VoidEmpowerment>())
                {
                    sparkColor2 = Color.Lerp(Color.DeepSkyBlue, Color.Purple, p);
                    if (Main.rand.NextBool())
                    {
                        PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                    }
                    else
                    {
                        PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (Projectile.frame == 7 ? 1f : 0.65f), Color.Purple, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2 * (Projectile.frame == 7 ? 1.2f : 1f)));
                    }
                }
                else
                {
                    if (TlipocasScythe.AltType(Projectile.GetOwner()))
                        sparkColor2 = TlipocasScythe.TrailColor(Projectile);
                    if (Main.rand.NextBool())
                    {
                        PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                    }
                    else
                    {
                        PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2, Main.rand.NextBool() ? Color.Red : Color.DarkRed, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2));
                    }
                }
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(255, 120, 120), 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);

        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(StickOnMouse);
            writer.Write(counter);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            StickOnMouse = reader.ReadBoolean();
            counter = reader.ReadSingle();
        }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            ProjScale = 1.4f + TlipocasScythe.GetLevel() * 0.02f;
            counter++;
            player.itemTime = player.itemAnimation = 2;
            if (counter > 16 * (StickOnMouse ? 66 : 16))
            {
                Projectile.velocity *= 0.996f;
                Projectile.velocity += (player.Center - Projectile.Center).normalize() * 0.05f;
                if (CEUtils.getDistance(Projectile.Center, player.Center) < Projectile.velocity.Length() + 128)
                {
                    Projectile.Kill();
                }
            }
            else
            {
                if (StickOnMouse)
                {
                    Projectile.velocity *= 0.98f;
                    Projectile.velocity += (player.Entropy().MouseWorld - Projectile.Center).normalize() * 0.1f;
                }
            }
            player.Entropy().MouseWorldListener = true;

            Projectile.rotation += 0.04f;
            oldScale.Add(1);
            oldPos.Add(Projectile.Center);
            oldRots.Add(Projectile.rotation);
            if (oldRots.Count > 16 * 6)
            {
                oldRots.RemoveAt(0);
                oldScale.RemoveAt(0);
                oldPos.RemoveAt(0);
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (Main.mouseLeft && !player.HasCooldown(TeleportSlashCooldown.ID) && CECal.DownedBrimstoneElemental)
                {
                    player.AddCooldown(TeleportSlashCooldown.ID, (EDownedBosses.downedCruiser ? 20 : 30) * 60);
                    player.Entropy().screenShift = 1f;
                    player.Entropy().screenPos = player.Center;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, (Projectile.Center - player.Center).SafeNormalize((Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX)) * 16, ModContent.ProjectileType<TlipocasScytheHeld>(), (int)(Projectile.damage * TeleportSlashDamageMult * 1.25f), Projectile.knockBack, player.whoAmI, 1, 1);
                    if (CECal.DownedPolterghast)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, (Projectile.Center - player.Center).SafeNormalize((Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX)) * 16, ModContent.ProjectileType<TlipocasScytheHeld>(), (int)(Projectile.damage * TeleportSlashDamageMult * 0.5f), Projectile.knockBack, player.whoAmI, 1, 1, 1);
                        var p = PRTLoader.NewParticle<PRT_PlayerShadowBlack>(player.Center, Vector2.Zero, Color.White, 1);
                        p.plr = player;
                        p.Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, 60);
                    }
                    Projectile.Kill();
                    Vector2 v1 = player.position;
                    Vector2 v2 = Projectile.Center - new Vector2(player.width, player.height) / 2;
                    for (float i = 0; i <= 1; i += 0.05f)
                    {
                        var p = PRTLoader.NewParticle<PRT_PlayerShadowBlack>(Vector2.Lerp(v1, v2, i), Vector2.Zero, Color.White, 1);
                        p.plr = player;
                        p.Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, 12);
                    }
                    player.Center = Projectile.Center;
                    player.Entropy().immune = 32;
                }
                if (!StickOnMouse && Main.mouseRight && TlipocasScythe.AllowSpin() && !RightLast)
                {
                    counter = -80;
                    StickOnMouse = true;
                    CEUtils.SyncProj(Projectile.whoAmI);
                }
                RightLast = Main.mouseRight;
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Projectile.Center.getRectCentered((int)(142 * ProjScale), (int)(142 * ProjScale)).Intersects(targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TlipocasScythe.GetTexture(Projectile.GetOwner()).Value;
            Texture2D trail = CEExtraAssets.StreakGoop;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);

            Effect shader = CEEffectAssets.SwordTrail3;

            {
                for (int i = 0; i < oldRots.Count; i++)
                {
                    Color b = new Color(255, 255, 255);
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + (new Vector2(74 * Projectile.scale * oldScale[i] * ProjScale, 0).RotatedBy(oldRots[i])),
                          new Vector3((i) / ((float)oldRots.Count - 1), 1, 1),
                          b));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + (new Vector2(30 * Projectile.scale * oldScale[i] * ProjScale, 0).RotatedBy(oldRots[i])),
                          new Vector3((i) / ((float)oldRots.Count - 1), 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    var gd = Main.graphics.GraphicsDevice;
                    SpriteBatch sb = Main.spriteBatch;

                    sb.End();
                    shader.Parameters["color1"].SetValue(TlipocasScythe.TrailColor(Projectile).ToVector4());
                    shader.Parameters["color2"].SetValue((Projectile.GetOwner().HasBuff<VoidEmpowerment>() ? new Color(190, 190, 255) : (TlipocasScythe.AltType(Projectile.GetOwner()) ? TlipocasScythe.TrailColor(Projectile) * 1.2f : new Color(255, 60, 60))).ToVector4());
                    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 2.4f);
                    shader.Parameters["alpha"].SetValue(1);
                    sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes["EffectPass"].Apply();
                    gd.Textures[0] = CEExtraAssets.Streak2;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }

            ve.Clear();
            {
                for (int i = 0; i < oldRots.Count; i++)
                {
                    Color b = new Color(255, 255, 255);
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + (new Vector2(69 * Projectile.scale * oldScale[i] * ProjScale, 0).RotatedBy(oldRots[i])),
                          new Vector3((i) / ((float)oldRots.Count - 1), 1, 1),
                          b));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + (new Vector2(35 * Projectile.scale * oldScale[i] * ProjScale, 0).RotatedBy(oldRots[i])),
                          new Vector3((i) / ((float)oldRots.Count - 1), 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    var gd = Main.graphics.GraphicsDevice;
                    SpriteBatch sb = Main.spriteBatch;

                    sb.End();
                    shader.Parameters["color1"].SetValue(new Vector4(1, 1, 1, 0));
                    shader.Parameters["color2"].SetValue(Color.White.ToVector4() * 0.82f);
                    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 3f);
                    shader.Parameters["alpha"].SetValue(1);
                    sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes["EffectPass"].Apply();
                    gd.Textures[0] = CEExtraAssets.Streak1;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }

            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);

            Vector2 origin = tex.Size() / 2f;
            float rot = Projectile.rotation;

            if (Projectile.GetOwner().HasBuff<VoidEmpowerment>())
            {
                Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, CEEffectAssets.RedTrans);
                CEEffectAssets.RedTrans.CurrentTechnique.Passes[0].Apply();
            }

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * ProjScale * scale, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public float alpha = 1;
        public float ProjScale = 1;
        public float scale = 1;
    }
    public class BloodCrack : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 128;
            Projectile.timeLeft = 48;
        }
        public List<Vector2> points = new List<Vector2>();
        int d = 0;
        public override void AI()
        {
            d++;
            if (d > 16)
            {
                Projectile.velocity *= 0;
            }
            else
            {
                Vector2 o = (points.Count > 0 ? points[points.Count - 1] : Projectile.Center - Projectile.velocity);
                Vector2 nv = Projectile.Center + CEUtils.randomVec(4);
                for (float i = 0.1f; i <= 1; i += 0.1f)
                {
                    points.Add(Vector2.Lerp(o, nv, i));
                }
            }
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (points.Count < 1)
            {
                return false;
            }
            for (int i = 1; i < points.Count; i++)
            {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, 30))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {

            return false;
        }

        public void draw()
        {
            if (points.Count < 1)
            {
                return;
            }
            Texture2D px = CEExtraAssets.white;
            float jd = 1;
            float lw = Projectile.timeLeft / 30f;
            Color color = Color.White;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 jv = Vector2.Zero;
                CEUtils.drawLine(Main.spriteBatch, px, points[i - 1], points[i] + jv, color * jd, 1f * lw * (new Vector2(-16, 0).RotatedBy(MathHelper.ToRadians(180 * ((float)i / points.Count)))).Y, 3);
            }
        }
    }
}
