using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.FriendFinderNPC
{
    public static class TargetFinder
    {
        public static Entity FindTarget(this ModNPC nPC) {
            Player owner = nPC.NPC.Entropy().friendFinderOwner.ToPlayer();
            NPC target = null;
            float distance = 1000;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.friendly) {
                    var d = CEUtils.getDistance(npc.Center, owner.Center);
                    if (d < distance) {
                        distance = d;
                        target = npc;
                    }
                }
            }
            if (target != null) {
                return target;
            }
            else {
                return owner;
            }
        }

        // 原灾厄 NPCUtils.HideFromBestiary 的自有等价实现：图鉴中隐藏该 NPC
        public static void HideFromBestiary(this ModNPC nPC) {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(nPC.Type, value);
        }

        public static void applyCollisionDamage(this ModNPC npc) {
            if (npc.NPC.Entropy().friendFinderOwner == Main.myPlayer) {
                foreach (NPC n in Main.ActiveNPCs) {
                    if (npc.NPC.Entropy().tfriendlyNPCHitCooldown[n.whoAmI] > 0) {
                        continue;
                    }
                    if (!n.friendly && !n.dontTakeDamage) {
                        if (n.getRect().Intersects(npc.NPC.getRect())) {
                            var dmgs = n.CalculateHitInfo(npc.NPC.damage, 0, false, 0, DamageClass.Generic);
                            n.StrikeNPC(dmgs);
                            NPCLoader.OnHitNPC(npc.NPC, n, dmgs);
                            if (Main.netMode != NetmodeID.SinglePlayer) {
                                NetMessage.SendStrikeNPC(n, dmgs);
                            }
                            npc.NPC.Entropy().tfriendlyNPCHitCooldown[n.whoAmI] = 14;
                        }
                    }
                }
            }
        }
    }
}
