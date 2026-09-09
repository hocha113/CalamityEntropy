using CalamityEntropy.Common;
using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Utilities;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    [AutoloadBossHead]
    public class NihilityActeriophage : ModNPC
    {
        //绘制用贴图,加载期由 VaultLoaden 赋值,只在客户端绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/NPCs/NihilityTwin/BodyAlt")]
        private static Asset<Texture2D> bodyAltTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/NihilityTwin/back")]
        private static Asset<Texture2D> backTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/NihilityTwin/mid")]
        private static Asset<Texture2D> midTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/NihilityTwin/front")]
        private static Asset<Texture2D> frontTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/NihilityTwin/NihRope")]
        internal static Asset<Texture2D> nihRopeTex;
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 360);
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.65f,
                PortraitScale = 0.7f,
                CustomTexturePath = "CalamityEntropy/Assets/Extra/NABes",
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -14
            };
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Burning] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<VoidVirus>()] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;

        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.NihilityTwinBestiary")
            });
        }

        public override void SetDefaults()
        {
            NPC.boss = true;
            NPC.width = 140;
            NPC.height = 140;
            NPC.damage = 106;
            if (Main.expertMode)
            {
                NPC.damage += 2;
            }
            if (Main.masterMode)
            {
                NPC.damage += 2;
            }
            NPC.defense = 75;
            NPC.lifeMax = 360000;
            // 难度映射:死亡→大师、复仇→专家(difficulty-map)
            if (Main.masterMode)
            {
                NPC.damage += 5;
            }
            else if (Main.expertMode)
            {
                NPC.damage += 4;
            }
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCHit4;
            NPC.value = Item.buyPrice(1, 2, 60, 0);
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.5f;
            NPC.dontCountMe = true;
            NPC.netAlways = true;
            SpawnModBiomes = new int[] { ModContent.GetInstance<VoidDummyBoime>().Type };
        }
        public override void BossLoot(ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<NihilityTwinBag>()));

            // 深渊亡魂移除后,幽渊魂髓与深渊书签改由本 Boss 承接(数量与概率照搬旧掉落表);
            // 与旧主人一样不挂 NotExpert,专家模式下也照常掉,不进宝袋
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WraithSoulEssence>(), 1, 15, 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkAbyss>(), 2));

            // 灾厄至尊回复药水→原版超级治疗药水,数量照搬(misc-map);按人掉落并隐藏图鉴条目
            npcLoot.Add(new DropPerPlayerOnThePlayer(ItemID.SuperHealingPotion, 1, 5, 15, new HiddenDropCondition()));

            LeadingConditionRule normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            {
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<NihilityShell>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<Voidseeker>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<EventideSniper>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<StarlessNight>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<NihilityBacteriophageWand>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(new CommonDrop(ModContent.ItemType<VoidPathology>(), 5, 1, 1, 4));
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<NihilityFragments>(), 1, 18, 24));
                normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ChaoticPiece>(), 1, 18, 24));
            }
            npcLoot.Add(normalOnly);
            // 遗物:原灾厄复仇/大师条件对齐原版大师掉落惯例(difficulty-map)
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<NihilityTwinRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NihilityTwinTrophy>(), 10));

            // 首杀传记:承接原灾厄按人实例掉落语义
            npcLoot.Add(new DropPerPlayerOnThePlayer(ModContent.ItemType<NihilityTwinLore>(), 1, 1, 1, new LoreFirstKill()));
        }

        // 原灾厄全局 DR=0.15 的本地等效;公有字段供血条等外部读取
        public float DamageReduction = 0.15f;
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1f - DamageReduction;
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
            public bool CanDrop(DropAttemptInfo info) => !EDownedBosses.downedNihilityTwin;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
        public override void OnKill()
        {
            NPC.SetEventFlagCleared(ref EDownedBosses.downedNihilityTwin, -1);
            if (cell != null)
            {
                cell.StrikeInstantKill();
            }
        }

        public float rotSpeed = 0f;
        public int escapeCounter = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.rotation);
            writer.Write(rotSpeed);
            writer.Write(cellIndex);
            writer.Write(aitype);
            writer.Write(phase);
            writer.Write(aicounter);
        }
        public int cellIndex = -1;

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.rotation = reader.ReadSingle();
            rotSpeed = reader.ReadSingle();
            cellIndex = reader.ReadInt32();
            aitype = reader.ReadInt32();
            phase = reader.ReadInt32();
            aicounter = reader.ReadInt32();
        }
        public override void BossHeadRotation(ref float rotation)
        {
            rotation = NPC.rotation + MathHelper.PiOver2;
        }
        public NPC cell = null;
        public bool SpawnCell = true;
        public Rope rope = null;
        public int aitype = 3;
        public int phase = 1;
        public int counter { get { return (int)NPC.ai[1]; } set { NPC.ai[1] = value; } }
        public int aicounter = 0;
        public void prepareAiChange()
        {
            aicounter = 0;
            aitype = -1;
            NPC.netUpdate = true;
        }
        public void randomAI()
        {
            aicounter = 0;
            aitype = Main.rand.Next(7);
            NPC.netUpdate = true;
            if (phase == 2 && aitype == 4)
            {
                int sum = 0;
                int sstype = ModContent.NPCType<ChaoticCellSmall>();
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (n.type == sstype)
                    {
                        sum++;
                    }
                }
                if (sum > 8)
                {
                    randomAI();
                }
            }
        }
        public float ropeLerp = 1;
        public int spawnAnm = 150;
        float shake = 0;
        // 出场动画持续屏震的复用实例(仅客户端)
        private ScreenShaker.ScreenShake spawnShake = null;
        public override void OnSpawn(IEntitySource source)
        {
        }
        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (SpawnCell)
                {
                    SpawnCell = false;
                    if (NPC.realLife < 0)
                    {
                        int n = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<ChaoticCell>());
                        n.ToNPC().realLife = NPC.whoAmI;
                        n.ToNPC().netUpdate = true;
                        cell = n.ToNPC();
                        cellIndex = cell.whoAmI;
                        NPC.netUpdate = true;
                        NPC.netSpam = 9;

                        if (Main.zenithWorld)
                        {
                            int n2 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<NihilityActeriophage>());
                            n2.ToNPC().realLife = NPC.whoAmI;
                            n2.ToNPC().netUpdate = true;
                            n2.ToNPC().position += CEUtils.randomPointInCircle(500);
                            if (n2.ToNPC().ModNPC is NihilityActeriophage na)
                            {
                                na.cell = cell;
                                na.cellIndex = cellIndex;
                            }
                        }
                    }
                }
            }
            if (spawnAnm > 0)
            {
                spawnAnm--;
                shake += 5f / 120f;
                if (cell != null)
                {
                    cell.Center = NPC.Center;
                    cell.velocity *= 0;
                    NPC.velocity *= 0;
                    // 原灾厄全局屏震(逐帧置强度)改自有 ScreenShaker:复用同一震动实例并逐帧刷新振幅
                    if (!Main.dedServ)
                    {
                        if (spawnShake == null || !spawnShake.active)
                        {
                            spawnShake = new ScreenShaker.ScreenShake(Vector2.Zero, 0);
                            ScreenShaker.AddShake(spawnShake);
                        }
                        spawnShake.amplitude = 3.2f * shake;
                    }
                }
                return;
            }
            if (Main.GameUpdateCount % 5 == 0 && !(Main.netMode == NetmodeID.MultiplayerClient))
            {
                if (cell == null || !cell.active)
                {
                    if (NPC.realLife < 0)
                    {
                        int n = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<ChaoticCell>());
                        n.ToNPC().realLife = NPC.whoAmI;
                        n.ToNPC().netUpdate = true;
                        cell = n.ToNPC();
                        cellIndex = cell.whoAmI;
                        NPC.netUpdate = true;
                        NPC.netSpam = 9;
                    }
                }
            }
            if (NPC.netSpam > 10)
            {
                NPC.netSpam = 9;
                NPC.netUpdate = true;
            }
            counter++;
            if (cell == null)
            {
                if (cellIndex >= 0)
                {
                    cell = cellIndex.ToNPC();
                }
            }
            if (cell != null)
            {
                if (rope == null)
                {
                    rope = new Rope(NPC.Center, cell.Center, 30, 0, new Vector2(0, 0f), 0.006f, 15, false);
                }


            }
            if (!Main.dedServ)
            {
                Main.LocalPlayer.Entropy().NihSky = 20;
            }
            NPC.localAI[0]++;
            if (!NPC.HasValidTarget)
            {
                NPC.TargetClosest(false);
            }

            if (NPC.HasValidTarget)
            {
                Player target = NPC.target.ToPlayer();
                Vector2 targetPos = target.Center;

                escapeCounter = 0;

                if (aitype == -1)
                {
                    aicounter++;
                    NPC.velocity *= 0.98f;
                    cell.velocity *= 0.98f;
                    if (phase == 1)
                    {
                        if (CEUtils.getDistance(cell.Center, NPC.Center) > 120)
                        {
                            cell.velocity += (NPC.Center - cell.Center).SafeNormalize(Vector2.Zero) * 0.6f;
                        }
                    }
                    else
                    {
                        cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * 0.5f;
                    }
                    NPC.velocity += (targetPos - NPC.Center).SafeNormalize(Vector2.Zero) * 0.6f;
                    NPC.rotation = NPC.velocity.ToRotation();
                    spawnParticle(buttom);
                    if (aicounter > 80)
                    {
                        randomAI();
                    }
                }
                if (phase == 1)
                {
                    if (NPC.life < NPC.lifeMax / 2)
                    {
                        foreach (Player plr in Main.ActivePlayers)
                        {
                            plr.Entropy().immune = 120;
                        }
                        phase = 2;
                        prepareAiChange();
                    }
                    if (aitype == 0)
                    {
                        if (NPC.ai[0]-- > 0)
                        {
                            if (CEUtils.getDistance(targetPos, NPC.Center) > 1400)
                            {
                                NPC.ai[0] = 36;
                            }
                            NPC.velocity += (targetPos - NPC.Center).SafeNormalize(Vector2.Zero) * 1.2f;

                            NPC.velocity *= 0.98f;
                        }
                        else
                        {
                            int scd = 24;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (counter % scd == 0)
                                {
                                    float rot = cell.rotation;
                                    for (int i = 0; i < 360; i += 40)
                                    {
                                        Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (rot + MathHelper.ToRadians(i)).ToRotationVector2() * 16, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    }
                                }
                                if (counter % 15 == 0)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 20, ModContent.ProjectileType<CellSpike>(), NPC.damage / 6, 2);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * 20, ModContent.ProjectileType<CellSpike>(), NPC.damage / 6, 2);
                                }
                            }
                            NPC.velocity += (targetPos - NPC.Center).SafeNormalize(Vector2.Zero) * 0.1f;
                            if (NPC.velocity.Length() < 30)
                            {
                                NPC.velocity *= 1.076f;
                            }

                            if (CEUtils.getDistance(targetPos, NPC.Center) > 1400)
                            {
                                NPC.ai[0] = 36;
                                CEUtils.PlaySound("beast_ghostdash" + Main.rand.Next(1, 5), 1);
                            }
                        }
                        NPC.rotation = NPC.velocity.ToRotation();
                        for (int i = 0; i < 10; i++)
                        {
                            spawnParticle(NPC.Center + NPC.velocity * ((float)i / 10f));
                        }
                        if (cell != null)
                        {
                            cell.velocity += (NPC.Center - cell.Center) * 0.0022f;
                        }
                        aicounter++;
                        if (aicounter > 360 && NPC.ai[0] > 0)
                        {
                            NPC.ai[0] = 0;
                            prepareAiChange();
                        }
                    }
                    if (aitype == 1)
                    {
                        cell.rotation = NPC.rotation;
                        NPC.velocity *= 0.98f;
                        NPC.velocity = (targetPos - NPC.Center) * 0.007f;
                        NPC.rotation += rotSpeed;
                        rotSpeed += 0.134f;
                        rotSpeed *= 0.62f;
                        cell.velocity *= 0.98f;
                        cell.velocity += (NPC.Center + NPC.rotation.ToRotationVector2() * -120 - cell.Center) * 0.14f;
                        if (aicounter > 60 && counter % 1 == 0 && Main.netMode != NetmodeID.MultiplayerClient && CEUtils.getDistance(cell.Center, NPC.Center) < 720)
                        {
                            Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, NPC.rotation.ToRotationVector2() * -14, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                        }
                        aicounter++;
                        if (aicounter > 200)
                        {
                            prepareAiChange();
                        }
                        rope.Update();
                        rope.Update();
                    }
                    else
                    {
                        if (aitype != 4)
                        {
                            rotSpeed = 0;
                        }
                    }
                    if (aitype == 2)
                    {
                        if (aicounter > 0 || CEUtils.getDistance(targetPos, NPC.Center) < 1200)
                        {
                            aicounter++;
                        }
                        else
                        {
                            NPC.velocity = (targetPos - NPC.Center) * 0.08f;
                        }
                        if (aicounter > 0)
                        {
                            if (aicounter < 30)
                            {
                                NPC.velocity *= 0.86f;
                                cell.velocity = ((NPC.Center + (targetPos - NPC.Center).SafeNormalize(Vector2.UnitX) * 260) - cell.Center) * 0.1f;
                                nz = (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * 10;
                            }
                            else if (aicounter < 140)
                            {
                                Vector2 j = nz * ((float)aicounter / 140f);
                                cell.velocity += j * 0.44f;
                                NPC.velocity -= j * 0.01f;
                                if (counter % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (cell.velocity.ToRotation() + MathHelper.PiOver2).ToRotationVector2() * 12, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (cell.velocity.ToRotation() - MathHelper.PiOver2).ToRotationVector2() * 12, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);

                                }
                            }
                            else
                            {
                                NPC.velocity *= 0.9f;
                                NPC.velocity = (targetPos - NPC.Center) * 0.006f;
                                cell.velocity = (NPC.Center - cell.Center) * 0.05f;
                            }
                        }
                        NPC.rotation = (NPC.Center - cell.Center).ToRotation();

                        if (aicounter > 220)
                        {
                            prepareAiChange();
                        }
                    }
                    if (aitype == 3)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            spawnParticle(NPC.Center + NPC.velocity * ((float)i / 10f));
                        }

                        if (aicounter > 0 || CEUtils.getDistance(targetPos, NPC.Center) < 800)
                        {
                            aicounter++;
                        }
                        else
                        {
                            if (NPC.velocity.Length() > 30)
                            {
                                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * 30;
                            }
                            NPC.velocity = (targetPos - new Vector2(0, 200) - NPC.Center) * 0.08f;
                            if (CEUtils.getDistance(targetPos, NPC.Center + NPC.velocity * 2) < 800)
                            {
                                NPC.velocity *= 0.36f;
                            }
                        }
                        if (aicounter > 200)
                        {
                            prepareAiChange();
                        }
                        if (aicounter > 0)
                        {
                            NPC.velocity *= 0.995f;

                            NPC.velocity += (targetPos - NPC.Center).SafeNormalize(Vector2.Zero) * 0.2f;

                            cell.velocity = ((NPC.Center + (targetPos - NPC.Center).SafeNormalize(Vector2.UnitX) * 200) - cell.Center) * 0.1f;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (counter % 30 == 0)
                                {
                                    float rot = CEUtils.randomRot();
                                    for (int i = 0; i < 360; i += 60)
                                    {
                                        Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (rot + MathHelper.ToRadians(i)).ToRotationVector2() * 12, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    }
                                }
                            }
                        }
                        NPC.rotation = NPC.velocity.ToRotation();
                    }
                    if (aitype == 4)
                    {
                        cell.rotation = NPC.rotation;
                        NPC.velocity *= 0.98f;
                        NPC.velocity = (targetPos - NPC.Center) * 0.007f;
                        NPC.rotation += rotSpeed * 0.26f;
                        rotSpeed += 0.134f;
                        rotSpeed *= 0.62f;
                        cell.velocity *= 0.98f;
                        cell.velocity = (NPC.Center + NPC.rotation.ToRotationVector2() * -520 - cell.Center) * 0.14f;
                        if (aicounter > 60 && counter % 30 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float rot = (targetPos - cell.Center).ToRotation();
                            for (int i = 0; i < 6; i++)
                            {
                                if (i > 0)
                                {
                                    Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center + new Vector2(-i * 30, (float)i * 14).RotatedBy(rot), rot.ToRotationVector2() * 14, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center + new Vector2(-i * 30, (float)i * -14).RotatedBy(rot), rot.ToRotationVector2() * 14, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);

                                }
                                else
                                {
                                    Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, rot.ToRotationVector2() * 14, ModContent.ProjectileType<CellBullet>(), NPC.damage / 7, 4);
                                }
                            }
                            rot = CEUtils.randomRot();
                            for (int i = 0; i < 360; i += 60)
                            {
                                Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (rot + MathHelper.ToRadians(i)).ToRotationVector2() * 10, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                            }

                        }
                        aicounter++;
                        if (aicounter > 220)
                        {
                            prepareAiChange();
                        }
                    }
                    if (aitype == 5)
                    {
                        if (aicounter == 0)
                        {
                            CEUtils.PlaySound("charge", 1, NPC.Center);
                            CEUtils.PlaySound("charge", 1, NPC.Center);
                            NPC.rotation = (NPC.Center - targetPos).ToRotation();
                            foreach (Player player in Main.ActivePlayers)
                            {
                                player.Entropy().immune = 60;
                            }
                        }
                        aicounter++;
                        Vector2 t = NPC.rotation.ToRotationVector2() * 840;


                        if (aicounter < 460)
                        {
                            NPC.velocity = (targetPos + t - NPC.Center) * 0.04f;
                            cell.velocity = (targetPos - t - cell.Center) * 0.04f;
                        }
                        else
                        {
                            NPC.velocity.Y -= 1.2f;
                            cell.velocity.Y -= 1.2f;
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient && aicounter > 80 && aicounter < 460)
                        {
                            if (Main.rand.NextBool(3))
                            {
                                if (Main.rand.NextBool(2))
                                {
                                    Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (Main.GameUpdateCount * 0.09f).ToRotationVector2() * 10, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (Main.GameUpdateCount * -0.09f).ToRotationVector2() * -14, ModContent.ProjectileType<NihilityFire>(), NPC.damage / 6, 4);
                                }
                            }
                        }
                        if (aicounter > 500)
                        {
                            prepareAiChange();
                        }
                        NPC.rotation += MathHelper.ToRadians(1.6f);

                    }
                    if (aitype == 6)
                    {
                        cell.velocity *= 0.98f;
                        cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * 0.36f;
                        cell.ai[2] = 4;
                        NPC.velocity = NPC.rotation.ToRotationVector2() * 18f;
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (cell.Center - NPC.Center).ToRotation(), 0.07f, false);
                        if (aicounter == 1)
                        {
                            NPC.ai[2] = CEUtils.randomRot();
                        }
                        NPC.ai[2] += MathHelper.ToRadians(0.5f);
                        aicounter++;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (Main.GameUpdateCount % 40 == 0)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 16, ModContent.ProjectileType<CellSpike>(), NPC.damage / 6, 2);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * 16, ModContent.ProjectileType<CellSpike>(), NPC.damage / 6, 2);
                            }
                            if (aicounter < 160)
                            {
                                if (Main.GameUpdateCount % 10 == 0)
                                {
                                    for (int i = 0; i < 360; i += 72)
                                    {
                                        Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (NPC.ai[2] + MathHelper.ToRadians(i)).ToRotationVector2() * 16, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < 360; i += 72)
                                {
                                    if (Main.rand.NextBool(3))
                                    {
                                        Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center + new Vector2(Main.rand.Next(-44, 44), Main.rand.Next(-44, 44)), (NPC.ai[2] + MathHelper.ToRadians(i)).ToRotationVector2() * 26, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    }
                                }

                            }
                        }
                        if (aicounter > 360)
                        {
                            prepareAiChange();
                        }
                    }
                }
                if (phase == 2)
                {
                    if (ropeLerp > 0)
                    {
                        ropeLerp -= 1f;
                    }

                    if (aitype == 0)
                    {
                        if (aicounter > 5)
                        {
                            NPC.ai[2] = 0;
                            prepareAiChange();
                        }
                        else
                        {
                            NPC.ai[2]--;
                            if (NPC.ai[2] < -30)
                            {
                                aicounter++;
                                NPC.ai[2] = 20;
                                if (aicounter <= 5)
                                {
                                    CEUtils.PlaySound("beast_ghostdash" + Main.rand.Next(1, 5), 1, NPC.Center);
                                }
                            }
                            if (NPC.ai[2] > 0)
                            {
                                NPC.velocity += NPC.rotation.ToRotationVector2() * 5f;
                                for (int i = 0; i < 10; i++)
                                {
                                    spawnParticle(NPC.Center + NPC.velocity * ((float)i / 10f));
                                }
                            }
                            else
                            {
                                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (targetPos - NPC.Center).ToRotation(), 0.09f, false);
                            }
                            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * 0.36f;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (counter % 30 == 0)
                                {
                                    float rot = CEUtils.randomRot();
                                    for (int i = 0; i < 360; i += 90)
                                    {
                                        Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (rot + MathHelper.ToRadians(i)).ToRotationVector2() * 18, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    }
                                }
                                if (counter % 10 == 0)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(Main.rand.NextFloat(-16, 16), Main.rand.NextFloat(-16, 16)), (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 20, ModContent.ProjectileType<CellSpike>(), NPC.damage / 6, 2);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(Main.rand.NextFloat(-16, 16), Main.rand.NextFloat(-16, 16)), (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * 20, ModContent.ProjectileType<CellSpike>(), NPC.damage / 6, 2);
                                }
                            }

                            NPC.velocity *= 0.96f;
                        }
                    }
                    if (aitype == 1)
                    {

                        if (aicounter > 0 || CEUtils.getDistance(cell.Center, NPC.Center) < 90)
                        {
                            aicounter++;
                        }
                        else
                        {
                            NPC.velocity *= 0.9f;
                            cell.velocity += (NPC.Center - cell.Center).SafeNormalize(Vector2.Zero) * 2f;
                        }
                        if (aicounter > 0 && aicounter < 100)
                        {
                            NPC.velocity *= 0.98f;
                            NPC.velocity = (targetPos - NPC.Center) * 0.007f;
                            NPC.rotation += rotSpeed;
                            rotSpeed += 0.03f;
                            rotSpeed *= 0.94f;
                            cell.Center = NPC.Center + new Vector2(-100, 0).RotatedBy(NPC.rotation);
                            cell.velocity *= 0;
                            if (counter % 2 == 0)
                            {
                                aicounter++;
                            }
                        }
                        if (aicounter == 100)
                        {
                            NPC.rotation = (targetPos - NPC.Center).ToRotation();
                            cell.velocity = NPC.rotation.ToRotationVector2() * 60;
                            NPC.rotation += MathHelper.Pi;
                            cell.Center = NPC.Center + new Vector2(-100, 0).RotatedBy(NPC.rotation);
                            NPC.ai[3] = NPC.rotation;
                        }
                        if (aicounter > 100 && aicounter < 130)
                        {
                            rotSpeed *= 0.96f;
                            NPC.rotation += rotSpeed;
                            cell.velocity = NPC.ai[3].ToRotationVector2() * -60;
                            if (counter % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (cell.velocity.ToRotation() + MathHelper.PiOver2).ToRotationVector2() * 18, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (cell.velocity.ToRotation() - MathHelper.PiOver2).ToRotationVector2() * 18, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                            }
                        }
                        if (aicounter > 150)
                        {

                            cell.velocity *= 0.98f;
                            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * 0.6f;
                        }
                        if (aicounter > 160)
                        {
                            prepareAiChange();
                        }
                    }
                    else
                    {
                        rotSpeed = 0;
                    }
                    if (aitype == 2)
                    {
                        if (aicounter > 2)
                        {
                            NPC.ai[2] = 0;
                            prepareAiChange();
                        }
                        else
                        {
                            NPC.ai[2]--;
                            if (NPC.ai[2] < -30)
                            {
                                aicounter++;
                                NPC.ai[2] = 20;
                                if (aicounter <= 2)
                                {
                                    CEUtils.PlaySound("beast_ghostdash" + Main.rand.Next(1, 5), 1, NPC.Center);
                                }
                            }
                            if (NPC.ai[2] > 0)
                            {
                                NPC.velocity += NPC.rotation.ToRotationVector2() * 5f;
                                for (int i = 0; i < 10; i++)
                                {
                                    spawnParticle(NPC.Center + NPC.velocity * ((float)i / 10f));
                                }
                            }
                            else
                            {
                                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (targetPos - NPC.Center).ToRotation(), 0.09f, false);
                            }
                            cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * 0.36f;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (counter % 6 == 0)
                                {
                                    float rot = MathHelper.ToRadians((Main.GameUpdateCount * 19) % 360);
                                    for (int i = 0; i < 360; i += 72)
                                    {
                                        Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (rot + MathHelper.ToRadians(i)).ToRotationVector2() * 6, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    }
                                }
                            }
                            if (counter % 40 == 0)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 20, ModContent.ProjectileType<CellSpike>(), NPC.damage / 6, 2);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * 20, ModContent.ProjectileType<CellSpike>(), NPC.damage / 6, 2);
                            }
                            NPC.velocity *= 0.96f;
                        }
                    }
                    if (aitype == 3)
                    {
                        if (aicounter == 1)
                        {
                            CEUtils.PlaySound("beast_lavaball_rise1", 1);
                        }
                        NPC.rotation = (cell.Center - NPC.Center).ToRotation() + MathHelper.Pi;
                        ropeLerp = 1;
                        aicounter++;
                        if (aicounter > 40)
                        {
                            NPC.velocity += (cell.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 3.4f;
                            cell.velocity += (NPC.Center - cell.Center).SafeNormalize(Vector2.Zero) * 3.4f;
                            if (CEUtils.getDistance(NPC.Center, cell.Center) < NPC.velocity.Length() + cell.velocity.Length() + 6)
                            {
                                Vector2 midPos = (NPC.Center + cell.Center) / 2;
                                NPC.velocity *= 0;
                                cell.velocity *= 0;
                                NPC.Center = midPos + NPC.rotation.ToRotationVector2() * 20;
                                cell.Center = midPos - NPC.rotation.ToRotationVector2() * 20;
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    float rot = MathHelper.ToRadians((Main.GameUpdateCount * 73) % 360);
                                    for (int i = 0; i < 360; i += 10)
                                    {
                                        Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (rot + MathHelper.ToRadians(i)).ToRotationVector2() * 16, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                    }

                                }
                                CEUtils.PlaySound("flashback", 1, NPC.Center);
                                prepareAiChange();
                                if (Main.rand.NextBool(2))
                                {
                                    aitype = 3;
                                }
                            }
                        }
                        else
                        {
                            NPC.velocity += (NPC.Center - cell.Center).SafeNormalize(Vector2.Zero) * 1f;
                            cell.velocity -= (NPC.Center - cell.Center).SafeNormalize(Vector2.Zero) * 1f;
                        }
                    }
                    if (aitype == 4)
                    {
                        if (aicounter == 2)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                NPC.NewNPC(NPC.GetSource_FromAI(), (int)cell.Center.X, (int)cell.Center.Y, ModContent.NPCType<ChaoticCellSmall>(), 0, cell.whoAmI);
                            }
                        }
                        NPC.rotation = NPC.velocity.ToRotation();
                        aicounter++;
                        cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * 0.5f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (counter % 30 == 0)
                            {
                                float rot = CEUtils.randomRot();
                                for (int i = 0; i < 360; i += 40)
                                {
                                    Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, (rot + MathHelper.ToRadians(i)).ToRotationVector2() * 18, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                                }
                            }
                        }
                        NPC.velocity *= 0.98f;
                        if (aicounter > 360)
                        {
                            prepareAiChange();
                        }
                    }
                    if (aitype == 5)
                    {
                        if (aicounter == 2)
                        {
                            CEUtils.PlaySound("charge", 1, NPC.Center);
                            CEUtils.PlaySound("charge", 1, NPC.Center);
                        }
                        if (aicounter < 40)
                        {
                            NPC.velocity = (targetPos - NPC.Center) * 0.009f;
                            NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (NPC.Center - targetPos).ToRotation(), 4f.ToRadians(), true);
                        }
                        else
                        {
                            if (aicounter > 160)
                            {
                                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (targetPos - NPC.Center).ToRotation(), 1.4f.ToRadians(), true);
                            }
                            NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (targetPos - NPC.Center).ToRotation(), 0.01f, false);
                        }
                        if (aicounter == 40)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CruiserLaserMouth>(), (int)(NPC.damage / 5f), 0, -1, NPC.whoAmI, 400);
                            }

                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (Main.rand.NextBool(3))
                            {
                                Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center + new Vector2(Main.rand.NextFloat(-16, 16), Main.rand.NextFloat(-16, 16)), (cell.Center - targetPos).SafeNormalize(Vector2.UnitX) * 12, ModContent.ProjectileType<CellBullet>(), NPC.damage / 6, 4);
                            }
                        }
                        cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.Zero) * 0.62f;
                        NPC.velocity = (targetPos - NPC.Center) * 0.007f;
                        aicounter++;
                        if (aicounter > 460)
                        {
                            prepareAiChange();
                        }
                    }
                    if (aitype == 6)
                    {
                        if (aicounter == 2 || aicounter == 62 || aicounter == 122)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float rot = CEUtils.randomRot();
                                for (int i = 0; i < 360; i += 40)
                                {
                                    Projectile.NewProjectile(cell.GetSource_FromThis(), cell.Center, Vector2.Zero, ModContent.ProjectileType<NihilityEnergyBall>(), NPC.damage / 6, 4, -1, cell.whoAmI, rot + MathHelper.ToRadians(i));
                                }

                            }
                        }
                        NPC.velocity *= 0.996f;
                        aicounter++;
                        NPC.velocity += (targetPos - NPC.Center).SafeNormalize(Vector2.Zero) * 0.36f;
                        NPC.rotation = NPC.velocity.ToRotation();
                        cell.velocity += (targetPos - cell.Center).SafeNormalize(Vector2.UnitX) * 0.4f;

                        if (aicounter > 360)
                        {
                            prepareAiChange();
                        }
                    }
                }

            }
            else
            {
                if (cell != null)
                {
                    cell.velocity += (NPC.Center - cell.Center) * 0.0022f;
                }
                NPC.velocity.Y -= 1.26f;
                escapeCounter++;
                if (escapeCounter > 180)
                {
                    NPC.active = false;
                }
                NPC.velocity *= 0.98f;
                NPC.rotation = NPC.velocity.ToRotation();
            }
            if (cell != null)
            {
                cell.life = NPC.life;
                cell.target = NPC.target;
            }
            NPC.velocity *= 0.996f;
            if (ropeLerp > 0)
            {
                Vector2 rend = Vector2.Lerp(buttom, cell.Center, ropeLerp);
                rope.segmentLength = CEUtils.getDistance(buttom, rend) / 35f;
                rope.Start = buttom;
                rope.End = rend;
                rope.Update();
            }
        }
        Vector2 nz = Vector2.Zero;


        public void spawnParticle(Vector2 center)
        {
            Vector2 vel = (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * (float)Math.Cos(NPC.localAI[0] * 0.3f) * 16;
            Vector2 vel2 = vel * -1;
            vel -= NPC.velocity * 1f;
            vel2 -= NPC.velocity * 1f;
            Dust.NewDust(center, 1, 1, DustID.MagicMirror, vel.X, vel.Y);
            Dust.NewDust(center, 1, 1, DustID.MagicMirror, vel2.X, vel2.Y);

        }

        public override bool CheckActive()
        {
            return false;
        }
        public Vector2 buttom { get { return NPC.Center + new Vector2(0, 64).RotatedBy(NPC.rotation + MathHelper.PiOver2); } }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spawnAnm > 0)
            {
                return false;
            }
            float rot = NPC.rotation + MathHelper.PiOver2;

            Texture2D tex = NPC.getTexture();
            if (phase == 2 && aitype == 5)
            {
                tex = bodyAltTex.Value;
            }
            Color color = Color.White;



            float erot = 0;
            erot += (1f - (1f / (1f + NPC.velocity.Length()))) * 0.12f;

            Texture2D l1 = backTex.Value;
            Texture2D l2 = midTex.Value;
            Texture2D l3 = frontTex.Value;

            Main.EntitySpriteDraw(l1, buttom - Main.screenPosition, null, color, rot - erot, new Vector2(40, 46), NPC.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(l1, buttom - Main.screenPosition, null, color, rot + erot, new Vector2(0, 46), NPC.scale, SpriteEffects.FlipHorizontally);
            Main.EntitySpriteDraw(l2, buttom - Main.screenPosition, null, color, rot - erot * 5, new Vector2(76, 34), NPC.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(l2, buttom - Main.screenPosition, null, color, rot + erot * 5, new Vector2(84 - 76, 46), NPC.scale, SpriteEffects.FlipHorizontally);

            Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition, null, color, rot, tex.Size() / 2, NPC.scale, SpriteEffects.None);

            Main.EntitySpriteDraw(l3, buttom - Main.screenPosition, null, color, rot - erot, new Vector2(50, 6), NPC.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(l3, buttom - Main.screenPosition, null, color, rot + erot, new Vector2(4, 6), NPC.scale, SpriteEffects.FlipHorizontally);

            return false;
        }

        public void drawRope()
        {
            if (rope == null)
            {
                return;
            }
            if (ropeLerp <= 0)
            {
                return;
            }
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<Vector2> points = new List<Vector2>();
            points = rope.GetPoints();

            points.Insert(0, buttom);
            points.Add(cell.Center);
            points.Add(cell.Center);
            float lc = 1;
            float jn = 0;

            for (int i = 1; i < points.Count - 1; i++)
            {
                jn += CEUtils.getDistance(points[i - 1], points[i]) / (float)28 * lc;

                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 7 * lc,
                      new Vector3(jn, 1, 1),
                      Color.White));
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 7 * lc,
                      new Vector3(jn, 0, 1),
                      Color.White));

            }

            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                gd.Textures[0] = nihRopeTex.Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
        }
    }
}
