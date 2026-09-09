using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Depletion
{
    public class Depletion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 62;
            Item.damage = 35;
            Item.noMelee = true;
            Item.useAnimation = Item.useTime = 4;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 0;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(2, 40);
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.shoot = ModContent.ProjectileType<DepletionHeld>();
            Item.shootSpeed = 16f;
            Item.mana = 5;
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
            if (CECal.CalChainReady(CEID.Item_UnholyEssence, CEID.Item_EffulgentFeather))
            {
                CreateRecipe()
                .AddIngredient<Malign.Malign>()
                .AddIngredient(CEID.Item_UnholyEssence, 8)
                .AddIngredient(CEID.Item_EffulgentFeather, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<Malign.Malign>()
                .AddIngredient(ItemID.FragmentNebula, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    public class DepletionHeld : ModProjectile
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
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Depletion/Depletion";
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
            if (player.HeldItem.ModItem is Depletion && !player.dead)
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
                                Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 130;
                                for (int i = 0; i < 16; i++)
                                {
                                    var d = Dust.NewDustDirect(pos, 0, 0, DustID.YellowTorch);
                                    d.position += CEUtils.randomPointInCircle(10);
                                    d.velocity = vel.normalize() * 10 * Main.rand.NextFloat() + Projectile.GetOwner().velocity;
                                    d.scale = 2;
                                    d.noGravity = true;
                                }
                                Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), pos, vel, ModContent.ProjectileType<DepletionBullet>(), player.GetWeaponDamage(player.HeldItem), player.GetWeaponKnockback(player.HeldItem), player.whoAmI);
                            }
                        }
                    }
                }
                if (MousePressed)
                {
                    if (ActiveProgress < 1)
                    {
                        ActiveProgress = float.Lerp(ActiveProgress, 1, 0.1f);
                        if (ActiveProgress > 0.98f)
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

            Vector2 top = Projectile.Center + Projectile.rotation.ToRotationVector2() * (48 + 12 * ActiveProgress) * Projectile.scale;

            Main.EntitySpriteDraw(tPart1, top - Main.screenPosition + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver4) * (ActiveProgress * 16 - 4), null, lightColor * ActiveProgress, Projectile.rotation + MathHelper.PiOver4 + -0.8f + 0.2f * ActiveProgress, new Vector2(0, tPart1.Height / 2), Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tPart2, top - Main.screenPosition + Projectile.rotation.ToRotationVector2().RotatedBy(-MathHelper.PiOver4) * (ActiveProgress * 16 - 4), null, lightColor * ActiveProgress, Projectile.rotation + MathHelper.PiOver4 + 0.8f + -0.2f * ActiveProgress, new Vector2(tPart2.Width / 2, tPart2.Height), Projectile.scale, SpriteEffects.None);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4, new Vector2(6, tex.Height - 6), Projectile.scale, SpriteEffects.None);

            DrawCircle(Projectile.Center + Projectile.rotation.ToRotationVector2() * 118, ActiveProgress, Projectile.rotation);

            CEUtils.DrawGlow(Projectile.Center + Projectile.rotation.ToRotationVector2() * 118 * Projectile.scale, Color.Yellow * 0.5f * (ActiveProgress * ActiveProgress * ActiveProgress), 1.2f);

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
            SetPoint(18, 32);
            CEUtils.DrawLines(points, new Color(255, 255, 160), 2f * active, 0);

            SetPoint(16, 3, Main.GlobalTimeWrappedHourly * 8);
            CEUtils.DrawLines(points, new Color(255, 255, 160), 2f * active, 0);
            SetPoint(5, 4, Main.GlobalTimeWrappedHourly * 8);
            CEUtils.DrawLines(points, new Color(255, 255, 160), 2f * active, 0);
            for (int i = 0; i < 3; i++)
            {
                float rot = (MathHelper.TwoPi / 3f) * i;
                rot += Main.GlobalTimeWrappedHourly * -8;
                SetPoint(6, 3, rot, rot.ToRotationVector2() * -15 * scale);
                CEUtils.DrawLines(points, new Color(255, 255, 160), 2f * active, 0);
            }

            Texture2D t = CEExtraAssets.Triangle;
            for (int i = 0; i < 3; i++)
            {
                float rot = (MathHelper.TwoPi / 3f) * i;
                rot += Main.GlobalTimeWrappedHourly * 6;
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 40 - Main.screenPosition, null, new Color(255, 255, 160), rot, new Vector2(48, t.Height / 2), new Vector2(0.05f * active, 0.03f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 40 - Main.screenPosition, null, new Color(255, 255, 160), rot + MathHelper.Pi, new Vector2(48, t.Height / 2), new Vector2(0.08f * active, 0.03f), SpriteEffects.None, 0);
            }
            for (int i = 0; i < 3; i++)
            {
                float rot = (MathHelper.TwoPi / 3f) * i + rotation;
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 22 - Main.screenPosition, null, new Color(255, 255, 160), rot + MathHelper.Pi, new Vector2(48, t.Height / 2), new Vector2(0.05f * active, 0.03f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 22 - Main.screenPosition, null, new Color(255, 255, 160), rot, new Vector2(48, t.Height / 2), new Vector2(0.08f * active, 0.03f), SpriteEffects.None, 0);
            }
            for (int i = 0; i < 3; i++)
            {
                float rot = (MathHelper.TwoPi / 3f) * i;
                rot += Main.GlobalTimeWrappedHourly * -6;
                Main.spriteBatch.Draw(t, center + rot.ToRotationVector2() * 18 - Main.screenPosition, null, new Color(255, 255, 160), rot, new Vector2(48, t.Height / 2), new Vector2(0.04f * active, 0.03f), SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
        }

    }
    public class DepletionLaser : ModProjectile
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
            Projectile.light = 2;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public float num = 1;
        public override bool? CanHitNPC(NPC target)
        {
            return Projectile.timeLeft > 42 ? null : false;
        }
        public override void AI()
        {
            num *= 0.94f;
            num -= 0.01f;
            if (num < 0)
                Projectile.Kill();
            Projectile.light = 2 * num;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.timeLeft <= 42)
                return false;
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity * num, targetHitbox, (int)(20 * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = CEExtraAssets.Triangle;
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            Color clr = Color.Lerp(Color.Yellow, Color.White, num * num * num * num);
            clr.A = (byte)(255 * num);
            if (Projectile.ai[0] == 0)
                Projectile.ai[0]++;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, clr, Projectile.velocity.ToRotation(), new Vector2(48, tex.Height / 2), new Vector2(Projectile.velocity.Length() * num / tex.Width * 2, 0.08f * Projectile.scale * Projectile.ai[0]), SpriteEffects.None);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, new Color(1f, 1f, 1f, num), Projectile.velocity.ToRotation(), new Vector2(48, tex.Height / 2), new Vector2(Projectile.velocity.Length() * num / tex.Width * 2, 0.08f * Projectile.scale * Projectile.ai[0]) * 0.7f, SpriteEffects.None);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override void OnKill(int timeLeft)
        {
        }
    }
    public class DepletionBullet : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, 1);
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
                //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Yellow, 0.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 5);
                CEUtils.PlaySound("malignShoot", Main.rand.NextFloat(0.6f, 0.8f), Projectile.Center, volume: 0.4f);
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (Projectile.timeLeft % 6 == 0)
                {
                    for (int i = 0; i < 2; i++)
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy((i == 0 ? -1 : 1) * 2.6f).normalize() * Main.rand.NextFloat(100, 140), ModContent.ProjectileType<DepletionLaser>(), Projectile.damage / 2, Projectile.knockBack / 4, Projectile.owner, 0.4f);
                }
            }
            if (trail == null)
            {
                //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 255), 0f);
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
                p.scaleStart = 0.03f * f;
                p.scaleEnd = 0.01f * f;
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 18);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CEUtils.DrawGlow(Projectile.Center, Color.Yellow, 0.7f * Projectile.scale);
            CEUtils.DrawGlow(Projectile.Center, Color.White, 0.6f * Projectile.scale);
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 60;
            target.Entropy().Decrease20DR = 80;
        }

        public override void OnKill(int timeLeft)
        {
            CEUtils.PlaySound("light_bolt", Main.rand.NextFloat(2.4f, 2.8f), Projectile.Center, 50, 0.4f);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Yellow, 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 2; i++)
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy(MathHelper.Pi * i - MathHelper.PiOver2).normalize() * Main.rand.NextFloat(100, 140), ModContent.ProjectileType<DepletionLaser>(), Projectile.damage / 2, Projectile.knockBack / 4, Projectile.owner);
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 700, (i) => i.ToNPC().Distance(Projectile.Center) > 80);
                Vector2 v = Projectile.velocity;
                if (target != null)
                {
                    v = (target.Center - Projectile.Center);
                }
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, v.normalize() * 600, ModContent.ProjectileType<DepletionLaser>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                for (int i = 0; i < 2; i++)
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, v.normalize().RotatedByRandom(0.6f) * Main.rand.NextFloat(240, 450), ModContent.ProjectileType<DepletionLaser>(), Projectile.damage / 4, Projectile.knockBack, Projectile.owner);

                for (int i = 0; i < 2; i++)
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy((i == 0 ? -1 : 1) * 2.6f).normalize() * Main.rand.NextFloat(100, 140), ModContent.ProjectileType<DepletionLaser>(), Projectile.damage / 2, Projectile.knockBack / 4, Projectile.owner, 0.4f);

            }
        }
    }
}
