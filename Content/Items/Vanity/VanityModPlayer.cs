using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class VanityModPlayer : ModPlayer
    {
        public string vanityEquippedLast = "";
        public string vanityEquipped = "";
        public int SpecialFlag = 0;
        public int TheocrazyDye = -1;
        public int TheocrazyDyeItemID = -1;
        public bool TheocracyMark = false;
        public override void PostUpdate()
        {
            if (TheocracyMark)
            {
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<Theostring>()] < 1)
                {
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Theostring>(), 0, 0, Player.whoAmI, -1);
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Theostring>(), 0, 0, Player.whoAmI, 1);
                    }
                }
            }
        }
        public override void ResetEffects()
        {
            vanityEquippedLast = vanityEquipped;
            TheocracyMark = false;
            SpecialFlag = 0;
            
            
            if (!Main.gameMenu)
            {
                vanityEquipped = "";
            }
        }

        public override void FrameEffects()
        {
            if (TheocracyMark)
            {
                Player.legs = EquipLoader.GetEquipSlot(Mod, "TheocracyMark", EquipType.Legs);
                Player.body = EquipLoader.GetEquipSlot(Mod, "TheocracyMark", EquipType.Body);
            }
            // if (vanityEquipped != "")
            // {
            //
            //     Player.legs = EquipLoader.GetEquipSlot(Mod, vanityEquipped, EquipType.Legs);
            //     Player.body = EquipLoader.GetEquipSlot(Mod, vanityEquipped, EquipType.Body);
            //     Player.head = EquipLoader.GetEquipSlot(Mod, vanityEquipped, EquipType.Head);
            //
            // }
        }
        
        public override void SaveData(TagCompound tag)
        {
            if (vanityEquipped != "") tag["vanityEquipped"] = vanityEquipped;
        }
        // 2. 在角色选择界面时，游戏会最先触发 LoadData 读取存档 为了在角色选择界面看见时装所以这个是必要的
        public override void LoadData(TagCompound tag)
        {
            
            if (tag.ContainsKey("vanityEquipped"))
            {
                vanityEquipped = tag.GetString("vanityEquipped");
            }
        }
        
        public override void UpdateVisibleVanityAccessories()
        {
            if (vanityEquipped != "")
            {
                Player.legs = EquipLoader.GetEquipSlot(Mod, vanityEquipped, EquipType.Legs);
                Player.body = EquipLoader.GetEquipSlot(Mod, vanityEquipped, EquipType.Body);
                Player.head = EquipLoader.GetEquipSlot(Mod, vanityEquipped, EquipType.Head);
            }
        }
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (TheocracyMark)
                drawInfo.colorHair = Color.Transparent;
        }
    }
}