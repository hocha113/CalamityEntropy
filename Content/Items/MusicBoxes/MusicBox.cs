using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.MusicBoxes
{
    public abstract class MusicBox : ModItem
    {
        public static void AddMusicBox(string musicFile, int itemID, int tileID) {
            Mod musicMod = CalamityEntropy.Instance;
            int musicID = MusicLoader.GetMusicSlot(musicMod, musicFile);
            MusicLoader.AddMusicBox(musicMod, musicID, itemID, tileID);
        }
        public abstract int MusicBoxTile { get; }
        public abstract string MusicFile { get; }
        public virtual bool Obtainable { get; } = true;
        public override void Load() {
            if (CalamityEntropy.mbRegs == null) {
                CalamityEntropy.mbRegs = new System.Collections.Generic.List<MusicBox>();
            }
            CalamityEntropy.mbRegs.Add(this);
        }
        public override void SetStaticDefaults() {
            ItemID.Sets.CanGetPrefixes[Type] = false;
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;

            if (!Obtainable) {
                Item.ResearchUnlockCount = 0;
            }
        }

        public override void SetDefaults() {
            Item.DefaultToMusicBox(MusicBoxTile, 0);
        }
    }
}
