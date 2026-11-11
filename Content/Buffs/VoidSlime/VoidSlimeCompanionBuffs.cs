using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs.VoidSlime
{
    /// <summary>虚空星辰史莱姆宠物</summary>
    public class VoidStarSlimeBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<VoidStarSlimePet>());
        }
    }

    /// <summary>虚空史莱姆坐骑</summary>
    public class VoidSlimeMountBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.mount.SetMount(ModContent.MountType<VoidSlimeMount>(), player);
            player.buffTime[buffIndex] = 10;
        }
    }
}
