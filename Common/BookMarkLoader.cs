using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.UI.EntropyBookUI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public static class BookMarkLoader
    {
        public static Dictionary<string, BookmarkEffectFunctionGroups> CustomBMEffectsByName;
        public static Dictionary<int, BookMarkTag> CustomBMByID;
        public static void RegisterBookmarkEffect(string name, Action<ModProjectile> onShoot = null,
                Action<ModProjectile> onActive = null,
                Action<Projectile, bool> onProjectileSpawn = null,
                Action<Projectile, bool> updateProjectile = null,
                Action<Projectile, NPC, int> onHitNPC = null,
                Action<Projectile, NPC, NPC.HitModifiers> modifyHitNPC = null,
                Action<Projectile, bool> bookUpdate = null,
                Action<Player, Vector2, Vector2, int, float> onStandaloneAttack = null) {
            CustomBMEffectsByName[name] = new BookmarkEffectFunctionGroups(onShoot, onActive, onProjectileSpawn, updateProjectile, onHitNPC, modifyHitNPC, bookUpdate, onStandaloneAttack);
        }

        public static void RegisterBookmark(int ItemType, Asset<Texture2D> tex, string effectName = "",
            Func<float, float> modifyStat_Damage = null,
       Func<float, float> modifyStat_Knockback = null,
       Func<float, float> modifyStat_ShootSpeed = null,
       Func<float, float> modifyStat_Homing = null,
       Func<float, float> modifyStat_Size = null,
       Func<float, float> modifyStat_Crit = null,
       Func<float, float> modifyStat_HomingRange = null,
       Func<int, int> modifyStat_PenetrateAddition = null,
       Func<float, float> modifyStat_AttackSpeed = null,
      Func<int, int> modifyStat_ArmorPenetration = null,
      Func<float, float> modifyStat_LifeSteal = null,
       Func<int, int> modifyProjectileType = null,
       Func<int> modifyBaseProjectileType = null,
       Func<int, int> modifyShootCooldown = null,
       Func<Item, Item, bool> canBeEq = null) {
            CustomBMByID[ItemType] = new BookMarkTag(tex == null ? null : tex.Value, effectName, canBeEq == null ? default : canBeEq) {
                ModifyStat_Damage = modifyStat_Damage,
                ModifyStat_Knockback = modifyStat_Knockback,
                ModifyStat_ShootSpeed = modifyStat_ShootSpeed,
                ModifyStat_Homing = modifyStat_Homing,
                ModifyStat_Size = modifyStat_Size,
                ModifyStat_Crit = modifyStat_Crit,
                ModifyStat_HomingRange = modifyStat_HomingRange,
                ModifyStat_PenetrateAddition = modifyStat_PenetrateAddition,
                ModifyStat_AttackSpeed = modifyStat_AttackSpeed,
                ModifyStat_ArmorPenetration = modifyStat_ArmorPenetration,
                ModifyStat_LifeSteal = modifyStat_LifeSteal,
                ModifyProjectileType = modifyProjectileType,
                ModifyBaseProjectileType = modifyBaseProjectileType,
                ModifyShootCooldown = modifyShootCooldown
            };
        }
        public static bool GetPlayerHeldEntropyBook(Player player, out EntropyBookHeldProjectile eb) {
            eb = null;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == player.whoAmI && p.ModProjectile != null && p.ModProjectile is EntropyBookHeldProjectile ebh) {
                    eb = ebh;
                    return true;
                }
            }
            return false;
        }
        public static bool HeldingBookAndHasBookmarkEffect<T>(Player player) where T : EBookProjectileEffect {
            if (player.HeldItem.ModItem != null && player.HeldItem.ModItem is EntropyBook ebi && GetPlayerHeldEntropyBook(player, out var eb)) {
                if (eb.UIOpen)
                    return false;
                int c = player.GetMyMaxActiveBookMarks(player.HeldItem);
                if (c > 0) {
                    for (int i = 0; i < c; i++) {
                        Item it = player.Entropy().EBookStackItems[i];
                        if (IsABookMark(it) && GetEffect(it) is T) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static bool HasEmptyBookMarkSlot(Item item, Player player) {
            bool f = false;
            int c = player.GetMyMaxActiveBookMarks(item);
            if (c > 0) {
                for (int i = 0; i < c; i++) {
                    Item it = player.Entropy().EBookStackItems[i];
                    if (it.IsAir) {
                        return true;
                    }
                }
            }
            return f;
        }
        public static void OnShoot(string Name, ModProjectile mp) {
            if (CustomBMEffectsByName.ContainsKey(Name)) {
                CustomBMEffectsByName[Name].OnShoot?.Invoke(mp);
            }
        }
        public static void OnActive(string Name, ModProjectile mp) {
            if (CustomBMEffectsByName.ContainsKey(Name)) {
                CustomBMEffectsByName[Name].OnActive?.Invoke(mp);
            }
        }
        public static void OnProjectileSpawn(string Name, Projectile p, bool ownerClient) {
            if (CustomBMEffectsByName.ContainsKey(Name)) {
                CustomBMEffectsByName[Name].OnProjectileSpawn?.Invoke(p, ownerClient);
            }
        }
        public static void UpdateProjectile(string Name, Projectile p, bool ownerClient) {
            if (CustomBMEffectsByName.ContainsKey(Name)) {
                CustomBMEffectsByName[Name].UpdateProjectile?.Invoke(p, ownerClient);
            }
        }
        public static void OnHitNPC(string Name, Projectile proj, NPC npc, int damageDone) {
            if (CustomBMEffectsByName.ContainsKey(Name)) {
                CustomBMEffectsByName[Name].OnHitNPC?.Invoke(proj, npc, damageDone);
            }
        }
        public static void ModifyHitNPC(string Name, Projectile proj, NPC npc, NPC.HitModifiers modifiers) {
            if (CustomBMEffectsByName.ContainsKey(Name)) {
                CustomBMEffectsByName[Name].ModifyHitNPC?.Invoke(proj, npc, modifiers);
            }
        }
        public static void BookUpdate(string Name, Projectile projectile, bool ownerClient) {
            if (CustomBMEffectsByName.ContainsKey(Name)) {
                CustomBMEffectsByName[Name].BookUpdate?.Invoke(projectile, ownerClient);
            }
        }


        public static int GetMyMaxActiveBookMarks(this Player player, Item book) {
            if (player.Entropy().EBookStackItems == null)
                return 0;
            return Math.Min(EBookUI.getMaxSlots(player, book), player.Entropy().EBookStackItems.Count);
        }
        public static Texture2D GetUITexture(Item item) {
            if (item.ModItem is BookMark bm) {
                return bm.UITexture;
            }
            if (CustomBMByID.ContainsKey(item.type)) {
                return CustomBMByID[item.type].UITexture;
            }
            return null;
        }
        public static EBookProjectileEffect GetEffect(Item item) {
            if (item.ModItem is BookMark bm) {
                return bm.getEffect();
            }
            if (CustomBMByID.ContainsKey(item.type)) {
                return CustomBMByID[item.type].getEffect();
            }
            return null;
        }
        public static void ModifyStat(Item item, EBookStatModifer modifer) {
            if (item.ModItem is BookMark bm) {
                bm.ModifyStat(modifer);
            }
            if (CustomBMByID.ContainsKey(item.type)) {
                var tag = CustomBMByID[item.type];
                if (tag.ModifyStat_Damage != null)
                    modifer.Damage = tag.ModifyStat_Damage(modifer.Damage);
                if (tag.ModifyStat_Knockback != null)
                    modifer.Knockback = tag.ModifyStat_Knockback(modifer.Knockback);
                if (tag.ModifyStat_ShootSpeed != null)
                    modifer.shotSpeed = tag.ModifyStat_ShootSpeed(modifer.shotSpeed);
                if (tag.ModifyStat_Homing != null)
                    modifer.Homing = tag.ModifyStat_Homing(modifer.Homing);
                if (tag.ModifyStat_Size != null)
                    modifer.Size = tag.ModifyStat_Size(modifer.Size);
                if (tag.ModifyStat_Crit != null)
                    modifer.Crit = tag.ModifyStat_Crit(modifer.Crit);
                if (tag.ModifyStat_HomingRange != null)
                    modifer.HomingRange = tag.ModifyStat_HomingRange(modifer.HomingRange);
                if (tag.ModifyStat_PenetrateAddition != null)
                    modifer.PenetrateAddition = tag.ModifyStat_PenetrateAddition(modifer.PenetrateAddition);
                if (tag.ModifyStat_AttackSpeed != null)
                    modifer.attackSpeed = tag.ModifyStat_AttackSpeed(modifer.attackSpeed);
                if (tag.ModifyStat_ArmorPenetration != null)
                    modifer.armorPenetration = tag.ModifyStat_ArmorPenetration(modifer.armorPenetration);
                if (tag.ModifyStat_LifeSteal != null)
                    modifer.lifeSteal = tag.ModifyStat_LifeSteal(modifer.lifeSteal);
            }
        }
        public static int ModifyProjectile(Item item, int origProj) {
            if (item.ModItem is BookMark bm) {
                return bm.modifyProjectile(origProj);
            }
            if (CustomBMByID.ContainsKey(item.type)) {
                var tag = CustomBMByID[item.type];
                if (tag.ModifyProjectileType != null) {
                    return tag.ModifyProjectileType(origProj);
                }
            }
            return -1;
        }
        public static int ModifyBaseProjectile(Item item) {
            if (item.ModItem is BookMark bm) {
                return bm.modifyBaseProjectile();
            }
            if (CustomBMByID.ContainsKey(item.type)) {
                var tag = CustomBMByID[item.type];
                if (tag.ModifyBaseProjectileType != null) {
                    return tag.ModifyBaseProjectileType();
                }
            }
            return -1;
        }
        public static void modifyShootCooldown(Item item, ref int shootCd) {
            if (item.ModItem is BookMark bm) {
                bm.modifyShootCooldown(ref shootCd);
            }
            if (CustomBMByID.ContainsKey(item.type)) {
                var tag = CustomBMByID[item.type];
                if (tag.ModifyShootCooldown != null) {
                    shootCd = tag.ModifyShootCooldown(shootCd);
                }
            }
        }
        public static bool IsABookMark(Item item) {
            return item.ModItem is BookMark || CustomBMByID.ContainsKey(item.type);
        }
        private static bool CanBeEquipWith_Base(Item a, Item b) {
            return a.type != b.type;
        }
        public static bool CanBeEquipWith(Item a, Item b) {
            if (a.ModItem is BookMark bm) {
                return bm.CanBeEquipWith(b);
            }
            if (IsABookMark(a)) {
                return CustomBMByID[a.type].CanBeEquipWith.Invoke(a, b);
            }
            return false;
        }
        public class BookmarkEffectFunctionGroups
        {
            public Action<ModProjectile> OnShoot;
            public Action<ModProjectile> OnActive;
            public Action<Projectile, bool> OnProjectileSpawn;
            public Action<Projectile, bool> UpdateProjectile;
            public Action<Projectile, NPC, int> OnHitNPC;
            public Action<Projectile, NPC, NPC.HitModifiers> ModifyHitNPC;
            public Action<Projectile, bool> BookUpdate;
            public Action<Player, Vector2, Vector2, int, float> OnStandaloneAttack;

            public BookmarkEffectFunctionGroups(
                Action<ModProjectile> onShoot = null,
                Action<ModProjectile> onActive = null,
                Action<Projectile, bool> onProjectileSpawn = null,
                Action<Projectile, bool> updateProjectile = null,
                Action<Projectile, NPC, int> onHitNPC = null,
                Action<Projectile, NPC, NPC.HitModifiers> modifyHitNPC = null,
                Action<Projectile, bool> bookUpdate = null,
                Action<Player, Vector2, Vector2, int, float> onStandaloneAttack = null
                ) {
                OnShoot = onShoot;
                OnActive = onActive;
                OnProjectileSpawn = onProjectileSpawn;
                UpdateProjectile = updateProjectile;
                OnHitNPC = onHitNPC;
                ModifyHitNPC = modifyHitNPC;
                BookUpdate = bookUpdate;
                OnStandaloneAttack = onStandaloneAttack;
            }
        }
        public class BookmarkEffect_OtherMod : EBookProjectileEffect
        {
            public override void OnShoot(EntropyBookHeldProjectile book) {
                BookMarkLoader.OnShoot(this.BMOtherMod_Name, book);
            }
            public override void OnActive(EntropyBookHeldProjectile book) {
                BookMarkLoader.OnActive(this.BMOtherMod_Name, book);
            }
            public override void OnProjectileSpawn(Projectile projectile, bool ownerClient) {
                BookMarkLoader.OnProjectileSpawn(this.BMOtherMod_Name, projectile, ownerClient);
            }
            public override void UpdateProjectile(Projectile projectile, bool ownerClient) {
                BookMarkLoader.UpdateProjectile(this.BMOtherMod_Name, projectile, ownerClient);
            }

            public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
                BookMarkLoader.OnHitNPC(this.BMOtherMod_Name, projectile, target, damageDone);
            }

            public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers) {
                BookMarkLoader.ModifyHitNPC(this.BMOtherMod_Name, projectile, target, modifiers);
            }

            public override void BookUpdate(Projectile projectile, bool ownerClient) {
                BookMarkLoader.BookUpdate(this.BMOtherMod_Name, projectile, ownerClient);
            }

            public override void OnStandaloneAttack(Player player, Vector2 position, Vector2 direction, int damage, float knockback) {
                if (CustomBMEffectsByName.TryGetValue(this.BMOtherMod_Name, out var funcs)) {
                    funcs.OnStandaloneAttack?.Invoke(player, position, direction, damage, knockback);
                }
            }
        }
        public class BookMarkTag
        {
            public string CustomBMEffectName;
            public Texture2D uiTex;
            public Func<Item, Item, bool> CanBeEquipWith = CanBeEquipWith_Base;
            public BookMarkTag(Texture2D uiTexture, string customBMEffectName = null, Func<Item, Item, bool> canBeEquipWith = default) {
                uiTex = uiTexture;
                this.CustomBMEffectName = customBMEffectName;
                if (canBeEquipWith != default) {
                    CanBeEquipWith = canBeEquipWith;
                }
            }
            public Texture2D UITexture => uiTex;
            public EBookProjectileEffect getEffect() {
                return CustomBMEffectsByName.ContainsKey(this.CustomBMEffectName) ? new BookmarkEffect_OtherMod() { BMOtherMod_Name = this.CustomBMEffectName } : null;
            }
            public Func<float, float> ModifyStat_Damage;
            public Func<float, float> ModifyStat_Knockback;
            public Func<float, float> ModifyStat_ShootSpeed;
            public Func<float, float> ModifyStat_Homing;
            public Func<float, float> ModifyStat_Size;
            public Func<float, float> ModifyStat_Crit;
            public Func<float, float> ModifyStat_HomingRange;
            public Func<int, int> ModifyStat_PenetrateAddition;
            public Func<float, float> ModifyStat_AttackSpeed;
            public Func<int, int> ModifyStat_ArmorPenetration;
            public Func<float, float> ModifyStat_LifeSteal;
            public Func<int, int> ModifyProjectileType;
            public Func<int> ModifyBaseProjectileType;
            public Func<int, int> ModifyShootCooldown;
        }

        /// <summary>
        /// 统一攻击接口，独立触发一个书签的攻击行为，无需EntropyBookHeldProjectile
        /// 外部Mod可通过此方法或ModCall("PerformBookmarkAttack", ...)调用
        /// 流程：应用属性修改 -> 确定弹幕类型 -> 计算冷却 -> 生成弹幕 -> 挂载Effect -> 触发OnStandaloneAttack
        /// 如果BookMark重写了PerformAttack()则使用其自定义实现
        /// </summary>
        public static BookmarkAttackResult PerformBookmarkAttack(
            Item bookmarkItem,
            Player player,
            Vector2 position,
            Vector2 direction,
            int baseDamage = 50,
            float baseKnockback = 2f,
            int baseProjectileType = -1,
            float baseShootSpeed = 12f,
            int baseCooldown = 20,
            DamageClass damageClass = null) {
            if (!IsABookMark(bookmarkItem))
                return new BookmarkAttackResult { Success = false, CooldownTicks = 0 };

            damageClass ??= DamageClass.Magic;

            //如果是内部BookMark且重写了PerformAttack，优先使用
            if (bookmarkItem.ModItem is BookMark bm) {
                var context = new BookmarkAttackContext {
                    Player = player,
                    Position = position,
                    Direction = direction,
                    BaseDamage = baseDamage,
                    BaseKnockback = baseKnockback,
                    BaseProjectileType = baseProjectileType,
                    BaseShootSpeed = baseShootSpeed,
                    BaseCooldown = baseCooldown,
                    DamageType = damageClass
                };
                var customResult = bm.PerformAttack(context);
                if (customResult != null)
                    return customResult;
            }

            //默认实现
            return PerformDefaultBookmarkAttack(bookmarkItem, player, position, direction,
                baseDamage, baseKnockback, baseProjectileType, baseShootSpeed, baseCooldown, damageClass);
        }

        private static BookmarkAttackResult PerformDefaultBookmarkAttack(
            Item bookmarkItem, Player player, Vector2 position, Vector2 direction,
            int baseDamage, float baseKnockback, int baseProjectileType, float baseShootSpeed,
            int baseCooldown, DamageClass damageClass) {
            var result = new BookmarkAttackResult();

            //收集属性修改
            EBookStatModifer modifer = new EBookStatModifer();
            modifer.Crit = player.GetTotalCritChance(damageClass);
            modifer.Knockback = player.GetTotalKnockback(damageClass).ApplyTo(baseKnockback);
            modifer.attackSpeed = player.GetTotalAttackSpeed(damageClass);
            ModifyStat(bookmarkItem, modifer);
            result.AppliedModifier = modifer;

            //确定弹幕类型
            int projType = baseProjectileType >= 0 ? baseProjectileType : ModContent.ProjectileType<RuneBullet>();
            int baseReplace = ModifyBaseProjectile(bookmarkItem);
            if (baseReplace >= 0) projType = baseReplace;
            int typeReplace = ModifyProjectile(bookmarkItem, projType);
            if (typeReplace >= 0) projType = typeReplace;

            //计算冷却
            int cooldown = baseCooldown;
            modifyShootCooldown(bookmarkItem, ref cooldown);
            cooldown = modifer.attackSpeed > 0 ? (int)(cooldown / modifer.attackSpeed) : cooldown;
            result.CooldownTicks = Math.Max(1, cooldown);

            //计算伤害和击退
            int dmg = (int)player.GetTotalDamage(damageClass).ApplyTo(baseDamage * modifer.Damage);
            float kb = modifer.Knockback;

            //生成弹幕
            Vector2 dir = direction.SafeNormalize(Vector2.UnitX);
            Vector2 vel = dir * baseShootSpeed * modifer.shotSpeed;

            int projIndex = Projectile.NewProjectile(
                player.GetSource_FromThis(), position, vel,
                projType, dmg, kb, player.whoAmI);

            Projectile proj = Main.projectile[projIndex];
            result.ProjectileIndex = projIndex;
            result.ProjectileType = projType;

            //将属性应用到弹幕
            if (proj.penetrate >= 0)
                proj.penetrate += modifer.PenetrateAddition;
            proj.CritChance = (int)modifer.Crit;
            proj.scale *= modifer.Size;
            proj.ArmorPenetration += (int)(player.GetTotalArmorPenetration(damageClass) + modifer.armorPenetration);
            proj.DamageType = damageClass;

            //获取效果实例（只取一次，避免CustomBM重复创建实例）
            EBookProjectileEffect effect = GetEffect(bookmarkItem);

            //若为EBookBaseProjectile，挂载书签效果
            if (proj.ModProjectile is EBookBaseProjectile bp) {
                bp.mainProj = true;
                bp.homing += modifer.Homing;
                bp.homingRange *= modifer.HomingRange;
                bp.attackSpeed = modifer.attackSpeed;
                bp.lifeSteal += modifer.lifeSteal;

                if (effect != null) {
                    bp.ProjectileEffects.Add(effect);
                }
            }

            //调用OnStandaloneAttack，复现OnShoot/OnActive中的独立逻辑
            if (effect != null) {
                effect.OnStandaloneAttack(player, position, direction, dmg, kb);
            }

            result.Success = true;
            return result;
        }

        /// <summary>
        /// 查询书签的能力信息，不触发任何攻击
        /// </summary>
        public static BookmarkInfo GetBookmarkInfo(Item bookmarkItem) {
            if (!IsABookMark(bookmarkItem))
                return null;

            var info = new BookmarkInfo();
            info.IsBookmark = true;

            //检查属性修改
            var testModifer = new EBookStatModifer();
            ModifyStat(bookmarkItem, testModifer);
            info.HasStatModifiers = testModifer.Damage != 1 || testModifer.Knockback != 1 ||
                testModifer.shotSpeed != 1 || testModifer.Homing != 0 || testModifer.Size != 1 ||
                testModifer.Crit != 0 || testModifer.HomingRange != 1 || testModifer.PenetrateAddition != 0 ||
                testModifer.attackSpeed != 1 || testModifer.armorPenetration != 0 || testModifer.lifeSteal != 0;
            info.StatSnapshot = testModifer;

            //检查弹幕替换
            info.ReplacesBaseProjectile = ModifyBaseProjectile(bookmarkItem) >= 0;
            info.ReplacesProjectile = ModifyProjectile(bookmarkItem, -1) >= 0;

            //检查冷却修改
            int testCd = 20;
            modifyShootCooldown(bookmarkItem, ref testCd);
            info.ModifiesCooldown = testCd != 20;

            //检查是否有主动效果
            info.HasEffect = GetEffect(bookmarkItem) != null;

            //UI纹理
            info.UITexture = GetUITexture(bookmarkItem);

            return info;
        }
    }

    /// <summary>
    /// 书签独立攻击的上下文参数，由外部Mod传入
    /// </summary>
    public class BookmarkAttackContext
    {
        public Player Player;
        public Vector2 Position;
        public Vector2 Direction;
        public int BaseDamage = 50;
        public float BaseKnockback = 2f;
        /// <summary>基础弹幕类型，-1表示使用默认RuneBullet，会被书签的弹幕替换覆盖</summary>
        public int BaseProjectileType = -1;
        public float BaseShootSpeed = 12f;
        public int BaseCooldown = 20;
        public DamageClass DamageType = DamageClass.Magic;
    }

    /// <summary>
    /// 书签攻击返回的结果
    /// </summary>
    public class BookmarkAttackResult
    {
        public bool Success;
        /// <summary>建议的冷却时间(Tick数)，外部Mod应在此时间后才再次触发</summary>
        public int CooldownTicks;
        /// <summary>应用的属性修改快照</summary>
        public EBookStatModifer AppliedModifier;
        /// <summary>生成的弹幕在Main.projectile中的索引，-1表示未生成</summary>
        public int ProjectileIndex = -1;
        /// <summary>实际使用的弹幕类型ID</summary>
        public int ProjectileType = -1;
    }

    /// <summary>
    /// 书签能力信息，只读查询用，不触发攻击
    /// </summary>
    public class BookmarkInfo
    {
        public bool IsBookmark;
        public bool HasStatModifiers;
        public bool ReplacesBaseProjectile;
        public bool ReplacesProjectile;
        public bool ModifiesCooldown;
        public bool HasEffect;
        public EBookStatModifer StatSnapshot;
        public Texture2D UITexture;
    }
}
