using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Buffs.VoidSlime
{
    /// <summary>
    /// 衰变:越过虚空史莱姆的限制圈或落进奇点黑洞时持续获得,每秒失去 100 点生命并禁用生命再生
    /// 扣血在 EDamageOverTimePlayer 结算,时长不随专家和大师延长
    /// </summary>
    public class CosmicDecay : DotBuff
    {
        public override int DamagePlayerPerSec => 100;
        public override int DamageEnemiesPerSec => 100;

        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}
