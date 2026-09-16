using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class StealthState : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.moveSpeed *= 2;
            player.Entropy().WingSpeed += 1;
            foreach (NPC npc in Main.npc) {
                int w = 40 + 32 * player.Entropy().WeaponBoost;
                if (npc.active && !npc.friendly && (npc.Hitbox.Intersects(player.getRect()) || (player.Entropy().WeaponBoost > 0 && npc.Hitbox.Intersects(player.Center.getRectCentered(w, w))))) {
                    if (!player.Entropy().DarkArtsTarget.Contains(npc)) {
                        npc.Entropy().daTarget = true;
                        player.Entropy().DarkArtsTarget.Add(npc);
                        SoundEffect se = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/da2").Value;
                        if (se != null) { se.Play(Main.soundVolume, 0, 0); }
                    }
                }
            }
            foreach (Projectile proj in Main.projectile) {
                int w = 40 + 32 * player.Entropy().WeaponBoost;
                if (proj.active && proj.hostile && proj.width < 100 && proj.height < 100 && (proj.Hitbox.Intersects(player.getRect())) || (player.Entropy().WeaponBoost > 0 && proj.Hitbox.Intersects(player.Center.getRectCentered(w, w)))) {
                    if (!player.Entropy().DarkArtsTarget.Contains(proj)) {
                        proj.Entropy().daTarget = true;
                        player.Entropy().DarkArtsTarget.Add(proj);
                        SoundEffect se = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/da2").Value;
                        if (se != null) { se.Play(Main.soundVolume, 0, 0); }
                    }
                }
            }
        }
    }
}
