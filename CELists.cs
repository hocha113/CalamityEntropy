using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Donator.BreakStar;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.Fractal;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityMod.Items.Accessories.Vanity;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Projectiles.Typeless;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace CalamityEntropy
{
    public static class CELists
    {
        public static List<string> tooltipNameUpList = new() { "zh-Hans" };
        public static List<int> SpecialTaintedEnchantmentList;
        public static List<int> GodheadBlacklist;
        public static List<int> CalVanityItems;
        public static List<int> CruiserSpecificDeathProjs;
        public static List<int> CruiserSegs;
        public static void Load()
        {
            int I<T>() where T : ModItem
            {
                return ModContent.ItemType<T>();
            }
            int ItemByName(string name)
            {
                if (ModContent.GetInstance<CalamityMod.CalamityMod>().TryFind<ModItem>(name, out var mi))
                    return mi.Type;
                return -1;
            }
            int P<T>() where T : ModProjectile
            {
                return ModContent.ProjectileType<T>();
            }
            int N<T>() where T : ModNPC
            {
                return ModContent.NPCType<T>();
            }
            SpecialTaintedEnchantmentList = new()
            {
                I<ShatteredFractal>(),
                I<WelkinFractal>(),
                I<BrilliantFractal>(),
                I<AbyssFractal>(),
                I<StarlitFractal>(),
                I<ElementalFractal>(),
                I<SpiritFractal>(),
                I<VoidFractal>(),
                I<FinalFractal>(),
                I<AshesSword>(),
                I<MoonlightSword>(),
                I<TrueMoonlightSword>(),
                I<Voidshade>()
            };
            SoyMilkProjectileBlacklist = new()
            {
                P<RailPulseBowProjectile>(),
                P<GhostdomWhisperHoldout>(),
                P<HadopelagicEchoIIProj>(),
                P<SolarStormHeld>(),
                P<HellkiteHoldout>(),
                P<GrandGuardianHoldout>(),
                P<MajesticGuardHoldout>(),
                P<GrandDadHoldout>(),
                P<EarthHoldout>(),
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
            GodheadBlacklist = new()
            {
                P<ElectricLaser>(),
                P<FlashBolt>()
            };
            CalVanityItems = new()
            {
                I<GhostBracelet>(),
                I<HapuFruit>(),
                I<OracleHeadphones>(),
                ItemByName("GlimmeringRibbon"),
                I<LittleE>(),
                I<SharkyPlush>(),
                I<XyksBlessingBlue>(),
                I<XyksBlessingOrange>() 
            };
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
        public static void Unload()
        {
            SpecialTaintedEnchantmentList = null;
            SoyMilkProjectileBlacklist = null;
            GodheadBlacklist = null;
            CalVanityItems = null;
            CruiserSpecificDeathProjs = null;
            CruiserSegs = null;
        }
    }
}
