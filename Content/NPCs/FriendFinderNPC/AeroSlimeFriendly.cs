using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.FriendFinderNPC
{
    public class AeroSlimeFriendly : FriendFindNPC
    {
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 4;
            this.HideFromBestiary();
        }
        public override bool CheckActive() {
            return false;
        }
        public override void SetDefaults() {
            NPC.noTileCollide = true;
            NPC.damage = 25;
            NPC.width = 40;
            NPC.height = 30;
            NPC.defense = 15;
            NPC.lifeMax = 500;
            NPC.knockBackResist = 0.8f;
            AnimationType = NPCID.Slimer;
            NPC.value = Item.buyPrice(0, 0, 1, 0);
            NPC.alpha = 50;
            NPC.lavaImmune = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.friendly = true;
        }
        public override void HitEffect(NPC.HitInfo hit) {
            for (int k = 0; k < 5; k++) {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueTorch, hit.HitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0) {
                for (int k = 0; k < 20; k++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueTorch, hit.HitDirection, -1f, 0, default, 1f);
                }
            }
        }

        public override void AI() {
            this.applyCollisionDamage();
            Entity target = this.FindTarget();
            NPC.velocity += (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 0.6f;
            NPC.velocity *= 0.966f;
            NPC.direction = NPC.velocity.X > 0 ? 1 : -1;
        }

        public override bool? CanFallThroughPlatforms() {
            return true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit) {
            NPC.velocity = (NPC.Center - target.Center).SafeNormalize(Vector2.Zero) * 7;
        }

    }
}
