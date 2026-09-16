using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class TectonicChainBlade : BaseWhipItem
    {
        public override int TagDamage => 3;
        public int UseCount = 0;

        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<TectonicChainBladeWhip>(), 22, 3, 4, 28);
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.buyPrice(gold: 10);
            Item.autoReuse = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            float swingDirection = 0.6f + (0.4f * Main.rand.NextFloat());
            if (Main.rand.NextBool(3)) {
                swingDirection *= -2.5f;
            }
            UseCount++;
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0, UseCount % 3 == 0 ? -1 : swingDirection, (Vector2.UnitX.RotatedBy(1f * (Main.rand.NextBool() ? 1 : -1)).RotatedByRandom(0.8f)).ToRotation());
            return false;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<TectonicShard>(8)
                .AddIngredient(ItemID.HellstoneBar, 3)
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }
    public class TectonicChainBladeWhip : BaseWhip
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = 7;
            this.segments = 10;
            this.rangeMult = 2f;
        }
        public override string getTagEffectName => "TectonicChainBlade";
        public override SoundStyle? WhipSound => Projectile.ai[1] == -1 ? CEUtils.GetSound("YharonFireball1", Main.rand.NextFloat(1.4f, 1.8f), 12, 1f) : CEUtils.GetSound("ApoctosisShoot", Main.rand.NextFloat(0.75f, 0.9f), 12, 0.6f);
        public override Color StringColor => new Color(230, 100, 60);
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff(ModContent.BuffType<TectonicChainBladeWhipDebuff>(), 240);
            target.AddBuff(BuffID.OnFire3, 300);

            float scale = 1.5f;
            for (int i = 0; i < 42; i++) {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.3f, 1f) * scale * 1.6f;
                dust.velocity = CEUtils.randomPointInCircle(30);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.Orange : Color.OrangeRed;
                dust.fadeIn = 2f;
            }

            CEUtils.PlaySound("lightHit2", Main.rand.NextFloat(0.9f, 1.3f), target.Center, 8, 1f);
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(2f, 2.3f), target.Center, 8, 0.24f);
            scale = 1.6f;
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.OrangeRed * 0.8f, scale * 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
            PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 21);
            PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 16);
            PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 13);
        }
        public Vector2 LastPoint = Vector2.Zero;
        public override bool PreAI() {
            var points = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, points);
            if (LastPoint != Vector2.Zero) {
                if ((FlyProgress > 0.4f && FlyProgress < 0.88f) || Projectile.ai[1] == -1 && (FlyProgress > 0.16f && FlyProgress < 0.92f)) {
                    float scale = 1.5f;
                    for (int i = 0; i < 2; i++) {
                        Dust dust = Dust.NewDustPerfect(Vector2.Lerp(LastPoint + Projectile.GetOwner().MountedCenter, EndPoint, 0.5f * (1 + i)), ModContent.DustType<SquashDust>(), Vector2.Zero);
                        dust.scale = Main.rand.NextFloat(0.6f, 1f) * scale * 1.4f;
                        dust.velocity = (EndPoint - LastPoint - Projectile.GetOwner().MountedCenter) * Main.rand.NextFloat(0.7f, 1f) * -0.8f;
                        dust.noGravity = true;
                        dust.color = Color.Orange * 1.25f;
                        dust.fadeIn = 2f;
                    }
                }
            }
            LastPoint = EndPoint - Projectile.GetOwner().MountedCenter;
            return base.PreAI();
        }
        public override void ModifyControlPoints(List<Vector2> points) {
            float cm = CEUtils.GetRepeatedCosFromZeroToOne(FlyProgress, 1);
            float vm = CEUtils.Parabola(cm, 1);
            Projectile.GetWhipSettings(Projectile, out float ttfo, out int segCount, out float rangeMul);
            if (Projectile.ai[1] == -1) {
                Vector2 lMid = Projectile.GetOwner().MountedCenter + Projectile.velocity.RotatedBy((cm - 0.5f) * Projectile.ai[2] * 1f).normalize() * Projectile.scale * rangeMul * 90 * vm * Projectile.GetOwner().whipRangeMultiplier;
                Vector2 lEnd = Projectile.GetOwner().MountedCenter + Projectile.velocity.RotatedBy((cm - 0.5f) * Projectile.ai[2] * -0.4f).normalize() * Projectile.scale * rangeMul * 280 * vm * Projectile.GetOwner().whipRangeMultiplier;
                for (int i = 0; i < points.Count; i++) {
                    points[i] = CEUtils.Bezier(new List<Vector2>() { Projectile.GetOwner().MountedCenter, lMid, lEnd }, (i + 1f) / points.Count);
                }
            }
        }
        public override int handleHeight => 66;
        public override int segHeight => 30;
        public override int endHeight => 60;
        public override int segTypes => 1;
    }
    public class TectonicShardHomingSummon : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float alpha = 1;
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/TectonicShardHoming";
        public override LocalizedText DisplayName => Mod.GetLocalization("Projectiles.TectonicShardHoming.DisplayName");
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void PostAI() {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 7) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255) * completionRatio * alpha;
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 12 * Projectile.scale, completionRatio);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(1.8f, 2.4f), Projectile.Center, 8, 0.24f);

            float scale = 50 / 40f;
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 16);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 13);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 11);

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;
            Projectile.timeLeft = 120;
        }
        public override void AI() {
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (Projectile.localAI[0]++ < 50) {
                Projectile.velocity *= 0.96f;
            }
            else {
                if (Projectile.HomingToNPCNearby(4, 0.94f, 2000))
                    if (Projectile.timeLeft < 60)
                        Projectile.timeLeft = 60;
            }
            alpha = float.Min(1, Projectile.timeLeft / 20f);
        }
        public override bool? CanDamage() {
            return Projectile.localAI[0] >= 50;
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Color color = Color.OrangeRed * alpha;
            var mp = this;
            if (mp.odp.Count > 1) {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = color * 0.66f;
                b.A = 255;
                float a = 0;
                float lr = 0;
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 14 * Projectile.scale,
                          new Vector3(0, 1, 1),
                        b * (1f / (float)mp.odp.Count)));
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 14 * Projectile.scale,
                      new Vector3(0, 0, 1),
                      b * (1f / (float)mp.odp.Count)));
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 14 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 14 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                          b * a));
                    lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.wohslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                    ve = new List<ColoredVertex>();
                    b = color;

                    a = 0;
                    lr = 0;
                    for (int i = 1; i < mp.odp.Count; i++) {
                        a += 1f / (float)mp.odp.Count;

                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 8 * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 8 * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                              b * a));
                        lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                    }
                    tx = CEExtraAssets.wohslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            tofs++;

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.Streak1Asset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            if (odp.Count > 1) {
                Texture2D texture = Projectile.GetTexture();
                Rectangle frame = CEUtils.GetCutTexRect(texture, 3, Projectile.whoAmI % 3, false);
                Vector2 position = odp[odp.Count - 1] - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
                Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);
                CEUtils.DrawGlow(position + Main.screenPosition, color, Projectile.scale * 0.6f);
                Main.EntitySpriteDraw(texture, position, frame, Projectile.GetAlpha(Color.White) * alpha, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

            }
            return false;
        }
        public int tofs;
    }
}
