using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class ShadowPact : ModItem
    {
        public float Damage = 0.06f;
        public static int BaseDamage = 16;

        public override void SetDefaults() {
            Item.width = 42;
            Item.defense = 4;
            Item.height = 36;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            // 新效果:全伤害加成 + 对健康敌人的首次命中附加暗影爆(潜行体系退役)
            player.GetDamage(DamageClass.Generic) += Damage;
            player.GetModPlayer<ShadowPactPlayer>().equipped = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Replace("[A]", Damage.ToPercent());
        }
        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.ShadowScale, 6)
                .AddIngredient(ItemID.Book, 4)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe().AddIngredient(ItemID.TissueSample, 6)
                .AddIngredient(ItemID.Book, 4)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    /// <summary>暗影契约触发器:对生命不低于 90% 的敌人首次命中附加暗影爆。</summary>
    public class ShadowPactPlayer : ModPlayer
    {
        public bool equipped;

        public override void ResetEffects() {
            equipped = false;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
            TryShadowBurst(target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
            if (proj.ModProjectile is CommonExplotionFriendly)
                return;
            TryShadowBurst(target, damageDone);
        }

        private void TryShadowBurst(NPC target, int damageDone) {
            if (!equipped || Player.whoAmI != Main.myPlayer)
                return;
            var mark = target.GetGlobalNPC<ShadowPactMarkNPC>();
            if (mark.burstDone)
                return;
            // 以命中前生命判定"健康"目标
            if (target.life + damageDone < target.lifeMax * 0.9f)
                return;
            mark.burstDone = true;
            int damage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(ShadowPact.BaseDamage);
            CEUtils.SpawnExplotionFriendly(Player.GetSource_FromThis(), Player, target.Center, damage, 90, DamageClass.Generic);
            CEUtils.PlaySound("shadowKnife", Main.rand.NextFloat(0.9f, 1.1f), target.Center, 4, 0.6f);
            for (int i = 0; i < 14; i++) {
                PRTLoader.NewParticle<PRT_AltSpark>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2, 9), Color.Lerp(Color.DarkViolet, Color.MediumPurple, Main.rand.NextFloat()), Main.rand.NextFloat(0.7f, 1.2f)).Configure(false, Main.rand.Next(20, 30));
            }
        }
    }

    /// <summary>暗影契约的敌怪标记:同一敌怪只吃一次暗影爆。</summary>
    public class ShadowPactMarkNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool burstDone;
    }
}
