using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ObjectData;

namespace CalamityEntropy.Common;

internal static class FurnitureCommon
{
    public static bool BedRightClick(int i, int j) {
        Player localPlayer = Main.LocalPlayer;
        Tile tile = Main.tile[i, j];
        int num = i - tile.TileFrameX / 18 + ((tile.TileFrameX >= 72) ? 5 : 2);
        int num2 = j + 2;
        if (tile.TileFrameY % 38 != 0) {
            num2--;
        }

        if (!Player.IsHoveringOverABottomSideOfABed(i, j)) {
            if (localPlayer.IsWithinSnappngRangeToTile(i, j, 96)) {
                localPlayer.GamepadEnableGrappleCooldown();
                localPlayer.sleeping.StartSleeping(localPlayer, i, j);
            }
        }
        else {
            localPlayer.FindSpawn();
            if (localPlayer.SpawnX == num && localPlayer.SpawnY == num2) {
                localPlayer.RemoveSpawn();
                Main.NewText(Language.GetTextValue("Game.SpawnPointRemoved"), byte.MaxValue, 240, 20);
            }
            else if (Player.CheckSpawn(num, num2)) {
                localPlayer.ChangeSpawn(num, num2);
                Main.NewText(Language.GetTextValue("Game.SpawnPointSet"), byte.MaxValue, 240, 20);
            }
        }

        return true;
    }

    public static void BenchMouseOver(int i, int j, int itemID) {
        Player localPlayer = Main.LocalPlayer;
        if (localPlayer.IsWithinSnappngRangeToTile(i, j, 40)) {
            localPlayer.noThrow = 2;
            localPlayer.cursorItemIconEnabled = true;
            localPlayer.cursorItemIconID = itemID;
        }
    }

    public static void BenchSitInfo(int i, int j, ref TileRestingInfo info, int nextStyleHeight = 40) {
        Tile tileSafely = Framing.GetTileSafely(i, j);
        Player localPlayer = Main.LocalPlayer;
        info.DirectionOffset = 0;
        float x = 0f;
        if (tileSafely.TileFrameX < 17 && localPlayer.direction == 1) {
            x = 8f;
        }

        if (tileSafely.TileFrameX < 17 && localPlayer.direction == -1) {
            x = -8f;
        }

        if (tileSafely.TileFrameX > 34 && localPlayer.direction == 1) {
            x = -8f;
        }

        if (tileSafely.TileFrameX > 34 && localPlayer.direction == -1) {
            x = 8f;
        }

        info.VisualOffset = new Vector2(x, 0f);
        info.TargetDirection = localPlayer.direction;
        info.AnchorTilePosition.X = i;
        info.AnchorTilePosition.Y = j;
        if (tileSafely.TileFrameY % nextStyleHeight == 0) {
            info.AnchorTilePosition.Y++;
        }
    }

    public static void ChairMouseOver(int i, int j, int itemID, bool fat = false) {
        Player localPlayer = Main.LocalPlayer;
        if (localPlayer.IsWithinSnappngRangeToTile(i, j, 40)) {
            localPlayer.noThrow = 2;
            localPlayer.cursorItemIconEnabled = true;
            localPlayer.cursorItemIconID = itemID;
            if (fat ? (Main.tile[i, j].TileFrameX <= 35) : (Main.tile[i, j].TileFrameX / 18 < 0)) {
                localPlayer.cursorItemIconReversed = true;
            }
        }
    }

    public static bool ChairRightClick(int i, int j) {
        Player localPlayer = Main.LocalPlayer;
        if (localPlayer.IsWithinSnappngRangeToTile(i, j, 40)) {
            localPlayer.GamepadEnableGrappleCooldown();
            localPlayer.sitting.SitDown(localPlayer, i, j);
        }

        return true;
    }

    public static void ChairSitInfo(int i, int j, ref TileRestingInfo info, int nextStyleHeight = 40, bool fat = false, bool hasOffset = false, bool shitter = false) {
        if (hasOffset) {
            info.DirectionOffset = 0;
            info.VisualOffset = new Vector2(-8f, 0f);
        }

        Tile tileSafely = Framing.GetTileSafely(i, j);
        bool num = (fat ? (tileSafely.TileFrameX >= 35) : (tileSafely.TileFrameX != 0));
        if (shitter) {
            info.ExtraInfo.IsAToilet = true;
        }

        info.TargetDirection = -1;
        if (num) {
            info.TargetDirection = 1;
        }

        if (fat) {
            int num2 = tileSafely.TileFrameX / 18;
            if (num2 == 1) {
                i--;
            }

            if (num2 == 2) {
                i++;
            }
        }

        info.AnchorTilePosition.X = i;
        info.AnchorTilePosition.Y = j;
        if (tileSafely.TileFrameY % nextStyleHeight == 0) {
            info.AnchorTilePosition.Y++;
        }
    }

