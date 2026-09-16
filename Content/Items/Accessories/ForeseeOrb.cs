using CalamityEntropy.Content.Buffs;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ForeseeOrb : ModItem
    {
        //完整/破碎两套物品贴图在加载期就位,佩戴时按 buff 状态切换,不再每帧请求
        [VaultLoaden("CalamityEntropy/Content/Items/Accessories/ForeseeOrb")]
        internal static Asset<Texture2D> OrbTex;
        [VaultLoaden("CalamityEntropy/Content/Items/Accessories/ForeseeOrbBreak")]
        internal static Asset<Texture2D> OrbBreakTex;
        public static float DMG = 0.16f;
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("{DMG}", DMG.ToPercent().ToString());
        }
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().foreseeOrbItem = Item;
            if (!player.HasBuff<ShatteredOrb>()) {
                player.GetDamage(DamageClass.Generic) += DMG;
            }
            if (player.whoAmI == Main.myPlayer) {
                if (player.HasBuff<ShatteredOrb>()) {
                    TextureAssets.Item[Type] = OrbBreakTex;
                }
                else {
                    TextureAssets.Item[Type] = OrbTex;
                }
            }
        }

    }
}
