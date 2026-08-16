using CalamityEntropy.Common;
using CalamityEntropy.Content.Projectiles;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class TheDeadCut : RogueWeapon
    {
        public override void SetDefaults()
        {
            Item.width = 98;
            Item.height = 88;
            Item.damage = 275;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = CalamityGlobalItem.RarityDarkBlueBuyPrice;
            Item.rare = ModContent.RarityType<CosmicPurple>();
            Item.shoot = ModContent.ProjectileType<TheDeadCutProjectile>();
            Item.shootSpeed = 16f;
            Item.DamageType = CEUtils.RogueDC;
            Item.ArmorPenetration = 50;
            Item.Entropy().tooltipStyle = 3;
            Item.Entropy().NameColor = new Color(110, 0, 140);
            Item.Entropy().stroke = true;
            Item.Entropy().strokeColor = new Color(200, 0, 255);
            Item.Entropy().HasCustomStrokeColor = true;
            Item.Entropy().HasCustomNameColor = true;
        }
        public override float StealthDamageMultiplier => 0.25f;
        public override float StealthVelocityMultiplier => 1f;
        public override float StealthKnockbackMultiplier => 1f;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool stealth = player.Calamity().StealthStrikeAvailable();
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (stealth)
            {
                p.ToProj().Calamity().stealthStrike = true;
                CEUtils.SyncProj(p);
            }
            return false;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Revelation>());
            recipe.AddIngredient<TwistingNether>(5);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }
    }
    public class TheDeadCutProjectile : ModProjectile
    {
        public override string Texture => base.Texture;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.width = Projectile.height = 70;
            Projectile.MaxUpdates = 4;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 300 * 4;
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
        public int OnHit { get { return (int)Projectile.ai[2] - 1; } set { Projectile.ai[2] = value + 1; } }
        public int HitCooldown = 0;

        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames && Projectile.Calamity().stealthStrike)
            {
                Projectile.localNPCHitCooldown = 4;
            }
            HitCooldown--;
            Player player = Projectile.GetOwner();
            int flyTime = 4 * 18;
            if (Counter < flyTime)
            {
                if (Counter > 4 * 9)
                {
                    Projectile.velocity *= 0.88f;
                }
                Size.Y = float.Lerp(Size.Y, 1, 0.024f);
            }
            if(Counter == flyTime)
            {
                vec1 = Projectile.Center;
                vec2 = Vector2.Lerp(player.Center, Projectile.Center, 0.5f) + (Projectile.Center - player.Center).RotatedBy(MathHelper.PiOver2).normalize() * Main.rand.NextFloat(200, 500) * (Main.rand.NextBool() ? 1 : -1);
            }
            if (OnHit < 0)
            {
                int BackTime = 4 * 12;
                if (Counter > flyTime)
                {
                    if (Projectile.Calamity().stealthStrike)
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
            }
            else
            {
                NPC npc = OnHit.ToNPC();
                if(!npc.active)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.ai[1]++;
                Projectile.velocity *= 0.88f;
                if (Projectile.Calamity().stealthStrike)
                {
                    Projectile.velocity += (npc.Center - Projectile.Center) * 0.006f;
                    Projectile.velocity += (npc.Center - Projectile.Center).normalize() * 1.6f;
                }
                else
                {
                    Projectile.velocity += (npc.Center - Projectile.Center) * 0.018f;
                }
                if (Projectile.ai[1] > 80)
                {
                    if (Projectile.Calamity().stealthStrike)
                    {
                    }
                    else
                    {
                        Projectile.Kill();
                    }
                }
                float decayFactor = 0.02f;
                float yo = 1.0f / (1.0f + decayFactor * Projectile.velocity.Length());
                Size = new Vector2(1f / yo, yo);
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
            return (Counter <= 4 * 22 || Projectile.Calamity().stealthStrike) && HitCooldown <= 0;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("DemonSwordImpact2", Main.rand.NextFloat(1.4f, 1.8f), target.Center, 8, 0.5f);
            SpawnVParticles(6, 2);
            OnHit = target.whoAmI;
            if (Projectile.Calamity().stealthStrike)
            {
                if (Projectile.numHits > 10)
                    Projectile.Kill();
                else
                {
                    Projectile.Center += CEUtils.randomRot().ToRotationVector2() * 500;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, (Projectile.Center - target.Center), ModContent.ProjectileType<TheDeadCutSlash>(), Projectile.damage, 0, Projectile.owner, 1, target.whoAmI);
                    HitCooldown = 12;
                    Projectile.velocity *= 0;
                }
            }
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void SpawnVParticles(int num = 1, float scale = 1)
        {
            float num2 = 360f / num;
            Color color1 = Color.White;
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
        public void DrawProj(Texture2D tex, Color color, Vector2 position, float rotation, float texRotation, Vector2 scale)
        {
            DrawProj(tex, color, position, rotation, texRotation, scale, BlendState.AlphaBlend);
        }
        public void DrawProj(Texture2D tex, Color color, Vector2 position, float rotation, float texRotation, Vector2 scale, BlendState bs)
        {
            Effect shader = CommonEffects.rotation;
            shader.Parameters["rad"].SetValue(texRotation);
            shader.Parameters["center"].SetValue(Vector2.One * 0.5f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(tex, position, null, color, rotation, tex.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                if (OnHit >= 0)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomRot().ToRotationVector2(), ModContent.ProjectileType<TheDeadCutSlash>(), Projectile.damage, 0, Projectile.owner);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Projectile.GetTexture();
            for (int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;
                DrawProj(texture, Color.White * 0.9f * p, oldPos[i] - Main.screenPosition, oldRot[i], oldTexRot[i], oldSize[i]);
            }
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4 * 0.5f)
            {
                DrawProj(texture, Color.White, Projectile.Center + i.ToRotationVector2() * 3 - Main.screenPosition, Projectile.rotation, texRot, Size, BlendState.Additive);
            }
            DrawProj(texture, Color.White, Projectile.Center - Main.screenPosition, Projectile.rotation, texRot, Size);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
    public class TheDeadCutSlash : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.timeLeft = 10;
            Projectile.localNPCHitCooldown = -1;
        }
        public float Length = 0;
        public float Num = 140f;
        public float Width = 1;
        public override void AI()
        {
            if(Projectile.Entropy().FirstFrames)
            {
                CEUtils.PlaySound("slice", Main.rand.NextFloat(1f, 1.1f), Projectile.Center);
            }
            if (Projectile.ai[0] > 0)
            {
                Projectile.velocity *= 0.86f;
            }
            Length += Num;
            Num *= 0.8f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Width = (Projectile.timeLeft / 12f);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[0] > 1)
                return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity, targetHitbox, 64);
            return CEUtils.LineThroughRect(Projectile.Center - Projectile.rotation.ToRotationVector2() * Length, Projectile.Center + Projectile.rotation.ToRotationVector2() * Length, targetHitbox, 64);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = CEUtils.getExtraTex("lightmask");
            if (Projectile.ai[0] > 0)
            {
                Vector2 c = Projectile.Center + Projectile.velocity / 2;
                float lg = Projectile.velocity.Length();
                Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearClamp);
                Main.spriteBatch.Draw(tex, c - Main.screenPosition, null, Color.Black, Projectile.rotation, tex.Size() * 0.5f, new Vector2(lg / 40f, Width * 0.6f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, c - Main.screenPosition, null, Color.Black, Projectile.rotation, tex.Size() * 0.5f, new Vector2(lg / 40f, Width * 0.6f), SpriteEffects.None, 0);
                Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearClamp);
                Main.spriteBatch.Draw(tex, c - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() * 0.5f, new Vector2(lg / 40f * 0.99f, Width * 0.2f), SpriteEffects.None, 0);
            }
            else
            {
                Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearClamp);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Black, Projectile.rotation, tex.Size() * 0.5f, new Vector2(Length / 40f, Width * 0.95f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Black, Projectile.rotation, tex.Size() * 0.5f, new Vector2(Length / 40f, Width * 0.95f), SpriteEffects.None, 0);
                Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearClamp);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() * 0.5f, new Vector2(Length / 40f * 0.99f, Width * 0.36f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() * 0.5f, new Vector2(Length / 40f * 0.16f, Width * 0.92f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() * 0.5f, new Vector2(Length / 40f * 0.16f, Width * 0.92f), SpriteEffects.None, 0);
            }
            return false;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
    }
}
