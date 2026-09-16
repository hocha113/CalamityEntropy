using CalamityEntropy.Content.Projectiles.Pets.DarkFissure;
using CalamityEntropy.Content.Projectiles.Pets.DoG;
using CalamityEntropy.Content.Projectiles.Pets.Signus;
using CalamityEntropy.Content.Projectiles.Pets.StormWeaver;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs.Pets
{
    public class DevourerAndTheApostles : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<SigPetProj>());
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<DarkFissure>());
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<StormWeaverPet>());
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<DoG>());
        }
    }
}
