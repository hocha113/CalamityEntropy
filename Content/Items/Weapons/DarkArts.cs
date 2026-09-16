using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class DarkArts : ModItem
    {
        public override void SetStaticDefaults() {
        }

        public override void SetDefaults() {
            Item.width = 26;
            Item.height = 26;
            Item.useTime = 1;
            Item.useAnimation = 1;
            Item.useStyle = -1;
            Item.damage = 460;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;

            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;

        }
        public override bool CanUseItem(Player player) {
            return !player.HasBuff(ModContent.BuffType<StealthState>());
        }

        public override bool? UseItem(Player player) {
            SoundEffect se = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/da1").Value;
            if (se != null) { se.Play(Main.soundVolume, 0, 0); }
            player.AddBuff(ModContent.BuffType<StealthState>(), 120);
            return true;
        }

    }
}
