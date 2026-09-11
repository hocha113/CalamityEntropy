using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Revelation : ModItem, ICEChargeWeapon
    {
        // 充能条 6 秒；原潜伏乘数 伤害0.75/弹速1/击退4 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(6f, 0.75f, 1f, 4f);

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 34;
            Item.damage = 100;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<RevelationThrown>();
            Item.shootSpeed = 20f;
            Item.DamageType = DamageClass.Melee;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (CEChargeWeapon.TryConsume(player, Item))
            {
                for(int i = 0; i < 3; i++)
                {
                    int p = Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.2f) * (0.8f + i * 0.12f), type, damage, knockback, player.whoAmI, 0, 0, i == 0 ? 1 : 0);
                    if (p >= 0 && p < Main.maxProjectiles)
                        CEChargeWeapon.Empower(p);
                }
                return false;
            }
            if (Main.zenithWorld)
            {
                for (int i = 0; i < 8; i++)
                    Projectile.NewProjectile(source, position, velocity.RotatedBy(i * MathHelper.PiOver4), type, damage / 6, knockback, player.whoAmI);
            }
            else
            {
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            }
            return false;
        }

        // 2026-08-31 平衡案删掉了 3.33 的配方、改挂拜月邪教徒 1/3。
        // 装灾厄时还原 3.33 配方,同时拜月那一行进 !CERef.Has 门;无灾厄什么都不注册,保持 4.0 现状
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_SubductionSlicer, CEID.Item_MeldBlob))
            {
                CreateRecipe()
                    .AddIngredient(CEID.Item_SubductionSlicer)
                    .AddIngredient(CEID.Item_MeldBlob, 12)
                    .AddTile(TileID.LunarCraftingStation)
                    .Register();
            }
        }
    }
    public class RevelationThrown : ModProjectile
    {
        public override string Texture => base.Texture;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 110;
            Projectile.MaxUpdates = 4;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 180 * 4;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRot = new List<float>();
        public List<float> oldTexRot = new List<float>();
        public List<Vector2> oldSize = new List<Vector2>();
        public Vector2 Size = new Vector2(1, 0);
        public float texRot = 0;
        public float Counter { get { return Projectile.localAI[0]; } set { Projectile.localAI[0] = value; } }
        public Vector2 vec1 = Vector2.Zero;
        public Vector2 vec2 = Vector2.Zero;
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            int flyTime = 4 * 22;
            if (Counter < flyTime)
            {
                if (Counter > 4 * 9)
                {
                    Projectile.velocity *= 0.88f;
                }
                Size.Y = float.Lerp(Size.Y, 1, 0.024f);

            }
            if (Counter == flyTime)
            {
                vec1 = Projectile.Center;
                vec2 = Vector2.Lerp(player.Center, Projectile.Center, 0.5f) + (Projectile.Center - player.Center).RotatedBy(MathHelper.PiOver2).normalize() * Main.rand.NextFloat(200, 500) * (Main.rand.NextBool() ? 1 : -1);
                CEUtils.PlaySound("CeramicImpact" + Main.rand.Next(1, 3), Main.rand.NextFloat(1.2f, 1.4f), Projectile.Center, 12, 0.7f);
                CEUtils.PlaySound("GunShotSmall", Main.rand.NextFloat(0.6f, 1f), Projectile.Center, 12, 1f);
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<RevelationExplode>(), Projectile.damage, 0, Projectile.owner, 100 * Projectile.scale);
                    if (!Projectile.IsEmpowered())
                    {
                        int sType = ModContent.ProjectileType<RevelationBolt>();
                        float rj = CEUtils.randomRot();
                        for (int i = 0; i < 2; i++)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, (i * MathHelper.TwoPi / 2f + rj).ToRotationVector2() * 32, sType, Projectile.damage / 2, Projectile.knockBack, Projectile.owner);
                        }
                    }
                    Projectile.ResetLocalNPCHitImmunity();
                }
            }
            int BackTime = 4 * 20;
            if (Counter > flyTime)
            {
                if (Projectile.IsEmpowered())
                {
                    NPC homing = CEUtils.FindTarget_HomingProj(Projectile, player.Center, 2000);
                    if (homing != null)
                    {
                        Projectile.velocity *= 0.96f;
                        Projectile.velocity += (homing.Center - Projectile.Center).normalize() * 1.6f;
                    }
                    Size.Y = float.Lerp(Size.Y, homing == null ? 1 : 0.24f, 0.08f);
                }
                else
                {
                    Size.Y = float.Lerp(Size.Y, 0.1f, 0.02f);
                    float p = (Counter - flyTime) / BackTime;
                    if (p >= 1)
                    {
                        Projectile.Kill();
                        return;
                    }
                    Vector2 v = CEUtils.Bezier(new List<Vector2>() { vec1, vec2, player.MountedCenter }, (1 - CEUtils.Parabola(0.5f + 0.5f * p, 1)));
                    Projectile.velocity = v - Projectile.Center;
                }
            }
            SpawnVParticles();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Counter++;
            oldPos.Add(Projectile.Center);
            oldRot.Add(Projectile.rotation);
            oldTexRot.Add(texRot);
            oldSize.Add(Size);
            if (oldPos.Count > 16)
            {
                oldPos.RemoveAt(0);
                oldRot.RemoveAt(0);
                oldTexRot.RemoveAt(0);
                oldSize.RemoveAt(0);
            }
            texRot += 0.12f * Math.Sign(Projectile.velocity.X);
        }
        public override bool? CanDamage()
        {
            return (Projectile.IsEmpowered() && Counter < 4 * 22) ? false : null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("DemonSwordImpact2", Main.rand.NextFloat(1.4f, 1.8f), target.Center, 8, 0.5f);
            SpawnVParticles(6, 2);
            RevelationExplode.ExpParticle(Projectile.Center, 90, 0.5f);
            if(Projectile.IsEmpowered())
            {
                Projectile.Kill();
                if (Projectile.ai[2] > 0)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<RevelationStealth>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0, target.whoAmI);
                }
            }
        }
        public void SpawnVParticles(int num = 1, float scale = 1)
        {
            float num2 = 360f / num;
            Color color1 = Color.LightGreen;
            Color color2 = Color.Black;
            for (int j = 0; (float)j < num; j++)
            {
                float num3 = CEUtils.randomRot();
                Vector2 vector = (Vector2.UnitX * Main.rand.NextFloat(12, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Vector2 vector2 = (Vector2.UnitX * Main.rand.NextFloat(12, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Dust dust = Dust.NewDustPerfect(Projectile.Center + vector, Main.rand.NextBool(4) ? ModContent.DustType<LightDust>() : (ModContent.DustType<VoidDustInverted>()), vector2);
                dust.noGravity = dust.type != 278;
                dust.color = color1;
                dust.velocity = vector2 * scale;
                dust.scale = Main.rand.NextFloat(1.6f, 2.2f) * 0.54f * scale;
            }
        }
        public void DrawRevelation(Texture2D tex, Color color, Vector2 position, float rotation, float texRotation, Vector2 scale)
        {
            DrawRevelation(tex, color, position, rotation, texRotation, scale, BlendState.AlphaBlend);
        }
        public void DrawRevelation(Texture2D tex, Color color, Vector2 position, float rotation, float texRotation, Vector2 scale, BlendState bs)
        {
            Effect shader = CommonEffects.rotation;
            shader.Parameters["rad"].SetValue(texRotation);
            shader.Parameters["center"].SetValue(Vector2.One * 0.5f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(tex, position, null, color, rotation, tex.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Projectile.GetTexture();
            for (int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;
                DrawRevelation(texture, Color.White * 0.4f * p, oldPos[i] - Main.screenPosition, oldRot[i], oldTexRot[i], oldSize[i]);
            }
            for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4 * 0.5f)
            {
                DrawRevelation(texture, Color.LightGreen, Projectile.Center + i.ToRotationVector2() * 5 - Main.screenPosition, Projectile.rotation, texRot, Size, BlendState.Additive);
            }
            DrawRevelation(texture, Color.White, Projectile.Center - Main.screenPosition, Projectile.rotation, texRot, Size);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
    public class RevelationStealth : ModProjectile
    {
        public override string Texture => CEUtils.ItemTexPath<Revelation>();
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 240;
            Projectile.MaxUpdates = 4;
            Projectile.localNPCHitCooldown = -1;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRot = new List<float>();
        public override bool? CanDamage()
        {
            return false;
        }
        public float Counter { get { return Projectile.localAI[0]; } set { Projectile.localAI[0] = value; } }
        public int target => (int)Projectile.ai[1];
        public int ExplodeCounts = 3;
        public Vector2 vec = Vector2.Zero;
        public void GigaExplode()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<RevelationExplode>(), Projectile.damage * 2, 0, Projectile.owner, 300);
            }
            RevelationExplode.ExpParticle(Projectile.Center, 300, 1);
            for (int i = 0; i < 32; i++)
            {
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(24, 80), Color.LightGreen, Projectile.scale * 0.08f).Configure(false, 36, new Vector2(0.22f, 1), false, false);
            }
            CEUtils.PlaySound("DoGLaserWallBigAttack", Main.rand.NextFloat(1.6f, 2f), Projectile.Center);
            CEUtils.PlaySound("explosionbig", Main.rand.NextFloat(1, 1.2f), Projectile.Center);
            CEUtils.PlaySound("explosionbig", Main.rand.NextFloat(1, 1.2f), Projectile.Center);
            CEUtils.PlaySound("DemonSwordInsaneImpact", Main.rand.NextFloat(1.4f, 1.8f), Projectile.Center, 12, 0.7f);
            ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(-Projectile.velocity.normalize() * 7, 4), Projectile.Distance(Main.LocalPlayer.Center), 5000);
        }
        public override void AI()
        {
            if(Projectile.Entropy().FirstFrames)
            {
                //各端首帧本地打标，无需补发同步
                Projectile.SetEmpowered(false);
                GigaExplode();
            }
            Player player = Projectile.GetOwner();
            NPC tn = target.ToNPC();
            if(tn == null || !tn.active)
            {
                GigaExplode();
                Projectile.Kill();
                return;
            }
            int flyTime = 4 * 30;
            if(Counter == 0)
            {
                Projectile.velocity = Projectile.velocity.normalize().RotatedByRandom(0.3f) * -60;
            }
            Counter++;
            int tt = (int)(flyTime * 0.58f);
            if (Counter == tt)
            {
                vec = Projectile.Center;
                Projectile.velocity *= 0;
            }
            if(Counter <= tt)
            {
                Projectile.velocity *= 0.94f;
            }
            else
            {
                float p = (Counter - tt) / (flyTime - tt);
                if (p >= 1)
                {
                    Counter = 0;
                    GigaExplode();
                    ExplodeCounts--;
                    if(ExplodeCounts <= 0)
                    {
                        Projectile.Kill();
                        return;
                    }
                }
                Vector2 tpos = Vector2.Lerp(vec, tn.Center, (1 - CEUtils.Parabola(0.5f + 0.5f * p, 1)));
                Projectile.velocity = tpos - Projectile.Center;
            }
            SpawnVParticles(2, 1.6f);
            Projectile.rotation += 0.15f;
            oldPos.Add(Projectile.Center);
            oldRot.Add(Projectile.rotation);
            if(oldPos.Count > 50)
            {
                oldPos.RemoveAt(0);
                oldRot.RemoveAt(0);
            }
        }
        public void SpawnVParticles(int num = 1, float scale = 1)
        {
            float num2 = 360f / num;
            Color color1 = Color.LightGreen;
            Color color2 = Color.Black;
            for (int j = 0; (float)j < num; j++)
            {
                float num3 = CEUtils.randomRot();
                Vector2 vector = (Vector2.UnitX * Main.rand.NextFloat(12, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Vector2 vector2 = (Vector2.UnitX * Main.rand.NextFloat(12, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Dust dust = Dust.NewDustPerfect(Projectile.Center + vector, Main.rand.NextBool(4) ? ModContent.DustType<LightDust>() : (ModContent.DustType<VoidDustInverted>()), vector2);
                dust.noGravity = dust.type != 278;
                dust.color = color1;
                dust.velocity = vector2 * scale;
                dust.scale = Main.rand.NextFloat(1.6f, 2.2f) * 0.54f * scale;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Projectile.GetTexture();
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);
            for (int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;

                Main.spriteBatch.Draw(texture, oldPos[i] - Main.screenPosition, null, Color.LightGreen * p, oldRot[i], texture.Size() * 0.5f, Projectile.scale * 2, SpriteEffects.None, 0);
            }
            for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4 * 0.5f)
            {
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + i.ToRotationVector2() * 10, null, Color.LightGreen, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 2, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 2, SpriteEffects.None, 0);
            return false;
        }
    }

    public class RevelationBolt : EBookBaseProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.timeLeft = 200;
            Projectile.MaxUpdates = 2;
        }
        public List<Vector2> odp = new List<Vector2>();
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            CEUtils.AddLight(Projectile.Center, Color.LightGreen, Projectile.scale);
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > 3)
            {
                Projectile.HomingToNPCNearby(4f, 0.85f, 2000);
            }
            for (float i = 0.1f; i <= 1f; i += 0.1f)
            {
                odp.Add(Projectile.Center + Projectile.velocity * i);
                if (odp.Count > 140 + (Projectile.ai[0] > 0 ? 60 : 0))
                {
                    odp.RemoveAt(0);
                }
            }
        }
        public override bool? CanDamage()
        {
            return Projectile.localAI[0] > 3 ? null : false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }
        public override void OnKill(int timeLeft)
        {
            RevelationExplode.ExpParticle(Projectile.Center, 30, 0.6f);
            CEUtils.PlaySound("cinderExplosion", Main.rand.NextFloat(1.6f, 2f), Projectile.Center, 12, 0.5f);
        }
        public static void DrawBlackTrail(List<CEUtils.VertexPointSets> sets, Color a, Texture2D trail1, float innerWidth = 1)
        {
            if (sets.Count > 1)
            {
                List<CEUtils.VertexPointSets> sets1 = new List<CEUtils.VertexPointSets>();
                Vector2 lastPoint = Vector2.Zero;
                float cxOffset = 0;
                for (int i = 0; i < sets.Count; i++)
                {
                    var s = sets[i];

                    if (i > 0)
                        cxOffset += CEUtils.getDistance(lastPoint, s.Position) * 0.007f;
                    float opc = (s.Color.A / 255f);
                    sets1.Add(new CEUtils.VertexPointSets(s.Position, a * opc, s.Width * innerWidth, cxOffset + Main.GlobalTimeWrappedHourly * 6));
                }
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearWrap);

                List<ColoredVertex> lt;
                lt = sets1.GetVertexesList(false);
                gd.Textures[0] = trail1;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, lt.ToArray(), 0, lt.Count - 2);

                Main.spriteBatch.ExitShaderRegion();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> vp = new();
            for (int i = 0; i < odp.Count; i++)
            {
                float p = (i / (odp.Count - 1f));
                float alpha = p < 0.7f ? p / 0.7f : 1;
                float width = 1;
                if (p < 0.8f)
                    width = p / 0.8f;
                else
                    width = CEUtils.Parabola(0.5f + (p - 0.8f) / 0.4f, 1);
                vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 15 * Projectile.scale * width * (Projectile.IsEmpowered() ? 1.8f : 1), 0));
            }
            DrawBlackTrail(vp, Color.Black, CEExtraAssets.MegaStreakBacking2b, 2f);
            if (vp.Count > 6)
            {
                vp = new();
                for (int i = 0; i < odp.Count - 6; i++)
                {
                    float p = (i / (odp.Count - 6f - 1f));
                    float alpha = p < 0.7f ? p / 0.7f : 1;
                    float width = 1;
                    if (p < 0.8f)
                        width = p / 0.8f;
                    else
                        width = CEUtils.Parabola(0.5f + (p - 0.8f) / 0.4f, 1);
                    vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 15 * Projectile.scale * width * (Projectile.IsEmpowered() ? 1.8f : 1), 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, Color.White, Color.LightGreen);
            }
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
    public class RevelationExplode : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public float Radius => Projectile.ai[0] * Projectile.scale;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(Projectile.Center, Radius).Intersects(targetHitbox);
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.timeLeft = 5;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void AI()
        {
            if(Projectile.Entropy().FirstFrames)
            {
                ExpParticle(Projectile.Center, Radius, 1);
            }
        }
        public static void ExpParticle(Vector2 pos, float Radius, float alpha = 1)
        {
            Color color1 = Color.LightGreen * alpha;
            Color color2 = Color.Black * alpha;
            float ExplosionRadius = Radius;
            PRTLoader.NewParticle<PRT_DetailedExplosionCal>(pos, Vector2.Zero, color1, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5f, 5f), ExplosionRadius * 0.0065f + 0.1f, Main.rand.Next(15, 22), true, renderLayer: PRTRenderLayer.AfterPlayers);
            PRTLoader.NewParticle<PRT_DetailedExplosionCal>(pos, Vector2.Zero, Color.Black * alpha, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5f, 5f), ExplosionRadius * 0.0045f + 0.1f, Main.rand.Next(15, 22), useAdditiveBlend: false);
            PRTLoader.NewParticle<PRT_DetailedExplosionCal>(pos, Vector2.Zero, Color.Black * alpha, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5f, 5f), ExplosionRadius * 0.003f + 0.1f, Main.rand.Next(15, 22), useAdditiveBlend: false);
            for (int i = 0; i < 4; i++)
            {
                PRTLoader.NewParticle<PRT_CustomPulse>(pos, Vector2.Zero, color1, 0f).Configure("CalamityEntropy/Assets/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, ExplosionRadius * 0.005f + 0.05f, 25, renderLayer: PRTRenderLayer.AfterPlayers);
            }

            float num = ExplosionRadius * 0.1f + 10f;
            float num2 = 360f / num;
            for (int j = 0; (float)j < num; j++)
            {
                float num3 = MathHelper.ToRadians((float)j * num2);
                Vector2 vector = (Vector2.UnitX * Main.rand.NextFloat(ExplosionRadius * 0.2f, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Vector2 vector2 = (Vector2.UnitX * Main.rand.NextFloat(ExplosionRadius * 0.2f, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Dust dust = Dust.NewDustPerfect(pos + vector, Main.rand.NextBool(4) ? ModContent.DustType<LightDust>() : ((Main.rand.NextBool()) ? 278 : ModContent.DustType<VoidDustInverted>()), vector2);
                dust.noGravity = dust.type != 278;
                dust.color = color1;
                dust.velocity = vector2;
                dust.scale = ((dust.type == 278) ? Main.rand.NextFloat(0.7f, 1.3f) : Main.rand.NextFloat(1.6f, 2.2f));
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
