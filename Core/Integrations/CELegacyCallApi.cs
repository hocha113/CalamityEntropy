using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Projectiles.TwistedTwin;
using CalamityEntropy.Utilities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using static CalamityEntropy.Common.EGlobalNPC;

namespace CalamityEntropy.Core.Integrations
{
    /// <summary>
    /// 早期的字符串式 <c>Mod.Call</c> 接口。原先整块直写在模组入口里。
    /// <para>
    /// 本模组现有两套 Call 面:<see cref="ModCall"/> 的字典分发先跑,返回非 null 就收工;
    /// 只有它没登记(或登记了却返回 null)的调用名才落到这里。
    /// 其中 IsBookMark / SetBarColor / GetBookMarkSlots / AddBookMarkSlot /
    /// SetTTHoldoutCheck / GetTTHoldoutCheck / CopyProjForTTwin 这七个两边都有,
    /// 且 ModCall 一侧恒返回非 null,所以本文件里对应的分支实际到不了。
    /// 保留是为了本次重构不改变对外行为,是否删除留给作者裁决。
    /// </para>
    /// </summary>
    internal static class CELegacyCallApi
    {
        public static object Handle(object[] args) {
            try {
                if (args.Length > 0) {
                    if (args[0] is string str) {
                        //Usage: bool flag = (bool)Mod.Call("CheckFlag", "cruiser(or any name below)");
                        if (str.ToLower().Equals("checkflag")) {
                            if (args.Length == 2 && args[1] is string name) {
                                name = name.ToLower();
                                if (name == "acropolis")
                                    return EDownedBosses.downedAcropolis;
                                if (name == "apsychos")
                                    return EDownedBosses.downedApsychos;
                                if (name == "luminaris")
                                    return EDownedBosses.downedLuminaris;
                                if (name == "prophet")
                                    return EDownedBosses.downedProphet;
                                if (name == "nihility_twins")
                                    return EDownedBosses.downedNihilityTwin;
                                if (name == "cruiser")
                                    return EDownedBosses.downedCruiser;
                            }
                            return false;
                        }
                        if (str.ToLower().Equals("RegisterBookMarkEffect".ToLower())) {
                            return RegisterBookmarkEffect(args);
                        }
                        if (str.ToLower().Equals("RegisterBookMark".ToLower())) {
                            return RegisterBookmark(args);
                        }
                        if (str.Equals("IsBookMark")) {
                            Item item = (Item)args[1];
                            return BookMarkLoader.IsABookMark(item);
                        }
                        #region TwistedTwinsStuff
                        if (str.Equals("SetTTHoldoutCheck")) {
                            EGlobalProjectile.checkHoldOut = (bool)args[1];
                        }
                        if (str.Equals("GetTTHoldoutCheck")) {
                            return EGlobalProjectile.checkHoldOut;
                        }
                        if (str.Equals("CopyProjForTTwin")) {
                            CopyProjectileForTwistedTwin((int)args[1]);
                        }
                        #endregion
                        //Set a specific color for NPC
                        //Usage: Mod.Call("SetBarColor", ModContent.NPCType<T>(), color);
                        if (str.Equals("SetBarColor")) {
                            int type = (int)args[1];
                            Color color = (Color)args[2];
                            EntropyBossbar.bossbarColor[type] = color;
                        }
                        if (str.Equals("GetBookMarkSlots")) {
                            return ((Player)args[1]).GetMyMaxActiveBookMarks(((Player)args[1]).HeldItem);
                        }
                        if (str.Equals("AddBookMarkSlot")) //Set this every update just like minion slots
                        {
                            ((Player)args[1]).Entropy().AdditionalBookmarkSlot += (int)args[2];
                        }
                        if (str.Equals("AddBookMarkSlotSpecialTexture")) //Set this every update just like minion slots, client only
                        {
                            ((Player)args[1]).Entropy().BookmarkHolderSpecialTextures.Add((Texture2D)args[2]);
                        }
                        if (str.Equals("RegisterDebuff")) {
                            ExternalDebuffs.Add(
                                new DebuffDisplayEntry(
                                    (Func<NPC, bool>)args[1],
                                    (Func<Texture2D>)args[2]
                                )
                            );
                            return null;
                        }
                    }
                }
            } catch {
                string e = (args[0] is string str) ? $"({str})" : "";
                CalamityEntropy.Instance.Logger.Warn($"CalamityEntropy: ModCall's parameter is Error!{e}");
            }
            return null;
        }

