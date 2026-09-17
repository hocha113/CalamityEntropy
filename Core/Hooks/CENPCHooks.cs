using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs;
using CalamityEntropy.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Core.Hooks
{
    /// <summary>
    /// 挂在原版 <see cref="NPC"/> 上的 On_* 钩子:灵魂禁锢与谵妄的更新改写、
    /// 友方化的索敌与帧定位劫持、熵灾模式的 Boss 伤害上限与 TDR 衰减。
    /// </summary>
    internal sealed class CENPCHooks : ICELoader
    {
        //熵灾模式下额外跑一次 AI 的机械 Boss 与其体节。原先每个 NPC 每帧现场 new 一个 List,
        //200 NPC × 60fps 就是每秒上万次分配,改成加载期建好的只读集合
        private static readonly HashSet<int> EntropyModeDoubleUpdateNPCs = new HashSet<int>()
        {
            NPCID.SkeletronPrime, 128, 129, 130, 131,
            NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail,
            139, 125, 126
        };

        void ICELoader.LoadData() {
            CEDetourRegistry.Add(() => On_NPC.UpdateNPC += UpdateNPCHook, () => On_NPC.UpdateNPC -= UpdateNPCHook);
            CEDetourRegistry.Add(() => On_NPC.FindFrame += FindFrameHook, () => On_NPC.FindFrame -= FindFrameHook);
            CEDetourRegistry.Add(() => On_NPC.TargetClosest += TargetClosestHook, () => On_NPC.TargetClosest -= TargetClosestHook);
            CEDetourRegistry.Add(() => On_NPC.StrikeNPC_HitInfo_bool_bool += StrikeNPCHook, () => On_NPC.StrikeNPC_HitInfo_bool_bool -= StrikeNPCHook);
        }

        private static void UpdateNPCHook(On_NPC.orig_UpdateNPC orig, NPC self, int i) {
            if (self == null || self.type <= NPCID.None) {
                return;
            }

            //很显然不活跃的NPC不符合我们的期望
            if (!self.active || !self.TryGetGlobalNPC<EGlobalNPC>(out var ceNPC)) {
                orig(self, i);
                return;
            }
            if (ceNPC.AnimaTrapped > 0) {
                ceNPC.AnimaTrapped--;
                self.position += self.velocity;
                self.velocity *= 0.9f;
                for (int ii = 0; ii < self.immune.Length; ii++) {
                    if (self.immune[ii] > 0) {
                        self.immune[ii]--;
                    }
                }
            }
            else {
                if (self.TryGetGlobalNPC<DeliriumGlobalNPC>(out var deliriumNPC) && deliriumNPC.delirium) {
                    NPC npc = self;
                    npc.damage = deliriumNPC.damage;
                    deliriumNPC.counter--;
                    if (deliriumNPC.counter <= 0) {
                        if (!Main.dedServ) {
                            CEUtils.PlaySound("clicker_static", 1, npc.Center);
                        }
                        deliriumNPC.counter = Main.rand.Next(60, 360);
                        npc.netUpdate = true;
                        npc.netSpam = 0;
                        int npc_ = NPC.NewNPC(npc.GetSource_FromThis(), (int)npc.Center.X, (int)npc.Center.Y, Delirium.npcTurns[Main.rand.Next(Delirium.npcTurns.Count)]);
                        NPC spawn = npc_.ToNPC();
                        spawn.Center = npc.Center;
                        spawn.lifeMax = npc.lifeMax;
                        spawn.life = npc.life;
                        spawn.damage = npc.damage;
                        spawn.GetGlobalNPC<DeliriumGlobalNPC>().delirium = true;
                        spawn.GetGlobalNPC<DeliriumGlobalNPC>().damage = deliriumNPC.damage;
                        spawn.GetGlobalNPC<DeliriumGlobalNPC>().counter = deliriumNPC.counter;
                        spawn.netUpdate = true;
                        spawn.netSpam = 0;
                        npc.active = false;
                    }
                    if (npc.type != NPCID.DukeFishron && npc.type != NPCID.Golem && npc.type != NPCID.SkeletronHead) {
                        orig(self, i);
                        if (npc.type != NPCID.EyeofCthulhu && npc.type != NPCID.QueenBee && npc.type != NPCID.Retinazer && npc.type != NPCID.Spazmatism && npc.type != NPCID.MoonLordCore) {
                            orig(self, i);
                        }
                    }
                }
                if (CalamityEntropy.EntropyMode) {
                    if (self.type == NPCID.Golem || self.type == NPCID.GolemHead || self.type == NPCID.GolemHeadFree) {
                        orig(self, i);
                        self.Center -= self.velocity * 0.5f;
                    }
                    if (EntropyModeDoubleUpdateNPCs.Contains(self.type)) {
                        orig(self, i);
                        self.position -= self.velocity;
                    }
                }
                orig(self, i);
            }
        }

        //友方化的 NPC 借 0 号玩家当索敌锚点:先把 0 号玩家挪到目标身上跑原版帧定位,跑完立刻还原
        private static void FindFrameHook(On_NPC.orig_FindFrame orig, NPC self) {
            EGlobalNPC entropy = self.Entropy();
            if (entropy.ToFriendly) {
                self.target = 0;
                NPC npc = self;
                npc.boss = false;

                npc.friendly = true;

                NPC t = null;
                //比距离不用开方,4600 的平方即可
                float distSq = 4600f * 4600f;
                foreach (NPC n in Main.ActiveNPCs) {
                    if (!n.friendly && !n.dontTakeDamage) {
                        float d = Vector2.DistanceSquared(n.Center, npc.Center);
                        if (d < distSq) {
                            t = n;
                            distSq = d;
                        }
                    }
                }
                entropy.plrOldPos3 = Main.player[0].position;
                entropy.plrOldVel3 = Main.player[0].velocity;
                if (t == null) {
                    Main.player[0].Center = entropy.f_owner.ToPlayer().Center;
                    Main.player[0].velocity = entropy.f_owner.ToPlayer().velocity;
                }
                else {
                    Main.player[0].Center = t.Center;
                    Main.player[0].velocity = t.velocity;
                }
            }
            orig(self);
            if (entropy.plrOldPos3.HasValue) {
                Main.player[0].position = entropy.plrOldPos3.Value;
                entropy.plrOldPos3 = null;
            }
            if (entropy.plrOldVel3.HasValue) {
                Main.player[0].velocity = entropy.plrOldVel3.Value;
                entropy.plrOldVel3 = null;
            }
        }

        private static void TargetClosestHook(On_NPC.orig_TargetClosest orig, NPC self, bool faceTarget) {
            orig(self, faceTarget);
            if (self.Entropy().ToFriendly) {
                self.target = 0;
                NPC npc = self;
                npc.boss = false;

                npc.friendly = true;
                SetTargetTrackingValues(self, faceTarget, -1);
            }
        }

        private static int StrikeNPCHook(On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig, NPC self, NPC.HitInfo hit, bool fromNet, bool noPlayerInteraction) {
            if (!hit.InstantKill) {
                if (self.boss && (CalamityEntropy.EntropyMode || EDownedBosses.TDR)) {
                    if (hit.Damage > self.lifeMax * 0.035f) {
                        hit.Damage = (int)(self.lifeMax * 0.035f);
                    }
                    hit.Damage = (int)(hit.Damage * (self.life < (self.Entropy().TDRCounter / (3f * 60 * 60) * self.lifeMax) ? (1 / (1 + ((self.Entropy().TDRCounter / (3f * 60 * 60) * self.lifeMax) - self.life) * (14f / self.lifeMax))) : 1));
                }
            }
            return orig(self, hit, fromNet, noPlayerInteraction);
        }

        /// <summary>原版 TargetClosest 尾段的朝向/目标矩形结算,友方化后要按新目标重跑一遍</summary>
        public static void SetTargetTrackingValues(NPC npc, bool faceTarget, int tankTarget) {
            if (tankTarget >= 0) {
                npc.targetRect = new Rectangle((int)Main.projectile[tankTarget].position.X, (int)Main.projectile[tankTarget].position.Y, Main.projectile[tankTarget].width, Main.projectile[tankTarget].height);
                npc.direction = 1;
                if (npc.targetRect.X + npc.targetRect.Width / 2 < npc.position.X + npc.width / 2)
                    npc.direction = -1;

                npc.directionY = 1;
                if (npc.targetRect.Y + npc.targetRect.Height / 2 < npc.position.Y + npc.height / 2)
                    npc.directionY = -1;
            }
            else {
                if (npc.target < 0 || npc.target >= 255)
                    npc.target = 0;

                npc.targetRect = new Rectangle((int)Main.player[npc.target].position.X, (int)Main.player[npc.target].position.Y, Main.player[npc.target].width, Main.player[npc.target].height);
                if (Main.player[npc.target].dead)
                    faceTarget = false;

                if (Main.player[npc.target].npcTypeNoAggro[npc.type] && npc.direction != 0)
                    faceTarget = false;

                if (faceTarget) {
                    bool flag = npc.oldTarget >= 0 && npc.oldTarget <= 254;
                    bool num = Main.player[npc.target].itemAnimation == 0 && Main.player[npc.target].aggro < 0;
                    bool flag2 = !npc.boss;
                    if (!(num && flag && flag2)) {
                        npc.direction = 1;
                        if (npc.targetRect.X + npc.targetRect.Width / 2 < npc.position.X + npc.width / 2)
                            npc.direction = -1;

                        npc.directionY = 1;
                        if (npc.targetRect.Y + npc.targetRect.Height / 2 < npc.position.Y + npc.height / 2)
                            npc.directionY = -1;
                    }
                }
            }

            if (npc.confused)
                npc.direction *= -1;

            if ((npc.direction != npc.oldDirection || npc.directionY != npc.oldDirectionY || npc.target != npc.oldTarget) && !npc.collideX && !npc.collideY)
                npc.netUpdate = true;
        }
    }
}
