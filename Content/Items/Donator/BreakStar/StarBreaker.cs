using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Utilities;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.BreakStar
{
    public class StarBreaker : ModItem, IDevItem
    {
        public string DevName => "锯角";

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D tex = TextureAssets.Item[Type].Value;
            Vector2 position = Item.position - Main.screenPosition + tex.Size() / 2;
            Rectangle iFrame = tex.Frame();
            for (int i = 0; i < 16; i++)
                spriteBatch.Draw(tex, position + MathHelper.ToRadians(i * 60f).ToRotationVector2() * 2f, null, Color.Blue with { A = 0 }, rotation, tex.Size() / 2, scale, 0, 0f);

            spriteBatch.Draw(tex, position, iFrame, Color.White, rotation, tex.Size() / 2, scale, 0f, 0f);
            Lighting.AddLight(position, TorchID.Blue);
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<Nadir>()
                .AddIngredient<FadingRunestone>()
                .AddIngredient(ItemID.FragmentNebula, 4)
                .AddIngredient(ItemID.FragmentSolar, 4)
                .AddIngredient(ItemID.FragmentStardust, 4)
                .AddIngredient(ItemID.FragmentVortex, 4)
                .AddTile<AbyssalAltarTile>()
                .Register();
        }

        public override void SetDefaults()
        {
            Item.width = 256;
            Item.height = 256;
            Item.damage = 3600;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item1;
            Item.maxStack = 1;
            Item.value = CalamityGlobalItem.RarityCalamityRedBuyPrice;
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<StarBreakerHeld>();
            Item.shootSpeed = 42;
            Item.DamageType = DamageClass.Melee;
            Item.channel = true;
            Item.crit = 12;
        }

        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[Item.shoot] < 1)
            {
                var p = Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, (Main.MouseWorld - player.Center).normalize() * Item.shootSpeed, Item.shoot, player.GetWeaponDamage(Item), player.GetWeaponKnockback(Item), player.whoAmI).ToProj();
                p.originalDamage = Item.damage;

            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }
    }
    public class StarBreakerHeld : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(ModContent.GetInstance<TrueMeleeDamageClass>(), false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 60;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (AttackType == 1 && AttackCount2 == 2 && AttackTime <= 0.2f)
                return false;
            return CEUtils.LineThroughRect(Projectile.Center + Projectile.rotation.ToRotationVector2() * 480 * Projectile.scale * ((AttackType == 1 && AttackCount2 < 2) ? 1.06f : 1f), Projectile.Center, targetHitbox, 200);
        }
        public override void CutTiles()
        {
            if (Projectile.GetOwner().channel)
                Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 480 * Projectile.scale, 128, DelegateMethods.CutTiles);
        }
        public Rope rope = null;
        public int AttackCount = 0;
        public int AttackCount2 = 0;
        public int AttackType = 0;
        public float AttackTime = 0;
        public int AttackDelay = 0;
        public float RotP = 0;
        public float BaseScale = 0;
        public float num = 0;
        public float Atk2Counter = 0;
        public bool SndFlag = true;
        public override bool? CanHitNPC(NPC target)
        {
            return (Projectile.GetOwner().channel) ? null : false;
        }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if (player.HeldItem.ModItem is StarBreaker)
            {
                Projectile.damage = (int)player.GetTotalDamage(Projectile.DamageType).ApplyTo(Projectile.originalDamage);
                Projectile.CritChance = player.GetWeaponCrit(player.HeldItem);
            }
            player.direction = Math.Sign(Projectile.velocity.X);

            Projectile.StickToPlayer((player.channel || Atk2Counter > 0) ? 1 : 0.25f);

            Projectile.rotation += RotP;
            Projectile.Center = player.HandPosition.Value;
            player.direction = (Projectile.rotation.ToRotationVector2().X > 0 ? 1 : -1);
            player.itemRotation = (Projectile.velocity * player.direction).ToRotation();
            if (BaseScale == 0)
                BaseScale = Projectile.scale;

            if (Main.myPlayer == Projectile.owner)
            {
                if (Main.mouseLeft && !Main.LocalPlayer.mouseInterface)
                {
                    player.channel = true;
                    if (Atk2Counter <= 0)
                    {
                        AttackType = 0;
                    }
                }
                if (Main.mouseRight && !Main.LocalPlayer.mouseInterface)
                {
                    player.channel = true;
                    if (AttackDelay <= 0 && Atk2Counter <= 0)
                    {
                        PlaySound = false;
                        SndFlag = true;
                        AttackType = 1;
                        Atk2Counter = 1;
                        Projectile.ResetLocalNPCHitImmunity();
                    }
                }
            }
            if (!player.dead && player.HeldItem.ModItem is StarBreaker)
            {
                Projectile.timeLeft = 2;
            }
            if (AttackType == 0 && AttackCount % 4 == 3 && AttackDelay <= 0)
            {
                Projectile.scale = BaseScale * 1.5f;
            }
            else
            {
                Projectile.scale = BaseScale;
            }
            if (Atk2Counter <= 0)
                AttackType = 0;
            if (Atk2Counter > 0 || AttackTime > 0)
                player.channel = true;
            Projectile.Center += Projectile.rotation.ToRotationVector2() * -160 * Projectile.scale;
            if (player.channel)
            {
                int ItemTime = player.HeldItem.useTime;
                if (AttackType == 0)
                {
                    player.itemTime = player.itemAnimation = 3;
                    AttackDelay--;
                    if (AttackDelay < 0 || AttackTime > 0)
                    {
                        float add = player.GetTotalAttackSpeed(Projectile.DamageType) * (1f / ItemTime) * (AttackCount % 4 == 3 ? 2 : 3f);
                        if (AttackTime == 0)
                        {
                            for (int i = 0; i < 4; i++)
                                SoundEngine.PlaySound(SoundID.Item1 with { MaxInstances = 10 }, Projectile.Center);
                            if (!(AttackCount % 4 == 3))
                                RotP = Main.rand.NextFloat(-0.3f, 0.3f);
                            num = (RotP * -2f) / (1 / add);
                            Projectile.ResetLocalNPCHitImmunity();
                            PlaySound = false;
                            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center + Projectile.velocity.normalize() * 500 * Projectile.scale, player.velocity + Projectile.velocity.normalize() * -32 * Projectile.scale, new Color(80, 80, 255), new Vector2(0.3f, 1), Projectile.velocity.ToRotation(), 0.1f, 1f * Projectile.scale, 16));
                        }
                        RotP += num;
                        AttackTime += add;

                        if (AttackTime > 1)
                            AttackTime = 1;
                        Projectile.Center += Projectile.rotation.ToRotationVector2() * 150 * Projectile.scale * CEUtils.Parabola(AttackTime, 1);//(AttackTime > 0.5f ? (1 - (AttackTime - 0.5f) * 2) : AttackTime * 2);

                        if (AttackTime >= 1)
                        {
                            AttackTime = 0;
                            AttackCount++;
                            if (AttackCount % 4 == 3 || AttackCount % 4 == 0)
                                AttackDelay = 5;
                        }
                    }
                    else { RotP = 0; }
                }
                else
                {
                    float p = 1 - Atk2Counter;
                    AttackTime = p;
                    RotP = 0;
                    if (AttackCount2 == 2)
                    {
                        float ofs = 0;
                        if (p >= 0.2f && SndFlag)
                        {
                            CEUtils.PlaySound("HellkiteSwing" + Main.rand.Next(1, 3), Main.rand.NextFloat(1f, 1.2f), Projectile.Center, 12, 0.76f * CEUtils.WeapSound);
                            SndFlag = false;
                        }
                        if (p < 0.2f)
                        {
                            ofs = CEUtils.Parabola(p / 0.2f, -40);
                        }
                        else
                        {
                            ofs = CEUtils.Parabola((p - 0.2f) / 0.6f, 60);
                        }
                        if(p > 0.2f && p < 0.7f)
                        {
                            RotP = Main.rand.NextFloat(-0.12f, 0.12f);
                            player.Entropy().immune = 12;
                            player.position += Projectile.velocity.normalize() * 80;
                            player.velocity *= 0;
                            if(CEUtils.CheckSolidTile(player.getRect()))
                            {
                                player.position -= Projectile.velocity.normalize() * 80;
                            }
                        }
                        Projectile.position += Projectile.velocity.normalize() * ofs;
                        Atk2Counter -= player.GetTotalAttackSpeed(Projectile.DamageType) * (1f / ItemTime) * 0.5f;
                    }
                    else
                    {
                        if (Atk2Counter <= 0.7f && SndFlag)
                        {
                            CEUtils.PlaySound("HellkiteSwing" + Main.rand.Next(1, 3), Main.rand.NextFloat(1f, 1.2f), Projectile.Center, 12, 0.66f * CEUtils.WeapSound);
                            SndFlag = false;
                        }
                        Projectile.Center += Projectile.rotation.ToRotationVector2() * 120 * Projectile.scale;
                        Projectile.rotation = Projectile.velocity.ToRotation() + (CEUtils.GetRepeatedCosFromZeroToOne(p, 2) - 0.5f) * 4f * (AttackCount2 == 0 ? 1 : -1) * (Projectile.velocity.X > 0 ? -1 : 1);
                        Projectile.position += (Projectile.rotation).ToRotationVector2() * CEUtils.Parabola(p, 1) * 80;
                        Projectile.Center += Projectile.rotation.ToRotationVector2() * -120 * Projectile.scale;
                        Atk2Counter -= player.GetTotalAttackSpeed(Projectile.DamageType) * (1f / ItemTime) * 1f;
                    }
                    if(Atk2Counter <= 0)
                    {
                        AttackCount2++;
                        AttackDelay = 8;
                        if (AttackCount2 > 2)
                            AttackCount2 = 0;
                        Atk2Counter = 0;
                        AttackType = 0;
                    }
                    oldRots.Add(Projectile.rotation);
                    oldPos.Add(Projectile.Center);
                }
            }
            else
            {
                RotP = 0;
                AttackDelay = 0;
                AttackCount = 0;
                AttackTime = 0;
            }

            Vector2 ropePoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * 234 * Projectile.scale;
            if (rope == null)
            {
                rope = new Rope(ropePoint, 10, 11f * Projectile.scale, new Vector2(0, 0.5f), 0.2f, 64);
            }
            Vector2 lst = rope.Start;
            rope.gravity = (Vector2.Lerp(Vector2.UnitY, -Projectile.rotation.ToRotationVector2(), 0.56f)).RotatedBy((float)Math.Sin(Main.GameUpdateCount * 0.028f) * 0.3f) * 0.6f;
            for (float r = 0.5f; r <= 1; r += 0.5f)
            {
                rope.Start = Vector2.Lerp(lst, ropePoint, r);
                rope.Update();
            }
            var points = rope.GetPoints();
            odp.Clear();
            odp.Add(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                for (float j = 0.25f; j <= 1f; j += 0.25f)
                {
                    odp.Add(Vector2.Lerp(points[i - 1], points[i], j));
                }
            }

            if(oldRots.Count > 12)
            {
                if (oldRots.Count > 0)
                {
                    oldRots.RemoveAt(0);
                    oldPos.RemoveAt(0);
                }
            }
            if(AttackType == 0)
            {
                oldRots.Clear();
                oldPos.Clear();
            }
        }
        public List<Vector2> odp = new();
        public List<float> oldRots = new();
        public List<Vector2> oldPos = new();
        public bool PlaySound = false;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 80 + target.defense / 2;
            if (AttackType == 0 && AttackCount % 4 == 3)
            {
                modifiers.SetCrit();
                modifiers.SourceDamage *= 3;
                modifiers.ArmorPenetration += 200;
            }
            if(AttackType == 1)
            {
                if(AttackCount2 == 2)
                {
                    modifiers.SourceDamage *= 4;
                }
                else
                {

                    modifiers.SourceDamage *= 1.2f;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MarkedforDeath>(180);

            if(AttackType == 1)
            {
                if (AttackCount2 == 0)
                    Projectile.GetOwner().AddBuff(BuffID.ParryDamageBuff, 180);
                if (AttackCount2 == 1)
                    target.AddBuff<SoulDisorder>(180);
            }

            if (!PlaySound)
            {
                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Projectile.velocity.normalize() * -4, 3));
                PlaySound = true;
                CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(0.8f, 1.4f), target.Center);
            }
            for (int i = 0; i < 24; i++)
            {
                float sparkScale2 = Main.rand.NextFloat(1.4f, 2.4f);
                Vector2 sparkVelocity2 = Projectile.rotation.ToRotationVector2().RotatedByRandom(0.2f) * 64 * Main.rand.NextFloat(0.2f, 1);
                if (Main.rand.NextBool(3))
                {
                    AltSparkParticle spark = new AltSparkParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), false, 12, sparkScale2 * (1.4f), new Color(60, 60, 255));
                    GeneralParticleHandler.SpawnParticle(spark, false, CalamityMod.Enums.GeneralDrawLayer.AfterPlayers);
                }
                else
                {
                    LineParticle spark = new LineParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2, false, 8, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f), Main.rand.NextBool() ? Color.Blue : new Color(140, 140, 255));
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (odp != null)
            {
                float w = 7 * Projectile.scale;
                int xp = 0;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Lighting.GetColor((odp[0] / 16).ToPoint());
                ve.Add(new ColoredVertex(new Vector2(xp, 0) + odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * w,
                          new Vector3((float)0, 1, 1),
                          b));
                ve.Add(new ColoredVertex(new Vector2(xp, 0) + odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * w,
                      new Vector3((float)0, 0, 1),
                      b));
                for (int i = 1; i < odp.Count; i++)
                {
                    b = Lighting.GetColor((odp[i] / 16).ToPoint());
                    ve.Add(new ColoredVertex(new Vector2(xp, 0) + odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * w,
                          new Vector3((float)(i + 1) / odp.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(new Vector2(xp, 0) + odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * w,
                          new Vector3((float)(i + 1) / odp.Count, 0, 1),
                          b));

                }
                Texture2D texst = this.getTextureAlt("String");
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = texst;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            var tex = Projectile.GetTexture();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver4, new Vector2(32, tex.Height - 32), Projectile.scale, 0f, 0f);
            Texture2D arrow = CEUtils.getExtraTex("SpearArrow");
            Texture2D glow = CEUtils.getExtraTex("SpearArrowGlow");
            float arrowAlpha = CEUtils.Parabola(AttackTime, 1) - 0.6f;
            if (arrowAlpha < 0)
                arrowAlpha = 0;
            arrowAlpha /= 0.4f;
            arrowAlpha *= 0.7f;
            bool dflag = false;
            float ap = 1;
            Color applyAlpha(Color bc)
            {
                return new Color(bc.R, bc.G, bc.B, (int)(255 * arrowAlpha * (dflag ? ap : 1)));
            }

            for (int i = 0; i < 2; i++)
            {
                Main.spriteBatch.Draw(arrow, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 330 + CEUtils.randomPointInCircle(24), null, applyAlpha(Color.Blue), Projectile.rotation + Main.rand.NextFloat(-0.22f, 0.22f), arrow.Size() / 2f, new Vector2(2f, 1) * Projectile.scale * 0.4f, SpriteEffects.None, 0);
            }

            if(oldRots.Count > 0)
            {
                Vector2 oCenter = Projectile.Center;
                dflag = true;
                for(int i = 0; i < oldRots.Count; i++)
                {
                    Projectile.Center = oldPos[i];
                    ap = 0.4f * (i + 1f) / oldRots.Count;
                    float rotation = oldRots[i];
                    Main.spriteBatch.UseBlendState(BlendState.Additive);
                    Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + rotation.ToRotationVector2() * 360, null, new Color(16, 16, 255) * arrowAlpha * ap, rotation, arrow.Size() / 2f, new Vector2(1.8f, 1) * Projectile.scale * 0.6f * Projectile.scale, SpriteEffects.None, 0);
                    Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
                    Main.spriteBatch.Draw(arrow, Projectile.Center - Main.screenPosition + rotation.ToRotationVector2() * 360, null, applyAlpha(Color.Blue), rotation, arrow.Size() / 2f, new Vector2(1.8f, 1) * Projectile.scale * 0.6f * Projectile.scale, SpriteEffects.None, 0);
                    Main.spriteBatch.UseBlendState(BlendState.Additive);
                    Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + rotation.ToRotationVector2() * 360, null, Color.White * arrowAlpha * ap * 2, rotation, arrow.Size() / 2f, new Vector2(1.7f, 0.4f) * Projectile.scale * 0.6f * Projectile.scale, SpriteEffects.None, 0);
                    ;
                    Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + rotation.ToRotationVector2() * 440, null, Color.White * arrowAlpha * ap * 0.5f, rotation, arrow.Size() / 2f, new Vector2(1.8f, 1f) * Projectile.scale * 0.24f * Projectile.scale, SpriteEffects.None, 0);
                }
                dflag = false;
                Projectile.Center = oCenter;
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 360, null, new Color(16, 16, 255) * arrowAlpha, Projectile.rotation, arrow.Size() / 2f, new Vector2(1.8f, 1) * Projectile.scale * 0.6f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            Main.spriteBatch.Draw(arrow, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 360, null, applyAlpha(Color.Blue), Projectile.rotation, arrow.Size() / 2f, new Vector2(1.8f, 1) * Projectile.scale * 0.6f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 360, null, Color.White * arrowAlpha * 2, Projectile.rotation, arrow.Size() / 2f, new Vector2(1.7f, 0.4f) * Projectile.scale * 0.6f * Projectile.scale, SpriteEffects.None, 0);
            ;
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 440, null, Color.White * arrowAlpha * 0.5f, Projectile.rotation, arrow.Size() / 2f, new Vector2(1.8f, 1f) * Projectile.scale * 0.24f * Projectile.scale, SpriteEffects.None, 0);

            if(AttackType == 1 && AttackCount2 < 2)
            {
                int dir = AttackCount2 == 0 ? 1 : -1;
                dir *= Projectile.velocity.X > 0 ? 1 : -1;
                Texture2D smr = CEUtils.getExtraTex("CircularSmear");

                Effect shader = CommonEffects.colorLerp;
                Main.spriteBatch.End();
                shader.Parameters["color"].SetValue((new Color(240, 240, 255) * arrowAlpha).ToVector4());
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                shader.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(smr, Projectile.GetOwner().Center - Main.screenPosition, null, new Color(0, 0, 255) * arrowAlpha, Projectile.rotation + -2.7f * dir, smr.Size() * 0.5f, Projectile.scale * 6f, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
                Main.spriteBatch.Draw(smr, Projectile.GetOwner().Center - Main.screenPosition, null, new Color(0, 0, 255) * arrowAlpha, Projectile.rotation + -2.7f * dir, smr.Size() * 0.5f, Projectile.scale * 6f, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            List<Vector2> points = new();
            for (int i = AttackCount % 4 == 3 ? 100 : 250; i <= 450; i += AttackCount % 4 == 3 ? 50 : 25)
            {
                points.Add(Projectile.Center + Projectile.rotation.ToRotationVector2() * i * Projectile.scale + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * Main.rand.Next(-60, 61) * ((450 - i) / (AttackCount % 4 != 3 ? 200f : 350f)));
            }
            Main.spriteBatch.UseAdditiveClamp();
            CEUtils.DrawLinesBetter(points, Color.White * (arrowAlpha / 0.5f), 2 * Projectile.scale);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
