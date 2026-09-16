using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Buffs
{
    // 虚空之火:玩家每秒 20 点并禁用生命再生(EDamageOverTimePlayer 归零正回复),敌怪每秒 200 点。
    // 时长不随专家/大师延长,施加方统一走 CEUtils.AddDebuffFixed
    public class VoidFire : DotBuff
    {
        public override int DamagePlayerPerSec => 20;
        public override int DamageEnemiesPerSec => 200;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }
    }
}
