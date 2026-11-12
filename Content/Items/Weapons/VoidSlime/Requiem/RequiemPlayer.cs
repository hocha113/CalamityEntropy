using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    public class RequiemPlayer : ModPlayer
    {
        public const int CycleLength = 5;           //发,一轮三金两银
        public const int HealCooldownTime = 30;     //帧

        public int CycleIndex;                      //下一发在一轮中的序号,只在持有者端推进
        public int HealCooldown;                    //帧,充能弹回血冷却

        public override void PostUpdate() {
            if (HealCooldown > 0)
                HealCooldown--;
        }

        /// <summary>取当前序号;advance 为 false 时只看不推进</summary>
        public int TakeCycleSlot(bool advance) {
            int slot = CycleIndex;
            if (advance)
                CycleIndex = (CycleIndex + 1) % CycleLength;
            return slot;
        }

        public bool TryStartHealCooldown() {
            if (HealCooldown > 0)
                return false;
            HealCooldown = HealCooldownTime;
            return true;
        }
    }
}
