using CalamityEntropy.Common;
using Terraria;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public abstract class BaseBookMinion : EBookBaseProjectile
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.penetrate = -1;
            Projectile.minion = true;
            Projectile.minionSlots = 0;
        }
        public virtual float DamageMult => 1;
        public int itemType = -1;
        /// <summary>≥0 时弹幕伤害固定为基伤×玩家加成,不吃书面板。</summary>
        public int fixedBaseDamage = -1;

        public override void AI() {
            base.AI();
            if (itemType == -1)
                itemType = Projectile.GetOwner().HeldItem.type;
            if (BookMarkLoader.GetPlayerHeldEntropyBook(Projectile.GetOwner(), out var eb)) {
                Projectile.damage = fixedBaseDamage >= 0
                    ? EBookProjectileEffect.FixedDamage(Projectile.GetOwner(), fixedBaseDamage, Projectile.DamageType)
                    : eb.CauculateProjectileDamage(DamageMult);
                Projectile.CritChance = (int)eb.GetProjectileModifer().Crit;
            }
            if (Projectile.GetOwner().HeldItem.type != itemType)
                Projectile.Kill();
        }
        public override void ApplyHoming() {
        }
    }
}