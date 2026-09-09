global using Microsoft.Xna.Framework;
using CalamityEntropy.Common;
using CalamityEntropy.Content.ArmorPrefixes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.ILEditing;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.EvilCards;
using CalamityEntropy.Content.Items.Accessories.Oath;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Armor.AzafureT3;
using CalamityEntropy.Content.Items.Atbm;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.MusicBoxes;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs;
using CalamityEntropy.Content.NPCs.Acropolis;
using CalamityEntropy.Content.NPCs.Apsychos;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.NPCs.LuminarisMoth;
using CalamityEntropy.Content.NPCs.NihilityTwin;
using CalamityEntropy.Content.NPCs.Prophet;
using CalamityEntropy.Content.NPCs.VoidInvasion;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Prophet;
using CalamityEntropy.Content.Projectiles.SamsaraCasket;
using CalamityEntropy.Content.Projectiles.TwistedTwin;
using CalamityEntropy.Content.Skies;
using CalamityEntropy.Content.UI;
using CalamityEntropy.Content.UI.EntropyBookUI;
using CalamityEntropy.Content.UI.Poops;
using CalamityEntropy.Core.Dash;
using CalamityEntropy.Utilities;
using InnoVault;
using InnoVault.Actors;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.UI;
using static CalamityEntropy.Common.EGlobalNPC;
namespace CalamityEntropy
{
    public partial class CalamityEntropy : Mod
    {
        //玩家护壳贴图在加载期就位,只在客户端绘制钩子里读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/shell")]
        internal static Asset<Texture2D> ShellTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/MariviniumShield")]
        internal static Asset<Texture2D> MariviniumShieldTex;
        internal static List<ICELoader> ILoaders { get; private set; }
        public static CESpawnConditionBestiaryInfoElement theVoid_SCBIE;
        public static ref bool EntropyMode => ref EDownedBosses.EntropyMode;
        public static bool AprilFool = false;
        public static List<int> calDebuffIconDisplayList = new List<int>();
        public static CalamityEntropy Instance;
        public static int noMusTime = 0;
        public float screenShakeAmp = 0;
        public float cvcount = 0;
        public static SoundEffect otLoop;
        public static bool ets = true;
        public static Texture2D pixel;
        public ArmorForgingStationUI armorForgingStationUI;
        public UserInterface userInterface;
        public static DynamicSpriteFont efont1;
        public static DynamicSpriteFont efont2;
        public static DynamicSpriteFont efont3;
        public static float cutScreenVel = 0;
        public static float cutScreen = 0;
        public static float cutScreenRot = 0;
        public static Vector2 cutScreenCenter = Vector2.Zero;
        public bool ChristmasEvent = false;
        public static float FlashEffectStrength = 0;
        public static Dictionary<int, Projectile> Proj_ID_To_Instance { get; set; } = null;
        public Rope Rope { get; set; }
        public static SoundEffect ealaserSound = null;
        public static SoundEffect ealaserSound2 = null;
        public static SoundEffect ofCharge = null;
        public string EntropyWikiURL;
        public override void Load()
        {
            VanityDisplaySys.VanityItems = new();
            CEUtils.TexCache = new Dictionary<string, Texture2D>();
            theVoid_SCBIE = new CESpawnConditionBestiaryInfoElement(this.GetLocalizationKey("TheVoid"), 0, "CalamityEntropy/Assets/VoidBack");
            BookMarkLoader.CustomBMEffectsByName = new Dictionary<string, BookMarkLoader.BookmarkEffectFunctionGroups>();
            BookMarkLoader.CustomBMByID = new Dictionary<int, BookMarkLoader.BookMarkTag>();
            Instance = this;
            Proj_ID_To_Instance = new Dictionary<int, Projectile>();
            DateTime today = DateTime.Now;
            AprilFool = today.Month == 4 && today.Day == 1;
            CEUtils.SoundStyles = new Dictionary<string, Terraria.Audio.SoundStyle>();

            ShadowCrystalDeltarune.Load();

            ILoaders = new List<ICELoader>();
            string name = typeof(ICELoader).Name;
            Type[] anyModCodeType = VaultUtils.GetAnyModCodeType();
            foreach (Type type in anyModCodeType)
            {
                if (type.IsClass && !type.IsAbstract && type.GetInterface(name) != null && RuntimeHelpers.GetUninitializedObject(type) is ICELoader item)
                {
                    ILoaders.Add(item);
                }
            }
            foreach (ICELoader setup in ILoaders)
            {
                setup.LoadData();
                setup.DompLoadText();
            }
            LoopSoundManager.init();

            //Wiki this support
            if (!Main.dedServ && ModLoader.TryGetMod("Wikithis", out var wikithis))
            {
                wikithis.Call(0, this, "http://calentropy.miraheze.org/wiki/{}", GameCulture.CultureName.Chinese);
                wikithis.Call("AddWikiTexture", this, ModContent.Request<Texture2D>("CalamityEntropy/Assets/UI/icon_s"));
                wikithis.Call(3, this, ModContent.Request<Texture2D>("CalamityEntropy/Assets/UI/icon_s"));
            }

            efont1 = ModContent.Request<DynamicSpriteFont>("CalamityEntropy/Assets/Fonts/EFont", AssetRequestMode.ImmediateLoad).Value;
            efont2 = ModContent.Request<DynamicSpriteFont>("CalamityEntropy/Assets/Fonts/VCRFont", AssetRequestMode.ImmediateLoad).Value;
            efont3 = ModContent.Request<DynamicSpriteFont>("CalamityEntropy/Assets/Fonts/MaruMonica", AssetRequestMode.ImmediateLoad).Value;
            if (!Main.dedServ)
            {
                EBookUI.shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/Outline", AssetRequestMode.ImmediateLoad).Value;
            }
            armorForgingStationUI = new ArmorForgingStationUI();
            armorForgingStationUI.Activate();
            userInterface = new UserInterface();
            userInterface.SetState(armorForgingStationUI);
            ets = true;
            pixel = CEUtils.getExtraTex("white");

            CruiserHead.loadHead();

            EntropySkies.setUpSkies();

            On_MapHeadRenderer.DrawPlayerHead += drawPlayerHeadHook;
            On_Lighting.AddLight_int_int_int_float += al_iiif;
            On_Lighting.AddLight_int_int_float_float_float += al_iifff;
            On_Lighting.AddLight_Vector2_float_float_float += al_vfff;
            On_Lighting.AddLight_Vector2_Vector3 += al_vv;
            On_Lighting.AddLight_Vector2_int += al_torch;
            On_Player.AddBuff += add_buff;
            On_NPC.TargetClosest += targetClost;
            On_NPC.TargetClosestUpgraded += targetClostUpgraded;
            On_NPC.FindFrame += findFrame;
            On_NPC.VanillaAI += vAi;
            On_NPC.UpdateNPC += npcupdate;
            On_NPC.StrikeNPC_HitInfo_bool_bool += StrikeNpc;
            On_Player.getRect += modifyRect;
            On_Main.DrawInfernoRings += drawIr;
            On_Main.DrawProjectiles += DrawBehindPlayer;
            On_Main.DrawMenu += drawmenu;
            On_Player.Heal += player_heal;
            On_Main.DrawTiles += drawtile;
            On_Projectile.FillWhipControlPoints += fill_whip_ctrl_points_hook;
            On_Projectile.GetWhipSettings += get_whip_settings_hook;
            On_Player.PickAmmo_Item_refInt32_refSingle_refBoolean_refInt32_refSingle_refInt32_bool += pickammoHook;
            On_LegacyPlayerRenderer.DrawPlayer += render_player;
            On_Player.GetTotalCritChance += gettotalcrit;
            //On_Player.ApplyDamageToNPC += applydamagetonpc;
            On_Main.DrawCursor += draw_cursor_hook;
            On_Main.DrawThickCursor += draw_thick_cursor_hook;
            On_Player.UpdateItemDye += update_item_dye;
            On_Player.Hurt_HurtInfo_bool += on_player_hurt;
            On_Player.Update_NPCCollision += update_npc_collision;
            On_Player.WaterCollision += waterCollisionHook;
            On_Player.ApplyEquipFunctional += apply_equip_func;
            On_Player.ApplyMeleeScale += apply_melee_scale;
            
            EModSys.timer = 0;
            EModILEdit.load();


        }

        private void apply_melee_scale(On_Player.orig_ApplyMeleeScale orig, Player self, ref float scale)
        {
            orig(self, ref scale);
            scale += (self.Entropy().MeleeScale - 1);
        }

        private void apply_equip_func(On_Player.orig_ApplyEquipFunctional orig, Player self, Item currentItem, bool hideVisual)
        {
            bool gloveScale = self.meleeScaleGlove;
            orig(self, currentItem, hideVisual);
            if (currentItem.type == ItemID.BerserkerGlove)
            {
                self.meleeScaleGlove = gloveScale;
                self.Entropy().MeleeScale += 0.1f;
            }
            if (currentItem.type == ItemID.PowerGlove)
            {
                self.meleeScaleGlove = gloveScale;
                self.Entropy().MeleeScale += 0.1f;
            }
            if (currentItem.type == ItemID.MechanicalGlove)
            {
                self.meleeScaleGlove = gloveScale;
                self.Entropy().MeleeScale += 0.1f;
            }
            if (currentItem.type == ItemID.FireGauntlet)
            {
                self.meleeScaleGlove = gloveScale;
                self.Entropy().MeleeScale += 0.1f;
            }
        }

        private void waterCollisionHook(On_Player.orig_WaterCollision orig, Player self, bool fallThrough, bool ignorePlats)
        {
            if (self.Entropy().MariviniumSet)
            {
                int num = ((!self.onTrack) ? self.height : (self.height - 20));
                Vector2 vector = self.velocity;
                self.velocity = Collision.TileCollision(self.position, self.velocity + new Vector2(0, self.controlDown ? (self.controlJump ? -self.velocity.Y : 5) : 0), self.width, num, fallThrough, ignorePlats, (int)self.gravDir);
                Vector2 vector2 = self.velocity;
                self.position += vector2;
                if (self.wingTime < self.wingTimeMax)
                    self.wingTime = self.wingTimeMax;
            }
            else if (self.Entropy().accAzureAbyss)
            {
                int num = ((!self.onTrack) ? self.height : (self.height - 20));
                Vector2 vector = self.velocity;
                self.velocity = Collision.TileCollision(self.position, self.velocity, self.width, num, fallThrough, ignorePlats, (int)self.gravDir);
                Vector2 vector2 = self.velocity * 0.7f;
                if (self.velocity.X != vector.X)
                    vector2.X = self.velocity.X;

                if (self.velocity.Y != vector.Y)
                    vector2.Y = self.velocity.Y;

                self.position += vector2;
            }
            else
            {
                orig(self, fallThrough, ignorePlats);
            }
        }

