using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Core.AI;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    [AutoloadBossHead]
    public class ChaoticCell : ModNPC
    {
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 360);
        }
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.ImmuneToRegularBuffs[NPC.type] = true;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = null,
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            //细胞炮那一手会把它甩到 60 px/f,原版 netOffset 在这个速度下只会锯齿;关掉后由纠偏器接管
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.NihilityTwinBestiary")
            });
        }
        public override void SetDefaults() {
            NPC.boss = true;
            NPC.width = 110;
            NPC.height = 110;
            NPC.damage = 100;
            if (Main.expertMode) {
                NPC.damage += 2;
            }
            if (Main.masterMode) {
                NPC.damage += 2;
            }
            NPC.defense = 50;
            NPC.lifeMax = 360000;
            //装灾厄读死亡/复仇,缺席仍走大师/专家兜底
            if (CECal.IsDeathMode) {
                NPC.damage += 8;
            }
            else if (CECal.IsRevengeance) {
                NPC.damage += 4;
            }
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCHit1;
            NPC.value = Item.buyPrice(1, 2, 60, 0);
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.5f;
            NPC.dontCountMe = true;
            NPC.netAlways = true;
            SpawnModBiomes = new int[] { ModContent.GetInstance<VoidDummyBoime>().Type };
        }
        // 原灾厄全局 DR=0.20 的本地等效;公有字段供血条等外部读取
        public float DamageReduction = 0.20f;
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 1f - DamageReduction;
        }
        public bool init = true;

        /// <summary>
        /// 客户端位置纠偏。
        /// <para>
        /// 归类依据:本体<b>不</b>直接写细胞的 <c>Center</c>(除蓄力焊接与对撞对齐那两处瞬移),
        /// 而是逐帧写 <c>velocity</c>,下一帧位置就是标准的 <c>position + velocity</c>——
        /// 正是 <see cref="CEBossNetMotion"/> 预测模型成立的前提,所以它走「本体型」通路
        /// (<c>BeginFrame</c> / <c>EndFrame</c>),不是只清平滑的锚定部件通路。
        /// 那两处瞬移由本体调 <see cref="ForgetPrediction"/> 主动作废旧预测
        /// </para>
        /// </summary>
        private readonly CEBossNetMotion netMotion = new();

        /// <summary>本体直写细胞位置时调用:丢掉旧预测,下一包不当失步处理</summary>
        public void ForgetPrediction() {
            netMotion.ForgetPrediction();
        }

        /// <summary>
        /// 定长块:宿主索引 + 朝向。朝向是逐帧按速度积分出来的累加量(<c>rotation += velocity.X * 0.006</c>),
        /// 原代码没同步它,靠每帧 netUpdate 的位置包掩盖;现在包率降到决策点 + 心跳,必须自己过线
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(NPC.realLife);
            writer.Write(NPC.rotation);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            NPC.realLife = reader.ReadInt32();
            NPC.rotation = reader.ReadSingle();
            netMotion.OnSnapshot(NPC, 0);
        }

        public List<CCTentacle> tentacles;
        /// <summary>
        /// 细胞自己只做两件事:触须骨架(纯绘制)与逐帧阻尼。攻击与航向全由本体每帧写进 <c>velocity</c>,
        /// 各端跑的是同一份本体状态机,所以这里不需要任何权威端分支
        /// </summary>
        public override void AI() {
            bool client = VaultUtils.isClient;
            if (client) {
                netMotion.BeginFrame(NPC);
            }
            if (tentacles == null) {
                int c = 0;
                tentacles = new List<CCTentacle>();
                for (float i = 0; i < 358; i += 45f) {
                    c++;
                    tentacles.Add(new CCTentacle(MathHelper.ToRadians(i), c % 2 == 0 ? 94 : 78));
                }
            }
            foreach (var t in tentacles) {
                t.Update(NPC);
            }
            if (NPC.ai[2] > 0) {
                if (al < 1) {
                    al += 0.02f;
                }
                NPC.ai[2]--;
            }
            else {
                if (al > 0) {
                    al -= 0.02f;
                }
            }
            //原代码在这里每帧 netUpdate = true。位置是本体驱动的确定性积分,不需要靠包率维持,
            //改成权威端的 45 帧兜底心跳,决策点由本体那边自己打
            NPC.velocity *= 0.965f;
            if (NPC.realLife < 0) {
                EndNetFrame(client);
                return;
            }
            NPC.rotation += NPC.velocity.X * 0.006f;
            if (init) {
                init = false;
            }
            if (Main.GameUpdateCount % 5 == 0) {
                frame++;
                if (frame > 4) {
                    frame = 1;
                }
            }
            if (owner != null && (!owner.active || owner.life <= 0)) {
                NPC.realLife = -1;
                //原代码在各端都直接自杀;血量改动收归权威端,客户端等 active 同步过来
                if (!VaultUtils.isClient) {
                    NPC.StrikeInstantKill();
                }
            }
            EndNetFrame(client);
        }

        private void EndNetFrame(bool client) {
            if (client) {
                netMotion.EndFrame(NPC);
            }
            else {
                CEBossHost.Heartbeat(NPC);
            }
        }
        public NPC owner { get { return NPC.realLife.ToNPC(); } }

        public override bool CheckActive() {
            if (NPC.realLife < 0) {
                return true;
            }
            return !owner.active;
        }
        public int frame = 1;
        public float al = 0;
        public class CCTentacle
        {
            public float rot;
            public List<Vector2> points;
            public List<float> pointRots;
            public float Length;
            public void Update(NPC npc) {
                pointRots[0] = npc.rotation + rot + (float)(Math.Cos(npc.localAI[2]++ * 0.008f) * 0.6f);
                points[0] = npc.Center + npc.velocity + (npc.rotation + rot).ToRotationVector2() * 30 * (npc.IsABestiaryIconDummy ? 0.5f : 1);

                for (int i = 1; i < points.Count; i++) {
                    pointRots[i] = (points[i] - points[i - 1]).ToRotation();
                    points[i] = points[i - 1] + (points[i] - points[i - 1]).normalize() * Length / 8f * (npc.IsABestiaryIconDummy ? 0.5f : 1);
                    pointRots[i] = CEUtils.RotateTowardsAngle(pointRots[i], pointRots[i - 1], 0.4f, false);
                    if (CEUtils.GetAngleBetweenVectors(pointRots[i - 1].ToRotationVector2(), pointRots[i].ToRotationVector2()) > 0.2f) {
                        pointRots[i] = CEUtils.RotateTowardsAngle(pointRots[i], pointRots[i - 1], CEUtils.GetAngleBetweenVectors(pointRots[i - 1].ToRotationVector2(), pointRots[i].ToRotationVector2()) - 0.2f);
                    }
                    points[i] = points[i - 1] + pointRots[i].ToRotationVector2() * Length / 8f * (npc.IsABestiaryIconDummy ? 0.5f : 1);
                }
            }

            public CCTentacle(float r, float l) {
                rot = r;
                Length = l;
                pointRots = new List<float>();
                points = new List<Vector2>();
                for (int i = 0; i < 12; i++) {
                    points.Add(Vector2.Zero);
                    pointRots.Add(0);
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (tentacles == null) {
                int c = 0;
                tentacles = new List<CCTentacle>();
                for (float i = 0; i < 358; i += 45f) {
                    c++;
                    tentacles.Add(new CCTentacle(MathHelper.ToRadians(i), c % 2 == 0 ? 94 : 78));
                }
            }
            Texture2D tex = NPC.getTexture();

            if (NPC.IsABestiaryIconDummy) {
                foreach (var t in tentacles) {
                    t.Update(NPC);
                }
                if (tentacles != null) {
                    foreach (var tent in tentacles) {
                        List<ColoredVertex> ve = new List<ColoredVertex>();
                        Color b = Color.White;
                        List<Vector2> points = tent.points;
                        float lc = 1;
                        float jn = 0;

                        for (int i = 1; i < points.Count; i++) {
                            jn = (float)(i - 1) / (points.Count - 2);
                            ve.Add(new ColoredVertex(points[i] - screenPos + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 6 * lc,
                                  new Vector3(jn, 1, 1),
                                  b));
                            ve.Add(new ColoredVertex(points[i] - screenPos + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 6 * lc,
                                  new Vector3(jn, 0, 1),
                                  b));
                        }

                        SpriteBatch sb = Main.spriteBatch;
                        GraphicsDevice gd = Main.graphics.GraphicsDevice;
                        if (ve.Count >= 3) {
                            gd.Textures[0] = CEUtils.RequestTex($"CalamityEntropy/Content/NPCs/NihilityTwin/H{(tent.Length > 80 ? "1" : "2")}");
                            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                        }
                    }
                }

                spriteBatch.Draw(tex, NPC.Center - screenPos, null, Color.White, NPC.rotation, tex.Size() / 2, 0.5f, SpriteEffects.None, 0);

                return false;
            }
            if (NPC.realLife >= 0) {
                if (owner.ModNPC is NihilityActeriophage na) {
                    if (na.spawnAnm > 0) {
                        return false;
                    }
                    na.drawRope();
                }
                if (Main.zenithWorld) {
                    foreach (NPC n in Main.ActiveNPCs) {
                        if (n.type == owner.type && n.whoAmI != owner.whoAmI) {
                            if (n.ModNPC is NihilityActeriophage na2) {
                                if (na2.spawnAnm > 0) {
                                    return false;
                                }
                                na2.drawRope();
                            }
                        }
                    }
                }
            }
            else {
                return false;
            }
            Color color = Color.White;



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (al > 0) {
                for (int i = 0; i < 8; i++) {
                    for (int j = 0; j < 3; j++) {
                        if (tentacles != null) {
                            foreach (var tent in tentacles) {
                                List<ColoredVertex> ve = new List<ColoredVertex>();
                                Color b = Color.White * al;
                                List<Vector2> points = tent.points;
                                float lc = 1;
                                float jn = 0;

                                for (int ij = 1; ij < points.Count; ij++) {
                                    jn = (float)(ij - 1) / (points.Count - 2);
                                    ve.Add(new ColoredVertex(points[ij] - Main.screenPosition + MathHelper.ToRadians(i * (360f / 8f)).ToRotationVector2() * 4 + (points[ij] - points[ij - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 12 * lc,
                                          new Vector3(jn, 1, 1),
                                          b));
                                    ve.Add(new ColoredVertex(points[ij] - Main.screenPosition + MathHelper.ToRadians(i * (360f / 8f)).ToRotationVector2() * 4 + (points[ij] - points[ij - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 12 * lc,
                                          new Vector3(jn, 0, 1),
                                          b));
                                }

                                SpriteBatch sb = Main.spriteBatch;
                                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                                if (ve.Count >= 3) {
                                    gd.Textures[0] = CEUtils.RequestTex($"CalamityEntropy/Content/NPCs/NihilityTwin/H{(tent.Length > 80 ? "1" : "2")}");
                                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                                }
                            }
                        }
                        Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition + MathHelper.ToRadians(i * (360f / 8f)).ToRotationVector2() * 4, null, Color.White * al, NPC.rotation, tex.Size() / 2, NPC.scale, SpriteEffects.None);
                    }
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (tentacles != null) {
                foreach (var tent in tentacles) {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    Color b = Color.White;
                    List<Vector2> points = tent.points;
                    float lc = 1;
                    float jn = 0;

                    for (int i = 1; i < points.Count; i++) {
                        jn = (float)(i - 1) / (points.Count - 2);
                        ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 12 * lc,
                              new Vector3(jn, 1, 1),
                              b));
                        ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 12 * lc,
                              new Vector3(jn, 0, 1),
                              b));
                    }

                    SpriteBatch sb = Main.spriteBatch;
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3) {
                        gd.Textures[0] = CEUtils.RequestTex($"CalamityEntropy/Content/NPCs/NihilityTwin/H{(tent.Length > 80 ? "1" : "2")}");
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
            }
            Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition, null, color, NPC.rotation, tex.Size() / 2, NPC.scale, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

    }
}
