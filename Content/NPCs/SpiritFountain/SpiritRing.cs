using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.SpiritFountainShoots;
using CalamityEntropy.Core.AI;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SpiritFountain
{
    /// <summary>
    /// 魂环。本体的伴生部件,血量经 <c>realLife</c> 转发给本体——本体常年免伤,
    /// 玩家真正能打的就是这一圈环。
    /// <para>
    /// 它是<b>锚定部件</b>:大部分时间位置由本体直写(<c>Center = 本体 + 柱偏移 + 柱方向 × 自身偏移</c>),
    /// 只有回旋段脱柱与落环喷泉段抛飞时才做速度积分,而且两段都会收敛回柱子上。
    /// 所以只清原版平滑(<see cref="CEBossHost.RunAnchoredPartFrame"/>),
    /// <b>绝不</b>进 <see cref="CEBossNetMotion"/> 的预测纠偏器:预测器算的是 position + velocity,
    /// 会和直写位置打架。
    /// </para>
    /// <para>
    /// 联机:骰点(脱柱目标偏移、抛飞初速)一律只在权威端骰,结果随 ExtraAI 或原版速度同步过线;
    /// 运动数学各端都跑。
    /// </para>
    /// </summary>
    public class SpiritRing : ModNPC
    {
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            // 图鉴隐藏:原灾厄隐藏扩展的原版等价写法
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            //位置多数帧由本体直写,原版平滑对它纯属噪声,还会让环和柱子错位
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetDefaults() {
            NPC.width = 160;
            NPC.height = 46;
            NPC.damage = 200;
            NPC.dontCountMe = true;
            NPC.lifeMax = 80000;
            NPC.HitSound = SoundID.NPCHit11;
            NPC.DeathSound = SoundID.NPCDeath11;
            NPC.value = 0f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.noGravity = true;
        }

        public NPC owner => ((int)(NPC.ai[0])).ToNPC();
        public SpiritFountain fountain => (SpiritFountain)(owner.ModNPC);
        public float Index => NPC.ai[1];
        public FountainColumn column => NPC.ai[2] == 0 ? fountain.column1 : fountain.column2;
        public float TrailLength = 60;
        private bool flag = true;
        /// <summary>沿柱方向的自身偏移。初值各端各骰一次,随即由生成包里的 ExtraAI 对齐</summary>
        public float columnOffset = Main.rand.NextFloat(-1200, 1200);
        public bool Lerping = false;
        public float LFrom = 0;
        public float LTo = 0;
        public float LProgress = 0;
        public void LerpTo(float offset) {
            Lerping = true;
            LFrom = columnOffset;
            LTo = offset;
            LProgress = 0;
        }

        /// <summary>
        /// 定长块,顺序固定在这一处。
        /// 除了偏移本身,脱柱插值的四个量与回旋飞行的三个闸也必须过线:
        /// 它们全是骰出来或一次性锁存的,一旦分叉就靠自身收敛不回来
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(columnOffset);
            writer.Write(Lerping);
            writer.Write(LFrom);
            writer.Write(LTo);
            writer.Write(LProgress);
            writer.Write(OnColumn);
            writer.Write(BMRCd);
            writer.Write(NPC.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            columnOffset = reader.ReadSingle();
            Lerping = reader.ReadBoolean();
            LFrom = reader.ReadSingle();
            LTo = reader.ReadSingle();
            LProgress = reader.ReadSingle();
            OnColumn = reader.ReadBoolean();
            BMRCd = reader.ReadBoolean();
            NPC.localAI[1] = reader.ReadSingle();
        }

        public bool OnColumn = true;
        public bool BMRCd = false;

        /// <summary>权威端(服务端或单机)。骰点、生成只在这里做;运动数学各端都要跑</summary>
        private static bool IsServer => Main.netMode != NetmodeID.MultiplayerClient;

        public override void AI() {
            //锚定部件:只清原版平滑,不进预测纠偏器
            CEBossHost.RunAnchoredPartFrame(NPC);

            if (flag) {
                flag = false;
                NPC.Opacity = 0;
            }
            if (NPC.Opacity < 1) {
                NPC.Opacity += 0.05f;
            }


            if (!owner.active || owner.ModNPC is not SpiritFountain) {
                //部件消失是世界写入:只在权威端做并发包。客户端自己抹掉就再也回不来了
                if (IsServer) {
                    NPC.active = false;
                    if (Main.netMode == NetmodeID.Server) {
                        NPC.netUpdate = true;
                        NPC.netSpam = 0;
                    }
                }
                return;
            }

            NPC.scale = owner.scale;
            NPC.Entropy().VoidTouchDR = owner.Entropy().VoidTouchDR;
            NPC.defense = owner.defense;
            // 原灾厄 DR 镜像删除:本体(灵泉)从未设置灾厄 DR,该行恒为 0 的空转
            NPC.dontTakeDamage = fountain.DontTakeDmg;
            NPC.realLife = (int)NPC.ai[0];
            NPC.lifeMax = owner.lifeMax;
            NPC.life = owner.life;
            NPC.damage = owner.damage;
            NPC.width = (int)float.Lerp(60, 160, CEUtils.GetRepeatedCosFromZeroToOne(Math.Abs(NPC.rotation.ToRotationVector2().X), 1));
            NPC.height = (int)float.Lerp(60, 160, CEUtils.GetRepeatedCosFromZeroToOne(Math.Abs(NPC.rotation.ToRotationVector2().Y), 1));

            bool DontSetPos = false;
            bool DontSetRot = false;
            //原代码写的是 == 1。fountain.aiTimer 现在是过线并带 ±2 容差收养的状态计时,
            //客户端可能一步跨过第 1 帧;而落环喷泉整段没有第二处清这个预警透明度,
            //跨过去就会拿着上一招的激光预警光束画满 280 帧。
            //这是一次幂等的清零,提前到换态帧(aiTimer == 0)再清一次不改变任何后续取值,
            //代价只是预警光束早一帧熄灭,所以改成区间判定
            if (fountain.aiTimer <= 1) {
                AlphaLaserWarning = 0;
            }
            if (fountain.ClearMyProjs > 0) {
                SRHandle = 5;
            }
            if (Lerping) {
                LProgress += 0.02f;
                columnOffset = float.Lerp(LFrom, LTo, CEUtils.GetRepeatedCosFromZeroToOne(LProgress, 1));
                if (LProgress >= 1) {
                    Lerping = false;
                    LProgress = 0;
                    columnOffset = LTo;
                }
            }
            if (SRHandle-- < 0) {
                if (IsServer) {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<SRDamageRect>(), NPC.damage / 6, 2, -1, NPC.whoAmI);
                }
            }
            drawColorLerp = Color.AliceBlue;

            #region Phase1
            if (fountain.ai == SpiritFountainStateIndex.Moving) {
                float targetofs = Index * 1900 + (float)Math.Sin(fountain.Counter * 0.02f) * 400;
                columnOffset = float.Lerp(columnOffset, targetofs, 0.05f);
                TrailLength = float.Lerp(TrailLength, 68, 0.05f);
            }
            Player target = owner.HasValidTarget ? owner.target.ToPlayer() : Main.player[0];

            if (fountain.ai == SpiritFountainStateIndex.Boomerang) {
                if (fountain.aiTimer == 10 && IsServer) {
                    //脱柱目标偏移只在权威端骰,结果随 ExtraAI 过线;客户端拿到之后照常跑插值
                    LerpTo(Main.rand.NextFloat(-1800, 1800));
                    if (fountain.phase == 3) {
                        //三阶段改成按 Index 均匀铺开,直接盖掉上一行骰出来的值。照搬
                        LerpTo(Index * 1200);
                    }
                    NPC.netUpdate = true;
                }
                if (fountain.num1 > (fountain.phase == 3 ? Math.Abs(Index) : (Index + 1) / 2f)) {
                    if (OnColumn && !BMRCd) {
                        CEUtils.PlaySound("scholarStaffAttack", Main.rand.NextFloat(0.6f, 1.4f), NPC.Center, -1);
                        OnColumn = false;
                        BMRCd = true;
                        NPC.rotation = 0;
                        NPC.velocity = (owner.target.ToPlayer().Center - NPC.Center).normalize() * 28f;
                        NPC.velocity.Y = float.Clamp(NPC.velocity.Y, -4 * fountain.phase, 4 * fountain.phase);
                        NPC.velocity.X = Math.Sign(NPC.velocity.X) * (fountain.phase == 3 ? 24 : 34);
                        if (fountain.phase == 3) {
                            NPC.velocity.Y = 0;
                        }
                        //决策点:脱柱锁向。各端都算得出方向,但玩家位置有插值差,让权威端立刻对账
                        if (IsServer) {
                            NPC.netUpdate = true;
                        }
                    }
                    if (!OnColumn) {
                        TCounter += 0.2f * (NPC.whoAmI % 2 == 0 ? 1 : -1);
                        TrailLength = float.Lerp(TrailLength, 128, 0.1f);
                        if (NPC.localAI[1]++ > 35) {
                            if (fountain.phase == 3) {
                                NPC.damage = 0;
                            }
                            if (NPC.localAI[1] > 30 && NPC.localAI[1] <= 120 && fountain.phase == 3) {

                                NPC.velocity *= 0.2f;
                                if (NPC.localAI[1] == 120) {
                                    fountain.Shoot(ModContent.ProjectileType<SpiritLaser>(), NPC.Center, (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * 5);
                                }
                                if (NPC.localAI[1] < 90) {
                                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (target.Center - NPC.Center).ToRotation() + MathHelper.PiOver2, 0.008f);
                                    NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (target.Center - NPC.Center).ToRotation() + MathHelper.PiOver2, 0.044f, false);
                                    AlphaLaserWarning = float.Lerp(AlphaLaserWarning, 1, 0.04f);
                                }
                                else {
                                    AlphaLaserWarning = float.Lerp(AlphaLaserWarning, 0, 0.04f);
                                }
                            }
                            else {
                                AlphaLaserWarning = 0;
                                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, 0, 0.12f, false);
                            }
                            //随机初速只在权威端骰,结果随弹幕本体过线;客户端不空转 Main.rand。
                            //等值判定安全:权威端的 localAI[1] 从不被收养(ReceiveExtraAI 只在客户端跑),严格单调
                            if (NPC.localAI[1] == 66 && fountain.phase == 2 && IsServer) {
                                fountain.Shoot(ModContent.ProjectileType<SpiritBullet>(), NPC.Center, CEUtils.randomRot().ToRotationVector2() * 4, 1, owner.whoAmI, 2, 1);
                            }
                            NPC.velocity.Y *= 0.97f;
                            NPC.velocity.X += (owner.Center.X + column.offset.X - NPC.Center.X) > 0 ? 1.6f : -1.6f;
                            if (NPC.localAI[1] > 40) {
                                if (((owner.Center.X + column.offset.X - NPC.localAI[0]) > 0) != ((owner.Center.X + column.offset.X - (NPC.Center.X + NPC.velocity.X * 2)) > 0)) {
                                    OnColumn = true;
                                    NPC.velocity *= 0;
                                    columnOffset = -(NPC.Center.Y - (owner.Center.Y + column.offset.Y));
                                }
                            }
                            NPC.localAI[0] = NPC.Center.X;

                        }
                    }
                    else {
                        NPC.velocity *= 0;
                    }
                }
                else {
                    //原代码这条 else 里又判了一次同样的条件,恒为假,整块是死代码。照搬
                    if (fountain.num1 > (fountain.phase == 3 ? Math.Abs(Index) : (Index + 1) / 2f)) {
                        drawColorLerp = new Color(255, 40, 40);
                    }
                }
                if (fountain.aiTimer < 70) {
                    NPC.damage = 0;
                }
            }
            else {
                if (BMRCd) {
                    OnColumn = true;
                    NPC.velocity *= 0;

                    BMRCd = false;

                    NPC.localAI[1] = 0;
                }
            }
            if (fountain.ai == SpiritFountainStateIndex.Lasers) {
                DontSetRot = true;
                int targetTime = (int)(fountain.phase == 3 ? 82 : (fountain.phase == 2 ? 98 : 120) / fountain.enrage);
                if (fountain.aiTimer < 416 || Lerping || AlphaLaserWarning > 0) {
                    if (fountain.aiTimer % (targetTime + 28) == 0 && IsServer) {
                        //同上:换驻点的骰点只在权威端
                        LerpTo(Main.rand.NextFloat(-1200, 1200));
                        NPC.netUpdate = true;
                    }
                    if (fountain.aiTimer % (targetTime + 28) <= targetTime) {
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, (target.Center.X > NPC.Center.X ? MathHelper.PiOver2 : -MathHelper.PiOver2) + ((fountain.phase - 1) * (NPC.whoAmI * 1.73523f).ToRotationVector2().ToRotation() * 0.08f), 0.24f, false);
                    }
                    else {
                        NPC.rotation = (target.Center.X > NPC.Center.X ? MathHelper.PiOver2 : -MathHelper.PiOver2) + ((fountain.phase - 1) * (NPC.whoAmI * 1.73523f).ToRotationVector2().ToRotation() * 0.08f);
                    }
                    if (fountain.aiTimer % (targetTime + 28) <= targetTime - 12) {
                        AlphaLaserWarning = float.Lerp(AlphaLaserWarning, 1, 0.04f);
                    }
                    else {
                        AlphaLaserWarning = float.Lerp(AlphaLaserWarning, 0, 0.04f);
                    }
                    if (fountain.aiTimer % (targetTime + 28) == 1 + targetTime) {
                        AlphaLaserWarning = 0;
                        fountain.Shoot(ModContent.ProjectileType<SpiritLaser>(), NPC.Center, (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * 5);
                    }

                }
                else {
                    AlphaLaserWarning = 0;
                }
            }
            NPC.noTileCollide = true;

            if (fountain.ai == SpiritFountainStateIndex.RingFountains) {
                DontSetPos = true;
                DontSetRot = true;
                //扶正同样从 == 1 放宽成区间:SyncNPC 不带 NPC.rotation,而本段一路 DontSetRot,
                //客户端跨过第 1 帧就会顶着上一招的朝向抛飞(判定盒宽高也是按朝向算的)。
                //写的是常量 0,提前一帧到换态帧清一次等价
                if (fountain.aiTimer <= 1) {
                    NPC.rotation = 0;
                }
                //抛飞初速仍钉死在恰好第 1 帧:权威端计时不被收养,严格单调,骰一次就是一次
                if (fountain.aiTimer == 1 && IsServer) {
                    //只在权威端骰,原版 NPC 同步自带 velocity,立刻发包让客户端接上
                    NPC.velocity = new Vector2(Main.rand.NextFloat(-45, 45), -6);
                    NPC.netUpdate = true;
                }
                if (fountain.aiTimer > 1) {
                    if (NPC.velocity.Y == 0) {
                        NPC.velocity.Y *= 0;
                        NPC.rotation = 0;

                        if (fountain.aiTimer < 130) {
                            NPC.velocity *= 0.84f;
                            foreach (NPC n in Main.ActiveNPCs) {
                                if (n.type == NPC.type && n.whoAmI != NPC.whoAmI) {
                                    if (CEUtils.getDistance(NPC.Center, n.Center) < 340) {
                                        NPC.velocity.X += (NPC.Center - n.Center).normalize().X * 1f;
                                    }
                                }
                            }
                        }
                        else {
                            NPC.velocity *= 0;
                        }
                        if (fountain.aiTimer > 40) {
                            if (fountain.aiTimer < 150) {
                                AlphaWaveWarning = float.Lerp(AlphaWaveWarning, 0.8f, 0.02f);
                            }
                            else {
                                if (fountain.aiTimer == 150) {
                                    fountain.Shoot(ModContent.ProjectileType<SpiritWave>(), NPC.Center, Vector2.Zero);
                                }
                                if (fountain.aiTimer > 140) {
                                    AlphaWaveWarning = float.Lerp(AlphaWaveWarning, 0, 0.1f);
                                    NPC.velocity.X *= 0;
                                }
                                if (fountain.aiTimer > 220) {
                                    NPC.damage = 0;
                                    NPC.Center = Vector2.Lerp(NPC.Center, owner.Center + column.offset + column.rotation.ToRotationVector2() * columnOffset, 0.1f);
                                }
                            }
                        }
                    }
                    else {
                        NPC.width = NPC.height = 60;
                        NPC.rotation += NPC.velocity.X * 0.004f;
                        NPC.velocity.X *= 0.96f;
                        NPC.velocity.Y += 0.9f;
                        NPC.noTileCollide = false;
                        NPC.damage = 0;
                    }
                }
            }
            else {
                AlphaWaveWarning = 0;
            }
            #endregion

            #region phase2
            if (fountain.ai == SpiritFountainStateIndex.PhaseTranse1) {
                float targetofs = Index * 1900 + (float)Math.Sin(fountain.Counter * 0.02f) * 400;
                columnOffset = float.Lerp(columnOffset, targetofs, 0.05f);
                TrailLength = float.Lerp(TrailLength, 74, 0.05f);
                NPC.damage = 0;
            }
            if (fountain.ai == SpiritFountainStateIndex.SpiritSlicing) {
                TrailLength = float.Lerp(TrailLength, 100, 0.05f);
                int counter = fountain.aiTimer;
                int t = 90;
                if (counter % (t + 42) < t)
                    NPC.damage = 0;
                if (counter % (t + 42) == 1 && IsServer) {
                    //同上:换驻点的骰点只在权威端
                    LerpTo(Main.rand.NextFloat(-1200, 1200));
                    NPC.netUpdate = true;
                }
                //counter%51触发HadLine,跟LerpTo硬切同步,不是每帧都有
                if (counter % (t + 42) == 51 && !Main.dedServ) {
                    //HadLine成对spawn(hm=0.36)旧SpiritRing双轨残影,column.id决定offset朝向
                    Vector2 offset = column.id == 0 ? new Vector2(column.Num < 0 ? 1 : -1, 0) : new Vector2(0, column.Num < 0 ? 1 : -1);
                    PRTLoader.NewParticle<PRT_HadLine>(NPC.Center + offset * 80, Vector2.Zero, Color.LightBlue, 3f)
                        .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, offset.ToRotation(), 32).hm = 0.36f;
                    //lifetime 32短帧,ShouldUpdatePosition=false位移全靠rotation
                    PRTLoader.NewParticle<PRT_HadLine>(NPC.Center + offset * 80, Vector2.Zero, Color.LightBlue, 3f)
                        .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, offset.ToRotation(), 32).hm = 0.36f;

                }
            }


            #endregion
            if (OnColumn) {
                if (!DontSetPos)
                    NPC.Center = owner.Center + column.offset + column.rotation.ToRotationVector2() * columnOffset;
                if (!DontSetRot)
                    NPC.rotation = column.rotation + MathHelper.PiOver2;
            }
            color = Color.Lerp(color, drawColorLerp, 0.1f);
            TCounter += 0.16f * (NPC.whoAmI % 2 == 0 ? 1 : -1);

        }
        public Color drawColorLerp = Color.AliceBlue;
        public Color color = Color.AliceBlue;
        public float TCounter = Main.rand.NextFloat() * 3.14159f;
        public void DrawTrail(float r, float rot) {
            float rt = rot;
            Texture2D glow = CEExtraAssets.Glow2;
            var sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (float i = 0; i < (int)TrailLength; i++) {
                float s = (1 + i) / TrailLength;
                s = 0.32f + 0.68f * s;
                var ofs = rt.ToRotationVector2() * r;
                ofs.Y *= 0.25f;
                ofs = ofs.RotatedBy(NPC.rotation);
                sb.Draw(glow, NPC.Center + ofs - Main.screenPosition, null, color * NPC.Opacity * 0.7f * (NPC.damage > 0 ? 1 : 0.5f), 0, glow.Size() * 0.5f, s * 0.2f, SpriteEffects.None, 0);
                rt += MathHelper.ToRadians(4) * (NPC.whoAmI % 2 == 0 ? 1 : -1);
            }
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        }
        public float AlphaLaserWarning = 0;
        public float AlphaWaveWarning = 0;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            yx += 0.036f;
            if (AlphaWaveWarning > 0.01f) {
                Texture2D glow = CEExtraAssets.a_circle;
                Texture2D tex = CEExtraAssets.MegaStreakBacking2;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(glow, NPC.Center - Main.screenPosition, null, Color.White * AlphaWaveWarning, NPC.rotation, glow.Size() / 2f, new Vector2(3, 1.2f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, NPC.Center - Main.screenPosition, null, Color.White * AlphaWaveWarning * 0.8f, NPC.rotation - MathHelper.PiOver2, new Vector2(0, tex.Height / 2f), new Vector2(12f, 0.6f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, NPC.Center - Main.screenPosition, null, Color.White * AlphaWaveWarning * 0.8f, NPC.rotation - MathHelper.PiOver2, new Vector2(0, tex.Height / 2f), new Vector2(12f, 0.6f), SpriteEffects.None, 0);
            }
            if (AlphaLaserWarning > 0.01f) {
                List<Vector2> points = new();
                for (float i = 0; i <= 1; i += 0.005f) {
                    points.Add(Vector2.Lerp(NPC.Center, NPC.Center + (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * 3600, i));
                }
                Texture2D tx = CEUtils.pixelTex;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                {
                    List<ColoredVertex> ve = new List<ColoredVertex>();

                    float w = 12;
                    float p = -Main.GlobalTimeWrappedHourly * 2;
                    for (int i = 1; i < points.Count; i++) {
                        float wd = (0.9f + 0.12f * (float)Math.Cos(i * 0.6f + Main.GlobalTimeWrappedHourly * 10)) * AlphaLaserWarning;
                        Color b = new Color(200, 200, 255) * wd;
                        ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * NPC.scale * w * wd,
                              new Vector3((float)i / points.Count, 1, 1),
                              b));
                        ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * NPC.scale * w * wd,
                              new Vector3((float)i / points.Count, 0, 1),
                              b));
                    }

                    SpriteBatch sb = Main.spriteBatch;
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3) {
                        gd.Textures[0] = tx;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
            }
            DrawTrail(90, TCounter);

            return false;
        }
        public float yx = 0;
        public override bool CheckActive() {
            if (owner.active) {
                return false;
            }
            return true;
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) {
            return false;
        }
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            return false;
        }
        public override bool? CanFallThroughPlatforms() {
            return true;
        }
        public int SRHandle = 3;
    }

    public class SRDamageRect : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.scale = 1f;
            Projectile.timeLeft = 4;
        }
        public override void AI() {
            var o = ((int)(Projectile.ai[0])).ToNPC();
            Projectile.damage = o.damage / 6;
            Projectile.Center = (o.Center);
            if (o.active && o.ModNPC is SpiritRing sr) {
                Projectile.timeLeft = 5;
                Projectile.rotation = o.rotation;
                sr.SRHandle = 3;
                if (sr.fountain.ClearMyProjs > 0) {
                    Projectile.Kill();
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center - Projectile.rotation.ToRotationVector2() * 80 * Projectile.scale, Projectile.Center + Projectile.rotation.ToRotationVector2() * 80 * Projectile.scale, targetHitbox, 36);
        }

        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }
}