        private float gettotalcrit(On_Player.orig_GetTotalCritChance orig, Player self, DamageClass damageClass)
        {
            float rt = orig(self, damageClass);
            if (EntropyMode)
                rt = float.Min(50, rt);
            return rt;
        }

        private void update_npc_collision(On_Player.orig_Update_NPCCollision orig, Player self)
        {
            self.Entropy().ApplyScale();
            // 原版冲刺起手帧只在 DashMovement 之后、接触伤害之前可见:暗影披风对原版冲刺的强化在这里接入
            self.GetModPlayer<CEDashPlayer>().PostMovementVanillaCheck();
            orig(self);
            self.Entropy().ResetScale();
        }

        private void on_player_hurt(On_Player.orig_Hurt_HurtInfo_bool orig, Player self, Player.HurtInfo info, bool quiet)
        {
            if (EntropyMode)
                info.Damage = (int)(info.Damage * 1.25f);
            float num = ModContent.GetInstance<ServerConfig>().LeastDamageSufferedBasedOnMaxHealth;
            if (EntropyMode && num < 22)
                num = 22;
            int leastDmg = (int)((num * 0.01f) * self.statLifeMax2);
            // 2026-08-31 平衡案:神谕卡组单次受伤上限改为最大生命66.7%,带5秒内置冷却
            if (self.Entropy().oracleDeck)
            {
                int cap = (int)(self.statLifeMax2 * 0.667f);
                if (info.Damage > cap && CECooldowns.CheckBMProc("OracleDeckDamageCap", 300))
                {
                    info.Damage = cap;
                }
            }
            if (info.Damage < leastDmg)
                info.Damage = leastDmg;

            if (self.Entropy().deusCore && info.Damage > 20)
            {
                self.Entropy().deusCoreBloodOut += info.Damage - 20;
                info.Damage = 20;
            }
            if (self.Entropy().NihTwinArmorConnetPlayer != -1)
            {
                if (self.statLife - info.Damage <= 0 && self.Entropy().NihTwinArmorConnetPlayer.ToPlayer().statLife > info.Damage)
                {
                    if (CECooldowns.CheckCD("NihDamageDeathTrans", 12 * 60))
                    {
                        CombatText.NewText(self.getRect(), Color.LightBlue, $"{info.Damage}->");
                        info.Cancelled = true;
                        info.Damage = 1;
                        self.Entropy().immune = 120;
                        self.Entropy().NihTwinArmorConnetPlayer.ToPlayer().statLife -= info.Damage;
                        self.Entropy().SyncLife(self.Entropy().NihTwinArmorConnetPlayer.ToPlayer());
                        CEUtils.PlaySound("charm");
                    }
                }
            }
            orig(self, info, quiet);
        }
        public static int cbptype = -1;
        private void render_player(On_LegacyPlayerRenderer.orig_DrawPlayer orig, LegacyPlayerRenderer self, Camera camera, Player drawPlayer, Vector2 position, float rotation, Vector2 rotationOrigin, float shadow, float scale)
        {
            bool hide = false;
            if (!Main.gameMenu)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient && drawPlayer.GetModPlayer<AtbmPlayer>().Active && !drawPlayer.GetModPlayer<AtbmPlayer>().CanDraw)
                    hide = true;
                if (drawPlayer.Entropy().DontDrawTime > 0)
                    hide = true;
                if (drawPlayer.TryGetModPlayer<AcropolisArmorPlayer>(out var mp))
                {
                    if (!mp.PlayerVisual)
                        hide = true;
                }
                scale *= drawPlayer.Entropy().Scale;
            }


