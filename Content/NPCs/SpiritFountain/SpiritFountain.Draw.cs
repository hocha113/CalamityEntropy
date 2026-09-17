using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.SpiritFountain.Core;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SpiritFountain
{
    /// <summary>
    /// 冥魂泉的绘制层。纯本地:只读上下文里的表现量,不回写任何 gameplay 状态。
    /// 本体贴图从不画,画面上的「本体」是三层瞳孔贴图 + 两根魂柱的流光
    /// </summary>
    public partial class SpiritFountain
    {
        //绘制用 Extra 池贴图,基座 CEExtraAssets 未收编的先放本文件私有字段,加载期由 VaultLoaden 赋值
        [VaultLoaden("CalamityEntropy/Assets/Extra/TrailInk")]
        private static Asset<Texture2D> trailInkTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/SpiritEye")]
        private static Asset<Texture2D> spiritEyeTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/SpiritEye2")]
        private static Asset<Texture2D> spiritEye2Tex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/SpiritEye3")]
        private static Asset<Texture2D> spiritEye3Tex;

        /// <summary>瞳孔透明度。纯表现量,由计时确定性推导,不过线</summary>
        public float EyeAlpha => Context?.EyeAlpha ?? 0f;

        /// <summary>瞳孔注视点。读的是 <c>Main.LocalPlayer</c>,天然是本地量</summary>
        public Vector2 starePoint => Context?.StarePoint ?? Vector2.Zero;

        public void DrawColumn(FountainColumn column, Texture2D TrailTex) {
            Main.spriteBatch.Draw(CEExtraAssets.MegaStreakBacking2, NPC.Center + column.offset - Main.screenPosition, new Rectangle(-(int)column.trailDrawOffset, 0, 3600, 256), Color.White * column.alpha * 1.4f, column.rotation, new Vector2(1800, 128), NPC.scale * 1.4f * column.scale, SpriteEffects.None, 0);
            CECylinderDraw.DrawCylinder(CEExtraAssets.B1, NPC.Center + column.offset - Main.screenPosition, Color.White * column.alpha, BlendState.AlphaBlend, 30, 0.26f * column.scale, 0.5f, Main.GlobalTimeWrappedHourly * 3f, 5, column.rotation - MathHelper.PiOver2, false, false);
            CECylinderDraw.DrawCylinder(CEExtraAssets.B1, NPC.Center + column.offset - Main.screenPosition, Color.White * column.alpha * 0.9f, BlendState.Additive, 30, 0.26f * column.scale, 0.5f, Main.GlobalTimeWrappedHourly * -3f, 5, column.rotation - MathHelper.PiOver2, false, false);
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointWrap);
            Main.spriteBatch.Draw(TrailTex, NPC.Center + column.offset - Main.screenPosition, new Rectangle(-(int)column.trailDrawOffset, 0, 3600, TrailTex.Height), Color.AliceBlue * column.alpha * 0.9f, column.rotation, new Vector2(1800, TrailTex.Height / 2), NPC.scale * 1.5f * column.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(TrailTex, NPC.Center + column.offset - Main.screenPosition, new Rectangle(-(int)(column.trailDrawOffset * 0.9f), 0, 3600, TrailTex.Height), Color.AliceBlue * column.alpha, column.rotation, new Vector2(1800, TrailTex.Height / 2), NPC.scale * 1.4f * column.scale, SpriteEffects.FlipVertically, 0);
            Main.spriteBatch.Draw(trailInkTex.Value, NPC.Center + column.offset - Main.screenPosition, new Rectangle((int)(column.trailDrawOffset * 0.85f), 0, 3600, TrailTex.Height), Color.AliceBlue * column.alpha * 0.5f, column.rotation, new Vector2(1800, 128), NPC.scale * 1.6f * column.scale, SpriteEffects.FlipHorizontally, 0);
            Main.spriteBatch.Draw(CEExtraAssets.BasicTrail, NPC.Center + column.offset - Main.screenPosition, new Rectangle(-(int)column.trailDrawOffset, 0, 3600, 200), new Color(140, 140, 255) * column.alpha, column.rotation, new Vector2(1800, 100), NPC.scale * 3f * column.scale, SpriteEffects.None, 0);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPosition, Color drawColor) {
            if (NPC.IsABestiaryIconDummy)
                return false;

            Texture2D mainTrail = CEExtraAssets.StreakFaded;
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointWrap);

            Texture2D eyeTex = spiritEyeTex.Value;
            Texture2D eyeTex2 = spiritEye2Tex.Value;
            Texture2D eyeTex3 = spiritEye3Tex.Value;

            Main.spriteBatch.Draw(eyeTex, NPC.Center - Main.screenPosition, null, Color.White * EyeAlpha, 0, eyeTex.Size() / 2f, 0.46f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(eyeTex2, NPC.Center - Main.screenPosition + (starePoint - NPC.Center).normalize() * ((float)Math.Sqrt(CEUtils.getDistance(starePoint, NPC.Center))), null, Color.White * EyeAlpha, 0, eyeTex.Size() / 2f, 0.46f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(eyeTex3, NPC.Center - Main.screenPosition + (starePoint - NPC.Center).normalize() * ((float)Math.Sqrt(CEUtils.getDistance(starePoint, NPC.Center) * 3f)), null, Color.White * EyeAlpha, 0, eyeTex.Size() / 2f, 0.46f, SpriteEffects.None, 0);

            DrawColumn(column1, mainTrail);
            DrawColumn(column2, mainTrail);

            Main.spriteBatch.ExitShaderRegion();

            //出场演出前 90 帧的立柱闪光。读的是状态号与状态体计时,和魂环读的是同一个量
            if (ai == SpiritFountainStateIndex.SpawnAnimation && aiTimer < SpiritFountainDirector.SpawnFlashFrames) {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.spriteBatch.Draw(CEExtraAssets.MegaStreakBacking2, NPC.Center - Main.screenPosition, null, Color.AliceBlue, MathHelper.PiOver2, CEExtraAssets.MegaStreakBacking2.Size() / 2f, new Vector2(SpiritFountainDirector.SpawnFlashWidth, CEUtils.Parabola(aiTimer / SpiritFountainDirector.SpawnFlashFrames, SpiritFountainDirector.SpawnFlashParabolaPower)), SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }
    }
}
