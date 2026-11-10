using CalamityEntropy.Content.Items;
using System;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Tiles.Furniture
{
    /// <summary>
    /// 一套家具的共用参数,十个材料系列各一个实例
    /// </summary>
    public sealed class CEFurnitureSeries
    {
        public Vector3 Light;            //灯具点亮时的光色,各分量 0-1
        public int Dust;                 //DustID
        public Color MapColor;           //箱子与梳妆台的地图色
        public bool LavaImmune;
        public bool LavaSink;            //水槽改当熔岩源
        public SoundStyle? HitSound;     //null 用物块默认音
        public Func<int> Material;       //主材料物品ID
        public Func<int> LightMaterial;  //灯具用它顶替火把,null 则用火把
        public Func<int> Station;        //合成站物块ID
        public int Divisor = 4;          //原版木料用量除以它,得主材料用量

        public static readonly CEFurnitureSeries Azafure = new() {
            Light = new Vector3(0.3f, 1f, 0.72f),
            Dust = DustID.Copper,
            MapColor = new Color(131, 81, 77),
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<AzafurePlating>(),
            LightMaterial = () => ModContent.ItemType<AzafureCircuitry>(),
            Station = () => TileID.Anvils,
            Divisor = 2,
        };

        public static readonly CEFurnitureSeries Tectonic = new() {
            Light = new Vector3(1f, 0.45f, 0.12f),
            Dust = DustID.Obsidian,
            MapColor = new Color(95, 71, 82),
            LavaImmune = true,
            LavaSink = true,
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<TectonicShard>(),
            Station = () => TileID.Hellforge,
        };

        public static readonly CEFurnitureSeries Starlit = new() {
            Light = new Vector3(0.45f, 0.95f, 1f),
            Dust = DustID.PinkCrystalShard,
            MapColor = new Color(178, 72, 80),
            Material = () => ModContent.ItemType<StarlitScaleDust>(),
            Station = () => TileID.MythrilAnvil,
        };

        public static readonly CEFurnitureSeries Nihility = new() {
            Light = new Vector3(1f, 0.55f, 0.95f),
            Dust = DustID.PurpleCrystalShard,
            MapColor = new Color(84, 66, 128),
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<NihilityFragments>(),
            Station = () => TileID.LunarCraftingStation,
        };

        public static readonly CEFurnitureSeries Chaotic = new() {
            Light = new Vector3(0.72f, 0.42f, 1f),
            Dust = DustID.Shadowflame,
            MapColor = new Color(60, 59, 71),
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<ChaoticPiece>(),
            Station = () => TileID.LunarCraftingStation,
        };

        public static readonly CEFurnitureSeries Starcluster = new() {
            Light = new Vector3(0.82f, 0.84f, 1f),
            Dust = DustID.BlueCrystalShard,
            MapColor = new Color(36, 56, 124),
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<WraithSoulEssence>(),
            Station = () => TileID.LunarCraftingStation,
        };

        public static readonly CEFurnitureSeries Sunblaze = new() {
            Light = new Vector3(1f, 0.9f, 0.55f),
            Dust = DustID.Gold,
            MapColor = new Color(224, 124, 16),
            LavaImmune = true,
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<BlazingSunlight>(),
            Station = () => TileID.LunarCraftingStation,
        };

        //系列子命名空间与材料类同名,材料类要写成 Items.DimBearing
        public static readonly CEFurnitureSeries DimBearing = new() {
            Light = new Vector3(1f, 0.2f, 0.8f),
            Dust = DustID.Iron,
            MapColor = new Color(67, 74, 80),
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<Items.DimBearing>(),
            Station = () => TileID.LunarCraftingStation,
        };

        public static readonly CEFurnitureSeries Voidforged = new() {
            Light = new Vector3(0.6f, 0.45f, 1f),
            Dust = DustID.PurpleTorch,
            MapColor = new Color(104, 22, 221),
            LavaImmune = true,
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<VoidOre>(),
            Station = () => ModContent.TileType<VoidWellTile>(),
            Divisor = 1,
        };

        public static readonly CEFurnitureSeries Wyrmtooth = new() {
            Light = new Vector3(1f, 0.68f, 0.32f),
            Dust = DustID.Bone,
            MapColor = new Color(134, 180, 240),
            HitSound = SoundID.Tink,
            Material = () => ModContent.ItemType<WyrmTooth>(),
            Station = () => ModContent.TileType<AbyssalAltarTile>(),
        };
    }
}
