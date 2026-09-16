using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class NyxolithrakenDragon : ModProjectile, iWyrmSeg
    {
        public float rot { get { return Projectile.rotation; } set { Projectile.rotation = value; } }
        public Vector2 Center { get { return Projectile.Center; } set { Projectile.Center = value; } }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 68;
            Projectile.height = 68;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 26;
            Projectile.extraUpdates = 1;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public NPC target = null;
        public bool spawnSeg = true;
        public List<WyrmSeg> segs;
        public float CrackCd => Projectile.ai[2];
        public override void AI() {
            if (Projectile.localAI[0] == 0 && !Main.dedServ) {
                for (int i = 0; i < 32; i++) {
                    //PRT_Abyssal不进常规PRT桶,EffectLoader DrawParticleEffectsAlt RT画
                    var p = PRTLoader.NewParticle<PRT_Abyssal>(Projectile.Center, CEUtils.randomPointInCircle(18), Color.White, 1f);
                    p.Opacity = Main.rand.NextFloat(0.35f, 0.7f);  //Opacity旧初始化器字段,Configure管不了
                    p.vd = 0.9f;
                }
            }
            Player player = Projectile.GetOwner();
            if (target == null) {
                Vector2 t = player.Center + new Vector2(0, -120);
                if (CEUtils.getDistance(t, Projectile.Center) > 300) {
                    Projectile.velocity *= 0.96f;
                    Projectile.velocity += (t - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.6f;
                }
            }
            else {
                AttackTarget(target);
            }
            if (spawnSeg) {
                spawnSeg = false;
                segs = new List<WyrmSeg>();
                iWyrmSeg seg = this;
                List<int> spacings = new List<int>() { 34, 46, 34, 34, 16 };
                for (int i = 0; i < 5; i++) {
                    WyrmSeg spawn = new WyrmSeg() { Center = Projectile.Center, follow = seg, rotC = 0.06f, spacing = spacings[i] };
                    segs.Add(spawn);
                    seg = spawn;
                }
            }
            Projectile.Center += Projectile.velocity;
            foreach (WyrmSeg seg in segs) {
                seg.update();
            }
            Projectile.Center -= Projectile.velocity;
            Projectile.localAI[0]++;
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 4000) {
                Projectile.Center = player.Center;
            }
            if (player.HasBuff(ModContent.BuffType<NyxolithrakenBuff>())) {
                Projectile.timeLeft = 3;
            }

            if (target == null || !target.active || target.dontTakeDamage || CEUtils.getDistance(Projectile.Center, target.Center) > 3000) {
                target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 2600);
            }
            if (player.MinionAttackTargetNPC >= 0 && player.MinionAttackTargetNPC.ToNPC().active) {
                target = player.MinionAttackTargetNPC.ToNPC();
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        internal ref float Time => ref base.Projectile.ai[0];

        internal ref float FlyAcceleration => ref base.Projectile.ai[1];
        internal void AttackTarget(NPC target) {
            float num = 0.18f;
            Vector2 center = target.Center;
            float num2 = base.Projectile.Distance(center);
            if (base.Projectile.Distance(center) > 725f) {
                center += (Time % 30f / 30f * (MathF.PI * 2f)).ToRotationVector2() * 145f;
                num2 = base.Projectile.Distance(center);
                num *= 2.5f;
            }

            if (num2 > 1500f && Time > 45f) {
                num = MathHelper.Min(6f, FlyAcceleration + 1f);
            }

            FlyAcceleration = MathHelper.Lerp(FlyAcceleration, num, 0.3f);
            float num3 = Vector2.Dot(base.Projectile.velocity.SafeNormalize(Vector2.Zero), base.Projectile.SafeDirectionTo(center));
            if (num2 > 320f) {
                float num4 = base.Projectile.velocity.Length();
                if (num4 < 23f) {
                    num4 += 0.08f;
                }

                if (num4 > 32f) {
                    num4 -= 0.08f;
                }

                if (num3 < 0.85f && num3 > 0.5f) {
                    num4 += 6f;
                }

                if (num3 < 0.5f && num3 > -0.7f) {
                    num4 -= 10f;
                }

                num4 = MathHelper.Clamp(num4, 16f, 34f);
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(base.Projectile.AngleTo(center), FlyAcceleration * 1.3f).ToRotationVector2() * num4;
            }
            Projectile.ai[2]--;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<LifeOppress>(600);
            target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
            if (CrackCd <= 0) {
                Projectile.ai[2] = 28;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 300, Projectile.velocity.SafeNormalize(Vector2.UnitX), ModContent.ProjectileType<NxCrack>(), Projectile.damage / 2, 0, Projectile.owner);
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            foreach (WyrmSeg seg in segs) {
                if (seg.Center.getRectCentered(36, 36).Intersects(targetHitbox)) {
                    return true;
                }
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
        public Texture2D tex => Projectile.GetTexture();
        public override bool PreDraw(ref Color lightColor) {
            Rectangle head = new Rectangle(278, 0, 80, 80);
            Vector2 ohead = new Vector2(46, 54);
            Rectangle seg1 = new Rectangle(190, 0, 86, 80);
            Vector2 oseg1 = new Vector2(48, 54);
            Rectangle seg2 = new Rectangle(128, 0, 60, 80);
            Vector2 oseg2 = new Vector2(36, 54);
            Rectangle seg3 = new Rectangle(90, 0, 36, 80);
            Vector2 oseg3 = new Vector2(18, 54);
            Rectangle seg4 = new Rectangle(50, 0, 38, 80);
            Vector2 oseg4 = new Vector2(18, 54);
            Rectangle tail = new Rectangle(0, 0, 48, 80);
            Vector2 otail = new Vector2(48, 54);

            DrawSeg(Center, head, rot, ohead, lightColor);
            DrawSeg(segs[0].Center, seg1, segs[0].rot, oseg1, lightColor);
            DrawSeg(segs[1].Center, seg2, segs[1].rot, oseg2, lightColor);
            DrawSeg(segs[2].Center, seg3, segs[2].rot, oseg3, lightColor);
            DrawSeg(segs[3].Center, seg4, segs[3].rot, oseg4, lightColor);
            DrawSeg(segs[4].Center, tail, segs[4].rot, otail, lightColor);

            return false;
        }
        public Texture2D texGlow => CEUtils.getExtraTex("NxDragonGlow");
        public void DrawSeg(Vector2 pos, Rectangle frame, float rot, Vector2 origin, Color color) {
            if (Projectile.velocity.X > 0) {
                Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color, rot, origin, Projectile.scale, SpriteEffects.None);
                Main.EntitySpriteDraw(texGlow, pos - Main.screenPosition, frame, Color.White, rot, origin, Projectile.scale, SpriteEffects.None);
            }
            else {
                Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color, rot, new Vector2(origin.X, tex.Height - origin.Y), Projectile.scale, SpriteEffects.FlipVertically);
                Main.EntitySpriteDraw(texGlow, pos - Main.screenPosition, frame, Color.White, rot, new Vector2(origin.X, tex.Height - origin.Y), Projectile.scale, SpriteEffects.FlipVertically);
            }
        }
    }
}

