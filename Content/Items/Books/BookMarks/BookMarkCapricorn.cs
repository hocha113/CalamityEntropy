using CalamityEntropy.Common;
using CalamityEntropy.Content.UI.EntropyBookUI;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkCapricorn : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.Entropy().stroke = true;
            Item.Entropy().NameColor = Color.LightBlue;
            Item.Entropy().strokeColor = Color.DarkBlue;
            Item.Entropy().tooltipStyle = 4;
            Item.value = Item.buyPrice(gold: 1);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Capricorn");
        public override Color tooltipColor => Color.LightBlue;
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.attackSpeed += (float)Math.Min(1, (float)Main.LocalPlayer.GetModPlayer<CapricornBookmarkRecordPlayer>().EBookUsingTime / 1400f) * 0.25f;
        }
    }

    public class CapricornBookmarkRecordPlayer : ModPlayer
    {
        public int EBookUsingTime = 0;
        public float SandStormCharge = 0;
        public override void PostUpdate() {
            bool isUsing = false;
            Projectile book = null;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile is EntropyBookHeldProjectile && p.owner == Player.whoAmI) {
                    book = p;
                    break;
                }
            }
            if (book != null && book.ModProjectile is EntropyBookHeldProjectile eb) {
                if (eb.active) {
                    isUsing = true;
                    EBookUsingTime++;
                }
            }
            bool sgbm = false;
            bool ssbm = false;
            if (book != null && book.ModProjectile is EntropyBookHeldProjectile ebk && Main.myPlayer == Player.whoAmI) {
                for (int i = 0; i < Math.Min(EBookUI.getMaxSlots(Main.LocalPlayer, ebk.bookItem), Player.Entropy().EBookStackItems.Count); i++) {
                    Item it = Player.Entropy().EBookStackItems[i];
                    if (BookMarkLoader.IsABookMark(it)) {
                        if (BookMarkLoader.GetEffect(it) is SandstormBMEffect && EBookUsingTime > 11) {
                            ssbm = true;
                        }
                        if (BookMarkLoader.GetEffect(it) is SnowgraveBMEffect) {
                            sgbm = true;
                        }
                    }
                }
            }
            if (!isUsing) {
                if (EBookUsingTime > 29 && book != null && book.ModProjectile is EntropyBookHeldProjectile e && Main.myPlayer == Player.whoAmI) {
                    for (int i = 0; i < Math.Min(EBookUI.getMaxSlots(Main.LocalPlayer, e.bookItem), Player.Entropy().EBookStackItems.Count); i++) {
                        Item it = Player.Entropy().EBookStackItems[i];
                        if (BookMarkLoader.IsABookMark(it)) {
                            if (BookMarkLoader.GetEffect(it) is SandstormBMEffect) {
                                BookmarkSandstorm.ShootProjectile(int.Min(EBookUsingTime, 300) / 30, Player, e);
                            }
                        }
                    }
                }
                EBookUsingTime = 0;
            }
            if (ssbm) {
                SandStormCharge = float.Min(1, EBookUsingTime / 300f);
            }
            else {
                SandStormCharge = 0;
            }
            if (!sgbm) {
                Player.Entropy().SnowgraveCharge = 0;
                Player.Entropy().SnowgraveChargeTime = 0;
            }
        }
    }
}
