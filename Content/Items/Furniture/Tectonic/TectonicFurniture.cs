using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Tectonic;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Tectonic
{
    public abstract class TectonicFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Tectonic;
    }

    public class TectonicBathtub : TectonicFurniture<TectonicBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class TectonicBed : TectonicFurniture<TectonicBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class TectonicBookcase : TectonicFurniture<TectonicBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class TectonicCandelabra : TectonicFurniture<TectonicCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class TectonicCandle : TectonicFurniture<TectonicCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class TectonicChair : TectonicFurniture<TectonicChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class TectonicChandelier : TectonicFurniture<TectonicChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class TectonicChest : TectonicFurniture<TectonicChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class TectonicClock : TectonicFurniture<TectonicClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class TectonicDoor : TectonicFurniture<TectonicDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class TectonicDresser : TectonicFurniture<TectonicDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class TectonicLamp : TectonicFurniture<TectonicLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class TectonicLantern : TectonicFurniture<TectonicLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class TectonicPiano : TectonicFurniture<TectonicPianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class TectonicPlatform : TectonicFurniture<TectonicPlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class TectonicSink : TectonicFurniture<TectonicSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class TectonicSofa : TectonicFurniture<TectonicSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class TectonicTable : TectonicFurniture<TectonicTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class TectonicWorkBench : TectonicFurniture<TectonicWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
