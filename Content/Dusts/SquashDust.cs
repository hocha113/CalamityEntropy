using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Dusts
{
    /// <summary>
    /// 自研移植自灾厄 SquashDust:随速度压扁拉伸的光球尘,行为逐式对齐灾厄源。
    /// 类贴图仅为自动加载占位,实际绘制走 PreDraw 的自有圆形贴图(见 Doc/decouple/dust-map.md)。
    /// </summary>
    public class SquashDust : ModDust
    {
        //圆形贴图改由 VaultLoaden 在加载期赋值,替代原 Load 手动 Request(服务器上保持 null,只在 PreDraw 读取)
        [VaultLoaden("CalamityEntropy/Assets/Particles/BasicCircle")]
        public static Asset<Texture2D> SolidCircle;

        [VaultLoaden("CalamityEntropy/Assets/Particles/BloomCircle")]
        public static Asset<Texture2D> BloomCircle;

        public override void OnSpawn(Dust dust) {
            dust.scale *= Main.rand.NextFloat(0.8f, 1f);
        }

        public override bool Update(Dust dust) {
            float fadeSpeed = (dust.fadeIn + 1);
            dust.rotation = dust.velocity.ToRotation() + MathHelper.PiOver2;
            dust.velocity *= 0.96f;
            if (dust.noGravity)
                dust.scale -= 0.045f * fadeSpeed;
            else {
                dust.scale -= 0.03f * fadeSpeed;
                dust.velocity.Y += Main.rand.NextFloat(0.1f, 0.35f) * fadeSpeed;
            }

            float light = MathHelper.Clamp(dust.scale * 0.8f, 0f, 1f);
            if (!dust.noLightEmittence)
                Lighting.AddLight(dust.position, dust.color.ToVector3() * light);

            if (dust.scale <= 0)
                dust.active = false;

            dust.position += dust.velocity;

            return false;
        }

        public override bool PreDraw(Dust dust) {
            Vector2 baseSize = Vector2.One;
            if (dust.customData != null && dust.customData is Vector2)
                baseSize = (Vector2)dust.customData;

            //速度越快越窄越长,customData 可传 Vector2 作基础比例
            Vector2 squash = new Vector2(Utils.Remap(dust.velocity.Length(), 2, 7, 1 * baseSize.X, 0.5f * baseSize.X), Utils.Remap(dust.velocity.Length(), 2, 7, 1 * baseSize.Y, 2.5f * baseSize.Y));

            Main.spriteBatch.Draw(BloomCircle.Value, dust.position - Main.screenPosition, null, dust.color with { A = 0 } * Utils.GetLerpValue(255, 0, dust.alpha), dust.rotation, BloomCircle.Size() * 0.5f, squash * dust.scale * 0.1f, SpriteEffects.None, 0);
            if (dust.alpha < 1)
                Main.spriteBatch.Draw(BloomCircle.Value, dust.position - Main.screenPosition, null, dust.color with { A = 0 } * 0.85f * Utils.GetLerpValue(255, 0, dust.alpha), dust.rotation, BloomCircle.Size() * 0.5f, squash * dust.scale * 0.04f, SpriteEffects.None, 0);
            if (!dust.noLight) {
                Main.spriteBatch.Draw(SolidCircle.Value, dust.position - Main.screenPosition, null, Color.Lerp(dust.color, Color.White, 0.3f) with { A = 0 } * Utils.GetLerpValue(255, 0, dust.alpha), dust.rotation, SolidCircle.Size() * 0.5f, squash * dust.scale * 0.075f, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
