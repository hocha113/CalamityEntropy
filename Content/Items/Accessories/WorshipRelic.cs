using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class WorshipRelic : ModItem
    {
        public static int ArrowDamage = 180;
        // 内置冷却 0.5 秒(rogue-weapons.md §三)
        public const int ArrowCooldown = 30;
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 新效果:命中概率天降圣光箭(潜行体系退役,原潜行字段写入移除)
            player.GetModPlayer<WorshipRelicPlayer>().equipped = true;
            player.Entropy().MaxBaitCharge += 1;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ScoriaBar, CEID.Item_SolarVeil))
            {
                CreateRecipe()
                .AddIngredient<ShadowPact>(1)
                .AddIngredient(CEID.Item_ScoriaBar, 6)
                .AddIngredient(CEID.Item_SolarVeil, 4)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<ShadowPact>()
                .AddIngredient(ItemID.SunStone)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    /// <summary>崇拜圣物触发器:命中时 1/4 概率天降圣光箭,内置冷却 0.5 秒。</summary>
    public class WorshipRelicPlayer : ModPlayer
    {
        public bool equipped;
        private int arrowCooldown;

        public override void ResetEffects()
        {
            equipped = false;
            if (arrowCooldown > 0)
                arrowCooldown--;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryCallArrow(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 圣光箭自身命中不再触发,避免连锁
            if (proj.ModProjectile is SolarArrow)
                return;
            TryCallArrow(target);
        }

        private void TryCallArrow(NPC target)
        {
            if (!equipped || arrowCooldown > 0 || Player.whoAmI != Main.myPlayer)
                return;
            if (!Main.rand.NextBool(4))
                return;
            arrowCooldown = WorshipRelic.ArrowCooldown;
            int damage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(WorshipRelic.ArrowDamage);
            Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-120, 120), -360);
            Vector2 vel = (target.Center - spawnPos).normalize() * 14f;
            Projectile.NewProjectile(Player.GetSource_FromThis(), spawnPos, vel, ModContent.ProjectileType<SolarArrow>(), damage, 1f, Player.whoAmI);
        }
    }
}
