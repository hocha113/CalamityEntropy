using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Content.Projectiles.Pets.Desert;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class DustyWhistle : ModItem
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<DSPet>();
            Item.buffType = ModContent.BuffType<DustyWhistleBuff>();
            Item.UseSound = null;
            Item.noUseGraphic = true;
            Item.useStyle = -1;

        }

        public override bool? UseItem(Player player) {
            if (!Main.dedServ) {
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/flute" + Main.rand.Next(1, 3).ToString()));
            }
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }
    }
}