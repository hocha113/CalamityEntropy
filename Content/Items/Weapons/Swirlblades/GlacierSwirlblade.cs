using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class GlacierSwirlblade : ModItem, ICEChargeWeapon
    {
        // 充能条 5 秒；原潜伏乘数 伤害0.5/弹速1.2 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(5f, 0.5f, 1.2f);

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = Item.useTime = 28;
            Item.width = 92;
            Item.height = 92;
            Item.damage = 35;
            Item.crit = 4;
            Item.ArmorPenetration = 10;
            Item.UseSound = SoundID.Item1 with { Volume = 1.2f };
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<GlacierSwirlbladeProj>();
            Item.shootSpeed = 52f;
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
            if (CECal.CalChainReady(CEID.Item_IceStar, CEID.Item_CryonicBar))
            {
                CreateRecipe()
                .AddIngredient(ModContent.ItemType<AzafureSwirlblade>())
                .AddIngredient(CEID.Item_IceStar)
                .AddIngredient(CEID.Item_CryonicBar, 6)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<AzafureSwirlblade>())
                .AddIngredient(ItemID.FrostCore, 2)
                .AddIngredient(ItemID.ChlorophyteBar, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
        public override bool RangedPrefix()
        {
            return true;
        }
    }
    public class GlacierSwirlbladeProj : BaseSwirlblade
    {
        public override string Texture => CEUtils.ItemTexPath<GlacierSwirlblade>();
        public override int OldPosLength => 11;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 6;
            Projectile.width = Projectile.height = 70;
            Projectile.tileCollide = false;
        }
        public override float Radius => 140 * (Projectile.IsEmpowered() ? 1.2f : 1);
        public override int SpreadTime => Projectile.IsEmpowered() ? 120 : 21;
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
                BaseSwirlblade.ApplyShader(new Color(200, 220, 255));
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 70, 255) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 70, 255) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 70, 255) * Projectile.Opacity * BladeScale, time * 30f, o, scale * 0.98f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 70, 255) * Projectile.Opacity * BladeScale, time * -30f, o, scale * 0.96f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void AI()
        {
            base.AI();
            if(Projectile.IsEmpowered() && Spreaded)
            {
                Projectile.HomingToNPCNearby(3.6f * float.Clamp((Counter - (float)FlyTime) / 20f, 0, 1), 0.97f, 1600);
            }
            if(BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), (Main.rand.NextBool() ? Color.AliceBlue : Color.LightSkyBlue) * BladeScale, Main.rand.NextFloat(0.6f, 1f) * 0.04f * BladeScale * Projectile.scale).Configure(false, Main.rand.Next(12, 16), new Vector2(0.18f, 1f), false, false);
            }
            CEUtils.AddLight(Projectile.Center, new Color(140, 140, 255));
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.9f, 1.2f), Projectile.Center);
            for (int i = 0; i < 10; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (i / 10f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, (Main.rand.NextBool() ? Color.Aqua : Color.SkyBlue) * 0.8f, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
        }
        public override void OnRetract()
        {
            int sawType = ModContent.ProjectileType<GlacierSwirlbladeSaw>();
            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < (Projectile.IsEmpowered() ? 4 : 2); i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(32, 38) * (Projectile.IsEmpowered() ? 1.5f : 1), sawType, Projectile.damage / 5, 6, Projectile.owner, Radius * (Projectile.IsEmpowered() ? 0.5f : 0.4f));
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<Deceive>(120);
            if(!target.boss)
            {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.2f, 1.5f), target.Center, volume: 0.9f);

            for (int i = 0; i < 12; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.AliceBlue : Color.LightSkyBlue, 0.04f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
        }
    }
    public class GlacierSwirlbladeSaw : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 148;
            Projectile.localNPCHitCooldown = 18;
            Projectile.light = 0.7f;
        }
        public override bool ShouldUpdatePosition()
        {
            return NoPosUpdate <= 0;
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                SoundStyle ShootSound = new("CalamityEntropy/Assets/Sounds/SawShot", 2) { Pitch = 0.8f, Volume = 0.6f };
                SoundEngine.PlaySound(ShootSound, Projectile.Center);

                for (int i = 0; i < 16; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.6f, 1f) * 3f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.6f) * Main.rand.NextFloat(0.5f, 1) * 40;
                    dust.noGravity = false;
                    dust.color = Main.rand.NextBool() ? Color.AliceBlue : Color.LightSkyBlue;
                    dust.fadeIn = 2f;
                }
            }
            if (NoPosUpdate > 0)
            {
                NoPosUpdate--;
            }
            else if (CD > 0)
            {
                CD--;
            }
            if (Projectile.timeLeft < 20)
                Projectile.Opacity -= 1 / 20f;
            else if (Projectile.localAI[0] ++ > 9)
                Projectile.HomingToNPCNearby(4.2f, 0.92f, 1500);
        }
        public int NoPosUpdate = 0;
        public int CD = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CD <= 0)
            {
                Projectile.velocity = Projectile.velocity.normalize() * float.Max(Projectile.velocity.Length(), 80);
                NoPosUpdate = 6;
                CD = 8;
                for (int i = 0; i < 6; i++)
                {
                    float rot = 2;
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16) * Projectile.scale, Color.AliceBlue, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(-rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16) * Projectile.scale, Color.AliceBlue, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                }
            }

            CEUtils.PlaySound("slice", Main.rand.NextFloat(1, 1.4f), target.Center, 8, 0.6f);
            target.AddBuff<Deceive>(120);
            float scale = 1.5f;
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.3f, 1f) * scale * 1.6f * Projectile.scale;
                dust.velocity = CEUtils.randomPointInCircle(30 * Projectile.scale);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.AliceBlue : Color.LightSkyBlue;
                dust.fadeIn = 2f;
            }
            scale = 1.6f;
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.Aqua * 0.8f, scale * 1f * Projectile.scale).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f * Projectile.scale).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
        }

        public float BladeScale => 1;
        public float Radius => Projectile.ai[0];
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(projHitbox.Center.ToVector2(), Radius * Projectile.scale * BladeScale).Intersects(targetHitbox);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D smear = CEExtraAssets.CircularSmear;
            float scale = Radius / 78f * Projectile.scale * BladeScale;
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 o = smear.Size() * 0.5f;
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);

            BaseSwirlblade.ApplyShader(new Color(200, 220, 255));
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(30, 60, 255) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 1f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(30, 60, 255) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 0.98f, SpriteEffects.None, 0);
            Main.spriteBatch.UseAdditiveClamp();
            for (float i = 0; i <= 1f; i += 0.2f)
            {
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition - Projectile.velocity * i * 2, null, new Color(180, 220, 255) * (1.2f - i) * Projectile.Opacity * BladeScale * 0.64f, time * -36f, o, scale, SpriteEffects.None, 0);
            }


            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return (Projectile.Opacity > 0.6f && Projectile.localAI[0] > 10) ? null : false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
