using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AquashardThrow : ModProjectile, IJavelin
    {
        List<Vector2> odp = new List<Vector2>();
        List<float> odr = new List<float>();
        public bool SetHandRot { get; set; }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.penetrate = 3;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 260;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            Projectile.ArmorPenetration = 6;
            SetHandRot = true;
        }
        public float handrot = 0;
        public float handrotspeed = 0;
        public Vector2 ownerMouse = Vector2.Zero;

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Projectile.rotation);
            writer.Write(handrot);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            Projectile.rotation = reader.ReadSingle();
            handrot = reader.ReadSingle();
        }
        public override void OnSpawn(IEntitySource source) {
            foreach (Projectile p in Main.projectile) {
                if (p.whoAmI != Projectile.whoAmI) {
                    if (p.ModProjectile is IJavelin jv) {
                        jv.SetHandRot = false;
                    }
                }
            }
        }
        public override void AI() {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (Projectile.ai[0] > 12 && Projectile.IsEmpowered() && Main.myPlayer == Projectile.owner && ++Projectile.localAI[1] % 3 == 0 && ++Projectile.localAI[2] < 9)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center - Projectile.velocity * 3, Projectile.velocity * 0.2f, ModContent.ProjectileType<AquashardSplit>(), (int)(Projectile.damage * 0.25), 0f, Projectile.owner).ToProj().DamageType = DamageClass.Melee;

            if (odp.Count > 16) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
            if (Projectile.ai[0] == 0) {
                handrotspeed = -0.3f;
            }
            else if (Projectile.ai[0] < 12) {
                handrotspeed += 0.056f;
            }
            if (Projectile.ai[0] < 12) {

                var owner = Projectile.owner.ToPlayer();

                if (Main.myPlayer == Projectile.owner) {
                    Projectile.rotation = (Main.MouseWorld - Projectile.Center).ToRotation();
                    Projectile.netUpdate = true;
                }
                if (this.SetHandRot) {
                    Projectile.owner.ToPlayer().heldProj = Projectile.whoAmI;
                    if (owner.direction == 1) {
                        Projectile.Center = owner.MountedCenter + new Vector2(26, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver2 - handrot);
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - handrot - MathHelper.Pi);
                    }
                    else {
                        Projectile.Center = owner.MountedCenter + new Vector2(26, 0).RotatedBy(Projectile.rotation + MathHelper.PiOver2 + handrot);
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + handrot);
                    }
                }
                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(Projectile.rotation);
            }
            else if (Projectile.ai[0] < 36) {
                handrotspeed *= 0.84f;
                var owner = Projectile.owner.ToPlayer();
                if (this.SetHandRot) {
                    if (owner.direction == 1) {
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - handrot - MathHelper.Pi);
                    }
                    else {
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + handrot);

                    }
                    Projectile.owner.ToPlayer().heldProj = -1;
                }

            }
            if (Projectile.ai[0] > 12) {
                Projectile.tileCollide = true;
                if (Projectile.ai[0] > 26) {
                    Projectile.velocity.Y += 1.6f;
                    Projectile.velocity *= 0.996f;
                    Projectile.velocity.X *= 0.96f;
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            handrot -= handrotspeed;
            if (Projectile.ai[0] == 10) {

                SoundStyle SwingSound = SoundID.Item1;
                SwingSound.Pitch = 0f;
                if (Projectile.IsEmpowered()) {
                    SwingSound.Pitch = 1f;
                }

                SoundEngine.PlaySound(SwingSound, Projectile.Center);
            }

            Projectile.ai[0]++;

        }
        public bool spawnShard = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(60, 255, 255), 0.01f).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, 0.64f, 14);
            CEUtils.PlaySound("slice", Projectile.IsEmpowered() ? 1.2f : 1f, target.Center);
            if (spawnShard) {
                spawnShard = false;
                if (Projectile.owner == Main.myPlayer) {
                    for (int i = 0; i < (Projectile.IsEmpowered() ? 0 : 3); i++) {
                        Vector2 velocity = new Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-34, -26));
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<AquaShardWaterBullet>(), (int)(Projectile.damage * 0.33f), 0f, Projectile.owner).ToProj().DamageType = DamageClass.Melee;
                    }
                }
                if (Projectile.IsEmpowered()) {
                    for (int i = 0; i < 16; i++) {
                        //EGlowOrb Additive走Configure
                        PRTLoader.NewParticle<PRT_EGlowOrb>(Projectile.Center, CEUtils.randomPointInCircle(16), Color.SkyBlue, 0.32f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 18);
                    }
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<WaterBulletSpawner>(), (int)(Projectile.damage * 0.3f), Projectile.knockBack / 3, Projectile.owner, target.whoAmI);
                }
            }
            for (int i = 0; i < 16; i++) {
                PRTLoader.NewParticle<PRT_EGlowOrb>(Projectile.Center + Projectile.velocity.normalize(), CEUtils.randomPointInCircle(2) + Projectile.velocity * 0.7f * Main.rand.NextFloat(0.2f, 1), Color.SkyBlue, 0.2f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
            }
        }
        public override void OnKill(int timeLeft) {
            if (timeLeft > 0) {
                SoundEngine.PlaySound(in SoundID.Item27, base.Projectile.position);
            }
        }
        public override bool ShouldUpdatePosition() {
            return Projectile.ai[0] >= 12;
        }

        public override bool? CanHitNPC(NPC target) {
            if (Projectile.ai[0] <= 10) {
                return false;
            }
            return null;
        }
        public bool eff = true;
        public Color ColorFunction(float completionRatio, Vector2 vertex) {
            return Color.Lerp(Color.Aqua, Color.AliceBlue, MathHelper.Clamp(completionRatio * 0.8f, 0f, 1f)) * base.Projectile.Opacity;
        }

        public float WidthFunction(float completionRatio, Vector2 vertex) {
            float num = 8f;
            float num2 = ((!(completionRatio < 0.1f)) ? MathHelper.Lerp(num, 0f, Utils.GetLerpValue(0.1f, 1f, completionRatio, clamped: true)) : ((float)Math.Sin(completionRatio / 0.1f * (MathF.PI / 2f)) * num + 0.1f));
            return num2 * base.Projectile.Opacity * Projectile.scale * 3.4f
            ;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.ai[0] > 14 && Projectile.IsEmpowered()) {
                Main.spriteBatch.EnterShaderRegion();
                GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.StreakGoopAsset);
                GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
                List<Vector2> lt = new List<Vector2>();
                for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++) {
                    lt.Add(Projectile.oldPos[i] + Projectile.Size / 2 + Projectile.oldRot[i].ToRotationVector2() * 68);
                }
                CEPrimitiveRenderer.RenderTrail(lt, new CEPrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
            float rj = 0;
            if (Projectile.ai[0] < 12) {
                rj = -handrot * Projectile.owner.ToPlayer().direction;
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver2) {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition + i.ToRotationVector2() * 3, null, Color.White, Projectile.rotation + MathHelper.PiOver4 + rj, tx.Size() / 2, Projectile.scale, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4 + rj, tx.Size() / 2, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
    public class WaterBulletSpawner : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, 2);
            Projectile.width = Projectile.height = 16;
            Projectile.timeLeft = 100;
            Projectile.Opacity = 0;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/Glow";
        public override void AI() {
            int NPC = (int)Projectile.ai[0];
            if (!NPC.ToNPC().active)
                Projectile.Kill();
            if (Projectile.Opacity < 1)
                Projectile.Opacity += 0.2f;
            Projectile.Center = NPC.ToNPC().Center;
            Projectile.scale -= 0.01f;
            if (Projectile.timeLeft % 10 == 0 && Projectile.timeLeft > 19) {
                if (Main.myPlayer == Projectile.owner)
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * 24, ModContent.ProjectileType<AquaShardWaterBullet>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 1);
            }
        }
        public override bool? CanDamage() {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            CEUtils.DrawGlow(Projectile.Center, new Color(50, 255, 255) * Projectile.Opacity, Projectile.scale * 3f);
            CEUtils.DrawGlow(Projectile.Center, Color.White * Projectile.Opacity, Projectile.scale * 2.7f);
            return false;
        }
    }
    public class AquaShardWaterBullet : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, 2);
            Projectile.width = Projectile.height = 46;
            Projectile.timeLeft = 120;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/Glow";
        public int NoChaseTime = 10;
        public override void AI() {
            if (Projectile.ai[0] == 0) {
                NoChaseTime--;
                NoChaseTime--;
                if (NoChaseTime > 0) {
                    NoChaseTime--;
                }
                else {
                    Projectile.velocity.Y += 4f;
                }
            }
            else {
                if (NoChaseTime > 0) {
                    Projectile.velocity *= 0.9f;
                    NoChaseTime--;
                }
                else {
                    Projectile.HomingToNPCNearby(12, 0.9f, 1200);
                }
            }
            for (float i = 0.1f; i <= 1f; i += 0.1f) {
                oldPos.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity, i));
                if (oldPos.Count > 40)
                    oldPos.RemoveAt(0);
            }
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            NoChaseTime = 8;
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(60, 255, 255), 0.01f).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, 0.55f, 14);
        }
        public override bool? CanHitNPC(NPC target) {
            return NoChaseTime <= 0 ? null : false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 20;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            float ap = 1f / (float)oldPos.Count;
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < oldPos.Count; i++) {
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, new Color(60, 255, 255) * ap, Projectile.velocity.ToRotation(), tex.Size() / 2, new Vector2(1 + (Projectile.velocity.Length() * 0.01f), (1f / (1 + (Projectile.velocity.Length() * 0.01f)))) * 0.26f * ap * (Projectile.ai[0] == 1 ? 1 : 0.8f), SpriteEffects.None, 0);
                ap += 1f / (float)oldPos.Count;
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
