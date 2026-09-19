using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault;
using InnoVault.Rigs2D.Data;
using InnoVault.Rigs2D.Runtime;
using InnoVault.Rigs2D.Solvers;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    /// <summary>
    /// 整链骨架:InnoVault Rigs2D。头为根,<c>length + 1</c> 节链骨(最后一节挂尾巴贴图)由一个
    /// <see cref="ChainFollowSolver"/> 接管,颌骨、鞭毛是头 / 尾节下的子骨,二阶段件与一阶段件同挂、按阶段切可见。
    /// <para>
    /// <b>求解器参数与迁移前的 <c>UpdateChain</c> 逐帧数学等价</b>:原代码每节取「自身到领队的方向」再按 0.12 的比例向领队朝向收敘,
    /// 落在领队后方一个节距处;<c>ChainFollow</c> 在 <c>poseWeightBase 0.12 / poseWeightCurl 0 / waveAmp 0 / maxBend π / turnRate 1</c>
    /// 下算的正是 <c>natural + wrap(leaderDir − natural) × 0.12</c>,再硬放到领队后方 <c>gap</c> 处。
    /// 输入只有已同步的本体坐标与朝向,骨架是纯本地量、不过线,各端同输入同输出
    /// </para>
    /// <para>
    /// 链长随难度变化(20 / 23 / 24 / 天顶 10),所以骨架<b>按实例代码直建</b>(<see cref="Rig2DBuilder"/>),不走 JSON;
    /// 体节 / 尾节实体每帧从这里读自己那节骨骼落位(<see cref="TryGetChainBone"/>),视觉链与碰撞链自此是同一条
    /// </para>
    /// </summary>
    public partial class CruiserHead
    {
        private const string TexRoot = "Content/NPCs/Cruiser/";
        private const int LayerSegBase = 10;
        private const int LayerJawDown = 200;
        private const int LayerJawUp = 201;
        private const int LayerHead = 210;

        private Rig2DInstance rig;
        private Vault2DRig rigAsset;
        private ChainFollowSolver chainSolver;
        private int[] segBones = [];
        private int[] segPieces = [];
        private int[] p2Pieces = [];
        private readonly List<int> flagABones = [];
        private readonly List<int> flagBBones = [];
        private readonly List<int> flagPieces = [];
        private int jawDownP1Bone = -1, jawUpP1Bone = -1, jawDownP2Bone = -1, jawUpP2Bone = -1;
        private int headP1Piece = -1, headP2Piece = -1, jawDownP1Piece = -1, jawUpP1Piece = -1, jawDownP2Piece = -1, jawUpP2Piece = -1;
        private bool rigPhase2Visible;
        private bool rigVisibilityInited;

        /// <summary>链骨数(含尾节)。骨架未建好时为 0</summary>
        public int ChainPointCount => segBones.Length;

        /// <summary>第 <paramref name="index"/> 节链骨的中心(0 = 紧跟头部的第一节,最后一节 = 尾)。越界回落头部坐标</summary>
        public Vector2 ChainPoint(int index) {
            if (rig == null || index < 0 || index >= segBones.Length) {
                return NPC.Center;
            }
            return rig.Bones[segBones[index]].Pos;
        }

        /// <summary>
        /// 体节 / 尾节实体读自己那节的位姿:<paramref name="pos"/> 是节中心,<paramref name="dir"/> 指向领队(与原 <c>wormFollow</c> 的 rotation 约定一致)
        /// </summary>
        public bool TryGetChainBone(int index, out Vector2 pos, out float dir) {
            if (rig == null || !rig.Built || index < 0 || index >= segBones.Length) {
                pos = NPC.Center;
                dir = NPC.rotation;
                return false;
            }
            ref Bone2D b = ref rig.Bones[segBones[index]];
            pos = b.Pos;
            dir = b.Dir;
            return true;
        }

        /// <summary>
        /// 首帧惰性建骨:此时 <c>length</c> 已由 SetDefaults 按难度定好。服务端也建(只解析定义、不取贴图),体节落位要用
        /// </summary>
        private void EnsureRig() {
            if (rig != null) {
                return;
            }
            int n = length + 1;
            Rig2DDefinition def = BuildChainDefinition(n, Main.zenithWorld, out int bd);
            if (def == null) {
                return;
            }
            rigAsset = Vault2DRig.FromDefinition(Mod, def);
            rig = rigAsset.CreateInstance(NPC.whoAmI);
            //图鉴 / 舞台实例才需要关调试;战斗实例保持可见,/vaultdebug 能看到整条链
            chainSolver = rig.Solver<ChainFollowSolver>("chain");

            segBones = new int[n];
            segPieces = new int[n];
            for (int i = 0; i < n; i++) {
                segBones[i] = rig.Bone($"seg{i}");
                segPieces[i] = rig.Piece($"seg{i}");
            }
            List<int> p2 = [];
            for (int k = 1; k <= bd; k++) {
                p2.Add(rig.Piece($"p2b{k}"));
            }
            p2Pieces = p2.ToArray();
            flagABones.Clear();
            flagBBones.Clear();
            flagPieces.Clear();
            for (int i = 0; i < n; i++) {
                int fa = rig.Bone($"flagA{i}");
                if (fa < 0) {
                    continue;
                }
                flagABones.Add(fa);
                flagBBones.Add(rig.Bone($"flagB{i}"));
                flagPieces.Add(rig.Piece($"flagA{i}"));
                flagPieces.Add(rig.Piece($"flagB{i}"));
            }
            jawDownP1Bone = rig.Bone("jawDownP1");
            jawUpP1Bone = rig.Bone("jawUpP1");
            jawDownP2Bone = rig.Bone("jawDownP2");
            jawUpP2Bone = rig.Bone("jawUpP2");
            headP1Piece = rig.Piece("headP1");
            headP2Piece = rig.Piece("headP2");
            jawDownP1Piece = rig.Piece("jawDownP1");
            jawUpP1Piece = rig.Piece("jawUpP1");
            jawDownP2Piece = rig.Piece("jawDownP2");
            jawUpP2Piece = rig.Piece("jawUpP2");
            rigVisibilityInited = false;
        }

        /// <summary>
        /// 整链骨架定义:头为根,<paramref name="n"/> 节链骨(末节挂尾巴)+ 颌骨 + 鞭毛 + 两阶段件 + ChainFollow 求解器。
        /// 战斗端按难度链长调用,图鉴沙盒(<see cref="CruiserPortraitActor"/>)按短链调用;
        /// <paramref name="p2PieceCount"/> 返回二阶段专用帧图件数(件名 <c>p2b1..p2bN</c>)。定义构建失败返回 null
        /// </summary>
        internal static Rig2DDefinition BuildChainDefinition(int n, bool zenith, out int p2PieceCount) {
            Rig2DBuilder b = new Rig2DBuilder("Cruiser").SnapDistance(CruiserDirector.RigSnapDistance);
            b.Bone("head");
            for (int i = 0; i < n; i++) {
                b.Bone($"seg{i}", i == 0 ? "head" : $"seg{i - 1}", CruiserDirector.ChainSpacing, new Vector2(-CruiserDirector.ChainSpacing, 0f));
            }
            b.Bone("jawDownP1", "head", 0f, new Vector2(CruiserDirector.P1JawOffset, CruiserDirector.P1JawOffset));
            b.Bone("jawUpP1", "head", 0f, new Vector2(CruiserDirector.P1JawOffset, -CruiserDirector.P1JawOffset));
            b.Bone("jawDownP2", "head", 0f, new Vector2(CruiserDirector.P2JawOffset, CruiserDirector.P2JawOffset));
            b.Bone("jawUpP2", "head", 0f, new Vector2(CruiserDirector.P2JawOffset, -CruiserDirector.P2JawOffset));
            for (int i = 0; i < n; i++) {
                //天顶世界每一节都带鞭毛,常规只有尾节带
                if (i == n - 1 || zenith) {
                    b.Bone($"flagA{i}", $"seg{i}", 0f, new Vector2(-CruiserDirector.FlagellumDrawOffset, 0f));
                    b.Bone($"flagB{i}", $"seg{i}", 0f, new Vector2(-CruiserDirector.FlagellumDrawOffset, 0f));
                }
            }

            //一阶段:奇偶节换贴图,尾节换尾巴;件都居中锚、贴图正面朝骨轴方向(领队方向)
            for (int i = 0; i < n; i++) {
                string tex = i == n - 1 ? "CruiserTail" : (i % 2 == 1 ? "CruiserBodyAlt" : "CruiserBody");
                b.Piece($"seg{i}", TexRoot + tex, Vector2.Zero, 0f, LayerSegBase + i * 2, $"seg{i}").ProximalNormalized(new Vector2(0.5f, 0.5f));
                if (i == n - 1 || zenith) {
                    //鞭毛贴图 180×12:A 片底边左端钉在锚点、朝上张开;B 片沿骨轴镜像(原 FlipVertically + 左上角原点)
                    b.Piece($"flagA{i}", TexRoot + "Flagellum", new Vector2(0f, CruiserDirector.FlagellumTexHeight), 0f, LayerSegBase + i * 2 + 1, $"flagA{i}");
                    b.Piece($"flagB{i}", TexRoot + "Flagellum", new Vector2(0f, CruiserDirector.FlagellumTexHeight), 0f, LayerSegBase + i * 2 + 1, $"flagB{i}").Look(mirror: true);
                }
            }
            //二阶段:只画 0~8 号节里除 0 与 2 之外的七节,七张专用帧图按序对应,默认隐藏
            int bd = 0;
            for (int d = 0; d < CruiserDirector.P2BodyNodeCount && d < n; d++) {
                if (d == CruiserDirector.P2BodySkipA || d == CruiserDirector.P2BodySkipB) {
                    continue;
                }
                bd++;
                b.Piece($"seg{d}", TexRoot + $"P2b{bd}", Vector2.Zero, 0f, LayerSegBase + d * 2, $"p2b{bd}").ProximalNormalized(new Vector2(0.5f, 0.5f));
            }
            //颌骨:原点数值照搬迁移前的 Draw(一阶段两侧贴图同高 74,所谓「拿对侧高度」结果相同)
            b.Piece("jawDownP1", TexRoot + "CruiserJawDown", new Vector2(CruiserDirector.P1JawOriginX / 2f, CruiserDirector.P1JawTexHeight / 2f), 0f, LayerJawDown, "jawDownP1");
            b.Piece("jawUpP1", TexRoot + "CruiserJawUp", new Vector2(CruiserDirector.P1JawOriginX / 2f, CruiserDirector.P1JawTexHeight / 2f), 0f, LayerJawUp, "jawUpP1");
            b.Piece("jawDownP2", TexRoot + "CruiserJawDown2", new Vector2(CruiserDirector.P2JawOriginX, CruiserDirector.P2JawOriginY), 0f, LayerJawDown, "jawDownP2");
            b.Piece("jawUpP2", TexRoot + "CruiserJawUp2", new Vector2(CruiserDirector.P2JawOriginX, CruiserDirector.P2JawTexHeight - CruiserDirector.P2JawOriginY), 0f, LayerJawUp, "jawUpP2");
            b.Piece("head", TexRoot + "CruiserHead", Vector2.Zero, 0f, LayerHead, "headP1").ProximalNormalized(new Vector2(0.5f, 0.5f));
            b.Piece("head", TexRoot + "Head2", Vector2.Zero, 0f, LayerHead, "headP2").ProximalNormalized(new Vector2(0.5f, 0.5f));

            //链求解器:与原 UpdateChain 等价的参数,见类注释
            string[] chainBones = new string[n];
            for (int i = 0; i < n; i++) {
                chainBones[i] = $"seg{i}";
            }
            b.Solver("ChainFollow", "chain", chainBones)
                .Param("poseWeightBase", CruiserDirector.ChainRotateRate)
                .Param("poseWeightCurl", 0f)
                .Param("curlGain", 0f)
                .Param("waveAmp", 0f)
                .Param("maxBend", MathHelper.Pi)
                .Param("turnRate", 1f);

            p2PieceCount = bd;
            return b.TryBuild();
        }

        /// <summary>
        /// 整链骨架落地(各端都跑,服务端也要,体节靶位读它)。
        /// 输入全是已同步量:本体坐标与朝向、嘴角、鞭毛张角。
        /// <para>
        /// 骑瓶期(<c>noaitime &gt; 0</c>)链求解器停机、全部链骨钉在头部,揭幕后求解器从这一团起步按 0.12 逐帧展开——
        /// 这就是迁移前「每帧把 bodies 全写成 Center」得到的登场展开感;骑瓶期本体不绘制,所以中间姿态无所谓
        /// </para>
        /// </summary>
        public void UpdateChainRig() {
            EnsureRig();
            if (rig == null) {
                return;
            }
            rig.Scale = NPC.scale;
            rig.SetRoot(NPC.Center, NPC.rotation);

            float mouth = MathHelper.ToRadians(Context?.MouthRot ?? 0f);
            rig.SetBoneLocalRotation(jawDownP1Bone, mouth);
            rig.SetBoneLocalRotation(jawUpP1Bone, -mouth);
            rig.SetBoneLocalRotation(jawDownP2Bone, mouth * CruiserDirector.P2JawRotFactor);
            rig.SetBoneLocalRotation(jawUpP2Bone, -mouth * CruiserDirector.P2JawRotFactor);

            float baseAngle = MathHelper.ToRadians(CruiserDirector.FlagellumBaseAngle);
            float spread = MathHelper.ToRadians(flagellumAngle);
            for (int i = 0; i < flagABones.Count; i++) {
                rig.SetBoneLocalRotation(flagABones[i], baseAngle - spread);
                rig.SetBoneLocalRotation(flagBBones[i], baseAngle + spread);
            }

            bool pinned = noaitime > 0;
            if (chainSolver != null) {
                chainSolver.Enabled = !pinned;
            }
            if (pinned) {
                for (int i = 0; i < segBones.Length; i++) {
                    rig.SetBoneWorld(segBones[i], NPC.Center, NPC.rotation);
                }
            }
            rig.Step();
        }

        /// <summary>按阶段切件可见性(纯绘制,只在客户端绘制路径调用)</summary>
        private void SyncRigVisibility(bool phase2) {
            if (rig == null || (rigVisibilityInited && rigPhase2Visible == phase2)) {
                return;
            }
            rigVisibilityInited = true;
            rigPhase2Visible = phase2;
            for (int i = 0; i < segPieces.Length; i++) {
                rig.Pieces[segPieces[i]].Visible = !phase2;
            }
            for (int i = 0; i < p2Pieces.Length; i++) {
                rig.Pieces[p2Pieces[i]].Visible = phase2;
            }
            //二阶段不画鞭毛,与迁移前 DrawPhase2Chain 一致
            for (int i = 0; i < flagPieces.Count; i++) {
                rig.Pieces[flagPieces[i]].Visible = !phase2;
            }
            rig.Pieces[headP1Piece].Visible = !phase2;
            rig.Pieces[jawDownP1Piece].Visible = !phase2;
            rig.Pieces[jawUpP1Piece].Visible = !phase2;
            rig.Pieces[headP2Piece].Visible = phase2;
            rig.Pieces[jawDownP2Piece].Visible = phase2;
            rig.Pieces[jawUpP2Piece].Visible = phase2;
        }

        /// <summary>整链绘制:件全部满亮(原 <c>Color.White × alpha</c>),调用方负责 shader 批次</summary>
        private void DrawChainRig(SpriteBatch spriteBatch, bool phase2) {
            if (rig == null || !rig.Built) {
                return;
            }
            SyncRigVisibility(phase2);
            Rig2DDrawContext ctx = Rig2DDrawContext.World(alpha).Flat(Color.White, alpha);
            Rig2DRenderer.Draw(spriteBatch, rig, in ctx);
        }

        /// <summary>
        /// 尾鞭结算。<see cref="CruiserStateContext.TailWhipCue"/> 起手(原 <c>tjv</c>,同帧消费),
        /// 之后鞭毛张角按 12 起、每帧减 1.5 的角速度甩一圈;张角落回 0 以下那一帧打出尾部新星。
        /// <para>
        /// 三个量(张角、角速度、进行中闩锁)都随快照过线:它们是逐帧积分出来的,
        /// 而且新星的触发帧完全由它们决定,不能任其在两端各自漂
        /// </para>
        /// <para>非鞭击期张角按速度推出静息值再一阶逼近,越快张得越窄</para>
        /// </summary>
        public void UpdateFlagellum() {
            if (Context.TailWhipCue) {
                whipActive = true;
                if (flagellumAngle < 0) {
                    flagellumAngle = 1;
                }
                whipSpeed = CruiserDirector.WhipLaunchSpeed;
            }
            if (whipActive) {
                flagellumAngle += whipSpeed;
                whipSpeed -= CruiserDirector.WhipDecel;
                if (flagellumAngle < 0) {
                    flagellumAngle = 0;
                    whipSpeed = 0;
                    whipActive = false;
                    //原代码在这里置 1 之后从不读,照搬
                    jaslowdown = 1;
                    FireTailNova();
                }
            }
            else {
                flagellumRest = CruiserDirector.FlagellumRestNumerator
                    / (NPC.velocity.Length() * CruiserDirector.FlagellumRestSpeedFactor) * CruiserDirector.FlagellumRestScale;
                if (flagellumRest < 0) {
                    flagellumRest = 0;
                }
                flagellumAngle += (flagellumRest - flagellumAngle) * CruiserDirector.FlagellumLerp;
            }
        }

        /// <summary>
        /// 尾部新星:从尾节后方甩出数环虚空星并附一发虚空爆。
        /// 弹幕与随机数只在权威端;音效各端本地放(服务端不放)。
        /// <para>
        /// <b>两处要照搬的怪写法</b>:环间初速每环 ×0.7 是<b>累乘同一个局部变量</b>,
        /// 而天顶世界那一段额外吐星用的是<b>循环跑完之后</b>的那个已经衰减过的初速 ×3;
        /// 另外环与环之间还额外转了 <c>一周 / num / counts</c> 的相位,所以各环星点是错开的
        /// </para>
        /// <para>原点从骨骼取:节骨的 <c>Dir</c> 指向领队,「节后方」即沿 −Dir 推 172 像素</para>
        /// </summary>
        private void FireTailNova() {
            int num = CruiserDirector.NovaNum;
            int counts = CruiserDirector.NovaCounts;
            float speed = CruiserDirector.NovaSpeed;
            //装灾厄读复仇/死亡,缺席仍走专家/大师兜底。原版专家/大师层叠在其上,顺序不动
            CruiserDirector.NovaScale(ref num, ref counts, ref speed);
            if (CurrentState == CruiserStateIndex.AroundPlayerAndShootVoidStar) {
                counts += CruiserDirector.NovaAroundCountsDelta;
                num /= 2;
                speed *= CruiserDirector.NovaAroundSpeedFactor;
            }

            if (!VaultUtils.isClient && rig != null && segBones.Length >= 2) {
                ref Bone2D tailBone = ref rig.Bones[segBones[^1]];
                Vector2 origin = tailBone.Pos - tailBone.Dir.ToRotationVector2() * CruiserDirector.NovaTailOffset * NPC.scale;
                int starType = ModContent.ProjectileType<VoidStar>();
                float angle = 0;
                for (int i = 0; i < counts; i++) {
                    for (int j = 0; j < num; j++) {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, angle.ToRotationVector2() * speed,
                            starType, (int)(NPC.damage / CruiserDirector.NovaStarDamageDivisor), CruiserDirector.NovaStarKnockback);
                        angle += (float)Math.PI * 2 / num;
                    }
                    angle += (float)Math.PI * 2 / num / counts;
                    speed *= CruiserDirector.NovaRingSpeedDecay;
                }
                Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, Vector2.Zero,
                    ModContent.ProjectileType<VoidExplode>(), (int)(NPC.damage / CruiserDirector.NovaExplodeDamageDivisor), CruiserDirector.NovaExplodeKnockback);

                if (Main.zenithWorld) {
                    for (int i = 1; i < segBones.Length; i++) {
                        ref Bone2D seg = ref rig.Bones[segBones[i]];
                        Vector2 segOrigin = seg.Pos - seg.Dir.ToRotationVector2() * CruiserDirector.NovaTailOffset * NPC.scale;
                        for (int _ = 0; _ < Main.rand.Next(CruiserDirector.NovaZenithCountMin, CruiserDirector.NovaZenithCountMax); _++) {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), segOrigin,
                                CEUtils.randomRot().ToRotationVector2() * speed * CruiserDirector.NovaZenithSpeedFactor,
                                starType, (int)(NPC.damage / CruiserDirector.NovaStarDamageDivisor), CruiserDirector.NovaStarKnockback);
                        }
                    }
                }
                //新星是决策点
                NPC.netUpdate = true;
            }
            if (Main.netMode != NetmodeID.Server) {
                SoundStyle sound = new SoundStyle("CalamityEntropy/Assets/Sounds/clap");
                sound.Pitch = CruiserDirector.NovaClapPitch;
                SoundEngine.PlaySound(sound);
                SoundEngine.PlaySound(SoundID.Item9);
            }
        }
    }
}