    public static void ChestMouseFar<T>(int i, int j) where T : ModItem {
        ChestMouseOver<T>(i, j);
        Player localPlayer = Main.LocalPlayer;
        if (localPlayer.cursorItemIconText == "") {
            localPlayer.cursorItemIconEnabled = false;
            localPlayer.cursorItemIconID = 0;
        }
    }

    public static void ChestMouseOver<T>(int i, int j) where T : ModItem {
        Player localPlayer = Main.LocalPlayer;
        Tile tile = Main.tile[i, j];
        string text = TileLoader.DefaultContainerName(tile.TileType, tile.TileFrameX, tile.TileFrameY);
        int num = i;
        int num2 = j;
        if (tile.TileFrameX % 36 != 0) {
            num--;
        }

        if (tile.TileFrameY != 0) {
            num2--;
        }

        int num3 = Chest.FindChest(num, num2);
        localPlayer.cursorItemIconID = -1;
        if (num3 < 0) {
            localPlayer.cursorItemIconText = Language.GetTextValue("LegacyChestType.0");
        }
        else {
            localPlayer.cursorItemIconText = ((Main.chest[num3].name.Length > 0) ? Main.chest[num3].name : text);
            if (localPlayer.cursorItemIconText == text) {
                localPlayer.cursorItemIconID = ModContent.ItemType<T>();
                localPlayer.cursorItemIconText = "";
            }
        }

        localPlayer.noThrow = 2;
        localPlayer.cursorItemIconEnabled = true;
    }

    public static bool ChestRightClick(int i, int j) {
        Player localPlayer = Main.LocalPlayer;
        Tile tile = Main.tile[i, j];
        Main.mouseRightRelease = false;
        int num = i;
        int num2 = j;
        if (tile.TileFrameX % 36 != 0) {
            num--;
        }

        if (tile.TileFrameY != 0) {
            num2--;
        }

        if (localPlayer.sign >= 0) {
            SoundEngine.PlaySound(in SoundID.MenuClose);
            localPlayer.sign = -1;
            Main.editSign = false;
            Main.npcChatText = "";
        }

        if (Main.editChest) {
            SoundEngine.PlaySound(in SoundID.MenuTick);
            Main.editChest = false;
            Main.npcChatText = "";
        }

        if (localPlayer.editedChestName) {
            NetMessage.SendData(33, -1, -1, NetworkText.FromLiteral(Main.chest[localPlayer.chest].name), localPlayer.chest, 1f);
            localPlayer.editedChestName = false;
        }

        if (Main.netMode == 1) {
            if (num == localPlayer.chestX && num2 == localPlayer.chestY && localPlayer.chest >= 0) {
                localPlayer.chest = -1;
                Recipe.FindRecipes();
                SoundEngine.PlaySound(in SoundID.MenuClose);
            }
            else {
                NetMessage.SendData(31, -1, -1, null, num, num2);
                Main.stackSplit = 600;
            }
        }
        else {
            int num3 = Chest.FindChest(num, num2);
            if (num3 >= 0) {
                Main.stackSplit = 600;
                if (num3 == localPlayer.chest) {
                    localPlayer.chest = -1;
                    SoundEngine.PlaySound(in SoundID.MenuClose);
                }
                else {
                    localPlayer.chest = num3;
                    Main.playerInventory = true;
                    Main.recBigList = false;
                    localPlayer.chestX = num;
                    localPlayer.chestY = num2;
                    SoundEngine.PlaySound((localPlayer.chest < 0) ? SoundID.MenuOpen : SoundID.MenuTick);
                }

                Recipe.FindRecipes();
            }
        }

        return true;
    }

    public static bool ClockRightClick() {
        string text = "AM";
        double num = Main.time;
        if (!Main.dayTime) {
            num += 54000.0;
        }

        num /= 3600.0;
        num -= 19.5;
        if (num < 0.0) {
            num += 24.0;
        }

        if (num >= 12.0) {
            text = "PM";
        }

        int num2 = (int)num;
        double num3 = num - (double)num2;
        num3 = (int)(num3 * 60.0);
        string text2 = num3.ToString();
        if (num3 < 10.0) {
            text2 = "0" + text2;
        }

        if (num2 > 12) {
            num2 -= 12;
        }

        if (num2 == 0) {
            num2 = 12;
        }

        Main.NewText("Time: " + num2 + ":" + text2 + " " + text, byte.MaxValue, 240, 20);
        return true;
    }

