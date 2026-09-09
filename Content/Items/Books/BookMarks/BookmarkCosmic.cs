using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkCosmic : BookMark
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = CECal.RarityCosmicPurple(ModContent.RarityType<AbyssalBlue>());
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Cosmic");
        public override EBookProjectileEffect getEffect()
        {
            return new BMCosmicEffect();
        }

        public override Color tooltipColor => Color.Blue;
        private static int projType = -1;
        public static int ProjType { get { if (projType == -1) { projType = ModContent.ProjectileType<GodSlayer>(); } return projType; } }
    }

    public class BMCosmicEffect : EBookProjectileEffect
    {
        public override void BookUpdate(Projectile projectile, bool ownerClient)
        {
            if (ownerClient && CECooldowns.CheckCD("BMCosmic", 120))
            {
                if (projectile.ModProjectile is EntropyBookHeldProjectile eb)
                {
                    float rot = CEUtils.randomRot();
                    NPC target = CEUtils.FindTarget_HomingProj(projectile, Main.MouseWorld, 4000);
                    Vector2 spos = Main.MouseWorld + CEUtils.randomPointInCircle(320);
                    if (target != null)
                    {
                        spos = target.Center + CEUtils.randomRot().ToRotationVector2() * 460;
                        rot = (target.Center - spos).ToRotation();
                    }
                    eb.ShootSingleProjectile(BookmarkCosmic.ProjType, spos, rot.ToRotationVector2(), 1, 1, 1f, fixedBaseDamage: 500);
                }
            }
        }
    }
    public class GodSlayer : EBookBaseProjectile, iWyrmSeg
    {
        public float rot { get { return Projectile.rotation; } set { Projectile.rotation = value; } }
        public Vector2 Center { get { return Projectile.Center; } set { Projectile.Center = value; } }
        public override void ApplyHoming()
        {
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100 * 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.extraUpdates = 3;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public NPC target = null;
        public bool spawnSeg = true;
        public List<WyrmSeg> segs;
        public bool portalParticle = true;
        public override void OnKill(int timeLeft)
        {
        }
        public int DashTime = 56;
        public override void AI()
        {
            if (Projectile.timeLeft > 8)
            {
                for (float i = 0; i < 1; i += 0.1f)
                {
                    int d = Dust.NewDust(Projectile.position - Projectile.velocity * i, Projectile.width, Projectile.height, DustID.CorruptTorch);
                    if (d < 6000)
                    {
                        Dust dust = Main.dust[d];
                        dust.scale = 0.4f;
                        dust.velocity = Projectile.velocity * 0.4f * Main.rand.NextFloat();
                    }
                }
            }
            if (Projectile.timeLeft == 16)
            {
                Projectile.velocity = Projectile.velocity.normalize() * 22;
                //PortalParticle Lifetime=-1永生,跟着射弹timeLeft死
                PRTLoader.NewParticle<PRT_PortalParticle>(Projectile.Center + Projectile.velocity * 7, Vector2.Zero, new Color(120, 120, 255), 1.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
            }
            if (portalParticle)
            {
                var sound = new SoundStyle("CalamityEntropy/Assets/Sounds/DevourerDeathImpact");
                sound.Pitch = 0f;
                sound.Volume = 0.5f;
                SoundEngine.PlaySound(sound, Projectile.Center);


                portalParticle = false;
                PRTLoader.NewParticle<PRT_PortalParticle>(Projectile.Center, Vector2.Zero, new Color(120, 120, 255), 1.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
            }
            Player player = Projectile.GetOwner();
            if (DashTime-- > 0 || Projectile.timeLeft <= 20)
            {
            }
            else
            {
                if (target == null)
                {
                    Vector2 t = player.Center + new Vector2(0, -120);
                    if (CEUtils.getDistance(t, Projectile.Center) > 300)
                    {
                        Projectile.velocity *= 0.96f;
                        Projectile.velocity += (t - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.6f;
                    }
                }
                else
                {
                    if (Projectile.Entropy().counter % 16 == 0)
                    {
                        if (Projectile.owner == Main.myPlayer)
                        {
                            int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, (target.Center - Projectile.Center).normalize() * 12, 88, Projectile.damage, Projectile.knockBack, Projectile.owner);
                            p.ToProj().usesLocalNPCImmunity = true;
                            p.ToProj().localNPCHitCooldown = 16;
                        }

                        SoundEngine.PlaySound(SoundID.Item12 with { Pitch = 0.6f, Volume = 0.6f }, Projectile.Center);
                    }
                    AttackTarget(target);
                }
            }
            if (spawnSeg)
            {
                spawnSeg = false;
                segs = new List<WyrmSeg>();
                iWyrmSeg seg = this;
                List<int> spacings = new List<int>() { 32, 36, 36, 36, 36, 36, 26 };
                for (int i = 0; i < spacings.Count; i++)
                {
                    WyrmSeg spawn = new WyrmSeg() { Center = Projectile.Center, follow = seg, rotC = 0.06f, spacing = spacings[i] };
                    segs.Add(spawn);
                    seg = spawn;
                }
            }
            Projectile.Center += Projectile.velocity;
            foreach (WyrmSeg seg in segs)
            {
                seg.update();
            }
            Projectile.Center -= Projectile.velocity;
            Projectile.localAI[0]++;

            if (target == null || !target.active || target.dontTakeDamage || CEUtils.getDistance(Projectile.Center, target.Center) > 3000)
            {
                target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 3200);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        internal ref float Time => ref base.Projectile.ai[0];
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitNPC(target, ref modifiers);
            modifiers.ArmorPenetration += 64;
            if (DashTime > 0)
            {
                modifiers.SourceDamage *= 5;
                modifiers.SetCrit();
            }
        }
        internal ref float FlyAcceleration => ref base.Projectile.ai[1];
        internal void AttackTarget(NPC target)
        {
            float num = 0.18f;
            Vector2 center = target.Center;
            float num2 = base.Projectile.Distance(center);
            if (base.Projectile.Distance(center) > 725f)
            {
                center += (Time % 30f / 30f * (MathF.PI * 2f)).ToRotationVector2() * 145f;
                num2 = base.Projectile.Distance(center);
                num *= 2.5f;
            }

            if (num2 > 1500f && Time > 45f)
            {
                num = MathHelper.Min(6f, FlyAcceleration + 1f);
            }

            FlyAcceleration = MathHelper.Lerp(FlyAcceleration, num, 0.3f);
            float num3 = Vector2.Dot(base.Projectile.velocity.SafeNormalize(Vector2.Zero), Projectile.SafeDirectionTo(center));
            if (num2 > 320f)
            {
                float num4 = base.Projectile.velocity.Length();
                if (num4 < 23f)
                {
                    num4 += 0.08f;
                }

                if (num4 > 32f)
                {
                    num4 -= 0.08f;
                }

                if (num3 < 0.85f && num3 > 0.5f)
                {
                    num4 += 6f;
                }

                if (num3 < 0.5f && num3 > -0.7f)
                {
                    num4 -= 10f;
                }

                num4 = MathHelper.Clamp(num4, 16f, 34f);
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(base.Projectile.AngleTo(center), FlyAcceleration * 1.3f).ToRotationVector2() * num4;
            }
            Projectile.ai[2]--;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 320);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            foreach (WyrmSeg seg in segs)
            {
                if (seg.Center.getRectCentered(42, 42).Intersects(targetHitbox))
                {
                    return true;
                }
            }
            return null;
        }
        public Texture2D tex => Projectile.GetTexture();
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D s1 = Projectile.GetTexture();
            Texture2D s2 = this.getTextureAlt();
            Texture2D s3 = this.getTextureAlt("Alt2");
            float rotationAddition = -MathHelper.PiOver2;
            lightColor *= Projectile.Opacity;
            if (segs != null)
            {
                for (int i = 0; i < segs.Count; i++)
                {
                    if (segs.Count - i < Projectile.timeLeft)
                    {
                        Texture2D tex = i == segs.Count - 1 ? s3 : s2;

                        iWyrmSeg seg = segs[i];
                        Main.EntitySpriteDraw(tex, seg.Center - Main.screenPosition, null, lightColor, seg.rot + rotationAddition, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
                    }
                }
            }
            if (Projectile.timeLeft > 7)
                Main.EntitySpriteDraw(s1, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + rotationAddition, s1.Size() / 2f, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
