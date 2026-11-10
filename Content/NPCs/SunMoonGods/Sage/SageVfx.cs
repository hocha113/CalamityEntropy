using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Skies;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SunMoonGods.Sage
{
    /// <summary>星月贤者的表现门面,纯本地,不回写 gameplay,配色与星月套装同调</summary>
    public static class SageVfx
    {
        public static readonly Color StarWhite = new(235, 240, 255);
        public static readonly Color StarBlue = new(110, 140, 255);
        public static readonly Color StarViolet = new(180, 130, 255);
        public static readonly Color MoonSilver = new(200, 215, 245);
        /// <summary>暗色描边,只配真透明贴图</summary>
        public static readonly Color Rim = new(24, 20, 70);

        //天幕压到深海军蓝,亮色留给本体和弹幕
        public static readonly Color SkyTop = new(5, 7, 22);
        public static readonly Color SkyHorizon = new(26, 22, 70);
        /// <summary>三阶段的地平线,更紫</summary>
        public static readonly Color SkyHorizonLate = new(44, 26, 96);
        public static readonly Color SkyTileTint = new(150, 170, 255);

        public static void Sound(SoundStyle style, Vector2 pos) {
            if (!Main.dedServ) {
                SoundEngine.PlaySound(style, pos);
            }
        }

        /// <summary>按文件名播模组自带的音效,pitch 1 是原调</summary>
        public static void Cue(string name, Vector2 pos, float pitch = 1f, float volume = 1f, int maxInstances = 4) {
            CEUtils.PlaySound(name, pitch, pos, maxInstances, volume);
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

        /// <summary>天幕上的月亮跟着拍点亮一下</summary>
        public static void SkyFlash(float strength) => GodSkyDrive.Moon.PushFlash(strength);

        /// <summary>向四周迸散的星尘</summary>
        public static void Stardust(Vector2 pos, int count, float minSpeed, float maxSpeed, float scale = 1f) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < count; i++) {
                Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(minSpeed, maxSpeed);
                int type = Main.rand.NextBool(3) ? DustID.PurpleTorch : DustID.MagicMirror;
                Dust d = Dust.NewDustPerfect(pos, type, v, 100, default, Main.rand.NextFloat(0.9f, 1.5f) * scale);
                d.noGravity = true;
            }
        }

        /// <summary>从外圈朝中心收束的星尘,蓄力用</summary>
        public static void Converge(Vector2 center, float minDist, float maxDist) {
            if (Main.dedServ) {
                return;
            }
            Vector2 from = center + Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(minDist, maxDist);
            Dust d = Dust.NewDustPerfect(from, DustID.MagicMirror, (center - from) * 0.07f, 100, default, Main.rand.NextFloat(0.8f, 1.3f));
            d.noGravity = true;
        }

        /// <summary>爆闪:脉冲环加星芒加星屑,size 1 约等于一枚星弹的爆点</summary>
        public static void Burst(Vector2 pos, float size) {
            if (Main.dedServ) {
                return;
            }
            PRTLoader.NewParticle<PRT_PulseRing>(pos, Vector2.Zero, StarBlue, 0.1f).Configure(1.6f * size, 18);
            PRTLoader.NewParticle<PRT_SparkleCal>(pos, Vector2.Zero, StarWhite, 1.8f * size).Configure(StarViolet, 16, 0f, 2.5f);
            int sparks = (int)(10 * size);
            for (int i = 0; i < sparks; i++) {
                Color color = Color.Lerp(StarBlue, StarViolet, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_SparkCal>(pos, Main.rand.NextVector2Circular(9f, 9f) * MathF.Sqrt(size), color, Main.rand.NextFloat(0.6f, 1.1f))
                    .Configure(false, 30);
            }
        }
    }
}