    public static void DresserMouseFar<T>() where T : ModItem {
        Player localPlayer = Main.LocalPlayer;
        Tile tile = Main.tile[Player.tileTargetX, Player.tileTargetY];
        string text = TileLoader.DefaultContainerName(tile.TileType, tile.TileFrameX, tile.TileFrameY);
        int tileTargetX = Player.tileTargetX;
        int num = Player.tileTargetY;
        int x = tileTargetX - tile.TileFrameX % 54 / 18;
        if (tile.TileFrameY % 36 != 0) {
            num--;
        }

        int num2 = Chest.FindChest(x, num);
        localPlayer.cursorItemIconID = -1;
        if (num2 < 0) {
            localPlayer.cursorItemIconText = Language.GetTextValue("LegacyDresserType.0");
        }
        else {
            if (Main.chest[num2].name != "") {
                localPlayer.cursorItemIconText = Main.chest[num2].name;
            }
            else {
                localPlayer.cursorItemIconText = text;
            }

            if (localPlayer.cursorItemIconText == text) {
                localPlayer.cursorItemIconID = ModContent.ItemType<T>();
                localPlayer.cursorItemIconText = "";
            }
        }

        localPlayer.noThrow = 2;
        localPlayer.cursorItemIconEnabled = true;
        if (localPlayer.cursorItemIconText == "") {
            localPlayer.cursorItemIconEnabled = false;
            localPlayer.cursorItemIconID = 0;
        }
    }

    public static void DresserMouseOver<T>() where T : ModItem {
        Player localPlayer = Main.LocalPlayer;
        Tile tile = Main.tile[Player.tileTargetX, Player.tileTargetY];
        string text = TileLoader.DefaultContainerName(tile.TileType, tile.TileFrameX, tile.TileFrameY);
        int tileTargetX = Player.tileTargetX;
        int num = Player.tileTargetY;
        int x = tileTargetX - tile.TileFrameX % 54 / 18;
        if (tile.TileFrameY % 36 != 0) {
            num--;
        }

        int num2 = Chest.FindChest(x, num);
        localPlayer.cursorItemIconID = -1;
        if (num2 < 0) {
            localPlayer.cursorItemIconText = Language.GetTextValue("LegacyDresserType.0");
        }
        else {
            if (Main.chest[num2].name != "") {
                localPlayer.cursorItemIconText = Main.chest[num2].name;
            }
            else {
                localPlayer.cursorItemIconText = text;
            }

            if (localPlayer.cursorItemIconText == text) {
                localPlayer.cursorItemIconID = ModContent.ItemType<T>();
                localPlayer.cursorItemIconText = "";
            }
        }

        localPlayer.noThrow = 2;
        localPlayer.cursorItemIconEnabled = true;
        if (Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameY > 0) {
            localPlayer.cursorItemIconID = 269;
        }
    }

    public static bool DresserRightClick() {
        Player localPlayer = Main.LocalPlayer;
        if (Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameY == 0) {
            Main.CancelClothesWindow(quiet: true);
            int num = Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX / 18;
            num %= 3;
            num = Player.tileTargetX - num;
            int num2 = Player.tileTargetY - Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameY / 18;
            if (localPlayer.sign > -1) {
                SoundEngine.PlaySound(in SoundID.MenuClose);
                localPlayer.sign = -1;
                Main.editSign = false;
                Main.npcChatText = string.Empty;
            }

            if (Main.editChest) {
                SoundEngine.PlaySound(in SoundID.MenuTick);
                Main.editChest = false;
                Main.npcChatText = string.Empty;
            }

            if (localPlayer.editedChestName) {
                NetMessage.SendData(33, -1, -1, NetworkText.FromLiteral(Main.chest[localPlayer.chest].name), localPlayer.chest, 1f);
                localPlayer.editedChestName = false;
            }

            if (Main.netMode == 1) {
                if (num == localPlayer.chestX && num2 == localPlayer.chestY && localPlayer.chest != -1) {
                    localPlayer.chest = -1;
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(in SoundID.MenuClose);
                }
                else {
                    NetMessage.SendData(31, -1, -1, null, num, num2);
                    Main.stackSplit = 600;
                }

                return true;
            }

            localPlayer.piggyBankProjTracker.Clear();
            localPlayer.voidLensChest.Clear();
            int num3 = Chest.FindChest(num, num2);
            if (num3 != -1) {
                Main.stackSplit = 600;
                if (num3 == localPlayer.chest) {
                    localPlayer.chest = -1;
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(in SoundID.MenuClose);
                }
                else if (num3 != localPlayer.chest && localPlayer.chest == -1) {
                    localPlayer.chest = num3;
                    Main.playerInventory = true;
                    Main.recBigList = false;
                    SoundEngine.PlaySound(in SoundID.MenuOpen);
                    localPlayer.chestX = num;
                    localPlayer.chestY = num2;
                }
                else {
                    localPlayer.chest = num3;
                    Main.playerInventory = true;
                    Main.recBigList = false;
                    SoundEngine.PlaySound(in SoundID.MenuTick);
                    localPlayer.chestX = num;
                    localPlayer.chestY = num2;
                }

                Recipe.FindRecipes();
                return true;
            }

            return false;
        }

        Main.playerInventory = false;
        localPlayer.chest = -1;
        Recipe.FindRecipes();
        Main.interactedDresserTopLeftX = Player.tileTargetX;
        Main.interactedDresserTopLeftY = Player.tileTargetY;
        Main.OpenClothesWindow();
        return true;
    }

