using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Fractal
{
    public class VoidFractal : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 600;
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;
            Item.height = 60;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<VoidFractalHeld>();
            Item.shootSpeed = 12f;
            Item.scale *= 0.66f;
        }
        public int atkType = 0;
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2 && player.chaosState)
            {
                return false;
            }
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                CEUtils.PlaySound("VoidAnticipation", 1, position, volume: CEUtils.WeapSound);
                player.AddBuff(BuffID.ChaosState, 10 * 60);
                Projectile.NewProjectile(source, position, velocity * 4, ModContent.ProjectileType<VoidSlash>(), damage * 25, 0, player.whoAmI);
                return false;
            }
            int at = 2;
            if (atkType == 0 || atkType == 2 || atkType == 4)
            {
                at = -1;
            }
            if (atkType == 1 || atkType == 3 || atkType == 5)
            {
                at = 1;
            }
            if (atkType == 7)
            {
                at = 3;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, at, 0, Main.MouseWorld.Distance(position) + 180);
            atkType += 1;
            if (atkType > 7)
            {
                atkType = 0;
            }
            return false;
        }

        public override bool MeleePrefix()
        {
            return true;
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_MawOfInfinity, ModContent.ItemType<ElementalFractal>())
                .AddIngredient<SpiritFractal>()
                .AddIngredient<VoidBar>(5)
                .AddTile<VoidWellTile>()
                .Register();
        }
    }

    public class VoidFractalHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/VoidFractal";
        List<float> odr = new List<float>();
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.extraUpdates = 9;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public float spawnProjCounter = 0;
        public override void AI()
        {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            counter += 1 * (Projectile.ai[0] == 3 ? 0.7f : (Projectile.ai[0] == 2 ? 0.5f : 1));
            if (init)
            {
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                if (Projectile.ai[0] == 2)
                {
                    CEUtils.PlaySound("sf_use", 0.6f, Projectile.Center, volume: 0.8f * CEUtils.WeapSound);
                    Projectile.scale *= 1.3f;
                    CEUtils.PlaySound("CastTriangles", 1, Projectile.Center, volume: CEUtils.WeapSound);

                }
                if (Projectile.ai[0] < 2)
                {
                    CEUtils.PlaySound("sf_use", 1 + Projectile.ai[0] * 0.12f, Projectile.Center, volume: 0.6f * CEUtils.WeapSound);
                }
                if (Projectile.ai[0] == 3)
                {
                    CEUtils.PlaySound("sf_use", 0.75f, Projectile.Center, volume: CEUtils.WeapSound);
                }
                init = false;
            }
            odr.Add(Projectile.rotation);
            Projectile.timeLeft = 3;

            if (Projectile.ai[0] == 2)
            {
                for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                {
                    if (Projectile.localNPCImmunity[i] == -1)
                        Projectile.localNPCImmunity[i] = (int)(Projectile.MaxUpdates * 4 / owner.GetTotalAttackSpeed(Projectile.DamageType));
                }
                Projectile.localNPCHitCooldown = (int)(Projectile.MaxUpdates * 4 / owner.GetTotalAttackSpeed(Projectile.DamageType));

                float RotF = MathHelper.ToRadians(280) + MathHelper.TwoPi * 3;
                if (progress > 0.3f && progress < 0.7f)
                    spawnProjCounter += owner.GetTotalAttackSpeed(Projectile.DamageType);
                if (spawnProjCounter >= 15f)
                {
                    spawnProjCounter -= 6f;
                    Vector2 spawnPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 48 * scale * Projectile.scale;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, CEUtils.randomPointInCircle(0.1f) + Projectile.rotation.ToRotationVector2() * 8, ModContent.ProjectileType<VoidStarF>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner).ToProj().DamageType = DamageClass.Melee;
                    }
                }
                alpha = 1;
                scale = 2.4f;
                Projectile.rotation = Projectile.velocity.ToRotation() + (MathHelper.ToRadians(-140) + RotF * CEUtils.GetRepeatedCosFromZeroToOne(progress, 1)) * (Projectile.velocity.X > 0 ? 1 : -1);
                Projectile.Center = CEUtils.GetOwner(Projectile).MountedCenter + CEUtils.normalize(Projectile.velocity) * (CEUtils.Parabola(progress, Projectile.ai[2]));
                if (Projectile.velocity.X > 0)
                {
                    owner.direction = 1;
                    owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Projectile.Center - owner.Center).ToRotation() - (float)(Math.PI * 0.5f));
                }
                else
                {
                    owner.direction = -1;
                    owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Projectile.Center - owner.Center).ToRotation() - (float)(Math.PI * 0.5f));
                }
            }
            else
            {
                if (Projectile.ai[0] == 3)
                {
                    float RotF = 4.8f;
                    alpha = 1;
                    scale = 3.5f;
                    Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + RotF * CEUtils.GetRepeatedCosFromZeroToOne(progress, 4)) * -1 * (Projectile.velocity.X > 0 ? -1 : 1);
                    Projectile.Center = Projectile.GetOwner().MountedCenter;


                    if (Projectile.velocity.X > 0)
                    {
                        owner.direction = 1;
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
                    }
                    else
                    {
                        owner.direction = -1;
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
                    }
                }
                else
                {
                    float RotF = 4f;
                    alpha = 1;
                    scale = 1.8f;
                    Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + RotF * CEUtils.GetRepeatedCosFromZeroToOne(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                    Projectile.Center = Projectile.GetOwner().MountedCenter;
                    if (progress > 0.2f && shoot)
                    {
                        shoot = false;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, (Vector2)(CEUtils.normalize(Projectile.velocity) * 12 + CEUtils.randomPointInCircle(6)), ModContent.ProjectileType<VoidWave>(), Projectile.damage * 1, Projectile.knockBack, Projectile.owner);
                    }
                    if (progress < 0.6f)
                    {
                        if (Projectile.velocity.X > 0)
                        {
                            owner.direction = 1;
                            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
                        }
                        else
                        {
                            owner.direction = -1;
                            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
                        }
                    }
                }
            }
            if (odr.Count > 60)
            {
                odr.RemoveAt(0);
            }

            if (progress < 0.6f || Projectile.ai[0] > 1)
            {
                owner.heldProj = Projectile.whoAmI;
                owner.itemTime = 2;
                owner.itemAnimation = 2;
            }
            if (counter > MaxUpdateTimes)
            {
                Projectile.Kill();
                if (Projectile.ai[0] <= 1)
                {
                    owner.itemTime = 2;
                    owner.itemAnimation = 2;
                }
            }
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public bool playHitSound = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {

            EGlobalNPC.AddVoidTouch(target, 40, 1.4f, 600, 16);
            if (playHitSound || Projectile.ai[0] == 2)
            {
                playHitSound = false;
                CEUtils.PlaySound("sf_hit", 1, Projectile.Center, volume: CEUtils.WeapSound);
                CEUtils.PlaySound("FractalHit", 1, Projectile.Center, volume: CEUtils.WeapSound);

                if (Projectile.ai[0] == 3)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, new Vector2(0, 8).RotatedByRandom(1), ModContent.ProjectileType<FractalLaser>(), Projectile.damage / 9, 0, Projectile.owner);
                    CEUtils.PlaySound("VoidAttack", 1, Projectile.Center, volume: CEUtils.WeapSound);
                }
            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TrueExcalibur, new ParticleOrchestraSettings
            {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
        }
        public bool spawnProj = true;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] == 3)
            {
                modifiers.FinalDamage *= 1.4f;
                modifiers.SourceDamage *= 1.4f;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            Texture2D tex = Projectile.GetTexture();
            Texture2D phantom = this.getTextureGlow();
            float texAlpha = 1;
            float phantomAlpha = 0;
            if (Projectile.ai[0] <= 1)
            {
                texAlpha = 1 - progress;
                phantomAlpha = progress;
            }
            if (Projectile.ai[0] == 2)
            {
                phantomAlpha = 0.5f;
                texAlpha = 1;
            }
            if (Projectile.ai[0] == 3)
            {
                phantomAlpha = 1;
                texAlpha = 0;
            }
            int dir = (int)(Projectile.ai[0] <= 1 ? Projectile.ai[0] : -1) * (Projectile.velocity.X > 0 ? -1 : 1);
            if (Projectile.ai[0] == 2)
            {
                dir = Math.Sign(Projectile.velocity.X);
            }
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            if (Projectile.ai[0] == 2)
            {
                origin = tex.Size() * 0.5f;
            }
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha * texAlpha, rot, origin, Projectile.scale * scale * 1.1f, effect);
            Main.EntitySpriteDraw(phantom, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, Color.White * alpha * phantomAlpha, rot, origin, Projectile.scale * scale * 1.1f, effect);

            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D bs = CEExtraAssets.SemiCircularSmear;
            Texture2D ss = CEExtraAssets.SlashSmear;

            Color color1 = new Color(20, 20, 255);
            Color color2 = new Color(102, 20, 255);

            float zScale = Projectile.ai[0] == 2 ? 0.5f : 1;

            float zAlpha = (float)(Math.Cos(CEUtils.GetRepeatedCosFromZeroToOne(counter / MaxUpdateTime, 3) * MathHelper.Pi - MathHelper.PiOver2));
            if (Projectile.ai[0] == 2)
            {
                zAlpha = 1;
            }
            Main.spriteBatch.Draw(bs, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, Color.Lerp(color1, color2, (float)counter / (Projectile.MaxUpdates * Projectile.GetOwner().itemTimeMax)) * zAlpha, Projectile.rotation + MathHelper.ToRadians(32) * -dir, bs.Size() / 2f, Projectile.scale * 1.9f * scale * zScale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(ss, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, Color.White * zAlpha, Projectile.rotation + MathHelper.ToRadians(32) * -dir, ss.Size() / 2f, Projectile.scale * 1.9f * scale * zScale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (152) * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (160) * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }

    }

    public class FractalLaser : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 80;

        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
        }
        public float widthAdd = 0.2f;
        public float width = 0;
        public float width2 = 1;
        public int length = 2000;
        public override void AI()
        {
            width += widthAdd;
            widthAdd *= 0.98f;
            width += (1 - width) * 0.08f;
            if (Projectile.timeLeft < 16)
            {
                width2 -= 1 / 16f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White;
            Texture2D t1 = CEExtraAssets.B1;
            Texture2D t2 = CEExtraAssets.T2;
            Texture2D te = CEExtraAssets.FLEND;
            float w = 32;
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);

            Main.spriteBatch.Draw(t1, Projectile.Center - Main.screenPosition, new Rectangle((int)(Main.GlobalTimeWrappedHourly * -900), 0, t1.Width, t1.Height), new Color(160, 160, 255), Projectile.rotation, t1.Size() / 2f, new Vector2((float)length / t1.Width, (w / (float)t1.Height) * width * width2), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(t2, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, t2.Size() / 2f, new Vector2((float)length / (float)t2.Width, w / (float)t2.Height * width * width2), SpriteEffects.None, 0);

            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearClamp);

            Main.spriteBatch.Draw(te, Projectile.Center + Projectile.rotation.ToRotationVector2() * length * 0.5f - Main.screenPosition, null, Color.LightBlue, Projectile.rotation, new Vector2(0, te.Height / 2f), new Vector2(0.6f, width * width2), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(te, Projectile.Center - Projectile.rotation.ToRotationVector2() * length * 0.5f - Main.screenPosition, null, Color.LightBlue, Projectile.rotation + MathHelper.Pi, new Vector2(0, te.Height / 2f), new Vector2(0.6f, width * width2), SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center + Projectile.rotation.ToRotationVector2() * length * 0.5f, Projectile.Center + Projectile.rotation.ToRotationVector2() * length * -0.5f, targetHitbox, (int)(40 * width * width2));
        }
    }
    public class VoidSlash : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 36;
        }
        public List<Vector2> points = new List<Vector2>();
        public int d = 0;
        public override void AI()
        {
            d++;
            if (d == 1)
            {
                Main.SetCameraLerp(0.12f, 25);
                Projectile.Center += Projectile.velocity;
            }
            if (d > 16)
            {
                Projectile.velocity *= 0;
            }
            else
            {
                if (Projectile.ai[0] == 1)
                {
                    int type = ModContent.ProjectileType<FinalFractalBlade>();
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy(MathHelper.PiOver2) * 0.1f, type, Projectile.damage, Projectile.knockBack, Projectile.owner);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy(-MathHelper.PiOver2) * 0.1f, type, Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                Vector2 o = (points.Count > 0 ? points[points.Count - 1] : Projectile.Center - Projectile.velocity);
                Vector2 nv = Projectile.Center + CEUtils.randomVec(4);
                for (float i = 0.1f; i <= 1; i += 0.1f)
                {
                    points.Add(Vector2.Lerp(o, nv, i));
                }
                Projectile.GetOwner().Center = Projectile.Center;
            }

        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (points.Count < 1)
            {
                return false;
            }
            for (int i = 1; i < points.Count; i++)
            {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, 30))
                {
                    return true;
                }
            }
            return false;
        }
        public List<Vector2> GetVPoints(Vector2 start, Vector2 end, int offset)
        {
            List<Vector2> rt = new List<Vector2>();
            for (float i = 0; i <= 1; i += 0.0025f)
            {
                rt.Add(Vector2.Lerp(start, end, i) + new Vector2(0, offset * (Projectile.timeLeft / 36f) * ((float)Math.Cos(i * MathHelper.TwoPi - MathHelper.Pi) + 1) * 0.5f).RotatedBy((end - start).ToRotation()));
            }
            return rt;
        }
        public static void DrawSlashPart(List<Vector2> l, List<Vector2> r, Texture2D tex, Color color, Color topColor)
        {
            List<ColoredVertex> vertex = new List<ColoredVertex>();

            for (int i = 0; i < l.Count; i++)
            {
                Color c = Color.Lerp(topColor, color, ((float)Math.Cos(i * MathHelper.TwoPi - MathHelper.Pi) + 1) * 0.5f);
                vertex.Add(new ColoredVertex(l[i] - Main.screenPosition,
                      new Vector3(i / (l.Count - 1f), 1, 1),
                    c));
                vertex.Add(new ColoredVertex(r[i] - Main.screenPosition,
                      new Vector3(i / (l.Count - 1f), 0, 1),
                      c));
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            gd.Textures[0] = tex;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertex.ToArray(), 0, vertex.Count - 2);

        }
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = points[0];
            Vector2 end = points[points.Count - 1];

            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearClamp);
            DrawSlashPart(GetVPoints(start, end, 100), GetVPoints(start, end, -100), CEExtraAssets.MegaStreakBacking2b, (Projectile.ai[0] == 0 ? new Color(180, 0, 255, 160) : new Color(240, 200, 255, 180)), (Projectile.ai[0] == 0 ? new Color(180, 0, 255, 160) : new Color(240, 200, 255, 180)));
            DrawSlashPart(GetVPoints(start, end, 26), GetVPoints(start, end, -26), CEUtils.pixelTex, new Color(180, 55, 235), new Color(255, 200, 255, 0));
            DrawSlashPart(GetVPoints(Vector2.Lerp(start, end, 0.2f), Vector2.Lerp(end, start, 0.2f), 26), GetVPoints(Vector2.Lerp(start, end, 0.1f), Vector2.Lerp(end, start, 0.1f), -26), CEUtils.pixelTex, Color.Black, Color.Black);
            return false;
        }

    }
}
