using CalamityEntropy.Content.Menu;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Skies
{
    public class VoidVortexScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player) => Main.LocalPlayer.Entropy().VortexSky > 0;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("CalamityEntropy:VoidVortex", isActive);
        }
    }

    /// <summary>
    /// 虚空漩涡天空(基座迁移版;VortexSky 字段唯一的写入者是 Content/Events/VoidInvasion 空壳,而该事件从未被激活,实际处于休眠)。
    /// 迁移只修正确性:旧实现用 UIScaleMatrix 画天空(错误空间,随 UI 缩放漂移)且七层加法无切片门控;
    /// 现为原始像素空间 + 最远切片一次,漩涡尺寸随屏高归一。
    /// </summary>
    public class VoidVortexSky : CESkyBase
    {
        //旋转漩涡贴图,加载期由 VaultLoaden 赋值,绘制里不再逐帧 Request
        [VaultLoaden("CalamityEntropy/Assets/Extra/menu/VoidVortex")]
        private static Asset<Texture2D> vortexTex;

        //七层漩涡的缩放与角速度(沿用旧实现)
        private static readonly float[] LayerScales = { 1f, 0.8f, 0.5f, 0.3f, 0.2f, 0.15f, 0.1f };
        private static readonly float[] LayerSpeeds = { 0.1f, 0.2f, 0.3f, 0.5f, 0.7f, 1f, 2f };

        public int counter;
        public static List<MenuParticle> particles = new List<MenuParticle>();

        protected override float FadeInStep => 0.005f;
        protected override float FadeOutStep => 0.01f;

        protected override bool KeepActive() => Main.LocalPlayer.Entropy().VortexSky > 0;

        public override float GetCloudAlpha() => 1f - opacity;

        protected override void OnReset() => particles.Clear();

        protected override void UpdatePayload(GameTime gameTime)
        {
            if (opacity <= 0f)
            {
                if (particles.Count > 0)
                    particles.Clear();
                return;
            }
            counter++;
            foreach (MenuParticle p in particles)
                p.update();
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                if (particles[i].timeleft <= 0)
                    particles.RemoveAt(i);
            }
            if (counter % 15 == 0)
            {
                //更新阶段的 ScreenSize 是真实值,轨道中心与原始像素空间绘制一致
                Vector2 center = Main.ScreenSize.ToVector2() / 2f;
                MenuParticle particle = new MenuParticle(center, center, CEUtils.randomRot().ToRotationVector2() * 1, new Vector2(1.5f, 1), 660);
                particles.Add(particle);
                particle.pos += particle.velocity * 2;
            }
        }

        protected override void DrawFar(SpriteBatch spriteBatch)
        {
            Texture2D l1 = vortexTex.Value;

            //底色罩:留在调用方批次,矩形恰好铺满
            spriteBatch.Draw(CEUtils.pixelTex, CESkyDrawing.CallerFullscreen, new Color(1, 2, 32) * opacity);

            //旋转漩涡与粒子走原始像素空间(粒子坐标按真实屏幕生成)
            Rectangle vp = CESkyDrawing.ViewportFullscreen;
            Vector2 center = new Vector2(vp.Width, vp.Height) / 2f;
            //漩涡尺寸随屏高归一(旧实现固定像素尺寸,高分屏留边)
            float norm = vp.Height / 1080f;

            CESkyDrawing.BeginRawScreen(spriteBatch, BlendState.Additive, SamplerState.LinearWrap, SpriteSortMode.Deferred);
            for (int i = 0; i < LayerScales.Length; i++)
            {
                float op = 0.88f + 0.02f * i;
                spriteBatch.Draw(l1, center, null, Color.White * opacity * op, MathHelper.ToRadians(counter * LayerSpeeds[i]), l1.Size() / 2, LayerScales[i] * norm, SpriteEffects.None, 0);
            }

            CESkyDrawing.BeginRawScreen(spriteBatch, BlendState.Additive, SamplerState.LinearClamp, SpriteSortMode.Deferred);
            foreach (MenuParticle p in particles)
                p.draw(opacity);

            CESkyDrawing.RestoreCallerBatch(spriteBatch);
        }
    }
}
