using CalamityEntropy.Common;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class Godhead : ModItem
    {
        // 2026-08-31 平衡案重做:在玩家身边形成半径60格的隐形光环,
        // 光环内敌人持续受伤(固定40,0.25秒一跳,25穿甲)并被施加破晓减益。
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = CECal.RarityTurquoise(ModContent.RarityType<NihilityBlue>());
            Item.accessory = true;

        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().GodHeadVisual = !hideVisual;
            player.GetModPlayer<EModPlayer>().Godhead = true;
            if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[ModContent.ProjectileType<GodheadAura>()] < 1)
            {
                Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<GodheadAura>(), GodheadAura.AuraDamage, 0, player.whoAmI);
            }
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DivineGeode, CEID.Item_Bloodstone))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_DivineGeode, 3).
                AddIngredient(CEID.Item_Bloodstone, 5).
                AddIngredient(ItemID.Ectoplasm, 3).
                AddTile(TileID.LunarCraftingStation).
                Register();
                return;
            }
            CreateRecipe().
                AddIngredient(ItemID.Ectoplasm, 5).
                AddIngredient(ItemID.FragmentSolar, 3).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }

    /// <summary>神性的隐形伤害光环:固定伤害不吃加成,60格半径,0.25秒一跳。</summary>
    public class GodheadAura : ModProjectile
    {
        public const int AuraDamage = 40;
        public const float Radius = 60 * 16f;
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.ArmorPenetration = 25;
        }
        public override void AI()
        {
            Player owner = Projectile.GetOwner();
            if (owner == null || !owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            if (owner.Entropy().Godhead)
            {
                Projectile.timeLeft = 2;
            }
            Projectile.Center = owner.Center;
            // 伤害恒定,不吃任何加成
            Projectile.damage = AuraDamage;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.getDistance(Projectile.Center, targetHitbox.Center.ToVector2()) < Radius;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 60);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
