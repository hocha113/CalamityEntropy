using CalamityEntropy.Content.Particles;
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
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class CrystalSpike : ModItem
    {
        public static int MAXSTICK => 8;
        // 条件触发大招：场上已布置的晶刺达到该数量时，下次投掷召回全部晶刺并强化
        public static int ULTSPIKES => 6;
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[S]", MAXSTICK.ToString());
            tooltips.Replace("[U]", ULTSPIKES.ToString());
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 20;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 16;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 3f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<CrystalSpikeThrow>();
            Item.shootSpeed = 12f;
            Item.DamageType = DamageClass.Melee;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_UrchinStinger))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_UrchinStinger)
                .AddIngredient(ItemID.ManaCrystal, 2)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Amethyst, 10)
                .AddIngredient(ItemID.Sapphire, 10)
                .AddIngredient(ItemID.Diamond, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 大招条件：场上布置的晶刺 ≥ ULTSPIKES 时，本次投掷召回全部晶刺并强化
            bool ult = CountDeployedSpikes(player) >= ULTSPIKES;
            if (ult)
            {
                ReturnAllSpikes(player);
            }
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0);

            if (ult && p >= 0 && p < Main.maxProjectiles)
            {
                CEChargeWeapon.Empower(p);
            }
            return false;
        }
        public int CountDeployedSpikes(Player player)
        {
            int sum = 0;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner == player.whoAmI && p.type == Item.shoot && p.ModProjectile is CrystalSpikeThrow cst && cst.StickNPC >= 0)
                    sum++;
            }
            return sum;
        }
        public void ReturnAllSpikes(Player player)
        {
            int type = ModContent.ProjectileType<CrystalSpikeReturning>();
            int tm = 0;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner == player.whoAmI && p.type == Item.shoot && p.ModProjectile is CrystalSpikeThrow cst && cst.StickNPC >= 0)
                {
                    Projectile.NewProjectile(p.GetSource_FromThis(), p.Center, Vector2.Zero, type, p.damage, p.knockBack * 2, player.whoAmI, tm);
                    p.Kill();
                    tm += 2;
                }
            }
            if (tm > 0)
                CEUtils.PlaySound("flashback", 1.6f, player.Center, 6, 0.55f);
        }
    }

    public class CrystalSpikeThrow : ModProjectile
    {
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 4;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/CrystalSpike";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, true, -1);
            Projectile.width = Projectile.height = 14;
            Projectile.timeLeft = 120 * 4;
            Projectile.MaxUpdates = 3;
        }
        public int StickNPC = -1;
        public Vector2 offset = Vector2.Zero;
        public Vector2 vel = Vector2.Zero;
        public int counter = 0;
        public override void AI()
        {
            if (counter == 0)
                CEUtils.PlaySound((Projectile.ai[0] == 1 ? "crystalsound" : "bne") + (Main.rand.NextBool() ? "2" : "3"), Main.rand.NextFloat(1.4f, 1.6f), Projectile.Center, 6, 0.7f)
; counter++;
            if (StickNPC == -1)
            {
                if (counter > 46)
                {
                    Projectile.velocity.Y += 0.16f;
                    Projectile.velocity *= 0.998f;
                    Projectile.velocity.X *= 0.98f;
                }
                if (Projectile.ai[0] == 0)
                    //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                    PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center, Vector2.Zero, Color.MediumPurple, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 20);
                else
                    PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center, Vector2.Zero, Color.MediumPurple, 0.36f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 8);

                for (float i = 0; i < 1; i += 0.5f)
                {
                    var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.PurpleTorch);
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
            if (StickNPC >= 0)
            {
                Projectile.Center = StickNPC.ToNPC().Center + offset;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (counter == 0)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(StickNPC);
            writer.WriteVector2(offset);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            StickNPC = reader.ReadInt32();
            offset = reader.ReadVector2();
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (StickNPC >= 0)
                return false;
            return null;
        }
        public override bool ShouldUpdatePosition()
        {
            return StickNPC < 0;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 10, Vector2.Zero, Color.MediumPurple * 1.3f, 1.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);

            CEUtils.PlaySound("truemoonlighthit", Main.rand.NextFloat(1.4f, 1.8f), target.Center, 60, 0.7f);
            int sum = 0;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner == Projectile.owner && p.type == Projectile.type && p.ModProjectile is CrystalSpikeThrow cs && cs.StickNPC == target.whoAmI)
                    sum++;
            }
            if (sum >= CrystalSpike.MAXSTICK) //Projectile.ai[0] == 1 || 
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(-1, -0.5f), ModContent.ProjectileType<CrystalSpikePop>(), 0, 0, Projectile.owner);
                Projectile.Kill();
            }
            else
            {
                if (StickNPC < 0)
                {
                    StickNPC = target.whoAmI;
                    offset = Projectile.Center - target.Center;
                    Projectile.timeLeft = 45 * 60 * Projectile.MaxUpdates;
                }
            }
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (StickNPC >= 0)
                return false;
            PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center + oldVelocity + Projectile.rotation.ToRotationVector2() * 4, Vector2.Zero, Color.MediumPurple * 1.3f, 1.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, oldVelocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(-1, -0.5f), ModContent.ProjectileType<CrystalSpikePop>(), 0, 0, Projectile.owner);
            }
            return true;
        }
    }
    public class CrystalSpikePop : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/CrystalSpike";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 12;
            Projectile.timeLeft = 60;
        }
        public override void AI()
        {
            Projectile.Opacity = Projectile.timeLeft / 60f;
            Projectile.velocity *= 0.984f;
            Projectile.velocity.Y += 0.36f;
            Projectile.rotation += Projectile.velocity.X * 0.04f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
    public class CrystalSpikeReturning : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/CrystalSpike";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 12;
            Projectile.timeLeft = 10000;
        }
        public int counter = 0;
        public Vector2 spawn = Vector2.Zero;
        public Vector2 ofst = CEUtils.randomPointInCircle(180);
        public override void AI()
        {
            if (spawn == Vector2.Zero)
                spawn = Projectile.Center;
            Vector2 target = Projectile.GetOwner().Center;
            counter++;
            Vector2 mid = (spawn + target) * 0.5f + ofst;
            if (counter == 36 && Projectile.ai[0] == 0 && Main.myPlayer == Projectile.owner)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.GetOwner().Center, Vector2.Zero, ModContent.ProjectileType<SwingSmear>(), 0, 0, Projectile.owner);
            if (counter <= 36f)
            {
                Vector2 pos = CEUtils.Bezier(new List<Vector2>() { spawn, mid, target }, counter / 36f);
                Vector2 offset = pos - Projectile.Center;
                Projectile.rotation += offset.X * 0.05f;
                PRTLoader.NewParticle<PRT_CrystalGlow>(Projectile.Center, Vector2.Zero, Color.MediumPurple, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 20);
                for (float i = 0; i < 1; i += 0.5f)
                {
                    var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.PurpleTorch);
                    d.position = Vector2.Lerp(Projectile.Center, pos, i) + CEUtils.randomPointInCircle(5);
                    d.velocity = offset * Main.rand.NextFloat(0.4f);
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1, 1.2f);
                }
                Projectile.Center = pos;
            }
            else
            {
                if (counter - 36 > Projectile.ai[0])
                {
                    Projectile.Kill();
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Vector2 vel = (Main.MouseWorld - Projectile.GetOwner().MountedCenter).normalize() * 15;
                        int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.GetOwner().MountedCenter + vel.normalize().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-10, 10), vel, ModContent.ProjectileType<CrystalSpikeThrow>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 1);
                        CEChargeWeapon.Empower(p);
                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (counter > 36)
                return false;
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
    public class SwingSmear : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Default, false, -1);
            Projectile.timeLeft = 10;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        public override void AI()
        {
            if (Projectile.GetOwner().ownedProjectileCounts[ModContent.ProjectileType<CrystalSpikeReturning>()] > 0)
                Projectile.timeLeft = 10;
            Projectile.Center = Projectile.GetOwner().GetDrawCenter();
            Projectile.rotation += 0.66f;
            if (Main.myPlayer == Projectile.owner)
                Main.LocalPlayer.direction = Main.MouseWorld.X > Main.LocalPlayer.Center.X ? 1 : -1;
            Projectile.GetOwner().SetHandRotWithDir(Projectile.rotation * Projectile.GetOwner().direction + (Projectile.GetOwner().direction > 0 ? 0 : MathHelper.Pi) + Projectile.GetOwner().direction * -0.6f, Projectile.GetOwner().direction);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, Color.MediumPurple * 1.25f * (Projectile.timeLeft / 10f), Projectile.rotation * Projectile.GetOwner().direction, Projectile.GetTexture().Size() * 0.5f, Projectile.scale * 0.36f, Projectile.GetOwner().direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
