using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Wyrmtooth;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Wyrmtooth
{
    public abstract class WyrmtoothFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Wyrmtooth;
    }

    public class WyrmtoothBathtub : WyrmtoothFurniture<WyrmtoothBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class WyrmtoothBed : WyrmtoothFurniture<WyrmtoothBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class WyrmtoothBookcase : WyrmtoothFurniture<WyrmtoothBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class WyrmtoothCandelabra : WyrmtoothFurniture<WyrmtoothCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class WyrmtoothCandle : WyrmtoothFurniture<WyrmtoothCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class WyrmtoothChair : WyrmtoothFurniture<WyrmtoothChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class WyrmtoothChandelier : WyrmtoothFurniture<WyrmtoothChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class WyrmtoothChest : WyrmtoothFurniture<WyrmtoothChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class WyrmtoothClock : WyrmtoothFurniture<WyrmtoothClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class WyrmtoothDoor : WyrmtoothFurniture<WyrmtoothDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class WyrmtoothDresser : WyrmtoothFurniture<WyrmtoothDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class WyrmtoothLamp : WyrmtoothFurniture<WyrmtoothLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class WyrmtoothLantern : WyrmtoothFurniture<WyrmtoothLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class WyrmtoothPiano : WyrmtoothFurniture<WyrmtoothPianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class WyrmtoothPlatform : WyrmtoothFurniture<WyrmtoothPlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class WyrmtoothSink : WyrmtoothFurniture<WyrmtoothSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class WyrmtoothSofa : WyrmtoothFurniture<WyrmtoothSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class WyrmtoothTable : WyrmtoothFurniture<WyrmtoothTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class WyrmtoothWorkBench : WyrmtoothFurniture<WyrmtoothWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
