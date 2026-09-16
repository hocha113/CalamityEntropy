using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Core.Cooldowns
{
    /// <summary>
    /// 每玩家冷却容器,替代原灾厄玩家类上的 cooldowns 字典。
    /// 递减、结束回调、死亡清除、存档语义与原灾厄逐帧行为一致。
    /// 联机:冷却逻辑各端本地推进;加入同步的序列化辅助见 WriteAllCooldowns / ReceiveAllCooldowns,
    /// 发包走 CEMessageType.SyncCooldowns,接线见 CENetWork.Handle(协议见 Doc/decouple/cooldown-api.md §7)。
    /// </summary>
    public class CECooldownPlayer : ModPlayer
    {
        private const string CooldownsSaveKey = "ceCooldowns";
        private const string ChargesSaveKey = "ceCharges";

        /// <summary>该玩家的冷却字典,键为冷却字符串 ID。</summary>
        public Dictionary<string, CECooldownInstance> cooldowns;

        /// <summary>该玩家的具名充能计量器。</summary>
        public Dictionary<string, CEChargeMeter> charges;

        public override void Initialize() {
            cooldowns = new Dictionary<string, CECooldownInstance>(16);
            charges = new Dictionary<string, CEChargeMeter>();
        }

        #region 增删查
        /// <summary>
        /// 添加冷却。时长原生应用 EModPlayer.CooldownTimeMult 倍率
        /// (替代原 EModILEdit 对灾厄 AddCooldown 的钩子)。
        /// 返回创建的实例;ID 未注册时返回 null。
        /// </summary>
        public CECooldownInstance Add(string id, int duration, bool overwrite = true) {
            duration = (int)(duration * Player.Entropy().CooldownTimeMult);
            var instance = new CECooldownInstance(Player, id, duration);
            if (instance.handler == null) {
                CalamityEntropy.Instance?.Logger?.Warn($"冷却 \"{id}\" 未注册,AddCooldown 被忽略。");
                return null;
            }

            if (overwrite || !cooldowns.ContainsKey(id))
                cooldowns[id] = instance;

            return instance;
        }

        public bool Has(string id) => cooldowns.ContainsKey(id);

        public bool TryGet(string id, out CECooldownInstance instance) => cooldowns.TryGetValue(id, out instance);

        public bool Remove(string id) => cooldowns.Remove(id);

        public void Clear() => cooldowns.Clear();

        /// <summary>应显示在冷却栏上的实例列表。</summary>
        public IList<CECooldownInstance> GetDisplayed() {
            List<CECooldownInstance> result = new List<CECooldownInstance>(cooldowns.Count);
            foreach (CECooldownInstance instance in cooldowns.Values) {
                if (instance.handler.ShouldDisplay)
                    result.Add(instance);
            }
            return result;
        }

        /// <summary>取具名充能计量器,不存在则按 max 创建。已存在时同步 Max 到最新值。</summary>
        public CEChargeMeter GetCharge(string key, float max) {
            if (!charges.TryGetValue(key, out CEChargeMeter meter)) {
                meter = new CEChargeMeter(max);
                charges[key] = meter;
            }
            else {
                meter.Max = max;
            }
            return meter;
        }
        #endregion

        #region 逐帧推进
        public override void PostUpdateMiscEffects() {
            TickCooldowns();
        }

        private void TickCooldowns() {
            if (cooldowns.Count == 0)
                return;

            List<string> expired = null;
            foreach (var kv in cooldowns) {
                CECooldownInstance instance = kv.Value;
                CECooldownHandler handler = instance.handler;

                if (handler.CanTickDown)
                    --instance.timeLeft;

                // Tick 总是执行,与计时是否递减无关
                handler.Tick();

                if (instance.timeLeft < 0) {
                    handler.OnCompleted();
                    if (!Main.dedServ && handler.EndSound != null && handler.ShouldPlayEndSound)
                        SoundEngine.PlaySound(handler.EndSound.GetValueOrDefault(), Player.Center);
                    (expired ??= new List<string>()).Add(kv.Key);
                }
            }

            if (expired != null) {
                foreach (string id in expired)
                    cooldowns.Remove(id);
            }
        }

        public override void UpdateDead() {
            if (cooldowns.Count == 0)
                return;

            List<string> removed = null;
            foreach (var kv in cooldowns) {
                if (!kv.Value.handler.PersistsThroughDeath)
                    (removed ??= new List<string>()).Add(kv.Key);
            }
            if (removed != null) {
                foreach (string id in removed)
                    cooldowns.Remove(id);
            }
        }
        #endregion

        #region 存档
        public override void SaveData(TagCompound tag) {
            TagCompound cdTag = new TagCompound();
            foreach (var kv in cooldowns) {
                if (kv.Value.handler.SavedWithPlayer)
                    cdTag[kv.Key] = kv.Value.Save();
            }
            tag[CooldownsSaveKey] = cdTag;

            TagCompound chargeTag = new TagCompound();
            foreach (var kv in charges) {
                if (kv.Value.Charge > 0)
                    chargeTag[kv.Key] = kv.Value.Save();
            }
            tag[ChargesSaveKey] = chargeTag;
        }

        public override void LoadData(TagCompound tag) {
            cooldowns.Clear();
            if (tag.TryGet(CooldownsSaveKey, out TagCompound cdTag)) {
                foreach (var kv in cdTag) {
                    var instance = new CECooldownInstance(Player, kv.Key, cdTag.GetCompound(kv.Key));
                    if (instance.handler != null)
                        cooldowns[kv.Key] = instance;
                    else
                        CalamityEntropy.Instance?.Logger?.Warn($"存档中的冷却 \"{kv.Key}\" 未注册,已丢弃。");
                }
            }

            charges.Clear();
            if (tag.TryGet(ChargesSaveKey, out TagCompound chargeTag)) {
                foreach (var kv in chargeTag)
                    charges[kv.Key] = CEChargeMeter.Load(chargeTag.GetCompound(kv.Key));
            }
        }
        #endregion

        #region 加入同步序列化(发包见 CENetWork 的 SyncCooldowns 分支)
        /// <summary>把该玩家全部冷却写入流。由 CENetWork 的 SyncPlayer 路径调用后发包。</summary>
        public void WriteAllCooldowns(BinaryWriter writer) {
            writer.Write((byte)Player.whoAmI);
            writer.Write((ushort)cooldowns.Count);
            foreach (var kv in cooldowns)
                kv.Value.Write(writer);
        }

        /// <summary>从流恢复目标玩家的全部冷却。由 CENetWork.Handle 的 SyncCooldowns 分支调用。</summary>
        public static void ReceiveAllCooldowns(BinaryReader reader) {
            int whoAmI = reader.ReadByte();
            int count = reader.ReadUInt16();
            Player target = Main.player[whoAmI];
            var modPlayer = target.GetModPlayer<CECooldownPlayer>();
            modPlayer.cooldowns.Clear();
            for (int i = 0; i < count; i++) {
                CECooldownInstance instance = CECooldownInstance.Read(reader, target);
                if (instance.handler != null)
                    modPlayer.cooldowns[instance.ID] = instance;
            }
        }
        #endregion
    }
}
