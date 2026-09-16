using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AshesBow : ModItem
    {
        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 72;
            Item.damage = 34;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.crit = 5;
            Item.value = Item.buyPrice(0, 20);
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<AshesBowHoldout>();
            Item.UseSound = null;
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;
            Item.channel = true;
            Item.noUseGraphic = true;
        }
        public override bool RangedPrefix() {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI);
            return false;
        }
    }
    public class AshesBowHoldout : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AshesBow";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
        }
        public float Charge = 0;
        public override bool? CanDamage() => false;
        public float ShootDelay = 0;
        public bool FireBeam = true;

        public override void AI() {
            Player player = Projectile.GetOwner();
            Projectile.StickToPlayer();
            player.SetHandRot(Projectile.rotation);
            if (player.channel || ShootDelay > 0) {
                Projectile.timeLeft = 2;
                if (Charge >= 1)
                    Projectile.timeLeft = 36;
            }
            if (!player.channel) {
                if (FireBeam && Charge >= 1) {
                    ShootDelay = 10;
                    for (int i = 0; i < 64; i++) {
                        var d = Dust.NewDustDirect(Projectile.Center + CEUtils.randomPointInCircle(6) + Projectile.velocity.normalize() * 8, 0, 0, DustID.Flare);
                        d.noGravity = true;
                        d.velocity = Projectile.velocity.normalize().RotatedByRandom(0.12f) * Main.rand.NextFloat(4, 98);
                        d.scale = 1.8f + Main.rand.NextFloat(-0.4f, 0.2f);
                    }
                    FireBeam = false;
                    player.velocity -= Projectile.velocity.normalize() * 8;
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<AshesFireBeam>(), Projectile.damage * 6, Projectile.knockBack * 5, Projectile.owner);
                    }
                }
            }
            player.itemTime = player.itemAnimation = 3;
            if (Charge < 1) {
                if (Main.rand.NextBool(2)) {
                    var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Smoke);
                    d.position = Projectile.Center + Projectile.rotation.ToRotationVector2() * 16 + CEUtils.randomPointInCircle(16);
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1.5f, 1.6f);
                    d.velocity = CEUtils.randomPointInCircle(3) + new Vector2(0, -4);
                }
                Charge += 1 / (172f * Projectile.MaxUpdates);
                if (Charge >= 1) {
                    Charge = 1;
                    SoundStyle s = SoundID.DD2_BetsyFireballShot with { MaxInstances = 12 };
                    SoundEngine.PlaySound(s, Projectile.Center);
                    SoundEngine.PlaySound(s, Projectile.Center);
                    //dedServ时PRTLoader.NewParticle给孤儿实例,Configure照常
                    PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 12, player.velocity, Color.OrangeRed * 0.85f, 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
                    //双Shine叠色,PRTDrawMode走Configure尾参
                    PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 12, player.velocity, Color.OrangeRed, 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
                }
            }
            Projectile.localAI[1] = float.Lerp(Projectile.localAI[1], Charge >= 1 ? 1 : (Charge * 0.85f), 0.12f);
            if (!FireBeam)
                Projectile.localAI[1] = 0;
            if (ShootDelay-- < 0 && player.channel && Charge < 1) {
                ShootDelay = Projectile.MaxUpdates * player.HeldItem.useTime / player.GetWeaponAttackSpeed(player.HeldItem);
                if (player.PickAmmo(player.HeldItem, out int type, out float speed, out int damage, out float kb, out int ammoItem, Projectile.ai[1]++ == 0 ? true : false)) {
                    for (int i = 0; i < 10; i++) {
                        var d = Dust.NewDustDirect(Projectile.Center + CEUtils.randomPointInCircle(6) + Projectile.velocity.normalize() * 8, 0, 0, DustID.Flare);
                        d.noGravity = true;
                        d.velocity = Projectile.velocity.normalize() * Main.rand.NextFloat(4, 18);
                        d.scale = 1.4f;
                    }
                    SoundEngine.PlaySound(SoundID.Item5, Projectile.Center);
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile proj = Projectile.NewProjectile(player.GetSource_ItemUse_WithPotentialAmmo(player.HeldItem, ammoItem), Projectile.Center, Projectile.velocity.normalize() * speed, type, damage, kb, player.whoAmI).ToProj();
                        proj.Entropy().ashesArrow = true;
                        CEUtils.SyncProj(proj);
                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            if (Charge >= 1 && FireBeam) {
                Main.spriteBatch.UseAdditive();
                for (int i = 0; i < 4; i++) {
                    float r = (MathHelper.TwoPi / 4f) * i + Main.GlobalTimeWrappedHourly * 8;
                    Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + r.ToRotationVector2() * 4, null, Color.White, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
                }
                Main.spriteBatch.ExitShaderRegion();
            }
            CEUtils.DrawGlow(Projectile.Center + Projectile.rotation.ToRotationVector2() * 4, Color.OrangeRed, Charge * 0.4f);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            Main.spriteBatch.UseAdditive();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 196, 50) * Projectile.localAI[1], Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 196, 50) * Projectile.localAI[1], Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
    public class AshesFireBeam : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.timeLeft = 26;
            Projectile.MaxUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public float length = 60;
        public float lv = 120;
        public float width = 1;
        public override void AI() {
            if (Projectile.localAI[2]++ == 0) {
                CEUtils.PlaySound("AshesBeam", 1, Projectile.Center);
                //dedServ屏震守卫,跟PRT spawn无关但别删
                if (CEUtils.getDistance(Projectile.Center, Main.LocalPlayer.Center) < 1000 && !Main.dedServ) {
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 10));
                    CalamityEntropy.FlashEffectStrength = 0.3f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.localAI[2] > 6)
                width -= 0.05f;
            length += lv;
            lv *= 0.9f;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.normalize() * length, targetHitbox, (int)(width * 200));
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseAdditive();
            Texture2D tex = CEExtraAssets.Glow2;
            for (float i = 0.2f; i <= 1; i += 0.2f) {
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Lerp(Color.White, Color.OrangeRed, i) * 1f, Projectile.velocity.ToRotation(), new Vector2(0, tex.Height * 0.5f), new Vector2(length / tex.Width, 200f / tex.Height * width) * i, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool? CanDamage() {
            return width > 0.1f;
        }
        public override bool ShouldUpdatePosition() => false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.OnFire3, 360);
        }
    }
}
