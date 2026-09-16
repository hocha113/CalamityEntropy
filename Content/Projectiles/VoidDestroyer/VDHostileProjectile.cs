using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 虚空驱逐舰敌对弹幕基类:统一敌对/穿透/无碰撞设定,命中按 DebuffType 施加固定时长减益。
    /// 伤害值由 Boss 侧 ProjDamage 换算后传入,这里不再碰难度
    /// </summary>
    public abstract class VDHostileProjectile : ModProjectile, IVoidDestroyerProjectile
    {
        /// <summary>命中施加的减益,-1 为无</summary>
        public virtual int DebuffType => -1;
        public virtual int DebuffTime => 180;
        public virtual int DefaultTimeLeft => 300;

        public override void SetDefaults() {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = DefaultTimeLeft;
            SetExtraDefaults();
        }

        public virtual void SetExtraDefaults() {
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            if (DebuffType >= 0) {
                target.AddDebuffFixed(DebuffType, DebuffTime);
            }
        }

        /// <summary>ai 槽里存的玩家索引,失效时返回 null</summary>
        protected Player TargetPlayer(int slot) {
            int idx = (int)Projectile.ai[slot];
            if (idx < 0 || idx >= Main.maxPlayers) {
                return null;
            }
            Player p = Main.player[idx];
            if (!p.active || p.dead) {
                return null;
            }
            return p;
        }

        /// <summary>限定当前弹幕是否从本 Boss 出手时的常用起点:为真时才在服务端做生成类副作用</summary>
        protected static bool IsServer => Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient;
    }
}
