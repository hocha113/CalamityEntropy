using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.NPCs.FriendFinderNPC;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Weapons.Fractal;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs
{
    public class RewardPoolItem
    {
        public int item = 0;
        public int stack = 1;
        public RewardPoolItem(int n, int s)
        {
            this.item = n;
            this.stack = s;
        }
    }
    public class RewardPool
    {
        public List<RewardPoolItem> items = new List<RewardPoolItem>();
        public void addPool(RewardPool pool)
        {
            foreach (RewardPoolItem item in pool.items)
            {
                this.items.Add(item);
            }
        }
        public void Add(RewardPoolItem item)
        {
            this.items.Add(item);

        }
        public RewardPoolItem RandomItem()
        {
            return this.items[Main.rand.Next(0, this.items.Count)];
        }
    }
    public class LotteryMachine : ModNPC
    {
        //旧写法是实例字段ModContent.Request,每条NPC实例化就拉一遍贴图还卡加载
        //PreDraw按openCouter/textureSpecial切帧,嵌套类纯粹为了不污染外层字段
        private static class LMTextures
        {
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/off")] internal static Asset<Texture2D> closed;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/open1")] internal static Asset<Texture2D> openf1;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/open2")] internal static Asset<Texture2D> openf2;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/open3")] internal static Asset<Texture2D> openf3;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/on")] internal static Asset<Texture2D> opened;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/warn")] internal static Asset<Texture2D> warning;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/warn2")] internal static Asset<Texture2D> warning2;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/unhappy")] internal static Asset<Texture2D> unhappy;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/serious")] internal static Asset<Texture2D> serious;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/tmad1")] internal static Asset<Texture2D> toMad1;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/tmad2")] internal static Asset<Texture2D> toMad2;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/mad")] internal static Asset<Texture2D> mad;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/angry")] internal static Asset<Texture2D> madangry;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/mad_talk")] internal static Asset<Texture2D> madtalk;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/smile")] internal static Asset<Texture2D> smile;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/flowey")] internal static Asset<Texture2D> flowey;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/think")] internal static Asset<Texture2D> think;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/prepare")] internal static Asset<Texture2D> prepare;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/sreward")] internal static Asset<Texture2D> specialReward;
            [VaultLoaden("CalamityEntropy/Content/NPCs/LM/what")] internal static Asset<Texture2D> what;
        }
        public bool open = false;
        public int openCouter = 0;
        public int openFrame = 0;
        public int sameItemCount = 0;
        public int lastCItem = -2;
        public int textureSpecial = 0;
        public int specialTime = 0;
        public int warnCounter = 0;
        public int SpawnTimer = 0;
        public int nucTime = 0;
        public int useCd = 0;
        public bool flag1 = false;
        private bool mouseRightClicked = false;
        public RewardPool s1;
        public RewardPool g1;
        public RewardPool p1;
        public RewardPool g2;
        public RewardPool p2;
        public RewardPool g3;
        public RewardPool p3;
        public RewardPool p4;
        public RewardPool p5;
        public RewardPool p6;
        public RewardPool p7;
        public bool sd = true;
        public bool say = false;
        public Color sayColor = Color.White;
        public string sayStr = "";

        //奖池条目:灾厄在场且该内容存在时用灾厄物,否则用 4.0 的自有/原版替身
        private static void AddCalOrOwn(RewardPool pool, int calType, int calStack, int ownType, int ownStack)
        {
            bool useCal = CERef.Has && calType > 0;
            pool.Add(new RewardPoolItem(useCal ? calType : ownType, useCal ? calStack : ownStack));
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(open);
            writer.Write(sameItemCount);
            writer.Write(lastCItem);
            writer.Write(textureSpecial);
            writer.Write(specialTime);
            writer.Write(nucTime);
            writer.Write(useCd);
            writer.Write(flag1);
            writer.Write(say);
            writer.WriteRGB(sayColor);
            writer.Write(sayStr);

        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            open = reader.ReadBoolean();
            sameItemCount = reader.ReadInt32();
            lastCItem = reader.ReadInt32();
            textureSpecial = reader.ReadInt32();
            specialTime = reader.ReadInt32();
            nucTime = reader.ReadInt32();
            useCd = reader.ReadInt32();
            flag1 = reader.ReadBoolean();
            say = reader.ReadBoolean();
            sayColor = reader.ReadRGB();
            sayStr = reader.ReadString();
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.width = 176;
            NPC.height = 176;
            NPC.damage = 0;
            NPC.defense = 2;
            NPC.lifeMax = 200;
            NPC.Entropy().VoidTouchDR = 1;
            NPC.value = 0f;
            NPC.knockBackResist = 1f;
            NPC.noTileCollide = false;
            NPC.noGravity = false;
            NPC.friendly = true;
            NPCID.Sets.ImmuneToAllBuffs[Type] = true;
            NPC.netAlways = true;

        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            //友好NPC就一颗RealisticExplosion,密度控最低,别学boss death那套
            if (NPC.life <= 0)
                //PRT_RealisticExplosion友好NPC单颗,密度控最低
                PRTLoader.NewParticle<PRT_RealisticExplosion>(NPC.Center, Vector2.Zero, Color.White, 4)
                    .Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.friendly)
            {
                open = true;
                openFrame = 3;
                if (specialTime < 1)
                {
                    sameItemCount = 12;
                    Say("LMDialog8", Color.Red);
                    textureSpecial = 7;
                    specialTime = 160;
                    SpawnTimer = 100;
                    useCd = 10;
                }
            }
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (projectile.hostile)
                return false;
            return null;
        }
        public override void OnSpawn(IEntitySource source)
        {

        }
        public override bool CanBeHitByNPC(NPC attacker)
        {
            return false;
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 0.6f;
            modifiers.SetMaxDamage(36);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 0;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tx;
            tx = LMTextures.closed.Value;
            if (open)
            {
                tx = LMTextures.opened.Value;
                if (openFrame < 3)
                {
                    if (openFrame == 0)
                    {
                        tx = LMTextures.openf1.Value;
                    }
                    if (openFrame == 1)
                    {
                        tx = LMTextures.openf2.Value;
                    }
                    if (openFrame == 2)
                    {
                        tx = LMTextures.openf3.Value;
                    }
                }
                else
                {
                    if (textureSpecial == 1)
                    {
                        tx = LMTextures.warning.Value;
                    }
                    if (textureSpecial == 2)
                    {
                        tx = LMTextures.unhappy.Value;
                    }
                    if (textureSpecial == 3)
                    {
                        tx = LMTextures.serious.Value;
                    }
                    if (textureSpecial == 4 || textureSpecial == 5 || textureSpecial == 6 || textureSpecial == 7)
                    {
                        if (warnCounter < 5)
                        {
                            tx = LMTextures.toMad1.Value;
                        }
                        if (warnCounter < 10)
                        {
                            tx = LMTextures.toMad2.Value;
                        }
                        if (warnCounter >= 10)
                        {
                            if (textureSpecial == 4)
                            {
                                tx = LMTextures.mad.Value;
                            }
                            if (textureSpecial == 5)
                            {
                                tx = LMTextures.madangry.Value;
                            }
                            if (textureSpecial == 6)
                            {
                                tx = LMTextures.madtalk.Value;
                            }
                            if (textureSpecial == 7)
                            {
                                tx = LMTextures.warning2.Value;
                            }


                        }
                    }
                    if (textureSpecial == 8)
                    {
                        tx = LMTextures.smile.Value;
                    }

                    if (textureSpecial == 10)
                    {
                        tx = LMTextures.think.Value;
                    }
                    if (textureSpecial == 11)
                    {
                        tx = LMTextures.what.Value;
                    }
                    if (textureSpecial == 12)
                    {
                        tx = LMTextures.prepare.Value;
                    }
                    if (textureSpecial == 13)
                    {
                        tx = LMTextures.specialReward.Value;
                    }
                    if (textureSpecial == 9)
                    {
                        tx = LMTextures.flowey.Value;
                    }

                }
            }
            spriteBatch.Draw(tx, NPC.Center - Main.screenPosition, null, Color.White, 0, new Vector2(NPC.width, NPC.height) / 2, 1, SpriteEffects.None, 0);
            return false;
        }

        public override void AI()
        {
            if (NPC.velocity.Y == 0)
                NPC.velocity.X *= 0.8f;
            NPC.velocity.X *= 0.96f;
            if (sd)
            {
                sd = false;
                #region pools

                //灾厄在场按 3.33 下标换回灾厄物;YharimsStimulants 已删、Wrathwing 盗贼武器不改回
                s1 = new RewardPool();
                AddCalOrOwn(s1, CEID.Item_WulfrumMetalScrap, 10, ItemID.IronBar, 10);
                s1.Add(new RewardPoolItem(ItemID.Feather, 10));
                AddCalOrOwn(s1, CEID.Item_EnergyCore, 1, ModContent.ItemType<AzafureCircuitry>(), 1);
                s1.Add(new RewardPoolItem(ItemID.LifeCrystal, 1));
                AddCalOrOwn(s1, CEID.Item_DubiousPlating, 4, ModContent.ItemType<AzafurePlating>(), 4);
                AddCalOrOwn(s1, CEID.Item_MysteriousCircuitry, 4, ModContent.ItemType<AzafureCircuitry>(), 4);
                AddCalOrOwn(s1, CEID.Item_StormlionMandible, 1, ItemID.AntlionMandible, 1);
                AddCalOrOwn(s1, CEID.Item_StormjawStaff, 1, ItemID.FlinxStaff, 1);
                s1.Add(new RewardPoolItem(ItemID.Diamond, 5));
                AddCalOrOwn(s1, CEID.Item_BloodOrb, 5, ItemID.Bone, 5);
                s1.Add(new RewardPoolItem(68, 8));
                AddCalOrOwn(s1, CEID.Item_AncientBoneDust, 2, ItemID.Bone, 2);
                s1.Add(new RewardPoolItem(ItemID.PoopBlock, 10));
                s1.Add(new RewardPoolItem(296, 1));
                s1.Add(new RewardPoolItem(0, 1));
                s1.Add(new RewardPoolItem(ItemID.Heart, 10));
                s1.Add(new RewardPoolItem(ItemID.LesserHealingPotion, 10));
                s1.Add(new RewardPoolItem(ItemID.LesserManaPotion, 10));
                s1.Add(new RewardPoolItem(ItemID.Ruby, 8));

                g1 = new RewardPool();
                AddCalOrOwn(g1, CEID.Item_SulphuricScale, 1, ItemID.SharkFin, 1);
                g1.Add(new RewardPoolItem(1320, 1));
                g1.Add(new RewardPoolItem(ItemID.LifeCrystal, 4));
                AddCalOrOwn(g1, CEID.Item_AshenStalactite, 1, ItemID.Shuriken, 100);
                AddCalOrOwn(g1, CEID.Item_RottenDogtooth, 1, ItemID.PoisonedKnife, 100);
                AddCalOrOwn(g1, CEID.Item_AnechoicCoating, 5, ItemID.GillsPotion, 5);
                g1.Add(new RewardPoolItem(1303, 1));
                g1.Add(new RewardPoolItem(1322, 1));
                g1.Add(new RewardPoolItem(ItemID.HealingPotion, 10));
                g1.Add(new RewardPoolItem(ItemID.HeartLantern, 1));
                g1.Add(new RewardPoolItem(ItemID.ManaPotion, 10));
                g1.Add(new RewardPoolItem(ItemID.Ruby, 15));
                g1.Add(new RewardPoolItem(1128, 1));

                p1 = new RewardPool();
                p1.Add(new RewardPoolItem(2341, 1));
                p1.Add(new RewardPoolItem(906, 1));
                AddCalOrOwn(p1, CEID.Item_OldLordClaymore, 1, ItemID.Katana, 1);
                p1.Add(new RewardPoolItem(2296, 1));
                p1.Add(new RewardPoolItem(ItemID.FallenStar, 300));
                AddCalOrOwn(p1, CEID.Item_GiantShell, 1, ItemID.ObsidianShield, 1);
                p1.Add(new RewardPoolItem(2430, 1));
                AddCalOrOwn(p1, CEID.Item_BurntSienna, 2, ItemID.GoldenCrate, 2);
                p1.Add(new RewardPoolItem(ItemID.HealingPotion, 100));
                p1.Add(new RewardPoolItem(ItemID.ManaPotion, 100));
                AddCalOrOwn(p1, CEID.Item_CrownJewel, 1, ItemID.RoyalGel, 1);
                AddCalOrOwn(p1, CEID.Item_RustyBeaconPrototype, 1, ItemID.LifeformAnalyzer, 1);
                p1.Add(new RewardPoolItem(ItemID.LifeCrystal, 8));

                g2 = new RewardPool();
                AddCalOrOwn(g2, CEID.Item_TitanHeart, 3, ItemID.LifeFruit, 3);
                AddCalOrOwn(g2, CEID.Item_UrsaSergeant, 1, ItemID.Amarok, 1);
                AddCalOrOwn(g2, CEID.Item_SolarVeil, 2, ItemID.Ectoplasm, 2);
                AddCalOrOwn(g2, CEID.Item_Poseidon, 1, ItemID.SkyFracture, 1);
                g2.Add(new RewardPoolItem(1518, 1));
                g2.Add(new RewardPoolItem(381, 15));
                g2.Add(new RewardPoolItem(1184, 15));
                g2.Add(new RewardPoolItem(1612, 1));
                AddCalOrOwn(g2, CEID.Item_IcicleTrident, 1, ItemID.UnholyTrident, 1);
                AddCalOrOwn(g2, CEID.Item_ElephantKiller, 1, ItemID.VenusMagnum, 1);
                AddCalOrOwn(g2, CEID.Item_Abaddon, 1, ItemID.FrozenTurtleShell, 1);
                AddCalOrOwn(g2, CEID.Item_CelestialClaymore, 1, ItemID.Excalibur, 1);
                g2.Add(new RewardPoolItem(ItemID.WrathPotion, 6));

                p2 = new RewardPool();
                p2.Add(new RewardPoolItem(1291, 5));
                p2.Add(new RewardPoolItem(365, 15));
                p2.Add(new RewardPoolItem(1105, 15));
                p2.Add(new RewardPoolItem(1253, 10));
                AddCalOrOwn(p2, CEID.Item_StormSaber, 1, ItemID.BeamSword, 1);
                AddCalOrOwn(p2, CEID.Item_FrigidflashBolt, 1, ItemID.CrystalSerpent, 1);
                AddCalOrOwn(p2, CEID.Item_TheFirstShadowflame, 1, ItemID.ShadowFlameHexDoll, 1);
                AddCalOrOwn(p2, CEID.Item_IgneousExaltation, 1, ItemID.SanguineStaff, 1);
                AddCalOrOwn(p2, CEID.Item_IceStar, 1, ItemID.FrostDaggerfish, 150);
                AddCalOrOwn(p2, CEID.Item_CryonicBar, 3, ItemID.HallowedBar, 3);
                AddCalOrOwn(p2, CEID.Item_RuinMedallion, 1, ItemID.MagmaStone, 1);
                AddCalOrOwn(p2, CEID.Item_BelchingSaxophone, 1, ItemID.MagicalHarp, 1);
                AddCalOrOwn(p2, CEID.Item_TheDarkMaster, 1, ItemID.OnyxBlaster, 1);
                AddCalOrOwn(p2, CEID.Item_SolarVeil, 2, ItemID.Ectoplasm, 2);

                g3 = new RewardPool();
                AddCalOrOwn(g3, CEID.Item_HivePod, 1, ItemID.OpticStaff, 1);
                AddCalOrOwn(g3, CEID.Item_LivingShard, 10, ItemID.ChlorophyteBar, 10);
                AddCalOrOwn(g3, CEID.Item_CoreofCalamity, 2, ItemID.LifeFruit, 5);
                g3.Add(new RewardPoolItem(1006, 30));
                g3.Add(new RewardPoolItem(1551, 1));
                g3.Add(new RewardPoolItem(3018, 1));
                g3.Add(new RewardPoolItem(3021, 1));
                AddCalOrOwn(g3, CEID.Item_TheCommunity, 1, ItemID.CelestialShell, 1);
                AddCalOrOwn(g3, CEID.Item_Regenerator, 1, ItemID.CharmofMyths, 1);
                AddCalOrOwn(g3, CEID.Item_BloomStone, 1, ItemID.CelestialStone, 1);
                AddCalOrOwn(g3, CEID.Item_AbyssalDivingGear, 1, ItemID.ArcticDivingGear, 1);

                p3 = new RewardPool();
                p3.Add(new RewardPoolItem(938, 1));
                p3.Add(new RewardPoolItem(1508, 15));
                p3.Add(new RewardPoolItem(1513, 1));
                p3.Add(new RewardPoolItem(1570, 1));
                p3.Add(new RewardPoolItem(1552, 20));
                p3.Add(new RewardPoolItem(3261, 20));
                p3.Add(new RewardPoolItem(1444, 1));
                p3.Add(new RewardPoolItem(1445, 1));
                p3.Add(new RewardPoolItem(1446, 1));
                p3.Add(new RewardPoolItem(4679, 1));
                AddCalOrOwn(p3, CEID.Item_BlossomFlux, 1, ItemID.Tsunami, 1);
                AddCalOrOwn(p3, CEID.Item_EternalBlizzard, 1, ItemID.Marrow, 1);
                AddCalOrOwn(p3, CEID.Item_Keelhaul, 1, ItemID.TacticalShotgun, 1);
                AddCalOrOwn(p3, CEID.Item_HadalUrn, 1, ItemID.RazorbladeTyphoon, 1);
                AddCalOrOwn(p3, CEID.Item_FantasyTalisman, 1, ItemID.PaladinsHammer, 1);
                AddCalOrOwn(p3, CEID.Item_PerennialBar, 20, ItemID.ChlorophyteBar, 20);
                AddCalOrOwn(p3, CEID.Item_GrandScale, 2, ItemID.FishronWings, 1);

                p4 = new RewardPool();
                p4.Add(new RewardPoolItem(3110, 1));
                p4.Add(new RewardPoolItem(1248, 1));
                p4.Add(new RewardPoolItem(1343, 1));
                p4.Add(new RewardPoolItem(1858, 1));
                p4.Add(new RewardPoolItem(3883, 1));
                p4.Add(new RewardPoolItem(3817, 80));
                AddCalOrOwn(p4, CEID.Item_Malachite, 1, ItemID.VampireKnives, 1);
                AddCalOrOwn(p4, CEID.Item_LifeAlloy, 5, ItemID.BeetleHusk, 5);
                AddCalOrOwn(p4, CEID.Item_PlagueCellCanister, 50, ItemID.Nanites, 50);
                AddCalOrOwn(p4, CEID.Item_ExaltedOathblade, 1, ItemID.TerraBlade, 1);
                AddCalOrOwn(p4, CEID.Item_TenebreusTides, 1, ItemID.NorthPole, 1);
                AddCalOrOwn(p4, CEID.Item_ScoriaBar, 25, ItemID.SpectreBar, 25);
                AddCalOrOwn(p4, CEID.Item_AegisBlade, 1, ItemID.PossessedHatchet, 1);
                AddCalOrOwn(p4, CEID.Item_StarSputter, 1, ItemID.StakeLauncher, 1);
                AddCalOrOwn(p4, CEID.Item_Vesuvius, 1, ItemID.TheEyeOfCthulhu, 1);
                AddCalOrOwn(p4, CEID.Item_BrinyBaron, 1, ItemID.SniperRifle, 1);

                p5 = new RewardPool();
                AddCalOrOwn(p5, CEID.Item_Necroplasm, 40, ItemID.LunarBar, 15);
                AddCalOrOwn(p5, CEID.Item_Bloodstone, 5, ItemID.FragmentSolar, 15);
                AddCalOrOwn(p5, CEID.Item_ArkoftheElements, 1, ItemID.Meowmere, 1);
                AddCalOrOwn(p5, CEID.Item_ClockworkBow, 1, ItemID.Phantasm, 1);
                AddCalOrOwn(p5, CEID.Item_SanctifiedSpark, 1, ItemID.NebulaBlaze, 1);
                AddCalOrOwn(p5, CEID.Item_AbyssalDivingSuit, 1, ItemID.MasterNinjaGear, 1);
                AddCalOrOwn(p5, CEID.Item_MirrorBlade, 1, ItemID.InfluxWaver, 1);
                AddCalOrOwn(p5, CEID.Item_Swordsplosion, 1, ItemID.StarWrath, 1);
                AddCalOrOwn(p5, CEID.Item_MoonstoneCrown, 1, ItemID.CelestialEmblem, 1);
                AddCalOrOwn(p5, CEID.Item_ExodiumCluster, 1, ItemID.LunarOre, 30);
                AddCalOrOwn(p5, CEID.Item_PlanetaryAnnihilation, 1, ItemID.VortexBeater, 1);
                AddCalOrOwn(p5, CEID.Item_StatisNinjaBelt, 1, ItemID.BlackBelt, 1);
                AddCalOrOwn(p5, CEID.Item_OccultSkullCrown, 1, ItemID.NightVisionHelmet, 1);
                AddCalOrOwn(p5, CEID.Item_UltraLiquidator, 1, ItemID.RainbowCrystalStaff, 1);
                p5.Add(new RewardPoolItem(ItemID.LastPrism, 1));

                p6 = new RewardPool();
                AddCalOrOwn(p6, CEID.Item_NightmareFuel, 25, ItemID.SpookyWood, 99);
                AddCalOrOwn(p6, CEID.Item_EndothermicEnergy, 25, ItemID.FrostCore, 5);
                AddCalOrOwn(p6, CEID.Item_DarksunFragment, 25, ItemID.FragmentSolar, 25);
                AddCalOrOwn(p6, CEID.Item_OmegaHealingPotion, 10, ItemID.SuperHealingPotion, 15);
                AddCalOrOwn(p6, CEID.Item_CosmicDischarge, 1, ModContent.ItemType<WraithSoulEssence>(), 20);
                AddCalOrOwn(p6, CEID.Item_GalaxySmasher, 1, ItemID.Meowmere, 1);
                AddCalOrOwn(p6, CEID.Item_Murasama, 1, ItemID.Terrarian, 1);
                AddCalOrOwn(p6, CEID.Item_VoidEaterMarionette, 1, ItemID.StardustDragonStaff, 1);
                AddCalOrOwn(p6, CEID.Item_MirrorofKalandra, 1, ItemID.MoonlordTurretStaff, 1);
                AddCalOrOwn(p6, CEID.Item_Riftburst, 1, ItemID.SDMG, 1);
                AddCalOrOwn(p6, CEID.Item_Omicron, 1, ItemID.LastPrism, 1);

                p7 = new RewardPool();
                AddCalOrOwn(p7, CEID.Item_ChickenCannon, 1, ItemID.Celeb2, 1);
                AddCalOrOwn(p7, CEID.Item_CodebreakerBase, 1, ModContent.ItemType<AzafureCircuitry>(), 30);
                AddCalOrOwn(p7, CEID.Item_YharimsCrystal, 1, ModContent.ItemType<VoidBar>(), 10);
                AddCalOrOwn(p7, CEID.Item_AscendantSpiritEssence, 15, ModContent.ItemType<WraithSoulEssence>(), 15);
                AddCalOrOwn(p7, CEID.Item_AuricOre, 100, ModContent.ItemType<VoidOre>(), 100);
                AddCalOrOwn(p7, CEID.Item_DragonsBreath, 1, ItemID.SDMG, 1);
                AddCalOrOwn(p7, CEID.Item_ArkoftheCosmos, 1, ModContent.ItemType<FinalFractal>(), 1);
                AddCalOrOwn(p7, CEID.Item_DragonPow, 1, ItemID.DD2SquireBetsySword, 1);
                p7.Add(new RewardPoolItem(ModContent.ItemType<FlowingLight>(), 1));
                AddCalOrOwn(p7, CEID.Item_YharonSoulFragment, 10, ModContent.ItemType<VoidScales>(), 10);
                AddCalOrOwn(p7, CEID.Item_AuricBar, 5, ModContent.ItemType<VoidBar>(), 5);
                p7.Add(new RewardPoolItem(ItemID.Zenith, 1));

                #endregion
            }
            if (NPC.ai[0] == 1 && !Main.dedServ)
            {
                NPC.ai[0] = 0;
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)CEMessageType.LotteryMachineRightClicked);
                    packet.Write(Main.LocalPlayer.whoAmI);
                    packet.Write(NPC.whoAmI);
                    packet.Write(Main.myPlayer);
                    packet.Send();
                }
                else { RightClicked(Main.LocalPlayer); }
            }
            var r = Main.rand;
            NPC.onFire = false;
            if (useCd > 0)
            {
                useCd--;
            }
            if (Main.netMode != NetmodeID.Server)
            {
                if (!mouseRightClicked && Mouse.GetState().RightButton == ButtonState.Pressed)
                {
                    if (new Rectangle((int)Main.MouseWorld.X - 1, (int)Main.MouseWorld.Y - 1, 2, 2).Intersects(NPC.getRect()))
                    {
                        if (CEUtils.getDistance(NPC.Center, Main.LocalPlayer.Center) < 250)
                        {
                            if ((SpawnTimer <= 0 && nucTime == 0) || (Main.LocalPlayer.HeldItem.type == ItemID.CopperCoin || Main.LocalPlayer.HeldItem.type == ItemID.SilverCoin || Main.LocalPlayer.HeldItem.type == ItemID.GoldCoin || Main.LocalPlayer.HeldItem.type == ItemID.PlatinumCoin))
                            {
                                if (useCd <= 0)
                                {
                                    useCd = 16;

                                    NPC.ai[0] = 1;
                                    NPC.ai[1] = Main.myPlayer;
                                    if (Main.LocalPlayer.HeldItem.type == ItemID.CopperCoin || Main.LocalPlayer.HeldItem.type == ItemID.SilverCoin || Main.LocalPlayer.HeldItem.type == ItemID.GoldCoin || Main.LocalPlayer.HeldItem.type == ItemID.PlatinumCoin)
                                    {
                                        Main.LocalPlayer.itemAnimation = 14;
                                        Main.LocalPlayer.itemAnimationMax = 14;
                                        Main.LocalPlayer.itemTime = 14;
                                        Main.LocalPlayer.itemTimeMax = 14;
                                        Main.LocalPlayer.ApplyItemAnimation(Main.LocalPlayer.HeldItem);

                                    }

                                }
                            }
                        }
                    }
                }
                mouseRightClicked = Mouse.GetState().RightButton == ButtonState.Pressed;
            }
            if (nucTime > 0)
            {
                nucTime = 0;
                Vector2 spawnPos = Main.LocalPlayer.position + new Vector2(0, -600);
                int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, new Vector2(0, 10), ModContent.ProjectileType<AtlasNuc>(), 0, 0, Main.myPlayer);
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p);
                }
                if (sameItemCount > 6)
                {
                    for (int i = 0; i < sameItemCount - 6; i++)
                    {
                        Projectile.NewProjectile(Main.LocalPlayer.GetSource_FromAI(), spawnPos + new Vector2(r.Next(-120, 120), r.Next(-100, 100)), new Vector2(0, 10), ModContent.ProjectileType<AtlasNuc>(), 0, 0, Main.myPlayer);

                    }
                }
            }
            if (open)
            {
                if (openFrame < 3)
                {
                    openCouter += 1;
                    if (openCouter == 5)
                    {
                        openCouter = 0;
                        openFrame++;
                        if (openFrame == 3)
                        {
                            Say("LMDialog1", Color.Green);
                        }
                    }
                }
                else
                {
                    if (textureSpecial == 0 || textureSpecial == -1)
                    {
                        warnCounter = 0;
                        specialTime = 0;
                    }
                    else
                    {

                        specialTime--;
                        if (specialTime <= 0)
                        {
                            textureSpecial = 0;
                        }
                    }
                    if (textureSpecial == 4 || textureSpecial == 5 || textureSpecial == 6 || textureSpecial == 7)
                    {
                        warnCounter++;
                        specialTime = 60;
                    }
                    else
                    {
                        warnCounter = 0;
                    }
                    if (SpawnTimer > 0)
                    {
                        SpawnTimer--;
                        if (SpawnTimer == 0)
                        {
                            if (textureSpecial == 7)
                            {
                                nucTime = 120;
                            }
                        }
                    }
                }
            }
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public void RightClicked(Player player)
        {
            if (Main.dedServ)
            {
                if (NPC.netSpam >= 10)
                {
                    NPC.netSpam = 9;
                }
            }
            var r = Main.rand;
            if (!open)
            {
                open = true;
                SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/system_open"), NPC.Center);
            }
            else
            {
                if (openFrame >= 3)
                {
                    bool hasBoss = false;
                    string bossName = "";
                    foreach (NPC n in Main.npc)
                    {
                        if (n.boss && n.active)
                        {
                            hasBoss = true;
                            bossName = n.FullName;
                            if (n.realLife >= 0)
                            {
                                bossName = Main.npc[n.realLife].FullName;
                            }
                        }
                    }
                    int itemType = -1;
                    itemType = player.HeldItem.type;
                    if (itemType == lastCItem)
                    {
                        sameItemCount++;
                    }
                    else
                    {
                        lastCItem = itemType;
                        sameItemCount = 0;
                    }
                    if (itemType == 0)
                    {
                        if (sameItemCount == 0)
                        {
                            Say("LMDialog2", Color.Green);
                            textureSpecial = 8;
                            specialTime = 120;
                        }
                        else if (sameItemCount == 1)
                        {
                            Say("LMDialog3", Color.Orange);
                            textureSpecial = 3;
                            specialTime = 160;
                        }
                        else if (sameItemCount == 2)
                        {
                            Say("LMDialog4", Color.Orange);
                            textureSpecial = 1;
                            specialTime = 160;
                        }
                        else if (sameItemCount == 3)
                        {
                            Say("LMDialog5", Color.Orange);
                            textureSpecial = 2;
                            specialTime = 160;
                        }
                        else if (sameItemCount == 4)
                        {
                            Say("LMDialog6", Color.OrangeRed);
                            textureSpecial = 4;
                            specialTime = 160;
                        }
                        else if (sameItemCount == 5)
                        {
                            Say("LMDialog7", Color.Red);
                            textureSpecial = 5;
                            specialTime = 160;
                        }
                        else if (sameItemCount >= 6)
                        {
                            Say("LMDialog8", Color.Red);
                            textureSpecial = 7;
                            specialTime = 160;
                            SpawnTimer = 100;
                            useCd = 10;
                        }
                    }
                    else if (itemType == ItemID.PoopBlock || itemType == ItemID.PoopWall)
                    {
                        Say("LMDialog8", Color.Red);
                        textureSpecial = 7;
                        specialTime = 160;
                        SpawnTimer = 100;
                        useCd = 400;
                        sameItemCount = 60;
                    }
                    else if (itemType == (CERef.Has && CEID.Item_AuricOre > 0 ? CEID.Item_AuricOre : ModContent.ItemType<VoidOre>()))
                    {
                        Say("LMDialog9", Color.Red);
                        textureSpecial = 9;
                        specialTime = 90;
                    }
                    else if (itemType == ItemID.CopperCoin)
                    {
                        if (SpawnTimer > 0)
                        {
                            flag1 = true;
                            textureSpecial = 9;
                            specialTime = 100;
                            SpawnTimer = 0;
                            Say("LMDialog10", Color.Yellow, 0.7f);

                        }
                        else
                        {
                            textureSpecial = -1;
                            Say("LMDialog11", Color.Yellow, 0.86f);
                            useCd = 160;
                            CEUtils.PlaySound("coininsert", 1, NPC.Center);
                            if (Main.myPlayer == player.whoAmI)
                            {
                                int pj = Projectile.NewProjectile(NPC.GetSource_FromAI(), player.Center - new Vector2(0, 650), new Vector2(0, 16), ModContent.ProjectileType<AtlasItem>(), 0, 0, Main.myPlayer);
                                Main.projectile[pj].Entropy().AtlasItemStack = 0;
                                Main.projectile[pj].Entropy().AtlasItemType = 0;
                                Main.projectile[pj].netUpdate = true;
                            }
                        }
                    }
                    else if (itemType == ItemID.SilverCoin)
                    {
                        if (SpawnTimer > 0)
                        {
                            flag1 = true;
                            textureSpecial = 9;
                            specialTime = 100;
                            SpawnTimer = 0;
                            Say("LMDialog12", Color.Yellow, 0.7f);

                        }
                        else
                        {
                            textureSpecial = -1;
                            player.HeldItem.stack--;
                            int rtype = 0;
                            int stack = 1;
                            RewardPool pool = new RewardPool();

                            pool.addPool(s1);


                            RewardPoolItem ri = pool.RandomItem();

                            rtype = ri.item;
                            stack = ri.stack;
                            useCd = 16;
                            CEUtils.PlaySound("coininsert", 1, NPC.Center);
                            if (Main.myPlayer == player.whoAmI)
                            {
                                int pj = Projectile.NewProjectile(NPC.GetSource_FromAI(), player.Center - new Vector2(0, 650), new Vector2(0, 16), ModContent.ProjectileType<AtlasItem>(), 0, 0, Main.myPlayer);
                                Main.projectile[pj].Entropy().AtlasItemStack = stack;
                                Main.projectile[pj].Entropy().AtlasItemType = rtype;
                                Main.projectile[pj].netUpdate = true;
                            }
                        }
                    }
                    else if (itemType == ItemID.GoldCoin)
                    {
                        if (SpawnTimer > 0)
                        {
                            flag1 = true;
                            textureSpecial = 9;
                            specialTime = 100;
                            SpawnTimer = 0;
                            Say("LMDialog12", Color.Yellow, 0.7f);

                        }
                        else
                        {
                            textureSpecial = -1;
                            player.HeldItem.stack--;
                            int rtype = 0;
                            int stack = 1;
                            RewardPool pool = new RewardPool();

                            pool.addPool(s1);
                            pool.addPool(g1);
                            if (Main.hardMode)
                            {
                                pool.addPool(g2);
                            }

                            RewardPoolItem ri = pool.RandomItem();


                            rtype = ri.item;
                            stack = ri.stack;
                            useCd = 16;
                            CEUtils.PlaySound("coininsert", 1, NPC.Center);
                            if (Main.myPlayer == player.whoAmI)
                            {
                                int pj = Projectile.NewProjectile(NPC.GetSource_FromAI(), player.Center - new Vector2(0, 650), new Vector2(0, 16), ModContent.ProjectileType<AtlasItem>(), 0, 0, Main.myPlayer);
                                Main.projectile[pj].Entropy().AtlasItemStack = stack;
                                Main.projectile[pj].Entropy().AtlasItemType = rtype;
                                Main.projectile[pj].netUpdate = true;
                            }
                        }
                    }
                    else if (itemType == ItemID.PlatinumCoin)
                    {
                        if (SpawnTimer > 0)
                        {
                            flag1 = true;
                            textureSpecial = 9;
                            specialTime = 100;
                            SpawnTimer = 0;
                            if (Main.hardMode)
                            {
                                Say("LMDialog12", Color.Yellow, 0.7f);
                            }
                            else
                            {
                                Say("LMDialog13", Color.Yellow, 0.7f);
                            }

                        }
                        else
                        {
                            textureSpecial = -1;
                            player.HeldItem.stack--;
                            int rtype = 0;
                            int stack = 1;
                            RewardPool pool = new RewardPool();

                            pool.addPool(s1);
                            pool.addPool(g1);
                            pool.addPool(p1);
                            if (Main.hardMode)
                            {
                                pool.addPool(g2);
                                pool.addPool(p2);
                            }
                            if (NPC.downedPlantBoss)
                            {
                                pool.addPool(g3);
                                pool.addPool(p3);
                            }
                            if (NPC.downedGolemBoss)
                            {
                                pool.addPool(p4);
                            }
                            if (NPC.downedMoonlord)
                            {
                                pool.addPool(p5);
                            }
                            // 灾厄在场读神明吞噬者/丛林龙,缺席回落虚无双子/巡游者
                            if (CECal.DownedDoG(EDownedBosses.downedNihilityTwin))
                            {
                                pool.addPool(p6);
                            }
                            if (CECal.DownedYharon(EDownedBosses.downedCruiser))
                            {
                                pool.addPool(p7);
                            }

                            RewardPoolItem ri = pool.RandomItem();


                            rtype = ri.item;
                            stack = ri.stack;
                            useCd = 16;
                            CEUtils.PlaySound("coininsert", 1, NPC.Center);
                            if (Main.myPlayer == player.whoAmI)
                            {
                                int pj = Projectile.NewProjectile(NPC.GetSource_FromAI(), player.Center - new Vector2(0, 650), new Vector2(0, 16), ModContent.ProjectileType<AtlasItem>(), 0, 0, Main.myPlayer);
                                Main.projectile[pj].Entropy().AtlasItemStack = stack;
                                Main.projectile[pj].Entropy().AtlasItemType = rtype;
                                Main.projectile[pj].netUpdate = true;
                            }

                        }
                    }
                    else if (itemType == ModContent.ItemType<LotteryBox>())
                    {
                        Say("LMDialog14", Color.Blue);
                        textureSpecial = 8;
                        specialTime = 90;
                    }
                    else if (hasBoss)
                    {
                        Say("LMDialog16", Color.Green, 0.7f, bossName);
                    }
                    else if (itemType == ItemID.DirtBlock || itemType == ItemID.StoneBlock || itemType == ItemID.Wood || itemType == ItemID.Mushroom || itemType == ItemID.Gel || itemType == 52)
                    {
                        Say("LMDialog17", Color.Red);
                        textureSpecial = 10;
                        specialTime = 90;
                    }
                    else
                    {
                        Say("LMDialog18", Color.Green, 0.4f);

                    }

                }
            }
        }


        public void Say(string key, Color color, float pitch = 1, string namereplace = "")
        {
            if (Main.dedServ)
            {
                return;
            }

            string text = Mod.GetLocalization(key).ToString().Replace("[NAME]", namereplace);
            int t = CombatText.NewText(NPC.getRect(), color, text);
            Main.combatText[t].lifeTime = 16 * text.Length;
            // 四个啁啾变体按音效表并成自有 beep 的四档基准音高，叠加调用方音高偏移保留原变化感
            SoundStyle s1 = new("CalamityEntropy/Assets/Sounds/beep");
            SoundStyle s2 = new("CalamityEntropy/Assets/Sounds/beep");
            SoundStyle s3 = new("CalamityEntropy/Assets/Sounds/beep");
            SoundStyle s4 = new("CalamityEntropy/Assets/Sounds/beep");
            s1.Pitch = pitch - 1f;
            s2.Pitch = pitch - 1f + 0.15f;
            s3.Pitch = pitch - 1f + 0.3f;
            s4.Pitch = pitch - 1f + 0.45f;
            SoundStyle toPlay = s1;
            int tpl = Main.rand.Next(0, 4);
            if (tpl == 1)
            {
                toPlay = s2;
            }
            else if (tpl == 2)
            {
                toPlay = s3;
            }
            else if (tpl == 3)
            {
                toPlay = s4;
            }

            SoundEngine.PlaySound(toPlay, NPC.Center);
        }
    }
}
