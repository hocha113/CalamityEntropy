using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.CelestialSlimeStaff
{
    /// <summary>
    /// 伽玛射线:从天体史莱姆的图腾射出的短促光束,无限穿透,每个敌人只吃一次。
    /// ai[0] 朝向(弧度),ai[1] 形态(1 超新星白、蓝边,2 奇点金边、黑芯)
    /// </summary>
    public class CelestialSlimeStaffRay : ModProjectile
    {
        #region 调参旋钮
        //射线长度(像素,150格,盖住120格的索敌范围)、寿命(帧)、前几帧有伤害
        public const float Length = 2400f;
        public const int Lifetime = 20;
        public const int DamageFrames = 14;
        //弹幕自身尺寸(像素,只用作根部定位)、伤害判定宽度(像素)、画面半宽(像素)
        public const int Size = 16;
        public const float HitWidth = 28f;
        public const float HalfWidth = 13f;
        //点亮与收束用的帧数、收束时最细剩下的宽度比例、细到这个半宽(像素)以下就不画
        public const int IgniteFrames = 4;
        public const int FadeFrames = 8;
        public const float FadeMinWidth = 0.3f;
        public const float MinVisibleHalfWidth = 0.5f;
        //火焰纹理每帧滚动量与纹理重复长度(像素)
        public const float ScrollSpeed = 0.05f;
        public const float StreakRepeat = 320f;
        //外晕:宽度倍率、强度、两端收尖占的比例;主体:宽度倍率、强度
        public const float OuterWidth = 2f;
        public const float OuterOpacity = 0.55f;
        public const float CapFraction = 0.05f;
        public const float BodyWidth = 1.2f;
        public const float BodyOpacity = 0.9f;
        //火焰纹理:起止点离两端的比例、宽度倍率、两端收尖的比例、最细半宽(像素)、强度
        public const float StreakInset = 0.1f;
        public const float StreakWidth = 1.1f;
        public const float StreakEdge = 0.02f;
        public const float StreakMinHalf = 2f;
        public const float StreakOpacity = 0.5f;
        //根部:光晕大小(半宽的倍数)、闪光强度、闪光每帧旋转(弧度)、闪光缩放
        public const float RootGlowSize = 7f;
        public const float RootFlashOpacity = 0.6f;
        public const float RootFlashSpin = 0.05f;
        public const float RootFlashScale = 0.4f;
        //芯线宽度比例:奇点的黑芯、超新星的白芯
        public const float CoreWidthDark = 0.6f;
        public const float CoreWidthBright = 0.35f;
        //照明强度;音效音高、音量与同时发声上限
        public const float LightStrength = 0.6f;
        public const float SoundPitch = 1.3f;
        public const float SoundVolume = 0.45f;
        public const int SoundMaxInstances = 2;
        #endregion

        //外晕色与主体色:奇点金、超新星蓝白
        private static Color OuterColor(int form) => form == 2 ? new Color(255, 170, 40) : new Color(170, 205, 255);
        private static Color BodyColor(int form) => form == 2 ? new Color(255, 214, 110) : new Color(235, 244, 255);

        public override string Texture => CEUtils.WhiteTexPath;

        private int Form => (int)Projectile.ai[1];
        private int Age => Lifetime - Projectile.timeLeft;
        private Vector2 Direction => Projectile.ai[0].ToRotationVector2();
        private Vector2 End => Projectile.Center + Direction * Length;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)Length;
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => Age < DamageFrames ? null : false;

        public override void AI() {
            int slimeType = ModContent.ProjectileType<CelestialSlimeStaffSlime>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == slimeType) {
                    Projectile.Center = p.Center + new Vector2(0f, CelestialSlimeStaffSlime.TotemOffsetY);
                    break;
                }
            }
            if (Main.dedServ) {
                return;
            }
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                VSlimeVfx.Cue("void_laser", Projectile.Center, SoundPitch, SoundVolume, SoundMaxInstances);
            }
            DelegateMethods.v3_1 = BodyColor(Form).ToVector3() * LightStrength;
            Utils.PlotTileLine(Projectile.Center, End, HalfWidth * 2f, DelegateMethods.CastLight);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, End, HitWidth, ref point);
        }

        //外晕、主体、火焰纹理与根部光晕走加法;芯线用普通混合画,奇点的黑芯在亮背景上也看得见
        public override bool PreDraw(ref Color lightColor) {
            float ignite = GodDraw.EaseOut(Age / (float)IgniteFrames);
            float fade = GodDraw.Span(Projectile.timeLeft, 0f, FadeFrames);
            float half = HalfWidth * ignite * MathHelper.Lerp(FadeMinWidth, 1f, fade);
            if (half <= MinVisibleHalfWidth) {
                return false;
            }
            Vector2 start = Projectile.Center;
            Vector2 end = End;
            Color outer = OuterColor(Form);
            Color body = BodyColor(Form);
            float scroll = Age * ScrollSpeed;

            GodDraw.BeginAdditive();
            GodDraw.Line(start, end, half * OuterWidth, GodDraw.Add(outer, OuterOpacity * fade), CapFraction);
            GodDraw.Line(start, end, half * BodyWidth, GodDraw.Add(body, BodyOpacity * fade), CapFraction);
            List<Vector2> points = new() { start, Vector2.Lerp(start, end, StreakInset), Vector2.Lerp(start, end, 1f - StreakInset), end };
            GodDraw.Strip(GodDraw.FireStreak, points, t => half * StreakWidth * MathF.Sin(MathHelper.Clamp(t, StreakEdge, 1f - StreakEdge) * MathHelper.Pi) + StreakMinHalf,
                t => GodDraw.Add(body, StreakOpacity * fade), -scroll, Length / StreakRepeat - scroll);
            GodDraw.Centered(GodDraw.Glow, start, GodDraw.Add(outer, fade), 0f, half * RootGlowSize / GodDraw.Glow.Width);
            GodDraw.Centered(GodDraw.Flash, start, GodDraw.Add(body, RootFlashOpacity * fade), Age * RootFlashSpin, RootFlashScale * ignite);
            GodDraw.End();

            GodDraw.BeginStraightAlpha();
            Color core = Form == 2 ? Color.Black : Color.White;
            GodDraw.Line(start, end, half * (Form == 2 ? CoreWidthDark : CoreWidthBright), core * fade, CapFraction, CEUtils.getExtraTex("white"));
            GodDraw.End();
            return false;
        }
    }
}
