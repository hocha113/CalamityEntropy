using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.LuminarisMoth;
using CalamityEntropy.Content.UI;
using CalamityEntropy.Core.ChatTags;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.ChatTags;
using CalamityMod.Cooldowns;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.NPCs;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Rarities;
using CalamityMod.Schematics;
using CalamityMod.UI;
using CalamityMod.UI.ResourceSets;
using CalamityMod.UI.Rippers;
using CalamityMod.World;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static InnoVault.GameSystem.ItemRebuildLoader;

namespace CalamityEntropy.Content.ILEditing
{
    public static class EModILEdit
    {
        [VaultLoaden("CalamityEntropy/Assets/UI/ExtraStealth")]
        public static Asset<Texture2D> extraStealthBar;
        [VaultLoaden("CalamityEntropy/Assets/UI/ExtraStealthFull")]
        public static Asset<Texture2D> extraStealthBarFull;
        [VaultLoaden("CalamityEntropy/Assets/UI/Shadowbar")]
        public static Asset<Texture2D> shadowBarTex;
        [VaultLoaden("CalamityEntropy/Assets/UI/ShadowBar1")]
        public static Asset<Texture2D> shadowProg;
        [VaultLoaden("CalamityEntropy/Assets/UI/ShadowBar2")]
        public static Asset<Texture2D> shadowProgFull;
        [VaultLoaden("CalamityEntropy/Assets/UI/SolarBar")]
        public static Asset<Texture2D> solarBarTex;
        [VaultLoaden("@CalamityMod/UI/MiscTextures/StealthMeter")]
        public static Asset<Texture2D> edgeTex;
        public static MethodBase updateStealthGenMethod;
        private delegate float UpdateStealthGenDelegate(Func<CalamityPlayer, float> orig, CalamityPlayer self);
        private delegate float CALEOCAI_Delegate(Func<NPC, Mod, bool> orig, NPC npc, Mod mod);
        private delegate void CalNPCModifyDelegate(CalamityGlobalNPC self, NPC npc, Projectile proj, ref NPC.HitModifiers modifuer);
        private delegate void CalNPCModifyHitByProj(CalNPCModifyDelegate orig, CalamityPlayer self, NPC npc, Projectile proj, ref NPC.HitModifiers modifier);
        private static void HookModifyHitByProj(CalNPCModifyDelegate orig, CalamityGlobalNPC self, NPC npc, Projectile proj, ref NPC.HitModifiers modifiers)
        {
            orig.Invoke(self, npc, proj, ref modifiers);
            npc.GetGlobalNPC<WhipDebuffNPC>().ModifyHitByProj(npc, proj, ref modifiers);
            if (npc.Entropy().nextHitCrit)
            {
                npc.Entropy().nextHitCrit = false;
                var fInfo = modifiers.GetType().GetField("_critOverride", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                object boxed = modifiers;
                fInfo.SetValue(boxed, (bool?)true);
                modifiers = (NPC.HitModifiers)boxed;
            }
        }
        public static void load()
        {
            updateStealthGenMethod = typeof(CalamityPlayer)
            .GetMethod("UpdateStealthGenStats",
                      System.Reflection.BindingFlags.NonPublic |
                      System.Reflection.BindingFlags.Instance,
                      null,
            Type.EmptyTypes,
            null);

            var _hook = EModHooks.Add(updateStealthGenMethod, UpdateStealthGenHook);


            var originalMethod = typeof(CalamityPlayer)
            .GetMethod("ConsumeStealthByAttacking",
                      System.Reflection.BindingFlags.Public |
                      System.Reflection.BindingFlags.Instance,
                      null,
            Type.EmptyTypes,
            null);
            _hook = EModHooks.Add(originalMethod, ConsumeStealthByAttackingHook);
            
            originalMethod = typeof(CalamityGlobalNPC)
             .GetMethod("ModifyHitByProjectile",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance,
                    null,
                    new Type[] { typeof(NPC), typeof(Projectile), typeof(NPC.HitModifiers).MakeByRefType() },
              null);
            _hook = EModHooks.Add(originalMethod, HookModifyHitByProj);
            /*originalMethod = typeof(EyeOfCthulhuAI)
            .GetMethod("BuffedEyeofCthulhuAI",
                      System.Reflection.BindingFlags.Public |
                      System.Reflection.BindingFlags.Static,
                      null,
            new Type[] { typeof(NPC), typeof(Mod) },
            null);
            _hook = EModHooks.Add(originalMethod, EOCAIHook);*/

            originalMethod = typeof(LoreItem)
            .GetMethod("CanUseItem",
                      System.Reflection.BindingFlags.Public |
                      System.Reflection.BindingFlags.Instance,
                      null,
            new Type[] { typeof(Player) },
            null);
            _hook = EModHooks.Add(originalMethod, canuseitem_hook);

            originalMethod = typeof(MurasamaSlash)
            .GetMethod("OnHitNPC",
                      System.Reflection.BindingFlags.Public |
                      System.Reflection.BindingFlags.Instance,
                      null,
            new Type[] { typeof(NPC), typeof(NPC.HitInfo), typeof(int) },
            null);
            _hook = EModHooks.Add(originalMethod, murasama_on_hit);

            //public static CooldownInstance AddCooldown(this Player p, string id, int duration, bool overwrite = true)
            originalMethod = typeof(CalamityUtils)
                .GetMethod("AddCooldown", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Player), typeof(string), typeof(int), typeof(bool) }, null);

