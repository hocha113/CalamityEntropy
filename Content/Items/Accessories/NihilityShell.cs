using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class NihilityShell : ModItem
    {
        public static int MaxCount = 3;
        public static float CirtDamageAddition = 0.18f;
        public static float HurtDamageReduce = 0.2f;
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = ModContent.RarityType<NihilityBlue>();
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().nihShell = true;
        }

        public override void AddRecipes() {
        }

        public static void checkDamage(Player player, NPC.HitInfo hitInfo) {
            if (hitInfo.Crit) {
                if (player.Entropy().nihShellCd <= 0) {
                    if (Main.rand.NextBool(9)) {
                        if (player.Entropy().nihShellCount < MaxCount) {
                            player.Entropy().nihShellCount++;
                            player.Entropy().nihShellCd = (10 * 60).ApplyCdDec(player);
                        }
                    }
                }
            }
        }
    }
}
