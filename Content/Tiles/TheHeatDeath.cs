using CalamityEntropy.Content.Items.PrefixItem;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Tiles
{
    public class TheHeatDeath : ModTile
    {
        public override void SetStaticDefaults() {
            Main.tileSolid[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileBrick[Type] = true;
            Main.tileLighted[base.Type] = true;
            Main.tileSpelunker[base.Type] = true;
            base.MineResist = 6f;
            // 脱离灾厄:原灾厄 AuricMine,按 sound-map 替换
            base.HitSound = SoundID.Tink with { Pitch = 0.3f, PitchVariance = 0.25f };
            AddMapEntry(new Color(150, 0, 0));
            RegisterItemDrop(ModContent.ItemType<BlessingHeatDeath>());
        }
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) {
            Lighting.AddLight(new Vector2(i, j) * 16, 0.2f, 0.05f, 0.05f);
        }
    }
}