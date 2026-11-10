using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess
{
    /// <summary>
    /// 坠日:在本体头顶蓄成日球,落点锁定后画落点圈,掷出、爆炸、散出火羽环
    /// ai[0] 本体下标,ai[1] 火羽环圈数,localAI[0] 存活帧
    /// 落点读本体上下文的 LockedPoint,那是过线事实,掷出前每帧刷新
    /// </summary>
    public class SunfallOrb : GodHostileProjectile
    {
        private const int ChargeFrames = SunDirector.SunfallThrowBeat - SunDirector.SunfallOrbBeat;
        private const int LockAge = SunDirector.SunfallLockBeat - SunDirector.SunfallOrbBeat;
        private const int ImpactAge = ChargeFrames + SunDirector.SunfallFlightFrames;
        /// <summary>爆炸判定结束后余光停留的帧数</summary>
        private const int LingerFrames = 16;
        private const float OrbRadius = 46f;

        public override string Texture => CEUtils.WhiteTexPath;
        public override GodKind Kind => GodKind.Sun;
        public override int DebuffType => ModContent.BuffType<SunblazeBurn>();
        public override int DebuffTime => SunDirector.BurnFrames;

        private Vector2 throwFrom;
        private Vector2 landing;

        private float Age => Projectile.localAI[0];
        private bool Blasting => Age > ImpactAge && Age <= ImpactAge + SunDirector.SunfallBlastFrames;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            //timeLeft 不过线,首帧各端自己定
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = ImpactAge + SunDirector.SunfallBlastFrames + LingerFrames;
                throwFrom = Projectile.Center;
                landing = Projectile.Center + new Vector2(0f, 300f);
            }
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;

            if (Age <= ChargeFrames) {
                NPC owner = OwnerNpc<SunGoddess>(Projectile.ai[0]);
                if (owner == null) {
                    Projectile.Kill();
                    return;
                }
                Projectile.Center = owner.Center + new Vector2(0f, -SunDirector.SunfallOrbHeight);
                Projectile.scale = MathHelper.Lerp(0.15f, 1f, GodDraw.EaseOut(Age / ChargeFrames));
                if (Age >= LockAge && owner.ModNPC is SunGoddess goddess && goddess.Context != null && goddess.Context.LockedPoint != Vector2.Zero) {
                    landing = goddess.Context.LockedPoint;
                }
                if (Age % 2f == 0f) {
                    SunVfx.Converge(Projectile.Center, 70f, 150f);
                }
                if (Age == ChargeFrames) {
                    throwFrom = Projectile.Center;
                    SunVfx.Sound(SoundID.DD2_BetsyFireballShot with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center);
                }
            }
            else if (Age <= ImpactAge) {
                float p = (Age - ChargeFrames) / SunDirector.SunfallFlightFrames;
                Projectile.Center = Vector2.Lerp(throwFrom, landing, GodDraw.EaseIn(p));
                if (Age == ImpactAge) {
                    Explode();
                }
            }
            Lighting.AddLight(Projectile.Center, 1.4f * Projectile.scale, 0.9f * Projectile.scale, 0.35f * Projectile.scale);
        }

        private void Explode() {
            Projectile.Center = landing;
            SunVfx.Burst(landing, 3f);
            SunVfx.Embers(landing, 30, 4f, 13f, 1.3f);
            SunVfx.Shake(landing, 9f);
            SunVfx.Sound(SoundID.DD2_BetsyFireballImpact with { Volume = 1f, Pitch = -0.3f }, landing);
            SunVfx.Sound(SoundID.Item14 with { Volume = 0.7f }, landing);
            if (VaultUtils.isClient) {
                return;
            }
            int damage = SunDirector.DmgFlare;
            if (OwnerNpc<SunGoddess>(Projectile.ai[0])?.ModNPC is SunGoddess goddess) {
                damage = goddess.ProjDamage(SunDirector.DmgFlare);
            }
            int rings = Projectile.ai[1] >= 2f ? 2 : 1;
            int type = ModContent.ProjectileType<SunFlareBolt>();
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int r = 0; r < rings; r++) {
                float speed = r == 0 ? SunDirector.SunfallRingSpeed : SunDirector.SunfallSecondRingSpeed;
                for (int i = 0; i < SunDirector.SunfallRingBolts; i++) {
                    //第二圈错开半格并晚几帧起飞
                    float angle = baseAngle + MathHelper.TwoPi * (i + 0.5f * r) / SunDirector.SunfallRingBolts;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), landing, angle.ToRotationVector2() * speed, type, damage, 0f, Main.myPlayer,
                        0f, r * SunDirector.SunfallSecondRingDelay);
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Age > ImpactAge) {
                return Blasting && CircleHits(landing, SunDirector.SunfallBlastRadius, targetHitbox);
            }
            return CircleHits(Projectile.Center, OrbRadius * Projectile.scale * 0.8f, targetHitbox);
        }

        private static bool CircleHits(Vector2 center, float radius, Rectangle box) {
            float x = MathHelper.Clamp(center.X, box.Left, box.Right);
            float y = MathHelper.Clamp(center.Y, box.Top, box.Bottom);
            return Vector2.DistanceSquared(center, new Vector2(x, y)) <= radius * radius;
        }

        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.localAI[1] == 0f) {
                return false;
            }
            if (Age >= LockAge && Age <= ImpactAge) {
                DrawMarker();
            }
            if (Age <= ImpactAge) {
                DrawOrb();
            }
            else {
                DrawBlast();
            }
            return false;
        }

        /// <summary>落点圈的半径就是爆炸判定半径,内圈收拢表示剩余时间</summary>
        private void DrawMarker() {
            float p = GodDraw.Span(Age, LockAge, ImpactAge);
            float flicker = p > 0.8f ? 0.6f + 0.4f * MathF.Sin(Age * 1.8f) : 1f;
            float radius = SunDirector.SunfallBlastRadius;
            GodDraw.BeginAdditive();
            GodDraw.Centered(GodDraw.SoftLight, landing, GodDraw.Add(SunVfx.Ember, 0.1f + 0.12f * p), 0f, radius * 2.3f / GodDraw.SoftLight.Width);
            GodDraw.RingStrip(GodDraw.SoftLine, landing, radius, 3f + 3f * p, GodDraw.Add(SunVfx.Gold, (0.45f + 0.5f * p) * flicker), Age * 0.01f);
            GodDraw.RingStrip(GodDraw.SoftLine, landing, radius * (1f - p), 2.5f, GodDraw.Add(SunVfx.Ember, 0.7f * flicker));
            GodDraw.End();
        }

        private void DrawOrb() {
            Vector2 center = Projectile.Center;
            float radius = OrbRadius * Projectile.scale;
            float time = Age * 0.02f;
            Texture2D corona = GodDraw.Corona;
            Texture2D fire = CEUtils.getExtraTex("Tornade_Fire");

            GodDraw.BeginAdditive();
            if (Age > ChargeFrames) {
                //掷出后身后拖一串渐小的余辉
                Vector2 back = (throwFrom - landing).SafeNormalize(Vector2.Zero);
                for (int k = 1; k <= 5; k++) {
                    GodDraw.Centered(GodDraw.Glow, center + back * (radius * 0.9f * k), GodDraw.Add(SunVfx.Ember, 0.5f * (1f - k / 6f)), 0f, radius * (3f - 0.35f * k) / 256f);
                }
            }
            GodDraw.Centered(corona, center, GodDraw.Add(SunVfx.Gold, 0.8f), time, radius * 4.4f / corona.Width);
            GodDraw.Centered(corona, center, GodDraw.Add(SunVfx.Ember, 0.6f), -time * 1.7f, radius * 3.4f / corona.Width);
            GodDraw.End();

            //实心橙盘做轮廓,上面再叠极坐标火纹和白热芯
            GodDraw.BeginStraightAlpha();
            GodDraw.Centered(GodDraw.Disc, center, GodDraw.Straight(SunVfx.Ember, 1f), 0f, radius * 2f / GodDraw.Disc.Width);
            GodDraw.End();

            GodDraw.BeginAdditive();
            GodDraw.RingStrip(fire, center, radius * 0.5f, radius * 0.5f, GodDraw.Add(SunVfx.Gold, 0.9f), time, MathHelper.TwoPi, 36, 2f);
            GodDraw.Centered(GodDraw.Glow, center, GodDraw.Add(SunVfx.Core, 0.9f), 0f, radius * 2.4f / 256f);
            GodDraw.Centered(GodDraw.BloomRing, center, GodDraw.Add(SunVfx.Gold, 0.85f), 0f, GodDraw.RingScale(radius));
            GodDraw.End();
        }

        private void DrawBlast() {
            float q = MathHelper.Clamp((Age - ImpactAge) / (SunDirector.SunfallBlastFrames + LingerFrames), 0f, 1f);
            float radius = SunDirector.SunfallBlastRadius;
            float grow = GodDraw.EaseOut(q * 2.2f);
            Texture2D ring = CEUtils.getExtraTex("CircularSmearFire1");

            GodDraw.BeginStraightAlpha();
            GodDraw.Centered(ring, landing, GodDraw.Straight(Color.White, 1f - q), Age * 0.2f, radius * 2f * grow / (ring.Width * 0.83f));
            GodDraw.End();

            GodDraw.BeginAdditive();
            GodDraw.Centered(GodDraw.Flash, landing, GodDraw.Add(SunVfx.Core, 1f - q), Age * 0.05f, radius * 2.8f * (0.5f + 0.6f * grow) / GodDraw.Flash.Width);
            GodDraw.Centered(GodDraw.SoftLight, landing, GodDraw.Add(SunVfx.Ember, 0.7f * (1f - q)), 0f, radius * 2.6f / GodDraw.SoftLight.Width);
            GodDraw.RingStrip(GodDraw.SoftLine, landing, radius * grow, 4f + 12f * (1f - q), GodDraw.Add(SunVfx.Gold, 1f - q));
            GodDraw.End();
        }
    }
}