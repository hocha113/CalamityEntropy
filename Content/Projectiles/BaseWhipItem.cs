using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public abstract class BaseWhipItem : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);
        public virtual int TagDamage => 5;
        public virtual float TagDamageMult => 1;
        public virtual float TagCritChance => 0;
        public virtual int TagTime => 4 * 60;

        public override bool CanShoot(Player player) {
            return player.ownedProjectileCounts[Item.shoot] == 0;
        }
        public override bool MeleePrefix() {
            return true;
        }
    }
}
