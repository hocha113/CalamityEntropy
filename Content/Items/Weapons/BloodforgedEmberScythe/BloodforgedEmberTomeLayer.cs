using CalamityEntropy.Assets.Register;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BloodforgedEmberScythe
{
    //挥砍时前手托着的咒书,画在前臂下面;每起一刀翻一页
    public class BloodforgedEmberTomeLayer : PlayerDrawLayer
    {
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/BloodforgedEmberScythe/BloodforgedEmberTome")]
        internal static Asset<Texture2D> TomeTex;
        public const int Frames = 5;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => drawInfo.shadow == 0f && !drawInfo.drawPlayer.dead && BloodforgedEmberHeld.FindFor(drawInfo.drawPlayer) != null;

        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ArmOverItem);

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            Player player = drawInfo.drawPlayer;
            BloodforgedEmberHeld held = BloodforgedEmberHeld.FindFor(player);
            if (held == null || held.TomeAlpha <= 0.01f) {
                return;
            }
            Texture2D tex = TomeTex.Value;
            Rectangle frame = tex.Frame(1, Frames, 0, held.TomeFrame);
            Vector2 world = player.Center + new Vector2(0f, player.gfxOffY) + (player.compositeFrontArm.rotation + MathHelper.PiOver2).ToRotationVector2() * 14f;
            Vector2 pos = (world - Main.screenPosition).Floor();
            Color color = Color.Lerp(Lighting.GetColor(world.ToTileCoordinates()), Color.White, 0.25f) * held.TomeAlpha;
            SpriteEffects fx = player.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            drawInfo.DrawDataCache.Add(new DrawData(tex, pos, frame, color, 0f, frame.Size() / 2f, 1f, fx));
            if (held.TomeFlash > 0f) {
                Texture2D glow = CEExtraAssets.Glow;
                Color flash = new Color(255, 40, 40, 0) * (0.6f * held.TomeFlash * held.TomeAlpha);
                drawInfo.DrawDataCache.Add(new DrawData(glow, pos, null, flash, 0f, glow.Size() / 2f, 48f / glow.Width, SpriteEffects.None));
            }
        }
    }
}
