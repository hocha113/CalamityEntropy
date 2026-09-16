using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Chainsaw
{
    public class BrokenChainsaw0 : ModProjectile
    {
        //链锯帧动画数组(序号 0 起),加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Chainsaw/BrokenChainsaw", 0, 3, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }
        public int frame = 2;
        public bool Hitted = false;
        public int Target = -1;
        public float Counter { get { return Projectile.localAI[1]; } set { Projectile.localAI[1] = value; } }
        public float Rotation = 0;
        public float sAlpha = 0;
        public float rVel = 0;
        public override bool? CanDamage() {
            return Counter <= cutTime;
        }
        public int cutTime => 80;
        public override void AI() {
            var player = Projectile.GetOwner();
            player.Entropy().MouseWorldListener = true;
            if (Projectile.localAI[0]++ == 0) {
                CEUtils.PlaySound("HellkiteSwing2", Main.rand.NextFloat(1.4f, 1.7f), Projectile.Center, 8, CEUtils.WeapSound * 0.4f);
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_ * 1.5f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            Projectile.Center = player.MountedCenter;
            Projectile.velocity = Projectile.velocity.Length() * (player.Entropy().MouseWorld - Projectile.Center).normalize();
            player.heldProj = Projectile.whoAmI;


            int MaxTime = (int)(22 / player.GetTotalAttackSpeed(Projectile.DamageType));
            float p = Projectile.localAI[0] / MaxTime;
            bool CollideTarget() {
                NPC npc = Target >= 0 ? Target.ToNPC() : null;
                if (npc == null)
                    return false;
                return Projectile.Colliding(Projectile.getRect(), npc.Hitbox);
            }
            float cr = 5.4f;
            if (Target >= 0 && !Target.ToNPC().active)
                Target = -1;
            if (Hitted) {
                Counter++;
                player.itemTime = player.itemAnimation = 3;
                if (Counter < cutTime) {
                    if (CollideTarget()) {
                        for (int i = 0; i < 20; i++) {
                            Rotation -= 0.025f;
                            Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                            if (!CollideTarget())
                                break;
                        }
                        Rotation += 0.175f;
                    }
                    else {
                        for (int i = 0; i < 20; i++) {
                            Rotation += 0.025f;
                            Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                            if (CollideTarget())
                                break;
                        }
                        Rotation += 0.15f;
                    }
                }
                if (Counter > cutTime) {
                    Rotation += 0.5f;
                }
                Rotation += rVel;
                rVel *= 0.7f;
                Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                if (Rotation > cr / 2)
                    Projectile.Kill();
            }
            else {
                Rotation = (CEUtils.Parabola(p * 0.5f, 1) - 0.5f) * cr;
                Projectile.rotation = Projectile.velocity.ToRotation() + Rotation * dir;
                sAlpha = 1 - CEUtils.Parabola(p, 1);
                sAlpha = 1 - (sAlpha * sAlpha);
                if (p >= 1)
                    Projectile.Kill();
            }

            float r = Projectile.rotation;
            if (r.ToRotationVector2().X > 0) {
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, r - (float)(Math.PI * 0.5f));
            }
            else {
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, r - (float)(Math.PI * 0.5f));
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.GetOwner().Center + Projectile.rotation.ToRotationVector2() * 20 * Projectile.scale, Projectile.GetOwner().Center + Projectile.rotation.ToRotationVector2() * 76 * Projectile.scale, targetHitbox, 40);
        }
        public int dir => (Projectile.velocity.X > 0 ? 1 : -1);
        public Vector2 heldOrigin => new Vector2(-26, 0 * dir);
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (!target.boss)
                target.velocity *= 0.1f;
            bool CollideTarget() {
                NPC npc = Target >= 0 ? Target.ToNPC() : null;
                if (npc == null)
                    return false;
                return Projectile.Colliding(Projectile.getRect(), npc.Hitbox);
            }
            if (this.Target == -1 || !CollideTarget()) {
                this.Target = target.whoAmI;
                CEUtils.PlaySound("chainsaw_break", 1.4f, Projectile.Center);
            }
            CalamityEntropy.Instance.screenShakeAmp = 1.2f;
            Hitted = true;
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1.2f, 1.6f), target.Center, 4, CEUtils.WeapSound * 0.8f);
            for (int i = 0; i < 9; i++) {
                Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * CEUtils.getDistance(Projectile.Center, target.Center) + (Projectile.rotation + MathHelper.PiOver2 * dir).ToRotationVector2() * 6 * Projectile.scale;
                Vector2 vel = Projectile.rotation.ToRotationVector2().RotatedByRandom(0.07f) * Main.rand.NextFloat(10, 70);
                Color color = Main.rand.NextBool() ? Color.Orange : Color.Firebrick;
                float scale = Main.rand.NextFloat(0.4f, 2.6f);
                if (Main.rand.NextBool()) {
                    //LineCal/SparkCal是CalamityPorts,Configure(false,lifetime)对齐Calamity原构造
                    PRTLoader.NewParticle<PRT_LineCal>(pos, vel, color, scale).Configure(false, Main.rand.Next(3, 9));
                }
                else {
                    PRTLoader.NewParticle<PRT_SparkCal>(pos, vel, color, scale).Configure(false, Main.rand.Next(3, 9));
                }
            }
        }
        public override bool PreDraw(ref Color dc) {
            Texture2D tx = Frames[((int)(Projectile.ai[0] / 4)) % frame];
            Main.spriteBatch.Draw(tx, Projectile.Center + CEUtils.randomPointInCircle((Hitted && Counter < cutTime) ? 8 : 0) - Main.screenPosition, null, dc * Projectile.Opacity, Projectile.rotation, tx.Size() * 0.5f + heldOrigin, Projectile.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            return false;
        }

        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 78 * Projectile.scale, 50, DelegateMethods.CutTiles);
        }
    }
}