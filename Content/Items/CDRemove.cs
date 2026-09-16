using CalamityEntropy.Core.Cooldowns;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items
{
    public class CDRemove : ModItem
    {
        public float CooldownReduce = 0;
        public override void SetDefaults() {
            Item.maxStack = 1;
            Item.width = 44;
            Item.height = 44;
            Item.rare = ItemRarityID.Master;
            Item.useTime = Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.EverythingElse;
        }

        public override bool? UseItem(Player player) {
            player.ClearCooldowns();
            return true;
        }

        public override bool CanRightClick() {
            return true;
        }
        public override void SaveData(TagCompound tag) {
            tag["Time"] = CooldownReduce;
        }
        public override void LoadData(TagCompound tag) {
            if (tag.TryGet<float>("Time", out float t)) {
                CooldownReduce = t;
            }
        }
        public override void RightClick(Player player) {
            CooldownReduce -= 0.25f;
            if (CooldownReduce < 0) {
                CooldownReduce = 1;
            }
        }
        public override bool ConsumeItem(Player player) {
            return false;
        }
        public override void UpdateInventory(Player player) {
            player.Entropy().CooldownTimeMult -= CooldownReduce;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[R]", CooldownReduce.ToPercent().ToString());
        }
    }
}
