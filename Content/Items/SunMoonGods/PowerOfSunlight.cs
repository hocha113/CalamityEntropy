using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SunMoonGods
{
    /// <summary>太阳女神掉落,数值在 SunMoonPowerPlayer</summary>
    public class PowerOfSunlight : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 9999;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
            Item.consumable = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 10);
        }

        public override bool? UseItem(Player player) {
            SunMoonPowerPlayer power = player.GetModPlayer<SunMoonPowerPlayer>();
            //返回 null 不消耗
            if (power.SunlightUsed) {
                return null;
            }
            power.SunlightUsed = true;
            player.UseHealthMaxIncreasingItem(SunMoonPowerPlayer.LifeBonus);
            player.UseManaMaxIncreasingItem(SunMoonPowerPlayer.ManaBonus);
            return true;
        }
    }
}