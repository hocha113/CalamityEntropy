using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Core.Cooldowns
{
    /// <summary>
    /// 通用充能计量器:武器大招、蓄力技能的充能进度原语。
    /// 每玩家实例走 player.GetChargeMeter(key, max)(存于 CECooldownPlayer,随玩家存档);
    /// 每物品实例走 item.GetChargeMeter(max)(存于 CEChargeGlobalItem,随物品存档与同步)。
    /// </summary>
    public class CEChargeMeter
    {
        /// <summary>当前充能量。</summary>
        public float Charge;

        /// <summary>充满所需量。</summary>
        public float Max = 1f;

        /// <summary>是否已就绪(充满)。</summary>
        public bool Ready => Charge >= Max;

        /// <summary>充能比例 0~1。</summary>
        public float Ratio => Max > 0 ? Math.Clamp(Charge / Max, 0f, 1f) : 0f;

        public CEChargeMeter() { }

        public CEChargeMeter(float max) {
            Max = max;
        }

        /// <summary>
        /// 增加充能,自动截断到 Max。
        /// 返回值为「本次是否恰好从未就绪变为就绪」,供调用方在就绪瞬间做提示。
        /// </summary>
        public bool Gain(float amount) {
            bool wasReady = Ready;
            Charge = Math.Min(Charge + amount, Max);
            return !wasReady && Ready;
        }

        /// <summary>就绪时消耗全部充能并返回 true,否则不动并返回 false。大招释放判定用这个。</summary>
        public bool Consume() {
            if (!Ready)
                return false;
            Charge = 0f;
            return true;
        }

        /// <summary>消耗指定量充能。不足时不动并返回 false。</summary>
        public bool Consume(float amount) {
            if (Charge < amount)
                return false;
            Charge -= amount;
            return true;
        }

        /// <summary>清空充能。</summary>
        public void Reset() {
            Charge = 0f;
        }

        /// <summary>简易就绪提示音,配合 Gain 的返回值在就绪瞬间调用。</summary>
        public static void PlayReadyCue(Player player) {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/WulfrumPingReady") { Volume = 0.6f }, player.Center);
        }

        internal TagCompound Save() {
            return new TagCompound
            {
                { "charge", Charge },
                { "max", Max }
            };
        }

        internal static CEChargeMeter Load(TagCompound tag) {
            return new CEChargeMeter {
                Charge = tag.GetFloat("charge"),
                Max = tag.GetFloat("max")
            };
        }
    }

    /// <summary>每物品充能存储:惰性创建,随物品存档,联机随物品同步。</summary>
    public class CEChargeGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        /// <summary>该物品的充能计量器,未使用过充能的物品保持 null。</summary>
        public CEChargeMeter meter;

        public override GlobalItem Clone(Item from, Item to) {
            CEChargeGlobalItem clone = (CEChargeGlobalItem)base.Clone(from, to);
            if (meter != null)
                clone.meter = new CEChargeMeter(meter.Max) { Charge = meter.Charge };
            return clone;
        }

        public override void SaveData(Item item, TagCompound tag) {
            if (meter != null && meter.Charge > 0)
                tag["ceCharge"] = meter.Save();
        }

        public override void LoadData(Item item, TagCompound tag) {
            if (tag.TryGet("ceCharge", out TagCompound meterTag))
                meter = CEChargeMeter.Load(meterTag);
        }

        public override void NetSend(Item item, System.IO.BinaryWriter writer) {
            writer.Write(meter != null);
            if (meter != null) {
                writer.Write(meter.Charge);
                writer.Write(meter.Max);
            }
        }

        public override void NetReceive(Item item, System.IO.BinaryReader reader) {
            if (reader.ReadBoolean()) {
                meter ??= new CEChargeMeter();
                meter.Charge = reader.ReadSingle();
                meter.Max = reader.ReadSingle();
            }
        }
    }
}
