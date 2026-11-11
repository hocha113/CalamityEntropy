using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SandseaDragon
{
    /// <summary>沙海冥龙的表现门面,纯本地,不回写 gameplay</summary>
    public static class SandseaVfx
    {
        /// <summary>原版沙尘龙卷的沙黄</summary>
        public static readonly Color Sand = new(212, 192, 100);
        public static readonly Color SandDark = new(131, 95, 57);
        public static readonly Color SandShadow = new(75, 48, 42);
        public static readonly Color SandLight = new(240, 226, 160);
        /// <summary>背鳍亮起后的青蓝</summary>
        public static readonly Color FinGlow = new(90, 220, 255);
        public static readonly Color FinDeep = new(40, 110, 190);
        /// <summary>冲刺屏障与禁戒能量的橙黄</summary>
        public static readonly Color Forbidden = new(255, 196, 70);
        public static readonly Color Venom = new(188, 120, 255);

        public static void Sound(SoundStyle style, Vector2 pos) {
            if (!Main.dedServ) {
                SoundEngine.PlaySound(style, pos);
            }
        }

        /// <summary>带距离衰减的一次性震屏</summary>
        public static void Shake(Vector2 center, float strength, float maxDist = 2800f) {
            if (!Main.dedServ) {
                CEUtils.SetShake(center, strength, maxDist);
            }
        }

        /// <summary>战吼:沙鲨的嗓音压低,大吼再叠一层龙啸</summary>
        public static void Roar(Vector2 pos, bool big) {
            if (Main.dedServ) {
                return;
            }
            SoundEngine.PlaySound(SoundID.Zombie7 with { Pitch = -0.55f, Volume = 1f, MaxInstances = 2 }, pos);
            if (big) {
                SoundEngine.PlaySound(SoundID.DD2_BetsyScream with { Pitch = -0.45f, Volume = 0.8f, MaxInstances = 2 }, pos);
            }
        }

        /// <summary>向上散开的沙粒,size 1 约等于一节身体破沙</summary>
        public static void SandBurst(Vector2 pos, float size, Vector2 bias = default) {
            if (Main.dedServ) {
                return;
            }
            int count = (int)(14 * size);
            for (int i = 0; i < count; i++) {
                Vector2 v = new Vector2(Main.rand.NextFloat(-1f, 1f) * 5f, -Main.rand.NextFloat(2f, 9f)) * MathF.Sqrt(size) + bias;
                Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(30f, 12f) * size, Main.rand.NextBool(3) ? DustID.Sandnado : DustID.Sand, v,
                    0, default, Main.rand.NextFloat(1.1f, 1.9f));
                d.noGravity = false;
            }
            int clouds = (int)(3 * size);
            for (int i = 0; i < clouds; i++) {
                Vector2 v = new Vector2(Main.rand.NextFloat(-2.4f, 2.4f), -Main.rand.NextFloat(0.6f, 2.6f)) + bias * 0.4f;
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(pos + Main.rand.NextVector2Circular(36f, 10f) * size, v, Color.Lerp(SandDark, Sand, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.9f) * MathF.Sqrt(size)).Configure(0.55f, Main.rand.Next(34, 54), Main.rand.NextFloat(-0.03f, 0.03f));
            }
        }

        /// <summary>浅游时背鳍划开沙面的细沙,朝行进的反方向甩</summary>
        public static void Wake(Vector2 surfacePos, float travelDirX) {
            if (Main.dedServ) {
                return;
            }
            Vector2 v = new Vector2(-travelDirX * Main.rand.NextFloat(0.5f, 2.5f), -Main.rand.NextFloat(1.5f, 4.5f));
            Dust d = Dust.NewDustPerfect(surfacePos + new Vector2(Main.rand.NextFloat(-18f, 18f), -2f), DustID.Sand, v, 0, default, Main.rand.NextFloat(0.9f, 1.4f));
            d.noGravity = false;
        }

        /// <summary>砸地:沙浪、冲击环、震屏和闷响</summary>
        public static void Slam(Vector2 pos, float size) {
            if (Main.dedServ) {
                return;
            }
            SandBurst(pos, 3f * size);
            for (int i = 0; i < (int)(22 * size); i++) {
                float side = Main.rand.NextBool() ? -1f : 1f;
                Vector2 v = new Vector2(side * Main.rand.NextFloat(4f, 13f), -Main.rand.NextFloat(1f, 6f));
                Dust d = Dust.NewDustPerfect(pos + new Vector2(side * Main.rand.NextFloat(0f, 40f), -4f), DustID.Sandnado, v, 0, default, Main.rand.NextFloat(1.2f, 2f));
                d.noGravity = false;
            }
            PRTLoader.NewParticle<PRT_PulseRing>(pos, Vector2.Zero, Sand, 0.2f).Configure(2.4f * size, 20);
            Shake(pos, 9f * size);
            SoundEngine.PlaySound(SoundID.DD2_OgreGroundPound with { Volume = 0.9f, Pitch = -0.2f, MaxInstances = 2 }, pos);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.4f, MaxInstances = 2 }, pos);
        }

        /// <summary>破沙或入沙的短促闷响</summary>
        public static void BreachSound(Vector2 pos, float volume = 0.8f) {
            Sound(SoundID.DD2_MonkStaffGroundImpact with { Volume = volume, Pitch = -0.35f, MaxInstances = 3 }, pos);
        }

        /// <summary>青蓝火花,背鳍亮起与眼部闪光用</summary>
        public static void FinSparks(Vector2 pos, int count, float speed) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < count; i++) {
                Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.3f, 1f) * speed;
                PRTLoader.NewParticle<PRT_SparkCal>(pos, v, Color.Lerp(FinGlow, Color.White, Main.rand.NextFloat(0.4f)), Main.rand.NextFloat(0.5f, 0.9f))
                    .Configure(false, 26);
            }
        }

        /// <summary>锁血时从身上喷出的白色粒子</summary>
        public static void WhiteSpray(Vector2 pos, int count) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < count; i++) {
                Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 12f);
                Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(40f, 40f), DustID.SilverFlame, v, 0, Color.White, Main.rand.NextFloat(1.1f, 1.8f));
                d.noGravity = true;
            }
            //粉尘吃环境光,暗处和远处不显眼,另加自发光的白色火花
            for (int i = 0; i < Math.Max(1, count / 2); i++) {
                Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 16f);
                PRTLoader.NewParticle<PRT_SparkCal>(pos + Main.rand.NextVector2Circular(30f, 30f), v, Color.White, Main.rand.NextFloat(0.6f, 1.1f))
                    .Configure(false, Main.rand.Next(18, 30));
            }
        }

        /// <summary>爆闪环,甲虫炸弹与转阶段用</summary>
        public static void Ring(Vector2 pos, Color color, float finalScale, int life = 18) {
            if (!Main.dedServ) {
                PRTLoader.NewParticle<PRT_PulseRing>(pos, Vector2.Zero, color, 0.15f).Configure(finalScale, life);
            }
        }
    }
}
