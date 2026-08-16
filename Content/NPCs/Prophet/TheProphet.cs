using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Prophet;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Particles;
using CalamityMod.World;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    [AutoloadBossHead]
    public class TheProphet : ModNPC
    {
        public float finRotCounter = 0;
        public bool music2 = false;
        public class TailPoint
        {
            public int timeLeft = 20;
            public Vector2 position;
            public Vector2 velocity;
            public TailPoint(Vector2 pos, Vector2 vel)
            {
                position = pos;
                velocity = vel;
            }
            public void update()
            {
                position += velocity;
                velocity *= 0.96f;
                timeLeft--;
            }
        }
        public List<TailPoint> tail = new();
        public OlderCruiserAIGNPC zenithAI = new OlderCruiserAIGNPC();
        public override void BossLoot(ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ProphetBag>()));

            npcLoot.DefineConditionalDropSet(() => true).Add(DropHelper.PerPlayer(ItemID.GreaterHealingPotion, 1, 5, 15), hideLootReport: true);


            var normalOnly = npcLoot.DefineNormalOnlyDropSet();
            {
                normalOnly.Add(ModContent.ItemType<RuneSong>(), new Fraction(4, 5));
                normalOnly.Add(ModContent.ItemType<UrnOfSouls>(), new Fraction(4, 5));
                normalOnly.Add(ModContent.ItemType<SpiritBanner>(), new Fraction(4, 5));
                normalOnly.Add(ModContent.ItemType<RuneMachineGun>(), new Fraction(4, 5));
                normalOnly.Add(ModContent.ItemType<ProphecyFlyingKnife>(), new Fraction(4, 5));
                normalOnly.Add(ModContent.ItemType<ForeseeOrb>(), new Fraction(4, 5));
                normalOnly.Add(ModContent.ItemType<RuneWing>(), new Fraction(4, 5));
                normalOnly.Add(ModContent.ItemType<ForeseeWhip>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<BookMarkForesee>(), new Fraction(2, 5));
                normalOnly.Add(ModContent.ItemType<CursedThread>(), 1);
            }
            npcLoot.DefineConditionalDropSet(DropHelper.RevAndMaster).Add(ModContent.ItemType<ProphetRelic>());

            npcLoot.Add(ModContent.ItemType<ProphetTrophy>(), 10);

            npcLoot.AddConditionalPerPlayer(() => !EDownedBosses.downedProphet, ModContent.ItemType<ProphetLore>());
        }
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = "CalamityEntropy/Assets/BCL/Prophet",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -66
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheDungeon,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.ProphetBestiary")
            });
        }
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            if (Main.zenithWorld)
                return zenithAI.aitype != 3f;
            if (AIStyle == 1 || AIStyle == 11)
                return true;
            return false;
        }
        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsOverPlayers.Add(index);
        }
        public override void SetDefaults()
        {
            NPC.boss = true;
            NPC.width = 80;
            NPC.height = 80;
            NPC.damage = 68;
            NPC.Calamity().DR = 0.10f;
            NPC.lifeMax = 48000;
            if (BossRushEvent.BossRushActive)
            {
                NPC.lifeMax += 350000;
            }
            if (CalamityWorld.death)
            {
                NPC.damage += 4;
            }
            else if (CalamityWorld.revenge)
            {
                NPC.damage += 2;
            }
            var snd = CEUtils.GetSound("prophet_hurt", maxIns: 1);
            var snd2 = CEUtils.GetSound("prophet_death");
            NPC.HitSound = snd;
            NPC.DeathSound = snd2;
            NPC.value = 2000f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.25f;
            NPC.dontCountMe = true;
            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/SpectralForesight");
            }
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (AIStyle == 8)
            {
                modifiers.FinalDamage *= 0.5f;
            }
        }
        public float dr = 0.26f;
        public void UpdateFins()
        {
            finRotCounter += NPC.velocity.Length() * 0.001f + 0.008f;
            if (finRotCounter > 1)
            {
                finRotCounter--;
            }
            NPC.localAI[1]++;
        }
        public float rl = 0;
        public int NoEnrange = 300;
        public override void AI()
        {
            rl = CEUtils.RotateTowardsAngle(rl, NPC.rotation, 0.1f, false);
            UpdateFins();
            if (!Main.dedServ)
            {
                if (phase == 2 && !music2)
                {
                    music2 = true;
                    Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Prophet2");
                }
            }
            if (Main.zenithWorld)
            {
                zenithAI.PreAI(NPC);
                UpdateTails();
                return;
            }
            if (dr > 0)
            {
                dr -= 0.5f / (160 * 60);
            }
            NPC.Calamity().CurrentlyIncreasingDefenseOrDR = AIStyle == 8;
            if (AIStyle == 8)
            {
                NPC.Calamity().DR = 0.50f;
            }
            else { NPC.Calamity().DR = 0.12f; }
            NPC.Calamity().DR += dr;
            if (spawnAnm > 0)
            {
                NPC.dontTakeDamage = true;
                NPC.rotation = MathHelper.PiOver2 * -1;
            }
            if (spawnAnm == 0)
            {
                NPC.dontTakeDamage = false;
            }


            spawnAnm--;
            if (!NPC.HasValidTarget)
            {
                NPC.TargetClosest();
            }
            if (spawnAnm <= 0)
            {
                if (!NPC.HasValidTarget)
                {
                    NPC.localAI[0]++;
                    NPC.velocity.Y -= 0.8f;
                    NPC.velocity *= 0.96f;
                    NPC.rotation = NPC.velocity.ToRotation();
                    if (NPC.localAI[0] > 180)
                    {
                        NPC.active = false;
                        NPC.netUpdate = true;
                    }
                }
                else
                {
                    NPC.localAI[0] = 0;
                    Player target = NPC.target.ToPlayer();
                    NPC.Calamity().CurrentlyEnraged = false;
                    AttackPlayer(target);
                }
                UpdateTails();
            }
        }

        public override bool CheckActive()
        {
            return false;
        }
        public void UpdateTails()
        {
            foreach (TailPoint p in tail)
            {
                p.update();
            }
            if (tail.Count > 0)
            {
                if (tail[0].timeLeft <= 0)
                {
                    tail.RemoveAt(0);
                }
            }
            tail.Add(new TailPoint(NPC.Center - rl.ToRotationVector2() * 26, (rl.ToRotationVector2() * -16) + rl.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * (float)(Math.Sin(Main.GameUpdateCount * 0.1f) * 6)));

        }

        public int AIStyle = 0;
        public int AIChangeDelay = 0;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AIStyle);
            writer.Write(AIChangeDelay);
            if (Main.zenithWorld)
                zenithAI.SendExtraAI(NPC, writer);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AIStyle = reader.ReadInt32();
            AIChangeDelay = reader.ReadInt32();
            if (Main.zenithWorld)
                zenithAI.ReceiveExtraAI(NPC, reader);
        }
        public int phase = 1;
        public int spawnAnm = 120;
        public int GetAIType(int r)
        {
            switch (r)
            {
                case 0: return 0;
                case 1: return 3;
                case 2: return 10;
                case 3: return 1;
                case 4: return Main.rand.NextBool() ? 6 : 7;
                case 5: return 9;
                case 6: return Main.rand.NextBool() ? 2 : 4;
                case 7: return Main.rand.NextBool() ? 8 : 11;
            }
            return 0;
        }
        public int AIC = -1;
        public void AttackPlayer(Player target)
        {
            if (target.ZoneDungeon || BossRushEvent.BossRushActive)
            {
                NoEnrange = 500;
            }
            NoEnrange--;
            if (NoEnrange < 0)
            {
                NPC.Calamity().CurrentlyEnraged = true;
            }
            if (NPC.life < NPC.lifeMax / 2)
            {
                phase = 2;
            }
            float difficult = 1;
            if (Main.expertMode)
            {
                difficult += 0.06f;
            }
            if (Main.masterMode)
            {
                difficult += 0.06f;
            }
            if (CalamityWorld.revenge)
            {
                difficult += 0.1f;
            }
            if (CalamityWorld.death)
            {
                difficult += 0.1f;
            }
            if (Main.getGoodWorld)
            {
                difficult += 0.15f;
            }
            if (Main.zenithWorld)
            {
                difficult += 0.15f;
            }
            difficult *= 1 + ((float)NPC.life / NPC.lifeMax) * 0.2f;
            if (AIChangeDelay <= 0)
            {
                AIC++;
                if (AIC > 7)
                {
                    AIC = 0;
                }
                AIStyle = GetAIType(AIC);
                if (AIStyle == 0)
                {
                    AIChangeDelay = 240;
                }
                if (AIStyle == 1)
                {
                    AIChangeDelay = 220;
                }
                if (AIStyle == 2)
                {
                    AIChangeDelay = 120 + 60 * 4;
                }
                if (AIStyle == 3)
                {
                    AIChangeDelay = 100;
                }
                if (AIStyle == 4)
                {
                    AIChangeDelay = 150;
                }
                if (AIStyle == 5)
                {
                    AIChangeDelay = 245;
                }
                if (AIStyle == 6)
                {
                    AIChangeDelay = 142;
                }
                if (AIStyle == 7)
                {
                    AIChangeDelay = 280;
                }
                if (AIStyle == 8)
                {
                    AIChangeDelay = 560;
                }
                if (AIStyle == 9)
                {
                    AIChangeDelay = 160;
                }
                if (AIStyle == 10)
                {
                    AIChangeDelay = 320;
                }
                if (AIStyle == 11)
                {
                    AIChangeDelay = 242;
                }
                NPC.netUpdate = true;
            }
            if (AIChangeDelay > 0)
            {
                if (AIStyle == 0) //4轮符文弹
                {
                    NPC.velocity *= 0.96f;
                    NPC.velocity += (NPC.Center - target.Center).normalize().RotatedBy(MathHelper.PiOver2) * (NPC.Center.X < target.Center.X ? 0.1f : -0.1f);
                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (target.Center - NPC.Center).ToRotation(), 0.6f, false);
                    if (AIChangeDelay >= 30 && AIChangeDelay % (phase == 1 ? 60 : 46) == 0)
                    {
                        TeleportTo(target.Center + target.velocity.SafeNormalize(CEUtils.randomRot().ToRotationVector2()) * 1250 / difficult);
                    }
                    if (AIChangeDelay >= 30 && AIChangeDelay % (phase == 1 ? 60 : 50) == (phase == 1 ? 56 : 46))
                    {
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/CometShardUse"), NPC.Center);
                            CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i <= (phase == 1 ? 2 : 3); i++)
                            {
                                if (i == 0)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize() * difficult, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 6 * difficult, 1);
                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * (phase == 1 ? 0.5f : 0.36f)) * difficult, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 6 * difficult, 1);
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * (phase == 1 ? 0.5f : 0.36f) * -1) * difficult, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 6 * difficult, 1);
                                }
                            }
                        }
                        NPC.velocity += (NPC.Center - target.Center).normalize() * 9;
                    }
                }
                if (AIStyle == 1)//冲刺
                {
                    if (AIChangeDelay == 220 || AIChangeDelay == 160 || AIChangeDelay == 100)
                    {
                        NPC.rotation = (target.Center + target.velocity * 12 - NPC.Center).ToRotation();
                        NPC.velocity += NPC.rotation.ToRotationVector2() * -(AIChangeDelay == 100 ? 16 : 6);
                        NPC.ai[1] = AIChangeDelay == 100 ? 80 : 46;
                        if (phase > 1)
                        {
                            if (!Main.dedServ)
                            {
                                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/CometShardUse"), NPC.Center);
                                CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
                            }
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i <= (AIChangeDelay == 100 ? 2 : 1); i++)
                                {
                                    if (i == 0)
                                    {
                                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize() * difficult * 2, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 6 * difficult, 1);
                                    }
                                    else
                                    {
                                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * 0.44f) * difficult * 2, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 6 * difficult, 1);
                                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * -0.44f) * difficult * 2, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 6 * difficult, 1);
                                    }
                                }
                            }
                        }
                        if (this.trail == null || this.trail.Lifetime < 1)
                        {
                            this.trail = new ProminenceTrail() { color1 = Color.DeepSkyBlue, color2 = Color.White, maxLength = 14 };
                            EParticle.NewParticle(this.trail, NPC.Center, Vector2.Zero, Color.White, 7f, 1, true, BlendState.AlphaBlend, 0);
                        }
                    }
                    if (NPC.ai[1] > 0)
                    {
                        NPC.ai[1]--;
                        NPC.velocity += NPC.rotation.ToRotationVector2() * difficult * (AIChangeDelay <= 100 ? 1.6f : 1) * (phase == 1 ? 1 : 1.4f);
                        NPC.velocity *= 0.98f;
                        if (this.trail != null)
                            this.trail.Lifetime = 13;
                        if (AIChangeDelay < 100)
                        {
                            if (NPC.ai[1] < 64 && AIChangeDelay % (int)((phase == 1 ? 10 : 8) / difficult) == 0)
                            {
                                CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3), Main.rand.NextFloat(0.7f, 1.3f), NPC.Center);
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.velocity.RotatedBy(MathHelper.PiOver2) * 0.15f * difficult, ModContent.ProjectileType<RuneBulletHostile>(), NPC.damage / 6, 2, -1, 20 * difficult);
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.velocity.RotatedBy(-MathHelper.PiOver2) * 0.15f * difficult, ModContent.ProjectileType<RuneBulletHostile>(), NPC.damage / 6, 2, -1, 20 * difficult);
                                }
                            }
                        }
                    }
                    else
                    {
                        NPC.velocity *= 0.8f;
                    }
                    if (AIChangeDelay == 10)
                    {
                        TeleportTo(target.Center + CEUtils.randomRot().ToRotationVector2() * 1000);
                    }
                }
                if (AIStyle == 2)//符文晶簇
                {
                    NPC.velocity *= 0.98f;
                    if (AIChangeDelay < 120)
                    {
                        NPC.velocity += (NPC.Center - (target.Center + (NPC.Center - target.Center).normalize().RotatedBy(0.4f) * 400)).normalize() * 1;
                    }
                    NPC.rotation = NPC.velocity.ToRotation();
                    if (AIChangeDelay >= 110 && AIChangeDelay % 60 == 0)
                    {
                        TeleportTo(target.Center + target.velocity.SafeNormalize(CEUtils.randomRot().ToRotationVector2()) * 900 / difficult);
                        NPC.velocity = (target.Center - NPC.Center).normalize() * 8;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (AIChangeDelay >= 110 && AIChangeDelay % 60 == 30)
                        {
                            for (int i = 0; i < (phase == 1 ? 1 : 2); i++)
                            {
                                if (i == 0)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.rotation.ToRotationVector2() * 20, ModContent.ProjectileType<RuneCrystalTop>(), NPC.damage / 6 - 5, 4);
                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.rotation.ToRotationVector2().RotatedBy(0.4f * i) * 20, ModContent.ProjectileType<RuneCrystalTop>(), NPC.damage / 6 - 5, 4);
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.rotation.ToRotationVector2().RotatedBy(-0.4f * i) * 20, ModContent.ProjectileType<RuneCrystalTop>(), NPC.damage / 6 - 5, 4);
                                }
                            }
                        }
                    }
                }
                if (AIStyle == 3)//2层符文洪流
                {
                    NPC.rotation = (target.Center - NPC.Center).ToRotation();
                    if (AIChangeDelay == 88)
                    {
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/CometShardUse"), NPC.Center);
                            CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
                        }
                        TeleportTo(target.Center + target.velocity.SafeNormalize(CEUtils.randomRot().ToRotationVector2()) * 900 / difficult);
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (AIChangeDelay == 77)
                        {
                            for (int i = 0; i <= (phase == 1 ? 4 : 6); i++)
                            {
                                if (i == 0)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize() * difficult * 0.8f, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 6 * difficult, 1);
                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * (phase == 1 ? 0.38f : 0.32f)) * difficult * 0.8f, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 5 * difficult, 1);
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * -(phase == 1 ? 0.38f : 0.32f)) * difficult * 0.8f, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 5 * difficult, 1);
                                }
                            }
                            for (float i = 0.5f; i <= (phase == 1 ? 5.5f : 7.5f); i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * (phase == 1 ? 0.38f : 0.32f)) * difficult * 0.5f, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 5 * difficult, 1);
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * -(phase == 1 ? 0.38f : 0.32f)) * difficult * 0.5f, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 5 * difficult, 1);

                            }
                        }
                    }
                }
                if (AIStyle == 4)//速射符文洪流
                {
                    NPC.rotation = (target.Center - NPC.Center).ToRotation();
                    if (AIChangeDelay == 149)
                    {
                        TeleportTo(target.Center + target.velocity.SafeNormalize(CEUtils.randomRot().ToRotationVector2()) * 900 / difficult);
                        NPC.velocity = (target.Center - NPC.Center).normalize() * 1;
                    }
                    if (AIChangeDelay > 80)
                    {
                        if (AIChangeDelay < 100)
                        {
                            if (AIChangeDelay % 4 == 0)
                            {
                                CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3), Main.rand.NextFloat(0.7f, 1.3f), NPC.Center);
                            }
                            if (Main.netMode != NetmodeID.MultiplayerClient && AIChangeDelay % 2 == 0)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedByRandom((phase == 1 ? 0.1f : 0.16f)) * difficult * 1.8f, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 12 * difficult);
                            }
                        }
                        else
                        {
                            if (AIChangeDelay < 120)
                            {
                                if (AIChangeDelay % 6 == 0)
                                {
                                    CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 3), Main.rand.NextFloat(0.7f, 1.3f), NPC.Center);
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedByRandom((phase == 1 ? 0.16f : 0.4f)) * difficult * 1.5f, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 12 * difficult);
                                    }
                                }
                            }
                        }
                    }
                }
                if (AIStyle == 5)//环状符文
                {
                    if (AIChangeDelay > 160)
                    {
                        if (AIChangeDelay % (phase == 1 ? 4 : 3) == 0)
                        {
                            TeleportTo(target.Center + CEUtils.randomRot().ToRotationVector2() * 1400 / difficult);
                            CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize() * difficult * 0.4f, ModContent.ProjectileType<RuneTorrent>(), NPC.damage / 6, 4, -1, 12 * difficult);
                            }
                            NPC.rotation = (target.Center - NPC.Center).ToRotation();
                        }
                    }
                    else
                    {
                        NPC.rotation = NPC.velocity.ToRotation();
                        NPC.velocity *= 0.96f;
                        NPC.velocity += (target.Center - NPC.Center).normalize() * 0.5f;
                    }
                }
                if (AIStyle == 6)//符文光球
                {
                    if (AIChangeDelay == 140)
                    {
                        TeleportTo(target.Center + CEUtils.randomRot().ToRotationVector2() * 800);
                    }
                    if (AIChangeDelay == 130)
                    {
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/CometShardUse"), NPC.Center);
                            CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
                        }
                        for (float i = 0; i < 360; i += (phase == 1 ? 60 : 40))
                        {
                            float rot = MathHelper.ToRadians(i);
                            float impactParticleScale = 4f;
                            var impactParticle2 = new SparkleParticle(NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, Color.White, Color.SkyBlue, impactParticleScale * 1.2f, 12, 0, 4.5f);
                            GeneralParticleHandler.SpawnParticle(impactParticle2);
                            var impactParticle = new SparkleParticle(NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, Color.SkyBlue, Color.SkyBlue, impactParticleScale, 10, 0, 3f);
                            GeneralParticleHandler.SpawnParticle(impactParticle);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, ModContent.ProjectileType<ProphetRune>(), NPC.damage / 6, 4, -1, NPC.whoAmI, rot, Main.rand.Next(1, 12));
                            }
                        }
                    }
                    if (AIChangeDelay == 120 && phase > 1)
                    {
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/CometShardUse"), NPC.Center);
                            CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
                        }
                        for (float i = 0; i < 360; i += (phase == 1 ? 60 : 40))
                        {
                            float rot = MathHelper.ToRadians(i);
                            float impactParticleScale = 4f;
                            var impactParticle2 = new SparkleParticle(NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, Color.White, Color.SkyBlue, impactParticleScale * 1.2f, 12, 0, 4.5f);
                            GeneralParticleHandler.SpawnParticle(impactParticle2);
                            var impactParticle = new SparkleParticle(NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, Color.SkyBlue, Color.SkyBlue, impactParticleScale, 10, 0, 3f);
                            GeneralParticleHandler.SpawnParticle(impactParticle);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, ModContent.ProjectileType<ProphetRune>(), NPC.damage / 6, 4, -1, NPC.whoAmI, rot, Main.rand.Next(1, 12));
                            }
                        }

                    }
                    if (AIChangeDelay == 110 && phase > 1)
                    {
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/CometShardUse"), NPC.Center);
                            CEUtils.PlaySound("crystedge_spawn_crystal", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
                        }
                        for (float i = 0; i < 360; i += (phase == 1 ? 60 : 40))
                        {
                            float rot = MathHelper.ToRadians(i);
                            float impactParticleScale = 4f;
                            var impactParticle2 = new SparkleParticle(NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, Color.White, Color.SkyBlue, impactParticleScale * 1.2f, 12, 0, 4.5f);
                            GeneralParticleHandler.SpawnParticle(impactParticle2);
                            var impactParticle = new SparkleParticle(NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, Color.SkyBlue, Color.SkyBlue, impactParticleScale, 10, 0, 3f);
                            GeneralParticleHandler.SpawnParticle(impactParticle);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + rot.ToRotationVector2() * 86, Vector2.Zero, ModContent.ProjectileType<ProphetRune>(), NPC.damage / 6, 4, -1, NPC.whoAmI, rot, Main.rand.Next(1, 12));
                            }
                        }
                    }
                    NPC.rotation = NPC.velocity.ToRotation();
                    NPC.velocity += (target.Center - NPC.Center).normalize() * 0.2f;
                    NPC.velocity *= 0.96f;
                }
                if (AIStyle == 7)//符文冲击
                {
                    bool flag = true;
                    if (AIChangeDelay > 50)
                    {
                        if (AIChangeDelay % 40 > 20)
                        {
                        }
                        else
                        {
                            flag = false;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (AIChangeDelay % 40 == 20)
                                {
                                    for (int i = 0; i <= (phase == 1 ? 2 : 4); i++)
                                    {
                                        if (i == 0)
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize() * 16, ModContent.ProjectileType<ProphetLightning>(), NPC.damage / 6, 4);
                                        }
                                        else
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * (phase == 1 ? 0.38f : 0.32f)) * 16, ModContent.ProjectileType<ProphetLightning>(), NPC.damage / 6, 4);
                                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (target.Center - NPC.Center).normalize().RotatedBy(i * -(phase == 1 ? 0.38f : 0.32f)) * 16, ModContent.ProjectileType<ProphetLightning>(), NPC.damage / 6, 4);
                                        }
                                    }
                                }
                            }
                            NPC.velocity *= 0.94f;
                        }
                    }
                    if (flag)
                    {
                        NPC.velocity = (target.Center + (NPC.Center - target.Center).normalize() * 160 - NPC.Center) * 0.08f;
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, NPC.velocity.ToRotation(), 0.06f, false);
                    }
                }
                if (AIStyle == 8)//大激光
                {
                    NPC.velocity *= 0.9f;
                    NPC.rotation = (target.Center - NPC.Center).ToRotation();
                    if (AIChangeDelay == 556)
                    {
                        int type = ModContent.ProjectileType<RuneCrystalTop>();
                        foreach (Projectile p in Main.ActiveProjectiles)
                        {
                            if (p.type == type)
                            {
                                p.Kill();
                            }
                        }
                        NPC.velocity *= 0;
                        TeleportTo(EDownedBosses.GetDungeonArchiveCenterPos() + new Vector2(0, 80));
                        if (CEUtils.getDistance(NPC.Center, NPC.target.ToPlayer().Center) > 1600)
                            TeleportTo(target.Center - target.velocity.SafeNormalize(-Vector2.UnitY) * 300);
                    }
                    if (AIChangeDelay > 480)
                    {
                        NPC.velocity *= 0.96f;
                        foreach (var plr in Main.ActivePlayers)
                        {
                            if (plr.Distance(NPC.Center) < 2400)
                            {
                                if (plr.Distance(NPC.Center + new Vector2(0, -80)) > 560)
                                {
                                    plr.Entropy().immune = 20;
                                    plr.wingTime = plr.wingTimeMax;
                                    plr.velocity = (NPC.Center + new Vector2(0, -80) - plr.Center).normalize() * 10;
                                    plr.Center += (NPC.Center + new Vector2(0, -80) - plr.Center).normalize() * 36;
                                    for (int i = 0; i < 8; i++)
                                        EParticle.spawnNew(new RuneParticle(), CEUtils.randomPoint(plr.getRect()), Vector2.Zero, Color.LightBlue, 1, 1, true, BlendState.Additive, 0, 40);
                                }
                            }
                        }
                    }
                    if (AIChangeDelay > 60 && AIChangeDelay < 480)
                    {
                        foreach (var plr in Main.ActivePlayers)
                        {
                            if (plr.Distance(NPC.Center) < 2400)
                            {
                                plr.wingTime = plr.wingTimeMax;
                                plr.Calamity().infiniteFlight = true;
                            }
                        }
                    }
                    if (AIChangeDelay == 480)
                    {
                        if (CEUtils.CheckSolidTile(NPC.Center.getRectCentered(250, 250)))
                        {
                            AIChangeDelay = 30;
                        }
                        else
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(0, -80), (target.Center - (NPC.Center + new Vector2(0, -80))).normalize() * 0.7f, ModContent.ProjectileType<FableEye>(), NPC.damage / 5, 4);
                            }
                        }
                    }
                }
                if (AIStyle == 9)//符文飞匕
                {
                    NPC.rotation = (target.Center - NPC.Center).ToRotation();
                    if (AIChangeDelay == 160)
                    {
                        TeleportTo(target.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(500, 600));
                    }
                    if (AIChangeDelay > 60)
                    {
                        if (AIChangeDelay % 5 == 0)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, CEUtils.randomPointInCircle(10), ModContent.ProjectileType<RuneSword>(), NPC.damage / 6, 4);
                            }
                        }
                    }
                }
                if (AIStyle == 10) // 虚空触手
                {
                    if (AIChangeDelay == 310)
                    {
                        TeleportTo(target.Center + CEUtils.randomRot().ToRotationVector2() * 400);
                    }
                    if (AIChangeDelay > 140 && AIChangeDelay % (phase == 1 ? 46 : 36) == 0)
                    {
                        float r = CEUtils.randomRot();
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (float i = 0; i < 360; i += (phase == 1 ? 36 : 30))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (r + MathHelper.ToRadians(i)).ToRotationVector2().RotatedByRandom(0.2f) * 8, ModContent.ProjectileType<ProphetVoidSpike>(), NPC.damage / 6, 4, -1, 0, NPC.whoAmI);
                            }
                        }
                    }
                    NPC.velocity = (target.Center - NPC.Center) * 0.008f;
                    NPC.rotation = NPC.velocity.ToRotation();
                }
                if (AIStyle == 11)
                {
                    if (AIChangeDelay == 239)
                    {
                        TeleportTo(target.Center + CEUtils.randomRot().ToRotationVector2() * 1400 / difficult);

                    }
                    if (AIChangeDelay == 228)
                    {
                        float r = CEUtils.randomRot();
                        for (float i = 0; i < 358; i += (phase == 1 ? 72 : 60))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ProphetRuneAlt>(), NPC.damage / 6, 2, -1, NPC.whoAmI, r + MathHelper.ToRadians(i), Main.rand.Next(1, 12));
                        }
                    }
                    if (AIChangeDelay == 210 || AIChangeDelay == 140 || AIChangeDelay == 70)
                    {
                        NPC.rotation = (target.Center + target.velocity * 8 - NPC.Center).ToRotation();
                        NPC.ai[1] = 62;
                        NPC.velocity = NPC.rotation.ToRotationVector2() * difficult * (phase == 1 ? 0.86f : 1.1f) * 18;

                    }
                    if (NPC.ai[1] > 0)
                    {
                        NPC.ai[1]--;
                        NPC.velocity *= 0.99f;
                    }
                    else
                    {
                        NPC.velocity *= 0.8f;
                    }
                }
            }
            else
            {
                NPC.rotation = NPC.velocity.ToRotation();
                NPC.velocity *= 0.98f;
                NPC.velocity += (target.Center - NPC.Center).SafeNormalize(Vector2.Zero);
            }
            this.trail?.AddPoint(NPC.Center + NPC.rotation.ToRotationVector2() * (NPC.velocity.Length() + 60));
            AIChangeDelay--;
            NPC.netUpdate = true;
            if (Main.netMode == NetmodeID.Server)
            {
                NPC.netSpam = 0;
            }
        }
        public ProminenceTrail trail = null;
        public void TeleportTo(Vector2 pos)
        {

            NPC.velocity *= 0;
            Color impactColor = Main.rand.NextBool(3) ? Color.SkyBlue : Color.White;
            float impactParticleScale = 5.6f;

            SparkleParticle impactParticle2 = new SparkleParticle(NPC.Center, Vector2.Zero, Color.White, Color.SkyBlue, impactParticleScale * 1.2f, 12, 0, 4.5f);
            GeneralParticleHandler.SpawnParticle(impactParticle2);
            SparkleParticle impactParticle = new SparkleParticle(NPC.Center, Vector2.Zero, impactColor, Color.SkyBlue, impactParticleScale, 10, 0, 3f);
            GeneralParticleHandler.SpawnParticle(impactParticle);

            NPC.Center = pos;

            impactParticle2 = new SparkleParticle(NPC.Center, Vector2.Zero, Color.White, Color.SkyBlue, impactParticleScale * 1.2f, 12, 0, 4.5f);
            GeneralParticleHandler.SpawnParticle(impactParticle2);
            impactParticle = new SparkleParticle(NPC.Center, Vector2.Zero, impactColor, Color.SkyBlue, impactParticleScale, 10, 0, 3f);
            GeneralParticleHandler.SpawnParticle(impactParticle);

            tail.Clear();
        }
        public List<Vector2> GP(float distAdd = 0, float c = 1)
        {
            float dist = distAdd;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 60; i++)
            {
                points.Add(new Vector2(dist, 0).RotatedBy(MathHelper.ToRadians(i * 6 - 80 * c * Main.GlobalTimeWrappedHourly)));
            }
            return points;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.zenithWorld)
            {
                DrawTail();
                DrawFins();
                return zenithAI.PreDraw(NPC, spriteBatch, screenPos, drawColor);
            }
            else
            {
                Draw();
            }
            return false;
        }
        public void Draw()
        {
            if (Main.zenithWorld)
                spawnAnm = 0;
            if (spawnAnm < 60)
            {
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
                DrawTail();
                DrawFins();
                Texture2D tex = TextureAssets.Npc[NPC.type].Value;
                Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition, null, Color.White, rl + MathHelper.PiOver2, tex.Size() / 2, NPC.scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            }
            if (spawnAnm > 0)
            {
                float a = (float)Math.Sin(spawnAnm / 120f * MathHelper.Pi);
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<Vector2> points = GP(0);
                    List<Vector2> pointsOutside = GP(360 * a);
                    int i;
                    for (i = 0; i < points.Count; i++)
                    {
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + points[i],
                        new Vector3((float)i / points.Count, 1, 0.9f),
                              Color.SkyBlue * a));
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + pointsOutside[i],
                              new Vector3((float)i / points.Count, 0, 0.9f),
                              Color.SkyBlue * a));

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3)
                    {
                        Texture2D tx = CEUtils.getExtraTex("AbyssalCircle2");
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<Vector2> points = GP(0, -1);
                    List<Vector2> pointsOutside = GP(420 * a, -1);
                    int i;
                    for (i = 0; i < points.Count; i++)
                    {
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + points[i],
                              new Vector3((float)i / points.Count, 1, 0.9f),
                              Color.White * a));
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + pointsOutside[i],
                              new Vector3((float)i / points.Count, 0, 0.9f),
                              Color.White * a));

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3)
                    {
                        Texture2D tx = CEUtils.getExtraTex("AbyssalCircle2");
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<Vector2> points = GP(0, 0.6f);
                    List<Vector2> pointsOutside = GP(420 * a, -1);
                    int i;
                    for (i = 0; i < points.Count; i++)
                    {
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + points[i],
                              new Vector3((float)i / points.Count, 1, 0.9f),
                              Color.SkyBlue * a));
                        ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + pointsOutside[i],
                              new Vector3((float)i / points.Count, 0, 0.9f),
                              Color.SkyBlue * a));

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3)
                    {
                        Texture2D tx = CEUtils.getExtraTex("AbyssalCircle2");
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
        }
        public List<Texture2D> fintexs = null;
        public void DrawFins()
        {
            if (fintexs == null)
            {
                fintexs = new List<Texture2D>() {
                    ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Prophet/Wing1", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value,
                    ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Prophet/Wing2", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value
                };
            }
            float rotj = finRotCounter <= 0.4f ? CEUtils.GetRepeatedCosFromZeroToOne(finRotCounter / 0.4f, 1) : 1 - CEUtils.GetRepeatedCosFromZeroToOne((finRotCounter - 0.4f) / 0.6f, 1);
            Main.EntitySpriteDraw(fintexs[1], NPC.Center + new Vector2(-20, -20).RotatedBy(NPC.rotation) - Main.screenPosition, null, Color.White, rl - rotj, fintexs[1].Size(), NPC.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(fintexs[1], NPC.Center + new Vector2(-20, 20).RotatedBy(rl) - Main.screenPosition, null, Color.White, rl + rotj, fintexs[1].Size() * new Vector2(1, 0), NPC.scale, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(fintexs[0], NPC.Center + new Vector2(0, -20).RotatedBy(rl) - Main.screenPosition, null, Color.White, rl - 1 + rotj, new Vector2(fintexs[0].Width * 0.5f, fintexs[0].Height), NPC.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(fintexs[0], NPC.Center + new Vector2(0, 20).RotatedBy(rl) - Main.screenPosition, null, Color.White, rl + 1 - rotj, new Vector2(fintexs[0].Width * 0.5f, 0), NPC.scale, SpriteEffects.FlipVertically);
        }
        public void DrawTail()
        {
            if (tail == null || tail.Count < 3)
            {
                return;
            }
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = Color.White;

            for (int i = 0; i < tail.Count - 3; i++)
            {
                ve.Add(new ColoredVertex(tail[i].position - Main.screenPosition + (tail[i + 1].position - tail[i].position).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 40,
                      new Vector3((((float)i) / tail.Count), 1, 1),
                      b));
                ve.Add(new ColoredVertex(tail[i].position - Main.screenPosition + (tail[i + 1].position - tail[i].position).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 40,
                      new Vector3((((float)i) / tail.Count), 0, 1),
                      b));

            }
            ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + (NPC.Center - tail[tail.Count - 1].position).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 40,
                      new Vector3((float)1, 1, 1),
                      b));
            ve.Add(new ColoredVertex(NPC.Center - Main.screenPosition + (NPC.Center - tail[tail.Count - 1].position).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 40,
                  new Vector3((float)1, 0, 1),
                  b));
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3)
            {
                Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Prophet/Tail").Value;
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Texture2D ring = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Prophet/ring").Value;
                if (tail.Count > 10)
                {
                    Main.EntitySpriteDraw(ring, tail[8].position - Main.screenPosition, null, Color.White, (tail[9].position - tail[8].position).ToRotation() - MathHelper.PiOver2, ring.Size() / 2f, NPC.scale, SpriteEffects.None);
                }
            }
        }
        public override void OnKill()
        {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedProphet, -1);
        }
    }
}
