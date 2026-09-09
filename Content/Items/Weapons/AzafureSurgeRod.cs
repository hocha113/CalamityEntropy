using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureSurgeRod : ModItem, ICEChargeWeapon, IAzafureEnhancable
    {
        // 充能条 6 秒；原潜伏乘数 伤害1/弹速1.46/击退3 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(6f, 1f, 1.46f, 3f);

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.damage = 21;
            Item.ArmorPenetration = 10;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 28;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.shoot = ModContent.ProjectileType<AzafureSurgeRodThrow>();
            Item.shootSpeed = 36f;
            Item.DamageType = DamageClass.Melee;
        }

        public static int ht = -1;
        public override void HoldItem(Player player)
        {
            if (ht == -1)
                ht = ModContent.ProjectileType<AzafureSurgeRodHeldEffect>();
            if (player.ownedProjectileCounts[ht] < 1 && Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, Vector2.Zero, ht, 0, 0, player.whoAmI);
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var s = ((SoundStyle)Item.UseSound);
            s.MaxInstances = 6;
            s.Volume = 2;
            SoundEngine.PlaySound(Item.UseSound, position); SoundEngine.PlaySound(Item.UseSound, position);
            if (CEChargeWeapon.TryConsume(player, Item))
            {
                for (int i = 0; i < 6; i++)
                {
                    int p = Projectile.NewProjectile(source, position, velocity + CEUtils.randomPointInCircle(9), type, damage, knockback, player.whoAmI, 0f, 1f);
                    if (p >= 0 && p < Main.maxProjectiles)
                    {
                        CEChargeWeapon.Empower(p);
                    }
                }
                return false;
            }
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<HellIndustrialComponents>(6).
                AddCalOrOwn(CEID.Item_MysteriousCircuitry, ModContent.ItemType<AzafureCircuitry>(), 2).
                AddIngredient(ItemID.CobaltBar, 8).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    public class AzafureSurgeRodThrow : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzafureSurgeRod";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, true, -1);
            Projectile.width = Projectile.height = 32;
            Projectile.timeLeft = 60 * 4;
        }
        public override void AI()
        {
            if (Projectile.localAI[0]++ == 0)
            {
                if (Projectile.IsEmpowered())
                {
                    Projectile.tileCollide = false;
                }
                int ht = ModContent.ProjectileType<AzafureSurgeRodHeldEffect>();
                foreach (var proj in Main.ActiveProjectiles)
                {
                    if (proj.owner == Projectile.owner && proj.type == ht)
                    {
                        proj.Kill();
                    }
                }
            }
            if (StickOnNPC && (stick == null || !stick.active))
                StickOnNPC = false;
            if (StickOnNPC)
            {
                Projectile.Center = stick.Center + StickPos.RotatedBy(stick.rotation);
                Projectile.rotation = stick.rotation + yr;
            }
            else if (StickOnGround)
            {

            }
            else
            {
                if (Projectile.IsEmpowered())
                {
                    Projectile.velocity *= 0.93f;
                    if (target == null || !target.active)
                        target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 340);
                    if (target != null)
                    {
                        Projectile.velocity *= 0.9f;
                        Projectile.velocity += (target.Center - Projectile.Center).normalize() * 9f;
                    }
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            if (StrikeCounter > 0)
            {
                StrikeCounter--;
                if (StrikeCounter == 0)
                {
                    Func<int, bool> filter = (i) => true;
                    if (StickOnNPC)
                    {
                        filter = (i) => i != stick.whoAmI;
                    }
                    NPC npc = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 400, filter);
                    if (npc == null)
                    {
                        npc = stick;
                    }
                    if (npc != null)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), stick != null ? stick.Center : Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TeslaLightningRed>(), Projectile.damage, 0, Projectile.owner, npc.Center.X, npc.Center.Y, (Projectile.IsEmpowered() ? 1 : 0)).ToProj().DamageType = Projectile.DamageType;
                        for (int i = 0; i < 8; i++)
                        {
                            //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                            PRTLoader.NewParticle<PRT_AltSpark>(npc.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4, 12), (Projectile.IsEmpowered() ? Color.Red : Color.White), Main.rand.NextFloat(0.9f, 1.4f)).Configure(false, 60);
                        }
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 velocity = ((MathHelper.TwoPi * i / 16f) - (MathHelper.Pi / 16f)).ToRotationVector2() * 12f;
                        //CritSparkCal CalamityPorts,Configure参数顺序跟AltSpark那套不一样
                        PRTLoader.NewParticle<PRT_CritSparkCal>(stick != null ? stick.Center : Projectile.Center, velocity, (Projectile.IsEmpowered() ? Color.DarkRed : Color.White), 0.8f).Configure((Projectile.IsEmpowered() ? Color.DarkRed : Color.White), 30, 0.1f, 3f, Main.rand.NextFloat(0f, 0.01f));
                    }
                    if (Projectile.GetOwner().AzafureEnhance())
                    {
                        Func<int, bool> filter2 = (i) => (!StickOnNPC || i != stick.whoAmI) && (npc == null || i != npc.whoAmI);
                        NPC npc2 = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 400, filter2);
                        if (npc2 == null)
                        {
                            npc2 = stick;
                        }
                        if (npc2 != null)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), stick != null ? stick.Center : Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TeslaLightningRed>(), Projectile.damage, 0, Projectile.owner, npc2.Center.X, npc2.Center.Y, (Projectile.IsEmpowered() ? 1 : 0)).ToProj().DamageType = Projectile.DamageType;
                        }
                    }
                    Projectile.timeLeft = 1;
                }
            }
        }
        NPC target = null;
        public int StrikeCounter = -1;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            if (Projectile.IsEmpowered())
                tex = this.getTextureAlt();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4, new Vector2(36, 12), Projectile.scale, SpriteEffects.None);
            if (StrikeCounter >= 0)
            {
                float wa = 1 - StrikeCounter / 52f;
                tex = this.getTextureGlow();
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, (Projectile.IsEmpowered() ? Color.Red : Color.White) * wa, Projectile.rotation + MathHelper.PiOver4, new Vector2(36, 12), Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }
        public bool StickOnGround = false;
        public bool StickOnNPC = false;
        public Vector2 StickPos = Vector2.Zero;
        public NPC stick = null;
        public float yr = 0;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(StickOnGround);
            writer.WriteVector2(StickPos);
            writer.Write(yr);
            writer.Write(StickOnNPC);
            if (StickOnNPC)
            {
                writer.Write(stick.whoAmI);
            }
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            StickOnGround = reader.ReadBoolean();
            StickPos = reader.ReadVector2();
            yr = reader.ReadSingle();
            StickOnNPC = reader.ReadBoolean();
            if (StickOnNPC)
            {
                stick = reader.ReadInt32().ToNPC();
            }
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (StickOnGround || StickOnNPC)
            {
                return false;
            }
            return null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MechanicalTrauma>(260);
            if (!StickOnGround && !StickOnNPC)
            {
                StickOnNPC = true;
                StickPos = Projectile.Center - target.Center;
                StickPos = StickPos.RotatedBy(-target.rotation);
                stick = target;
                StrikeCounter = 52;
                yr = Projectile.rotation - target.rotation;
                Projectile.tileCollide = false;
            }
            Projectile.velocity *= 0;
            CEUtils.PlaySound("ExoHit" + Main.rand.Next(1, 5), Main.rand.NextFloat(1.9f, 2.3f), target.Center, 4, 1f);
            for (int i = 0; i < 2; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Projectile.IsEmpowered() ? new Color(255, 200, 200) : Color.LightBlue, 0.04f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!StickOnGround)
            {
                Projectile.Center += oldVelocity * 1f;
                Projectile.velocity *= 0;
                StickOnNPC = false;
                StickOnGround = true;
                StickPos = Projectile.Center;
                Projectile.tileCollide = false;
                StrikeCounter = 52;

                if (Main.myPlayer == Projectile.owner)
                {
                    CEUtils.SyncProj(Projectile.whoAmI);
                }
            }
            return false;
        }
    }
    public class AzafureSurgeRodHeldEffect : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzafureSurgeRod";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 16;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public float counter { get { return Projectile.localAI[0]; } set { Projectile.localAI[0] = value; } }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if (player.dead)
            {
                Projectile.Kill();
                return;
            }
            if (player.HeldItem.ModItem is AzafureSurgeRod)
            {
                Projectile.timeLeft = 4;
            }
            else
            {
                Projectile.Kill();
                return;
            }
            float progress = counter / (player.HeldItem.useTime - 4);
            counter++;
            if (progress > 1)
                progress = 1;
            player.Entropy().MouseWorldListener = true;
            Projectile.Center = player.GetDrawCenter();
            if (progress > 0.4f)
            {
                float rj = 2.5f;
                float p = (progress - 0.5f) * 2;
                player.direction = (player.Entropy().MouseWorld - Projectile.Center).X > 0 ? 1 : -1;
                Projectile.rotation = (player.Entropy().MouseWorld - Projectile.Center).ToRotation() - (player.direction * (CEUtils.GetRepeatedCosFromZeroToOne(p, 2) * rj));

                player.SetHandRot(Projectile.rotation);
                player.direction = (player.Entropy().MouseWorld - Projectile.Center).X > 0 ? 1 : -1;
                player.heldProj = Projectile.whoAmI;
            }
            else
            {
                player.SetHandRot((player.Entropy().MouseWorld - Projectile.Center).ToRotation());
            }

        }
        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Projectile.GetOwner();
            float progress = counter / (player.HeldItem.useTime - 4);
            if (progress <= 0.5f)
                return false;
            Texture2D tex = Projectile.GetTexture();
            if (CEChargeWeapon.IsReady(Projectile.GetOwner().HeldItem))
                tex = this.getTextureAlt();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4, new Vector2(0, tex.Height), Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
