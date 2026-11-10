using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Skies;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Goddess
{
    /// <summary>太阳女神的表现门面,纯本地,不回写 gameplay,配色与烈阳套装同调</summary>
    public static class SunVfx
    {
        public static readonly Color Core = new(255, 248, 220);
        public static readonly Color Gold = new(255, 196, 90);
        public static readonly Color Ember = new(255, 120, 40);
        public static readonly Color Crimson = new(190, 46, 20);
        /// <summary>暗底色,只配真透明贴图</summary>
        public static readonly Color Dark = new(70, 18, 10);

        //天幕压暗去饱和,主题金色留给本体和弹幕
        public static readonly Color SkyTop = new(30, 14, 12);
        public static readonly Color SkyHorizon = new(104, 42, 20);
        /// <summary>三阶段的地平线,更红</summary>
        public static readonly Color SkyHorizonLate = new(126, 36, 16);
        public static readonly Color SkyTileTint = new(255, 216, 176);

        public static void Sound(SoundStyle style, Vector2 pos) {
            if (!Main.dedServ) {
                SoundEngine.PlaySound(style, pos);
            }
        }

        /// <summary>带距离衰减的一次性震屏</summary>
        public static void Shake(Vector2 center, float strength, float maxDist = 3200f) {
            if (!Main.dedServ) {
                CEUtils.SetShake(center, strength, maxDist);
            }
        }

        /// <summary>全屏白闪,只抬高不压低,每帧由模组统一衰减</summary>
        public static void Flash(Vector2 center, float strength) {
            if (Main.dedServ || Main.LocalPlayer.Distance(center) > 3200f) {
                return;
            }
            CalamityEntropy.FlashEffectStrength = Math.Max(CalamityEntropy.FlashEffectStrength, strength);
        }

        /// <summary>天幕上的太阳跟着拍点亮一下</summary>
        public static void SkyFlash(float strength) => GodSkyDrive.Sun.PushFlash(strength);

        /// <summary>向四周迸散的火星</summary>
        public static void Embers(Vector2 pos, int count, float minSpeed, float maxSpeed, float scale = 1f) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < count; i++) {
                Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(minSpeed, maxSpeed);
                Dust d = Dust.NewDustPerfect(pos, DustID.SolarFlare, v, 100, default, Main.rand.NextFloat(0.9f, 1.5f) * scale);
                d.noGravity = true;
            }
        }

        /// <summary>从外圈朝中心收束的火星,蓄力用</summary>
        public static void Converge(Vector2 center, float minDist, float maxDist) {
            if (Main.dedServ) {
                return;
            }
            Vector2 from = center + Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(minDist, maxDist);
            Dust d = Dust.NewDustPerfect(from, DustID.SolarFlare, (center - from) * 0.07f, 100, default, Main.rand.NextFloat(0.8f, 1.3f));
            d.noGravity = true;
        }

        /// <summary>爆闪:脉冲环加星芒加火花,size 1 约等于一只神鸟的爆点</summary>
        public static void Burst(Vector2 pos, float size) {
            if (Main.dedServ) {
                return;
            }
            PRTLoader.NewParticle<PRT_PulseRing>(pos, Vector2.Zero, Ember, 0.1f).Configure(1.6f * size, 18);
            PRTLoader.NewParticle<PRT_SparkleCal>(pos, Vector2.Zero, Core, 1.8f * size).Configure(Ember, 16, 0f, 2.5f);
            int sparks = (int)(10 * size);
            for (int i = 0; i < sparks; i++) {
                Color color = Color.Lerp(Ember, Gold, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_SparkCal>(pos, Main.rand.NextVector2Circular(9f, 9f) * MathF.Sqrt(size), color, Main.rand.NextFloat(0.6f, 1.1f))
                    .Configure(true, 30);
            }
        }
    }
}