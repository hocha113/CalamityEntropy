using CalamityEntropy.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarBlessing
{
    //召唤物、哨兵打中带本鞭标记的目标时放星座;命中钩子只在弹幕主人端跑,星座做成不造成伤害的弹幕,各端都看得到
    public class StarBlessingNPC : GlobalNPC
    {
        //同一目标两次星座的最短间隔(帧),召唤物命中很密,不限会刷屏
        public const int ConstellationGap = 15;

        public override bool InstancePerEntity => true;

        private uint nextConstellation;

        private static bool HasTag(NPC npc) {
            string name = ModContent.GetInstance<StarBlessing>().FullName;
            foreach (WhipTag tag in npc.GetGlobalNPC<WhipDebuffNPC>().Tags) {
                if (tag.ItemFullName == name)
                    return true;
            }
            return false;
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) {
            if (projectile.npcProj || projectile.trap || !(projectile.minion || projectile.sentry || ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type]))
                return;
            if (projectile.owner != Main.myPlayer || Main.GameUpdateCount < nextConstellation || !HasTag(npc))
                return;
            nextConstellation = Main.GameUpdateCount + ConstellationGap;
            Vector2 at = Vector2.Clamp(projectile.Center, npc.TopLeft, npc.BottomRight);
            Projectile.NewProjectile(projectile.GetSource_FromThis(), at, Vector2.Zero, ModContent.ProjectileType<StarBlessingConstellation>(), 0, 0f, projectile.owner, Main.rand.Next(1, 1 << 20), npc.whoAmI + 1);
        }
    }
}
