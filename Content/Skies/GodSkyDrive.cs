using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    /// <summary>
    /// 日月双神天幕的客户端驱动,租约模式同 VDSkyDrive:宿主每帧 Report,拍点 PushFlash,没人续租就自然衰减
    /// 纯表现,各端看自己的状态机副本,不过线
    /// </summary>
    public sealed class GodSkyDrive
    {
        public static readonly GodSkyDrive Sun = new();
        public static readonly GodSkyDrive Moon = new();

        private const float TrackPerTick = 0.02f;
        private const float FallPerTick = 0.012f;
        private const float PhaseLerp = 0.03f;
        private const float FlashDecay = 0.93f;

        /// <summary>存在强度 0..1,和天空件的淡入包络相乘成可见强度</summary>
        public float Intensity { get; private set; }
        /// <summary>平滑后的阶段 1..3</summary>
        public float Phase { get; private set; } = 1f;
        /// <summary>拍点闪光 0..1,自衰减</summary>
        public float Flash { get; private set; }
        public Vector2 BossWorldPos { get; private set; }
        /// <summary>开打第一帧的本体位置,视差按相机减它算,不用绝对世界坐标</summary>
        public Vector2 OriginWorldPos { get; private set; }
        public bool Active => Intensity > 0.004f;

        private float pendIntensity;
        private int pendPhase = 1;
        private Vector2 pendBossPos;
        private float pendFlash;
        private bool pendLease;
        private bool leaseAlive;

        public void Report(float intensity, int phase, Vector2 bossWorldPos) {
            if (Main.dedServ) {
                return;
            }
            pendIntensity = Math.Max(pendIntensity, MathHelper.Clamp(intensity, 0f, 1f));
            pendPhase = Math.Max(pendPhase, Math.Clamp(phase, 1, 3));
            pendBossPos = bossWorldPos;
            pendLease = true;
        }

        public void PushFlash(float strength) {
            if (Main.dedServ || strength <= 0f) {
                return;
            }
            pendFlash = Math.Max(pendFlash, MathHelper.Clamp(strength, 0f, 1f));
        }

        internal void Update() {
            bool wasAlive = leaseAlive;
            leaseAlive = pendLease;
            if (pendLease) {
                //新一场开打,阶段不从上一场的残值慢慢退回来
                if (!wasAlive && Intensity < 0.05f) {
                    OriginWorldPos = pendBossPos;
                    Phase = pendPhase;
                }
                Intensity = MoveToward(Intensity, pendIntensity, TrackPerTick);
                Phase = MathHelper.Lerp(Phase, pendPhase, PhaseLerp);
                BossWorldPos = pendBossPos;
            }
            else {
                Intensity = Math.Max(0f, Intensity - FallPerTick);
            }
            Flash = Math.Max(Flash * FlashDecay, pendFlash);
            if (Flash < 0.003f) {
                Flash = 0f;
            }
            pendIntensity = 0f;
            pendPhase = 1;
            pendFlash = 0f;
            pendLease = false;
        }

        internal void Reset() {
            Intensity = 0f;
            Phase = 1f;
            Flash = 0f;
            BossWorldPos = Vector2.Zero;
            OriginWorldPos = Vector2.Zero;
            pendIntensity = 0f;
            pendPhase = 1;
            pendFlash = 0f;
            pendLease = false;
            leaseAlive = false;
        }

        private static float MoveToward(float current, float target, float maxStep) {
            float diff = target - current;
            return Math.Abs(diff) <= maxStep ? target : current + Math.Sign(diff) * maxStep;
        }
    }

    /// <summary>NPC 都上报完再结算,换世界清零</summary>
    [Autoload(Side = ModSide.Client)]
    public class GodSkyDriveSystem : ModSystem
    {
        public override void PostUpdateEverything() {
            GodSkyDrive.Sun.Update();
            GodSkyDrive.Moon.Update();
        }

        public override void ClearWorld() {
            GodSkyDrive.Sun.Reset();
            GodSkyDrive.Moon.Reset();
        }
    }
}