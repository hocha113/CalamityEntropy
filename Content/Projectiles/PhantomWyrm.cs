using CalamityEntropy.Content.Buffs.Wyrm;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public interface iWyrmSeg
    {
        public float rot { get; set; }
        public Vector2 Center { get; set; }
    }
    public class WyrmSeg : iWyrmSeg
    {
        public float rot { get; set; }
        public Vector2 Center { get; set; }
        public iWyrmSeg follow;
        public int spacing = 48;
        public float rotC = 0.14f;
        public bool AlwaysFollow = true;
        public void update()
        {
            if (follow == null)
                return;
            this.rot = (follow.Center - this.Center).ToRotation();
            if(rotC > 0)
                this.rot = CEUtils.RotateTowardsAngle(this.rot, follow.rot, rotC, false);
            if(AlwaysFollow || CEUtils.getDistance(Center, follow.Center) > spacing)
                this.Center = follow.Center - this.rot.ToRotationVector2() * spacing;

        }
    }
    public class PhantomWyrm : ModProjectile, iWyrmSeg
    {
        public float rot { get { return Projectile.rotation; } set { this.rot = value; } }
        public Vector2 Center { get { return Projectile.Center; } set { Projectile.Center = value; } }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 224;
            Projectile.height = 224;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 3;
            Projectile.MaxUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        float alpha = 0;
        public bool spawnSeg = true;
        public List<WyrmSeg> segs;
        public override void AI()
        {
            if (spawnSeg)
            {
                spawnSeg = false;
                segs = new List<WyrmSeg>();
                iWyrmSeg seg = this;
                for (int i = 0; i < 32; i++)
                {
                    WyrmSeg spawn = new WyrmSeg() { Center = Projectile.Center, follow = seg };
                    segs.Add(spawn);
                    seg = spawn;
                }
            }
            foreach (WyrmSeg seg in segs)
            {
                seg.update();
            }
            var player = Projectile.GetOwner();
            if (player.HasBuff<WyrmPhantom>())
            {
                if (alpha < 1)
                {
                    alpha += 0.01f;
                }
            }
            else
            {
                if (alpha > 0)
                {
                    alpha -= 0.01f;
                }
            }
            if (alpha > 0)
            {
                Projectile.timeLeft = 3;
            }
            if (target == null || !target.active || !target.CanBeChasedBy(Projectile))
            {
                target = CEUtils.findTarget(player, Projectile, 3600, false);
            }
            if (target == null)
            {
                Vector2 t = player.Center + new Vector2(0, -120);
                if (CEUtils.getDistance(t, Projectile.Center) > 300)
                {
                    Projectile.velocity *= 0.98f;
                    Projectile.velocity += (t - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.33f;
                }
            }
            else
            {
                AttackTarget(target);
            }
            if (player.MinionAttackTargetNPC >= 0 && player.MinionAttackTargetNPC.ToNPC().active)
            {
                target = player.MinionAttackTargetNPC.ToNPC();
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        internal ref float Time => ref base.Projectile.ai[0];

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
            float num3 = Vector2.Dot(base.Projectile.velocity.SafeNormalize(Vector2.Zero), base.Projectile.SafeDirectionTo(center));
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

        }
        public NPC target = null;
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D head = CEUtils.getExtraTex("pw_head");
            Texture2D tail = CEUtils.getExtraTex("pw_tail");
            Texture2D body1 = CEUtils.getExtraTex("pw_body");
            Texture2D body2 = CEUtils.getExtraTex("pw_bodyalt");
            //Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(head, Projectile.Center - Main.screenPosition, null, Color.White * 0.8f * alpha, Projectile.rotation, head.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            for (int i = 0; i < segs.Count; i++)
            {
                Texture2D draw = tail;
                if (i < segs.Count - 1)
                {
                    draw = (i % 2 == 0) ? body1 : body2;
                }
                Main.spriteBatch.Draw(draw, segs[i].Center - Main.screenPosition, null, Color.White * 0.8f * alpha, segs[i].rot, draw.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            }
            //Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

}