using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common.LoreReworks;
using CalamityEntropy.Content.ArmorPrefixes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.Cards;
using CalamityEntropy.Content.Items.Accessories.EvilCards;
using CalamityEntropy.Content.Items.Accessories.Hungry;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Items.Armor.VoidFaquir;
using CalamityEntropy.Content.Items.Atbm;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Pets.Glue;
using CalamityEntropy.Content.Items.PrefixItem;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Items.Weapons.CrystalBalls;
using CalamityEntropy.Content.Items.Weapons.DustCarverBow;
using CalamityEntropy.Content.Items.Weapons.Torch;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.TwistedTwin;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.UI.EntropyBookUI;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Common
{
    public class S3Particle
    {
        //粒子贴图在加载期就位,不再每帧 Request
        [VaultLoaden("CalamityEntropy/Assets/Extra/style3")]
        internal static Asset<Texture2D> Style3Tex;
        public Vector2 velocity = Vector2.Zero;
        public Vector2 position;
        public void update()
        {
            this.position += this.velocity;
        }

        public void draw(float alpha, Vector2 offset, Color color)
        {
            SpriteBatch sb = Main.spriteBatch;
            Color b = color * alpha;
            Texture2D tx = Style3Tex.Value;
            sb.Draw(tx, this.position + offset, null, b, this.velocity.ToRotation(), new Vector2(tx.Width, tx.Height) / 2, 0.3f, SpriteEffects.None, 0);
            sb.Draw(tx, this.position + offset, null, b, this.velocity.ToRotation(), new Vector2(tx.Width, tx.Height) / 2, 0.3f, SpriteEffects.None, 0);
        }
    }


    public class EGlobalItem : GlobalItem
    {
        //工具提示辉光贴图在加载期就位,不再每帧走 getExtraTex 查表
        [VaultLoaden("CalamityEntropy/Assets/Extra/Soulight")]
        internal static Asset<Texture2D> SoulightTex;
        public bool Legend = false;
        public int tooltipStyle = 0;
        public bool stroke = false;
        public Color strokeColor = Color.White;
        public Color NameColor = Color.White;
        public Color NameLightColor = Color.White;
        public bool HasCustomNameColor = false;
        public bool HasCustomStrokeColor = false;
        public List<S3Particle> particles1 = new List<S3Particle>();
        public float[] wispColor = null;
        //脱离灾厄:骷髅王Lore的弹药省耗效果随灾厄Lore下线删除(原CanBeConsumedAsAmmo覆写)
        public readonly static Dictionary<int, int> GemItemIDToTileIDMap = new() {
            {ItemID.Ruby, TileID.Ruby },
            {ItemID.Sapphire, TileID.Sapphire },
            {ItemID.Diamond, TileID.Diamond },
            {ItemID.Emerald, TileID.Emerald },
            {ItemID.Topaz, TileID.Topaz },
            {ItemID.Amethyst, TileID.Topaz },
        }; public readonly static Dictionary<int, int> AzafureMinerExtra = new() {
            {ItemID.LifeCrystal, TileID.Heart },
            {ItemID.LifeFruit, TileID.LifeFruit },
            {3380, 407 }
        };
        public readonly static Dictionary<int, int> GemTileIDToItemIDMap = new() {
            {TileID.Ruby, ItemID.Ruby },
            {TileID.Sapphire, ItemID.Sapphire },
            {TileID.Diamond, ItemID.Diamond },
            {TileID.Emerald, ItemID.Emerald },
            {TileID.Topaz, ItemID.Topaz },
            {TileID.Amethyst, ItemID.Topaz },
        };
        public override void SetDefaults(Item entity)
        {
            if (entity.type == ItemID.ChainKnife)
            {
                entity.damage = 32;
                entity.shootSpeed *= 1.25f;
            }
        }
        public static bool GetOverrideName(Item item, string origName, out string NewName)
        {
            if (item.ModItem != null && item.ModItem is BasePrefixItem pitem)
            {
                NewName = origName.Replace("|", ArmorPrefix.findByName(pitem.PrefixName).GivenName);
                return true;
            }
            if (CEUtils.IsArmor(item) && item.Entropy().armorPrefix != null)
            {
                NewName = item.Entropy().armorPrefix.getName() + " " + origName;
                return true;
            }
            NewName = origName;
            return false;
        }
        // 盗贼饰品标记体系（RogueAccs / EquipedAnyRogueAcc）已随潜行系统整体退役，
        // 自有 7 件盗贼饰品（护符与怀表同链两级）的新效果已按 rogue-weapons.md 实装
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (item.wingSlot != -1)
            {
                player.Entropy().wing = item;
            }
        }


        public override bool CanRightClick(Item item)
        {
            return (CEUtils.IsArmor(item) && Main.mouseItem.IsArmorReforgeItem(out var _) && ServerConfig.Instance.EnableArmorPrefix) || (BookMarkLoader.IsABookMark(item) && EBookUI.active && BookMarkLoader.HasEmptyBookMarkSlot(EBookUI.bookItem, Main.LocalPlayer));
        }
        public override void RightClick(Item item, Player player)
        {
            if (BookMarkLoader.IsABookMark(item) && EBookUI.active)
            {
                bool flag = true;
                for (int h = 0; h < Math.Min(EBookUI.getMaxSlots(Main.LocalPlayer, EBookUI.bookItem), Main.LocalPlayer.Entropy().EBookStackItems.Count); h++)
                {
                    if (BookMarkLoader.IsABookMark(Main.LocalPlayer.Entropy().EBookStackItems[h]))
                    {
                        var bm = Main.LocalPlayer.Entropy().EBookStackItems[h];
                        if (!BookMarkLoader.CanBeEquipWith(item, bm))
                        {
                            flag = false;
                            break;
                        }
                    }
                }

                if (flag)
                {
                    for (int i = 0; i < player.Entropy().EBookStackItems.Count; i++)
                    {
                        if (player.Entropy().EBookStackItems[i].IsAir)
                        {
                            player.Entropy().EBookStackItems[i] = item.Clone();
                            item.TurnToAir();
                            if (Main.netMode != NetmodeID.SinglePlayer)
                            {
                                player.Entropy().SyncBookmarks();
                            }
                        }
                    }
                }
            }
            Item held = Main.mouseItem;
            if (CEUtils.IsArmor(item) && ServerConfig.Instance.EnableArmorPrefix)
            {
                if (held.IsArmorReforgeItem(out var p))
                {
                    bool flag = true;
                    if (p == null)
                    {
                        flag = false;
                        for (int i = 0; i < ItemLoader.ItemCount; i++)
                        {
                            var ins = ItemLoader.GetItem(i);
                            if (ins != null && ins is BasePrefixItem pi && pi.PrefixName == armorPrefixName && ins is not AncientPrefixItem && ins is not BlessingHeatDeath)
                            {
                                flag = true;
                                player.QuickSpawnItem(player.GetSource_FromThis(), new Item(ins.Type), 1);
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        item.Entropy().SetArmorPrefix(p);
                        SoundStyle s = new SoundStyle("CalamityEntropy/Assets/Sounds/Reforge");
                        SoundEngine.PlaySound(s);
                    }
                    else
                    {
                        CEUtils.PlaySound("metalhit", 1);
                    }
                }
            }
        }

        public override void GetHealMana(Item item, Player player, bool quickHeal, ref int healValue)
        {
            healValue += (int)(healValue * player.Entropy().ManaExtraHeal);
            if (player.Entropy().hasAcc("VastLV2"))
            {
                healValue = (int)((CalCI ? 0.25f : 0.75f) * healValue);
            }
        }
        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if(item.ModItem != null && item.ModItem is IBaitItem && Main.LocalPlayer.HeldItem.type == item.type)
            {
                scale = 1;
                float charge = float.Clamp(Main.LocalPlayer.Entropy().BaitCharge, 0, 1);
                CEUtils.DrawChargeBar(scale, position + new Vector2(0, 16 * scale), ((float)charge / 1f), Color.Yellow);
            }
        }

        public static bool CalCI = false;
        public int DyeType = 0;
        public override bool ConsumeItem(Item item, Player player)
        {
            if(item.useStyle == ItemUseStyleID.EatFood || item.useStyle == ItemUseStyleID.DrinkLiquid)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Flowery>()] > 0)
                    CEUtils.PlaySound("VoiceClips/ConsumeFood", 1, player.Entropy().floweryPosition);
            }
            if (player.Entropy().hasAcc("VastLV2") && item.healMana > 0)
            {
                CalCI = true;
                int h = item.healMana;
                ItemLoader.GetHealMana(item, player, true, ref h);
                CalCI = false;
                player.Entropy().ManaRegenPer30Tick = h / 10;
                player.Entropy().ManaRegenTime = 60 * 5 + 5;
            }
            if (BookMarkLoader.IsABookMark(item) && EBookUI.active)
            {
                return false;
            }
            Item held = Main.mouseItem;
            if (CEUtils.IsArmor(item))
            {
                if (held.IsArmorReforgeItem(out var _))
                {
                    if (ItemLoader.ConsumeItem(held, player))
                    {
                        held.Shrink();
                    }
                    return false;
                }
            }
            return true;
        }
        public void SetArmorPrefix(ArmorPrefix armorPrefixS)
        {
            if (armorPrefixS == null)
            {
                this.armorPrefix = null;
                this.armorPrefixName = string.Empty;
                return;
            }
            this.armorPrefix = armorPrefixS;
            this.armorPrefixName = armorPrefixS.RegisterName();
        }
        public override void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
        {
            speed *= player.Entropy().WingSpeed;
            acceleration *= player.Entropy().WingSpeed;
            speed *= 1 + player.Entropy().VoidCharge * 0.25f;
            acceleration *= 1 + player.Entropy().VoidCharge * 0.25f;

        }


        public override void UpdateEquip(Item item, Player player)
        {
            if (item.type == ItemID.SantaHat)
            {
                player.Entropy().cHat = true;
            }
            if (armorPrefix != null)
            {
                armorPrefix.UpdateEquip(player, item);
                player.statDefense += (int)(Math.Ceiling(item.defense * armorPrefix.AddDefense()));
            }
        }
        public override void UpdateVanity(Item item, Player player)
        {
            if (item.wingSlot != -1)
            {
                player.Entropy().vanityWing = item;
            }
            if (item.type == ItemID.SantaHat)
            {
                player.Entropy().cHat = true;
            }
        }

        public override bool? UseItem(Item item, Player player)
        {
            if (player.channel || player.whoAmI != Main.myPlayer || item.pick > 0 || item.damage <= 0 || item.ammo != AmmoID.None || item.axe > 0 || !player.Entropy().TarnishCard)
            {
                return null;
            }
            var mp = player.Entropy();
            if (mp.BlackFlameCd <= 0 && player.whoAmI == Main.myPlayer)
            {
                mp.BlackFlameCd = Math.Max(item.useTime, Tarnish.BlackFireCooldownMin);
                Projectile.NewProjectile(player.GetSource_FromAI(), player.Center, (Main.MouseWorld - player.Center).SafeNormalize(Vector2.One) * 14, ModContent.ProjectileType<BlackFire>(), Tarnish.BlackFireDamage, 2, player.whoAmI);
            }
            return null;
        }
        public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling *= 1 + player.Entropy().VoidCharge * 0.5f;
            ascentWhenRising *= 1 + player.Entropy().VoidCharge * 0.5f;
            maxAscentMultiplier *= 1 + player.Entropy().VoidCharge * 0.5f;
            maxCanAscendMultiplier *= 1 + player.Entropy().VoidCharge * 0.5f;
            constantAscend *= 1 + player.Entropy().VoidCharge * 0.5f;
            ascentWhenFalling *= player.Entropy().WingSpeed;
            ascentWhenRising *= player.Entropy().WingSpeed;
            maxAscentMultiplier *= player.Entropy().WingSpeed;
            maxCanAscendMultiplier *= player.Entropy().WingSpeed;
            constantAscend *= player.Entropy().WingSpeed;

        }

        public override bool InstancePerEntity => true;
        public string armorPrefixName = string.Empty;
        public ArmorPrefix armorPrefix = null;
        public override void SaveData(Item item, TagCompound tag)
        {
            tag.Add("ArmorPrefix", armorPrefixName);
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            if (tag.ContainsKey("ArmorPrefix"))
            {
                armorPrefixName = tag.Get<string>("ArmorPrefix");
                ArmorPrefix result = ArmorPrefix.findByName(armorPrefixName);
                armorPrefix = result;
            }
        }
        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(armorPrefixName);
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            armorPrefixName = reader.ReadString();
            armorPrefix = ArmorPrefix.findByName(armorPrefixName);
        }

        public static string getAmmoName(int type)
        {
            var Mod = CalamityEntropy.Instance;
            if (type == AmmoID.Solution)
            {
                return Mod.GetLocalization("AmmoSolution").Value;
            }
            if (type == AmmoID.Arrow)
            {
                return Mod.GetLocalization("AmmoArrow").Value;
            }
            if (type == AmmoID.Bullet)
            {
                return Mod.GetLocalization("AmmoBullet").Value;
            }
            if (type == AmmoID.CandyCorn)
            {
                return Mod.GetLocalization("AmmoCandyCorn").Value;
            }
            if (type == AmmoID.Coin)
            {
                return Mod.GetLocalization("AmmoCoin").Value;
            }
            if (type == AmmoID.Dart)
            {
                return Mod.GetLocalization("AmmoDart").Value;
            }
            if (type == AmmoID.FallenStar)
            {
                return Mod.GetLocalization("AmmoFallenStar").Value;
            }
            if (type == AmmoID.Flare)
            {
                return Mod.GetLocalization("AmmoFlare").Value;
            }
            if (type == AmmoID.Gel)
            {
                return Mod.GetLocalization("AmmoGel").Value;
            }
            if (type == AmmoID.JackOLantern)
            {
                return Mod.GetLocalization("AmmoJackOLantern").Value;
            }
            if (type == AmmoID.NailFriendly)
            {
                return Mod.GetLocalization("AmmoNail").Value;
            }
            if (type == AmmoID.Rocket)
            {
                return Mod.GetLocalization("AmmoRocket").Value;
            }
            if (type == AmmoID.Sand)
            {
                return Mod.GetLocalization("AmmoSand").Value;
            }
            if (type == AmmoID.Snowball)
            {
                return Mod.GetLocalization("AmmoSnowball").Value;
            }
            if (type == AmmoID.Stake)
            {
                return Mod.GetLocalization("AmmoStake").Value;
            }
            if (type == AmmoID.StyngerBolt)
            {
                return Mod.GetLocalization("AmmoStyngerBolt").Value;
            }
            if (type == 353)
            {
                return Mod.GetLocalization("AmmoAle").Value;
            }
            if (ModLoader.HasMod("MoreBoulders") && type == 540)
            {
                return Mod.GetLocalization("AmmoBoulders").Value;
            }
            // 原灾厄星耀煤灰弹药组（3728）按 material-map/misc-map 定稿改指自有星辉鳞尘，文案键复用
            if (type == ModContent.ItemType<StarlitScaleDust>())
            {
                return Mod.GetLocalization("AmmoStarblightSoot").Value;
            }
            if (type == 5809)
            {
                return Mod.GetLocalization("AmmoBloodrune").Value;
            }
            if (type == BaseMissileProj.AmmoType)
            {
                return Mod.GetLocalization("AmmoMissile").Value;
            }
            if (type == 520)
            {
                return Mod.GetLocalization("AmmoSouls").Value;
            }
            return type.ToString();
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.ModItem != null)
            {
                Mod mod = item.ModItem.Mod;
                if (item.ModItem is IAzafureEnhancable && Main.LocalPlayer.AzafureEnhance())
                {
                    tooltips.Add(new TooltipLine(Mod, "Azafure Enhance", $"{Mod.GetLocalization("AzafureEnhance").Value}: " + mod.GetLocalization($"AzafureEnhances.{item.ModItem.Name}").Value) { OverrideColor = Color.Yellow });
                }
                if (item.ModItem is ExquisiteCrown || item.ModItem is RottenFangs)
                {
                    LocalizedText itemName = item.ModItem is ExquisiteCrown ? ModContent.GetInstance<RottenFangs>().DisplayName : ModContent.GetInstance<ExquisiteCrown>().DisplayName;
                    TooltipLine lineExtra = new TooltipLine(Mod, "Desc2", Mod.GetLocalization("MinionAccDescCrownFangs").Value.Replace("[ITEM]", itemName.Value));
                    lineExtra.OverrideColor = (Main.LocalPlayer.Entropy().exquisiteCrown && Main.LocalPlayer.Entropy().rottenFangs) ? Color.Yellow : Color.Gray;
                    tooltips.Add(lineExtra);
                }
            }
            if (ModContent.GetInstance<Config>().ItemAdditionalInfo)
            {
                if (item.ammo != AmmoID.None)
                {
                    tooltips.Add(new TooltipLine(Mod, "Ammo Type", Mod.GetLocalization("AmmoType").Value + ": " + getAmmoName(item.ammo)));
                    if (item.shoot > ProjectileID.None)
                    {
                        tooltips.Add(new TooltipLine(Mod, "Ammo Life Time", Mod.GetLocalization("AmmoLifeTime").Value + ": " + Math.Round((CalamityEntropy.GetAProjectileInstance(item.shoot).timeLeft / (float)CalamityEntropy.GetAProjectileInstance(item.shoot).MaxUpdates) / 60f, 2).ToString() + "s"));
                        tooltips.Add(new TooltipLine(Mod, "Ammo Shoot Speed", Mod.GetLocalization("AmmoShootSpeed").Value + ": " + ((item.shootSpeed * (float)CalamityEntropy.GetAProjectileInstance(item.shoot).MaxUpdates)).ToString()));
                        tooltips.Add(new TooltipLine(Mod, "Ammo Penetrate", Mod.GetLocalization("AmmoPenetrate").Value + ": " + ((CalamityEntropy.GetAProjectileInstance(item.shoot).penetrate) >= 0 ? (CalamityEntropy.GetAProjectileInstance(item.shoot).penetrate - 1).ToString() : Mod.GetLocalization("AmmoPenetrateInfinite").Value)));
                        if (CalamityEntropy.GetAProjectileInstance(item.shoot).ArmorPenetration > 0)
                        {
                            tooltips.Add(new TooltipLine(Mod, "Ammo Armor Penetration", Mod.GetLocalization("ArmorPenetrationItemTooltip").Value + ": " + (CalamityEntropy.GetAProjectileInstance(item.shoot).ArmorPenetration).ToString()));
                        }
                    }
                }
                if (item.useAmmo != AmmoID.None)
                {
                    tooltips.Add(new TooltipLine(Mod, "Use Ammo", Mod.GetLocalization("UseAmmo").Value + ": " + getAmmoName(item.useAmmo)));
                }
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Mod == "Terraria" && tooltips[i].Name == "Knockback")
                    {
                        if (item.damage > 0 && item.ArmorPenetration > 0)
                        {
                            tooltips.Insert(i + 1, new TooltipLine(Mod, "Armor Penetration", Mod.GetLocalization("ArmorPenetrationItemTooltip").WithFormatArgs(item.ArmorPenetration.ToString()).Value));
                        }
                    }
                }
            }
            int index = 0;
            int tIndex = 0;
            foreach (var tooltip in tooltips)
            {
                if (tooltip.Mod == "Terraria")
                {
                    if (tooltip.Name.Contains("Tooltip"))
                    {
                        tIndex = index;
                    }
                }
                index++;
            }
            if (item.ModItem != null)
            {
                if (item.ModItem is ThreadOfFate || item.ModItem is ThreadOfAbyss || item.ModItem is CursedThread || item.ModItem is OracleDeck || item.ModItem is TaintedDeck || item.ModItem is SoulDeck)
                    goto DeckEnd;
                string ns = (item.ModItem.GetType()).Namespace;
                if (ns.Contains("CalamityEntropy.Content.Items.Accessories.Cards"))
                {
                    tooltips.Insert(tIndex + 1, new TooltipLine(Mod, $"Tooltip{tIndex + 1}", Mod.GetLocalization("CardsDesc").Value) { OverrideColor = Color.SkyBlue });
                }
                if (ns.Contains("CalamityEntropy.Content.Items.Accessories.EvilCards"))
                {
                    tooltips.Insert(tIndex + 1, new TooltipLine(Mod, $"Tooltip{tIndex + 1}", Mod.GetLocalization("CardsDesc").Value) { OverrideColor = Color.Red });
                }
                if (ns.Contains("CalamityEntropy.Content.Items.Accessories.SoulCards"))
                {
                    tooltips.Insert(tIndex + 1, new TooltipLine(Mod, $"Tooltip{tIndex + 1}", Mod.GetLocalization("CardsDesc").Value) { OverrideColor = Color.Yellow });
                }
            }
        DeckEnd:
            if (item.Entropy().armorPrefix != null)
            {
                foreach (var tooltip in tooltips)
                {
                    if (tooltip.Mod == "Terraria")
                    {
                        if (tooltip.Name == "ItemName")
                        {
                            tooltip.Text = item.Entropy().armorPrefix.getName() + " " + tooltip.Text;
                        }
                        if (tooltip.Name == "Defense" && armorPrefix.AddDefense() != 0)
                        {
                            int df = (int)(Math.Ceiling(item.defense * armorPrefix.AddDefense()));
                            tooltip.Text += (armorPrefix.AddDefense() > 0 ? "(+" : "(") + df.ToString() + ")";
                        }
                    }
                }
            }
            if (item.type == ModContent.ItemType<VoidFaquirBodyArmor>() || item.type == ModContent.ItemType<VoidFaquirCuises>() || item.type == ModContent.ItemType<VoidFaquirCosmosHood>() || item.type == ModContent.ItemType<VoidFaquirDevourerHelm>() || item.type == ModContent.ItemType<VoidFaquirEvokerHelm>() || item.type == ModContent.ItemType<VoidFaquirLurkerMask>() || item.type == ModContent.ItemType<VoidFaquirShadowHelm>())
            {
                if (Main.LocalPlayer.Entropy().VFSet)
                {
                    TooltipLine t = new TooltipLine(CalamityEntropy.Instance, "Armor Bonus", Language.GetOrRegister("Mods.CalamityEntropy.vfb").Value);
                    tooltips.Add(t);
                }
                if (Main.LocalPlayer.Entropy().VFHelmMagic)
                {
                    TooltipLine t = new TooltipLine(CalamityEntropy.Instance, "Armor Bonus", Language.GetOrRegister("Mods.CalamityEntropy.helmvfc").Value);
                    tooltips.Add(t);
                }
                if (Main.LocalPlayer.Entropy().VFHelmMelee)
                {
                    TooltipLine t = new TooltipLine(CalamityEntropy.Instance, "Armor Bonus", Language.GetOrRegister("Mods.CalamityEntropy.helmvfd").Value);
                    tooltips.Add(t);
                }
                if (Main.LocalPlayer.Entropy().VFHelmRanged)
                {
                    TooltipLine t = new TooltipLine(CalamityEntropy.Instance, "Armor Bonus", Language.GetOrRegister("Mods.CalamityEntropy.helmvfs").Value);
                    tooltips.Add(t);
                }
                if (Main.LocalPlayer.Entropy().VFHelmRogue)
                {
                    TooltipLine t = new TooltipLine(CalamityEntropy.Instance, "Armor Bonus", Language.GetOrRegister("Mods.CalamityEntropy.helmvfl").Value);
                    tooltips.Add(t);
                }
                if (Main.LocalPlayer.Entropy().VFHelmSummoner)
                {
                    TooltipLine t = new TooltipLine(CalamityEntropy.Instance, "Armor Bonus", Language.GetOrRegister("Mods.CalamityEntropy.helmvfe").Value);
                    tooltips.Add(t);
                }
            }
            if (armorPrefix != null)
            {
                tooltips.Add(armorPrefix.getDescTooltipLine());
            }
            if (item.Entropy().Legend)
            {
                TooltipLine tl = new TooltipLine(CalamityEntropy.Instance, "LegendItem", Language.GetTextValue("Mods.CalamityEntropy.LegendTooltip"));
                tl.OverrideColor = new Microsoft.Xna.Framework.Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);
                tooltips.Add(tl);
            }
            // 灾厄家族软集成（灾厄重制弱引用 tooltip 注入）已随脱钩整体移除
        }

        public override GlobalItem Clone(Item from, Item to)
        {
            EGlobalItem obj = (EGlobalItem)base.Clone(from, to);
            obj.Legend = Legend;
            obj.tooltipStyle = tooltipStyle;
            obj.stroke = stroke;
            obj.strokeColor = strokeColor;
            obj.NameColor = NameColor;
            obj.HasCustomNameColor = HasCustomNameColor;
            obj.HasCustomStrokeColor = HasCustomStrokeColor;
            obj.armorPrefix = armorPrefix;
            obj.armorPrefixName = armorPrefixName;
            return obj;
        }

        public override bool CanUseItem(Item item, Player player)
        {
            // 潜行系统退役：原「换装清空灾厄潜行值」拦截已移除（ServerConfig.ClearStealthWhenChangeEquipSet 已一并删除）
            if (player.GetModPlayer<AtbmPlayer>().Active && item.ModItem is not AzafureTBMTerminal)
                return false;
            if ((CalamityEntropy.EntropyMode && player.Entropy().HitTCounter > 0) && item.healLife > 0)
            {
                return false;
            }
            if (player.HasBuff(ModContent.BuffType<StealthState>()) || player.Entropy().DarkArtsTarget.Count > 0 || player.Entropy().noItemTime > 0)
            {
                return false;
            }
            return base.CanUseItem(item, player);
        }
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 影约/疾风腕刃/崇拜圣物的旧潜行接线已整体退役，新效果由饰品文件自含实现
            if (type == ModContent.ProjectileType<RockBulletShot>())
            {
                if (Main.rand.NextBool(6))
                {
                    CEUtils.PlaySound("gunshot_small" + Main.rand.Next(1, 4).ToString(), 1, position);
                    return false;
                }
            }
            if (!Main.dedServ)
            {
                if (item.DamageType != DamageClass.Summon)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        if (player.ownedProjectileCounts[ModContent.ProjectileType<TwistedTwinMinion>()] > 0)
                        {
                            if ((item.useAmmo == AmmoID.Arrow && type == ProjectileID.WoodenArrowFriendly) || (item.useAmmo == AmmoID.Bullet && type == ProjectileID.Bullet))
                            {
                                type = item.shoot;

                            }
                            else if (item.useAmmo == AmmoID.Arrow || item.useAmmo == AmmoID.Bullet)
                            {
                                Item t = player.ChooseAmmo(item);
                                if (t != null)
                                {
                                    type = t.shoot;
                                }
                            }
                            foreach (Projectile p in Main.projectile)
                            {
                                if (p.type == ModContent.ProjectileType<TwistedTwinMinion>() && p.active && p.owner == Main.myPlayer)
                                {
                                    player.Entropy().twinSpawnIndex = p.identity;
                                    p.ai[0] = 30;
                                    if (item.ModItem == null)
                                    {
                                        int pj = Projectile.NewProjectile(p.GetSource_FromAI(), position + p.Center - player.Center, velocity, type, (int)(damage * TwistedTwinMinion.damageMul), knockback, Main.myPlayer);

                                        pj.ToProj().scale *= 0.8f;
                                        pj.ToProj().Entropy().IndexOfTwistedTwinShootedThisProj = p.identity;
                                        pj.ToProj().netUpdate = true;

                                        Projectile projts = pj.ToProj();
                                        if (!projts.usesLocalNPCImmunity)
                                        {
                                            pj.ToProj().usesLocalNPCImmunity = true;
                                            pj.ToProj().localNPCHitCooldown = 12;
                                        }
                                    }
                                    else
                                    {
                                        if (item.ModItem.Shoot(player, source, position + p.Center - player.Center, velocity, type, (int)(damage * TwistedTwinMinion.damageMul), knockback))
                                        {
                                            int pj = Projectile.NewProjectile(p.GetSource_FromAI(), position + p.Center - player.Center, velocity, type, (int)(damage * TwistedTwinMinion.damageMul), knockback, Main.myPlayer);
                                            pj.ToProj().scale *= 0.8f;
                                            pj.ToProj().Entropy().IndexOfTwistedTwinShootedThisProj = p.identity;
                                            pj.ToProj().netUpdate = true;
                                            Projectile projts = pj.ToProj();
                                            if (!projts.usesLocalNPCImmunity)
                                            {
                                                pj.ToProj().usesLocalNPCImmunity = true;
                                                pj.ToProj().localNPCHitCooldown = 12;
                                            }
                                        }
                                    }
                                    player.Entropy().twinSpawnIndex = -1;
                                    /*int pj = Projectile.NewProjectile(p.GetSource_FromAI(), position + p.Center - player.Center, velocity, type, (int)(damage * 0.26f), knockback, Main.myPlayer);
                                    *                                     {
                                        
                                    }*                                     
                                    pj.ToProj().scale *= 0.8f;
                                    
                                    pj.ToProj().Entropy().ttindex = p.whoAmI;*/
                                }

                            }
                        }
                    }
                }
            }

            return true;
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 2026-08-31 平衡案:瘟疫内燃机重做,原真近战效果退役(新效果统一挂 EModPlayer.OnHitNPC)
            // 原对灾厄「星流brand」的 WeaponBoost 强化（追加星辰弹幕）已随灾厄脱钩移除
        }

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            float counter = ModContent.GetInstance<EModSys>().counter;
            Color namecolor = line.Color;
            if (!HasCustomNameColor)
            {
                namecolor = (Color)item.Entropy().NameColor;
            }
            if (line.Mod == "Terraria")
            {
                if (item.type == ModContent.ItemType<TheFilthyContractWithMammon>() && line.Text.Contains("*"))
                {
                    return false;
                }
                if (line.Text.Contains("$"))
                {
                    if (item.type == ModContent.ItemType<TheFilthyContractWithMammon>())
                    {
                        float p = 1;
                        Main.spriteBatch.Draw(CEExtraAssets.T1, new Vector2(line.X, line.Y - 4) + new Vector2(p, p), Color.Red); Main.spriteBatch.Draw(CEExtraAssets.T1, new Vector2(line.X, line.Y - 4), Color.Red);
                        Main.spriteBatch.Draw(CEExtraAssets.T1, new Vector2(line.X, line.Y - 4) + new Vector2(-p, p), Color.Red);
                        Main.spriteBatch.Draw(CEExtraAssets.T1, new Vector2(line.X, line.Y - 4) + new Vector2(p, -p), Color.Red);
                        Main.spriteBatch.Draw(CEExtraAssets.T1, new Vector2(line.X, line.Y - 4) + new Vector2(-p, -p), Color.Red);

                        Main.spriteBatch.Draw(CEExtraAssets.T1, new Vector2(line.X, line.Y - 4), Color.Black);


                        return false;
                    }
                    if (item.type == ModContent.ItemType<CelestialChronometer>())
                    {
                        string textall = line.Text.Replace("$", "");
                        float xa = 0; var font = FontAssets.MouseText.Value;
                        float h = 0;
                        for (int i = 0; i < textall.Length; i++)
                        {
                            var text = textall[i].ToString();
                            Vector2 size = font.MeasureString(text);
                            float yofs;
                            if (size.Y > h)
                            {
                                h = size.Y;
                            }
                            Color color = Color.White;
                            yofs = 0;
                            Color strokeColord = Main.DiscoColor;

                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);

                            xa += size.X + 2;

                        }
                        SpriteBatch sb = Main.spriteBatch;
                        sb.End();
                        sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                        Texture2D glow = CEExtraAssets.Glow;
                        sb.Draw(glow, new Vector2(line.X + xa / 2, line.Y + h / 4), null, new Color(255, 255, 255) * 0.6f, 0, glow.Size() / 2, new Vector2((32 + xa * 2.4f) / glow.Width, 0.34f), SpriteEffects.None, 0);
                        sb.End();
                        sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);

                        return false;
                    }
                    if (item.type == ModContent.ItemType<ScorchingShoot>())
                    {
                        string textall = line.Text.Replace("$", "");
                        float xa = 0; var font = FontAssets.MouseText.Value;
                        float h = 0;
                        for (int i = 0; i < textall.Length; i++)
                        {
                            var text = textall[i].ToString();
                            Vector2 size = font.MeasureString(text);
                            float yofs;
                            if (size.Y > h)
                            {
                                h = size.Y;
                            }
                            Color color = Color.White;
                            yofs = 0;
                            Color strokeColord = Color.Orange;

                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);

                            xa += size.X + 2;

                        }
                        SpriteBatch sb = Main.spriteBatch;
                        sb.End();
                        sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                        Texture2D glow = CEExtraAssets.Glow;
                        sb.Draw(glow, new Vector2(line.X + xa / 2, line.Y + h / 4), null, Color.Orange * 0.8f, 0, glow.Size() / 2, new Vector2((32 + xa * 2.6f) / glow.Width, 0.34f), SpriteEffects.None, 0);
                        sb.End();
                        sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);

                        return false;
                    }
                }
            }
            if (line.Name == "ItemName")
            {
                if (item.rare == ModContent.RarityType<ShiningViolet>())
                {
                    ShiningViolet.Draw(item, line);
                    return false;
                }
                if (item.rare == ModContent.RarityType<Lunarblight>())
                {
                    Lunarblight.Draw(item, line);
                    return false;
                }
                if (item.rare == ModContent.RarityType<NihilityBlue>())
                {
                    NihilityBlue.Draw(item, line);
                    return false;
                }
                if (item.rare == ModContent.RarityType<AzafureOrange>())
                {
                    AzafureOrange.Draw(item, line);
                    return false;
                }
                if (item.Entropy().tooltipStyle == 1 || item.Entropy().tooltipStyle == 4)
                {
                    float xa = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();
                        var font = FontAssets.MouseText.Value;
                        Vector2 size = font.MeasureString(text);
                        float yofs;
                        int cj = (int)(Math.Cos(counter / 14 - i * 1) * 50);
                        Color color = new Color(namecolor.R + cj, namecolor.G + cj, namecolor.B + cj, namecolor.A);
                        if (color.R > 255)
                        {
                            color.R = 255;
                        }
                        if (color.G > 255)
                        {
                            color.G = 255;
                        }
                        if (color.B > 255)
                        {
                            color.B = 255;
                        }
                        if (color.R < 0)
                        {
                            color.R = 0;
                        }
                        if (color.G < 0)
                        {
                            color.G = 0;
                        }
                        if (color.B < 0)
                        {
                            color.B = 0;
                        }

                        yofs = 0;
                        if (item.Entropy().tooltipStyle == 1)
                        {
                            yofs = (float)(Math.Cos(counter / 14 - i * 1) * 1.3f) + 1f;
                        }
                        if (item.Entropy().stroke)
                        {
                            Color strokeColord = Color.White;
                            if (!HasCustomStrokeColor)
                            {
                                strokeColord = color;
                                strokeColord.R = (byte)(strokeColord.R * 0.2f);
                                strokeColord.G = (byte)(strokeColord.G * 0.2f);
                                strokeColord.B = (byte)(strokeColord.B * 0.2f);

                            }
                            else
                            {
                                strokeColord = (Color)strokeColor;
                            }
                            strokeColord.A = 255;
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                            Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);

                        }
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);



                        xa += size.X;
                        if (item.Entropy().stroke)
                        {
                            xa += 2;
                        }
                    }
                    return false;
                }
                if (item.rare == ModContent.RarityType<VoidPurple>())
                {
                    var font = FontAssets.MouseText.Value;
                    Texture2D glow = CEExtraAssets.Glow;
                    Main.spriteBatch.UseBlendState_UI(BlendState.Additive);
                    Vector2 origin = font.MeasureString(line.Text) * new Vector2(1, 0.6f) * 0.5f;
                    Main.spriteBatch.Draw(glow, new Vector2(line.X, line.Y) + origin, null, Color.AliceBlue * 0.6f, 0, glow.Size() * 0.5f, origin * 0.02f * new Vector2(1, 0.6f), SpriteEffects.None, 0);
                    Main.spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
                    float xa = 0;
                    List<float> scales = new List<float>() { 0, 0.5f };
                    Vector2 ms = font.MeasureString(line.Text);
                    ms.Y *= 0.7f;
                    for (int i_ = 0; i_ < scales.Count; i_++)
                    {
                        scales[i_] = CEUtils.Frac(scales[i_] + Main.GlobalTimeWrappedHourly);
                        float sc = scales[i_] * 12f;
                        Main.spriteBatch.DrawString(font, line.Text, new Vector2(-sc, 0) + new Vector2(line.X, line.Y) + ms * 0.5f, Color.Lerp(new Color(190, 50, 190), new Color(160, 0, 180), scales[i_]) * (1 - scales[i_]), 0, ms * 0.5f, 1, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, line.Text, new Vector2(sc, 0) + new Vector2(line.X, line.Y) + ms * 0.5f, Color.Lerp(new Color(190, 50, 190), new Color(160, 0, 180), scales[i_]) * (1 - scales[i_]), 0, ms * 0.5f, 1, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, line.Text, new Vector2(0, sc) + new Vector2(line.X, line.Y) + ms * 0.5f, Color.Lerp(new Color(190, 50, 190), new Color(160, 0, 180), scales[i_]) * (1 - scales[i_]), 0, ms * 0.5f, 1, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, line.Text, new Vector2(0, -sc) + new Vector2(line.X, line.Y) + ms * 0.5f, Color.Lerp(new Color(190, 50, 190), new Color(160, 0, 180), scales[i_]) * (1 - scales[i_]), 0, ms * 0.5f, 1, SpriteEffects.None, 0);

                    }
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();
                        Vector2 size = font.MeasureString(text);
                        float yofs;
                        float lerp = 0.5f + (0.5f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * -6 + i * 3f / line.Text.Length)));
                        Color color = Color.Lerp(Color.Black, new Color(20, 16, 25), lerp);
                        Color strokeColord = new Color(160, 100, 255);
                        yofs = 0;


                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);



                        xa += size.X;

                    }
                    return false;
                }
                if (item.rare == ModContent.RarityType<Soulight>())
                {
                    var font = FontAssets.MouseText.Value;
                    float xa = 0;
                    float h = 0;
                    float xy = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        xy = size.Y;
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        xa += size.X + 0;

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    sb.End();
                    sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    Texture2D glow = SoulightTex.Value;
                    sb.Draw(glow, new Vector2(line.X + xa / 2 + 1, line.Y + xy / 3), null, new Color(255, 255, 255) * 0.8f, 0, new Vector2(glow.Width / 2, glow.Height / 2), new Vector2((xa + 14) / glow.Width, (xy - 8) / glow.Height), SpriteEffects.None, 0);
                    sb.End();
                    sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);

                    xa = 0;
                    h = 0;
                    xy = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        xy = size.Y;
                        float yofs;
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        Color color = new Color(210, 240, 255);
                        yofs = 0;
                        Color strokeColord = new Color(40, 140, 255);

                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);

                        xa += size.X + 0;

                    }
                    return false;
                }
                if (tooltipStyle == 8)
                {
                    float xa = 0; var font = FontAssets.MouseText.Value;
                    float h = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        float yofs;
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        Color color = namecolor;
                        yofs = 0;
                        Color strokeColord = strokeColor;

                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);

                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);
                        xa += size.X;
                    }
                    SpriteBatch sb = Main.spriteBatch;
                    sb.End();
                    sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    Texture2D glow = CEExtraAssets.Glow;
                    sb.Draw(glow, new Vector2(line.X + xa / 2, line.Y + h / 4), null, NameLightColor, 0, glow.Size() / 2, new Vector2((32 + xa * 2.4f) / glow.Width, 0.34f), SpriteEffects.None, 0);
                    sb.End();
                    sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);

                    return false;
                }
                if (item.ModItem != null && item.ModItem is DustCarver)
                {
                    float xa = 0; var font = FontAssets.MouseText.Value;
                    float h = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        xa += size.X;

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    sb.End();
                    sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    Texture2D glow = CEExtraAssets.Glow;
                    float ey = CELists.tooltipNameUpList.Contains(Language.ActiveCulture.Name) ? 0 : 3;
                    sb.Draw(glow, new Vector2(line.X + xa / 2, line.Y + h / 4 + ey), null, new Color(255, 0, 0), 0, glow.Size() / 2, new Vector2((32 + xa * 2.4f) / glow.Width, 0.26f), SpriteEffects.None, 0);
                    sb.End();
                    sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    xa = 0;
                    h = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        float yofs;
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        Color color = new Color(0, 0, 0);
                        yofs = 0;
                        Color strokeColord = new Color(255, 0, 0);

                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);

                        xa += size.X;

                    }
                    return false;
                }
                if (item.rare == ModContent.RarityType<AbyssalBlue>())
                {
                    Texture2D glow = CEExtraAssets.Glow;
                    Texture2D star = CEExtraAssets.StarTexture;
                    var font = FontAssets.MouseText.Value;
                    float xa = 0;
                    float h = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();
                        Vector2 size = font.MeasureString(text);
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        xa += size.X + 2;
                    }
                    SpriteBatch sb = Main.spriteBatch;
                    sb.End();
                    sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    float ssz = 1f + (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 36) * 0.14f);
                    float ey = CELists.tooltipNameUpList.Contains(Language.ActiveCulture.Name) ? 0 : 4;
                    sb.Draw(star, new Vector2(line.X + xa / 2, line.Y + h / 4 + ey), null, new Color(140, 150, 255), 0, star.Size() / 2, ssz * new Vector2((12 + xa * 2f) / glow.Width, 0.1f), SpriteEffects.None, 0);
                    sb.Draw(star, new Vector2(line.X + xa / 2, line.Y + h / 4 + ey), null, new Color(140, 150, 255), 0, star.Size() / 2, ssz * new Vector2((12 + xa * 2f) / glow.Width * 0.2f, 0.16f), SpriteEffects.None, 0);
                    sb.End();
                    sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    xa = h = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        float yofs;
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        Color color = Color.Lerp(new Color(255, 210, 12), new Color(140, 180, 255), (i / (line.Text.Length - 1f)));
                        yofs = 0;
                        Color strokeColord = Color.Lerp(new Color(70, 110, 255), new Color(150, 155, 180), (i / (line.Text.Length - 1f)));
                        float n = (float)(0.25f * Math.Sin(Main.GlobalTimeWrappedHourly * -6 + i * 0.65f));
                        strokeColord *= 1 + n;
                        color *= 1 + n;

                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);

                        xa += size.X + 2;

                    }
                    sb.End();
                    sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    sb.Draw(glow, new Vector2(line.X + xa / 2, line.Y + h / 4), null, new Color(140, 150, 255) * 0.7f, 0, glow.Size() / 2, new Vector2((32 + xa * 2.4f) / glow.Width, 0.36f), SpriteEffects.None, 0);
                    sb.Draw(glow, new Vector2(line.X + xa / 2, line.Y + h / 4), null, new Color(140, 150, 255) * 0.5f, 0, glow.Size() / 2, new Vector2((42 + xa * 2.4f) / glow.Width, 0.16f), SpriteEffects.None, 0);
                    sb.End();
                    sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);

                    return false;
                }
                if (item.ModItem != null && item.ModItem is DustCarver)
                {
                    float xa = 0; var font = FontAssets.MouseText.Value;
                    float h = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        xa += size.X;

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    sb.End();
                    sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    Texture2D glow = CEExtraAssets.Glow;
                    sb.Draw(glow, new Vector2(line.X + xa / 2, line.Y + h / 4), null, new Color(255, 0, 0) * 0.8f, 0, glow.Size() / 2, new Vector2((32 + xa * 2.4f) / glow.Width, 0.34f), SpriteEffects.None, 0);
                    sb.End();
                    sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    xa = 0;
                    h = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        float yofs;
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        Color color = new Color(0, 0, 0);
                        yofs = 0;
                        Color strokeColord = new Color(255, 0, 0);

                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);

                        xa += size.X;

                    }
                    return false;
                }
                if (item.rare == ModContent.RarityType<Golden>())
                {
                    float xa = 0; var font = FontAssets.MouseText.Value;
                    float h = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();

                        Vector2 size = font.MeasureString(text);
                        float yofs;
                        if (size.Y > h)
                        {
                            h = size.Y;
                        }
                        Color color = new Color(120, 120, 240);
                        yofs = 0;
                        Color strokeColord = new Color(250, 200, 10);

                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);

                        xa += size.X + 2;

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    sb.End();
                    sb.Begin(0, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
                    Texture2D glow = CEExtraAssets.Glow;
                    float ey = CELists.tooltipNameUpList.Contains(Language.ActiveCulture.Name) ? 0 : 4;
                    sb.Draw(glow, new Vector2(line.X + xa / 2, line.Y + h / 4 + ey), null, new Color(210, 180, 120) * 0.8f, 0, glow.Size() / 2, new Vector2((32 + xa * 2.4f) / glow.Width, 0.34f), SpriteEffects.None, 0);
                    sb.End();
                    sb.Begin(0, BlendState.AlphaBlend, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);

                    return false;
                }
                if (item.rare == ModContent.RarityType<GlowGreen>() || item.rare == ModContent.RarityType<GlowPurple>() || item.rare == ModContent.RarityType<SkyBlue>())
                {
                    float xa = 0;
                    for (int i = 0; i < line.Text.Length; i++)
                    {
                        string text = line.Text[i].ToString();
                        var font = FontAssets.MouseText.Value;
                        Vector2 size = font.MeasureString(text);
                        float yofs;
                        Color color = new Color(80, 255, 80);
                        if (item.rare == ModContent.RarityType<GlowPurple>())
                        {
                            color = new Color(160, 80, 230);
                        }
                        if (item.rare == ModContent.RarityType<SkyBlue>())
                        {
                            color = new Color(84, 84, 255);
                        }
                        yofs = 0;
                        Color strokeColord = new Color(210, 255, 210);
                        /*if (item.rare == ModContent.RarityType<GlowPurple>())
                        {
                            strokeColord = new Color(146, 86, 240);
                        }
                        if (item.rare == ModContent.RarityType<SkyBlue>())
                        {
                            strokeColord = new Color(180, 180, 255);
                        }*/
                        strokeColord = color;
                        color *= 0.3f;
                        color.A = 255;
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);


                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);



                        xa += size.X + 1;

                    }
                    return false;
                }
            }
            if (line.Name == "LegendItem" || (line.Name == "ItemName" && item.Entropy().tooltipStyle == 2))
            {
                float xa = 0;
                for (int i = 0; i < line.Text.Length; i++)
                {
                    string text = line.Text[i].ToString();
                    var font = FontAssets.MouseText.Value;
                    Vector2 size = font.MeasureString(text);
                    float yofs;
                    int cj = (int)(Math.Cos(counter / 10 - i * 1) * 70);
                    Color color = new Color(Main.DiscoR + cj, Main.DiscoG + cj, Main.DiscoB + cj, namecolor.A);
                    if (color.R > 255)
                    {
                        color.R = 255;
                    }
                    if (color.G > 255)
                    {
                        color.G = 255;
                    }
                    if (color.B > 255)
                    {
                        color.B = 255;
                    }
                    if (color.R < 0)
                    {
                        color.R = 0;
                    }
                    if (color.G < 0)
                    {
                        color.G = 0;
                    }
                    if (color.B < 0)
                    {
                        color.B = 0;
                    }
                    yofs = (float)(Math.Cos(counter / 14) * 1.3f) + 1f;
                    if (item.Entropy().stroke)
                    {
                        Color strokeColord = Color.White;
                        if (!HasCustomStrokeColor)
                        {
                            strokeColord = color;
                            strokeColord.R = (byte)(strokeColord.R * 0.5f);
                            strokeColord.G = (byte)(strokeColord.G * 0.5f);
                            strokeColord.B = (byte)(strokeColord.B * 0.5f);
                        }
                        else
                        {
                            strokeColord = (Color)strokeColor;
                        }
                        strokeColord.A = 255;
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);

                    }
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs), color);
                    xa += size.X;
                    if (item.Entropy().stroke)
                    {
                        xa += 2;
                    }
                }
                return false;
            }
            if (line.Name == "ItemName" && item.Entropy().tooltipStyle == 3)
            {
                string text = line.Text.ToString();
                var font = FontAssets.MouseText.Value;
                Vector2 size = font.MeasureString(text);
                int cj = (int)(Math.Cos(counter / 16) * 50) - 40;
                Color color = new Color(namecolor.R + cj, namecolor.G + cj, namecolor.B + cj, namecolor.A);
                if (color.R > 255)
                {
                    color.R = 255;
                }
                if (color.G > 255)
                {
                    color.G = 255;
                }
                if (color.B > 255)
                {
                    color.B = 255;
                }
                if (color.R < 0)
                {
                    color.R = 0;
                }
                if (color.G < 0)
                {
                    color.G = 0;
                }
                if (color.B < 0)
                {
                    color.B = 0;
                }
                if (item.Entropy().stroke)
                {
                    Color strokeColord = Color.White;
                    if (!HasCustomStrokeColor)
                    {
                        strokeColord = color;
                        strokeColord.R = (byte)(strokeColord.R * 0.5f);
                        strokeColord.G = (byte)(strokeColord.G * 0.5f);
                        strokeColord.B = (byte)(strokeColord.B * 0.5f);
                    }
                    else
                    {
                        strokeColord = (Color)strokeColor;
                    }
                    strokeColord.A = 255;
                    int xa = 0;
                    int yofs = 0;
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(-1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(0, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, -1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 0), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.DrawString(font, text, new Vector2(line.X + xa, line.Y + yofs) + new Vector2(1, 1), strokeColord, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);

                }
                Main.spriteBatch.DrawString(font, text, new Vector2(line.X, line.Y), color);

                if (counter % 15 == 0)
                {
                    S3Particle pt = new S3Particle();
                    var r = Main.rand;
                    pt.velocity = new Vector2((float)r.Next(-2, 3) / 10, -(float)r.Next(4, 6) / 10);
                    pt.position = new Vector2(r.Next(0, (int)size.X), size.Y);

                    particles1.Add(pt);
                }

                Main.spriteBatch.UseBlendState_UI(BlendState.Additive);
                foreach (S3Particle p in particles1)
                {
                    p.update();
                    float alpha = 1;
                    if (p.position.Y > size.Y - 10)
                    {
                        alpha = (10f - (float)(p.position.Y - (size.Y - 10))) / 10;
                        if (alpha > 1)
                        {
                            alpha = 1;
                        }

                    }
                    if (p.position.Y < 8)
                    {
                        alpha = ((float)p.position.Y) / 8;
                    }
                    if (alpha > 1)
                    {
                        alpha = 1;
                    }
                    p.draw(alpha, new Vector2(line.X, line.Y), namecolor);

                }
                Main.spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
                foreach (S3Particle p in particles1)
                {
                    if (p.position.Y < -30)
                    {
                        particles1.Remove(p);
                        break;
                    }
                }
                return false;
            }
            return true;
        }

        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            if (item.type == ItemID.DeerclopsBossBag)
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookmarkSnowgrave>(), 5));
            if (item.type == ItemID.KingSlimeBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ExquisiteCrown>(), 2));
            }
            if (item.type == ItemID.EaterOfWorldsBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedTorch>(), 2));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<MindCorruptor>(), 5));
            }
            if (item.type == ItemID.BrainOfCthulhuBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CreeperWand>(), 2));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SinewLash>(), 5));
            }
            if (item.type == ItemID.EyeOfCthulhuBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RottenFangs>(), 2));
            }
            if (item.type == ItemID.FishronBossBag)
            {
                itemLoot.Add(ItemDropRule.ByCondition(new IsDeathMode(), ModContent.ItemType<IlmeranAsylum>()));
            }
            if (item.type == ItemID.FloatingIslandFishingCrate)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<IndigoCard>(), 5));
            }
            if (item.type == ItemID.FloatingIslandFishingCrateHard)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<IndigoCard>(), 5));
            }
            if (item.type == ItemID.GolemBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<MourningCard>()));
            }
            if (item.type == 3203 || item.type == 3204 || item.type == 3983 || item.type == 3982)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ObscureCard>(), 5));
            }
            // 灾厄宝袋掉落注入已整体拆除，按 bookmark-rehang.md 重挂（见本方法末尾重挂段）
            if (item.type == ItemID.QueenSlimeBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Crystedge>(), 3));
            }
            if (item.type == ItemID.SkeletronBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<OblivionSkull>()));
            }
            if (item.type == ItemID.QueenBeeBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkBee>()));
            }
            if (item.type == ItemID.EaterOfWorldsBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkCorrupt>(), 2));
            }
            if (item.type == ItemID.BrainOfCthulhuBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkCrimson>(), 2));
            }
            if (item.type == ItemID.WallOfFleshBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkFlesh>()));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HungryLantern>(), 5));
            }
            if (item.Is<NihilityTwinBag>())
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkGemini>()));
            }
            // 灾厄家族联动模组（猎杀灾厄 / 星辉灾变）宝袋软集成已随脱钩整体移除
            if (item.type == ItemID.FairyQueenBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkLibra>()));
            }
            if (item.type == ItemID.MoonLordBossBag)
            {
                // 原 3/5 与 2/5 概率，CommonDrop 分子写法保持不化简
                itemLoot.Add(new CommonDrop(ModContent.ItemType<BookMarkLunar>(), 5, 1, 1, 3));
                itemLoot.Add(new CommonDrop(ModContent.ItemType<MoonlightCore>(), 5, 1, 1, 2));
            }
            if (item.type == ItemID.QueenSlimeBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkOfLight>()));
            }
            if (item.type == ItemID.FishronBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkPisces>()));
            }
            if (item.type == ItemID.EyeOfCthulhuBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkVirgo>(), 2));
            }
            if (item.type == ItemID.PlanteraBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkSilva>(), 2));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<MutantBulb>(), 2));
                itemLoot.Add(new CommonDrop(ModContent.ItemType<LashingBramblerod>(), 5, 1, 1, 4));
            }
            if (item.type == ItemID.GolemBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkTerra>(), 2));
            }
            if (item.type == ItemID.KingSlimeBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkRoyal>(), 2));
            }
            if (item.Is<CruiserBag>())
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkVoid>()));
            }

            if (item.type == ItemID.PlanteraBossBag)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ToyGuitar>(), 5));
            }
            if (item.type == ItemID.PlanteraBossBag)
            {
                itemLoot.Add(ItemDropRule.ByCondition(new IsDeathMode(), ModContent.ItemType<SilvasCrown>()));
            }
            if (item.type == ItemID.KingSlimeBossBag)
            {
                itemLoot.Add(new CommonDrop(ModContent.ItemType<SlimeYoyo>(), 10, 1, 1, 4));
            }
            if (item.type == ItemID.DeerclopsBossBag)
            {
                itemLoot.Add(new CommonDrop(ModContent.ItemType<Antler>(), 10, 1, 1, 4));
            }
            if (item.type == ItemID.IronCrate || item.type == ItemID.IronCrateHard)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<AuraCard>(), 10));
            }
            if (item.type == ItemID.OasisCrate || item.type == ItemID.OasisCrateHard)
            {
                itemLoot.Add(new CommonDrop(ModContent.ItemType<InspirationCard>(), 10, 1, 1, 3));
            }
            // —— 以下为脱离灾厄重挂（bookmark-rehang.md：原灾厄宝袋掉落改挂原版宝袋 / 自有 Boss 袋）——
            if (item.Is<ApsychosBag>())
            {
                // 原灾厄史莱姆之神袋 1/2
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkTaurus>(), 2));
            }
            if (item.Is<NihilityTwinBag>())
            {
                // 原灾厄神明使徒段位掉落集中重挂（书签 100%，武器/饰品 1/4，bookmark-rehang §四）
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HellBohea>(), 4));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BottleDarkMatter>(), 4));
                // 2026-08-31 平衡案:风暴之心改为合成(3星旋碎片+3夜明锭),袋装来源退役
            }
            // 2026-08-31 平衡案:仙萤流光改为虚空井合成,巡游者袋来源退役
            // —— 增补段（bookmark-rehang / misc-map §五 · 表外补充裁定的原无映射条目）——
            // 2026-08-31 平衡案:苏西腕带改为海龟25%掉落,石巨人袋来源退役
            if (item.type == ItemID.MoonLordBossBag)
            {
                // 原灾厄亵渎卫士袋宠物（18.5→ML 档）
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LavaPancake>(), 10));
            }
            if (item.type == ItemID.ObsidianLockbox)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<EnduranceCard>(), 3));
            }
            // 原挂在灾厄新手包（StarterBag）上的开局注入已定稿分流：IGetFromStarterBag 物品经 StartBagGItem
            // 注入自有礼包「熵之馈赠」，MagicStorage/ImproveGame 便利注入重挂 EntropyStarterBag.ModifyItemLoot（2026-08-27）
        }
        // 难度映射定稿（difficulty-map.md）：灾厄死亡模式 → 原版大师模式
        public class IsDeathMode : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => Main.masterMode;
            public bool CanShowItemDropInUI() => Main.masterMode;
            public string GetConditionDescription() => Language.GetTextValue("Mods.CalamityEntropy.DeathMode");
        }
    }
}
