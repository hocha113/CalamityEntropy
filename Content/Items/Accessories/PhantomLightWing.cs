using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Wings)]
    public class PhantomLightWing : CEBaseWings, ISpecialDrawingWing
    {
        public static float HorSpeed = 6.4f;
        public static float AccMul = 1.1f;
        public static int wTime = 160;
        public int AnimationTick => 4;
        public int FallingFrame => 2;
        public int MaxFrame => 8;
        public int SlowFallingFrame => 1;
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(wTime, HorSpeed, AccMul, false, 20, 2.8f);
        }

        public override void SetDefaults() {
            base.SetDefaults();
            Item.width = 22;
            Item.height = 20;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ModContent.RarityType<Lunarblight>();
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual) {
            base.UpdateAccessory(player, hideVisual);
            player.Entropy().addEquip("PLWing", !hideVisual);
            if (!hideVisual) {
                player.Entropy().light += 0.8f;
            }
        }
        public override void UpdateVanity(Player player) {
            base.UpdateVanity(player);
            player.Entropy().addEquipVisual("PLWing");
        }
        public override float BonusAscentWhileFalling => 0.4f;
        public override float BonusAscentWhileRising => 0.12f;
        public override float RisingSpeedThreshold => 1.2f;
        public override float MaxAscentSpeed => 2.0f;
        public override float BaseAscent => 0.125f;


    }
}