    public static string GetMapChestName(string baseName, int x, int y) {
        if (!WorldGen.InWorld(x, y, 2)) {
            return baseName;
        }

        Tile tile = Main.tile[x, y];
        int num = x;
        int num2 = y;
        if (tile.TileFrameX % 36 != 0) {
            num--;
        }

        if (tile.TileFrameY != 0) {
            num2--;
        }

        int num3 = Chest.FindChest(num, num2);
        if (num3 < 0) {
            return baseName;
        }

        string text = baseName;
        if (!string.IsNullOrEmpty(Main.chest[num3].name)) {
            text = text + ": " + Main.chest[num3].name;
        }

        return text;
    }

    public static void LightHitWire(int type, int i, int j, int tileX, int tileY) {
        Tile tile = Main.tile[i, j];
        int num = i - tile.TileFrameX / 18 % tileX;
        tile = Main.tile[i, j];
        int num2 = j - tile.TileFrameY / 18 % tileY;
        int num3 = 18 * tileX;
        for (int k = num; k < num + tileX; k++) {
            for (int l = num2; l < num2 + tileY; l++) {
                tile = Main.tile[k, l];
                if (!tile.HasTile) {
                    continue;
                }

                tile = Main.tile[k, l];
                if (tile.TileType == type) {
                    tile = Main.tile[k, l];
                    if (tile.TileFrameX < num3) {
                        tile = Main.tile[k, l];
                        tile.TileFrameX += (short)num3;
                    }
                    else {
                        tile = Main.tile[k, l];
                        tile.TileFrameX -= (short)num3;
                    }
                }
            }
        }

        if (Wiring.running) {
            for (int m = 0; m < tileX; m++) {
                for (int n = 0; n < tileY; n++) {
                    Wiring.SkipWire(num + m, num2 + n);
                }
            }
        }

        if (Main.netMode != 0) {
            NetMessage.SendTileSquare(-1, num, num2, tileX, tileY);
        }
    }

    public static void LockedChestMouseOver<K, C>(int i, int j) where K : ModItem where C : ModItem {
        Player localPlayer = Main.LocalPlayer;
        Tile tile = Main.tile[i, j];
        string text = TileLoader.DefaultContainerName(tile.TileType, tile.TileFrameX, tile.TileFrameY);
        int num = i;
        int num2 = j;
        if (tile.TileFrameX % 36 != 0) {
            num--;
        }

        if (tile.TileFrameY != 0) {
            num2--;
        }

        int num3 = Chest.FindChest(num, num2);
        localPlayer.cursorItemIconID = -1;
        if (num3 < 0) {
            localPlayer.cursorItemIconText = Language.GetTextValue("LegacyChestType.0");
        }
        else {
            localPlayer.cursorItemIconText = ((Main.chest[num3].name.Length > 0) ? Main.chest[num3].name : text);
            if (localPlayer.cursorItemIconText == text) {
                localPlayer.cursorItemIconID = ModContent.ItemType<C>();
                if (Main.tile[num, num2].TileFrameX / 36 == 1) {
                    localPlayer.cursorItemIconID = ModContent.ItemType<K>();
                }

                localPlayer.cursorItemIconText = "";
            }
        }

        localPlayer.noThrow = 2;
        localPlayer.cursorItemIconEnabled = true;
    }

    public static void LockedChestMouseOverFar<K, C>(int i, int j) where K : ModItem where C : ModItem {
        LockedChestMouseOver<K, C>(i, j);
        Player localPlayer = Main.LocalPlayer;
        if (localPlayer.cursorItemIconText == "") {
            localPlayer.cursorItemIconEnabled = false;
            localPlayer.cursorItemIconID = 0;
        }
    }

