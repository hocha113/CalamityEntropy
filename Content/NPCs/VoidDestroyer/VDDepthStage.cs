using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 能按深度分层绘制的实体(本体与深度弹幕)。两个绘制口都在批次活跃(Deferred / AlphaBlend / Transform)时被调用,
    /// 内部切了着色器必须在返回前恢复同样的批次
    /// </summary>
    public interface IVDDepthDrawable
    {
        /// <summary>当前深度 Z(排序用,远者先画)</summary>
        float DepthZ { get; }
        /// <summary>远景层绘制:墙与黑幕之后、物块之前</summary>
        void DrawDepthFar(SpriteBatch spriteBatch);
        /// <summary>平面标记绘制:实心物块之后、NPC 之前。带外且在逼近的实体在这里画落点标记</summary>
        void DrawDepthMarker(SpriteBatch spriteBatch);
    }

    /// <summary>
    /// 深度分层登记表(纯本地)。实体在 DrawBehind(每个绘制帧、缓存阶段)里按 Z 分路:
    /// 远 → hide 并登记到远景表;近 → hide 并加入原版 overPlayers 缓存;平面 → 走原层。
    /// 逼近中的带外实体另登记标记表。两张表由 <see cref="VDDepthRenderHandle"/> 在对应阶段消费并清空
    /// </summary>
    public static class VDDepthStage
    {
        private static readonly List<IVDDepthDrawable> far = new();
        private static readonly List<IVDDepthDrawable> markers = new();

        public static bool HasWork => far.Count > 0 || markers.Count > 0;

        /// <summary>登记到远景层(调用方已把自己 hide 掉)</summary>
        public static void RegisterFar(IVDDepthDrawable drawable) {
            if (Main.dedServ || drawable == null) {
                return;
            }
            far.Add(drawable);
        }

        /// <summary>登记一枚平面标记</summary>
        public static void RegisterMarker(IVDDepthDrawable drawable) {
            if (Main.dedServ || drawable == null) {
                return;
            }
            markers.Add(drawable);
        }

        /// <summary>远景层:Z 大者先画,近者压在上面</summary>
        internal static void DrawFar(SpriteBatch spriteBatch) {
            if (far.Count == 0) {
                return;
            }
            far.Sort(static (a, b) => b.DepthZ.CompareTo(a.DepthZ));
            for (int i = 0; i < far.Count; i++) {
                far[i].DrawDepthFar(spriteBatch);
            }
            far.Clear();
        }

        internal static void DrawMarkers(SpriteBatch spriteBatch) {
            for (int i = 0; i < markers.Count; i++) {
                markers[i].DrawDepthMarker(spriteBatch);
            }
            markers.Clear();
        }

        internal static void Clear() {
            far.Clear();
            markers.Clear();
        }
    }

    /// <summary>
    /// 深度分层的绘制阶段宿主(InnoVault RenderHandle,不占屏幕 RT、不依赖捕获,复古光照下照常工作):
    /// <see cref="DrawBeforeTiles"/> 消费远景表(墙后物块前,与月亮领主同一层位),
    /// <see cref="DrawNPCsOverTiles"/> 消费标记表(实心物块之后、NPC 之前,落点标记压在地面上)
    /// </summary>
    internal class VDDepthRenderHandle : RenderHandle
    {
        public override int ScreenSlot => 0;
        public override bool Active => VDDepthStage.HasWork;
        public override bool RequireScreenCapture => false;

        /// <summary>批次活跃进入、活跃返回</summary>
        public override void DrawBeforeTiles(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (Main.gameMenu) {
                VDDepthStage.Clear();
                return;
            }
            VDDepthStage.DrawFar(spriteBatch);
        }

        /// <summary>批次未活跃进入,自开自收</summary>
        public override void DrawNPCsOverTiles(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (Main.gameMenu) {
                VDDepthStage.Clear();
                return;
            }
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            VDDepthStage.DrawMarkers(spriteBatch);
            spriteBatch.End();
        }

        public override void OnWorldUnload() {
            VDDepthStage.Clear();
        }
    }
}
