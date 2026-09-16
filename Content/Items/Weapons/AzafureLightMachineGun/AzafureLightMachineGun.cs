using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Weapons;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.AzafureLightMachineGun
{
    public class AzafureLightMachineGun : ModItem, ICEChargeWeapon, IAzafureEnhancable
    {
        // 命中计数 30；原潜伏乘数 伤害2/击退2 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(30, 2f, knockbackMult: 2f);

        public override void SetDefaults() {
            Item.damage = 30;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 82;
            Item.height = 32;
            Item.useTime = 8;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 4;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AzafureLightMachineGunHeld>();
            Item.shootSpeed = 26;
            Item.channel = true;
            Item.noUseGraphic = true;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Minishark)
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient(ItemID.HallowedBar, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
        public override void HoldItem(Player player) {
            //阿扎弗强化效果本体(文案键AzafureEnhances.AzafureLightMachineGun):大招充能速度+50%
            if (player.AzafureEnhance())
                player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.5f;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            bool ult = CEChargeWeapon.TryConsume(player, Item);
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
            if (ult && p >= 0 && p < Main.maxProjectiles) {
                CEChargeWeapon.Empower(p);
            }
            return false;
        }
    }
    public class AzafureLightMachineGunHeld : ModProjectile
    {
        public float rotup = 0;
        public float rotv = 0.16f;
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void SetDefaults() {
            Projectile.HeldProjSetDefaults(DamageClass.Ranged);
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override void AI() {
            Projectile.GetOwner().Entropy().MouseWorldListener = true;
            Player player = Projectile.GetOwner();
            if (player.dead) {
                Projectile.Kill();
                return;
            }
            if (Projectile.IsEmpowered()) {
                rotup += rotv;
                rotv *= 0.8f;
                rotup *= 0.82f;
                if (Projectile.ai[0]++ == 0) {
                    CEUtils.PlaySound("AAGShot", 1.55f, Projectile.Center, 2, 0.41f);
                    Projectile.timeLeft = 32;
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.velocity.normalize() * 32, Projectile.velocity, ModContent.ProjectileType<AzafureLightMachineGunStealth>(), Projectile.damage * 6, Projectile.knockBack * 6, Projectile.owner).ToProj().SetEmpowered();
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center - Projectile.velocity.normalize() * 2, Projectile.velocity.RotatedBy(-2.3f * player.direction).normalize() * 12, ModContent.ProjectileType<ALMGShell>(), 0, 0, Projectile.owner);
                    }
                }
                player.Entropy().MouseWorldListener = true;
                Projectile.rotation = (player.Entropy().MouseWorld - player.Center).ToRotation();
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 16;
                player.SetHandRot(((player.Entropy().MouseWorld - player.Center).ToRotation().ToRotationVector2() + new Vector2(0, 1f)).ToRotation());
                player.itemAnimation = player.itemTime = 4;
                player.heldProj = Projectile.whoAmI;
                Projectile.Center = player.GetDrawCenter() + Projectile.rotation.ToRotationVector2() * 24;
                return;
            }

            if (player.channel) {
                Projectile.timeLeft = 4;
                player.Entropy().MouseWorldListener = true;
                Projectile.rotation = (player.Entropy().MouseWorld - player.Center).ToRotation();
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 16;
                player.SetHandRot(((player.Entropy().MouseWorld - player.Center).ToRotation().ToRotationVector2() + new Vector2(0, 1f)).ToRotation());
                player.itemAnimation = player.itemTime = 4;
                player.heldProj = Projectile.whoAmI;
                if (Projectile.ai[2]-- <= 0) {
                    Projectile.ai[2] = 4;
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.velocity.normalize() * 32, Projectile.velocity, ModContent.ProjectileType<ALMGLaser>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

                    }
                    if (!Main.dedServ) {
                        Main.gore[Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy(-2.3f * player.direction).normalize() * 4, Mod.Find<ModGore>("ALMGShellGore").Type)].timeLeft = 100;
                    }
                }
            }
            else {
                Projectile.Kill();
            }
            Projectile.Center = player.GetDrawCenter() + Projectile.rotation.ToRotationVector2() * 24;
        }
        public override void OnKill(int timeLeft) {
            Projectile.GetOwner().itemTime = Projectile.GetOwner().itemAnimation = 0;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D t = Projectile.GetTexture();
            Main.EntitySpriteDraw(t, Projectile.Center - Main.screenPosition - Projectile.rotation.ToRotationVector2() * 10, CEUtils.GetCutTexRect(t, 2, (int)Main.GameUpdateCount / 4 % 2, false), lightColor, Projectile.rotation + (Math.Sign(Projectile.velocity.X) * -rotup), t.Size() / new Vector2(2, 4), Projectile.scale, (Projectile.velocity.X > 0) ? SpriteEffects.None : SpriteEffects.FlipVertically);

            return false;
        }
    }
    public class ALMGLaser : ModProjectile
    {
        //激光线遮罩贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/MaskLaserLine")]
        internal static Asset<Texture2D> MaskLaserLineTex;
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.light = 0.6f;
            Projectile.timeLeft = 12;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public float dist = 0;
        public override void AI() {

            if (Projectile.ai[0]++ == 0) {
                Vector2 mousew = Projectile.GetOwner().Entropy().MouseWorld;
                Projectile.Center = Projectile.GetOwner().GetDrawCenter();
                Projectile.velocity = new Vector2(8, 0).RotatedBy((mousew - Projectile.Center).ToRotation());
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.Center += (mousew - Projectile.Center).normalize() + new Vector2(60, -8 * (Projectile.velocity.X > 0 ? 1 : -1)).RotatedBy(Projectile.rotation);
                dist = 0;
                for (int i = 0; i < 4; i++) {
                    Vector2 top = Projectile.Center;
                    Vector2 velocity = Projectile.velocity;
                    Vector2 sparkVelocity2 = velocity.normalize().RotateRandom(0.8f) * Main.rand.NextFloat(6f, 36f);
                    int sparkLifetime2 = Main.rand.Next(6, 8);
                    float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                    var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                    //dedServ时NewParticle给孤儿实例不是null,后面字段赋值照常别挡
                    PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                }

                List<NPC> checkNpcs = new();
                foreach (NPC n in Main.ActiveNPCs) {
                    if (!n.dontTakeDamage && !n.friendly && CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 1800, n.getRect(), 6)) {
                        checkNpcs.Add(n);
                    }
                }
                for (float d = 0; d < 1800; d += 4) {
                    dist = d;
                    CEUtils.AddLight(Projectile.Center + Projectile.rotation.ToRotationVector2() * dist, new Color(255, 120, 120), 0.5f);
                    if (!CEUtils.isAir(Projectile.Center + Projectile.rotation.ToRotationVector2() * d)) {
                        break;
                    }
                    bool brk = false;
                    foreach (var n in checkNpcs) {
                        if ((Projectile.Center + Projectile.rotation.ToRotationVector2() * d).getRectCentered(6, 6).Intersects(n.Hitbox)) {
                            dist += 4;
                            brk = true;
                            break;
                        }
                    }
                    if (brk)
                        break;
                }
                for (int i = 0; i < 10; i++) {
                    Vector2 top = Projectile.Center + Projectile.rotation.ToRotationVector2() * dist;
                    Vector2 velocity = -Projectile.velocity;
                    Vector2 sparkVelocity2 = velocity.normalize().RotateRandom(1.2f) * Main.rand.NextFloat(12f, 36f);
                    int sparkLifetime2 = Main.rand.Next(6, 8);
                    float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                    var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                    //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                    PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                }

                for (float i = 0; i < 1; i += 0.02f) {
                    var shineR = PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 500 * i, Vector2.Zero, Color.Red, 0.25f * (1 - i));
                    shineR.drawScale = new Vector2(3, 1);
                    shineR.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), 8);
                    var shineW = PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 500 * i, Vector2.Zero, Color.White, 0.18f * (1 - i));
                    shineW.drawScale = new Vector2(3, 1);
                    shineW.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), 8);
                    if (i * 500 > dist || i > 0.8f) break;
                }

                Vector2 edp = Projectile.Center + Projectile.rotation.ToRotationVector2() * dist;
                PRTLoader.NewParticle<PRT_ShineParticle>(edp, Vector2.Zero, Color.Red, 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 8);
                PRTLoader.NewParticle<PRT_ShineParticle>(edp, Vector2.Zero, Color.White, 0.2f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 8);
                CEUtils.PlaySound("gunshot", Main.rand.NextFloat(1.3f, 1.6f), Projectile.Center, 6, 0.25f);
            }

        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * dist, targetHitbox, 6);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D tex = MaskLaserLineTex.Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 255, 255), Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(dist / tex.Width, Projectile.scale * 0.3f * (Projectile.timeLeft / 12f)), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 10, 10), Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(dist / tex.Width, Projectile.scale * 0.5f * (Projectile.timeLeft / 12f)), SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void OnKill(int timeLeft) {
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<MechanicalTrauma>(60);
        }
    }

    public class AzafureLightMachineGunStealth : ModProjectile
    {
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<MechanicalTrauma>(320);
        }
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, true, 1);
            Projectile.width = Projectile.height = 16;
            Projectile.extraUpdates = 5;
        }
        public PRT_TrailParticle trail;
        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.ai[0]++ == 0) {
                Vector2 mousew = Projectile.GetOwner().Entropy().MouseWorld;
                Projectile.Center = Projectile.GetOwner().GetDrawCenter();
                Projectile.velocity = new Vector2(8, 0).RotatedBy((mousew - Projectile.Center).ToRotation());
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.Center += (mousew - Projectile.Center).normalize() + new Vector2(60, -8 * (Projectile.velocity.X > 0 ? 1 : -1)).RotatedBy(Projectile.rotation);

                for (int i = 0; i < 16; i++) {
                    Vector2 top = Projectile.Center;
                    Vector2 velocity = Projectile.velocity;
                    Vector2 sparkVelocity2 = velocity.normalize().RotateRandom(0.22f) * Main.rand.NextFloat(6f, 36f);
                    int sparkLifetime2 = Main.rand.Next(6, 8);
                    float sparkScale2 = Main.rand.NextFloat(0.6f, 1.4f);
                    var sparkColor2 = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(0, 1));

                    PRTLoader.NewParticle<PRT_LineCal>(top, sparkVelocity2, sparkColor2, sparkScale2).Configure(false, (int)(sparkLifetime2));
                }
                //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, new Color(255, 120, 120), 0.6f);
                trail.maxLength = 40;
                trail.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);

            }
            trail.Lifetime = 13;
            trail.AddPoint(Projectile.Center + Projectile.velocity);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.FinalDamage *= 0.1f;
        }
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("pulseBlast", 0.95f, Projectile.Center, 6, 0.55f);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(2.4f, 8);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick, 6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            if (Projectile.owner == Main.myPlayer) {
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage, 180, Projectile.DamageType);
            }
            for (int i = 0; i < 32; i++) {
                var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Firework_Yellow);
                d.scale = 0.8f;
                d.velocity = CEUtils.randomPointInCircle(14);
                d.position += d.velocity * 4;
            }
        }
    }
}
