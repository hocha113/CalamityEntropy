using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    public partial class CruiserHead
    {
        //绘制用贴图,加载期由 VaultLoaden 赋值;白化着色器读共享基座 CEEffectAssets(只在客户端绘制路径读取)
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/P2b", 1, 7, AssetMode = AssetMode.TextureValueArray)]
        private static Texture2D[] p2BodyFrames;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/Head2")]
        private static Asset<Texture2D> head2Tex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawUp2")]
        private static Asset<Texture2D> jawUp2Tex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawDown2")]
        private static Asset<Texture2D> jawDown2Tex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/Flagellum")]
        private static Asset<Texture2D> flagellumTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserTail")]
        private static Asset<Texture2D> cruiserTailTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserBodyAlt")]
        private static Asset<Texture2D> cruiserBodyAltTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserBody")]
        private static Asset<Texture2D> cruiserBodyTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserHead")]
        private static Asset<Texture2D> cruiserHeadTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawUp")]
        private static Asset<Texture2D> jawUpTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawDown")]
        private static Asset<Texture2D> jawDownTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/T3")]
        private static Asset<Texture2D> t3Tex;

        /// <summary>
        /// 整链集中绘制。体节实体的 <c>PreDraw</c> 一律返回 false,链序前后压盖由这里的循环决定。
        /// <para>
        /// <b>坐标一律读裸值</b>:头部用本帧 AI 末尾记下的 <see cref="vtodraw"/>,骨节用
        /// <see cref="bodies"/>。原版会把 NPC 画在 <c>position + netOffset</c>,而头、体节、尾节
        /// 三个类型都已显式关掉平滑(<c>NoMultiplayerSmoothingByType</c>),
        /// 所以 <c>NPC.Center</c>(预警光束读它)与 <c>vtodraw</c> 处在同一层级、不会分家
        /// </para>
        /// </summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPosition, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
                return false;

            //二阶段交给 EffectLoader 的像素通道代画(candraw 只在那条路径上为真)
            if (!candraw && !(phase == 1) && ModContent.GetInstance<Config>().EnablePixelEffect)
            {
                return false;
            }
            if (noaitime > 0)
            {
                return false;
            }
            if (whiteLerp > 0)
            {
                Effect shader = CEEffectAssets.WhiteTrans;
                shader.Parameters["strength"].SetValue(whiteLerp);
                Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
                shader.CurrentTechnique.Passes[0].Apply();
            }

            float mouthRot = Context?.MouthRot ?? 0f;
            if (phaseTrans > CruiserDirector.PhaseTransDrawSwitch)
            {
                DrawPhase2Chain(spriteBatch, screenPosition, mouthRot);
            }
            else
            {
                DrawPhase1Chain(spriteBatch, screenPosition, mouthRot);
            }

            DrawWarningBeam();
            return false;
        }

        /// <summary>二阶段:只画 0~8 号骨节里除 0 与 2 之外的七节,贴图换成七张专用帧图</summary>
        private void DrawPhase2Chain(SpriteBatch spriteBatch, Vector2 screenPosition, float mouthRot)
        {
            int bd = 0;
            for (int d = 0; d < CruiserDirector.P2BodyNodeCount; d++)
            {
                if (d == CruiserDirector.P2BodySkipA || d == CruiserDirector.P2BodySkipB)
                {
                    continue;
                }
                float rot = bd == 0 ? (vtodraw - bodies[d]).ToRotation() : (bodies[d - 1] - bodies[d]).ToRotation();
                Vector2 pos = bodies[d];
                Texture2D tx = p2BodyFrames[bd];
                spriteBatch.Draw(tx, pos - screenPosition, null, Color.White * alpha, rot, new Vector2(tx.Width, tx.Height) / 2, NPC.scale, SpriteEffects.None, 0f);
                bd += 1;
            }

            Texture2D txd = head2Tex.Value;
            Texture2D j2 = jawUp2Tex.Value;
            Texture2D j1 = jawDown2Tex.Value;
            Vector2 joffset = new Vector2(CruiserDirector.P2JawOffset, CruiserDirector.P2JawOffset);
            Vector2 ofs2 = joffset * new Vector2(1, -1);
            float roth = mouthRot * CruiserDirector.P2JawRotFactor;

            spriteBatch.Draw(j1, vtodraw - screenPosition + joffset.RotatedBy(NPC.rotation) * NPC.scale, null, Color.White * alpha, NPC.rotation + MathHelper.ToRadians(roth), new Vector2(CruiserDirector.P2JawOriginX, CruiserDirector.P2JawOriginY), NPC.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(j2, vtodraw - screenPosition + ofs2.RotatedBy(NPC.rotation) * NPC.scale, null, Color.White * alpha, NPC.rotation - MathHelper.ToRadians(roth), new Vector2(CruiserDirector.P2JawOriginX, j2.Height - CruiserDirector.P2JawOriginY), NPC.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(txd, vtodraw - screenPosition, null, Color.White * alpha, NPC.rotation, new Vector2(txd.Width, txd.Height) / 2, NPC.scale, SpriteEffects.None, 0f);
        }

        /// <summary>一阶段:整条链逐节画,奇偶节换贴图,尾节换尾巴贴图并额外画左右两片鞭毛</summary>
        private void DrawPhase1Chain(SpriteBatch spriteBatch, Vector2 screenPosition, float mouthRot)
        {
            for (int d = 0; d <= bodies.Count - 1; d++)
            {
                float rot = d == 0 ? (vtodraw - bodies[d]).ToRotation() : (bodies[d - 1] - bodies[d]).ToRotation();
                Vector2 pos = bodies[d];
                Texture2D f1 = flagellumTex.Value;
                if (d == bodies.Count - 1)
                {
                    Texture2D tx = cruiserTailTex.Value;
                    spriteBatch.Draw(tx, pos - screenPosition, null, Color.White * alpha, rot, new Vector2(tx.Width, tx.Height) / 2, NPC.scale, SpriteEffects.None, 0f);
                }
                else
                {
                    Texture2D tx = d % 2 == 1 ? cruiserBodyAltTex.Value : cruiserBodyTex.Value;
                    spriteBatch.Draw(tx, pos - screenPosition, null, Color.White * alpha, rot, new Vector2(tx.Width, tx.Height) / 2, NPC.scale, SpriteEffects.None, 0f);
                }
                //天顶世界每一节都带鞭毛,常规只有尾节带
                if (d == bodies.Count - 1 || Main.zenithWorld)
                {
                    Vector2 anchor = pos - screenPosition - new Vector2(CruiserDirector.FlagellumDrawOffset, 0).RotatedBy(rot) * NPC.scale;
                    spriteBatch.Draw(f1, anchor, null, Color.White * alpha, rot + MathHelper.ToRadians(CruiserDirector.FlagellumBaseAngle - flagellumAngle), new Vector2(0, f1.Height), NPC.scale, SpriteEffects.None, 0);
                    spriteBatch.Draw(f1, anchor, null, Color.White * alpha, rot + MathHelper.ToRadians(CruiserDirector.FlagellumBaseAngle + flagellumAngle), new Vector2(0, 0), NPC.scale, SpriteEffects.FlipVertically, 0);
                }
            }

            Texture2D txd = cruiserHeadTex.Value;
            Texture2D j2 = jawUpTex.Value;
            Texture2D j1 = jawDownTex.Value;
            Vector2 joffset = new Vector2(CruiserDirector.P1JawOffset, CruiserDirector.P1JawOffset);
            Vector2 ofs2 = joffset * new Vector2(1, -1);
            float roth = mouthRot;
            //原代码这两处的原点分别拿了对侧贴图的高度(j1 用 j2.Height、j2 用 j1.Height),照搬
            spriteBatch.Draw(j1, vtodraw - screenPosition + joffset.RotatedBy(NPC.rotation) * NPC.scale, null, Color.White * alpha, NPC.rotation + MathHelper.ToRadians(roth), new Vector2(CruiserDirector.P1JawOriginX, j2.Height) / 2, NPC.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(j2, vtodraw - screenPosition + ofs2.RotatedBy(NPC.rotation) * NPC.scale, null, Color.White * alpha, NPC.rotation - MathHelper.ToRadians(roth), new Vector2(CruiserDirector.P1JawOriginX, j1.Height) / 2, NPC.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(txd, vtodraw - screenPosition, null, Color.White * alpha, NPC.rotation, new Vector2(txd.Width, txd.Height) / 2, NPC.scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 预警光束:激光的瞄准窗内,或直扑时距离超过 1200,都会亮起一条指向航向的长条。
        /// 二阶段插值更快(0.2 对 0.064),所以二阶段的预告更急
        /// </summary>
        private void DrawWarningBeam()
        {
            float lerp = phase == 2 ? CruiserDirector.WarningLerpPhase2 : CruiserDirector.WarningLerpPhase1;
            int laserAim = Context?.LaserAim ?? 0;
            bool warn = (CurrentState == CruiserStateIndex.VoidLaser && laserAim < CruiserDirector.LaserWarningAimFrames)
                || (CurrentState == CruiserStateIndex.TryToClosePlayer
                    && CEUtils.getDistance(NPC.Center, NPC.target.ToPlayer().Center) > CruiserDirector.WarningCloseInDistance);
            WarningAlpha = float.Lerp(WarningAlpha, warn ? 1 : 0, lerp);
            if (WarningAlpha > CruiserDirector.WarningVisibleThreshold)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Texture2D w = t3Tex.Value;
                float outer = (phase == 1 ? CruiserDirector.WarningWidthOuterP1 : CruiserDirector.WarningWidthOuterP2) * WarningAlpha;
                float inner = (phase == 1 ? CruiserDirector.WarningWidthInnerP1 : CruiserDirector.WarningWidthInnerP2) * WarningAlpha * WarningAlpha * WarningAlpha;
                Main.spriteBatch.Draw(w, NPC.Center - Main.screenPosition, null, Color.AliceBlue * CruiserDirector.WarningOpacity * WarningAlpha, NPC.rotation, new Vector2(0, w.Height / 2f), new Vector2(CruiserDirector.WarningLength, outer), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(w, NPC.Center - Main.screenPosition, null, Color.AliceBlue * CruiserDirector.WarningOpacity * WarningAlpha, NPC.rotation, new Vector2(0, w.Height / 2f), new Vector2(CruiserDirector.WarningLength, inner), SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
        }

        public override void PostDraw(SpriteBatch sbb, Vector2 screenPos, Color drawColor)
        {
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