            _hook = EModHooks.Add(originalMethod, addCdHook);

            originalMethod = typeof(StealthUI)
                .GetMethod("DrawStealthBar", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(SpriteBatch), typeof(CalamityPlayer), typeof(Vector2) }, null);

            _hook = EModHooks.Add(originalMethod, drawStealthBarHook);

            if (ModLoader.TryGetMod("AlchemistNPCLite", out var anpc))
            {
                ANPCSupport.ANPCShopAdd.LoadHook();
            }
            var Item_Name_Get_Method = typeof(Item).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            if (Item_Name_Get_Method != null)
            {
                EModHooks.Add(Item_Name_Get_Method, On_Name_Get_Hook);
            }

            var NPC_Get_Name = typeof(NPC).GetProperty("TypeName", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            if (NPC_Get_Name != null)
            {
                EModHooks.Add(NPC_Get_Name, On_NPC_Get_Hook);
            }

            var RipperUIDrawMethod = typeof(RipperUI).GetMethod("Draw", BindingFlags.Static | BindingFlags.Public);
            if (RipperUIDrawMethod != null)
            {
                EModHooks.Add(RipperUIDrawMethod, RipperUIDraw);
            }

            var postdrawResMethod = typeof(CalamityResourceOverlay).GetMethod("PostDrawResource", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(ResourceOverlayDrawContext) }, null);
            if (postdrawResMethod != null)
            {
                EModHooks.Add(postdrawResMethod, EResourceOverlay.PostDrawResourceHook);
            }

            var calPlrShoot = typeof(CalamityPlayer).GetMethod("Shoot", BindingFlags.Instance | BindingFlags.Public);
            EModHooks.Add(calPlrShoot, calPlrShootHook);

            var ApplyDRMethod = typeof(CalamityGlobalNPC).GetMethod("ApplyDR", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { typeof(NPC), typeof(NPC.HitModifiers).MakeByRefType() });
            EModHooks.Add(ApplyDRMethod, apply_dr_hook);

            var elmentalGloveUpdateAccessory = typeof(ElementalGauntlet).GetMethod("UpdateAccessory", BindingFlags.Instance | BindingFlags.Public);
            EModHooks.Add(elmentalGloveUpdateAccessory, elmentalGloveUpdateAccessoryHook);

            var updateLifeRegenMethod = typeof(CalamityGlobalNPC).GetMethod("UpdateLifeRegen", BindingFlags.Instance | BindingFlags.Public, new Type[] { typeof(NPC), typeof(int).MakeByRefType() });
            EModHooks.Add(updateLifeRegenMethod, updateLiferegenHook);

            /*var update_rogue_stealth_f = typeof(CalamityPlayer).GetMethod("UpdateRogueStealth", BindingFlags.Public | BindingFlags.Instance);
            EModHooks.Add(update_rogue_stealth_f, UpdateRogueStealthHook);*/

            var organic_m = typeof(CalamityUtils).GetMethod("Organic", BindingFlags.Static | BindingFlags.Public);
            EModHooks.Add(organic_m, organic_hook);

            TextEffectFix.LoadHooks();

            StoreForbiddenArchivePositionHook.LoadHook();

            if (MaliciousCode.CALAMITY__OVERHAUL)
            {
                CWRWeakRef.CWRRef.HookFSActive();
            }

