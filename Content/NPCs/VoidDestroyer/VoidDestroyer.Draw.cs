using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 绘制层(纯本地,只读 Context 与宿主的视觉推导量):传送门、限制圈、本体(变形帧/假 Z/全息残影)、
    /// 能量翼、底部火焰、红射线预警、核心光、护盾环。抖动偏移由原版 netOffset 承担,这里不再叠位置
    /// </summary>
    public partial class VoidDestroyer
    {
        /// <summary>核心相对贴图中心的偏移(二阶段能量核心的位置)</summary>
        private static readonly Vector2 CoreOffset = new Vector2(0, 4);
        /// <summary>能量翼环绕中心相对本体中心的偏移</summary>
        private static readonly Vector2 WingPivotOffset = new Vector2(0, -8);
        private const float WingBaseRadius = 150f;
        private const float WingExpandRadius = 70f;
        private const int TransformFrameHeight = 122;
        private const int TransformFramePitch = 124;
        /// <summary>退入背景时的冷色调</summary>
        private static readonly Color FarTint = new Color(120, 120, 200);

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            if (NPC.IsABestiaryIconDummy || Context == null)
            {
                spriteBatch.Draw(bodyTex, NPC.Center - screenPos, null, drawColor, 0f, bodyTex.Size() / 2f, NPC.scale, SpriteEffects.None, 0f);
                return false;
            }

            if (Context.PortalOpenness > 0.01f)
            {
                DrawPortalAt(Context.AnchorPos, Context.PortalOpenness, 0f, 190f, screenPos);
            }
            if (Context.ArenaActive && !Context.Dying)
            {
                DrawArenaRing(screenPos);
            }

            if (Alpha <= 0.01f)
            {
                return false;
            }

            Texture2D tex = bodyTex;
            Rectangle? frame = null;
            if (Context.TransformFrame >= 0 && transformTex != null)
            {
                tex = transformTex.Value;
                frame = new Rectangle(0, Math.Clamp(Context.TransformFrame, 0, 5) * TransformFramePitch, tex.Width, TransformFrameHeight);
            }
            else if (Phase >= 2 && p2Tex != null)
            {
                tex = p2Tex.Value;
            }
            Vector2 origin = frame.HasValue ? frame.Value.Size() / 2f : tex.Size() / 2f;

            //假 Z:退入背景时缩小、冷色、略透明(判定位置不动,接触窗由宿主关)
            float depthScale = MathHelper.Lerp(1f, VDDirector.OrbitalFarScale, FakeZ);
            float scale = NPC.scale * DrawScale * depthScale;
            Color bodyColor = Color.Lerp(drawColor, FarTint, FakeZ * 0.65f) * (Alpha * MathHelper.Lerp(1f, 0.8f, FakeZ));

            //底部火焰与能量翼画在本体后面
            if (WingAlpha > 0.01f)
            {
                DrawBottomFlame(screenPos, scale);
                DrawWings(screenPos, scale);
            }

            DrawSpeedGhosts(tex, frame, origin, scale, screenPos);

            spriteBatch.Draw(tex, NPC.Center - screenPos, frame, bodyColor, NPC.rotation, origin, scale, SpriteEffects.None, 0f);

            DrawRedRayWarning(screenPos);
            DrawAimLine(screenPos);
            DrawCoreGlow(screenPos, scale);
            if (ShieldAlpha > 0.01f)
            {
                DrawShield(screenPos, scale);
            }
            return false;
        }

        #region 残影
        /// <summary>高速残影:速度门控(≥18px/f 才出现),走全息着色器的紫色扫描线投影,不是简单的半透明贴图</summary>
        private void DrawSpeedGhosts(Texture2D tex, Rectangle? frame, Vector2 origin, float scale, Vector2 screenPos)
        {
            float speed = NPC.velocity.Length();
            float intensity = MathHelper.Clamp((speed - 18f) / 22f, 0f, 1f);
            if (intensity <= 0.02f || NPC.oldPos == null)
            {
                return;
            }
            VDHologramDraw.Begin();
            for (int i = 1; i < NPC.oldPos.Length; i++)
            {
                if (NPC.oldPos[i] == Vector2.Zero)
                {
                    continue;
                }
                float fade = 1f - i / (float)NPC.oldPos.Length;
                Vector2 pos = NPC.oldPos[i] + NPC.Size / 2f - screenPos;
                VDHologramDraw.DrawPart(tex, pos, frame, VDVfx.VoidPurple, 0.5f * fade * intensity * Alpha, NPC.rotation, origin, scale * (0.96f + 0.04f * fade), SpriteEffects.None);
            }
            VDHologramDraw.End();
        }
        #endregion

        #region 传送门
        /// <summary>
        /// 椭圆形紫色传送门:SoulVortex 四点绘制(先旋转再压 X 再整体转向,纹理在固定椭圆内自转),外圈叠加加法光环。
        /// rotation 是椭圆短轴的朝向(门面垂直于通过方向);Boss 本体与 VDPortal 弹幕共用
        /// </summary>
        public static void DrawPortalAt(Vector2 worldPos, float openness, float rotation, float baseSize, Vector2 screenPos, float glowMult = 1f)
        {
            float size = baseSize * openness;
            float xmul = 0.42f;
            float angle = Main.GlobalTimeWrappedHourly * 1.6f;
            Vector2 lu = new Vector2(size, 0).RotatedBy(angle - MathHelper.ToRadians(135));
            Vector2 ru = new Vector2(size, 0).RotatedBy(angle - MathHelper.ToRadians(45));
            Vector2 ld = new Vector2(size, 0).RotatedBy(angle + MathHelper.ToRadians(135));
            Vector2 rd = new Vector2(size, 0).RotatedBy(angle + MathHelper.ToRadians(45));
            lu.X *= xmul;
            ru.X *= xmul;
            ld.X *= xmul;
            rd.X *= xmul;
            lu = lu.RotatedBy(rotation);
            ru = ru.RotatedBy(rotation);
            ld = ld.RotatedBy(rotation);
            rd = rd.RotatedBy(rotation);
            Vector2 dp = worldPos - screenPos;
            Color inner = VDVfx.VoidDeep * MathHelper.Clamp(openness * 1.2f, 0f, 1f);
            CEUtils.drawTextureToPoint(Main.spriteBatch, CEUtils.getExtraTex("SoulVortex"), inner, dp + lu, dp + ru, dp + ld, dp + rd);

            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 ringScale = new Vector2(size * xmul * 2.6f / glow.Width, size * 2.6f / glow.Height);
            Main.spriteBatch.Draw(glow, dp, null, VDVfx.VoidPurple * (0.55f * openness * glowMult), rotation, glow.Size() / 2f, ringScale, SpriteEffects.None, 0f);
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Vector2 rimScale = new Vector2(size * xmul * 2.9f / ring.Width, size * 2.9f / ring.Height);
            Main.spriteBatch.Draw(ring, dp, null, new Color(230, 150, 255) * (0.9f * openness * glowMult), rotation, ring.Size() / 2f, rimScale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
        }
        #endregion

        #region 限制圈
        private void DrawArenaRing(Vector2 screenPos)
        {
            //只画屏幕附近的弧段,96 段折线,加法淡紫
            const int segments = 96;
            Rectangle view = new Rectangle((int)screenPos.X - 200, (int)screenPos.Y - 200, Main.screenWidth + 400, Main.screenHeight + 400);
            float pulse = 0.55f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f);
            Color c = new Color(200, 120, 255) * pulse;
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < segments; i++)
            {
                float a0 = MathHelper.TwoPi * i / segments;
                float a1 = MathHelper.TwoPi * (i + 1) / segments;
                Vector2 p0 = NPC.Center + a0.ToRotationVector2() * VDDirector.ArenaRadius;
                Vector2 p1 = NPC.Center + a1.ToRotationVector2() * VDDirector.ArenaRadius;
                if (!view.Contains((int)p0.X, (int)p0.Y) && !view.Contains((int)p1.X, (int)p1.Y))
                {
                    continue;
                }
                CEUtils.drawLineBetter(p0, p1, c, 26f, 8);
                CEUtils.drawLine(p0, p1, Color.White * (pulse * 0.7f), 3f);
            }
            CEUtils.ReSetToEndShader();
        }
        #endregion

        #region 二阶段部件
        private void DrawWings(Vector2 screenPos, float scale)
        {
            if (wingTex == null)
            {
                return;
            }
            Texture2D tex = wingTex.Value;
            Vector2 pivot = NPC.Center + WingPivotOffset * scale;
            float radius = (WingBaseRadius + WingExpandRadius * WingExpand) * scale;
            Color color = Color.White * (WingAlpha * Alpha);
            for (int i = 0; i < 4; i++)
            {
                float ang = WingRotation + MathHelper.PiOver4 + MathHelper.PiOver2 * i;
                Vector2 pos = pivot + ang.ToRotationVector2() * radius;
                //贴图竖直、能量核在下端,让核朝向环心
                float rot = ang - MathHelper.PiOver2;
                Main.spriteBatch.Draw(tex, pos - screenPos, null, color, rot, tex.Size() / 2f, scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            for (int i = 0; i < 4; i++)
            {
                float ang = WingRotation + MathHelper.PiOver4 + MathHelper.PiOver2 * i;
                Vector2 pos = pivot + ang.ToRotationVector2() * (radius - 24f * scale);
                Main.spriteBatch.Draw(glow, pos - screenPos, null, VDVfx.VoidPurple * (0.5f * WingAlpha * Alpha), 0f, glow.Size() / 2f, 0.22f * scale * (1f + WingExpand * 0.4f), SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();
        }

        /// <summary>底部紫色火焰喷吐:加法锥形光 + 脉动,粒子由 AI 侧补</summary>
        private void DrawBottomFlame(Vector2 screenPos, float scale)
        {
            Texture2D cone = CEUtils.getExtraTex("GlowCone");
            Vector2 basePos = NPC.Center + new Vector2(0, 30f) * scale;
            float flicker = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 23f);
            Main.spriteBatch.UseAdditive();
            //GlowCone 尖端在左,旋转 -90° 让尖端朝上、锥体向下张开
            for (int i = 0; i < 3; i++)
            {
                float wobble = (float)Math.Sin(Main.GlobalTimeWrappedHourly * (9f + i * 4f) + i) * 0.12f;
                Vector2 sc = new Vector2(0.18f + i * 0.05f, 0.5f - i * 0.1f) * scale * flicker;
                Color c = Color.Lerp(VDVfx.VoidPurple, VDVfx.VoidPink, i * 0.35f) * (0.55f * WingAlpha * Alpha);
                Main.spriteBatch.Draw(cone, basePos - screenPos, null, c, MathHelper.PiOver2 + wobble, new Vector2(0, cone.Height / 2f), sc, SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();
        }
        #endregion

        #region 核心光与护盾
        /// <summary>红色地狱:红射线发射前的竖直预警线,随蓄力变亮变粗</summary>
        private void DrawRedRayWarning(Vector2 screenPos)
        {
            float warn = Context.RedRayWarning;
            if (warn <= 0f)
            {
                return;
            }
            Vector2 start = CorePos;
            Vector2 end = start + new Vector2(0, VDDirector.RedHellRayLength);
            Color c = new Color(255, 80, 80) * (0.3f + 0.6f * warn);
            Main.spriteBatch.UseAdditive();
            CEUtils.drawLineBetter(start, end, c, 6f + 14f * warn);
            CEUtils.drawLine(start, end, Color.White * (0.5f * warn), 2f);
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, start - screenPos, null, new Color(255, 90, 90) * warn, 0f, glow.Size() / 2f, 0.3f + 0.3f * warn, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
        }

        /// <summary>导引线:主炮扫射起点的细线预告,越近出手越亮、越粗,末尾闪烁</summary>
        private void DrawAimLine(Vector2 screenPos)
        {
            float s = Context.AimLineStrength;
            if (s <= 0f || Context.AimLineDir == Vector2.Zero)
            {
                return;
            }
            Vector2 dir = Context.AimLineDir.SafeNormalize(Vector2.UnitY);
            Vector2 start = CorePos;
            Vector2 end = start + dir * VDDirector.CannonBeamLength;
            float flicker = s > 0.75f ? 0.75f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 60f) : 1f;
            Color c = Context.AimLineColor * (0.25f + 0.65f * s) * flicker;
            Main.spriteBatch.UseAdditive();
            CEUtils.drawLineBetter(start, end, c, 4f + 10f * s);
            CEUtils.drawLine(start, end, Color.White * (0.6f * s * flicker), 1.5f + s);
            CEUtils.ReSetToEndShader();
        }

        private void DrawCoreGlow(Vector2 screenPos, float scale)
        {
            if (Phase < 2 && CoreGlow <= 0.01f)
            {
                return;
            }
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 pos = NPC.Center + CoreOffset.RotatedBy(NPC.rotation) * scale;
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f);
            float strength = (Phase >= 2 ? 0.55f : 0f) + CoreGlow * 0.9f;
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glow, pos - screenPos, null, CoreColor * (strength * pulse * Alpha), 0f, glow.Size() / 2f, (0.32f + CoreGlow * 0.25f) * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, pos - screenPos, null, Color.White * (strength * 0.5f * Alpha), 0f, glow.Size() / 2f, (0.14f + CoreGlow * 0.1f) * scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
        }

        /// <summary>三阶段外围淡紫防护盾</summary>
        private void DrawShield(Vector2 screenPos, float scale)
        {
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Texture2D glow = CEUtils.getExtraTex("Glow");
            float pulse = 0.75f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            float ringScale = 2.1f * scale * (1f + 0.03f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f));
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glow, NPC.Center - screenPos, null, new Color(170, 110, 255) * (0.35f * ShieldAlpha * pulse * Alpha), 0f, glow.Size() / 2f, ringScale * 0.75f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, NPC.Center - screenPos, null, new Color(210, 160, 255) * (0.8f * ShieldAlpha * pulse * Alpha), Main.GlobalTimeWrappedHourly * 0.6f, ring.Size() / 2f, ringScale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
        }
        #endregion
    }
}
