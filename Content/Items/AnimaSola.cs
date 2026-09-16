using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Cooldowns;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    public class AnimaSola : ModItem
    {
        public override void SetStaticDefaults() {
        }

        public override void SetDefaults() {
            Item.width = 52;
            Item.height = 52;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = -1;
            Item.noMelee = true;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.noUseGraphic = true;

        }
        public override bool CanUseItem(Player player) {
            return !player.HasCooldown("AnimasolaCd");
        }

        public override bool? UseItem(Player player) {
            if (Main.myPlayer == player.whoAmI) {
                NPC target = null;
                float dist = 3400;
                foreach (NPC n in Main.ActiveNPCs) {
                    if (CEUtils.getDistance(n.Center, player.Center) < dist && !n.friendly && n.chaseable && n.realLife < 0 && n.Entropy().AnimaTrapped <= 0) {
                        target = n;
                        dist = CEUtils.getDistance(n.Center, player.Center);
                    }
                }
                if (target != null) {
                    Projectile.NewProjectile(player.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<AnimaChain>(), 0, 0, player.whoAmI, target.whoAmI);
                    int time = 25 * 60;
                    foreach (NPC n in Main.ActiveNPCs) {
                        if (n.IsABoss()) {
                            time = 180 * 60;
                            break;
                        }
                    }
                    player.AddCooldown("AnimasolaCd", time);
                }
            }
            return true;
        }

    }
}
