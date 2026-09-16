using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class DaylightProjectile : BaseWhip
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = 5;
            this.segments = 20;
            this.rangeMult = 1;
        }
        public override float getSegScale(int segCount, int segCounts) {
            return base.getSegScale(segCount, segCounts) * 1.5f;
        }
        public override Color modifySegColor(int segCount, Color color) {
            return Color.Lerp(color, Color.White, segCount / (this.segments - 1f));
        }
        public override void DrawSegs(List<Vector2> points) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D tex = CEExtraAssets.StarTexture;
            float num = 0.82f * (float)Math.Sin(Main.GameUpdateCount / 10f);
            float num2 = 0.82f * (float)Math.Sin(Main.GameUpdateCount / 10f + MathHelper.PiOver4);
            Vector2 pos = points[points.Count - 1];
            Color color = new Color(180, 180, 0);
            float size = CEUtils.Parabola(this.FlyProgress, 1);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, color * Projectile.Opacity, 0, tex.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.54f * size, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, color * Projectile.Opacity, 0, tex.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.54f * size, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, tex.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.26f * size, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, tex.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.26f * size, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            base.DrawSegs(points);
        }
        public override string getTagEffectName => "DaylightProjectile";
        public override SoundStyle? WhipSound => SoundID.Item45;
        public override Color StringColor => new Color(140, 140, 0);
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            for (int i = 0; i < 46; i++) {
                var d = Dust.NewDustDirect(target.position, target.width, target.height, DustID.YellowTorch);
                d.scale = 3.2f;
                d.velocity = new Vector2(0, Main.rand.NextFloat(-8, -5));
                d.noGravity = true;
            }
            target.AddBuff(BuffID.OnFire3, 160);
            if (CECooldowns.CheckCD(getTagEffectName, 80)) {
                int ds_type = ModContent.ProjectileType<DaylightSun>();
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (p.owner == Projectile.owner && p.type == ds_type) {
                        p.timeLeft = 40;
                    }
                }
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(160, 200), Vector2.Zero, ds_type, (int)(Projectile.GetOwner().GetTotalDamage(DamageClass.Summon).ApplyTo(18)), 0, Projectile.GetOwner().whoAmI);
            }
            if (Projectile.localAI[1]++ == 0) {
                SoundEngine.PlaySound(SoundID.Item153 with { PitchRange = (-0.2f, 0) }, target.Center);
                CEUtils.PlaySound("beast_lavaball_rise1", Main.rand.NextFloat(1.2f, 1.4f), target.Center, 8, 0.9f);
            }
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff(ModContent.BuffType<DaylightWhipDebuff>(), 240);
        }
        public override bool PreAI() {
            var points = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, points);

            if (FlyProgress > 0.43f && FlyProgress < 0.86f) {
                if (Main.rand.NextBool())
                    for (int i = 0; i < 1; i++) {
                        var d = Dust.NewDustDirect(points[points.Count - 1], 0, 0, DustID.YellowStarDust);
                        d.scale = 1.4f;
                        d.noGravity = true;
                    }
                for (int i = 0; i < 1; i++) {
                    var d = Dust.NewDustDirect(Vector2.Lerp(Projectile.GetOwner().Center, points[points.Count - 1], Main.rand.NextFloat(Main.rand.NextFloat(0, 0.4f), 1)), 0, 0, DustID.YellowStarDust);
                    d.scale = 1.4f;
                    d.noGravity = true;
                    d.velocity += (points[points.Count - 1] - Projectile.GetOwner().Center).normalize() * 5;
                }
                //GlowLightParticle daylight glow,旧PRT/EParticle
                PRTLoader.NewParticle<PRT_GlowLightParticle>(points[points.Count - 1], CEUtils.randomPointInCircle(4), Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat()), Main.rand.NextFloat(0.6f, 1f)).Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, 0, 38);
            }
            return base.PreAI();
        }
        public override int handleHeight => 40;
        public override int segHeight => 16;
        public override int endHeight => 30;
        public override int segTypes => 2;
    }
    public class DaylightSun : ModProjectile
    {
        public override void SetStaticDefaults() {
            //ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 240;
        }
        public override void AI() {
            if (Projectile.ai[0] > 0 && Projectile.ai[2] == 0) {
                Projectile.ai[2] = 1;

            }
            if (Projectile.ai[2] > 0 && Projectile.ai[2] < 22) {
                Projectile.ai[2]++;
                if (Projectile.ai[2] % 5 == 0) {
                    if (Main.myPlayer == Projectile.owner) {
                        if (Projectile.OwnerEntropy().lastHitTarget != null) {
                            Vector2 tpos = Projectile.OwnerEntropy().lastHitTarget.Center + Projectile.OwnerEntropy().lastHitTarget.velocity * 4;
                            Vector2 spos = Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(30, 80);
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), spos, (tpos - spos).normalize() * 16, ModContent.ProjectileType<DaylightLaser>(), Projectile.damage, 8, Projectile.GetOwner().whoAmI);
                        }
                    }
                }
            }
            if (Projectile.timeLeft < 40) {
                Projectile.Opacity -= 1 / 40f;
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D c = CEExtraAssets.Circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (float i = 0; i < 1; i += 0.2f) {
                float scale = (1 - i) * 1.2f;
                float alpha = 0.16f;
                Main.spriteBatch.Draw(c, Projectile.Center - Main.screenPosition, null, new Color(255, 255, 125) * alpha * Projectile.Opacity, 0, c.Size() / 2f, scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Main.GlobalTimeWrappedHourly * 2, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.OnFire3, 2 * 60);
        }
    }
    public class DaylightLaser : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.OnFire3, 2 * 60);
        }
        public override void SetStaticDefaults() {
            //ProjectileID.Sets.MinionShot[Type] = true;
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 8000;
        }
        public int counter = 0;
        public int length = 3600;
        NPC ownern = null;
        public float width = 0;
        public int aicounter = 0;
        public override void SetDefaults() {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Summon;
        }
        public bool st = true;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 16;
        }
        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (st) {
                //ShineParticle FollowOwner字段spawn后赋,Configure只管Additive和lifetime
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 20), 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 160), 0.29f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 160), 0.29f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 160), 0.29f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                CEUtils.PlaySound("CrystalBallActive", 0.6f + Main.rand.NextFloat(-0.2f, 0.2f), Projectile.Center, 10, 0.4f);
                st = false;
            }
            if (Projectile.timeLeft < 6) {
                width -= 1f / 16f;
            }
            else {
                width += 1f / 16f;

            }
            aicounter++;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * length, targetHitbox, 30);
        }
        public override bool PreDraw(ref Color lightColor) {
            counter++;
            Texture2D tex = CEExtraAssets.DeathRay2;
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearWrap);

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 18, 0, length, tex.Height), Color.Yellow, Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.8f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 26, 0, length, tex.Height), new Color(255, 255, 90), Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.6f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 40, 0, length, tex.Height), Color.White, Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.5f), SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);

            Texture2D star = CEExtraAssets.StarTexture;
            float num = 0.5f * (float)Math.Sin(Main.GameUpdateCount / 10f);
            float num2 = 0.5f * (float)Math.Sin(Main.GameUpdateCount / 10f + MathHelper.PiOver4);
            Vector2 pos = Projectile.Center;
            Color color = new Color(180, 180, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, 0, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, 0, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.2f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.2f, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
    }
}