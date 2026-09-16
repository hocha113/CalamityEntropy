using CalamityEntropy.Common;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class PenMinion : ModProjectile
    {
        //蓄力光柱背景贴图,加载期就位,不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Extra/lbg")]
        internal static Asset<Texture2D> LbgTex;
        private List<Vector2> odp = new List<Vector2>();
        private List<float> odi = new List<float>();
        private Player target;
        private Vector2 tpos;
        private int notargettime = 0;
        private int xz = 0;
        private bool nts = false;
        private int lbg = -1;
        private Vector2 centerd;
        private float rotationd;
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.width = Projectile.height = 64;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI() {
            if (!Projectile.GetOwner().mount.Active && Projectile.GetOwner().miscEquips[3].type == ModContent.ItemType<TheReplicaofThePen>()) {
                Projectile.timeLeft = 3;
            }
            else {
                Projectile.Kill();
            }
            if (lbg >= 0) {
                lbg += 1;
                if (lbg > 30) {
                    lbg = -1;
                }
            }
            Projectile.ai[0] += 1;
            Vector2 currentPosition = Projectile.Center;
            odp.Add(currentPosition + (new Vector2(25, -25).RotatedBy(Projectile.rotation)));
            odi.Add(Projectile.rotation);
            if (odp.Count > 40) {
                odi.RemoveAt(0);
                odp.RemoveAt(0);
            }
            var target = CEUtils.FindTarget_HomingProj(Projectile, currentPosition, 3000);
            if (target != null) {
                if ((xz == 0 && CEUtils.getDistance(target.Center, currentPosition) < 160) || (xz == 1 && CEUtils.getDistance(target.Center, Projectile.Center) < 140) || (xz == 2 && CEUtils.getDistance(target.Center, Projectile.Center) < 260)) {
                    if (!nts) {
                        if (xz == 2) {
                            centerd = Projectile.Center;
                            rotationd = (Projectile.rotation - ((float)(Math.PI / 4)));
                            lbg = 0;
                        }
                    }
                    tpos = target.Center;
                    if (xz == 0) {
                        Projectile.velocity = Projectile.rotation.ToRotationVector2() * 16;
                        Projectile.rotation += 0.14f;
                    }
                    if (xz == 1) {
                        Projectile.velocity = (Projectile.rotation + ((float)(Math.PI / 4 + Math.PI))).ToRotationVector2() * 20;
                        Projectile.rotation -= 0.1f;
                    }
                    if (xz == 2) {
                        Projectile.velocity = ((Projectile.rotation - ((float)(Math.PI / 4))).ToRotationVector2() * 26);
                        if (lbg < 0) {
                            lbg = 0;
                        }

                    }
                    nts = true;
                }
                else {
                    Random random = new Random();
                    if (nts) {
                        xz = random.Next(0, 3);
                        if (xz == 2) {
                            Projectile.rotation = (target.Center - Projectile.Center).ToRotation() + ((float)(Math.PI / 4));
                        }
                        nts = false;
                    }
                    if (xz == 0) {
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation() - ((float)(Math.PI / 4)), 10f.ToRadians());
                        Projectile.velocity += (Projectile.rotation + ((float)(Math.PI / 4))).ToRotationVector2() * 3.8f;
                    }
                    if (xz == 1) {
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation() - ((float)(Math.PI / 4 + Math.PI)), 10f.ToRadians());
                        Projectile.velocity += (Projectile.rotation + ((float)(Math.PI / 4 + Math.PI))).ToRotationVector2() * 3.8f;
                    }
                    if (xz == 2) {
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation() + ((float)(Math.PI / 4)), 30f.ToRadians());
                        Projectile.velocity += (Projectile.rotation - ((float)(Math.PI / 4))).ToRotationVector2() * 3.8f;
                    }
                    Projectile.velocity *= 0.94f;
                }
            }
            else {
                Projectile.rotation = MathHelper.PiOver4 + MathHelper.PiOver2;
                Projectile.Center += ((Projectile.GetOwner().Center + new Vector2(-60 * Projectile.GetOwner().direction, -20)) - Projectile.Center) * 0.1f;
                Projectile.velocity *= 0.6f;
            }

        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 40, 1);
        }

        public override bool PreDraw(ref Color lightColor) {
            Color dc = new Color(255, 255, 255) * 0;
            float ap = 0;
            Vector2 p1;
            Vector2 p2;
            float i1;
            float i2;
            for (int j = -25; j < 25; j++) {
                for (int i = odp.Count; i > 1; i--) {
                    if (i == odp.Count) {
                        i1 = Projectile.rotation;
                        i2 = odi[i - 1];
                        p1 = Projectile.Center + (new Vector2(25, -25).RotatedBy(Projectile.rotation)) - (new Vector2(25 - j, -25 + j).RotatedBy(i1));
                        p2 = odp[i - 1] - (new Vector2(25 - j, -25 + j).RotatedBy(i2));
                    }
                    else {
                        i1 = odi[i];
                        i2 = odi[i - 1];
                        p1 = odp[i] - (new Vector2(25 - j, -25 + j).RotatedBy(i1));
                        p2 = odp[i - 1] - (new Vector2(25 - j, -25 + j).RotatedBy(i2));
                    }
                    CEUtils.drawTexture(CEUtils.pixelTex, (p1 + p2) / 2f, (p1 - p2).ToRotation(), dc, new Vector2(CEUtils.getDistance(p1, p2), 6));
                    dc *= 0.9f;
                }
                dc = Color.Black * ((float)ap / 255f) * 0.3f;
                ap += (255f / 50f);
            }
            if (lbg >= 0) {
                float xw = lbg / 5f;
                float op = 1;
                if (lbg > 10) {
                    op = (30 - lbg) / 20f;
                }
                CEUtils.drawTexture(LbgTex.Value, centerd + rotationd.ToRotationVector2() * 200, rotationd + ((float)(Math.PI / 2f)), (new Color(0, 0, 0)) * 0.7f * op, new Vector2(xw, 9));
            }
            Projectile.rotation -= MathHelper.PiOver4;
            Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, null));
            Projectile.rotation += MathHelper.PiOver4;
            return false;
        }

    }
}