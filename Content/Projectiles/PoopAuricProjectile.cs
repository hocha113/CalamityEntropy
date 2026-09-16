using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoopAuricProjectile : PoopProj
    {
        public override void PushNPC(NPC npc) {
            if (npc.velocity.Length() > 0.1f && npc.type != NPCID.WallofFlesh) {
                Vector2 v = (npc.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 16;
                npc.velocity += v;
            }
            if (Main.myPlayer == Projectile.owner) {
                NPC.HitInfo hit = npc.CalculateHitInfo(Projectile.damage, 0, false, 0, Projectile.DamageType);
                npc.StrikeNPC(hit);
            }
            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/shockBlast"));
        }

        public override bool BreakWhenHitNPC => false;
        public override int damageChance => 40;

    }

}