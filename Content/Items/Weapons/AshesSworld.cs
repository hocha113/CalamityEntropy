using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AshesSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 25;
            Item.DamageType = DamageClass.Melee;
            Item.width = 70;
            Item.height = 72;
            Item.useTime = 27;
            Item.useAnimation = 27;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 8;
            Item.value = Item.buyPrice(0, 10);
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AshesSworldHoldout>();
            Item.shootSpeed = 12f;
            Item.scale *= 0.66f;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int t = ModContent.ProjectileType<SmolderingRock>();

            for (int i = 0; i < 4; i++)
            {
                Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.6f), t, damage / 2, knockback, player.whoAmI);
            }
            return true;
        }

        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Cinderplate))
            {
                CreateRecipe().AddIngredient(ItemID.FieryGreatsword)
                .AddIngredient<TectonicShard>(6)
                .AddIngredient(CEID.Item_Cinderplate, 10)
                .AddTile(TileID.Hellforge)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient(ItemID.FieryGreatsword)
                .AddIngredient<TectonicShard>(16)
                .AddTile(TileID.Hellforge)
                .Register();
        }

    }
    public class AshesSworldHoldout : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AshesSword";
        //挥砍拖影贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Particles/SemiCircularSmearVerticalBlank")]
        internal static Asset<Texture2D> SmearVerticalBlankTex;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 1000;
        }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if (Projectile.localAI[0] == 0)
            {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            Projectile.localAI[0]++;

            float p = Projectile.localAI[0] / (player.itemTimeMax * Projectile.MaxUpdates);
            if (p >= 1)
            {
                Projectile.Kill();
                player.itemTime = 1;
                player.itemAnimation = 1;
                return;
            }
            player.itemTime = player.itemAnimation = 2;
            float r = -1.8f + 3.6f * CEUtils.CustomLerp1(p);
            Projectile.localAI[1] = CEUtils.Parabola(p, 1);
            Vector2 ov = Projectile.velocity;
            float or = Projectile.rotation;
            Projectile.StickToPlayer();
            Projectile.velocity = ov;
            Projectile.rotation = or;
            player.SetHandRotWithDir(Projectile.rotation, player.direction);
            Projectile.rotation = Projectile.velocity.ToRotation() + r * player.direction;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D c = SmearVerticalBlankTex.Value;
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(c, Projectile.Center - Main.screenPosition, null, Color.OrangeRed * 0.8f * Projectile.localAI[1], Projectile.GetOwner().direction * 0.4f + Projectile.rotation, c.Size() * 0.5f, Projectile.scale * 1.94f, Projectile.GetOwner().direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            Main.spriteBatch.ExitShaderRegion();
            int dir = -Projectile.GetOwner().direction;
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = dir > 0 ? new Vector2(20, 134) : new Vector2(tex.Width - 20, 134);
            float rot = Projectile.rotation + (dir > 0 ? MathHelper.ToRadians(70) : MathHelper.ToRadians(110));
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor, rot, origin, Projectile.scale, effect);
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), target.Center, Projectile.damage / 2, 180, DamageClass.Melee);
            for (int i = 0; i < 24; i++)
                //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 14, Main.rand.NextBool() ? Color.Firebrick : Color.Red, 0.06f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(3f, 0.4f), true);
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Pitch = 1.4f }, target.Center);
            for (int i = 0; i < 12; i++)
            {
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, Projectile.velocity.normalize().RotatedByRandom(0.66f) * Main.rand.NextFloat(4, 30), Color.OrangeRed, Projectile.scale * 0.04f).Configure(false, 12, new Vector2(0.3f, 1), false, false);
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 142 * Projectile.scale, targetHitbox, 30);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (142 * Projectile.scale) * Projectile.scale, 30, DelegateMethods.CutTiles);
        }
    }
    public class SmolderingRock : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.light = 0.3f;
            Projectile.timeLeft = 42;
            Projectile.tileCollide = true;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Vector2 vel = oldVelocity;
            if (Projectile.velocity.X == 0)
            {
                Projectile.velocity.X = -oldVelocity.X;
            }
            if (Projectile.velocity.Y == 0)
            {
                Projectile.velocity.Y = -oldVelocity.Y;
            }
            //CustomPulse贴图路径Configure现传,Texture属性填白图应付框架
            var p = PRTLoader.NewParticle<PRT_HadCircle2>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.16f);
            p.scale2 = 0.16f;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            return false;
        }
        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (Projectile.localAI[0]++ > 8)
                Projectile.velocity.Y += 0.6f;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1200);
            if (target != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (target.Center - Projectile.Center).normalize().RotatedByRandom(1f) * -Main.rand.NextFloat(4, 16), Color.OrangeRed, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                }
                if (Main.myPlayer == Projectile.owner)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, (target.Center - Projectile.Center).normalize() * 20, ModContent.ProjectileType<AshesFireballMelee>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(1.4f, 1.6f), Projectile.Center, 60, 0.2f);
            float scale = 60 / 40f;
            Projectile p = Projectile;
            PRTLoader.NewParticle<PRT_ShineParticle>(p.Center, Vector2.Zero, Color.OrangeRed * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_ShineParticle>(p.Center, Vector2.Zero, Color.Firebrick * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_CustomPulse>(p.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(p.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
            PRTLoader.NewParticle<PRT_CustomPulse>(p.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, 120, DamageClass.Melee);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Projectile.GetTexture();
            Rectangle frame = CEUtils.GetCutTexRect(texture, 3, Projectile.whoAmI % 3, false);
            Vector2 position = Projectile.Center;
            Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

            return false;
        }
        public int tofs;
    }
    public class AshesFireballMelee : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void PostAI()
        {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 9)
            {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public Color TrailColor(float completionRatio, Vector2 vertex)
        {
            Color result = new Color(255, 255, 255) * completionRatio;
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex)
        {
            return MathHelper.Lerp(0, 14 * Projectile.scale, completionRatio);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            float scale = 30 / 40f;
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.OrangeRed, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            target.AddBuff(BuffID.OnFire3, 180);
            if (Projectile.timeLeft > 2)
                Projectile.timeLeft = 2;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 25;
        }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 400;
            Projectile.MaxUpdates = 2;
        }
        public override void AI()
        {
            if (Projectile.timeLeft % 10 == 0)
                for (int i = 0; i < 5; i++)
                {
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, Projectile.velocity.normalize().RotatedByRandom(1f) * -Main.rand.NextFloat(2, 8), Color.OrangeRed, Projectile.scale * 0.025f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                }
            PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center + CEUtils.randomPointInCircle(4), CEUtils.randomPointInCircle(3), Color.Firebrick, 0.4f).Configure(1f, 6, Main.rand.NextFloat(-0.1f, 0.1f), true);

        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Color color = Color.OrangeRed;
            var mp = this;
            if (mp.odp.Count > 1)
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = color * 0.66f;
                b.A = 255;
                float a = 0;
                float lr = 0;
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18 * Projectile.scale,
                          new Vector3(0, 1, 1),
                        b * (1f / (float)mp.odp.Count)));
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18 * Projectile.scale,
                      new Vector3(0, 0, 1),
                      b * (1f / (float)mp.odp.Count)));
                for (int i = 1; i < mp.odp.Count; i++)
                {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                          b * a));
                    lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3)
                {
                    Texture2D tx = CEExtraAssets.wohslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                    ve = new List<ColoredVertex>();
                    b = color;

                    a = 0;
                    lr = 0;
                    for (int i = 1; i < mp.odp.Count; i++)
                    {
                        a += 1f / (float)mp.odp.Count;

                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 16 * a * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 16 * a * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                              b * a));
                        lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                    }
                    tx = CEExtraAssets.MegaStreakBacking2;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            tofs++;

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.Streak1Asset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            if (odp.Count > 1)
            {
                Vector2 position = odp[odp.Count - 1] - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
                CEUtils.DrawGlow(position + Main.screenPosition, color, Projectile.scale * 0.5f);
                CEUtils.DrawGlow(position + Main.screenPosition, Color.White, Projectile.scale * 0.3f);
            }
            return false;
        }
        public int tofs;
    }
}
