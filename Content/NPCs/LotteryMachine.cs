using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
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

                // 奖池灾厄条目已按杂项处置表 §二 换为自有与原版物品，原版条目保持不动
                s1 = new RewardPool();
                s1.Add(new RewardPoolItem(ItemID.IronBar, 10)); s1.Add(new RewardPoolItem(ItemID.Feather, 10));
                s1.Add(new RewardPoolItem(ModContent.ItemType<AzafureCircuitry>(), 1)); s1.Add(new RewardPoolItem(ItemID.LifeCrystal, 1));
                s1.Add(new RewardPoolItem(ModContent.ItemType<AzafurePlating>(), 4)); s1.Add(new RewardPoolItem(ModContent.ItemType<AzafureCircuitry>(), 4));
                s1.Add(new RewardPoolItem(ItemID.AntlionMandible, 1)); s1.Add(new RewardPoolItem(ItemID.FlinxStaff, 1));
                s1.Add(new RewardPoolItem(ItemID.Diamond, 5));
                s1.Add(new RewardPoolItem(ItemID.Bone, 5));
                s1.Add(new RewardPoolItem(68, 8));
                s1.Add(new RewardPoolItem(ItemID.Bone, 2)); s1.Add(new RewardPoolItem(ItemID.PoopBlock, 10));
                s1.Add(new RewardPoolItem(296, 1));
                s1.Add(new RewardPoolItem(0, 1));
                s1.Add(new RewardPoolItem(ItemID.Heart, 10));
                s1.Add(new RewardPoolItem(ItemID.LesserHealingPotion, 10));
                s1.Add(new RewardPoolItem(ItemID.LesserManaPotion, 10));
                s1.Add(new RewardPoolItem(ItemID.Ruby, 8));

                g1 = new RewardPool();
                g1.Add(new RewardPoolItem(ItemID.SharkFin, 1)); g1.Add(new RewardPoolItem(1320, 1));
                g1.Add(new RewardPoolItem(ItemID.LifeCrystal, 4));
                g1.Add(new RewardPoolItem(ItemID.Shuriken, 100));
                g1.Add(new RewardPoolItem(ItemID.PoisonedKnife, 100));
                g1.Add(new RewardPoolItem(ItemID.GillsPotion, 5)); g1.Add(new RewardPoolItem(1303, 1));
                g1.Add(new RewardPoolItem(1322, 1));
                g1.Add(new RewardPoolItem(ItemID.HealingPotion, 10));
                g1.Add(new RewardPoolItem(ItemID.HeartLantern, 1));
                g1.Add(new RewardPoolItem(ItemID.ManaPotion, 10));
                g1.Add(new RewardPoolItem(ItemID.Ruby, 15));
                g1.Add(new RewardPoolItem(1128, 1));

                p1 = new RewardPool();
                p1.Add(new RewardPoolItem(2341, 1));
                p1.Add(new RewardPoolItem(906, 1));
                p1.Add(new RewardPoolItem(ItemID.Katana, 1)); p1.Add(new RewardPoolItem(2296, 1));
                p1.Add(new RewardPoolItem(ItemID.FallenStar, 300)); p1.Add(new RewardPoolItem(ItemID.ObsidianShield, 1));
                p1.Add(new RewardPoolItem(2430, 1));
                p1.Add(new RewardPoolItem(ItemID.GoldenCrate, 2));
                p1.Add(new RewardPoolItem(ItemID.HealingPotion, 100));
                p1.Add(new RewardPoolItem(ItemID.ManaPotion, 100));
                p1.Add(new RewardPoolItem(ItemID.RoyalGel, 1));
                p1.Add(new RewardPoolItem(ItemID.LifeformAnalyzer, 1));
                p1.Add(new RewardPoolItem(ItemID.LifeCrystal, 8));

                g2 = new RewardPool();
                g2.Add(new RewardPoolItem(ItemID.LifeFruit, 3));
                g2.Add(new RewardPoolItem(ItemID.Amarok, 1));
                g2.Add(new RewardPoolItem(ItemID.Ectoplasm, 2));
                g2.Add(new RewardPoolItem(ItemID.SkyFracture, 1));
                g2.Add(new RewardPoolItem(1518, 1));
                g2.Add(new RewardPoolItem(381, 15));
                g2.Add(new RewardPoolItem(1184, 15));
                g2.Add(new RewardPoolItem(1612, 1));
                g2.Add(new RewardPoolItem(ItemID.UnholyTrident, 1));
                g2.Add(new RewardPoolItem(ItemID.VenusMagnum, 1));
                g2.Add(new RewardPoolItem(ItemID.FrozenTurtleShell, 1));
                g2.Add(new RewardPoolItem(ItemID.Excalibur, 1));
                g2.Add(new RewardPoolItem(ItemID.WrathPotion, 6));

                p2 = new RewardPool();
                p2.Add(new RewardPoolItem(1291, 5));
                p2.Add(new RewardPoolItem(365, 15));
                p2.Add(new RewardPoolItem(1105, 15));
                p2.Add(new RewardPoolItem(1253, 10));
                p2.Add(new RewardPoolItem(ItemID.BeamSword, 1));
                p2.Add(new RewardPoolItem(ItemID.CrystalSerpent, 1));
                p2.Add(new RewardPoolItem(ItemID.ShadowFlameHexDoll, 1));
                p2.Add(new RewardPoolItem(ItemID.SanguineStaff, 1));
                p2.Add(new RewardPoolItem(ItemID.FrostDaggerfish, 150));
                p2.Add(new RewardPoolItem(ItemID.HallowedBar, 3));
                p2.Add(new RewardPoolItem(ItemID.MagmaStone, 1));
                p2.Add(new RewardPoolItem(ItemID.MagicalHarp, 1));
                p2.Add(new RewardPoolItem(ItemID.OnyxBlaster, 1));
                p2.Add(new RewardPoolItem(ItemID.Ectoplasm, 2));

                g3 = new RewardPool();
                g3.Add(new RewardPoolItem(ItemID.OpticStaff, 1));
                g3.Add(new RewardPoolItem(ItemID.ChlorophyteBar, 10));
                g3.Add(new RewardPoolItem(ItemID.LifeFruit, 5));
                g3.Add(new RewardPoolItem(1006, 30));
                g3.Add(new RewardPoolItem(1551, 1));
                g3.Add(new RewardPoolItem(3018, 1));
                g3.Add(new RewardPoolItem(3021, 1));
                g3.Add(new RewardPoolItem(ItemID.CelestialShell, 1));
                g3.Add(new RewardPoolItem(ItemID.CharmofMyths, 1));
                g3.Add(new RewardPoolItem(ItemID.CelestialStone, 1));
                g3.Add(new RewardPoolItem(ItemID.ArcticDivingGear, 1));

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
                p3.Add(new RewardPoolItem(ItemID.Tsunami, 1));
                p3.Add(new RewardPoolItem(ItemID.Marrow, 1));
                p3.Add(new RewardPoolItem(ItemID.TacticalShotgun, 1));
                p3.Add(new RewardPoolItem(ItemID.RazorbladeTyphoon, 1));
                p3.Add(new RewardPoolItem(ItemID.PaladinsHammer, 1));
                p3.Add(new RewardPoolItem(ItemID.ChlorophyteBar, 20));
                p3.Add(new RewardPoolItem(ItemID.FishronWings, 1));

                p4 = new RewardPool();
                p4.Add(new RewardPoolItem(3110, 1));
                p4.Add(new RewardPoolItem(1248, 1));
                p4.Add(new RewardPoolItem(1343, 1));
                p4.Add(new RewardPoolItem(1858, 1));
                p4.Add(new RewardPoolItem(3883, 1));
                p4.Add(new RewardPoolItem(3817, 80));
                p4.Add(new RewardPoolItem(ItemID.VampireKnives, 1));
                p4.Add(new RewardPoolItem(ItemID.BeetleHusk, 5));
                p4.Add(new RewardPoolItem(ItemID.Nanites, 50));
                p4.Add(new RewardPoolItem(ItemID.TerraBlade, 1));
                p4.Add(new RewardPoolItem(ItemID.NorthPole, 1));
                p4.Add(new RewardPoolItem(ItemID.SpectreBar, 25));
                p4.Add(new RewardPoolItem(ItemID.PossessedHatchet, 1));
                p4.Add(new RewardPoolItem(ItemID.StakeLauncher, 1));
                p4.Add(new RewardPoolItem(ItemID.TheEyeOfCthulhu, 1));
                p4.Add(new RewardPoolItem(ItemID.SniperRifle, 1));

                p5 = new RewardPool();
                p5.Add(new RewardPoolItem(ItemID.LunarBar, 15));
                p5.Add(new RewardPoolItem(ItemID.FragmentSolar, 15));
                p5.Add(new RewardPoolItem(ItemID.Meowmere, 1));
                p5.Add(new RewardPoolItem(ItemID.Phantasm, 1));
                p5.Add(new RewardPoolItem(ItemID.NebulaBlaze, 1));
                p5.Add(new RewardPoolItem(ItemID.MasterNinjaGear, 1));
                p5.Add(new RewardPoolItem(ItemID.InfluxWaver, 1));
                p5.Add(new RewardPoolItem(ItemID.StarWrath, 1));
                p5.Add(new RewardPoolItem(ItemID.CelestialEmblem, 1));
                p5.Add(new RewardPoolItem(ItemID.LunarOre, 30));
                p5.Add(new RewardPoolItem(ItemID.VortexBeater, 1));
                p5.Add(new RewardPoolItem(ItemID.BlackBelt, 1));
                p5.Add(new RewardPoolItem(ItemID.NightVisionHelmet, 1));
                p5.Add(new RewardPoolItem(ItemID.RainbowCrystalStaff, 1));
                p5.Add(new RewardPoolItem(ItemID.LastPrism, 1));

                // 原 VoidEaterMarionette/MirrorofKalandra/Riftburst/Omicron 实为灾厄物品（处置表误标自有），
                // 按材料表表外兜底规则以月总掉落同职业替换
                p6 = new RewardPool();
                p6.Add(new RewardPoolItem(ItemID.SpookyWood, 99));
                p6.Add(new RewardPoolItem(ItemID.FrostCore, 5));
                p6.Add(new RewardPoolItem(ItemID.FragmentSolar, 25));
                p6.Add(new RewardPoolItem(ItemID.SuperHealingPotion, 15));
                p6.Add(new RewardPoolItem(ModContent.ItemType<WraithSoulEssence>(), 20));
                p6.Add(new RewardPoolItem(ItemID.Meowmere, 1));
                p6.Add(new RewardPoolItem(ItemID.Terrarian, 1));
                p6.Add(new RewardPoolItem(ItemID.StardustDragonStaff, 1));
                p6.Add(new RewardPoolItem(ItemID.MoonlordTurretStaff, 1));
                p6.Add(new RewardPoolItem(ItemID.SDMG, 1));
                p6.Add(new RewardPoolItem(ItemID.LastPrism, 1));

                p7 = new RewardPool();
                p7.Add(new RewardPoolItem(ItemID.Celeb2, 1));
                p7.Add(new RewardPoolItem(ModContent.ItemType<AzafureCircuitry>(), 30));
                p7.Add(new RewardPoolItem(ModContent.ItemType<VoidBar>(), 10));
                p7.Add(new RewardPoolItem(ModContent.ItemType<WraithSoulEssence>(), 15));
                p7.Add(new RewardPoolItem(ModContent.ItemType<VoidOre>(), 100));
                p7.Add(new RewardPoolItem(ItemID.SDMG, 1));
                p7.Add(new RewardPoolItem(ModContent.ItemType<FinalFractal>(), 1));
                //飞龙剑的原版内部名为DD2SquireBetsySword
                p7.Add(new RewardPoolItem(ItemID.DD2SquireBetsySword, 1));
                p7.Add(new RewardPoolItem(ModContent.ItemType<FlowingLight>(), 1));
                p7.Add(new RewardPoolItem(ModContent.ItemType<VoidScales>(), 10));
                p7.Add(new RewardPoolItem(ModContent.ItemType<VoidBar>(), 5));
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
                    else if (itemType == ModContent.ItemType<VoidOre>())
                    {
                        // 彩蛋判定改为手持自有虚空矿，文案不变
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
                            // 池门槛按进度表：p6 原挂深渊亡魂，该 Boss 移除后回落到虚无双子；p7 巡游者后
                            if (EDownedBosses.downedNihilityTwin)
                            {
                                pool.addPool(p6);
                            }
                            if (EDownedBosses.downedCruiser)
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
