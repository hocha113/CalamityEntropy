
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Events
{
    public class VoidInvasion : ModSystem
    {
        public static bool Active = false;
        public static float Progress = 0;
        public override void PostUpdateEverything()
        {
            if (Active)
            {
                Main.LocalPlayer.Entropy().VortexSky = 5;
            }
            else
            {
                Progress = 0;
            }
        }
    }
}