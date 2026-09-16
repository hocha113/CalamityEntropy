using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VoidLaser : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 8000;
        }
        public int counter = 0;
        List<Vector2> l = new List<Vector2>();
        public int length = 2000;
        public float width = 0;
        public override void SetDefaults() {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 190;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }
        public bool st = true;
        public int dmgLength = 0;
        public List<int> nlist = new List<int>();
        public bool nst = true;
        // 持续屏震：存活期间每帧刷新幅度，死亡后自然衰减
        private ScreenShaker.ScreenShake shake = null;
        public override void AI() {
            if (nst) {
                nst = false;
                for (int ii = 0; ii < 80; ii++) {
                    l.Add(new Vector2(24, Main.rand.Next(0, 21) - 10));
                    l.Add(new Vector2(57, Main.rand.Next(0, 21) - 10));
                    for (int i = 0; i < l.Count; i++) {
                        l[i] = l[i] + new Vector2(66, 0);
                    }

                }
            }
            if (!Main.dedServ) {
                float shakeAmp = Utils.Remap(Main.LocalPlayer.Distance(Projectile.Center), 1800f, 1000f, 0f, 1.5f);
                if (shake == null || !shake.active)
                    shake = ScreenShaker.AddShake(Vector2.Zero, shakeAmp);
                else
                    shake.amplitude = shakeAmp;
            }
            Projectile.Center = ((int)(Projectile.ai[2])).ToProj_Identity().Center + ((int)(Projectile.ai[2])).ToProj_Identity().rotation.ToRotationVector2() * 60;
            Projectile.rotation = ((int)(Projectile.ai[2])).ToProj_Identity().rotation;
            Projectile.velocity = Vector2.Zero;
            if (Projectile.timeLeft < 20) {
                width -= 1f / 20f;
            }
            else {
                if (width < 1) {
                    width += 1f / 8f;
                }
            }
            Vector2 checkPos = Projectile.Center;
            dmgLength = 0;
            nlist.Clear();
            foreach (NPC n in Main.npc) {
                if (n.active && !n.friendly && !n.dontTakeDamage) {
                    if (CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 2000, n.Hitbox, 46)) {
                        nlist.Add(n.whoAmI);
                    }
                }
            }
            for (int i = 0; i < 100; i++) {
                Rectangle rect = new Rectangle((int)checkPos.X - 30, (int)checkPos.Y - 30, 60, 60);
                bool flag = true;
                foreach (int id in nlist) {
                    NPC npc = id.ToNPC();
                    if (rect.Intersects(npc.Hitbox)) {
                        flag = false;
                        break;
                    }
                }
                dmgLength += 20;
                if (!flag) {
                    dmgLength += 20;
                    break;
                }
                checkPos += Projectile.rotation.ToRotationVector2() * 20;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 12, 1, 600, 8);
            target.Entropy().vtnoparticle = target.Entropy().VoidTouchTime + 2;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * dmgLength, targetHitbox, (int)(26 * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor) {
            counter++;
            l.Add(new Vector2(24, Main.rand.Next(0, 21) - 10));
            l.Add(new Vector2(57, Main.rand.Next(0, 21) - 10));
            for (int i = 0; i < l.Count; i++) {
                l[i] = l[i] + new Vector2(66, 0);
            }

            for (int i = 0; i < l.Count; i++) {
                if (l[i].X > 2000) {
                    l.RemoveAt(i);
                    break;
                }
            }
            Texture2D tl = CEExtraAssets.cllight;

            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D ball = CEUtils.getExtraTex("vlball");
            Texture2D laser = CEUtils.getExtraTex("VoidLaser");

            float dw = 1f + ((float)Math.Cos((float)counter / 5f) * 0.16f) * Projectile.scale;
            sb.Draw(laser, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, dmgLength, laser.Height), Color.Blue * 0.85f, Projectile.rotation, new Vector2(0, laser.Height / 2), new Vector2(1, dw * 1.1f * width), SpriteEffects.None, 0);
            sb.Draw(laser, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, dmgLength, laser.Height), Color.White, Projectile.rotation, new Vector2(0, laser.Height / 2), new Vector2(1, dw * width), SpriteEffects.None, 0);

            sb.Draw(ball, Projectile.Center - Main.screenPosition, null, Color.Blue * 0.86f, Projectile.rotation, ball.Size() / 2, dw * 1.3f * width, SpriteEffects.None, 0);
            sb.Draw(ball, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, ball.Size() / 2, dw * 1.2f * width, SpriteEffects.None, 0);

            foreach (Vector2 ps in l) {
                if (ps.X < dmgLength - 20) {
                    Main.spriteBatch.Draw(tl, Projectile.Center + (ps * new Vector2(1, width)).RotatedBy(Projectile.rotation) - Main.screenPosition, null, new Color(0, 0, 255), Projectile.rotation, tl.Size() / 2, new Vector2(1f, 1.5f * dw * width), SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(tl, Projectile.Center + (ps * new Vector2(1, width)).RotatedBy(Projectile.rotation) - Main.screenPosition, null, new Color(0, 0, 255) * 0.1f, Projectile.rotation, tl.Size() / 2, new Vector2(1f, 1.5f * dw * width), SpriteEffects.None, 0);

                }
            }
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D lend = CEExtraAssets.vlend;
            sb.Draw(lend, Projectile.Center + Projectile.rotation.ToRotationVector2() * (dmgLength - 16) - Main.screenPosition, CEUtils.GetCutTexRect(lend, 4, (int)(counter * 0.5f) % 4), Color.White, Projectile.rotation + (float)Math.PI / 2f, new Vector2(64, 52) / 2, Projectile.scale * 2, SpriteEffects.None, 0);

            return false;
        }
    }

}