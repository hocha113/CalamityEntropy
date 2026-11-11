using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs.VoidSlime
{
    /// <summary>
    /// 宇宙空间:虚空史莱姆的引力场展开期间,场内玩家飞行时间不耗,吸血类治疗减半
    /// 由 Boss 在每个客户端给本地玩家逐帧续上,离场半秒后消失
    /// </summary>
    public class CosmicSpace : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.wingTime = player.wingTimeMax;
            player.rocketTime = player.rocketTimeMax;
        }

        /// <summary>Player.Heal 的钩子和原版吸血弹幕都走这里折半,至少回 1 点</summary>
        public static int ScaleHeal(Player player, int amount) {
            if (amount <= 1 || player == null || !player.HasBuff<CosmicSpace>()) {
                return amount;
            }
            return Math.Max(1, amount / 2);
        }
    }

    /// <summary>原版吸血刀和幽灵套的治疗弹幕把治疗量存在 ai[1],生成时折半</summary>
    public class CosmicSpaceHealProjectile : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.type == ProjectileID.VampireHeal || entity.type == ProjectileID.SpiritHeal;

        public override void OnSpawn(Projectile projectile, IEntitySource source) {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || projectile.owner != Main.myPlayer) {
                return;
            }
            if (Main.player[projectile.owner].HasBuff<CosmicSpace>() && projectile.ai[1] > 1f) {
                projectile.ai[1] = MathF.Max(1f, projectile.ai[1] * 0.5f);
            }
        }
    }
}
