using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Starlit;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Starlit
{
    public abstract class StarlitFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Starlit;
    }

    public class StarlitBathtub : StarlitFurniture<StarlitBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class StarlitBed : StarlitFurniture<StarlitBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class StarlitBookcase : StarlitFurniture<StarlitBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class StarlitCandelabra : StarlitFurniture<StarlitCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class StarlitCandle : StarlitFurniture<StarlitCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class StarlitChair : StarlitFurniture<StarlitChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class StarlitChandelier : StarlitFurniture<StarlitChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class StarlitChest : StarlitFurniture<StarlitChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class StarlitClock : StarlitFurniture<StarlitClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class StarlitDoor : StarlitFurniture<StarlitDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class StarlitDresser : StarlitFurniture<StarlitDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class StarlitLamp : StarlitFurniture<StarlitLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class StarlitLantern : StarlitFurniture<StarlitLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class StarlitPiano : StarlitFurniture<StarlitPianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class StarlitPlatform : StarlitFurniture<StarlitPlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class StarlitSink : StarlitFurniture<StarlitSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class StarlitSofa : StarlitFurniture<StarlitSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class StarlitTable : StarlitFurniture<StarlitTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class StarlitWorkBench : StarlitFurniture<StarlitWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
