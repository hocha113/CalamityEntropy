using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Core.AI;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityEntropy.Content.NPCs.Cruiser.CruiserHead;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    //[StaticImmunity(typeof(CruiserHead))]
    /// <summary>
    /// 体节。<b>锚定型部件</b>:位置由 <c>CEUtils.wormFollow</c> 每帧直写(下一帧位置不是
    /// <c>position + velocity</c>,velocity 全程为 0),所以只清原版平滑、<b>不</b>进
    /// <c>CEBossNetMotion</c> 的预测纠偏器,也绝不调 <c>EndFrame</c>——预测器会和直写打架。
    /// 它本身是自愈的:任何偏移下一帧就被重新钉回链上
    /// </summary>
    public class CruiserBody : ModNPC
    {
        public override void BossHeadRotation(ref float rotation) {
            rotation = NPC.rotation;
        }
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            // 图鉴隐藏:原灾厄隐藏扩展的原版等价写法
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
            //整链是头部集中绘制、读裸坐标。头部已按类型关掉原版 netOffset 平滑,
            //体节若还留着,接缝会在每个快照上崩一次(头没偏移而体节有)。三个类型必须一致
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }
        // 原灾厄 DR 的本地等效,每帧从头部镜像
        public float DamageReduction = 0.4f;
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 1f - DamageReduction;
        }

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(NPC.width);
            writer.Write(NPC.height);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            NPC.width = reader.ReadInt32();
            NPC.height = reader.ReadInt32();
        }

        public override void SetDefaults() {

            NPC.width = 70;
            NPC.height = 70;
            NPC.damage = 160;
            NPC.dontCountMe = true;
            NPC.dontCountMe = true;
            NPC.lifeMax = 80000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.value = 50f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.defense = 80;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.7f;
            NPC.scale = 1.1f;
            DamageReduction = 0.4f;
            if (Main.getGoodWorld) {
                NPC.scale = 0.5f;
            }
            if (!Main.dedServ) {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/CruiserBoss");
            }
        }
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            if (Main.npc[(int)NPC.ai[3]].ModNPC == null)
                return true;
            if (Main.npc[(int)NPC.ai[3]].active)
                return Main.npc[(int)NPC.ai[3]].ModNPC.CanHitPlayer(target, ref cooldownSlot);
            return true;
        }
        public bool Phase2 => (Main.npc[(int)NPC.ai[3]].ModNPC is CruiserHead ch && ch.phaseTrans >= CruiserDirector.PhaseTransDrawSwitch) ? true : false;
        public override void AI() {
            //锚定型部件:清掉原版平滑,不进预测纠偏器
            CEBossHost.RunAnchoredPartFrame(NPC);
            NPC.scale = Main.npc[(int)NPC.ai[3]].scale;
            NPC.Entropy().VoidTouchDR = Main.npc[(int)NPC.ai[3]].Entropy().VoidTouchDR;
            NPC.defense = Main.npc[(int)NPC.ai[3]].defense;
            if (Main.npc[(int)NPC.ai[3]].ModNPC is CruiserHead headSeg)
                DamageReduction = headSeg.DamageReduction;
            NPC.dontTakeDamage = Main.npc[(int)NPC.ai[3]].dontTakeDamage;
            NPC.ai[0] += 1;
            NPC.life = Main.npc[(int)NPC.ai[3]].life;
            NPC.lifeMax = Main.npc[(int)NPC.ai[3]].lifeMax;
            if (NPC.ai[0] < CruiserDirector.SegmentWarmupFrames) {
                return;
            }
            /*            if (((int)NPC.ai[3]).ToNPC().life < (((int)NPC.ai[3]).ToNPC().lifeMax / 2) && NPC.ai[2] > 8)
                        {
                            NPC.active = false;
                            NPC.netUpdate = true;
                        }*/
            if (!Main.dedServ) {
                Lighting.AddLight(NPC.Center, 1f, 1f, 1f);
            }
            if (NPC.ai[1] < Main.maxNPCs) {
                if (Main.npc[(int)NPC.ai[1]].active) {

                    int spacing = CruiserDirector.ChainSpacing;
                    NPC follow = Main.npc[(int)NPC.ai[1]];
                    if (follow.active) {
                        CEUtils.wormFollow(NPC.whoAmI, (int)NPC.ai[1], (int)(spacing * NPC.scale), false);
                        if (NPC.ai[0] > CruiserDirector.SegmentTightFollowFrames) {
                            CEUtils.wormFollow(NPC.whoAmI, (int)NPC.ai[1], (int)(spacing * NPC.scale), true, CruiserDirector.ChainRotateRate);
                        }
                    }
                }
                else {
                    NPC.active = false;
                }

            }
            else {
                NPC.active = false;
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) {
            if (Main.npc[(int)NPC.ai[3]].ModNPC is CruiserHead ch) {
                bool flag = false;
                HitRecord hr = null;
                foreach (var hrc in ch.hitRecords) {
                    if (hrc.ProjID == projectile.whoAmI) {
                        flag = true;
                        hr = hrc;
                        break;
                    }
                }
                if (flag) {
                    modifiers.FinalDamage *= hr.dmgMult;
                    hr.dmgMult *= CruiserHead.ProjDamageReduce;
                    if (!projectile.minion && (projectile.penetrate == -1 || projectile.penetrate > 4))
                        hr.dmgMult *= CruiserHead.ProjDamageReduce;
                    if (!projectile.minion) {
                        hr.Timeleft += 20;
                        if (hr.Timeleft > 250) {
                            hr.Timeleft = 250;
                        }
                    }
                }
                else {
                    ch.hitRecords.Add(new HitRecord(projectile.whoAmI));
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            return false;
        }

        public override bool CheckActive() {
            if (((int)NPC.ai[1]).ToNPC().active) {
                return false;
            }
            return true;
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) {
            return false;
        }
    }
}
