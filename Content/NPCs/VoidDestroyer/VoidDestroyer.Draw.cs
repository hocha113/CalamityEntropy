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
    /// 绘制层(纯本地,只读 Context 与宿主的视觉推导量):传送门、限制圈、本体(变形帧/全息残影)、
    /// 能量翼、底部火焰、导引光锥、核心光、护盾环。抖动偏移由原版 netOffset 承担,这里不再叠位置。
    /// 纵深:所有子绘制都吃一次算好的「投影中心 + 透视缩放」;按 <see cref="VDDepth.LayerOf"/> 分路,
    /// 远景层(墙后物块前)由 <see cref="VDDepthRenderHandle"/> 在 DrawBeforeTiles 消费 <see cref="VDDepthStage"/> 的登记、调 <see cref="IVDDepthDrawable.DrawDepthFar"/>,
    /// 近景层走原版 overPlayers 缓存,平面层走原版 DrawNPCs;本体隐藏时平面装饰(限制圈 / 门 / 俯冲落点环)由标记阶段补画
    /// </summary>
    public partial class VoidDestroyer : IVDDepthDrawable
    {
        /// <summary>核心相对贴图中心的偏移(二阶段能量核心的位置)</summary>
        private static readonly Vector2 CoreOffset = new Vector2(0, 4);
        /// <summary>能量翼环绕中心相对本体中心的偏移</summary>
        private static readonly Vector2 WingPivotOffset = new Vector2(0, -8);
        private const float WingBaseRadius = 150f;
        private const float WingExpandRadius = 70f;
        private const int TransformFrameHeight = 122;
        private const int TransformFramePitch = 124;

        float IVDDepthDrawable.DepthZ => Depth;

        /// <summary>投影后的本体中心(世界坐标);绘制层与弹幕的表现都用它</summary>
        public Vector2 ProjectedCenter => VDDepth.Project(NPC.Center, Depth);
        /// <summary>投影后的核心位置(世界坐标),供瞄准线 / 光锥等表现取起点</summary>
        public Vector2 ProjectedCorePos => ProjectedCenter + CoreOffset.RotatedBy(NPC.rotation) * (NPC.scale * DrawScale * VDDepth.Scale(Depth));

        #region 分层
        /// <summary>每个绘制帧的分路:远 → hide 并登记远景表;近 → hide 并进 overPlayers 缓存;平面 → 原版 DrawNPCs。隐藏时另登记标记表补画平面装饰</summary>
        public override void DrawBehind(int index) {
            if (NPC.IsABestiaryIconDummy || Context == null) {
                NPC.hide = false;
                drawLayer = VDDepthLayer.Plane;
                return;
            }
            drawLayer = VDDepth.LayerOf(Depth);
            Vector2 projected = VDDepth.Project(NPC.Center + NPC.netOffset, Depth);
            if (drawLayer == VDDepthLayer.Far && VDDepth.FarLayerUsable(projected)) {
                NPC.hide = true;
                VDDepthStage.RegisterFar(this);
            }
            else if (drawLayer == VDDepthLayer.Near) {
                NPC.hide = true;
                Main.instance.DrawCacheNPCsOverPlayers.Add(index);
            }
            else {
                drawLayer = VDDepthLayer.Plane;
                NPC.hide = false;
            }
            if (NPC.hide || Context.DiveMarkerProgress > 0f) {
                VDDepthStage.RegisterMarker(this);
            }
        }

        /// <summary>远景层:批次活跃(Deferred / AlphaBlend / Transform);缓存路径原版不加 netOffset,这里自己加上</summary>
        void IVDDepthDrawable.DrawDepthFar(SpriteBatch spriteBatch) {
            if (Context == null || Alpha <= 0.01f) {
                return;
            }
            DrawBodyStack(spriteBatch, Main.screenPosition, NPC.Center + NPC.netOffset, Color.White);
        }

        /// <summary>标记阶段(实心物块之后、NPC 之前):本体隐藏时补画平面装饰,再画俯冲落点大环</summary>
        void IVDDepthDrawable.DrawDepthMarker(SpriteBatch spriteBatch) {
            if (Context == null) {
                return;
            }
            if (NPC.hide) {
                DrawPlaneDecor(Main.screenPosition);
            }
            if (Context.DiveMarkerProgress > 0f) {
                VDVfx.DrawDepthMarker(Context.DiveMarkerPos, Context.DiveMarkerProgress, VDDirector.DiveMarkerRadius, VDVfx.VoidPurple, 0.9f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            if (NPC.IsABestiaryIconDummy || Context == null) {
                spriteBatch.Draw(bodyTex, NPC.Center - screenPos, null, drawColor, 0f, bodyTex.Size() / 2f, NPC.scale, SpriteEffects.None, 0f);
                return false;
            }

            //平面层由原版 DrawNPCs 调,position 已含 netOffset;近景层由 overPlayers 缓存调,不含,补上
            bool cached = drawLayer == VDDepthLayer.Near;
            if (!cached) {
                DrawPlaneDecor(screenPos);
            }
            if (Alpha <= 0.01f) {
                return false;
            }
            Vector2 worldCenter = cached ? NPC.Center + NPC.netOffset : NPC.Center;
            DrawBodyStack(spriteBatch, screenPos, worldCenter, drawColor);
            return false;
        }

        /// <summary>平面装饰:传送门(画在锚点,按门的深度投影缩小)、限制圈(圆心是钉住的 ArenaCenter)</summary>
        private void DrawPlaneDecor(Vector2 screenPos) {
            if (Context.PortalOpenness > 0.01f) {
                float portalScale = VDDepth.Scale(Context.PortalDepth);
                DrawPortalAt(VDDepth.Project(Context.AnchorPos, Context.PortalDepth), Context.PortalOpenness, 0f, 190f * portalScale, screenPos);
            }
            if (Context.ArenaActive && !Context.Dying) {
                DrawArenaRing(screenPos);
            }
        }

        /// <summary>本体整叠:火焰 / 翼 → 残影 → 描边下层 → 本体 → 描边上层 → 预警线 / 导引线 → 核心光 → 护盾。全部吃投影中心与透视缩放</summary>
        private void DrawBodyStack(SpriteBatch spriteBatch, Vector2 screenPos, Vector2 worldCenter, Color drawColor) {
            Texture2D tex = TextureAssets.Npc[Type].Value;
            Rectangle? frame = null;
            if (Context.TransformFrame >= 0 && transformTex != null) {
                tex = transformTex.Value;
                frame = new Rectangle(0, Math.Clamp(Context.TransformFrame, 0, 5) * TransformFramePitch, tex.Width, TransformFrameHeight);
            }
            else if (Phase >= 2 && p2Tex != null) {
                tex = p2Tex.Value;
            }
            Vector2 origin = frame.HasValue ? frame.Value.Size() / 2f : tex.Size() / 2f;

            float depthScale = VDDepth.Scale(Depth);
            float scale = NPC.scale * DrawScale * depthScale;
            Vector2 center = VDDepth.Project(worldCenter, Depth);
            float depthAlpha = VDDepth.Alpha(Depth);
            float alpha = Alpha * depthAlpha;

            //底部火焰与能量翼画在本体后面;护盾的后环也垫在这里,与压在本体上的前环合成一个有前后的球
            if (WingAlpha > 0.01f) {
                DrawBottomFlame(center, screenPos, scale, alpha);
                DrawWings(center, screenPos, scale, alpha);
            }
            if (ShieldAlpha > 0.01f) {
                DrawShield(center, screenPos, scale, alpha, back: true);
            }

            DrawSpeedGhosts(tex, frame, origin, scale, screenPos, alpha);

            //描边两遍夹住本体:外扩光晕垫在下面,贴边内缘锐光压在上面
            DrawRimUnder(tex, frame, origin, scale, center, screenPos);
            VDDepthDraw.Draw(tex, center - screenPos, frame, drawColor, Alpha, NPC.rotation, origin, scale, SpriteEffects.None, Depth);
            DrawRimOver(tex, frame, origin, scale, center, screenPos);

            DrawAimLine(center, screenPos, scale, alpha);
            DrawCoreGlow(center, screenPos, scale, alpha);
            if (ShieldAlpha > 0.01f) {
                DrawShield(center, screenPos, scale, alpha, back: false);
            }
        }
        #endregion

        #region 残影
        /// <summary>高速残影:速度门控(≥18px/f 才出现),走全息着色器的紫色扫描线投影,不是简单的半透明贴图;历史位置按当前深度投影</summary>
        private void DrawSpeedGhosts(Texture2D tex, Rectangle? frame, Vector2 origin, float scale, Vector2 screenPos, float alpha) {
            float speed = NPC.velocity.Length() * VDDepth.Scale(Depth);
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
                Vector2 pos = VDDepth.Project(NPC.oldPos[i] + NPC.Size / 2f, Depth) - screenPos;
                VDHologramDraw.DrawPart(tex, pos, frame, VDVfx.VoidPurple, 0.5f * fade * intensity * alpha, NPC.rotation, origin, scale * (0.96f + 0.04f * fade), SpriteEffects.None);
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
        /// worldPos 是已投影的世界坐标、baseSize 已乘透视缩放:深度门由调用方先投影再传进来。Boss 本体与 VDPortal 弹幕共用
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
            //只画屏幕附近的弧段,96 段折线,加法淡紫;圆心是钉住的 ArenaCenter(本体退入深处时不跟着飘)
            const int segments = 96;
            Vector2 arena = ArenaCenter;
            Rectangle view = new Rectangle((int)screenPos.X - 200, (int)screenPos.Y - 200, Main.screenWidth + 400, Main.screenHeight + 400);
            float pulse = 0.55f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f);
            Color c = new Color(200, 120, 255) * pulse;
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < segments; i++) {
                float a0 = MathHelper.TwoPi * i / segments;
                float a1 = MathHelper.TwoPi * (i + 1) / segments;
                Vector2 p0 = arena + a0.ToRotationVector2() * VDDirector.ArenaRadius;
                Vector2 p1 = arena + a1.ToRotationVector2() * VDDirector.ArenaRadius;
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
        private void DrawWings(Vector2 center, Vector2 screenPos, float scale, float alpha) {
            if (wingTex == null) {
                return;
            }
            Texture2D tex = wingTex.Value;
            Vector2 pivot = center + WingPivotOffset * scale;
            float radius = (WingBaseRadius + WingExpandRadius * WingExpand) * scale;
            float wingAlpha = WingAlpha * Alpha;
            for (int i = 0; i < 4; i++) {
                float ang = WingRotation + MathHelper.PiOver4 + MathHelper.PiOver2 * i;
                Vector2 pos = pivot + ang.ToRotationVector2() * radius;
                //贴图竖直、能量核在下端,让核朝向环心
                float rot = ang - MathHelper.PiOver2;
                VDDepthDraw.Draw(tex, pos - screenPos, null, Color.White, wingAlpha, rot, tex.Size() / 2f, scale, SpriteEffects.None, Depth);
            }
            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Color glowColor = VDDepth.Fog(VDVfx.VoidPurple, Depth);
            for (int i = 0; i < 4; i++) {
                float ang = WingRotation + MathHelper.PiOver4 + MathHelper.PiOver2 * i;
                Vector2 pos = pivot + ang.ToRotationVector2() * (radius - 24f * scale);
                Main.spriteBatch.Draw(glow, pos - screenPos, null, glowColor * (0.5f * WingAlpha * alpha), 0f, glow.Size() / 2f, 0.22f * scale * (1f + WingExpand * 0.4f), SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();
        }

        /// <summary>底部紫色火焰喷吐:加法锥形光 + 脉动,粒子由 AI 侧补</summary>
        private void DrawBottomFlame(Vector2 center, Vector2 screenPos, float scale, float alpha) {
            Texture2D cone = CEUtils.getExtraTex("GlowCone");
            Vector2 basePos = center + new Vector2(0, 30f) * scale;
            float flicker = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 23f);
            Main.spriteBatch.UseAdditive();
            //GlowCone 尖端在左,旋转 -90° 让尖端朝上、锥体向下张开
            for (int i = 0; i < 3; i++) {
                float wobble = (float)Math.Sin(Main.GlobalTimeWrappedHourly * (9f + i * 4f) + i) * 0.12f;
                Vector2 sc = new Vector2(0.18f + i * 0.05f, 0.5f - i * 0.1f) * scale * flicker;
                Color c = VDDepth.Fog(Color.Lerp(VDVfx.VoidPurple, VDVfx.VoidPink, i * 0.35f), Depth) * (0.55f * WingAlpha * alpha);
                Main.spriteBatch.Draw(cone, basePos - screenPos, null, c, MathHelper.PiOver2 + wobble, new Vector2(0, cone.Height / 2f), sc, SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();
        }
        #endregion

        #region 描边
        /// <summary>
        /// 描边整体亮度:强度(受静默压暗,但最多压掉 RimSuppressMax,常驻描边在任何拍子都不整圈熄灭)与爆闪(不受压)相加,
        /// 再乘总亮度倍率(加法混合下可大于 1),随本体透明度、深度衰减与远端雾化衰减
        /// </summary>
        private float RimOpacity() {
            float suppress = 1f - RimSuppress * VDDirector.RimSuppressMax;
            return (RimGlow * suppress + RimFlash) * VDDirector.RimBrightness * Alpha * VDDepth.Alpha(Depth) * (1f - VDDepth.FogAmount(Depth) * 0.5f);
        }

        /// <summary>蓄力热度:活跃度(不含底噪)过阈值起线性升到 1,常态再亮也不会发红</summary>
        private float RimHeat() {
            return MathHelper.Clamp((RimActive - VDDirector.RimHeatStart) / (1f - VDDirector.RimHeatStart), 0f, 1f);
        }

        /// <summary>贴边亮线的噪声侵蚀比例:常态基本是一条实线只轻微闪动、蓄力收得更实,爆闪归零整圈实心;过热风格几乎不侵蚀</summary>
        private float RimErode(VDRimStyle style) {
            float erode = style == VDRimStyle.Overheat
                ? VDDirector.RimErodeOverheat
                : MathHelper.Lerp(VDDirector.RimErodeIdle, VDDirector.RimErodeCharge, RimActive);
            return erode * (1f - RimFlash);
        }

        /// <summary>
        /// 光晕环的噪声侵蚀比例:外环碎成向外逸散的丝(能量感),内环实心贴身(常驻感);
        /// 随活跃度按亮线同一条斜坡收实(蓄力时整圈凝聚),爆闪归零;过热风格几乎不侵蚀
        /// </summary>
        private float RimHaloErode(VDRimStyle style, bool inner) {
            float erode = inner ? VDDirector.RimHaloErodeInner : VDDirector.RimHaloErodeOuter;
            if (style == VDRimStyle.Overheat) {
                erode = Math.Min(erode, VDDirector.RimErodeOverheat);
            }
            float shrink = MathHelper.Lerp(1f, VDDirector.RimErodeCharge / VDDirector.RimErodeIdle, RimActive);
            return erode * shrink * (1f - RimFlash);
        }

        /// <summary>
        /// 外扩半径(px,已乘绘制缩放)。逸散 / 拖尾 / 过热随活跃度外扩;塌缩反着来:蓄力声明越满半径越贴边,
        /// 读成能量被吸回,声明一停(奇点放出)半径弹回。爆闪一律再往外炸一圈
        /// </summary>
        private float RimRadius(VDRimStyle style, float scale) {
            float radius;
            if (style == VDRimStyle.Collapse) {
                radius = MathHelper.Lerp(VDDirector.RimBaseRadius + VDDirector.RimChargeRadius, VDDirector.RimBaseRadius * VDDirector.RimCollapseMin, Context.RimCharge);
            }
            else {
                radius = VDDirector.RimBaseRadius + VDDirector.RimChargeRadius * RimActive;
            }
            return (radius + VDDirector.RimFlashRadius * RimFlash) * scale;
        }

        /// <summary>拖尾风格的拖长比例 0..1:按速度占 RimStreakFullSpeed 的比例,其余风格恒 0</summary>
        private float RimStreakRatio(VDRimStyle style) {
            if (style != VDRimStyle.Streak) {
                return 0f;
            }
            return MathHelper.Clamp(NPC.velocity.Length() / VDDirector.RimStreakFullSpeed, 0f, 1f);
        }

        /// <summary>
        /// 一环里第 i 抽(共 taps 抽)的屏幕偏移:绕圈匀布、随时间慢转,整环再转 phase(内外环抽位错开,多边形顶点不重合);
        /// 拖尾风格再把整圈沿速度反向抹开,抽序越靠后拖得越远,与高速残影叠成一条
        /// </summary>
        private Vector2 RimTapOffset(VDRimStyle style, int i, int taps, float radius, float scale, float phase) {
            float ang = Main.GlobalTimeWrappedHourly * VDDirector.RimSpin + MathHelper.TwoPi * i / taps + phase;
            Vector2 ofs = ang.ToRotationVector2() * radius;
            float streak = RimStreakRatio(style);
            if (streak > 0f) {
                Vector2 back = (-NPC.velocity).SafeNormalize(Vector2.Zero);
                ofs += back * (VDDirector.RimStreakLength * streak * scale * (i + 0.5f) / taps);
            }
            return ofs;
        }

        /// <summary>一环里第 i 抽的亮度分摊:拖尾时越靠后的抽越暗,尾巴是渐隐的</summary>
        private float RimTapAlpha(VDRimStyle style, int i, int taps, float tapOpacity) {
            float streak = RimStreakRatio(style);
            return tapOpacity * (1f - 0.6f * streak * (i + 0.5f) / taps);
        }

        /// <summary>过热风格出手前后的高频闪烁,其余风格恒 1</summary>
        private static float RimFlicker(VDRimStyle style) {
            if (style != VDRimStyle.Overheat) {
                return 1f;
            }
            return 1f + VDDirector.RimOverheatFlickerAmp * (float)Math.Sin(Main.GlobalTimeWrappedHourly * VDDirector.RimOverheatFlickerSpeed);
        }

        /// <summary>当前绘制帧在整张贴图里的 UV 中心:条带贴图按帧算,整图为 0.5</summary>
        private static Vector2 RimFrameCenter(Texture2D tex, Rectangle? frame) {
            if (!frame.HasValue) {
                return new Vector2(0.5f);
            }
            Rectangle r = frame.Value;
            return new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f) / tex.Size();
        }

        /// <summary>
        /// 喂描边着色器(VDRimLight 亮线与 VDRimHalo 光晕的参数同名)的全部参数并 Apply;每抽换一个噪声相位,叠画不是复印。
        /// color 是这一遍的基色(进雾色),erode 是这一遍的侵蚀比例,opacityMult 乘在 uOpacity 上(顶点色是字节会被钳到 1,大于 1 的亮度只能走这里)
        /// </summary>
        private void ApplyRimShader(Effect shader, Texture2D tex, Rectangle? frame, VDRimStyle style, int tap, Color color, float erode, float opacityMult) {
            Color hot = Color.Lerp(RimHotColor, Color.White, RimFlash * VDDirector.RimFlashWhiten);
            Vector2 scroll = RimDirScroll + new Vector2(tap * 0.173f, tap * 0.291f);
            float radialMix = style == VDRimStyle.Streak ? VDDirector.RimRadialMixStreak : VDDirector.RimRadialMixDefault;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uOpacity"]?.SetValue(RimOpacity() * RimFlicker(style) * opacityMult);
            shader.Parameters["uColor"]?.SetValue(VDDepth.Fog(color, Depth).ToVector3());
            shader.Parameters["uHotColor"]?.SetValue(hot.ToVector3());
            shader.Parameters["uHeat"]?.SetValue(RimHeat());
            shader.Parameters["uFlash"]?.SetValue(RimFlash);
            shader.Parameters["uErode"]?.SetValue(erode);
            shader.Parameters["uNoiseScroll"]?.SetValue(scroll);
            shader.Parameters["uRadialScroll"]?.SetValue(RimRadialScroll + tap * 0.137f);
            shader.Parameters["uRadialMix"]?.SetValue(radialMix);
            shader.Parameters["uFrameCenter"]?.SetValue(RimFrameCenter(tex, frame));
            shader.Parameters["uImageSize"]?.SetValue(tex.Size());
            shader.CurrentTechnique.Passes[0].Apply();
        }

        /// <summary>
        /// 外扩光晕环(垫在本体之下):VDRimHalo 把本体贴图整个涂成描边色的实心剪影,在屏幕空间绕圈偏移叠画,
        /// 本体压在上面盖住剪影内部,剩下剪影之外那一圈就是宽 = 偏移半径的外扩描边带。
        /// 外环 RimTaps 抽、半径 RimBaseRadius,噪声侵蚀成向外逸散的丝;内环 RimInnerTaps 抽、半径 × RimInnerRadiusMult,几乎不侵蚀,贴身实心亮带;
        /// 过热风格再在 RimOverheatOuterMult 倍半径叠一环,成厚实的白炽电晕。
        /// 本体贴图四边只有 2~4px 留白,任何在贴图内做膨胀的着色器都会被四边裁掉(旧做法用 VDRimLight 的 2 texel 缘带偏移 6 抽,每抽只露一弯月牙,肉眼看不见),
        /// 独立四边形往外挪不吃留白,也不需要 RenderTarget(Apsychos.DrawOutLine / AcropolisMachine.DrawHarpoonOutline 同款做法)。
        /// 着色器缺失时退回 WhiteTrans 平色叠画同样的两环,功能不丢
        /// </summary>
        private void DrawRimUnder(Texture2D tex, Rectangle? frame, Vector2 origin, float scale, Vector2 worldCenter, Vector2 screenPos) {
            float opacity = RimOpacity();
            if (opacity <= 0.01f) {
                return;
            }
            VDRimStyle style = VDDirector.RimStyleFor(CurrentStateIndex);
            float radius = RimRadius(style, scale);
            float innerRadius = radius * VDDirector.RimInnerRadiusMult;
            Vector2 center = worldCenter - screenPos;
            Effect halo = CEEffectAssets.VDRimHalo?.Value;

            if (halo != null) {
                Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive, halo);
                Main.instance.GraphicsDevice.Textures[1] = noise;
                Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                int tapSeed = 0;
                //外环:丝状逸散
                DrawHaloRing(halo, tex, frame, origin, scale, center, style, VDDirector.RimTaps, radius, VDDirector.RimTapOpacity, RimHaloErode(style, inner: false), 0f, ref tapSeed);
                //内环:实心贴身,抽位错开半格,与外环的多边形顶点不重合
                DrawHaloRing(halo, tex, frame, origin, scale, center, style, VDDirector.RimInnerTaps, innerRadius, VDDirector.RimInnerTapOpacity, RimHaloErode(style, inner: true), MathHelper.Pi / VDDirector.RimInnerTaps, ref tapSeed);
                if (style == VDRimStyle.Overheat) {
                    float outer = radius * VDDirector.RimOverheatOuterMult;
                    DrawHaloRing(halo, tex, frame, origin, scale, center, style, VDDirector.RimTaps, outer, VDDirector.RimTapOpacity * VDDirector.RimOverheatOuterOpacity, RimHaloErode(style, inner: false), MathHelper.Pi / VDDirector.RimTaps, ref tapSeed);
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
            Color flat = Color.Lerp(RimColor, RimHotColor, RimHeat()) * (MathHelper.Clamp(opacity, 0f, 1f) * (1f + RimFlash));
            for (int i = 0; i < VDDirector.RimTaps; i++) {
                Main.spriteBatch.Draw(tex, center + RimTapOffset(style, i, VDDirector.RimTaps, radius, scale, 0f), frame, flat * VDDirector.RimTapOpacity, NPC.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            for (int i = 0; i < VDDirector.RimInnerTaps; i++) {
                Main.spriteBatch.Draw(tex, center + RimTapOffset(style, i, VDDirector.RimInnerTaps, innerRadius, scale, MathHelper.Pi / VDDirector.RimInnerTaps), frame, flat * VDDirector.RimInnerTapOpacity, NPC.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.ExitShaderRegion();
        }

        /// <summary>
        /// 光晕着色器批次内画一环:taps 抽绕圈偏移 radius,每抽换噪声相位(tapSeed 逐抽递增,几环之间也不重相位),
        /// 顶点色只承担各抽的亮度分摊(着色器输出整体乘顶点色)
        /// </summary>
        private void DrawHaloRing(Effect halo, Texture2D tex, Rectangle? frame, Vector2 origin, float scale, Vector2 center, VDRimStyle style, int taps, float radius, float tapOpacity, float erode, float phase, ref int tapSeed) {
            for (int i = 0; i < taps; i++) {
                ApplyRimShader(halo, tex, frame, style, tapSeed++, RimColor, erode, 1f);
                Vector2 ofs = RimTapOffset(style, i, taps, radius, scale, phase);
                Main.spriteBatch.Draw(tex, center + ofs, frame, Color.White * RimTapAlpha(style, i, taps, tapOpacity), NPC.rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// 贴边亮线(压在本体之上):零偏移画一遍 VDRimLight 缘带着色器,光像是从机体表面漏出来。
        /// 颜色向 VoidWhite 偏 RimEdgeWhiten(紫线压紫机体读不出来),亮度 RimEdgeOpacity 走 uOpacity(可大于 1)。无着色器时不画(平色会盖住本体)
        /// </summary>
        private void DrawRimOver(Texture2D tex, Rectangle? frame, Vector2 origin, float scale, Vector2 worldCenter, Vector2 screenPos) {
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
            Color edge = Color.Lerp(RimColor, VDVfx.VoidWhite, VDDirector.RimEdgeWhiten);
            ApplyRimShader(shader, tex, frame, style, VDDirector.RimTaps * 2 + VDDirector.RimInnerTaps, edge, RimErode(style), VDDirector.RimEdgeOpacity);
            Main.spriteBatch.Draw(tex, worldCenter - screenPos, frame, Color.White, NPC.rotation, origin, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
        }
        #endregion

        #region 核心光与护盾
        /// <summary>
        /// 导引线:主炮扫射起点的预告,越近出手越亮、越粗,末尾闪烁。本体在平面时从核心沿方向直画;
        /// 本体在镜头后(越肩主炮)时先从投影核心拉一段收敛的透视光锥到它在平面上的枢(NPC.Center),再从枢沿方向画
        /// </summary>
        private void DrawAimLine(Vector2 center, Vector2 screenPos, float scale, float alpha) {
            float s = Context.AimLineStrength;
            if (s <= 0f || Context.AimLineDir == Vector2.Zero) {
                return;
            }
            Vector2 dir = Context.AimLineDir.SafeNormalize(Vector2.UnitY);
            Vector2 core = center + CoreOffset.RotatedBy(NPC.rotation) * scale;
            Vector2 pivot = Depth < -0.05f ? NPC.Center : core;
            Vector2 end = pivot + dir * VDDirector.CannonBeamLength;
            float flicker = s > 0.75f ? 0.75f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 60f) : 1f;
            //光锥的亮度不随近景剪影衰减:它是打进画面里的光,不是剪影的一部分
            float lineAlpha = Depth < -0.05f ? 1f : alpha;
            Color c = Context.AimLineColor * ((0.25f + 0.65f * s) * flicker * lineAlpha);
            if (pivot != core) {
                //传本体的 Z(镜头前,近端不衰减),枢端端帽 0 直接接上平面导引线
                VDBeamDraw.DrawTapered(core, pivot, (6f + 14f * s) * scale, 4f + 10f * s, Context.AimLineColor, Color.White, 1f, (0.3f + 0.6f * s) * flicker, 0.61f, endGlow: false, zStart: Depth, zEnd: 0f, capEnd: 0f);
            }
            Main.spriteBatch.UseAdditive();
            CEUtils.drawLineBetter(pivot, end, c, 4f + 10f * s);
            CEUtils.drawLine(pivot, end, Color.White * (0.6f * s * flicker * lineAlpha), 1.5f + s);
            CEUtils.ReSetToEndShader();
        }

        private void DrawCoreGlow(Vector2 center, Vector2 screenPos, float scale, float alpha) {
            if (Phase < 2 && CoreGlow <= 0.01f) {
                return;
            }
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 pos = center + CoreOffset.RotatedBy(NPC.rotation) * scale;
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f);
            float strength = (Phase >= 2 ? 0.55f : 0f) + CoreGlow * 0.9f;
            Color core = VDDepth.Fog(CoreColor, Depth);
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glow, pos - screenPos, null, core * (strength * pulse * alpha), 0f, glow.Size() / 2f, (0.32f + CoreGlow * 0.25f) * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, pos - screenPos, null, Color.White * (strength * 0.5f * alpha), 0f, glow.Size() / 2f, (0.14f + CoreGlow * 0.1f) * scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
        }

        /// <summary>
        /// 三阶段外围淡紫防护盾:前后两环合成一个有厚度的球。后环(垫在本体下)略大、偏暗、反向慢转,
        /// 前环(压在本体上)带背光;两环的转向相反,读成球面在转而不是两张贴纸
        /// </summary>
        private void DrawShield(Vector2 center, Vector2 screenPos, float scale, float alpha, bool back) {
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Texture2D glow = CEUtils.getExtraTex("Glow");
            float pulse = 0.75f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            float ringScale = 2.1f * scale * (1f + 0.03f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f));
            Main.spriteBatch.UseAdditive();
            if (back) {
                Main.spriteBatch.Draw(ring, center - screenPos, null, new Color(150, 100, 220) * (0.45f * ShieldAlpha * pulse * alpha), -Main.GlobalTimeWrappedHourly * 0.45f, ring.Size() / 2f, ringScale * 1.12f, SpriteEffects.None, 0f);
            }
            else {
                Main.spriteBatch.Draw(glow, center - screenPos, null, new Color(170, 110, 255) * (0.35f * ShieldAlpha * pulse * alpha), 0f, glow.Size() / 2f, ringScale * 0.75f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(ring, center - screenPos, null, new Color(210, 160, 255) * (0.8f * ShieldAlpha * pulse * alpha), Main.GlobalTimeWrappedHourly * 0.6f, ring.Size() / 2f, ringScale, SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();
        }
        #endregion
    }
}
