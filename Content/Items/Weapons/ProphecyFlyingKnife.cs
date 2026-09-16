using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
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
    public class ProphecyFlyingKnife : ModItem, ICEChargeWeapon
    {
        // 命中计数 10；原潜伏乘数 伤害2.4/弹速3/击退2 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(10, 2.4f, 3f, 2f);

        public int atkType = 1;
        public override void SetStaticDefaults() {
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults() {
            Item.width = 60;
            Item.height = 60;
            Item.damage = 30;
            Item.ArmorPenetration = 30;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2.8f;
            Item.UseSound = SoundID.Item1;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<FutureKnife>();
            Item.shootSpeed = 26f;
            Item.DamageType = DamageClass.Melee;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<PFKnifeHeld>(), 0, 0, player.whoAmI, atkType);

            if (CEChargeWeapon.TryConsume(player, Item)) {
                int p = Projectile.NewProjectile(source, position, velocity, type, damage * 2, knockback, player.whoAmI);
                if (p >= 0 && p < Main.maxProjectiles) {
                    CEChargeWeapon.Empower(p);
                }
                Projectile.NewProjectile(source, position, velocity.RotatedBy(0.2f), type, (int)(damage * 0.2f), knockback, player.whoAmI);
                Projectile.NewProjectile(source, position, velocity.RotatedBy(-0.2f), type, (int)(damage * 0.2f), knockback, player.whoAmI);
                CEUtils.PlaySound("bne0", 1, position);
                return false;
            }
            else {
                CEUtils.PlaySound("HiltAttack", Main.rand.NextFloat(1.5f, 1.72f), position);
                float r = 0.6f;
                type = ModContent.ProjectileType<FutureKnifeSpin>();
                NPC target = CEUtils.FindTarget_HomingProj(player, Main.MouseWorld, 800);
                for (int i = 0; i < 4; i++) {
                    float c = (atkType < 0 ? (Math.Abs(r - ((i / 4f) * r - r * 0.5f))) : (Math.Abs(r - (((8 - i) / 4f) * r - r * 0.5f))));
                    c = 1f / c;
                    Vector2 tp = (target != null ? target.Center : Main.MouseWorld) + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(260, 300);
                    Projectile.NewProjectile(source, position, velocity.RotatedBy((i / 4f) * r - r * 0.5f) * c, type, (int)(damage * 1.4f), knockback, player.whoAmI, tp.X, tp.Y);
                }
            }

            atkType *= -1;
            return false;
        }
    }
    public class PFKnifeHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/ProphecyFlyingKnife";
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
            Projectile.MaxUpdates = 14;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;

        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override void AI() {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            counter++;

            odr.Add(Projectile.rotation);
            Projectile.timeLeft = 3;
            float RotF = 3.2f;
            alpha = 1;
            scale = 1f;
            float cr = MathHelper.ToRadians(90);
            if (progress <= 0.5f) {
                Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + CEUtils.Parabola(progress, RotF + cr)) * Projectile.ai[0];
            }
            else {
                Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * 0.5f + cr - CEUtils.GetRepeatedCosFromZeroToOne(2 * (progress - 0.5f), 1) * cr) * Projectile.ai[0];
            }
            Projectile.Center = Projectile.GetOwner().MountedCenter;


            if (odr.Count > 2600) {
                odr.RemoveAt(0);
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
            if (counter > MaxUpdateTimes) {
                Projectile.Kill();
            }
            odr.Add(Projectile.rotation);
            if (odr.Count > 32) {
                odr.RemoveAt(0);
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();

            int dir = (int)(Projectile.ai[0]);
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale, effect);

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (160 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (160 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, 54, DelegateMethods.CutTiles);
        }
    }

    public class FutureKnifeSpin : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Projectiles/FutureKnife";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 36;
            Projectile.timeLeft = 90;
        }
        public override bool? CanDamage() {
            return Projectile.localAI[0] > 12 ? null : false;
        }
        public Vector2 targetPos { get { return new Vector2(Projectile.ai[0], Projectile.ai[1]); } set { Projectile.ai[0] = value.X; Projectile.ai[1] = value.Y; } }
        public PRT_TrailParticle trail;
        public override void AI() {
            Projectile.GetOwner().Entropy().MouseWorldListener = true;
            if (Projectile.Entropy().FirstFrames) {
                Projectile.velocity = (targetPos - Projectile.Center) / 12f;
            }
            if (Projectile.localAI[0]++ >= 12) {
                sAlpha *= 0.8f;
                if (Projectile.localAI[0] > 20) {
                    if (Projectile.localAI[0] == 21) {
                        NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1600);
                        if (target != null) {
                            float tr = (target.Center - Projectile.Center).ToRotation();
                            Projectile.rotation = tr;
                        }
                        else {
                            Projectile.rotation = (Projectile.GetOwner().Entropy().MouseWorld - Projectile.Center).ToRotation();
                        }
                        Projectile.velocity = Projectile.rotation.ToRotationVector2() * 40;
                        Projectile.MaxUpdates *= 2;
                    }
                    //dedServ时NewParticle给孤儿实例不是null,后面字段赋值照常别挡
                    PRTLoader.NewParticle<PRT_RuneParticle>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-0.34f, 0.34f), Color.Aqua, 0.5f).Configure(0.5f, true, PRTDrawModeEnum.AlphaBlend, 0);
                    if (!Main.dedServ) {
                        if (trail == null) {
                            //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                            trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Color.Aqua * 0.6f, Projectile.scale * 0.6f * (Projectile.IsEmpowered() ? 2 : 1));
                            trail.maxLength = 16;
                            trail.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);
                        }
                        trail.Lifetime = 16;
                        trail.AddPoint(Projectile.Center + Projectile.velocity);
                    }
                }
                else {
                    Projectile.velocity *= 0;
                    NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1600);
                    if (target != null) {
                        float tr = (target.Center - Projectile.Center).ToRotation();
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, tr, 0.2f, false);
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, tr, 0.04f, true);
                    }
                    else {
                        float tr = (Projectile.GetOwner().Entropy().MouseWorld - Projectile.Center).ToRotation();
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, tr, 0.2f, false);
                        Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, tr, 0.04f, true);
                    }
                }
            }
            else {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.localAI[0] <= 12) {
                Main.spriteBatch.Draw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, Color.White, Main.GlobalTimeWrappedHourly * 64, Projectile.GetTexture().Size().Half(), Projectile.scale, SpriteEffects.None, 0);
            }
            else {
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White));
            }
            Main.spriteBatch.UseAdditive();
            Texture2D t = CEExtraAssets.CircularSmear;
            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.Aqua * sAlpha, Main.GlobalTimeWrappedHourly * 64 + MathHelper.PiOver2 * 1.5f, t.Size().Half(), Projectile.scale * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.Aqua * sAlpha, Main.GlobalTimeWrappedHourly * 64 + MathHelper.PiOver2 * 1.5f, t.Size().Half(), Projectile.scale * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public float sAlpha = 1;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("HIT", Main.rand.NextFloat(1.4f, 1.7f), target.Center, volume: 0.6f);
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1f, 1.3f), target.Center, volume: 0.5f);

            for (int i = 0; i < 5; i++)
                //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.Aqua : Color.SkyBlue, 0.04f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
        }
    }
}
