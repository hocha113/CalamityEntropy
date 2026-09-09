using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class Vitalfeather : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DragonWhipDebuff.TagDamage);

        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<VitalfeatherProjectile>(), 140, 2, 4, 42);
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.autoReuse = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

            return false;
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }

    /// <summary>2026-08-31 平衡案:沐生之羽改为月亮领主掉落(25%)。</summary>
    public class VitalfeatherDropGNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (!CERef.Has && npc.type == NPCID.MoonLordCore)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Vitalfeather>(), 4));
            }
        }
    }
}
