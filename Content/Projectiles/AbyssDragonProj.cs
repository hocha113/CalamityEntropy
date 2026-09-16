using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AbyssDragonProj : ModProjectile, iWyrmSeg
    {
        public float rot { get { return Projectile.rotation; } set { Projectile.rotation = value; } }
        public Vector2 Center { get { return Projectile.Center; } set { Projectile.Center = value; } }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ArmorPenetration = 60;
            Projectile.localNPCHitCooldown = 8;
            Projectile.extraUpdates = 2;
        }
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 90, 3.6f, 1000, 16);
        }

        public NPC target = null;
        public bool spawnSeg = true;
        public List<WyrmSeg> segs;

        public override void AI() {
            Player player = Projectile.GetOwner();
            player.itemTime = player.itemAnimation = 3;
            if (Main.GameUpdateCount % 9 == 0) {
                if (!player.CheckMana(player.HeldItem.mana, true)) {
                    player.channel = false;
                    if (Projectile.timeLeft > 1)
                        Projectile.timeLeft = 1;
                }
                Vector2 spawnPos = player.Center + CEUtils.randomRot().ToRotationVector2() * 1400;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, (player.Entropy().MouseWorld - spawnPos).normalize() * 12, ModContent.ProjectileType<AbyssalStar>(), (int)(Projectile.damage * 0.24f), Projectile.knockBack, player.whoAmI);

            }
            player.Entropy().MouseWorldListener = true;
            if (target == null) {
                Vector2 t = player.Entropy().MouseWorld + new Vector2(0, -120);
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
                List<int> spacings = new List<int>() { 28, 30, 32, 32, 32, 34, 18 };
                for (int i = 0; i < 7; i++) {
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
            if (player.channel) {
                Projectile.timeLeft = 3;
            }

            Vector2 orgPos = Projectile.Center;
            Projectile.Center = player.Entropy().MouseWorld;
            target = null;
            foreach (NPC n in Main.ActiveNPCs) {
                if (!n.friendly && n.CanBeChasedBy(Projectile)) {
                    if (target == null || target.Distance(player.Entropy().MouseWorld) > n.Distance(player.Entropy().MouseWorld)) {
                        target = n;
                    }
                }
            }
            Projectile.Center = orgPos;

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

            FlyAcceleration = MathHelper.Lerp(FlyAcceleration, num, 0.26f);
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
            Rectangle head = new Rectangle(162, 0, 124, 80);
            Vector2 ohead = new Vector2(72, 54);
            Rectangle seg1 = new Rectangle(124, 0, 38, 80);
            Vector2 oseg1 = new Vector2(17, 44);
            Rectangle seg2 = new Rectangle(90, 0, 32, 80);
            Vector2 oseg2 = new Vector2(17, 44);
            Rectangle seg3 = seg2;
            Vector2 oseg3 = oseg2;
            Rectangle seg4 = seg3;
            Vector2 oseg4 = oseg3;
            Rectangle seg5 = seg4;
            Vector2 oseg5 = oseg4;
            Rectangle seg6 = seg1;
            Vector2 oseg6 = oseg1;
            Rectangle tail = new Rectangle(0, 0, 88, 80);
            Vector2 otail = new Vector2(84, 44);

            DrawSeg(Center, head, rot, ohead, lightColor);
            DrawSeg(segs[0].Center, seg1, segs[0].rot, oseg1, lightColor);
            DrawSeg(segs[1].Center, seg2, segs[1].rot, oseg2, lightColor);
            DrawSeg(segs[2].Center, seg3, segs[2].rot, oseg3, lightColor);
            DrawSeg(segs[3].Center, seg4, segs[3].rot, oseg4, lightColor);
            DrawSeg(segs[4].Center, seg5, segs[4].rot, oseg5, lightColor);
            DrawSeg(segs[5].Center, seg6, segs[5].rot, oseg6, lightColor);
            DrawSeg(segs[6].Center, tail, segs[6].rot, otail, lightColor);

            return false;
        }
        public void DrawSeg(Vector2 pos, Rectangle frame, float rot, Vector2 origin, Color color) {
            if (Projectile.velocity.X > 0) {
                Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color, rot, origin, Projectile.scale, SpriteEffects.None);
            }
            else {
                Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color, rot, new Vector2(origin.X, tex.Height - origin.Y), Projectile.scale, SpriteEffects.FlipVertically);
            }
        }
    }


}