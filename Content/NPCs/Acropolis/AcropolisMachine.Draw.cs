using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.Acropolis.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using InnoVault.Rigs2D.Runtime;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 卫城机器的表现层:整机由 Rigs2D 骨架件集中绘制(腿、两条臂、本体、肩甲、停靠的鱼叉),焦痕着色器逐件套参,受击与死亡粒子。
    /// 纯本地,只读 gameplay 状态,绝不回写
    /// </summary>
    public partial class AcropolisMachine
    {
        //harpoonOutlineTex 设为 internal 供同目录 Harpoon 复用;其余贴图全由骨架件持有(Assets/Rigs/Acropolis.rig.json)
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/HarpoonOutline")]
        internal static Asset<Texture2D> harpoonOutlineTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Acropolis/Harpoon")]
        private static Asset<Texture2D> harpoonTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/cloudNoise")]
        private static Asset<Texture2D> cloudNoiseTex;

        /// <summary>逐件焦痕参数(件索引 → 阈值 / 噪声偏移),每帧按 whoAmI 播种重摇,顺序与迁移前逐张贴图的调用顺序一致</summary>
        private float[] charredAlpha = [];
        private Vector2[] charredOfs = [];

        /// <summary>
        /// 焦痕着色器:开一个 Immediate 批次,噪声贴图挂在 s1;逐件参数由 <see cref="Rig2DDrawContext.BeforePiece"/> 在每次 Draw 前写入。
        /// 阈值为 1 时着色器原样返回贴图色,等价于迁移前「minAlpha ≥ 1 就退出 shader 区」的那一支
        /// </summary>
        private static void BeginCharredBatch(SpriteBatch spriteBatch, Effect shader, Texture2D noise) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            Main.graphics.GraphicsDevice.Textures[1] = noise;
        }

        private void CharredPieceHook(Rig2DInstance r, int pieceIndex, Texture2D texture, Rectangle frame) {
            Effect shader = CommonEffects.charred;
            if (shader == null || pieceIndex >= charredAlpha.Length) {
                return;
            }
            shader.Parameters["minAlpha"].SetValue(charredAlpha[pieceIndex]);
            shader.Parameters["noiseOffset"].SetValue(charredOfs[pieceIndex]);
            shader.Parameters["texSize"].SetValue(texture.Size());
        }

        /// <summary>
        /// 摇出每件的焦痕参数。随机序列按 whoAmI 播种,所以每台机器的斑驳是固定的;
        /// 调用顺序照搬迁移前逐张贴图 <c>prepareShader</c> 的顺序(内左腿、内右腿、外左腿、外右腿各三节,鱼叉臂、鱼叉、发射器、本体、炮臂两节、肩甲),
        /// 停靠鱼叉不可见时也照样摇一次,免得后面几件的斑驳跟着鱼叉出膛跳变
        /// </summary>
        private void RollCharred(UnifiedRandom random, Texture2D noise) {
            if (charredAlpha.Length != rig.Pieces.Length) {
                charredAlpha = new float[rig.Pieces.Length];
                charredOfs = new Vector2[rig.Pieces.Length];
            }
            float cAlpha = random.NextBool(6) ? random.NextFloat(0.6f, 1f) : random.NextFloat(0.8f, 1f);
            void roll(int piece) {
                float al = float.Clamp(cAlpha + random.NextFloat(-0.1f, 0.1f), 0, 1);
                if (random.NextBool(5)) {
                    al = 0;
                }
                Vector2 ofs = new Vector2(random.NextFloat(0, 0.5f), random.NextFloat(0, 0.5f)) * noise.Size();
                if (piece >= 0 && piece < charredAlpha.Length) {
                    charredAlpha[piece] = al;
                    charredOfs[piece] = ofs;
                }
            }
            //原 LegMounts 顺序:内左、内右、外左、外右;骨架腿序是 0 内左、1 外左、2 内右、3 外右
            foreach (int leg in AcropolisDirector.LegCharredOrder) {
                for (int p = 0; p < LegPartCount; p++) {
                    roll(legPieces[leg, p]);
                }
            }
            roll(armLinkPieces[0]);
            roll(facingPieces[4]);
            roll(facingPieces[3]);
            roll(facingPieces[0]);
            roll(armLinkPieces[1]);
            roll(facingPieces[2]);
            roll(facingPieces[1]);
        }

        /// <summary>
        /// 整机集中绘制:骨架件按层序一次画出(腿 → 鱼叉臂 → 停靠鱼叉 → 发射器 → 本体 → 炮臂 → 肩甲),
        /// 全部件同吃一个 <paramref name="drawColor"/>(天顶世界换迪斯科色),与迁移前一致。
        /// 停靠鱼叉的蓄力轮廓光环不吃焦痕 shader,夹在鱼叉臂第一节与鱼叉本体之间画,所以骨架分两个层序带画
        /// </summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!RigReady) {
                return false;
            }
            Effect shader = CommonEffects.charred;
            Texture2D noise = cloudNoiseTex?.Value;
            if (shader == null || noise == null) {
                Rig2DDrawContext plain = Rig2DDrawContext.World().Flat(drawColor);
                Rig2DRenderer.Draw(spriteBatch, rig, in plain);
                return false;
            }
            //焦痕的随机量按 whoAmI 播种,每台机器的斑驳是固定的
            UnifiedRandom random = new UnifiedRandom(NPC.type + NPC.whoAmI * 47);
            RollCharred(random, noise);
            if (Main.zenithWorld) {
                drawColor = Main.DiscoColor;
            }
            shader.Parameters["cColor"].SetValue((Color.Black * 0.4f).ToVector4());
            shader.Parameters["noiseSize"].SetValue(noise.Size() * 2f);

            Rig2DDrawContext ctx = Rig2DDrawContext.World().Flat(drawColor).WithPieceHook(CharredPieceHook);
            bool docked = _harpoon < 0 || HarpoonOnLauncher;

            BeginCharredBatch(spriteBatch, shader, noise);
            Rig2DDrawContext lower = ctx.Layers(float.NegativeInfinity, LayerHarpoonArm);
            Rig2DRenderer.Draw(spriteBatch, rig, in lower);
            if (docked) {
                spriteBatch.ExitShaderRegion();
                DrawHarpoonOutline();
                BeginCharredBatch(spriteBatch, shader, noise);
            }
            Rig2DDrawContext upper = ctx.Layers(LayerHarpoonDocked, float.PositiveInfinity);
            Rig2DRenderer.Draw(spriteBatch, rig, in upper);
            spriteBatch.ExitShaderRegion();
            return false;
        }

        /// <summary>停靠鱼叉的蓄力轮廓:六个方向各偏 2 像素叠画一遍轮廓贴图,亮度随蓄力涨。位姿从枪口骨读</summary>
        private void DrawHarpoonOutline() {
            float charge = Context?.HarpoonCharge ?? 0f;
            Texture2D outline = harpoonOutlineTex?.Value;
            Texture2D harpoon = harpoonTex?.Value;
            if (outline == null || harpoon == null || charge <= 0.001f) {
                return;
            }
            Vector2 muzzle = HarpoonPos;
            float rot = HarpoonArm.BarrelDir;
            SpriteEffects fx = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Vector2 origin = new Vector2(70, harpoon.Height / 2f);
            for (float r = 0; r <= 360; r += 60) {
                Main.EntitySpriteDraw(outline, MathHelper.ToRadians(r).ToRotationVector2() * 2 + muzzle - Main.screenPosition, null,
                    Color.OrangeRed * charge, rot, origin, NPC.scale, fx);
            }
        }

        /// <summary>受击与解体表现。全部在 <c>!dedServ</c> 内,纯本地</summary>
        public override void HitEffect(NPC.HitInfo hit) {
            if (DeathCounter > 0) {
                return;
            }
            if (chargeSnd != null) {
                chargeSnd.timeleft = 0;
            }
            if (NPC.life > 0 || Main.dedServ) {
                return;
            }

            if (Main.zenithWorld) {
                PRTLoader.NewParticle<PRT_RealisticExplosion>(NPC.Center, Vector2.Zero, Color.White, 18f * NPC.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, -1);
            }
            else {
                //正常死亡PulseRing+双Shine+40 EMediumSmoke,zenith改单RealisticExplosion
                PRTLoader.NewParticle<PRT_PulseRing>(NPC.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(7f, 8);
                PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.Firebrick, 14f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                PRTLoader.NewParticle<PRT_ShineParticle>(NPC.Center, Vector2.Zero, Color.White, 10f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.Zero, 100), CEUtils.getDistance(NPC.Center, Main.LocalPlayer.Center), 1200);

                for (int i = 0; i < 40; i++) {
                    //40颗EMediumSmoke随机喷出,跟PulseRing/Shine同帧,死亡密度最高的一段
                    PRTLoader.NewParticle<PRT_EMediumSmoke>(NPC.Center + CEUtils.randomPointInCircle(60 * NPC.scale), CEUtils.randomPointInCircle(32 * NPC.scale), Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()), Main.rand.NextFloat(1f, 4f) * NPC.scale).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), 120);
                }
            }
            SpawnGore("AcrGore0", 1);
            SpawnGore("AcrGore1", 1);
            SpawnGore("AcrGore2", 1);
            SpawnGore("AcrGore3", 1);
            SpawnGore("AcrGore4", 4);
            SpawnGore("AcrGore5", 4);
            SpawnGore("AcrGore6", 1);
            SpawnGore("AcrGore7", 4);
            SpawnGore("AcrGore8", 1);
            SpawnGore("AcrGore9", 1);
        }

        private void SpawnGore(string name, int count) {
            int type = Mod.Find<ModGore>(name).Type;
            for (int i = 0; i < count; i++) {
                Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center + CEUtils.randomPointInCircle(46), CEUtils.randomPointInCircle(16), type, NPC.scale);
            }
        }
    }
}