    public static bool LockedChestRightClick(bool isLocked, int left, int top, int i, int j) {
        Player localPlayer = Main.LocalPlayer;
        if (localPlayer.sign >= 0) {
            SoundEngine.PlaySound(in SoundID.MenuClose);
            localPlayer.sign = -1;
            Main.editSign = false;
            Main.npcChatText = "";
        }

        if (Main.editChest) {
            SoundEngine.PlaySound(in SoundID.MenuTick);
            Main.editChest = false;
            Main.npcChatText = "";
        }

        if (localPlayer.editedChestName) {
            NetMessage.SendData(33, -1, -1, NetworkText.FromLiteral(Main.chest[localPlayer.chest].name), localPlayer.chest, 1f);
            localPlayer.editedChestName = false;
        }

        if (Main.netMode == 1 && !isLocked) {
            if (left == localPlayer.chestX && top == localPlayer.chestY && localPlayer.chest >= 0) {
                localPlayer.chest = -1;
                Recipe.FindRecipes();
                SoundEngine.PlaySound(in SoundID.MenuClose);
            }
            else {
                NetMessage.SendData(31, -1, -1, null, left, top);
                Main.stackSplit = 600;
            }

            return true;
        }

        if (isLocked) {
            if (Chest.Unlock(left, top)) {
                if (Main.netMode == 1) {
                    NetMessage.SendData(52, -1, -1, null, localPlayer.whoAmI, 1f, left, top);
                }

                return true;
            }
        }
        else {
            int num = Chest.FindChest(left, top);
            if (num >= 0) {
                Main.stackSplit = 600;
                if (num == localPlayer.chest) {
                    localPlayer.chest = -1;
                    SoundEngine.PlaySound(in SoundID.MenuClose);
                }
                else {
                    localPlayer.chest = num;
                    Main.playerInventory = true;
                    Main.recBigList = false;
                    localPlayer.chestX = left;
                    localPlayer.chestY = top;
                    SoundEngine.PlaySound((localPlayer.chest < 0) ? SoundID.MenuOpen : SoundID.MenuTick);
                }

                Recipe.FindRecipes();
                return true;
            }
        }

        return false;
    }

    public static void MouseOver(int i, int j, int itemID) {
        Player localPlayer = Main.LocalPlayer;
        localPlayer.noThrow = 2;
        localPlayer.cursorItemIconEnabled = true;
        localPlayer.cursorItemIconID = itemID;
    }

    public static void RightClickBreak(int i, int j) {
        if (Main.tile[i, j] != null && Main.tile[i, j].HasTile) {
            WorldGen.KillTile(i, j);
            if (!Main.tile[i, j].HasTile && Main.netMode != 0) {
                NetMessage.SendData(17, -1, -1, null, 0, i, j);
            }
        }
    }

    internal static void SetUp6x6Painting(this ModTile mt, bool lavaImmune = false) {
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileSpelunker[mt.Type] = true;
        Main.tileWaterDeath[mt.Type] = false;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.newTile.Width = 6;
        TileObjectData.newTile.Height = 6;
        TileObjectData.newTile.Origin = new Point16(2, 2);
        TileObjectData.newTile.CoordinateHeights = new int[6] { 16, 16, 16, 16, 16, 16 };
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
    }

    internal static void SetUpBar(this ModTile mt, int itemDropID, Color mapColor, bool lavaImmune = true) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileShine[mt.Type] = 1100;
        Main.tileSolid[mt.Type] = true;
        Main.tileSolidTop[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        mt.AddMapEntry(mapColor, Language.GetText("MapObject.MetalBar"));
    }

