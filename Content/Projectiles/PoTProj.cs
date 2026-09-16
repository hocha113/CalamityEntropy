using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PoTProj : ModProjectile
    {
        public List<float> odr = new List<float>();
        public List<float> ods = new List<float>();
        public int noSlowTime = 0;
        public float timej = 1f;
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
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 4;
            Projectile.ArmorPenetration = 80;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(rotSpeed);
            writer.Write(timej);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            rotSpeed = reader.ReadSingle();

            timej = reader.ReadSingle();
        }
        public float scaleD = 1f;
        public float rotSpeed = 0f;
        public float scale2 = 0.6f;
        public int potbuffcd = 0;
        public override void AI() {
            if (potbuffcd > 0) {
                potbuffcd--;
            }
            Player owner = Projectile.owner.ToPlayer();
            if (Main.myPlayer == Projectile.owner) {
                timej = (1f - (float)owner.Entropy().pot_amp * 0.05f);
            }
            else {
                Projectile.netUpdate = true;
            }
            if (Projectile.owner == Main.myPlayer) {
                if (Projectile.ai[0] == 0 || Projectile.ai[0] == (int)(60 * timej)) {
                    odr.Clear();

                    ods.Clear();
                    rotSpeed = 0;
                    Projectile.velocity = (Main.MouseWorld - Main.LocalPlayer.Center).SafeNormalize(Vector2.One) * 12;
                    if (Projectile.velocity.X > 0) {
                        Projectile.rotation = (Main.MouseWorld - Main.LocalPlayer.Center).ToRotation() - (float)Math.PI * 0.5f;
                    }
                    else {
                        Projectile.rotation = (Main.MouseWorld - Main.LocalPlayer.Center).ToRotation() + (float)Math.PI * 0.5f;
                    }
                    Projectile.netUpdate = true;
                }
                if (Projectile.ai[0] == (int)(30 * timej) || Projectile.ai[0] == (int)(90 * timej)) {
                    odr.Clear();
                    ods.Clear();
                    rotSpeed = 0;
                    Projectile.velocity = (Main.MouseWorld - Main.LocalPlayer.Center).SafeNormalize(Vector2.One) * 12;
                    if (Projectile.velocity.X < 0) {
                        Projectile.rotation = (Main.MouseWorld - Main.LocalPlayer.Center).ToRotation() - (float)Math.PI * 0.5f;
                    }
                    else {
                        Projectile.rotation = (Main.MouseWorld - Main.LocalPlayer.Center).ToRotation() + (float)Math.PI * 0.5f;
                    }
                    Projectile.netUpdate = true;
                }
                if (Projectile.ai[0] == (int)(120 * timej)) {

                    odr.Clear();
                    ods.Clear();
                    rotSpeed = 0;
                    Projectile.velocity = (Main.MouseWorld - Main.LocalPlayer.Center).SafeNormalize(Vector2.One) * 12;
                    if (Projectile.velocity.X > 0) {
                        Projectile.rotation = (Main.MouseWorld - Main.LocalPlayer.Center).ToRotation() - (float)Math.PI * 0.7f;
                    }
                    else {
                        Projectile.rotation = (Main.MouseWorld - Main.LocalPlayer.Center).ToRotation() + (float)Math.PI * 0.7f;
                    }
                    Projectile.netUpdate = true;
                }

            }
            if (Projectile.ai[0] == (int)(120 * timej) + 3) {
                noSlowTime = 4;
                SoundStyle s = SoundID.Item1; s.Pitch = 1 - timej;
                SoundEngine.PlaySound(s, Projectile.Center); odr.Clear();
                Projectile.damage *= 2;
                odr.Clear();
                ods.Clear();
                if (Projectile.velocity.X > 0) {
                    rotSpeed = 0.67f;
                }
                else {
                    rotSpeed = -0.67f;
                }
                if (Projectile.owner == Main.myPlayer) {
                    for (int i = 0; i < 4; i++) {
                        Player player = Projectile.owner.ToPlayer();
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(16)) * (1.6f + ((float)Main.rand.NextDouble() * 0.5f)), ModContent.ProjectileType<TyrantPhantomBlade>(), (int)(Projectile.damage * 0.9f), 4, Projectile.owner, (float)player.direction * player.gravDir, 56, player.GetAdjustedItemScale(player.HeldItem));
                    }
                }
            }
            if (Projectile.ai[0] == 2 || Projectile.ai[0] == (int)(60 * timej) + 2) {
                noSlowTime = 4;
                SoundStyle s = SoundID.Item1; s.Pitch = 1 - timej;
                SoundEngine.PlaySound(s, Projectile.Center); odr.Clear();
                ods.Clear();
                if (Projectile.velocity.X > 0) {
                    rotSpeed = 0.5f;
                }
                else {
                    rotSpeed = -0.5f;
                }
                if (Projectile.owner == Main.myPlayer) {
                    for (int i = 0; i < 2; i++) {
                        Player player = Projectile.owner.ToPlayer();
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(16)) * (1.6f + ((float)Main.rand.NextDouble() * 0.5f)), ModContent.ProjectileType<TyrantPhantomBlade>(), (int)(Projectile.damage * 0.9f), 4, Projectile.owner, (float)player.direction * player.gravDir, 56, player.GetAdjustedItemScale(player.HeldItem));
                    }
                }
            }
            if (Projectile.ai[0] == (int)(30 * timej) + 2 || Projectile.ai[0] == (int)(90 * timej) + 2) {
                noSlowTime = 4;
                SoundStyle s = SoundID.Item1; s.Pitch = 1 - timej;
                SoundEngine.PlaySound(s, Projectile.Center); odr.Clear();
                ods.Clear();
                if (Projectile.velocity.X < 0) {
                    rotSpeed = 0.5f;
                }
                else {
                    rotSpeed = -0.5f;
                }
                if (Projectile.owner == Main.myPlayer) {
                    for (int i = 0; i < 2; i++) {
                        Player player = Projectile.owner.ToPlayer();
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(16)) * (1.6f + ((float)Main.rand.NextDouble() * 0.5f)), ModContent.ProjectileType<TyrantPhantomBlade>(), (int)(Projectile.damage * 0.9f), 4, Projectile.owner, (float)player.direction * player.gravDir, 56, player.GetAdjustedItemScale(player.HeldItem));
                    }
                }
            }
            if (noSlowTime <= 0) {
                rotSpeed *= 0.86f;
            }
            else {
                noSlowTime--;
            }
            scaleD = 0.4f + Math.Abs(rotSpeed) * 2.6f;
            Projectile.rotation += rotSpeed;
            if (rotSpeed == 0) {
                scaleD = 1.6f;
            }
            if (!owner.channel && (Projectile.ai[0] == (int)(165 * timej) - 1 || Projectile.ai[0] == (int)(30 * timej) - 1 || Projectile.ai[0] == (int)(60 * timej) - 1 || Projectile.ai[0] == (int)(90 * timej) - 1 || Projectile.ai[0] == (int)(120 * timej) - 1)) {
                Projectile.Kill();
            }
            else {
                Projectile.timeLeft = 4;
            }
            if (Projectile.ai[0] >= (int)(165 * timej)) {
                Projectile.ai[0] = -1;
                Projectile.damage /= 2;
            }
            owner.itemAnimation = 2;
            owner.itemTime = 2;
            owner.heldProj = Projectile.whoAmI;
            Projectile.ai[0]++;
            Projectile.Center = owner.MountedCenter;
            if (Projectile.velocity.X > 0) {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }

            if (rotSpeed != 0) {
                float ds;
                if (ods.Count > 0) {
                    ds = ods[ods.Count - 1];
                }
                else {
                    ds = scaleD * scale2;
                }
                odr.Add(Projectile.rotation - rotSpeed * 0.8f);
                ods.Add(MathHelper.Lerp(ds, scaleD * scale2, 0.2f));
                if (odr.Count > 17) {
                    odr.RemoveAt(0);
                    ods.RemoveAt(0);
                }
                odr.Add(Projectile.rotation - rotSpeed * 0.6f);
                ods.Add(MathHelper.Lerp(ds, scaleD * scale2, 0.4f));
                if (odr.Count > 17) {
                    odr.RemoveAt(0);
                    ods.RemoveAt(0);
                }
                odr.Add(Projectile.rotation - rotSpeed * 0.4f);
                ods.Add(MathHelper.Lerp(ds, scaleD * scale2, 0.6f));
                if (odr.Count > 17) {
                    odr.RemoveAt(0);
                    ods.RemoveAt(0);
                }
                odr.Add(Projectile.rotation - rotSpeed * 0.2f);
                ods.Add(MathHelper.Lerp(ds, scaleD * scale2, 0.8f));
                if (odr.Count > 17) {
                    odr.RemoveAt(0);
                    ods.RemoveAt(0);
                }
            }
            odr.Add(Projectile.rotation);
            ods.Add(scaleD * scale2);
            if (odr.Count > 17) {
                odr.RemoveAt(0);
                ods.RemoveAt(0);
            }
            if (owner.ownedProjectileCounts[ModContent.ProjectileType<PoTPhantom>()] < 1 && owner.Entropy().pot_amp >= 10 && owner.channel) {
                if (Main.myPlayer == Projectile.owner) {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<PoTPhantom>(), Projectile.damage * 2, Projectile.knockBack * 2, Projectile.owner, Projectile.whoAmI);
                }
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Player player = Main.player[Projectile.owner];

            Texture2D tail = CEExtraAssets.Extra_201;
            var r = Main.rand;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<ColoredVertex> ve = new List<ColoredVertex>();

            for (int i = 0; i < odr.Count; i++) {
                Color b = Color.Lerp(new Color(24, 24, 20), new Color(180, 50, 60), (float)i / (float)odr.Count) * 0.8f;
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(280 * ods[i] * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3(i / (float)odr.Count, 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(0 * ods[i] * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3(i / (float)odr.Count, 0, 1),
                      b));
            }

            if (ve.Count >= 3) {
                gd.Textures[0] = tail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(TextureAssets.Projectile[Projectile.type].Value, Projectile.owner.ToPlayer().MountedCenter - Main.screenPosition, null, Color.White, Projectile.rotation + (float)Math.PI * 0.25f, new Vector2(0, TextureAssets.Projectile[Projectile.type].Value.Height), Projectile.scale * 1.5f * scaleD * scale2, SpriteEffects.None, 0);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (potbuffcd <= 0) {
                SoundStyle s = new SoundStyle("CalamityEntropy/Assets/Sounds/swing4"); s.Pitch = 1 - timej;
                SoundEngine.PlaySound(s, Projectile.Center); odr.Clear();
                potbuffcd = 4;
                Player player = Projectile.owner.ToPlayer();
                if (player.Entropy().pot_time < 600 + player.Entropy().WeaponBoost * 200) {
                    player.Entropy().pot_time += 60 + player.Entropy().WeaponBoost * 40;
                    if (player.Entropy().pot_time > 600 + player.Entropy().WeaponBoost * 200) {
                        player.Entropy().pot_time = 600 + player.Entropy().WeaponBoost * 200;
                    }
                }

                if (player.Entropy().pot_amp < 10 + player.Entropy().WeaponBoost * 5) {
                    player.Entropy().pot_amp += 1 + player.Entropy().WeaponBoost;
                }
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (scaleD < 0.6f || rotSpeed == 0) {
                return false;
            }
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 150 * Projectile.scale * scaleD * scale2, targetHitbox, 100) || CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + (Projectile.rotation - rotSpeed * 0.5f).ToRotationVector2() * 300 * Projectile.scale * scaleD, targetHitbox, 100);
        }
    }

}