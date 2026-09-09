using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Neltharion : ModItem
    {
        public int UseCount = 0;
        public override void SetDefaults()
        {
            Item.width = 146;
            Item.height = 86;
            Item.damage = 240;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 4;
            Item.useAnimation = 4;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.knockBack = 0;
            Item.value = Item.buyPrice(platinum: 2);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<NeltharionHoldout>();
            Item.UseSound = null;
            Item.shootSpeed = 5f;
            Item.useAmmo = AmmoID.Bullet;
            Item.autoReuse = true;
            Item.ArmorPenetration = 10;
        }
        public static int AmmoSavedPercent = 90;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AmmoSavedPercent);
        public override bool RangedPrefix()
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] <= 0;
        }

        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            if (player.ownedProjectileCounts[Item.shoot] > 0)
            {
                return Main.rand.Next(100) >= AmmoSavedPercent;
            }
            return false;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectileDirect(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI, Item.useTime).velocity = (player.mouseWorld() - player.MountedCenter).SafeNormalize(Vector2.Zero);
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Kingsbane, CEID.Item_Onyxia, CEID.Item_RuinousSoul))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_Kingsbane)
                .AddIngredient(CEID.Item_Onyxia)
                .AddIngredient<FadingRunestone>()
                .AddIngredient(CEID.Item_RuinousSoul, 2)
                .AddTile<VoidWellTile>()
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.VortexBeater)
                .AddIngredient(ItemID.OnyxBlaster)
                .AddIngredient<VoidBar>(5)
                .AddTile<VoidWellTile>()
                .Register();
        }
    }

    #region Projectiles
    public class NeltharionHoldout : ModProjectile
    {
        private Player Owner => Main.player[Projectile.owner];
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Neltharion";
        public override void SetDefaults()
        {
            Projectile.width = 146;
            Projectile.height = 86;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
        }
        public int WindUp = 60;
        public int WindUpSoundCounter = 0;
        public float ShootDelay = 0;
        public int EndShootC = 62;
        public int EndShootTime = 4;
        public int EndShootDelay = 0;
        public float offset = 0;
        public bool EndShoot = false;
        public int CrystalCounter = 0;
        public int frame = 0;
        public override void AI()
        {
            FireFx--;
            Projectile.StickToPlayer();
            Projectile.position += Projectile.velocity.normalize() * (16 + offset);
            offset *= 0.84f;
            Owner.SetHandRot(Projectile.rotation);
            if (Owner.channel)
            {
                Projectile.timeLeft = 3;
                Owner.itemTime = Owner.itemAnimation = 2;
            }
            else
            {
                if (WindUp == 0)
                    EndShoot = true;
            }
            if (WindUp > 0)
            {
                WindUp--;
                if (WindUpSoundCounter-- <= 0)
                {
                    frame++;
                    SoundEngine.PlaySound(SoundID.Item23 with { Pitch = (60 - WindUp) / 60f * 1.4f - 0.2f }, Projectile.Center);
                    WindUpSoundCounter = (int)Utils.Remap(WindUp, 60, 0, 19, 4);
                }
            }
            else
            {
                if (EndShoot)
                {
                    if (EndShootC-- > 0)
                    {
                        Projectile.timeLeft = 3;
                        Owner.itemTime = Owner.itemAnimation = 2;
                    }
                    if (EndShootDelay-- < 0)
                    {
                        if (EndShootTime > 0)
                        {
                            EndShootTime--;
                            EndShootDelay = 16;
                            offset = -16;
                            CEUtils.PlaySound("gunshot", 1, Projectile.Center);
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Owner.PickAmmo(Owner.HeldItem, out int type, out float sts, out int dmg, out float kb, out int _, true);
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), FirePos, Projectile.velocity.normalize() * 46, ModContent.ProjectileType<NeltharionCrystal>(), dmg * 8, kb, Projectile.owner);
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), FirePos, Projectile.velocity.normalize().RotatedBy(0.054f) * 42, ModContent.ProjectileType<NeltharionCrystal>(), dmg * 8, kb, Projectile.owner);
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), FirePos, Projectile.velocity.normalize().RotatedBy(-0.054f) * 42, ModContent.ProjectileType<NeltharionCrystal>(), dmg * 8, kb, Projectile.owner);
                                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Projectile.velocity.normalize(), 8));
                            }
                            for (int i = 0; i < 80; i++)
                            {
                                var ds = Dust.NewDustDirect(FirePos, 0, 0, DustID.CorruptTorch);
                                ds.position = FirePos + Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1, 1);
                                ds.velocity = Projectile.velocity.normalize().RotatedByRandom(0.1f) * Main.rand.NextFloat(2, 32);
                                ds.scale = Main.rand.NextFloat(1.8f, 2f);
                                ds.noGravity = true;
                            }
                            Owner.velocity += Projectile.velocity * -1f;
                            frame += 2;
                        }
                    }
                }
                else
                {
                    ShootDelay--;
                    if (ShootDelay <= 0)
                    {
                        FireFx = 3;
                        ShootDelay += (Projectile.ai[0] / Owner.GetWeaponAttackSpeed(Owner.HeldItem)) * Projectile.MaxUpdates;
                        offset = -10;
                        CEUtils.PlaySound("gunshot", Main.rand.NextFloat(1.4f, 1.6f), Projectile.Center, 32, 0.32f);
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Owner.PickAmmo(Owner.HeldItem, out int type, out float sts, out int dmg, out float kb, out int _, false);

                            for (int i = 0; i < 3; i++)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), FirePos, Projectile.velocity.normalize().RotatedByRandom(0.05f) * sts, type, dmg, kb, Projectile.owner).ToProj().ArmorPenetration += 60;
                            }
                            if (CrystalCounter-- < 0)
                            {
                                CrystalCounter = 3;
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), FirePos, Projectile.velocity.normalize().RotatedByRandom(0.05f) * 42, ModContent.ProjectileType<NeltharionCrystal>(), dmg * 8, kb, Projectile.owner);
                                for (int i = 0; i < 24; i++)
                                {
                                    var ds = Dust.NewDustDirect(FirePos, 0, 0, DustID.CorruptTorch);
                                    ds.position = FirePos + Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1, 1);
                                    ds.velocity = Projectile.velocity.normalize().RotatedByRandom(0.1f) * Main.rand.NextFloat(2, 32);
                                    ds.scale = Main.rand.NextFloat(1.8f, 2f);
                                    ds.noGravity = true;
                                }
                            }
                        }
                        for (int i = 0; i < 12; i++)
                        {
                            var ds = Dust.NewDustDirect(FirePos, 0, 0, DustID.Flare);
                            ds.position = FirePos + Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1, 1) - Owner.velocity;
                            ds.velocity = Projectile.velocity.normalize().RotatedByRandom(0.1f) * Main.rand.NextFloat(2, 20) + Owner.velocity;
                            ds.scale = Main.rand.NextFloat(0.9f, 1.1f);
                            ds.noGravity = true;
                        }
                        frame++;
                        var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.CorruptTorch);
                        d.scale = 1.2f;
                        d.velocity = Owner.velocity * 0.8f + Projectile.velocity.normalize().RotatedBy(-2.5f * (Projectile.velocity.X > 0 ? 1 : -1) + Main.rand.NextFloat(-0.16f, 0.16f)) * 4f;
                        d.noGravity = false;
                    }
                }
            }
        }
        public int FireFx = 0;
        public Vector2 FirePos => Projectile.Center + new Vector2(66, 1 * (Projectile.velocity.X > 0 ? 1 : -1)).RotatedBy(Projectile.rotation);
        public override bool? CanDamage()
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = this.getTextureAlt((frame % 4).ToString());
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            if (FireFx > 0)
            {
                Texture2D tf = this.getTextureAlt("Fire");
                int Frame = 3 - FireFx;
                Rectangle rect = CEUtils.GetCutTexRect(tf, 3, Frame, false);
                Main.spriteBatch.Draw(tf, FirePos - Main.screenPosition, rect, Color.White, Projectile.rotation, new Vector2(0, tf.Height / 3 * 0.5f), Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
    public class NeltharionCrystal : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, 1);
            Projectile.timeLeft = 26;
            Projectile.width = Projectile.height = 32;
        }
        public override void AI()
        {
            Projectile.velocity *= 0.982f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.localAI[2]++ > 0)
            {
                for (float i = 0; i < 1; i += 0.05f)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.PurpleTorch);
                    Vector2 of = Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-3, 3);
                    d.noGravity = true;
                    d.position = Projectile.Center + Projectile.velocity * i + of;
                    d.velocity = Projectile.velocity * 0.1f - of * 0.2f;
                    d.scale = 0.9f;
                    d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.PurpleTorch);
                    of = Projectile.velocity.normalize().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-8, 8);
                    d.noGravity = true;
                    d.position = Projectile.Center + Projectile.velocity * i + of;
                    d.velocity = Projectile.velocity * 0.05f - of * 0.08f;
                    d.scale = 0.5f;
                }
            }
        }
        public override void OnKill(int timeLeft)
        {
            //四连CustomPulse不同贴图,全走Configure现传Assets/Particles路径
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.MediumPurple * 1.5f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShineExplosion2", Vector2.One, Main.rand.NextFloat(-10, 10), 0.005f, 0.14f * Projectile.scale, 27);
            //Neltharion死亡四连CustomPulse,贴图路径全走Configure现传
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.MediumPurple * 1.5f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShineExplosion1", Vector2.One, Main.rand.NextFloat(-10, 10), 0.005f, 0.14f * Projectile.scale, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.MediumPurple * 1.5f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.005f, 0.14f * Projectile.scale, 20);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.MediumPurple * 1.5f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.005f, 0.14f * Projectile.scale, 30);
            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage, 120, Projectile.DamageType);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            CEUtils.PlaySound("explosion", Main.rand.NextFloat(2.4f, 2.8f), Projectile.Center, 10, 0.5f);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (float i = 0; i <= MathHelper.TwoPi; i += MathHelper.TwoPi / 3f)
            {
                for (int i_ = 0; i_ < 4; i_++)
                    Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, null, Projectile.Center + (i + Main.GlobalTimeWrappedHourly * 16f).ToRotationVector2() * 4f));
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(Color.White));
            return false;
        }
    }
    #endregion
}
