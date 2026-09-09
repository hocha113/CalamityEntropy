using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafurePulseWand : ModItem, IAzafureEnhancable
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.damage = 80;
            Item.DamageType = DamageClass.Magic;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AzafurePulseWandHeld>();
            Item.knockBack = 9f;
            Item.value = Item.buyPrice(0, 2);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.shootSpeed = 22f;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.mana = 12;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_PlasmaRod, CEID.Item_AerialiteBar))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_PlasmaRod)
                .AddIngredient<HellIndustrialComponents>(4)
                .AddIngredient(CEID.Item_AerialiteBar, 5)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.MeteoriteBar, 20)
                .AddIngredient<HellIndustrialComponents>(5)
                .AddTile(TileID.Anvils)
                .Register();
        }

        public override bool MagicPrefix()
        {
            return true;
        }
    }
    public class AzafurePulseWandHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzafurePulseWand";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.timeLeft = 4;
        }
        public float Charge = 0;
        public float AttackR = 0;
        public float RVel = 0;
        public float RPulseAlpha = 1;
        public bool Helding = true;
        public List<NPC> targetNpcs = new List<NPC>();
        public Vector2 topPos => Projectile.Center + Projectile.velocity.normalize() * 84 * Projectile.scale;
        public override void AI()
        {
            if (Projectile.localAI[2]++ == 0)
            {
                // 原灾厄 WulfrumTreasurePinger.ScanBeepSound（WulfrumPing 变体），按 sound-map 换自有
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/WulfrumPingReady") { PitchVariance = 0.1f }, Projectile.Center);
            }
            Player player = Projectile.GetOwner();
            player.Entropy().MouseWorldListener = true;
            Projectile.Center = player.GetDrawCenter();
            if (Helding)
            {
                Projectile.rotation = (player.Entropy().MouseWorld - Projectile.Center).ToRotation();
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 16;
                player.SetHandRot(Projectile.rotation);
                player.heldProj = Projectile.whoAmI;
            }
            if (!player.channel)
            {
                if (Helding)
                {
                    Projectile.timeLeft = 36;
                    if (Charge > 20)
                    {
                        foreach (NPC npc in targetNpcs)
                        {
                            if (npc.active)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), topPos, Vector2.Zero, ModContent.ProjectileType<TeslaLightningRed>(), Projectile.damage, 0, Projectile.owner, npc.Center.X, npc.Center.Y).ToProj().DamageType = Projectile.DamageType; ;
                                for (int i = 0; i < 8; i++)
                                {
                                    PRTLoader.NewParticle<PRT_AltSpark>(npc.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(16, 24), new Color(240, 240, 255), Main.rand.NextFloat(0.9f, 2)).Configure(false, Main.rand.Next(6, 10));
                                }
                                npc.AddBuff<MechanicalTrauma>(360);
                                if (Main.zenithWorld)
                                {
                                    npc.velocity = (Projectile.Center - npc.Center).normalize() * 18;
                                }
                            }
                        }
                    }
                }
                Helding = false;
            }
            if (Helding)
            {
                Projectile.timeLeft = 36;
                Charge++;
                if (AttackR < (player.AzafureDurability() * 600 + 1200))
                {
                    RVel += 0.25f;
                    AttackR += RVel;
                }
                else
                {
                    RPulseAlpha *= 0.94f;
                }
                player.itemAnimation = player.itemTime = 36;
                if (RPulseAlpha > 0.3f && Helding && targetNpcs.Count < (player.AzafureEnhance() ? 12 : 6))
                {
                    foreach (var npc in Main.ActiveNPCs)
                    {
                        if (!npc.friendly && CEUtils.getDistance(npc.Center, topPos) < AttackR / 2 && !targetNpcs.Contains(npc))
                        {
                            targetNpcs.Add(npc);
                        }
                    }
                }
            }
            else
            {
                RVel *= 0.98f;
                RPulseAlpha *= 0.94f;
            }
            RVel *= 0.996f;

            AttackR += RVel;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition - Projectile.rotation.ToRotationVector2() * 20, null, Color.White, Projectile.rotation + MathHelper.PiOver4, new Vector2(0, tex.Height), Projectile.scale, SpriteEffects.None, 0); ;



            Texture2D pulse = CEExtraAssets.HollowCircleSoftEdge;

            Main.spriteBatch.End();
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            EffectLoader.PreparePixelShader(gd);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            if (Helding)
            {
                foreach (NPC npc in targetNpcs)
                {
                    if (npc.active)
                    {
                        List<Vector2> lol = new List<Vector2>();
                        for (int i = 0; i < 9; i++)
                        {
                            lol.Add(npc.Center + CEUtils.randomPointInCircle(32));
                        }
                        CEUtils.DrawLines(lol, Color.Red * 0.85f, 4);
                    }
                }
            }
            Main.spriteBatch.Draw(pulse, topPos - Main.screenPosition, null, new Color(240, 240, 255) * 0.76f * RPulseAlpha, 0, pulse.Size() / 2f, (AttackR / (float)pulse.Width), SpriteEffects.None, 0); ;
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.End();
            EffectLoader.ApplyPixelShader(gd);
            Main.spriteBatch.begin_();
            return false;
        }
    }
}
