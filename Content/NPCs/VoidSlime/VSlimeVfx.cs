using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.VoidSlime
{
    /// <summary>虚空史莱姆的表现门面,纯本地,不回写 gameplay,色阶取自本体贴图</summary>
    public static class VSlimeVfx
    {
        //虚空紫的四档和星点的金
        public static readonly Color VoidDeep = new(38, 12, 74);
        public static readonly Color VoidPurple = new(104, 40, 172);
        public static readonly Color VoidBright = new(164, 92, 240);
        public static readonly Color Lavender = new(198, 164, 255);
        public static readonly Color StarGold = new(255, 232, 150);
        public static readonly Color StarCore = new(255, 251, 226);
        //苍白壳的灰阶
        public static readonly Color PaleDark = new(58, 58, 66);
        public static readonly Color PaleMid = new(128, 128, 138);
        public static readonly Color PaleLight = new(206, 206, 214);
        //红色黑洞与蓄力光
        public static readonly Color HoleRed = new(255, 64, 44);
        public static readonly Color HoleEmber = new(255, 150, 70);
        public static readonly Color HoleDark = new(96, 8, 18);
        //辉星与射线
        public static readonly Color StarBlue = new(110, 170, 255);
        public static readonly Color StarIce = new(196, 228, 255);
        public static readonly Color StarDeepBlue = new(44, 76, 210);
        public static readonly Color StarOrange = new(255, 168, 70);
        public static readonly Color GiantRed = new(255, 90, 50);
        /// <summary>暗色描边,只配真透明贴图</summary>
        public static readonly Color Rim = new(20, 8, 44);

        //天幕压暗去饱和,亮色留给本体和弹幕
        public static readonly Color SkyTop = new(4, 4, 14);
        public static readonly Color SkyHorizon = new(24, 14, 48);
        public static readonly Color AuroraViolet = new(150, 96, 230);
        public static readonly Color AuroraTeal = new(84, 190, 196);
        public static readonly Color SkyTileTint = new(150, 140, 220);

        /// <summary>小辉星的三种颜色,下标同弹幕的 ai[2]</summary>
        public static Color StarletColor(int kind) => kind == 1 ? VoidBright : kind == 2 ? StarOrange : StarBlue;

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
        public static void Flash(Vector2 center, float strength, float maxDist = 3600f) {
            if (Main.dedServ || Main.LocalPlayer.Distance(center) > maxDist) {
                return;
            }
            CalamityEntropy.FlashEffectStrength = Math.Max(CalamityEntropy.FlashEffectStrength, strength);
        }

        /// <summary>天幕上的极光跟着拍点亮一下</summary>
        public static void SkyFlash(float strength) => Skies.VSlimeSkyDrive.PushFlash(strength);

        /// <summary>向四周辸散的尘点</summary>
        public static void Scatter(Vector2 pos, int dustType, int count, float minSpeed, float maxSpeed, float scale = 1f) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < count; i++) {
                Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(minSpeed, maxSpeed);
                Dust d = Dust.NewDustPerfect(pos, dustType, v, 100, default, Main.rand.NextFloat(0.9f, 1.5f) * scale);
                d.noGravity = true;
            }
        }

        /// <summary>虚空紫的星尘</summary>
        public static void Stardust(Vector2 pos, int count, float minSpeed, float maxSpeed, float scale = 1f)
            => Scatter(pos, Main.rand.NextBool(3) ? DustID.GemAmethyst : DustID.PurpleTorch, count, minSpeed, maxSpeed, scale);

        /// <summary>苍白壳的碎屑,带重力</summary>
        public static void PaleShards(Vector2 pos, int count, float minSpeed, float maxSpeed) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < count; i++) {
                Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(minSpeed, maxSpeed);
                Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(60f, 40f), DustID.Silver, v, 60, default, Main.rand.NextFloat(1f, 1.7f));
                d.noGravity = Main.rand.NextBool(3);
            }
        }

        /// <summary>从外圈朝中心收束的尘点,蓄力用</summary>
        public static void Converge(Vector2 center, float minDist, float maxDist, int dustType = DustID.PurpleTorch) {
            if (Main.dedServ) {
                return;
            }
            Vector2 from = center + Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(minDist, maxDist);
            Dust d = Dust.NewDustPerfect(from, dustType, (center - from) * 0.07f, 100, default, Main.rand.NextFloat(0.8f, 1.3f));
            d.noGravity = true;
        }

        /// <summary>爆闪:脉冲环加星芒加火花,size 1 约等于一颗小辉星的爆点</summary>
        public static void Burst(Vector2 pos, float size, Color ring, Color core) {
            if (Main.dedServ) {
                return;
            }
            PRTLoader.NewParticle<PRT_PulseRing>(pos, Vector2.Zero, ring, 0.1f).Configure(1.6f * size, 18);
            PRTLoader.NewParticle<PRT_SparkleCal>(pos, Vector2.Zero, core, 1.8f * size).Configure(ring, 16, 0f, 2.5f);
            int sparks = (int)(10 * size);
            for (int i = 0; i < sparks; i++) {
                Color color = Color.Lerp(ring, core, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_SparkCal>(pos, Main.rand.NextVector2Circular(9f, 9f) * MathF.Sqrt(size), color, Main.rand.NextFloat(0.6f, 1.1f))
                    .Configure(false, 30);
            }
        }

        /// <summary>虚空紫配金芯的爆闪</summary>
        public static void VoidBurst(Vector2 pos, float size) => Burst(pos, size, VoidBright, StarCore);

        /// <summary>蓝色星辉的爆闪</summary>
        public static void StarBurst(Vector2 pos, float size) => Burst(pos, size, StarBlue, StarIce);
    }
}
