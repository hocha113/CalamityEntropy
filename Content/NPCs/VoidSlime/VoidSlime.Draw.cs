using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using CalamityEntropy.Content.Skies;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidSlime
{
    public partial class VoidSlime
    {
        private const string PaleTexturePath = "CalamityEntropy/Content/NPCs/VoidSlime/VoidSlime_Pale";
        private const string SingularityTexturePath = "CalamityEntropy/Content/NPCs/VoidSlime/VoidSlime_Singularity";
        private const string BigStarTexturePath = "CalamityEntropy/Content/Projectiles/VoidSlime/VSlimeBigStar";
        /// <summary>视界占着色器方块半边长的比例,黑盘边缘就是判定边缘</summary>
        private const float HoleCoreFraction = 0.5f;
        private const float StarShellFraction = 0.62f;
        private const float WormholeRadius = 150f;
        private const float ShapeBlendStep = 0.12f;

        private static Asset<Texture2D> paleTexture;
        private static Asset<Texture2D> singularityTexture;
        private static Asset<Texture2D> bigStarTexture;

        #region 本地视觉字段(由声明逐帧推导,不过线)
        public float Alpha = 1f;
        /// <summary>闪现淡出淡入的透明度,乘在 Alpha 上</summary>
        private float blinkAlpha = 1f;
        private float wSlime = 1f;
        private float wOrb;
        private float wWormhole;
        private float wHole;
        private float wStar;
        private float visSquash;
        private float visSwell;
        private float visRim;
        private float visCharge;
        private float visGlow;
        private float visHalo;
        private float visDepth;
        private float visArena;
        private float visHoleRadius;
        private float visStarRadius;
        private float orbRotation;
        private int drawClock;
        #endregion

        private static Texture2D PaleTex => (paleTexture ??= ModContent.Request<Texture2D>(PaleTexturePath, AssetRequestMode.ImmediateLoad)).Value;

        private static Texture2D SingularityTex => (singularityTexture ??= ModContent.Request<Texture2D>(SingularityTexturePath, AssetRequestMode.ImmediateLoad)).Value;

        private static Texture2D BigStarTex => (bigStarTexture ??= ModContent.Request<Texture2D>(BigStarTexturePath, AssetRequestMode.ImmediateLoad)).Value;

        private void UpdateVisualState() {
            VSlimeStateContext c = Context;
            Alpha = float.IsNaN(c.AlphaDeclared) ? MathHelper.Lerp(Alpha, 1f, 0.2f) : c.AlphaDeclared;
            if (Main.dedServ) {
                return;
            }
            drawClock++;
            Blend(ref wSlime, c.Shape == VSlimeShape.Slime);
            Blend(ref wOrb, c.Shape == VSlimeShape.Orb);
            Blend(ref wWormhole, c.Shape == VSlimeShape.Wormhole);
            Blend(ref wHole, c.Shape == VSlimeShape.BlackHole);
            Blend(ref wStar, c.Shape == VSlimeShape.Star);
            visSquash = MathHelper.Lerp(visSquash, c.Squash, 0.35f);
            visSwell = MathHelper.Lerp(visSwell, c.Swell, 0.3f);
            visRim = MathHelper.Lerp(visRim, c.RimGlow, 0.2f);
            visCharge = MathHelper.Lerp(visCharge, c.ChargeGlow, 0.2f);
            visGlow = MathHelper.Lerp(visGlow, c.BodyGlow, 0.25f);
            visHalo = MathHelper.Lerp(visHalo, c.Halo, 0.2f);
            visDepth = MathHelper.Lerp(visDepth, c.Depth, 0.2f);
            visArena = MathHelper.Lerp(visArena, c.ArenaActive ? 1f : 0f, 0.05f);
            //半径只在声明时跟随,淡出那几帧沿用上一次的值
            if (c.HoleRadius > 0f) {
                visHoleRadius = c.HoleRadius;
            }
            if (c.StarRadius > 0f) {
                visStarRadius = c.StarRadius;
            }
            orbRotation = wOrb > 0.02f ? MathHelper.WrapAngle(orbRotation + MathHelper.Clamp(NPC.velocity.X * 0.012f, -0.3f, 0.3f)) : 0f;

            Music = c.Phase <= VSlimeForm.Pale ? MusicID.Boss1 : musicVoid;
            if (c.SkyIntensity > 0f) {
                VSlimeSkyDrive.Report(c.SkyIntensity, c.SkyDarkness, NPC.Center);
            }
            Vector3 light = c.Phase <= VSlimeForm.Pale ? new Vector3(0.5f, 0.5f, 0.56f) : new Vector3(0.6f, 0.34f, 0.92f);
            Lighting.AddLight(NPC.Center, light * (Alpha * blinkAlpha));
        }

        private static void Blend(ref float weight, bool on) {
            weight = MathHelper.Clamp(weight + (on ? ShapeBlendStep : -ShapeBlendStep), 0f, 1f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (NPC.IsABestiaryIconDummy || Context == null) {
                return true;
            }
            VSlimeStateContext c = Context;
            DrawArena(c);
            DrawTelegraphs(c);
            float alpha = Alpha * blinkAlpha;
            if (alpha <= 0.01f) {
                return false;
            }
            //抖动按帧钟算,暂停时画面不跳
            Vector2 shake = new Vector2(MathF.Sin(drawClock * 1.7f), MathF.Cos(drawClock * 2.3f)) * (5f * c.ShakeStrength);
            Vector2 center = NPC.Center + shake;
            float depthScale = 1f - 0.55f * visDepth;
            if (wHole > 0.01f) {
                DrawHole(center, visHoleRadius * depthScale, c.HoleTint, wHole * alpha * (1f - 0.35f * visDepth));
                if (c.Hazard2Center != Vector2.Zero) {
                    DrawHole(c.Hazard2Center, visHoleRadius, c.HoleTint, wHole * alpha);
                }
            }
            if (wWormhole > 0.01f) {
                DrawWormhole(center, wWormhole * alpha);
            }
            if (wStar > 0.01f) {
                DrawStar(center, visStarRadius, c.StarHeat, wStar * alpha);
            }
            float body = MathHelper.Clamp(wSlime + wOrb, 0f, 1f) * alpha;
            if (body > 0.01f) {
                DrawBody(spriteBatch, c, shake, drawColor, body, depthScale);
            }
            DrawAttachments(c, center, alpha);
            return false;
        }

        #region 本体
        private void DrawBody(SpriteBatch sb, VSlimeStateContext c, Vector2 shake, Color light, float opacity, float depthScale) {
            bool pale = c.Phase <= VSlimeForm.Pale;
            bool dark = c.Phase >= VSlimeForm.Singularity;
            Texture2D tex = pale ? PaleTex : dark ? SingularityTex : TextureAssets.Npc[Type].Value;
            float orb = wOrb / Math.Max(0.001f, wSlime + wOrb);
            float size = VSlimeDirector.BodyDrawScale * (1f + visSwell) * depthScale;
            Vector2 slimeScale = new Vector2(1f + visSquash * 0.55f, 1f - visSquash) * size;
            //团成球:横向收窄纵向拉高,边滚边飞
            Vector2 orbScale = new Vector2(0.8f, 1.14f) * (size * 0.9f);
            Vector2 scale = Vector2.Lerp(slimeScale, orbScale, orb);
            Vector2 origin = Vector2.Lerp(new Vector2(tex.Width / 2f, tex.Height), tex.Size() / 2f, orb);
            Vector2 world = Vector2.Lerp(NPC.Bottom + new Vector2(0f, 2f), NPC.Center, orb) + shake;
            Vector2 mid = world - new Vector2(0f, tex.Height * scale.Y * 0.5f * (1f - orb));
            float rotation = orbRotation * orb;
            Vector2 extent = new Vector2(tex.Width * scale.X, tex.Height * scale.Y);
            Texture2D glow = GodDraw.Glow;
            float solid = opacity * (1f - 0.92f * c.Splatter);

            float aura = Math.Max(visRim, dark ? 0.6f : 0f);
            if (aura > 0.01f || visCharge > 0.01f) {
                GodDraw.BeginAdditive();
                GodDraw.Centered(glow, mid, GodDraw.Add(VSlimeVfx.VoidBright, 0.6f * aura * opacity), 0f, extent * 1.8f / glow.Size());
                GodDraw.Centered(glow, mid, GodDraw.Add(VSlimeVfx.HoleRed, 0.7f * visCharge * opacity), 0f, extent * 1.7f / glow.Size());
                GodDraw.End();
            }

            Color bodyColor = Color.Lerp(light, Color.White, pale ? 0.3f : 0.65f);
            sb.Draw(tex, world - Main.screenPosition, null, bodyColor * solid, rotation, origin, scale, SpriteEffects.None, 0f);
            if (c.Splatter > 0.01f) {
                DrawSplatter(sb, mid, c.Splatter, opacity, size);
            }

            GodDraw.BeginAdditive();
            if (visCharge > 0.01f) {
                sb.Draw(tex, world - Main.screenPosition, null, GodDraw.Add(VSlimeVfx.HoleRed, 0.7f * visCharge * solid), rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (visGlow > 0.01f) {
                Color hot = GodDraw.Add(Color.White, visGlow * solid);
                sb.Draw(tex, world - Main.screenPosition, null, hot, rotation, origin, scale, SpriteEffects.None, 0f);
                sb.Draw(tex, world - Main.screenPosition, null, hot, rotation, origin, scale, SpriteEffects.None, 0f);
                GodDraw.Centered(glow, mid, GodDraw.Add(pale ? VSlimeVfx.PaleLight : VSlimeVfx.Lavender, 0.8f * visGlow * opacity), 0f, extent * 1.5f / glow.Size());
            }
            if (c.Cracks > 0.01f) {
                DrawCracks(mid, extent, c.Cracks, opacity);
            }
            GodDraw.End();
        }

        /// <summary>壳体裂纹,从身体中心向外长,调用前须在加法批次里</summary>
        private static void DrawCracks(Vector2 mid, Vector2 extent, float cracks, float opacity) {
            const int count = 9;
            for (int i = 0; i < count; i++) {
                float angle = MathHelper.TwoPi * i / count + 0.37f * MathF.Sin(i * 2.1f);
                Vector2 dir = angle.ToRotationVector2();
                float reach = (0.32f + 0.14f * MathF.Sin(i * 3.7f)) * cracks;
                Vector2 knee = mid + dir * extent * (reach * 0.55f);
                Vector2 tip = mid + dir.RotatedBy(0.35f * MathF.Sin(i * 5.3f)) * extent * reach;
                Color color = GodDraw.Add(VSlimeVfx.VoidBright, (0.5f + 0.5f * cracks) * opacity);
                GodDraw.Line(mid, knee, 3.5f, color, 0.05f);
                GodDraw.Line(knee, tip, 2.5f, color, 0.05f);
            }
        }

        /// <summary>身体溅散成的黑团,散开再收拢</summary>
        private void DrawSplatter(SpriteBatch sb, Vector2 mid, float splatter, float opacity, float size) {
            const int count = 9;
            Texture2D disc = GodDraw.Disc;
            Texture2D glow = GodDraw.Glow;
            for (int pass = 0; pass < 2; pass++) {
                if (pass == 0) {
                    GodDraw.BeginAdditive();
                }
                for (int i = 0; i < count; i++) {
                    float angle = MathHelper.TwoPi * i / count + 0.5f * MathF.Sin(i * 1.9f) + drawClock * 0.01f;
                    float reach = (70f + 46f * MathF.Sin(i * 2.7f + 1f)) * splatter * size;
                    float radius = (26f + 12f * MathF.Sin(i * 4.1f)) * size * (0.6f + 0.4f * splatter);
                    Vector2 pos = mid + angle.ToRotationVector2() * reach;
                    if (pass == 0) {
                        GodDraw.Centered(glow, pos, GodDraw.Add(VSlimeVfx.VoidBright, 0.55f * splatter * opacity), 0f, radius * 4.4f / glow.Width);
                    }
                    else {
                        sb.Draw(disc, pos - Main.screenPosition, null, new Color(30, 20, 46) * (splatter * opacity), 0f, disc.Size() / 2f, radius * 2f / disc.Width, SpriteEffects.None, 0f);
                    }
                }
                if (pass == 0) {
                    GodDraw.End();
                }
            }
        }
        #endregion

        #region 黑洞、虫洞与恒星
        private static void DrawHole(Vector2 center, float radius, float tint, float opacity) {
            if (radius <= 1f) {
                return;
            }
            Color disk = Color.Lerp(VSlimeVfx.HoleRed, VSlimeVfx.StarBlue, tint);
            Color hot = Color.Lerp(VSlimeVfx.HoleEmber, VSlimeVfx.StarIce, tint);
            Color deep = Color.Lerp(VSlimeVfx.HoleDark, VSlimeVfx.StarDeepBlue, tint);
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, center, GodDraw.Add(deep, 0.8f * opacity), 0f, radius * 5.4f / glow.Width);
            GodDraw.End();

            DrawSwirl(center, radius / HoleCoreFraction, HoleCoreFraction, disk, hot, 1.4f, opacity);

            //视界边缘再描一圈亮线,判定边界一眼可读
            GodDraw.BeginAdditive();
            GodDraw.RingStrip(GodDraw.SoftLine, center, radius, Math.Max(3f, radius * 0.035f), GodDraw.Add(hot, 0.85f * opacity), 0f, MathHelper.TwoPi, 64);
            GodDraw.End();
        }

        private static void DrawWormhole(Vector2 center, float opacity) {
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, center, GodDraw.Add(VSlimeVfx.VoidPurple, 0.7f * opacity), 0f, WormholeRadius * 3.4f / glow.Width);
            GodDraw.End();
            DrawSwirl(center, WormholeRadius, 0.1f, VSlimeVfx.VoidBright, VSlimeVfx.Lavender, 2.4f, opacity);
        }

        /// <summary>旋流盘走 VDSingularity,预乘输出配 AlphaBlend,黑盘是真遮挡;着色器缺失时退回环加黑盘</summary>
        internal static void DrawSwirl(Vector2 center, float half, float coreFraction, Color disk, Color hot, float spin, float opacity) {
            Effect shader = CEEffectAssets.VDSingularity?.Value;
            if (shader == null) {
                Texture2D disc = GodDraw.Disc;
                GodDraw.BeginAdditive();
                GodDraw.Centered(GodDraw.BloomRing, center, GodDraw.Add(disk, opacity), 0f, GodDraw.RingScale(half * 0.75f));
                GodDraw.End();
                Main.spriteBatch.Draw(disc, center - Main.screenPosition, null, Color.Black * opacity, 0f, disc.Size() / 2f, half * coreFraction * 2f / disc.Width, SpriteEffects.None, 0f);
                return;
            }
            Texture2D quad = CEUtils.getExtraTex("white");
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
            gd.Textures[1] = CEUtils.getExtraTex("TurbulentNoise");
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uColor"]?.SetValue(disk.ToVector3());
            shader.Parameters["uColor2"]?.SetValue(hot.ToVector3());
            shader.Parameters["uCoreRadius"]?.SetValue(coreFraction);
            shader.Parameters["uOpacity"]?.SetValue(opacity);
            shader.Parameters["uSpin"]?.SetValue(spin);
            shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(quad, center - Main.screenPosition, null, Color.White, 0f, quad.Size() / 2f, half * 2f / quad.Width, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            gd.Textures[1] = null;
        }

        /// <summary>恒星走 VDPlasmaOrb,色温 0 蓝 1 红 2 白</summary>
        private void DrawStar(Vector2 center, float radius, float heat, float opacity) {
            if (radius <= 1f) {
                return;
            }
            Color warmCore = new Color(255, 226, 170);
            Color warmDeep = new Color(120, 20, 10);
            float cool = MathHelper.Clamp(heat, 0f, 1f);
            float white = MathHelper.Clamp(heat - 1f, 0f, 1f);
            Color main = Color.Lerp(Color.Lerp(VSlimeVfx.StarBlue, VSlimeVfx.GiantRed, cool), new Color(214, 232, 255), white);
            Color core = Color.Lerp(Color.Lerp(VSlimeVfx.StarIce, warmCore, cool), Color.White, white);
            Color deep = Color.Lerp(Color.Lerp(VSlimeVfx.StarDeepBlue, warmDeep, cool), new Color(120, 140, 190), white);
            float beat = 0.9f + 0.1f * MathF.Sin(drawClock * 0.07f);
            Texture2D glow = GodDraw.Glow;
            Texture2D flare = GodDraw.Star;

            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, center, GodDraw.Add(main, 0.75f * opacity * beat), 0f, radius * 4.8f / glow.Width);
            GodDraw.Centered(flare, center, GodDraw.Add(core, 0.6f * opacity), drawClock * 0.004f, radius * 5.2f / flare.Width);
            GodDraw.End();

            Effect shader = CEEffectAssets.VDPlasmaOrb?.Value;
            if (shader == null) {
                Texture2D disc = GodDraw.Disc;
                Main.spriteBatch.Draw(disc, center - Main.screenPosition, null, main * opacity, 0f, disc.Size() / 2f, radius * 2f / disc.Width, SpriteEffects.None, 0f);
                return;
            }
            Texture2D quad = CEUtils.getExtraTex("white");
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
            gd.Textures[1] = CEUtils.getExtraTex("SwirlNoise");
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            gd.Textures[2] = CEUtils.getExtraTex("GradientNoise");
            gd.SamplerStates[2] = SamplerState.LinearWrap;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uCharge"]?.SetValue(0.6f + 0.3f * white);
            shader.Parameters["uOpacity"]?.SetValue(opacity);
            shader.Parameters["uRadius"]?.SetValue(StarShellFraction);
            shader.Parameters["uSeed"]?.SetValue(0.37f);
            shader.Parameters["uColorMain"]?.SetValue(main.ToVector3());
            shader.Parameters["uColorCore"]?.SetValue(core.ToVector3());
            shader.Parameters["uColorDeep"]?.SetValue(deep.ToVector3());
            shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(quad, center - Main.screenPosition, null, Color.White, 0f, quad.Size() / 2f, radius / StarShellFraction * 2f / quad.Width, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            gd.Textures[1] = null;
            gd.Textures[2] = null;
        }
        #endregion

        #region 挂件、场地与预告
        private void DrawAttachments(VSlimeStateContext c, Vector2 center, float alpha) {
            if (visHalo <= 0.01f && c.OrbitStars <= 0 && c.Pulse <= 0.01f) {
                return;
            }
            GodDraw.BeginAdditive();
            if (visHalo > 0.01f) {
                Vector2 top = center - new Vector2(0f, VSlimeDirector.BodySize.Y * 0.5f + 64f + 4f * MathF.Sin(drawClock * 0.05f));
                float ring = GodDraw.RingScale(74f);
                GodDraw.Centered(GodDraw.BloomRing, top, GodDraw.Add(VSlimeVfx.Lavender, visHalo * alpha), 0f, new Vector2(ring, ring * 0.32f));
                GodDraw.Centered(GodDraw.Glow, top, GodDraw.Add(VSlimeVfx.VoidBright, 0.5f * visHalo * alpha), 0f, new Vector2(260f, 90f) / GodDraw.Glow.Size());
            }
            if (c.OrbitStars > 0) {
                Texture2D star = BigStarTex;
                for (int i = VSlimeDirector.BlastStars - c.OrbitStars; i < VSlimeDirector.BlastStars; i++) {
                    Vector2 pos = center + VSlimeDirector.OrbitStarOffset(i, StateTimer);
                    GodDraw.Centered(GodDraw.Glow, pos, GodDraw.Add(VSlimeVfx.VoidBright, 0.7f * alpha), 0f, 150f / GodDraw.Glow.Width);
                    Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, GodDraw.Add(Color.White, alpha), drawClock * 0.05f, star.Size() / 2f, 1f, SpriteEffects.None, 0f);
                }
            }
            if (c.Pulse > 0.01f) {
                Texture2D flash = GodDraw.Flash;
                GodDraw.Centered(flash, center, GodDraw.Add(VSlimeVfx.Lavender, c.Pulse), 0f, (240f + 360f * c.Pulse) / flash.Width);
            }
            GodDraw.End();
        }

        /// <summary>引力场边界:一圈亮线、外侧一层雾带和沿环慢转的星芒</summary>
        private void DrawArena(VSlimeStateContext c) {
            if (visArena <= 0.01f || c.ArenaRadius <= 1f) {
                return;
            }
            const int sparks = 16;
            float radius = c.ArenaRadius;
            float spin = drawClock * 0.0016f;
            Texture2D star = GodDraw.Star;
            GodDraw.BeginAdditive();
            GodDraw.RingStrip(GodDraw.SoftLine, c.ArenaCenter, radius + 46f, 60f, GodDraw.Add(VSlimeVfx.VoidPurple, 0.3f * visArena), spin, MathHelper.TwoPi, 96);
            GodDraw.RingStrip(GodDraw.SoftLine, c.ArenaCenter, radius, 9f, GodDraw.Add(VSlimeVfx.Lavender, 0.9f * visArena), spin, MathHelper.TwoPi, 96);
            for (int i = 0; i < sparks; i++) {
                float angle = spin + MathHelper.TwoPi * i / sparks;
                float twinkle = 0.6f + 0.4f * MathF.Sin(drawClock * 0.06f + i * 1.3f);
                GodDraw.Centered(star, c.ArenaCenter + angle.ToRotationVector2() * radius, GodDraw.Add(VSlimeVfx.StarGold, 0.8f * twinkle * visArena), 0f, 90f / star.Width);
            }
            GodDraw.End();
        }

        private void DrawTelegraphs(VSlimeStateContext c) {
            bool any = c.WarnStrength > 0.01f || c.LaneStrength > 0.01f || c.MarkerStrength > 0.01f || c.PentagramStrength > 0.01f || c.TotemStrength > 0.01f;
            if (!any) {
                return;
            }
            float blink = 0.8f + 0.2f * MathF.Sin(drawClock * 0.3f);
            GodDraw.BeginAdditive();
            if (c.WarnStrength > 0.01f && c.WarnRadius > 1f) {
                Texture2D disc = GodDraw.Disc;
                GodDraw.Centered(disc, NPC.Center, GodDraw.Add(VSlimeVfx.VoidPurple, 0.12f * c.WarnStrength), 0f, c.WarnRadius * 2f / disc.Width);
                GodDraw.RingStrip(GodDraw.SoftLine, NPC.Center, c.WarnRadius, 7f, GodDraw.Add(VSlimeVfx.Lavender, c.WarnStrength * blink), 0f, MathHelper.TwoPi, 96);
            }
            if (c.LaneStrength > 0.01f) {
                Vector2 dir = (c.LaneEnd - c.LaneStart).SafeNormalize(Vector2.UnitY);
                Vector2 side = dir.RotatedBy(MathHelper.PiOver2) * (c.LaneWidth * 0.5f);
                Color edge = GodDraw.Add(VSlimeVfx.Lavender, 0.85f * c.LaneStrength * blink);
                GodDraw.Line(c.LaneStart, c.LaneEnd, c.LaneWidth * 0.5f, GodDraw.Add(VSlimeVfx.VoidPurple, 0.3f * c.LaneStrength), 0.06f, GodDraw.Glow);
                GodDraw.Line(c.LaneStart + side, c.LaneEnd + side, 3f, edge, 0.06f);
                GodDraw.Line(c.LaneStart - side, c.LaneEnd - side, 3f, edge, 0.06f);
            }
            if (c.MarkerStrength > 0.01f) {
                GodDraw.RingStrip(GodDraw.SoftLine, c.MarkerPos, 40f, 4f, GodDraw.Add(VSlimeVfx.Lavender, c.MarkerStrength * blink), drawClock * 0.03f, MathHelper.TwoPi, 32);
                GodDraw.Centered(GodDraw.Star, c.MarkerPos, GodDraw.Add(VSlimeVfx.StarGold, c.MarkerStrength), 0f, 120f / GodDraw.Star.Width);
            }
            if (c.PentagramStrength > 0.01f) {
                DrawPentagram(c);
            }
            if (c.TotemStrength > 0.01f) {
                DrawTotems(c);
            }
            GodDraw.End();
        }

        /// <summary>五角星:整张淡底稿加已经画完的亮边,调用前须在加法批次里</summary>
        private static void DrawPentagram(VSlimeStateContext c) {
            for (int k = 0; k < 5; k++) {
                Vector2 from = VSlimeDirector.PentagramVertex(c.ArenaCenter, k);
                Vector2 to = VSlimeDirector.PentagramVertex(c.ArenaCenter, k + 1);
                GodDraw.Line(from, to, 4f, GodDraw.Add(VSlimeVfx.VoidPurple, 0.45f * c.PentagramStrength), 0.02f);
                float drawn = MathHelper.Clamp(c.PentagramDrawn - k, 0f, 1f);
                if (drawn <= 0f) {
                    continue;
                }
                Vector2 tip = Vector2.Lerp(from, to, drawn);
                GodDraw.Line(from, tip, 9f, GodDraw.Add(VSlimeVfx.VoidBright, c.PentagramStrength), 0.02f);
                GodDraw.Line(from, tip, 3f, GodDraw.Add(VSlimeVfx.StarCore, 0.9f * c.PentagramStrength), 0.02f);
                GodDraw.Centered(GodDraw.Glow, from, GodDraw.Add(VSlimeVfx.Lavender, 0.8f * c.PentagramStrength), 0f, 120f / GodDraw.Glow.Width);
            }
        }

        /// <summary>红巨星的安全圈:圈内铺底、圈线和圈里的小五角星,调用前须在加法批次里</summary>
        private void DrawTotems(VSlimeStateContext c) {
            Texture2D disc = GodDraw.Disc;
            float radius = VSlimeDirector.TotemRadius;
            for (int i = 0; i < VSlimeDirector.TotemCount; i++) {
                Vector2 pos = VSlimeDirector.TotemCenter(c.ArenaCenter, c.TotemAngle, i);
                GodDraw.Centered(disc, pos, GodDraw.Add(VSlimeVfx.VoidPurple, 0.3f * c.TotemStrength), 0f, radius * 2f / disc.Width);
                GodDraw.RingStrip(GodDraw.SoftLine, pos, radius, 6f, GodDraw.Add(VSlimeVfx.Lavender, c.TotemStrength), 0f, MathHelper.TwoPi, 40);
                float spin = -MathHelper.PiOver2 + drawClock * 0.01f;
                for (int k = 0; k < 5; k++) {
                    Vector2 from = pos + (spin + MathHelper.TwoPi * 2f / 5f * k).ToRotationVector2() * (radius * 0.86f);
                    Vector2 to = pos + (spin + MathHelper.TwoPi * 2f / 5f * (k + 1)).ToRotationVector2() * (radius * 0.86f);
                    GodDraw.Line(from, to, 3f, GodDraw.Add(VSlimeVfx.VoidBright, 0.9f * c.TotemStrength), 0.02f);
                }
            }
        }
        #endregion
    }
}
