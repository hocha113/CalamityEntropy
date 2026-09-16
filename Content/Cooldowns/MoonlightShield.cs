using CalamityEntropy.Common;
using CalamityEntropy.Core.Cooldowns;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;

namespace CalamityEntropy.Content.Cooldowns
{
    public class MoonlightShield : CECooldownHandler
    {
        public static new string ID => "MoonlightSield";
        public override bool ShouldDisplay => true;
        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.CalamityEntropy.CdMoonlight");
        public override string Texture => "CalamityEntropy/Content/Cooldowns/MoonlightShield";
        public override Color OutlineColor => Color.LightBlue;
        public override bool CanTickDown => false;
        public override Color CooldownStartColor => Color.LightBlue;
        public override Color CooldownEndColor => Color.LightBlue;
        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale) {
            base.DrawExpanded(spriteBatch, position, opacity, scale);

            float Xoffset = -5;
            if (EModPlayer.localPlayerMaxShield - instance.timeLeft > 9) {
                Xoffset = -10;
            }
            if (EModPlayer.localPlayerMaxShield - instance.timeLeft > 99) {
                Xoffset = -15;
            }
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, (EModPlayer.localPlayerMaxShield - instance.timeLeft).ToString(), position + new Vector2(Xoffset, 4) * scale, new Color(180, 220, 255), Color.Black, scale);
        }
        public override SoundStyle? EndSound => null;


    }
}
