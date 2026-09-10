using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Core.Weapons
{
    /// <summary>
    /// 蓄势强化标志的弹幕侧载体,原灾厄弹幕 stealthStrike 标志的 1:1 平替。
    /// 读:proj.IsEmpowered();写:CEChargeWeapon.Empower(p) 或 proj.SetEmpowered()。
    /// 标志随弹幕生成包与 netUpdate 同步(SendExtraAI/ReceiveExtraAI)。
    /// <para>同时兼管"这颗弹幕是不是玩家主动使用武器打出来的"这一来源标记
    /// (读:proj.IsFromWeaponUse()),给需要区分武器命中与饰品常驻光环的效果用。
    /// 来源链的继承规则与蓄势来源武器共用一套,不另起一个 GlobalProjectile。</para>
    /// </summary>
    public class CEEmpowerGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        /// <summary>是否为蓄势强化弹(大招弹幕)。</summary>
        public bool Empowered;

        /// <summary>
        /// 本弹幕出自玩家主动使用的武器(含其衍生弹)。饰品、护甲与常驻伤害光环生成的一律为假。
        /// 只在生成端算得出,不过网;消费点都在主人端(命中钩子只在主人端跑),因此无需同步。
        /// </summary>
        internal bool FromWeaponUse;

        /// <summary>发射本弹幕的蓄势武器,仅所有者端有值,用于命中计数回充。</summary>
        internal Item sourceItem;

        /// <summary>本弹幕为大招弹幕的衍生弹,不参与命中计数回充(大招及其产物不给自己充能)。</summary>
        internal bool creditBlocked;

        /// <summary>
        /// 判定生成源是不是"玩家主动使用武器"。注意 Player.GetSource_Accessory 返回的同样是
        /// EntitySource_ItemUse(上游 Player.cs:52963),只是 Item 换成了那件饰品,
        /// 所以必须再排掉饰品与不造成伤害的物品,否则地雷盒、虚空核心之类会被误判成武器。
        /// </summary>
        private static bool IsWeaponUse(EntitySource_ItemUse itemUse)
            => itemUse.Item != null && !itemUse.Item.accessory && itemUse.Item.damage > 0;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            // 直接由物品使用生成:先记武器来源标记,再按蓄势武器记来源与当帧强化窗口。
            // (EntitySource_ItemUse 继承自 EntitySource_Parent,须先判)
            if (source is EntitySource_ItemUse itemUse)
            {
                FromWeaponUse = IsWeaponUse(itemUse);

                if (itemUse.Item?.ModItem is not ICEChargeWeapon)
                    return;

                sourceItem = itemUse.Item;

                // TryConsume 打开的当帧强化窗口:同帧由该玩家此武器发出的弹幕自动打标。
                // OnSpawn 先于生成同步包发出,标志随首包到达其他端,无需二次同步。
                if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers
                    && Main.player[projectile.owner].GetModPlayer<CEChargePlayer>().EmpowerWindowActive)
                {
                    Empowered = true;
                }
                return;
            }

            // 父弹幕链路(GetSource_FromAI 等):父弹幕已记录来源武器时,子弹幕继承其来源。
            // 覆盖手持弹幕→伤害弹的间接生成链(如 AzafureLightMachineGun 的 ALMGLaser)。
            // 每次生成继承一跳,深链由逐级继承自然传递,不做向上遍历,因此不存在环。
            if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProj)
            {
                CEEmpowerGlobalProjectile parentGlobal = parentProj.GetGlobalProjectile<CEEmpowerGlobalProjectile>();
                FromWeaponUse = parentGlobal.FromWeaponUse;
                if (parentGlobal.sourceItem != null)
                {
                    sourceItem = parentGlobal.sourceItem;
                    creditBlocked = parentGlobal.Empowered || parentGlobal.creditBlocked;
                }
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 命中计数回充:仅普通弹幕计数,大招弹幕及其衍生弹不回充
            if (Empowered || creditBlocked || sourceItem == null)
                return;
            CEChargeWeapon.CreditHit(Main.player[projectile.owner], sourceItem);
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(Empowered);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            Empowered = bitReader.ReadBit();
        }
    }

    /// <summary>蓄势强化标志的查询与写入扩展。</summary>
    public static class CEEmpowerExtensions
    {
        /// <summary>该弹幕是否为蓄势强化弹。对照原灾厄 stealthStrike 读取点。</summary>
        public static bool IsEmpowered(this Projectile projectile)
            => projectile.GetGlobalProjectile<CEEmpowerGlobalProjectile>().Empowered;

        /// <summary>
        /// 该弹幕是否出自玩家主动使用的武器(含手持弹幕与仆从的衍生弹)。
        /// 仅在生成端有效,只应在主人端的命中钩子里读。
        /// </summary>
        public static bool IsFromWeaponUse(this Projectile projectile)
            => projectile.GetGlobalProjectile<CEEmpowerGlobalProjectile>().FromWeaponUse;

        /// <summary>
        /// 标记为蓄势强化弹。sync = true 时立即补发同步包
        /// (生成后再打标时首包不含标志,需要补同步;走 TryConsume 窗口自动打标的不需要)。
        /// </summary>
        public static void SetEmpowered(this Projectile projectile, bool sync = true)
        {
            projectile.GetGlobalProjectile<CEEmpowerGlobalProjectile>().Empowered = true;
            if (sync)
                CEUtils.SyncProj(projectile);
        }
    }
}
