using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Dusts
{
    /// <summary>
    /// 自研移植自灾厄 VoidDustInverted:黑底描边的虚空泛光尘(黑色外晕+彩色内核),行为逐式对齐灾厄源。
    /// 类贴图仅为自动加载占位,实际绘制走 PreDraw 的自有圆形贴图(见 Doc/decouple/dust-map.md)。
    /// </summary>
    public class VoidDustInverted : ModDust
    {
        //圆形贴图改由 VaultLoaden 在加载期赋值,替代原 Load 手动 Request(服务器上保持 null,只在 PreDraw 读取)
        [VaultLoaden("CalamityEntropy/Assets/Particles/BasicCircle")]
        public static Asset<Texture2D> SolidCircle;

        [VaultLoaden("CalamityEntropy/Assets/Particles/BloomCircle")]
        public static Asset<Texture2D> BloomCircle;

        [VaultLoaden("CalamityEntropy/Assets/Particles/SmallBloom")]
        public static Asset<Texture2D> SmallBloomCircle;

        public override void OnSpawn(Dust dust) {
            dust.scale *= Main.rand.NextFloat(0.8f, 1f);
        }

        public override bool Update(Dust dust) {
            dust.rotation += MathF.Sign(dust.velocity.X);
            dust.velocity *= 0.98f;
            if (dust.noGravity)
                dust.scale += 0.02f;
            else
                dust.scale -= 0.01f;

            float light = MathHelper.Clamp(dust.scale * 0.8f, 0f, 1f);
            if (!dust.noLightEmittence)
                Lighting.AddLight(dust.position, dust.color.ToVector3() * light);

            return true;
        }

        public override bool PreDraw(Dust dust) {
            //黑色外晕垫底,再叠彩色泛光与实心核,形成"反相虚空"观感
            Main.spriteBatch.Draw(SmallBloomCircle.Value, dust.position - Main.screenPosition, null, Color.Black * 0.4f * Utils.GetLerpValue(255, 0, dust.alpha), dust.rotation, SmallBloomCircle.Size() * 0.5f, dust.scale * 0.068f, SpriteEffects.None, 0);
            if (dust.alpha < 1)
                Main.spriteBatch.Draw(SmallBloomCircle.Value, dust.position - Main.screenPosition, null, Color.Black * Utils.GetLerpValue(255, 0, dust.alpha), dust.rotation, SmallBloomCircle.Size() * 0.5f, dust.scale * 0.057f, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(BloomCircle.Value, dust.position - Main.screenPosition, null, dust.color with { A = 0 } * Utils.GetLerpValue(255, 0, dust.alpha), dust.rotation, BloomCircle.Size() * 0.5f, dust.scale * 0.07f, SpriteEffects.None, 0);
            if (!dust.noLight)
                Main.spriteBatch.Draw(SolidCircle.Value, dust.position - Main.screenPosition, null, dust.color with { A = 0 } * 0.75f * Utils.GetLerpValue(255, 0, dust.alpha), dust.rotation, SolidCircle.Size() * 0.5f, dust.scale * 0.075f, SpriteEffects.None, 0);
            return false;
        }
    }
}
