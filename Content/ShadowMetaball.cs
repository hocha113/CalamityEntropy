using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content
{
    // 脱离灾厄:原继承灾厄 Metaball 融球框架,现自立为 ModSystem;
    // 黑色同色实心圆叠画视觉等价于融球,描边用底层放大一圈的描边色近似
    public class ShadowMetaball : ModSystem
    {
        public class ShadowParticle
        {
            public float Size;

            public Vector2 Velocity;

            public Vector2 Center;
            public ShadowParticle(Vector2 center, Vector2 velocity, float size) {
                Center = center;
                Velocity = velocity;
                Size = size;
            }

            public void Update() {
                Size *= 0.94f;
                Center += Velocity;
                Velocity *= 0.96f;
            }
        }

        public static readonly Color EdgeColor = new(255, 255, 255);

        public static List<ShadowParticle> Particles {
            get;
            private set;
        } = new();

        public static void SpawnParticle(Vector2 position, Vector2 velocity, float size) =>
            Particles.Add(new(position, velocity, size));

        public override void ClearWorld() => Particles.Clear();

        public override void Unload() => Particles = null;

        public override void PostUpdateEverything() {
            for (int i = 0; i < Particles.Count; i++)
                Particles[i].Update();
            Particles.RemoveAll(p => p.Size <= 2.5f);
        }

        public override void PostDrawTiles() {
            if (Main.dedServ || Particles.Count == 0)
                return;

            Texture2D tex = CEExtraAssets.SmallGreyscaleCircle;
            Vector2 origin = tex.Size() * 0.5f;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            // 先整体画一圈描边色,再覆盖黑色主体,簇内接缝被主体盖掉,近似融球描边
            foreach (ShadowParticle particle in Particles) {
                Vector2 drawPosition = particle.Center - Main.screenPosition;
                Vector2 scale = Vector2.One * (particle.Size + 4f) / tex.Size();
                Main.spriteBatch.Draw(tex, drawPosition, null, EdgeColor, 0f, origin, scale, 0, 0f);
            }
            foreach (ShadowParticle particle in Particles) {
                Vector2 drawPosition = particle.Center - Main.screenPosition;
                Vector2 scale = Vector2.One * particle.Size / tex.Size();
                Main.spriteBatch.Draw(tex, drawPosition, null, Color.Black, 0f, origin, scale, 0, 0f);
            }
            Main.spriteBatch.End();
        }
    }
}
