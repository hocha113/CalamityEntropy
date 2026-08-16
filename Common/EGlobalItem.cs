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
using CalamityMod;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Fishing.SulphurCatches;
using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using CalamityMod.Items.TreasureBags;
using CalamityMod.Items.TreasureBags.MiscGrabBags;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.World;
using Microsoft.Xna.Framework.Graphics;
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
            Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/style3").Value;
            sb.Draw(tx, this.position + offset, null, b, this.velocity.ToRotation(), new Vector2(tx.Width, tx.Height) / 2, 0.3f, SpriteEffects.None, 0);
            sb.Draw(tx, this.position + offset, null, b, this.velocity.ToRotation(), new Vector2(tx.Width, tx.Height) / 2, 0.3f, SpriteEffects.None, 0);
        }
    }


    public class EGlobalItem : GlobalItem
    {
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
        public override bool CanBeConsumedAsAmmo(Item ammo, Item weapon, Player player)
        {
            if (LoreReworkSystem.Enabled<LoreSkeletron>())
            {
                if (ammo.stack >= LESkeletron.AmountLimit && Main.rand.NextFloat() < LESkeletron.Perc)
                {
                    return false;
                }
            }
            return true;
        }
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
            if (entity.type == ModContent.ItemType<StarblightSoot>())
            {
                //entity.ammo = 3728;
            }
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
        public List<int> RogueAccs = null;
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (RogueAccs == null)
            {
                int IT<T>() where T : ModItem
                {
                    return ModContent.ItemType<T>();
                }
                RogueAccs = new List<int>()
                {
                    IT<ScuttlersJewel>(),
                    IT<CoinofDeceit>(),
                    IT<RaidersTalisman>(),
                    IT<RottenDogtooth>(),
                    IT<InkBomb>(),
                    IT<SandCloak>(),
                    IT<SilencingSheath>(),
                    IT<BloodstainedGlove>(),
                    IT<FilthyGlove>(),
                    IT<MirageMirror>(),
                    IT<RogueEmblem>(),
                    IT<CorrosiveSpine>(),
                    IT<ElectriciansGlove>(),
                    IT<RuinMedallion>(),
                    IT<VampiricTalisman>(),
                    IT<GloveOfPrecision>(),
                    IT<GloveOfRecklessness>(),
                    IT<AbyssalMirror>(),
                    IT<EtherealExtorter>(),
                    IT<PlaguedFuelPack>(),
                    IT<DarkMatterSheath>(),
                    IT<BlunderBooster>(),
                    IT<SpectralVeil>(),
                    IT<VeneratedLocket>(),
                    IT<EclipseMirror>(),
                    IT<Nanotech>(),
                    IT<DragonScales>(),
                    IT<MineBox>(),
                    IT<GaleWristblades>(),
                    IT<ShadowPact>(),
                    IT<ShadowMantle>(),
                    IT<LurkersCharm>(),
                    IT<WorshipRelic>(),
                    IT<ThiefsPocketwatchOfEclipse>()
                };
            }
            if (RogueAccs.Contains(item.type))
                player.Entropy().EquipedAnyRogueAcc = true;
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
            /*if (item.type == ItemID.RodOfHarmony)
            {
                if (NPC.AnyNPCs(ModContent.NPCType<AbyssalWraith>()))
                {
                    SubworldSystem.Enter<VOIDSubworld>();
                }
            }*/
            if (player.channel || player.whoAmI != Main.myPlayer || item.pick > 0 || item.damage <= 0 || item.ammo != AmmoID.None || item.axe > 0 || !player.Entropy().TarnishCard)
            {
                return null;
            }
            var mp = player.Entropy();
            if (mp.BlackFlameCd <= 0 && player.whoAmI == Main.myPlayer)
            {
                mp.BlackFlameCd = item.useTime - 2;
                Projectile.NewProjectile(player.GetSource_FromAI(), player.Center, (Main.MouseWorld - player.Center).SafeNormalize(Vector2.One) * 14, ModContent.ProjectileType<BlackFire>(), player.GetWeaponDamage(item) / 8 + 1, 2, player.whoAmI);
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
            if (type == 3728)
            {
                return Mod.GetLocalization("AmmoStarblightSoot").Value;
            }
            if (type == 5809)
            {
                return Mod.GetLocalization("AmmoBloodrune").Value;
            }
            if (type == 6259 || type == 8584)
            {
                return CalamityUtils.GetItemName<WulfrumMetalScrap>().Value;
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
                    LocalizedText itemName = item.ModItem is ExquisiteCrown ? CalamityUtils.GetItemName<RottenFangs>() : CalamityUtils.GetItemName<ExquisiteCrown>();
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
            if (MaliciousCode.CALAMITY__OVERHAUL)
            {
                CWRWeakRef.CWRRef.CheckTooltips(item, tooltips);
            }
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

        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<CalamityMod.Items.Placeables.FurnitureAuric.AuricToilet>())
            {
                Item ai = new Item(ModContent.ItemType<AuricToilet>(), item.stack, 0);
                item.stack = 0;
                for (int i = 0; i < player.inventory.Count(); i++)
                {
                    if (player.inventory[i] == item)
                    {
                        player.inventory[i] = ai;
                        break;
                    }
                }

            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (ModContent.GetInstance<ServerConfig>().ClearStealthWhenChangeEquipSet)
            {
                var mp = player.Entropy();
                if (mp.StealthMaxLast != player.Calamity().rogueStealthMax)
                {
                    player.Calamity().rogueStealth = 0;
                    mp.RstStealth = true;
                    return false;
                }
            }
            if (player.GetModPlayer<AtbmPlayer>().Active && item.ModItem is not AzafureTBMTerminal)
                return false;
            if ((player.HasBuff<VoidVirus>() || (CalamityEntropy.EntropyMode && player.Entropy().HitTCounter > 0)) && item.healLife > 0)
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

            if (player.Entropy().shadowPact && item.DamageType.CountsAsClass<ThrowingDamageClass>())
            {
                if (player.Entropy().shadowStealth >= 1)
                {
                    CEUtils.PlaySound("shadowKnife");
                    player.Entropy().shadowStealth = 0; Projectile.NewProjectile(source, position, velocity.normalize() * 12, ModContent.ProjectileType<ShadowShoot>(), (int)player.GetTotalDamage<RogueDamageClass>().ApplyTo(ShadowPact.BaseDamage), 2, player.whoAmI);
                }
            }
            if (player.Entropy().worshipRelic && item.DamageType.CountsAsClass<ThrowingDamageClass>() && player.Calamity().StealthStrikeAvailable())
            {
                Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<SolarArrowSpawner>(), (int)player.GetTotalDamage<RogueDamageClass>().ApplyTo(WorshipRelic.ArrowDamage), 2, player.whoAmI);
                player.Entropy().ResetStealth = true;
            }
            if (player.Entropy().GaleWristbladeCharge >= 5)
            {
                player.Entropy().GaleWristbladeCharge = 0;
                Projectile.NewProjectile(source, position, velocity.normalize() * 8, ModContent.ProjectileType<WristTornado>(), (int)player.GetTotalDamage<RogueDamageClass>().ApplyTo(GaleWristblades.BaseDamage), 2, player.whoAmI);
            }
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
            if (player.Entropy().plagueEngine && item.DamageType.CountsAsClass<TrueMeleeDamageClass>())
            {
                PlagueInternalCombustionEngine.ApplyTrueMeleeEffect(player);
            }
            if (item.type == ModContent.ItemType<StellarStriker>())
            {
                IEntitySource source_ItemUse = player.GetSource_ItemUse(item);
                SoundEngine.PlaySound(in SoundID.Item88, player.Center);
                int myPlayer = Main.myPlayer;
                float shootSpeed = item.shootSpeed;
                Vector2 vector = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: true);
                for (int i = 0; i < player.Entropy().WeaponBoost; i++)
                {
                    vector = new Vector2(player.Center.X + (float)Main.rand.Next(201) * (0f - (float)player.direction) + ((float)Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
                    vector.X = (vector.X + player.Center.X) / 2f + (float)Main.rand.Next(-200, 201);
                    vector.Y -= 100 * i;
                    Vector2 velocity = CalamityUtils.CalculatePredictiveAimToTargetMaxUpdates(vector, target, shootSpeed, 6);
                    int num = Projectile.NewProjectile(source_ItemUse, vector, velocity, 645, damageDone, player.GetWeaponKnockback(item), myPlayer, 0f, Main.rand.Next(3));
                    if (num.WithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[num].DamageType = DamageClass.Melee;
                    }
                }
            }
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
                        Main.spriteBatch.Draw(CEUtils.getExtraTex("T1"), new Vector2(line.X, line.Y - 4) + new Vector2(p, p), Color.Red); Main.spriteBatch.Draw(CEUtils.getExtraTex("T1"), new Vector2(line.X, line.Y - 4), Color.Red);
                        Main.spriteBatch.Draw(CEUtils.getExtraTex("T1"), new Vector2(line.X, line.Y - 4) + new Vector2(-p, p), Color.Red);
                        Main.spriteBatch.Draw(CEUtils.getExtraTex("T1"), new Vector2(line.X, line.Y - 4) + new Vector2(p, -p), Color.Red);
                        Main.spriteBatch.Draw(CEUtils.getExtraTex("T1"), new Vector2(line.X, line.Y - 4) + new Vector2(-p, -p), Color.Red);

                        Main.spriteBatch.Draw(CEUtils.getExtraTex("T1"), new Vector2(line.X, line.Y - 4), Color.Black);


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
                        Texture2D glow = CEUtils.getExtraTex("Glow");
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
                        Texture2D glow = CEUtils.getExtraTex("Glow");
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
                    Texture2D glow = CEUtils.getExtraTex("Glow");
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
                    Texture2D glow = CEUtils.getExtraTex("Soulight");
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
                    Texture2D glow = CEUtils.getExtraTex("Glow");
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
                    Texture2D glow = CEUtils.getExtraTex("Glow");
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
                    Texture2D glow = CEUtils.getExtraTex("Glow");
                    Texture2D star = CEUtils.getExtraTex("StarTexture");
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
                    Texture2D glow = CEUtils.getExtraTex("Glow");
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
                    Texture2D glow = CEUtils.getExtraTex("Glow");
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
                itemLoot.Add(ModContent.ItemType<BookmarkSnowgrave>(), 5, 1, 1);
            if (item.type == ItemID.KingSlimeBossBag)
            {
                itemLoot.Add(ModContent.ItemType<ExquisiteCrown>(), 2);
            }
            if (item.type == ItemID.EaterOfWorldsBossBag)
            {
                itemLoot.Add(ModContent.ItemType<CursedTorch>(), 2);
            }
            if (item.type == ItemID.BrainOfCthulhuBossBag)
            {
                itemLoot.Add(ModContent.ItemType<CreeperWand>(), 2);
            }
            if (item.type == ItemID.EyeOfCthulhuBossBag)
            {
                itemLoot.Add(ModContent.ItemType<RottenFangs>(), 2);
            }
            if (item.type == ItemID.FishronBossBag)
            {
                itemLoot.Add(ItemDropRule.ByCondition(new IsDeathMode(), ModContent.ItemType<IlmeranAsylum>()));
            }
            if (item.type == ItemID.FloatingIslandFishingCrate)
            {
                itemLoot.Add(ModContent.ItemType<IndigoCard>(), 5);
            }
            if (item.type == ItemID.FloatingIslandFishingCrateHard)
            {
                itemLoot.Add(ModContent.ItemType<IndigoCard>(), 5);
            }
            if (item.type == ItemID.GolemBossBag)
            {
                itemLoot.Add(ModContent.ItemType<MourningCard>(), 1);
            }
            if (item.type == 3203 || item.type == 3204 || item.type == 3983 || item.type == 3982)
            {
                itemLoot.Add(ModContent.ItemType<ObscureCard>(), 5);
            }
            if (item.Is<CeaselessVoidBag>())
            {
                itemLoot.Add(ModContent.ItemType<BottleDarkMatter>(), 4);
            }
            if (item.Is<DevourerofGodsBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookmarkCosmic>(), 2);
            }
            if (item.Is<PlaguebringerGoliathBag>())
            {
                itemLoot.Add(ModContent.ItemType<PlagueInternalCombustionEngine>(), 4);
            }
            if (item.Is<CalamitasCloneBag>())
            {
                itemLoot.Add(ModContent.ItemType<FriendBox>(), 5);
            }
            if (item.type == ModContent.ItemType<HiveMindBag>())
            {
                itemLoot.Add(ModContent.ItemType<MindCorruptor>(), 3);
            }
            if (item.type == ModContent.ItemType<PerforatorBag>())
            {
                itemLoot.Add(ModContent.ItemType<SinewLash>(), 3);
            }
            if (item.type == ModContent.ItemType<HiveMindBag>() || item.type == ModContent.ItemType<PerforatorBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkAerialite>(), new Fraction(1, 2));
            }
            if (item.Is<LeviathanBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkAquarius>(), new Fraction(1, 2));
            }
            if (item.type == ItemID.QueenSlimeBossBag)
            {
                itemLoot.Add(ModContent.ItemType<Crystedge>(), new Fraction(1, 3));
            }
            if (item.type == ItemID.SkeletronBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkAries>(), new Fraction(1, 1));
                itemLoot.Add(ModContent.ItemType<OblivionSkull>(), new Fraction(1, 1));
            }
            if (item.Is<AstrumDeusBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkAstral>(), new Fraction(1, 2));
            }
            if (item.Is<YharonBag>())
            {
                bool l(DropAttemptInfo info)
                {
                    return info.player.name == "仙萤流光" || info.player.name == "五彩斑斓的黑";
                }
                itemLoot.AddIf(l, ModContent.ItemType<FlowingLight>(), 1);
                itemLoot.Add(ModContent.ItemType<BookMarkAuric>(), 4);
            }
            if (item.type == ItemID.QueenBeeBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkBee>(), new Fraction(1, 1));
            }
            if (item.Is<BrimstoneElementalBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkBrimstone>(), new Fraction(1, 2));
            }
            if (item.Is<CrabulonBag>())
            {
                itemLoot.Add(ModContent.ItemType<WisperCard>(), 2);
                itemLoot.Add(ModContent.ItemType<BookMarkCancer>(), new Fraction(2, 5));
                itemLoot.Add(ModContent.ItemType<BookmarkSpore>(), new Fraction(2, 5));
                itemLoot.Add(ModContent.ItemType<BlueFlatTopMushroom>(), new Fraction(2, 5));
            }
            if (item.Is<AquaticScourgeBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkCapricorn>(), new Fraction(1, 2));
            }
            if (item.type == ItemID.EaterOfWorldsBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkCorrupt>(), new Fraction(1, 2));
            }
            if (item.type == ItemID.BrainOfCthulhuBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkCrimson>(), new Fraction(1, 2));
            }
            if (item.type == ItemID.WallOfFleshBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkFlesh>(), new Fraction(1, 1));
                itemLoot.Add(ModContent.ItemType<HungryLantern>(), new Fraction(1, 3));
            }
            if (item.Is<NihilityTwinBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkGemini>(), new Fraction(1, 1));
            }
            if (ModLoader.TryGetMod("CalamityHunt", out Mod ch) && (item.type == ch.Find<ModItem>("TreasureTrunk").Type || item.type == ch.Find<ModItem>("TreasureBucket").Type))
            {
                itemLoot.Add(ModContent.ItemType<BookMarkGoozma>(), new Fraction(1, 1));
            }
            if (ModLoader.TryGetMod("CatalystMod", out Mod cl) && item.type == cl.Find<ModItem>("AstrageldonBag").Type)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkIntergelactic>(), new Fraction(1, 2));
            }
            if (item.Is<CryogenBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkIce>(), new Fraction(1, 2));
                itemLoot.Add(ModContent.ItemType<FrostboundCage>(), new Fraction(2, 5));
            }
            if (item.Is<DesertScourgeBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkLeo>(), new Fraction(1, 2));
                itemLoot.Add(ModContent.ItemType<AntlionShell>(), new Fraction(1, 3));
            }
            if (item.type == ItemID.FairyQueenBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkLibra>(), new Fraction(1, 1));
            }
            if (item.type == ItemID.MoonLordBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkLunar>(), new Fraction(3, 5));
                itemLoot.Add(ModContent.ItemType<MoonlightCore>(), new Fraction(2, 5));
            }
            if (item.type == ItemID.SkeletronPrimeBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkMechanical>(), new Fraction(1, 1));
            }
            if (item.type == ItemID.QueenSlimeBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkOfLight>(), new Fraction(1, 1));
            }
            if (item.Is<CalamitasCloneBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkOfNight>(), new Fraction(1, 1));
            }
            if (item.Is<CalamitasCoffer>())
            {
                itemLoot.Add(ModContent.ItemType<BookmarkPactOfDecay>(), new Fraction(1, 1));
            }
            if (item.Is<DraedonBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookmarkPactOfWar>(), new Fraction(1, 1));
            }
            if (item.type == ItemID.FishronBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkPisces>(), new Fraction(1, 1));
            }
            if (item.Is<ProvidenceBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkProfaned>(), new Fraction(3, 5));
                itemLoot.Add(ModContent.ItemType<SacredStone>(), new Fraction(3, 5));
            }
            if (item.type == ItemID.EyeOfCthulhuBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkSagittarius>(), new Fraction(1, 2));
                itemLoot.Add(ModContent.ItemType<BookMarkVirgo>(), new Fraction(1, 2));
            }
            if (item.Is<AstrumAureusBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkScorpio>(), new Fraction(1, 2));
            }
            if (item.type == ItemID.PlanteraBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkSilva>(), new Fraction(1, 2));
                itemLoot.Add(ModContent.ItemType<MutantBulb>(), new Fraction(1, 2));
                itemLoot.Add(ModContent.ItemType<LashingBramblerod>(), new Fraction(4, 5));
            }
            if (item.Is<SlimeGodBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkTaurus>(), new Fraction(1, 2));
            }
            if (item.type == ItemID.GolemBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkTerra>(), new Fraction(1, 2));
            }
            if (item.type == ItemID.KingSlimeBossBag)
            {
                itemLoot.Add(ModContent.ItemType<BookMarkRoyal>(), new Fraction(1, 2));
            }
            if (item.Is<CruiserBag>())
            {
                itemLoot.Add(ModContent.ItemType<BookMarkVoid>(), new Fraction(1, 1));
            }

            if (item.type == ItemID.PlanteraBossBag)
            {
                itemLoot.Add(ModContent.ItemType<ToyGuitar>(), new Fraction(1, 5));
            }
            if (item.type == ModContent.ItemType<StormWeaverBag>())
            {
                itemLoot.Add(ItemDropRule.ByCondition(new IsDeathMode(), ModContent.ItemType<HeartOfStorm>()));
            }
            if (item.type == ModContent.ItemType<AstrumDeusBag>())
            {
                itemLoot.Add(ItemDropRule.ByCondition(new IsDeathMode(), ModContent.ItemType<DeusCore>()));
            }
            if (item.type == ModContent.ItemType<BrimstoneElementalBag>())
            {
                itemLoot.Add(ModContent.ItemType<EvilFriend>(), new Fraction(4, 9));
            }
            if (item.type == ModContent.ItemType<YharonBag>())
            {
                itemLoot.Add(ModContent.ItemType<Vitalfeather>(), new Fraction(1, 4));
            }
            if (item.type == ModContent.ItemType<AstrumAureusBag>())
            {
                itemLoot.Add(ModContent.ItemType<NightProjection>(), new Fraction(4, 9));
            }
            if (item.type == ModContent.ItemType<PolterghastBag>())
            {
                itemLoot.Add(ModContent.ItemType<AnimaSola>(), new Fraction(1, 2));
            }
            if (item.type == ModContent.ItemType<AquaticScourgeBag>())
            {
                itemLoot.Add(ModContent.ItemType<AquaticFlute>(), new Fraction(1, 3));
            }
            if (item.type == ModContent.ItemType<DesertScourgeBag>())
            {
                itemLoot.Add(ModContent.ItemType<DustyWhistle>(), new Fraction(1, 4));
            }
            if (item.type == ModContent.ItemType<CalamitasCloneBag>())
            {
                //itemLoot.Add(ModContent.ItemType<FriendBox>(), new Fraction(1, 10));
            }
            if (item.type == ItemID.PlanteraBossBag)
            {
                itemLoot.Add(ItemDropRule.ByCondition(new IsDeathMode(), ModContent.ItemType<SilvasCrown>()));
            }
            if (item.type == ItemID.KingSlimeBossBag)
            {
                itemLoot.Add(ModContent.ItemType<SlimeYoyo>(), new Fraction(4, 10));
            }
            if (item.type == ItemID.DeerclopsBossBag)
            {
                itemLoot.Add(ModContent.ItemType<Antler>(), new Fraction(4, 10));
            }
            if (item.type == ModContent.ItemType<HydrothermalCrate>())
            {
                itemLoot.Add(ModContent.ItemType<EnduranceCard>(), new Fraction(1, 5));
            }
            if (item.type == ItemID.IronCrate || item.type == ItemID.IronCrateHard)
            {
                itemLoot.Add(ModContent.ItemType<AuraCard>(), new Fraction(1, 10));
            }
            if (item.type == ItemID.OasisCrate || item.type == ItemID.OasisCrateHard)
            {
                itemLoot.Add(ModContent.ItemType<InspirationCard>(), new Fraction(3, 10));
            }
            if (item.type == ModContent.ItemType<StarterBag>())
            {
                static bool getsDev(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    foreach (string str in Donators.Donors)
                        if (PGetPlayer.RemoveCharAndToLower(playerName).Contains(PGetPlayer.RemoveCharAndToLower(str))) return true;
                    return false;
                }
                ;
                itemLoot.AddIf(getsDev, ModContent.ItemType<TheocracyMark>());
                static bool getsDH(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    return playerName.ToLower().Contains("polaris");
                }
                ;
                static bool getsWyrm(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    return playerName.ToLower().Contains("polaris") || playerName.ToLower().Contains("妖龙") || playerName.ToLower().Contains("wyrm");
                }
                ;
                itemLoot.AddIf(getsDH, ModContent.ItemType<DustyStar>());
                itemLoot.AddIf(getsDH, ItemID.Ruby, 8);
                itemLoot.AddIf(getsDH, ModContent.ItemType<VoidCruiseDye>(), 3);
                itemLoot.AddIf(getsWyrm, ModContent.ItemType<AbyssLantern>());
                static bool getsAH(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    return playerName.ToLower().Contains("ahi") || playerName.ToLower().Contains("fr9");
                }
                ;
                itemLoot.AddIf(getsAH, ModContent.ItemType<GalaxyGrapeSoda>());

                static bool getsDD(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    return playerName.ToLower().Contains("dream") || playerName.ToLower().Contains("梦");
                }
                ;
                itemLoot.AddIf(getsDD, ModContent.ItemType<DreamCatcher>());


                static bool getsCHA(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    return playerName.ToLower().Contains("cha") || playerName.ToLower().Contains("lost");
                }
                ;
                itemLoot.AddIf(getsCHA, ModContent.ItemType<ToyKnife>());

                static bool getsAN(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    return playerName.ToLower().Contains("rat") || playerName.ToLower().Contains("ant");
                }
                ;
                itemLoot.AddIf(getsAN, ModContent.ItemType<Antler>());

                static bool getsSW(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    return playerName.ToLower().Contains("away") || playerName.ToLower().Contains("weaver");
                }
                ;
                itemLoot.AddIf(getsSW, ModContent.ItemType<CrimsonNight>());

                static bool getsMO(DropAttemptInfo info)
                {
                    string playerName = info.player.name;
                    return playerName.ToLower().Contains("mo");
                }
                ;
                itemLoot.AddIf(getsMO, ModContent.ItemType<MosHat>());

                itemLoot.AddIf((info) => (info.player.name.ToLower().Contains("ylg") || info.player.name.ToLower().Contains("烟玉")), ModContent.ItemType<YanyusHat>());

                itemLoot.AddIf((info) => (info.player.name.ToLower().Contains("sora")), ModContent.ItemType<MysteriousBook>());

                itemLoot.AddIf((info) => (info.player.name.ToLower().Contains("心斩狂歌")), ModContent.ItemType<LostChubbyBird>());

                itemLoot.AddIf((info) => (info.player.name.ToLower().Contains("nicholas")), ModContent.ItemType<PineappleDog>());

                itemLoot.AddIf((info) => (info.player.name.ToLower().Contains("lily") || info.player.name.Contains("莉莉")), ModContent.ItemType<LostHeirloom>());
                itemLoot.AddIf((info) => info.player.name.ToLower() == "tlipoca" || info.player.name.ToLower().Contains("kino"), ModContent.ItemType<TlipocasScythe>());
                bool cfg(DropAttemptInfo info)
                {
                    return ModContent.GetInstance<ServerConfig>().ExtraItemsInStarterBag;
                }
                if (ModLoader.TryGetMod("MagicStorage", out Mod magicStorage))
                {
                    ModItem i;
                    if (magicStorage.TryFind<ModItem>("CraftingAccess", out i))
                    {
                        itemLoot.AddIf(cfg, i.Type);
                    }
                    if (magicStorage.TryFind<ModItem>("StorageHeart", out i))
                    {
                        itemLoot.AddIf(cfg, i.Type, 1);
                    }
                    if (magicStorage.TryFind<ModItem>("StorageUnit", out i))
                    {
                        itemLoot.AddIf(cfg, i.Type, 1, 10, 10);
                    }
                }
                if (ModLoader.TryGetMod("ImproveGame", out Mod qot))
                {
                    ModItem i;
                    if (qot.TryFind<ModItem>("MagickWand", out i))
                    {
                        itemLoot.AddIf(cfg, i.Type);
                    }
                    if (qot.TryFind<ModItem>("SpaceWand", out i))
                    {
                        itemLoot.AddIf(cfg, i.Type);
                    }
                    if (qot.TryFind<ModItem>("CreateWand", out i))
                    {
                        itemLoot.AddIf(cfg, i.Type);
                    }
                    if (qot.TryFind<ModItem>("PotionBag", out i))
                    {
                        itemLoot.AddIf(cfg, i.Type);
                    }
                    if (qot.TryFind<ModItem>("BannerChest", out i))
                    {
                        itemLoot.AddIf(cfg, i.Type);
                    }
                }
                itemLoot.AddIf(cfg, 300, 1, 30, 30);
                itemLoot.AddIf(cfg, 2324, 1, 30, 30);
                itemLoot.AddIf(cfg, 148);
                itemLoot.AddIf(cfg, 3117);
                itemLoot.AddIf(cfg, ItemID.Sunflower);
            }
        }
        public class IsDeathMode : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => CalamityWorld.death;
            public bool CanShowItemDropInUI() => CalamityWorld.death;
            public string GetConditionDescription() => Language.GetTextValue("Mods.CalamityEntropy.DeathMode");
        }
    }
}
