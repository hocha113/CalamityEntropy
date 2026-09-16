using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class EmberSpike : ModItem, ICEChargeWeapon
    {
        // 命中计数 6；原潜伏乘数均为 1，无释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(6);

        public static int MAXSTICK => 12;
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[S]", MAXSTICK.ToString());
        }
        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 28;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 16;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 3.6f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<EmberSpikeThrow>();
            Item.shootSpeed = 12f;
            Item.DamageType = DamageClass.Melee;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<CrystalSpike>()
                .AddIngredient<TectonicShard>(6)
                .AddTile(TileID.Hellforge)
                .Register();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            bool ult = CEChargeWeapon.TryConsume(player, Item);
            if (ult) {
                ReturnAllSpikes(player);
            }
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0);

            if (ult && p >= 0 && p < Main.maxProjectiles) {
                CEChargeWeapon.Empower(p);
            }
            return false;
        }
        public void ReturnAllSpikes(Player player) {
            int type = ModContent.ProjectileType<EmberSpikeReturning>();
            int tm = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == player.whoAmI && p.type == Item.shoot && p.ModProjectile is EmberSpikeThrow cst && cst.StickNPC >= 0) {
                    Projectile.NewProjectile(p.GetSource_FromThis(), p.Center, Vector2.Zero, type, p.damage, p.knockBack * 2, player.whoAmI, tm);
                    p.Kill();
                    tm += 2;
                }
            }
            if (tm > 0)
                CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(2.5f, 2.8f), player.Center, 60, 0.5f);

        }
    }

    public class EmberSpikeThrow : ModProjectile
    {
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 8;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/EmberSpike";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, true, -1);
            Projectile.width = Projectile.height = 12;
            Projectile.timeLeft = 120 * 4;
            Projectile.MaxUpdates = 3;
        }
        public int StickNPC = -1;
        public Vector2 offset = Vector2.Zero;
        public Vector2 vel = Vector2.Zero;
        public int counter = 0;
        public override void AI() {
            if (counter == 0) {
                if (Projectile.IsEmpowered())
                    BounceTime = 5;
                CEUtils.PlaySound("flamethrower end", Main.rand.NextFloat(4f, 4.2f), Projectile.Center, 6, 0.3f);
            }
            counter++;
            if (StickNPC == -1) {
                if (counter > 46) {
                    Projectile.velocity.Y += 0.16f;
                    Projectile.velocity *= 0.998f;
                    Projectile.velocity.X *= 0.98f;
                }
                if (Projectile.ai[0] == 0)
                    //CustomPulse贴图路径Configure现传,Texture属性填白图应付框架
                    PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center, Vector2.Zero, Color.OrangeRed, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 20);
                else
                    PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center, Vector2.Zero, Color.OrangeRed, 0.36f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 8);

                for (float i = 0; i < 1; i += 0.5f) {
                    var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.RedTorch);
                    d.position = Vector2.Lerp(Projectile.Center - Projectile.velocity, Projectile.Center, i) + CEUtils.randomPointInCircle(5);
                    d.velocity = Projectile.velocity * Main.rand.NextFloat(0.4f);
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1, 1.2f);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (vel == Vector2.Zero)
                vel = Projectile.velocity;
            if (StickNPC >= 0)
                if (!StickNPC.ToNPC().active || StickNPC.ToNPC().dontTakeDamage)
                    StickNPC = -1;
            if (StickNPC >= 0) {
                Projectile.Center = StickNPC.ToNPC().Center + offset;
                StickNPC.ToNPC().AddBuff(BuffID.OnFire3, 180);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            if (counter == 0)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(StickNPC);
            writer.WriteVector2(offset);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            StickNPC = reader.ReadInt32();
            offset = reader.ReadVector2();
        }
        public override bool? CanHitNPC(NPC target) {
            if (StickNPC >= 0)
                return false;
            return null;
        }
        public override bool ShouldUpdatePosition() {
            return StickNPC < 0;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            BounceTime = 0;
            //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
            PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 10, Vector2.Zero, Color.OrangeRed * 1.3f, 1.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);

            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(2.4f, 2.8f), target.Center, 60, 0.4f);
            int sum = 0;
            foreach (Projectile pj in Main.ActiveProjectiles) {
                if (pj.owner == Projectile.owner && pj.type == Projectile.type && pj.ModProjectile is EmberSpikeThrow cs && cs.StickNPC == target.whoAmI)
                    sum++;
            }
            if (sum >= EmberSpike.MAXSTICK || Projectile.ai[0] == 1) {
                if (Projectile.IsEmpowered())
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.6f, 1f), ModContent.ProjectileType<TectonicShardHoming>(), Projectile.damage, 4, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(-1, -0.5f), ModContent.ProjectileType<EmberSpikePop>(), 0, 0, Projectile.owner);
                Projectile.Kill();
            }
            else {
                target.AddBuff(BuffID.OnFire3, 180);
                Projectile p = Projectile;
                Player player = Projectile.GetOwner();
                CEUtils.SpawnExplotionFriendly(p.GetSource_FromThis(), player, p.Center, p.damage, 120, Projectile.DamageType);
                float scale = 100 / 40f;
                PRTLoader.NewParticle<PRT_ShineParticle>(p.Center, Vector2.Zero, Color.OrangeRed * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(p.Center, Vector2.Zero, Color.Firebrick * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_CustomPulse>(p.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(p.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
                PRTLoader.NewParticle<PRT_CustomPulse>(p.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);

                if (StickNPC < 0) {
                    StickNPC = target.whoAmI;
                    offset = Projectile.Center - target.Center;
                    Projectile.timeLeft = 45 * 60 * Projectile.MaxUpdates;
                }
            }
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public int BounceTime = 2;
        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (StickNPC >= 0)
                return false;
            PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center + oldVelocity + Projectile.rotation.ToRotationVector2() * 4, Vector2.Zero, Color.OrangeRed * 1.3f, 1.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            if (BounceTime > 0) {
                BounceTime--;
                if (Projectile.velocity.X != oldVelocity.X) {
                    Projectile.velocity.X = -oldVelocity.X * 0.84f;
                }
                if (Projectile.velocity.Y != oldVelocity.Y) {
                    Projectile.velocity.Y = -oldVelocity.Y * 0.84f;
                }
                counter = 28;
                CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(2.8f, 3.4f), Projectile.Center, 16, 0.4f);

                float scale = 0.42f;
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 0.95f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.95f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.04f, 12);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 16);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.06f, 20);

                return false;
            }
            else {
                float scale = 0.6f;
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 0.95f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.95f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.04f, 12);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 16);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.06f, 20);

                if (Main.myPlayer == Projectile.owner) {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, oldVelocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(-1, -0.5f), ModContent.ProjectileType<EmberSpikePop>(), 0, 0, Projectile.owner);
                }
            }
            return true;
        }
    }
    public class EmberSpikePop : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/EmberSpike";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 12;
            Projectile.timeLeft = 60;
        }
        public override void AI() {
            Projectile.Opacity = Projectile.timeLeft / 60f;
            Projectile.velocity *= 0.984f;
            Projectile.velocity.Y += 0.36f;
            Projectile.rotation += Projectile.velocity.X * 0.04f;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override bool? CanDamage() {
            return false;
        }
    }
    public class EmberSpikeReturning : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/EmberSpike";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 12;
            Projectile.timeLeft = 10000;
        }
        public int counter = 0;
        public Vector2 spawn = Vector2.Zero;
        public Vector2 ofst = CEUtils.randomPointInCircle(180);
        public override void AI() {
            if (spawn == Vector2.Zero)
                spawn = Projectile.Center;
            Vector2 target = Projectile.GetOwner().Center;
            counter++;
            Vector2 mid = (spawn + target) * 0.5f + ofst;
            if (counter == 36 && Projectile.ai[0] == 0 && Main.myPlayer == Projectile.owner)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.GetOwner().Center, Vector2.Zero, ModContent.ProjectileType<SwingSmearEmber>(), 0, 0, Projectile.owner);
            if (counter <= 36f) {
                Vector2 pos = CEUtils.Bezier(new List<Vector2>() { spawn, mid, target }, counter / 36f);
                Vector2 offset = pos - Projectile.Center;
                Projectile.rotation += offset.X * 0.05f;
                PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center, Vector2.Zero, Color.OrangeRed, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 20);
                for (float i = 0; i < 1; i += 0.5f) {
                    var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.RedTorch);
                    d.position = Vector2.Lerp(Projectile.Center, pos, i) + CEUtils.randomPointInCircle(5);
                    d.velocity = offset * Main.rand.NextFloat(0.4f);
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1, 1.2f);
                }
                Projectile.Center = pos;
            }
            else {
                if (counter - 36 > Projectile.ai[0]) {
                    Projectile.Kill();
                    if (Main.myPlayer == Projectile.owner) {
                        Vector2 vel = (Main.MouseWorld - Projectile.GetOwner().MountedCenter).normalize() * 15;
                        int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.GetOwner().MountedCenter + vel.normalize().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-10, 10), vel, ModContent.ProjectileType<EmberSpikeThrow>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 1);
                        CEChargeWeapon.Empower(p);
                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            if (counter > 36)
                return false;
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override bool? CanDamage() {
            return false;
        }
    }
    public class SwingSmearEmber : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Default, false, -1);
            Projectile.timeLeft = 10;
        }
        public override bool? CanDamage() {
            return false;
        }
        public override void AI() {
            if (Projectile.GetOwner().ownedProjectileCounts[ModContent.ProjectileType<EmberSpikeReturning>()] > 0)
                Projectile.timeLeft = 10;
            Projectile.Center = Projectile.GetOwner().GetDrawCenter();
            Projectile.rotation += 0.66f;
            if (Main.myPlayer == Projectile.owner)
                Main.LocalPlayer.direction = Main.MouseWorld.X > Main.LocalPlayer.Center.X ? 1 : -1;
            Projectile.GetOwner().SetHandRotWithDir(Projectile.rotation * Projectile.GetOwner().direction + (Projectile.GetOwner().direction > 0 ? 0 : MathHelper.Pi) + Projectile.GetOwner().direction * -0.6f, Projectile.GetOwner().direction);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, Color.OrangeRed * 1.25f * (Projectile.timeLeft / 10f), Projectile.rotation * Projectile.GetOwner().direction, Projectile.GetTexture().Size() * 0.5f, Projectile.scale * 0.36f, Projectile.GetOwner().direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
