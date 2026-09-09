using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles.SpiritFountainShoots;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SpiritFountain
{
    public class FountainColumn
    {
        public Vector2 offset = Vector2.Zero;
        public float rotation = -MathHelper.PiOver2;
        public float trailDrawOffset = 0;
        public float alpha = 1;
        public float scale = 1;
        public float Num = 0;
        public int id = 0;
        public FountainColumn(float a)
        {
            alpha = a;
        }
        public Vector2 GetPointAtMe(float poffset)
        {
            return offset + rotation.ToRotationVector2() * poffset;
        }
    }
    [AutoloadBossHead]
    public class SpiritFountain : ModNPC
    {
        //绘制用 Extra 池贴图,基座 CEExtraAssets 未收编的先放本文件私有字段,加载期由 VaultLoaden 赋值
        [VaultLoaden("CalamityEntropy/Assets/Extra/TrailInk")]
        private static Asset<Texture2D> trailInkTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/SpiritEye")]
        private static Asset<Texture2D> spiritEyeTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/SpiritEye2")]
        private static Asset<Texture2D> spiritEye2Tex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/SpiritEye3")]
        private static Asset<Texture2D> spiritEye3Tex;
        public FountainColumn column1 = new FountainColumn(0) { id = 0 };
        public FountainColumn column2 = new FountainColumn(0) { id = 1 };
        public int CenterRing = 0;
        public int GatheringAnimation = 300;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            /*NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = "CalamityEntropy/Assets/Extra/CruiserBes",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;*/
            // 图鉴隐藏:原灾厄隐藏扩展的原版等价写法
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            //bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            //{
            //    new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.CruiserBestiary")
            //});
        }
        public int SpiritCount = 10;
        public bool DontTakeDmg = false;
        public override void SetDefaults()
        {
            NPC.boss = true;
            NPC.width = 64;
            NPC.height = 64;
            NPC.damage = 520;
            if (Main.expertMode)
            {
                NPC.damage += 10;
                SpiritCount += 2;
            }
            if (Main.masterMode)
            {
                NPC.damage += 4;
                SpiritCount += 2;
            }
            //装灾厄读复仇/死亡,缺席仍走专家/大师兜底
            if (CECal.IsRevengeance)
            {
                SpiritCount += 2;
            }
            if (CECal.IsDeathMode)
            {
                SpiritCount += 2;
            }
            NPC.buffImmune[ModContent.BuffType<SoulDisorder>()] = true;
            NPC.defense = 0;
            NPC.lifeMax = 9000000;
            NPC.HitSound = SoundID.NPCHit11;
            NPC.DeathSound = SoundID.NPCDeath11;
            NPC.value = 100000f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.9f;
            NPC.dontCountMe = true;
            NPC.scale = 1f;
            NPC.timeLeft *= 12;
            if (Main.masterMode)
            {
                NPC.scale = 1.12f;
            }
            if (Main.getGoodWorld)
            {
                SpiritCount += 4;
            }
            if (Main.zenithWorld)
            {
                SpiritCount += 4;
            }
            NPC.netAlways = true;
            NPC.Entropy().damageMul = 0.1f;
            if (!Main.dedServ)
            {
                Music = MusicID.OtherworldlyTowers;//MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/");
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // 原灾厄欧米茄回复药水→超级治疗药水,数量 5-15 按 misc-map ×1.5 取整为 8-23;隐藏图鉴条目
            npcLoot.Add(new DropPerPlayerOnThePlayer(ItemID.SuperHealingPotion, 1, 8, 23, new HiddenDropCondition()));
        }

        // 恒真但隐藏图鉴条目的条件:对应原 hideLootReport 语义
        private class HiddenDropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => true;
            public bool CanShowItemDropInUI() => false;
            public string GetConditionDescription() => null;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(num1);
            writer.Write(Counter);
            writer.Write(mAmp);
            writer.Write(mCounter);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            num1 = reader.ReadSingle();
            Counter = reader.ReadSingle();
            mAmp = reader.ReadSingle();
            mCounter = reader.ReadSingle();
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override bool CheckDead()
        {
            return true;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return false;
        }
        public override bool CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool? CanBeHitByItem(Player player, Item item)
        {
            return null;
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return null;
        }

        public override bool CanBeHitByNPC(NPC attacker)
        {
            return false;
        }
        public enum AIStyle
        {
            SpawnAnimation,
            Moving,
            Boomerang,
            Lasers,
            RingFountains,
            PhaseTranse1,
            SpiritSlicing
        }
        public AIStyle ai = AIStyle.SpawnAnimation;
        public void Shoot(int type, Vector2 pos, Vector2 velo, float damageMult = 1, float ai0 = 0, float ai1 = 0, float ai2 = 0)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, velo, type, (int)(NPC.damage / 6 * damageMult), 3, -1, ai0, ai1, ai2);
        }
        public int aiTimer = 0;
        public bool SetPos = false;
        public float EyeAlpha = 0;
        public float FountainSpeed = 20;
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public bool SpawnSpirits = true;
        public bool SpawnSpirits2 = true;
        public float Counter = 0;
        public float c1LastPos = 0;
        public Vector2 starePoint = Vector2.Zero;
        public int ClearMyProjs = 0;
        public float num1 = 0;
        public int phase = 1;
        public float enrage = 1;
        public override void AI()
        {
            float phase1_2 = 0.9f;
            float phase1_3 = 0.75f;
            float phase2_1 = 0.66f;
            float phase2_2 = 0.44f;
            float phase3_1 = 0.33f;
            float phase3_2 = 0.15f;
            phase = 1;
            float p = (float)NPC.life / NPC.lifeMax;
            if (p < phase1_2)
            {
                phase++;
            }
            if (p < phase1_3)
            {
                phase++;
            }
            if (p < phase2_1)
            {
                phase++;
            }
            if (p < phase2_2)
            {
                phase++;
            }
            if (p < phase3_1)
            {
                phase++;
            }
            if (p < phase3_2)
            {
                phase++;
            }

            ClearMyProjs--;
            enrage = 1;
            if (Main.masterMode)
            {
                enrage += 0.2f;
            }
            else if (Main.expertMode)
            {
                enrage += 0.1f;
            }
            //装灾厄读死亡/复仇,缺席仍走大师/专家兜底。上方原版层不动
            if (CECal.IsDeathMode)
            {
                enrage += 0.2f;
            }
            else if (CECal.IsRevengeance)
            {
                enrage += 0.1f;
            }
            if (Main.zenithWorld)
            {
                enrage += 0.3f;
            }
            else if (Main.getGoodWorld)
            {
                enrage += 0.15f;
            }
            foreach (var plr in Main.ActivePlayers)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    if (column1.alpha > 0.1f && CEUtils.LineThroughRect(NPC.Center + column1.GetPointAtMe(-2000), NPC.Center + column1.GetPointAtMe(2000), plr.getRect(), (int)(100 * NPC.scale)))
                    {
                        plr.AddBuff(ModContent.BuffType<SoulDisorder>(), 10);
                    }
                    if (column2.alpha > 0.1f && CEUtils.LineThroughRect(NPC.Center + column2.GetPointAtMe(-2000), NPC.Center + column2.GetPointAtMe(2000), plr.getRect(), (int)(100 * NPC.scale)))
                    {
                        plr.AddBuff(ModContent.BuffType<SoulDisorder>(), 10);
                    }
                }
            }
            NPC.dontTakeDamage = true;
            Counter++;
            aiTimer++;
            column1.trailDrawOffset += FountainSpeed;
            column2.trailDrawOffset += FountainSpeed;
            float EyeAlphaT = 0.6f;
            NPC.TargetClosest();
            //双柱烟雾90×循环密度很高,3200px屏心裁剪+50%随机省一半spawn
            for (int i = 0; i < 90; i++)
            {
                Vector2 pos;
                if (column1.alpha > 0)
                {
                    pos = NPC.Center + column1.offset + column1.rotation.ToRotationVector2() * Main.rand.NextFloat(-1600, 1600) + column1.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-40, 40) * column1.scale;
                    if (Vector2.Distance(pos, Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight).Half()) < 3200)
                    {
                        if (Main.rand.NextBool())
                        {
                            var smoke = PRTLoader.NewParticle<PRT_Smoke>(pos, Vector2.Zero, Color.AliceBlue * column1.alpha * 3, Main.rand.NextFloat(0.1f, 0.2f));
                            smoke.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 50);
                        }
                        else
                        {
                            PRTLoader.NewParticle<PRT_GlowLightParticle>(pos, CEUtils.randomPointInCircle(6), Color.AliceBlue * column1.alpha * 2f, Main.rand.NextFloat(0.6f, 0.8f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 28);
                        }
                    }
                }
                if (column2.alpha > 0)
                {
                    pos = NPC.Center + column2.offset + column2.rotation.ToRotationVector2() * Main.rand.NextFloat(-1600, 1600) + column2.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-100, 100) * column2.scale;
                    if (Vector2.Distance(pos, Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight).Half()) < 3200)
                    {
                        if (Main.rand.NextBool())
                        {
                            var smoke2 = PRTLoader.NewParticle<PRT_Smoke>(pos, Vector2.Zero, Color.AliceBlue * column2.alpha * 3, Main.rand.NextFloat(0.1f, 0.2f));
                            smoke2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 50);
                        }
                        else
                        {
                            PRTLoader.NewParticle<PRT_GlowLightParticle>(pos, CEUtils.randomPointInCircle(6), Color.AliceBlue * column2.alpha * 2f, Main.rand.NextFloat(0.6f, 0.8f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 28);
                        }
                    }
                }
            }

            if (ai == AIStyle.SpawnAnimation)
            {
                if (!SetPos)
                {
                    column1.rotation = -MathHelper.PiOver2;
                    NPC.Opacity = 0;
                    SetPos = true;
                    // 禁忌档案坐标写入源已随灾厄 IL 删除，新世界恒为 (-1,-1)：下行 pos.X < 10 即安全短路，
                    // 落点回退到目标玩家处；仅旧档遗留有效坐标时才用档案馆位置（2026-08-27 核查定稿）
                    Vector2 pos = EDownedBosses.GetDungeonArchiveCenterPos();
                    NPC.Center = pos.X < 10 ? (NPC.HasValidTarget ? NPC.target.ToPlayer().Center : Main.player[0].Center) : pos;
                    starePoint = NPC.Center;
                }
                if (GatheringAnimation == 300)
                {
                    //SpawnAnimation入场Gathering==300双Shine,lifetime 320一次性大粒子
                    var shine1 = PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.AliceBlue, 5);
                    shine1.flag = true;
                    shine1.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 320);
                    var shine2 = PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.White, 3.6f);
                    shine2.flag = true;
                    shine2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 320);
                }

                if (GatheringAnimation-- > 0)
                {
                    if (GatheringAnimation > 70)
                    {
                        //HomingSpirit概率0.2~1随GatheringAnimation递减,后半段才密起来
                        if (Main.rand.NextFloat() < 0.2f + (1 - (GatheringAnimation - 100) / 200f))
                        {
                            float rr = CEUtils.randomRot();
                            var spirit = PRTLoader.NewParticle<PRT_HomingSpiritParticle>(NPC.Center + rr.ToRotationVector2() * 1600, rr.ToRotationVector2().RotatedByRandom(0.4f).RotatedBy(2.7f * (Main.rand.NextBool() ? 1 : -1)) * Main.rand.NextFloat(12, 16), Color.AliceBlue, 1);
                            spirit.TargetPos = NPC.Center;
                            spirit.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
                        }
                    }
                    aiTimer = 0;
                    return;
                }
                if (aiTimer > 40)
                {
                    starePoint = Vector2.Lerp(starePoint, Main.LocalPlayer.Center, 0.08f);
                }
                if (NPC.Opacity < 1 && EyeAlpha >= 0.6f)
                {
                    NPC.Opacity += 0.025f;

                }
                else
                {
                    if (EyeAlpha < 0.6f)
                    {
                        EyeAlpha += 0.01f;
                    }
                }
                column1.alpha = NPC.Opacity * 0.6f;
                if (aiTimer > 140)
                {
                    FountainSpeed = float.Lerp(FountainSpeed, 4, 0.06f);

                }
                if (aiTimer > 200)
                {
                    NPC.Opacity = 1;
                    column1.alpha = 0.6f;
                    EyeAlpha = 0.6f;
                    FountainSpeed = 4;
                    aiTimer = 0;
                    ai = AIStyle.Moving;
                    if (SpawnSpirits)
                    {
                        SpawnSpirits = false;
                        CenterRing = (int)Math.Ceiling(SpiritCount / 2f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < SpiritCount; i++)
                            {
                                int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritRing>(), 0, NPC.whoAmI, float.Lerp(-1, 1, i / (float)(SpiritCount - 1)), 0);
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, idx);
                                    Main.npc[idx].netUpdate = true;
                                }
                            }
                        }
                    }
                }
                EyeAlphaT = EyeAlpha;
            }
            else
            {
                starePoint = Main.LocalPlayer.Center;

            }
            if (phase > 3)
            {
                if (SpawnSpirits2)
                {
                    aiTimer = 0;
                    ai = AIStyle.PhaseTranse1;
                    column2.rotation = 0;
                    SpawnSpirits2 = false;
                    CenterRing = (int)Math.Ceiling(SpiritCount / 2f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < SpiritCount; i++)
                        {
                            int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritRing>(), 0, NPC.whoAmI, float.Lerp(-1, 1, i / (float)(SpiritCount - 1)), 1);
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, idx);
                                Main.npc[idx].netUpdate = true;
                            }
                        }
                    }

                }
            }
            if (ai == AIStyle.Moving)
            {
                FountainSpeed = float.Lerp(FountainSpeed, 8, 0.06f);
                mCounter += 0.01f * enrage * (phase == 3 ? 1f : 1);
                mAmp = float.Lerp(mAmp, 1, 0.004f * enrage);
                c1LastPos = column1.offset.X;

                column1.offset.X = float.Lerp(column1.offset.X, ((float)Math.Cos(mCounter) * 1800 * mAmp), 0.2f);
                if (aiTimer > 3)
                {
                    column1.rotation = (c1LastPos - column1.offset.X) * (Main.zenithWorld ? -0.01f : -0.006f) * (1 + phase * 0.02f) - MathHelper.PiOver2;
                }
                column1.alpha = float.Lerp(column1.alpha, 0.45f, 0.06f);
                if (phase == 1)
                {
                    if (Counter % (int)(20 / enrage) == 0)
                    {
                        Shoot(ModContent.ProjectileType<SpiritBullet>(), NPC.Center, CEUtils.randomRot().ToRotationVector2() * 6, 1, NPC.whoAmI);
                    }
                }
                if (phase == 2)
                {
                    if (Counter % (int)(16 / enrage) == 0)
                    {
                        Shoot(ModContent.ProjectileType<SpiritBullet>(), NPC.Center, (Counter * 0.04f).ToRotationVector2() * 8, 1, NPC.whoAmI, 0.4f);
                        Shoot(ModContent.ProjectileType<SpiritBullet>(), NPC.Center, -(Counter * 0.04f).ToRotationVector2() * 8, 1, NPC.whoAmI, 0.4f);
                    }
                }
                if (phase == 3)
                {
                    EyeAlphaT = 1;
                    NPC.dontTakeDamage = false;

                    if (Counter % (int)(80 / enrage) == 0)
                    {
                        for (float i = 0; i < 350; i += 45f)
                        {
                            float rt = (Counter * 0.04f + MathHelper.ToRadians(i));
                            Shoot(ModContent.ProjectileType<SpiritBullet>(), NPC.Center - rt.ToRotationVector2() * 2200f / enrage, rt.ToRotationVector2() * 16, 1, NPC.whoAmI, 1, 1);
                            for (float d = 0; d < 1; d += 0.025f)
                            {
                                //Lasers phase LineCal d+=0.025铺整条弹幕路径,每45°扇每格40颗量级
                                float dmx = 2200f / enrage;
                                Vector2 top = NPC.Center + rt.ToRotationVector2() * (d * dmx);
                                Vector2 sparkVelocity2 = (NPC.Center - top) * 0.03f;
                                top += CEUtils.randomPointInCircle(8);
                                int sparkLifetime2 = 100;
                                float sparkScale2 = Main.rand.NextFloat(1.4f, 2f);
                                Color sparkColor2 = Color.Lerp(Color.AliceBlue, Color.SkyBlue, Main.rand.NextFloat(0, 1));
                                PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                            }
                        }
                    }
                }

                if (aiTimer > 700)
                {
                    aiTimer = 0;
                    ai = AIStyle.Boomerang;
                }
            }
            else
            {
                mCounter = 0;
                mAmp = 0;
            }
            if (ai == AIStyle.Boomerang)
            {
                column1.offset.X = float.Lerp(column1.offset.X, ((float)Math.Cos(Main.GameUpdateCount * 0.02f) * 100), 0.03f);
                column1.rotation = -MathHelper.PiOver2;
                if (aiTimer > 80)
                {
                    num1 += phase == 3 ? 0.005f : 0.003f;
                }
                else
                {
                    num1 = 0;
                }
                if (num1 > (phase == 3 ? 1.66f : 1.36f))
                {
                    num1 = 0;
                    ai = AIStyle.Lasers;
                    aiTimer = 0;
                }
            }
            if (ai == AIStyle.Lasers)
            {
                column1.offset.X *= 0.9f;
                if (aiTimer > 460)
                {
                    ai = AIStyle.RingFountains;
                    aiTimer = 0;
                }
            }
            if (ai == AIStyle.RingFountains)
            {
                column1.offset.X *= 0.9f;
                if (aiTimer > 280)
                {
                    ai = AIStyle.Moving;
                    aiTimer = 0;
                }
            }
            if (ai == AIStyle.PhaseTranse1)
            {
                DontTakeDmg = true;
                column1.offset *= 0;
                column2.alpha = float.Lerp(column2.alpha, 0.45f, 0.04f);
                column2.id = 1;
                aiTimer++;
                if (aiTimer > 140)
                {
                    DontTakeDmg = false;
                    aiTimer = 0;
                    ai = AIStyle.SpiritSlicing;
                    column2.alpha = 0.45f;
                }
            }
            if (ai == AIStyle.SpiritSlicing)
            {
                NPC.dontTakeDamage = false;
                EyeAlphaT = 1;
                int t = 90 + 42;
                if (aiTimer == 1)
                {
                    column1.rotation = -MathHelper.PiOver2;
                    column2.rotation = 0;
                    column1.Num = 1400 * (Main.rand.NextBool() ? 1 : -1);
                    column2.Num = 1400 * (Main.rand.NextBool() ? 1 : -1);
                }
                column1.offset.X = float.Lerp(column1.offset.X, column1.Num, 0.056f);
                column2.offset.Y = float.Lerp(column2.offset.Y, column2.Num, 0.056f);
                if (aiTimer % t == 90)
                {
                    column1.Num *= -1;
                    column2.Num *= -1;
                }

                if (aiTimer > 800 && aiTimer % t < 40)
                {
                    ai = AIStyle.SpiritSlicing;
                    aiTimer = 0;
                }
            }
            NPC.localAI[2] = NPC.HasValidTarget ? 0 : NPC.localAI[2] + 1;
            if (NPC.localAI[2] > 600 || (NPC.HasValidTarget && !NPC.target.ToPlayer().Center.getRectCentered(10, 10).Intersects(NPC.Center.getRectCentered(150 * 16, 150 * 16))))
            {
                NPC.active = false;
            }
            EyeAlpha = float.Lerp(EyeAlpha, EyeAlphaT, 0.04f);
        }
        public float mAmp = 0;
        public float mCounter = 0;
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            return false;
        }
        public override void OnKill()
        {
            //NPC.SetEventFlagCleared(ref EDownedBosses.downedCruiser, -1);
        }

        public void DrawColumn(FountainColumn column, Texture2D TrailTex)
        {
            Main.spriteBatch.Draw(CEExtraAssets.MegaStreakBacking2, NPC.Center + column.offset - Main.screenPosition, new Rectangle(-(int)column.trailDrawOffset, 0, 3600, 256), Color.White * column.alpha * 1.4f, column.rotation, new Vector2(1800, 128), NPC.scale * 1.4f * column.scale, SpriteEffects.None, 0);
            EffectLoader.DrawCylinder(CEExtraAssets.B1, NPC.Center + column.offset - Main.screenPosition, Color.White * column.alpha, BlendState.AlphaBlend, 30, 0.26f * column.scale, 0.5f, Main.GlobalTimeWrappedHourly * 3f, 5, column.rotation - MathHelper.PiOver2, false, false);
            EffectLoader.DrawCylinder(CEExtraAssets.B1, NPC.Center + column.offset - Main.screenPosition, Color.White * column.alpha * 0.9f, BlendState.Additive, 30, 0.26f * column.scale, 0.5f, Main.GlobalTimeWrappedHourly * -3f, 5, column.rotation - MathHelper.PiOver2, false, false);
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointWrap);
            Main.spriteBatch.Draw(TrailTex, NPC.Center + column.offset - Main.screenPosition, new Rectangle(-(int)column.trailDrawOffset, 0, 3600, TrailTex.Height), Color.AliceBlue * column.alpha * 0.9f, column.rotation, new Vector2(1800, TrailTex.Height / 2), NPC.scale * 1.5f * column.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(TrailTex, NPC.Center + column.offset - Main.screenPosition, new Rectangle(-(int)(column.trailDrawOffset * 0.9f), 0, 3600, TrailTex.Height), Color.AliceBlue * column.alpha, column.rotation, new Vector2(1800, TrailTex.Height / 2), NPC.scale * 1.4f * column.scale, SpriteEffects.FlipVertically, 0);
            Main.spriteBatch.Draw(trailInkTex.Value, NPC.Center + column.offset - Main.screenPosition, new Rectangle((int)(column.trailDrawOffset * 0.85f), 0, 3600, TrailTex.Height), Color.AliceBlue * column.alpha * 0.5f, column.rotation, new Vector2(1800, 128), NPC.scale * 1.6f * column.scale, SpriteEffects.FlipHorizontally, 0);
            Main.spriteBatch.Draw(CEExtraAssets.BasicTrail, NPC.Center + column.offset - Main.screenPosition, new Rectangle(-(int)column.trailDrawOffset, 0, 3600, 200), new Color(140, 140, 255) * column.alpha, column.rotation, new Vector2(1800, 100), NPC.scale * 3f * column.scale, SpriteEffects.None, 0);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPosition, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
                return false;

            Texture2D mainTrail = CEExtraAssets.StreakFaded;
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointWrap);

            Texture2D eyeTex = spiritEyeTex.Value;
            Texture2D eyeTex2 = spiritEye2Tex.Value;
            Texture2D eyeTex3 = spiritEye3Tex.Value;

            Main.spriteBatch.Draw(eyeTex, NPC.Center - Main.screenPosition, null, Color.White * EyeAlpha, 0, eyeTex.Size() / 2f, 0.46f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(eyeTex2, NPC.Center - Main.screenPosition + (starePoint - NPC.Center).normalize() * ((float)Math.Sqrt(CEUtils.getDistance(starePoint, NPC.Center))), null, Color.White * EyeAlpha, 0, eyeTex.Size() / 2f, 0.46f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(eyeTex3, NPC.Center - Main.screenPosition + (starePoint - NPC.Center).normalize() * ((float)Math.Sqrt(CEUtils.getDistance(starePoint, NPC.Center) * 3f)), null, Color.White * EyeAlpha, 0, eyeTex.Size() / 2f, 0.46f, SpriteEffects.None, 0);

            DrawColumn(column1, mainTrail);
            DrawColumn(column2, mainTrail);

            Main.spriteBatch.ExitShaderRegion();

            if (ai == AIStyle.SpawnAnimation && aiTimer < 90)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.spriteBatch.Draw(CEExtraAssets.MegaStreakBacking2, NPC.Center - Main.screenPosition, null, Color.AliceBlue, MathHelper.PiOver2, CEExtraAssets.MegaStreakBacking2.Size() / 2f, new Vector2(24, CEUtils.Parabola(aiTimer / 90f, 4)), SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }
    }
}
