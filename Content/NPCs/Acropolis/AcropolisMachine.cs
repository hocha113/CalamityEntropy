using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.MusicBoxes;
using CalamityEntropy.Content.Items.Tools;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityMod;
using CalamityMod.BiomeManagers;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.World;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.Utilities.Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    [AutoloadBossHead]
    public class AcropolisMachine : ModNPC
    {
        public class AcropolisLeg
        {
            public Vector2 StandPoint = Vector2.Zero;
            public float Scale = 1f;
            public NPC NPC;
            public Vector2 offset;
            public int NoMoveTime = 0;
            public Vector2 targetPos;
            public void NetSend(BinaryWriter writer)
            {
                writer.Write(NoMoveTime);
                writer.WriteVector2(targetPos);
                writer.WriteVector2(StandPoint);
            }
            public void NetReceive(BinaryReader reader)
            {
                NoMoveTime = reader.ReadInt32();
                targetPos = reader.ReadVector2();
                StandPoint = reader.ReadVector2();
            }
            public AcropolisLeg(NPC npc, Vector2 offset, float scale = 1)
            {
                NPC = npc;
                this.offset = offset;
                this.Scale = scale;
                StandPoint = npc.Center;
            }
            bool o = false;
            public bool OnTile => !CEUtils.isAir(StandPoint, true) && o;
            public bool Update()
            {
                if (NPC.ModNPC is AcropolisMachine am)
                {
                    if (am.Dummy || targetPos == Vector2.Zero)
                    {
                        StandPoint = Vector2.Lerp(StandPoint, NPC.Center + ((offset * new Vector2(0.36f, 2.2f)).RotatedBy(am.Dummy ? NPC.rotation : 0) * NPC.scale), 0.2f);
                        targetPos = StandPoint;
                        return false;
                    }
                }
                
                if (CEUtils.getDistance(StandPoint, targetPos) < ms * (NPC.velocity.Y > 1f ? 3 : 1))
                {
                    StandPoint = targetPos;
                }
                else
                {
                    StandPoint += (targetPos - StandPoint).normalize() * ms * (NPC.velocity.Y > 0.5f ? 3 : 1);
                }
                NoMoveTime--;
                float distToMove = 100 * NPC.scale;
                if (((AcropolisMachine)NPC.ModNPC).Jumping)
                {
                    o = false;
                    targetPos = NPC.Center + new Vector2(offset.X * 0.2f, 200) * NPC.scale;
                    ms = CEUtils.getDistance(targetPos, StandPoint) * 0.2f;
                    return false;
                }
                if (!OnTile || (NPC.boss && NoMoveTime <= 0 && CEUtils.getDistance(StandPoint, NPC.Center + NPC.velocity * 16 + (offset * NPC.scale).RotatedBy(((AcropolisMachine)NPC.ModNPC).dir > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi))) > distToMove) || ((NoMoveTime <= 0 || NPC.boss) && CEUtils.getDistance(StandPoint, NPC.Center + NPC.velocity * 16 + (offset * NPC.scale).RotatedBy(((AcropolisMachine)NPC.ModNPC).dir > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi))) > distToMove * 1.4f))
                {
                    if (!NPC.boss)
                        NoMoveTime = 8;
                    targetPos = FindStandPoint(NPC.Center + NPC.velocity * 16 + (offset * NPC.scale).RotatedBy(((AcropolisMachine)NPC.ModNPC).dir > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi)) + new Vector2(Math.Sign(NPC.velocity.X) == Math.Sign(offset.X) ? (Math.Sign(NPC.velocity.X) * 12) : 0, 0), 60 * Scale * NPC.scale, 128);
                    ms = CEUtils.getDistance(targetPos, StandPoint) * 0.2f;
                    if (NoMoveTime < 4)
                        NoMoveTime = 4;
                    return true;
                }

                return false;
            }
            public float ms;
            public Vector2 FindStandPoint(Vector2 center, float MaxOffset, float MaxTry = 64)
            {
                o = false;
                if (!NPC.boss)
                    center.Y -= 10;
                for (int i = 0; i < MaxTry; i++)
                {
                    Vector2 pos = CEUtils.randomPointInCircle(MaxTry) * new Vector2(1f, 1f) + center;
                    if (CEUtils.getDistance(pos, center) <= MaxOffset * 0.9f && CanStandOn(pos))
                    {
                        o = true;
                        Vector2 orgPos = pos;
                        int c = 128;
                        while (CanStandOn(pos))
                        {
                            c--;
                            pos.Y -= 2 * NPC.scale;
                            if (c <= 0)
                            {
                                return orgPos;
                            }
                        }
                        pos.Y += 2 * NPC.scale;
                        return pos;
                    }
                }
                if (!NPC.boss)
                    return Vector2.Zero;
                return NPC.Center + new Vector2(offset.X, 200).RotatedBy(((AcropolisMachine)NPC.ModNPC).dir > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi));
            }
        }
        public class Hand
        {
            public float Seg1RotV = 0;
            public float Seg1Length = 0;
            public float Seg1Rot = 0;
            public float Seg2Rot = 0;
            public float Seg1MaxRadians = MathHelper.ToRadians(50);
            public Vector2 offset;
            public NPC npc;
            public Vector2 DummyPos = Vector2.Zero;
            public Hand(NPC n, Vector2 offset, float seg1Length, float seg1Rot, float seg2Rot)
            {
                npc = n;
                Seg1Length = seg1Length;
                Seg1Rot = seg1Rot;
                Seg2Rot = seg2Rot;
                this.offset = offset;
                DummyPos = n.Center;
            }
            public Vector2 TopPos => seg1end + Seg2Rot.ToRotationVector2() * 60 * npc.scale;
            public void PointAPos(Vector2 pos)
            {
                Seg1Rot = CEUtils.RotateTowardsAngle(Seg1Rot, (pos - (npc.Center + (offset * new Vector2(((AcropolisMachine)npc.ModNPC).dir, 1)).RotatedBy(((AcropolisMachine)npc.ModNPC).dir > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi)))).ToRotation(), 0.06f, false);
                if (CEUtils.GetAngleBetweenVectors(Seg1Rot.ToRotationVector2(), -Vector2.UnitY) > Seg1MaxRadians * 2)
                {
                    if (Seg1Rot > (MathHelper.PiOver2 + Seg1MaxRadians))
                    {
                        Seg1Rot = (MathHelper.PiOver2 + Seg1MaxRadians);
                    }
                    if (Seg1Rot < (MathHelper.PiOver2 - Seg1MaxRadians))
                    {
                        Seg1Rot = (MathHelper.PiOver2 - Seg1MaxRadians);
                    }
                }
                Seg2Rot = CEUtils.RotateTowardsAngle(Seg2Rot, (pos - seg1end).ToRotation(), 0.06f, false);
            }
            public void NetSend(BinaryWriter writer)
            {
                writer.Write(Seg1Rot);
                writer.Write(Seg2Rot);
                writer.Write(Seg1RotV);
            }
            public void NetReceive(BinaryReader reader)
            {
                Seg1Rot = reader.ReadSingle();
                Seg2Rot = reader.ReadSingle();
                Seg1RotV = reader.ReadSingle();
            }

            public void Update()
            {
                if (!npc.boss)
                {
                    PointAPos(DummyPos);
                    DummyPos = Vector2.Lerp(DummyPos, npc.Center + offset + new Vector2(0, Seg1Length * npc.scale * 2), 0.3f);
                }
                Seg1Rot += Seg1RotV;
                Seg1RotV *= 0.96f;
            }
            public Vector2 seg1end => npc.Center + (offset * new Vector2(((AcropolisMachine)npc.ModNPC).dir, 1) * npc.scale).RotatedBy(((AcropolisMachine)npc.ModNPC).dir > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi)) + Seg1Rot.ToRotationVector2() * Seg1Length;
        }
        public bool JFlag = false;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = "CalamityEntropy/Assets/BCL/AcropolisMachine",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -4
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.Acropolis")
            });
        }
        public int DeathCounter = 240;
        public bool Defeated = false;

        public override bool CheckDead()
        {
            if (DeathCounter <= 0)
                return true;

            Defeated = true;
            NPC.dontTakeDamage = true;
            NPC.active = true;
            NPC.netUpdate = true;
            NPC.damage = 0;
            NPC.boss = true;
            NPC.life = 1;
            if (Main.dedServ)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);

            return false;
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return Defeated ? false : null;
        }
        public override bool? CanBeHitByItem(Player player, Item item)
        {
            return Defeated ? false : null;
        }
        public bool SetBoss = true;
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 8;
            target.AddBuff(ModContent.BuffType<MechanicalTrauma>(), 180);
        }
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
        }

        public override void SetDefaults()
        {
            NPC.width = 142;
            NPC.height = 132;
            NPC.damage = 26;
            NPC.defense = 8;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = CEUtils.GetSound("chainsaw_break");
            NPC.value = 1600f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 12;
            NPC.lavaImmune = true;
            NPC.scale = 1f;
            if (Main.getGoodWorld)
            {
                NPC.scale += 0.2f;
            }
            if (Main.zenithWorld)
            {
                NPC.scale += 0.8f;
            }
            NPC.boss = false;
            NPC.Calamity().VulnerableToHeat = false;
            NPC.Calamity().VulnerableToCold = true;
            NPC.Calamity().VulnerableToWater = true;
            SpawnModBiomes = new int[1] { ModContent.GetInstance<BrimstoneCragsBiome>().Type };
        }
        public override bool CheckActive()
        {
            return !NPC.boss;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return (spawnInfo.Player.Calamity().ZoneCalamity && !NPC.AnyNPCs(Type) && EModSys.AcropolisDontSpawn <= 0) ? (NPC.downedMoonlord ? 0.04f : (Main.hardMode ? 0.07f : 0.18f)) : 0f;
        }
        public static bool CanStandOn(Vector2 pos)
        {
            return !CEUtils.isAir(pos, true);
        }
        public bool CanStandOn(int x, int y)
        {
            if (!CEUtils.inWorld(x, y)) return false;
            return CanStandOn(new Vector2(x, y) * 16f);
        }
        public List<AcropolisLeg> legs = null;
        public Hand cannon;
        public Hand harpoon;
        public float TeslaCD = 120;
        public void SegCheck()
        {
            if (legs == null)
            {
                legs =
                [
                    new AcropolisLeg(NPC, new Vector2(-100, 120), 0.8f),
                    new AcropolisLeg(NPC, new Vector2(100, 120), 0.8f),
                    new AcropolisLeg(NPC, new Vector2(-140, 120), 1),
                    new AcropolisLeg(NPC, new Vector2(140, 120), 1),
                ];
                cannon = new Hand(NPC, new Vector2(-80, -32), 76, MathHelper.PiOver2, MathHelper.PiOver2);
                harpoon = new Hand(NPC, new Vector2(60, -18), 66, MathHelper.PiOver2, MathHelper.PiOver2);
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return NPC.boss;
        }
        public LoopSound chargeSnd = null;
        public int dcounter = 0;
        public bool Dummy = false;
        public override void AI()
        {
            NPC.chaseable = NPC.boss;
            JumpCD--;
            SegCheck();
            cannon.Update();
            harpoon.Update();
            if (_harpoon == -1 && (!(Main.netMode == NetmodeID.MultiplayerClient)))
            {
                _harpoon = NPC.NewNPC(NPC.GetSource_FromAI(), 0, 0, ModContent.NPCType<Harpoon>(), 0, NPC.whoAmI);
                _harpoon.ToNPC().Center = HarpoonPos;
                _harpoon.ToNPC().netSpam = 9;
                _harpoon.ToNPC().netUpdate = true;
            }
            foreach (var l in legs)
            {
                if (l.Update())
                {
                    foreach (var l2 in legs)
                    {
                        if (Math.Sign(l2.offset.X) == Math.Sign(l.offset.X))
                        {
                            if (l2.NoMoveTime < 8)
                            {
                                l2.NoMoveTime = 8;
                            }
                        }
                    }
                }
            }
            if (NPC.life < 2)
                Defeated = true;
            if (Defeated)
            {
                NPC.netUpdate = true;
                if (NPC.netSpam >= 10)
                    NPC.netSpam = 9;
                int d = 1;
                if (CalamityWorld.death)
                    if (Main.GameUpdateCount % 2 == 0)
                        d++;
                if (Main.zenithWorld)
                    d = 1;
                DeathCounter -= d;
                NPC.velocity *= 0;
                Jumping = false;
                CannonUpAtk = -1;
                JumpAndShoot = -1;
                if (!Main.dedServ)
                {
                    if (chargeSnd == null)
                    {
                        chargeSnd = new LoopSound(CalamityEntropy.ofCharge);
                        chargeSnd.instance.Pitch = 0;
                        chargeSnd.instance.Volume = 0;
                        chargeSnd.play();
                        chargeSnd.timeleft = 2;
                    }
                    if (chargeSnd != null)
                    {
                        chargeSnd.setVolume_Dist(NPC.Center, 400, 1800, 1);
                        chargeSnd.instance.Pitch = (1 - (DeathCounter / 240f)) * 3f;
                        chargeSnd.timeleft = 2;
                    }
                    if (Main.GameUpdateCount % 2 == 0)
                    {
                        ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, Utils.Remap(Main.LocalPlayer.Center.Distance(NPC.Center), 4000, 1000, 0, 5)));
                        EParticle.NewParticle(new ShockParticle(), NPC.Center, Vector2.Zero, Color.White, 0.1f * NPC.scale, 1, true, BlendState.NonPremultiplied, CEUtils.randomRot());
                    }
                }

                if (DeathCounter < 0)
                {
                    if (chargeSnd != null)
                        chargeSnd.timeleft = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.dontTakeDamage = false;
                        NPC.StrikeInstantKill();
                        NPC.netUpdate = true;
                    }
                }
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                }
                return;
            }

            if (!NPC.HasValidTarget)
            {
                NPC.TargetClosest();
            }
            if (((float)NPC.life / NPC.lifeMax) < 0.98f)
            {
                if (SetBoss)
                {
                    SetBoss = false;
                    if (!Main.dedServ)
                    {
                        Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/HellBlazenRobotics");
                    }
                    NPC.boss = true;
                }
                NPC.noTileCollide = true;
                if (NPC.HasValidTarget && NPC.target.ToPlayer().Distance(NPC.Center) < 6000)
                {
                    AttackPlayer(Main.player[NPC.target]);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NPC.netUpdate = true;
                    }
                    dcounter = 0;
                }
                else
                {
                    dcounter++;
                    NPC.velocity.X += 0.25f;
                    if (CEUtils.CheckSolidTile(NPC.getRect()))
                    {
                        NPC.velocity.Y -= 0.4f;
                    }
                    else
                    {
                        NPC.velocity.Y += 0.7f;
                    }
                    if (dcounter > 295)
                    {
                        NPC.active = false;
                    }
                }
            }
            else
            {
                NPC.boss = false;
                NPC.noTileCollide = false;
                NPC.velocity.Y += 0.4f;
                if (CEUtils.CheckSolidTile(NPC.getRect()))
                    NPC.velocity.Y = 0;
            }
            if (Jumping)
            {
                NPC.velocity.Y += 0.4f * NPC.scale;
            }
            else
            {
                NPC.velocity *= 0.97f;
            }
            Dummy = false;
            bool ff = CEUtils.CheckSolidTileOrPlatform(new Rectangle((int)NPC.position.X, (int)NPC.position.Y, NPC.width, (int)(NPC.height * 1.4f)));
            if (!NPC.boss)
            {
                if (ff)
                    NPC.velocity.X *= 0.96f;
                else
                {
                    Dummy = true;
                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, NPC.velocity.X * 0.04f, 0.16f, false);
                }
            }
            if (Jumping)
            {
                NPC.rotation = (Math.Abs(NPC.velocity.X * 0.04f).ToRotationVector2() * new Vector2(dir, 1)).ToRotation();
            }
            else if (NPC.boss || ff)
            {
                Vector2 lr = Vector2.Zero;
                Vector2 rr = Vector2.Zero;
                int lc = 0;
                int rc = 0;
                int ontile = 0;
                foreach (var leg in legs)
                {
                    if (leg.OnTile)
                        ontile++;
                    else
                    {
                        continue;
                    }
                    if (leg.offset.X < 0 && leg.OnTile)
                    {
                        lr += leg.StandPoint;
                        lc++;
                    }
                    if (leg.offset.X > 0 && leg.OnTile)
                    {
                        rr += leg.StandPoint;
                        rc++;
                    }
                }
                if (ontile > 2)
                {
                    if (lc > 0 && rc > 0)
                    {
                        float r = ((rr / rc) - (lr / lc)).ToRotation();
                        float maxr = MathHelper.ToRadians(60);
                        if (r > maxr)
                            r = maxr;
                        if (r < -maxr)
                        {
                            r = -maxr;
                        }
                        if (dir < 0)
                        {
                            r += MathHelper.Pi;
                        }
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, r, 0.12f, false);
                    }
                    else if (lc > rc)
                    {
                        NPC.rotation += 0.16f;
                    }
                    else if (lc < rc)
                    {
                        NPC.rotation -= 0.16f;
                    }
                    else
                    {
                        float r = dir == 1 ? 0 : MathHelper.Pi;
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, r, 0.3f, false);
                    }
                }
                else
                {
                    float r = dir == 1 ? 0 : MathHelper.Pi;
                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, r, 0.12f, false);
                }
            }
        }
        public int JumpCD = 0;
        public bool Jumping = false;
        public float TeslaUpCD = 0;
        public int JumpAndShoot = 0;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.boss);
            writer.Write(CannonUpAtk);
            cannon.NetSend(writer);
            harpoon.NetSend(writer);
            writer.Write(_harpoon);
            writer.Write(legs.Count > 0);
            if (legs.Count > 0)
            {
                foreach (var le in legs)
                {
                    le.NetSend(writer);
                }
            }
            writer.Write(JumpCD);
            writer.Write(Jumping);
            writer.Write(TeslaCD);
            writer.Write(HarpoonCD);
            writer.Write(JumpAndShoot);
            writer.Write(CannonUpAtk);
            writer.Write(Defeated);
            writer.Write(DeathCounter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            SegCheck();
            NPC.boss = reader.ReadBoolean();
            CannonUpAtk = reader.ReadInt32();
            cannon.NetReceive(reader);
            harpoon.NetReceive(reader);
            _harpoon = reader.ReadInt32();
            if (reader.ReadBoolean())
            {
                if (legs.Count > 0)
                {
                    foreach (var le in legs)
                    {
                        le.NetReceive(reader);
                    }
                }
            }
            else
            {
                for (int a = 0; a < 4; a++)
                    new AcropolisLeg(NPC, Vector2.Zero).NetReceive(reader);
            }
            JumpCD = reader.ReadInt32();
            Jumping = reader.ReadBoolean();
            TeslaCD = reader.ReadSingle();
            HarpoonCD = reader.ReadSingle();
            JumpAndShoot = reader.ReadInt32();
            CannonUpAtk = reader.ReadInt32();
            Defeated = reader.ReadBoolean();
            DeathCounter = reader.ReadInt32();
        }
        public int _harpoon = -1;
        public void AttackPlayer(Player player)
        {
            float enrange = 1 + (1 - (float)NPC.life / NPC.lifeMax);
            if (Main.expertMode)
            {
                enrange += 0.07f;
            }
            if (Main.masterMode)
            {
                enrange += 0.07f;
            }
            if (CalamityWorld.revenge)
            {
                enrange += 0.1f;
            }
            if (CalamityWorld.death)
            {
                enrange += 0.1f;
            }
            if (CalamityEntropy.EntropyMode)
            {
                enrange += 0.4f;
                enrange *= 1.15f;
            }
            if (Main.getGoodWorld)
            {
                enrange *= 1.3f;
            }
            if (Main.zenithWorld)
            {
                enrange *= 0.88f;
            }
            float d = CEUtils.getDistance(player.Center, NPC.Center);
            if (CannonUpAtk-- > 0)
            {
                cannon.PointAPos(player.Center + new Vector2(0, -800));
                if (CannonUpAtk < 140)
                {
                    TeslaUpCD -= enrange;
                    if (TeslaUpCD <= 0)
                    {
                        TeslaUpCD = 12;
                        Shoot<AcropolisTeslaBall>(cannon.TopPos, cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(0.6f) * 5, 1, -1, NPC.whoAmI);
                        CEUtils.PlaySound("ofshoot", 1, cannon.TopPos);
                        cannon.Seg1RotV = 0.06f * dir;
                    }
                }
            }
            else if (JumpAndShoot-- > 0)
            {
                for(int i = 0; i < 2; i++)
                    cannon.PointAPos(NPC.Center + cannon.offset * NPC.scale + new Vector2(0, 220)); //(player.Center + new Vector2(NPC.velocity.normalize().X * 400, -160));
                TeslaUpCD -= enrange;
                if (TeslaUpCD <= 0)
                {
                    TeslaUpCD = 23;
                    Shoot<AcropolisTeslaBall>(cannon.TopPos, cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(0.03f) * 3, 1, 1, NPC.whoAmI);
                    CEUtils.PlaySound("ofshoot", 1, cannon.TopPos);
                }
            }
            else
            {
                cannon.PointAPos(player.Center + new Vector2(0, -14) + new Vector2(0, d > 500 ? -(((d - 500) * 0.02f) * ((d - 500) * 0.02f)) : 0));
            }
            if (!Jumping)
            {
                JumpAndShoot = -1;
            }
            if (HarpoonCharge <= 0.8f && HarpoonOnLauncher)
            {
                harpoon.PointAPos(player.Center);
            }
            else if (!HarpoonOnLauncher)
            {
                harpoon.PointAPos(_harpoon.ToNPC().Center);
            }
            TeslaCD -= enrange;
            if (TeslaCD <= 0)
            {
                if (Main.rand.NextBool(6))
                {
                    TeslaCD = 360;
                    CannonUpAtk = 200;
                }
                else if (Main.rand.NextBool(6) && !Jumping && HarpoonOnLauncher)
                {
                    Jumping = true;
                    NPC.velocity = new Vector2(12f * Math.Sign(player.Center.X - NPC.Center.X) / NPC.scale, -24) * NPC.scale;
                    JumpCD = 200;
                    JumpAndShoot = 200;
                    TeslaCD = 360;
                    TeslaUpCD = 30;
                }
                else
                {
                    TeslaCD = 160;
                    Shoot<AcropolisTeslaBall>(cannon.TopPos, cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(0.1f) * 6, 1, 0, NPC.whoAmI);
                    CEUtils.PlaySound("ofshoot", 1, cannon.TopPos);
                    cannon.Seg1RotV = 0.2f * dir;
                }
            }
            if (HarpoonOnLauncher)
            {
                HarpoonCD -= enrange;
            }
            if (HarpoonCD <= 0)
            {
                HarpoonCharge += 0.01f * enrange;
                if (HarpoonCharge >= 1)
                {
                    HarpoonCharge = 0;
                    HarpoonCD = 160;
                    Harpoon hp = ((Harpoon)_harpoon.ToNPC().ModNPC);
                    hp.Back = 40;
                    hp.OnLauncher = false;
                    _harpoon.ToNPC().velocity = harpoon.Seg2Rot.ToRotationVector2() * 36 * NPC.scale;
                    harpoon.Seg1RotV = 0.3f * dir;
                    CEUtils.PlaySound("chainsawHit", 1, NPC.Center);
                }
            }
            if (!Jumping)
            {
                bool flag = false;
                int c = 0;
                foreach (var l in legs)
                {
                    if (l.OnTile)
                    {
                        c++;
                    }
                }
                if (c >= 3)
                {
                    flag = true;
                    if (JFlag)
                    {
                        JFlag = false;
                        if (NPC.velocity.Y > 0)
                            NPC.velocity.Y = 0;
                    }
                }
                if (flag || CEUtils.CheckSolidTile(NPC.getRect()))
                {
                    if (HarpoonOnLauncher && player.Center.Y + 200 * NPC.scale < NPC.Center.Y)
                    {
                        if (JumpCD <= -260)
                        {
                            Jumping = true;
                            NPC.velocity = new Vector2(0.01f * (player.Center.X - NPC.Center.X) / NPC.scale, float.Max((player.Center.Y - NPC.Center.Y) / NPC.scale * 0.08f, -30)) * NPC.scale;
                            JumpCD = 160;
                        }
                    }
                    float yof = -90 * NPC.scale;
                    if (NPC.Center.Y - yof + 90 * NPC.scale * NPC.scale > player.Center.Y)
                    {
                        if (NPC.velocity.Y > 2 * NPC.scale)
                        {
                            NPC.velocity.Y = 2 * NPC.scale;
                        }
                        if (NPC.velocity.Y > 0)
                            NPC.velocity.Y *= 0.84f;
                    }
                    float v = 0.2f;
                    if (Math.Abs(NPC.Center.Y + yof - player.Center.Y) > 150 * NPC.scale)
                    {
                        v = 1;
                    }
                    if (Math.Abs(NPC.Center.Y + yof - player.Center.Y) < 14 * NPC.scale)
                    {
                        v = 0;
                        NPC.velocity.Y *= 0.8f;
                    }
                    v *= NPC.scale;
                    if (HarpoonOnLauncher && Math.Abs(yof + player.Center.Y - NPC.Center.Y) > 20 * NPC.scale)
                    {
                        if (player.Center.Y + yof > NPC.Center.Y)
                        {
                            NPC.velocity.Y += 0.4f * enrange * v;
                        }
                        else
                        {
                            bool f = true;
                            bool f2 = false;
                            foreach (var l in legs)
                            {
                                if (l.OnTile && l.StandPoint.Y > NPC.Center.Y + 110 * NPC.scale)
                                {
                                    f = false;
                                }
                                if (l.OnTile && l.StandPoint.Y > NPC.Center.Y + 130 * NPC.scale)
                                {
                                    f2 = true;
                                }
                            }
                            if (f || CEUtils.CheckSolidTile(NPC.getRect()))
                                NPC.velocity.Y -= 0.6f * enrange * v;
                            else if (f2)
                                NPC.velocity.Y += 2f * enrange * v;
                        }
                    }
                }
                else
                {
                    NPC.velocity.Y += 0.42f;
                    if (NPC.velocity.Y > 12)
                        NPC.velocity.Y = 12;
                }
                if (HarpoonOnLauncher)
                {
                    if (CEUtils.getDistance(NPC.Center, player.Center) > 100 * NPC.scale)
                    {
                        NPC.velocity.X += Math.Sign(player.Center.X - NPC.Center.X) * 0.1f * enrange * NPC.scale;
                    }
                }
            }
            else
            {
                bool flag = false;
                int c = 0;
                foreach (var l in legs)
                {
                    if (l.OnTile)
                    {
                        c++;
                    }
                }
                if (c > 2)
                {
                    flag = true;
                }
                if (JumpAndShoot <= 150 && NPC.ai[2] <= 0)
                {
                    if (((NPC.velocity.Y > 0 && !(JumpAndShoot > 0)) || NPC.Center.Y < player.Center.Y) && flag)
                    {
                        //Jumping = false;
                        //NPC.velocity *= 0;
                    }
                    if (JumpCD < 20 || (NPC.velocity.Y > 0 && CEUtils.CheckSolidTileOrPlatform(new Rectangle((int)NPC.position.X, (int)NPC.position.Y, NPC.width, (int)(NPC.height * 1.4f)))) && NPC.velocity.Y > 8)
                    {
                        Jumping = false;
                        NPC.velocity *= 0;
                    }
                }
                JFlag = true;
            }
            if (NPC.velocity.X > 0)
            {
                if (dir == -1)
                    NPC.rotation += MathHelper.Pi;
                dir = 1;
            }
            if (NPC.velocity.X < 0)
            {
                if (dir == 1)
                    NPC.rotation += MathHelper.Pi;
                dir = -1;
            }
            if (NPC.ai[2]-- > 0)
            {
                NPC.velocity = (_harpoon.ToNPC().Center - NPC.Center).normalize() * 40;
            }
        }
        public bool HarpoonOnLauncher
        {
            get
            {
                if (_harpoon.ToNPC().ModNPC is Harpoon)
                {
                    return ((Harpoon)_harpoon.ToNPC().ModNPC).OnLauncher;
                }
                return false;
            }
        }
        public void Shoot<T>(Vector2 pos, Vector2 velocity, float damageMult = 1, float ai0 = 0, float ai1 = 0, float ai2 = 0) where T : ModProjectile
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int baseDamage = (int)(NPC.damage / 6.2f);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, velocity, ModContent.ProjectileType<T>(), (int)(baseDamage * damageMult), 4, -1, ai0, ai1, ai2);
            }
        }
        public int phase = 1;
        public int dir = 1;
        public int CannonUpAtk = 0;
        public float HarpoonCharge = 0;
        public float HarpoonCD = 120;
        public static void PrepareCharredShader(Texture2D tex, Texture2D noise, float minAlpha, Vector2 noiseOffset, Color color)
        {
            Effect shader = CommonEffects.charred;
            if (minAlpha >= 1)
            {
                Main.spriteBatch.ExitShaderRegion();
            }
            else
            {
                Main.spriteBatch.End();
                shader.Parameters["minAlpha"].SetValue(minAlpha);
                shader.Parameters["cColor"].SetValue(color.ToVector4());
                shader.Parameters["noiseOffset"].SetValue(noiseOffset);
                shader.Parameters["texSize"].SetValue(tex.Size());
                shader.Parameters["noiseSize"].SetValue(noise.Size() * 2f);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = tex;
                gd.Textures[1] = noise;
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UnifiedRandom random = new UnifiedRandom((NPC.type + NPC.whoAmI * 47));
            Texture2D noise = CEUtils.getExtraTex("cloudNoise");
            float cAlpha = random.NextBool(6) ? random.NextFloat(0.6f, 1f) : random.NextFloat(0.8f, 1f);
            void prepareShader(Texture2D texture)
            {
                float al = float.Clamp(cAlpha + random.NextFloat(-0.1f, 0.1f), 0, 1);
                if (random.NextBool(5))
                    al = 0;
                Vector2 ofs = new Vector2(random.NextFloat(0, 0.5f), random.NextFloat(0, 0.5f)) * noise.Size();
                PrepareCharredShader(texture, noise, al, ofs, Color.Black * 0.4f);
            }
            if (Main.zenithWorld)
            {
                drawColor = Main.DiscoColor;
            }
            Texture2D body = NPC.getTexture();
            /*if (legs != null)
            {
                foreach (var l in legs)
                {
                    CEUtils.DrawGlow(l.StandPoint, Color.White, 0.2f);
                    CEUtils.DrawGlow(l.StandPoint, Color.White, 0.2f);

                    CEUtils.DrawGlow(l.offset + NPC.Center, Color.Blue, 0.2f, false);
                    CEUtils.DrawGlow(l.offset + NPC.Center, Color.Blue, 0.2f, false);
                }
            }*/
            Texture2D t1 = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/Leg1");
            Texture2D t2 = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/Leg2");
            Texture2D t3 = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/Foot");

            if (legs == null)
                return false;
            foreach (var leg in legs)
            {
                float l1 = 46 * NPC.scale * leg.Scale;
                float l2 = 70 * NPC.scale * leg.Scale;
                float l3 = 72 * NPC.scale * leg.Scale;
                List<Vector2> points = new List<Vector2>();
                points.Add(NPC.Center + (new Vector2(Math.Sign(leg.offset.X) * 20, 60) * NPC.scale).RotatedBy(dir > 0 ? NPC.rotation : -MathHelper.Pi + NPC.rotation));
                Vector2 e = CalculateLegJoints(points[0], leg.StandPoint, l1, l2, l3, out var p1, out var p2);
                points.Add(p1);
                points.Add(CEUtils.GetCircleIntersection(p1, l2, leg.StandPoint, l3));
                points.Add(points[points.Count - 1] + (leg.StandPoint - points[points.Count - 1]).normalize() * l3);

                prepareShader(t1);
                Main.EntitySpriteDraw(t1, points[0] - Main.screenPosition, null, drawColor, (points[1] - points[0]).ToRotation(), new Vector2(4, 13), NPC.scale * leg.Scale, SpriteEffects.None);
                prepareShader(t2);
                Main.EntitySpriteDraw(t2, points[1] - Main.screenPosition, null, drawColor, (points[2] - points[1]).ToRotation(), new Vector2(6, 9), NPC.scale * leg.Scale, SpriteEffects.None);
                prepareShader(t3);
                Main.EntitySpriteDraw(t3, points[2] - Main.screenPosition, null, drawColor, (points[3] - points[2]).ToRotation() + ((leg.offset.X > 0 ? 1 : -1) * MathHelper.ToRadians(24)), new Vector2(27, t3.Height / 2f), NPC.scale * leg.Scale, leg.offset.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
                //CEUtils.DrawLines(points, Color.Blue, 4);
            }
            NPC npc = NPC;
            Texture2D cannon1 = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/CannonConnect");
            Texture2D cannon2 = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/Cannon");
            Texture2D harpoon1 = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/HarpoonArm");
            Texture2D harpoon2 = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/HarpoonLauncher");
            Texture2D harpoon3 = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/Harpoon");
            Texture2D harpoonOutline = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/HarpoonOutline");

            Texture2D shoulder = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/Shoulder");

            prepareShader(harpoon1);
            Main.EntitySpriteDraw(harpoon1, (harpoon.offset * new Vector2(dir, 1) * NPC.scale).RotatedBy(((AcropolisMachine)npc.ModNPC).dir > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi)) + NPC.Center - Main.screenPosition, null, drawColor, harpoon.Seg1Rot, new Vector2(6, harpoon1.Height / 2f), NPC.scale, SpriteEffects.None);
            if (_harpoon < 0 || (((Harpoon)_harpoon.ToNPC().ModNPC).OnLauncher))
            {
                Main.spriteBatch.ExitShaderRegion();
                for (float r = 0; r <= 360; r += 60)
                {
                    Main.EntitySpriteDraw(harpoonOutline, MathHelper.ToRadians(r).ToRotationVector2() * 2 + harpoon.seg1end + harpoon.Seg2Rot.ToRotationVector2() * 150 * NPC.scale + new Vector2(0, 10 * dir).RotatedBy(harpoon.Seg2Rot) * NPC.scale - Main.screenPosition, null, Color.OrangeRed * HarpoonCharge, harpoon.Seg2Rot, new Vector2(70, harpoon3.Height / 2f), NPC.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
                }
                prepareShader(harpoon3);
                Main.EntitySpriteDraw(harpoon3, harpoon.seg1end + harpoon.Seg2Rot.ToRotationVector2() * 150 * NPC.scale + new Vector2(0, 10 * dir).RotatedBy(harpoon.Seg2Rot) * NPC.scale - Main.screenPosition, null, drawColor, harpoon.Seg2Rot, new Vector2(70, harpoon3.Height / 2f), NPC.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            }
            else
            {
                //Don't mess up the random
                prepareShader(harpoon3);
            }
            prepareShader(harpoon2);
            Main.EntitySpriteDraw(harpoon2, harpoon.seg1end - Main.screenPosition, null, drawColor, harpoon.Seg2Rot, new Vector2(6, harpoon2.Height / 2f), NPC.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            prepareShader(body);
            Main.EntitySpriteDraw(body, NPC.Center - screenPos, null, drawColor, NPC.rotation, body.Size() / 2f, NPC.scale, dir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
            prepareShader(cannon1);
            Main.EntitySpriteDraw(cannon1, (cannon.offset * new Vector2(dir, 1) * NPC.scale).RotatedBy(((AcropolisMachine)npc.ModNPC).dir > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi)) + NPC.Center - Main.screenPosition, null, drawColor, cannon.Seg1Rot, new Vector2(6, cannon1.Height / 2f), NPC.scale, SpriteEffects.None);
            prepareShader(cannon2);
            Main.EntitySpriteDraw(cannon2, cannon.seg1end - Main.screenPosition, null, drawColor, cannon.Seg2Rot, new Vector2(6, cannon2.Height / 2f), NPC.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);

            prepareShader(shoulder);
            Main.EntitySpriteDraw(shoulder, NPC.Center - screenPos, null, drawColor, NPC.rotation, shoulder.Size() / 2f, NPC.scale, dir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);

            return false;
        }
        public Vector2 HarpoonPos => harpoon.seg1end + harpoon.Seg2Rot.ToRotationVector2() * 150 * NPC.scale + new Vector2(0, 10 * dir).RotatedBy(harpoon.Seg2Rot) * NPC.scale;
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            //npcHitbox = npcHitbox.Center.ToVector2().getRectCentered((npcHitbox.Width * NPC.scale), (npcHitbox.Height * NPC.scale));
            return false;
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (DeathCounter <= 0)
            {
                if (chargeSnd != null)
                    chargeSnd.timeleft = 0;
                if (NPC.life <= 0 && !Main.dedServ)
                {

                    if (Main.zenithWorld)
                    {
                        EParticle.spawnNew(new RealisticExplosion(), NPC.Center, Vector2.Zero, Color.White, 18f * NPC.scale, 1, true, BlendState.AlphaBlend);
                    }
                    else
                    {
                        GeneralParticleHandler.SpawnParticle(new PulseRing(NPC.Center, Vector2.Zero, Color.Firebrick, 0.1f, 7f, 8));
                        EParticle.spawnNew(new ShineParticle(), NPC.Center, Vector2.Zero, Color.Firebrick, 14f, 1, true, BlendState.Additive, 0, 16);
                        EParticle.spawnNew(new ShineParticle(), NPC.Center, Vector2.Zero, Color.White, 10f, 1, true, BlendState.Additive, 0, 16);
                        ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.Zero, 100), CEUtils.getDistance(NPC.Center, Main.LocalPlayer.Center), 1200);

                        for (int i = 0; i < 40; i++)
                        {
                            EParticle.NewParticle(new EMediumSmoke(), NPC.Center + CEUtils.randomPointInCircle(60 * NPC.scale), CEUtils.randomPointInCircle(32 * NPC.scale), Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()), Main.rand.NextFloat(1f, 4f) * NPC.scale, 1, true, BlendState.AlphaBlend, CEUtils.randomRot(), 120);
                        }
                    }
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore0").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore1").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore2").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore3").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore4").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore4").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore4").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore4").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore5").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore5").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore5").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore5").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore6").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore7").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore7").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore7").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore7").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore8").Type, NPC.scale);
                    Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), Mod.Find<ModGore>("AcrGore9").Type, NPC.scale);
                }
            }
        }
        public Vector2 CalculateLegJoints(Vector2 Center, Vector2 legStandPoint, float l1, float l2, float l3, out Vector2 P1, out Vector2 P2)
        {
            P1 = Vector2.Zero;
            P2 = Vector2.Zero;

            if (l1 <= 0 || l2 <= 0 || l3 <= 0)
            {
                return Center;
            }

            Vector2 D = legStandPoint - Center;
            float dist = D.Length();

            Vector2 target = legStandPoint;
            if (dist > l1 + l2 + l3)
            {
                target = Center + Vector2.Normalize(D) * (l1 + l2 + l3);
            }

            Vector2 downDirection = new Vector2(0, 1);
            Vector2 targetDirection = D.Length() > 0 ? Vector2.Normalize(D) : downDirection;

            float maxDeflectionAngle = MathHelper.ToRadians(68);
            float angleToTarget = (float)Math.Atan2(targetDirection.Y, targetDirection.X) - (float)Math.PI / 2; // 相对于Y轴正方向
            float deflectionAngle = MathHelper.Clamp(angleToTarget, -maxDeflectionAngle, maxDeflectionAngle);

            float cosAngle = (float)Math.Cos(deflectionAngle);
            float sinAngle = (float)Math.Sin(deflectionAngle);
            Vector2 firstSegmentDirection = new Vector2(
                downDirection.X * cosAngle - downDirection.Y * sinAngle,
                downDirection.X * sinAngle + downDirection.Y * cosAngle
            );

            P1 = Center + l1 * firstSegmentDirection;

            float y2 = target.Y - l3;
            float deltaY = y2 - P1.Y;
            float deltaX;
            try
            {
                deltaX = (float)Math.Sqrt(l2 * l2 - deltaY * deltaY);
            }
            catch
            {
                deltaX = 0;
                y2 = P1.Y - l2;
            }

            float x2_positive = P1.X + deltaX;
            float x2_negative = P1.X - deltaX;
            float x2 = (Math.Abs(x2_positive - target.X) < Math.Abs(x2_negative - target.X)) ? x2_positive : x2_negative;

            P2 = new Vector2(x2, y2);

            float distP2ToTarget = Vector2.Distance(P2, target);
            if (Math.Abs(distP2ToTarget - l3) > 0.001f)
            {
                P2 = new Vector2(P1.X, P1.Y - l2);
                target = new Vector2(P2.X, P2.Y + l3);
            }

            return target;
        }

        public override void OnKill()
        {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedAcropolis, -1);
            int dmg = 40;
            if (Main.expertMode)
                dmg *= 2;
            if (Main.masterMode || CalamityWorld.death)
                dmg *= 2;
            dmg = (int)(dmg * NPC.scale);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                CEUtils.SpawnExplotionHostile(NPC.GetSource_FromAI(), NPC.Center, dmg, 500 * NPC.scale, true);
        }
        public override void BossLoot(ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HellIndustrialComponents>(), 1, 24, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DubiousPlating>(), 1, 18, 36));
            npcLoot.Add(ModContent.ItemType<MottledSpear>(), new Fraction(2, 5));
            npcLoot.DefineConditionalDropSet(DropHelper.RevAndMaster).Add(ModContent.ItemType<AcropolisRelic>());
            npcLoot.Add(ModContent.ItemType<AcropolisTrophy>(), 10);
            npcLoot.Add(ModContent.ItemType<AzafurePhonograph>(), 8);
            npcLoot.AddConditionalPerPlayer(() => !EDownedBosses.downedAcropolis, ModContent.ItemType<LoreAcropolis>());
        }
    }
}
