using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Common
{
    public class CooldownShort
    {
        public int Time;
        public string ID;
        public CooldownShort(int time, string id) {
            Time = time;
            ID = id;
        }
    }
    public static class CECooldowns
    {
        public static List<CooldownShort> cooldowns = new List<CooldownShort>();
        public static int BMLightCD = 0;
        public static int BMProphecy = 0;
        public static int BMAbyss = 0;
        public static int BMSilva = 0;
        public static int BMVoid = 0;
        public static int BMAuric = 0;
        public static int MineBoxCd = 0;
        public static int BMTaurus = 0;

        /// <summary>书签触发内置CD表(effect名→剩余帧)。固定时长,不吃冷却缩减。</summary>
        public static Dictionary<string, int> BMProcCD = new Dictionary<string, int>();

        /// <summary>书签触发统一闸门:未在CD中则放行并上CD(默认60帧=1秒)。仅弹幕主人客户端调用。</summary>
        public static bool CheckBMProc(string effectName, int cd = 60) {
            if (BMProcCD.TryGetValue(effectName, out int t) && t > 0) {
                return false;
            }
            BMProcCD[effectName] = cd;
            return true;
        }
        public static void Update() {
            CountDown(ref BMLightCD);
            CountDown(ref BMProphecy);
            CountDown(ref BMAbyss);
            CountDown(ref BMSilva);
            CountDown(ref BMVoid);
            CountDown(ref BMAuric);
            CountDown(ref MineBoxCd);
            CountDown(ref BMTaurus);

            if (BMProcCD.Count > 0) {
                foreach (string key in new List<string>(BMProcCD.Keys)) {
                    if (--BMProcCD[key] <= 0)
                        BMProcCD.Remove(key);
                }
            }

            for (int i = 0; i < cooldowns.Count; i++) {
                cooldowns[i].Time--;
            }
            for (int i = cooldowns.Count - 1; i >= 0; i--) {
                if (cooldowns[i].Time <= 0) {
                    cooldowns.RemoveAt(i);
                }
            }
        }
        public static void AddCooldown(string id, int time) {
            cooldowns.Add(new CooldownShort(time.ApplyCdDec(Main.LocalPlayer), id));
        }
        public static bool HasCooldown(string id) {
            return cooldowns.Find((cd) => cd.ID == id) != null;
        }
        public static void CountDown(ref int value) {
            if (value > 0) {
                value--;
            }
        }
        public static bool CheckCD(string id, int maxValue = 60, bool reset = true) {
            var cd = cooldowns.Find((cd) => cd.ID == id);
            if (cd != null) {
                return false;
            }

            if (reset) {
                AddCooldown(id, maxValue);
            }
            return true;
        }
        public static bool CheckCD(ref int value, int maxValue = 60, bool reset = true) {
            if (value > 0) {
                return false;
            }

            if (reset) {
                value = maxValue;
            }
            return true;
        }
    }
}