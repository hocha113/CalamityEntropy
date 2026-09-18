using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 深空红魔的红射线(死亡探照灯):从 Z 2.5 背景里的红恶魔射向镜头的锥形 Z 射线,落点是平面上半径 RedRaySpotRadius 的光盘,
    /// 本弹幕的 Center 就是光盘。ai[0] 发射帧数,ai[1] 预警帧数,ai[2] = 本体 whoAmI + 1(红魔位置 = 本体 AnchorPos 在 RedDevilDepth 的投影)。
    /// 预警期光盘紧跟玩家、红魔到光盘拉一条越来越粗的预警锥;发射期光盘每帧最多挪 4px 慢慢追,判定圆只在发射期。
    /// 命中 396,无减益;FTW 光盘加大 30%
    /// </summary>
    public class VDRedRay : VDHostileProjectile
    {
        public static readonly Color RayColor = new Color(255, 60, 60);
        public static readonly Color RayCore = new Color(255, 200, 160);

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DefaultTimeLeft => 120;

        public int Duration => (int)Math.Max(Projectile.ai[0], 10f);
        public int WarnFrames => (int)Math.Max(Projectile.ai[1], 6f);
        public float Radius => VDDirector.RedRaySpotRadius * (Main.getGoodWorld ? 1.3f : 1f);
        public int Age => (int)Projectile.localAI[1];
        public bool Firing => Age >= WarnFrames && Age < WarnFrames + Duration;

        private VoidDestroyerNPC OwnerBoss {
            get {
                int idx = (int)Projectile.ai[2] - 1;
                if (idx < 0 || idx >= Main.maxNPCs || !Main.npc[idx].active || Main.npc[idx].ModNPC is not VoidDestroyerNPC boss) {
                    return null;
                }
                return boss;
            }
        }

        /// <summary>射线源:红魔的投影位置(没有本体时从光盘正上方远处打下来)</summary>
        private Vector2 SourceShown {
            get {
                VoidDestroyerNPC boss = OwnerBoss;
                if (boss == null) {
                    return Projectile.Center + new Vector2(0f, -600f);
                }
                return VDDepth.Project(boss.AnchorPos, VDDirector.RedDevilDepth);
            }
        }

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>宽度包络:发射前 6 帧张开,最后 10 帧收拢</summary>
        public float Envelope() {
            if (!Firing) {
                return 0f;
            }
            int fireAge = Age - WarnFrames;
            float open = MathHelper.Clamp(fireAge / 6f, 0f, 1f);
            float close = MathHelper.Clamp((WarnFrames + Duration - Age) / 10f, 0f, 1f);
            return Math.Min(open, close);
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = WarnFrames + Duration;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("VoidAnticipation", 1.1f, Projectile.Center, 3, 0.8f);
                }
            }
            Projectile.localAI[1]++;
            //追的是本体的目标;没有本体就追最近的玩家
            VoidDestroyerNPC boss = OwnerBoss;
            Player target = null;
            if (boss != null && boss.NPC.target >= 0 && boss.NPC.target < Main.maxPlayers) {
                Player t = Main.player[boss.NPC.target];
                target = t.active && !t.dead ? t : null;
            }
            if (target == null) {
                int closest = Player.FindClosest(Projectile.Center, 1, 1);
                target = closest >= 0 ? Main.player[closest] : null;
            }
            if (target != null) {
                if (Age < WarnFrames) {
                    //预警期紧跟:光盘钉在玩家脚下,逃不掉但看得见
                    Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center, VDDirector.RedRayWarnTrack);
                }
                else {
                    //发射期慢追:跑得动、站不住
                    Vector2 to = target.Center - Projectile.Center;
                    float dist = to.Length();
                    if (dist > 1f) {
                        Projectile.Center += to / dist * Math.Min(dist, VDDirector.RedRaySpotTrackSpeed);
                    }
                }
            }
            if (Age == WarnFrames && !Main.dedServ) {
                CEUtils.PlaySound("void_laser", 0.7f, Projectile.Center, 3, 1f);
                CEUtils.SetShake(Projectile.Center, 7f, 2400f);
            }
            float env = Envelope();
            Lighting.AddLight(Projectile.Center, RayColor.ToVector3() * (0.3f + 0.8f * env));
            if (!Main.dedServ && Firing && Main.rand.NextBool(2)) {
                Vector2 pos = Projectile.Center + CEUtils.randomPointInCircle(Radius * 0.8f);
                Vector2 v = (pos - Projectile.Center).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 5f);
                VDVfx.Spark(pos, v, RayColor, Main.rand.NextFloat(0.4f, 0.8f), 0.9f, 16);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Envelope() < 0.5f) {
                return false;
            }
            Vector2 nearest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return nearest.DistanceSQ(Projectile.Center) <= Radius * Radius;
        }

        public override bool PreDraw(ref Color lightColor) {
            Vector2 from = SourceShown;
            Vector2 spot = Projectile.Center;
            float radius = Radius;
            if (Age < WarnFrames) {
                //预警锥:红魔 → 光盘,随进度变粗变亮;光盘处一枚收紧的标记环
                float p = Age / (float)WarnFrames;
                float flicker = WarnFrames - Age <= 8 ? 0.6f + 0.4f * MathF.Sin(Age * 1.4f) : 1f;
                VDBeamDraw.DrawTapered(from, spot, 2f + 2f * p, 10f + 40f * p, RayColor, RayCore, 1f, (0.25f + 0.45f * p) * flicker, Projectile.whoAmI * 0.37f, endGlow: false);
                VDVfx.DrawDepthMarker(spot, p, radius, RayColor, 0.9f);
                return false;
            }
            float env = Envelope();
            if (env <= 0.01f) {
                return false;
            }
            //射线主体:从背景里的红魔打到脚下,近端粗(光盘直径),VDVoidBeam 着色器的红色板
            VDBeamDraw.DrawTapered(from, spot, 10f * env, radius * 2f * env, RayColor, RayCore, 1f, 1f, Projectile.whoAmI * 0.37f, endGlow: false);
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Vector2 pos = spot - Main.screenPosition;
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glow, pos, null, RayColor * (0.9f * env), 0f, glow.Size() / 2f, radius * 2.6f / glow.Width, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, pos, null, RayCore * (0.8f * env), 0f, glow.Size() / 2f, radius * 1.3f / glow.Width, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, pos, null, RayColor * (0.9f * env), Age * 0.05f, ring.Size() / 2f, radius * 2f / ring.Width, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
