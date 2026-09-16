using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class TerrorOfAbyss : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 24;
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;
            Item.height = 48;
            Item.useTime = Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TerrorOfAbyssHeld>();
            Item.shootSpeed = 12f;
        }
        public int atkType = 1;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType == 0 ? -1 : atkType);
            atkType *= -1;
            return false;
        }

        public override bool MeleePrefix() {
            return true;
        }
    }
    public class TerrorOfAbyssHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/TerrorOfAbyss";
        List<float> odr = new List<float>();
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
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.MaxUpdates = 8;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("antivoidhit", 1, target.Center);
            target.AddBuff(ModContent.BuffType<HeavyBleeding>(), 520);
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 520);
            float sparkCount = 42;
            for (int i = 0; i < sparkCount; i++) {
                float p = Main.rand.NextFloat();
                Vector2 sparkVelocity2 = (target.Center - Projectile.Center).normalize().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(6, 14 * (2 - p)) * 1f;
                int sparkLifetime2 = (int)((2 - p) * 8);
                float sparkScale2 = 0.5f * (0.6f + (1 - p));
                Color sparkColor2 = Color.Lerp(new Color(102, 209, 224), new Color(15, 42, 77), p);
                if (Main.rand.NextBool()) {
                    //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + sparkVelocity2, sparkVelocity2, sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                }
                else {
                    //LineCal分支frame==7时寿命系数不同,Configure(false,life)照抄Calamity
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + sparkVelocity2, sparkVelocity2 * (Projectile.frame == 7 ? 1f : 0.65f), Main.rand.NextBool() ? Color.DarkBlue : Color.DeepSkyBlue, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2 * (Projectile.frame == 7 ? 1.2f : 1f)));
                }
            }
        }
        public override void AI() {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            counter++;
            if (init) {
                CEUtils.PlaySound("Dizzy", 1 + Projectile.ai[0] * 0.08f, Projectile.Center);
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                init = false;

            }
            Projectile.timeLeft = 3;
            float RotF = 4.4f;
            alpha = 1;
            scale = 1f;
            float cr = MathHelper.ToRadians(40);
            if (progress <= 0.5f) {
                Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + CEUtils.Parabola(progress, RotF + cr)) * Projectile.ai[0];
            }
            else {
                Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * 0.5f + cr - CEUtils.GetRepeatedCosFromZeroToOne(2 * (progress - 0.5f), 1) * cr) * Projectile.ai[0];
            }
            Projectile.Center = Projectile.GetOwner().MountedCenter;

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
            if (counter > MaxUpdateTimes) {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
            if (progress <= 0.38f) {
                odr.Add(Projectile.rotation);
                if (odr.Count > 64) {
                    odr.RemoveAt(0);
                }
            }
            else {
                if (odr.Count > 0) {
                    odr.RemoveAt(0);
                }
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D trail = CEExtraAssets.MotionTrail2;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);

            for (int i = 0; i < odr.Count; i++) {
                Color b = new Color(20, 20, 65);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(86 * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition,
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = CEEffectAssets.SwordTrail;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue((new Color(80, 80, 160)).ToVector4());
                shader.Parameters["color1"].SetValue((new Color(40, 40, 100)).ToVector4());
                shader.Parameters["alpha"].SetValue(1 - progress);
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Main.spriteBatch.ExitShaderRegion();
            }


            int dir = (int)(Projectile.ai[0]);
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver2 : Projectile.rotation + MathHelper.PiOver2;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale, effect);

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (90 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (90 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, 54, DelegateMethods.CutTiles);
        }
    }
}