            if (!hide)
            {
                if (cbptype == -1)
                    cbptype = ModContent.ProjectileType<CBPSmash>();
                if (drawPlayer.ownedProjectileCounts[cbptype] > 0)
                {
                    foreach (var pj in Main.ActiveProjectiles)
                    {
                        if (pj.owner == drawPlayer.whoAmI && pj.type == cbptype)
                        {
                            position = pj.Center;
                            break;
                        }
                    }
                }
                orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, scale);
            }
        }

        public static int tmtype = -1;
        public static int retype = -1;
        public static int aetype = -1;
        public static int obtype = -1;
        private void update_item_dye(On_Player.orig_UpdateItemDye orig, Player self, bool isNotInVanitySlot, bool isSetToHidden, Item armorItem, Item dyeItem)
        {
            if (tmtype < 1)
                tmtype = ModContent.ItemType<TheocracyMark>();
            if (retype < 1)
                retype = ModContent.ItemType<RustyDetectionEquipment>();
            if (aetype < 1)
                aetype = ModContent.ItemType<AzafureDetectionEquipment>();
            if (obtype < 1)
                obtype = ModContent.ItemType<OathBanner>();
            if (!armorItem.IsAir)
            {
                armorItem.Entropy().DyeType = dyeItem.type;
            }
            if (!dyeItem.IsAir && dyeItem.ModItem != null && dyeItem.ModItem is RoaringDye)
            {
                self.Entropy().roaringDye = true;
            }
            if (!armorItem.IsAir && armorItem.type == tmtype)
            {
                self.GetModPlayer<VanityModPlayer>().TheocrazyDye = dyeItem.IsAir ? 0 : dyeItem.dye;
                self.GetModPlayer<VanityModPlayer>().TheocrazyDyeItemID = dyeItem.type;
            }
            if (!armorItem.IsAir && (armorItem.type == retype || armorItem.type == aetype))
            {
                self.Entropy().JetpackDye = dyeItem.dye;
            }
            if (!armorItem.IsAir && armorItem.type == obtype)
            {
                self.Entropy().oathBannerDye = dyeItem.IsAir ? 0 : dyeItem.dye;
            }
            orig(self, isNotInVanitySlot, isSetToHidden, armorItem, dyeItem);
        }

        private void pickammoHook(On_Player.orig_PickAmmo_Item_refInt32_refSingle_refBoolean_refInt32_refSingle_refInt32_bool orig, Player player, Item item, ref int projToShoot, ref float speed, ref bool canShoot, ref int totalDamage, ref float KnockBack, out int usedAmmoItemId, bool dontConsume)
        {
            orig(player, item, ref projToShoot, ref speed, ref canShoot, ref totalDamage, ref KnockBack, out usedAmmoItemId, dontConsume);
            if (projToShoot >= 0 && projToShoot != ProjectileID.None)
            {
                if (item.useAmmo != AmmoID.None && player.Entropy().fruitCake)
                {
                    if (Fruitcake.ammoList.ContainsKey(item.useAmmo))
                    {
                        projToShoot = ContentSamples.ItemsByType[Fruitcake.ammoList[item.useAmmo].random<int>()].shoot;
                    }
                }
            }
        }

        private void drawPlayerHeadHook(On_MapHeadRenderer.orig_DrawPlayerHead orig, MapHeadRenderer self, Camera camera, Player drawPlayer, Vector2 position, float alpha, float scale, Color borderColor)
        {
            orig(self, camera, drawPlayer, position, alpha, scale, borderColor);
        }

        private void draw_cursor_hook(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart)
        {
            if (!EModSys.mi)
            {
                orig(bonus, smart);
            }
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI) => CENetWork.Handle(reader, whoAmI);

        private void get_whip_settings_hook(On_Projectile.orig_GetWhipSettings orig, Projectile proj, out float timeToFlyOut, out int segments, out float rangeMultiplier)
        {
            orig(proj, out timeToFlyOut, out segments, out rangeMultiplier);
            if (proj.ModProjectile != null && proj.ModProjectile is BaseWhip bw)
            {
                bw.ModifyWhipSettings(ref timeToFlyOut, ref segments, ref rangeMultiplier);
            }
        }

        private void fill_whip_ctrl_points_hook(On_Projectile.orig_FillWhipControlPoints orig, Projectile proj, List<Vector2> controlPoints)
        {
            orig(proj, controlPoints);
            if (proj.ModProjectile != null && proj.ModProjectile is BaseWhip bw)
            {
                bw.ModifyControlPoints(controlPoints);
            }
        }

        public static Projectile GetAProjectileInstance(int type)
        {
            if (!Proj_ID_To_Instance.ContainsKey(type))
            {
                Projectile p = new Projectile();
                p.SetDefaults(type);
                Proj_ID_To_Instance[type] = p;
            }
            return Proj_ID_To_Instance[type];
        }

        public override void Unload()
        {
            CommonEffects.Unload();
            CELists.Unload();
            Typer.activeTypers = null;
            ScreenShaker.Unload();
            VanityDisplaySys.VanityItems = null;
            CEUtils.SoundStyles = null;
            theVoid_SCBIE = null;
            StartBagGItem.items = null;
            ShadowCrystalDeltarune.Reset();
            EBookUI.shader = null;
            if (ILoaders != null)
            {
                foreach (ICELoader setup in ILoaders)
                {
                    setup.UnLoadData();
                    setup.DompUnLoadText();
                }
            }
            ILoaders = null;
            CERecipeGroups.unload();
            CEUtils.TexCache = null;
            BookMarkLoader.CustomBMEffectsByName = null;
            BookMarkLoader.CustomBMByID = null;

            Proj_ID_To_Instance = null;
            EModHooks.UnLoadData();
            LoopSoundManager.unload();
            ealaserSound = null;
            ealaserSound2 = null;
            ArmorPrefix.instances = null;
            Poop.instances = null;
            WallpaperHelper.wallpaper = null;
            efont1 = null;
            efont2 = null;
            efont3 = null;
            Instance = null;
            pixel = null;

            On_MapHeadRenderer.DrawPlayerHead -= drawPlayerHeadHook;
            On_Lighting.AddLight_int_int_int_float -= al_iiif;
            On_Lighting.AddLight_int_int_float_float_float -= al_iifff;
            On_Lighting.AddLight_Vector2_float_float_float -= al_vfff;
            On_Lighting.AddLight_Vector2_Vector3 -= al_vv;
            On_Lighting.AddLight_Vector2_int -= al_torch;
            On_Player.AddBuff -= add_buff;
            On_NPC.TargetClosest -= targetClost;
            On_NPC.TargetClosestUpgraded -= targetClostUpgraded;
            On_NPC.FindFrame -= findFrame;
            On_NPC.VanillaAI -= vAi;
            On_NPC.UpdateNPC -= npcupdate;
            On_NPC.StrikeNPC_HitInfo_bool_bool -= StrikeNpc;
            On_Player.getRect -= modifyRect;
            On_Main.DrawInfernoRings -= drawIr;
            On_Main.DrawProjectiles -= DrawBehindPlayer;
            On_Main.DrawMenu -= drawmenu;
            On_Player.Heal -= player_heal;
            On_Main.DrawTiles -= drawtile;
            On_Projectile.FillWhipControlPoints -= fill_whip_ctrl_points_hook;
            On_Projectile.GetWhipSettings -= get_whip_settings_hook;
            On_Player.WaterCollision -= waterCollisionHook;
            //On_Player.ApplyDamageToNPC -= applydamagetonpc;
            On_Player.GetTotalCritChance -= gettotalcrit;
            On_Main.DrawCursor -= draw_cursor_hook;
            On_Main.DrawThickCursor -= draw_thick_cursor_hook;
            On_Player.UpdateItemDye -= update_item_dye;
            On_LegacyPlayerRenderer.DrawPlayer -= render_player;
            On_Player.Hurt_HurtInfo_bool -= on_player_hurt;
            On_Player.Update_NPCCollision -= update_npc_collision;
            On_Player.ApplyEquipFunctional -= apply_equip_func;
            On_Player.ApplyMeleeScale -= apply_melee_scale;
        }

        private Vector2 draw_thick_cursor_hook(On_Main.orig_DrawThickCursor orig, bool smart)
        {
            if (!EModSys.mi)
            {
                return orig(smart);
            }
            return Vector2.Zero;
        }

        public void drawtile(On_Main.orig_DrawTiles orig, Main self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets, int waterStyleOverride)
        {
            orig(self, solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride);
        }

        private void player_heal(On_Player.orig_Heal orig, Player self, int amount)
        {
            if (!(EntropyMode && self.Entropy().HitTCounter > 0))
            {
                orig(self, amount);
            }
        }

        private void DrawBehindPlayer(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);
            Main.spriteBatch.begin_();
            Texture2D shell = ShellTex.Value;
            Texture2D crystalShield = MariviniumShieldTex.Value;
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.Entropy().nihShellCount > 0)
                {
                    float rot = player.Entropy().CasketSwordRot * 0.2f;
                    int count = player.Entropy().nihShellCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (rot.ToRotationVector2().Y < 0)
                        {
                            Vector2 center = new Vector2(36, 0).RotatedBy(rot);
                            center.Y = 0;
                            float sizeX = Math.Abs(new Vector2(56, 0).RotatedBy(rot + 0.3f).X - new Vector2(56, 0).RotatedBy(rot - 0.3f).X);
                            Main.spriteBatch.Draw(shell, player.Center + player.gfxOffY * Vector2.UnitY - Main.screenPosition + center, null, Color.White * 0.8f * ((((rot.ToRotationVector2().Y) + 1) * 0.5f) * 0.7f + 0.3f), 0, shell.Size() / 2, new Vector2(sizeX / shell.Width, 1), SpriteEffects.None, 0);
                        }
                        rot += MathHelper.TwoPi / count;
                    }
                }
                if (player.Entropy().MariviniumShieldCount > 0)
                {
                    float rot = player.Entropy().CasketSwordRot * -0.2f;
                    int count = player.Entropy().MariviniumShieldCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (rot.ToRotationVector2().Y < 0)
                        {
                            Vector2 center = new Vector2(48, 0).RotatedBy(rot);
                            center.Y = 0;
                            float sizeX = Math.Abs(new Vector2(56, 0).RotatedBy(rot + 0.3f).X - new Vector2(56, 0).RotatedBy(rot - 0.3f).X);
                            Main.spriteBatch.Draw(crystalShield, player.Center + player.gfxOffY * Vector2.UnitY - Main.screenPosition + center, null, Color.White * 0.6f * ((((rot.ToRotationVector2().Y) + 1) * 0.5f) * 0.7f + 0.3f), 0, shell.Size() / 2, new Vector2(sizeX / shell.Width, 1), SpriteEffects.None, 0);
                        }
                        rot += MathHelper.TwoPi / count;
                    }
                }
            }
            Main.spriteBatch.End();
        }
        public float AzShieldBarAlpha = 0;
        private void drawIr(On_Main.orig_DrawInfernoRings orig, Main self)
        {
            //orig 只能在方法末尾调一次。InnoVault 的 PRT 粒子层挂在同一个 DrawInfernoRings 钩子里(比本钩子先注册,
            //因而在 orig 链内),这里原先开头也调了一次 orig,导致全部 PRT 粒子每帧被画两遍,加法粒子亮度直接翻倍。
            //旧 EParticle 时代粒子是在本钩子内、DrawMech 之后手动画一遍,所以保留末尾那次 orig 即可维持原先的层序。

            // I'm assuming these are not needed, as they're handled in EffectLoader.EnsureRenderTargets and other methods.
            // Why is screen2 not here?
            /*
            screen?.Dispose();
            screen = null;
            screen = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            screen3?.Dispose();
            screen3 = null;
            screen3 = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);*/

            Texture2D shell = ShellTex.Value;
            Texture2D crystalShield = MariviniumShieldTex.Value;
            if (Main.LocalPlayer.Entropy().AzafureChargeShieldItem != null || Main.LocalPlayer.Entropy().AzafureDriverShieldItem != null)
            {
                float charge = 0;
                float maxCharge = 0;
                if (Main.LocalPlayer.Entropy().AzafureChargeShieldItem != null && Main.LocalPlayer.Entropy().AzafureChargeShieldItem.ModItem is AzafureChargeShield mi)
                {
                    charge = mi.charge;
                    maxCharge = mi.maxCharge;
                }
                if (Main.LocalPlayer.Entropy().AzafureDriverShieldItem != null && Main.LocalPlayer.Entropy().AzafureDriverShieldItem.ModItem is AzafureDriverCore mi2)
                {
                    charge = mi2.charge;
                    maxCharge = mi2.maxCharge;
                }
                if (charge >= maxCharge)
                {
                    AzShieldBarAlpha = float.Lerp(AzShieldBarAlpha, 0, 0.1f);
                }
                else
                {
                    AzShieldBarAlpha = float.Lerp(AzShieldBarAlpha, 1, 0.1f);
                }
                CEUtils.DrawChargeBar(1.5f, Main.LocalPlayer.Center + Main.LocalPlayer.gfxOffY * Vector2.UnitY - Main.screenPosition + new Vector2(0, -42), ((float)charge / maxCharge), ((charge > 1) ? Color.Lerp(Color.OrangeRed, Color.Orange, (float)Math.Cos(Main.GameUpdateCount * 0.2f) * 0.5f + 0.5f) : Color.Firebrick) * AzShieldBarAlpha);
            }
            else
            {
                AzShieldBarAlpha = float.Lerp(AzShieldBarAlpha, 0, 0.1f);
            }
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom))
                {
                    CEUtils.DrawGlow(player.Center, Color.White * 0.2f, 5.2f);
                }
                if (player.Entropy().nihShellCount > 0)
                {
                    float rot = player.Entropy().CasketSwordRot * 0.2f;
                    int count = player.Entropy().nihShellCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (rot.ToRotationVector2().Y > 0)
                        {
                            Vector2 center = new Vector2(36, 0).RotatedBy(rot);
                            center.Y = 0;
                            float sizeX = Math.Abs(new Vector2(56, 0).RotatedBy(rot + 0.3f).X - new Vector2(56, 0).RotatedBy(rot - 0.3f).X);
                            Main.spriteBatch.Draw(shell, player.Center + player.gfxOffY * Vector2.UnitY - Main.screenPosition + center, null, Color.White * 0.8f * ((((rot.ToRotationVector2().Y) + 1) * 0.5f) * 0.7f + 0.3f), 0, shell.Size() / 2, new Vector2(sizeX / shell.Width, 1), SpriteEffects.None, 0);
                        }
                        rot += MathHelper.TwoPi / count;
                    }
                }
                if (player.Entropy().MariviniumShieldCount > 0)
                {
                    float rot = player.Entropy().CasketSwordRot * -0.2f;
                    int count = player.Entropy().MariviniumShieldCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (rot.ToRotationVector2().Y > 0)
                        {
                            Vector2 center = new Vector2(48, 0).RotatedBy(rot);
                            center.Y = 0;
                            float sizeX = Math.Abs(new Vector2(56, 0).RotatedBy(rot + 0.3f).X - new Vector2(56, 0).RotatedBy(rot - 0.3f).X);
                            Main.spriteBatch.Draw(crystalShield, player.Center + player.gfxOffY * Vector2.UnitY - Main.screenPosition + center, null, Color.White * 0.6f * ((((rot.ToRotationVector2().Y) + 1) * 0.5f) * 0.7f + 0.3f), 0, shell.Size() / 2, new Vector2(sizeX / shell.Width, 1), SpriteEffects.None, 0);
                        }
                        rot += MathHelper.TwoPi / count;
                    }
                }
                if (pocType == -1)
                {
                    pocType = ModContent.ProjectileType<PrisonOfPermafrostCircle>();
                }
                else
                {
                    if (player.ownedProjectileCounts[pocType] > 0)
                    {
                        foreach (Projectile p in Main.ActiveProjectiles)
                        {
                            if (p.type == pocType && p.owner == player.whoAmI)
                            {
                                if (p.ModProjectile is PrisonOfPermafrostCircle poc)
                                {
                                    float alpha = poc.usingTime / 60f;
                                    if (alpha > 1)
                                    {
                                        alpha = 1;
                                    }
                                    Main.spriteBatch.Draw(poc.itemTex, p.Center + p.rotation.ToRotationVector2() * 28 - Main.screenPosition, null, Color.White * alpha, p.rotation + MathHelper.PiOver2, poc.itemTex.Size() / 2, p.scale * 0.5f, SpriteEffects.None, 0);

                                    break;
                                }
                            }
                        }
                    }
                }
            }


            GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;

            Main.spriteBatch.End();


            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.TryGetModPlayer<AcropolisArmorPlayer>(out var mp))
                {
                    mp.DrawMech();
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            orig(self);

        }
        public int pocType = -1;
        private void drawmenu(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);
            EModSys.mi = false;
            if (LoopSoundManager.sounds != null)
            {
                if (LoopSoundManager.sounds.Count > 0)
                {
                    for (int i = 0; i < LoopSoundManager.sounds.Count; i++)
                    {
                        var sound = LoopSoundManager.sounds[i];
                        sound.stop();
                    }
                }
                LoopSoundManager.sounds.Clear();
            }
        }

        private void npcupdate(On_NPC.orig_UpdateNPC orig, NPC self, int i)
        {
            if (self == null || self.type <= NPCID.None)
            {
                return;
            }

            //很显然不活跃的NPC不符合我们的期望
            if (!self.active || !self.TryGetGlobalNPC<EGlobalNPC>(out var ceNPC))
            {
                orig(self, i);
                return;
            }
            if (self.active && self.Entropy().AnimaTrapped > 0)
            {
                ceNPC.AnimaTrapped--;
                self.position += self.velocity;
                self.velocity *= 0.9f;
                for (int ii = 0; ii < self.immune.Length; ii++)
                {
                    if (self.immune[ii] > 0)
                    {
                        self.immune[ii]--;
                    }
                }
            }
            else
            {
                if (self.active && self.TryGetGlobalNPC<DeliriumGlobalNPC>(out var deliriumNPC) && deliriumNPC.delirium)
                {
                    NPC npc = self;
                    npc.damage = deliriumNPC.damage;
                    deliriumNPC.counter--;
                    if (deliriumNPC.counter <= 0)
                    {
                        if (!Main.dedServ)
                        {
                            CEUtils.PlaySound("clicker_static", 1, npc.Center);
                        }
                        deliriumNPC.counter = Main.rand.Next(60, 360);
                        npc.netUpdate = true;
                        npc.netSpam = 0;
                        int npc_ = NPC.NewNPC(npc.GetSource_FromThis(), (int)npc.Center.X, (int)npc.Center.Y, Delirium.npcTurns[Main.rand.Next(Delirium.npcTurns.Count)]);
                        NPC spawn = npc_.ToNPC();
                        spawn.Center = npc.Center;
                        spawn.lifeMax = npc.lifeMax;
                        spawn.life = npc.life;
                        spawn.damage = npc.damage;
                        spawn.GetGlobalNPC<DeliriumGlobalNPC>().delirium = true;
                        spawn.GetGlobalNPC<DeliriumGlobalNPC>().damage = deliriumNPC.damage;
                        spawn.GetGlobalNPC<DeliriumGlobalNPC>().counter = deliriumNPC.counter;
                        spawn.netUpdate = true;
                        spawn.netSpam = 0;
                        npc.active = false;
                    }
                    if (npc.type != NPCID.DukeFishron && npc.type != NPCID.Golem && npc.type != NPCID.SkeletronHead)
                    {
                        orig(self, i);
                        if (npc.type != NPCID.EyeofCthulhu && npc.type != NPCID.QueenBee && npc.type != NPCID.Retinazer && npc.type != NPCID.Spazmatism && npc.type != NPCID.MoonLordCore)
                        {
                            orig(self, i);
                        }
                    }
                }
                if (EntropyMode)
                {
                    if (self.type == NPCID.Golem || self.type == NPCID.GolemHead || self.type == NPCID.GolemHeadFree)
                    {
                        orig(self, i);
                        self.Center -= self.velocity * 0.5f;
                    }
                    if (new List<int> { NPCID.SkeletronPrime, 128, 129, 130, 131, NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, 139, 125, 126 }.Contains(self.type))
                    {
                        orig(self, i);
                        self.position -= self.velocity;
                    }
                }
                orig(self, i);
            }
        }

        private Rectangle modifyRect(On_Player.orig_getRect orig, Player self)
        {
            if (self.GetModPlayer<AtbmPlayer>().Active && Main.netMode != NetmodeID.SinglePlayer)
            {
                return self.GetModPlayer<AtbmPlayer>().opos.getRectCentered(self.width, self.height);
            }
            Rectangle rect = orig(self);
            if (self.Entropy().Scale != 1)
                rect = rect.Center.ToVector2().getRectCentered(self.Entropy().Scale * rect.Width, self.Entropy().Scale * rect.Height);
            return rect;
        }

        private int StrikeNpc(On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig, NPC self, NPC.HitInfo hit, bool fromNet, bool noPlayerInteraction)
        {
            /*if (self.Entropy().nextHitCrit)
            {
                hit.Crit = true;
                float mul = self.Entropy().critDamage.Additive * self.Entropy().critDamage.Multiplicative;
                hit.Damage = (int)(hit.Damage * mul);
            }*/
            if (!hit.InstantKill)
            {
                if (self.boss && (EntropyMode || EDownedBosses.TDR))
                {
                    if (hit.Damage > self.lifeMax * 0.035f)
                    {
                        hit.Damage = (int)(self.lifeMax * 0.035f);
                    }
                    hit.Damage = (int)(hit.Damage * (self.life < (self.Entropy().TDRCounter / (3f * 60 * 60) * self.lifeMax) ? (1 / (1 + ((self.Entropy().TDRCounter / (3f * 60 * 60) * self.lifeMax) - self.life) * (14f / self.lifeMax))) : 1));
                }
            }
            return orig(self, hit, fromNet, noPlayerInteraction);
        }

        private void vAi(On_NPC.orig_VanillaAI orig, NPC self)
        {
            orig(self);
        }

        private void findFrame(On_NPC.orig_FindFrame orig, NPC self)
        {
            if (self.Entropy().ToFriendly)
            {
                self.target = 0;
                NPC npc = self;
                npc.boss = false;

                npc.friendly = true;

                NPC t = null;
                float dist = 4600;
                foreach (NPC n in Main.npc)
                {
                    if (n.active && !n.friendly && !n.dontTakeDamage)
                    {
                        if (CEUtils.getDistance(n.Center, npc.Center) < dist)
                        {
                            t = n;
                            dist = CEUtils.getDistance(n.Center, npc.Center);
                        }
                    }
                }
                if (t == null)
                {
                    npc.Entropy().plrOldPos3 = Main.player[0].position;
                    npc.Entropy().plrOldVel3 = Main.player[0].velocity;
                    Main.player[0].Center = npc.Entropy().f_owner.ToPlayer().Center;
                    Main.player[0].velocity = npc.Entropy().f_owner.ToPlayer().velocity;
                }
                else
                {
                    npc.Entropy().plrOldPos3 = Main.player[0].position;
                    npc.Entropy().plrOldVel3 = Main.player[0].velocity;
                    Main.player[0].Center = t.Center;
                    Main.player[0].velocity = t.velocity;
                }
            }
            orig(self);
            if (self.Entropy().plrOldPos3.HasValue)
            {
                Main.player[0].position = self.Entropy().plrOldPos3.Value;
                self.Entropy().plrOldPos3 = null;
            }
            if (self.Entropy().plrOldVel3.HasValue)
            {
                Main.player[0].velocity = self.Entropy().plrOldVel3.Value;
                self.Entropy().plrOldVel3 = null;
            }
        }

        private void targetClostUpgraded(On_NPC.orig_TargetClosestUpgraded orig, NPC self, bool faceTarget, Vector2? checkPosition)
        {
            orig(self, faceTarget, checkPosition);
            /*if (self.Entropy().ToFriendly)
            {
                self.target = 0;
                NPC npc = self;
                npc.boss = false;

                npc.friendly = true;

                SetTargetTrackingValues(self, faceTarget, CEUtils.getDistance(self.Center, Main.player[0].Center), -1);
            }*/
        }

        public static void SetTargetTrackingValues(NPC npc, bool faceTarget, float realDist, int tankTarget)
        {
            if (tankTarget >= 0)
            {
                npc.targetRect = new Rectangle((int)Main.projectile[tankTarget].position.X, (int)Main.projectile[tankTarget].position.Y, Main.projectile[tankTarget].width, Main.projectile[tankTarget].height);
                npc.direction = 1;
                if (npc.targetRect.X + npc.targetRect.Width / 2 < npc.position.X + npc.width / 2)
                    npc.direction = -1;

                npc.directionY = 1;
                if (npc.targetRect.Y + npc.targetRect.Height / 2 < npc.position.Y + npc.height / 2)
                    npc.directionY = -1;
            }
            else
            {
                if (npc.target < 0 || npc.target >= 255)
                    npc.target = 0;

                npc.targetRect = new Rectangle((int)Main.player[npc.target].position.X, (int)Main.player[npc.target].position.Y, Main.player[npc.target].width, Main.player[npc.target].height);
                if (Main.player[npc.target].dead)
                    faceTarget = false;

                if (Main.player[npc.target].npcTypeNoAggro[npc.type] && npc.direction != 0)
                    faceTarget = false;

                if (faceTarget)
                {
                    _ = Main.player[npc.target].aggro;
                    _ = (Main.player[npc.target].height + Main.player[npc.target].width + npc.height + npc.width) / 4;
                    bool flag = npc.oldTarget >= 0 && npc.oldTarget <= 254;
                    bool num = Main.player[npc.target].itemAnimation == 0 && Main.player[npc.target].aggro < 0;
                    bool flag2 = !npc.boss;
                    if (!(num && flag && flag2))
                    {
                        npc.direction = 1;
                        if (npc.targetRect.X + npc.targetRect.Width / 2 < npc.position.X + npc.width / 2)
                            npc.direction = -1;

                        npc.directionY = 1;
                        if (npc.targetRect.Y + npc.targetRect.Height / 2 < npc.position.Y + npc.height / 2)
                            npc.directionY = -1;
                    }
                }
            }

            if (npc.confused)
                npc.direction *= -1;

            if ((npc.direction != npc.oldDirection || npc.directionY != npc.oldDirectionY || npc.target != npc.oldTarget) && !npc.collideX && !npc.collideY)
                npc.netUpdate = true;
        }
        private void targetClost(On_NPC.orig_TargetClosest orig, NPC self, bool faceTarget)
        {
            orig(self, faceTarget);
            if (self.Entropy().ToFriendly)
            {
                self.target = 0;
                NPC npc = self;
                npc.boss = false;

                npc.friendly = true;
                SetTargetTrackingValues(self, faceTarget, CEUtils.getDistance(self.Center, Main.player[0].Center), -1);
            }
        }

        private void add_buff(On_Player.orig_AddBuff orig, Player self, int type, int timeToAdd, bool quiet, bool foodHack)
        {
            if (self.Entropy().hasAcc("VastLV4"))
            {
                if (type == BuffID.ManaSickness)
                {
                    timeToAdd /= 2;
                }
            }
            if (Main.debuff[type])
            {
                if (Main.rand.NextDouble() < self.Entropy().DebuffImmuneChance)
                {
                    return;
                }
            }
            if (cooldownBuffs.Contains(type))
            {
                timeToAdd = (int)(timeToAdd * self.Entropy().CooldownTimeMult);
            }
            if (Main.debuff[type] && !cooldownBuffs.Contains(type))
            {
                timeToAdd = (int)(timeToAdd * self.Entropy().DebuffTime);
            }
            orig(self, type, timeToAdd, quiet, foodHack);
        }

        private void al_torch(On_Lighting.orig_AddLight_Vector2_int orig, Vector2 position, int torchID)
        {
            if (brillianceLightMulti > 1)
            {
                TorchID.TorchColor(torchID, out var R, out var G, out var B);
                Lighting.AddLight((int)position.X / 16, (int)position.Y / 16, R * brillianceLightMulti, G * brillianceLightMulti, B * brillianceLightMulti);
            }
            else
            {
                orig(position, torchID);

            }
        }

        public static bool BrilEnable
        {
            get
            {
                return !Main.gameMenu && Main.LocalPlayer.Entropy().brillianceCard > 0;
            }
            set
            {
                if (Main.gameMenu)
                {
                    return;
                }
                Main.LocalPlayer.Entropy().brillianceCard = value ? 3 : 0;
            }
        }

        public static float BrillianceCardValue = 1.5f;
        public static float OracleDeckBrilValue = 2f;
        public static float brillianceLightMulti
        {
            get
            {
                if (Main.gameMenu) { return 1; }
                float Value = 1;
                if (Main.LocalPlayer.Entropy().oracleDeck) { Value = OracleDeckBrilValue; }
                else if (BrilEnable) { Value = BrillianceCardValue; }
                return Value;
            }
        }
        private void al_vv(On_Lighting.orig_AddLight_Vector2_Vector3 orig, Vector2 position, Vector3 rgb)
        {
            orig(position, rgb * brillianceLightMulti);
        }


        private void al_vfff(On_Lighting.orig_AddLight_Vector2_float_float_float orig, Vector2 position, float r, float g, float b)
        {
            orig(position, r * brillianceLightMulti, g * brillianceLightMulti, b * brillianceLightMulti);
        }

        private void al_iifff(On_Lighting.orig_AddLight_int_int_float_float_float orig, int i, int j, float r, float g, float b)
        {
            orig(i, j, r * brillianceLightMulti, g * brillianceLightMulti, b * brillianceLightMulti);
        }


        private void al_iiif(On_Lighting.orig_AddLight_int_int_int_float orig, int i, int j, int torchID, float lightAmount)
        {
            orig(i, j, torchID, lightAmount * brillianceLightMulti);
        }
        private Action<T1> GetAction<T1>(Dictionary<string, object> objects, string key)
        {
            if (objects.TryGetValue(key, out object actionObj) && actionObj is Action<T1>)
            {
                return (Action<T1>)actionObj;
            }
            return null;
        }

        private Action<T1, T2> GetAction<T1, T2>(Dictionary<string, object> objects, string key)
        {
            if (objects.TryGetValue(key, out object actionObj) && actionObj is Action<T1, T2>)
            {
                return (Action<T1, T2>)actionObj;
            }
            return null;
        }

        private Action<T1, T2, T3> GetAction<T1, T2, T3>(Dictionary<string, object> objects, string key)
        {
            if (objects.TryGetValue(key, out object actionObj) && actionObj is Action<T1, T2, T3>)
            {
                return (Action<T1, T2, T3>)actionObj;
            }
            return null;
        }
        public override object Call(params object[] args)
        {
            var obj = ModCall.Call(args);
            if (obj != null)
            {
                return obj;
            }
            try
            {
                if (args.Length > 0)
                {
                    if (args[0] is string str)
                    {
                        //Usage: bool flag = (bool)Mod.Call("CheckFlag", "cruiser(or any name below)");
                        if (str.ToLower().Equals("checkflag"))
                        {
                            if (args.Length == 2 && args[1] is string name)
                            {
                                name = name.ToLower();
                                if (name == "acropolis")
                                    return EDownedBosses.downedAcropolis;
                                if (name == "apsychos")
                                    return EDownedBosses.downedApsychos;
                                if (name == "luminaris")
                                    return EDownedBosses.downedLuminaris;
                                if (name == "prophet")
                                    return EDownedBosses.downedProphet;
                                if (name == "nihility_twins")
                                    return EDownedBosses.downedNihilityTwin;
                                if (name == "cruiser")
                                    return EDownedBosses.downedCruiser;
                            }
                            return false;
                        }
                        if (str.ToLower().Equals("RegisterBookMarkEffect".ToLower()))
                        {
                            if (!(args[1] is Dictionary<string, object>))
                            {
                                this.Logger.Warn("Args[1] Must be a Dictionary<string, object>");
                                return null;
                            }
                            Dictionary<string, object> objects = (Dictionary<string, object>)args[1];
                            if (!objects.TryGetValue("Name", out object nameObj) || !(nameObj is string))
                            {
                                this.Logger.Warn("Name is required and must be a string");
                                return null;
                            }
                            string name = (string)nameObj;

                            Action<ModProjectile> onShoot = GetAction<ModProjectile>(objects, "OnShoot");
                            Action<ModProjectile> onActive = GetAction<ModProjectile>(objects, "OnActive");
                            Action<Projectile, bool> onProjectileSpawn = GetAction<Projectile, bool>(objects, "OnProjectileSpawn");
                            Action<Projectile, bool> updateProjectile = GetAction<Projectile, bool>(objects, "UpdateProjectile");
                            Action<Projectile, NPC, int> onHitNPC = GetAction<Projectile, NPC, int>(objects, "OnHitNPC");
                            Action<Projectile, NPC, NPC.HitModifiers> modifyHitNPC = GetAction<Projectile, NPC, NPC.HitModifiers>(objects, "ModifyHitNPC");
                            Action<Projectile, bool> BookUpdate = GetAction<Projectile, bool>(objects, "BookUpdate");

                            BookMarkLoader.RegisterBookmarkEffect(
                                name,
                                onShoot,
                                onActive,
                                onProjectileSpawn,
                                updateProjectile,
                                onHitNPC,
                                modifyHitNPC,
                                BookUpdate
                            );
                        }
                        if (str.ToLower().Equals("RegisterBookMark".ToLower()))
                        {
                            Func<TInput, TOutput> GetModifierFunc<TInput, TOutput>(Dictionary<string, object> objects, string key)
                            {
                                if (objects.TryGetValue(key, out object funcObj) && funcObj is Func<TInput, TOutput>)
                                {
                                    return (Func<TInput, TOutput>)funcObj;
                                }
                                return null;
                            }
                            if (!(args[1] is Dictionary<string, object>))
                            {
                                this.Logger.Warn("Args[1] Must be a Dictionary<string, object>");
                                return null;
                            }
                            Dictionary<string, object> objects = (Dictionary<string, object>)args[1];
                            if (!objects.TryGetValue("ItemType", out object itemTypeObj) || !(itemTypeObj is int))
                            {
                                this.Logger.Warn("ItemType is required and must be an integer");
                                return null;
                            }
                            int itemType = (int)itemTypeObj;

                            if (!objects.TryGetValue("Texture", out object textureObj) || !(textureObj is Asset<Texture2D>))
                            {
                                this.Logger.Warn("Texture is required and must be an Asset<Texture2D>");
                                return null;
                            }
                            Func<Item, Item, bool> func = null;
                            if (objects.TryGetValue("CanBeEquipWithFunc", out var cbew_func))
                            {
                                if (cbew_func is Func<Item, Item, bool> fc)
                                {
                                    func = fc;
                                }
                            }
                            Asset<Texture2D> texture = (Asset<Texture2D>)textureObj;

                            string effectName = objects.TryGetValue("EffectName", out object effectNameObj) && effectNameObj is string
                                ? (string)effectNameObj : "";

                            Func<float, float> modifyStat_Damage = GetModifierFunc<float, float>(objects, "ModifyStat_Damage");
                            Func<float, float> modifyStat_Knockback = GetModifierFunc<float, float>(objects, "ModifyStat_Knockback");
                            Func<float, float> modifyStat_ShootSpeed = GetModifierFunc<float, float>(objects, "ModifyStat_ShootSpeed");
                            Func<float, float> modifyStat_Homing = GetModifierFunc<float, float>(objects, "ModifyStat_Homing");
                            Func<float, float> modifyStat_Size = GetModifierFunc<float, float>(objects, "ModifyStat_Size");
                            Func<float, float> modifyStat_Crit = GetModifierFunc<float, float>(objects, "ModifyStat_Crit");
                            Func<float, float> modifyStat_HomingRange = GetModifierFunc<float, float>(objects, "ModifyStat_HomingRange");
                            Func<int, int> modifyStat_PenetrateAddition = GetModifierFunc<int, int>(objects, "ModifyStat_PenetrateAddition");
                            Func<float, float> modifyStat_AttackSpeed = GetModifierFunc<float, float>(objects, "ModifyStat_AttackSpeed");
                            Func<int, int> modifyStat_ArmorPenetration = GetModifierFunc<int, int>(objects, "ModifyStat_ArmorPenetration");
                            Func<float, float> modifyStat_LifeSteal = GetModifierFunc<float, float>(objects, "ModifyStat_LifeSteal");
                            Func<int, int> modifyProjectileType = GetModifierFunc<int, int>(objects, "ModifyProjectileType");
                            Func<int> modifyBaseProjectileType = objects.TryGetValue("ModifyBaseProjectileType", out object mbptObj) && mbptObj is Func<int>
                                ? (Func<int>)mbptObj : null;
                            Func<int, int> modifyShootCooldown = GetModifierFunc<int, int>(objects, "ModifyShootCooldown");

                            BookMarkLoader.RegisterBookmark(
                                itemType,
                                texture,
                                effectName,
                                modifyStat_Damage,
                                modifyStat_Knockback,
                                modifyStat_ShootSpeed,
                                modifyStat_Homing,
                                modifyStat_Size,
                                modifyStat_Crit,
                                modifyStat_HomingRange,
                                modifyStat_PenetrateAddition,
                                modifyStat_AttackSpeed,
                                modifyStat_ArmorPenetration,
                                modifyStat_LifeSteal,
                                modifyProjectileType,
                                modifyBaseProjectileType,
                                modifyShootCooldown,
                                func
                            );
                        }
                        if (str.Equals("IsBookMark"))
                        {
                            Item item = (Item)args[1];
                            return BookMarkLoader.IsABookMark(item);
                        }
                        #region TwistedTwinsStuff
                        if (str.Equals("SetTTHoldoutCheck"))
                        {
                            EGlobalProjectile.checkHoldOut = (bool)args[1];
                        }
                        if (str.Equals("GetTTHoldoutCheck"))
                        {
                            return EGlobalProjectile.checkHoldOut;
                        }
                        if (str.Equals("CopyProjForTTwin"))
                        {
                            Projectile projectile = ((int)args[1]).ToProj();
                            EGlobalProjectile.checkHoldOut = false;
                            foreach (Projectile p in Main.projectile)
                            {
                                if (p.active && p.type == ModContent.ProjectileType<TwistedTwinMinion>() && p.owner == Main.myPlayer)
                                {

                                    int phd = Projectile.NewProjectile(Main.LocalPlayer.GetSource_ItemUse(Main.LocalPlayer.HeldItem), p.Center, Vector2.Zero, projectile.type, projectile.damage, projectile.knockBack, projectile.owner);
                                    Projectile ph = phd.ToProj();
                                    ph.scale *= 0.8f;
                                    ph.Entropy().IndexOfTwistedTwinShootedThisProj = p.identity;
                                    ph.netUpdate = true;
                                    Projectile projts = ph;
                                    ph.damage = (int)(ph.damage * TwistedTwinMinion.damageMul);
                                    if (!projts.usesLocalNPCImmunity)
                                    {
                                        projts.usesLocalNPCImmunity = true;
                                        projts.localNPCHitCooldown = 12;
                                    }
                                }
                            }
                            EGlobalProjectile.checkHoldOut = true;
                        }
                        #endregion
                        //Set a specific color for NPC
                        //Usage: Mod.Call("SetBarColor", ModContent.NPCType<T>(), color);
                        if (str.Equals("SetBarColor"))
                        {
                            int type = (int)args[1];
                            Color color = (Color)args[2];
                            EntropyBossbar.bossbarColor[type] = color;
                        }
                        if (str.Equals("GetBookMarkSlots"))
                        {
                            return ((Player)args[1]).GetMyMaxActiveBookMarks(((Player)args[1]).HeldItem);
                        }
                        if (str.Equals("AddBookMarkSlot")) //Set this every update just like minion slots
                        {
                            ((Player)args[1]).Entropy().AdditionalBookmarkSlot += (int)args[2];
                        }
                        if (str.Equals("AddBookMarkSlotSpecialTexture")) //Set this every update just like minion slots, client only
                        {
                            ((Player)args[1]).Entropy().BookmarkHolderSpecialTextures.Add((Texture2D)args[2]);
                        }
                        if (str.Equals("RegisterDebuff"))
                        {
                            ExternalDebuffs.Add(
                                new DebuffDisplayEntry(
                                    (Func<NPC, bool>)args[1],
                                    (Func<Texture2D>)args[2]
                                )
                            );
                            return null;
                        }
                    }
                }
            }
            catch
            {
                string e = (args[0] is string str) ? $"({str})" : "";
                Logger.Warn($"CalamityEntropy: ModCall's parameter is Error!{e}");
            }
            return null;
        }
        private static void AddBoss(Mod bossChecklist, Mod hostMod, string name, float difficulty, Func<bool> downed, object npcTypes, Dictionary<string, object> extraInfo, bool miniBoss = false)
            => bossChecklist.Call(miniBoss ? "LogMiniBoss" : "LogBoss", hostMod, name, difficulty, downed, npcTypes, extraInfo);
        public static List<MusicBox> mbRegs = null;
        public void RegistryMusicBoxes()
        {
            foreach (var mb in mbRegs)
            {
                MusicBox.AddMusicBox(mb.MusicFile, mb.Type, mb.MusicBoxTile);
            }
            mbRegs = null;
        }
        public static List<int> cooldownBuffs;
        public override void PostSetupContent()
        {
            CommonEffects.Load();
            CELists.Load();
            Apsychos.WhiteTransShader();
            if (!Main.dedServ)
            {
                EntropySkies.setUpShaderFilters();
            }
            ScreenShaker.Init();
            Typer.activeTypers = new();
            StartBagGItem.items = new List<int>();
            VanityDisplaySys.SetupVanities();

            void bookUpdateDirt(Projectile projectile, bool ownerClient)
            {
                if (ownerClient && CECooldowns.CheckCD("Dirt", 60))
                {
                    if (projectile.ModProjectile is EntropyBookHeldProjectile eb)
                        eb.ShootSingleProjectile(ModContent.ProjectileType<BMDirtProj>(), projectile.Center, projectile.rotation.ToRotationVector2(), 0.3f, 1, 0.8f, (proj) => { proj.ai[1] = -1; proj.ai[0] = ItemID.DirtBlock; });
                }
            }
            BookMarkLoader.RegisterBookmarkEffect("DirtEffect", bookUpdate: bookUpdateDirt);
            BookMarkLoader.RegisterBookmark(ItemID.DirtBlock, null, effectName: "DirtEffect");

            void bookUpdateStone(Projectile projectile, bool ownerClient)
            {
                if (ownerClient && CECooldowns.CheckCD("Stone", 60))
                {
                    if (projectile.ModProjectile is EntropyBookHeldProjectile eb)
                        eb.ShootSingleProjectile(ModContent.ProjectileType<BMDirtProj>(), projectile.Center, projectile.rotation.ToRotationVector2(), 0.25f, 1, 0.8f, (proj) => { proj.ai[1] = 1; proj.ai[0] = ItemID.StoneBlock; });
                }
            }
            BookMarkLoader.RegisterBookmarkEffect("StoneEffect", bookUpdate: bookUpdateStone);
            BookMarkLoader.RegisterBookmark(ItemID.StoneBlock, null, effectName: "StoneEffect");
            if (!Main.dedServ)
            {
                Main.instance.LoadItem(ItemID.StoneBlock);
                Main.instance.LoadItem(ItemID.DirtBlock);
            }
            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                Item item = ContentSamples.ItemsByType[i];
                if (item.ModItem != null && item.ModItem is IGetFromStarterBag)
                {
                    StartBagGItem.items.Add(i);
                }
            }
            if (ModLoader.TryGetMod("MoreObtainingTooltips", out Mod moreObtainingTooltips))
            {
                this.Logger.Info("MOT Support:" + moreObtainingTooltips.Call(
                    "AddCustomizedSource",
                    this.GetLocalization("HallowedEnemiesDrop").Value,
                    new int[1] { ModContent.ItemType<HolyMantle>() }));

                moreObtainingTooltips.Call(
                    "AddCustomizedSource",
                    this.GetLocalization("VoidOreMine").Value,
                    new int[1] { ModContent.ItemType<VoidOre>() });
                moreObtainingTooltips.Call(
                    "AddCustomizedSource",
                    this.GetLocalization("EvilEnemiesDrop").Value,
                    new int[1] { ModContent.ItemType<BitternessCard>() });
                moreObtainingTooltips.Call(
                    "AddCustomizedSource",
                    this.GetLocalization("DungeonEnemiesDrop").Value,
                    new int[1] { ModContent.ItemType<BookMarkBlackKnife>() });
                moreObtainingTooltips.Call(
                    "AddCustomizedSource",
                    this.GetLocalization("AbyssalPiercerObt").Value,
                    new int[1] { ModContent.ItemType<AbyssalPiercer>() });
                moreObtainingTooltips.Call(
                    "AddCustomizedSource",
                    this.GetLocalization("AstralFishing").Value,
                    new int[1] { ModContent.ItemType<GreedCard>() });

            }
            cooldownBuffs = new List<int>() { BuffID.PotionSickness, BuffID.ChaosState, ModContent.BuffType<DivineShieldCooldown>(), ModContent.BuffType<ShatteredOrb>() };
            foreach (ICELoader setup in ILoaders)
            {
                setup.SetupData();
                if (!Main.dedServ)
                {
                    setup.LoadAsset();
                }
            }
            Type baseTypeLR = typeof(LoreEffect);
            Type[] lrTypes = AssemblyManager.GetLoadableTypes(this.Code);
            foreach (Type type in lrTypes)
            {
                if (!type.IsSubclassOf(baseTypeLR) || type.IsAbstract)
                    continue;
                var loreEffect = (LoreEffect)Activator.CreateInstance(type);
                LoreReworkSystem.loreEffects[loreEffect.ItemType] = loreEffect;
                var _ = loreEffect.Decription.Value;
            }
            RegistryMusicBoxes();
            for (int i = 0; i < NPCLoader.NPCCount; i++)
            {
                NPCID.Sets.SpecificDebuffImmunity[i][ModContent.BuffType<Content.Buffs.HeatDeath>()] = false;
                NPCID.Sets.SpecificDebuffImmunity[i][ModContent.BuffType<LifeOppress>()] = false;
            }

            string MyGameFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games");
            string Isaac1 = Path.Combine(MyGameFolder, "Binding of Isaac Repentance").Replace("/", "\\");
            string Isaac2 = Path.Combine(MyGameFolder, "Binding of Isaac Repentance+").Replace("/", "\\");
            BrokenAnkh.isaac = Directory.Exists(Isaac1) || Directory.Exists(Isaac2);

            //Load special sounds
            if (!Main.dedServ)
            {
                ealaserSound = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/VoidLaserLoop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                ealaserSound2 = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/portal_loop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                ofCharge = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/ElectricLoop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                FableEye.sound = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/prophetlaserloop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                UrnOfSoulsHoldout.loopSnd = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/flamethrower loop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                otLoop = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/ThretherLoop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            #region MusicDisplay
            if(ModLoader.TryGetMod("MusicDisplay", out Mod display))
            {
                display.Call("AddMusic",
                  (short)MusicLoader.GetMusicSlot(this, "Assets/Sounds/Music/Apsychos"),
                  this.GetLocalization("Musics.Apsychos"),
                  this.GetLocalization("Musics.Francium"),
                  this.GetLocalization("Musics.ThemeOf").WithFormatArgs(CEUtils.GetNPCName<Apsychos>()));
                display.Call("AddMusic",
                  (short)MusicLoader.GetMusicSlot(this, "Assets/Sounds/Music/CruiserBoss"),
                  this.GetLocalization("Musics.Cruiser"),
                  this.GetLocalization("Musics.Francium"),
                  this.GetLocalization("Musics.ThemeOf").WithFormatArgs(CEUtils.GetNPCName<CruiserHead>()));
                display.Call("AddMusic",
                  (short)MusicLoader.GetMusicSlot(this, "Assets/Sounds/Music/SpectralForesight"),
                  this.GetLocalization("Musics.Prophet1"),
                  this.GetLocalization("Musics.Francium"),
                  this.GetLocalization("Musics.ThemeOf").WithFormatArgs(CEUtils.GetNPCName<TheProphet>()));
                display.Call("AddMusic",
                  (short)MusicLoader.GetMusicSlot(this, "Assets/Sounds/Music/Prophet2"),
                  this.GetLocalization("Musics.Prophet2"),
                  this.GetLocalization("Musics.BadbfHX"),
                  this.GetLocalization("Musics.ThemeOf").WithFormatArgs(CEUtils.GetNPCName<TheProphet>()));
                display.Call("AddMusic",
                  (short)MusicLoader.GetMusicSlot(this, "Assets/Sounds/Music/vtfight"),
                  this.GetLocalization("Musics.Cruiser"),
                  this.GetLocalization("Musics.BadbfHX"),
                  this.GetLocalization("Musics.ThemeOf").
                  WithFormatArgs(this.GetLocalization("NPCs.NihilityActeriophage.BossChecklistIntegration.EntryName")));
                display.Call("AddMusic",
                  (short)MusicLoader.GetMusicSlot(this, "Assets/Sounds/Music/LuminarisBoss"),
                  this.GetLocalization("Musics.Luminaris"),
                  this.GetLocalization("Musics.BadbfHX"),
                  this.GetLocalization("Musics.ThemeOf").WithFormatArgs(CEUtils.GetNPCName<Luminaris>()));
                display.Call("AddMusic",
                  (short)MusicLoader.GetMusicSlot(this, "Assets/Sounds/Music/HellBlazenRobotics"),
                  this.GetLocalization("Musics.Acropolis"),
                  this.GetLocalization("Musics.SobaNoodles"),
                  this.GetLocalization("Musics.ThemeOf").WithFormatArgs(CEUtils.GetNPCName<AcropolisMachine>()));
            }
            #endregion

            #region RegisterBoss
            Mod bossChecklist;
            if (ModLoader.TryGetMod("BossChecklist", out bossChecklist))
            {
                if (bossChecklist != null)
                {
                    {
                        {
                            string entryName = "AcropolisMechine";
                            List<int> collection = new List<int>() { };
                            Action<SpriteBatch, Rectangle, Color> portrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                            {
                                Texture2D texture = ModContent.Request<Texture2D>("CalamityEntropy/Assets/BCL/AcropolisMachine").Value;
                                sb.Draw(texture, rect.Center.ToVector2(), null, color, 0, texture.Size() / 2, 0.8f, SpriteEffects.None, 0);
                            };
                            Func<bool> AcropDowned = () => EDownedBosses.downedAcropolis;
                            AddBoss(bossChecklist, Instance, entryName, 2.8f, AcropDowned, ModContent.NPCType<AcropolisMachine>(), new Dictionary<string, object>()
                            {
                                ["displayName"] = Language.GetText("Mods.CalamityEntropy.NPCs.AcropolisMachine.BossChecklistIntegration.EntryName"),
                                ["spawnInfo"] = Language.GetText("Mods.CalamityEntropy.NPCs.AcropolisMachine.BossChecklistIntegration.SpawnInfo"),
                                ["despawnMessage"] = Language.GetText("Mods.CalamityEntropy.NPCs.AcropolisMachine.BossChecklistIntegration.DespawnMessage"),
                                ["collectibles"] = collection,
                                ["customPortrait"] = portrait
                            }, true);
                        }
                        {
                            string entryName = "Apsychos";
                            List<int> collection = new List<int>() { };
                            Action<SpriteBatch, Rectangle, Color> portrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                            {
                                Texture2D texture = ModContent.Request<Texture2D>("CalamityEntropy/Assets/BCL/Apsychos").Value;
                                sb.Draw(texture, rect.Center.ToVector2(), null, color, 0, texture.Size() / 2, 0.36f, SpriteEffects.None, 0);
                            };
                            Func<bool> downed = () => EDownedBosses.downedApsychos;
                            AddBoss(bossChecklist, Instance, entryName, 6.4f, downed, ModContent.NPCType<Apsychos>(), new Dictionary<string, object>()
                            {
                                ["displayName"] = Language.GetText("Mods.CalamityEntropy.NPCs.Apsychos.BossChecklistIntegration.EntryName"),
                                ["spawnInfo"] = Language.GetText("Mods.CalamityEntropy.NPCs.Apsychos.BossChecklistIntegration.SpawnInfo"),
                                ["despawnMessage"] = Language.GetText("Mods.CalamityEntropy.NPCs.Apsychos.BossChecklistIntegration.DespawnMessage"),
                                ["spawnItems"] = ModContent.ItemType<CursedRunestone>(),
                                ["collectibles"] = collection,
                                ["customPortrait"] = portrait
                            });
                        }
                        {
                            string entryName = "Luminaris";
                            List<int> collection = new List<int>() { };
                            Action<SpriteBatch, Rectangle, Color> portrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                            {
                                Texture2D texture = ModContent.Request<Texture2D>("CalamityEntropy/Assets/BCL/LuminarisBossCheckList").Value;
                                sb.Draw(texture, rect.Center.ToVector2(), null, color, 0, texture.Size() / 2, 1, SpriteEffects.None, 0);
                            };
                            Func<bool> Luminaris = () => EDownedBosses.downedLuminaris;
                            AddBoss(bossChecklist, Instance, entryName, 9.505f, Luminaris, ModContent.NPCType<Luminaris>(), new Dictionary<string, object>()
                            {
                                ["displayName"] = Language.GetText("Mods.CalamityEntropy.NPCs.Luminaris.BossChecklistIntegration.EntryName"),
                                ["spawnInfo"] = Language.GetText("Mods.CalamityEntropy.NPCs.Luminaris.BossChecklistIntegration.SpawnInfo"),
                                ["despawnMessage"] = Language.GetText("Mods.CalamityEntropy.NPCs.Luminaris.BossChecklistIntegration.DespawnMessage"),
                                ["spawnItems"] = ModContent.ItemType<IllusionaryDew>(),
                                ["collectibles"] = collection,
                                ["customPortrait"] = portrait
                            });
                        }
                        {
                            string entryName = "TheProphet";
                            List<int> collection = new List<int>() { ModContent.ItemType<RuneSong>(), ModContent.ItemType<UrnOfSouls>(), ModContent.ItemType<SpiritBanner>(), ModContent.ItemType<ProphecyFlyingKnife>(), ModContent.ItemType<RuneMachineGun>(), ModContent.ItemType<ForeseeOrb>(), ModContent.ItemType<RuneWing>() };
                            Action<SpriteBatch, Rectangle, Color> portrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                            {
                                Texture2D texture = ModContent.Request<Texture2D>("CalamityEntropy/Assets/BCL/Prophet").Value;
                                sb.Draw(texture, rect.Center.ToVector2(), null, color, 0, texture.Size() / 2, 1, SpriteEffects.None, 0);
                            };
                            Func<bool> prophet = () => EDownedBosses.downedProphet;
                            AddBoss(bossChecklist, Instance, entryName, 12.02f, prophet, ModContent.NPCType<TheProphet>(), new Dictionary<string, object>()
                            {
                                ["displayName"] = Language.GetText("Mods.CalamityEntropy.NPCs.TheProphet.BossChecklistIntegration.EntryName"),
                                ["spawnInfo"] = Language.GetText("Mods.CalamityEntropy.NPCs.TheProphet.BossChecklistIntegration.SpawnInfo"),
                                ["despawnMessage"] = Language.GetText("Mods.CalamityEntropy.NPCs.TheProphet.BossChecklistIntegration.DespawnMessage"),
                                ["spawnItems"] = ModContent.ItemType<ProphecyToken>(),
                                ["collectibles"] = collection,
                                ["customPortrait"] = portrait
                            });
                        }
                        {
                            string entryName = "NihilityTwin";
                            List<int> segments = new List<int>() { ModContent.NPCType<NihilityActeriophage>(), ModContent.NPCType<ChaoticCell>() };
                            List<int> collection = new List<int>() { ModContent.ItemType<NihilityTwinBag>(), ModContent.ItemType<NihilityTwinTrophy>(), ModContent.ItemType<NihilityTwinRelic>(), ModContent.ItemType<NihilityShell>(), ModContent.ItemType<Voidseeker>(), ModContent.ItemType<EventideSniper>(), ModContent.ItemType<NihilityBacteriophageWand>(), ModContent.ItemType<StarlessNight>(), ModContent.ItemType<VoidPathology>() };
                            Action<SpriteBatch, Rectangle, Color> portrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                            {
                                Texture2D texture = ModContent.Request<Texture2D>("CalamityEntropy/Assets/BCL/NihilityTwin").Value;
                                sb.Draw(texture, rect.Center.ToVector2(), null, color, 0, texture.Size() / 2, 0.7f, SpriteEffects.None, 0);
                            };
                            Func<bool> nihtwin = () => EDownedBosses.downedNihilityTwin;
                            AddBoss(bossChecklist, Instance, entryName, 19.3f, nihtwin, segments, new Dictionary<string, object>()
                            {
                                ["displayName"] = Language.GetText("Mods.CalamityEntropy.NPCs.NihilityActeriophage.BossChecklistIntegration.EntryName"),
                                ["spawnInfo"] = Language.GetText("Mods.CalamityEntropy.NPCs.NihilityActeriophage.BossChecklistIntegration.SpawnInfo"),
                                ["despawnMessage"] = Language.GetText("Mods.CalamityEntropy.NPCs.NihilityActeriophage.BossChecklistIntegration.DespawnMessage"),
                                ["spawnItems"] = ModContent.ItemType<NihilityHorn>(),
                                ["collectibles"] = collection,
                                ["customPortrait"] = portrait
                            });
                        }
                        {
                            string entryName = "Cruiser";
                            List<int> segments = new List<int>() { ModContent.NPCType<CruiserHead>(), ModContent.NPCType<CruiserBody>(), ModContent.NPCType<CruiserTail>() };
                            List<int> collection = new List<int>() { ModContent.ItemType<CruiserBag>(), ModContent.ItemType<CruiserTrophy>(), ModContent.ItemType<VoidScales>(), ModContent.ItemType<VoidMonolith>(), ModContent.ItemType<CruiserRelic>(), ModContent.ItemType<VoidRelics>(), ModContent.ItemType<VoidAnnihilate>(), ModContent.ItemType<VoidElytra>(), ModContent.ItemType<VoidEcho>(), ModContent.ItemType<Content.Items.Weapons.Silence>(), ModContent.ItemType<WingsOfHush>(), ModContent.ItemType<WindOfUndertaker>(), ModContent.ItemType<VoidToy>(), ModContent.ItemType<TheocracyPearlToy>(), ModContent.ItemType<CruiserPlush>() };
                            Action<SpriteBatch, Rectangle, Color> portrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                            {
                                Texture2D texture = ModContent.Request<Texture2D>("CalamityEntropy/Assets/BCL/Cruiser").Value;
                                sb.Draw(texture, rect.Center.ToVector2(), null, color, 0, texture.Size() / 2, 0.7f, SpriteEffects.None, 0);
                            };
                            Func<bool> cruiser = () => EDownedBosses.downedCruiser;
                            AddBoss(bossChecklist, Instance, entryName, 22.1f, cruiser, segments, new Dictionary<string, object>()
                            {
                                // 键挂在头部 NPC 的内部名下,与其余 Boss 一致:BossChecklist 自身按
                                // Mods.<模组>.NPCs.<头部NPC>.BossChecklistIntegration.EntryName 取名,挂错位置会回退英文
                                ["displayName"] = Language.GetText("Mods.CalamityEntropy.NPCs.CruiserHead.BossChecklistIntegration.EntryName"),
                                ["spawnInfo"] = Language.GetText("Mods.CalamityEntropy.NPCs.CruiserHead.BossChecklistIntegration.SpawnInfo"),
                                ["despawnMessage"] = Language.GetText("Mods.CalamityEntropy.NPCs.CruiserHead.BossChecklistIntegration.DespawnMessage"),
                                ["spawnItems"] = ModContent.ItemType<VoidBottle>(),
                                ["collectibles"] = collection,
                                ["customPortrait"] = portrait
                            });
                        }
                    }

                }
            }
            #endregion
            #region Bossbar Colors
            EntropyBossbar.bossbarColor[NPCID.KingSlime] = new Color(90, 160, 255);
            EntropyBossbar.bossbarColor[NPCID.EyeofCthulhu] = new Color(255, 40, 40);
            EntropyBossbar.bossbarColor[NPCID.EaterofWorldsBody] = new Color(80, 40, 255);
            EntropyBossbar.bossbarColor[NPCID.EaterofWorldsHead] = new Color(80, 40, 255);
            EntropyBossbar.bossbarColor[NPCID.EaterofWorldsTail] = new Color(80, 40, 255);
            EntropyBossbar.bossbarColor[NPCID.BrainofCthulhu] = new Color(255, 40, 40);
            EntropyBossbar.bossbarColor[NPCID.QueenBee] = new Color(242, 242, 145);
            EntropyBossbar.bossbarColor[NPCID.DD2DarkMageT1] = new Color(180, 230, 255);
            EntropyBossbar.bossbarColor[NPCID.DD2DarkMageT3] = new Color(180, 230, 255);
            EntropyBossbar.bossbarColor[NPCID.SkeletronHead] = new Color(221, 221, 188);
            EntropyBossbar.bossbarColor[NPCID.Deerclops] = new Color(220, 200, 200);
            EntropyBossbar.bossbarColor[NPCID.WallofFlesh] = new Color(255, 40, 40);
            EntropyBossbar.bossbarColor[NPCID.Retinazer] = new Color(190, 190, 190);
            EntropyBossbar.bossbarColor[NPCID.Spazmatism] = new Color(190, 190, 190);
            EntropyBossbar.bossbarColor[NPCID.TheDestroyer] = new Color(190, 190, 190);
            EntropyBossbar.bossbarColor[NPCID.SkeletronPrime] = new Color(190, 190, 190);
            EntropyBossbar.bossbarColor[491] = new Color(180, 120, 80);
            EntropyBossbar.bossbarColor[NPCID.QueenSlimeBoss] = new Color(200, 160, 240);
            EntropyBossbar.bossbarColor[NPCID.Plantera] = new Color(255, 170, 255);
            EntropyBossbar.bossbarColor[NPCID.Golem] = new Color(225, 106, 9);
            EntropyBossbar.bossbarColor[NPCID.GolemHead] = new Color(225, 106, 9);
            EntropyBossbar.bossbarColor[325] = new Color(255, 206, 106);
            EntropyBossbar.bossbarColor[327] = new Color(244, 184, 106);
            EntropyBossbar.bossbarColor[344] = new Color(0, 255, 172);
            EntropyBossbar.bossbarColor[344] = new Color(240, 28, 28);
            EntropyBossbar.bossbarColor[345] = new Color(200, 244, 246);
            EntropyBossbar.bossbarColor[392] = new Color(150, 250, 255);
            EntropyBossbar.bossbarColor[NPCID.DukeFishron] = new Color(80, 146, 255);
            EntropyBossbar.bossbarColor[636] = Color.White;
            EntropyBossbar.bossbarColor[551] = new Color(180, 75, 80);
            EntropyBossbar.bossbarColor[NPCID.CultistBoss] = new Color(0, 60, 255);
            EntropyBossbar.bossbarColor[422] = new Color(208, 255, 235);
            EntropyBossbar.bossbarColor[493] = new Color(14, 155, 230);
            EntropyBossbar.bossbarColor[507] = new Color(255, 30, 170);
            EntropyBossbar.bossbarColor[517] = new Color(255, 100, 46);
            EntropyBossbar.bossbarColor[NPCID.MoonLordCore] = new Color(213, 194, 156);
            EntropyBossbar.bossbarColor[NPCID.MoonLordLeechBlob] = new Color(213, 194, 156);
            EntropyBossbar.bossbarColor[NPCID.MoonLordHead] = new Color(213, 194, 156);
            EntropyBossbar.bossbarColor[NPCID.MoonLordHand] = new Color(213, 194, 156);
            EntropyBossbar.bossbarColor[ModContent.NPCType<CruiserHead>()] = new Color(150, 60, 255);
            EntropyBossbar.bossbarColor[ModContent.NPCType<VoidPope>()] = new Color(200, 40, 255);
            EntropyBossbar.bossbarColor[ModContent.NPCType<NihilityActeriophage>()] = new Color(255, 155, 248);
            EntropyBossbar.bossbarColor[ModContent.NPCType<ChaoticCell>()] = new Color(255, 155, 248);
            EntropyBossbar.bossbarColor[ModContent.NPCType<TheProphet>()] = new Color(180, 233, 255);
            EntropyBossbar.bossbarColor[ModContent.NPCType<Luminaris>()] = new Color(150, 100, 215);
            EntropyBossbar.bossbarColor[ModContent.NPCType<AcropolisMachine>()] = new Color(255, 93, 13);
            EntropyBossbar.bossbarColor[ModContent.NPCType<Apsychos>()] = new Color(255, 160, 20);

            try
            {
                if (!Main.dedServ)
                {
                    if (ModLoader.TryGetMod("SOTS", out Mod sots))
                    {
                        AddBossbarColor(sots, "SubspaceSerpentHead", new Color(115, 114, 160));
                        AddBossbarColor(sots, "PutridPinky1", Color.Pink);
                        AddBossbarColor(sots, "PutridPinkyPhase2", Color.Pink);
                        AddBossbarColor(sots, "Polaris", new Color(200, 250, 250));
                        AddBossbarColor(sots, "NewPolaris", new Color(200, 250, 250));
                        AddBossbarColor(sots, "Lux", new Color(255, 200, 230));
                        AddBossbarColor(sots, "Glowmoth", new Color(255, 240, 200));
                        AddBossbarColor(sots, "PharaohsCurse", Color.Gold);
                        AddBossbarColor(sots, "UnusedAdvisorHead", new Color(238, 208, 255));
                    }
                    if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fs))
                    {
                        AddBossbarColor(fs, "AbomBoss", new Color(249, 226, 77));
                        AddBossbarColor(fs, "BanishedBaron", new Color(230, 240, 242));
                        AddBossbarColor(fs, "CosmosChampion", Color.DarkOrange);
                        AddBossbarColor(fs, "EarthChampion", Color.Orange);
                        AddBossbarColor(fs, "LifeChampion", Color.Gold);
                        AddBossbarColor(fs, "NatureChampion", Color.Green);
                        AddBossbarColor(fs, "ShadowChampion", new Color(143, 100, 234));
                        AddBossbarColor(fs, "SpiritChampion", Color.DarkGoldenrod);
                        AddBossbarColor(fs, "TerraChampion", Color.DarkGreen);
                        AddBossbarColor(fs, "TimberChampion", new Color(230, 240, 242));
                        AddBossbarColor(fs, "WillChampion", new Color(234, 213, 143));
                        AddBossbarColor(fs, "CursedCoffin", Color.Yellow);
                        AddBossbarColor(fs, "LifeChallenger", Color.Gold);
                        AddBossbarColor(fs, "Magmaw", Color.Gray);
                        AddBossbarColor(fs, "MutantBoss", AprilFool ? new Color(217, 142, 67) : new Color(100, 200, 255));
                        AddBossbarColor(fs, "TrojanSquirrel", new Color(147, 108, 85));
                    }
                }
            }
            catch
            {
                Logger.Warn("CalamityEntropy: Other mods' bossbar color failed to setup");
            }
            #endregion

            //Custom titles
            if (!Main.dedServ && Main.rand.NextBool(9))
            {
                SetARandomEntropyTitle();
            }
        }
        public static void SetARandomEntropyTitle()
        {
            if (Main.dedServ)
                return;
            int titleType = Main.rand.Next(7);
            string text = Instance.GetLocalization("TitleTexts.Terraria").Value + Instance.GetLocalization("TitleTexts.Title" + titleType.ToString()).Value;
            if (titleType == 4)
            {
                List<string> names = new List<string>();
                //Pick a random weapon
                for (int i = 0; i < ItemLoader.ItemCount; i++)
                {
                    Item item = ContentSamples.ItemsByType[i];
                    if (item.damage > 0 && item.ammo == AmmoID.None)
                    {
                        names.Add(item.Name);
                    }
                }
                text = text.Replace("[NAME]", names[Main.rand.Next(names.Count)]);
            }
            if (titleType == 5)
            {
                List<string> names = new List<string>();
                //Pick a random entropy item
                for (int i = ItemID.Count; i < ItemLoader.ItemCount; i++)
                {
                    Item item = ContentSamples.ItemsByType[i];
                    if (item.ModItem != null && item.ModItem.Mod is CalamityEntropy)
                    {
                        names.Add(item.Name);
                    }
                }
                text = text.Replace("[NAME]", names[Main.rand.Next(names.Count)]);
            }
            Main.instance.Window.Title = text;
        }
        public static bool SetupBossbarClrAuto = true;

        public static void AddBossbarColor(Mod mod, string name, Color color)
        {
            if (mod == null)
                return;
            if (mod.TryFind<ModNPC>(name, out var mnpc))
            {
                EntropyBossbar.bossbarColor[mnpc.Type] = color;
            }
        }

        public static float blackMaskAlpha = 0;
        public static int blackMaskTime = 0;
        public static Vector2 vLToCenter(Vector2 v, float z)
        {
            return Main.ScreenSize.ToVector2() / 2 + (v - Main.ScreenSize.ToVector2() / 2) * z;
        }
        public bool beegameInited = false;

        public static void SpawnHeavenSpark(Vector2 pos, float rot, float length, float scale, Color color = default, int LifeTime = 24)
        {
            Vector2 norl = rot.ToRotationVector2();
            float sengs = length;
            if (color == default)
            {
                color = Color.BlueViolet;
            }
            for (int j = 0; j < 53; j++)
            {
                PRTLoader.NewParticle<PRT_HeavenfallStar>(pos, norl * (0.1f + j * 0.34f) * sengs, color, Main.rand.NextFloat(0.6f, 1.3f) * scale)
                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, norl.ToRotation(), LifeTime);
            }
            for (int j = 0; j < 53; j++)
            {
                PRTLoader.NewParticle<PRT_HeavenfallStar>(pos, norl * -(0.1f + j * 0.34f) * sengs, color, Main.rand.NextFloat(0.6f, 1.3f) * scale)
                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, (-norl).ToRotation(), LifeTime);
            }
        }
    }
}
