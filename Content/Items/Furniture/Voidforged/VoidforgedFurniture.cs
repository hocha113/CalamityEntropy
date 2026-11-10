using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Voidforged;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Voidforged
{
    public abstract class VoidforgedFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Voidforged;
    }

    public class VoidforgedBathtub : VoidforgedFurniture<VoidforgedBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class VoidforgedBed : VoidforgedFurniture<VoidforgedBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class VoidforgedBookcase : VoidforgedFurniture<VoidforgedBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class VoidforgedCandelabra : VoidforgedFurniture<VoidforgedCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class VoidforgedCandle : VoidforgedFurniture<VoidforgedCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class VoidforgedChair : VoidforgedFurniture<VoidforgedChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class VoidforgedChandelier : VoidforgedFurniture<VoidforgedChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class VoidforgedChest : VoidforgedFurniture<VoidforgedChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class VoidforgedClock : VoidforgedFurniture<VoidforgedClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class VoidforgedDoor : VoidforgedFurniture<VoidforgedDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class VoidforgedDresser : VoidforgedFurniture<VoidforgedDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class VoidforgedLamp : VoidforgedFurniture<VoidforgedLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class VoidforgedLantern : VoidforgedFurniture<VoidforgedLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class VoidforgedPiano : VoidforgedFurniture<VoidforgedPianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class VoidforgedPlatform : VoidforgedFurniture<VoidforgedPlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class VoidforgedSink : VoidforgedFurniture<VoidforgedSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class VoidforgedSofa : VoidforgedFurniture<VoidforgedSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class VoidforgedTable : VoidforgedFurniture<VoidforgedTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class VoidforgedWorkBench : VoidforgedFurniture<VoidforgedWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
