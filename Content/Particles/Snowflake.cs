using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Particles
{
    public class Snowflake : EParticle
    {
        public override Texture2D Texture => ModContent.Request<Texture2D>("CalamityEntropy/Content/Particles/Snowflake").Value;
        public override void OnSpawn()
        {
            this.Lifetime = 36;
        }
        public override void AI()
        {
            base.AI();
            this.Opacity = float.Min(this.Lifetime / 6f, 1f);
            this.Rotation += this.Velocity.X * 0.01f;
            this.Velocity *= 0.99f;
        }
    }
}
