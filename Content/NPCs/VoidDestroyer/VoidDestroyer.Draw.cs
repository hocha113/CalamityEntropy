using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using CalamityEntropy.Core.Graphics;
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

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            if (NPC.IsABestiaryIconDummy || Context == null) {
                spriteBatch.Draw(bodyTex, NPC.Center - screenPos, null, drawColor, 0f, bodyTex.Size() / 2f, NPC.scale, SpriteEffects.None, 0f);
                return false;
            }

            if (Context.PortalOpenness > 0.01f) {
                DrawPortalAt(Context.AnchorPos, Context.PortalOpenness, 0f, 190f, screenPos);
            }
            if (Context.ArenaActive && !Context.Dying) {
                DrawArenaRing(screenPos);
            }

            if (Alpha <= 0.01f) {
                return false;
            }

            Texture2D tex = bodyTex;
            Rectangle? frame = null;
            if (Context.TransformFrame >= 0 && transformTex != null) {
                tex = transformTex.Value;
                frame = new Rectangle(0, Math.Clamp(Context.TransformFrame, 0, 5) * TransformFramePitch, tex.Width, TransformFrameHeight);
            }
            else if (Phase >= 2 && p2Tex != null) {
                tex = p2Tex.Value;
            }
            Vector2 origin = frame.HasValue ? frame.Value.Size() / 2f : tex.Size() / 2f;

            //假 Z:退入背景时缩小、冷色、略透明(判定位置不动,接触窗由宿主关)
            float depthScale = MathHelper.Lerp(1f, VDDirector.OrbitalFarScale, FakeZ);
            float scale = NPC.scale * DrawScale * depthScale;
            Color bodyColor = Color.Lerp(drawColor, FarTint, FakeZ * 0.65f) * (Alpha * MathHelper.Lerp(1f, 0.8f, FakeZ));

            //底部火焰与能量翼画在本体后面
            if (WingAlpha > 0.01f) {
                DrawBottomFlame(screenPos, scale);
                DrawWings(screenPos, scale);
            }

            DrawSpeedGhosts(tex, frame, origin, scale, screenPos);

            //描边两遍夹住本体:外扩光晕垫在下面,贴边内缘锐光压在上面
            DrawRimUnder(tex, frame, origin, scale, screenPos);
            spriteBatch.Draw(tex, NPC.Center - screenPos, frame, bodyColor, NPC.rotation, origin, scale, SpriteEffects.None, 0f);
            DrawRimOver(tex, frame, origin, scale, screenPos);

            DrawRedRayWarning(screenPos);
            DrawAimLine(screenPos);
            DrawCoreGlow(screenPos, scale);
            if (ShieldAlpha > 0.01f) {
                DrawShield(screenPos, scale);
            }
            return false;
        }

        #region 残影
        /// <summary>高速残影:速度门控(≥18px/f 才出现),走全息着色器的紫色扫描线投影,不是简单的半透明贴图</summary>
        private void DrawSpeedGhosts(Texture2D tex, Rectangle? frame, Vector2 origin, float scale, Vector2 screenPos) {
            float speed = NPC.velocity.Length();
            float intensity = MathHelper.Clamp((speed - 18f) / 22f, 0f, 1f);
            if (intensity <= 0.02f || NPC.oldPos == null) {
                return;
            }
            VDHologramDraw.Begin();
            for (int i = 1; i < NPC.oldPos.Length; i++) {
                if (NPC.oldPos[i] == Vector2.Zero) {
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
        /// <summary>椭圆短轴 / 长轴之比:门面朝 rotation 方向,侧视成窄椭圆</summary>
        private const float PortalAspect = 0.42f;
        /// <summary>环折线段数:40 段在 400px 周长上每段 10px,肉眼是平滑曲线</summary>
        private const int PortalRimSegments = 40;
        /// <summary>
        /// 旋流盘四边形全宽 / 椭圆长轴半径。着色器里吸积盘的实亮外沿在 r=0.55(r=1 为四边形半宽),
        /// 3.1 让实亮沿落在 0.85 倍长轴、正好贴着环内侧,0.55~1 的渐散尾巴溢出环外成一圈软雾
        /// </summary>
        private const float PortalDiskQuadMult = 3.1f;

        /// <summary>
        /// 椭圆形虚空传送门。盘体走 VDSingularity 着色器(极坐标旋流 + 暗核)画在白方块上再按椭圆压扁转向,
        /// 着色器缺失时退回 SoulVortex 四点贴图;环用等宽折线沿椭圆周长画(紫宽层 + 白细层 + 一段转动的亮弧),
        /// 背光用 Glow 径向渐变。这里刻意不用 BloomRing 压椭圆:灰度环贴图非等比缩放会得到两侧薄、上下厚的歪光圈。
        /// rotation 是椭圆短轴的朝向(门面垂直于通过方向);glowMult 只放大白层亮度(幻影舰队真身门的破绽)。
        /// Boss 本体与 VDPortal 弹幕共用
        /// </summary>
        public static void DrawPortalAt(Vector2 worldPos, float openness, float rotation, float baseSize, Vector2 screenPos, float glowMult = 1f) {
            float size = baseSize * openness;
            if (size < 2f)
            {
                return;
            }
            Vector2 dp = worldPos - screenPos;
            Effect shader = CEEffectAssets.VDSingularity?.Value;
            Texture2D quad = CEExtraAssets.white ?? CEUtils.getExtraTex("white");

            //背光:径向渐变可以压椭圆(没有环线可被扭曲),亮度压低只做衬底
            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 glowScale = new Vector2(size * PortalAspect * 2.4f / glow.Width, size * 2.4f / glow.Height);
            Main.spriteBatch.Draw(glow, dp, null, VDVfx.VoidPurple * (0.3f * openness), rotation, glow.Size() / 2f, glowScale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            //盘体:旋流盘,四边形按椭圆压扁,旋流随之斜视
            if (shader != null && quad != null)
            {
                Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
                Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
                Main.instance.GraphicsDevice.Textures[1] = noise;
                Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                shader.Parameters["uColor"]?.SetValue(VDVfx.VoidPurple.ToVector3());
                shader.Parameters["uColor2"]?.SetValue(VDVfx.VoidWhite.ToVector3());
                shader.Parameters["uCoreRadius"]?.SetValue(0.12f);
                shader.Parameters["uOpacity"]?.SetValue(MathHelper.Clamp(openness * 1.2f, 0f, 1f));
                shader.Parameters["uSpin"]?.SetValue(2.2f);
                shader.CurrentTechnique.Passes[0].Apply();
                Vector2 quadScale = new Vector2(size * PortalAspect * PortalDiskQuadMult / quad.Width, size * PortalDiskQuadMult / quad.Height);
                Main.spriteBatch.Draw(quad, dp, null, Color.White, rotation, quad.Size() / 2f, quadScale, SpriteEffects.None, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }
            else
            {
                float angle = Main.GlobalTimeWrappedHourly * 1.6f;
                Vector2 lu = new Vector2(size, 0).RotatedBy(angle - MathHelper.ToRadians(135));
                Vector2 ru = new Vector2(size, 0).RotatedBy(angle - MathHelper.ToRadians(45));
                Vector2 ld = new Vector2(size, 0).RotatedBy(angle + MathHelper.ToRadians(135));
                Vector2 rd = new Vector2(size, 0).RotatedBy(angle + MathHelper.ToRadians(45));
                lu.X *= PortalAspect;
                ru.X *= PortalAspect;
                ld.X *= PortalAspect;
                rd.X *= PortalAspect;
                Color inner = VDVfx.VoidDeep * MathHelper.Clamp(openness * 1.2f, 0f, 1f);
                CEUtils.drawTextureToPoint(Main.spriteBatch, CEUtils.getExtraTex("SoulVortex"), inner,
                    dp + lu.RotatedBy(rotation), dp + ru.RotatedBy(rotation), dp + ld.RotatedBy(rotation), dp + rd.RotatedBy(rotation));
            }

            //环:等宽折线沿椭圆周长,紫宽层 + 白细层;一段亮弧绕环转动
            Main.spriteBatch.UseAdditive();
            float glint = Main.GlobalTimeWrappedHourly * 2.4f;
            Vector2 prev = worldPos + PortalRimPoint(0f, size, rotation);
            for (int i = 1; i <= PortalRimSegments; i++)
            {
                float t = MathHelper.TwoPi * i / PortalRimSegments;
                Vector2 next = worldPos + PortalRimPoint(t, size, rotation);
                //亮弧:与转动相位的角距在 0.6 弧度内渐亮
                float glintDist = Math.Abs(MathHelper.WrapAngle(t - glint));
                float hot = MathHelper.Clamp(1f - glintDist / 0.6f, 0f, 1f);
                CEUtils.drawLine(prev, next, VDVfx.VoidPurple * (0.45f * openness), 6f, 1);
                CEUtils.drawLine(prev, next, Color.White * ((0.55f + 0.45f * hot) * openness * glowMult), 2f, 1);
                prev = next;
            }
            CEUtils.ReSetToEndShader();
        }

        /// <summary>椭圆周长上的点:短轴沿 rotation(半径 size·Aspect),长轴垂直(半径 size)</summary>
        private static Vector2 PortalRimPoint(float t, float size, float rotation)
        {
            Vector2 p = new Vector2((float)Math.Cos(t) * size * PortalAspect, (float)Math.Sin(t) * size);
            return p.RotatedBy(rotation);
        }
        #endregion

        #region 限制圈
        private void DrawArenaRing(Vector2 screenPos) {
            //只画屏幕附近的弧段,96 段折线,加法淡紫
            const int segments = 96;
            Rectangle view = new Rectangle((int)screenPos.X - 200, (int)screenPos.Y - 200, Main.screenWidth + 400, Main.screenHeight + 400);
            float pulse = 0.55f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f);
            Color c = new Color(200, 120, 255) * pulse;
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < segments; i++) {
                float a0 = MathHelper.TwoPi * i / segments;
                float a1 = MathHelper.TwoPi * (i + 1) / segments;
                Vector2 p0 = NPC.Center + a0.ToRotationVector2() * VDDirector.ArenaRadius;
                Vector2 p1 = NPC.Center + a1.ToRotationVector2() * VDDirector.ArenaRadius;
                if (!view.Contains((int)p0.X, (int)p0.Y) && !view.Contains((int)p1.X, (int)p1.Y)) {
                    continue;
                }
                CEUtils.drawLineBetter(p0, p1, c, 26f, 8);
                CEUtils.drawLine(p0, p1, Color.White * (pulse * 0.7f), 3f);
            }
            CEUtils.ReSetToEndShader();
        }
        #endregion

        #region 二阶段部件
        private void DrawWings(Vector2 screenPos, float scale) {
            if (wingTex == null) {
                return;
            }
            Texture2D tex = wingTex.Value;
            Vector2 pivot = NPC.Center + WingPivotOffset * scale;
            float radius = (WingBaseRadius + WingExpandRadius * WingExpand) * scale;
            Color color = Color.White * (WingAlpha * Alpha);
            for (int i = 0; i < 4; i++) {
                float ang = WingRotation + MathHelper.PiOver4 + MathHelper.PiOver2 * i;
                Vector2 pos = pivot + ang.ToRotationVector2() * radius;
                //贴图竖直、能量核在下端,让核朝向环心
                float rot = ang - MathHelper.PiOver2;
                Main.spriteBatch.Draw(tex, pos - screenPos, null, color, rot, tex.Size() / 2f, scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            for (int i = 0; i < 4; i++) {
                float ang = WingRotation + MathHelper.PiOver4 + MathHelper.PiOver2 * i;
                Vector2 pos = pivot + ang.ToRotationVector2() * (radius - 24f * scale);
                Main.spriteBatch.Draw(glow, pos - screenPos, null, VDVfx.VoidPurple * (0.5f * WingAlpha * Alpha), 0f, glow.Size() / 2f, 0.22f * scale * (1f + WingExpand * 0.4f), SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();
        }

        /// <summary>底部紫色火焰喷吐:加法锥形光 + 脉动,粒子由 AI 侧补</summary>
        private void DrawBottomFlame(Vector2 screenPos, float scale) {
            Texture2D cone = CEUtils.getExtraTex("GlowCone");
            Vector2 basePos = NPC.Center + new Vector2(0, 30f) * scale;
            float flicker = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 23f);
            Main.spriteBatch.UseAdditive();
            //GlowCone 尖端在左,旋转 -90° 让尖端朝上、锥体向下张开
            for (int i = 0; i < 3; i++) {
                float wobble = (float)Math.Sin(Main.GlobalTimeWrappedHourly * (9f + i * 4f) + i) * 0.12f;
                Vector2 sc = new Vector2(0.18f + i * 0.05f, 0.5f - i * 0.1f) * scale * flicker;
                Color c = Color.Lerp(VDVfx.VoidPurple, VDVfx.VoidPink, i * 0.35f) * (0.55f * WingAlpha * Alpha);
                Main.spriteBatch.Draw(cone, basePos - screenPos, null, c, MathHelper.PiOver2 + wobble, new Vector2(0, cone.Height / 2f), sc, SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();
        }
        #endregion

        #region 描边
        /// <summary>描边整体亮度:强度与爆闪相加封顶,再随本体透明度与退入背景衰减</summary>
        private float RimOpacity() {
            return MathHelper.Clamp(RimGlow + RimFlash, 0f, 1f) * Alpha * (1f - FakeZ * 0.5f);
        }

        /// <summary>蓄力热度:强度过阈值起线性升到 1</summary>
        private float RimHeat() {
            return MathHelper.Clamp((RimGlow - VDDirector.RimHeatStart) / (1f - VDDirector.RimHeatStart), 0f, 1f);
        }

        /// <summary>噪声侵蚀比例:常态碎成丝、蓄力收成实心带,爆闪归零整圈实心;过热风格常态就几乎不侵蚀</summary>
        private float RimErode(VDRimStyle style) {
            float erode = style == VDRimStyle.Overheat
                ? VDDirector.RimErodeOverheat
                : MathHelper.Lerp(VDDirector.RimErodeIdle, VDDirector.RimErodeCharge, RimGlow);
            return erode * (1f - RimFlash);
        }

        /// <summary>
        /// 外扩半径(px,已乘绘制缩放)。逸散/拖尾/过热随强度外扩;塌缩反着来:蓄力声明越满半径越贴边,
        /// 读成能量被吸回,声明一停(奇点放出)半径弹回。爆闪一律再往外炸一圈
        /// </summary>
        private float RimRadius(VDRimStyle style, float scale) {
            float radius;
            if (style == VDRimStyle.Collapse) {
                radius = MathHelper.Lerp(VDDirector.RimBaseRadius + VDDirector.RimChargeRadius, VDDirector.RimBaseRadius * VDDirector.RimCollapseMin, Context.RimCharge);
            }
            else {
                radius = VDDirector.RimBaseRadius + VDDirector.RimChargeRadius * RimGlow;
            }
            return (radius + VDDirector.RimFlashRadius * RimFlash) * scale;
        }

        /// <summary>
        /// 叠画第 i 抽的屏幕偏移:绕圈匀布并随时间慢转,奇偶抽交替取全半径 / 半半径,让光晕由内向外有一层衰减;
        /// 拖尾风格再把整圈沿速度反向抹开,速度越快抹得越长,与高速残影叠成一条
        /// </summary>
        private Vector2 RimTapOffset(VDRimStyle style, int i, float radius, float scale) {
            float ang = Main.GlobalTimeWrappedHourly * VDDirector.RimSpin + MathHelper.TwoPi * i / VDDirector.RimTaps;
            float rad = (i % 2 == 0) ? radius : radius * 0.55f;
            Vector2 ofs = ang.ToRotationVector2() * rad;
            if (style == VDRimStyle.Streak) {
                float speed = NPC.velocity.Length();
                float ratio = MathHelper.Clamp(speed / VDDirector.RimStreakFullSpeed, 0f, 1f);
                Vector2 back = NPC.velocity.SafeNormalize(Vector2.Zero) * -1f;
                ofs += back * (VDDirector.RimStreakLength * ratio * scale * (i + 0.5f) / VDDirector.RimTaps);
            }
            return ofs;
        }

        /// <summary>过热风格出手前后的高频闪烁,其余风格恒 1</summary>
        private static float RimFlicker(VDRimStyle style) {
            if (style != VDRimStyle.Overheat) {
                return 1f;
            }
            return 1f + VDDirector.RimOverheatFlickerAmp * (float)Math.Sin(Main.GlobalTimeWrappedHourly * VDDirector.RimOverheatFlickerSpeed);
        }

        /// <summary>喂描边着色器的全部参数并 Apply;每抽换一个噪声相位,六份叠画不是六份复印</summary>
        private void ApplyRimShader(Effect shader, Texture2D tex, VDRimStyle style, int tap) {
            Color hot = Color.Lerp(RimHotColor, Color.White, RimFlash * VDDirector.RimFlashWhiten);
            Vector2 scroll = VDDirector.RimNoiseScroll * Main.GlobalTimeWrappedHourly + new Vector2(tap * 0.173f, tap * 0.291f);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uOpacity"]?.SetValue(RimOpacity() * RimFlicker(style));
            shader.Parameters["uColor"]?.SetValue(RimColor.ToVector3());
            shader.Parameters["uHotColor"]?.SetValue(hot.ToVector3());
            shader.Parameters["uHeat"]?.SetValue(RimHeat());
            shader.Parameters["uFlash"]?.SetValue(RimFlash);
            shader.Parameters["uErode"]?.SetValue(RimErode(style));
            shader.Parameters["uNoiseScroll"]?.SetValue(scroll);
            shader.Parameters["uImageSize"]?.SetValue(tex.Size());
            shader.CurrentTechnique.Passes[0].Apply();
        }

        /// <summary>
        /// 外扩光晕(垫在本体之下):同一遍描边着色器在屏幕空间偏移叠画 RimTaps 次,每份只露出被本体挡不住的那一弯,
        /// 合起来是一圈由内向外衰减、随噪声碎成丝的逸散光。本体贴图四边只有 2~4px 留白,着色器往外膨胀会被裁,
        /// 所以外扩只能靠独立四边形往外挪(Apsychos.DrawOutLine / AcropolisMachine.DrawHarpoonOutline 同款做法)。
        /// 着色器缺失时退回 WhiteTrans 平色叠画,功能不丢
        /// </summary>
        private void DrawRimUnder(Texture2D tex, Rectangle? frame, Vector2 origin, float scale, Vector2 screenPos) {
            float opacity = RimOpacity();
            if (opacity <= 0.01f) {
                return;
            }
            VDRimStyle style = VDDirector.RimStyleFor(CurrentStateIndex);
            float radius = RimRadius(style, scale);
            Vector2 center = NPC.Center - screenPos;
            Effect shader = CEEffectAssets.VDRimLight?.Value;

            if (shader != null) {
                Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive, shader);
                Main.instance.GraphicsDevice.Textures[1] = noise;
                Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                for (int i = 0; i < VDDirector.RimTaps; i++) {
                    ApplyRimShader(shader, tex, style, i);
                    //顶点色整体乘在着色器输出上:各抽的亮度分摊直接走它,远抽再暗一档
                    float tapAlpha = VDDirector.RimTapOpacity * ((i % 2 == 0) ? 0.8f : 1f);
                    Main.spriteBatch.Draw(tex, center + RimTapOffset(style, i, radius, scale), frame, Color.White * tapAlpha, NPC.rotation, origin, scale, SpriteEffects.None, 0f);
                }
                Main.spriteBatch.ExitShaderRegion();
                return;
            }

            Effect white = CEEffectAssets.WhiteTrans;
            if (white == null) {
                return;
            }
            white.Parameters["strength"].SetValue(1f);
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive, white);
            white.CurrentTechnique.Passes[0].Apply();
            Color flat = Color.Lerp(RimColor, RimHotColor, RimHeat()) * (opacity * VDDirector.RimTapOpacity * (1f + RimFlash));
            for (int i = 0; i < VDDirector.RimTaps; i++) {
                Main.spriteBatch.Draw(tex, center + RimTapOffset(style, i, radius, scale), frame, flat, NPC.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.ExitShaderRegion();
        }

        /// <summary>贴边内缘锐光(压在本体之上):零偏移画一遍描边着色器,光像是从机体表面漏出来。无着色器时不画(平色会盖住本体)</summary>
        private void DrawRimOver(Texture2D tex, Rectangle? frame, Vector2 origin, float scale, Vector2 screenPos) {
            if (RimOpacity() <= 0.01f) {
                return;
            }
            Effect shader = CEEffectAssets.VDRimLight?.Value;
            if (shader == null) {
                return;
            }
            VDRimStyle style = VDDirector.RimStyleFor(CurrentStateIndex);
            Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive, shader);
            Main.instance.GraphicsDevice.Textures[1] = noise;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            ApplyRimShader(shader, tex, style, VDDirector.RimTaps);
            Main.spriteBatch.Draw(tex, NPC.Center - screenPos, frame, Color.White * VDDirector.RimEdgeOpacity, NPC.rotation, origin, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
        }
        #endregion

        #region 核心光与护盾
        /// <summary>红色地狱:红射线发射前的竖直预警线,随蓄力变亮变粗</summary>
        private void DrawRedRayWarning(Vector2 screenPos) {
            float warn = Context.RedRayWarning;
            if (warn <= 0f) {
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
        private void DrawAimLine(Vector2 screenPos) {
            float s = Context.AimLineStrength;
            if (s <= 0f || Context.AimLineDir == Vector2.Zero) {
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

        private void DrawCoreGlow(Vector2 screenPos, float scale) {
            if (Phase < 2 && CoreGlow <= 0.01f) {
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
        private void DrawShield(Vector2 screenPos, float scale) {
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
