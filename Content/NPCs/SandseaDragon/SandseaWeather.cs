using System;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.SandseaDragon
{
    /// <summary>
    /// 沙尘暴的开关,只在权威端生效,结果由原版的世界数据包带给客户端
    /// 原版沙尘暴在风速不到 0.6 时以十六倍速流逝,风速为零时当帧结束,所以开之前先把风补够
    /// </summary>
    public static class SandseaWeather
    {
        /// <summary>补到的目标风速,原版的门槛是 0.6</summary>
        private const float ForcedWind = 0.8f;
        /// <summary>原版认可沙尘暴的最低风速</summary>
        private const float SufficientWind = 0.6f;
        /// <summary>补风后至少这么多帧不让原版重摇风向</summary>
        private const int WindHoldFrames = 3600;
        /// <summary>已经在刮的沙尘暴续到的最短剩余时长,取原版随机时长的下限(8 分钟)</summary>
        private const double MinTimeLeft = 28800.0;

        public static void Start() {
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }
            if (Math.Abs(Main.windSpeedTarget) < ForcedWind) {
                float dir = Main.windSpeedCurrent != 0f ? Math.Sign(Main.windSpeedCurrent) : Main.rand.NextBool() ? 1f : -1f;
                Main.windSpeedTarget = dir * ForcedWind;
            }
            if (Math.Abs(Main.windSpeedCurrent) < SufficientWind) {
                Main.windSpeedCurrent = Math.Sign(Main.windSpeedTarget) * SufficientWind;
            }
            Main.windCounter = Math.Max(Main.windCounter, WindHoldFrames);
            Main.extremeWindCounter = Math.Max(Main.extremeWindCounter, 10);
            if (!Sandstorm.Happening) {
                //StartSandstorm 自己会发世界数据包,风速目标也在包里
                Sandstorm.StartSandstorm();
                return;
            }
            //已经在刮的只续时长,补过的风速要自己发包
            if (Sandstorm.TimeLeft < MinTimeLeft) {
                Sandstorm.TimeLeft = MinTimeLeft;
            }
            if (Main.netMode == NetmodeID.Server) {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        public static void Stop() {
            if (Main.netMode == NetmodeID.MultiplayerClient || !Sandstorm.Happening) {
                return;
            }
            Sandstorm.StopSandstorm();
        }
    }
}
