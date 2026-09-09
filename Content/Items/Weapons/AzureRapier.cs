using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzureRapier : ModItem, IDevItem
    {
        public string DevName => "Polaris";
        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 68;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 7;
            Item.useAnimation = 7;
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.DamageType = DamageClass.Melee;
            Item.damage = 10;
            Item.knockBack = 5;
            Item.crit = 6;
            Item.shoot = ModContent.ProjectileType<AzureRapierHeld>();
            Item.shootSpeed = 16;
            Item.value = Item.buyPrice(0, 2);
            Item.rare = ModContent.RarityType<Soulight>();
            Item.ArmorPenetration = 16;
        }
        public override bool AltFunctionUse(Player player)
        {
            return false;
        }
        public bool RMBLast = true;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return true;
        }
        public override void HoldItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                int type = ModContent.ProjectileType<AzureRapierBlock>();
                if (!RMBLast && Main.mouseRight && !Main.LocalPlayer.mouseInterface && player.ownedProjectileCounts[type] < 1)
                {
                    player.direction = Math.Sign(Main.MouseWorld.X - player.Center.X);
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, (Main.MouseWorld - player.MountedCenter).normalize() * 8, type, player.GetWeaponDamage(Item), 0, player.whoAmI);
                }
                RMBLast = Main.mouseRight;
            }
        }
        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddCalOrOwn(CEID.Item_PearlShard, ItemID.WhitePearl, 4)
                .AddCalOrOwn(CEID.Item_SeaPrism, ItemID.Coral, 6)
                .AddCalOrOwn(CEID.Item_PrismShard, ItemID.CrystalShard, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class AzureRapierHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzureRapier";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public int counter;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(1.2f, 1.6f), target.Center);
            // 原灾厄 Eutrophication 对 NPC 唯一效果是 +0.05 减速，与自研 GalvanicCorrosion 同值；
            // 原 RiptideDebuff 对 NPC 无机制效果（纯玩家减益），施加行随脱离灾厄删除
            if (Main.rand.NextBool(6))
                target.AddBuff<GalvanicCorrosion>(12 * 60);

            if (Main.rand.NextBool(6))
                target.AddBuff<ArmorCrunch>(12 * 60);

            if (Main.rand.NextBool(6))
                target.AddBuff<Crumbling>(10 * 60);

            if (Main.rand.NextBool(6))
                target.AddBuff(BuffID.Bleeding, 16 * 60);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        public override void AI()
        {
            if (Projectile.localAI[0]++ == 0)
            {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                Projectile.velocity = Projectile.velocity.RotatedByRandom(0.24f);
                CEUtils.PlaySound("powerwhip", Main.rand.NextFloat(2.4f, 2.8f), Projectile.Center, 12, 0.6f * CEUtils.WeapSound);
            }
            var player = Projectile.GetOwner();
            player.heldProj = Projectile.whoAmI;
            int MaxTime = (int)(player.itemTimeMax * 1.4f);
            if (MaxTime < 5)
                MaxTime = 5;
            counter++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            player.SetHandRot(Projectile.rotation, (Player.CompositeArmStretchAmount)(Main.rand.Next(0, 3)));
            Projectile.Center = player.GetFrontHandPositionImproved(player.compositeFrontArm);
            Projectile.Center += Projectile.rotation.ToRotationVector2() * (CEUtils.Parabola((float)counter / MaxTime, 22 * player.HeldItem.scale) - 16);
            Projectile.timeLeft = 4;
            if (counter >= MaxTime)
            {
                Projectile.timeLeft = 0;
                Projectile.Kill();
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 210 * Projectile.scale, targetHitbox, 24);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 212 * Projectile.scale, 16, DelegateMethods.CutTiles);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var player = Projectile.GetOwner();
            int MaxTime = (int)(player.itemTimeMax * 1.4f);
            if (MaxTime < 5)
                MaxTime = 5;
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = CEExtraAssets.SpearArrowGlow2;
            float alpha = 1 - ((float)counter / MaxTime);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4, new Vector2(4, tex.Height - 4), Projectile.scale, SpriteEffects.None);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 50, null, Color.Aqua * alpha, Projectile.rotation, new Vector2(0, glow.Height / 2), new Vector2(0.7f, 0.2f * alpha) * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 50, null, Color.White * alpha, Projectile.rotation, new Vector2(0, glow.Height / 2), new Vector2(0.7f, 0.1f * alpha) * Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public static void OnBlock(Player player, Vector2 targetPos, Vector2 targetVel)
        {
            int pjtype = ModContent.ProjectileType<AzureRapierBlockSlash>();
            player.AddCooldown(BlockingCooldown.ID, 600);

            if (!CECooldowns.HasCooldown("AzureBlock"))
            {
                CEUtils.PlaySound("metalhit", 1.4f, player.Center);
                CEUtils.PlaySound("SwordHit0", 1.5f, player.Center);
                CEUtils.PlaySound("metalhit", 1.4f, player.Center);
                CEUtils.PlaySound("SwordHit0", 1.5f, player.Center);
                player.velocity = targetVel - player.velocity;
                Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), targetPos, Vector2.Zero, pjtype, (int)(player.GetWeaponDamage(player.HeldItem) * 1.5f + 1), 2, player.whoAmI);
                CECooldowns.AddCooldown("AzureBlock", 30);
            }
        }
    }
    public class AzureRapierBlockSlash : ModProjectile
    {
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.type != NPCID.WallofFlesh)
                target.velocity *= 0.24f;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzureRapier";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.timeLeft = 60;
            Projectile.localNPCHitCooldown = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.width = 700;
            Projectile.height = 400;
            Projectile.ArmorPenetration += 60;
        }
        public int counter = 0;
        public Vector2 plrPos = Vector2.Zero;
        public override bool? CanHitNPC(NPC target)
        {
            return counter > 16 ? null : false;
        }
        public override void AI()
        {
            var player = Projectile.GetOwner();
            if (player.dead)
                Projectile.Kill();
            counter++;
            if (counter == 2)
            {
                player.Entropy().immune = 100;
            }
            if (counter == 16)
                plrPos = player.position;
            if (counter >= 16)
            {
                player.Entropy().DontDrawTime = 2;
                if (counter % 3 == 0)
                {
                    Vector2 spawnPos = Projectile.Center + new Vector2(Main.rand.Next(160, 240) * (Main.rand.NextBool() ? 1 : -1), Main.rand.Next(-140, 140));
                    //PRT_DOracleSlash Configure传NonPremultipliedBlend,跟Oracle系一致
                    var slash = PRTLoader.NewParticle<PRT_DOracleSlash>(spawnPos, Vector2.Zero, Color.Aqua, 300);
                    slash.centerColor = Color.White;
                    slash.widthMult = 0.8f;
                    slash.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, (Projectile.Center + new Vector2(0, spawnPos.Y - Projectile.Center.Y + Main.rand.NextFloat(-60, 60)) - spawnPos).ToRotation(), 16);
                    CEUtils.PlaySound("SwiftSlice", Main.rand.NextFloat(1.4f, 2f), Projectile.Center);
                }
                player.Entropy().noItemTime = 4;
                player.velocity *= 0;
                if (plrPos != Vector2.Zero)
                    player.position = plrPos;
            }

        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
    public class AzureRapierBlock : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzureRapier";
        //格挡闪光贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/Particles/ThinEndedLine")]
        internal static Asset<Texture2D> ThinEndedLineTex;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.timeLeft = 24;
        }
        public float TScale = 0;
        public float TSpeed = 0.5f;
        public float TAlpha = 1;
        public override void AI()
        {
            var player = Projectile.GetOwner();
            if (Projectile.localAI[0]++ == 0)
            {

                CEUtils.PlaySound("metalhit", 2.4f, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item27 with { Pitch = 1 }, Projectile.Center);
            }
            Projectile.Center = player.Center;
            player.SetHandRot(new Vector2(player.direction, 0.15f).ToRotation());
            Projectile.Center = player.GetDrawCenter() + new Vector2(player.direction * 9, -12);
            Projectile.rotation = new Vector2(-player.direction, -9).ToRotation();
            TScale += TSpeed;
            TAlpha *= 0.9f;
            TSpeed *= 0.9f;
            if (Projectile.localAI[0] == 1)
            {
                for (int i = 0; i < 6; i++)
                {
                    //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                    PRTLoader.NewParticle<PRT_AltSpark>(Projectile.Center, player.velocity + Projectile.rotation.ToRotationVector2().RotatedByRandom(0.1f) * Main.rand.NextFloat(16, 28), new Color(240, 240, 255), Main.rand.NextFloat(1, 1.4f)).Configure(false, 32);
                    PRTLoader.NewParticle<PRT_AltSpark>(Projectile.Center, player.velocity + Projectile.rotation.ToRotationVector2().RotatedByRandom(0.1f) * -Main.rand.NextFloat(16, 28), new Color(240, 240, 255), Main.rand.NextFloat(1, 1.4f)).Configure(false, 32);
                }
            }
            player.Entropy().AzureRapierBlock = 2;
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation = 3;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D glow = ThinEndedLineTex.Value;
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.Aqua * TAlpha * 2, Projectile.rotation + MathHelper.PiOver2, glow.Size() / 2f, new Vector2(1f, TScale), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.White * TAlpha * 2, Projectile.rotation + MathHelper.PiOver2, glow.Size() / 2f, new Vector2(0.75f, TScale), SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
    }
}
