using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.DimBearing;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.DimBearing
{
    public abstract class DimBearingFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.DimBearing;
    }

    public class DimBearingBathtub : DimBearingFurniture<DimBearingBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class DimBearingBed : DimBearingFurniture<DimBearingBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class DimBearingBookcase : DimBearingFurniture<DimBearingBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class DimBearingCandelabra : DimBearingFurniture<DimBearingCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class DimBearingCandle : DimBearingFurniture<DimBearingCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class DimBearingChair : DimBearingFurniture<DimBearingChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class DimBearingChandelier : DimBearingFurniture<DimBearingChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class DimBearingChest : DimBearingFurniture<DimBearingChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class DimBearingClock : DimBearingFurniture<DimBearingClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class DimBearingDoor : DimBearingFurniture<DimBearingDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class DimBearingDresser : DimBearingFurniture<DimBearingDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class DimBearingLamp : DimBearingFurniture<DimBearingLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class DimBearingLantern : DimBearingFurniture<DimBearingLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class DimBearingPiano : DimBearingFurniture<DimBearingPianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class DimBearingPlatform : DimBearingFurniture<DimBearingPlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class DimBearingSink : DimBearingFurniture<DimBearingSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class DimBearingSofa : DimBearingFurniture<DimBearingSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class DimBearingTable : DimBearingFurniture<DimBearingTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class DimBearingWorkBench : DimBearingFurniture<DimBearingWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
