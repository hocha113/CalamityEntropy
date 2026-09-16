using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs
{
    #region TagDebuffs
    public class JailerWhipDebuff : ModBuff
    {
        public static readonly int TagDamage = 2;
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class DragonWhipDebuff : ModBuff
    {
        public static readonly int TagDamage = 15;
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class WyrmWhipDebuff : ModBuff
    {
        public static readonly int TagDamage = 90;
        public static readonly float TagDamageMul = 0.15f;
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class CruiserWhipDebuff : ModBuff
    {
        public static readonly int TagDamage = 30;
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class CrystedgeWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class DaylightWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class ForeseeWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class LashingBramblerodWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class MindCorruptorWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class SinewLashWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class WhipOfEvilKingWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    public class TectonicChainBladeWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityEntropy/Content/Buffs/WhipDebuff";
        public override void SetStaticDefaults() {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }
    #endregion
    //Entropy Whip Tag System
    public class WhipTag
    {
        public int TagDamage;
        public float TagDamageMult;
        public float CritChance;
        public int TimeLeft = 0;
        public string ItemFullName;
        public string EffectName;
        public bool IsABaitTag = false;
        public WhipTag(string name, int tick, int tagDamage, float tagDamageMult, float Crit = 0, string effectName = "") {
            ItemFullName = name;
            TimeLeft = tick;
            TagDamage = tagDamage;
            TagDamageMult = tagDamageMult;
            this.CritChance = Crit;
            EffectName = effectName;
        }
    }
    public class WhipDebuffNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public List<WhipTag> Tags = new List<WhipTag>();
        public int BaitStick = 0;
        public void ClearBaitTags() {
            for (int i = Tags.Count - 1; i >= 0; i--) {
                if (Tags[i].IsABaitTag)
                    Tags.RemoveAt(i);
            }
        }
        public void ClearTag(string Name) {
            for (int i = Tags.Count - 1; i >= 0; i--) {
                if (Tags[i].EffectName == Name)
                    Tags.RemoveAt(i);
            }
        }
        //Hooked to CalamityGlobalNPC.ModifyHitByProjectile in EModILEdit:107
        public void ModifyHitByProj(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) {
            if (projectile.npcProj || projectile.trap || !(projectile.DamageType.CountsAsClass(DamageClass.Summon)) || ProjectileID.Sets.IsAWhip[projectile.type])
                return;
            bool crit = false;
            if (projectile.TryGetOwner(out var owner)) {
                if (Main.rand.Next(0, 100) < owner.Entropy().summonCrit) {
                    crit = true;
                }
            }
            var projTagMultiplier = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
            if (npc.HasBuff<JailerWhipDebuff>()) {
                modifiers.FlatBonusDamage += JailerWhipDebuff.TagDamage * projTagMultiplier;
                if (Main.rand.NextBool(50)) {
                    crit = true;
                }
            }
            foreach (var t in Tags) {
                modifiers.FlatBonusDamage += t.TagDamage * projTagMultiplier;
                modifiers.SourceDamage *= t.TagDamageMult;
                if (Main.rand.NextFloat() < t.CritChance) {
                    crit = true;
                }
            }

            if (npc.HasBuff<DragonWhipDebuff>()) {
                modifiers.FlatBonusDamage += DragonWhipDebuff.TagDamage * projTagMultiplier;
                if (Main.rand.NextBool(50)) {
                    crit = true;
                }
            }
            if (npc.HasBuff<CruiserWhipDebuff>()) {
                modifiers.FlatBonusDamage += CruiserWhipDebuff.TagDamage * projTagMultiplier;
                if (Main.rand.NextBool(10)) {
                    crit = true;
                }
            }
            if (npc.HasBuff<WyrmWhipDebuff>()) {
                modifiers.FlatBonusDamage += WyrmWhipDebuff.TagDamage * projTagMultiplier;
                modifiers.SourceDamage += WyrmWhipDebuff.TagDamageMul * projTagMultiplier;
                if (Main.rand.NextBool(8)) {
                    crit = true;
                }
            }

            // 2026-08-31 平衡案:暗影符石(ShadowRune)召唤物护甲穿透 128→25
            if (projectile.GetOwner().Entropy().shadowRune) {
                modifiers.ArmorPenetration += 25;
            }

            if (crit) {
                //npc.Entropy().nextHitCrit = true;
                //modifiers.SetCrit();
                var fInfo = modifiers.GetType().GetField("_critOverride", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                object boxed = modifiers;
                fInfo.SetValue(boxed, (bool?)true);
                modifiers = (NPC.HitModifiers)boxed;
            }
        }
        public override bool PreAI(NPC npc) {
            for (int i = Tags.Count - 1; i >= 0; i--) {
                if (--Tags[i].TimeLeft <= 0) {
                    Tags.RemoveAt(i);
                }
            }
            BaitStick--;
            return base.PreAI(npc);
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) {
            if (projectile.npcProj || projectile.trap || !(projectile.minion || projectile.sentry || ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type]))
                return;
            /*if(npc.Entropy().nextHitCrit)
            {
                npc.Entropy().nextHitCrit = false;
                hit.Crit = true;
            }*/
            if (true) {
                if (npc.HasBuff<DragonWhipDebuff>()) {
                    // 2026-08-31 平衡案:沐生标记重做。仆从命中标记目标时每0.75秒引爆一次:
                    // 600固定基伤的破晓爆炸(召唤伤害)+为玩家回复1生命,爆炸附加破晓减益。
                    if (projectile.TryGetOwner(out var owner) && CECooldowns.CheckCD("VitalfeatherBurst", 45)) {
                        int burstDamage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(VitalfeatherBurst.BaseDamage);
                        Projectile.NewProjectile(projectile.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<VitalfeatherBurst>(), burstDamage, 4f, projectile.owner);
                        owner.Heal(1);
                    }
                }
                foreach (var t in Tags) {
                    if (hit.Crit) {
                        if (t.EffectName == "LashingBramblerod") {
                            if (projectile.TryGetOwner(out var owner)) {
                                if (owner.ownedProjectileCounts[SilvaVineDRPlayer.VineType] > 0) {
                                    foreach (Projectile p in Main.ActiveProjectiles) {
                                        if (p.type == SilvaVineDRPlayer.VineType && p.ModProjectile is SilvaVine sv) {
                                            if (sv.flowerCount < SilvaVine.MaxFlowers) {
                                                sv.flowerCount++;
                                            }
                                        }
                                    }
                                }
                                else {
                                    Projectile.NewProjectile(owner.GetSource_FromThis(), owner.position, Vector2.Zero, SilvaVineDRPlayer.VineType, 40, 0, owner.whoAmI);
                                }
                            }
                        }
                        if (t.EffectName == "Crystedge") {
                            Projectile.NewProjectile(projectile.GetSource_FromAI(), npc.Center, CEUtils.randomVec(5.6f), ModContent.ProjectileType<CrystedgeCrystalBig>(), projectile.damage * 3, projectile.knockBack, projectile.owner);
                        }
                        if (t.EffectName == "ForeseeWhip") {
                            int C = 5;
                            foreach (NPC n in Main.ActiveNPCs) {
                                if (!n.friendly && n.CanBeChasedBy(projectile) && n.Distance(npc.Center) < 400 && n != npc) {
                                    if (C > 0) {
                                        C--;
                                        int dmg = damageDone;
                                        projectile.GetOwner().ApplyDamageToNPC(n, dmg, 0, 0, false, projectile.DamageType);
                                        for (float f = 0; f <= 1; f += 0.1f) {
                                            //预见鞭连锁,RuneParticle多帧贴图走PRTFrameTextures
                                            PRTLoader.NewParticle<PRT_RuneParticle>(Vector2.Lerp(npc.Center, n.Center, f), CEUtils.randomPointInCircle(0.1f), Color.White, 0.5f)
                                                .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (t.EffectName == "TectonicChainBlade") {
                        if (Main.rand.NextBool(5)) {
                            Projectile.NewProjectile(projectile.GetSource_FromAI(), npc.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(16, 22), ModContent.ProjectileType<TectonicShardHomingSummon>(), (int)(projectile.damage * 0.33f) + 1, 2, projectile.owner);
                        }
                    }
                    if (t.EffectName == "MindCorruptor") {
                        float rot = CEUtils.randomRot();
                        Projectile.NewProjectile(projectile.GetSource_FromAI(), npc.Center - rot.ToRotationVector2() * 128, rot.ToRotationVector2() * 256 / 10f, ModContent.ProjectileType<CorruptStrike>(), projectile.damage / 12 + 1, 2, projectile.owner);
                    }
                    if (t.EffectName == "DaylightProjectile") {
                        int type = ModContent.ProjectileType<DaylightSun>();
                        foreach (Projectile p in Main.ActiveProjectiles) {
                            if (p.owner == projectile.owner && p.type == type) {
                                if (p.ai[0] == 0)
                                    p.ai[0]++;
                            }
                        }
                    }
                    if (t.EffectName == "SinewLash") {
                        if (CECooldowns.CheckCD("SinwLashFlesh", 70)) {
                            Projectile.NewProjectile(projectile.GetSource_FromAI(), npc.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(8, 14), ModContent.ProjectileType<FleshChunk>(), (int)(projectile.GetOwner().GetTotalDamage(DamageClass.Summon).ApplyTo(34)), 5, projectile.owner, 0, 0, Main.rand.Next(0, 2));
                        }
                    }
                    if (t.EffectName == "EvilKingWhip") {
                        if (CECooldowns.CheckCD("EvilKingWhip", 60)) {
                            int spade = ModContent.ProjectileType<Spade>();
                            if (projectile.GetOwner().ownedProjectileCounts[spade] < 3)
                                Projectile.NewProjectile(projectile.GetSource_FromAI(), CEUtils.randomPoint(npc.Hitbox), Vector2.Zero, spade, (int)(projectile.GetOwner().GetTotalDamage(DamageClass.Summon).ApplyTo(5)), 0);
                        }
                    }
                }
            }
        }
    }
}
