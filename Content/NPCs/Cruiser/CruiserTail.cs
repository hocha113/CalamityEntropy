using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Core.AI;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityEntropy.Content.NPCs.Cruiser.CruiserHead;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    //[StaticImmunity(typeof(CruiserHead))]
    /// <summary>
    /// 尾节。与体节同型的<b>锚定型部件</b>:位置每帧从头部 Rigs2D 链骨的最后一节直读,
    /// 只清原版平滑、不进预测纠偏器。
    /// 它的 <c>ai[3]</c> 是头部索引(生成时写入),这一处槽位在迁移后<b>不变</b>——
    /// 让位给状态号的是头部自己的 <c>ai[3]</c>
    /// </summary>
    public class CruiserTail : ModNPC
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
            //与头部、体节保持同一个平滑层级,见 CruiserBody 的说明
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }
        // 原灾厄 DR 的本地等效,每帧从头部镜像
        public float DamageReduction = 0.4f;
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 1f - DamageReduction;
        }
        public bool Phase2 => (Main.npc[(int)NPC.ai[3]].ModNPC is CruiserHead ch && ch.phaseTrans >= CruiserDirector.PhaseTransDrawSwitch) ? true : false;

        public Vector2 lastPos;
        public override void SetDefaults() {
            NPC.width = 45;
            NPC.height = 45;
            NPC.damage = 120;
            NPC.dontCountMe = true;
            NPC.defense = 10;
            NPC.lifeMax = 80000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.value = 50f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.Entropy().VoidTouchDR = 0.6f;
            NPC.scale = 1.1f;
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
        public override bool CheckActive() {
            if (((int)NPC.ai[1]).ToNPC().active) {
                return false;
            }
            return true;
        }

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
            Lighting.AddLight(NPC.Center, 1f, 1f, 1f);
            /*            if (((int)NPC.ai[3]).ToNPC().life < (((int)NPC.ai[3]).ToNPC().lifeMax / 2))
                        {
                            NPC.active = false;
                            NPC.netUpdate = true;
                        }*/
            if (NPC.ai[1] < Main.maxNPCs && Main.npc[(int)NPC.ai[1]].active) {
                //落到头部链骨的最后一节(ai[2] == length);骨架未建好的首帧退回硬跟随
                if (Main.npc[(int)NPC.ai[3]].ModNPC is CruiserHead head && head.TryGetChainBone((int)NPC.ai[2], out Vector2 pos, out float dir)) {
                    NPC.Center = pos;
                    NPC.rotation = dir;
                }
                else {
                    CEUtils.wormFollow(NPC.whoAmI, (int)NPC.ai[1], (int)(CruiserDirector.ChainSpacing * NPC.scale), false);
                }
            }
            else {
                NPC.active = false;
            }
        }
        /*vel = NPC.Center - lastPos;
        if (NPC.ai[3] == 1)
        {
            NPC.ai[3] = 0;
            jv = true;
            if (da < 0)
            {
                da = 1;
            }
            tail_vj = 20;
        }*/
        /*            if (jv)
                    {
                        da += tail_vj;
                        tail_vj -= 1.5f;
                        if (da < 0)
                        {
                            da = 0;
                            tail_vj = 0;
                            jv = false;
                        }
                    }
                    else
                    {
                        ja -= (float)Math.Sqrt((float)vel.Length() / 120f);
                        if (ja < 0)
                        {
                            ja = 0;
                        }
                        ja = Util.rotatedToAngle(ja, 50, 0.22f, false);
                        ja = (100f / ((float)vel.Length() + 1f)) * 5;
                        da = da + (ja - da) * 0.1f;
                        lastPos = NPC.Center;
                    }*/

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) {
            return false;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            return false;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            return;
            //Texture2D f1 = ModContent.Request<Texture2D>("CalamityEntropy/Content/NPCs/Cruiser/Flagellum").Value;

            //spriteBatch.Draw(f1, NPC.Center - Main.screenPosition - new Vector2(32, 0).RotatedBy(NPC.rotation), null, Color.White, NPC.rotation + MathHelper.ToRadians(190 - da), new Vector2(0, f1.Height), NPC.scale, SpriteEffects.None, 0);
            //spriteBatch.Draw(f1, NPC.Center - Main.screenPosition - new Vector2(32, 0).RotatedBy(NPC.rotation), null, Color.White, NPC.rotation + MathHelper.ToRadians(170 + da), new Vector2(0, 0), NPC.scale, SpriteEffects.FlipVertically, 0);
        }
    }
}
