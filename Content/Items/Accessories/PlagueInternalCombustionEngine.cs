using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.CalamityRef;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class PlagueInternalCombustionEngine : ModItem
    {
        // 2026-08-31 平衡案重做:4防御;受击时使附近敌人遭受困惑与酸性毒液;
        // 手持武器攻击命中时召唤穿墙瘟疫能量(0.5秒内置CD,挂点在 EModPlayer.OnHitNPC/OnHurt)。
        public const int EnergyBaseDamage = 90;
        public const float HurtDebuffRadius = 480f;

        public override void SetDefaults()
        {
            Item.width = 98;
            Item.height = 60;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;
            Item.defense = 4;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().plagueEngine = true;
        }

        public override void AddRecipes()
        {
            // 3.33 没有配方,唯一来源是瘟疫使者歌利亚的宝藏袋 1/4(已在 EGlobalItem 的灾厄宝袋段补回)。
            // 这条是脱灾期的补偿合成,装灾厄时整条不注册,把获取方式还原成 3.33 的样子
            if (CERef.Has)
            {
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Hive)
                .AddIngredient(ItemID.VialofVenom, 20)
                .AddIngredient(ItemID.Nanites, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    /// <summary>瘟疫能量:穿墙追踪的绿色能量团。</summary>
    public class PlagueEnergy : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
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
                Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.9f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Min(Projectile.velocity.Length(), 14);
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.6f, 0.2f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.CursedTorch, -Projectile.velocity * 0.15f);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.1f, 1.6f);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 240);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.CursedTorch, CEUtils.randomVec(4));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.2f, 1.8f);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glow = CEExtraAssets.lightball;
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, new Color(120, 255, 120) * 0.7f, 0, glow.Size() / 2f, 0.16f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.White * 0.8f, 0, glow.Size() / 2f, 0.09f * Projectile.scale, SpriteEffects.None, 0);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
