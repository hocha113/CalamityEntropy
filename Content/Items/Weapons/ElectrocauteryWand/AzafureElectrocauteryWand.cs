using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.ElectrocauteryWand
{
    public class AzafureElectrocauteryWand : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.damage = 80;
            Item.DamageType = DamageClass.Magic;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AzafureElectrocauteryWandHeld>();
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(0, 2);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.shootSpeed = 22f;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.mana = 22;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<AzafurePulseWand>()
                .AddIngredient(ItemID.SoulofMight, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public override bool MagicPrefix() {
            return true;
        }
    }
    public class AzafureElectrocauteryWandHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/ElectrocauteryWand/AzafureElectrocauteryWand";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.timeLeft = 4;
            Projectile.width = Projectile.height = 12;
            Projectile.light = 1;
        }
        public float Charge = 0;
        public int AfterAnm = 30;
        public float DrawOffset = 0;
        public float OffsetVel = -8;
        public bool Shoot = true;
        public override void AI() {
            Player player = Projectile.GetOwner();
            var proj = Projectile;
            bool Charging = player.channel;
            player.Entropy().MouseWorldListener = true;
            proj.Center = player.GetDrawCenter();
            proj.rotation = (player.mouseWorld() + new Vector2(0, -200) - proj.Center).ToRotation();
            proj.velocity = proj.rotation.ToRotationVector2() * player.HeldItem.shootSpeed;
            player.heldProj = proj.whoAmI;
            player.SetHandRot(Projectile.rotation);
            player.itemTime = player.itemAnimation = 4;
            if (Charging) {
                Projectile.timeLeft = 3;
                if (Charge < 1) {
                    if (Charge == 0) {
                        CEUtils.PlaySound("scholarStaffAttack", 0.4f, Projectile.Center);
                    }
                    Charge += player.AzafureEnhance() ? 0.035f : 0.02f;
                    if (Charge >= 1) {
                        CEUtils.PlaySound("beep", 1, Projectile.Center);
                        Charge = 1;
                    }
                }

            }
            else {
                if (Shoot) {
                    if (Charge < 0.2f) {
                        Projectile.Kill();
                        return;
                    }
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 60, Projectile.velocity * (0.3f + 0.7f * Charge), ModContent.ProjectileType<AzafureElectrocauteryWandBomb>(), (int)(Projectile.damage * Charge), Projectile.knockBack * Charge, Projectile.owner, 0, 0, Charge);
                    for (int i = 0; i < (int)(20 * Charge); i++)
                        //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                        PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center + CEUtils.randomPointInCircle(4) + (Projectile.velocity.X > 0 ? new Vector2(80, 8) : new Vector2(80, -8)).RotatedBy(Projectile.rotation), CEUtils.randomPointInCircle(6), Color.Lerp(Color.Yellow, Color.White, 0.5f), Main.rand.NextFloat(0.12f, 0.36f)).Configure(0.6f, 22, 0.006f, true);
                    for (int i = 0; i < 3; i++)
                        CEUtils.PlaySound("cannon", 1, Projectile.Center, volume: Charge);
                    Shoot = false;
                    Charge = 0;
                }
                AfterAnm--;
                DrawOffset += OffsetVel;
                OffsetVel *= 0.78f;
                DrawOffset *= 0.86f;
                if (AfterAnm <= 0) {
                    Projectile.Kill();
                    player.itemTime = player.itemAnimation = 0;
                }
                else {
                    Projectile.timeLeft = 3;
                }
            }
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = this.getTextureGlow();
            int dir = Projectile.velocity.X > 0 ? 1 : -1;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.Pi / 4f : Projectile.rotation - MathHelper.Pi / 4f;
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(0, 0);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * (-42 + DrawOffset), null, lightColor, rot, origin, Projectile.scale, effect, 0);
            float num = 0.6f;
            int gw = (int)(glow.Width * (num + (1 - num) * Charge));
            Rectangle rect = new Rectangle(0, 0, gw, glow.Height);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * (-42 + DrawOffset), rect, Color.White, rot, origin, Projectile.scale, effect, 0);

            return false;
        }
    }
    public class AzafureElectrocauteryWandBomb : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);

            Projectile.light = 1;
            Projectile.width = Projectile.height = 12;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }

        public float PulseScale = 0;
        public float PulseScaleV = 156;
        public float PulseAlpha = 1;
        public List<NPC> target = new List<NPC>();
        public override void AI() {
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 24)
                for (int i = 0; i < 5; i++)
                    CEUtils.PlaySound("alert2", 1f, Projectile.Center, 10, 2);
            if (Projectile.ai[0] < 32) {
                if (Projectile.ai[0] > 8)
                    Projectile.velocity += new Vector2(0, 0.6f);
            }
            else {
                Projectile.velocity *= 0.86f;
                if (Projectile.ai[0] > 48) {
                    PulseScale += PulseScaleV;
                    PulseScaleV *= 0.96f;
                    if (Projectile.ai[0] > 60)
                        PulseAlpha *= 0.9f;
                    for (int i = target.Count - 1; i >= 0; i--) {
                        NPC t = target[i];
                        if (t.dontTakeDamage || !t.active) {
                            target.RemoveAt(i);
                        }
                    }
                    if (target.Count < 8 && PulseAlpha > 0.16f) {
                        foreach (var npc in Main.ActiveNPCs) {
                            if (!npc.friendly && !npc.dontTakeDamage & npc.CanBeChasedBy(Projectile)) {
                                if (!target.Contains(npc) && CEUtils.getDistance(npc.Center, Projectile.Center) < PulseScale / 2) {
                                    target.Add(npc);
                                }
                            }
                        }
                    }
                }
                if (Projectile.ai[0] > 66) {
                    CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, 256, Projectile.DamageType);
                    PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.06f).Configure(2.4f, 16);
                    PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.06f).Configure(2.6f, 16);
                    foreach (var npc in target) {
                        if (npc.active) {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), npc.Center + new Vector2(0, -1200), Vector2.Zero, ModContent.ProjectileType<TeslaLightningRed>(), Projectile.damage, 0, Projectile.owner, npc.Center.X, npc.Center.Y).ToProj().DamageType = Projectile.DamageType; ;
                            for (int i = 0; i < 12; i++) {
                                PRTLoader.NewParticle<PRT_AltSpark>(npc.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(32, 46), new Color(255, 160, 165), Main.rand.NextFloat(0.9f, 2)).Configure(false, Main.rand.Next(6, 10));
                            }
                        }
                    }
                    PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, new Color(255, 122, 122), 0.1f).Configure(new Vector2(2f, 2f), 0, 0.4f, 16);
                    CEUtils.SetShake(Projectile.Center, 4);
                    CEUtils.PlaySound("energyImpact", Main.rand.NextFloat(0.7f, 1.3f), Projectile.Center);

                    Projectile.Kill();
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            if (Projectile.ai[0] <= 48) {
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tex, 4, ((int)Main.GameUpdateCount / 2) % 4, false), lightColor, Projectile.velocity.ToRotation(), new Vector2(22, 11), Projectile.scale, SpriteEffects.None);
            }
            else {
                Texture2D pulse = CEExtraAssets.HollowCircleSoftEdge;
                Main.spriteBatch.End();
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                EffectLoader.PreparePixelShader(gd);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                foreach (NPC npc in target) {
                    if (npc.active) {
                        List<Vector2> lol = new List<Vector2>();
                        for (int i = 0; i < 9; i++) {
                            lol.Add(npc.Center + CEUtils.randomPointInCircle(32));
                        }
                        CEUtils.DrawLines(lol, Color.Red * 0.85f, 4);
                    }
                }
                if (PulseScale > 0)
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, new Color(255, 180, 180) * 0.76f * PulseAlpha, 0, pulse.Size() / 2f, (PulseScale / (float)pulse.Width), SpriteEffects.None, 0);

                List<Vector2> l = new List<Vector2>();
                for (int i = 0; i < 12; i++) {
                    l.Add(Projectile.Center + CEUtils.randomPointInCircle(46));
                }
                CEUtils.DrawLines(l, Color.Red * 0.85f, 4 * Projectile.ai[2]);
                CEUtils.DrawLines(l, Color.Red * 0.4f, 8 * Projectile.ai[2]);
                CEUtils.DrawGlow(Projectile.Center, Color.Firebrick, 1.8f * Projectile.ai[2]);
                CEUtils.DrawGlow(Projectile.Center, Color.White, 1.4f * Projectile.ai[2]);
                Main.spriteBatch.ExitShaderRegion();
                Main.spriteBatch.End();
                EffectLoader.ApplyPixelShader(gd);
                Main.spriteBatch.begin_();
            }
            return false;
        }
    }
}
