using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Core.AI;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.Rigs2D.Data;
using InnoVault.Rigs2D.Runtime;
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

        //==================== 触须骨架(Rigs2D,定义在 Assets/Rigs/ChaoticCell.rig.json) ====================
        //八条 11 节 ChainFollow 触须带:根点在细胞中心外 30 像素、按 45° 步进,根向带一个慢余弦摆头;
        //每节按「与前一节的相对角 ≤ 0.2、朝向以 0.4 收敘」逐节铺开,与迁移前的 CCTentacle 逐帧数学等价。纯绘制

        private Rig2DInstance rig;
        [Rig2DBone("tRoot{0}", Count = TentacleCount)]
        private readonly int[] tentacleRoots = new int[TentacleCount];
        [Rig2DRibbon("tent{0}", Count = TentacleCount)]
        private readonly int[] tentacleRibbons = new int[TentacleCount];
        [Rig2DRibbon("glow{0}", Count = TentacleCount)]
        private readonly int[] glowRibbons = new int[TentacleCount];
        [Rig2DPiece("body")]
        private int bodyPiece;

        /// <summary>触须条数,与 rig.json 里的 repeat 8 对齐</summary>
        private const int TentacleCount = 8;
        /// <summary>根向摆头:原代码每条触须各自把 <c>localAI[2]</c> 加一,八条共享一个计数器,所以相位每帧走 8 × 0.008</summary>
        private const float TentacleWobbleSpeed = 0.008f * TentacleCount;
        private const float TentacleWobbleAmp = 0.6f;

        private bool TentacleRigReady => rig != null && rig.Bound && rig.Built;

        private void EnsureTentacleRig() {
            if (rig != null) {
                return;
            }
            Vault2DRig asset = CERigAssets.ChaoticCell;
            if (asset == null || !asset.IsValid) {
                return;
            }
            rig = asset.CreateInstance(NPC.whoAmI);
            rig.Bind(this, null);
            //图鉴里的假体不在世界里,别让调试叠层去画它
            rig.DebugVisible = !NPC.IsABestiaryIconDummy;
        }

        /// <summary>
        /// 触须骨架落地。根点取「本帧结束后」的位置(<c>Center + velocity</c>,原逻辑同);
        /// 图鉴假体缩到一半(原代码把根距与节距都乘 0.5,带宽从 12 缩到 6,这里整副 Scale 0.5 一并覆盖)
        /// </summary>
        private void UpdateTentacleRig() {
            EnsureTentacleRig();
            if (rig == null || !rig.Bound) {
                return;
            }
            bool dummy = NPC.IsABestiaryIconDummy;
            rig.Scale = dummy ? 0.5f : NPC.scale;
            rig.SetRoot(NPC.Center + NPC.velocity, NPC.rotation);
            for (int k = 0; k < tentacleRoots.Length; k++) {
                float wobble = (float)Math.Cos(rig.Time * TentacleWobbleSpeed + k * 0.008f) * TentacleWobbleAmp;
                //根骨静息朝内(链的 Dir 约定指向领队),摆头叠在半圈之上
                rig.SetBoneLocalRotation(tentacleRoots[k], MathHelper.Pi + wobble);
            }
            rig.Step();
        }

        /// <summary>
        /// 细胞自己只做两件事:触须骨架(纯绘制)与逐帧阻尼。攻击与航向全由本体每帧写进 <c>velocity</c>,
        /// 各端跑的是同一份本体状态机,所以这里不需要任何权威端分支
        /// </summary>
        public override void AI() {
            bool client = VaultUtils.isClient;
            if (client) {
                netMotion.BeginFrame(NPC);
            }
            UpdateTentacleRig();
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

        /// <summary>切换正常触须带 / 加色发光带的可见性(bloom 通道用同一串骨、另一组 additive 带状件)</summary>
        private void SetTentacleGlowVisible(bool glow) {
            for (int k = 0; k < tentacleRibbons.Length; k++) {
                rig.Ribbons[tentacleRibbons[k]].Visible = !glow;
                rig.Ribbons[glowRibbons[k]].Visible = glow;
            }
        }

        /// <summary>
        /// 图鉴假体:AI 不跑,骨架在这里推进;带状件直接以 <paramref name="screenPos"/> 为视口偏移画进当前批次
        /// (图鉴批次矩阵与世界不同,Rig2DDrawContext.World 的批次参数不适用,所以这里只借骨骼坐标手铺条带,不切批次)
        /// </summary>
        private void DrawBestiary(SpriteBatch spriteBatch, Vector2 screenPos, Texture2D tex) {
            UpdateTentacleRig();
            if (TentacleRigReady) {
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                for (int k = 0; k < tentacleRibbons.Length; k++) {
                    Ribbon2DDef def = rig.Definition.Ribbons[tentacleRibbons[k]];
                    int[] chain = def.BoneIndices;
                    List<ColoredVertex> ve = new List<ColoredVertex>();
                    for (int i = 1; i < chain.Length; i++) {
                        Vector2 prev = rig.Bones[chain[i - 1]].Pos;
                        Vector2 cur = rig.Bones[chain[i]].Pos;
                        float jn = (float)(i - 1) / (chain.Length - 2);
                        Vector2 side = (cur - prev).ToRotation().ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 6;
                        ve.Add(new ColoredVertex(cur - screenPos + side, new Vector3(jn, 1, 1), Color.White));
                        ve.Add(new ColoredVertex(cur - screenPos - side, new Vector3(jn, 0, 1), Color.White));
                    }
                    Texture2D ribbonTex = rig.Asset.RibbonTextures.Length > tentacleRibbons[k] ? rig.Asset.RibbonTextures[tentacleRibbons[k]]?.Value : null;
                    if (ve.Count >= 3 && ribbonTex != null) {
                        gd.Textures[0] = ribbonTex;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    }
                }
            }
            spriteBatch.Draw(tex, NPC.Center - screenPos, null, Color.White, NPC.rotation, tex.Size() / 2, 0.5f, SpriteEffects.None, 0);
        }

        /// <summary>
        /// 绘制顺序与迁移前一致:先请宿主画双子绳(压在最底),再在加色批次里画 8 方向 × 3 遍的 bloom(触须发光带 + 本体),
        /// 最后正常画触须带与本体。带状件每组会自己切一轮批次,所以 bloom 的每个方向都重开一次加色批次
        /// </summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            Texture2D tex = NPC.getTexture();
            if (NPC.IsABestiaryIconDummy) {
                DrawBestiary(spriteBatch, screenPos, tex);
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
            if (!TentacleRigReady) {
                Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition, null, Color.White, NPC.rotation, tex.Size() / 2, NPC.scale, SpriteEffects.None);
                return false;
            }

            if (al > 0) {
                SetTentacleGlowVisible(true);
                Rig2DDrawContext glowCtx = Rig2DDrawContext.World().Flat(Color.White, al);
                for (int i = 0; i < 8; i++) {
                    Vector2 ofs = MathHelper.ToRadians(i * (360f / 8f)).ToRotationVector2() * 4;
                    glowCtx.ViewOffset = Main.screenPosition - ofs;
                    for (int j = 0; j < 3; j++) {
                        //本体件跟随当前批次的混合态,先在加色批次里画;发光带按定义就是加色,自己切批次
                        Main.spriteBatch.End();
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        Rig2DRenderer.Draw(spriteBatch, rig, in glowCtx);
                        Rig2DRibbonRenderer.DrawIndices(spriteBatch, rig, rig.Bones, in glowCtx, glowRibbons);
                    }
                }
                SetTentacleGlowVisible(false);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Rig2DDrawContext ctx = Rig2DDrawContext.World().Flat(Color.White);
            Rig2DRenderer.DrawAll(spriteBatch, rig, in ctx);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

    }
}
