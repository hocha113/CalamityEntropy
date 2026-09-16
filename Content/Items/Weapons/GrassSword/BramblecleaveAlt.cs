using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.GrassSword
{
    public class BramblecleaveAlt : ModProjectile
    {
        //剑体贴图,加载期由 VaultLoaden 赋值;拖尾着色器读共享基座 CEEffectAssets
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/GrassSword/SwordTex")]
        internal static Asset<Texture2D> SwordTex;
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/GrassSword/Vine";
        public override void SetDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 36;
            Projectile.MaxUpdates = 4;
            Projectile.width = Projectile.height = 80;
        }
        public bool MouseRight = true;
        public bool MouseLeft = false;
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(MouseRight);
            writer.Write(MouseLeft);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            MouseRight = reader.ReadBoolean();
            MouseLeft = reader.ReadBoolean();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("GrassSwordHit" + Main.rand.Next(4).ToString(), 1.4f, target.Center, 16, CEUtils.WeapSound * 0.7f);

            float sparkCount = 20;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = Projectile.velocity * 0.6f + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(8, 20);
                int sparkLifetime2 = 16;
                float sparkScale2 = 0.7f;
                sparkScale2 *= (1 + Bramblecleave.GetLevel() * 0.05f);
                Color sparkColor2 = Color.Lerp(Color.Green, Color.LightGreen, Main.rand.NextFloat());

                //AltSpark CalamityPorts,Configure(false,life)对齐旧版粒子系统
                //EParticle→PRT,草剑alt命中spark迁移没改数值
                PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
            }

        }
        public Vector2 LerpCenter = Vector2.Zero;
        public override void AI() {
            if (Projectile.GetOwner().dead) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 5;
            var player = Projectile.GetOwner();

            if (Projectile.localAI[2]++ == 0) {
                LerpCenter = Projectile.Center;
                Projectile.scale = 1.3f + 0.1f * Bramblecleave.GetLevel();
            }
            counter++;
            Projectile.netUpdate = true;
            player.Entropy().BrambleBarCharge -= 0.0002f;
            if (Main.myPlayer == Projectile.owner) {
                MouseLeft = Main.mouseLeft;
                MouseRight = Main.mouseRight;
                if (player.Entropy().BrambleBarCharge <= 0) {
                    MouseRight = false;
                }
            }

            player.itemTime = player.itemAnimation = 10;
            if (counter > 20 && !MouseRight) {
                Projectile.velocity += (player.Center - Projectile.Center).normalize() * 1.3f;
                Projectile.velocity *= 0.94f;
                LerpCenter = Vector2.Lerp(LerpCenter, (player.Center + Projectile.Center) / 2f, 0.1f);

                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (Projectile.Center - player.Center).ToRotation(), 0.08f, false);
                if (CEUtils.getDistance(Projectile.Center, player.Center) < Projectile.velocity.Length() * 2.5f + 60) {
                    Projectile.Kill();
                }
            }
            else {
                if (MouseLeft) {
                    if (counter % 32 == 0) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * 16, ModContent.ProjectileType<BrambleShoot>(), (int)(Projectile.damage * 0.1f), 2, Projectile.owner);
                    }
                    Projectile.velocity *= 0.88f;

                    Projectile.velocity += (player.mouseWorld() - Projectile.Center).normalize() * 0.96f;
                    Projectile.rotation += 0.14f;
                    LerpCenter = Vector2.Lerp(LerpCenter, (player.Center + Projectile.Center) / 2f, 0.01f);
                }
                else {
                    var target = CEUtils.FindTarget_HomingProj(Projectile, player.mouseWorld(), 400);
                    if (target != null) {
                        if (CEUtils.getDistance(Projectile.Center, target.Center) > 280) {
                            Projectile.velocity *= 0.8f;
                            Projectile.velocity += (target.Center - Projectile.Center).normalize() * 8;
                        }
                        else {
                            if (Projectile.velocity.Length() < 25) {
                                Projectile.velocity = Projectile.velocity.normalize() * 25;
                            }
                        }
                    }
                    else {
                        Projectile.velocity *= 0.98f;
                        if (CEUtils.getDistance(player.mouseWorld(), Projectile.Center) > 60) {
                            Projectile.velocity += (player.mouseWorld() - Projectile.Center).normalize();
                        }
                    }
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    LerpCenter = Vector2.Lerp(LerpCenter, Projectile.Center - Projectile.rotation.ToRotationVector2() * 500, 0.04f);

                }
            }
            player.SetHandRot((Projectile.Center - player.Center).ToRotation());
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            player.heldProj = Projectile.whoAmI;
            if (odp.Count > 36) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
            trailAlpha = float.Lerp(trailAlpha, MouseLeft ? 0.6f : 0, 0.1f);
        }
        public List<Vector2> odp = new List<Vector2>();
        public float counter { get { return Projectile.ai[0]; } set { Projectile.ai[0] = value; } }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return projHitbox.Center.ToVector2().getRectCentered(140 * Projectile.scale, 140 * Projectile.scale).Intersects(targetHitbox);
        }
        public override bool PreDraw(ref Color lightColor) {
            Player player = Projectile.GetOwner();
            int Count = 46;

            Texture2D draw = this.getTextureAlt("Alt2");
            Vector2 last = Projectile.GetOwner().GetDrawCenter();
            for (int i = 1; i < Count; i++) {
                Vector2 pos = CEUtils.Bezier(new List<Vector2>() { Projectile.GetOwner().GetDrawCenter(), LerpCenter, Projectile.Center }, (float)i / Count);
                Main.EntitySpriteDraw(draw, pos - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.25f), (pos - last).ToRotation(), new Vector2(0, draw.Height / 2), 1, SpriteEffects.None);
                last = pos;
            }
            Texture2D trail = CEExtraAssets.MotionTrail2;
            List<ColoredVertex> ve = new List<ColoredVertex>();

            for (int i = 0; i < odr.Count; i++) {
                Color b = new Color(220, 255, 200);
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (new Vector2(62 * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition,
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = CEEffectAssets.SwordTrail;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue((new Color(90, 255, 90)).ToVector4());
                shader.Parameters["color1"].SetValue((new Color(60, 200, 60)).ToVector4());
                shader.Parameters["alpha"].SetValue(trailAlpha);
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                trail = CEExtraAssets.SplitTrail;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D sword = SwordTex.Value;
            Main.EntitySpriteDraw(sword, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.6f), Projectile.rotation + MathHelper.PiOver4, sword.Size() / 2f, 1.6f + 0.1f * Bramblecleave.GetLevel(), SpriteEffects.None);

            return false;
        }
        List<float> odr = new List<float>();
        public float trailAlpha = 0;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (!MouseLeft) {
                modifiers.SourceDamage *= 1.0f;
            }
        }
    }
}
