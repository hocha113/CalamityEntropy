using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Nihility;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Nihility
{
    public abstract class NihilityFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Nihility;
    }

    public class NihilityBathtub : NihilityFurniture<NihilityBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class NihilityBed : NihilityFurniture<NihilityBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class NihilityBookcase : NihilityFurniture<NihilityBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class NihilityCandelabra : NihilityFurniture<NihilityCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class NihilityCandle : NihilityFurniture<NihilityCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class NihilityChair : NihilityFurniture<NihilityChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class NihilityChandelier : NihilityFurniture<NihilityChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class NihilityChest : NihilityFurniture<NihilityChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class NihilityClock : NihilityFurniture<NihilityClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class NihilityDoor : NihilityFurniture<NihilityDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class NihilityDresser : NihilityFurniture<NihilityDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class NihilityLamp : NihilityFurniture<NihilityLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class NihilityLantern : NihilityFurniture<NihilityLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class NihilityPiano : NihilityFurniture<NihilityPianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class NihilityPlatform : NihilityFurniture<NihilityPlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class NihilitySink : NihilityFurniture<NihilitySinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class NihilitySofa : NihilityFurniture<NihilitySofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class NihilityTable : NihilityFurniture<NihilityTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class NihilityWorkBench : NihilityFurniture<NihilityWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
