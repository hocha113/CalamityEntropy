using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Starcluster;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Starcluster
{
    public abstract class StarclusterFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Starcluster;
    }

    public class StarclusterBathtub : StarclusterFurniture<StarclusterBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class StarclusterBed : StarclusterFurniture<StarclusterBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class StarclusterBookcase : StarclusterFurniture<StarclusterBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class StarclusterCandelabra : StarclusterFurniture<StarclusterCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class StarclusterCandle : StarclusterFurniture<StarclusterCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class StarclusterChair : StarclusterFurniture<StarclusterChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class StarclusterChandelier : StarclusterFurniture<StarclusterChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class StarclusterChest : StarclusterFurniture<StarclusterChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class StarclusterClock : StarclusterFurniture<StarclusterClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class StarclusterDoor : StarclusterFurniture<StarclusterDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class StarclusterDresser : StarclusterFurniture<StarclusterDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class StarclusterLamp : StarclusterFurniture<StarclusterLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class StarclusterLantern : StarclusterFurniture<StarclusterLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class StarclusterPiano : StarclusterFurniture<StarclusterPianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class StarclusterPlatform : StarclusterFurniture<StarclusterPlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class StarclusterSink : StarclusterFurniture<StarclusterSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class StarclusterSofa : StarclusterFurniture<StarclusterSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class StarclusterTable : StarclusterFurniture<StarclusterTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class StarclusterWorkBench : StarclusterFurniture<StarclusterWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
