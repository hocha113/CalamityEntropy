using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Core.Graphics.Screen;
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
        //链体贴图全部由 Rigs2D 骨架件持有(CruiserChainRig.cs);这里只剩预警光束的贴图
        [VaultLoaden("CalamityEntropy/Assets/Extra/T3")]
        private static Asset<Texture2D> t3Tex;

        /// <summary>
        /// 整链集中绘制。体节实体的 <c>PreDraw</c> 一律返回 false,链序前后压盖由骨架件的层序键决定
        /// (体节按索引递增、颌骨与头压在最上)。
        /// <para>
        /// <b>坐标一律读裸值</b>:骨架根是本帧 AI 末尾的 <c>NPC.Center</c>。原版会把 NPC 画在
        /// <c>position + netOffset</c>,而头、体节、尾节三个类型都已显式关掉平滑(<c>NoMultiplayerSmoothingByType</c>),
        /// 所以 <c>NPC.Center</c>(预警光束读它)与骨架处在同一层级、不会分家
        /// </para>
        /// </summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPosition, Color drawColor) {
            if (NPC.IsABestiaryIconDummy)
                return false;

            //二阶段交给 CEEntityOverlay.DrawLateOverlay 代画(candraw 只在那条路径上为真)。
            //门必须跟代画方一致:复古/迷幻光照或关掉绚丽特效时管线不跑,这里就得自己画,否则整只消失
            if (!candraw && !(phase == 1) && CEScreenPipeline.PixelPassActive) {
                return false;
            }
            if (noaitime > 0) {
                return false;
            }
            if (whiteLerp > 0) {
                Effect shader = CEEffectAssets.WhiteTrans;
                shader.Parameters["strength"].SetValue(whiteLerp);
                Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
                shader.CurrentTechnique.Passes[0].Apply();
            }

            //一阶段整链 / 二阶段七节专用帧图 + 颌骨 + 头,全在骨架件里,按阶段切可见后一次画完
            DrawChainRig(spriteBatch, phaseTrans > CruiserDirector.PhaseTransDrawSwitch);

            DrawWarningBeam();
            return false;
        }

        /// <summary>
        /// 预警光束:激光的瞄准窗内,或直扑时距离超过 1200,都会亮起一条指向航向的长条。
        /// 二阶段插值更快(0.2 对 0.064),所以二阶段的预告更急
        /// </summary>
        private void DrawWarningBeam() {
            float lerp = phase == 2 ? CruiserDirector.WarningLerpPhase2 : CruiserDirector.WarningLerpPhase1;
            int laserAim = Context?.LaserAim ?? 0;
            bool warn = (CurrentState == CruiserStateIndex.VoidLaser && laserAim < CruiserDirector.LaserWarningAimFrames)
                || (CurrentState == CruiserStateIndex.TryToClosePlayer
                    && CEUtils.getDistance(NPC.Center, NPC.target.ToPlayer().Center) > CruiserDirector.WarningCloseInDistance);
            WarningAlpha = float.Lerp(WarningAlpha, warn ? 1 : 0, lerp);
            if (WarningAlpha > CruiserDirector.WarningVisibleThreshold) {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Texture2D w = t3Tex.Value;
                float outer = (phase == 1 ? CruiserDirector.WarningWidthOuterP1 : CruiserDirector.WarningWidthOuterP2) * WarningAlpha;
                float inner = (phase == 1 ? CruiserDirector.WarningWidthInnerP1 : CruiserDirector.WarningWidthInnerP2) * WarningAlpha * WarningAlpha * WarningAlpha;
                Main.spriteBatch.Draw(w, NPC.Center - Main.screenPosition, null, Color.AliceBlue * CruiserDirector.WarningOpacity * WarningAlpha, NPC.rotation, new Vector2(0, w.Height / 2f), new Vector2(CruiserDirector.WarningLength, outer), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(w, NPC.Center - Main.screenPosition, null, Color.AliceBlue * CruiserDirector.WarningOpacity * WarningAlpha, NPC.rotation, new Vector2(0, w.Height / 2f), new Vector2(CruiserDirector.WarningLength, inner), SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
        }

        public override void PostDraw(SpriteBatch sbb, Vector2 screenPos, Color drawColor) {
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
