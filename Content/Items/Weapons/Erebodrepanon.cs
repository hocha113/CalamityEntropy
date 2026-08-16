using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.Melee;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Erebodrepanon : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 10000;
            Item.crit = 20;
            Item.DamageType = ModContent.GetInstance<TrueMeleeDamageClass>();
            Item.width = 132;
            Item.height = 156;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 7;
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.value = CalamityGlobalItem.RarityCalamityRedBuyPrice;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<ErebodrepanonHeld>();
            Item.shootSpeed = 16f;
            Item.autoReuse = true;
        }
        public int UseCount = 0;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage * (UseCount == 2 ? 2 : 1), knockback, player.whoAmI, UseCount);
            UseCount++;
            if(UseCount >= 3)
                UseCount = 0;
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<DeathsAscension>()
                .AddIngredient(ModContent.ItemType<WyrmTooth>(), 12)
                .AddIngredient<FadingRunestone>()
                .AddTile<AbyssalAltarTile>()
                .Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class ErebodrepanonHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Erebodrepanon";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(ModContent.GetInstance<TrueMeleeDamageClass>(), false, -1);
            Projectile.width = Projectile.height = 8;
            Projectile.MaxUpdates = 10;
            Projectile.localNPCHitCooldown = -1;
        }
        public int dir => (Projectile.velocity.X > 0 ? 1 : -1) * (style == 0 ? -1 : 1);
        public int style => (int)Projectile.ai[0];
        public int counter = 0;
        public float rotateSpeed = 0;
        public float ScaleExtra = 1;
        public float tAlpha = 0;
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            float speed = Projectile.GetOwner().GetTotalAttackSpeed<TrueMeleeDamageClass>();
            Projectile.Center = player.MountedCenter;
            player.itemTime = player.itemAnimation = 3;
            player.heldProj = Projectile.whoAmI;
            if (counter == 0)
            {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                Projectile.MaxUpdates = (int)Math.Ceiling(Projectile.MaxUpdates * speed);
            }
            if (style < 3)
            {
                ScaleExtra = style == 2 ? 2.2f : 1.8f;
                if (counter == 0)
                {
                    Projectile.rotation = Projectile.velocity.ToRotation() + dir * -3.2f;
                }
                if(counter == 9 * 12)
                {
                    CEUtils.PlaySound("scytheswing", 1.6f - (style == 2 ? 0.22f : Main.rand.NextFloat(0, 0.1f)), Projectile.Center);
                    tAlpha = 1;
                }
                if (counter < 9 * 12)
                {
                    rotateSpeed *= 0.86f;
                    rotateSpeed -= dir * 0.0004f;
                }
                else
                {
                    if (counter % 4 == 0)
                    {
                        oldRot.Add(Projectile.rotation);
                        if (oldRot.Count > 20)
                            oldRot.RemoveAt(0);
                    }
                    if (style == 2)
                    {
                        if (counter < 11 * 12)
                        {
                            rotateSpeed += dir * 0.0061f;
                            rotateSpeed *= 0.93f;
                        }
                    }
                    else
                    {
                        if (counter < 10 * 12)
                        {
                            rotateSpeed *= 0.96f;
                            rotateSpeed += dir * 0.01f;
                        }
                    }
                    if (counter > (style == 2 ? 12 : 10) * 12)
                    {
                        if (counter > 18 * 12)
                        {
                            tAlpha *= 0.97f;
                            if (tAlpha < 0.02f)
                                tAlpha = 0;
                        }
                        if (counter < (style == 2 ? 40 : 25) * 12)
                        {
                            rotateSpeed *= 0.984f;
                        }
                        else
                        {
                            player.itemTime = player.itemAnimation = 0;
                            Projectile.Kill();
                        }
                    }
                }
                Projectile.rotation += rotateSpeed;
                player.SetHandRotWithDir(Projectile.rotation, Projectile.velocity.X > 0 ? 1 : -1);
            }
            
            counter++;
        }
        public List<float> oldRot = new List<float>();
        public override bool? CanDamage()
        {
            if(style <= 2)
            {
                return counter < 19 * 12 && counter > 8 * 12;
            }
            return null;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float l = Projectile.scale * ScaleExtra * 260;
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * l, targetHitbox, 160);
        }
        public override void CutTiles()
        {
            float l = Projectile.scale * ScaleExtra * 260;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * l, 160, DelegateMethods.CutTiles);
        }
        public override bool ShouldUpdatePosition()
        {
            return style > 2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if(style == 2)
            {
                if (Projectile.numHits < 3)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<ErebodrepanonMark>(), (int)(Projectile.damage * 0.4f), 0, Projectile.owner, 0, 0, target.whoAmI);
                }
            }
            target.AddBuff<LifeOppress>(5 * 60);
            CEUtils.PlaySound("WScytheHit", Main.rand.NextFloat(1.4f, 1.7f), target.Center);
            EParticle.spawnNew(new ShineParticle(), target.Center, Vector2.Zero, Color.Blue, 1.8f, 1, true, BlendState.Additive, 0, 10);
            EParticle.spawnNew(new ShineParticle(), target.Center, Vector2.Zero, Color.White, 1.2f, 1, true, BlendState.Additive, 0, 12);
            EParticle.spawnNew(new ShineParticle(), target.Center, Vector2.Zero, Color.White, 1.2f, 1, true, BlendState.Additive, 0, 12);
            for (int i = 0; i < 18; i++)
            {
                EParticle.NewParticle(new StarTrailParticle() { fadeOut = 6 }, target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(12, 44), Main.rand.NextBool() ? new Color(160, 160, 255) : new Color(255, 160, 80), Main.rand.NextFloat(1.6f, 2f), 1, true, BlendState.Additive, 0, 12);
                EParticle.NewParticle(new StarTrailParticle() { fadeOut = 16 }, target.Center, Projectile.velocity.RotatedByRandom(0.5f).normalize() * Main.rand.NextFloat(12, 64), Main.rand.NextBool() ? new Color(160, 160, 255) : new Color(255, 160, 80), Main.rand.NextFloat(1.6f, 2f), 1, true, BlendState.Additive, 0, 24);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D star = CEUtils.getExtraTex("Star2");
            Texture2D s3 = CEUtils.getExtraTex("CircularSmear");
            Texture2D s2 = CEUtils.getExtraTex("CircularSmearSmokey");
            float rotation = Projectile.rotation + 1.047f * dir; 
            SpriteEffects ef = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Vector2 origin = dir > 0 ? new Vector2(8, tex.Height - 8) : new Vector2(8, 8);
            float scale = Projectile.scale * ScaleExtra;
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);
            for(int i = 0; i < oldRot.Count; i++)
            {
                float a = i / (oldRot.Count - 1.0f);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + CEUtils.randomPointInCircle(2) * scale + Vector2.UnitY * Projectile.GetOwner().gfxOffY, null, (Main.rand.NextBool(15) ? Color.Orange : new Color(120, 120, 255, 255)) * 0.04f * i, oldRot[i] + +1.047f * dir, origin, scale * 1.3f, ef, 0);
            }
            for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4 * 0.5f)
            {
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + i.ToRotationVector2() * 6 + Vector2.UnitY * Projectile.GetOwner().gfxOffY, null, new Color(60, 60, 255, 255) * 0.7f, rotation, origin, scale * 1.3f, ef, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.GetOwner().gfxOffY, null, Color.White, rotation, origin, scale * 1.3f, ef, 0);
            Main.spriteBatch.UseAdditive();
            Effect shader = CommonEffects.colorLerp;
            Main.spriteBatch.End();
            shader.Parameters["color"].SetValue((new Color(200, 200, 255) * tAlpha * 0.75f).ToVector4());
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            float ssc = 3.4f;
            Main.spriteBatch.Draw(s2, Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.GetOwner().gfxOffY, null, new Color(0, 0, 255) * tAlpha * 0.6f, rotation + 0.2f * dir, s2.Size() * 0.5f, scale * ssc, ef, 0);
            Main.spriteBatch.Draw(s3, Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.GetOwner().gfxOffY, null, new Color(2, 2, 255) * tAlpha * 0.6f, rotation + 0.4f * dir, s3.Size() * 0.5f, scale * ssc * 0.98f, ef, 0);
            Vector2 spos = Projectile.Center + new Vector2(138, 104 * dir).RotatedBy(Projectile.rotation) * scale;
            Main.spriteBatch.Draw(star, spos - Main.screenPosition, null, Color.Blue * tAlpha, 0, star.Size() * 0.5f, new Vector2(1, 0.4f) * scale * 3.2f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, spos - Main.screenPosition, null, Color.White * tAlpha, 0, star.Size() * 0.5f, new Vector2(1, 0.4f) * scale * 2.8f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
    public class ErebodrepanonMark : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 450;
        }
        public override bool? CanDamage()
        {
            return Projectile.ai[1] > 0;
        }
        List<StarTrailParticle> trails = new List<StarTrailParticle>();
        public float tRot = 0f;
        public float tDist = 1f;
        public override void AI()
        {
            NPC n = ((int)Projectile.ai[2]).ToNPC();
            if (n != null && n.active)
            {
                Projectile.Center = n.Center;
            }
            if (Projectile.ai[0] == 0)
            {
                for(int i = 0; i < 4; i++)
                {
                    var t = new StarTrailParticle();
                    t.maxLength = 14;
                    EParticle.spawnNew(t, Projectile.Center + (i * MathHelper.PiOver2).ToRotationVector2() * 600, Vector2.Zero, new Color(80, 80, 255), 2.5f, 1, true, BlendState.Additive, 0, 12);
                    trails.Add(t);
                }
            }
                Projectile.ai[0]++;
            if (Projectile.ai[0] == 40)
            {
                CEUtils.PlaySound("bne_hit", 1, Projectile.Center);
                for (int i = 0; i < 24; i++)
                {
                    EParticle.NewParticle(new StarTrailParticle() { fadeOut = 16 }, Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(10, 44), Main.rand.NextBool() ? new Color(160, 160, 255) : new Color(255, 160, 80), Main.rand.NextFloat(1.6f, 2f), 1, true, BlendState.Additive, 0, 28);
                }
            }
            if (Projectile.ai[0] > 40)
                Projectile.ai[1] = 1;
            else
            {
                tRot += Projectile.ai[0] * 0.008f + 0.03f;
                tDist = (1 - Projectile.ai[0] / 46f);
                tDist = 1 - CEUtils.Parabola(0.5f + tDist * 0.5f, 1);
                for (int i = 0; i < 4;i++)
                {
                    var t = trails[i];
                    Vector2 op = t.Position;
                    t.Position = Projectile.Center + (i * MathHelper.PiOver2 + tRot).ToRotationVector2() * 600 * tDist;
                    t.Velocity = (t.Position - op).normalize() * 4;
                    t.Lifetime = 12;
                    t.Scale = 7f * Projectile.ai[0] / 40f;
                }
            }
            if (Projectile.ai[0] >= 49)
                Projectile.Kill();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.UseAdditive();
            Texture2D s = CEUtils.getExtraTex("StarChromatic");
            float scale = Projectile.ai[0] / 40f * 0.2f;
            float a = Projectile.ai[0] / 40f;
            if (Projectile.ai[0] >= 40)
                scale = Utils.Remap(Projectile.ai[0], 40, 49, 0.64f, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, Color.White * a, 0, s.Size() * 0.5f, scale * 0.7f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, new Color(60, 60, 255) * a, 0, s.Size() * 0.5f, scale * 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, Color.White * a, MathHelper.PiOver4, s.Size() * 0.5f, scale * 0.7f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, new Color(60, 60, 255) * a, MathHelper.PiOver4, s.Size() * 0.5f, scale * 1, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
