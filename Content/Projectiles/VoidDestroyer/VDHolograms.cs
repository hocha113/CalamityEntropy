using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>全息弹幕公共部分:通过 ai[0] 找到本体,本体不在对应状态时自行消散;节拍读本体 Context 的表现通道,不读状态私有量</summary>
    public abstract class VDHoloProjectile : VDHostileProjectile
    {
        public abstract VDStateIndex OwnerMode { get; }
        public abstract Color HoloColor { get; }

        /// <summary>本体引用,无效时为 null</summary>
        protected VoidDestroyerNPC Owner
        {
            get
            {
                int idx = (int)Projectile.ai[0];
                if (idx < 0 || idx >= Main.maxNPCs)
                {
                    return null;
                }
                NPC npc = Main.npc[idx];
                if (!npc.active || npc.ModNPC is not VoidDestroyerNPC boss)
                {
                    return null;
                }
                return boss;
            }
        }

        /// <summary>本体仍在本状态里(且没死)</summary>
        protected bool OwnerActive
        {
            get
            {
                VoidDestroyerNPC boss = Owner;
                return boss != null && !boss.Dying && boss.CurrentStateIndex == OwnerMode;
            }
        }

        /// <summary>整体透明度:出现 12 帧渐显,寿命末尾 20 帧渐隐</summary>
        protected float HoloOpacity(float peak = 0.85f)
        {
            float fadeIn = MathHelper.Clamp(Projectile.localAI[1] / 12f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            return peak * Math.Min(fadeIn, fadeOut);
        }

        /// <summary>本体离开模式时收尾:把剩余寿命压到渐隐长度</summary>
        protected void FadeOutIfOrphaned()
        {
            if (!OwnerActive && Projectile.timeLeft > 20)
            {
                Projectile.timeLeft = 20;
            }
        }
    }

    /// <summary>
    /// 全息红恶魔(纯演出,无伤害):停在本体 AnchorPos(服务端选定的玩家左/右 30 格),
    /// 按本体 Context.HoloCharge 蓄力发亮;三叉戟由本体状态生成
    /// </summary>
    public class VDHoloRedDevil : VDHoloProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override VDStateIndex OwnerMode => VDStateIndex.RedHell;
        public override Color HoloColor => VDHologramDraw.HellRed;
        public override int DefaultTimeLeft => 900;

        private Vector2 lastAnchor;
        private float blinkPop;

        public override void SetExtraDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 60;
            Projectile.hostile = false;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[1]++;
            FadeOutIfOrphaned();
            VoidDestroyerNPC boss = Owner;
            if (boss == null)
            {
                return;
            }
            //本体进入收尾拍时提前渐隐
            if (boss.CurrentStateIndex == OwnerMode && boss.Context.HoloWrapUp && Projectile.timeLeft > 20)
            {
                Projectile.timeLeft = 20;
            }
            Vector2 anchor = boss.AnchorPos;
            if (Projectile.localAI[1] <= 1f)
            {
                lastAnchor = anchor;
                Projectile.Center = anchor;
            }
            if (Vector2.DistanceSquared(anchor, lastAnchor) > 16f)
            {
                //锚点换了 = 红恶魔传送:旧位置爆一圈全息碎片,新位置弹一下
                if (!Main.dedServ)
                {
                    SpawnHoloBurst(Projectile.Center, HoloColor);
                    SpawnHoloBurst(anchor, HoloColor);
                }
                lastAnchor = anchor;
                blinkPop = 1f;
            }
            Projectile.Center = anchor + new Vector2(0, (float)Math.Sin(Projectile.localAI[1] * 0.06f) * 8f);
            blinkPop *= 0.85f;
            Projectile.direction = boss.Target.Center.X > Projectile.Center.X ? 1 : -1;
            Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.6f);

            //蓄力粒子:跟本体声明的蓄力读数走
            if (!Main.dedServ && !boss.Context.HoloWrapUp && boss.Context.HoloCharge > 0.02f && Main.rand.NextBool(2))
            {
                Vector2 from = Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(50f, 110f);
                Vector2 v = (Projectile.Center - from) * 0.08f;
                var s = PRTLoader.NewParticle<PRT_GlowSpark>(from, v, HoloColor, Main.rand.NextFloat(0.4f, 0.8f))
                    .Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 13);
                s.grav = false;
            }
        }

        public static void SpawnHoloBurst(Vector2 pos, Color color)
        {
            for (int i = 0; i < 14; i++)
            {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 9f);
                PRTLoader.NewParticle<PRT_GlowSpark>(pos, v, color, Main.rand.NextFloat(0.5f, 1f))
                    .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 22);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadNPC(NPCID.RedDevil);
            Texture2D tex = TextureAssets.Npc[NPCID.RedDevil].Value;
            int frames = Math.Max(1, Main.npcFrameCount[NPCID.RedDevil]);
            int frameH = tex.Height / frames;
            int frame = (int)(Main.GlobalTimeWrappedHourly * 7f) % frames;
            Rectangle src = new Rectangle(0, frame * frameH, tex.Width, frameH);
            Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
            SpriteEffects fx = Projectile.direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float opacity = HoloOpacity();
            float scale = 1.25f + blinkPop * 0.3f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            VoidDestroyerNPC boss = Owner;
            float charge = 0f;
            if (boss != null && boss.CurrentStateIndex == OwnerMode && !boss.Context.HoloWrapUp)
            {
                charge = MathHelper.Clamp(boss.Context.HoloCharge, 0f, 1f);
            }

            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, HoloColor * (0.35f * opacity), 0f, glow.Size() / 2f, 0.6f + blinkPop * 0.3f, SpriteEffects.None, 0f);
            if (charge > 0f)
            {
                Main.spriteBatch.Draw(glow, drawPos, null, new Color(255, 160, 120) * (0.8f * charge * opacity), 0f, glow.Size() / 2f, 0.25f + 0.35f * charge, SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();

            VDHologramDraw.Draw(tex, drawPos, src, HoloColor, opacity, 0f, origin, scale, fx);
            return false;
        }
    }

    /// <summary>全息邪恶三叉戟:直飞持续加速,命中 450</summary>
    public class VDHoloTrident : VDHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DefaultTimeLeft => 150;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override void AI()
        {
            Projectile.localAI[1]++;
            float speed = Math.Min(Projectile.velocity.Length() * 1.03f + 0.05f, 30f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, VDHologramDraw.HellRed.ToVector3() * 0.4f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadProjectile(ProjectileID.UnholyTridentHostile);
            Texture2D tex = TextureAssets.Projectile[ProjectileID.UnholyTridentHostile].Value;
            Vector2 origin = tex.Size() / 2f;
            float opacity = MathHelper.Clamp(Projectile.localAI[1] / 8f, 0f, 1f) * 0.9f;
            VDHologramDraw.Begin();
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                float a = (1f - i / (float)Projectile.oldPos.Length) * 0.35f;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                VDHologramDraw.DrawPart(tex, pos, null, VDHologramDraw.HellRed, opacity * a, Projectile.oldRot[i], origin, 1.1f, SpriteEffects.None);
            }
            VDHologramDraw.DrawPart(tex, Projectile.Center - Main.screenPosition, null, VDHologramDraw.HellRed, opacity, Projectile.rotation, origin, 1.1f, SpriteEffects.None);
            VDHologramDraw.End();
            return false;
        }
    }

    /// <summary>
    /// 全息丛林陆龟:8×8 格旋转体,传送到玩家移动方向一侧 80 格外横向冲刺 200 格(90 帧),
    /// 起冲时扇形放 4 发全息毒刺,纵向随玩家闪避轻微修正;之后回同一侧重复,共 6 次(三阶段 7)。
    /// ai[0] 本体,ai[1] 目标玩家,ai[2] 预设侧向(0 = 首冲时按玩家移动方向自选)。命中 432
    /// </summary>
    public class VDHoloTortoise : VDHoloProjectile
    {
        public const float DashDistance = 200f * 16f;
        public const int DashFrames = 90;
        public const int AppearFrames = 12;
        public const float StartOffset = 80f * 16f;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override VDStateIndex OwnerMode => VDStateIndex.GreenJungle;
        public override Color HoloColor => VDHologramDraw.JungleGreen;
        public override int DefaultTimeLeft => 1200;

        /// <summary>服务端状态机:0 出现待机,1 冲刺,2 收尾。客户端不跑状态机,只按同步来的速度判定是否在冲刺</summary>
        private int phase;
        private int phaseTimer;
        private int dashesDone;
        private float spin;
        private Vector2 lastPos;
        private bool wasDashing;

        public override void SetExtraDefaults()
        {
            Projectile.width = 128;
            Projectile.height = 128;
        }

        /// <summary>速度即同步来的"是否在冲刺":待机期速度为零,门在原地</summary>
        public bool Dashing => Projectile.velocity.LengthSquared() > 1f;

        public override bool ShouldUpdatePosition() => Dashing;

        private int TotalDashes => Owner != null && Owner.Phase >= 3 ? 7 : 6;

        public override void AI()
        {
            Projectile.localAI[1]++;
            FadeOutIfOrphaned();
            Player target = TargetPlayer(1);
            if (target == null)
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.localAI[1] <= 1f)
            {
                lastPos = Projectile.Center;
            }

            if (IsServer)
            {
                ServerStateMachine(target);
            }

            bool dashing = Dashing;
            spin += dashing ? 0.35f * Math.Sign(Projectile.velocity.X) : 0.08f;
            Projectile.rotation = spin;
            Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.6f);

            //客户端演出靠"位置跳变 / 冲刺起落"这两个可观测量,不依赖服务端私有状态
            if (!Main.dedServ)
            {
                if (Vector2.DistanceSquared(Projectile.Center, lastPos) > 300f * 300f)
                {
                    VDHoloRedDevil.SpawnHoloBurst(lastPos, HoloColor);
                    VDHoloRedDevil.SpawnHoloBurst(Projectile.Center, HoloColor);
                    CEUtils.PlaySound("vbapear", 0.8f, Projectile.Center, 4, 0.9f);
                }
                if (dashing && !wasDashing)
                {
                    CEUtils.PlaySound("CruiserDash", 0.8f, Projectile.Center, 3, 0.9f);
                }
                if (dashing && Main.rand.NextBool(2))
                {
                    Vector2 v = -Projectile.velocity * 0.1f + CEUtils.randomPointInCircle(2f);
                    var s = PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center + CEUtils.randomPointInCircle(50f), v, HoloColor, Main.rand.NextFloat(0.5f, 1f))
                        .Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 18);
                    s.grav = false;
                }
            }

            //冲刺中的纵向修正全端同算:尚未越过玩家时向玩家 y 轻微偏,越过后回直
            if (dashing)
            {
                bool ahead = Math.Sign(target.Center.X - Projectile.Center.X) == Math.Sign(Projectile.velocity.X);
                if (ahead)
                {
                    float dy = target.Center.Y - Projectile.Center.Y;
                    Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + Math.Sign(dy) * 0.4f, -6f, 6f);
                }
                else
                {
                    Projectile.velocity.Y *= 0.9f;
                }
            }
            lastPos = Projectile.Center;
            wasDashing = dashing;
        }

        private void ServerStateMachine(Player target)
        {
            if (phase == 0)
            {
                if (phaseTimer == 0)
                {
                    //首冲按玩家移动方向选侧,之后每次都回同一侧
                    if (Projectile.ai[2] == 0)
                    {
                        Projectile.ai[2] = Math.Abs(target.velocity.X) > 0.5f ? Math.Sign(target.velocity.X) : (Main.rand.NextBool() ? 1 : -1);
                    }
                    Projectile.Center = target.Center + new Vector2(Projectile.ai[2] * StartOffset, 0f);
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }
                phaseTimer++;
                if (phaseTimer >= AppearFrames)
                {
                    //起冲:朝玩家方向横冲 200 格,同时扇形 4 发毒刺
                    float dir = -Projectile.ai[2];
                    Projectile.velocity = new Vector2(dir * DashDistance / DashFrames, 0f);
                    Vector2 aim = (target.Center - Projectile.Center).SafeNormalize(new Vector2(dir, 0));
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = MathHelper.ToRadians(-30f + 20f * i);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, aim.RotatedBy(ang) * 12f, ModContent.ProjectileType<VDHoloStinger>(), Owner != null ? Owner.ProjDamage(276) : Projectile.damage / 2, 0f, Main.myPlayer);
                    }
                    Projectile.netUpdate = true;
                    phase = 1;
                    phaseTimer = 0;
                }
                return;
            }
            if (phase == 1)
            {
                phaseTimer++;
                if (phaseTimer % 30 == 0)
                {
                    Projectile.netUpdate = true;
                }
                if (phaseTimer >= DashFrames)
                {
                    dashesDone++;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                    if (dashesDone >= TotalDashes)
                    {
                        Projectile.timeLeft = Math.Min(Projectile.timeLeft, 20);
                        phase = 2;
                    }
                    else
                    {
                        phase = 0;
                        phaseTimer = 0;
                    }
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Dashing)
            {
                return false;
            }
            return projHitbox.Intersects(targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadNPC(NPCID.GiantTortoise);
            Texture2D tex = TextureAssets.Npc[NPCID.GiantTortoise].Value;
            int frames = Math.Max(1, Main.npcFrameCount[NPCID.GiantTortoise]);
            int frameH = tex.Height / frames;
            Rectangle src = new Rectangle(0, 0, tex.Width, frameH);
            Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
            float scale = 128f / Math.Max(tex.Width, frameH) * 1.15f;
            float opacity = HoloOpacity(0.9f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, HoloColor * (0.4f * opacity), 0f, glow.Size() / 2f, 0.9f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            VDHologramDraw.Begin();
            if (Dashing)
            {
                for (int i = 1; i <= 4; i++)
                {
                    Vector2 pos = drawPos - Projectile.velocity * i * 1.5f;
                    VDHologramDraw.DrawPart(tex, pos, src, HoloColor, opacity * (0.3f - i * 0.06f), Projectile.rotation - i * 0.2f, origin, scale, SpriteEffects.None);
                }
            }
            VDHologramDraw.DrawPart(tex, drawPos, src, HoloColor, opacity, Projectile.rotation, origin, scale, SpriteEffects.None);
            VDHologramDraw.End();
            return false;
        }
    }

    /// <summary>全息青苔黄蜂毒刺:直飞,命中 276</summary>
    public class VDHoloStinger : VDHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DefaultTimeLeft => 120;

        public override void SetExtraDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
        }

        public override void AI()
        {
            Projectile.localAI[1]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadProjectile(ProjectileID.Stinger);
            Texture2D tex = TextureAssets.Projectile[ProjectileID.Stinger].Value;
            float opacity = MathHelper.Clamp(Projectile.localAI[1] / 6f, 0f, 1f) * 0.9f;
            VDHologramDraw.Draw(tex, Projectile.Center - Main.screenPosition, null, VDHologramDraw.JungleGreen, opacity, Projectile.rotation, tex.Size() / 2f, 1.6f, SpriteEffects.None);
            return false;
        }
    }

    /// <summary>
    /// 全息小白龙:单弹幕 75 节、两倍粗,绕本体做半径 75 格(FTW 60 格)的快速圆周;
    /// 玩家出圈时龙头脱轨直冲 40 帧后归位。ai[0] 本体,ai[1] 起始角。逐节判定,命中 432
    /// </summary>
    public class VDHoloWyvern : VDHoloProjectile
    {
        public const int SegmentCount = 75;
        public const float SegmentLength = 44f;
        public const float SegmentScale = 2f;
        public const float AngularSpeed = MathHelper.TwoPi / 180f;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override VDStateIndex OwnerMode => VDStateIndex.BlueSky;
        public override Color HoloColor => VDHologramDraw.SkyBlue;
        public override int DefaultTimeLeft => 900;

        private readonly Vector2[] segments = new Vector2[SegmentCount];
        private bool initialized;
        /// <summary>0 绕圈,1 出圈直冲,2 归位</summary>
        private int mode;
        private int modeTimer;
        private float orbitAngle;

        public float OrbitRadius => (Main.getGoodWorld ? 60f : 75f) * 16f;

        public override void SetExtraDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[1]++;
            FadeOutIfOrphaned();
            VoidDestroyerNPC boss = Owner;
            if (boss == null)
            {
                Projectile.Kill();
                return;
            }
            Vector2 center = boss.NPC.Center;
            if (!initialized)
            {
                initialized = true;
                orbitAngle = Projectile.ai[1];
                Projectile.Center = center + orbitAngle.ToRotationVector2() * OrbitRadius;
                for (int i = 0; i < SegmentCount; i++)
                {
                    segments[i] = Projectile.Center;
                }
            }

            //圈上目标点始终按角度推进,离轨期间也在走,归位时接回
            orbitAngle += AngularSpeed;
            Vector2 orbitPos = center + orbitAngle.ToRotationVector2() * OrbitRadius;
            Player target = boss.NPC.HasValidTarget ? boss.Target : null;

            switch (mode)
            {
                case 0:
                    Projectile.Center = orbitPos;
                    if (target != null && Vector2.Distance(target.Center, center) > OrbitRadius + 60f && Projectile.timeLeft > 120)
                    {
                        mode = 1;
                        modeTimer = 0;
                        if (!Main.dedServ)
                        {
                            CEUtils.PlaySound("CruiserDash", 0.7f, Projectile.Center, 3, 0.9f);
                        }
                        if (IsServer)
                        {
                            Projectile.netUpdate = true;
                        }
                    }
                    break;
                case 1:
                    modeTimer++;
                    if (target != null)
                    {
                        Vector2 toTarget = target.Center - Projectile.Center;
                        Projectile.Center += toTarget.SafeNormalize(Vector2.Zero) * Math.Min(42f, toTarget.Length());
                        if (toTarget.Length() < 50f || modeTimer >= 40)
                        {
                            mode = 2;
                            modeTimer = 0;
                        }
                    }
                    else
                    {
                        mode = 2;
                        modeTimer = 0;
                    }
                    break;
                default:
                    modeTimer++;
                    Projectile.Center = Vector2.Lerp(Projectile.Center, orbitPos, 0.12f);
                    if (modeTimer >= 30 || Vector2.Distance(Projectile.Center, orbitPos) < 30f)
                    {
                        mode = 0;
                    }
                    break;
            }

            //蠕虫跟随:每节钉在前一节后方定长处
            segments[0] = Projectile.Center;
            for (int i = 1; i < SegmentCount; i++)
            {
                Vector2 diff = segments[i] - segments[i - 1];
                if (diff.LengthSquared() < 0.01f)
                {
                    diff = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    if (diff == Vector2.Zero)
                    {
                        diff = Vector2.UnitX;
                    }
                }
                segments[i] = segments[i - 1] + diff.SafeNormalize(Vector2.UnitX) * SegmentLength;
            }
            Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.7f);
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                int idx = Main.rand.Next(SegmentCount);
                Vector2 v = CEUtils.randomPointInCircle(1.5f);
                var s = PRTLoader.NewParticle<PRT_GlowSpark>(segments[idx] + CEUtils.randomPointInCircle(20f), v, HoloColor, Main.rand.NextFloat(0.4f, 0.8f))
                    .Configure(0.8f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 16);
                s.grav = false;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.localAI[1] < 12f)
            {
                return false;
            }
            int half = (int)(26f * SegmentScale);
            for (int i = 0; i < SegmentCount; i++)
            {
                Rectangle seg = new Rectangle((int)segments[i].X - half, (int)segments[i].Y - half, half * 2, half * 2);
                if (seg.Intersects(targetHitbox))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadNPC(NPCID.WyvernHead);
            Main.instance.LoadNPC(NPCID.WyvernLegs);
            Main.instance.LoadNPC(NPCID.WyvernBody);
            Main.instance.LoadNPC(NPCID.WyvernBody2);
            Main.instance.LoadNPC(NPCID.WyvernBody3);
            Main.instance.LoadNPC(NPCID.WyvernTail);
            float opacity = HoloOpacity(0.8f);

            VDHologramDraw.Begin();
            for (int i = SegmentCount - 1; i >= 0; i--)
            {
                int type;
                if (i == 0)
                {
                    type = NPCID.WyvernHead;
                }
                else if (i == SegmentCount - 1)
                {
                    type = NPCID.WyvernTail;
                }
                else if (i % 12 == 3)
                {
                    type = NPCID.WyvernLegs;
                }
                else
                {
                    type = (i % 3) switch { 0 => NPCID.WyvernBody, 1 => NPCID.WyvernBody2, _ => NPCID.WyvernBody3 };
                }
                Texture2D tex = TextureAssets.Npc[type].Value;
                int frames = Math.Max(1, Main.npcFrameCount[type]);
                int frameH = tex.Height / frames;
                Rectangle src = new Rectangle(0, 0, tex.Width, frameH);
                Vector2 origin = new Vector2(tex.Width / 2f, frameH / 2f);
                Vector2 toward = i == 0 ? (segments[0] - segments[1]) : (segments[i - 1] - segments[i]);
                float rot = toward.ToRotation() + MathHelper.PiOver2;
                VDHologramDraw.DrawPart(tex, segments[i] - Main.screenPosition, src, HoloColor, opacity, rot, origin, SegmentScale, SpriteEffects.None);
            }
            VDHologramDraw.End();
            return false;
        }
    }
}
