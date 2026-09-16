using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Particles
{
    /// <summary>
    /// PreDraw 里每帧 Request 会把帧率吃没；MCode/Rune/Sakura 三套路由见下方 accessor
    /// </summary>
    internal static class PRTFrameTextures
    {
        private static Asset<Texture2D>[] _mcodeFrames;
        private static Asset<Texture2D>[] _runeFrames;
        private static Asset<Texture2D>[] _sakuraFrames;

        //MCodeParticle + PRT_MCodeParticle,t0~t15共16帧竖排
        internal static Texture2D MCode(int frame) {
            _mcodeFrames ??= new Asset<Texture2D>[16];
            ref Asset<Texture2D> slot = ref _mcodeFrames[frame];
            slot ??= ModContent.Request<Texture2D>($"CalamityEntropy/Assets/Extra/MALICIOUSCODE/t{frame}");   //16帧,首次才Request
            return slot.Value;
        }

        //RuneParticle + PRT_RuneParticle,Runes/r0~r13共14帧
        internal static Texture2D Rune(int frame) {
            _runeFrames ??= new Asset<Texture2D>[14];
            ref Asset<Texture2D> slot = ref _runeFrames[frame];
            slot ??= ModContent.Request<Texture2D>($"CalamityEntropy/Content/Particles/Runes/r{frame}");   //14帧r0~r13
            return slot.Value;
        }

        //SakuraPetalsParticle,横排4变体SakuraPetalsParticle0~3
        internal static Texture2D Sakura(int frame) {
            _sakuraFrames ??= new Asset<Texture2D>[4];
            ref Asset<Texture2D> slot = ref _sakuraFrames[frame];
            slot ??= ModContent.Request<Texture2D>($"CalamityEntropy/Content/Particles/SakuraPetalsParticle{frame}");   //0~3四变体
            return slot.Value;
        }
    }
}
