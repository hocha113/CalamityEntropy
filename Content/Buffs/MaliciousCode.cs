using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    public class MaliciousCode : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            player.Entropy().maliciousCode = true;
            if (Main.rand.NextBool(14)) {
                //恶意代码飘字,MCodeParticle的frame在AI里随机跳
                PRTLoader.NewParticle<PRT_MCodeParticle>(player.Center + CEUtils.randomVec(28), Vector2.Zero, Main.rand.NextBool(5) ? Main.DiscoColor : Color.White, Main.rand.NextFloat(0.6f, 3.4f))
                    .Configure(1, true, PRTDrawModeEnum.AlphaBlend);
            }
        }
        public override void Update(NPC npc, ref int buffIndex) {
            if (Main.rand.NextBool(8)) {
                PRTLoader.NewParticle<PRT_MCodeParticle>(npc.Center + new Vector2(Main.rand.NextFloat(npc.width) - npc.width / 2, Main.rand.NextFloat(npc.height) - npc.height / 2), Vector2.Zero, Main.rand.NextBool(4) ? Main.DiscoColor : Color.White, Main.rand.NextFloat(0.6f, 3.4f))
                    .Configure(1, true, PRTDrawModeEnum.AlphaBlend);
            }
        }
    }
    public class MaliciousCodeNPC : GlobalNPC
    {
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers) {
            if (npc.HasBuff<MaliciousCode>()) {
                modifiers.FinalDamage *= 0.7f;
            }
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (npc.HasBuff<MaliciousCode>()) {
                if (Main.rand.NextBool(3)) {
                    return false;
                }
            }
            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) {
            if (npc.HasBuff<MaliciousCode>()) {
                modifiers.ArmorPenetration += 25;
            }
        }
    }
    public class MaliciousCodeProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool malicious = false;
        public override void OnSpawn(Projectile projectile, IEntitySource source) {
            if (source is EntitySource_Parent ep) {
                if (ep.Entity is NPC npc && npc.HasBuff<MaliciousCode>()) {
                    malicious = true;
                }
            }
        }
        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers) {
            if (malicious) {
                modifiers.FinalDamage *= 0.9f;
            }
        }
        public bool SHOTSPEEDDOWN = true;
        public override bool PreAI(Projectile projectile) {
            if (malicious) {
                if (SHOTSPEEDDOWN) {
                    SHOTSPEEDDOWN = false;
                    projectile.velocity *= 0.8f;
                }
            }
            return base.PreAI(projectile);
        }
    }
}
