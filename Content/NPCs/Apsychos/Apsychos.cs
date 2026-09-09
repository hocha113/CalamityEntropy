using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles.ApsychosProjs;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    [AutoloadBossHead]
    public class Apsychos : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.44f,
                PortraitScale = 0.3f,
                CustomTexturePath = "CalamityEntropy/Assets/BCL/Apsychos",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -90
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheUnderworld,
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.ApsychosBestiary")
            });
        }

        public Vector2 vec1 = Vector2.Zero;
        public Vector2 vec2 = Vector2.Zero;
        public float num1 = 0;
        public float num2 = 0;
        public float num3 = 0;
        #region Sync
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(TailNPCIndex);
            writer.WriteVector2(vec1);
            writer.WriteVector2(vec2);
            writer.Write(num1);
            writer.Write(num2);
            writer.Write(num3);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            TailNPCIndex = reader.ReadInt32();
            vec1 = reader.ReadVector2();
            vec2 = reader.ReadVector2();
            num1 = reader.ReadSingle();
            num2 = reader.ReadSingle();
            num3 = reader.ReadSingle();
        }
        #endregion
        #region Tail
        public class TailSeg
        {
            public Vector2 Center = Vector2.Zero;
            public float rotation = 0;
        }
        public int TailNPCIndex = -1;
        public NPC tail;
        public enum TailStyle
        {
            Follow,
            OnePoint,
            TwoPoint
        }
        public List<TailSeg> segs = null;
        public TailStyle tailStyle = TailStyle.Follow;
        public void UpdateTail()
        {
            if (segs == null)
            {
                segs = new List<TailSeg>();
                for (int i = 0; i < 12; i++)
                {
                    segs.Add(new TailSeg() { Center = NPC.Center, rotation = NPC.rotation + MathHelper.Pi });
                }
            }
            if (tailStyle == TailStyle.Follow)
            {
                for (int i = 0; i < segs.Count; i++)
                {
                    float fRot = i == 0 ? NPC.rotation + MathHelper.Pi : segs[i - 1].rotation;
                    Vector2 fPos = i == 0 ? NPC.velocity + NPC.Center - NPC.rotation.ToRotationVector2() * 70 * NPC.scale : segs[i - 1].Center;
                    float spacing = 46 * NPC.scale;
                    segs[i].rotation = CEUtils.RotateTowardsAngle((segs[i].Center - fPos).ToRotation(), fRot, 0.12f, false);
                    segs[i].Center = fPos + segs[i].rotation.ToRotationVector2() * spacing;
                }
                if (tail != null)
                {
                    int c = segs.Count;
                    float fRot = segs[c - 1].rotation;
                    Vector2 fPos = segs[c - 1].Center;
                    float spacing = 36 * NPC.scale;

                    tail.rotation = CEUtils.RotateTowardsAngle((tail.Center - fPos).ToRotation(), fRot, 0.12f, false);
                    tail.Center = fPos + tail.rotation.ToRotationVector2() * spacing;
                }
            }
            else
            {
                for (int i = 0; i < segs.Count; i++)
                {
                    segs[i].Center += NPC.velocity;
                }
            }
            if (tailStyle == TailStyle.OnePoint)
            {
                Vector2 p1 = NPC.Center - NPC.rotation.ToRotationVector2() * 300 * NPC.scale;
                for (int i = 0; i < segs.Count; i++)
                {
                    float pg = i / (segs.Count - 1f);
                    List<Vector2> lt = new List<Vector2> { NPC.Center - NPC.rotation.ToRotationVector2() * 70 * NPC.scale, p1, tail.Center };
                    Vector2 p = CEUtils.Bezier(lt, pg);
                    segs[i].Center = Vector2.Lerp(segs[i].Center, p, 0.6f);
                    Vector2 fp = i == 0 ? NPC.Center + NPC.rotation.ToRotationVector2() * 16 : segs[i - 1].Center;
                    segs[i].rotation = (segs[i].Center - fp).ToRotation();
                }
                tail.rotation = segs[segs.Count - 1].rotation;
            }
            if (tailStyle == TailStyle.TwoPoint)
            {
                Vector2 p1 = NPC.Center - NPC.rotation.ToRotationVector2() * 300 * NPC.scale;
                Vector2 p2 = tail.Center - tail.rotation.ToRotationVector2() * 160 * NPC.scale;
                for (int i = 0; i < segs.Count; i++)
                {
                    float pg = i / (segs.Count - 1f);
                    List<Vector2> lt = new List<Vector2> { NPC.Center - NPC.rotation.ToRotationVector2() * 70 * NPC.scale, p1, p2, tail.Center };
                    Vector2 p = CEUtils.Bezier(lt, pg);
                    segs[i].Center = Vector2.Lerp(segs[i].Center, p, 0.6f);

                    Vector2 fp = i == 0 ? NPC.Center + NPC.rotation.ToRotationVector2() * 16 : segs[i - 1].Center;
                    segs[i].rotation = (segs[i].Center - fp).ToRotation();
                }
            }
        }
        #endregion
        public override void SetDefaults()
        {
            NPC.boss = true;
            NPC.width = 156;
            NPC.height = 156;
            NPC.damage = 54;
            NPC.defense = 10;
            NPC.lifeMax = 10000;
            NPC.HitSound = null;
            NPC.DeathSound = SoundID.NPCDeath25;
            NPC.value = 1000f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 4;
            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Apsychos");
            }
            if (Main.getGoodWorld)
                NPC.scale = 1.25f;
            if (Main.zenithWorld)
                NPC.scale = 0.7f;
        }
        #region AI
        public enum AIStyle
        {
            MoveToTarget,
            Dash,
            FireballShooting,
            FlameThrow,
            FireballBig,
            PhaseTrans,
            TailDash,
            Laser
        }

        public float Outline = 0;
        public int AIRound = 0;
        public AIStyle ai = AIStyle.MoveToTarget;
        public bool SpawnFlag = true;
        public int deactiveCount = 160;
        public override void AI()
        {
            if (SpawnFlag)
            {
                SpawnFlag = false;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int index = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<ApsychosTail>(), 0, NPC.whoAmI);
                    TailNPCIndex = index;
                    tail = index.ToNPC();
                    NPC.netUpdate = true;
                    NPC.netSpam = 0;
                }
            }
            if (TailNPCIndex >= 0)
                tail = TailNPCIndex.ToNPC();
            UpdateTail();
            if (tail == null)
            {
                SpawnFlag = true;
                return;
            }
            if (!tail.active)
            {
                SpawnFlag = true;
            }
            NPC.netUpdate = true;
            NPC.TargetClosest(false);
            if (NPC.HasValidTarget && NPC.target.ToPlayer().Distance(NPC.Center) < 5000)
            {
                deactiveCount = 160;
                AttackPlayer(NPC.target.ToPlayer());
            }
            else
            {
                Outline *= 0.9f;
                TailLight *= 0.9f;
                HighLight *= 0.9f;
                tail.velocity *= 0.9f;
                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, MathHelper.PiOver2, 0.07f, false);
                NPC.velocity = NPC.rotation.ToRotationVector2() * 15;
                tailStyle = TailStyle.Follow;
                AIChangeCounter = 0;
                ai = AIStyle.MoveToTarget;
                deactiveCount--;
                if (deactiveCount <= 0)
                    NPC.active = false;
            }
        }

        public void AttackPlayer(Player player)
        {
            float enrange = 1;
            if (Main.expertMode)
            {
                enrange += 0.1f;
            }
            if (Main.masterMode)
            {
                enrange += 0.1f;
            }
            //装灾厄读复仇/死亡,缺席仍走专家/大师兜底。勿连带改下方 EntropyMode
            if (CECal.IsRevengeance)
            {
                enrange += 0.15f;
            }
            if (CECal.IsDeathMode)
            {
                enrange += 0.15f;
            }
            if (CalamityEntropy.EntropyMode)
            {
                enrange *= 1.4f;
            }
            if (Main.getGoodWorld)
            {
                enrange *= 1.1f;
            }
            if (Main.zenithWorld)
            {
                enrange *= 0.85f;
            }
            bool OutlineFlag = true;
            bool TailSpeedMultFlag = true;
            bool TailLightFlag = true;
            if (ai == AIStyle.MoveToTarget)
            {
                tailStyle = TailStyle.Follow;
                AIChangeCounter++;
                float targetRot = (player.Center - NPC.Center).ToRotation();
                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.02f, true);
                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.06f, false);
                NPC.velocity *= 0.9f;
                float distance = player.Distance(NPC.Center);
                float spd = Utils.Remap(distance, 200, 900, 0.4f, 1f);
                NPC.velocity += NPC.rotation.ToRotationVector2() * spd * enrange;
                if (NPC.Distance(player.Center) < 400)
                    AIChangeCounter++;
                if (AIChangeCounter > 280 / enrange)
                {
                    SetAIStyle();
                }
            }
            if (ai == AIStyle.Dash)
            {
                OutlineFlag = false;
                tailStyle = TailStyle.Follow;
                if (AIChangeCounter < 30)
                {
                    NPC.velocity *= 0.9f;
                    float targetRot = (player.Center - NPC.Center).ToRotation();
                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.1f, false);
                    Outline += 1 / 25f;
                    if (Outline > 1)
                        Outline = 1;
                }
                if (AIChangeCounter > 60 / enrange)
                {
                    if (AIChangeCounter < 60 / enrange + 20)
                    {
                        if (num1 <= 16)
                        {
                            num1 += 1;
                            if (!Main.dedServ)
                            {
                                if (num1 == 1)
                                    CEUtils.PlaySound("flamethrower end", 1, NPC.Center);
                                CalamityEntropy.FlashEffectStrength = 0.32f;
                                if (Main.LocalPlayer.ZoneUnderworldHeight)
                                {
                                    CalamityEntropy.FlashEffectStrength = 0.55f;
                                }
                            }
                        }
                        NPC.velocity *= 0.9f;
                        NPC.velocity += NPC.rotation.ToRotationVector2() * 5 * enrange;
                        if (!Main.dedServ)
                        {
                            //Dash尾迹isp 0~1步长0.05,Smoke+dust双通道,phase1暖橙/p2冷蓝
                            for (float isp = 0; isp < 1; isp += 0.05f)
                            {
                                var d = Dust.NewDustDirect(NPC.Center, 0, 0, DustID.FlameBurst);
                                d.position = NPC.Center + NPC.velocity * isp + new Vector2(-40, 134).RotatedBy(NPC.rotation) * NPC.scale;
                                d.noGravity = true;
                                d.scale = 1.6f;
                                d.velocity = NPC.velocity * 0.16f + CEUtils.randomPointInCircle(4);
                                d = Dust.NewDustDirect(NPC.Center, 0, 0, DustID.FlameBurst);
                                d.position = NPC.Center + NPC.velocity * isp + new Vector2(-40, -134).RotatedBy(NPC.rotation) * NPC.scale;
                                d.noGravity = true;
                                d.scale = 1.6f;
                                d.velocity = NPC.velocity * 0.16f + CEUtils.randomPointInCircle(4);
                                var p = PRTLoader.NewParticle<PRT_Smoke>(NPC.Center + NPC.velocity * isp, Vector2.Zero, phase == 1 ? new Color(255, 160, 140) : new Color(120, 120, 255), 0.45f);
                                p.scaleStart = 0.6f;
                                p.scaleEnd = 0;
                                //scaleStart→scaleEnd Additive,跟FlameBurst dust叠是刻意双通道
                                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 32);
                            }
                        }
                    }
                }
                if (AIChangeCounter > 60 / enrange + 20)
                {
                    NPC.velocity *= 0.94f;
                    Outline -= 1 / 20f;
                    if (Outline < 0)
                        Outline = 0;
                }
                if (AIChangeCounter > 60 / enrange + 40)
                {
                    SetAIStyle();
                }
            }
            if (ai == AIStyle.FireballShooting)
            {
                TailSpeedMultFlag = false;
                NPC.velocity *= 0.94f;
                float targetRot = (player.Center - NPC.Center).ToRotation();
                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.1f, false);
                NPC.velocity *= 0.92f;
                NPC.velocity += NPC.rotation.ToRotationVector2() * 0.2f;
                tailStyle = TailStyle.OnePoint;
                tail.Center = Vector2.Lerp(tail.Center, NPC.Center + NPC.rotation.ToRotationVector2() * 180 * NPC.scale, 0.08f * enrange);
                int total = 5;
                if (AIChangeCounter > 80 / enrange)
                {
                    if (num2-- <= 0)
                    {
                        if (num1 <= total)
                        {
                            num2 = 40 / enrange;
                            num1++;
                            CEUtils.PlaySound("YharonFireball1", 0.9f, NPC.Center);
                            CEUtils.PlaySound("YharonFireball1", 0.9f, NPC.Center);
                            if (phase == 1)
                            {
                                Shoot<ApsychosFireball>(tail.Center + tail.rotation.ToRotationVector2() * 32 * NPC.scale, tail.rotation.ToRotationVector2() * 3.8f * enrange, 1, phase);
                                Shoot<ApsychosFireball>(tail.Center + tail.rotation.ToRotationVector2() * 32 * NPC.scale, tail.rotation.ToRotationVector2().RotatedBy(0.44f) * 3 * enrange, 1, phase);
                                Shoot<ApsychosFireball>(tail.Center + tail.rotation.ToRotationVector2() * 32 * NPC.scale, tail.rotation.ToRotationVector2().RotatedBy(-0.44f) * 3 * enrange, 1, phase);
                            }
                            else
                            {
                                Shoot<ApsychosFireball>(tail.Center + tail.rotation.ToRotationVector2() * 32 * NPC.scale, tail.rotation.ToRotationVector2() * 4f * enrange, 1, phase);
                                Shoot<ApsychosFireball>(tail.Center + tail.rotation.ToRotationVector2() * 32 * NPC.scale, tail.rotation.ToRotationVector2().RotatedBy(0.3f) * 4 * enrange, 1, phase);
                                Shoot<ApsychosFireball>(tail.Center + tail.rotation.ToRotationVector2() * 32 * NPC.scale, tail.rotation.ToRotationVector2().RotatedBy(-0.3f) * 4 * enrange, 1, phase);
                            }
                            tail.velocity = -tail.rotation.ToRotationVector2() * 18 * enrange;
                        }
                    }
                }
                if (num1 > total)
                {
                    num3++;
                    if (num3 > 60 / enrange)
                        SetAIStyle();
                }
                else
                {
                    TailLight += 0.04f;
                }
                tail.velocity *= 0.98f;
            }
            if (ai == AIStyle.FlameThrow)
            {
                tailStyle = TailStyle.TwoPoint;
                Vector2 tpos = NPC.Center + new Vector2(180, (float)Math.Sin(AIChangeCounter * 0.06f) * 100).RotatedBy(NPC.rotation);
                tail.Center = Vector2.Lerp(tail.Center, tpos, 0.24f);
                tail.rotation = CEUtils.RotateTowardsAngle(tail.rotation, (player.Center - tail.Center).ToRotation(), 0.08f, false);
                TailLight += 0.08f;
                float targetRot = (player.Center - NPC.Center).ToRotation();
                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.1f * enrange, false);
                NPC.velocity *= 0.96f;
                NPC.velocity += NPC.rotation.ToRotationVector2() * 0.16f;
                tail.Center = Vector2.Lerp(tail.Center, NPC.Center + NPC.rotation.ToRotationVector2() * 160 * NPC.scale, 0.08f * enrange);
                if (AIChangeCounter > 60 && AIChangeCounter < 140)
                {
                    tail.velocity += tail.rotation.ToRotationVector2() * -0.8f;
                    float v = 1;
                    if (AIChangeCounter < 100)
                        v = (AIChangeCounter - 60) / 40f;
                    if (AIChangeCounter > 150)
                        v = 1 - (AIChangeCounter - 150) / 40f;
                    Shoot<ApsychosFire>(tail.Center + tail.rotation.ToRotationVector2() * 60, tail.rotation.ToRotationVector2() * 52 * v, 1.2f, 40 + (phase - 1) * 24, phase);
                }
                if (AIChangeCounter > 190)
                    SetAIStyle();
            }
            if (ai == AIStyle.FireballBig)
            {
                tailStyle = TailStyle.OnePoint;
                float targetRot = (player.Center - NPC.Center).ToRotation();
                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.1f, false);
                NPC.velocity *= 0.9f;
                NPC.velocity += NPC.rotation.ToRotationVector2() * 0.16f;

                num1++;
                if (num1 < 65 - phase * 15)
                {
                    tail.Center = Vector2.Lerp(tail.Center, NPC.Center + NPC.rotation.ToRotationVector2() * 180 * NPC.scale, 0.08f * enrange);
                    TailLight += 0.08f;
                }
                if (num1 >= 65 - phase * 15)
                {
                    num1 = 0;
                    TailLight = 0;
                    num2++;
                    CEUtils.PlaySound("YharonFireball1", 0.9f, NPC.Center);
                    CEUtils.PlaySound("YharonFireball1", 0.9f, NPC.Center);
                    tail.velocity -= tail.rotation.ToRotationVector2() * 26;
                    Shoot<ApsychosFireballBig>(tail.Center, tail.rotation.ToRotationVector2() * (6 + 2 * phase) * enrange, 1.2f, 0, phase);
                }
                if (num2 > 3)
                    SetAIStyle();
            }
            if (ai == AIStyle.PhaseTrans)
            {
                NPC.velocity *= 0.98f;
                if (AIChangeCounter < 80)
                {
                    HighLight += 1 / 80f;
                    p2lerp += 1 / 80f;
                }
                else
                {
                    HighLight *= 0.94f;
                    p2lerp = 1;
                }
                if (AIChangeCounter > 80)
                    phase = 2;
                if (AIChangeCounter > 120)
                    SetAIStyle();
            }
            else
            {
                HighLight *= 0.94f;
            }
            if (ai == AIStyle.TailDash)
            {
                tailStyle = TailStyle.OnePoint;
                num1++;
                if (num1 < 60)
                {
                    float targetRot = (player.Center - NPC.Center).ToRotation();
                    if (num1 > 4)
                    {
                        num3 = float.Lerp(num3, -180, 0.04f);
                    }
                    tail.Center = NPC.Center + NPC.rotation.ToRotationVector2() * num3;
                    if (num1 > 12)
                    {
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.02f, true);
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.08f, false);
                    }
                    NPC.velocity *= 0.9f;
                    float distance = player.Distance(NPC.Center);
                    float spd = Utils.Remap(distance, 200, 900, 0.2f, 0.4f);
                    NPC.velocity += NPC.rotation.ToRotationVector2() * spd * enrange;

                }
                if (num1 == 60)
                {
                    NPC.velocity = NPC.rotation.ToRotationVector2() * 32 * enrange;
                }
                if (num1 >= 60)
                {
                    NPC.velocity *= 0.9f;
                    num2 *= 0.997f;
                    num2 += 12f;
                    num3 += num2;
                    tail.Center = NPC.Center + NPC.rotation.ToRotationVector2() * num3;
                    if (num3 > 180)
                    {
                        CEUtils.PlaySound("scatter", 1.6f, NPC.Center, volume: 0.9f);
                        Shoot<ApsychosTailShoot>(tail.Center, tail.rotation.ToRotationVector2() * 5 * enrange, 1.2f, NPC.scale);
                        NPC.ai[2]++;
                        num1 = 0;
                        num2 = 0;
                    }
                }
                if (NPC.ai[2] > ((NPC.life < NPC.lifeMax / 4) ? 5 : 2))
                {
                    NPC.ai[2] = 0;
                    SetAIStyle();
                }

            }
            if (ai == AIStyle.Laser)
            {
                tailStyle = TailStyle.TwoPoint;
                Vector2 tpos = NPC.Center + new Vector2(140, (float)Math.Sin(AIChangeCounter * 0.04f) * 200).RotatedBy(NPC.rotation);
                tail.Center = Vector2.Lerp(tail.Center, tpos, 0.24f);
                tail.rotation = (NPC.Center + NPC.rotation.ToRotationVector2() * 800 - tail.Center).ToRotation();
                TailLight += 0.1f;
                if (AIChangeCounter < 30)
                {
                    float targetRot = (player.Center - NPC.Center).ToRotation();
                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.1f, false);
                    NPC.velocity *= 0.96f;
                    NPC.velocity += NPC.rotation.ToRotationVector2() * 0.2f;
                }
                else
                {
                    if (AIChangeCounter > 80)
                    {
                        float targetRot = (player.Center - NPC.Center).ToRotation();
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.02f, false);
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, targetRot, 0.008f, true);
                    }
                    NPC.velocity *= 0.96f;
                    NPC.velocity -= NPC.rotation.ToRotationVector2() * 0.04f;
                }
                tail.Center = Vector2.Lerp(tail.Center, NPC.Center + NPC.rotation.ToRotationVector2() * 160 * NPC.scale, 0.08f * enrange);
                if (AIChangeCounter == 0)
                {
                    tail.velocity += tail.rotation.ToRotationVector2() * -0.8f;
                    Shoot<ApsychosLaser>(tail.Center + tail.rotation.ToRotationVector2() * 60, tail.rotation.ToRotationVector2() * 36, 1.2f, tail.whoAmI);
                }
                if (AIChangeCounter > 350)
                    SetAIStyle();
            }

            if (TailSpeedMultFlag)
            {
                tail.velocity *= 0.96f;
            }
            if (OutlineFlag)
            {
                Outline *= 0.9f;
            }
            if (TailLightFlag)
            {
                TailLight *= 0.96f;
            }
            AIChangeCounter++;
        }
        public int AIChangeCounter = 0;
        public float TailLight = 0;
        public void SetAIStyle()
        {
            float p2life = 0.5f;
            num1 = num2 = num3 = 0;
            vec1 = vec2 = Vector2.Zero;
            AIChangeCounter = 0;
            if (NPC.HasValidTarget && NPC.target.ToPlayer().Distance(NPC.Center) > 2500)
            {
                ai = AIStyle.MoveToTarget;
            }
            else if (phase == 1 && (NPC.life < NPC.lifeMax * p2life))
            {
                ai = AIStyle.PhaseTrans;
                AIRound = 0;
            }
            else
            {
                AIRound++;
                if (phase == 1)
                {
                    if (AIRound > 9)
                        AIRound = 0;
                    if (AIRound == 0)
                        ai = AIStyle.MoveToTarget;
                    if (AIRound == 1)
                        ai = AIStyle.Dash;
                    if (AIRound == 2)
                        ai = AIStyle.MoveToTarget;
                    if (AIRound == 3)
                        ai = AIStyle.FireballShooting;
                    if (AIRound == 4)
                        ai = AIStyle.MoveToTarget;
                    if (AIRound == 5)
                        ai = AIStyle.Dash;
                    if (AIRound == 6)
                        ai = AIStyle.MoveToTarget;
                    if (AIRound == 7)
                        ai = AIStyle.FlameThrow;
                    if (AIRound == 8)
                        ai = AIStyle.MoveToTarget;
                    if (AIRound == 9)
                        ai = AIStyle.FireballBig;
                }
                else
                {
                    if (NPC.life > NPC.lifeMax / 4)
                    {
                        if (AIRound > 14)
                            AIRound = 0;
                        if (AIRound == 0)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 1)
                            ai = AIStyle.Dash;
                        if (AIRound == 2)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 3)
                            ai = AIStyle.FireballShooting;
                        if (AIRound == 4)
                            ai = AIStyle.Dash;
                        if (AIRound == 5)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 6)
                            ai = AIStyle.FlameThrow;
                        if (AIRound == 7)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 8)
                            ai = AIStyle.FireballBig;
                        if (AIRound == 9)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 10)
                            ai = AIStyle.TailDash;
                        if (AIRound == 11)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 12)
                            ai = AIStyle.Dash;
                        if (AIRound == 13)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 14)
                            ai = AIStyle.Laser;
                    }
                    else
                    {
                        if (AIRound > 12)
                            AIRound = 0;
                        if (AIRound == 0)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 1)
                            ai = AIStyle.FireballBig;
                        if (AIRound == 2)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 3)
                            ai = AIStyle.TailDash;
                        if (AIRound == 4)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 5)
                            ai = AIStyle.Dash;
                        if (AIRound == 6)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 7)
                            ai = AIStyle.Laser;
                        if (AIRound == 8)
                            ai = AIStyle.Dash;
                        if (AIRound == 9)
                            ai = AIStyle.MoveToTarget;
                        if (AIRound == 10)
                            ai = AIStyle.Laser;
                        if (AIRound == 11)
                            ai = AIStyle.Dash;
                        if (AIRound == 12)
                            ai = AIStyle.Dash;
                    }
                }
            }
        }
        public int phase = 1;
        #endregion
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return true;
        }
        public void Shoot<T>(Vector2 pos, Vector2 velocity, float damageMult = 1, float ai0 = 0, float ai1 = 0, float ai2 = 0) where T : ModProjectile
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int baseDamage = phase == 1 ? (int)(NPC.damage / 6.5f) : (int)(NPC.damage / 5.4f);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, velocity, ModContent.ProjectileType<T>(), (int)(baseDamage * damageMult), 4, -1, ai0, ai1, ai2);
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
                CEUtils.PlaySound("ApsychosHit", Main.rand.NextFloat(0.8f, 1.2f), NPC.Center);
            if (NPC.life <= 0 && !Main.dedServ)
            {
                float scale = 360 / 40f;
                //死亡CustomPulse三层ShatteredExplosion+双Shine,scale=360/40是贴图原尺寸比
                PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.Red * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_CustomPulse>(NPC.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(NPC.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
                PRTLoader.NewParticle<PRT_CustomPulse>(NPC.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
                if (tail != null && segs != null)
                {
                    Gore.NewGore(NPC.GetSource_Death(), tail.Center, CEUtils.randomPointInCircle(6), Mod.Find<ModGore>("ApsychosGore1").Type);
                    foreach (var seg in segs)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), seg.Center, CEUtils.randomPointInCircle(6), Mod.Find<ModGore>("ApsychosGore2").Type);
                    }
                }
            }
        }
        public float HighLight = 1;
        public float p2lerp = 0;
        #region Drawing
        //身体贴图由 VaultLoaden 在加载期赋值,白化着色器读共享基座 CEEffectAssets(专用服务器上恒 null,只在绘制路径读取)
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/ApsychosSeg")]
        private static Asset<Texture2D> segTexAsset;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/ApsychosTail")]
        private static Asset<Texture2D> tailTexAsset;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/Apsychos2")]
        private static Asset<Texture2D> body2TexAsset;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/ApsychosSeg2")]
        private static Asset<Texture2D> seg2TexAsset;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Apsychos/ApsychosTail2")]
        private static Asset<Texture2D> tail2TexAsset;
        public static Effect WhiteTransShader()
        {
            return CEEffectAssets.WhiteTrans;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Outline > 0.01f)
                DrawOutLine(Outline);
            drawColor = Color.White;

            CEEffectAssets.WhiteTrans.Parameters["strength"].SetValue(HighLight);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, CEEffectAssets.WhiteTrans, Main.GameViewMatrix.TransformationMatrix);
            CEEffectAssets.WhiteTrans.CurrentTechnique.Passes[0].Apply();
            Texture2D bodyTex = NPC.getTexture();
            Texture2D segTex = segTexAsset.Value;
            Texture2D tailTex = tailTexAsset.Value;
            if (phase == 2)
            {
                bodyTex = body2TexAsset.Value;
                segTex = seg2TexAsset.Value;
                tailTex = tail2TexAsset.Value;
            }
            Main.EntitySpriteDraw(bodyTex, NPC.Center - Main.screenPosition, null, drawColor, NPC.rotation, bodyTex.Size() * 0.5f, NPC.scale, SpriteEffects.None);

            if (tail != null)
            {
                Main.EntitySpriteDraw(tailTex, tail.Center - Main.screenPosition, null, drawColor, tail.rotation, new Vector2(20, tailTex.Height * 0.5f), NPC.scale, SpriteEffects.None);
                if (segs != null)
                {
                    for (int i = 0; i < segs.Count; i++)
                    {
                        TailSeg seg = segs[i];
                        Main.EntitySpriteDraw(segTex, seg.Center - Main.screenPosition, null, drawColor, seg.rotation, segTex.Size() * 0.5f, NPC.scale, SpriteEffects.None);
                    }
                }
                Main.spriteBatch.ExitShaderRegion();
                if (TailLight > 0.01f)
                {
                    float p = 110;
                    Main.spriteBatch.UseBlendState(BlendState.Additive);
                    Texture2D ray = CEExtraAssets.Ray;
                    Main.spriteBatch.Draw(ray, tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale - Main.screenPosition, null, (phase == 1 ? new Color(255, 200, 160) : new Color(160, 160, 255)) * TailLight, Main.GlobalTimeWrappedHourly * 3, ray.Size() * 0.5f, new Vector2(1, 0.3f) * NPC.scale, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(ray, tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale - Main.screenPosition, null, (phase == 1 ? new Color(255, 200, 160) : new Color(160, 160, 255)) * TailLight, Main.GlobalTimeWrappedHourly * 3, ray.Size() * 0.5f, new Vector2(0.3f, 1) * NPC.scale, SpriteEffects.None, 0);
                    Color c1 = phase == 1 ? Color.OrangeRed : Color.Blue;
                    CEUtils.DrawGlow(tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale, Color.White, 0.6f * NPC.scale * TailLight, setState: false);
                    CEUtils.DrawGlow(tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale, c1, 1f * NPC.scale * TailLight, setState: false);
                    CEUtils.DrawGlow(tail.Center + tail.rotation.ToRotationVector2() * p * NPC.scale, c1, 1.4f * NPC.scale * TailLight, setState: false);
                    Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
                }
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public void DrawOutLine(float alpha)
        {
            if (CEEffectAssets.WhiteTrans == null)
                return;
            Color drawColor = new Color(255, 80, 40) * alpha;
            if (phase == 2)
                drawColor = new Color(70, 70, 255) * alpha;
            Texture2D bodyTex = NPC.getTexture();
            Texture2D segTex = segTexAsset.Value;
            Texture2D tailTex = tailTexAsset.Value;
            Main.spriteBatch.End();
            CEEffectAssets.WhiteTrans.Parameters["strength"].SetValue(1);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, CEEffectAssets.WhiteTrans, Main.GameViewMatrix.TransformationMatrix);
            CEEffectAssets.WhiteTrans.CurrentTechnique.Passes[0].Apply();
            for (int ir = 0; ir < 4; ir++)
            {
                float r = ir * MathHelper.PiOver2 + Main.GlobalTimeWrappedHourly * 10;
                Vector2 ofs = r.ToRotationVector2() * 8 * NPC.scale;
                Main.spriteBatch.Draw(bodyTex, NPC.Center + ofs - Main.screenPosition, null, drawColor, NPC.rotation, bodyTex.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0);
                if (tail != null)
                {
                    Main.spriteBatch.Draw(tailTex, tail.Center + ofs - Main.screenPosition, null, drawColor, tail.rotation, new Vector2(20, tailTex.Height * 0.5f), NPC.scale, SpriteEffects.None, 0);
                    if (segs != null)
                    {
                        for (int i = 0; i < segs.Count; i++)
                        {
                            TailSeg seg = segs[i];
                            Main.spriteBatch.Draw(segTex, seg.Center + ofs - Main.screenPosition, null, drawColor, seg.rotation, segTex.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0);
                        }
                    }
                }
            }
            Main.spriteBatch.ExitShaderRegion();
        }
        #endregion

        public override void OnKill()
        {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedApsychos, -1);
        }
        public override void BossLoot(ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ApsychosBag>()));
            // 治疗药水按人 5-15 瓶,隐藏图鉴条目(承接原灾厄 PerPlayer 语义)
            npcLoot.Add(new DropPerPlayerOnThePlayer(ItemID.HealingPotion, 1, 5, 15, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            {
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TectonicShard>(), 1, 24, 28));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<GreatSwordofEmbers>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<ScorchingChakram>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<AshesBow>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<EmberBolt>(), 5, 1, 1, 2));
                normalOnly.OnSuccess(ItemDropRule.Common(ItemID.Hellstone, 1, 32, 40));
            }
            npcLoot.Add(normalOnly);
            // 遗物:原灾厄复仇/大师条件对齐原版大师掉落惯例(difficulty-map)
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<ApsychosRelic>()));

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ApsychosTrophy>(), 10));

            // 首杀传记:承接原灾厄按人实例掉落语义
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<LoreApsychos>(), 1, 1, 1, new LoreFirstKill()));
        }

        // 恒真但隐藏图鉴条目的条件:对应原 hideLootReport 语义
        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }

        // 首杀传记条件:对应 downed 旗标未置位时每名玩家各掉一份
        private class LoreFirstKill : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedApsychos;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
    }
}
