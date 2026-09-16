using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class RuneSong : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 230;
            Item.DamageType = DamageClass.Melee;
            Item.width = 56;
            Item.noUseGraphic = true;
            Item.height = 56;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = null;
            Item.channel = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<RuneSongHeld>();
            Item.shootSpeed = 6f;
        }
        public override bool MeleePrefix() {
            return true;
        }
    }
    public class RuneSongHeld : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 16;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 800;
            Projectile.localNPCHitCooldown = -1;
        }
        public int flag { get { return (int)Projectile.ai[1]; } set { Projectile.ai[1] = value; } }
        public int dir = 1;
        public float RotVel = 0;
        public float Rot = 0;
        public override bool? CanDamage() {
            return flag == 1 || flag == 2;
        }
        public float glow = 0;
        public float sAlpha = 0;
        public override void AI() {
            if (Projectile.Entropy().FirstFrames) {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            Player player = Projectile.GetOwner();
            float speedMelee = (1 + ((player.GetTotalAttackSpeed(DamageClass.Melee) - 1) * ItemID.Sets.BonusAttackSpeedMultiplier[player.HeldItem.type]));
            float speedTrueMelee = player.GetWeaponAttackSpeed(player.HeldItem);
            player.itemTime = player.itemAnimation = 2;
            if (flag == 0) {
                RotVel *= (float)Math.Pow(0.9f, speedTrueMelee);
                if (Projectile.Entropy().FirstFrames) {
                    dir = Projectile.velocity.X > 0 ? 1 : -1;
                    Rot = -1.2f;
                    RotVel = -0.2f;
                }
                Projectile.ai[0] += speedTrueMelee / 60f;
                if (Projectile.ai[0] >= 1) {
                    Projectile.ai[0] = 1;
                    flag = 1;
                    sAlpha = 1f;
                    CEUtils.PlaySound("runesong3", Main.rand.NextFloat(0.6f, 0.8f), Projectile.Center);
                }
                glow = Projectile.ai[0];
            }
            if (flag == 1) {
                RotVel *= (float)Math.Pow(0.875f, speedTrueMelee);
                if (Projectile.ai[0] == 1) {
                    RotVel = 0.64f * speedMelee;
                }
                if (Projectile.ai[0] > 1.3f)
                    sAlpha *= 0.86f;
                Projectile.ai[0] += speedTrueMelee / 42f;
                if (Projectile.ai[0] >= 2) {
                    flag = 3;
                    Projectile.ai[0] = 2;
                    if (Main.myPlayer == Projectile.owner && !Main.mouseLeft) {
                        Projectile.Kill();
                        return;
                    }
                }
                if (Projectile.ai[0] < 1.56f)
                    for (int i = 0; i < 3; i++)
                        PRTLoader.NewParticle<PRT_RuneParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(40, Length * scaleE), CEUtils.randomPointInCircle(2), Color.LightBlue, 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 38);
            }
            if (flag == 4) {
                RotVel *= (float)Math.Pow(0.9f, speedTrueMelee);
                Projectile.ai[0] += speedTrueMelee / 34f;
                if (Projectile.ai[0] >= 2) {
                    Projectile.ai[0] = 2;
                    flag = 2;
                }
                scaleE = 1 + (CEUtils.Parabola(0.5f * ((Projectile.ai[0] - 1)), 1f)) * 0.5f;
            }
            if (flag == 2) {

                if (Projectile.ai[0] == 2) {
                    sAlpha = 1;
                    RotVel = 0.6f * speedMelee;
                    Projectile.ResetLocalNPCHitImmunity();
                    Projectile.localNPCHitCooldown = 8;
                }
                if (Projectile.ai[0] > 2.5f)
                    sAlpha *= 0.86f;
                if (Projectile.ai[0] > 2.4f)
                    RotVel *= (float)Math.Pow(0.87f, speedTrueMelee);
                Projectile.localAI[1] += Math.Abs(RotVel);
                Projectile.ai[0] += speedTrueMelee / 60f;
                if (Projectile.localAI[2] <= 0 && Projectile.ai[0] < 2.7f) {
                    CEUtils.PlaySound("HellkiteSwing" + Main.rand.Next(1, 3), Main.rand.NextFloat(2.4f, 2.8f), Projectile.Center, 40, 0.5f);
                    Projectile.localAI[2] = 1f;
                }
                Projectile.localAI[2] -= speedTrueMelee / 14f;
                if (Projectile.ai[0] >= 3) {
                    Projectile.Kill();
                    return;
                }
                if (Projectile.ai[0] < 2.6f)
                    for (int i = 0; i < 3; i++)
                        PRTLoader.NewParticle<PRT_RuneParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(40, Length * scaleE), CEUtils.randomPointInCircle(2), Color.LightBlue, 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 38);
            }
            if (sAlpha < 0.02f)
                sAlpha = 0;
            if (flag == 3) {
                Rot = CEUtils.RotateTowardsAngle(Rot, 0, 0.08f, false);
                Rot = CEUtils.RotateTowardsAngle(Rot, 0, 0.01f, true);
                Projectile.ai[0] += speedMelee / 60f;
                if (Projectile.ai[0] >= 3) {
                    glow *= 0.7f;
                    if (Projectile.localAI[2] == 0) {
                        glow = 2f;
                        CEUtils.PlaySound("scholarStaffImpact", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
                        CEUtils.PlaySound("sf_hit", Main.rand.NextFloat(1.1f, 1.15f), Projectile.Center);
                        CEUtils.PlaySound("sf_hit", Main.rand.NextFloat(1.1f, 1.15f), Projectile.Center);

                        for (int i = 0; i < 128; i++) {
                            PRTLoader.NewParticle<PRT_RuneParticle>(Projectile.Center + CEUtils.randomPointInCircle(12) + Projectile.velocity.normalize() * 160, Projectile.velocity.normalize() * Main.rand.NextFloat(4, 80), Color.Aqua, Main.rand.NextFloat(0.8f, 1.4f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 42);
                        }
                        if (Main.myPlayer == Projectile.owner) {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 180, Projectile.velocity.normalize() * 8, ModContent.ProjectileType<RuneBolt>(), (int)(Projectile.damage * 2.5f), Projectile.knockBack, Projectile.owner);
                            CalamityEntropy.FlashEffectStrength = 0.24f;
                        }
                    }
                    Projectile.localAI[2] += speedMelee / 10f;
                    if (Projectile.localAI[2] >= 1) {
                        Projectile.Kill();
                        return;
                    }
                }
            }
            Rot += RotVel;
            Projectile.StickToPlayer();
            Projectile.rotation = Projectile.rotation + Rot * dir;
            player.SetHandRotWithDir(Projectile.rotation, player.direction);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= 1f;
        }
        public int Length = 250;
        public float scaleE = 1f;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Length * Projectile.scale * scaleE, targetHitbox, 100);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Length * Projectile.scale * scaleE, 40, DelegateMethods.CutTiles);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glowTex = this.getTextureGlow();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 20, null, Color.White, Projectile.rotation + MathHelper.PiOver4, new Vector2(17, 81), Projectile.scale * scaleE * 2, SpriteEffects.None, 0);
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glowTex, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 20, null, Color.Aqua * glow * 0.6f, Projectile.rotation + MathHelper.PiOver4, new Vector2(17, 81), Projectile.scale * scaleE * 2, SpriteEffects.None, 0);
            Texture2D sm = CEExtraAssets.CircularSmearSmokey;
            Main.spriteBatch.Draw(sm, Projectile.Center - Main.screenPosition, null, new Color(120, 120, 190) * sAlpha, Projectile.rotation + MathHelper.PiOver4 * 3 + -0.6f * dir, sm.Size().Half(), 3.16f * Projectile.scale * scaleE, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("runesonghit", Main.rand.NextFloat(1f, 1.4f), target.Center);
            CalamityEntropy.SpawnHeavenSpark(target.Center, CEUtils.randomRot(), 1.2f, 1f, Color.LightBlue * 1.4f, 14);
            if (flag == 1) {
                CEUtils.PlaySound("runesonghit", Main.rand.NextFloat(1f, 1.4f), target.Center);
                CalamityEntropy.SpawnHeavenSpark(target.Center, CEUtils.randomRot(), 1.2f, 1f, Color.LightBlue * 1.4f, 14);
                Projectile.ai[0] = 1;
                flag = 4;
                RotVel = -0.4f;
                sAlpha = 0;
                CEUtils.SyncProj(Projectile.whoAmI);
            }
            target.AddBuff<SoulDisorder>(200);
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(RotVel);
            writer.Write(sAlpha);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            RotVel = reader.ReadSingle();
            sAlpha = reader.ReadSingle();
        }
    }

    public class RuneBolt : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        //符文飘带贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/RuneRibbon2")]
        internal static Asset<Texture2D> RuneRibbon2Tex;
        public int length = 2400;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.ai[1] = 1;
            Projectile.ArmorPenetration = 36;
        }
        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.ai[0] > 4) {
                Projectile.ai[1] -= 1 / 8f;
            }
            else {
                Projectile.ai[1] += 0.25f;
            }
            Projectile.ai[0]++;
            if (Projectile.owner == Main.myPlayer) {
                Projectile.netUpdate = true;
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.ai[0] > 4) {
                return false;
            }
            float laserLength = Projectile.scale * length;
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.One) * laserLength, targetHitbox, 90);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("beast_lavaball_rise1", Main.rand.NextFloat(1.4f, 1.8f), target.Center);
            float sparkCount = 16;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = Projectile.velocity.RotateRandom(0.2f) * Main.rand.NextFloat(0.5f, 1.8f);
                int sparkLifetime2 = Main.rand.Next(20, 24);
                float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                Color sparkColor2 = Color.DarkBlue;

                float velc = 1f;
                PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * velc, Main.rand.NextBool() ? Color.Aqua : Color.LightBlue, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));

            }
            for (int i = 0; i < 29; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.rotation.ToRotationVector2() * CEUtils.getDistance(target.Center, Projectile.Center), ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(3f, 3.5f);
                dust.velocity =
                    Projectile.velocity.normalize().RotatedByRandom(0.4f) * Main.rand.NextFloat(8, 36);
                dust.noGravity = true;
                dust.color = Color.LightBlue;
                dust.fadeIn = 2f;
            }
            Projectile.damage = (int)(Projectile.damage * 0.85f);
            target.AddBuff<SoulDisorder>(200);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
            Texture2D tex = CEExtraAssets.Streak1;
            Texture2D r = RuneRibbon2Tex.Value;
            CEUtils.DrawGlow(Projectile.Center, Color.White * Projectile.ai[1], 2f * Projectile.scale, true, null, false);
            CEUtils.DrawGlow(Projectile.Center, Color.White * Projectile.ai[1], 1.6f * Projectile.scale, true, null, false);
            CEUtils.DrawGlow(Projectile.Center, Color.Aqua * Projectile.ai[1], 4f * Projectile.scale, true, null, false);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 900), 0, (int)(Projectile.scale * length), tex.Height), new Color(90, 90, 140), Projectile.rotation, new Vector2(0, tex.Height * 0.5f), new Vector2(1, Projectile.scale * Projectile.ai[1] * 0.37f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 1400), 0, (int)(Projectile.scale * length), tex.Height), new Color(255, 255, 255), Projectile.rotation, new Vector2(0, tex.Height * 0.5f), new Vector2(1, Projectile.scale * Projectile.ai[1] * 0.22f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(r, Projectile.Center - Main.screenPosition, new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 1400), 0, (int)(Projectile.scale * length), r.Height), new Color(160, 160, 200), Projectile.rotation, new Vector2(0, r.Height * 0.5f), new Vector2(1.2f, Projectile.scale * Projectile.ai[1] * 1f), SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }

    }
}
