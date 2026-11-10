using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using InnoVault.Rigs2D.Runtime;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage
{
    /// <summary>
    /// 绘制:星遁落点 → 航道与扇面预告 → 月轮 → 残影 → 骨架本体 → 星辉叠层 → 环身星符 → 嘴前星印 → 死亡星芒
    /// 本体是骨架件,走默认批次;残影和星辉用透明度为 0 的染色在默认批次里叠加,骨架渲染器画完带状件会把批次还原成 AlphaBlend
    /// </summary>
    public partial class StarmoonSage
    {
        private const string MarkTexture = "CalamityEntropy/Content/Items/Armor/Starmoon/StarmoonMark";
        private const float CrestRadius = 150f;
        private const int CrestSegments = 48;
        private const int GhostLifeTicks = 14;

        private static BlendState pureAdditive;
        private static readonly Vector2[] crestPoints = new Vector2[CrestSegments + 1];

        /// <summary>带状件的加法态:源色不乘 alpha,配透明度为 0 的染色</summary>
        private static BlendState PureAdditive => pureAdditive ??= new BlendState {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        /// <summary>AlphaBlend 批次里的叠加染色:RGB 按强度缩,A 置 0</summary>
        private static Color Overlay(Color color, float intensity) {
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            return new Color((int)(color.R * intensity), (int)(color.G * intensity), (int)(color.B * intensity), 0);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (NPC.IsABestiaryIconDummy || Context == null) {
                return false;
            }
            float shown = Alpha * blinkAlpha;
            float time = Main.GlobalTimeWrappedHourly;
            //抖动只是绘制偏移,用时间算而不掷骰,暂停时画面不跳
            Vector2 shake = new Vector2(MathF.Sin(time * 97f), MathF.Cos(time * 71f)) * (5f * Context.ShakeStrength);
            Vector2 center = NPC.Center + shake;
            Vector2 facing = NPC.rotation.ToRotationVector2();

            GodDraw.BeginAdditive();
            if (Context.BlinkTimer > 0) {
                DrawBlinkMarker(time);
            }
            if (Context.LaneStrength > 0.01f) {
                DrawLane(time);
            }
            if (Context.FanLanes > 0 && Context.FanStrength > 0.01f) {
                //扇面起点按锁定角算,和星魂的出生点是同一个点,不跟着还在转的头晃
                DrawFan(center + Context.LockedAngle.ToRotationVector2() * SageDirector.MuzzleDistance);
            }
            float crest = CrestGlow * blinkAlpha;
            if (crest > 0.01f || Context.CrestPulse > 0.01f) {
                DrawCrest(center, crest, Context.CrestPulse * blinkAlpha, time);
            }
            GodDraw.End();

            if (RigReady && shown > 0.01f) {
                Rig2DDrawContext world = Rig2DDrawContext.World();
                world.ViewOffset -= shake;
                DrawGhosts(spriteBatch, world, shown);
                Rig2DRenderer.DrawAll(spriteBatch, rig, world.Flat(Color.White, shown));
                if (BodyGlow > 0.01f) {
                    Rig2DRenderer.DrawAll(spriteBatch, rig, world.Flat(Overlay(SageVfx.StarBlue, BodyGlow * shown)).WithRibbonBlend(PureAdditive));
                }
            }
            if (Context.RingSlots > Context.RingFired && shown > 0.01f) {
                DrawOrbitRunes(center, shown);
            }

            GodDraw.BeginAdditive();
            if (Context.RingSlots > Context.RingFired && shown > 0.01f) {
                DrawOrbitGlow(center, shown, time);
            }
            if (RigReady && ringBone >= 0 && crest > 0.01f) {
                GodDraw.Centered(GodDraw.Glow, rig.Bones[ringBone].Pos + shake, GodDraw.Add(SageVfx.StarBlue, 0.45f * crest * shown), 0f, 0.9f);
            }
            if (CastGlow > 0.02f) {
                DrawSigil(center + facing * SageDirector.MuzzleDistance, CastGlow * shown, time);
            }
            if (Context.DeathStars > 0.01f) {
                DrawDeathStars(center, Context.DeathStars, time);
            }
            GodDraw.End();
            return false;
        }

        /// <summary>冲刺残影:拿姿态快照重画整副骨架,越旧越淡</summary>
        private void DrawGhosts(SpriteBatch spriteBatch, Rig2DDrawContext world, float shown) {
            for (int age = poseTrail.Slots - 1; age >= 0; age--) {
                Rig2DPoseTrail.Snapshot snapshot = poseTrail.Get(age);
                if (snapshot == null || snapshot.Extra.Length == 0) {
                    continue;
                }
                float fade = 1f - (ghostClock - snapshot.Extra[0]) / GhostLifeTicks;
                if (fade <= 0.02f) {
                    continue;
                }
                Rig2DDrawContext ghost = world.Flat(Overlay(SageVfx.StarBlue, 0.55f * fade * shown)).WithRibbonBlend(PureAdditive);
                Rig2DRenderer.DrawAll(spriteBatch, rig, snapshot.Bones, in ghost);
            }
        }

        #region 发光层(调用前须在加法批次里)
        /// <summary>
        /// 月轮:开口朝前的一弯光弧,两端收尖成月牙;阶段越高弧越长,三阶段合成满月的整环
        /// </summary>
        private void DrawCrest(Vector2 center, float glow, float pulse, float time) {
            int phase = Phase;
            float arc = SageDirector.CrestArc(phase);
            bool full = arc >= MathHelper.TwoPi - 0.01f;
            float radius = CrestRadius * (1f + 0.08f * (phase - 1) + 0.08f * pulse);
            float start = NPC.rotation + MathHelper.Pi - arc * 0.5f;
            for (int i = 0; i <= CrestSegments; i++) {
                crestPoints[i] = center + (start + arc * i / CrestSegments).ToRotationVector2() * radius;
            }
            //月牙两端收到 0,满月是等宽整环
            float Taper(float t) => full ? 1f : MathF.Sin(t * MathHelper.Pi);
            GodDraw.Strip(GodDraw.SoftLine, crestPoints, t => 30f * Taper(t) + 1f, t => GodDraw.Add(SageVfx.StarBlue, 0.4f * glow));
            GodDraw.Strip(GodDraw.SoftLine, crestPoints, t => 11f * Taper(t) + 0.5f, t => GodDraw.Add(SageVfx.MoonSilver, 0.35f + 0.65f * glow));
            GodDraw.Centered(GodDraw.Glow, center, GodDraw.Add(SageVfx.StarBlue, 0.3f * glow), 0f, 2.6f);

            if (!full) {
                //月牙尖上各一颗星
                float twinkle = 0.7f + 0.3f * MathF.Sin(time * 5f);
                GodDraw.Centered(GodDraw.ThinStar, crestPoints[0], GodDraw.Add(SageVfx.StarWhite, glow * twinkle), time, 0.34f);
                GodDraw.Centered(GodDraw.ThinStar, crestPoints[CrestSegments], GodDraw.Add(SageVfx.StarWhite, glow * twinkle), -time, 0.34f);
            }
            if (pulse > 0.01f) {
                GodDraw.Centered(GodDraw.Flash, center, GodDraw.Add(SageVfx.StarWhite, pulse), time, 0.6f + 0.8f * pulse);
                GodDraw.Centered(GodDraw.Star, center, GodDraw.Add(SageVfx.StarBlue, pulse), 0f, 0.5f + 0.9f * pulse);
            }
        }

        /// <summary>星遁落点:收缩的环加一颗慢慢变亮的四芒星,从星遁第一帧就在</summary>
        private void DrawBlinkMarker(float time) {
            float p = 1f - Context.BlinkTimer / (float)SageDirector.BlinkDuration;
            Vector2 at = Context.AnchorPos;
            GodDraw.Centered(GodDraw.BloomRing, at, GodDraw.Add(SageVfx.StarBlue, 0.5f + 0.5f * p), 0f, GodDraw.RingScale(MathHelper.Lerp(90f, 34f, p)));
            GodDraw.Centered(GodDraw.Star, at, GodDraw.Add(SageVfx.StarWhite, 0.4f + 0.6f * p), time * 0.8f, 0.25f + 0.25f * p);
            GodDraw.Centered(GodDraw.Glow, at, GodDraw.Add(SageVfx.StarViolet, 0.5f * p), 0f, 1.2f);
        }

        /// <summary>冲刺航道:底光带、两条边线标出实际扫过的宽度、中线和朝冲刺方向跑的光段</summary>
        private void DrawLane(float time) {
            float strength = Context.LaneStrength;
            Vector2 start = Context.LaneStart;
            Vector2 dir = Context.LaneDir;
            float length = Context.LaneLength;
            Vector2 end = start + dir * length;
            float half = SageDirector.ChargeLaneWidth * 0.5f;
            Vector2 edge = dir.RotatedBy(MathHelper.PiOver2) * half;

            GodDraw.Line(start, end, half * 1.25f, GodDraw.Add(SageVfx.StarViolet, 0.18f * strength), 0.1f);
            GodDraw.Line(start + edge, end + edge, 1.5f, GodDraw.Add(SageVfx.StarBlue, 0.6f * strength), 0.1f);
            GodDraw.Line(start - edge, end - edge, 1.5f, GodDraw.Add(SageVfx.StarBlue, 0.6f * strength), 0.1f);
            GodDraw.Line(start, end, 3f, GodDraw.Add(SageVfx.MoonSilver, 0.8f * strength), 0.06f);

            const float Gap = 220f;
            const float Dash = 90f;
            for (float d = time * 900f % Gap; d < length - Dash; d += Gap) {
                Vector2 from = start + dir * d;
                GodDraw.Line(from, from + dir * Dash, 4f, GodDraw.Add(SageVfx.StarWhite, 0.7f * strength), 0.35f);
            }
        }

        /// <summary>星流扇的航线:每道一条中线,外加一条和摆幅等宽的淡光带</summary>
        private void DrawFan(Vector2 muzzle) {
            int lanes = Context.FanLanes;
            float strength = Context.FanStrength;
            for (int i = 0; i < lanes; i++) {
                float angle = Context.LockedAngle + (i - (lanes - 1) * 0.5f) * SageDirector.TorrentLaneSpread;
                Vector2 end = muzzle + angle.ToRotationVector2() * SageDirector.TorrentLaneLength;
                GodDraw.Line(muzzle, end, SageDirector.TorrentWeaveAmplitude + 14f, GodDraw.Add(SageVfx.StarViolet, 0.14f * strength), 0.08f);
                GodDraw.Line(muzzle, end, 2.5f, GodDraw.Add(SageVfx.StarBlue, 0.7f * strength), 0.06f);
            }
        }

        private void DrawOrbitGlow(Vector2 center, float shown, float time) {
            int slots = Context.RingSlots;
            for (int i = Context.RingFired; i < slots; i++) {
                Vector2 pos = center + (Context.RingAngle + MathHelper.TwoPi * i / slots).ToRotationVector2() * SageDirector.RingSlotRadius;
                float glow = (0.5f + 0.5f * RingGlow) * shown;
                GodDraw.Centered(GodDraw.Glow, pos, GodDraw.Add(SageVfx.StarBlue, 0.6f * glow), 0f, 0.42f);
                GodDraw.Centered(GodDraw.ThinStar, pos, GodDraw.Add(SageVfx.StarWhite, 0.8f * glow), time * 1.5f + i, 0.26f);
            }
        }

        /// <summary>嘴前的星印</summary>
        private static void DrawSigil(Vector2 pos, float strength, float time) {
            GodDraw.Centered(GodDraw.Glow, pos, GodDraw.Add(SageVfx.StarBlue, 0.6f * strength), 0f, 0.6f);
            GodDraw.Centered(GodDraw.BloomRing, pos, GodDraw.Add(SageVfx.MoonSilver, 0.8f * strength), 0f, GodDraw.RingScale(18f + 4f * MathF.Sin(time * 6f)));
            GodDraw.Centered(GodDraw.ThinStar, pos, GodDraw.Add(SageVfx.StarWhite, strength), time * 2f, 0.4f * strength);
            GodDraw.Centered(GodDraw.ThinStar, pos, GodDraw.Add(SageVfx.StarViolet, 0.8f * strength), -time * 1.3f + 0.6f, 0.28f * strength);
        }

        /// <summary>死亡演出:体内逐颗点亮的星,以及一枚随强度越拉越长的十字星芒</summary>
        private void DrawDeathStars(Vector2 center, float strength, float time) {
            const int Stars = 9;
            for (int i = 0; i < Stars; i++) {
                float open = GodDraw.Span(strength, i / (float)Stars, i / (float)Stars + 0.25f);
                if (open <= 0f) {
                    continue;
                }
                //黄金角步进铺开,位置按下标固定
                Vector2 pos = center + (NPC.whoAmI + i * 2.39996f).ToRotationVector2() * (24f + 13f * i);
                GodDraw.Centered(GodDraw.Star, pos, GodDraw.Add(i % 2 == 0 ? SageVfx.StarWhite : SageVfx.StarBlue, open), time * (i % 2 == 0 ? 0.6f : -0.5f), 0.22f + 0.3f * open);
            }
            float length = 260f + 1000f * strength;
            float half = 5f + 15f * strength;
            for (int k = 0; k < 4; k++) {
                Vector2 dir = (MathHelper.PiOver2 * k).ToRotationVector2();
                GodDraw.Line(center, center + dir * length, half * 2.2f, GodDraw.Add(SageVfx.StarViolet, 0.4f * strength), 0.3f);
                GodDraw.Line(center, center + dir * length, half, GodDraw.Add(SageVfx.StarBlue, 0.9f * strength), 0.3f);
                GodDraw.Line(center, center + dir * length, half * 0.35f, GodDraw.Add(SageVfx.StarWhite, strength), 0.3f);
            }
        }
        #endregion

        /// <summary>环身星符的像素本体,走默认批次,尖端朝外</summary>
        private void DrawOrbitRunes(Vector2 center, float shown) {
            Texture2D mark = CEUtils.RequestTex(MarkTexture);
            int slots = Context.RingSlots;
            for (int i = Context.RingFired; i < slots; i++) {
                float angle = Context.RingAngle + MathHelper.TwoPi * i / slots;
                Vector2 pos = center + angle.ToRotationVector2() * SageDirector.RingSlotRadius;
                Main.spriteBatch.Draw(mark, pos - Main.screenPosition, null, Color.White * shown, angle + MathHelper.PiOver2, mark.Size() / 2f, 1.3f, SpriteEffects.None, 0f);
            }
        }
    }
}