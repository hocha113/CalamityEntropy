using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles.Furniture
{
    /// <summary>
    /// 家具物块基类。派生类只给出所属系列,安放规则走 <see cref="FurnitureCommon"/>
    /// </summary>
    public abstract class CEFurnitureTile<TItem> : ModTile where TItem : ModItem
    {
        protected abstract CEFurnitureSeries Series { get; }

        protected static int DropItem => ModContent.ItemType<TItem>();

        public sealed override void SetStaticDefaults() {
            SetUpFurniture();
            DustType = Series.Dust;
            if (Series.HitSound.HasValue) {
                HitSound = Series.HitSound.Value;
            }
        }

        protected abstract void SetUpFurniture();

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public abstract class CEChairTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpChair(DropItem, Series.LavaImmune);

        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info) => FurnitureCommon.ChairSitInfo(i, j, ref info);

        public override bool RightClick(int i, int j) => FurnitureCommon.ChairRightClick(i, j);

        public override void MouseOver(int i, int j) => FurnitureCommon.ChairMouseOver(i, j, DropItem);

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) {
            return settings.player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance);
        }
    }

    public abstract class CESofaTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpSofa(DropItem, Series.LavaImmune);

        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info) => FurnitureCommon.BenchSitInfo(i, j, ref info);

        public override bool RightClick(int i, int j) => FurnitureCommon.ChairRightClick(i, j);

        public override void MouseOver(int i, int j) => FurnitureCommon.BenchMouseOver(i, j, DropItem);

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) {
            return settings.player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance);
        }
    }

    public abstract class CEBedTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpBed(DropItem, Series.LavaImmune);

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        //床的左右两半各自响应智能交互
        public override void ModifySmartInteractCoords(ref int width, ref int height, ref int frameWidth, ref int frameHeight, ref int extraY) {
            width = 2;
            height = 2;
        }

        public override bool RightClick(int i, int j) => FurnitureCommon.BedRightClick(i, j);

        public override void MouseOver(int i, int j) {
            Player player = Main.LocalPlayer;
            if (Player.IsHoveringOverABottomSideOfABed(i, j)) {
                FurnitureCommon.MouseOver(i, j, DropItem);
            }
            else if (player.IsWithinSnappngRangeToTile(i, j, PlayerSleepingHelper.BedSleepingMaxDistance)) {
                FurnitureCommon.MouseOver(i, j, ItemID.SleepingIcon);
            }
        }
    }

    public abstract class CETableTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpTable(DropItem, Series.LavaImmune);
    }

    public abstract class CEWorkBenchTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpWorkBench(DropItem, Series.LavaImmune);
    }

    public abstract class CEBookcaseTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpBookcase(DropItem, Series.LavaImmune);
    }

    public abstract class CEPianoTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpPiano(DropItem, Series.LavaImmune);
    }

    public abstract class CEBathtubTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpBathtub(DropItem, Series.LavaImmune);
    }

    public abstract class CESinkTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpSink(DropItem, Series.LavaImmune, water: !Series.LavaSink, lava: Series.LavaSink);
    }

    public abstract class CEPlatformTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpPlatform(DropItem, Series.LavaImmune);

        public override void PostSetDefaults() => Main.tileNoSunLight[Type] = false;
    }

    public abstract class CEClockTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() => this.SetUpClock(DropItem, Series.LavaImmune);

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool RightClick(int i, int j) => FurnitureCommon.ClockRightClick();

        public override void NearbyEffects(int i, int j, bool closer) {
            if (closer) {
                Main.SceneMetrics.HasClock = true;
            }
        }

        public override void MouseOver(int i, int j) => FurnitureCommon.MouseOver(i, j, DropItem);
    }

    public abstract class CEChestTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() {
            this.SetUpChest(DropItem);
            //联机客户端靠 IsAContainer 跳过通用放置同步,改由箱子自己的放置包创建
            TileID.Sets.IsAContainer[Type] = true;
            TileID.Sets.AvoidedByNPCs[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;
            TileID.Sets.FriendlyFairyCanLureTo[Type] = true;
            TileID.Sets.GeneralPlacementTiles[Type] = false;
            AddMapEntry(Series.MapColor, CEUtils.GetItemName<TItem>(), FurnitureCommon.GetMapChestName);
        }

        public override LocalizedText DefaultContainerName(int frameX, int frameY) => CEUtils.GetItemName<TItem>();

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool RightClick(int i, int j) => FurnitureCommon.ChestRightClick(i, j);

        public override void MouseOver(int i, int j) => FurnitureCommon.ChestMouseOver<TItem>(i, j);

        public override void MouseOverFar(int i, int j) => FurnitureCommon.ChestMouseFar<TItem>(i, j);

        public override void KillMultiTile(int i, int j, int frameX, int frameY) => Chest.DestroyChest(i, j);
    }

    public abstract class CEDresserTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected override void SetUpFurniture() {
            this.SetUpDresser(DropItem);
            TileID.Sets.IsAContainer[Type] = true;
            TileID.Sets.AvoidedByNPCs[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;
            AddMapEntry(Series.MapColor, CEUtils.GetItemName<TItem>(), FurnitureCommon.GetMapChestName);
        }

        public override LocalizedText DefaultContainerName(int frameX, int frameY) => CEUtils.GetItemName<TItem>();

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool RightClick(int i, int j) => FurnitureCommon.DresserRightClick();

        public override void MouseOver(int i, int j) => FurnitureCommon.DresserMouseOver<TItem>();

        public override void MouseOverFar(int i, int j) => FurnitureCommon.DresserMouseFar<TItem>();

        public override void KillMultiTile(int i, int j, int frameX, int frameY) => Chest.DestroyChest(i, j);
    }

    public abstract class CEDoorClosedTile<TItem, TOpen> : CEFurnitureTile<TItem> where TItem : ModItem where TOpen : ModTile
    {
        protected override void SetUpFurniture() {
            this.SetUpDoorClosed(DropItem, Series.LavaImmune);
            TileID.Sets.OpenDoorID[Type] = ModContent.TileType<TOpen>();
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override void MouseOver(int i, int j) => FurnitureCommon.MouseOver(i, j, DropItem);
    }

    public abstract class CEDoorOpenTile<TItem, TClosed> : CEFurnitureTile<TItem> where TItem : ModItem where TClosed : ModTile
    {
        protected override void SetUpFurniture() {
            this.SetUpDoorOpen(DropItem, Series.LavaImmune);
            TileID.Sets.CloseDoorID[Type] = ModContent.TileType<TClosed>();
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override void MouseOver(int i, int j) => FurnitureCommon.MouseOver(i, j, DropItem);
    }

    /// <summary>
    /// 灯具基类。图集左半是亮态、右半是灭态,电线信号在两者间切换
    /// </summary>
    public abstract class CELightTile<TItem> : CEFurnitureTile<TItem> where TItem : ModItem
    {
        protected abstract int Wide { get; }  //占几格宽
        protected abstract int Tall { get; }  //占几格高

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
            if (Main.tile[i, j].TileFrameX < 18 * Wide) {
                (r, g, b) = (Series.Light.X, Series.Light.Y, Series.Light.Z);
            }
        }

        public override void HitWire(int i, int j) => FurnitureCommon.LightHitWire(Type, i, j, Wide, Tall);
    }

    /// <summary>
    /// 悬挂灯具,整件交给原版的随风摆动绘制
    /// </summary>
    public abstract class CEHangingLightTile<TItem> : CELightTile<TItem> where TItem : ModItem
    {
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) {
            if (TileObjectData.IsTopLeft(Main.tile[i, j])) {
                Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.MultiTileVine);
            }
            return false;
        }
    }

    public abstract class CELampTile<TItem> : CELightTile<TItem> where TItem : ModItem
    {
        protected override int Wide => 1;
        protected override int Tall => 3;

        protected override void SetUpFurniture() => this.SetUpLamp(DropItem, Series.LavaImmune);
    }

    public abstract class CECandelabraTile<TItem> : CELightTile<TItem> where TItem : ModItem
    {
        protected override int Wide => 2;
        protected override int Tall => 2;

        protected override void SetUpFurniture() => this.SetUpCandelabra(DropItem, Series.LavaImmune);
    }

    public abstract class CECandleTile<TItem> : CELightTile<TItem> where TItem : ModItem
    {
        protected override int Wide => 1;
        protected override int Tall => 1;

        protected override void SetUpFurniture() => this.SetUpCandle(DropItem, Series.LavaImmune);

        public override void MouseOver(int i, int j) => FurnitureCommon.MouseOver(i, j, DropItem);

        public override bool RightClick(int i, int j) {
            FurnitureCommon.RightClickBreak(i, j);
            return true;
        }
    }

    public abstract class CELanternTile<TItem> : CEHangingLightTile<TItem> where TItem : ModItem
    {
        protected override int Wide => 1;
        protected override int Tall => 2;

        protected override void SetUpFurniture() => this.SetUpLantern(DropItem, Series.LavaImmune);
    }

    public abstract class CEChandelierTile<TItem> : CEHangingLightTile<TItem> where TItem : ModItem
    {
        protected override int Wide => 3;
        protected override int Tall => 3;

        protected override void SetUpFurniture() => this.SetUpChandelier(DropItem, Series.LavaImmune);
    }
}
