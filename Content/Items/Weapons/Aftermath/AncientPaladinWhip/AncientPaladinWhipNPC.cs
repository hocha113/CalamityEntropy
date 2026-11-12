using CalamityEntropy.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.AncientPaladinWhip
{
    //圣骑士锤的触发:只在挥鞭者自己的客户端上记录和判定,与仓库鞭子标记一致
    public class AncientPaladinWhipNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        //最近一次用这把鞭子抽中它的玩家,-1为无;当时的鞭子弹幕伤害
        public int MarkOwner = -1;
        public int MarkWhipDamage;

        public void Mark(int owner, int whipDamage) {
            MarkOwner = owner;
            MarkWhipDamage = whipDamage;
        }

        //标记的有无与时长沿用仓库鞭子标记表,被别的鞭子抽中清标记时一并失效
        private static bool HasTag(NPC npc) {
            string name = ModContent.GetInstance<AncientPaladinWhip>().FullName;
            foreach (WhipTag tag in npc.GetGlobalNPC<WhipDebuffNPC>().Tags) {
                if (tag.ItemFullName == name)
                    return true;
            }
            return false;
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) {
            if (projectile.npcProj || projectile.trap || !(projectile.minion || projectile.sentry || ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type]))
                return;
            if (projectile.owner != Main.myPlayer || projectile.owner != MarkOwner || !npc.active || npc.life <= 0 || !HasTag(npc))
                return;
            if (!Main.player[projectile.owner].GetModPlayer<AncientPaladinWhipPlayer>().TryUseHammer())
                return;
            AncientPaladinWhipHammer.Spawn(projectile, npc, MarkWhipDamage);
        }
    }
}
