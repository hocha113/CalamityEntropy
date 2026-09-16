using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Items
{
    /// <summary>宝袋掉落在世界中的发光与银焰微尘效果（原生移植，替代灾厄同名扩展）。</summary>
    public static class CETreasureBagEffects
    {
        public static void TreasureBagLightAndDust(this Item item) {
            Lighting.AddLight(item.Center, Color.White.ToVector3() * 0.4f);

            if (item.timeSinceItemSpawned % 12 == 0) {
                Vector2 center = item.Center + new Vector2(0f, item.height * -0.1f);
                Vector2 direction = Main.rand.NextVector2CircularEdge(item.width * 0.6f, item.height * 0.6f);
                float distance = 0.3f + Main.rand.NextFloat() * 0.5f;
                Vector2 velocity = new Vector2(0f, -Main.rand.NextFloat() * 0.3f - 1.5f);

                Dust dust = Dust.NewDustPerfect(center + direction * distance, DustID.SilverFlame, velocity);
                dust.scale = 0.5f;
                dust.fadeIn = 1.1f;
                dust.noGravity = true;
                dust.noLight = true;
                dust.alpha = 0;
            }
        }
    }
}
