using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Core.Graphics;
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
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 360);
        }
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.ImmuneToRegularBuffs[NPC.type] = true;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.48f,
                PortraitScale = 0.56f,
                CustomTexturePath = null,
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = 0
            };
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
            NPC.width = 110;
            NPC.height = 110;
            NPC.damage = 100;
            if (Main.expertMode)
            {
                NPC.damage += 2;
            }
            if (Main.masterMode)
            {
                NPC.damage += 2;
            }
            NPC.defense = 50;
            NPC.lifeMax = 360000;
            //装灾厄读死亡/复仇,缺席仍走大师/专家兜底
            if (CECal.IsDeathMode)
            {
                NPC.damage += 8;
            }
            else if (CECal.IsRevengeance)
            {
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
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1f - DamageReduction;
        }
        public bool init = true;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.realLife);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.realLife = reader.ReadInt32();
        }

        public List<CCTentacle> tentacles;
        public override void AI()
        {
            if (tentacles == null)
            {
                int c = 0;
                tentacles = new List<CCTentacle>();
                for (float i = 0; i < 358; i += 45f)
                {
                    c++;
                    tentacles.Add(new CCTentacle(MathHelper.ToRadians(i), c % 2 == 0 ? 94 : 78));
                }
            }
            foreach (var t in tentacles)
            {
                t.Update(NPC);
            }
            if (NPC.ai[2] > 0)
            {
                if (al < 1)
                {
                    al += 0.02f;
                }
                NPC.ai[2]--;
            }
            else
            {
                if (al > 0)
                {
                    al -= 0.02f;
                }
            }
            NPC.netUpdate = true;
            NPC.velocity *= 0.965f;
            if (NPC.realLife < 0)
            {
                return;
            }
            NPC.rotation += NPC.velocity.X * 0.006f;
            if (init)
            {
                init = false;
            }
            if (Main.GameUpdateCount % 5 == 0)
            {
                frame++;
                if (frame > 4)
                {
                    frame = 1;
                }
            }
            if (owner != null && (!owner.active || owner.life <= 0))
            {
                NPC.realLife = -1;
                NPC.StrikeInstantKill();
            }
        }
        public NPC owner { get { return NPC.realLife.ToNPC(); } }

        public override bool CheckActive()
        {
            if (NPC.realLife < 0)
            {
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
            public void Update(NPC npc)
            {
                pointRots[0] = npc.rotation + rot + (float)(Math.Cos(npc.localAI[2]++ * 0.008f) * 0.6f);
                points[0] = npc.Center + npc.velocity + (npc.rotation + rot).ToRotationVector2() * 30 * (npc.IsABestiaryIconDummy ? 0.5f : 1);

                for (int i = 1; i < points.Count; i++)
                {
                    pointRots[i] = (points[i] - points[i - 1]).ToRotation();
                    points[i] = points[i - 1] + (points[i] - points[i - 1]).normalize() * Length / 8f * (npc.IsABestiaryIconDummy ? 0.5f : 1);
                    pointRots[i] = CEUtils.RotateTowardsAngle(pointRots[i], pointRots[i - 1], 0.4f, false);
                    if (CEUtils.GetAngleBetweenVectors(pointRots[i - 1].ToRotationVector2(), pointRots[i].ToRotationVector2()) > 0.2f)
                    {
                        pointRots[i] = CEUtils.RotateTowardsAngle(pointRots[i], pointRots[i - 1], CEUtils.GetAngleBetweenVectors(pointRots[i - 1].ToRotationVector2(), pointRots[i].ToRotationVector2()) - 0.2f);
                    }
                    points[i] = points[i - 1] + pointRots[i].ToRotationVector2() * Length / 8f * (npc.IsABestiaryIconDummy ? 0.5f : 1);
                }
            }

            public CCTentacle(float r, float l)
            {
                rot = r;
                Length = l;
                pointRots = new List<float>();
                points = new List<Vector2>();
                for (int i = 0; i < 12; i++)
                {
                    points.Add(Vector2.Zero);
                    pointRots.Add(0);
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (tentacles == null)
            {
                int c = 0;
                tentacles = new List<CCTentacle>();
                for (float i = 0; i < 358; i += 45f)
                {
                    c++;
                    tentacles.Add(new CCTentacle(MathHelper.ToRadians(i), c % 2 == 0 ? 94 : 78));
                }
            }
            Texture2D tex = NPC.getTexture();

            if (NPC.IsABestiaryIconDummy)
            {
                foreach (var t in tentacles)
                {
                    t.Update(NPC);
                }
                if (tentacles != null)
                {
                    foreach (var tent in tentacles)
                    {
                        List<ColoredVertex> ve = new List<ColoredVertex>();
                        Color b = Color.White;
                        List<Vector2> points = tent.points;
                        float lc = 1;
                        float jn = 0;

                        for (int i = 1; i < points.Count; i++)
                        {
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
                        if (ve.Count >= 3)
                        {
                            gd.Textures[0] = CEUtils.RequestTex($"CalamityEntropy/Content/NPCs/NihilityTwin/H{(tent.Length > 80 ? "1" : "2")}");
                            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                        }
                    }
                }

                spriteBatch.Draw(tex, NPC.Center - screenPos, null, Color.White, NPC.rotation, tex.Size() / 2, 0.5f, SpriteEffects.None, 0);

                return false;
            }
            if (NPC.realLife >= 0)
            {
                if (owner.ModNPC is NihilityActeriophage na)
                {
                    if (na.spawnAnm > 0)
                    {
                        return false;
                    }
                    na.drawRope();
                }
                if (Main.zenithWorld)
                {
                    foreach (NPC n in Main.ActiveNPCs)
                    {
                        if (n.type == owner.type && n.whoAmI != owner.whoAmI)
                        {
                            if (n.ModNPC is NihilityActeriophage na2)
                            {
                                if (na2.spawnAnm > 0)
                                {
                                    return false;
                                }
                                na2.drawRope();
                            }
                        }
                    }
                }
            }
            else
            {
                return false;
            }
            Color color = Color.White;



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (al > 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (tentacles != null)
                        {
                            foreach (var tent in tentacles)
                            {
                                List<ColoredVertex> ve = new List<ColoredVertex>();
                                Color b = Color.White * al;
                                List<Vector2> points = tent.points;
                                float lc = 1;
                                float jn = 0;

                                for (int ij = 1; ij < points.Count; ij++)
                                {
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
                                if (ve.Count >= 3)
                                {
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

            if (tentacles != null)
            {
                foreach (var tent in tentacles)
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    Color b = Color.White;
                    List<Vector2> points = tent.points;
                    float lc = 1;
                    float jn = 0;

                    for (int i = 1; i < points.Count; i++)
                    {
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
                    if (ve.Count >= 3)
                    {
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
