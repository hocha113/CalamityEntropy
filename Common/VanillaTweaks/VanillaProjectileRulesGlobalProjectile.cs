using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.VanillaTweaks
{
    /// <summary>天顶剑剑影对低防御目标无视防御</summary>
    public class VanillaProjectileRulesGlobalProjectile : GlobalProjectile
    {
        //目标当前防御低于此值时剑影无视防御,与提示键 ZenithIgnoreDefense 的数字一致
        public const int ZenithIgnoreDefenseBelow = 100;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) {
            return entity.type == ProjectileID.FinalFractal;
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers) {
            if (target.defense < ZenithIgnoreDefenseBelow) {
                modifiers.DefenseEffectiveness *= 0f;
            }
        }
    }
}
