using CalamityEntropy.Common;
using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Tiles;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Potions;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.World;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    [AutoloadBossHead]
    //[StaticImmunity(staticImmunityCooldown: 6)]

    public class CruiserHead : ModNPC
    {
        public static float ProjDamageReduce = 0.5f;
        public class HitRecord
        {
            public int Timeleft = 200;
            public int ProjID = -1;
            public float dmgMult = 1;
            public HitRecord(int id)
            {
                ProjID = id;
            }
        }
        public List<HitRecord> hitRecords = new List<HitRecord>();
        public float ProgressDraw = 0;
        private int length = 20;
        public float speedMuti = 1;
        public float speed = 18;
        public float targetSpeed = 18;
        public int noaitime = 280;
        public int notargettime = 0;
        public int tjv = 0;
        public bool bite = false;
        public float mouthRot = 0;
        public int tail = -1;
        private bool b_added = false;
        public int nrc = 0;
        float ja = 50;
        float da = 50;
        float tail_vj = 0;
        bool jv = false;
        public int phaseTrans = 0;
        public bool flag = false;
        public int slowDownTime = 0;
        public List<Vector2> bodies = new List<Vector2>();
        public Vector2 vtodraw = new Vector2();
        public float jaslowdown = 0;
        public float aitype = 0;
        public int changeCounter = 0;
        public float maxDistance = 6000;
        public float maxDistanceTarget = 2900;
        public int rotDist = 900;
        public Vector2 rotPos = Vector2.Zero;
        public int phase = 1;
        public int circleDir = 1;
        public float alpha = 1;
        public bool candraw = false;
        public bool DeathAnm = false;
        public int DeathAnmCount = 200;

        public static int icon = ModContent.GetModBossHeadSlot("CalamityEntropy/Content/NPCs/Cruiser/CruiserHead_Head_Boss");
        public static int iconP2;
        public static void loadHead()
        {
            string path = "CalamityEntropy/Content/NPCs/Cruiser/p2head";
            CalamityEntropy.Instance.AddBossHeadTexture(path, -1);
            iconP2 = ModContent.GetModBossHeadSlot(path);

        }
        public override void BossHeadSlot(ref int index)
        {
            if (phaseTrans >= 120)
            {
                index = iconP2;
            }
            else
            {
                index = icon;
            }
        }
        public override void BossHeadRotation(ref float rotation)
        {
            rotation = NPC.rotation - MathHelper.PiOver2;
        }
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = "CalamityEntropy/Assets/Extra/CruiserBes",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.MPAllowedEnemies[ModContent.NPCType<PrimordialWyrmHead>()] = true;
            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
        }
        int tdamage = 0;
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.CruiserBestiary")
            });
        }
        public override void SetDefaults()
        {
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.Calamity().DR = 0.54f;
            NPC.boss = true;
            NPC.width = 96;
            NPC.height = 96;
            NPC.damage = 200;
            if (Main.expertMode)
            {
                NPC.damage += 4;
            }
            if (Main.masterMode)
            {
                NPC.damage += 4;
            }
            NPC.defense = 80;
            NPC.lifeMax = 1120000;
            if (CalamityWorld.death)
            {
                NPC.damage += 4;
                length += 4;
            }
            else if (CalamityWorld.revenge)
            {
                NPC.damage += 2;
                length += 3;
            }
            tdamage = NPC.damage;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCHit4;
            NPC.value = 100000f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.9f;
            NPC.dontCountMe = true;
            NPC.scale = 1f;
            if (Main.masterMode)
            {
                NPC.scale = 1.05f;
            }
            if (Main.getGoodWorld)
            {
                NPC.scale = 1.3f;
                NPC.lifeMax += 750000;
            }
            if (Main.zenithWorld)
            {
                NPC.scale = 1.5f;
                length = 10;
            }
            NPC.netAlways = true;
            NPC.Entropy().damageMul = 0.1f;
            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/CruiserBoss");
            }
            SpawnModBiomes = new int[] { ModContent.GetInstance<VoidDummyBoime>().Type };
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(Main.zenithWorld ? ModContent.BuffType<MaliciousCode>() : ModContent.BuffType<VoidTouch>(), 150);
        }
        public override void BossLoot(ref int potionType)
        {
            potionType = ModContent.ItemType<OmegaHealingPotion>();
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<CruiserBag>()));

            npcLoot.DefineConditionalDropSet(() => true).Add(DropHelper.PerPlayer(ModContent.ItemType<OmegaHealingPotion>(), 1, 5, 15), hideLootReport: true);


            var normalOnly = npcLoot.DefineNormalOnlyDropSet();
            {
                normalOnly.Add(ModContent.ItemType<BottledFissure>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<VoidRelics>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<VoidElytra>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<VoidEcho>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<Silence>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<VoidAnnihilate>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<WindOfUndertaker>(), new Fraction(2, 5));
                normalOnly.Add(ModContent.ItemType<WingsOfHush>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<VoidCandle>(), new Fraction(3, 5));
                normalOnly.Add(ModContent.ItemType<VoidMonolith>(), 3);
                normalOnly.Add(ModContent.ItemType<VoidToy>(), 3);
                normalOnly.Add(ModContent.ItemType<VoidScales>(), new Fraction(1, 1), 88, 128);
            }
            npcLoot.DefineConditionalDropSet(DropHelper.RevAndMaster).Add(ModContent.ItemType<CruiserRelic>());

            npcLoot.Add(ModContent.ItemType<CruiserTrophy>(), 10);

            npcLoot.AddConditionalPerPlayer(() => !EDownedBosses.downedCruiser, ModContent.ItemType<CruiserLore>());
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(speedMuti);
            writer.Write(targetSpeed);
            writer.Write(slowDownTime);
            writer.Write(nrc);
            writer.Write((byte)ai);
            writer.Write(changeCounter);
            writer.Write(maxDistanceTarget);
            writer.Write(rotDist);
            writer.WriteVector2(rotPos);
            writer.Write(circleDir);
            writer.Write(flag);
            writer.Write(noaitime);
            writer.Write(phaseTrans);
            writer.Write(phase);
            writer.WriteVector2(SpaceCenter);
            writer.Write(DeathAnm);
            writer.Write(DeathAnmCount);
            writer.Write(NPC.dontTakeDamage);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            speedMuti = reader.ReadSingle();
            targetSpeed = reader.ReadSingle();
            slowDownTime = reader.ReadInt32();
            nrc = reader.ReadInt32();
            ai = (AIStyle)reader.ReadByte();
            changeCounter = reader.ReadInt32();
            maxDistanceTarget = reader.ReadSingle();
            rotDist = reader.ReadInt32();
            rotPos = reader.ReadVector2();
            circleDir = reader.ReadInt32();
            flag = reader.ReadBoolean();
            noaitime = reader.ReadInt32();
            phaseTrans = reader.ReadInt32();
            phase = reader.ReadInt32();
            SpaceCenter = reader.ReadVector2();
            DeathAnm = reader.ReadBoolean();
            DeathAnmCount = reader.ReadInt32();
            NPC.dontTakeDamage = reader.ReadBoolean();
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            bool flag = false;
            HitRecord hr = null;
            foreach (var hrc in hitRecords)
            {
                if (hrc.ProjID == projectile.whoAmI)
                {
                    flag = true;
                    hr = hrc;
                    break;
                }
            }
            if (flag)
            {
                modifiers.FinalDamage *= hr.dmgMult;
                hr.dmgMult *= CruiserHead.ProjDamageReduce;
                if (!projectile.minion && (projectile.penetrate == -1 || projectile.penetrate > 4))
                    hr.dmgMult *= CruiserHead.ProjDamageReduce;
                if (!projectile.minion)
                {
                    hr.Timeleft += 20;
                    if (hr.Timeleft > 250)
                    {
                        hr.Timeleft = 250;
                    }
                }
            }
            else
            {
                hitRecords.Add(new HitRecord(projectile.whoAmI));
            }
        }
        public override bool CheckDead()
        {
            if (DeathAnmCount <= 0)
            {
                return true;
            }
            DeathAnm = true;
            NPC.damage = 0;
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            NPC.active = true;
            NPC.netUpdate = true;
            if (NPC.netSpam >= 10)
                NPC.netSpam = 9;
            return false;
        }

        public void changeAi()
        {
            changeCounter = 0;
            NPC.netUpdate = true;
            if (phase == 1)
            {
                aiRound++;
                if (aiRound > 19)
                {
                    aiRound = 0;
                }
                if (aiRound == 1 || aiRound == 3 || aiRound == 5)
                {
                    ai = AIStyle.StayAwayAndShootVoidStar;
                }
                if (aiRound == 0 || aiRound == 2 || aiRound == 4)
                {
                    ai = AIStyle.TryToClosePlayer;
                }
                if (aiRound == 8 || aiRound == 10 || aiRound == 12)
                {
                    ai = AIStyle.StayAwayAndShootVoidStar;
                }
                if (aiRound == 7 || aiRound == 9 || aiRound == 11)
                {
                    ai = AIStyle.TryToClosePlayer;
                }
                if (aiRound == 14 || aiRound == 16 || aiRound == 18)
                {
                    ai = AIStyle.StayAwayAndShootVoidStar;
                }
                if (aiRound == 15 || aiRound == 17)
                {
                    ai = AIStyle.TryToClosePlayer;
                }

                if (aiRound == 6 || aiRound == 19)
                {
                    if (ai == AIStyle.StayAwayAndShootVoidStar)
                    {
                        aiRound--;
                        ai = AIStyle.TryToClosePlayer;
                    }
                    else
                    {
                        ai = Main.rand.NextBool() ? AIStyle.EnergyBall : AIStyle.VoidResidue;
                    }
                }
                if (aiRound == 13)
                {
                    ai = AIStyle.AroundPlayerAndShootVoidStar;
                }

            }
            else
            {
                NPC.defense = 50;
                NPC.Calamity().DR = 0.42f;
                aiRound++;
                if (aiRound >= 10)
                {
                    aiRound = 0;
                }
                if (aiRound == 0 || aiRound == 2)
                {
                    ai = AIStyle.VoidSpike;
                }
                if (aiRound == 1)
                {
                    ai = AIStyle.AroundSpawnVoidBomb;
                }
                if (aiRound == 3)
                {
                    ai = AIStyle.SplittingVoidStar;
                }
                if (aiRound == 4)
                {
                    ai = AIStyle.QuickDash;
                }
                if (aiRound == 5)
                {
                    ai = AIStyle.VoidSpike;
                }
                if (aiRound == 6)
                {
                    ai = AIStyle.VoidLaser;
                }
                if (aiRound == 7)
                {
                    ai = AIStyle.VoidResidue;
                }
                if (aiRound == 8)
                {
                    ai = AIStyle.Cruise;
                }
                if (aiRound == 9)
                {
                    ai = AIStyle.BiteAndDash;
                }
            }
        }
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            if (ai == AIStyle.PhaseTransing)
                return false;
            return noaitime <= 0 && ai != AIStyle.BiteAndDash;
        }
        public override bool CanHitNPC(NPC target)
        {
            return noaitime <= 0 && base.CanHitNPC(target);
        }
        public override bool? CanBeHitByItem(Player player, Item item)
        {
            if (noaitime > 0)
            {
                return false;
            }
            return base.CanBeHitByItem(player, item);
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (noaitime > 0)
            {
                return false;
            }
            return base.CanBeHitByProjectile(projectile);
        }
        public override bool CanBeHitByNPC(NPC attacker)
        {
            return noaitime <= 0 && base.CanBeHitByNPC(attacker);
        }
        public int counterc = 0;
        public Vector2 SpaceCenter = Vector2.Zero;
        public enum AIStyle
        {
            TryToClosePlayer,
            StayAwayAndShootVoidStar,
            AroundPlayerAndShootVoidStar,
            EnergyBall,
            VoidResidue,

            PhaseTransing,

            VoidSpike,
            BiteAndDash,
            Cruise,
            SplittingVoidStar,
            QuickDash,
            AroundSpawnVoidBomb,
            VoidLaser
        }
        public int aiRound = 0;
        public AIStyle ai = AIStyle.TryToClosePlayer;
        public void Shoot(int type, Vector2 pos, Vector2 velo, float damageMult = 1, float ai0 = 0, float ai1 = 0, float ai2 = 0)
        {
            Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, velo, type, (int)(NPC.damage / 6.9f * damageMult), 3, -1, ai0, ai1, ai2);
        }
        public float whiteLerp = 0;
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0 && DeathAnmCount <= 10)
            {
                if (!Main.zenithWorld)
                {
                    CEUtils.PlaySound("VoidAttack", 1, NPC.Center);
                    for (int i = 0; i < 86; i++)
                    {
                        Particle p = new Particle();
                        p.position = NPC.Center;
                        p.alpha = Main.rand.NextFloat(1f, 2f);
                        p.shape = 4;
                        p.vd = 0.97f;
                        p.velocity = CEUtils.randomPointInCircle(16);
                        VoidParticles.particles.Add(p);
                    }
                }
                else
                {
                    EParticle.spawnNew(new RealisticExplosion(), NPC.Center, Vector2.Zero, Color.White, 10, 1, true, BlendState.AlphaBlend);
                }
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 16;

            }
        }
        public float camLerp = 0;
        public override void AI()
        {
            for (int i = hitRecords.Count - 1; i >= 0; i--)
            {
                hitRecords[i].dmgMult = float.Lerp(hitRecords[i].dmgMult, 1, 0.09f);
                if (hitRecords[i].ProjID < 0 || !hitRecords[i].ProjID.ToProj().active)
                {
                    hitRecords.RemoveAt(i);
                }
            }
            bool canShoot = Main.netMode != NetmodeID.MultiplayerClient;
            if (DeathAnm)
            {
                WarningAlpha = 0;
                if (camLerp < 1)
                {
                    camLerp += 0.025f;
                }
                else
                {
                    camLerp = 24f;
                }
                Main.LocalPlayer.Entropy().screenShift = camLerp;
                Main.LocalPlayer.Entropy().screenPos = NPC.Center;
                if (NPC.velocity.Length() > 6)
                {
                    NPC.velocity *= 0.96f;
                }
                NPC.rotation = NPC.velocity.ToRotation();
                DeathAnmCount--;
                if (whiteLerp < 1)
                    whiteLerp += 1 / 160f;
                if (DeathAnmCount % 6 == 0 && !Main.dedServ)
                {
                    EParticle.NewParticle(new PremultBurst(), NPC.Center, Vector2.Zero, Color.LightBlue, 3.2f, 1, true, BlendState.Additive, 0);
                }
                if (DeathAnmCount <= 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.StrikeInstantKill();
                        NPC.netSpam = 9;
                        NPC.netUpdate = true;
                    }
                }
                vtodraw = NPC.Center;
                for (int i = 0; i < bodies.Count; i++)
                {
                    Vector2 oPos;
                    float oRot;

                    if (i == 0)
                    {
                        oPos = NPC.Center;
                        oRot = NPC.rotation;
                    }
                    else
                    {
                        oPos = bodies[i - 1];
                        if (i == 1)
                        {
                            oRot = (NPC.Center - bodies[0]).ToRotation();
                        }
                        else
                        {
                            oRot = (bodies[i - 2] - bodies[i - 1]).ToRotation();
                        }
                    }
                    float rot = (oPos - bodies[i]).ToRotation();
                    rot = CEUtils.RotateTowardsAngle(rot, oRot, 0.12f, false);

                    int spacing = 80;
                    bodies[i] = oPos - rot.ToRotationVector2() * spacing * NPC.scale;
                }
                return;
            }
            NPC.Entropy().damageMul += 1f / 10000f;
            if (NPC.Entropy().damageMul > 1)
            {
                NPC.Entropy().damageMul = 1;
            }
            counterc++;
            if (noaitime > 0)
            {
                NPC.dontTakeDamage = true;
                for (int i = 0; i < bodies.Count; i++)
                {
                    bodies[i] = NPC.Center;
                }
                foreach (Projectile pj in Main.ActiveProjectiles)
                {
                    if (pj.ModProjectile is VoidBottleThrow)
                    {
                        NPC.Center = pj.Center;
                        break;
                    }
                }
            }
            else
            {
                if (ai == AIStyle.PhaseTransing)
                {
                    NPC.dontTakeDamage = true;
                }
            }
            noaitime--;

            if (noaitime == 0)
            {
                NPC.dontTakeDamage = false;
            }

            if (noaitime < 0)
            {
                if (!b_added)
                {
                    b_added = true;
                    for (int i = 0; i < length + 1; i++)
                    {
                        bodies.Add(NPC.Center - new Vector2(0, 0));
                    }
                    if (!(Main.netMode == NetmodeID.MultiplayerClient))
                    {
                        int bodyIndex;
                        int syg;
                        syg = NPC.whoAmI;
                        for (int i = 0; i < length + 1; i++)
                        {
                            int type = ModContent.NPCType<CruiserBody>();
                            if (i == length)
                            {
                                type = ModContent.NPCType<CruiserTail>();
                            }

                            bodyIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, type);

                            Main.npc[bodyIndex].ai[1] = syg;
                            Main.npc[bodyIndex].ai[2] = i;
                            Main.npc[bodyIndex].ai[3] = NPC.whoAmI;
                            Main.npc[bodyIndex].realLife = NPC.whoAmI;
                            syg = bodyIndex;
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, bodyIndex);
                                Main.npc[bodyIndex].netUpdate = true;
                            }

                        }
                        NPC.ai[3] = syg;
                        tail = syg;
                    }
                }
                Main.LocalPlayer.Entropy().crSky = 30;
                NPC.TargetClosest();
                maxDistance += (maxDistanceTarget - maxDistance) * 0.001f;
                if (noaitime <= 0)
                {
                    foreach (Player p in Main.ActivePlayers)
                    {
                        if (CEUtils.getDistance(SpaceCenter, p.Center) > maxDistance)
                        {
                            if (!Main.dedServ)
                            {
                                p.AddBuff(ModContent.BuffType<VoidTouch>(), 5);
                            }
                        }
                    }
                    if (NPC.HasValidTarget)
                    {
                        Player target = NPC.target.ToPlayer();
                        if (!bite && NPC.Distance(target.Center) < 900 && ai != AIStyle.SplittingVoidStar && ai != AIStyle.VoidResidue && ai != AIStyle.BiteAndDash && ai != AIStyle.EnergyBall && ai != AIStyle.AroundPlayerAndShootVoidStar && ai != AIStyle.AroundSpawnVoidBomb)
                        {
                            mouthRot += Utils.Remap(NPC.Distance(target.Center), 900, 50, 0, 7f);
                            if (NPC.Distance(target.Center) < float.Max(30, NPC.velocity.Length()) * 4.6f)
                            {
                                bite = true;
                            }
                        }

                        int phaseNow = 1;
                        if (NPC.life < NPC.lifeMax / 2)
                        {
                            phaseNow = 2;
                        }
                        phase = phaseNow;
                        if (phaseNow == 2)
                        {

                            if (phaseTrans < 122)
                            {
                                ai = AIStyle.PhaseTransing;
                                phaseTrans++;
                                alpha *= 0.967f;
                                aiRound = 0;
                                if (phaseTrans <= 60)
                                {
                                    da = 0;
                                    tail_vj = 0;
                                    jv = false;
                                    foreach (Projectile p in Main.ActiveProjectiles)
                                    {
                                        if (p.ModProjectile is CruiserEnergyBall || p.ModProjectile is VoidResidue)
                                        {
                                            p.active = false;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (ai == AIStyle.PhaseTransing)
                                {
                                    NPC.Entropy().VoidTouchDR = 0.4f;
                                    ai = AIStyle.VoidSpike;
                                    NPC.dontTakeDamage = false;
                                    NPC.width = 156;
                                    NPC.height = 156;
                                    foreach (NPC n in Main.npc)
                                    {
                                        if (n.realLife == NPC.whoAmI)
                                        {
                                            if (n.ai[2] <= 8 && n.ai[2] > 4)
                                            {
                                                n.width = 26;
                                                n.height = 26;
                                            }
                                            n.netUpdate = true;
                                            if (n.ai[2] > 8)
                                            {
                                                n.active = false;
                                            }
                                            if (Main.dedServ)
                                            {
                                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n.whoAmI, 0f, 0f, 0f, 0);
                                            }
                                        }
                                    }
                                }
                                if (alpha < 1)
                                {
                                    alpha += 0.02f;
                                    if (alpha > 1)
                                    {
                                        alpha = 1;
                                    }
                                }
                            }
                        }
                        maxDistanceTarget = 12000;
                        SpaceCenter = (NPC.Center + bodies[bodies.Count - 1]) / 2f;
                        if (ai == AIStyle.PhaseTransing)
                        {
                            SpaceCenter = target.Center;
                            maxDistanceTarget = 6000;
                            if (NPC.velocity.Length() < 8)
                            {
                                NPC.velocity *= 1.01f;
                            }
                            else
                            {
                                NPC.velocity *= 0.98f;
                            }
                            foreach (var p in bodies)
                            {
                                VoidParticles.particles.Add(new Particle() { position = p, alpha = Main.rand.NextFloat(0.2f, 1.4f), shape = 4, velocity = CEUtils.randomPointInCircle(6) });
                            }
                        }
                        if (ai == AIStyle.TryToClosePlayer)
                        {
                            if (NPC.velocity.Length() < 36)
                            {
                                NPC.velocity *= 1.02f;
                            }
                            NPC.velocity += (target.Center - NPC.Center).normalize() * Utils.Remap(NPC.Distance(target.Center), 0, 700, 1f, 3f);
                            NPC.velocity *= Utils.Remap(NPC.Distance(target.Center), 0, 700, 0.98f, 0.97f);
                            NPC.velocity = Vector2.Lerp(NPC.velocity, (target.Center - NPC.Center).normalize() * NPC.velocity.Length(), Utils.Remap(NPC.Distance(target.Center), 0, 1000, 0f, 0.1f));
                            changeCounter++;
                            if (changeCounter > 600 || NPC.Distance(target.Center) < 700 + NPC.velocity.Length())
                            {
                                changeAi();
                            }
                        }
                        if (ai == AIStyle.StayAwayAndShootVoidStar)
                        {
                            if (NPC.velocity.Length() < 30)
                            {
                                NPC.velocity *= 1.1f;
                            }
                            else
                            {
                                NPC.velocity *= 0.97f;
                            }
                            changeCounter++;
                            if (changeCounter == 90)
                            {
                                tjv = 1;
                            }
                            if (changeCounter > 70)
                            {
                                NPC.velocity = Vector2.Lerp(NPC.velocity, (target.Center - NPC.Center).normalize() * NPC.velocity.Length(), 0.02f);
                                if (NPC.velocity.Length() < 30)
                                {
                                    NPC.velocity *= 1.02f;
                                }
                            }
                            if (changeCounter > 120)
                            {
                                if (NPC.velocity.Length() < 30)
                                {
                                    NPC.velocity *= 1.046f;
                                }
                                NPC.velocity += (target.Center - NPC.Center).normalize() * 0.1f;
                                NPC.velocity = Vector2.Lerp(NPC.velocity, (target.Center - NPC.Center).normalize() * NPC.velocity.Length(), 0.03f);
                                NPC.velocity *= 0.998f;
                            }
                            if (changeCounter > 140)
                            {
                                changeAi();
                            }
                        }
                        if (ai == AIStyle.EnergyBall)
                        {
                            if (changeCounter == 0)
                            {
                                if (canShoot)
                                {
                                    Shoot(ModContent.ProjectileType<CruiserEnergyBall>(), NPC.Center, Vector2.Zero, 1.15f, NPC.whoAmI);
                                }
                            }
                            changeCounter++;
                            if (changeCounter > 240)
                            {
                                changeAi();
                            }
                            NPC.velocity += (target.Center - NPC.Center).normalize() * (NPC.Distance(target.Center) > 1000 ? 4f : 1);
                            NPC.velocity *= 0.92f;
                        }
                        if (ai == AIStyle.VoidResidue)
                        {
                            if (changeCounter < 80)
                            {
                                mouthRot -= 4.8f;
                            }
                            else
                            {
                                if (changeCounter < 100)
                                {
                                    mouthRot += 5f;
                                }
                            }
                            changeCounter++;
                            if (changeCounter < 80 && NPC.Distance(target.Center) > 1000)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.velocity += (target.Center - NPC.Center).normalize() * 4f;
                            }
                            else
                            {
                                NPC.velocity *= 0.92f;
                                NPC.velocity += (target.Center - NPC.Center).normalize() * 0.36f;
                            }
                            if (changeCounter == 2)
                                CEUtils.PlaySound("voidSound", 0.8f, NPC.Center);
                            if (changeCounter == 80)
                            {
                                if (canShoot)
                                {
                                    for (int i = 0; i < 80; i++)
                                    {
                                        Shoot(ModContent.ProjectileType<VoidResidue>(), NPC.Center, NPC.velocity.normalize().RotatedByRandom(2f) * 24 * Main.rand.NextFloat(0.2f, 1f), 0.8f);
                                    }
                                }
                                CEUtils.PlaySound("CruiserSpit2", 1.4f, NPC.Center);
                                CEUtils.PlaySound("CruiserVoidResidue", 1, NPC.Center);
                            }
                            if (changeCounter > 140)
                            {
                                NPC.velocity += NPC.rotation.ToRotationVector2() * 6f;
                                NPC.velocity *= 0.98f;
                            }
                            if (changeCounter > 200)
                            {
                                changeAi();
                            }
                        }
                        if (ai == AIStyle.AroundPlayerAndShootVoidStar)
                        {
                            Vector2 targetPos = target.Center + (NPC.Center - target.Center).normalize().RotatedBy(0.6f) * 600;
                            NPC.velocity += (targetPos - NPC.Center).normalize() * 3f;
                            NPC.velocity *= 0.98f;
                            changeCounter++;
                            if (changeCounter % 40 == 0)
                            {
                                tjv = 1;
                            }
                            if (changeCounter > 40 * 8 + 30)
                            {
                                changeAi();
                            }
                        }
                        if (ai == AIStyle.VoidSpike)
                        {
                            NPC.velocity = NPC.velocity.normalize() * (NPC.velocity.Length() + (46 - NPC.velocity.Length()) * 0.08f);
                            NPC.velocity = CEUtils.RotateTowardsAngle(NPC.velocity.ToRotation(), (target.Center - NPC.Center).ToRotation(), 0.0376f, false).ToRotationVector2() * NPC.velocity.Length();
                            changeCounter++;
                            if (changeCounter == 40 || changeCounter == 60 || changeCounter == 80 || changeCounter == 100)
                            {
                                if (canShoot)
                                {
                                    for (float i = 0; i < 360; i += 30)
                                    {
                                        Shoot(ModContent.ProjectileType<VoidSpike>(), NPC.Center, MathHelper.ToRadians(i).ToRotationVector2() * 5);
                                    }
                                }
                            }
                            if (changeCounter > 150)
                            {
                                changeAi();
                            }
                        }
                        if (ai == AIStyle.BiteAndDash)
                        {
                            if (changeCounter == 0)
                            {
                                var pLt = new List<int>() { ModContent.ProjectileType<VoidStar>(), ModContent.ProjectileType<VoidResidue>(), ModContent.ProjectileType<VoidSpike>() };
                                foreach (Projectile p in Main.ActiveProjectiles)
                                {
                                    if (pLt.Contains(p.type))
                                        p.Kill();
                                }
                                NPC.velocity *= 0.9f;
                                NPC.velocity += (target.Center - NPC.Center).normalize() * 6;
                                if (CEUtils.getDistance(NPC.Center + NPC.rotation.ToRotationVector2() * 160, target.Center) < 160)
                                {
                                    changeCounter++;
                                    target.velocity *= 0;
                                    target.Center = NPC.Center + NPC.rotation.ToRotationVector2() * 160;
                                }
                            }
                            else
                            {
                                changeCounter++;
                                if (changeCounter < 20)
                                {
                                    mouthRot -= 5f;
                                    NPC.velocity = NPC.velocity.normalize() * (NPC.velocity.Length() + (80 - NPC.velocity.Length()) * 0.2f);

                                    if (CEUtils.getDistance(NPC.Center + NPC.rotation.ToRotationVector2() * 160, target.Center) < 160)
                                    {
                                        target.velocity *= 0;
                                        target.Entropy().immune = 12;
                                        target.Center = NPC.Center + NPC.rotation.ToRotationVector2() * 160;
                                    }
                                    if (!CEUtils.isAir(NPC.Center + NPC.rotation.ToRotationVector2() * 360))
                                    {
                                        changeCounter = 60;
                                    }
                                }
                                else
                                {
                                    Vector2 targetPos = target.Center + (NPC.Center - target.Center).normalize().RotatedBy(0.6f) * 1600;
                                    NPC.velocity += (targetPos - NPC.Center).normalize() * 1f;
                                    NPC.velocity *= 0.98f;
                                    if (changeCounter > 120)
                                    {
                                        changeAi();
                                    }
                                }
                                if (changeCounter == 20)
                                {
                                    if (CEUtils.getDistance(NPC.Center + NPC.rotation.ToRotationVector2() * 80, target.Center) < 160)
                                    {
                                        target.velocity = NPC.velocity * 1.6f;
                                        target.Entropy().CruiserAntiGravTime = 100;
                                    }

                                    if (canShoot)
                                    {
                                        for (int i = 1; i < 18; i++)
                                        {
                                            for (int j = -6; j < 7; j++)
                                            {
                                                if (j == 0)
                                                {
                                                    Shoot(ModContent.ProjectileType<CruiserSlash>(), NPC.Center + NPC.velocity.normalize() * 300 * i, NPC.velocity);
                                                }
                                                else
                                                {
                                                    Shoot(ModContent.ProjectileType<CruiserSlash>(), NPC.Center + NPC.velocity.normalize().RotatedBy(0.125f * j) * 300 * i, NPC.velocity.RotatedBy(0.125f * j));
                                                }
                                            }
                                        }
                                    }
                                    NPC.velocity *= 0.3f;
                                }

                            }
                        }
                        if (ai == AIStyle.AroundSpawnVoidBomb)
                        {
                            NPC.velocity = NPC.velocity.normalize() * (NPC.velocity.Length() + (32 - NPC.velocity.Length()) * 0.08f);
                            NPC.velocity = CEUtils.RotateTowardsAngle(NPC.velocity.ToRotation(), (target.Center - NPC.Center).ToRotation(), 0.022f, false).ToRotationVector2() * NPC.velocity.Length();

                            changeCounter++;
                            if (changeCounter < 180)
                            {
                                if (changeCounter % 7 == 0)
                                {
                                    if (canShoot)
                                        Shoot(ModContent.ProjectileType<VoidBomb>(), NPC.Center, CEUtils.randomPointInCircle(8) + (target.Center - NPC.Center).normalize() * 20);
                                }
                            }
                            if (changeCounter > 340)
                            {
                                changeAi();
                            }
                        }
                        if (ai == AIStyle.Cruise)
                        {
                            NPC.velocity = NPC.velocity.normalize() * (NPC.velocity.Length() + (40 - NPC.velocity.Length()) * 0.2f);

                            NPC.velocity += (target.Center - NPC.Center).normalize() * 0.1f;
                            NPC.velocity = Vector2.Lerp(NPC.velocity, (target.Center - NPC.Center).normalize() * NPC.velocity.Length(), 0.058f);
                            NPC.velocity *= 0.998f;
                            changeCounter++;
                            if (changeCounter > 100)
                            {
                                if (Main.rand.NextBool(150) || changeCounter > 200)
                                {
                                    changeAi();
                                }
                            }
                        }
                        if (ai == AIStyle.SplittingVoidStar)
                        {

                            if (changeCounter < 100)
                            {
                                mouthRot -= 4.8f;
                            }
                            else
                            {
                                if (changeCounter < 120)
                                {
                                    mouthRot += 5f;
                                }
                            }
                            if (changeCounter == 20)
                                CEUtils.PlaySound("voidSound", 1.05f, NPC.Center);
                            changeCounter++;
                            if (changeCounter < 100 && NPC.Distance(target.Center) > 900)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.velocity += (target.Center - NPC.Center).normalize() * 1f;
                            }
                            else
                            {
                                NPC.velocity *= 0.94f;
                                NPC.velocity += (target.Center - NPC.Center).normalize() * 0.26f;
                            }
                            if (changeCounter == 100)
                            {
                                if (canShoot)
                                {
                                    for (int i = 0; i < 80; i++)
                                    {
                                        Shoot(ModContent.ProjectileType<VoidStar>(), NPC.Center, NPC.velocity.normalize().RotatedByRandom(2f) * 24 * Main.rand.NextFloat(0.2f, 1f), 0.75f);
                                    }
                                }
                                CEUtils.PlaySound("CruiserSpit", 1.2f, NPC.Center);
                                CEUtils.PlaySound("VoidBomb", 1.1f, NPC.Center);
                                CEUtils.PlaySound("VoidBomb", 1.1f, NPC.Center);
                                CEUtils.PlaySound("VoidBomb", 1.1f, NPC.Center);
                                CEUtils.PlaySound("vbuse", 1, NPC.Center);
                            }
                            if (changeCounter > 140)
                            {
                                changeAi();
                            }
                        }
                        if (ai == AIStyle.QuickDash)
                        {
                            if (changeCounter == 0)
                            {
                                NPC.rotation = (target.Center - NPC.Center).ToRotation();
                            }
                            changeCounter++;

                            if (changeCounter > 38)
                            {
                                NPC.velocity *= 0.97f;
                                NPC.velocity += (target.Center - NPC.Center).normalize() * 1.4f;
                            }
                            else
                            {
                                NPC.velocity += NPC.rotation.ToRotationVector2() * 3.5f;
                            }
                            if (changeCounter > 100)
                            {
                                changeAi();
                            }
                        }
                        if (ai == AIStyle.VoidLaser)
                        {
                            if (NPC.localAI[2]++ < 35)
                            {
                                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (target.Center + target.velocity * 46 * 0.8f - NPC.Center).ToRotation(), 0.16f, false);
                                NPC.velocity *= 0.96f;
                                NPC.velocity += NPC.rotation.ToRotationVector2() * -0.4f;
                            }
                            if (NPC.localAI[2] > 36)
                            {
                                int u = (int)Utils.Remap(46 * (int)(changeCounter / 46f), 0, 6 * 46, 42, 18);
                                if (changeCounter % 46 == 0)
                                {
                                    if (changeCounter > 1)
                                        NPC.rotation = (target.Center + target.velocity * u * 0.8f - NPC.Center).ToRotation();
                                    NPC.velocity = NPC.rotation.ToRotationVector2();
                                    EParticle.NewParticle(new CruiserWarn(), NPC.Center, Vector2.Zero, Color.White, 1.8f, 1, true, BlendState.Additive, NPC.rotation);
                                    EParticle.NewParticle(new CruiserWarn(), NPC.Center, Vector2.Zero, Color.White, 0.8f, 1, true, BlendState.Additive, NPC.rotation);
                                }
                                if (changeCounter % 46 == u)
                                {
                                    if (canShoot)
                                    {
                                        Shoot(ModContent.ProjectileType<CruiserLaser2>(), NPC.Center, NPC.rotation.ToRotationVector2() * 10, ai0: NPC.whoAmI);
                                    }
                                    NPC.velocity = NPC.rotation.ToRotationVector2() * ((CEUtils.getDistance(NPC.Center, target.Center) + 1400f) / (45f - u));
                                }
                                if (changeCounter % 46 == 45)
                                {
                                    NPC.velocity = NPC.velocity.normalize() * 4;
                                }
                                changeCounter++;
                                if (changeCounter >= 6 * 46)
                                {
                                    NPC.localAI[2] = 0;
                                    changeAi();
                                }
                            }
                        }
                        if (ai != AIStyle.VoidLaser)
                        {
                            NPC.rotation = NPC.velocity.ToRotation();
                        }
                    }
                    else
                    {
                        notargettime++;
                        NPC.velocity.Y += -1f;
                        if (notargettime > 190)
                        {
                            NPC.active = false;
                        }
                        NPC.rotation = NPC.velocity.ToRotation();
                    }
                }

                if (bite)
                {
                    mouthRot -= 12;
                    if (mouthRot < -48)
                    {
                        bite = false;
                    }
                }
                else
                {
                    mouthRot *= 0.9f;
                }
                if (mouthRot < -48)
                {
                    mouthRot = -48;
                }
                if (phaseTrans > 120)
                {
                    foreach(var plr in Main.ActivePlayers)
                    {
                        plr.Calamity().infiniteFlight = true;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        Particle p = new Particle();
                        p.shape = 4;
                        p.position = NPC.Center - NPC.rotation.ToRotationVector2() * 60;
                        p.alpha = 1.6f * NPC.scale;
                        p.ad = 0.013f;
                        var r = Main.rand;
                        p.velocity = new Vector2((float)((r.NextDouble() - 0.5) * .3), (float)((r.NextDouble() - 0.5) * 1.3));
                        VoidParticles.particles.Add(p);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        Particle p = new Particle();
                        p.shape = 4;
                        p.position = NPC.Center - NPC.rotation.ToRotationVector2() * 60 - NPC.velocity * 0.5f;
                        p.alpha = 1.6f * NPC.scale;
                        p.ad = 0.013f;
                        var r = Main.rand;
                        p.velocity = new Vector2((float)((r.NextDouble() - 0.5) * .3), (float)((r.NextDouble() - 0.5) * 1.3));
                        VoidParticles.particles.Add(p);
                    }
                }
                if (tjv == 1)
                {
                    tjv = 0;
                    jv = true;
                    if (da < 0)
                    {
                        da = 1;
                    }
                    tail_vj = 12;
                }
                if (jv)
                {
                    da += tail_vj;
                    tail_vj -= 1.5f;
                    if (da < 0)
                    {
                        da = 0;
                        tail_vj = 0;
                        jv = false;
                        jaslowdown = 1;

                        int num = 8;
                        int counts = 3;
                        float speed = 9;
                        if (CalamityWorld.revenge)
                        {
                            num = 11;
                            counts = 4;
                            speed = 12;
                        }
                        if (CalamityWorld.death)
                        {
                            num = 11;
                            counts = 5;
                            speed = 18;
                        }
                        if (Main.expertMode)
                        {
                            num += 2;
                            speed *= 1.25f;
                        }
                        if (Main.masterMode)
                        {
                            num += 2;
                            counts += 1;
                            speed *= 1.4f;
                        }
                        if (ai == AIStyle.AroundPlayerAndShootVoidStar)
                        {
                            counts -= 2;
                            num /= 2;
                            speed *= 0.45f;
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            {
                                float angle = 0;
                                for (int i = 0; i < counts; i++)
                                {

                                    for (int j = 0; j < num; j++)
                                    {
                                        Projectile.NewProjectile(NPC.GetSource_FromAI(), bodies[bodies.Count - 1] - (bodies[bodies.Count - 2] - bodies[bodies.Count - 1]).SafeNormalize(Vector2.Zero) * 172 * NPC.scale, angle.ToRotationVector2() * speed, ModContent.ProjectileType<VoidStar>(), (int)(NPC.damage / 6.5f), 1);
                                        angle += ((float)Math.PI * 2 / (float)num);
                                    }
                                    angle += ((float)Math.PI * 2 / (float)num) / (float)counts;
                                    speed *= 0.7f;
                                }
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), bodies[bodies.Count - 1] - (bodies[bodies.Count - 2] - bodies[bodies.Count - 1]).SafeNormalize(Vector2.Zero) * 172 * NPC.scale, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), (int)(NPC.damage / 6f), 0);
                            }
                            {
                                if (Main.zenithWorld)
                                {
                                    for (int i = 1; i < bodies.Count; i++)
                                    {
                                        for (int _ = 0; _ < Main.rand.Next(3, 10); _++)
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromAI(), bodies[i] - (bodies[i - 1] - bodies[i]).SafeNormalize(Vector2.Zero) * 172 * NPC.scale, CEUtils.randomRot().ToRotationVector2() * speed * 3f, ModContent.ProjectileType<VoidStar>(), (int)(NPC.damage / 6.5f), 1);
                                        }
                                    }
                                }
                            }

                        }
                        if (Main.netMode != NetmodeID.Server)
                        {
                            SoundStyle sound = new SoundStyle("CalamityEntropy/Assets/Sounds/clap");
                            sound.Pitch = 1.4f;
                            SoundEngine.PlaySound(sound);
                            SoundEngine.PlaySound(SoundID.Item9);
                        }

                    }
                }
                else
                {
                    ja = (100f / ((float)NPC.velocity.Length() * 3)) * 5;
                    if (ja < 0)
                    {
                        ja = 0;
                    }

                    da = da + (ja - da) * 0.1f;

                }
            }
            NPC.netSpam = 0;
            NPC.netUpdate = true;
            vtodraw = NPC.Center;
            for (int i = 0; i < bodies.Count; i++)
            {
                Vector2 oPos;
                float oRot;

                if (i == 0)
                {
                    oPos = NPC.Center;
                    oRot = NPC.rotation;
                }
                else
                {
                    oPos = bodies[i - 1];
                    if (i == 1)
                    {
                        oRot = (NPC.Center - bodies[0]).ToRotation();
                    }
                    else
                    {
                        oRot = (bodies[i - 2] - bodies[i - 1]).ToRotation();
                    }
                }
                float rot = (oPos - bodies[i]).ToRotation();
                rot = CEUtils.RotateTowardsAngle(rot, oRot, 0.12f, false);

                int spacing = 80;
                bodies[i] = oPos - rot.ToRotationVector2() * spacing * NPC.scale;
            }
        }
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            if (aitype == 3)
            {
                npcHitbox = new Rectangle(0, 0, 0, 0);
                return true;
            }
            return false;
        }
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            if (aitype == 3)
            {
                modifiers.SetMaxDamage(0);
                modifiers.FinalDamage *= 0;
                modifiers.DisableSound();
                target.immuneTime = 10;
            }
        }
        public override void OnKill()
        {
            if (!EDownedBosses.downedCruiser)
            {
                if (!BossRushEvent.BossRushActive)
                {
                    VoidOreSystem.BlessWorldWithOre();
                }
            }

            NPC.SetEventFlagCleared(ref EDownedBosses.downedCruiser, -1);

        }

        public override bool CheckActive()
        {
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPosition, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
                return false;

            if (!candraw && !(phase == 1) && ModContent.GetInstance<Config>().EnablePixelEffect)
            {
                return false;
            }
            if (noaitime > 0)
            {
                return false;
            }
            if (whiteLerp > 0)
            {
                Effect shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/WhiteTrans", AssetRequestMode.ImmediateLoad).Value;
                shader.Parameters["strength"].SetValue(whiteLerp);
                Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
                shader.CurrentTechnique.Passes[0].Apply();
            }
            if (phaseTrans > 120)
            {
                int bd = 0;

                for (int d = 0; d < 9; d++)
                {
                    if (d == 0 || d == 2)
                    {
                        continue;
                    }
                    float rot = 0;
                    if (bd == 0)
                    {
                        rot = (vtodraw - bodies[d]).ToRotation();
                    }
                    else
                    {
                        rot = (bodies[d - 1] - bodies[d]).ToRotation();
                    }
                    Vector2 pos = bodies[d];

                    Texture2D tx;
                    tx = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/P2b" + (bd + 1).ToString()).Value;

                    spriteBatch.Draw(tx, pos - screenPosition, null, Color.White * alpha, rot, new Vector2(tx.Width, tx.Height) / 2, NPC.scale, SpriteEffects.None, 0f);

                    bd += 1;


                }
                Texture2D txd = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/Head2").Value;
                Texture2D j2 = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawUp2").Value;
                Texture2D j1 = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawDown2").Value;
                Vector2 joffset = new Vector2(54, 54);
                Vector2 ofs2 = joffset * new Vector2(1, -1);
                float roth = mouthRot * 0.8f;

                spriteBatch.Draw(j1, vtodraw - screenPosition + joffset.RotatedBy(NPC.rotation) * NPC.scale, null, Color.White * alpha, NPC.rotation + MathHelper.ToRadians(roth), new Vector2(28, 20), NPC.scale, SpriteEffects.None, 0);

                spriteBatch.Draw(j2, vtodraw - screenPosition + ofs2.RotatedBy(NPC.rotation) * NPC.scale, null, Color.White * alpha, NPC.rotation - MathHelper.ToRadians(roth), new Vector2(28, j2.Height - 20), NPC.scale, SpriteEffects.None, 0);

                spriteBatch.Draw(txd, vtodraw - screenPosition, null, Color.White * alpha, NPC.rotation, new Vector2(txd.Width, txd.Height) / 2, NPC.scale, SpriteEffects.None, 0f);

            }
            else
            {
                for (int d = 0; d <= bodies.Count - 1; d++)

                {
                    float rot = 0;
                    if (d == 0)
                    {
                        rot = (vtodraw - bodies[d]).ToRotation();
                    }
                    else
                    {
                        rot = (bodies[d - 1] - bodies[d]).ToRotation();
                    }
                    Vector2 pos = bodies[d];
                    Texture2D f1 = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/Flagellum").Value;
                    if (d == bodies.Count - 1)
                    {
                        Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/CruiserTail").Value;
                        spriteBatch.Draw(tx, pos - screenPosition, null, Color.White * alpha, rot, new Vector2(tx.Width, tx.Height) / 2, NPC.scale, SpriteEffects.None, 0f);

                    }
                    else
                    {
                        Texture2D tx;
                        if (d % 2 == 1)
                        {
                            tx = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/CruiserBodyAlt").Value;
                        }
                        else
                        {
                            tx = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/CruiserBody").Value;
                        }
                        spriteBatch.Draw(tx, pos - screenPosition, null, Color.White * alpha, rot, new Vector2(tx.Width, tx.Height) / 2, NPC.scale, SpriteEffects.None, 0f);

                    }
                    if (d == bodies.Count - 1 || Main.zenithWorld)
                    {
                        spriteBatch.Draw(f1, pos - screenPosition - new Vector2(36, 0).RotatedBy(rot) * NPC.scale, null, Color.White * alpha, rot + MathHelper.ToRadians(180 - da), new Vector2(0, f1.Height), NPC.scale, SpriteEffects.None, 0);
                        spriteBatch.Draw(f1, pos - screenPosition - new Vector2(36, 0).RotatedBy(rot) * NPC.scale, null, Color.White * alpha, rot + MathHelper.ToRadians(180 + da), new Vector2(0, 0), NPC.scale, SpriteEffects.FlipVertically, 0);

                    }

                }
                Texture2D txd = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/CruiserHead").Value;
                Texture2D j2 = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawUp").Value;
                Texture2D j1 = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawDown").Value;
                Vector2 joffset = new Vector2(42, 42);
                Vector2 ofs2 = joffset * new Vector2(1, -1);
                float roth = mouthRot;
                spriteBatch.Draw(j1, vtodraw - screenPosition + joffset.RotatedBy(NPC.rotation) * NPC.scale, null, Color.White * alpha, NPC.rotation + MathHelper.ToRadians(roth), new Vector2(58, j2.Height) / 2, NPC.scale, SpriteEffects.None, 0);

                spriteBatch.Draw(j2, vtodraw - screenPosition + ofs2.RotatedBy(NPC.rotation) * NPC.scale, null, Color.White * alpha, NPC.rotation - MathHelper.ToRadians(roth), new Vector2(58, j1.Height) / 2, NPC.scale, SpriteEffects.None, 0);

                spriteBatch.Draw(txd, vtodraw - screenPosition, null, Color.White * alpha, NPC.rotation, new Vector2(txd.Width, txd.Height) / 2, NPC.scale, SpriteEffects.None, 0f);

            }
            float lerp = phase == 2 ? 0.2f : 0.064f;
            if ((ai == AIStyle.VoidLaser && NPC.localAI[2] < 31) || (ai == AIStyle.TryToClosePlayer && CEUtils.getDistance(NPC.Center, NPC.target.ToPlayer().Center) > 1200))
            {
                WarningAlpha = float.Lerp(WarningAlpha, 1, lerp);
            }
            else { WarningAlpha = float.Lerp(WarningAlpha, 0, lerp); }
            if (WarningAlpha > 0.002f)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Texture2D w = CEUtils.getExtraTex("T3");
                Main.spriteBatch.Draw(w, NPC.Center - Main.screenPosition, null, Color.AliceBlue * 0.7f * WarningAlpha, NPC.rotation, new Vector2(0, w.Height / 2f), new Vector2(20, (phase == 1 ? 0.6f : 0.8f) * WarningAlpha), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(w, NPC.Center - Main.screenPosition, null, Color.AliceBlue * 0.7f * WarningAlpha, NPC.rotation, new Vector2(0, w.Height / 2f), new Vector2(20, (phase == 1 ? 0.5f : 0.65f) * WarningAlpha * WarningAlpha * WarningAlpha), SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }

            return false;
        }
        public float WarningAlpha = 0;

        public override void PostDraw(SpriteBatch sbb, Vector2 screenPos, Color drawColor)
        {
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
