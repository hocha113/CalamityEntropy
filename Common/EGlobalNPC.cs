using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.DamageClasses;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.Cards;
using CalamityEntropy.Content.Items.Accessories.EvilCards;
using CalamityEntropy.Content.Items.Accessories.Hungry;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Donator.RocketLauncher;
using CalamityEntropy.Content.Items.Donator.RocketLauncher.Ammo;
using CalamityEntropy.Content.Items.Pets;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Items.Weapons.Swirlblades;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs;
using CalamityEntropy.Content.NPCs.FriendFinderNPC;
using CalamityEntropy.Content.NPCs.VoidInvasion;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Pets;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static Terraria.ModLoader.ModContent;

namespace CalamityEntropy.Common
{
    public class EGlobalNPC : GlobalNPC, ICELoader
    {
        //注意:这些字段只在客户端绘制钩子里读,专用服务器上恒为 null。
        [VaultLoaden("CalamityEntropy/Assets/Extra/AbyssalCircle")]
        internal static Asset<Texture2D> AbyssalCircleTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/SoulDiscorderColorMap")]
        internal static Asset<Texture2D> SoulDiscorderColorMapTex;
        [VaultLoaden("CalamityEntropy/Assets/Effects/SoulDiscorder", AssetMode.EffectValue, "EnchantedPass")]
        internal static Effect SoulDiscorderShader;
        [VaultLoaden("CalamityEntropy/Assets/Effects/HeatDeath", AssetMode.EffectValue, "EnchantedPass")]
        internal static Effect HeatDeathShader;
        public int VoidTouchTime = 0;
        public float VoidTouchLevel = 0;
        public float VoidTouchDR = 0;
        public int vtnoparticle = 0;
        public float damageMul = 1;
        public int AnimaTrapped = 0;
        public int[] tfriendlyNPCHitCooldown = new int[201];
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return true;
        }
        public float DebuffDamageMult()
        {
            float r = 1;
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.Entropy().hasAcc("Leyla"))
                {
                    // 2026-08-31 平衡案:莱拉去成长,固定+50% debuff伤害
                    r += Leyla.DoTBonus;
                }
            }
            return r;
        }
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            // 原生重实现：原先靠 IL 钩灾厄 DoT 管线应用减益伤害倍率，现直接放大负生命回复。
            // 乘区链定稿（已核实）：GlobalNPC 按 FullName 字母序执行，EDamageOverTimeNPC 恒先于本类，
            // 其自乘已删除——原版减益与本模组 DotBuff 的倍率统一由此处全局放大（各乘一次，无重叠）；
            // PortsDoT 自研 DoT 由 CEDoTGlobalNPC 自乘（Core 命名空间字母序晚于本类，不吃本处放大）。
            float dotMult = DebuffDamageMult();
            if (dotMult > 1f && npc.lifeRegen < 0)
            {
                npc.lifeRegen = (int)(npc.lifeRegen * dotMult);
                // 跳字随倍率同步放大，保持显示与实际伤害一致（全局放大只改 lifeRegen 不改跳字）
                damage = (int)(damage * dotMult);
            }
        }
        public static void RemoveAllTags(NPC npc)
        {
            npc.GetGlobalNPC<WhipDebuffNPC>().Tags.Clear();
            for (int i = 0; i < NPC.maxBuffs; i++)
            {
                if (npc.buffTime[i] <= 0)
                    continue;
                if (BuffID.Sets.IsATagBuff[npc.buffType[i]])
                    npc.buffTime[i] = 0;
            }
        }
        public bool nextHitCrit = false;
        public StatModifier critDamage = new StatModifier(2, 1);
        public int Lifetime = 0;
        public override bool InstancePerEntity => true;
        public int dscd = 0;
        public bool daTarget = false;
        public bool ToFriendly = false;
        public int hitCd = 30;
        public int f_target = -1;
        public Vector2? plrOldPos = null;
        public Vector2? plrOldVel = null;
        public Vector2? plrOldPos2 = null;
        public Vector2? plrOldVel2 = null;
        public Vector2? plrOldPos3 = null;
        public Vector2? plrOldVel3 = null;
        public int applyMarkedOfDeath = 0;
        public int StareOfAbyssLevel = 0;
        public int EclipsedImprintLevel = 0;
        public int StareOfAbyssTime = 0;
        public int EclipsedImprintTime = 0;
        public int friendFinderOwner = 0;
        public float TDRCounter = 3 * 60 * 60;
        public int HitCounter = 0;
        public static float DamageReduceMult(NPC npc)
        {
            float mult = 1;

            if (npc.HasBuff<Koishi>())
                mult -= 0.2f;
            if (npc.HasBuff<SoulDisorder>())
                mult -= 0.12f;
            if (npc.HasBuff<VoidVirus>())
                mult -= 0.12f;
            if (npc.Entropy().Decrease20DR > 0)
                mult -= 0.2f;

            if (mult < 0)
                mult = 0;

            // 原灾厄 DR 越高衰减越弱的补偿项已随灾厄 DR 体系移除，倍率直接生效
            if (npc.HasBuff<LifeOppress>())
                mult -= 0.25f;

            if (mult < 0)
                mult = 0;

            return mult;
        }
        public override void SetupTravelShop(int[] shop, ref int nextSlot)
        {
            if (Main.rand.NextBool(4))
            {
                shop[nextSlot] = ModContent.ItemType<ExquisiteBookmarkHolder>();
                nextSlot++;
            }
            if (Main.rand.NextBool(10))
            {
                shop[nextSlot] = ModContent.ItemType<BigShotsWing>();
                nextSlot++;
            }
        }
        public override void SetStaticDefaults()
        {
            //---如果希望注册原版NPC，解除下面的注释查看效果---///
            //VaultUtils.LoadenNPCStaticImmunityData(
            //    npcSourceID: NPCID.TheDestroyer,
            //    npcIDs: [NPCID.TheDestroyerBody, NPCID.TheDestroyerTail],
            //    staticImmuneCool: 10
            //);

            //---如果希望手动调整源NPC的无敌帧，使用 VaultUtils.SetStaticImmunity，在合适的时机将无敌帧设置为0即可取消无敌状态---//
        }
        public List<Vector2> getAbyssalCirclePointsRelative(NPC npc, float distAdd = 0, float c = 1)
        {
            float dist = (npc.width + npc.height) / 2f + 30 - (float)Math.Cos(Main.GlobalTimeWrappedHourly) * 12 + distAdd;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 60; i++)
            {
                points.Add(new Vector2(dist, 0).RotatedBy(MathHelper.ToRadians(i * 6 - 80 * c * Main.GlobalTimeWrappedHourly)));
            }
            return points;
        }
        public override bool? CanBeCaughtBy(NPC npc, Item item, Player player)
        {
            if (npc.type == NPCID.FairyCritterBlue || npc.type == NPCID.FairyCritterGreen || npc.type == NPCID.FairyCritterPink)
            {
                return true;
            }

            return base.CanBeCaughtBy(npc, item, player);
        }
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (needExitShader)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.begin_();
            }
            if (npc.Entropy().StareOfAbyssLevel > 0)
            {

                float alpha = npc.Entropy().StareOfAbyssLevel / 12f;
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<Vector2> points = npc.Entropy().getAbyssalCirclePointsRelative(npc, -50);
                    List<Vector2> pointsOutside = npc.Entropy().getAbyssalCirclePointsRelative(npc, 50);
                    int i;
                    for (i = 0; i < points.Count; i++)
                    {
                        ve.Add(new ColoredVertex(npc.Center - Main.screenPosition + points[i],
                              new Vector3((float)i / points.Count, 1, 1),
                              Color.SkyBlue * 0.66f * alpha));
                        ve.Add(new ColoredVertex(npc.Center - Main.screenPosition + pointsOutside[i],
                              new Vector3((float)i / points.Count, 0, 1),
                              Color.SkyBlue * 0.66f * alpha));

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3)
                    {
                        Texture2D tx = AbyssalCircleTex.Value;
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    List<Vector2> points = npc.Entropy().getAbyssalCirclePointsRelative(npc, -50, -1);
                    List<Vector2> pointsOutside = npc.Entropy().getAbyssalCirclePointsRelative(npc, 50, -1);
                    int i;
                    for (i = 0; i < points.Count; i++)
                    {
                        ve.Add(new ColoredVertex(npc.Center - Main.screenPosition + points[i],
                              new Vector3((float)i / points.Count, 1, 1),
                              Color.SkyBlue * 0.66f * alpha));
                        ve.Add(new ColoredVertex(npc.Center - Main.screenPosition + pointsOutside[i],
                              new Vector3((float)i / points.Count, 0, 1),
                              Color.SkyBlue * 0.66f * alpha));

                    }
                    SpriteBatch sb = Main.spriteBatch;
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3)
                    {
                        Texture2D tx = AbyssalCircleTex.Value;
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
        }
        public int LastLife = -1;
        public int StickByMissile = 0;
        public float MissileDamageAddition = 0;
        public override void PostAI(NPC npc)
        {
            Lifetime++;
            if (Lifetime > 3 * 60 * 60 && npc.ModNPC != null && npc.ModNPC is FriendFindNPC)
            {
                npc.active = false;
            }
            if (StickByMissile > 0)
            {
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.ModProjectile != null && proj.ModProjectile is BaseMissileProj bmp)
                    {
                        MissileDamageAddition += bmp.StickDamageAddition;
                    }
                }
            }
            if (LastLife < 0)
                LastLife = npc.life;

            if (npc.HasBuff<LifeOppress>())
            {
                if (npc.life > LastLife && !npc.dontTakeDamage)
                {
                    npc.life = LastLife;
                }
            }

            if (npc.life >= 0)
            {
                LastLife = npc.life;
            }
            HitCounter++;
            if (TDRCounter > 0)
            {
                TDRCounter -= 0.75f;
                if (TDRCounter < 0)
                    TDRCounter = 0;
            }
            noelctime--;
            if (deusBloodOut > 0 && !npc.dontTakeDamage)
            {
                int dmgApply = (int)(deusBloodOut * 0.01f + 1);
                if (dmgApply > deusBloodOut)
                {
                    dmgApply = deusBloodOut;
                }
                deusBloodOut -= dmgApply;
                dmgApply *= 6;
                (npc.realLife >= 0 ? npc.realLife.ToNPC() : npc).life -= dmgApply;
                if ((npc.realLife >= 0 ? npc.realLife.ToNPC() : npc).life < 1)
                {
                    (npc.realLife >= 0 ? npc.realLife.ToNPC() : npc).life = 1;
                }
                if ((npc.realLife >= 0 ? npc.realLife.ToNPC() : npc).life <= 5)
                {
                    //deusBloodOut = 0;
                }
            }
            for (int i = 0; i < tfriendlyNPCHitCooldown.Length; i++)
            {
                if (tfriendlyNPCHitCooldown[i] > 0)
                {
                    tfriendlyNPCHitCooldown[i]--;
                }
            }
            if (StareOfAbyssTime > 0)
            {
                StareOfAbyssTime--;
            }
            if (StareOfAbyssTime <= 0)
            {
                StareOfAbyssLevel = 0;
            }
            if (EclipsedImprintTime > 0)
            {
                EclipsedImprintTime--;
            }
            if (EclipsedImprintTime <= 0)
            {
                EclipsedImprintLevel = 0;
            }

            if (applyMarkedOfDeath > 0)
            {
                // 自研移植的死亡标记（debuff-map：PortsDoT 同短名，受击伤害 ×1.1）
                npc.AddBuff(ModContent.BuffType<MarkedforDeath>(), applyMarkedOfDeath);
                applyMarkedOfDeath = 0;
            }
            if (plrOldPos.HasValue)
            {
                Main.player[0].position = plrOldPos.Value;
                plrOldPos = null;
            }
            if (plrOldVel.HasValue)
            {
                Main.player[0].velocity = plrOldVel.Value;
                plrOldVel = null;
            }
            if (plrOldPos2.HasValue)
            {
                Main.player[0].position = plrOldPos2.Value;
                plrOldPos2 = null;
            }
            if (plrOldVel2.HasValue)
            {
                Main.player[0].velocity = plrOldVel2.Value;
                plrOldVel2 = null;
            }
            if (!ToFriendly)
            {
                ModContent.GetInstance<EModSys>().LastPlayerVel = Main.player[0].velocity;
                ModContent.GetInstance<EModSys>().LastPlayerPos = Main.player[0].Center;

            }
        }
        public static int TamedDmgMul = 16;
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            /*binaryWriter.Write(StareOfAbyssLevel);
            binaryWriter.Write(StareOfAbyssTime);
            binaryWriter.Write(EclipsedImprintLevel);
            binaryWriter.Write(EclipsedImprintTime);*/
            /*
            binaryWriter.Write(VoidTouchLevel);
            binaryWriter.Write(VoidTouchTime);
            */
        }
        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (npc.type == ModContent.NPCType<PrimordialWyrmNPC>() && projectile.friendly)
            {
                return false;
            }
            return null;
        }
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (target.Entropy().immune > 0)
                return false;
            if (target.ownedProjectileCounts[ModContent.ProjectileType<TSSlash>()] > 0)
            {
                return false;
            }
            if (AnimaTrapped > 0)
            {
                return false;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }
        public override bool CanHitNPC(NPC npc, NPC target)
        {
            if (AnimaTrapped > 0)
            {
                return false;
            }
            return base.CanHitNPC(npc, target);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            /*StareOfAbyssLevel = binaryReader.ReadInt32();
            StareOfAbyssTime = binaryReader.ReadInt32();
            EclipsedImprintLevel = binaryReader.ReadInt32();
            EclipsedImprintTime = binaryReader.ReadInt32();*/
            /* VoidTouchLevel = binaryReader.ReadSingle();
             VoidTouchTime = binaryReader.ReadInt32();*/
        }
        public bool ffoFlag = false;
        public static void setFriendly(int id, int owner = 0)
        {
            if (id.ToNPC().Entropy().ToFriendly)
            {
                return;
            }
            id.ToNPC().Entropy().ToFriendly = true;
            id.ToNPC().Entropy().f_owner = owner;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket p = CalamityEntropy.Instance.GetPacket();
                p.Write((byte)CEMessageType.TurnFriendly);
                p.Write(id);
                p.Write(owner);
                p.Send();
            }

        }
        public bool friendlyDecLife = true;
        public int counter = 0;
        public int Decrease20DR = 0;
        public override bool PreAI(NPC npc)
        {
            if (HungryTagged > 0)
                HungryTagged--;
            if (Decrease20DR > 0)
                Decrease20DR--;
            StickByMissile--;
            MissileDamageAddition = 0;
            if (npc.ModNPC != null && npc.ModNPC is FriendFindNPC ff)
            {
                if (npc.localAI[3] > 0)
                {
                    friendFinderOwner = (int)npc.localAI[3] - 1;
                    npc.localAI[3] = 0;
                    ffoFlag = true;
                }
                if (ffoFlag)
                {
                    float slots = 0;
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (n.ModNPC is FriendFindNPC)
                        {
                            slots += 1;
                        }
                    }
                    if (slots > friendFinderOwner.ToPlayer().maxMinions + friendFinderOwner.ToPlayer().Entropy().ffDecSlot)
                    {
                        npc.active = false;
                        return false;
                    }
                }
            }
            counter++;
            if (npc.Entropy().EclipsedImprintLevel > 0)
            {
                int c = 16 - npc.Entropy().EclipsedImprintLevel;
                if (counter % c == 0)
                {
                    //PRT_AbyssalLine日蚀印记光环,lx/xadd/spawnColor spawn后直赋
                    var __prt = PRTLoader.NewParticle<PRT_AbyssalLine>(npc.Center, Vector2.Zero, Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
                    __prt.lx = 1.2f;
                    __prt.xadd = 0.32f;
                    __prt.spawnColor = Color.Gold;
                    __prt.endColor = Color.DarkGoldenrod;

                }
            }
            /*if (ToFriendly)
            {
                if (friendlyDecLife)
                {
                    friendlyDecLife = false;
                    npc.life = 1 + (int)(npc.life / damageMul);
                }
                hitCd--;
                npc.boss = false;
                
                npc.friendly = true;

                bool h = false;
                foreach (NPC npcc in Main.npc)
                {
                    if (npcc.active && !npcc.friendly && !npcc.dontTakeDamage && npc.Hitbox.Intersects(npcc.Hitbox))
                    {
                        if (hitCd <= 0 && !(Main.netMode == NetmodeID.MultiplayerClient))
                        {
                            h = true;
                            npcc.StrikeNPC(npcc.CalculateHitInfo(npc.damage * TamedDmgMul, npc.velocity.X > 0 ? 1 : -1, false, 6, DamageClass.Generic));
                        }
                    }
                }
                if (h)
                {
                    hitCd = 20;
                }
                NPC t = null;
                float dist = 4600;
                foreach (NPC n in Main.npc)
                {
                    if (n.active && !n.friendly && !n.dontTakeDamage)
                    {
                        if (Util.getDistance(n.Center,npc.Center) < dist)
                        {
                            t = n;
                            dist = Util.getDistance(n.Center, npc.Center);
                        }
                    }
                }
                if (t == null)
                {
                    f_target = -1;
                    plrOldPos = Main.player[0].position;
                    plrOldVel = Main.player[0].velocity;
                    Main.player[0].Center = f_owner.ToPlayer().Center;
                    Main.player[0].velocity = f_owner.ToPlayer().velocity;
                }
                else
                {
                    f_target = t.whoAmI;
                    plrOldPos = Main.player[0].position;
                    plrOldVel = Main.player[0].velocity;
                    Main.player[0].Center = t.Center;
                    Main.player[0].velocity = t.velocity;
                }
                if (npc.aiStyle == NPCAIStyleID.Slime)
                {
                    Main.LocalPlayer.npcTypeNoAggro[npc.type] = false;
                    npc.TargetClosest();
                }
                if (npc.realLife < 0) {
                    foreach (NPC nPC in Main.npc)
                    {
                        if (nPC.realLife == npc.whoAmI)
                        {
                            nPC.Entropy().ToFriendly = true;
                            nPC.Entropy().f_owner = f_owner;
                        }
                    } 
                }
            }*/
            if (npc.Entropy().daTarget && npc.realLife == -1)
            {
                npc.velocity *= 0;
                return false;
            }
            dscd--;
            vtnoparticle--;
            if (npc.Entropy().VoidTouchTime > 0)
            {
                if (vtnoparticle <= 0 && false)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        var rd = Main.rand;
                        var p = PRTLoader.NewParticle<PRT_Void>(npc.Center, new Vector2((float)((rd.NextDouble() - 0.5) * 6), (float)((rd.NextDouble() - 0.5) * 6)), Color.White, 1f);
                        p.Opacity = 0.5f;
                    }
                }
                if (Main.GameUpdateCount % 20 == 0 && !npc.dontTakeDamage)
                {
                    NPC.HitInfo hit = npc.CalculateHitInfo((int)(26 * npc.Entropy().VoidTouchLevel * (1 - npc.Entropy().VoidTouchDR)), 0, false, 0, DamageClass.Generic, false, 0);
                    hit.HideCombatText = true;
                    int damageDone = npc.StrikeNPC(hit, false, false);
                    CombatText.NewText(npc.getRect(), new Color(148, 148, 255), damageDone);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendStrikeNPC(npc, hit);
                    }

                }
                if (!(npc.ModNPC is VoidCultist))
                {
                    if (!npc.boss)
                    {
                        npc.velocity *= 0.96f;
                    }
                }
                var r = Main.rand;
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.CorruptSpray, (float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1);
                npc.Entropy().VoidTouchTime = VoidTouchTime - 1;
            }
            if (npc.Entropy().VoidTouchTime > 0)
            {
                npc.AddBuff(ModContent.BuffType<VoidTouch>(), npc.Entropy().VoidTouchTime);
            }
            else
            {
                npc.Entropy().VoidTouchLevel = 0;
            }

            return base.PreAI(npc);
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage += MissileDamageAddition;
            if (npc.active)
            {
                if (npc.HasBuff<HeatDeath>())
                {
                    modifiers.FinalDamage *= 1.1f;
                }
                if (modifiers.DamageType != null && modifiers.DamageType.CountsAsClass(NoDRMelee.Instance))
                {
                    if (modifiers.FinalDamage.Multiplicative < 1 && modifiers.FinalDamage.Multiplicative > 0)
                    {
                        modifiers.FinalDamage /= modifiers.FinalDamage.Multiplicative;
                    }
                }
            }
            // 原生重实现：原先通过 IL 把 DamageReduceMult 乘进灾厄 DR 属性，
            // 现改为按等效比例直接放大受击伤害（mult 每降 1% 即多受 1% 伤害）
            float drMult = DamageReduceMult(npc);
            if (drMult < 1f)
            {
                modifiers.FinalDamage *= 2f - drMult;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            // 原生重实现：鞭类 tag 增伤与强制暴击原先经 IL 钩灾厄的 ModifyHitByProjectile 触发，
            // 灾厄钩子拆除后改由本钩子直接调用
            npc.GetGlobalNPC<WhipDebuffNPC>().ModifyHitByProj(npc, projectile, ref modifiers);

            modifiers.FinalDamage += (npc.Entropy().VoidTouchLevel) * 0.01f * (1 - npc.Entropy().VoidTouchDR);
            if (projectile.owner >= 0 && projectile.friendly)
            {
                if (projectile.GetOwner().Entropy().CritDamage != null)
                {
                    foreach (var v in projectile.GetOwner().Entropy().CritDamage)
                    {
                        if (projectile.DamageType.CountsAsClass(v.Key) || v.Key.CountsAsClass(DamageClass.Generic))
                        {
                            modifiers.CritDamage += v.Value - 1;
                        }
                    }
                }
                if (projectile.GetOwner().Entropy().hasAcc("HEATDEATH"))
                {
                    npc.AddBuff(ModContent.BuffType<HeatDeath>(), 8 * 60);
                }
                if (projectile.owner.ToPlayer().Entropy().nihShell)
                {
                    modifiers.CritDamage += NihilityShell.CirtDamageAddition;
                }
                if (projectile.owner.ToPlayer().Entropy().devouringCard)
                {
                    modifiers.ArmorPenetration += npc.defense * DevouringCard.ArmorPene;
                }
                if (projectile.GetOwner().Entropy().hasAcc(SmartScope.ID))
                {
                    modifiers.FinalDamage *= 0.75f;
                }
            }
            if (projectile.owner >= 0)
            {
                if (projectile.owner.ToPlayer().Entropy().VFSet)
                {
                    // 潜行系统退役：原潜伏攻击额外充能分支移除，统一按普通命中充能
                    // 2026-08-31 平衡案:虚湮吞天盔额外充能随职业奖励重做退役
                    projectile.owner.ToPlayer().Entropy().VoidCharge += 0.008f;
                    if (projectile.owner.ToPlayer().Entropy().VoidCharge > 1)
                    {
                        projectile.owner.ToPlayer().Entropy().VoidCharge = 1;
                    }
                }
            }
            critDamage = modifiers.CritDamage;
        }
        public int HungryTagged = 0;
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (player.Entropy().devouringCard)
            {
                modifiers.ArmorPenetration += npc.defense * DevouringCard.ArmorPene;
            }
            if (player.Entropy().hasAcc("HEATDEATH"))
            {
                npc.AddBuff(ModContent.BuffType<HeatDeath>(), 8 * 60);
            }
            if (player.Entropy().nihShell)
            {
                modifiers.CritDamage += NihilityShell.CirtDamageAddition;
            }
            if (player.Entropy().CritDamage != null)
            {
                foreach (var v in player.Entropy().CritDamage)
                {
                    if (item.DamageType.CountsAsClass(v.Key) || v.Key.CountsAsClass(DamageClass.Generic))
                    {
                        modifiers.CritDamage += v.Value - 1;
                    }
                }
            }
            modifiers.FinalDamage += (npc.Entropy().VoidTouchLevel) * 0.05f * (1 - npc.Entropy().VoidTouchDR);
            if (player.Entropy().VFSet)
            {
                player.Entropy().VoidCharge += 0.008f;

                if (player.Entropy().VoidCharge > 1)
                {
                    player.Entropy().VoidCharge = 1;
                }
            }

            critDamage = modifiers.CritDamage;
        }

        public static bool AddVoidTouch(NPC nPC, int time, float level, int maxTime = 600, int maxLevel = 10)
        {
            if (nPC.Entropy().VoidTouchDR == 1)
            {
                return false;
            }
            if (nPC.Entropy().VoidTouchTime < maxTime)
            {
                nPC.Entropy().VoidTouchTime += (int)(time * 1.4f);
                if (nPC.Entropy().VoidTouchTime > maxTime)
                {
                    nPC.Entropy().VoidTouchTime = maxTime;
                }
            }
            if (nPC.Entropy().VoidTouchLevel < maxLevel)
            {
                nPC.Entropy().VoidTouchLevel += level / 10;
                if (nPC.Entropy().VoidTouchLevel > maxLevel)
                {
                    nPC.Entropy().VoidTouchLevel = maxLevel;
                }
            }
            return true;
        }
        public static bool AddVoidTouch(Player nPC, int time, int level, int maxTime = 600, int maxLevel = 10)
        {
            nPC.AddBuff(ModContent.BuffType<VoidTouch>(), maxTime);
            return true;
        }
        public static ReLogic.Content.Asset<Texture2D> Request(string p)
        {
            return ModContent.Request<Texture2D>(p);
        }
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (npc.Entropy().daTarget)
            {
                drawColor = Color.Black;
            }
        }
        // 灾厄 NPC 掉落注入已整体拆除，并已按 bookmark-rehang.md 重挂（见方法末尾重挂段；无映射条目的物品另有表外增补裁定）
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 掉落规则全部为 tML 原生写法：Common = 无条件 1/n，ByCondition(NotExpert) = 仅普通模式（原灾厄掉落扩展的等价改写）
            List<int> osseousRemainsDropEnemies = new List<int>() { 174, 101, 94, 173, -22, -23, 181, 6, -11, -12 };
            if (osseousRemainsDropEnemies.Contains(npc.type))
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OsseousRemains>(), 3, 6, 8));
            }
            if (npc.type == NPCID.MoonLordCore)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<MoonlightCore>(), 3));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Nothing>(), 3));
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsMasterMode(), ModContent.ItemType<DeusCore>()));
            }
            if (npc.type == NPCID.GoblinSorcerer)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Tarnish>(), 3));
            }
            if (npc.type == NPCID.BloodNautilus)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Fool>(), 5));
            }
            if (npc.type == NPCID.WallofFlesh)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<HungryLantern>(), 5));
            }
            if (npc.type == NPCID.BrainofCthulhu)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<CreeperWand>(), 3));
            }
            if (npc.type == NPCID.SkeletronHead)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<OblivionSkull>()));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkSagittarius>()));
            }
            if (npc.type == NPCID.KingSlime)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<ExquisiteCrown>(), 3));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<EntityCard>(), 3));
            }
            if (npc.type == NPCID.EyeofCthulhu)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<RottenFangs>(), 3));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WisperCard>(), 3));
            }
            if (npc.type == NPCID.Deerclops)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<BookmarkSnowgrave>(), 5));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Frail>(), 3));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkAries>(), 3));
            }
            if (npc.type == NPCID.Paladin)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DevouringCard>(), 2));
                npcLoot.Add(ItemDropRule.ByCondition(new PostMoonLord(), ModContent.ItemType<AnimaSola>(), 20));
            }
            if (npc.type == NPCID.Golem)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<MourningCard>(), 2));
            }
            if (npc.type == NPCID.DungeonSpirit)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RequiemCard>(), 16));
            }
            if (npc.type == NPCID.BigMimicHallow)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurificationCard>(), 2));
            }
            if (npc.type == NPCID.Plantera)
            {
                // 原 3/5 与 2/5 概率，分子写法保持不化简
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<LashingBramblerod>(), 5, 1, 1, 3));
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<MutantBulb>(), 5, 1, 1, 2));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkAquarius>(), 3));
            }
            if (npc.type == NPCID.WyvernHead)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VetrasylsEye>(), 20));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkAerialite>(), 10));
            }
            if (npc.boss)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkPerfection>(), 30));
            }
            if (npc.type == NPCID.QueenSlimeBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<Crystedge>(), 4));
            }
            // —— 以下为脱离灾厄重挂（bookmark-rehang.md：原灾厄 Boss 掉落改挂自然敌怪 / 自有 Boss）——
            if (npc.type == NPCID.Vulture || npc.type == NPCID.Antlion || npc.type == NPCID.WalkingAntlion || npc.type == NPCID.FlyingAntlion || npc.type == NPCID.TombCrawlerHead)
            {
                // 原灾厄荒漠灾虫掉落，改挂前期沙漠敌怪
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkLeo>(), 30));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DustyWhistle>(), 25));
            }
            if (npc.type == NPCID.TombCrawlerHead)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AntlionShell>(), 3));
            }
            if (npc.type == NPCID.AnomuraFungus || npc.type == NPCID.MushiLadybug || npc.type == NPCID.FungiBulb || npc.type == NPCID.GiantFungiBulb || npc.type == NPCID.FungoFish || npc.type == NPCID.ZombieMushroom || npc.type == NPCID.ZombieMushroomHat)
            {
                // 原灾厄菌生蟹掉落，改挂发光蘑菇群系敌怪
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookmarkSpore>(), 40));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlueFlatTopMushroom>(), 40));
                // 新材料星辉鳞尘（material-map §一）：夜间 25% 掉 1–3
                npcLoot.Add(ItemDropRule.ByCondition(new IsNight(), ModContent.ItemType<StarlitScaleDust>(), 4, 1, 3));
            }
            if (npc.type == NPCID.Shark)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TerrorOfAbyss>(), 100));
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<AbyssalPiercer>(), 50));
            }
            if (npc.type == NPCID.Shark || npc.type == NPCID.Squid || npc.type == NPCID.SeaSnail || npc.type == NPCID.PinkJellyfish)
            {
                // 原灾厄深渊怪宠物掉落，改挂困难模式海洋敌怪（misc-map §五增补段）
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<ToyRock>(), 40));
            }
            if (npc.type == NPCID.CultistBoss)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SacrificalMask>()));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SacredStone>(), 3));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Content.Items.Weapons.BuriedSun>(), 3));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Revelation>(), 3));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlazingSwirlblade>(), 3));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkProfaned>()));
            }
            if (npc.type == NPCID.Crab)
            {
                // 原灾厄菌生蟹掉落，改挂海洋螃蟹
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkCancer>(), 100));
            }
            if (npc.type == NPCID.IceElemental || npc.type == NPCID.IcyMerman || npc.type == NPCID.IceTortoise || npc.type == NPCID.ArmoredViking || npc.type == NPCID.Wolf)
            {
                // 原灾厄极地之灵掉落，改挂困难模式冰雪群系敌怪
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkIce>(), 40));
            }
            if (npc.type == NPCID.IceGolem)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FrostboundCage>(), 5));
            }
            if (npc.type == NPCID.RedDevil)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<EvilFriend>(), 20));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkBrimstone>(), 50));
            }
            if (npc.type == NPCID.Lavabat)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FriendBox>(), 100));
            }
            if (npc.type == ModContent.NPCType<Content.NPCs.Cruiser.CruiserHead>())
            {
                // 原灾厄渊海灾虫掉落，槽位并入巡游者（progression-map §五）
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WyrmTooth>(), 1, 65, 80));
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookmarkCosmic>(), 2));
                // 2026-08-31 平衡案:沐生之羽改由月亮领主掉落(Vitalfeather.cs 的 VitalfeatherDropGNPC),巡游者侧退役
            }
            if (npc.type == NPCID.BoneLee)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkBlackKnife>(), 10));
            }
            if (npc.type == NPCID.Unicorn)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkCapricorn>(), 50));
            }
            if (npc.type == NPCID.TheDestroyer)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookMarkOfNight>(), 2));
            }
            if (npc.type == NPCID.Clinger)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BookmarkSulphurous>(), 50));
            }
            if (npc.type == NPCID.DesertScorpionWalk || npc.type == NPCID.DesertScorpionWall)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new PostPlantera(), ModContent.ItemType<BookMarkScorpio>(), 50));
            }
        }
        public float WhiteLerp = 0;

        public record DebuffDisplayEntry(Func<NPC, bool> Condition, Func<Texture2D> TextureGetter);

        public static readonly List<DebuffDisplayEntry> ExternalDebuffs = [];

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 原挂靠灾厄血条体系的 Boss 头顶 debuff 图标列表已整体退役（含灾厄全局实例读取与灾厄贴图）
            needExitShader = false;
            List<Effect> shaders = new List<Effect>();
            if (npc.HasBuff<SoulDisorder>())
            {
                Effect shader = SoulDiscorderShader;
                shader.Parameters["strength"].SetValue(1);
                shader.Parameters["f1"].SetValue((float)npc.frame.Y / npc.getTexture().Height);
                shader.Parameters["f2"].SetValue((float)(npc.frame.Y + npc.frame.Height) / npc.getTexture().Height);
                shader.Parameters["offset"].SetValue(Main.GlobalTimeWrappedHourly);
                shader.Parameters["colorMap"].SetValue(SoulDiscorderColorMapTex.Value);
                shaders.Add(shader);
            }
            if (npc.HasBuff<HeatDeath>())
            {
                if (hdStrength < 1)
                {
                    hdStrength += 0.01f;
                }
                Effect shader = HeatDeathShader;
                shader.Parameters["strength"].SetValue(hdStrength * 0.6f * (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 1.3f) * 0.25f + 0.75f));
                shader.Parameters["minColor"].SetValue((Color.Lerp(Color.DarkRed, new Color(170, 0, 250), (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 2) * 0.5f + 0.5f))).ToVector4());
                shader.Parameters["maxColor"].SetValue((Color.Lerp(new Color(170, 0, 250), Color.DarkRed, (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 2) * 0.5f + 0.5f))).ToVector4());
                shaders.Add(shader);
            }
            else
            {
                if (hdStrength > 0)
                {
                    hdStrength -= 0.01f;
                }
            }
            if (WhiteLerp > 0)
            {
                WhiteLerp -= 1 / 5f;
                Effect shader = CEEffectAssets.WhiteTrans;
                shader.Parameters["strength"].SetValue(WhiteLerp);
                shaders.Add(shader);
            }
            if (shaders.Count > 0)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shaders[0], Main.GameViewMatrix.TransformationMatrix);
                shaders[shaders.Count - 1].CurrentTechnique.Passes[0].Apply();

                needExitShader = true;
            }
            /*if (CalamityEntropy.EntropyMode)
            {
                if (npc.type == NPCID.AncientLight || npc.type == NPCID.AncientDoom || EModILEdit.LostNPCsEntropy.Contains(npc.type))
                {
                    needExitShader = true;
                    Effect trans = CEEffectAssets.Trans;
                    Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, trans);
                    trans.Parameters["strength"].SetValue(1);
                    trans.Parameters["color"].SetValue(new Vector4(0, 0, 0, 1));
                    trans.CurrentTechnique.Passes[0].Apply();
                }
            }*/
            return true;
        }
        public float hdStrength = 0;
        public bool needExitShader = false;
        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            if (npc.life <= 0)
            {
                if (!Main.dedServ)
                {
                    if (npc.HasBuff<FlamingBlood>())
                    {
                        // 原灾厄穿孔者巢死亡音效为字段引用，sound-map 未收录该条：以原版血肉爆裂音近似定稿
                        SoundEngine.PlaySound(SoundID.NPCDeath12 with { Pitch = 0.4f }, npc.Center);
                        for (int i = 0; i < 90; i++)
                        {
                            PRTLoader.NewParticle<PRT_BloodCal>(npc.Center, CEUtils.randomPointInCircle(22), Color.Red, Main.rand.NextFloat(0.6f, 1)).Configure(16);
                        }
                        PRTLoader.NewParticle<PRT_CustomPulse>(npc.Center, Vector2.Zero, new Color(255, 24, 24), 0.01f).Configure("CalamityEntropy/Assets/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.01f, 0.15f, 28);
                    }
                }
            }
        }
        public override void OnKill(NPC npc)
        {
            if (npc.HasBuff<FlamingBlood>())
            {
                bool spawnExp = true;
                int dmg = (int)(npc.lifeMax * 0.15f);
                if (dmg > 100)
                    dmg = 100;
                if (dmg < 20)
                    dmg = 20;
                if (npc.lifeMax < 40)
                    dmg = 10;
                if (npc.realLife >= 0 || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
                {
                    spawnExp = Main.rand.NextBool(20);
                    dmg = 2;
                }
                var plr = Main.player[Player.FindClosest(npc.Center, 100000, 100000)];
                if (spawnExp)
                {
                    var p = CEUtils.SpawnExplotionFriendly(npc.GetSource_Death(), plr, npc.Center, dmg, 200, DamageClass.Summon);
                    if (p.ModProjectile is CommonExplotionFriendly cef)
                    {
                        void onhit(NPC npc)
                        {
                            npc.AddBuff<FlamingBlood>(16 * 60);
                        }
                        cef.modifyHitAction = onhit;
                    }
                }
            }
            if (npc.type == NPCID.WallofFlesh)
            {
                for (int i = 0; i < 32; i++)
                {
                    float rot;
                    rot = CEUtils.randomRot();
                    Main.item[Item.NewItem(npc.GetSource_Death(), npc.Center + rot.ToRotationVector2() * 128, new Item(ItemID.SoulofLight, 2))].velocity = rot.ToRotationVector2() * Main.rand.NextFloat(2, 32);
                    Main.item[Item.NewItem(npc.GetSource_Death(), npc.Center + rot.ToRotationVector2() * 128, new Item(ItemID.SoulofNight, 2))].velocity = rot.ToRotationVector2() * Main.rand.NextFloat(2, 32);
                }
            }
            if ((Main.player[Player.FindClosest(npc.Center, 1000000, 1000000)].ZoneCrimson || Main.player[Player.FindClosest(npc.Center, 1000000, 1000000)].ZoneCorrupt) && Main.player[Player.FindClosest(npc.Center, 1000000, 1000000)].Center.Y > Main.worldSurface + 256)
            {
                if (Main.rand.NextBool(54))
                {
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<BitternessCard>()));
                }
            }
            if (!npc.friendly && npc.lifeMax > 20)
            {
                if (Main.bloodMoon)
                {
                    if (Main.rand.NextBool(800))
                    {
                        Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<CrimsonNight>()));
                    }
                    // 原灾厄掉落的两张邪恶卡已改挂专属 Boss 掉落（批次I）
                }
                Player n = null;
                Player h = null;
                foreach (Player plr in Main.player)
                {
                    if (plr.active && CEUtils.getDistance(plr.Center, npc.Center) < 4000)
                    {
                        if (plr.ZoneHallow)
                        {
                            n = plr;
                        }
                        if (plr.Center.Y / 16 > Main.UnderworldLayer)
                        {
                            h = plr;
                        }
                    }
                }
                if (n != null)
                {
                    if (Main.rand.NextBool(70))
                    {
                        Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<HolyMantle>()));
                    }
                    if (Main.rand.NextBool(80) && NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                    {
                        Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<TheRevelation>()));
                    }
                }
                if (h != null)
                {
                    if (Main.rand.NextBool(60) && NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                    {
                        Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<MawOfTheVoid>()));
                    }
                }
            }
            if (ToFriendly)
            {
                Main.player[Main.player.Length - 1].active = false;
            }
            if (npc.boss)
            {
                if (Main.dedServ)
                {
                    ModPacket pack = Mod.GetPacket();
                    pack.Write((byte)CEMessageType.BossKilled);
                    // 灾厄脱钩后不存在灾厄 Boss，原「非灾厄 Boss」判定恒为真（接收端目前也未消费该值）
                    pack.Write(true);
                    pack.Send();
                }
                else
                {

                }
                if (lostSoulDrop)
                {
                    foreach (Projectile p in Main.projectile)
                    {
                        if (p.active && p.ModProjectile is LostSoulProj ls)
                        {
                            if (ls.hideVisualTime <= 0 && npc.realLife < 0)
                            {
                                ls.bosses.Add(npc.whoAmI);
                            }
                        }
                    }
                }
            }
            if (npc.type == NPCID.SkeletronHead)
            {
                Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<WisdomCard>()));

            }
            if (npc.type == NPCID.SkeletronPrime)
            {
                Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<TemperanceCard>()));
            }

            if (npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
            {
                Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<Perplexed>()));
            }
            if (npc.type == NPCID.GiantWormHead)
            {
                if (Main.rand.NextDouble() < 0.04f)
                {
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<CannedCarrion>(), 1));
                }
            }
            if (npc.type == NPCID.WyvernHead)
            {
                if (Main.rand.NextDouble() < 0.02f)
                {
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<DreamCatcher>(), 1));
                }
            }
            if (npc.type == NPCID.Harpy || npc.type == NPCID.WyvernHead)
            {
                if (Main.rand.NextDouble() < 0.012f)
                {
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<LightningPendant>(), 1));
                }
            }
            if (npc.type == NPCID.Wraith || npc.type == NPCID.PossessedArmor)
            {
                if (Main.rand.NextDouble() < 0.02f)
                {
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<SoulCandle>(), 1));
                }
                if (Main.rand.NextDouble() < 0.02f)
                {
                    Item.NewItem(npc.GetSource_Death(), npc.getRect(), new Item(ModContent.ItemType<LostSoul>(), 1));
                }
            }
        }
        public class IsNormal : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !Main.expertMode;
            public bool CanShowItemDropInUI() => !Main.expertMode;
            public string GetConditionDescription() => "Normal Only";
        }
        /// <summary>月亮领主击败后才掉落(原版无现成条件,2026-08-31 平衡案)。</summary>
        public class PostMoonLord : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => NPC.downedMoonlord;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
        /// <summary>世纪之花击败后才掉落。</summary>
        public class PostPlantera : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => NPC.downedPlantBoss;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => null;
        }
        public class IsNight : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public bool CanDrop(DropAttemptInfo info) => !Main.dayTime;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => "At night";
        }
        public int f_owner = -1;
        public bool lostSoulDrop = true;
        public int deusBloodOut = 0;
        public int noelctime = 0;
        public void onHurt(NPC npc, int damage, Player player, Entity source, NPC.HitInfo hit)
        {
            if (player != null && player.Entropy().hasAcc("Leyla"))
            {
                var l = Leyla.ApplyBuffType();
                foreach (int i in l)
                {
                    if (Main.rand.NextBool(10))
                        npc.AddBuff(i, Main.rand.Next(60, 300));
                }

            }
            if (npc.life <= 0)
            {
                if (player != null && player.Entropy().goldenRock != null && player.Entropy().goldenRock.ModItem is GoldenRock gr)
                {
                    gr.price += int.Min(5000, (int)npc.value) + npc.lifeMax / 5;
                }
            }
            HitCounter = 0;
            if (player != null)
            {
                player.Entropy().lastHitTarget = npc;
                if (player.Entropy().NihilitySet)
                {
                    if (CECooldowns.CheckCD("NihilityLasers", 150))
                    {
                        player.Entropy().ShootLaserTime = 20;
                    }
                }
                if (player.Entropy().LifeStealP > 0 && player.statLife < player.statLifeMax2 && CECooldowns.CheckCD("LifeStealHealFloat"))
                {
                    player.Entropy().HealFloat(player.statLifeMax2 * player.Entropy().LifeStealP);
                }
                if (player.Entropy().hasAcc("VastLV5") && hit.Crit)
                {
                    npc.AddBuff<SoulDisorder>(360);
                }
                // 崇拜圣物/疾风腕刃/诡雷盒的旧投掷命中接线已整体退役，新效果由饰品文件自含实现
                if (player.Entropy().grudgeCard)
                {
                    if (Main.rand.NextBool(4) && !CECooldowns.HasCooldown("GrudgeCD"))
                    {
                        CECooldowns.AddCooldown("GrudgeCD", GrudgeCard.TriggerCooldown);
                        Projectile.NewProjectile(player.GetSource_FromThis(), npc.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4, 5), ModContent.ProjectileType<HealingSpirit>(), 0, 0, player.whoAmI);
                    }
                }
                if (player.Entropy().heartOfStorm)
                {
                    // 2026-08-31 平衡案:重做为命中目标时召唤闪电,内置冷却1秒,基础伤害800
                    if (source is not Projectile srcProj || srcProj.type != ModContent.ProjectileType<ElectricLaser>())
                    {
                        if (CECooldowns.CheckBMProc("HeartOfStormBolt", 60))
                        {
                            int boltDamage = (int)player.GetTotalDamage(DamageClass.Generic).ApplyTo(800);
                            Projectile.NewProjectile(player.GetSource_FromThis(), npc.Center - new Vector2(0, 480), Vector2.Zero, ModContent.ProjectileType<ElectricLaser>(), boltDamage, 0, player.whoAmI, npc.Center.X, npc.Center.Y, 0);
                        }
                    }
                }
            }
        }
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            onHurt(npc, damageDone, player, null, hit);
            if (player.Entropy().deusCoreBloodOut > 0 && player.Entropy().bloodTrCD <= 0)
            {
                int btransfer = (int)MathHelper.Min(player.Entropy().deusCoreBloodOut, player.Entropy().deusCoreBloodOut / 13 + 1);
                if (btransfer > 120)
                {
                    btransfer = 120;
                }
                player.Entropy().bloodTrCD = 42;
                player.Entropy().deusCoreBloodOut -= btransfer;
                deusBloodOut += btransfer * 5;
            }
            if (player.Entropy().nihShell)
            {
                NihilityShell.checkDamage(player, hit);
            }
            if (player.Entropy().ConfuseCard && !npc.boss)
            {
                npc.AddBuff(ModContent.BuffType<Deceive>(), 420);
            }
            if (player.Entropy().AttackVoidTouch > 0)
            {
                float vt = player.Entropy().AttackVoidTouch * 10;
                AddVoidTouch(npc, (int)(vt * 120), vt, 600, (int)Math.Round(vt * 8));
            }
            player.Entropy().damageRecord += damageDone;
            if (player.Entropy().brokenAnkh && player.Entropy().damageRecord > 420)
            {
                player.Entropy().damageRecord = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int i = Item.NewItem(player.GetSource_FromThis(), player.getRect(), new Item(ModContent.ItemType<PoopPickup>()), false, true);
                    Main.item[i].noGrabDelay = 100;
                    if (!Main.dedServ)
                    {
                        CEUtils.PlaySound("fart", 1, player.Center);
                    }
                }
                else
                {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)CEMessageType.SpawnItem);
                    packet.Write(player.whoAmI);
                    packet.Write(ModContent.ItemType<PoopPickup>());
                    packet.Write(1);
                    packet.Send();
                }
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Player sourcePlr = null;
            if (projectile.friendly)
            {
                Player player = projectile.owner.ToPlayer();
                sourcePlr = player;

                if (ProjectileID.Sets.IsAWhip[projectile.type])
                {
                    if (player.Entropy().ashesCore)
                    {
                        foreach (Projectile proj in Main.ActiveProjectiles)
                        {
                            if (proj.owner == player.whoAmI && proj.type == AshesCore.ProjType)
                            {
                                if (CECooldowns.CheckCD("AshesFireball", 20))
                                {
                                    var vc = (npc.Center - proj.Center).normalize();
                                    CEUtils.PlaySound("YharonFireball1", 0.9f, npc.Center);
                                    CEUtils.PlaySound("YharonFireball1", 0.9f, npc.Center);
                                    Projectile.NewProjectile(proj.GetSource_FromThis(), proj.Center, vc * 36, ModContent.ProjectileType<AshesSpiritFireball>(), AshesCore.BaseDamage, 4, player.whoAmI);
                                    proj.velocity += vc * -12;
                                }
                            }
                        }
                    }
                }
                if (player.Entropy().deusCoreBloodOut > 0 && player.Entropy().bloodTrCD <= 0)
                {
                    int btransfer = (int)MathHelper.Min(player.Entropy().deusCoreBloodOut, player.Entropy().deusCoreBloodOut / 13 + 1);
                    if (btransfer > 120)
                    {
                        btransfer = 120;
                    }
                    player.Entropy().bloodTrCD = 42;
                    player.Entropy().deusCoreBloodOut -= btransfer;
                    deusBloodOut += btransfer * 5;
                }
                if (player.Entropy().ConfuseCard && !npc.boss)
                {
                    npc.AddBuff(ModContent.BuffType<Deceive>(), 420);
                }
                if (projectile.owner != -1)
                {
                    if (projectile.owner.ToPlayer().active)
                    {
                        if (projectile.owner.ToPlayer().Entropy().AttackVoidTouch > 0)
                        {
                            float vt = projectile.owner.ToPlayer().Entropy().AttackVoidTouch * 10;
                            AddVoidTouch(npc, (int)(vt * 120), vt, 600, (int)Math.Round(vt * 8));
                        }
                    }
                }
                if (player.Entropy().nihShell)
                {
                    NihilityShell.checkDamage(player, hit);
                }
                player.Entropy().damageRecord += damageDone;
                if (player.Entropy().brokenAnkh && player.Entropy().damageRecord > 420)
                {
                    player.Entropy().damageRecord = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int i = Item.NewItem(player.GetSource_FromThis(), player.getRect(), new Item(ModContent.ItemType<PoopPickup>()), false, true);
                        Main.item[i].noGrabDelay = 100;
                        if (!Main.dedServ)
                        {
                            CEUtils.PlaySound("fart", 1, player.Center);
                        }
                    }
                    else
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)CEMessageType.SpawnItem);
                        packet.Write(player.whoAmI);
                        packet.Write(ModContent.ItemType<PoopPickup>());
                        packet.Write(1);
                        packet.Send();
                    }
                }
            }
            onHurt(npc, damageDone, sourcePlr, projectile, hit);
        }
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            /*if(source is EntitySource_Parent esource)
            {
                if(esource.Entity is NPC np)
                {
                    ToFriendly = np.Entropy().ToFriendly;
                    if (ToFriendly)
                    {
                        f_owner = np.Entropy().f_owner;
                    }
                }
                if (esource.Entity is Projectile pj)
                {
                    ToFriendly = pj.Entropy().ToFriendly;
                }
                if (ToFriendly)
                {
                    npc.friendly = true;
                }
            }*/
        }


        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.Clothier)
            {
                shop.Add(ModContent.ItemType<Barren>());
            }
            if (shop.NpcType == 17)
            {
                shop.Add(ModContent.ItemType<SoyMilk>(), new Condition(Mod.GetLocalization("DownedBoss2").Value, () => NPC.downedBoss2));
                shop.Add(ModContent.ItemType<BrillianceCard>());
            }
            if (shop.NpcType == 108)
            {
                // 命运之绳与大法师手镜原挂灾厄大法师货架,脱钩时随该 NPC 一并删除,现重挂到原版巫师
                shop.Add(ModContent.ItemType<ThreadOfFate>());
                shop.Add(ModContent.ItemType<ArchmagesHandmirror>(), Condition.DownedMoonLord);

                shop.Add(ModContent.ItemType<AuraCard>(), new Condition(Mod.GetLocalization("HaveOracleDeck"), () => Main.LocalPlayer.Entropy().oracleDeckInInv));
                shop.Add(ModContent.ItemType<BrillianceCard>(), new Condition(Mod.GetLocalization("HaveOracleDeck"), () => Main.LocalPlayer.Entropy().oracleDeckInInv));
                shop.Add(ModContent.ItemType<InspirationCard>(), new Condition(Mod.GetLocalization("HaveOracleDeck"), () => Main.LocalPlayer.Entropy().oracleDeckInInv));
                shop.Add(ModContent.ItemType<TemperanceCard>(), new Condition(Mod.GetLocalization("HaveOracleDeck"), () => Main.LocalPlayer.Entropy().oracleDeckInInv));
                shop.Add(ModContent.ItemType<WisdomCard>(), new Condition(Mod.GetLocalization("HaveOracleDeck"), () => Main.LocalPlayer.Entropy().oracleDeckInInv));

                shop.Add(ModContent.ItemType<Confuse>(), new Condition(Mod.GetLocalization("HaveTaintedDeck"), () => Main.LocalPlayer.Entropy().taintedDeckInInv));
                shop.Add(ModContent.ItemType<Perplexed>(), new Condition(Mod.GetLocalization("HaveTaintedDeck"), () => Main.LocalPlayer.Entropy().taintedDeckInInv));

                AddSoulCard<BitternessCard>(shop);
                AddSoulCard<DevouringCard>(shop);
                AddSoulCard<GrudgeCard>(shop);
                AddSoulCard<IndigoCard>(shop);
                AddSoulCard<MourningCard>(shop);
                AddSoulCard<ObscureCard>(shop);
                AddSoulCard<PurificationCard>(shop);
                AddSoulCard<RequiemCard>(shop);
            }
            if (shop.NpcType == 20)
            {
                shop.Add(ModContent.ItemType<Confuse>());
            }
        }
        public static void AddSoulCard<T>(NPCShop shop) where T : ModItem
        {
            shop.Add(ModContent.ItemType<T>(), new Condition(CalamityEntropy.Instance.GetLocalization("HaveSoulDeck"), () => Main.LocalPlayer.Entropy().soulDeckInInv));
        }
    }
}
