using CalamityEntropy.Content.NPCs.SandseaDragon;
using InnoVault;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SandseaDragon
{
    /// <summary>
    /// 沙海冥龙的敌对弹幕基类,damage 字段存普通档基值
    /// 命中时原版再乘 2 和难度倍率,这里不碰难度
    /// </summary>
    public abstract class SandseaHostileProjectile : ModProjectile
    {
        /// <summary>命中施加的减益,-1 为无,时长不随难度变</summary>
        public virtual int DebuffType => -1;
        public virtual int DebuffTime => 120;
        public virtual int DefaultTimeLeft => 300;
        /// <summary>竞技场件,转阶段清场时留着</summary>
        public virtual bool Persistent => false;

        public override void SetDefaults() {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = DefaultTimeLeft;
            CooldownSlot = ImmunityCooldownID.Bosses;
            SetExtraDefaults();
        }

        public virtual void SetExtraDefaults() {
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            if (DebuffType >= 0) {
                target.AddDebuffFixed(DebuffType, DebuffTime);
            }
        }

        /// <summary>ai 槽里存的本体下标,类型不对或已失效返回 null</summary>
        protected static SandseaDragonHead OwnerHead(float slotValue) {
            int index = (int)slotValue;
            if (index < 0 || index >= Main.maxNPCs) {
                return null;
            }
            NPC npc = Main.npc[index];
            return npc.active && npc.ModNPC is SandseaDragonHead head && head.Context != null ? head : null;
        }

        /// <summary>圆与矩形是否相交</summary>
        protected static bool CircleHits(Vector2 center, float radius, Rectangle box) {
            float x = MathHelper.Clamp(center.X, box.Left, box.Right);
            float y = MathHelper.Clamp(center.Y, box.Top, box.Bottom);
            return Vector2.DistanceSquared(center, new Vector2(x, y)) <= radius * radius;
        }

        /// <summary>只在权威端清,Kill 自带同步</summary>
        public static void ClearAll() {
            if (VaultUtils.isClient) {
                return;
            }
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile is SandseaHostileProjectile sandsea && !sandsea.Persistent) {
                    p.Kill();
                }
            }
        }
    }
}
