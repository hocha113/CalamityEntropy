using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Dusts
{
    public class IcePiece1 : ModDust
    {
        public override void OnSpawn(Dust dust) {
            var r = Main.rand;
            dust.scale *= (float)r.Next(20, 160) / 100f;
            dust.velocity = new Vector2((float)(r.Next(-60, 61)) / 10, (float)(r.Next(-60, 61)) / 10);

        }

        public override bool MidUpdate(Dust dust) {
            dust.velocity.Y += 0.05f;
            return true;
        }
    }
}
