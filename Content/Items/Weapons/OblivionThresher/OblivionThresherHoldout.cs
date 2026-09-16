
using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Prophet;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.OblivionThresher
{
    public class OblivionThresherHoldout : ModProjectile
    {
        //本文件三个锯刃类共用的贴图,加载期由 VaultLoaden 赋值;漩涡着色器读共享基座 CEEffectAssets
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/OblivionThresher/OblivionThresherShootE1")]
        internal static Asset<Texture2D> ThresherShootE1Tex;
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/OblivionThresher/OblivionThresherShootE2")]
        internal static Asset<Texture2D> ThresherShootE2Tex;
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 7;
        }
        public float Charge = 0;
        public LoopSound ChargeIdle;
        public float ShootAnm = 30;
        public float Xoffset = 0;
        public float Xvel = -10f;
        public bool Shoot = true;
        public override bool? CanHitNPC(NPC target) {
            return (!NoSawOnHoldout) ? null : false;
        }
        public override void AI() {
            if (Projectile.localAI[2]++ == 0) {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            Player player = Projectile.GetOwner();

            Projectile.rotation = CEUtils.RotateTowardsAngle(CEUtils.RotateTowardsAngle(Projectile.rotation, (player.Entropy().MouseWorld - Projectile.Center).ToRotation(), 0.1f, false), (player.Entropy().MouseWorld - Projectile.Center).ToRotation(), 0.1f, true);
            Projectile.Center = player.GetDrawCenter() - new Vector2(-Xoffset + 12, 0).RotatedBy(Projectile.rotation);
            player.Entropy().MouseWorldListener = true;
            if (Projectile.owner == Main.myPlayer && Main.mouseRight) {
                if (!player.HasCooldown(OblivionThretherCooldown.ID)) {
                    player.AddCooldown(OblivionThretherCooldown.ID, 320);
                    Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), player.Center, (Main.MouseWorld - player.Center).normalize() * 24, ModContent.ProjectileType<OblivionCruiserDash>(), Projectile.damage, Projectile.knockBack);
                }
            }
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * player.HeldItem.shootSpeed;
            player.heldProj = Projectile.whoAmI;
            player.SetHandRot(Projectile.rotation);
            if (Projectile.timeLeft <= 2) {
                return;
            }
            Vector2 jpos = Projectile.Center + new Vector2(0, Projectile.velocity.X > 0 ? -10 : 10).RotatedBy(Projectile.rotation) + Projectile.rotation.ToRotationVector2() * 80;
            Projectile e = null;
            int t = ModContent.ProjectileType<OblivionThresherShootAlt>();
            foreach (var p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == t) {
                    e = p;
                    break;
                }
            }
            bool ho = false;
            if (e != null && e.ModProjectile is OblivionThresherShootAlt alt && alt.OnJaw) {
                ho = true;
            }
            if (ho) {

                Charge = 1;
                NoSawOnHoldout = true;
                Projectile.damage = 0;
                Xoffset = 0;
                Xvel = -10;
                Shoot = false;
                ERot = 1.2f;
                ShootAnm = 30;
                player.channel = false;
            }
            if (!player.channel) {
                if (ShootAnm-- < 0) {
                    Projectile.timeLeft = 2;
                    return;
                }
                else {
                    if (Shoot) {
                        if (Charge < 0.25f && !ho) {
                            Projectile.timeLeft = 2;
                            return;
                        }
                        Shoot = false;
                        SoundStyle ShootSound = new("CalamityEntropy/Assets/Sounds/SawShot", 2) { PitchVariance = 0.1f, Volume = 0.4f + Charge * 0.5f };
                        SoundEngine.PlaySound(ShootSound, Projectile.Center);
                        NoSawOnHoldout = true;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), jpos, Projectile.velocity, ModContent.ProjectileType<OblivionThresherShoot>(), (int)(Projectile.damage * Charge), Projectile.knockBack, Projectile.owner, Charge);
                    }
                    ChargeIdle?.stop();
                    ChargeIdle = null;
                    Xoffset += Xvel * Charge;
                    Xvel *= 0.84f;
                    Xoffset *= 0.84f;
                }
            }
            Projectile.timeLeft = 4;
            player.itemTime = player.itemAnimation = 3;

            if (Charge < 1) {
                Charge += player.GetTotalAttackSpeed(Projectile.DamageType) / 120f;
            }
            else {
                if (Main.rand.NextBool(3) && !NoSawOnHoldout) {
                    Vector2 smokeVelocity = Vector2.UnitY * Main.rand.NextFloat(-7f, -12f);
                    smokeVelocity = smokeVelocity.RotatedByRandom(MathHelper.Pi / 8f);
                    Color smokeColor = Main.rand.NextBool() ? Color.AliceBlue : Color.LightBlue;

                    //dedServ时PRTLoader.NewParticle给孤儿实例,Configure照常
                    PRTLoader.NewParticle<PRT_HeavySmokeCal>(jpos + Main.rand.NextVector2CircularEdge(3f, 3f), smokeVelocity, smokeColor, 0.65f).Configure(0.5f, 30, Main.rand.NextFloat(-0.2f, 0.2f), true);
                }
            }
            Projectile.ai[2]++;
            if (Charge > 1)
                Charge = 1;
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OblivionThresherShoot>()] > 3) {
                Charge = 0;
            }
            //dedServ时NewParticle给孤儿实例不是null,后面字段赋值照常别挡
            if (ChargeIdle == null && !(Main.dedServ) && player.channel) {
                ChargeIdle = new LoopSound(FableEye.sound);
                ChargeIdle.play();

            }
            if (ChargeIdle != null && player.channel) {
                ChargeIdle.setVolume_Dist(jpos, 200, 1600, 0.4f);
                ChargeIdle.instance.Pitch = 1.4f * Charge - 1;
                ChargeIdle.timeleft = 3;
            }

            if (NoSawOnHoldout) {
                ERot *= 0.82f;
            }
            else {
                ERot = CEUtils.Parabola(Charge * 0.5f, 1) * 0.42f;
            }
            HitSoundCD--;
        }
        public bool NoSawOnHoldout = false;
        public float Time => Projectile.ai[2];
        public float ERot = 0;
        public Vector2 WPos => Projectile.Center + new Vector2(0, Projectile.velocity.X > 0 ? -10 : 10).RotatedBy(Projectile.rotation) + Projectile.rotation.ToRotationVector2() * 80;

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D e1 = this.getTextureAlt("E1");
            Texture2D e2 = this.getTextureAlt("E2");
            Vector2 shakeOffset = CEUtils.randomPointInCircle(NoSawOnHoldout ? 0 : Charge * 3);
            Main.EntitySpriteDraw(e1, Projectile.Center + shakeOffset + new Vector2(36, Projectile.velocity.X > 0 ? -20 : 4).RotatedBy(Projectile.rotation) * Projectile.scale - Main.screenPosition, null, lightColor, Projectile.rotation + Math.Sign(Projectile.velocity.X) * -ERot * Math.Sign(Projectile.velocity.X), new Vector2(8, 26), Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(e2, Projectile.Center + shakeOffset + new Vector2(36, Projectile.velocity.X > 0 ? -4 : 20).RotatedBy(Projectile.rotation) * Projectile.scale - Main.screenPosition, null, lightColor, Projectile.rotation + Math.Sign(Projectile.velocity.X) * ERot * Math.Sign(Projectile.velocity.X), new Vector2(8, 8), Projectile.scale, SpriteEffects.None);

            Main.EntitySpriteDraw(tex, Projectile.Center + shakeOffset - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);


            DrawSaw(shakeOffset);
            return false;
        }
        public void DrawSaw(Vector2 shakeOffset) {
            if (NoSawOnHoldout)
                return;
            Vector2 jpos = Projectile.Center + shakeOffset + new Vector2(0, Projectile.velocity.X > 0 ? -10 : 10).RotatedBy(Projectile.rotation) + Projectile.rotation.ToRotationVector2() * 80;
            if (Charge > 0) {
                DrawVortex(jpos, new Color(80, 70, 250), Charge * 0.8f);
                DrawVortex(jpos, new Color(90, 80, 255) * Charge * 0.8f, 2.6f, 0.2f);
            }
            Texture2D j1 = OblivionThresherHoldout.ThresherShootE1Tex.Value;
            Texture2D j2 = OblivionThresherHoldout.ThresherShootE2Tex.Value;
            Texture2D s = CEExtraAssets.SemiCircularSmear;
            if (Charge >= 1f) {
                Color c = Color.Lerp(Color.White, Color.Blue, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f) * 0.5f + 0.5f);

                Main.EntitySpriteDraw(j1, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * 0.6f, j1.Size() / 2f, Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.4f, Main.GameUpdateCount * 0.6f + MathHelper.Pi * 0.6f, s.Size() / 2f, Projectile.scale * 1.4f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }
            if (Charge >= 0.5f) {
                Color c = Color.Lerp(Color.Blue, Color.White, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 10f) * 0.5f + 0.5f);
                Main.EntitySpriteDraw(j2, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * -0.6f, j2.Size() / 2f, Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.4f, Main.GameUpdateCount * -0.6f - MathHelper.Pi * 0.6f, s.Size() / 2f, Projectile.scale * 0.84f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }
        }
        public int HitSoundCD = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 90, 3.6f, 1000, 16);
            if (Charge < 1) {
                Charge += 0.01f;
            }
            if (HitSoundCD <= 0) {
                HitSoundCD = 6;
                CEUtils.PlaySound("slice", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center, volume: 0.7f);
            }
            for (int i = 0; i < 6; i++) {
                Vector2 direction = target.Center;
                Vector2 smokeSpeed = CEUtils.randomPointInCircle(6);
                //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(direction, smokeSpeed, new Color(60, 60, 200), Main.rand.NextFloat(1f, 1.4f)).Configure(0.8f, 30, 0.008f, true, 0.01f, true);
            }
            float sparkCount = 64;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(5, 26);
                int sparkLifetime2 = Main.rand.Next(9, 12);
                float sparkScale2 = Main.rand.NextFloat(1, 1.6f);
                Color sparkColor2 = Color.Lerp(Color.LightSkyBlue, Color.AliceBlue, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.3f, target.height * 0.3f), sparkVelocity2, sparkColor2, sparkScale2).Configure(false, sparkLifetime2);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= 0.2f * Charge;
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center - new Vector2(120, 0) * Charge * Projectile.scale, Projectile.Center + new Vector2(120, 0) * Charge * Projectile.scale, 240 * Charge * Projectile.scale, DelegateMethods.CutTiles);
        }
        public void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f) {
            Main.spriteBatch.End();
            Effect effect = CEEffectAssets.Vortex;
            effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
            effect.Parameters["Strength"].SetValue(22);
            effect.Parameters["AspectRatio"].SetValue(1);
            effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
            float fadeOutDistance = 0.06f;
            float fadeOutWidth = 0.3f;
            effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
            effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
            effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(CEExtraAssets.VoronoiShapes, pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, CEExtraAssets.VoronoiShapes.Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
            CEUtils.DrawGlow(pos, Color.White * 0.4f * glow, 0.8f * Size * glow);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 jpos = Projectile.Center + new Vector2(0, Projectile.velocity.X > 0 ? -10 : 10).RotatedBy(Projectile.rotation) + Projectile.rotation.ToRotationVector2() * 80;

            return targetHitbox.Intersects(jpos.getRectCentered(Charge * 220 * Projectile.scale, Charge * 220 * Projectile.scale));
        }

    }
    public class OblivionThresherShoot : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.timeLeft = 800;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float Charge = Projectile.ai[0];
            return targetHitbox.Intersects(Projectile.Center.getRectCentered(Charge * 220 * Projectile.scale, Charge * 220 * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor) {
            DrawSaw();
            return false;
        }
        public void DrawSaw() {
            float Charge = Projectile.ai[0];
            void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f) {
                Main.spriteBatch.End();
                Effect effect = CEEffectAssets.Vortex;
                effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
                effect.Parameters["Strength"].SetValue(22);
                effect.Parameters["AspectRatio"].SetValue(1);
                effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
                float fadeOutDistance = 0.06f;
                float fadeOutWidth = 0.3f;
                effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
                effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
                effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(CEExtraAssets.VoronoiShapes, pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, CEExtraAssets.VoronoiShapes.Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
                CEUtils.DrawGlow(pos, Color.White * glow * 0.6f, 1.4f * Size * glow);
            }
            Vector2 shakeOffset = CEUtils.randomPointInCircle(Charge * 3);
            Vector2 jpos = Projectile.Center + shakeOffset;
            if (Charge > 0) {
                DrawVortex(jpos, new Color(60, 50, 250), 1 * Charge);
                DrawVortex(jpos, new Color(90, 110, 255) * Charge * 0.8f, 2.6f, 0.4f);
            }
            Texture2D j1 = OblivionThresherHoldout.ThresherShootE1Tex.Value;
            Texture2D j2 = OblivionThresherHoldout.ThresherShootE2Tex.Value;
            Texture2D s = CEExtraAssets.SemiCircularSmear;
            if (Charge >= 1f) {
                Color c = Color.Lerp(Color.White, Color.Blue, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f) * 0.5f + 0.5f);

                Main.EntitySpriteDraw(j1, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * 0.6f, j1.Size() / 2f, Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f, Main.GameUpdateCount * 0.6f + MathHelper.Pi * 0.6f, s.Size() / 2f, Projectile.scale * 1.4f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }
            if (Charge >= 0.5f) {
                Color c = Color.Lerp(Color.Blue, Color.White, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 10f) * 0.5f + 0.5f);
                Main.EntitySpriteDraw(j2, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * -0.6f, j2.Size() / 2f, Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f, Main.GameUpdateCount * -0.6f - MathHelper.Pi * 0.6f, s.Size() / 2f, Projectile.scale * 0.84f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }
        }
        public override bool ShouldUpdatePosition() {
            return Projectile.ai[2] <= 0;
        }
        public override void CutTiles() {
            float Charge = Projectile.ai[0];
            Utils.PlotTileLine(Projectile.Center - new Vector2(120, 0) * Charge * Projectile.scale, Projectile.Center + new Vector2(120, 0) * Charge * Projectile.scale, 240 * Charge * Projectile.scale, DelegateMethods.CutTiles);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 90, 3.6f, 1000, 16);
            if (Projectile.localAI[1] < 80) {
                if (Projectile.localAI[1] < 62) {
                    Projectile.localAI[1] += 6f;
                }
                else {
                    Projectile.localAI[1] = 62;
                }
            }
            if (Projectile.ai[1] + 1 != target.whoAmI && Projectile.ai[2] <= -10) {
                Projectile.ai[1] = target.whoAmI + 1;
                Projectile.ai[2] = 8;
            }
            CEUtils.PlaySound("slice", 1f + Projectile.numHits * 0.1f, Projectile.Center, volume: 0.7f);
            for (int i = 0; i < 6; i++) {
                Vector2 direction = target.Center;
                Vector2 smokeSpeed = CEUtils.randomPointInCircle(6);
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(direction, smokeSpeed, new Color(60, 60, 200), Main.rand.NextFloat(1f, 1.4f)).Configure(0.8f, 30, 0.008f, true, 0.01f, true);
            }
            float sparkCount = 64;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(5, 26);
                int sparkLifetime2 = Main.rand.Next(9, 12);
                float sparkScale2 = Main.rand.NextFloat(1, 1.6f);
                Color sparkColor2 = Color.Lerp(Color.LightSkyBlue, Color.AliceBlue, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.3f, target.height * 0.3f), sparkVelocity2, sparkColor2, sparkScale2).Configure(false, sparkLifetime2);
            }
        }
        public override void AI() {
            if (Main.GameUpdateCount % 3 == 0 && Projectile.ai[0] > 0.6f) {
                PRTLoader.NewParticle<PRT_ShineParticle>(
                    Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(60, 70) * Projectile.scale * Projectile.ai[0],
                    Projectile.velocity, Color.LightBlue, Projectile.scale * Projectile.ai[0] * Main.rand.NextFloat(0.6f, 1.2f))
                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
            }
            Projectile.ai[2]--;
            Projectile.localAI[1]++;
            if (Projectile.localAI[1] > 82 && Projectile.GetOwner().ownedProjectileCounts[ModContent.ProjectileType<OblivionThresherShootAlt>()] > 0) {
                Projectile.velocity += (Projectile.GetOwner().mouseWorld() - Projectile.Center).normalize() * 3f;
                Projectile.velocity *= 0.9f;
            }
            else if (Projectile.ai[0] >= 0.5f) {
                if (Projectile.localAI[1] >= 80) {
                    if (Projectile.localAI[1] == 81 && Projectile.numHits > 0) {
                        if (Main.myPlayer == Projectile.owner) {
                            for (int i = 0; i < int.Min(10, (int)(Projectile.numHits * Projectile.ai[0])) * 2; i++) {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomPointInCircle(26), ModContent.ProjectileType<VoidStarF>(), Projectile.damage / 4, Projectile.knockBack, Projectile.owner).ToProj().DamageType = Projectile.DamageType;
                            }
                        }
                        CEUtils.PlaySound("voidseekercrit", 1f, Projectile.Center);
                        PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, new Color(200, 136, 255), 0.2f).Configure(new Vector2(2f, 2f), 0, 1.2f * Projectile.scale * Projectile.ai[0] * (int.Min(Projectile.numHits, 10) / 10f), 36);
                    }
                    Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * Utils.Remap(CEUtils.getDistance(Projectile.Center, Projectile.GetOwner().Center), 500, 1400, 2.2f, 3);
                    Projectile.velocity *= 0.9f;
                    if (CEUtils.getDistance(Projectile.GetOwner().Center, Projectile.Center) < Projectile.velocity.Length() + 64) {
                        CEUtils.PlaySound("Dizzy", 1f, Projectile.Center);
                        Projectile.Kill();
                    }
                }
            }
        }

    }
    public class OblivionThresherShootAlt : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public bool OnJaw = true;
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.timeLeft = 1000;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float Charge = 2f;
            return targetHitbox.Intersects(Projectile.Center.getRectCentered(Charge * 220 * Projectile.scale, Charge * 220 * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor) {
            DrawSaw();
            return false;
        }
        public void DrawSaw() {
            float ap = (Projectile.timeLeft / 40f);
            if (ap > 1)
                ap = 1;
            float Charge = 2f * ap;
            void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f, float strength = 1) {
                Main.spriteBatch.End();
                Effect effect = CEEffectAssets.Vortex;
                effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
                effect.Parameters["Strength"].SetValue(16 * strength);
                effect.Parameters["AspectRatio"].SetValue(1);
                effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
                float fadeOutDistance = 0.06f;
                float fadeOutWidth = 0.3f;
                effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
                effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
                effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(CEExtraAssets.VoronoiShapes, pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12 * Math.Sign(strength), CEExtraAssets.VoronoiShapes.Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
                CEUtils.DrawGlow(pos, Color.White * glow * 0.7f, 0.7f * Size * glow);
                CEUtils.DrawGlow(pos, Color.White * glow * 0.4f, 4f * Size * glow);
            }
            Vector2 shakeOffset = CEUtils.randomPointInCircle(Charge * 3);
            Vector2 jpos = Projectile.Center + shakeOffset;
            if (Charge > 0) {
                DrawVortex(jpos, new Color(40, 50, 250), 1 * Charge);
                DrawVortex(jpos, new Color(50, 40, 255) * Charge * 0.8f, 3f * ap, 0.2f);
                DrawVortex(jpos, new Color(46, 40, 255) * Charge, 4f, 0.2f, 1.5f);
                DrawVortex(jpos, new Color(46, 40, 255) * Charge, 4f, 0.2f, -1.5f);
            }
            Texture2D j1 = OblivionThresherHoldout.ThresherShootE1Tex.Value;
            Texture2D j2 = OblivionThresherHoldout.ThresherShootE2Tex.Value;
            Texture2D s = CEExtraAssets.SemiCircularSmear;
            if (Charge >= 1f) {
                Color c = Color.Lerp(Color.White, Color.Blue, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f) * 0.5f + 0.5f);

                Main.EntitySpriteDraw(j1, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * 0.6f, j1.Size() / 2f, Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f, Main.GameUpdateCount * 0.6f + MathHelper.Pi * 0.6f, s.Size() / 2f, Projectile.scale * 1.4f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }
            if (Charge >= 0.5f) {
                Color c = Color.Lerp(Color.Blue, Color.White, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 10f) * 0.5f + 0.5f);
                Main.EntitySpriteDraw(j2, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * -0.6f, j2.Size() / 2f, Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f, Main.GameUpdateCount * -0.6f - MathHelper.Pi * 0.6f, s.Size() / 2f, Projectile.scale * 0.84f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }
        }
        public override bool ShouldUpdatePosition() {
            return Projectile.ai[2] <= 0;
        }
        public override void CutTiles() {
            float Charge = 1.4f;
            Utils.PlotTileLine(Projectile.Center - new Vector2(120, 0) * Charge * Projectile.scale, Projectile.Center + new Vector2(120, 0) * Charge * Projectile.scale, 240 * Charge * Projectile.scale, DelegateMethods.CutTiles);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 90, 3.6f, 1000, 16);
            CEUtils.PlaySound("slice", 1.1f, Projectile.Center, 18, 0.8f);
            for (int i = 0; i < 6; i++) {
                Vector2 direction = target.Center;
                Vector2 smokeSpeed = CEUtils.randomPointInCircle(6);
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(direction, smokeSpeed, new Color(60, 60, 200), Main.rand.NextFloat(1f, 1.4f)).Configure(0.8f, 30, 0.008f, true, 0.01f, true);
            }
            float sparkCount = 64;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(5, 26);
                int sparkLifetime2 = Main.rand.Next(9, 12);
                float sparkScale2 = Main.rand.NextFloat(1, 1.6f);
                Color sparkColor2 = Color.Lerp(Color.LightSkyBlue, Color.AliceBlue, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.3f, target.height * 0.3f), sparkVelocity2, sparkColor2, sparkScale2).Configure(false, sparkLifetime2);
            }
        }
        public override void AI() {
            if (OnJaw) {
                int t = ModContent.ProjectileType<OblivionThresherHoldout>();
                if (Projectile.owner == Main.myPlayer && Projectile.GetOwner().ownedProjectileCounts[t] <= 0) {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.GetOwner().Center, Projectile.velocity, ModContent.ProjectileType<OblivionThresherHoldout>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                Projectile e = null;

                foreach (var p in Main.ActiveProjectiles) {
                    if (p.owner == Projectile.owner && p.type == t) {
                        e = p;
                        break;
                    }
                }
                if (e != null && e.ModProjectile is OblivionThresherHoldout ot) {
                    Projectile.Center = ot.WPos;
                }
                Projectile.timeLeft = 540;
            }
            if (Main.myPlayer == Projectile.owner && OnJaw) {
                if (Main.mouseLeft) {
                    Projectile.velocity = (Main.MouseWorld - Projectile.Center).normalize() * 26;
                    Projectile.netUpdate = true;
                    SoundStyle ShootSound = new("CalamityEntropy/Assets/Sounds/SawShot", 2) { PitchVariance = 0.1f, Volume = 1f };
                    SoundEngine.PlaySound(ShootSound, Projectile.Center);
                    OnJaw = false;
                    Projectile.GetOwner().velocity = Projectile.velocity * -0.4f;
                }
            }
            if (!OnJaw) {
                Vector2 ver = Projectile.velocity * -0.24f;
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(120 * Projectile.scale), ver, Color.AliceBlue, Main.rand.NextFloat(0.6f, 1f))
                    .Configure(0.6f, lifetime: 60);
                if (Projectile.timeLeft % 24 == 0) {
                    if (Main.myPlayer == Projectile.owner) {
                        for (int i = 0; i < 8; i++) {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomPointInCircle(26), ModContent.ProjectileType<VoidStarF>(), Projectile.damage / 10, Projectile.knockBack, Projectile.owner).ToProj().DamageType = Projectile.DamageType;
                        }
                    }
                }
                if (Projectile.localAI[2]++ > 60) {
                    Projectile.velocity += (Projectile.GetOwner().mouseWorld() - Projectile.Center).normalize() * 1f;
                    Projectile.velocity *= 0.9f;
                }
            }
            Projectile.velocity *= 0.96f;
        }

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(OnJaw);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            OnJaw = reader.ReadBoolean();
        }

        public override bool? CanHitNPC(NPC target) {
            if (OnJaw)
                return false;
            return null;
        }

    }
}


