using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class WhipOfEvilKing : BaseWhipItem, IPriceFromRecipe, IGetFromStarterBag
    {
        public override int TagDamage => 2;
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
        }
        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<EvilKingWhipProjectile>(), 9, 4, 8f, 26);
            Item.rare = ItemRarityID.Yellow;
            Item.autoReuse = true;
            Item.width = 44;
            Item.height = 38;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float swingDirection = 0.6f + (0.4f * Main.rand.NextFloat());
            if (Main.rand.NextBool(3))
            {
                swingDirection *= -2.5f;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, swingDirection);
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AncientBoneDust))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_AncientBoneDust)
                .AddIngredient(ItemID.Silk, 8)
                .AddRecipeGroup(CERecipeGroups.evilBar, 5)
                .AddIngredient(ItemID.GoldCoin, 99)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.GoldenKey)
                .AddIngredient(ItemID.Silk, 10)
                .AddIngredient(ItemID.GoldCoin, 99)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public bool OwnAble(Player player, ref int count)
        {
            if (player.Entropy().drCrystals == null) return false;
            return player.Entropy().drCrystals[0];
        }
    }
    public class EvilKingWhipProjectile : BaseWhip
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.MaxUpdates = 4;
            segments = 32;
            rangeMult = 1;
        }
        public override string getTagEffectName => "EvilKingWhip";
        public override Color StringColor => new Color(24, 24, 24);
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            target.AddBuff(ModContent.BuffType<WhipOfEvilKingWhipDebuff>(), 240);
        }
        public override int handleHeight => 18;
        public override int segHeight => 16;
        public override int endHeight => 24;
        public override int segTypes => 1;
    }
    public class Spade : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 40;
            Projectile.width = Projectile.height = 30;
        }
        public override void AI()
        {
            Projectile.ai[0] = float.Lerp(Projectile.ai[0], 1, 0.3f);
            Projectile.ai[2] = float.Lerp(Projectile.ai[2], 1, 0.1f);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 16; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Demonite);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = tex.Size() / 2f;
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver2)
            {
                Vector2 offset = i.ToRotationVector2() * 4;
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + offset, null, Color.Black * Projectile.ai[2], 0, origin, Projectile.scale * Projectile.ai[0], SpriteEffects.None);
            }
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.ai[2], 0, origin, Projectile.scale * Projectile.ai[0], SpriteEffects.None);
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 30;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.timeLeft += 20;
            if (Projectile.timeLeft > 80)
                Projectile.timeLeft = 80;
            //AbyssalLine/Abyssal有的走EffectLoader RT合成,Configure只管常规参数
            var p = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, Color.White, 1);
            p.lx = 0.8f;
            p.xadd = 0.62f;
            p.spawnColor = Color.White;
            p.endColor = Color.Black;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 32);
            //第二道AbyssalLine endColor字段Configure前赋
            var p2 = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, Color.White, 1);
            p2.lx = 0.8f;
            p2.xadd = 0.62f;
            p2.spawnColor = Color.White;
            p2.endColor = Color.Black;
            p2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, MathHelper.PiOver2, 32);
            Projectile.ai[0] += 0.16f;
        }
    }
}
