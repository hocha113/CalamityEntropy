using CalamityEntropy.Core.Cooldowns;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;

namespace CalamityEntropy.Content.Cooldowns
{
    public class RatizelShieldCD : CECooldownHandler
    {
        public static new string ID => "RatizelShield";
        public override bool ShouldDisplay => true;
        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.CalamityEntropy.RatizelShield");
        public override string Texture => "CalamityEntropy/Content/Cooldowns/RatizelShield";
        public override Color OutlineColor => new Color(180, 180, 255);
        public override bool CanTickDown => false;
        public override Color CooldownStartColor => new Color(190, 190, 255) * ((Main.LocalPlayer.Entropy().VoidShield > 0) ? 1 : 0.4f);
        public override Color CooldownEndColor => new Color(190, 190, 255) * ((Main.LocalPlayer.Entropy().VoidShield > 0) ? 1 : 0.4f);
        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale) {
            base.DrawExpanded(spriteBatch, position, opacity, scale);

            float Xoffset = -5;
            int shield = Main.LocalPlayer.Entropy().RatzielShield;
            if (shield > 9) {
                Xoffset = -10;
            }
            if (shield > 99) {
                Xoffset = -15;
            }
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, shield.ToString(), position + new Vector2(Xoffset, 4) * scale, ((Main.LocalPlayer.Entropy().RatzielShield > 0) ? new Color(190, 190, 255) : new Color(255, 40, 80)), new Color(20, 20, 50), scale);
        }
        public override SoundStyle? EndSound => null;


    }
}