            CalamityEntropy.Instance.Logger.Info("CalamityEntropy's Hook Loaded");
        }

        public static bool organic_hook(Func<NPC, bool> orig, NPC target)
        {
            if (target.type == ModContent.NPCType<SuperDummyNPC>())
                return true;
            return orig(target);
        }
        public static bool calPlrShootHook(Func<CalamityPlayer, Item, EntitySource_ItemUse_WithAmmo, Vector2, Vector2, int, int, float, bool> orig, CalamityPlayer self, Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockBack)
        {
            bool org = orig.Invoke(self, item, source, position, velocity, type, damage, knockBack);
            if (self.bladeArmEnchant && CELists.SpecialTaintedEnchantmentList.Contains(self.Player.HeldItem.type))
            {
                return true;
            }
            return org;
        }
        public static void elmentalGloveUpdateAccessoryHook(Action<ElementalGauntlet, Player, bool> orig, ElementalGauntlet self, Player player, bool hv)
        {
            bool msg = player.meleeScaleGlove;
            orig(self, player, hv);
            player.meleeScaleGlove = msg;
            player.Entropy().MeleeScale += 0.1f;
        }
        public delegate void UpdateLifeRegenDelegate(CalamityGlobalNPC self, NPC npc, ref int damage);
        public static void updateLiferegenHook(UpdateLifeRegenDelegate orig, CalamityGlobalNPC self, NPC npc, ref int damage)
        {
            orig(self, npc, ref damage);
            if (damage > 0)
            {
                float m = npc.Entropy().DebuffDamageMult();
                damage = int.Max(1, (int)(damage * m));
                npc.lifeRegen = (int)(Math.Round(npc.lifeRegen * m));
            }
        }

        public delegate void ApplyDRDelegate(CalamityGlobalNPC self, NPC npc, ref NPC.HitModifiers modifer);
        public delegate void DrawAdrenalineBarDelegate(SpriteBatch spriteBatch, CalamityPlayer modPlayer, Vector2 screenPos);
        public static void drawAdrBar_hook(DrawAdrenalineBarDelegate orig, SpriteBatch spriteBatch, CalamityPlayer modPlayer, Vector2 screenPos)
        {
            if (!modPlayer.Player.Entropy().NoAdrenaline)
            {
                orig(spriteBatch, modPlayer, screenPos);
            }
        }
        public static void apply_dr_hook(ApplyDRDelegate orig, CalamityGlobalNPC self, NPC npc, ref NPC.HitModifiers modifer)
        {
            orig(self, npc, ref modifer);
            NPC.HitModifiers dummy = new NPC.HitModifiers();
            dummy.FinalDamage = new StatModifier(0, 1);
            orig(self, npc, ref dummy);
            float NowDR = dummy.FinalDamage.Multiplicative;
            float mult = GetNPCDRMultiply(npc);

            //修改伤害减免
            if (mult != 1)
            {
                float DRShouldApply = 1 - (1 - NowDR) * mult;
                modifer.FinalDamage /= NowDR;
                modifer.FinalDamage *= DRShouldApply;
            }
        }
        public static float GetNPCDRMultiply(NPC npc)
        {
            return EGlobalNPC.DamageReduceMult(npc);
        }
        public delegate string On_GetNPCName_get_Delegate(NPC npc);
        public delegate bool AdrEnabled_get_Delegate(CalamityPlayer calPlayer);
        public static List<int> LostNPCsEntropy = new() { 454, 455, 456, 457, 458, 459, 521 };
        public static string On_NPC_Get_Hook(On_GetNPCName_get_Delegate orig, NPC npc)
        {
            string n = orig(npc);
            if (CalamityEntropy.EntropyMode)
            {
                if (npc.type == NPCID.CultistBoss || npc.type == NPCID.Golem || npc.type == NPCID.GolemFistLeft || npc.type == NPCID.GolemFistRight || npc.type == NPCID.GolemHead || npc.type == NPCID.GolemHeadFree || LostNPCsEntropy.Contains(npc.type))
                    n = (Language.ActiveCulture == GameCulture.FromCultureName(GameCulture.CultureName.Chinese) ? "失心" : "Lost") + " " + n;
            }
            if (npc.ModNPC != null && npc.ModNPC is Luminaris && Main.zenithWorld)
                n = CalamityEntropy.Instance.GetLocalization("Luminariswarm").Value;
            return n;
        }
        public static bool On_AdrenalineEnabled_Get_Hook(AdrEnabled_get_Delegate orig, CalamityPlayer calPlayer)
        {
            if (calPlayer.Player.Entropy().NoAdrenaline)
                return false;
            return orig(calPlayer);
        }
        public static void RipperUIDraw(Action<SpriteBatch, Player> orig, SpriteBatch batch, Player player)
        {
            bool dHeart = player.Calamity().draedonsHeart;
            bool revenge = CalamityWorld.revenge;
            bool shatteredCommunity = player.Calamity().shatteredCommunity;
            if (player.Entropy().NoAdrenaline)
            {
                if (CalamityWorld.revenge)
                    player.Calamity().shatteredCommunity = true;
                player.Calamity().draedonsHeart = false;
                CalamityWorld.revenge = false;
            }

            orig(batch, player);
            player.Calamity().shatteredCommunity = shatteredCommunity;
            player.Calamity().draedonsHeart = dHeart;
            CalamityWorld.revenge = revenge;
        }
        /*private static void UpdateRogueStealthHook(CalamityPlayer self)
        {
            Player Player = self.Player;
            if (self.temporaryStealthTimer > 0)
            {
                if (self.rogueStealthMax < self.temporaryStealthMax)
                    self.rogueStealthMax = self.temporaryStealthMax;
                self.wearingRogueArmor = true;
            }

            // If the player un-equips rogue armor, then reset the sound so it'll play again when they re-equip it
            if (!self.wearingRogueArmor)
            {
                self.rogueStealth = 0f;
                self.playRogueStealthSound = false;
                return;
            }

            // Sound plays upon hitting full stealth, not upon having stealth strike available (this can occur at lower than 100% stealth)
            if (self.playRogueStealthSound && self.rogueStealth >= self.rogueStealthMax && Player.whoAmI == Main.myPlayer)
            {
                self.playRogueStealthSound = false;
                SoundEngine.PlaySound(CalamityPlayer.RogueStealthSound, Player.Center);
            }

            // If the player isn't at full stealth, reset the sound so it'll play again when they hit full stealth.
            else if (self.rogueStealth < self.rogueStealthMax)
                self.playRogueStealthSound = true;

            // Calculate stealth generation and gain stealth accordingly
            // 1f is normal speed, anything higher is faster. Default stealth generation is 2 seconds while standing still.
            float usgs = (float)typeof(CalamityPlayer).GetMethod("UpdateStealthGenStats", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(self, new object[0]);
            float currentStealthGen = usgs;
            self.rogueStealth += self.rogueStealthMax * (currentStealthGen / 120f); // 120 frames = 2 seconds
            if (self.rogueStealth > self.rogueStealthMax)
                self.rogueStealth = self.rogueStealthMax;

            typeof(CalamityPlayer).GetMethod("ProvideStealthStatBonuses", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(self, new object[0]);

            // If the player is using an item that deals damage and is on their first frame of a use of that item,
            // consume stealth if a stealth strike wasn't triggered manually by item code.

            // This doesn't trigger stealth strike effects (ConsumeStealthStrike instead of StealthStrike)
            // so non-rogue weapons can't call lasers down from the sky and such.
            // Using any item which deals no damage or is a tool doesn't consume stealth.
            Item it = Player.HeldItem;
            bool hasDamage = it.damage > 0;
            bool hasHitboxes = !it.noMelee || it.shoot > ProjectileID.None;
            bool isPickaxe = it.pick > 0;
            bool isAxe = it.axe > 0;
            bool isHammer = it.hammer > 0;
            bool isPlaced = it.createTile != -1;
            bool isChannelable = it.channel;
            bool hasNonWeaponFunction = isPickaxe || isAxe || isHammer || isPlaced || isChannelable;
            bool playerUsingWeapon = hasDamage && hasHitboxes && !hasNonWeaponFunction;

            // The Gem Tech armor's rogue crystal ensures that stealth is not consumed by non-rogue items. Forbidden Circlet does this for summon weapons
            if ((it.IsAir || (!it.CountsAsClass<RogueDamageClass>()) && self.GemTechSet && self.GemTechState.IsRedGemActive) || (it.CountsAsClass<SummonDamageClass>() && self.forbiddenCirclet))
                playerUsingWeapon = false;

            // Molten Amputator consumes stealth in a special way
            if (it.type == ModContent.ItemType<MoltenAmputator>())
                playerUsingWeapon = false;

            // Shock Grenade consumes stealth in a special way
            if (it.type == ModContent.ItemType<DoomsdayDevice>())
                playerUsingWeapon = false;

            // Animation check depends on whether the item is "clockwork", like Clockwork Assault Rifle.
            // "Clockwork" weapons can chain-fire multiple stealth strikes (really only 2 max) until you run out of stealth.
            bool animationCheck = it.useAnimation == it.useTime
                ? Player.itemAnimation == Player.itemAnimationMax - 1 // Standard weapon (first frame of use animation)
                : Player.itemTime == (int)(it.useTime / Player.GetTotalAttackSpeed<RogueDamageClass>()); // Clockwork weapon (first frame of any individual use event)

            if (!self.stealthStrikeThisFrame && animationCheck && playerUsingWeapon)
            {
                bool canStealthStrike = self.StealthStrikeAvailable();

                // If you can stealth strike, you do.
                if (canStealthStrike)
                    self.ConsumeStealthByAttacking();
                // Otherwise you get a "partial stealth strike" (stealth damage is still added to the weapon) and return to normally attacking.
                else
                    self.rogueStealth = 0f;
            }
        }*/
        public static string On_Name_Get_Hook(On_GetItemName_get_Delegate orig, Item item)
        {
            if (Main.gameMenu || item.ModItem == null)
                return orig(item);
            string orgName = orig.Invoke(item);
            if (item.active)
            {
                string name = orgName;
                if (EGlobalItem.GetOverrideName(item, orgName, out string NameNew))
                {
                    name = NameNew;
                }
                return name;
            }
            return orgName;
        }
        public static FieldInfo mouseTextCacheField = null;
        public static void drawStealthBarHook(Action<SpriteBatch, CalamityPlayer, Vector2> orig, SpriteBatch spriteBatch, CalamityPlayer modPlayer, Vector2 screenPos)
        {
            var edgeTexField = typeof(StealthUI).GetField("edgeTexture", BindingFlags.Static | BindingFlags.NonPublic);
            var barTexField = typeof(StealthUI).GetField("barTexture", BindingFlags.Static | BindingFlags.NonPublic);
            var barFullTexField = typeof(StealthUI).GetField("fullBarTexture", BindingFlags.Static | BindingFlags.NonPublic);
            bool resetBarTex = false;
            Texture2D origTex = CEUtils.RequestTex("CalamityMod/UI/MiscTextures/StealthMeter");
            Texture2D origBar = null;
            Texture2D origBarFull = null;
            if (modPlayer.Player.Entropy().worshipRelic)
            {
                resetBarTex = true;
                origBar = (Texture2D)barTexField.GetValue(null);
                origBarFull = (Texture2D)barFullTexField.GetValue(null);
                edgeTexField.SetValue(null, solarBarTex.Value);
            }
            float num = modPlayer.rogueStealth;
            if (modPlayer.Player.Entropy().shadowPact)
            {
                resetBarTex = true;
                origBar = (Texture2D)barTexField.GetValue(null);
                origBarFull = (Texture2D)barFullTexField.GetValue(null);
                edgeTexField.SetValue(null, shadowBarTex.Value);
                barTexField.SetValue(null, shadowProg.Value);
                barFullTexField.SetValue(null, shadowProgFull.Value);
                modPlayer.rogueStealth = modPlayer.Player.Entropy().shadowStealth * modPlayer.rogueStealthMax;
            }
            orig(spriteBatch, modPlayer, screenPos);
            float uiScale = Main.UIScale;
            Rectangle mouseHitbox = new Rectangle((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 8, 8);
            Rectangle stealthBar = Utils.CenteredRectangle(screenPos, origTex.Size() * uiScale);

            if (modPlayer.Player.Entropy().ExtraStealth > 0)
            {
                if (stealthBar.Intersects(mouseHitbox))
                {
                    if (!CalamityClientConfig.Instance.MeterPosLock)
                        Main.LocalPlayer.mouseInterface = true;

                    if (modPlayer.rogueStealthMax > 0f && modPlayer.stealthUIAlpha >= 0.5f)
                    {
                        string stealthStr = (100f * modPlayer.rogueStealth).ToString("n2") + "+" + (100f * modPlayer.Player.Entropy().ExtraStealth).ToString("n2");
                        string maxStealthStr = (100f * modPlayer.rogueStealthMax).ToString("n2");
                        string textToDisplay = $"{CalamityUtils.GetTextValue("UI.Stealth")}: {stealthStr}/{maxStealthStr}\n";

                        if (!Main.keyState.IsKeyDown(Keys.LeftShift))
                        {
                            textToDisplay += CalamityUtils.GetTextValue("UI.StealthShiftText");
                        }
                        else
                        {
                            textToDisplay += CalamityUtils.GetTextValue("UI.StealthInfoText");
                        }

                        Main.instance.MouseText(textToDisplay, null, 0, 0, -1, -1, -1, -1, noOverride: true);
                        modPlayer.stealthUIAlpha = MathHelper.Lerp(modPlayer.stealthUIAlpha, 0.25f, 0.035f);
                    }
                }
            }
            if (modPlayer.Player.Entropy().shadowPact)
            {
                modPlayer.rogueStealth = num;
                if (stealthBar.Intersects(mouseHitbox))
                {
                    if (modPlayer.Player.Entropy().shadowStealth > 0)
                    {
                        string stealthStr = (100f * modPlayer.Player.Entropy().shadowStealth).ToString("n2");
                        string maxStealthStr = 100.ToString("n2");
                        string textToDisplay = $"{CalamityEntropy.Instance.GetLocalization("ShadowBar")}: {stealthStr}/{maxStealthStr}\n";

                        if (!Main.keyState.IsKeyDown(Keys.LeftShift))
                        {
                            textToDisplay += CalamityEntropy.Instance.GetLocalization("ShiftMoreInfo");
                        }
                        else
                        {
                            textToDisplay += CalamityEntropy.Instance.GetLocalization("ShadowBarInfo");
                        }

                        Main.instance.MouseText(textToDisplay, null, 0, 0, -1, -1, -1, -1, noOverride: true);
                    }
                }
            }
            if (modPlayer.Player.Entropy().worshipRelic)
            {
                if (stealthBar.Intersects(mouseHitbox))
                {
                    if (modPlayer.rogueStealthMax > 0f && modPlayer.stealthUIAlpha >= 0.5f)
                    {
                        string stealthStr = (100f * modPlayer.rogueStealth).ToString("n2");
                        string maxStealthStr = (100f * modPlayer.rogueStealthMax).ToString("n2");
                        string textToDisplay = $"{CalamityEntropy.Instance.GetLocalization("SolarBar")}: {stealthStr}/{maxStealthStr}\n";

                        if (!Main.keyState.IsKeyDown(Keys.LeftShift))
                        {
                            textToDisplay += CalamityEntropy.Instance.GetLocalization("ShiftMoreInfo");
                        }
                        else
                        {
                            textToDisplay += CalamityEntropy.Instance.GetLocalization("SolarBarInfo");
                        }

                        Main.instance.MouseText(textToDisplay, null, 0, 0, -1, -1, -1, -1, noOverride: true);
                    }
                }
            }
            if (resetBarTex)
            {
                edgeTexField.SetValue(null, origTex);
                barTexField.SetValue(null, origBar);
                barFullTexField.SetValue(null, origBarFull);
            }
            var emp = modPlayer.Player.Entropy();
            if (emp.ExtraStealth > 0)
            {
                float offset = (edgeTex.Value.Width - extraStealthBar.Value.Width) * 0.5f;
                float completionRatio = emp.ExtraStealth / modPlayer.rogueStealthMax;
                Rectangle barRectangle = new Rectangle(0, 0, (int)(extraStealthBar.Value.Width * completionRatio), extraStealthBar.Value.Width);
                bool full = emp.ExtraStealth > 0 && emp.ExtraStealth >= modPlayer.rogueStealthMax;
                spriteBatch.Draw(full ? extraStealthBarFull.Value : extraStealthBar.Value, screenPos + new Vector2(offset * uiScale, 0), barRectangle, Color.White * modPlayer.stealthUIAlpha, 0f, CEUtils.RequestTex("CalamityMod/UI/MiscTextures/StealthMeterStrikeIndicator").Size() * 0.5f, uiScale, SpriteEffects.None, 0);
            }
        }


        public static CooldownInstance addCdHook(Func<Player, string, int, bool, CooldownInstance> orig, Player player, string id, int duration, bool overwrite)
        {
            return orig.Invoke(player, id, (int)(duration * player.Entropy().CooldownTimeMult), overwrite);
        }
        private static bool EOCAIHook(Func<NPC, Mod, bool> orig, NPC npc, Mod mod)
        {
            if (CalamityEntropy.EntropyMode)
            {
                return EOCEntropyModeAI.BuffedEyeofCthulhuAI(npc, mod);
            }
            else
            {
                return orig(npc, mod);
            }
        }
        public static void ConsumeStealthByAttackingHook(Action<CalamityPlayer> orig, CalamityPlayer self)
        {
            if (self.Player.TryGetModPlayer<EModPlayer>(out var emp) && emp.WeaponsNoCostRogueStealth)
            {
                return;
            }
            orig(self);
        }
        private static float UpdateStealthGenHook(Func<CalamityPlayer, float> orig, CalamityPlayer self)
        {
            if (self.Player.TryGetModPlayer<EModPlayer>(out var mp) && (mp.NoNaturalStealthRegen || mp.StealthRegenDelay > 0))
            {
                return 0;
            }
            return (orig(self) + self.Player.Entropy().RogueStealthRegen) * self.Player.Entropy().RogueStealthRegenMult;
        }
        private static bool canuseitem_hook(Func<ModItem, Player, bool> orig, ModItem self, Player player)
        {
            if (LoreEffect.Enabled && LoreReworkSystem.loreEffects.ContainsKey(self.Type))
            {
                return true;
            }
            return orig(self, player);
        }
        private static void murasama_on_hit(Action<MurasamaSlash, NPC, NPC.HitInfo, int> orig, MurasamaSlash self, NPC target, NPC.HitInfo info, int damageDone)
        {
            if (EGlobalProjectile.VoidsamaTex(self.Projectile))
            {
                MuraOnHitSpec(self.Projectile, target, info, damageDone);
            }
            else
            {
                orig(self, target, info, damageDone);
            }
        }
        private static void MuraOnHitSpec(Projectile Projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            MurasamaSlash mp = (MurasamaSlash)Projectile.ModProjectile;
            Player Owner = Projectile.GetOwner();
            if (target.Organic())
                SoundEngine.PlaySound(Murasama.OrganicHit with { Pitch = (mp.Slash2 ? -0.1f : mp.Slash3 ? 0.1f : mp.Slash1 ? -0.15f : 0) }, Projectile.Center);
            else
                SoundEngine.PlaySound(Murasama.InorganicHit with { Pitch = (mp.Slash2 ? -0.1f : mp.Slash3 ? 0.1f : mp.Slash1 ? -0.15f : 0) }, Projectile.Center);

            for (int i = 0; i < 3; i++)
            {
                Color impactColor = mp.Slash3 ? Main.rand.NextBool(3) ? new Color(62, 35, 92) : Color.SkyBlue : Main.rand.NextBool(4) ? new Color(62, 35, 92) : Color.MediumPurple
                    ;
                float impactParticleScale = Main.rand.NextFloat(1f, 1.75f);

                if (mp.Slash3)
                {
                    SparkleParticle impactParticle2 = new SparkleParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, Color.SkyBlue, Color.SkyBlue, impactParticleScale * 1.2f, 8, 0, 4.5f);
                    GeneralParticleHandler.SpawnParticle(impactParticle2);
                }
                SparkleParticle impactParticle = new SparkleParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, impactColor, new Color(62, 35, 92), impactParticleScale, 8, 0, 2.5f);
                GeneralParticleHandler.SpawnParticle(impactParticle);
            }

            float sparkCount = MathHelper.Clamp(mp.Slash3 ? 18 - Projectile.numHits * 3 : 5 - Projectile.numHits * 2, 0, 18);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 sparkVelocity2 = Projectile.velocity.RotatedBy(mp.Slash2 ? -0.45f * Owner.direction : mp.Slash3 ? 0 : mp.Slash1 ? 0.45f * Owner.direction : 0).RotatedByRandom(0.35f) * Main.rand.NextFloat(0.5f, 1.8f);
                int sparkLifetime2 = Main.rand.Next(23, 35);
                float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                Color sparkColor2 = mp.Slash3 ? Main.rand.NextBool(3) ? new Color(62, 35, 92) : Color.MediumPurple : Main.rand.NextBool() ? new Color(62, 35, 92) : new Color(62, 35, 92);
                if (Main.rand.NextBool())
                {
                    AltSparkParticle spark = new AltSparkParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * (mp.Slash3 ? 1f : 0.65f), false, (int)(sparkLifetime2 * (mp.Slash3 ? 1.2f : 1f)), sparkScale2 * (mp.Slash3 ? 1.4f : 1f), sparkColor2);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
                else
                {
                    LineParticle spark = new LineParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * (Projectile.frame == 7 ? 1f : 0.65f), false, (int)(sparkLifetime2 * (Projectile.frame == 7 ? 1.2f : 1f)), sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f), Main.rand.NextBool() ? new Color(92, 35, 62) : new Color(62, 35, 92));
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }
            float dustCount = MathHelper.Clamp(mp.Slash3 ? 25 - Projectile.numHits * 3 : 12 - Projectile.numHits * 2, 0, 25);
            for (int i = 0; i <= dustCount; i++)
            {
                int dustID = Main.rand.NextBool(3) ? 182 : Main.rand.NextBool() ? mp.Slash3 ? 309 : 296 : 90;
                Dust dust2 = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), dustID, Projectile.velocity.RotatedBy(mp.Slash2 ? -0.45f * Owner.direction : mp.Slash3 ? 0 : mp.Slash1 ? 0.45f * Owner.direction : 0).RotatedByRandom(0.55f) * Main.rand.NextFloat(0.3f, 1.1f));
                dust2.scale = Main.rand.NextFloat(0.9f, 2.4f);
                dust2.noGravity = true;
            }
        }
    }
    public static class StoreForbiddenArchivePositionHook
    {
        public static void LoadHook()
        {
            var method = typeof(DungeonArchive).GetMethod("PlaceArchive", BindingFlags.Static | BindingFlags.Public);
            MonoModHooks.Modify(method, sfahook);
        }

        private static void sfahook(ILContext il)
        {
            ILCursor cursor = new(il);

            int xLocalIndex = 0;
            int yLocalIndex = 0;
            ConstructorInfo pointConstructor = typeof(Point).GetConstructor([typeof(int), typeof(int)]);
            MethodInfo placementMethod = typeof(SchematicManager).GetMethods().First(m => m.Name == "PlaceSchematic");

            // Find the first instance of the schematic placement call. There are three, but they all take the same information so it doesn't matter which one is used as a reference.
            cursor.GotoNext(i => i.MatchLdstr(SchematicKeys.BlueArchiveKey));

            // Find the part of the method call where the placement Point type is made, and read off the IL indices for the X and Y coordinates with intent to store them elsewhere.
            cursor.GotoNext(i => i.MatchNewobj(pointConstructor));
            cursor.GotoPrev(i => i.MatchLdloc(out yLocalIndex));
            cursor.GotoPrev(i => i.MatchLdloc(out xLocalIndex));

            // Go back to the beginning of the method and store the placement position so that it isn't immediately discarded after world generation- Ceaseless Void's natural spawning needs it.
            // This needs to be done at each of hte three schematic placement variants since sometimes post-compilation optimizations can scatter about return instructions.
            cursor.Index = 0;
            for (int i = 0; i < 3; i++)
            {
                cursor.GotoNext(i => i.MatchLdftn(out _));
                cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(out _));
                cursor.Emit(OpCodes.Ldloc, xLocalIndex);
                cursor.Emit(OpCodes.Ldloc, yLocalIndex);
                cursor.EmitDelegate((int x, int y) =>
                {
                    EDownedBosses.ForbiddenArchiveCenter = new(x, y);
                });
            }
        }
    }
    public static class EModHooks
    {
        private static ConcurrentDictionary<(MethodBase, Delegate), Hook> _hooks = new ConcurrentDictionary<(MethodBase, Delegate), Hook>();
        public static ConcurrentDictionary<(MethodBase, Delegate), Hook> Hooks => _hooks;
        public static Hook Add(MethodBase method, Delegate hookDelegate)
        {
            if (method == null)
            {
                CalamityEntropy.Instance.Logger.Warn($"CalamityEntropy: Error when add hook to method: The MethodBase passed in is Null");
                return null;
                //throw new ArgumentException("The MethodBase passed in is Null");
            }
            if (hookDelegate == null)
            {
                CalamityEntropy.Instance.Logger.Warn($"CalamityEntropy: Error when add hook to {method.Name}: The HookDelegate passed in is Null");
                return null;
                //throw new ArgumentException("The HookDelegate passed in is Null");
            }

            Hook hook = new Hook(method, hookDelegate);

            if (!hook.IsApplied)
            {
                hook.Apply();
            }
            _hooks.TryAdd((method, hookDelegate), hook);
            return hook;
        }

        public static bool CheckHookStatus()
        {
            int hookDownNum = 0;
            foreach (var hook in _hooks.Values)
            {
                if (!hook.IsApplied)
                {
                    hookDownNum++;
                }
            }
            if (hookDownNum > 0)
            {
                return false;
            }
            return true;
        }

        public static void UnLoadData()
        {
            foreach (var hook in _hooks.Values)
            {
                if (hook == null)
                {
                    continue;
                }
                if (hook.IsApplied)
                {
                    hook.Undo();
                }
                hook.Dispose();
            }
            _hooks.Clear();
        }

    }
}