    internal static void SetUpBathtub(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileObjectData.newTile.Width = 4;
        TileObjectData.newTile.Height = 2;
        TileObjectData.newTile.CoordinateHeights = new int[2] { 16, 18 };
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinatePadding = 2;
        TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.Origin = new Point16(1, 1);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 4, 0);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
        TileObjectData.addAlternate(1);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        mt.AddMapEntry(new Color(144, 148, 144), Language.GetText("ItemName.Bathtub"));
    }

    internal static void SetUpBed(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileID.Sets.CanBeSleptIn[mt.Type] = true;
        TileID.Sets.InteractibleByNPCs[mt.Type] = true;
        TileID.Sets.IsValidSpawnPoint[mt.Type] = true;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.Width = 4;
        TileObjectData.newTile.Height = 2;
        TileObjectData.newTile.CoordinateHeights = new int[2] { 16, 18 };
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinatePadding = 2;
        TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.Origin = new Point16(1, 1);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 4, 0);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
        TileObjectData.addAlternate(1);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
        mt.AddMapEntry(new Color(191, 142, 111), Language.GetText("ItemName.Bed"));
        mt.AdjTiles = new int[1] { 79 };
    }

    internal static void SetUpBookcase(this ModTile mt, int itemDropID, bool lavaImmune = false, bool solidTop = true, bool autoBookcase = true) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileSolidTop[mt.Type] = solidTop;
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileTable[mt.Type] = solidTop;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        if (autoBookcase) {
            mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
            mt.AddMapEntry(new Color(191, 142, 111), Language.GetText("ItemName.Bookcase"));
            mt.AdjTiles = new int[1] { 101 };
        }
    }

    internal static void SetUpCandelabra(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.StyleLineSkip = 2;
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        mt.AddMapEntry(new Color(253, 221, 3), Language.GetText("ItemName.Candelabra"));
        mt.AdjTiles = new int[1] { 100 };
    }

    internal static void SetUpCandle(this ModTile mt, int itemDropID, bool lavaImmune = false, bool autoMapEntry = true, int offset = -4) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
        TileObjectData.newTile.CoordinateHeights = new int[1] { 20 };
        TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.DrawYOffset = offset;
        TileObjectData.newTile.StyleLineSkip = 2;
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        if (autoMapEntry) {
            mt.AddMapEntry(new Color(253, 221, 3), Language.GetText("ItemName.Candle"));
        }

        mt.AdjTiles = new int[1] { 33 };
    }

    internal static void SetUpChair(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.CanBeSatOnForNPCs[mt.Type] = true;
        TileID.Sets.CanBeSatOnForPlayers[mt.Type] = true;
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
        TileObjectData.newTile.CoordinateHeights = new int[2] { 16, 18 };
        TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.newTile.StyleWrapLimit = 2;
        TileObjectData.newTile.StyleMultiplier = 2;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
        TileObjectData.addAlternate(1);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
        mt.AddMapEntry(new Color(191, 142, 111), Language.GetText("MapObject.Chair"));
        mt.AdjTiles = new int[1] { 15 };
    }

    internal static void SetUpChandelier(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.MultiTileSway[mt.Type] = true;
        TileID.Sets.IsAMechanism[mt.Type] = true;
        TileObjectData.newTile.Width = 3;
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.CoordinateHeights = new int[3] { 16, 16, 16 };
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinatePadding = 2;
        TileObjectData.newTile.Origin = new Point16(1, 0);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.StyleLineSkip = 2;
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        mt.AddMapEntry(new Color(235, 166, 135), Language.GetText("MapObject.Chandelier"));
        mt.AdjTiles = new int[1] { 34 };
    }

    internal static void SetUpChest(this ModTile mt, int itemDropID, bool offset = false, int offsetAmt = 4) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileSpelunker[mt.Type] = true;
        Main.tileContainer[mt.Type] = true;
        Main.tileShine2[mt.Type] = true;
        Main.tileShine[mt.Type] = 1200;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileOreFinderPriority[mt.Type] = 500;
        TileID.Sets.BasicChest[mt.Type] = true;
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        if (offset) {
            TileObjectData.newTile.DrawYOffset = offsetAmt;
        }

        TileObjectData.newTile.Origin = new Point16(0, 1);
        TileObjectData.newTile.CoordinateHeights = new int[2] { 16, 18 };
        TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, processedCoordinates: true);
        TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, processedCoordinates: false);
        TileObjectData.newTile.AnchorInvalidTiles = new int[1] { 127 };
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.LavaPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.addTile(mt.Type);
        mt.AdjTiles = new int[1] { 21 };
    }

    internal static void SetUpClock(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
        TileObjectData.newTile.Height = 5;
        TileObjectData.newTile.CoordinateHeights = new int[5] { 16, 16, 16, 16, 16 };
        TileObjectData.newTile.Origin = new Point16(0, 4);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        mt.AddMapEntry(new Color(191, 142, 111), Language.GetText("ItemName.GrandfatherClock"));
        mt.AdjTiles = new int[1] { 104 };
    }

    internal static void SetUpDoorClosed(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileBlockLight[mt.Type] = true;
        Main.tileSolid[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.NotReallySolid[mt.Type] = true;
        TileID.Sets.DrawsWalls[mt.Type] = true;
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.Width = 1;
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.Origin = new Point16(0, 0);
        TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.CoordinateHeights = new int[3] { 16, 16, 16 };
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinatePadding = 2;
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Origin = new Point16(0, 1);
        TileObjectData.addAlternate(0);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Origin = new Point16(0, 2);
        TileObjectData.addAlternate(0);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
        mt.AddMapEntry(new Color(119, 105, 79), Language.GetText("MapObject.Door"));
        mt.AdjTiles = new int[1] { 10 };
    }

    internal static void SetUpDoorOpen(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileSolid[mt.Type] = false;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        Main.tileNoSunLight[mt.Type] = true;
        TileObjectData.newTile.Width = 2;
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.Origin = new Point16(0, 0);
        TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 0);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.LavaDeath = true;
        TileObjectData.newTile.CoordinateHeights = new int[3] { 16, 16, 16 };
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinatePadding = 2;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.StyleMultiplier = 2;
        TileObjectData.newTile.StyleWrapLimit = 2;
        TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Origin = new Point16(0, 1);
        TileObjectData.addAlternate(0);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Origin = new Point16(0, 2);
        TileObjectData.addAlternate(0);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Origin = new Point16(1, 0);
        TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
        TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.addAlternate(1);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Origin = new Point16(1, 1);
        TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
        TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.addAlternate(1);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Origin = new Point16(1, 2);
        TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
        TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.addAlternate(1);
        TileObjectData.addTile(mt.Type);
        TileID.Sets.HousingWalls[mt.Type] = true;
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
        mt.AddMapEntry(new Color(119, 105, 79), Language.GetText("MapObject.Door"));
        mt.AdjTiles = new int[1] { 11 };
    }

    internal static void SetUpDresser(this ModTile mt, int itemDropID) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileSolidTop[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileTable[mt.Type] = true;
        Main.tileContainer[mt.Type] = true;
        Main.tileWaterDeath[mt.Type] = false;
        Main.tileLavaDeath[mt.Type] = false;
        TileID.Sets.BasicDresser[mt.Type] = true;
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
        TileObjectData.newTile.Origin = new Point16(1, 1);
        TileObjectData.newTile.CoordinateHeights = new int[2] { 16, 16 };
        TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, processedCoordinates: true);
        TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, processedCoordinates: false);
        TileObjectData.newTile.AnchorInvalidTiles = new int[1] { 127 };
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        mt.AdjTiles = new int[1] { 88 };
    }

    internal static void SetUpFountain(this ModTile mt, int itemDropID, Color mapColor, bool lava = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = false;
        Main.tileWaterDeath[mt.Type] = false;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.LavaPlacement = LiquidPlacement.Allowed;
        TileObjectData.addTile(mt.Type);
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileObjectData.newTile.Width = 2;
        TileObjectData.newTile.Height = 4;
        TileObjectData.newTile.CoordinateHeights = new int[4] { 16, 16, 16, 16 };
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinatePadding = 2;
        TileObjectData.newTile.Origin = new Point16(0, 3);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 2, 0);
        TileObjectData.newTile.StyleLineSkip = 2;
        TileObjectData.addTile(mt.Type);
        //脱离灾厄:熔岩喷泉地图名改用自有键(hjson 键 Tiles.LavaFountain)
        mt.AddMapEntry(mapColor, lava ? CEUtils.GetText("Tiles.LavaFountain") : Language.GetText("MapObject.WaterFountain"));
        mt.AnimationFrameHeight = 72;
    }

    internal static void SetUpLamp(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1xX);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.StyleLineSkip = 2;
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        mt.AddMapEntry(new Color(253, 221, 3), Language.GetText("MapObject.FloorLamp"));
        mt.AdjTiles = new int[1] { 93 };
    }

    internal static void SetUpLantern(this ModTile mt, int itemDropID, bool lavaImmune = false, bool autoMapEntry = true) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileID.Sets.MultiTileSway[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.StyleLineSkip = 2;
        TileObjectData.newTile.DrawYOffset = -2;
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.Platform, TileObjectData.newTile.Width, 0);
        TileObjectData.newAlternate.DrawYOffset = -10;
        TileObjectData.addAlternate(0);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        if (autoMapEntry) {
            mt.AddMapEntry(new Color(251, 235, 127), Language.GetText("MapObject.Lantern"));
        }

        mt.AdjTiles = new int[1] { 42 };
    }

    internal static void SetUpPiano(this ModTile mt, int itemDropID, bool lavaImmune = false, bool solidTop = true) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileTable[mt.Type] = solidTop;
        Main.tileSolidTop[mt.Type] = solidTop;
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        mt.AddMapEntry(new Color(191, 142, 111), Language.GetText("ItemName.Piano"));
    }

    internal static void SetUpPlatform(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileSolidTop[mt.Type] = true;
        Main.tileSolid[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileTable[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        TileID.Sets.Platforms[mt.Type] = true;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.CoordinateHeights = new int[1] { 16 };
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinatePadding = 2;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.StyleMultiplier = 27;
        TileObjectData.newTile.StyleWrapLimit = 27;
        TileObjectData.newTile.UsesCustomCanPlace = false;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
        mt.AddMapEntry(new Color(191, 142, 111));
        mt.AdjTiles = new int[1] { 19 };
    }

    internal static void SetUpPylon(this ModPylon mp, TEModdedPylon pylonHook, bool lavaImmune = false, int offset = 2) {
        Main.tileLighted[mp.Type] = true;
        Main.tileFrameImportant[mp.Type] = true;
        Main.tileLavaDeath[mp.Type] = !lavaImmune;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
        TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(pylonHook.PlacementPreviewHook_CheckIfCanPlace, 1, 0, processedCoordinates: true);
        TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(pylonHook.Hook_AfterPlacement, -1, 0, processedCoordinates: false);
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.DrawYOffset = offset;
        TileObjectData.addTile(mp.Type);
        mp.AddToArray(ref TileID.Sets.CountsAsPylon);
    }

    internal static void SetUpSink(this ModTile mt, int itemDropID, bool lavaImmune = false, bool water = true, bool lava = false, bool honey = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.CountsAsWaterSource[mt.Type] = water;
        TileID.Sets.CountsAsLavaSource[mt.Type] = lava;
        TileID.Sets.CountsAsHoneySource[mt.Type] = honey;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        mt.AddMapEntry(new Color(191, 142, 111), Language.GetText("MapObject.Sink"));
        if (water) {
            mt.AdjTiles = new int[1] { 172 };
        }
    }

    internal static void SetUpSofa(this ModTile mt, int itemDropID, bool lavaImmune = false, bool bench = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.CanBeSatOnForPlayers[mt.Type] = true;
        TileID.Sets.HasOutlines[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
        mt.AddMapEntry(new Color(191, 142, 111), bench ? Language.GetText("ItemName.Bench") : Language.GetText("ItemName.Sofa"));
    }

    internal static void SetUpTable(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileSolidTop[mt.Type] = true;
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileTable[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        mt.AddMapEntry(new Color(191, 142, 111), Language.GetText("MapObject.Table"));
        mt.AdjTiles = new int[1] { 14 };
    }

    internal static void SetUpTorch(this ModTile mt, int itemDropID, bool waterImmune = false, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileLighted[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileSolid[mt.Type] = false;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileNoFail[mt.Type] = true;
        Main.tileWaterDeath[mt.Type] = !waterImmune;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileID.Sets.Torch[mt.Type] = true;
        TileID.Sets.FramesOnKillWall[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.StyleTorch);
        TileObjectData.newTile.WaterDeath = !waterImmune;
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.WaterPlacement = ((!waterImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
        TileObjectData.newAlternate.WaterDeath = !waterImmune;
        TileObjectData.newAlternate.LavaDeath = !lavaImmune;
        TileObjectData.newAlternate.WaterPlacement = ((!waterImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, TileObjectData.newTile.Height, 0);
        TileObjectData.newAlternate.AnchorAlternateTiles = new int[1] { 124 };
        TileObjectData.addAlternate(1);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
        TileObjectData.newAlternate.WaterDeath = !waterImmune;
        TileObjectData.newAlternate.LavaDeath = !lavaImmune;
        TileObjectData.newAlternate.WaterPlacement = ((!waterImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, TileObjectData.newTile.Height, 0);
        TileObjectData.newAlternate.AnchorAlternateTiles = new int[1] { 124 };
        TileObjectData.addAlternate(2);
        TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
        TileObjectData.newAlternate.WaterDeath = !waterImmune;
        TileObjectData.newAlternate.LavaDeath = !lavaImmune;
        TileObjectData.newAlternate.WaterPlacement = ((!waterImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.newAlternate.AnchorWall = true;
        TileObjectData.addAlternate(0);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
        mt.AddMapEntry(new Color(253, 221, 3), Language.GetText("ItemName.Torch"));
        mt.AdjTiles = new int[1] { 4 };
    }

    internal static void SetUpTrophy(this ModTile mt) {
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = true;
        Main.tileSpelunker[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.addTile(mt.Type);
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileID.Sets.FramesOnKillWall[mt.Type] = true;
        mt.AddMapEntry(new Color(120, 85, 60), Language.GetText("MapObject.Trophy"));
        mt.DustType = 7;
    }

    internal static void SetUpWorkBench(this ModTile mt, int itemDropID, bool lavaImmune = false) {
        mt.RegisterItemDrop(itemDropID);
        Main.tileSolidTop[mt.Type] = true;
        Main.tileFrameImportant[mt.Type] = true;
        Main.tileNoAttach[mt.Type] = true;
        Main.tileTable[mt.Type] = true;
        Main.tileLavaDeath[mt.Type] = !lavaImmune;
        Main.tileWaterDeath[mt.Type] = false;
        TileID.Sets.DisableSmartCursor[mt.Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
        TileObjectData.newTile.CoordinateHeights = new int[1] { 18 };
        TileObjectData.newTile.LavaDeath = !lavaImmune;
        TileObjectData.newTile.LavaPlacement = ((!lavaImmune) ? LiquidPlacement.NotAllowed : LiquidPlacement.Allowed);
        TileObjectData.addTile(mt.Type);
        mt.AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
        mt.AddMapEntry(new Color(191, 142, 111), Language.GetText("ItemName.WorkBench"));
        mt.AdjTiles = new int[1] { 18 };
    }
}