using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.Constellation
{
    //ai[0] 宿主悠悠球 identity, ai[1] 剩余帧数(主球命中时主人端重置并同步); 宿主不在时只淡出不判定
    public class ConstellationPentagram : ModProjectile
    {
        //时间单位帧, 距离单位 px(80 px = 5 格), 角度单位 rad
        public const float Radius = 80f;
        public const int HitCooldown = 30;
        public const int FadeInFrames = 12;
        public const int FadeOutFrames = 20;
        public const float Spin = 0.02f;
        public const int RingSegments = 48;
        public const float CoreWidth = 2f;
        public const float GlowWidth = 6f;
        public const float VertexScale = 0.12f;

        public static readonly Color LineColor = new(150, 190, 255);
        public static readonly Color GlowColor = new(80, 110, 255);
        public static readonly Color VertexColor = new(220, 240, 255);

        private int age;
        private bool hostAlive;

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.timeLeft = 2;
        }

        public override void AI() {
            Projectile host = FindHost();
            hostAlive = host != null;
            if (hostAlive)
                Projectile.Center = host.Center;
            else if (Projectile.ai[1] > FadeOutFrames)
                Projectile.ai[1] = FadeOutFrames;
            Projectile.ai[1]--;
            if (Projectile.ai[1] <= 0f) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;
            if (age == 0 && !Main.dedServ)
                CEUtils.PlaySound("CastTriangles", 1.2f, Projectile.Center, 2, 0.5f);
            age++;
            Projectile.rotation += Spin;
            Projectile.Opacity = Math.Min(1f, Math.Min(age / (float)FadeInFrames, Projectile.ai[1] / FadeOutFrames));
            Lighting.AddLight(Projectile.Center, LineColor.ToVector3() * 0.4f * Projectile.Opacity);
        }

        private Projectile FindHost() {
            int type = ModContent.ProjectileType<ConstellationYoyo>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == type && p.identity == (int)Projectile.ai[0])
                    return p;
            }
            return null;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => hostAlive && Projectile.Opacity > 0.5f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => targetHitbox.Distance(Projectile.Center) <= Radius;

        public override bool PreDraw(ref Color lightColor) {
            Texture2D px = Projectile.GetTexture();
            Texture2D star = CEUtils.getExtraTex("StarTexture");
            Vector2 c = Projectile.Center - Main.screenPosition;
            float o = Projectile.Opacity;
            Color core = LineColor * o;
            Color glow = (GlowColor with { A = 0 }) * 0.8f * o;
            for (int i = 0; i < RingSegments; i++) {
                float a0 = MathHelper.TwoPi * i / RingSegments - Projectile.rotation * 0.5f;
                float a1 = MathHelper.TwoPi * (i + 1) / RingSegments - Projectile.rotation * 0.5f;
                DrawLine(px, c + a0.ToRotationVector2() * Radius, c + a1.ToRotationVector2() * Radius, core, glow);
            }
            Vector2[] points = new Vector2[5];
            for (int i = 0; i < 5; i++)
                points[i] = c + (Projectile.rotation - MathHelper.PiOver2 + MathHelper.TwoPi * i / 5f).ToRotationVector2() * Radius;
            for (int i = 0; i < 5; i++)
                DrawLine(px, points[i], points[(i + 2) % 5], core, glow);
            for (int i = 0; i < 5; i++)
                Main.EntitySpriteDraw(star, points[i], null, (VertexColor with { A = 0 }) * o, 0f, star.Size() / 2f, VertexScale, SpriteEffects.None);
            return false;
        }

        private static void DrawLine(Texture2D px, Vector2 a, Vector2 b, Color core, Color glow) {
            Vector2 d = b - a;
            float rot = d.ToRotation();
            float len = d.Length();
            Main.EntitySpriteDraw(px, a, null, glow, rot, new Vector2(0f, 0.5f), new Vector2(len, GlowWidth), SpriteEffects.None);
            Main.EntitySpriteDraw(px, a, null, core, rot, new Vector2(0f, 0.5f), new Vector2(len, CoreWidth), SpriteEffects.None);
        }
    }
}
