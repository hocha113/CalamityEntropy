using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SandseaDragon
{
    /// <summary>
    /// 表现层,纯本地:整条身体由头部按尾到头的顺序集中绘制,走物块之后的那一层,埋进沙里的部分被物块挡住
    /// 航道与弹道预告画在物块之上,由 SandseaOverlaySystem 调 DrawOverlay
    /// 抖动只是绘制偏移,判定盒不动
    /// </summary>
    public partial class SandseaDragonHead
    {
        private const string TexRoot = "CalamityEntropy/Content/NPCs/SandseaDragon/";
        private static readonly string[] PieceNames =
        {
            "SandseaDragonSkull", "SandseaDragonBody1", "SandseaDragonBody2", "SandseaDragonBody3", "SandseaDragonTail",
        };
        private static Asset<Texture2D>[] pieceTex;
        private static Asset<Texture2D>[] glowTex;
        private static Asset<Texture2D> jawTex;
        private bool tailWasBuried;

        private static void EnsureTextures() {
            if (pieceTex != null) {
                return;
            }
            Asset<Texture2D>[] pieces = new Asset<Texture2D>[PieceNames.Length];
            Asset<Texture2D>[] glows = new Asset<Texture2D>[PieceNames.Length];
            for (int i = 0; i < PieceNames.Length; i++) {
                pieces[i] = ModContent.Request<Texture2D>(TexRoot + PieceNames[i], AssetRequestMode.ImmediateLoad);
                glows[i] = ModContent.Request<Texture2D>(TexRoot + PieceNames[i] + "_Glow", AssetRequestMode.ImmediateLoad);
            }
            jawTex = ModContent.Request<Texture2D>(TexRoot + "SandseaDragonJaw", AssetRequestMode.ImmediateLoad);
            glowTex = glows;
            pieceTex = pieces;
        }

        public override void Unload() {
            pieceTex = null;
            glowTex = null;
            jawTex = null;
        }

        #region 本地特效
        private void UpdateLocalEffects() {
            if (!spineReady) {
                return;
            }
            if (Glow > 0.05f) {
                Lighting.AddLight(NPC.Center, SandseaVfx.FinGlow.ToVector3() * (0.7f * Glow * Alpha));
            }

            //头与尾尖穿过沙面时各溅一次
            bool buried = SandseaTerrain.Buried(NPC.Center);
            bool tailBuried = SandseaTerrain.Buried(Joints[SandseaDirector.SegmentCount]);
            if (buriedKnown && Alpha > 0.3f) {
                if (buried != wasBuried) {
                    SandseaVfx.SandBurst(NPC.Center, 2.2f, NPC.velocity * 0.25f);
                    SandseaVfx.BreachSound(NPC.Center);
                    SandseaVfx.Shake(NPC.Center, 3f, 1600f);
                }
                if (tailBuried != tailWasBuried) {
                    SandseaVfx.SandBurst(Joints[SandseaDirector.SegmentCount], 1.2f, NPC.velocity * 0.15f);
                }
            }
            wasBuried = buried;
            tailWasBuried = tailBuried;
            buriedKnown = true;

            //浅游时背鳍划开沙面
            if (buried && NPC.velocity.LengthSquared() > 9f) {
                float surface = SandseaTerrain.Surface(NPC.Center.X, NPC.Center.Y);
                if (NPC.Center.Y - surface < 170f) {
                    float dirX = Math.Sign(NPC.velocity.X);
                    SandseaVfx.Wake(new Vector2(NPC.Center.X, surface), dirX);
                    SandseaVfx.Wake(new Vector2(Joints[2].X, surface), dirX);
                }
            }

            if (Context.OmenStrength > 0.01f) {
                if (Main.rand.NextFloat() < 0.25f + 0.6f * Context.OmenStrength) {
                    SandseaVfx.SandBurst(Context.OmenPos, 0.35f + 0.8f * Context.OmenStrength);
                }
                if (Main.GameUpdateCount % 9 == 0) {
                    SandseaVfx.Shake(Context.OmenPos, 1.5f * Context.OmenStrength, 1400f);
                }
            }

            if (Context.WhiteBurst > 0.01f) {
                for (int k = 0; k <= SandseaDirector.SegmentCount; k++) {
                    //埋在沙里的关节不喷,粒子画在物块之上,会凭空从地里冒出来
                    if (!SandseaTerrain.Buried(Joints[k])) {
                        SandseaVfx.WhiteSpray(Joints[k], 1 + (int)(3f * Context.WhiteBurst));
                    }
                }
            }

            if (Glow > 0.4f && Alpha > 0.3f && Main.rand.NextBool(5)) {
                int seg = Main.rand.Next(SandseaDirector.SegmentCount);
                //火花画在物块之上,埋着的节不冒
                if (!SandseaTerrain.Buried(Joints[seg])) {
                    SandseaVfx.FinSparks(Joints[seg] + Main.rand.NextVector2Circular(40f, 40f), 1, 2f);
                }
            }
        }
        #endregion

        #region 本体绘制
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (NPC.IsABestiaryIconDummy) {
                return true;
            }
            if (!spineReady || Context == null) {
                return false;
            }
            EnsureTextures();
            Vector2 shake = Context.ShakeStrength > 0f ? Main.rand.NextVector2Circular(1f, 1f) * (7f * Context.ShakeStrength) : Vector2.Zero;
            int visible = SandseaDirector.SegmentCount - Context.SegmentsGone;
            for (int seg = visible - 1; seg >= 0; seg--) {
                DrawSegment(spriteBatch, screenPos, seg, shake);
            }
            if (visible > 0) {
                DrawHeadLights(screenPos, shake);
            }
            return false;
        }

        /// <summary>
        /// 这一节露在物块外的程度:两个关节各占一半
        /// 没照亮的地下物块原版是不画的,埋着的节若还补亮或发光,身体会隔着沙子透出来
        /// </summary>
        private float SegmentExposed(int seg)
            => (SandseaTerrain.Buried(Joints[seg]) ? 0f : 0.5f) + (SandseaTerrain.Buried(Joints[seg + 1]) ? 0f : 0.5f);

        private Color SegmentLight(int seg, float exposed) {
            Vector2 mid = (Joints[seg] + Joints[seg + 1]) * 0.5f;
            Color light = Lighting.GetColor((int)(mid.X / 16f), (int)(mid.Y / 16f));
            //夜里跃到空中也要看得清轮廓,埋着的部分不补亮
            return Color.Lerp(light, Color.White, 0.18f * exposed) * Alpha;
        }

        private void DrawSegment(SpriteBatch spriteBatch, Vector2 screenPos, int seg, Vector2 shake) {
            Texture2D tex = pieceTex[seg].Value;
            float rotation = SegmentDrawRotation(seg, out bool flip);
            Vector2 front = SandseaDirector.JointFront[seg];
            Vector2 origin = flip ? new Vector2(tex.Width - front.X, front.Y) : front;
            SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 pos = Joints[seg] + shake - screenPos;
            float exposed = SegmentExposed(seg);
            Color color = SegmentLight(seg, exposed);

            if (seg == 0) {
                //下颌压在头骨下面,绕转轴张合,闭合时与原图逐像素一致
                Texture2D jaw = jawTex.Value;
                Vector2 hinge = SandseaDirector.JawHinge;
                Vector2 jawOrigin = flip ? new Vector2(jaw.Width - hinge.X, hinge.Y) : hinge;
                float open = JawOpen * SandseaDirector.JawMaxOpen * (flip ? 1f : -1f);
                spriteBatch.Draw(jaw, SegmentPoint(0, hinge) + shake - screenPos, null, color, rotation + open, jawOrigin, NPC.scale, effects, 0f);
            }

            spriteBatch.Draw(tex, pos, null, color, rotation, origin, NPC.scale, effects, 0f);

            if (Glow > 0.01f && exposed > 0f) {
                float pulse = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4f + seg * 0.9f);
                Color glow = SandseaVfx.FinGlow * (Glow * pulse * Alpha * exposed);
                glow.A = 0;
                spriteBatch.Draw(glowTex[seg].Value, pos, null, glow, rotation, origin, NPC.scale, effects, 0f);
            }

            //锁血喷白:整条身体跟着泛白,闪烁由强度乘一个快速正弦
            if (Context.WhiteBurst > 0.01f && exposed > 0f) {
                float flicker = 0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 26f + seg);
                Color flash = Color.White * (0.5f * Context.WhiteBurst * flicker * Alpha * exposed);
                flash.A = 0;
                spriteBatch.Draw(tex, pos, null, flash, rotation, origin, NPC.scale, effects, 0f);
            }

            //冲刺屏障:前半身蔽一层薄薄的黄光
            if (Barrier > 0.02f && seg <= 1 && exposed > 0f) {
                Color shell = SandseaVfx.Forbidden * (0.4f * Barrier * Alpha * exposed);
                shell.A = 0;
                spriteBatch.Draw(tex, pos, null, shell, rotation, origin, NPC.scale, effects, 0f);
            }
        }

        /// <summary>眼部蓝光与冲刺屏障的弧光,都是光,进加法批次</summary>
        private void DrawHeadLights(Vector2 screenPos, Vector2 shake) {
            float eye = Math.Max(Context.EyeFlash, Glow * 0.35f) * Alpha;
            if (eye <= 0.02f && Barrier <= 0.02f) {
                return;
            }
            //头埋在物块里时不画光,理由同 SegmentExposed
            if (SandseaTerrain.Buried(Joints[0])) {
                return;
            }
            if (Barrier > 0.02f) {
                //纯加法的黄在亮天空上会被冲成白,先垫两道带 alpha 的实色弧,屏障才读得出是黄的
                Vector2 shield = Joints[0] + shake;
                float facing = SegmentAngles[0];
                GodDraw.BeginStraightAlpha();
                GodDraw.RingStrip(TextureAssets.MagicPixel.Value, shield, 128f, 4f, GodDraw.Straight(SandseaVfx.Forbidden, 0.8f * Barrier * Alpha), facing - 1.05f, 2.1f, 28);
                GodDraw.RingStrip(TextureAssets.MagicPixel.Value, shield, 116f, 2f, GodDraw.Straight(new Color(255, 150, 40), 0.6f * Barrier * Alpha), facing - 0.85f, 1.7f, 24);
                GodDraw.End();
            }
            GodDraw.BeginAdditive();
            if (eye > 0.02f) {
                Vector2 eyePos = EyeWorld + shake;
                GodDraw.Centered(GodDraw.Glow, eyePos, GodDraw.Add(SandseaVfx.FinGlow, 0.9f * eye), 0f, 0.16f + 0.5f * Context.EyeFlash);
                if (Context.EyeFlash > 0.05f) {
                    GodDraw.Centered(GodDraw.ThinStar, eyePos, GodDraw.Add(Color.White, Context.EyeFlash), 0f, new Vector2(1.6f, 0.5f) * (0.3f + 0.7f * Context.EyeFlash));
                    GodDraw.Centered(GodDraw.ThinStar, eyePos, GodDraw.Add(SandseaVfx.FinGlow, Context.EyeFlash), MathHelper.PiOver2, new Vector2(1.1f, 0.4f) * (0.3f + 0.7f * Context.EyeFlash));
                }
            }
            if (Barrier > 0.02f) {
                Vector2 center = Joints[0] + shake;
                float angle = SegmentAngles[0];
                float flicker = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 30f);
                GodDraw.RingStrip(GodDraw.SoftLine, center, 128f, 10f, GodDraw.Add(SandseaVfx.Forbidden, 0.9f * Barrier * flicker), angle - 1.05f, 2.1f, 28);
                GodDraw.RingStrip(GodDraw.SoftLine, center, 112f, 6f, GodDraw.Add(SandseaVfx.SandLight, 0.5f * Barrier), angle - 0.8f, 1.6f, 24);
                GodDraw.Centered(GodDraw.Glow, center + angle.ToRotationVector2() * 96f, GodDraw.Add(SandseaVfx.Forbidden, 0.35f * Barrier), angle, new Vector2(0.5f, 1.1f));
            }
            GodDraw.End();
        }
        #endregion

        #region 预告层(物块之上)
        /// <summary>航道与弹道预告,用带 alpha 的橙红实色画,亮沙地和亮天空上都看得见</summary>
        internal void DrawOverlay(SpriteBatch spriteBatch) {
            if (Context == null) {
                return;
            }
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle one = new Rectangle(0, 0, 1, 1);
            Color warn = new Color(255, 96, 40);

            if (Context.LaneStrength > 0.01f) {
                float s = MathHelper.Clamp(Context.LaneStrength, 0f, 1f);
                Vector2 dir = Context.LaneDir.SafeNormalize(Vector2.UnitX);
                Vector2 normal = new Vector2(-dir.Y, dir.X);
                float rotation = dir.ToRotation();
                Vector2 start = Context.LaneStart - Main.screenPosition;
                float half = Context.LaneHalfWidth;
                //淡底加两条向前滚动的虚线边
                spriteBatch.Draw(pixel, start, one, warn * (0.12f * s), rotation, new Vector2(0f, 0.5f), new Vector2(Context.LaneLength, half * 2f), SpriteEffects.None, 0f);
                const float Dash = 34f;
                const float Gap = 22f;
                float scroll = Main.GlobalTimeWrappedHourly * 220f % (Dash + Gap);
                for (float d = scroll - Dash; d < Context.LaneLength; d += Dash + Gap) {
                    float from = Math.Max(d, 0f);
                    float to = Math.Min(d + Dash, Context.LaneLength);
                    if (to <= from) {
                        continue;
                    }
                    float fade = 1f - from / Context.LaneLength * 0.7f;
                    Color c = warn * (0.85f * s * fade);
                    for (int side = -1; side <= 1; side += 2) {
                        spriteBatch.Draw(pixel, start + dir * from + normal * (half * side), one, c, rotation, new Vector2(0f, 0.5f), new Vector2(to - from, 4f), SpriteEffects.None, 0f);
                    }
                }
            }

            if (Context.ArcStrength > 0.01f) {
                float s = MathHelper.Clamp(Context.ArcStrength, 0f, 1f);
                Vector2 v = Context.ArcVelocity;
                float g = Context.ArcGravity;
                //落回起点高度以下一截就停
                for (int t = 4; t < 220; t += 4) {
                    Vector2 p = Context.ArcStart + v * t + new Vector2(0f, 0.5f * g * t * t);
                    if (p.Y > Context.ArcStart.Y + 120f) {
                        break;
                    }
                    Vector2 tangent = v + new Vector2(0f, g * t);
                    float fade = 1f - t / 220f * 0.6f;
                    spriteBatch.Draw(pixel, p - Main.screenPosition, one, warn * (0.8f * s * fade), tangent.ToRotation(), new Vector2(0.5f, 0.5f), new Vector2(14f, 5f), SpriteEffects.None, 0f);
                }
            }
        }
        #endregion
    }

    /// <summary>物块画完后补画沙海冥龙的预告层</summary>
    public class SandseaOverlaySystem : ModSystem
    {
        public override void PostDrawTiles() {
            int type = ModContent.NPCType<SandseaDragonHead>();
            if (!NPC.AnyNPCs(type)) {
                return;
            }
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.type == type && npc.ModNPC is SandseaDragonHead head) {
                    head.DrawOverlay(Main.spriteBatch);
                }
            }
            Main.spriteBatch.End();
        }
    }
}
