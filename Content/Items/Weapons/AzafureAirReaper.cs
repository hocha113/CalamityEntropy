using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
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
    public class AzafureAirReaper : ModItem, ICEChargeWeapon, IAzafureEnhancable
    {
        // 充能条 5 秒；原潜伏乘数 伤害0.8/弹速1.2/击退2 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(5f, 0.8f, 1.2f, 2f);

        public override void SetDefaults()
        {
            Item.width = 78;
            Item.height = 78;
            Item.damage = 9;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 32;
            Item.useAnimation = 32;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shootSpeed = 48f;
            Item.shoot = ModContent.ProjectileType<AzafureAirReaperThrow>();
        }
        public override bool CanUseItem(Player player)
        {
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(5)
                .AddCalOrOwn(CEID.Item_MysteriousCircuitry, ModContent.ItemType<AzafureCircuitry>(), 4)
                .AddRecipeGroup(CERecipeGroups.IronBar, 8)
                .AddTile(TileID.Anvils)
                .Register();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool empowered = CEChargeWeapon.TryConsume(player, Item);
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 1f);
            if (empowered && p >= 0 && p < Main.maxProjectiles)
            {
                CEChargeWeapon.Empower(p);
            }
            return false;
        }
    }
    public class AzafureAirReaperThrow : ModProjectile
    {
        public float TeleportSlashDamageMult = 4f;
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzafureAirReaper";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.width = Projectile.height = 80;
        }

        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRots = new List<float>();
        public float counter = 0;
        public int StickTime = 0;
        public int StickNPC = -1;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MechanicalTrauma>(260);
            if (StickNPC < 0)
            {
                StickNPC = target.whoAmI;
                CEUtils.SyncProj(Projectile.whoAmI);
                Projectile.velocity *= 0.2f;
            }
            CEUtils.PlaySound("voidseekershort", 1, target.Center, 6, CEUtils.WeapSound * 0.4f);
            if (!target.Organic())
            {
                CEUtils.PlaySound("metalhit", Main.rand.NextFloat(0.8f, 1.2f) / Projectile.ai[1], target.Center, 6, CEUtils.WeapSound * 0.4f);
            }
            Color impactColor = Color.Red;
            float impactParticleScale = Main.rand.NextFloat(1.4f, 1.6f);

            //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, impactColor, impactParticleScale).Configure(Color.LawnGreen, 8, 0, 2.5f);

            float sparkCount = 6;
            for (int i = 0; i < sparkCount; i++)
            {
                float p = Main.rand.NextFloat();
                Vector2 sparkVelocity2 = CEUtils.randomRot().ToRotationVector2().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(6, 20 * (2 - p)) * (0.6f);
                int sparkLifetime2 = (int)((2 - p) * 16);
                float sparkScale2 = 0.6f + (1 - p);
                sparkScale2 *= 1;
                Color sparkColor2 = Color.Lerp(Color.DarkRed, Color.IndianRed, p);
                if (Main.rand.NextBool())
                {
                    //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                }
                else
                {
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2, Main.rand.NextBool() ? Color.Red : Color.DarkRed, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2));
                }
            }

            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(255, 120, 120), 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);

        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(StickNPC);
            writer.Write(counter);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            StickNPC = reader.ReadInt32();
            counter = reader.ReadSingle();
        }
        public override void AI()
        {
            if (counter == 0)
            {
                CEUtils.PlaySound("scytheswing", Main.rand.NextFloat(1.3f, 1.5f) + (Projectile.IsEmpowered() ? 0.2f : 0), Projectile.Center, 4, CEUtils.WeapSound);
            }
            Player player = Projectile.GetOwner();
            counter++;
            bool backing = false;
            if (StickNPC >= 0)
            {
                NPC stick = StickNPC.ToNPC();
                if (!stick.active)
                {
                    StickNPC = -1;
                    goto FLAG;
                }
                StickTime++;

                if (StickTime > (Projectile.IsEmpowered() ? 180 : 20) * (player.AzafureEnhance() ? 1.6f : 1))
                {
                    backing = true;
                }
                else
                {
                    if (Projectile.IsEmpowered())
                    {
                        if (CEUtils.getDistance(Projectile.Center, stick.Center) > 90)
                        {
                            Projectile.velocity *= 0.98f;
                            Projectile.velocity += (stick.Center - Projectile.Center).normalize() * 8f;
                        }
                        else
                        {
                            if (Projectile.velocity.Length() < 60)
                                Projectile.velocity *= 1.1f;
                        }

                        if (Projectile.velocity.Length() > 60)
                            Projectile.velocity = Projectile.velocity.normalize() * 60;
                    }
                    else
                    {
                        Projectile.velocity *= 0.965f;
                        Projectile.velocity += (stick.Center - Projectile.Center).normalize() * 5f;
                    }
                }
            }
            else
            {
                if (counter > 13)
                    backing = true;
            }
        FLAG:
            if (backing)
            {
                Projectile.velocity += (player.Center - Projectile.Center).normalize() * 12f;
                Projectile.velocity *= 0.9f;
                if (CEUtils.getDistance(Projectile.Center, player.Center) < Projectile.velocity.Length() + 12)
                {
                    Projectile.Kill();
                }
            }
            Projectile.rotation += 0.7f;
            oldPos.Add(Projectile.Center);
            oldRots.Add(Projectile.rotation);
            if (oldRots.Count > 9)
            {
                oldRots.RemoveAt(0);
                oldPos.RemoveAt(0);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            List<Vector2> list = new();
            List<float> list2 = new();

            if (oldPos.Count > 1)
            {
                for (int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0; j < 1; j += 0.1f)
                    {
                        list.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                        list2.Add(CEUtils.RotateTowardsAngle(oldRots[i - 1], oldRots[i], j, false));
                    }
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                float alpha = (i + 1f) / list.Count;
                alpha *= 0.06f;
                Main.spriteBatch.Draw(tex, list[i] - Main.screenPosition, null, lightColor * alpha, list2[i], tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
