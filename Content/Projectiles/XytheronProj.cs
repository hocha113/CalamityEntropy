using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class XytheronProj : ModProjectile
    {
        public static List<float> MotifList;
        public static int soundCount = 0;
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Xytheron";
        List<float> odr = new List<float>();
        List<float> ods = new List<float>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 80;
            Projectile.timeLeft = 100000;
            Projectile.extraUpdates = 7;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(rotSpeed);
        }
        public int addcharge = 3;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<LifeOppress>(600);
            CEUtils.PlaySound("xhit", Main.rand.NextFloat(0.8f, 1.1f), Projectile.Center, 8, volume: 0.32f);
            CEUtils.PlaySound("DevourerDeathImpact", Main.rand.NextFloat(0.8f, 1f), Projectile.Center, 8, volume: 0.32f);
            CalamityEntropy.Instance.screenShakeAmp = 5;
            for (int i = 0; i < 3; i++) {
                //AbyssalLine旧版粒子系统 spawn,现走BasePRT,参数照抄
                PRTLoader.NewParticle<PRT_AbyssalLine>(target.Center, Vector2.Zero, Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());  //AbyssalLine带lifetime的Configure是CalamityPorts签名
            }
            if (Projectile.owner.ToPlayer().HeldItem.ModItem is Xytheron xr) {
                if (addcharge > 0) {
                    xr.charge += 1;
                    if (xr.charge > 20) {
                        xr.charge = 20;
                    }
                    addcharge--;
                }
            }
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            rotSpeed = reader.ReadSingle();
        }
        public float scaleD = 0.64f;
        public float rotSpeed = 0f;
        public float rotSpeedJ = 0;
        float glowalpha = 0;
        public bool playsound = true;
        public override void AI() {
            float updates = Projectile.MaxUpdates;
            if (Projectile.localAI[2]++ == 0) {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            if (Projectile.ai[0] == 0) {
                Projectile.direction = Projectile.velocity.X > 0 ? 1 : -1;
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.rotation -= 2.42f * Projectile.direction;
            }
            Player owner = Projectile.owner.ToPlayer();
            float meleeSpeed = owner.GetTotalAttackSpeed(Projectile.DamageType) * 1.56f;

            Projectile.Center = owner.MountedCenter + owner.gfxOffY * Vector2.UnitY;
            Projectile.rotation += rotSpeed * meleeSpeed * 0.32f;
            if (Projectile.ai[0] >= 74 && playsound) {
                MotifList = new List<float>() { 1, 0, 0, 0, 0.6f, 1, 0.8f, 1, 1.18f, 0, 0.9f, 1, 0, 0, 0, 0, 0, 1f, 0.9f, 0.8f, 0.7f, 0.84f, 0, 1, 0.95f, 1.05f, 1.2f, 0, 1.2f, 1.3f, 1.2f, 0, 1.1f, 1, 0.8f, 0, 1f, 0, 0.7f, 0, 0, 1.05f, 0, 1.1f, 0, 0, 1.2f, 1f, 0.9f, 0, 0, 0, 0 };
                float pitch = Main.rand.NextFloat(0.9f, 1.4f);
                if (Main.zenithWorld) {
                    pitch = MotifList[soundCount];
                    soundCount++;
                    if (soundCount >= MotifList.Count) {
                        soundCount = 0;
                    }
                }
                if (pitch > 0) {
                    CEUtils.PlaySound("xswing", pitch, Projectile.Center, 8, 0.8f);
                }
                playsound = false;
            }
            if (Projectile.ai[0] < 60 * updates) {
                Projectile.ai[0] = 60 * updates;
            }
            else {
                if (Projectile.ai[0] < 86 * updates) {
                    rotSpeed += 0.0006f * Projectile.direction * meleeSpeed;
                }
                else {
                    rotSpeed *= (float)Math.Pow(0.94, 1.0 / meleeSpeed);
                    if (Projectile.ai[0] > 86 * updates) {
                        rotSpeed *= 0.6f;
                        if (Projectile.owner == Main.myPlayer) {
                            Projectile.direction = (Main.MouseWorld - owner.Center).X > 0 ? 1 : -1;
                            float targetrot = (Main.MouseWorld - owner.Center).ToRotation() - 2.42f * Projectile.direction;
                            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetrot, 0.05f * meleeSpeed, false);
                        }
                        if (odr.Count > 0) {
                            odr.RemoveAt(0);
                            ods.RemoveAt(0);
                        }
                    }
                    if (Projectile.ai[0] > 94 * updates) {
                        owner.itemTime = 1;
                        owner.itemAnimation = 1;
                        Projectile.Kill();
                        return;
                    }
                }
            }

            if (Projectile.ai[0] > 88 * updates) {
                alpha *= 0.96f;
            }

            Projectile.ai[0] += meleeSpeed;
            odr.Add(Projectile.rotation);
            ods.Add(scaleD);
            if (odr.Count > 60) {
                odr.RemoveAt(0);
                ods.RemoveAt(0);
            }
            if (Projectile.velocity.X > 0) {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
        }
        public float alpha = 1;
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? CanHitNPC(NPC target) {
            return base.CanHitNPC(target);
        }
        public override bool PreDraw(ref Color lightColor) {
            drawSlash();
            drawSword();
            return false;
        }
        public void drawSword() {
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Player player = Main.player[Projectile.owner];
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(CEExtraAssets.StarlessNightGlow, Projectile.owner.ToPlayer().MountedCenter - Main.screenPosition, null, new Color(180, 180, 255) * glowalpha * 0.8f, Projectile.rotation + (float)Math.PI * 0.25f, new Vector2(32, 168), Projectile.scale * 3f * scaleD, SpriteEffects.None, 0);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(TextureAssets.Projectile[Projectile.type].Value, Projectile.owner.ToPlayer().MountedCenter - Main.screenPosition - Projectile.rotation.ToRotationVector2() * 8, null, Color.White, Projectile.rotation + (float)Math.PI * 0.25f, new Vector2(0, TextureAssets.Projectile[Projectile.type].Value.Height), Projectile.scale * 3f * scaleD, SpriteEffects.None, 0);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }
        public void drawSlash() {
            if (odr.Count < 2)
                return;
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Player player = Main.player[Projectile.owner];

            Texture2D tail = CEExtraAssets.Extra_201;
            Texture2D tail2 = CEExtraAssets.B1;
            Texture2D tail3 = CEExtraAssets.Noise_10;
            var r = Main.rand;
            List<float> odr_ = new List<float>();
            List<float> ods_ = new List<float>();
            for (int i = 1; i < odr.Count; i++) {
                for (float j = 0.1f; j <= 1; j += 0.1f) {
                    odr_.Add(CEUtils.RotateTowardsAngle(odr[i - 1], odr[i], j, false));
                    ods_.Add(float.Lerp(ods[i - 1], ods[i], j));
                }
            }
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<ColoredVertex> ve = new List<ColoredVertex>();
            for (int i = 0; i < odr_.Count; i++) {
                Color b = new Color(100, 100, 100) * alpha;
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(708 * ods_[i] * Projectile.scale, 0).RotatedBy(odr_[i])),
                      new Vector3((float)i / (float)odr_.Count, 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(0 * ods_[i] * Projectile.scale, 0).RotatedBy(odr_[i])),
                      new Vector3((float)i / (float)odr_.Count, 0, 1),
                      b));
            }

            if (ve.Count >= 3) {
                gd.Textures[0] = tail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                ve.Clear();
                for (int i = 0; i < odr_.Count; i++) {
                    Color b = new Color(255, 255, 255) * alpha;
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(708 * ods_[i] * Projectile.scale, 0).RotatedBy(odr_[i])),
                          new Vector3((float)i / (float)odr_.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(0 * ods_[i] * Projectile.scale, 0).RotatedBy(odr_[i])),
                          new Vector3((float)i / (float)odr_.Count, 0, 1),
                          b));
                }
                Effect shader = CEEffectAssets.SlashTrans2;

                sb.End();
                sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.GameViewMatrix.TransformationMatrix);
                shader.CurrentTechnique.Passes["EnchantedPass"].Apply();
                gd.Textures[1] = CEUtils.getExtraTex("xt_colormap");

                shader.Parameters["ofs"].SetValue(Main.GlobalTimeWrappedHourly * 1.6f);
                gd.Textures[0] = tail2;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                shader.Parameters["ofs"].SetValue(Main.GlobalTimeWrappedHourly * 3);
                gd.Textures[0] = tail3;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                sb.End();
                sb.Begin(0, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.GameViewMatrix.TransformationMatrix);

            }

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 720 * Projectile.scale * scaleD, targetHitbox, 100);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 725 * Projectile.scale * scaleD, 128, DelegateMethods.CutTiles);
        }
    }

}