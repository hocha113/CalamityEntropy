using CalamityEntropy.Content.Projectiles.TwistedTwin;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class TwistedTwinBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<TwistedTwinMinion>()] > 0) {
                player.GetDamage(DamageClass.Generic) *= 0.20f;
                player.buffTime[buffIndex] = 18000;
            }
            else {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
