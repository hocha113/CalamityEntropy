using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.NPCs.FriendFinderNPC
{
    public class ScryllarFriendly : FriendFindNPC
    {
        public override void SetStaticDefaults() {
            this.HideFromBestiary();
        }
        public override bool CheckActive() {
            return false;
        }
        public override void SetDefaults() {
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.damage = 12;
            NPC.width = 80;
            NPC.height = 80;
            NPC.defense = 18;
            NPC.lifeMax = 800;
            NPC.alpha = 160;
            NPC.knockBackResist = 0.8f;
            NPC.value = Item.buyPrice(0, 0, 5, 0);
            NPC.HitSound = SoundID.NPCHit49;
            NPC.DeathSound = SoundID.NPCDeath51;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.friendly = true;
        }

        public override void AI() {
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.type == NPC.type && npc.whoAmI != NPC.whoAmI && npc.getRect().Intersects(NPC.getRect())) {
                    NPC.velocity += (NPC.Center - npc.Center).SafeNormalize(Vector2.UnitX) * 0.2f;
                }
            }
            NPC.rotation = NPC.velocity.X * 0.04f;
            NPC.spriteDirection = (NPC.direction > 0) ? 1 : -1;
            Entity target = this.FindTarget();
            NPC.direction = target.Center.X > NPC.Center.X ? 1 : -1;
            NPC.damage = 50;

            if (target is NPC) {
                NPC.ai[1]--;
                if (NPC.ai[1] < -160) {
                    NPC.ai[1] = 46;
                    NPC.ai[2] = (target.Center - NPC.Center).ToRotation();
                    NPC.netUpdate = true;
                    NPC.velocity -= NPC.ai[2].ToRotationVector2() * 6;
                }
                if (NPC.ai[1] < 0) {
                    NPC.velocity += new Vector2((float)MathHelper.Max(0, (target.Center - NPC.Center).Length()) - 100, 0).RotatedBy((target.Center - NPC.Center).ToRotation()) * 0.0014f;
                    NPC.velocity *= 0.94f;
                }
                else {
                    NPC.damage = 360;
                    if (NPC.ai[1] == 30) {
                        NPC.ai[2] = (target.Center + target.velocity * 5 - NPC.Center).ToRotation();
                    }
                    if (NPC.ai[1] < 26) {
                        //Charge蓄力前26tick每帧6 Smoke,Additive 0.14低opacity,读条VFX
                        for (int i = 0; i < 6; i++) {
                            var p = PRTLoader.NewParticle<PRT_Smoke>(NPC.Center + CEUtils.randomVec(8), CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0, 0.6f) + new Vector2(0, -1.2f), Color.OrangeRed, 0.16f);
                            p.timeleftmax = 30;
                            //timeleftmax跟Configure lifetime双写,旧PRT_Smoke字段别删其一
                            p.Configure(0.14f, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);
                        }
                    }
                    if (NPC.ai[1] < 30) {
                        NPC.velocity += NPC.ai[2].ToRotationVector2() * 3.6f;
                    }
                    NPC.velocity *= 0.98f;
                }
            }
            else {
                NPC.ai[1] = 0;
                if (CEUtils.getDistance(NPC.Center, target.Center) > 160) {
                    NPC.velocity += (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 0.6f;
                }
                NPC.velocity *= 0.98f;
            }
            this.applyCollisionDamage();
            NPC.velocity *= 0.994f;
        }
        public override void HitEffect(NPC.HitInfo hit) {
            // 硫火尘暂以原版红火把尘近似；灾厄 gore 资产不复存在，碎块演出删除
            for (int k = 0; k < 5; k++) {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RedTorch, hit.HitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0) {
                for (int k = 0; k < 40; k++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RedTorch, hit.HitDirection, -1f, 0, default, 1f);
                }
            }
        }
    }
}
