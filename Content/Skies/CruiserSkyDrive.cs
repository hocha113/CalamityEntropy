using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>
    /// 巡游者天空强度中枢(续租模式,参照 CWR MLordEclipse):驱动源每帧本地上报,
    /// 下一帧未续租自动过期;各端本地观察 NPC 状态驱动,不走网络包。
    /// 来源:CruiserHead AI(登场窗 noaitime 渐临 0→0.6,开战推满,P2 抬躁动;
    /// Boss 在投瓶瞬间即已生成并骑瓶蓄力,故单点上报即覆盖召唤全程)、
    /// 旧 crSky 计时(VoidMonolith 佩戴,走弱档)。
    /// Intensity 是演出强度曲线,与 CrSky 的存在包络 opacity 相乘使用。
    /// </summary>
    public static class CruiserSkyDrive
    {
        //强度逼近步长:进快出缓(登场揭幕约 18 tick 推满,离场约 2 秒收干)
        private const float RisePerTick = 1f / 45f;
        private const float FallPerTick = 1f / 120f;
        //旧 crSky 计时折算的弱档强度(修饰性用途:轻微扭曲、无闪电)
        private const float LegacyDrive = 0.35f;

        /// <summary>演出强度 0~1(平滑后)。扭曲滤镜强度、闪电解锁、附加层增亮都读它。</summary>
        public static float Intensity { get; private set; }

        /// <summary>躁动 0~1(平滑后)。P2 抬升闪电频率与扭曲湍流。</summary>
        public static float Agitation { get; private set; }

        /// <summary>待消费的闪电爆发条数(登场揭幕/二阶段转换的一次性演出),由 CrSky 在 Update 里取走。</summary>
        public static int PendingBurst { get; private set; }

        private static float driveLease;
        private static float agitationLease;
        private static bool leaseAlive;

        /// <summary>驱动源每帧续租(各端本地,同帧取最大)。</summary>
        public static void Report(float drive, float agitation = 0f)
        {
            if (Main.dedServ)
                return;
            driveLease = Math.Max(driveLease, MathHelper.Clamp(drive, 0f, 1f));
            agitationLease = Math.Max(agitationLease, MathHelper.Clamp(agitation, 0f, 1f));
            leaseAlive = true;
        }

        /// <summary>一次性闪电爆发(登场揭幕、二阶段转换等瞬间拍点)。</summary>
        public static void PushBurst(int bolts)
        {
            if (Main.dedServ)
                return;
            PendingBurst = Math.Max(PendingBurst, bolts);
        }

        /// <summary>CrSky 消费爆发条数。</summary>
        internal static int ConsumeBurst()
        {
            int n = PendingBurst;
            PendingBurst = 0;
            return n;
        }

        internal static void Update()
        {
            float target = 0f;
            if (Main.LocalPlayer.Entropy().crSky > 0)
                target = LegacyDrive;

            if (leaseAlive)
            {
                target = Math.Max(target, driveLease);
                Agitation = MathHelper.Lerp(Agitation, agitationLease, 0.2f);
            }
            else
            {
                Agitation = MathHelper.Lerp(Agitation, 0f, 0.1f);
            }
            driveLease = 0f;
            agitationLease = 0f;
            leaseAlive = false;

            float step = Intensity < target ? RisePerTick : -FallPerTick;
            Intensity = MathHelper.Clamp(Math.Abs(target - Intensity) <= RisePerTick ? target : Intensity + step, 0f, 1f);
        }

        internal static void Reset()
        {
            Intensity = 0f;
            Agitation = 0f;
            PendingBurst = 0;
            driveLease = 0f;
            agitationLease = 0f;
            leaseAlive = false;
        }
    }

    /// <summary>中枢的步进与清理宿主。</summary>
    [Autoload(Side = ModSide.Client)]
    public class CruiserSkyDriveSystem : ModSystem
    {
        public override void PostUpdateEverything() => CruiserSkyDrive.Update();

        public override void ClearWorld() => CruiserSkyDrive.Reset();
    }
}
