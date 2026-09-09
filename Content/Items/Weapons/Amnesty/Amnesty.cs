using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Amnesty
{
    public class Amnesty : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 62;
            Item.damage = 115;
            Item.noMelee = true;
            Item.useAnimation = Item.useTime = 4;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 0;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(2, 0);
            Item.rare = CECal.RarityCosmicPurple(ModContent.RarityType<AbyssalBlue>());
            Item.shoot = ModContent.ProjectileType<AmnestyHeld>();
            Item.shootSpeed = 16f;
            Item.mana = 4;
            Item.DamageType = DamageClass.Magic;
            Item.channel = true;
            Item.useTurn = true;
            Item.noUseGraphic = true;
        }
        public override bool MagicPrefix()
        {
            return true;
        }
        public override void HoldItem(Player player)
        {
            player.CheckAndSpawnHeldProj(Item.shoot);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_HyperdeathRiftScepter, CEID.Item_AscendantSpiritEssence, CEID.Item_CosmiliteBar, CEID.Tile_CosmicAnvil))
            {
                CreateRecipe()
                .AddIngredient<Depletion.Depletion>()
                .AddIngredient(CEID.Item_HyperdeathRiftScepter)
                .AddIngredient(CEID.Item_AscendantSpiritEssence, 2)
                .AddIngredient(CEID.Item_CosmiliteBar, 8)
                .AddTile(CEID.Tile_CosmicAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Depletion.Depletion>()
                .AddIngredient<ChaoticPiece>(10)
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    public class AmnestyHeld : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Amnesty/Amnesty";
        public Texture2D tPart1 => this.getTextureAlt("P1");
        public Texture2D tPart2 => this.getTextureAlt("P2");
        public float ActiveProgress = 0;
        public bool MousePressed = false;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(MousePressed);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            MousePressed = reader.ReadBoolean();
        }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            Projectile.ai[1]--;
            if (player.HeldItem.ModItem is Amnesty && !player.dead)
            {
                Projectile.timeLeft = 2;
                Projectile.StickToPlayer();
                player.SetHandRot(Projectile.rotation);
                if (Main.myPlayer == Projectile.owner)
                {
                    if ((!player.mouseInterface && Main.mouseLeft) != MousePressed)
                    {
                        CEUtils.SyncProj(Projectile.whoAmI);
                    }
                    MousePressed = !player.mouseInterface && Main.mouseLeft;
                    if (MousePressed && ActiveProgress > 0.95f)
                    {
                        int cMana = int.Max(1, (int)(player.HeldItem.mana * player.manaCost));
                        player.channel = true;
                        if (player.manaRegenDelay < 16 && player.CheckMana(cMana, false))
                            player.manaRegenDelay = 16;
                        if (Projectile.ai[1] <= 0)
                        {
                            Projectile.ai[1] = player.HeldItem.useTime;
                            if (player.CheckMana(cMana, true))
                            {
                                PlayerLoader.OnConsumeMana(player, player.HeldItem, cMana);
                                Vector2 vel = Projectile.velocity.RotatedByRandom(0.6f) * 2;
                                Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 160;
                                for (int i = 0; i < 16; i++)
                                {
                                    var d = Dust.NewDustDirect(pos, 0, 0, DustID.BlueTorch);
                                    d.position += CEUtils.randomPointInCircle(10);
                                    d.velocity = vel.normalize() * 10 * Main.rand.NextFloat() + Projectile.GetOwner().velocity;
                                    d.scale = 2;
                                    d.noGravity = true;
                                }
                                Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), pos, vel, ModContent.ProjectileType<AmnestyBullet>(), player.GetWeaponDamage(player.HeldItem), player.GetWeaponKnockback(player.HeldItem), player.whoAmI);
                            }
                        }
                    }
                }
                if (MousePressed)
                {
                    if (ActiveProgress < 1)
                    {
                        ActiveProgress = float.Lerp(ActiveProgress, 1, 0.1f);
                        if (ActiveProgress > 0.992f)
                            ActiveProgress = 1;
                    }

                }
                else
                {
                    if (ActiveProgress > 0)
                    {
                        ActiveProgress = float.Lerp(ActiveProgress, 0, 0.1f);
                        if (ActiveProgress < 0.02f)
                            ActiveProgress = 0;
                    }
                }
                if (MousePressed || ActiveProgress > 0.3)
                {
                    player.itemTime = player.itemAnimation = 3;
                }
            }
            else
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, ActiveProgress);
            Texture2D tex = Projectile.GetTexture();

            Vector2 top = Projectile.Center + Projectile.rotation.ToRotationVector2() * (116 + -32 * ActiveProgress) * Projectile.scale;

            Main.EntitySpriteDraw(tPart1, top - Main.screenPosition + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver4) * (ActiveProgress * 8), null, new Color(200, 200, 255) * ActiveProgress * 0.7f, Projectile.rotation + MathHelper.PiOver4 + 1.8f + -2f * ActiveProgress, new Vector2(0, tPart1.Height / 2), Projectile.scale * ActiveProgress, SpriteEffects.None);
            Main.EntitySpriteDraw(tPart2, top - Main.screenPosition + Projectile.rotation.ToRotationVector2().RotatedBy(-MathHelper.PiOver4) * (ActiveProgress * 8), null, new Color(200, 200, 255) * ActiveProgress * 0.7f, Projectile.rotation + MathHelper.PiOver4 + -1.8f + 2f * ActiveProgress, new Vector2(tPart2.Width / 2, tPart2.Height), Projectile.scale * ActiveProgress, SpriteEffects.None);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4, new Vector2(6, tex.Height - 6), Projectile.scale, SpriteEffects.None);

            DrawCircle(Projectile.Center + Projectile.rotation.ToRotationVector2() * 138, ActiveProgress, Projectile.rotation);

            CEUtils.DrawGlow(Projectile.Center + Projectile.rotation.ToRotationVector2() * 138 * Projectile.scale, new Color(80, 80, 255) * 0.5f * (ActiveProgress * ActiveProgress * ActiveProgress), 1.2f);

            return false;
        }
        public static void DrawCircle(Vector2 center, float active, float rotation, float scale = 1)
        {
            //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            List<Vector2> points = new();
            void SetPoint(float r, int step, float rot = 0, Vector2 c = default)
            {
                points.Clear();
                for (int i = 0; i <= step; i++)
                {
                    points.Add(center + c + Vector2.UnitX.RotatedBy((MathHelper.TwoPi / step) * i + rot) * r * scale);
                }
            }
            if (center == default)
                center = Vector2.Zero;
            SetPoint(24, 6, Main.GlobalTimeWrappedHourly * -10);
            CEUtils.DrawLines(points, new Color(200, 200, 255), 2f * active, 0);

            SetPoint(20, 3, Main.GlobalTimeWrappedHourly * 8);
            CEUtils.DrawLines(points, new Color(220, 220, 255), 2f * active, 0);
            SetPoint(8, 4, Main.GlobalTimeWrappedHourly * 8);
            CEUtils.DrawLines(points, new Color(230, 230, 255), 2f * active, 0);
            for (int i = 0; i < 6; i++)
            {
                float rot = (MathHelper.TwoPi / 6f) * i;
                rot += Main.GlobalTimeWrappedHourly * -10 + MathHelper.TwoPi / 12f;
                SetPoint(4, 3, rot, rot.ToRotationVector2() * -19 * scale);
                CEUtils.DrawLines(points, new Color(200, 200, 255), 2f * active, 0);
            }
            Texture2D t = CEExtraAssets.Triangle;
            points.Clear();
            for (int i = 0; i <= 6; i++)
            {
                float rot = (MathHelper.TwoPi / 6f) * i;
                rot += Main.GlobalTimeWrappedHourly * 2f;
                float d = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5);
                d = 1 + d * 0.1f * (i % 2 == 0 ? -1 : 1);
                points.Add(center + rot.ToRotationVector2() * (40 * d + 19 * (i % 2 == 1 ? -1 : 1)) * scale);
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 40 * d * scale - Main.screenPosition, null, new Color(200, 200, 255), rot + (i % 2 == 1 ? 0 : MathHelper.Pi), new Vector2(50, t.Height / 2), new Vector2(0.03f * active, 0.05f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 40 * d * scale - Main.screenPosition, null, new Color(200, 200, 255), rot + (i % 2 == 0 ? 0 : MathHelper.Pi), new Vector2(50, t.Height / 2), new Vector2(0.1f * active, 0.05f), SpriteEffects.None, 0);
            }
            CEUtils.DrawLines(points, new Color(200, 200, 255), 2 * active, 0);
            for (int i = 0; i < 3; i++)
            {
                float rot = (MathHelper.TwoPi / 3f) * i + rotation + MathHelper.PiOver2;
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 14 - Main.screenPosition, null, new Color(200, 200, 255), rot + MathHelper.Pi, new Vector2(50, t.Height / 2), new Vector2(0.02f * active, 0.015f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 14 - Main.screenPosition, null, new Color(200, 200, 255), rot, new Vector2(50, t.Height / 2), new Vector2(0.04f * active, 0.015f), SpriteEffects.None, 0);
            }
            for (int i = 0; i < 5; i++)
            {
                float rot = (MathHelper.TwoPi / 5f) * i;
                rot += Main.GlobalTimeWrappedHourly * -10;
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 18 - Main.screenPosition, null, new Color(200, 200, 255), rot, new Vector2(50, t.Height / 2), new Vector2(0.04f * active, 0.03f), SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
        }

    }
    public class AmnestyLaser : ModProjectile
    {
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 50;
            target.Entropy().Decrease20DR = 80;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.width = Projectile.height = 16;
            Projectile.timeLeft = 46;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public float num = 1;
        public bool flag = true;
        public override void AI()
        {
            if (flag)
            {
                flag = false;
                points.Add(Projectile.Center);
                Vector2 end = Projectile.Center + Projectile.velocity;
                for (float i = 0.05f; i < 1; i += 0.05f)
                {
                    float n = CEUtils.Parabola(i, 16);
                    points.Add(Vector2.Lerp(Projectile.Center, end, i) + CEUtils.randomPointInCircle(n));
                }
                points.Add(end);
            }
            num *= 0.9f;
            num -= 0.01f;
            if (num < 0)
                Projectile.Kill();
        }
        public List<Vector2> points = new List<Vector2>();
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity, targetHitbox, (int)(4 * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CEUtils.DrawLines(points, Color.Lerp(new Color(180, 180, 255), Color.White, num * num * num), 10 * num);
            CEUtils.DrawLines(points, Color.Lerp(new Color(222, 222, 255), Color.White, num * num * num), 4 * num);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
        }
    }
    public class AmnestyMiniLaser : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 30 * 6;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 6;
        }
        public override void AI()
        {
            for (float i = 0.8f; i >= 0; i -= 0.2f)
            {
                oldPos.Add(Projectile.Center - Projectile.velocity * i);
                if (oldPos.Count > 64)
                {
                    oldPos.RemoveAt(0);
                }
            }
            if (Projectile.localAI[1]++ > 10)
                if (Projectile.HomingToNPCNearby(0.2f, 0.99f, 1200))
                {
                    if (Projectile.timeLeft < 15 * 6)
                        Projectile.timeLeft = 15 * 6;
                    Projectile.timeLeft++;
                }
            Projectile.Opacity = float.Min(1, Projectile.timeLeft / (15f * 6));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float scale = 0.32f * Projectile.scale;
            for (int i = 0; i < oldPos.Count; i++)
            {
                float c = (i + 1f) / oldPos.Count;
                DrawEnergyBall(oldPos[i], scale * c, Projectile.Opacity * c);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public static void DrawEnergyBall(Vector2 pos, float size, float alpha)
        {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(200, 200, 255) * alpha, 0, tex.Size() * 0.5f, size * 0.24f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(55, 55, 255) * alpha, 0, tex.Size() * 0.5f, size * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
        }
    }
    public class AmnestyBullet : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, 6);
            Projectile.width = Projectile.height = 16;
            Projectile.timeLeft = 30;
            Projectile.light = 1;
        }
        public float f = 0;
        public PRT_TrailParticle trail;
        public override void AI()
        {
            if (Projectile.timeLeft < 10)
                Projectile.tileCollide = true;
            if (Projectile.localAI[2]++ == 0)
            {
                //EParticle→PRT,批量Smoke的timeleftmax/Lifetime字段直赋对齐旧初始化器
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(80, 80, 255), 0.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 5);
                CEUtils.PlaySound("malignShoot", Main.rand.NextFloat(1.4f, 1.8f), Projectile.Center, volume: 0.4f);
            }
            if (Projectile.timeLeft == 27 && Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 2; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedByRandom(1).normalize() * 4, ModContent.ProjectileType<AmnestyMiniLaser>(), Projectile.damage / 2, 4, Projectile.owner);
                }
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (Projectile.timeLeft % 4 == 0 && Main.rand.NextBool(8))
                {
                    List<Projectile> lp = new();
                    foreach (Projectile p in Main.ActiveProjectiles)
                    {
                        if (p.type == Projectile.type && p.owner == Projectile.owner && p.whoAmI != Projectile.whoAmI)
                        {
                            lp.Add(p);
                        }
                    }
                    if (lp.Count > 0)
                    {
                        Projectile t = lp[Main.rand.Next(lp.Count)];
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (t.Center - Projectile.Center), ModContent.ProjectileType<AmnestyLaser>(), Projectile.damage / 2, 4, Projectile.owner);
                    }
                }
            }
            if (trail == null)
            {
                //轨迹字段设完再Configure,PRTDrawMode只能走Configure不能塞SetProperty
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 255), Projectile.scale);
                trail.maxLength = 10;
                trail.SameAlpha = true;
                trail.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);
            }

            trail.AddPoint(Projectile.Center + Projectile.velocity);
            trail.Lifetime = 13;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft < 24)
            {
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 600);
                if (target != null)
                {
                    Projectile.velocity = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), 0.12f, true).ToRotationVector2() * Projectile.velocity.Length();
                }
            }

            for (float i = 0; i < 1; i += 0.05f)
            {
                if (f < 1)
                    f += 0.01f;
                var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.velocity * i, Vector2.Zero, Color.White, 0.02f);
                p.endColor = Color.Yellow;
                p.colorTrans = true;
                p.Lifetime = 18;
                p.timeleftmax = 18;
                p.scaleStart = 0.015f * f;
                p.scaleEnd = 0.01f * f;
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 18);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CEUtils.DrawGlow(Projectile.Center, new Color(140, 140, 255), 1.2f);
            float r = Main.GlobalTimeWrappedHourly * 12;
            float num1 = 8;
            float num2 = 6;
            CEUtils.drawLine(Projectile.Center + r.ToRotationVector2() * num1 * Projectile.scale, Projectile.Center - r.ToRotationVector2() * num1 * Projectile.scale, new Color(230, 220, 255), num2 * Projectile.scale);
            CEUtils.drawLine(Projectile.Center + r.ToRotationVector2() * num1 * Projectile.scale * 1.5f, Projectile.Center - r.ToRotationVector2() * num1 * Projectile.scale * 1.5f, new Color(190, 160, 255) * 0.5f, num2 * Projectile.scale * 2f);

            r += MathHelper.PiOver2;
            CEUtils.drawLine(Projectile.Center + r.ToRotationVector2() * num1 * Projectile.scale, Projectile.Center - r.ToRotationVector2() * num1 * Projectile.scale, new Color(230, 220, 255), num2 * Projectile.scale);
            CEUtils.drawLine(Projectile.Center + r.ToRotationVector2() * num1 * Projectile.scale * 1.5f, Projectile.Center - r.ToRotationVector2() * num1 * Projectile.scale * 1.5f, new Color(190, 160, 255) * 0.5f, num2 * Projectile.scale * 2f);
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 60;
            target.Entropy().Decrease20DR = 80;
        }
        public override void OnKill(int timeLeft)
        {
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 900);
            if (Main.myPlayer == Projectile.owner && target != null)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (target.Center - Projectile.Center), ModContent.ProjectileType<AmnestyLaser>(), Projectile.damage, 5, Projectile.owner);
            }
            //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(80, 80, 255), 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);

        }
    }

}
