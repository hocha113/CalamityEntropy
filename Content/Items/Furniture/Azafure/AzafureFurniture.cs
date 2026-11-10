using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Azafure;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Azafure
{
    public abstract class AzafureFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Azafure;
    }

    public class AzafureBathtub : AzafureFurniture<AzafureBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class AzafureBed : AzafureFurniture<AzafureBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class AzafureBookcase : AzafureFurniture<AzafureBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class AzafureCandelabra : AzafureFurniture<AzafureCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class AzafureCandle : AzafureFurniture<AzafureCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class AzafureChair : AzafureFurniture<AzafureChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class AzafureChandelier : AzafureFurniture<AzafureChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class AzafureChest : AzafureFurniture<AzafureChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class AzafureClock : AzafureFurniture<AzafureClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class AzafureDoor : AzafureFurniture<AzafureDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class AzafureDresser : AzafureFurniture<AzafureDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class AzafureLamp : AzafureFurniture<AzafureLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class AzafureLantern : AzafureFurniture<AzafureLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class AzafurePiano : AzafureFurniture<AzafurePianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class AzafurePlatform : AzafureFurniture<AzafurePlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class AzafureSink : AzafureFurniture<AzafureSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class AzafureSofa : AzafureFurniture<AzafureSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class AzafureTable : AzafureFurniture<AzafureTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class AzafureWorkBench : AzafureFurniture<AzafureWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
