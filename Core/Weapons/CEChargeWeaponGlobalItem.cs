using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Weapons
{
    /// <summary>
    /// 蓄势武器的自动化钩子:只挂在实现 ICEChargeWeapon 的物品上。
    /// 负责充能条/周期就绪的每帧推进、就绪时的释放乘数、物品栏充能角标。
    /// </summary>
    public class CEChargeWeaponGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => lateInstantiation && entity.ModItem is ICEChargeWeapon;

        // 两种触发都只在手持时推进。周期就绪原本挂在 UpdateInventory 上"在背包也计时",
        // 但 UpdateInventory 对背包里每一个物品每帧都跑,而充能是按物品实例存的,
        // 于是带 N 把同款就有 N 根独立的条一起涨,攒满能连放 N 次大招。
        public override void HoldItem(Item item, Player player)
        {
            var profile = ((ICEChargeWeapon)item.ModItem).ChargeProfile;
            if (profile.Trigger == CEChargeTrigger.ChargeBar || profile.Trigger == CEChargeTrigger.Periodic)
                CEChargeWeapon.Gain(player, item, profile, 1f);
        }

        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // 就绪的下一次攻击即大招:在 Shoot 之前套用释放乘数,时序对齐原灾厄 RogueWeapon.ModifyShootStats
            if (!CEChargeWeapon.IsReady(item))
                return;
            var profile = ((ICEChargeWeapon)item.ModItem).ChargeProfile;
            damage = (int)(damage * profile.DamageMult);
            velocity *= profile.VelocityMult;
            knockback *= profile.KnockbackMult;
        }

        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // 物品栏角标:槽位底部一条充能进度线,就绪时呼吸闪烁
            var meter = CEChargeWeapon.GetMeter(item);
            if (meter == null)
                return;

            var profile = ((ICEChargeWeapon)item.ModItem).ChargeProfile;
            Color barColor = CEChargeBarHUD.TriggerColor(profile.Trigger);
            if (meter.Ready)
                barColor = Color.Lerp(barColor, Color.White, 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f));

            int fullWidth = (int)(frame.Width * scale);
            int fillWidth = (int)(fullWidth * meter.Ratio);
            Vector2 barPos = position - origin * scale + new Vector2(0, frame.Height * scale - 2f);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(pixel, barPos, new Rectangle(0, 0, 1, 1), Color.Black * 0.45f, 0f, Vector2.Zero, new Vector2(fullWidth, 2f), SpriteEffects.None, 0f);
            if (fillWidth > 0)
                spriteBatch.Draw(pixel, barPos, new Rectangle(0, 0, 1, 1), barColor * 0.9f, 0f, Vector2.Zero, new Vector2(fillWidth, 2f), SpriteEffects.None, 0f);
        }
    }
}
