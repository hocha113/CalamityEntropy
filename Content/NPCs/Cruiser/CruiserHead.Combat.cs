using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    public partial class CruiserHead
    {
        #region 弹幕重复命中衰减
        /// <summary>
        /// 同一发弹幕每次命中后的伤害乘算衰减。体节与尾节共用这一份记录表与这个系数,
        /// 所以名字与可见性都不能改
        /// </summary>
        public static float ProjDamageReduce = 0.5f;

        public class HitRecord
        {
            /// <summary>原代码只加不减也从不读,保留以维持对外形状</summary>
            public int Timeleft = 200;
            public int ProjID = -1;
            public float dmgMult = 1;
            public HitRecord(int id) {
                ProjID = id;
            }
        }
        public List<HitRecord> hitRecords = new List<HitRecord>();

        /// <summary>每帧把衰减系数往 1 拉回,顺手清掉已消失弹幕的记录</summary>
        private void UpdateHitRecords() {
            for (int i = hitRecords.Count - 1; i >= 0; i--) {
                hitRecords[i].dmgMult = float.Lerp(hitRecords[i].dmgMult, 1, CruiserDirector.HitRecordDecay);
                if (hitRecords[i].ProjID < 0 || !hitRecords[i].ProjID.ToProj().active) {
                    hitRecords.RemoveAt(i);
                }
            }
        }
        #endregion

        #region 承伤
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) {
            // 原灾厄 DR 减伤的本地结算
            modifiers.FinalDamage *= 1f - DamageReduction;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) {
            bool found = false;
            HitRecord hr = null;
            foreach (var hrc in hitRecords) {
                if (hrc.ProjID == projectile.whoAmI) {
                    found = true;
                    hr = hrc;
                    break;
                }
            }
            if (found) {
                modifiers.FinalDamage *= hr.dmgMult;
                hr.dmgMult *= ProjDamageReduce;
                if (!projectile.minion && (projectile.penetrate == -1 || projectile.penetrate > 4))
                    hr.dmgMult *= ProjDamageReduce;
                if (!projectile.minion) {
                    hr.Timeleft += 20;
                    if (hr.Timeleft > 250) {
                        hr.Timeleft = 250;
                    }
                }
            }
            else {
                hitRecords.Add(new HitRecord(projectile.whoAmI));
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            if (CurrentState == CruiserStateIndex.PhaseTransing)
                return false;
            return noaitime <= 0 && CurrentState != CruiserStateIndex.BiteAndDash;
        }
        public override bool CanHitNPC(NPC target) {
            return noaitime <= 0 && base.CanHitNPC(target);
        }
        public override bool? CanBeHitByItem(Player player, Item item) {
            if (noaitime > 0) {
                return false;
            }
            return base.CanBeHitByItem(player, item);
        }
        public override bool? CanBeHitByProjectile(Projectile projectile) {
            if (noaitime > 0) {
                return false;
            }
            return base.CanBeHitByProjectile(projectile);
        }
        public override bool CanBeHitByNPC(NPC attacker) {
            return noaitime <= 0 && base.CanBeHitByNPC(attacker);
        }

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox) {
            //aitype 从未被赋值,这一支到不了。照搬
            if (aitype == 3) {
                npcHitbox = new Rectangle(0, 0, 0, 0);
                return true;
            }
            return false;
        }
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
            //同上,死分支
            if (aitype == 3) {
                modifiers.SetMaxDamage(0);
                modifiers.FinalDamage *= 0;
                modifiers.DisableSound();
                target.immuneTime = 10;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
            target.AddBuff(Main.zenithWorld ? ModContent.BuffType<MaliciousCode>() : ModContent.BuffType<VoidTouch>(), 150);
        }

        /// <summary>血量归零改走 200 帧死亡演出,演出跑完才真死</summary>
        public override bool CheckDead() {
            if (DeathAnmCount <= 0) {
                return true;
            }
            DeathAnm = true;
            NPC.damage = 0;
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            NPC.active = true;
            NPC.netUpdate = true;
            if (NPC.netSpam >= 10)
                NPC.netSpam = 9;
            return false;
        }

        public override bool CheckActive() {
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit) {
            if (NPC.life <= 0 && DeathAnmCount <= 10 && !Main.dedServ) {
                if (!Main.zenithWorld) {
                    CEUtils.PlaySound("VoidAttack", 1, NPC.Center);
                    //死亡爆散全走 CEVoidScreen RT 合成,shape=4 是旧 VoidParticles 几何,zenith 改 RealisticExplosion
                    for (int i = 0; i < CruiserDirector.DeathVoidParticles; i++) {
                        var p = PRTLoader.NewParticle<PRT_Void>(NPC.Center, CEUtils.randomPointInCircle(CruiserDirector.DeathVoidScatter), Color.White, 1f);
                        p.Opacity = Main.rand.NextFloat(1f, 2f);
                        p.shape = 4;
                        p.vd = CruiserDirector.DeathVoidDrag;
                    }
                }
                else {
                    PRTLoader.NewParticle<PRT_RealisticExplosion>(NPC.Center, Vector2.Zero, Color.White, 10).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, -1);
                }
                // 原灾厄全局屏震改自有 ScreenShaker
                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, CruiserDirector.DeathShake));
            }
        }
        #endregion
    }
}
