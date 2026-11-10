using CalamityEntropy.Content.Tiles.Furniture;
using CalamityEntropy.Content.Tiles.Furniture.Chaotic;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Furniture.Chaotic
{
    public abstract class ChaoticFurniture<TTile> : CEFurnitureItem<TTile> where TTile : ModTile
    {
        protected override CEFurnitureSeries Series => CEFurnitureSeries.Chaotic;
    }

    public class ChaoticBathtub : ChaoticFurniture<ChaoticBathtubTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bathtub;
    }

    public class ChaoticBed : ChaoticFurniture<ChaoticBedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bed;
    }

    public class ChaoticBookcase : ChaoticFurniture<ChaoticBookcaseTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Bookcase;
    }

    public class ChaoticCandelabra : ChaoticFurniture<ChaoticCandelabraTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candelabra;
    }

    public class ChaoticCandle : ChaoticFurniture<ChaoticCandleTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Candle;
    }

    public class ChaoticChair : ChaoticFurniture<ChaoticChairTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chair;
    }

    public class ChaoticChandelier : ChaoticFurniture<ChaoticChandelierTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chandelier;
    }

    public class ChaoticChest : ChaoticFurniture<ChaoticChestTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Chest;
    }

    public class ChaoticClock : ChaoticFurniture<ChaoticClockTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Clock;
    }

    public class ChaoticDoor : ChaoticFurniture<ChaoticDoorClosedTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Door;
    }

    public class ChaoticDresser : ChaoticFurniture<ChaoticDresserTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Dresser;
    }

    public class ChaoticLamp : ChaoticFurniture<ChaoticLampTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lamp;
    }

    public class ChaoticLantern : ChaoticFurniture<ChaoticLanternTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Lantern;
    }

    public class ChaoticPiano : ChaoticFurniture<ChaoticPianoTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Piano;
    }

    public class ChaoticPlatform : ChaoticFurniture<ChaoticPlatformTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Platform;
    }

    public class ChaoticSink : ChaoticFurniture<ChaoticSinkTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sink;
    }

    public class ChaoticSofa : ChaoticFurniture<ChaoticSofaTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Sofa;
    }

    public class ChaoticTable : ChaoticFurniture<ChaoticTableTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.Table;
    }

    public class ChaoticWorkBench : ChaoticFurniture<ChaoticWorkBenchTile>
    {
        protected override CEFurnitureKind Kind => CEFurnitureKind.WorkBench;
    }
}
