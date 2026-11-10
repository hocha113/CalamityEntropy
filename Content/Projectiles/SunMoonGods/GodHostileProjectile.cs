using InnoVault;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods
{
    /// <summary>弹幕归属,转阶段和死亡时按它清场</summary>
    public enum GodKind : byte
    {
        Sun,
        Moon,
    }

    /// <summary>
    /// 日月双神的敌对弹幕基类,damage 字段存普通档基值
    /// 命中时原版再乘 2 和难度倍率,这里不碰难度
    /// </summary>
    public abstract class GodHostileProjectile : ModProjectile
    {
        public abstract GodKind Kind { get; }
        /// <summary>命中施加的减益,-1 为无</summary>
        public virtual int DebuffType => -1;
        public virtual int DebuffTime => 120;
        public virtual int DefaultTimeLeft => 300;

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

        /// <summary>ai 槽里存的 NPC 下标,类型不对或已失效返回 null</summary>
        protected static NPC OwnerNpc<T>(float slotValue) where T : ModNPC {
            int index = (int)slotValue;
            if (index < 0 || index >= Main.maxNPCs) {
                return null;
            }
            NPC npc = Main.npc[index];
            return npc.active && npc.ModNPC is T ? npc : null;
        }

        /// <summary>只在权威端清,Kill 自带同步</summary>
        public static void ClearAll(GodKind kind) {
            if (VaultUtils.isClient) {
                return;
            }
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile is GodHostileProjectile god && god.Kind == kind) {
                    p.Kill();
                }
            }
        }

        /// <summary>存活的本 Boss 敌对弹幕数,连接段的杂波阀读它</summary>
        public static int CountAlive(GodKind kind) {
            int count = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.hostile && p.ModProjectile is GodHostileProjectile god && god.Kind == kind) {
                    count++;
                }
            }
            return count;
        }
    }
}