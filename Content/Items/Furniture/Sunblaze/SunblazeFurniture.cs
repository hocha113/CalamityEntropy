using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Sunblaze;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Sunblaze
{
    public abstract class SunblazeFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Sunblaze;
    }

    public class SunblazeBathtub : SunblazeFurniture<SunblazeBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class SunblazeBed : SunblazeFurniture<SunblazeBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class SunblazeBookcase : SunblazeFurniture<SunblazeBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class SunblazeCandelabra : SunblazeFurniture<SunblazeCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class SunblazeCandle : SunblazeFurniture<SunblazeCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class SunblazeChair : SunblazeFurniture<SunblazeChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class SunblazeChandelier : SunblazeFurniture<SunblazeChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class SunblazeChest : SunblazeFurniture<SunblazeChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class SunblazeClock : SunblazeFurniture<SunblazeClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class SunblazeDoor : SunblazeFurniture<SunblazeDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class SunblazeDresser : SunblazeFurniture<SunblazeDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class SunblazeLamp : SunblazeFurniture<SunblazeLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class SunblazeLantern : SunblazeFurniture<SunblazeLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class SunblazePiano : SunblazeFurniture<SunblazePianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class SunblazePlatform : SunblazeFurniture<SunblazePlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class SunblazeSink : SunblazeFurniture<SunblazeSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class SunblazeSofa : SunblazeFurniture<SunblazeSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class SunblazeTable : SunblazeFurniture<SunblazeTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class SunblazeWorkBench : SunblazeFurniture<SunblazeWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
