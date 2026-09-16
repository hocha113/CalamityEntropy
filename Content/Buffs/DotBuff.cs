
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public abstract class DotBuff : ModBuff
    {
        public static List<DotBuff> Instances;
        public static Dictionary<int, DotBuff> InstanceByType() {
            Dictionary<int, DotBuff> r = new Dictionary<int, DotBuff>();
            if (Instances == null)
                return r;
            foreach (var instance in Instances) {
                r[instance.Type] = instance;
            }
            return r;
        }
        public override void Load() {
            if (Instances == null)
                Instances = new List<DotBuff>();
            Instances.Add(this);
        }
        public override void Unload() {
            Instances.Clear();
        }
        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = false;
        }

        public virtual int DamagePlayerPerSec => 1;
        public virtual int DamageEnemiesPerSec => 4;
    }
}
