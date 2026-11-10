using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess
{
    /// <summary>
    /// 绘制:航道预告 → 日轮 → 残影 → 本体(双翼、身体、双臂) → 白热叠层 → 手印 → 死亡光柱
    /// 本体是有遮挡的像素贴图,走默认批次;发光层分前后两段进加法批次
    /// </summary>
    public partial class SunGoddess
    {
        private const string TexDir = "CalamityEntropy/Content/NPCs/SunMoonGods/Goddess/";
        /// <summary>日轮光环的基准半径,随阶段放大</summary>
        private const float HaloRadius = 84f;
        private const int DeathRayCount = 9;

        private static Texture2D ArmTex => CEUtils.RequestTex(TexDir + "SunGoddessArm");
        private static Texture2D WingTex => CEUtils.RequestTex(TexDir + "SunGoddessWing");

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (NPC.IsABestiaryIconDummy || Context == null) {
                return false;
            }
            float shown = Alpha * blinkAlpha;
            float time = Main.GlobalTimeWrappedHourly;
            //抖动只是绘制偏移,用时间算而不掷骰,暂停时画面不跳
            Vector2 shake = new Vector2(MathF.Sin(time * 97f), MathF.Cos(time * 71f)) * (5f * Context.ShakeStrength);
            Vector2 center = NPC.Center + shake;
            float rot = NPC.rotation;
            float swing = (WingPose > 0f ? WingPose * 0.75f : WingPose * 0.24f) + 0.05f * MathF.Sin(wingPhase);
            int armLeft = Math.Clamp(Context.ArmLeft, 0, SunDirector.ArmFrames - 1);
            int armRight = Math.Clamp(Context.ArmRight, 0, SunDirector.ArmFrames - 1);

            GodDraw.BeginAdditive();
            if (Context.LaneStrength > 0.01f) {
                DrawLane(time);
            }
            float halo = HaloGlow * blinkAlpha;
            if (halo > 0.01f || Context.HaloPulse > 0.01f) {
                DrawHalo(center + HaloOffset.RotatedBy(rot), halo, Context.HaloPulse * blinkAlpha, time);
            }
            if (shown > 0.05f) {
                DrawAfterimages(shown, swing, armLeft, armRight);
                //翼后托一层火光
                DrawWings(center, rot, GodDraw.Add(SunVfx.Ember, (0.2f + 0.45f * HaloGlow) * shown), swing, 1.05f);
            }
            GodDraw.End();

            if (shown > 0.01f) {
                DrawFigure(center, rot, Color.White * shown, swing, armLeft, armRight);
            }

            bool sigils = CastLeft > 0.02f || CastRight > 0.02f;
            if (BodyGlow > 0.01f || sigils || Context.DeathRays > 0.01f) {
                GodDraw.BeginAdditive();
                if (Context.DeathRays > 0.01f) {
                    DrawDeathRays(center, Context.DeathRays, time);
                }
                if (BodyGlow > 0.01f && shown > 0.01f) {
                    DrawFigure(center, rot, GodDraw.Add(SunVfx.Gold, BodyGlow * shown), swing, armLeft, armRight);
                    GodDraw.Centered(GodDraw.Glow, center, GodDraw.Add(SunVfx.Ember, 0.5f * BodyGlow * shown), 0f, 1.7f);
                }
                if (CastLeft > 0.02f) {
                    DrawSigil(HandWorld(center, rot, -1, armLeft), CastLeft * shown, time);
                }
                if (CastRight > 0.02f) {
                    DrawSigil(HandWorld(center, rot, 1, armRight), CastRight * shown, time + 1.7f);
                }
                GodDraw.End();
            }
            return false;
        }

        #region 本体
        /// <summary>翼根相对身体中心的位置,由左翼贴图中心偏移和翼根像素坐标折算</summary>
        private static Vector2 WingRoot(Texture2D wing) => SunDirector.WingOffset + SunDirector.WingPivot - wing.Size() / 2f;

        /// <summary>swing 为正时双翼朝下收拢,为负时上扬;右翼是左翼的镜像</summary>
        private void DrawWings(Vector2 center, float rot, Color color, float swing, float scale = 1f) {
            Texture2D wing = WingTex;
            Vector2 at = center - Main.screenPosition;
            Vector2 root = WingRoot(wing);
            Vector2 rootRight = new Vector2(-root.X, root.Y);
            Vector2 originRight = new Vector2(wing.Width - SunDirector.WingPivot.X, SunDirector.WingPivot.Y);
            Main.spriteBatch.Draw(wing, at + root.RotatedBy(rot), null, color, rot - swing, SunDirector.WingPivot, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(wing, at + rootRight.RotatedBy(rot), null, color, rot + swing, originRight, scale, SpriteEffects.FlipHorizontally, 0f);
        }

        private void DrawFigure(Vector2 center, float rot, Color color, float swing, int armLeft, int armRight) {
            DrawWings(center, rot, color, swing);
            Texture2D body = TextureAssets.Npc[Type].Value;
            Main.spriteBatch.Draw(body, center - Main.screenPosition, null, color, rot, body.Size() / 2f, 1f, SpriteEffects.None, 0f);
            //帧 5 的手臂横过身前,要盖在另一只手上
            if (armLeft == 5) {
                DrawArm(center, rot, color, armRight, true);
                DrawArm(center, rot, color, armLeft, false);
            }
            else {
                DrawArm(center, rot, color, armLeft, false);
                DrawArm(center, rot, color, armRight, true);
            }
        }

        private static void DrawArm(Vector2 center, float rot, Color color, int frame, bool right) {
            Texture2D arm = ArmTex;
            Rectangle source = arm.Frame(1, SunDirector.ArmFrames, 0, frame);
            Vector2 offset = new Vector2(right ? -SunDirector.ArmOffset.X : SunDirector.ArmOffset.X, SunDirector.ArmOffset.Y);
            Main.spriteBatch.Draw(arm, center - Main.screenPosition + offset.RotatedBy(rot), source, color, rot, source.Size() / 2f, 1f,
                right ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        }

        /// <summary>冲刺残影,速度过线才画,越旧越淡</summary>
        private void DrawAfterimages(float shown, float swing, int armLeft, int armRight) {
            float speed = NPC.velocity.Length();
            float strength = GodDraw.Span(speed, 14f, 34f) * shown;
            if (strength <= 0.01f) {
                return;
            }
            for (int i = NPC.oldPos.Length - 1; i >= 1; i--) {
                if (NPC.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float fade = 1f - i / (float)NPC.oldPos.Length;
                DrawFigure(NPC.oldPos[i] + NPC.Size / 2f, NPC.oldRot[i], GodDraw.Add(SunVfx.Ember, 0.55f * fade * strength), swing, armLeft, armRight);
            }
        }
        #endregion

        #region 发光层(调用前须在加法批次里)
        /// <summary>日轮:底光、两层反向转的日冕、内外光环、一圈长短相间的光芒,阶段越高越大、光芒越多</summary>
        private void DrawHalo(Vector2 haloCenter, float glow, float pulse, float time) {
            int phase = Phase;
            float size = 1f + 0.12f * (phase - 1) + 0.1f * pulse;
            float radius = HaloRadius * size;

            GodDraw.Centered(GodDraw.Glow, haloCenter, GodDraw.Add(SunVfx.Ember, 0.35f * glow), 0f, 2.1f * size);
            GodDraw.Centered(GodDraw.Corona, haloCenter, GodDraw.Add(SunVfx.Ember, 0.55f * glow), time * 0.25f, 0.62f * size);
            GodDraw.Centered(GodDraw.Corona, haloCenter, GodDraw.Add(SunVfx.Gold, 0.4f * glow), -time * 0.18f, 0.5f * size);
            GodDraw.Centered(GodDraw.BloomRing, haloCenter, GodDraw.Add(SunVfx.Gold, 0.3f + 0.7f * glow), 0f, GodDraw.RingScale(radius));
            GodDraw.Centered(GodDraw.BloomRing, haloCenter, GodDraw.Add(SunVfx.Ember, 0.5f * glow), 0f, GodDraw.RingScale(radius * 1.22f));

            int spokes = 8 + 4 * (phase - 1);
            float from = radius + 8f;
            for (int i = 0; i < spokes; i++) {
                bool major = i % 2 == 0;
                Vector2 dir = (time * 0.2f + MathHelper.TwoPi * i / spokes).ToRotationVector2();
                float length = (major ? 64f : 36f) * size * (0.5f + 0.5f * glow) * (1f + 0.6f * pulse);
                GodDraw.Line(haloCenter + dir * from, haloCenter + dir * (from + length), major ? 5f : 3.5f,
                    GodDraw.Add(SunVfx.Gold, 0.25f + 0.65f * glow), 0.5f);
            }
            if (pulse > 0.01f) {
                GodDraw.Centered(GodDraw.Flash, haloCenter, GodDraw.Add(SunVfx.Core, pulse), time, (0.5f + 0.7f * pulse) * size);
            }
        }

        /// <summary>冲刺航道:底光带、两条边线标出实际扫过的宽度、中线和朝冲刺方向跑的光段</summary>
        private void DrawLane(float time) {
            float strength = Context.LaneStrength;
            Vector2 start = Context.LaneStart;
            Vector2 dir = Context.LaneDir;
            float length = Context.LaneLength;
            Vector2 end = start + dir * length;
            float half = SunDirector.DashLaneWidth * 0.5f;
            Vector2 edge = dir.RotatedBy(MathHelper.PiOver2) * half;

            GodDraw.Line(start, end, half * 1.25f, GodDraw.Add(SunVfx.Ember, 0.2f * strength), 0.1f);
            GodDraw.Line(start + edge, end + edge, 1.5f, GodDraw.Add(SunVfx.Gold, 0.5f * strength), 0.1f);
            GodDraw.Line(start - edge, end - edge, 1.5f, GodDraw.Add(SunVfx.Gold, 0.5f * strength), 0.1f);
            GodDraw.Line(start, end, 3f, GodDraw.Add(SunVfx.Gold, 0.8f * strength), 0.06f);

            const float Gap = 220f;
            const float Dash = 90f;
            for (float d = time * 900f % Gap; d < length - Dash; d += Gap) {
                Vector2 from = start + dir * d;
                GodDraw.Line(from, from + dir * Dash, 4f, GodDraw.Add(SunVfx.Core, 0.7f * strength), 0.35f);
            }
        }

        private static Vector2 HandWorld(Vector2 center, float rot, int side, int armFrame) {
            Vector2 offset = armFrame == 4 ? SunDirector.HandRaisedOffset : SunDirector.HandCastOffset;
            return center + new Vector2(side * offset.X, offset.Y).RotatedBy(rot);
        }

        /// <summary>掌心的小日轮</summary>
        private static void DrawSigil(Vector2 pos, float strength, float time) {
            GodDraw.Centered(GodDraw.Glow, pos, GodDraw.Add(SunVfx.Ember, 0.6f * strength), 0f, 0.45f);
            GodDraw.Centered(GodDraw.BloomRing, pos, GodDraw.Add(SunVfx.Gold, 0.8f * strength), 0f, GodDraw.RingScale(13f + 3f * MathF.Sin(time * 6f)));
            GodDraw.Centered(GodDraw.ThinStar, pos, GodDraw.Add(SunVfx.Core, strength), time * 2f, 0.32f * strength);
            GodDraw.Centered(GodDraw.ThinStar, pos, GodDraw.Add(SunVfx.Gold, 0.8f * strength), -time * 1.3f + 0.6f, 0.22f * strength);
        }

        /// <summary>死亡演出的光柱,随强度逐道迸出,方位按下标固定</summary>
        private void DrawDeathRays(Vector2 center, float strength, float time) {
            for (int i = 0; i < DeathRayCount; i++) {
                float open = GodDraw.Span(strength, i / (float)DeathRayCount, i / (float)DeathRayCount + 0.25f);
                if (open <= 0f) {
                    continue;
                }
                //黄金角步进让相邻光柱落在远离彼此的方位
                float angle = NPC.whoAmI + i * 2.39996f + time * (i % 2 == 0 ? 0.25f : -0.2f);
                Vector2 dir = angle.ToRotationVector2();
                float length = (520f + 60f * (i % 4)) * open;
                float half = (10f + 3f * (i % 3)) * open;
                GodDraw.Line(center, center + dir * length, half * 2.2f, GodDraw.Add(SunVfx.Ember, 0.45f * open), 0.3f);
                GodDraw.Line(center, center + dir * length, half, GodDraw.Add(SunVfx.Gold, 0.9f * open), 0.3f);
                GodDraw.Line(center, center + dir * length, half * 0.35f, GodDraw.Add(SunVfx.Core, open), 0.3f);
            }
        }
        #endregion
    }
}