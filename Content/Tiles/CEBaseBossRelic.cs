using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityEntropy.Content.Tiles
{
    // 自有 Boss 遗物基类(脱离灾厄:替代灾厄 BaseBossRelic,行为等价;托座贴图自制)
    public abstract class CEBaseBossRelic : ModTile
    {
        public const int FrameWidth = 18 * 3;
        public const int FrameHeight = 18 * 4;
        public const int HorizontalFrames = 1;
        public const int VerticalFrames = 1;

        public Asset<Texture2D> RelicTexture;

        // 托座上方悬浮部分的贴图路径。各 Boss 尺寸不一,按 frame 中心对齐,不要求统一画布
        public abstract string RelicTextureName { get; }

        public abstract int AssociatedItem { get; }

        // 所有遗物共用同一张托座贴图。这张只是加载期的占位,实际绘制在 PreDraw 里换成原版的托座图
        public override string Texture => "CalamityEntropy/Content/Tiles/RelicPedestal";

        public override void Load()
        {
            if (!Main.dedServ)
            {
                RelicTexture = ModContent.Request<Texture2D>(RelicTextureName);
            }
        }

        public override void Unload()
        {
            RelicTexture = null;
            pedestalTextureBorrowed = false;
        }

        public override void SetStaticDefaults()
        {
            RegisterItemDrop(AssociatedItem);

            Main.tileShine[Type] = 400;
            Main.tileFrameImportant[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.newTile.StyleHorizontal = false;

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
            TileObjectData.addAlternate(1);

            TileObjectData.addTile(Type);

            AddMapEntry(new Color(233, 207, 94), Language.GetText("MapObject.Relic"));
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            return false;
        }

        private bool pedestalTextureBorrowed;

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // 脱灾时自制的 RelicPedestal.png 只是块占位色板,与原版大师遗物底座对不上。
            // 原版 617 的托座图幅同样是 54x144、同样的 18px 格距,直接把本物块的贴图槽指向它,
            // 绘制仍由引擎按本类的 TileObjectData 走,不需要手写几何。只换一次。
            if (!pedestalTextureBorrowed)
            {
                Main.instance.LoadTiles(TileID.MasterTrophyBase);
                Asset<Texture2D> vanillaPedestal = TextureAssets.Tile[TileID.MasterTrophyBase];
                if (vanillaPedestal != null && vanillaPedestal.IsLoaded)
                {
                    TextureAssets.Tile[Type] = vanillaPedestal;
                    pedestalTextureBorrowed = true;
                }
            }
            return true;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            // 悬浮部分不在物块贴图内,登记特殊绘制点由 SpecialDraw 处理
            if (drawData.tileFrameX % FrameWidth == 0 && drawData.tileFrameY % FrameHeight == 0)
            {
                Main.instance.TilesRenderer.AddSpecialPoint(i, j, Terraria.GameContent.Drawing.TileDrawing.TileCounterType.CustomNonSolid);
            }
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Point p = new Point(i, j);
            Tile tile = Main.tile[p.X, p.Y];
            if (!tile.HasTile)
                return;

            Texture2D texture = RelicTexture.Value;

            int frameY = tile.TileFrameX / FrameWidth;
            Rectangle frame = texture.Frame(HorizontalFrames, VerticalFrames, 0, frameY);

            Vector2 origin = frame.Size() / 2f;
            Vector2 worldPos = p.ToWorldCoordinates(24f, 64f);

            Color color = Lighting.GetColor(p.X, p.Y);

            bool direction = tile.TileFrameY / FrameHeight != 0;
            SpriteEffects effects = direction ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // 随时间上下缓动
            const float TwoPi = (float)Math.PI * 2f;
            float offset = (float)Math.Sin(Main.GlobalTimeWrappedHourly * TwoPi / 5f);
            Vector2 drawPos = worldPos - Main.screenPosition + new Vector2(0f, -40f) + new Vector2(0f, offset * 4f);

            spriteBatch.Draw(texture, drawPos, frame, color, 0f, origin, 1f, effects, 0f);

            // 周期性泛光
            float scale = (float)Math.Sin(Main.GlobalTimeWrappedHourly * TwoPi / 2f) * 0.3f + 0.7f;
            Color effectColor = color;
            effectColor.A = 0;
            effectColor = effectColor * 0.1f * scale;
            float offset2 = 6f + offset * 2f;
            float oneSixth = 1f / 6f;
            for (float k = 0f; k < 1f; k += oneSixth)
                spriteBatch.Draw(texture, drawPos + (TwoPi * k).ToRotationVector2() * offset2, frame, effectColor, 0f, origin, 1f, effects, 0f);
        }
    }
}
