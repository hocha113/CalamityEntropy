using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class LunarHeadLayer : PlayerDrawLayer
    {
        //帧动画与单张贴图在加载期就位,绘制时按下标取帧,消灭每帧字符串拼接
        [VaultLoaden("CalamityEntropy/Assets/Extra/LunarHairs/Stand", 0, 3, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] StandFrames;
        [VaultLoaden("CalamityEntropy/Assets/Extra/LunarHairs/Walk", 0, 4, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] WalkFrames;
        [VaultLoaden("CalamityEntropy/Assets/Extra/LunarHairs/Fall")]
        internal static Asset<Texture2D> FallTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/LunarHairs/Blink")]
        internal static Asset<Texture2D> BlinkTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/LuminarRing")]
        internal static Asset<Texture2D> RingTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            if (drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.head == EquipLoader.GetEquipSlot(Mod, "LuminarRing", EquipType.Head) || drawInfo.drawPlayer.head == EquipLoader.GetEquipSlot(Mod, "LunarMulse", EquipType.Head);
        }

        public override bool IsHeadLayer => true;

        public override Position GetDefaultPosition() {
            return new AfterParent(PlayerDrawLayers.Head);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D texture = StandFrames[(int)((Main.GameUpdateCount / 8) % 3)];

            if (player.velocity.Y > 0) {
                texture = FallTex.Value;
            }
            else {
                if (Math.Abs(player.velocity.X) > 0.4f) {
                    texture = WalkFrames[(int)((Main.GameUpdateCount / 4) % 4)];
                }
            }
            Vector2 headPos = drawInfo.HeadPosition(true);
            drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, drawInfo.colorArmorHead, drawInfo.drawPlayer.headRotation, new Vector2(drawInfo.playerEffect == SpriteEffects.FlipHorizontally ? texture.Width - 28 : 28, texture.Height / 2f + 3), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.cHead });

            if (Main.GameUpdateCount % 320 > 310) {
                texture = BlinkTex.Value;
                drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, drawInfo.colorArmorHead, drawInfo.drawPlayer.headRotation, new Vector2(drawInfo.playerEffect == SpriteEffects.FlipHorizontally ? texture.Width - 28 : 28, texture.Height / 2f + 3), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.cHead });
            }

            texture = RingTex.Value;
            headPos = drawInfo.HeadPosition(false);
            drawInfo.DrawDataCache.Add(new DrawData(texture, headPos, null, Color.White * (1 - drawInfo.shadow) * (float)(Math.Cos(Main.GlobalTimeWrappedHourly) * 0.15f + 0.8f), drawInfo.drawPlayer.headRotation, new Vector2(texture.Width / 2, texture.Height + 22 + (float)(Math.Cos(Main.GlobalTimeWrappedHourly) * 2)), 1, drawInfo.playerEffect) { shader = drawInfo.drawPlayer.cHead });

        }

    }
}
