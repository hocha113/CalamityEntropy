using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Wings)]
    public class BigShotsWing : CEBaseWings, ISpecialDrawingWing, IGetFromStarterBag
    {
        public static float HorSpeed = 2.6f;
        public static float AccMul = 0.3f;
        public static int wTime = 700;
        public int AnimationTick => 4;
        public int FallingFrame => 2;
        public int MaxFrame => 8;
        public int SlowFallingFrame => 1;
        public override void SetStaticDefaults() {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(wTime, HorSpeed, AccMul, false, 20, 2.8f);
        }

        public override void SetDefaults() {
            base.SetDefaults();
            Item.width = 22;
            Item.height = 20;
            Item.value = Item.buyPrice(0, 12, 25);
            Item.rare = ItemRarityID.Pink;
            Item.accessory = true;

        }
        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().addEquip("BSWing", !hideVisual);
            if (!hideVisual) {
                player.Entropy().light += 0.5f;
            }
            bool flag = false;
            foreach (NPC n in Main.ActiveNPCs) {
                // 脱离灾厄:灾厄 IsABoss 判定改为原版 boss 旗标+世吞体节特判
                if (n.boss || n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsTail) {
                    flag = true;
                    break;
                }
            }
            if (flag) {
                player.Entropy().WingTimeMult *= 0.1f;
            }
            player.Entropy().BigShotWingVisual = visual;
        }
        public override void UpdateVanity(Player player) {
            player.Entropy().addEquipVisual("BSWing");
            player.Entropy().BigShotWingVisual = visual;
        }
        #region ToggleVisual

        int visual = 0;
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.FindAndReplace("[TOGGLE]", this.GetLocalizedValue("Visual" + visual));
        }
        public override bool CanRightClick() => Main.keyState.PressingShift();
        public override void RightClick(Player player) {
            visual++;
            if (visual > 2)
                visual = 0;
            Item.NetStateChanged();
        }
        public override bool ConsumeItem(Player player) => false;
        public override void SaveData(TagCompound tag) {
            tag.Add("visual", visual);
        }

        public override void LoadData(TagCompound tag) {
            if (tag.ContainsKey("visual"))
                visual = tag.GetInt("visual");
        }

        public override void NetSend(BinaryWriter writer) {
            writer.Write((byte)visual);
        }

        public override void NetReceive(BinaryReader reader) {
            visual = (int)reader.ReadByte();
        }
        #endregion
        public override float BonusAscentWhileRising => 0.1f;
        public override float RisingSpeedThreshold => 0.5f;
        public override float MaxAscentSpeed => 0.5f;
        public override float BaseAscent => 0.06f;
        public bool OwnAble(Player player, ref int count) {
            if (player.Entropy().drCrystals == null) return false;
            return player.Entropy().drCrystals[1];
        }
    }
}