using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class BlazingSwirlblade : ModItem, ICEChargeWeapon
    {
        // 充能条 6 秒；原潜伏乘数 伤害0.35/弹速1.4 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(6f, 0.35f, 1.4f);

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = Item.useTime = 30;
            Item.width = 74;
            Item.height = 74;
            Item.damage = 200;
            Item.crit = 7;
            Item.ArmorPenetration = 25;
            Item.UseSound = SoundID.Item1;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = CECal.RarityTurquoise(ModContent.RarityType<NihilityBlue>());
            Item.shoot = ModContent.ProjectileType<BlazingSwirlbladeProj>();
            Item.shootSpeed = 58f;
            Item.knockBack = 4f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CEUtils.PlaySound("SwingMid", Main.rand.NextFloat(1.2f, 1.5f), position, 16, 0.4f);
            bool ult = CEChargeWeapon.TryConsume(player, Item);
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (ult && p >= 0 && p < Main.maxProjectiles)
            {
                CEChargeWeapon.Empower(p);
            }
            return false;
        }
        public override bool RangedPrefix()
        {
            return true;
        }

        // 2026-08-31 平衡案删掉了 3.33 的配方、改挂拜月邪教徒 1/3,导致装灾厄时稀有度(亵渎后档)
        // 与获取档位(月总前)对不上,轮刃链中段也断了。装灾厄时还原 3.33 配方并给拜月那行加门;
        // 无灾厄什么都不注册,保持 4.0 现状
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_MoltenAmputator, CEID.Item_DivineGeode, CEID.Item_UnholyEssence))
            {
                CreateRecipe()
                    .AddIngredient<ExergySwirlblade>()
                    .AddIngredient(CEID.Item_MoltenAmputator)
                    .AddIngredient(CEID.Item_DivineGeode, 12)
                    .AddIngredient(CEID.Item_UnholyEssence, 8)
                    .AddTile(TileID.MythrilAnvil)
                    .Register();
            }
        }
    }
    public class BlazingSwirlbladeProj : BaseSwirlblade
    {
        //火焰拖影贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Particles/CircularSmearFire2")]
        internal static Asset<Texture2D> SmearFire2Asset;
        [VaultLoaden("CalamityEntropy/Assets/Particles/CircularSmearFire3")]
        internal static Asset<Texture2D> SmearFire3Asset;
        public override string Texture => CEUtils.ItemTexPath<BlazingSwirlblade>(); public override int OldPosLength => 12;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 6;
            Projectile.tileCollide = false;
            Projectile.light = 1;
        }
        public override float Radius => 180 * (Projectile.IsEmpowered() ? 1.6f : 1);
        public override int SpreadTime => Projectile.IsEmpowered() ? 90 : 22;
        public override void AI()
        {
            base.AI();
            if (BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), (Main.rand.NextBool() ? Color.Firebrick : Color.Orange) * BladeScale, Main.rand.NextFloat(0.6f, 1f) * 0.04f * BladeScale * Projectile.scale).Configure(false, Main.rand.Next(12, 16), new Vector2(0.18f, 1f), false, false);
            }
            fhCd--;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for (int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }

                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int i = 0; i < posC.Count; i++)
                {
                    float p = ((float)(1 + i) / posC.Count);
                    Color clr = new Color(255, 255, 200) * 0.58f * p;
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
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(255, 255, 120));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            if (BladeScale > 0)
            {
                Texture2D smear = CEExtraAssets.CircularSmear;
                float scale = Radius / 78f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 o = smear.Size() * 0.5f;
                Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);

                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                float drawRotation = Projectile.rotation;
                float effectScale = 1f;
                float fakeRot = time * 16;

                Asset<Texture2D> p = SmearFire2Asset;
                Asset<Texture2D> p2 = SmearFire3Asset;
                for (int i = 0; i < 3; i++)
                {
                    Main.EntitySpriteDraw(p2.Value, drawPosition, null, Color.Orchid * 0.7f * effectScale, fakeRot * (Main.rand.NextFloat(1.5f, 1.55f) * (i * 0.7f + 0.2f)), p2.Size() * 0.5f, 1.1f * Main.rand.NextFloat(0.8f, 1.15f) * effectScale * scale * 0.9f, SpriteEffects.None);
                    Main.EntitySpriteDraw(p.Value, drawPosition, null, Color.Orange * 0.8f * effectScale, fakeRot * (Main.rand.NextFloat(1.1f, 1.15f) * (i * 0.5f + 0.2f)), p.Size() * 0.5f, 0.9f * effectScale * scale * 0.9f, SpriteEffects.None);
                }
                fakeRot *= -1f;
                for (int i = 0; i < 3; i++)
                {
                    Main.EntitySpriteDraw(p2.Value, drawPosition, null, Color.Orchid * 0.7f * effectScale, fakeRot * (Main.rand.NextFloat(1.5f, 1.55f) * (i * 0.5f + 0.2f)), p2.Size() * 0.5f, 1.1f * Main.rand.NextFloat(0.8f, 1.15f) * effectScale * scale * 0.78f, SpriteEffects.FlipHorizontally);
                    Main.EntitySpriteDraw(p.Value, drawPosition, null, Color.Orange * 0.8f * effectScale, fakeRot * (Main.rand.NextFloat(1.1f, 1.15f) * (i * 0.5f + 0.2f)), p.Size() * 0.5f, 0.9f * effectScale * scale * 0.78f, SpriteEffects.FlipHorizontally);
                }
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("BlazingSwirlbladeUse", Main.rand.NextFloat(1f, 1.2f), Projectile.Center, 16, 0.85f);
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.6f, 0.76f), Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (i / 12f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.Yellow : Color.Orange, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
            }
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = ((MathHelper.TwoPi * i / 18) - (MathHelper.Pi / 16f)).ToRotationVector2() * 40f;
                PRTLoader.NewParticle<PRT_CritSparkCal>(Projectile.Center, velocity, new Color(255, 255, 230), 2f).Configure(Main.rand.NextBool() ? Color.Yellow : Color.LightBlue, 28, 0.1f, 3f, Main.rand.NextFloat(0f, 0.01f));
            }
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BlazingSwirlbladeFlame>(), (int)(Projectile.damage * 0.66f), 0, Projectile.owner, 0, Projectile.IsEmpowered() ? 0.25f : 0);
            }
        }
        public int fhCd = 0;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("ApsychosHit", Main.rand.NextFloat(1f, 1.6f), target.Center, 3);
            target.AddBuff<HolyFlames>(300);
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1f, 1.4f), target.Center, volume: 0.6f);
            for (int i = 0; i < 12; i++) {
                Vector2 velocity = ((MathHelper.TwoPi * i / 12) - (MathHelper.Pi / 16f) + Main.GameUpdateCount * 0.2f).ToRotationVector2() * 22f;
                PRTLoader.NewParticle<PRT_CritSparkCal>(target.Center, velocity, new Color(255, 255, 230), 0.8f).Configure(Main.rand.NextBool() ? Color.Yellow : Color.LightBlue, 20, 0.1f, 1.8f, Main.rand.NextFloat(0f, 0.01f));
            }
            if (!target.boss) {
                target.velocity *= 0.4f;
            }

            for (int i = 0; i < 8; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, CEUtils.randomRot().ToRotationVector2() *
                    Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.Yellow : Color.SkyBlue,
                    0.04f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 12, new Vector2(2.4f, 0.6f), true);

            float scale = 1.8f;
            for (int i = 0; i < 4; i++) {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.3f, 1f) * scale * 1.6f;
                dust.velocity = CEUtils.randomPointInCircle(30);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.Orange : Color.Yellow;
                dust.fadeIn = 2f;
            }
            if (fhCd <= 0) {
                fhCd = 2;
                scale = 4f;
                for (float i = 1; i >= 0.2f; i -= 0.4f) {
                    PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.Lerp(new Color(200, 200, 120), new Color(160, 160, 0), i) * i, 0.005f * i * (Radius / 180f)).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f * i * (Radius / 180f), scale * 0.07f * i * (Radius / 180f), 12 + (int)(i * 8));
                }
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), target.Center, Projectile.damage / 5, scale * 54 * (Radius / 180f), Projectile.DamageType);
            }
        }
    }
    public class BlazingSwirlbladeFlame : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 40;
            Projectile.MaxUpdates = 2;
            Projectile.localNPCHitCooldown = -1;
        }
        public float Radius = 600;
        public float rm = 0;
        public override void AI()
        {
            rm = (CEUtils.Parabola((1 - Projectile.timeLeft / 40f) * 0.5f, 1)) * (1 + Projectile.ai[1]);
            if (Projectile.timeLeft < 13)
                Projectile.Opacity -= 1f / 13f;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(Projectile.Center, Radius * rm * (1 + Projectile.ai[1])).Intersects(targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<HolyFlames>(300);

            float scale = 1.8f;
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.3f, 1f) * scale * 1.6f;
                dust.velocity = CEUtils.randomPointInCircle(30);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.Orange : Color.Yellow;
                dust.fadeIn = 2f;
            }
            scale = 1.6f;
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.Yellow * 0.8f, scale * 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White, scale * 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var gd = Main.graphics.GraphicsDevice;
            Texture2D tx = CEExtraAssets.DeathRay;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            gd.Textures[0] = tx;
            {
                List<Vector3> points = new List<Vector3>();
                for (float i = 0; i <= 1; i += 0.005f)
                {
                    float rot = i * MathHelper.TwoPi;
                    float c = (float)(Math.Sin(MathHelper.TwoPi * 10 * i)) * 0.016f + 1f;
                    c *= (Projectile.ai[1] + 1);
                    points.Add(new Vector3(rot.ToRotationVector2() * Radius * rm * c, 1));
                }
                Vector3 lastPoint = points[points.Count - 2];
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<ColoredVertex> ve2 = new List<ColoredVertex>();
                float alpha = Projectile.Opacity;
                float trailOffset = Main.GlobalTimeWrappedHourly * -10f;
                Vector2 center = Projectile.Center;
                for (int ii = 0; ii < points.Count; ii++)
                {
                    int i = ii;
                    Vector2 pos = points[i].xy();
                    float w = points[i].Z * 40;
                    Vector2 v = (lastPoint.xy() - pos).RotatedBy(MathHelper.PiOver2).normalize();
                    ve.Add(new ColoredVertex(center + pos + v * w * Projectile.scale - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 12 + trailOffset * 0.6f, 1, 1),
                          new Color(200, 200, 90) * alpha));
                    ve.Add(new ColoredVertex(center + pos - v * w * Projectile.scale - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 12 + trailOffset * 0.6f, 0, 1),
                          new Color(120, 120, 170) * alpha));
                    ve2.Add(new ColoredVertex(center + pos + v * w * Projectile.scale * 0.76f - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 12 + trailOffset, 1, 1),
                          new Color(200, 200, 90) * alpha));
                    ve2.Add(new ColoredVertex(center + pos - v * w * Projectile.scale * 0.76f - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 12 + trailOffset, 0, 1),
                          new Color(120, 120, 120) * alpha));

                    lastPoint = points[i];
                }
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
            }

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return null;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
