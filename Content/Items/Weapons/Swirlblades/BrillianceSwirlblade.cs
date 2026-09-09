using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class BrillianceSwirlblade : ModItem, ICEChargeWeapon
    {
        // 充能条 4 秒；原潜伏乘数 伤害1/弹速1.25 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(4f, 1f, 1.25f);

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = Item.useTime = 24;
            Item.width = 42;
            Item.height = 46;
            Item.damage = 7;
            Item.ArmorPenetration = 7;
            Item.UseSound = SoundID.Item1 with { Volume = 1.2f };
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.shoot = ModContent.ProjectileType<BrillianceSwirlbladeProj>();
            Item.shootSpeed = 42f;
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool ult = CEChargeWeapon.TryConsume(player, Item);
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (ult && p >= 0 && p < Main.maxProjectiles)
            {
                CEChargeWeapon.Empower(p);
            }
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_PearlShard, CEID.Item_SeaPrism, CEID.Item_PrismShard))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_PearlShard, 6)
                .AddIngredient(CEID.Item_SeaPrism, 10)
                .AddIngredient(CEID.Item_PrismShard, 6)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Sapphire, 10)
                .AddIngredient(ItemID.Coral, 10)
                .AddIngredient(ItemID.WhitePearl, 3)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        public override bool RangedPrefix()
        {
            return true;
        }
    }
    public class BrillianceSwirlbladeProj : BaseSwirlblade
    {
        public override string Texture => CEUtils.ItemTexPath<BrillianceSwirlblade>();
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 6;
        }
        public override float Radius => 140 * (Projectile.IsEmpowered() ? 1.7f : 1);
        public override int SpreadTime => Projectile.IsEmpowered() ? 34 : 17;
        public override void AI()
        {
            base.AI();
            if (BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), (Main.rand.NextBool() ? Color.Aqua : Color.SkyBlue) * BladeScale, Main.rand.NextFloat(0.6f, 1f) * 0.04f * BladeScale * Projectile.scale).Configure(false, Main.rand.Next(12, 16), new Vector2(0.18f, 1f), false, false);
            }
            CEUtils.AddLight(Projectile.Center, new Color(50, 150, 255));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for(int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }

                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int i = 0; i < posC.Count; i++)
                {
                    float p = ((float)(1 + i) / posC.Count);
                    Color clr = Color.Aqua * 0.58f * p;
                    Main.spriteBatch.Draw(tex, posC[i] - Main.screenPosition, null, clr, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();

                for (int i = 0; i < posC.Count; i++)
                {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 22 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(0, 150, 160));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            if (BladeScale > 0)
            {
                Texture2D smear = CEExtraAssets.CircularSmear;
                float scale = Radius / 78f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 o = smear.Size() * 0.5f;

                ApplyShader(new Color(180, 230, 255));

                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 20, 255) * Projectile.Opacity * BladeScale, time * 50f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 20, 200) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 0.97f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 20, 255) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 0.94f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 20, 255) * Projectile.Opacity * BladeScale, time * -28f, o, scale * 0.9f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.75f, 1f), Projectile.Center);
            for (int i = 0; i < 12; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (i / 12f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.Aqua : Color.SkyBlue, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if(!target.boss)
            {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.4f, 1.7f), target.Center, volume: 0.9f);

            for (int i = 0; i < 12; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.Aqua : Color.SkyBlue, 0.04f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
        }
    }
}
