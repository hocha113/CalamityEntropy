using CalamityEntropy.Core.Cooldowns;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;

namespace CalamityEntropy.Content.Cooldowns
{
    public class DriverCoreCooldown : CECooldownHandler
    {
        public static new string ID => "DriverCoreCooldown";
        public override bool ShouldDisplay => true;
        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.CalamityEntropy.DriverCoreCooldown");
        public override string Texture => "CalamityEntropy/Content/Cooldowns/DriverCoreCooldown";
        public override Color OutlineColor => new Color(255, 190, 190);
        public override bool CanTickDown => false;
        public override Color CooldownStartColor => Color.Red;
        public override Color CooldownEndColor => Color.Orange;
        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale) {
            base.DrawExpanded(spriteBatch, position, opacity, scale);

            float Xoffset = -5;
            int shield = Main.LocalPlayer.Entropy().DriverShield;
            if (shield > 9) {
                Xoffset = -10;
            }
            if (shield > 99) {
                Xoffset = -15;
            }
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, shield.ToString(), position + new Vector2(Xoffset, 4) * scale, Color.Orange, Color.Black, scale);
        }
        public override SoundStyle? EndSound => null;


    }
}
