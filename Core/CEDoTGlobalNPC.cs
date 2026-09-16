using CalamityEntropy.Content.Buffs.PortsDoT;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core
{
    /// <summary>
    /// 移植减益（PortsDoT）的每类结算参数。
    /// 语义对齐灾厄 DebuffData：LostRegen 为 lifeRegen 扣减，2 点 = 每秒 1 点伤害。
    /// </summary>
    public class CEDoTEntry
    {
        /// <summary>lifeRegen 扣减量（2 = 每秒 1 点伤害）</summary>
        public int LostRegen;

        /// <summary>跳字下限</summary>
        public int MinTick = 1;

        /// <summary>跳字 = LostRegen × TickMult 与 MinTick 取大</summary>
        public float TickMult = 0.25f;

        /// <summary>电系：目标横向移动中 DoT ×4</summary>
        public bool ElectricMoving;

        /// <summary>风寒：目标浸湿（wet/honeyWet/dripping 或身负水系移植减益）时 ×1.5</summary>
        public bool WetBoost;

        /// <summary>放逐之焰：lifeMax ≥ 100 万时改用 lifeMax / 500</summary>
        public bool ScaleWithMaxLife;
    }

    /// <summary>
    /// PortsDoT 移植减益的集中结算：
    /// DoT 走 UpdateLifeRegen（数据驱动，注册表见 <see cref="Registry"/>），
    /// 破甲/碎甲/死亡标记走 ModifyIncomingHit，减速与钳速走 PostAI。
    /// 乘区顺序（tML 按类型 FullName 字母序执行 GlobalNPC 钩子）：
    /// Common.EDamageOverTimeNPC → Common.EGlobalNPC（全局放大负回复）→ 本类。
    /// EGlobalNPC 的全局放大跑在本类之前，覆盖不到这里的扣减，
    /// 因此本类自乘 DebuffDamageMult，且不会被二次放大。
    /// </summary>
    public class CEDoTGlobalNPC : GlobalNPC
    {
        /// <summary>buffType → 结算参数；由 PortsDoT 各 ModBuff 在 SetStaticDefaults 注册</summary>
        public static readonly Dictionary<int, CEDoTEntry> Registry = new Dictionary<int, CEDoTEntry>();

        public static void Register(int buffType, CEDoTEntry entry) {
            Registry[buffType] = entry;
        }

        public override void Unload() {
            Registry.Clear();
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage) {
            float dotMult = 0f;
            for (int i = 0; i < NPC.maxBuffs; i++) {
                if (npc.buffTime[i] <= 0 || !Registry.TryGetValue(npc.buffType[i], out var entry))
                    continue;

                // 惰性求值：只在确有移植减益时取一次倍率
                if (dotMult == 0f)
                    dotMult = npc.Entropy().DebuffDamageMult();

                int regen = entry.LostRegen;
                if (entry.ScaleWithMaxLife && npc.lifeMax >= 1000000)
                    regen = npc.lifeMax / 500;
                if (entry.ElectricMoving && npc.velocity.X != 0f)
                    regen *= 4;
                if (entry.WetBoost && IsWetTarget(npc))
                    regen = (int)(regen * 1.5f);
                if (dotMult != 1f)
                    regen = (int)(regen * dotMult);

                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;
                npc.lifeRegen -= regen;

                int tick = Math.Max((int)(regen * entry.TickMult), entry.MinTick);
                if (damage < tick)
                    damage = tick;
            }
        }

        private static bool IsWetTarget(NPC npc) {
            return npc.wet || npc.honeyWet || npc.dripping
                || npc.HasBuff<CrushDepth>() || npc.HasBuff<HadopelagicPressure>();
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) {
            if (npc.HasBuff<ArmorCrunch>())
                modifiers.Defense.Flat -= ArmorCrunch.DefenseReduction;
            if (npc.HasBuff<Crumbling>())
                modifiers.Defense.Flat -= Crumbling.DefenseReduction;
            if (npc.HasBuff<MarkedforDeath>())
                modifiers.SourceDamage *= MarkedforDeath.DamageTakenMult;
        }

        public override void PostAI(NPC npc) {
            float slow = 1f;
            if (npc.HasBuff<TemporalSadness>())
                slow += 0.2f;
            if (npc.HasBuff<GalvanicCorrosion>())
                slow += 0.05f;
            if (slow > 1f)
                npc.velocity /= slow;

            if (npc.HasBuff<VulnerabilityHex>() || npc.HasBuff<TrueVulnerabilityHex>())
                npc.velocity = Vector2.Clamp(npc.velocity, new Vector2(-5f, -5f), new Vector2(5f, 10f));
        }
    }
}
