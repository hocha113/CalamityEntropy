using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Armor;
using CalamityEntropy.Content.Rarities;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class VetrasylsEye : ModItem
    {
        public const int ShieldCooldownFrames = 3 * 60;
        public const int ShieldCooldownBossFrames = 15 * 60;
        public const int ReflectDamageCap = 1000;
        public const float ReflectDamageRatio = 0.5f;

        public static bool AnyBossAlive() {
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.boss)
                    return true;
            }
            return false;
        }

        public static int GetShieldCooldown() {
            return AnyBossAlive() ? ShieldCooldownBossFrames : ShieldCooldownFrames;
        }
        // 脱离灾厄:灾厄 IntegrateHotkey 扩展改自有键名提示
        public override void ModifyTooltips(List<TooltipLine> list) => list.Replace("[KEY]", CEKeybinds.VetrasylsEyeBlockHotKey.TooltipKeyHint());

        public override void SetDefaults() {
            Item.width = 52;
            Item.height = 52;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ModContent.RarityType<SkyBlue>();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().vetrasylsEye = true;
        }

        public override void AddRecipes() {
        }
    }
}
