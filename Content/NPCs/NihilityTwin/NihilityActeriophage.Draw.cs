using CalamityEntropy.Content.NPCs.NihilityTwin.Core;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    /// <summary>
    /// 虚无双子的表现层:贴图声明、本体三层触须绘制、连接绳索、尾迹尘。
    /// 全部纯本地,不回写任何 gameplay 状态
    /// </summary>
    public partial class NihilityActeriophage
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
        /// <summary>绳索贴图。<see cref="ChaoticCellSmall"/> 也复用这个字段,不要挪走</summary>
        [VaultLoaden("CalamityEntropy/Content/NPCs/NihilityTwin/NihRope")]
        internal static Asset<Texture2D> nihRopeTex;

        /// <summary>绳索在本体那一端的挂点:中心沿朝向 +90° 偏移 64</summary>
        public Vector2 buttom => NPC.Center + new Vector2(0, NihilityDirector.RopeAnchorOffset).RotatedBy(NPC.rotation + MathHelper.PiOver2);

        /// <summary>
        /// 尾迹尘。频率由 <c>localAI[0]</c> 的余弦给,所以两侧尘线会交替张合。
        /// <c>Dust.NewDust</c> 在服务端直接返回,不吃随机数
        /// </summary>
        public void SpawnParticle(Vector2 center) {
            Vector2 vel = (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * (float)Math.Cos(NPC.localAI[0] * 0.3f) * 16;
            Vector2 vel2 = vel * -1;
            vel -= NPC.velocity * 1f;
            vel2 -= NPC.velocity * 1f;
            Dust.NewDust(center, 1, 1, DustID.MagicMirror, vel.X, vel.Y);
            Dust.NewDust(center, 1, 1, DustID.MagicMirror, vel2.X, vel2.Y);
        }

        /// <summary>二阶段口部激光那一手要换本体贴图。读同步槽 <c>ai[3]</c>,客户端不必等本地换态</summary>
        private bool InLaserPose => Context != null && Context.Phase == 2
            && (int)NPC.ai[3] == (int)NihilityStateIndex.P2Laser;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (spawnAnm > 0) {
                return false;
            }
            float rot = NPC.rotation + MathHelper.PiOver2;

            Texture2D tex = NPC.getTexture();
            if (InLaserPose) {
                tex = bodyAltTex.Value;
            }
            Color color = Color.White;

            //触须的张开量随速度渐进饱和
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

        /// <summary>
        /// 本体与细胞之间的绳索。由 <see cref="ChaoticCell"/> 的绘制路径回调,
        /// 两端读的都是未加平滑偏移的原始 <c>Center</c>(本体与细胞都已关掉 netOffset),不会出现根部跳动
        /// </summary>
        public void drawRope() {
            if (rope == null || cell == null) {
                return;
            }
            if (ropeLerp <= 0) {
                return;
            }
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<Vector2> points = rope.GetPoints();

            points.Insert(0, buttom);
            points.Add(cell.Center);
            points.Add(cell.Center);
            float lc = 1;
            float jn = 0;

            for (int i = 1; i < points.Count - 1; i++) {
                jn += CEUtils.getDistance(points[i - 1], points[i]) / (float)28 * lc;

                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 7 * lc,
                      new Vector3(jn, 1, 1),
                      Color.White));
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 7 * lc,
                      new Vector3(jn, 0, 1),
                      Color.White));
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3) {
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
