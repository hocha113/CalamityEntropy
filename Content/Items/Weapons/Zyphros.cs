using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Zyphros : ModItem
    {
        public override bool RangedPrefix()
        {
            return true;
        }
        public override void SetDefaults()
        {
            Item.damage = 1000;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 36;
            Item.height = 110;
            // 2026-08-31 平衡案:降低攻速,改为一次5连发(原3帧速射)
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 6;
            Item.UseSound = null;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;
            Item.autoReuse = true;
            Item.ArmorPenetration = 100;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ModContent.RarityType<AbyssalBlue>();

        }

        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 10;

        public override Vector2? HoldoutOffset() => new Vector2(-42, -8);

        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return Main.rand.NextBool(12);
        }

        public override bool? UseItem(Player player)
        {
            CEUtils.PlaySound("zypshot" + Main.rand.Next(1, 3).ToString(), Main.rand.NextFloat(1f, 1.6f), player.Center, 3, 0.3f);
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 2026-08-31 平衡案:一次发射5发追踪的水晶箭矢;协同水晶改由命中生成(见 ZyphrosArrowGlobal)
            player.Entropy().itemTime = 30;
            for (int i = -2; i <= 2; i++)
            {
                int arrow = Projectile.NewProjectile(source, position, velocity.RotatedBy(i * 0.06f).RotatedByRandom(MathHelper.ToRadians(2)), type, damage, knockback, player.whoAmI);
                arrow.ToProj().Entropy().zypArrow = true;
                arrow.ToProj().ArmorPenetration += 30;
                CEUtils.SyncProj(arrow);
            }
            return false;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Drataliornus))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_Drataliornus, 1)
                .AddIngredient(ModContent.ItemType<WyrmTooth>(), 14)
                .AddIngredient(ModContent.ItemType<FadingRunestone>())
                .AddTile(ModContent.TileType<AbyssalAltarTile>())
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Phantasm)
                .AddIngredient(ModContent.ItemType<WyrmTooth>(), 12)
                .AddIngredient(ModContent.ItemType<FadingRunestone>())
                .AddTile(ModContent.TileType<AbyssalAltarTile>())
                .Register();
        }

        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 76f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(28, 0);
            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
    }

    /// <summary>
    /// 逝怨晶辉箭矢命中派生(2026-08-31 平衡案):
    /// 命中同时发射3发追踪穿墙幻影箭(30%倍率),并生成协同攻击的水晶(场上上限6枚)。
    /// </summary>
    public class ZyphrosArrowGlobal : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!projectile.Entropy().zypArrow || projectile.owner != Main.myPlayer)
                return;
            for (int i = 0; i < 3; i++)
            {
                int p = Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center,
                    (projectile.velocity.SafeNormalize(Vector2.UnitX) * 10).RotatedByRandom(1.2f),
                    ModContent.ProjectileType<ZyphrosPhantomArrow>(), (int)(projectile.damage * 0.3f), projectile.knockBack * 0.5f, projectile.owner, target.whoAmI);
                CEUtils.SyncProj(p);
            }
            Player owner = projectile.GetOwner();
            if (owner != null && owner.ownedProjectileCounts[ModContent.ProjectileType<ZyphrosCrystal>()] < 6 && Main.rand.NextBool(3))
            {
                int c = Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<ZyphrosCrystal>(), projectile.damage, projectile.knockBack, projectile.owner, Main.rand.Next(1, 6));
                CEUtils.SyncProj(c);
            }
        }
    }

    /// <summary>追踪穿墙幻影箭:锁定被命中的目标,幽蓝残影。</summary>
    public class ZyphrosPhantomArrow : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
        }
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
        }
        public override void AI()
        {
            NPC target = null;
            int locked = (int)Projectile.ai[0];
            if (locked >= 0 && locked < Main.maxNPCs && Main.npc[locked].active && Main.npc[locked].CanBeChasedBy(Projectile))
            {
                target = Main.npc[locked];
            }
            else
            {
                target = Projectile.FindTargetWithinRange(900, false);
            }
            if (target != null)
            {
                Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 1.4f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Min(Projectile.velocity.Length(), 18);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard, -Projectile.velocity * 0.1f);
                d.noGravity = true;
                d.scale = 0.9f;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = (i == 0 ? Projectile.Center : Projectile.oldPos[i] + Projectile.Size / 2f);
                Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(120, 160, 255) * 0.55f * fade, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            }
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