        /// <summary>
        /// 让场上每个属于本地玩家的扭曲双子仆从复制一份指定弹幕。
        /// <see cref="ModCall"/> 的同名处理器也走这里。
        /// 原先那个处理器是回调 <c>Instance.Call("CopyProjForTTwin", …)</c>,
        /// 而 Call 的第一句就是 ModCall 分发,等于自己调自己:一次调用直接爆栈,
        /// 且 StackOverflowException 在 .NET 里抓不住,进程当场结束。
        /// </summary>
        public static void CopyProjectileForTwistedTwin(int projectileIndex) {
            Projectile projectile = projectileIndex.ToProj();
            int twinType = ModContent.ProjectileType<TwistedTwinMinion>();
            EGlobalProjectile.checkHoldOut = false;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type != twinType || p.owner != Main.myPlayer) {
                    continue;
                }
                int phd = Projectile.NewProjectile(Main.LocalPlayer.GetSource_ItemUse(Main.LocalPlayer.HeldItem), p.Center, Vector2.Zero,
                    projectile.type, projectile.damage, projectile.knockBack, projectile.owner);
                Projectile ph = phd.ToProj();
                ph.scale *= 0.8f;
                ph.Entropy().IndexOfTwistedTwinShootedThisProj = p.identity;
                ph.netUpdate = true;
                ph.damage = (int)(ph.damage * TwistedTwinMinion.damageMul);
                if (!ph.usesLocalNPCImmunity) {
                    ph.usesLocalNPCImmunity = true;
                    ph.localNPCHitCooldown = 12;
                }
            }
            EGlobalProjectile.checkHoldOut = true;
        }

        private static object RegisterBookmarkEffect(object[] args) {
            if (!(args[1] is Dictionary<string, object> objects)) {
                CalamityEntropy.Instance.Logger.Warn("Args[1] Must be a Dictionary<string, object>");
                return null;
            }
            if (!objects.TryGetValue("Name", out object nameObj) || !(nameObj is string)) {
                CalamityEntropy.Instance.Logger.Warn("Name is required and must be a string");
                return null;
            }
            string name = (string)nameObj;

            BookMarkLoader.RegisterBookmarkEffect(
                name,
                GetAction<ModProjectile>(objects, "OnShoot"),
                GetAction<ModProjectile>(objects, "OnActive"),
                GetAction<Projectile, bool>(objects, "OnProjectileSpawn"),
                GetAction<Projectile, bool>(objects, "UpdateProjectile"),
                GetAction<Projectile, NPC, int>(objects, "OnHitNPC"),
                GetAction<Projectile, NPC, NPC.HitModifiers>(objects, "ModifyHitNPC"),
                GetAction<Projectile, bool>(objects, "BookUpdate")
            );
            return null;
        }

        private static object RegisterBookmark(object[] args) {
            if (!(args[1] is Dictionary<string, object> objects)) {
                CalamityEntropy.Instance.Logger.Warn("Args[1] Must be a Dictionary<string, object>");
                return null;
            }
            if (!objects.TryGetValue("ItemType", out object itemTypeObj) || !(itemTypeObj is int)) {
                CalamityEntropy.Instance.Logger.Warn("ItemType is required and must be an integer");
                return null;
            }
            int itemType = (int)itemTypeObj;

            if (!objects.TryGetValue("Texture", out object textureObj) || !(textureObj is Asset<Texture2D> texture)) {
                CalamityEntropy.Instance.Logger.Warn("Texture is required and must be an Asset<Texture2D>");
                return null;
            }
            Func<Item, Item, bool> canBeEquipWith = null;
            if (objects.TryGetValue("CanBeEquipWithFunc", out var cbew_func) && cbew_func is Func<Item, Item, bool> fc) {
                canBeEquipWith = fc;
            }

            string effectName = objects.TryGetValue("EffectName", out object effectNameObj) && effectNameObj is string
                ? (string)effectNameObj : "";

            Func<int> modifyBaseProjectileType = objects.TryGetValue("ModifyBaseProjectileType", out object mbptObj) && mbptObj is Func<int> mbpt
                ? mbpt : null;

            BookMarkLoader.RegisterBookmark(
                itemType,
                texture,
                effectName,
                GetFunc<float, float>(objects, "ModifyStat_Damage"),
                GetFunc<float, float>(objects, "ModifyStat_Knockback"),
                GetFunc<float, float>(objects, "ModifyStat_ShootSpeed"),
                GetFunc<float, float>(objects, "ModifyStat_Homing"),
                GetFunc<float, float>(objects, "ModifyStat_Size"),
                GetFunc<float, float>(objects, "ModifyStat_Crit"),
                GetFunc<float, float>(objects, "ModifyStat_HomingRange"),
                GetFunc<int, int>(objects, "ModifyStat_PenetrateAddition"),
                GetFunc<float, float>(objects, "ModifyStat_AttackSpeed"),
                GetFunc<int, int>(objects, "ModifyStat_ArmorPenetration"),
                GetFunc<float, float>(objects, "ModifyStat_LifeSteal"),
                GetFunc<int, int>(objects, "ModifyProjectileType"),
                modifyBaseProjectileType,
                GetFunc<int, int>(objects, "ModifyShootCooldown"),
                canBeEquipWith
            );
            return null;
        }

        //跨模组传进来的委托类型对不上就当没给,不抛
        private static Action<T1> GetAction<T1>(Dictionary<string, object> objects, string key) {
            return objects.TryGetValue(key, out object actionObj) && actionObj is Action<T1> a ? a : null;
        }

        private static Action<T1, T2> GetAction<T1, T2>(Dictionary<string, object> objects, string key) {
            return objects.TryGetValue(key, out object actionObj) && actionObj is Action<T1, T2> a ? a : null;
        }

        private static Action<T1, T2, T3> GetAction<T1, T2, T3>(Dictionary<string, object> objects, string key) {
            return objects.TryGetValue(key, out object actionObj) && actionObj is Action<T1, T2, T3> a ? a : null;
        }

        private static Func<TInput, TOutput> GetFunc<TInput, TOutput>(Dictionary<string, object> objects, string key) {
            return objects.TryGetValue(key, out object funcObj) && funcObj is Func<TInput, TOutput> f ? f : null;
        }
    }
}
