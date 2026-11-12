using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.Constellation
{
    //ai[0] 目标 NPC 索引, -1 为无; 主人端选目标并同步, 运动照搬原版夜光(931): 先漂移后平滑追踪
    public class ConstellationStar : ModProjectile
    {
        //时间单位为更新(extraUpdates 1, 每帧 2 次), 距离单位 px, 速度单位 px/更新
        public const int Life = 240;
        public const int DriftEnd = 180;
        public const int HomeFloor = 30;
        public const int RetargetInterval = 8;
        public const float LaunchSpeed = 5f;
        public const float HomeSpeed = 14f;
        public const float SearchRange = 900f;
        public const float Drag = 0.97f;

        public static readonly Color BodyColor = new(170, 210, 255);
        public static readonly Color GlowColor = new(100, 140, 255);

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FairyQueenMagicItemShot;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = Life;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            NPC target = null;
            int index = (int)Projectile.ai[0];
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].CanBeChasedBy(Projectile))
                target = Main.npc[index];
            if (target == null && Projectile.owner == Main.myPlayer && Projectile.timeLeft % RetargetInterval == 0) {
                target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, SearchRange);
                float next = target?.whoAmI ?? -1;
                if (next != Projectile.ai[0]) {
                    Projectile.ai[0] = next;
                    Projectile.netUpdate = true;
                }
            }

            if (Projectile.timeLeft > DriftEnd) {
                float wave = (float)Math.Cos(Projectile.identity % 6 / 6f + Projectile.position.X / 320f + Projectile.position.Y / 160f);
                Projectile.velocity *= Drag;
                Projectile.velocity = Projectile.velocity.RotatedBy(wave * MathHelper.TwoPi * 0.125f / 30f);
            }
            else if (target != null) {
                float amount = MathHelper.Lerp(0.075f, 0.125f, Utils.GetLerpValue(DriftEnd, 30f, Projectile.timeLeft, true));
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.DirectionTo(target.Center) * HomeSpeed, amount);
                if (Projectile.timeLeft < HomeFloor)
                    Projectile.timeLeft = HomeFloor;
            }
            else {
                Projectile.velocity *= Drag;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(Life, Life - 20, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, HomeFloor, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.4f * Projectile.Opacity);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ || Projectile.Opacity < 0.3f)
                return;
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, BodyColor, 0.9f).Configure(GlowColor, 10, 0f, 1.6f);
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 4; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, dir.RotatedByRandom(0.6f) * Main.rand.NextFloat(4f, 8f), BodyColor, 0.04f).Configure(false, 10, new Vector2(0.3f, 1f), false, false);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 origin = tex.Size() / 2f;
            Vector2 half = Projectile.Size / 2f;
            float o = Projectile.Opacity;
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, null, (GlowColor with { A = 0 }) * 0.6f * t * o, Projectile.oldRot[i], origin, Projectile.scale * (0.5f + 0.5f * t), SpriteEffects.None);
            }
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, pos, null, (GlowColor with { A = 0 }) * 0.5f * o, 0f, glow.Size() / 2f, 0.16f, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, BodyColor * o, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, (Color.White with { A = 0 }) * 0.6f * o, Projectile.rotation + MathHelper.PiOver2, origin, Projectile.scale * 0.6f, SpriteEffects.None);
            return false;
        }
    }
}
