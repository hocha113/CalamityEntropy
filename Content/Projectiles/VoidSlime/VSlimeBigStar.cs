using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 紫色大辉星,从出生点飞向锁定的落点,落点标记全程可见,到点爆成一圈小辉星
    /// 轨迹由出生点、落点和存活帧解析算出,各端一致
    /// ai[0] ai[1] 落点坐标,ai[2] 爆开的规格(0 虚空形态,1 奇点形态多四颗追踪星),localAI[0] 存活帧
    /// </summary>
    public class VSlimeBigStar : VSlimeHostileProjectile
    {
        private Vector2 origin;
        private bool initialized;

        private float Age => Projectile.localAI[0];
        private Vector2 Destination => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        private float Progress => MathHelper.Clamp(Age / VSlimeDirector.BigStarFlight, 0f, 1f);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 44;
            Projectile.height = 44;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (!initialized) {
                initialized = true;
                origin = Projectile.Center;
                //timeLeft 不过线,首帧各端自己定,多留几帧等权威端的爆开
                Projectile.timeLeft = VSlimeDirector.BigStarFlight + 30;
            }
            Projectile.localAI[0]++;
            //起步慢,越飞越快,落点前没有减速
            float eased = VSlimeDirector.EaseIn(Progress) * 0.7f + Progress * 0.3f;
            Vector2 next = Vector2.Lerp(origin, Destination, eased);
            Projectile.velocity = next - Projectile.Center;
            Projectile.Center = next;
            Projectile.rotation += 0.12f;
            Projectile.Opacity = MathHelper.Clamp(Age / 8f, 0f, 1f);
            Lighting.AddLight(Projectile.Center, 0.7f, 0.4f, 1.1f);

            if (Age >= VSlimeDirector.BigStarFlight) {
                Projectile.Center = Destination;
                if (!VaultUtils.isClient) {
                    Detonate();
                    Projectile.Kill();
                }
            }
        }

        /// <summary>只在权威端:等角一圈蓝色小辉星,奇点形态再加四颗弱追踪的紫色小辉星</summary>
        private void Detonate() {
            int phase = Projectile.ai[2] >= 1f ? VSlimeForm.Singularity : VSlimeForm.Void;
            int blue = VSlimeDirector.BlastBlueCount(phase);
            int homing = VSlimeDirector.BlastHomingCount(phase);
            int type = ModContent.ProjectileType<VSlimeStarlet>();
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < blue; i++) {
                Vector2 dir = (baseAngle + MathHelper.TwoPi * i / blue).ToRotationVector2();
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Destination, dir * VSlimeDirector.BlastStarletSpeed, type,
                    VoidSlimeNPC.ProjDamage(VSlimeDirector.DmgStarlet), 0f, Main.myPlayer, VSlimeStarlet.ModeStraight, VSlimeDirector.BlastStarletLife, VSlimeStarlet.ColorBlue);
            }
            for (int i = 0; i < homing; i++) {
                Vector2 dir = (baseAngle + MathHelper.TwoPi * (i + 0.5f) / homing).ToRotationVector2();
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Destination, dir * VSlimeDirector.HomingStarletSpeed, type,
                    VoidSlimeNPC.ProjDamage(VSlimeDirector.DmgHomingStarlet), 0f, Main.myPlayer, VSlimeStarlet.ModeHoming, VSlimeDirector.HomingStarletLife, VSlimeStarlet.ColorPurple);
            }
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircleHits(Projectile.Center, 22f, targetHitbox);

        public override void OnKill(int timeLeft) {
            VSlimeVfx.VoidBurst(Projectile.Center, 2.2f);
            VSlimeVfx.Stardust(Projectile.Center, 18, 2f, 9f, 1.2f);
            VSlimeVfx.Shake(Projectile.Center, 4f, 2000f);
            VSlimeVfx.Cue("LunarStar", Projectile.Center, 0.9f, 0.7f, 3);
        }

        public override bool PreDraw(ref Color lightColor) {
            if (!initialized) {
                return false;
            }
            float opacity = Projectile.Opacity;
            float p = Progress;
            float time = Main.GlobalTimeWrappedHourly;

            //落点标记:外环随进度收紧,十字指向爆心
            GodDraw.BeginAdditive();
            Vector2 dest = Destination;
            float flicker = p > 0.75f ? 0.6f + 0.4f * MathF.Sin(Age * 1.4f) : 1f;
            GodDraw.Centered(GodDraw.BloomRing, dest, GodDraw.Add(VSlimeVfx.VoidBright, (0.35f + 0.5f * p) * flicker * opacity), 0f, GodDraw.RingScale(MathHelper.Lerp(86f, 30f, p)));
            GodDraw.Centered(GodDraw.BloomRing, dest, GodDraw.Add(VSlimeVfx.Lavender, 0.3f * opacity), 0f, GodDraw.RingScale(90f));
            for (int k = 0; k < 2; k++) {
                Vector2 arm = (MathHelper.PiOver4 + MathHelper.PiOver2 * k).ToRotationVector2() * 70f;
                GodDraw.Line(dest - arm, dest + arm, 1.5f + 1.5f * p, GodDraw.Add(VSlimeVfx.Lavender, (0.25f + 0.5f * p) * flicker * opacity), 0.3f);
            }
            GodDraw.Centered(GodDraw.Star, Projectile.Center, GodDraw.Add(VSlimeVfx.VoidBright, 0.6f * opacity), time * 1.5f, 0.42f);
            GodDraw.End();

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            DrawBolt(tex, tex.Frame(), VSlimeVfx.VoidBright, opacity, Projectile.scale, 0.8f);
            return false;
        }
    }
}
