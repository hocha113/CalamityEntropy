using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 全息弹幕公共部分:通过 ai[0] 找到本体,本体不在对应状态时自行消散;节拍读本体 Context 的表现通道,不读状态私有量。
    /// 建在深度基类上:全息体可以停在深处或穿过平面,深度的雾化对全息(自发光)只取三成,红魔在背景里仍是红的
    /// </summary>
    public abstract class VDHoloProjectile : VDDepthProjectile
    {
        public abstract VDStateIndex OwnerMode { get; }
        public abstract Color HoloColor { get; }
        /// <summary>全息体默认自己管标记(或不需要)</summary>
        public override bool WantsMarker => false;

        /// <summary>本体引用,无效时为 null</summary>
        protected VoidDestroyerNPC Owner {
            get {
                int idx = (int)Projectile.ai[0];
                if (idx < 0 || idx >= Main.maxNPCs) {
                    return null;
                }
                NPC npc = Main.npc[idx];
                if (!npc.active || npc.ModNPC is not VoidDestroyerNPC boss) {
                    return null;
                }
                return boss;
            }
        }

        /// <summary>本体仍在本状态里(且没死)</summary>
        protected bool OwnerActive {
            get {
                VoidDestroyerNPC boss = Owner;
                return boss != null && !boss.Dying && boss.CurrentStateIndex == OwnerMode;
            }
        }

        /// <summary>整体透明度:出现 12 帧渐显,寿命末尾 20 帧渐隐</summary>
        protected float HoloOpacity(float peak = 0.85f) {
            float fadeIn = MathHelper.Clamp(Projectile.localAI[1] / 12f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            return peak * Math.Min(fadeIn, fadeOut);
        }

        /// <summary>本体离开模式时收尾:把剩余寿命压到渐隐长度</summary>
        protected void FadeOutIfOrphaned() {
            if (!OwnerActive && Projectile.timeLeft > 20) {
                Projectile.timeLeft = 20;
            }
        }

        /// <summary>全息体的深度配色:自发光只吃三成雾</summary>
        protected static Color HoloTint(Color color, float z) => Color.Lerp(color, VDVfx.FarFog, VDDepth.FogAmount(z) * 0.35f);
    }

    /// <summary>
    /// 全息红恶魔(纯演出,无伤害):停在 Z <see cref="VDDirector.RedDevilDepth"/> 的远景层,平面位置 = 本体 AnchorPos
    /// (服务端按表观「玩家侧上方」换算),平面缩放 8 → 表观 2.3 倍,一尊压在虚空天幕上的巨影;按本体 Context.HoloCharge 蓄力发亮。
    /// 红射线(<see cref="VDRedRay"/>)从它射向镜头,三叉戟由本体状态从它的位置生成
    /// </summary>
    public class VDHoloRedDevil : VDHoloProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override VDStateIndex OwnerMode => VDStateIndex.RedHell;
        public override Color HoloColor => VDHologramDraw.HellRed;
        public override int DefaultTimeLeft => 900;
        protected override float WhooshStrength => 0f;

        private Vector2 lastAnchor;
        private float blinkPop;

        public override void SetExtraDefaults() {
            Projectile.width = 40;
            Projectile.height = 60;
            Projectile.hostile = false;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        protected override void DepthAI() {
            Projectile.localAI[1]++;
            //深度钉死,不积分
            Z = VDDirector.RedDevilDepth;
            ZVel = 0f;
            ZAccel = 0f;
            FadeOutIfOrphaned();
            VoidDestroyerNPC boss = Owner;
            if (boss == null) {
                return;
            }
            //本体进入收尾拍时提前渐隐
            if (boss.CurrentStateIndex == OwnerMode && boss.Context.HoloWrapUp && Projectile.timeLeft > 20) {
                Projectile.timeLeft = 20;
            }
            Vector2 anchor = boss.AnchorPos;
            if (Projectile.localAI[1] <= 1f) {
                lastAnchor = anchor;
                Projectile.Center = anchor;
            }
            if (Vector2.DistanceSquared(anchor, lastAnchor) > 16f) {
                //锚点换了 = 红恶魔传送:旧位置爆一圈全息碎片,新位置弹一下(粒子放在投影位置)
                if (!Main.dedServ) {
                    SpawnHoloBurst(ProjectedCenter, HoloColor);
                    SpawnHoloBurst(VDDepth.Project(anchor, Z), HoloColor);
                }
                lastAnchor = anchor;
                blinkPop = 1f;
            }
            //浮动量按深度放大,表观上仍是 8px
            Projectile.Center = anchor + new Vector2(0, MathF.Sin(Projectile.localAI[1] * 0.06f) * 8f / VDDepth.Scale(Z));
            blinkPop *= 0.85f;
            Projectile.direction = boss.Target.Center.X > VDDepth.Project(Projectile.Center, Z).X ? 1 : -1;

            //蓄力粒子:跟本体声明的蓄力读数走,放在投影位置
            if (!Main.dedServ && !boss.Context.HoloWrapUp && boss.Context.HoloCharge > 0.02f && Main.rand.NextBool(2)) {
                Vector2 shown = ProjectedCenter;
                Vector2 from = shown + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(70f, 160f);
                Vector2 v = (shown - from) * 0.08f;
                VDVfx.Spark(from, v, HoloColor, Main.rand.NextFloat(0.4f, 0.8f), 0.9f, 13);
            }
        }

        public static void SpawnHoloBurst(Vector2 pos, Color color) {
            for (int i = 0; i < 14; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 9f);
                VDVfx.Spark(pos, v, color, Main.rand.NextFloat(0.5f, 1f), 1f, 22, gravity: true);
            }
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            Main.instance.LoadNPC(NPCID.RedDevil);
            Texture2D tex = TextureAssets.Npc[NPCID.RedDevil].Value;
            int frames = Math.Max(1, Main.npcFrameCount[NPCID.RedDevil]);
            int frameH = tex.Height / frames;
            int frame = (int)(Main.GlobalTimeWrappedHourly * 7f) % frames;
            Rectangle src = new Rectangle(0, frame * frameH, tex.Width, frameH);
            Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
            SpriteEffects fx = Projectile.direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float opacity = HoloOpacity() * MathHelper.Lerp(1f, VDDepth.Alpha(Z), 0.5f);
            float scale = (VDDirector.RedDevilPlaneScale + blinkPop * 1.5f) * VDDepth.Scale(Z);
            Vector2 drawPos = ProjectedCenter - Main.screenPosition;
            Color tint = HoloTint(HoloColor, Z);

            VoidDestroyerNPC boss = Owner;
            float charge = 0f;
            if (boss != null && boss.CurrentStateIndex == OwnerMode && !boss.Context.HoloWrapUp) {
                charge = MathHelper.Clamp(boss.Context.HoloCharge, 0f, 1f);
            }

            spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            spriteBatch.Draw(glow, drawPos, null, tint * (0.35f * opacity), 0f, glow.Size() / 2f, (1.4f + blinkPop * 0.5f) * scale / VDDirector.RedDevilPlaneScale * 4f, SpriteEffects.None, 0f);
            if (charge > 0f) {
                spriteBatch.Draw(glow, drawPos, null, new Color(255, 160, 120) * (0.8f * charge * opacity), 0f, glow.Size() / 2f, (0.6f + 0.8f * charge) * scale / VDDirector.RedDevilPlaneScale * 4f, SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();

            VDHologramDraw.Draw(tex, drawPos, src, tint, opacity, 0f, origin, scale, fx);
        }
    }

    /// <summary>
    /// 全息邪恶三叉戟:带深度生成时从背景里的红魔处沿三维直线收敛到平面落点(纵深贯穿,只在穿过平面那几帧有判定,再掠过镜头);
    /// 不带深度时直飞持续加速(旧行为)。命中 450
    /// </summary>
    public class VDHoloTrident : VDDepthProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DefaultTimeLeft => 150;
        public override float MarkerRadius => 26f;
        public override Color MarkerColor => VDHologramDraw.HellRed;
        protected override float WhooshStrength => Projectile.identity % 3 == 0 ? 0.25f : 0f;

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        protected override void DepthAI() {
            Projectile.localAI[1]++;
            if (!HasDepth) {
                float speed = Math.Min(Projectile.velocity.Length() * 1.03f + 0.05f, 30f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            }
            else if (OutOfSight) {
                Projectile.Kill();
                return;
            }
            //朝向按表观运动方向:三维直线在屏幕上是向消失点收敛 / 发散的
            Vector2 dir = ApparentDirection();
            Projectile.rotation = dir.ToRotation() + MathHelper.PiOver4;
            if (!HasDepth || Math.Abs(Z) < 0.5f) {
                Lighting.AddLight(Projectile.Center, VDHologramDraw.HellRed.ToVector3() * 0.4f);
            }
        }

        private Vector2 ApparentDirection() {
            if (!HasDepth || trailCount < 2) {
                return Projectile.velocity.SafeNormalize(Vector2.UnitX);
            }
            Vector2 prev = VDDepth.Project(trailPos[1], trailZ[1]);
            Vector2 d = ProjectedCenter - prev;
            return d.LengthSquared() < 0.01f ? Projectile.velocity.SafeNormalize(Vector2.UnitX) : d.SafeNormalize(Vector2.UnitX);
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            Main.instance.LoadProjectile(ProjectileID.UnholyTridentHostile);
            Texture2D tex = TextureAssets.Projectile[ProjectileID.UnholyTridentHostile].Value;
            Vector2 origin = tex.Size() / 2f;
            float opacity = MathHelper.Clamp(Projectile.localAI[1] / 8f, 0f, 1f) * 0.9f * VDDepth.Alpha(Z);
            if (opacity <= 0.01f) {
                return;
            }
            float scale = 1.1f * VDDepth.Scale(Z);
            Color tint = HoloTintStatic(Z);
            VDHologramDraw.Begin();
            for (int i = trailCount - 1; i >= 1; i--) {
                float a = (1f - i / (float)TrailLength) * 0.35f;
                Vector2 pos = VDDepth.Project(trailPos[i], trailZ[i]) - Main.screenPosition;
                VDHologramDraw.DrawPart(tex, pos, null, tint, opacity * a, Projectile.rotation, origin, 1.1f * VDDepth.Scale(trailZ[i]), SpriteEffects.None);
            }
            VDHologramDraw.DrawPart(tex, ProjectedCenter - Main.screenPosition, null, tint, opacity, Projectile.rotation, origin, scale, SpriteEffects.None);
            VDHologramDraw.End();
        }

        private static Color HoloTintStatic(float z) => Color.Lerp(VDHologramDraw.HellRed, VDVfx.FarFog, VDDepth.FogAmount(z) * 0.35f);
    }

    /// <summary>
    /// 全息丛林陆龟:8×8 格旋转体。偶数次冲锋是平面横冲:传送到玩家移动方向一侧 80 格外横向冲 200 格(90 帧),起冲时扇形放 4 发全息毒刺,
    /// 纵向随玩家闪避轻微修正;奇数次是穿层冲锋:传送到 Z 1.5 的背景里,18 帧待机后沿三维直线冲向锁定点,第 30 帧穿过平面(那一帧放毒刺扇),
    /// 再遁到镜头后消失。之后回同一侧重复,共 JungleDashes 次。
    /// ai[0] 本体,ai[1] 目标玩家,ai[2] 预设侧向(0 = 首冲时按玩家移动方向自选)。命中 432;穿层冲锋只在穿过平面那几帧有判定
    /// </summary>
    public class VDHoloTortoise : VDHoloProjectile
    {
        public const int AppearFrames = VDDirector.TortoiseAppearFrames;
        public const int DashFrames = VDDirector.TortoiseDashFrames;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override VDStateIndex OwnerMode => VDStateIndex.GreenJungle;
        public override Color HoloColor => VDHologramDraw.JungleGreen;
        public override int DefaultTimeLeft => 1200;
        public override float MarkerRadius => 70f;
        public override Color MarkerColor => VDHologramDraw.JungleGreen;
        /// <summary>穿层冲锋用基类的逼近标记(锁定点上的收缩环)</summary>
        public override bool WantsMarker => HasDepth && Approaching && !VDDepth.InHitBand(Z, ZVel);
        protected override float WhooshStrength => 0.6f;

        /// <summary>服务端状态机:0 出现待机,1 冲刺,2 收尾。客户端不跑状态机,只按同步来的速度判定是否在冲刺</summary>
        private int phase;
        private int phaseTimer;
        private int dashesDone;
        private float spin;
        private Vector2 lastPos;
        private bool wasDashing;

        public override void SetExtraDefaults() {
            Projectile.width = 128;
            Projectile.height = 128;
        }

        /// <summary>速度即同步来的"是否在冲刺":待机期速度为零,门在原地</summary>
        public bool Dashing => Projectile.velocity.LengthSquared() > 1f;

        public override bool ShouldUpdatePosition() => Dashing;

        private int TotalDashes => VDDirector.JungleDashes(Owner != null ? Owner.Phase : 1);

        protected override void DepthAI() {
            Projectile.localAI[1]++;
            FadeOutIfOrphaned();
            Player target = TargetPlayer(1);
            if (target == null) {
                Projectile.Kill();
                return;
            }
            if (Projectile.localAI[1] <= 1f) {
                lastPos = Projectile.Center;
            }

            if (IsServer) {
                ServerStateMachine(target);
            }

            bool dashing = Dashing;
            spin += dashing ? 0.35f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X) : 0.08f;
            Projectile.rotation = spin;
            if (Math.Abs(Z) < 0.5f) {
                Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.6f);
            }

            //客户端演出靠"位置跳变 / 冲刺起落"这两个可观测量,不依赖服务端私有状态;粒子放在投影位置
            if (!Main.dedServ) {
                Vector2 shown = ProjectedCenter;
                if (Vector2.DistanceSquared(Projectile.Center, lastPos) > 300f * 300f) {
                    VDHoloRedDevil.SpawnHoloBurst(VDDepth.Project(lastPos, Z), HoloColor);
                    VDHoloRedDevil.SpawnHoloBurst(shown, HoloColor);
                    CEUtils.PlaySound("vbapear", 0.8f * (HasDepth ? VDDepth.DopplerPitch(Math.Max(Z, 0f)) : 1f), shown, 4, 0.9f);
                }
                if (dashing && !wasDashing) {
                    CEUtils.PlaySound("CruiserDash", 0.8f, shown, 3, 0.9f);
                }
                if (dashing && Math.Abs(Z) < 0.6f && Main.rand.NextBool(2)) {
                    float sc = VDDepth.Scale(Z);
                    Vector2 v = -Projectile.velocity * 0.1f * sc + CEUtils.randomPointInCircle(2f);
                    VDVfx.Spark(shown + CEUtils.randomPointInCircle(50f * sc), v, HoloColor, Main.rand.NextFloat(0.5f, 1f) * sc, 0.9f, 18);
                }
            }

            //平面横冲中的纵向修正全端同算:尚未越过玩家时向玩家 y 轻微偏,越过后回直;穿层冲锋走定死的三维直线,不修正
            if (dashing && !HasDepth) {
                bool ahead = Math.Sign(target.Center.X - Projectile.Center.X) == Math.Sign(Projectile.velocity.X);
                if (ahead) {
                    float dy = target.Center.Y - Projectile.Center.Y;
                    Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + Math.Sign(dy) * 0.4f, -6f, 6f);
                }
                else {
                    Projectile.velocity.Y *= 0.9f;
                }
            }
            lastPos = Projectile.Center;
            wasDashing = dashing;
        }

        private void ServerStateMachine(Player target) {
            bool depthDash = dashesDone % 2 == 1;
            if (phase == 0) {
                if (phaseTimer == 0) {
                    //首冲按玩家移动方向选侧,之后每次都回同一侧
                    if (Projectile.ai[2] == 0) {
                        Projectile.ai[2] = Math.Abs(target.velocity.X) > 0.5f ? Math.Sign(target.velocity.X) : (Main.rand.NextBool() ? 1 : -1);
                    }
                    if (depthDash) {
                        //穿层:传送到背景里(表观在玩家侧上方),Z 钉在深处
                        Vector2 apparent = new Vector2(Projectile.ai[2] * VDDirector.TortoiseZApparent.X, VDDirector.TortoiseZApparent.Y);
                        Projectile.Center = VDDepth.WorldFromApparent(target.Center, apparent, VDDirector.TortoiseZDepth);
                        Z = VDDirector.TortoiseZDepth;
                    }
                    else {
                        Projectile.Center = target.Center + new Vector2(Projectile.ai[2] * VDDirector.TortoiseStartOffset, 0f);
                        Z = 0f;
                    }
                    ZVel = 0f;
                    ZAccel = 0f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }
                phaseTimer++;
                if (phaseTimer >= AppearFrames) {
                    if (depthDash) {
                        //穿层起冲:三维直线,第 TortoiseZCrossFrame 帧在锁定点穿过平面
                        Vector2 lock2 = target.Center + target.velocity * VDDirector.TortoiseZLead;
                        Projectile.velocity = (lock2 - Projectile.Center) / VDDirector.TortoiseZCrossFrame;
                        ZVel = -Z / VDDirector.TortoiseZCrossFrame;
                    }
                    else {
                        //平面起冲:朝玩家方向横冲 200 格,同时扇形 4 发毒刺
                        float dir = -Projectile.ai[2];
                        Projectile.velocity = new Vector2(dir * VDDirector.TortoiseDashDistance / DashFrames, 0f);
                        FireStingers(target);
                    }
                    Projectile.netUpdate = true;
                    phase = 1;
                    phaseTimer = 0;
                }
                return;
            }
            if (phase == 1) {
                phaseTimer++;
                if (phaseTimer % 30 == 0) {
                    Projectile.netUpdate = true;
                }
                if (depthDash && phaseTimer == VDDirector.TortoiseZCrossFrame) {
                    //穿过平面那一帧放毒刺扇
                    FireStingers(target);
                }
                int frames = depthDash ? VDDirector.TortoiseZDashFrames : DashFrames;
                if (phaseTimer >= frames) {
                    dashesDone++;
                    Projectile.velocity = Vector2.Zero;
                    ZVel = 0f;
                    Projectile.netUpdate = true;
                    if (dashesDone >= TotalDashes) {
                        Projectile.timeLeft = Math.Min(Projectile.timeLeft, 20);
                        phase = 2;
                    }
                    else {
                        phase = 0;
                        phaseTimer = 0;
                    }
                }
            }
        }

        /// <summary>扇形 4 发全息毒刺,朝玩家</summary>
        private void FireStingers(Player target) {
            Vector2 aim = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 4; i++) {
                float ang = MathHelper.ToRadians(-30f + 20f * i);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, aim.RotatedBy(ang) * 12f, ModContent.ProjectileType<VDHoloStinger>(), Owner != null ? Owner.ProjDamage(VDDirector.DmgStinger) : Projectile.damage / 2, 0f, Main.myPlayer);
            }
        }

        protected override bool? CollidingOnPlane(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Dashing) {
                return false;
            }
            return projHitbox.Intersects(targetHitbox);
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            Main.instance.LoadNPC(NPCID.GiantTortoise);
            Texture2D tex = TextureAssets.Npc[NPCID.GiantTortoise].Value;
            int frames = Math.Max(1, Main.npcFrameCount[NPCID.GiantTortoise]);
            int frameH = tex.Height / frames;
            Rectangle src = new Rectangle(0, 0, tex.Width, frameH);
            Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
            float scale = 128f / Math.Max(tex.Width, frameH) * 1.15f * VDDepth.Scale(Z);
            float opacity = HoloOpacity(0.9f) * VDDepth.Alpha(Z);
            if (opacity <= 0.01f) {
                return;
            }
            Vector2 drawPos = ProjectedCenter - Main.screenPosition;
            Color tint = HoloTint(HoloColor, Z);

            spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            spriteBatch.Draw(glow, drawPos, null, tint * (0.4f * opacity), 0f, glow.Size() / 2f, 0.9f * VDDepth.Scale(Z), SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            VDHologramDraw.Begin();
            if (Dashing) {
                for (int i = 1; i < trailCount && i <= 4; i++) {
                    Vector2 pos = VDDepth.Project(trailPos[i], trailZ[i]) - Main.screenPosition;
                    float gs = 128f / Math.Max(tex.Width, frameH) * 1.15f * VDDepth.Scale(trailZ[i]);
                    VDHologramDraw.DrawPart(tex, pos, src, tint, opacity * (0.3f - i * 0.06f), Projectile.rotation - i * 0.2f, origin, gs, SpriteEffects.None);
                }
            }
            VDHologramDraw.DrawPart(tex, drawPos, src, tint, opacity, Projectile.rotation, origin, scale, SpriteEffects.None);
            VDHologramDraw.End();
        }
    }

    /// <summary>全息青苔黄蜂毒刺:直飞,命中 276</summary>
    public class VDHoloStinger : VDHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DefaultTimeLeft => 120;

        public override void SetExtraDefaults() {
            Projectile.width = 12;
            Projectile.height = 12;
        }

        public override void AI() {
            Projectile.localAI[1]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.instance.LoadProjectile(ProjectileID.Stinger);
            Texture2D tex = TextureAssets.Projectile[ProjectileID.Stinger].Value;
            float opacity = MathHelper.Clamp(Projectile.localAI[1] / 6f, 0f, 1f) * 0.9f;
            VDHologramDraw.Draw(tex, Projectile.Center - Main.screenPosition, null, VDHologramDraw.JungleGreen, opacity, Projectile.rotation, tex.Size() / 2f, 1.6f, SpriteEffects.None);
            return false;
        }
    }

    /// <summary>
    /// 全息小白龙(倾斜轨道):单弹幕 75 节、两倍粗,绕本体做倾 55° 的三维椭圆轨道:平面上是短轴 = 半径 × cos55° 的椭圆,
    /// 深度 Z = sinθ × 振幅(下半圈退到 1.4 的远处、上半圈压到 -0.3 的镜头前),逐节各自算深度、各自投影,
    /// 只有 |Z| ≤ WyvernHitBand 的节有判定(θ = 0 / π 两处穿越点);玩家出圈时龙头脱轨直冲 40 帧后归位(追击期深度拉回平面)。
    /// 本弹幕自身的 Z 恒 0(整条龙横跨三层,统一画在弹幕层),深度只在节级。ai[0] 本体,ai[1] 起始角(与状态同一颗骰子:形状弹在穿越帧释放)。命中 432
    /// </summary>
    public class VDHoloWyvern : VDHoloProjectile
    {
        public const int SegmentCount = 75;
        public const float SegmentLength = 44f;
        public const float SegmentScale = 2f;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override VDStateIndex OwnerMode => VDStateIndex.BlueSky;
        public override Color HoloColor => VDHologramDraw.SkyBlue;
        public override int DefaultTimeLeft => 900;
        protected override float WhooshStrength => 0f;

        private readonly Vector2[] segments = new Vector2[SegmentCount];
        private readonly float[] segDepth = new float[SegmentCount];
        private readonly int[] drawOrder = new int[SegmentCount];
        private bool initialized;
        /// <summary>0 绕圈,1 出圈直冲,2 归位</summary>
        private int mode;
        private int modeTimer;
        private float orbitAngle;
        /// <summary>追击期深度压回平面的混合量 0..1</summary>
        private float chaseBlend;

        public float OrbitRadius => VDDirector.WyvernOrbitRadius;
        private static float TiltCos => MathF.Cos(MathHelper.ToRadians(VDDirector.WyvernTiltDeg));

        public override void SetExtraDefaults() {
            Projectile.width = 60;
            Projectile.height = 60;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>轨道上角 θ 处的平面位置</summary>
        private Vector2 OrbitPoint(Vector2 center, float theta) => center + new Vector2(MathF.Cos(theta), MathF.Sin(theta) * TiltCos) * OrbitRadius;

        /// <summary>轨道上角 θ 处的深度:下半圈远、上半圈近(近端振幅小,放大后仍留在屏内)</summary>
        private static float OrbitDepth(float theta) {
            float s = MathF.Sin(theta);
            return s >= 0f ? s * VDDirector.WyvernFarAmp : s * VDDirector.WyvernNearAmp;
        }

        /// <summary>平面上任一点按轨道椭圆反推的深度(节级深度用它,不必逐节记历史)</summary>
        private float DepthAt(Vector2 center, Vector2 pos) {
            Vector2 d = pos - center;
            float theta = MathF.Atan2(d.Y / Math.Max(TiltCos, 0.05f), d.X);
            return OrbitDepth(theta) * (1f - chaseBlend);
        }

        protected override void DepthAI() {
            Projectile.localAI[1]++;
            //整条龙的弹幕级 Z 恒 0:分层与判定都在节级
            Z = 0f;
            ZVel = 0f;
            ZAccel = 0f;
            FadeOutIfOrphaned();
            VoidDestroyerNPC boss = Owner;
            if (boss == null) {
                Projectile.Kill();
                return;
            }
            Vector2 center = boss.NPC.Center;
            float angular = VDDirector.WyvernAngularSpeed(boss.Phase);
            if (!initialized) {
                initialized = true;
                orbitAngle = Projectile.ai[1];
                Projectile.Center = OrbitPoint(center, orbitAngle);
                for (int i = 0; i < SegmentCount; i++) {
                    segments[i] = Projectile.Center;
                    segDepth[i] = OrbitDepth(orbitAngle);
                }
            }

            //圈上目标点始终按角度推进,离轨期间也在走,归位时接回
            orbitAngle += angular;
            Vector2 orbitPos = OrbitPoint(center, orbitAngle);
            Player target = boss.NPC.HasValidTarget ? boss.Target : null;

            switch (mode) {
                case 0:
                    Projectile.Center = orbitPos;
                    chaseBlend = Math.Max(0f, chaseBlend - 0.08f);
                    if (target != null && Vector2.Distance(target.Center, center) > OrbitRadius + 60f && Projectile.timeLeft > 120) {
                        mode = 1;
                        modeTimer = 0;
                        if (!Main.dedServ) {
                            CEUtils.PlaySound("CruiserDash", 0.7f, ProjectedHead(), 3, 0.9f);
                        }
                        if (IsServer) {
                            Projectile.netUpdate = true;
                        }
                    }
                    break;
                case 1:
                    modeTimer++;
                    chaseBlend = Math.Min(1f, chaseBlend + 0.1f);
                    if (target != null) {
                        Vector2 toTarget = target.Center - Projectile.Center;
                        Projectile.Center += toTarget.SafeNormalize(Vector2.Zero) * Math.Min(42f, toTarget.Length());
                        if (toTarget.Length() < 50f || modeTimer >= 40) {
                            mode = 2;
                            modeTimer = 0;
                        }
                    }
                    else {
                        mode = 2;
                        modeTimer = 0;
                    }
                    break;
                default:
                    modeTimer++;
                    chaseBlend = Math.Max(0f, chaseBlend - 0.05f);
                    Projectile.Center = Vector2.Lerp(Projectile.Center, orbitPos, 0.12f);
                    if (modeTimer >= 30 || Vector2.Distance(Projectile.Center, orbitPos) < 30f) {
                        mode = 0;
                    }
                    break;
            }

            //蠕虫跟随:每节钉在前一节后方定长处;深度按各节在轨道椭圆上的角反推
            segments[0] = Projectile.Center;
            for (int i = 1; i < SegmentCount; i++) {
                Vector2 diff = segments[i] - segments[i - 1];
                if (diff.LengthSquared() < 0.01f) {
                    diff = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    if (diff == Vector2.Zero) {
                        diff = Vector2.UnitX;
                    }
                }
                segments[i] = segments[i - 1] + diff.SafeNormalize(Vector2.UnitX) * SegmentLength;
            }
            for (int i = 0; i < SegmentCount; i++) {
                segDepth[i] = DepthAt(center, segments[i]);
            }
            Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.7f * VDDepth.Scale(Math.Max(segDepth[0], 0f)));
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                int idx = Main.rand.Next(SegmentCount);
                if (Math.Abs(segDepth[idx]) < 0.6f) {
                    Vector2 v = CEUtils.randomPointInCircle(1.5f);
                    VDVfx.Spark(VDDepth.Project(segments[idx], segDepth[idx]) + CEUtils.randomPointInCircle(20f), v, HoloColor, Main.rand.NextFloat(0.4f, 0.8f), 0.8f, 16);
                }
            }
        }

        private Vector2 ProjectedHead() => VDDepth.Project(segments[0], segDepth[0]);

        protected override bool? CollidingOnPlane(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.localAI[1] < 12f) {
                return false;
            }
            int half = (int)(26f * SegmentScale);
            for (int i = 0; i < SegmentCount; i++) {
                if (Math.Abs(segDepth[i]) > VDDirector.WyvernHitBand) {
                    continue;
                }
                Rectangle seg = new Rectangle((int)segments[i].X - half, (int)segments[i].Y - half, half * 2, half * 2);
                if (seg.Intersects(targetHitbox)) {
                    return true;
                }
            }
            return false;
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            Main.instance.LoadNPC(NPCID.WyvernHead);
            Main.instance.LoadNPC(NPCID.WyvernLegs);
            Main.instance.LoadNPC(NPCID.WyvernBody);
            Main.instance.LoadNPC(NPCID.WyvernBody2);
            Main.instance.LoadNPC(NPCID.WyvernBody3);
            Main.instance.LoadNPC(NPCID.WyvernTail);
            float opacity = HoloOpacity(0.8f);

            //远的节先画,近的节压在上面
            for (int i = 0; i < SegmentCount; i++) {
                drawOrder[i] = i;
            }
            Array.Sort(drawOrder, (a, b) => segDepth[b].CompareTo(segDepth[a]));

            VDHologramDraw.Begin();
            for (int k = 0; k < SegmentCount; k++) {
                int i = drawOrder[k];
                float z = segDepth[i];
                float a = opacity * VDDepth.Alpha(z);
                if (a <= 0.01f) {
                    continue;
                }
                int type;
                if (i == 0) {
                    type = NPCID.WyvernHead;
                }
                else if (i == SegmentCount - 1) {
                    type = NPCID.WyvernTail;
                }
                else if (i % 12 == 3) {
                    type = NPCID.WyvernLegs;
                }
                else {
                    type = (i % 3) switch { 0 => NPCID.WyvernBody, 1 => NPCID.WyvernBody2, _ => NPCID.WyvernBody3 };
                }
                Texture2D tex = TextureAssets.Npc[type].Value;
                int frames = Math.Max(1, Main.npcFrameCount[type]);
                int frameH = tex.Height / frames;
                Rectangle src = new Rectangle(0, 0, tex.Width, frameH);
                Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
                //朝向按投影后的前后节连线算,透视里身体是弯的
                Vector2 here = VDDepth.Project(segments[i], z);
                Vector2 ahead = i == 0 ? here + (here - VDDepth.Project(segments[1], segDepth[1])) : VDDepth.Project(segments[i - 1], segDepth[i - 1]);
                float rot = (ahead - here).ToRotation() + MathHelper.PiOver2;
                VDHologramDraw.DrawPart(tex, here - Main.screenPosition, src, HoloTint(HoloColor, z), a, rot, origin, SegmentScale * VDDepth.Scale(z), SpriteEffects.None);
            }
            VDHologramDraw.End();
        }
    }
}
