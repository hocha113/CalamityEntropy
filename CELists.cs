using CalamityEntropy.Content.Items.Donator.BreakStar;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace CalamityEntropy
{
    public static class CELists
    {
        public static List<string> tooltipNameUpList = new() { "zh-Hans" };
        public static List<int> GodheadBlacklist;
        /// <summary>脱离灾厄:原为灾厄时装联动表,灾厄条目全删后留空列表以保持消费方(VanityDisplay)编译。</summary>
        public static List<int> CalVanityItems;
        public static List<int> CruiserSpecificDeathProjs;
        public static List<int> CruiserSegs;
        public static void Load() {
            int P<T>() where T : ModProjectile {
                return ModContent.ProjectileType<T>();
            }
            int N<T>() where T : ModNPC {
                return ModContent.NPCType<T>();
            }
            //脱离灾厄:灾厄弹幕条目(Hellkite/GrandGuardian/MajesticGuard/GrandDad/Earth Holdout)已删除
            SoyMilkProjectileBlacklist = new()
            {
                P<RailPulseBowProjectile>(),
                P<GhostdomWhisperHoldout>(),
                P<HadopelagicEchoIIProj>(),
                P<SolarStormHeld>(),
                P<BatteringRamProj>(),
                P<CinderConvergencerHoldout>(),
                P<VoidAnnihilateCharge>(),
                P<VoidAnnihilateSpawner>(),
                P<AzafureEKatanaSlash>(),
                P<RuneSongHeld>(),
                P<AzafureImperialGuardMachineGunHeld>(),
                P<VoidshadeHeld>(),
                P<StarBreakerHeld>()
            };
            //脱离灾厄:灾厄弹幕条目(FlashBolt)已删除,ElectricLaser 为本模组自有类型
            GodheadBlacklist = new()
            {
                P<ElectricLaser>()
            };
            CalVanityItems = new();
            CruiserSpecificDeathProjs = new()
            {
                P<VoidStar>(),
                P<CruiserEnergyBall>(),
                P<VoidResidue>(),
                P<VoidSpike>(),
                P<CruiserSlash>(),
                P<CruiserLaser2>(),
                P<VoidBomb>(),
                P<VoidExplode>()
            };
            CruiserSegs = new()
            {
                N<CruiserHead>(),
                N<CruiserBody>(),
                N<CruiserTail>()
            };
        }
        public static List<int> SoyMilkProjectileBlacklist;
        public static void Unload() {
            SoyMilkProjectileBlacklist = null;
            GodheadBlacklist = null;
            CalVanityItems = null;
            CruiserSpecificDeathProjs = null;
            CruiserSegs = null;
        }
    }
}
