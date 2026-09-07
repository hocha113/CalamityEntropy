using CalamityEntropy.Common;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.TwistedTwin;
using CalamityEntropy.Content.UI.EntropyBookUI;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityEntropy.Common.BookMarkLoader;

namespace CalamityEntropy.Content.Items.Books
{
    public abstract class EntropyBook : ModItem
    {
        public static string BaseFolder = "CalamityEntropy/Content/Items/Books";
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Magic;
            Item.damage = 44;
            Item.useTime = Item.useAnimation = 20;
            Item.shootSpeed = 26;
            Item.width = Item.height = 40;
            Item.knockBack = 4f;
            Item.useStyle = -1;
            Item.channel = true;
            Item.noMelee = true;
            Item.crit = 4;
            Item.mana = 4;
            Item.noUseGraphic = true;
            Item.rare = ItemRarityID.Orange;
        }
        public override bool CanUseItem(Player player)
        {
            return false;
        }
        public virtual bool PreDrawBookmarkSlot(Item bookmark, Vector2 pos, float alpha, float scale, float outlineAlpha)
        {
            return true;
        }
        public virtual void PlayBookmarkInsertSound()
        {
            CEUtils.PlaySound("turnPage");
        }
        public virtual int HeldProjectileType => -1;
        /// <summary>
        /// 书签栏位数,每本书各自覆写为固定值(正式书籍 1~5,按获取阶段分档)。
        /// 2026-08-31 平衡案曾改为随世界进度统一解锁(骷髅王/世花/月总各+1),2026-09-07 回退为逐本固定值并按测试组定稿表重排。
        /// </summary>
        public virtual int SlotCount => 6;
        //默认书签底座贴图,加载期就位;各书籍子类各自持有同名字段覆写
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BookMark1")]
        internal static Asset<Texture2D> BookMarkTex;
        public virtual Texture2D BookMarkTexture => BookMarkTex.Value;
        public virtual void CheckSpawn(Player player)
        {
            if (Main.myPlayer == player.whoAmI && !EBookUI.active)
            {
                if (player.HeldItem == Item)
                {
                    if (player.ownedProjectileCounts[HeldProjectileType] <= 0)
                    {
                        ((EntropyBookHeldProjectile)Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, HeldProjectileType, 0, 0, player.whoAmI, Item.type).ToProj().ModProjectile).bookItem = Item;

                        foreach (Projectile p in Main.projectile)
                        {
                            if (p.active && p.type == ModContent.ProjectileType<TwistedTwinMinion>() && p.owner == Main.myPlayer)
                            {
                                int phd = Projectile.NewProjectile(player.GetSource_ItemUse(Item), p.Center, Vector2.Zero, HeldProjectileType, 0, 0, player.whoAmI, Item.type);
                                Projectile ph = phd.ToProj();
                                (ph.ModProjectile as EntropyBookHeldProjectile).bookItem = Item;
                                ph.scale *= 0.8f;
                                ph.Entropy().IndexOfTwistedTwinShootedThisProj = p.identity;
                                p.netUpdate = true;
                                ph.netUpdate = true;
                                ph.damage = (int)(ph.damage * TwistedTwinMinion.damageMul);
                            }
                        }
                    }
                }
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Calamity Entropy: Entropy Book Info", Mod.GetLocalization("EBookTooltip").Value) { OverrideColor = Color.Yellow });
        }
    }

    public class EBookStatModifer
    {
        public float Damage = 1;
        public float Knockback = 1;
        public float shotSpeed = 1;
        public float Homing = 0;
        public float Size = 1;
        public float Crit = 0;
        public float HomingRange = 1;
        public int PenetrateAddition = 0;
        public float attackSpeed = 1;
        public int armorPenetration = 0;
        public float lifeSteal = 0;
        /// <summary>魔力消耗乘区(2026-08-31 平衡案:巨蟹座书签-10%)。</summary>
        public float ManaCost = 1;
    }

    public abstract class EntropyBookHeldProjectile : ModProjectile
    {
        public override void OnKill(int timeLeft)
        {
            active = false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (active)
                overPlayers.Add(index);
        }
        public int ItemType => (int)Projectile.ai[0];
        public int openAnim = 0;
        public bool UIOpen = false;
        public int UIOpenAnm = 0;
        public int shotCooldown = 0;
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.height = Projectile.width = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;

        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }

        /// <summary>
        /// 基础属性修改
        /// </summary>
        public virtual EBookStatModifer getBaseModifer()
        {
            EBookStatModifer modifer = new EBookStatModifer();
            modifer.Damage = 1;
            modifer.Knockback = Projectile.GetOwner().GetTotalKnockback(Projectile.DamageType).ApplyTo(bookItem.knockBack);
            modifer.Crit = Projectile.GetOwner().GetTotalCritChance(Projectile.DamageType) + Projectile.GetOwner().HeldItem.crit;
            modifer.attackSpeed = Projectile.GetOwner().GetTotalAttackSpeed(Projectile.DamageType);
            modifer.armorPenetration = Projectile.ArmorPenetration;
            return modifer;
        }

        /// <summary>
        /// 基础射弹特效
        /// </summary>
        public virtual EBookProjectileEffect getEffect()
        {
            return null;
        }

        public virtual string OpenAnimationPath => "";
        public virtual Texture2D[] OpenAnimations()
        {
            Texture2D[] texs = new Texture2D[3];
            for (int i = 0; i < 3; i++)
            {
                texs[i] = ModContent.Request<Texture2D>(OpenAnimationPath + i.ToString(), AssetRequestMode.ImmediateLoad).Value;
            }
            return texs;
        }
        public virtual string PageAnimationPath => "";
        public virtual Texture2D[] PageAnimations()
        {
            Texture2D[] texs = new Texture2D[5];
            for (int i = 0; i < 5; i++)
            {
                texs[i] = ModContent.Request<Texture2D>(PageAnimationPath + i.ToString(), AssetRequestMode.ImmediateLoad).Value;
            }
            return texs;
        }
        public virtual string UIOpenAnimationPath => "";
        public virtual Texture2D[] UIOpenAnimations()
        {
            Texture2D[] texs = new Texture2D[4];
            for (int i = 0; i < 4; i++)
            {
                texs[i] = ModContent.Request<Texture2D>(UIOpenAnimationPath + i.ToString(), AssetRequestMode.ImmediateLoad).Value;
            }
            return texs;
        }
        public int pageTurnAnm = 0;
        public virtual void playTurnPageAnimation()
        {
            playPageSound();
            pageTurnAnm = 4;
            Projectile.frameCounter = 0;
        }
        public virtual void playPageSound()
        {
            CEUtils.PlaySound("pageflip", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center, 4, 0.5f);
        }
        public bool active = false;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(active);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            active = reader.ReadBoolean();
        }

        public virtual Texture2D getTexture()
        {
            if (UIOpen)
            {
                return UIOpenAnimations()[UIOpenAnm];
            }
            else
            {
                if (openAnim < 2)
                {
                    return OpenAnimations()[openAnim];
                }
                else
                {
                    return PageAnimations()[pageTurnAnm];
                }
            }
        }
        public virtual void setVel()
        {
            if (Main.myPlayer != Projectile.owner)
                return;
            Vector2 newVel = (Main.MouseWorld - Projectile.GetOwner().Center).SafeNormalize(Vector2.UnitX);
            if (Projectile.velocity != newVel)
            {
                Projectile.velocity = newVel;
                Projectile.netUpdate = true;
            }
        }
        /// <summary>
        /// 手持坐标位移
        /// </summary>
        public virtual Vector2 heldOffset => new Vector2(14, 6);
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        /// <summary>
        /// 射弹类型
        /// </summary>
        public virtual int baseProjectileType => ModContent.ProjectileType<RuneBullet>();
        public virtual int getShootProjectileType()
        {
            int r = baseProjectileType;
            for (int i = 0; i < Projectile.GetOwner().GetMyMaxActiveBookMarks(bookItem); i++)
            {
                if (BookMarkLoader.IsABookMark(Projectile.GetOwner().Entropy().EBookStackItems[i]))
                {
                    Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                    int b = BookMarkLoader.ModifyBaseProjectile(it);
                    if (b >= 0)
                    {
                        r = b; break;
                    }
                }
            }
            return r;
        }
        /// <summary>
        /// 射击
        /// </summary>
        public virtual bool Shoot()
        {
            int type = getShootProjectileType();
            for (int i = 0; i < Main.LocalPlayer.GetMyMaxActiveBookMarks(bookItem); i++)
            {
                var bm = Projectile.owner.ToPlayer().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(bm))
                {
                    int pn = BookMarkLoader.ModifyProjectile(bm, type);
                    if (pn >= 0)
                    {
                        type = pn;
                    }
                }
            }
            ShootSingleProjectile(type, Projectile.Center, Projectile.velocity, MainProjectile: true);

            return true;
        }
        public Item bookItem;
        public virtual float randomShootRotMax => 0.1f;
        public virtual bool canApplyShootCDModifer => true;
        /// <summary>
        /// 根据武器面板，玩家属性，指定的属性修改计算最后的射弹伤害
        /// </summary>
        public int CauculateProjectileDamage(EBookStatModifer modifer, float mult = 1)
        {
            return (int)(Projectile.GetOwner().GetTotalDamage(Projectile.DamageType).ApplyTo(bookItem.damage * modifer.Damage * mult * (Projectile.Entropy().IndexOfTwistedTwinShootedThisProj < 0 ? 1 : TwistedTwinMinion.damageMul)));
        }
        /// <summary>
        /// 根据武器面板，玩家属性，装备书签计算最后的射弹伤害
        /// </summary>
        public int CauculateProjectileDamage(float mult = 1)
        {
            var modifer = GetProjectileModifer();
            return (int)(Projectile.GetOwner().GetTotalDamage(Projectile.DamageType).ApplyTo(bookItem.damage * modifer.Damage * mult * (Projectile.Entropy().IndexOfTwistedTwinShootedThisProj < 0 ? 1 : TwistedTwinMinion.damageMul)));
        }
        /// <summary>
        /// 计算射速倍率
        /// </summary>
        public float CauculateAttackSpeed()
        {
            var modifer = GetProjectileModifer();
            return Projectile.GetOwner().GetTotalAttackSpeed(Projectile.DamageType) * modifer.attackSpeed;
        }
        /// <summary>
        /// 获取最终的属性修改
        /// </summary>
        /// <summary>书本次施放的魔力消耗(吃书签 ManaCost 乘区,2026-08-31 平衡案:巨蟹座-10%)。</summary>
        public int GetManaCost()
        {
            return int.Max(1, (int)(bookItem.mana * GetProjectileModifer().ManaCost));
        }
        public EBookStatModifer GetProjectileModifer()
        {
            EBookStatModifer modifer = getBaseModifer();
            for (int i = 0; i < Projectile.GetOwner().GetMyMaxActiveBookMarks(bookItem); i++)
            {
                Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(it))
                {
                    BookMarkLoader.ModifyStat(it, modifer);
                }
            }
            return modifer;
        }
        /// <summary>
        /// 发射一个带有特效的单个射弹
        /// </summary>
        public virtual void ShootSingleProjectile(int type, Vector2 pos, Vector2 velocity, float damageMul = 1, float scaleMul = 1, float shotSpeedMul = 1, Action<Projectile> initAction = null, float randomRotMult = 1, bool MainProjectile = false, Color colorMult = default, int fixedBaseDamage = -1)
        {
            if (!Projectile.active)
                return;
            int origType = this.getShootProjectileType();
            var modifer = GetProjectileModifer();
            Vector2 shootVel = (velocity.normalize() * bookItem.shootSpeed * modifer.shotSpeed * shotSpeedMul).RotatedByRandom(this.randomShootRotMax * randomRotMult);
            float kb = Projectile.GetOwner().GetTotalKnockback(Projectile.DamageType).ApplyTo(bookItem.knockBack * modifer.Knockback);
            // fixedBaseDamage>=0:书签派生弹幕走固定基伤,只吃玩家加成,不吃书面板(2026-08-31 平衡案)
            int dmg = fixedBaseDamage >= 0
                ? EBookProjectileEffect.FixedDamage(Projectile.GetOwner(), fixedBaseDamage, Projectile.DamageType)
                : CauculateProjectileDamage(modifer, damageMul);
            bookItem.channel = false;
            if (ItemLoader.Shoot(bookItem, Projectile.GetOwner(), new Terraria.DataStructures.EntitySource_ItemUse_WithAmmo(Projectile.GetOwner(), bookItem, 0), pos, velocity * ContentSamples.ProjectilesByType[type].MaxUpdates, type, dmg, kb))
            {
                Projectile proj = Projectile.NewProjectile(Projectile.GetOwner().GetSource_ItemUse(bookItem), pos, shootVel, type, dmg, kb, Projectile.owner).ToProj();
                if (proj.penetrate >= 0)
                    proj.penetrate += modifer.PenetrateAddition;
                proj.CritChance = (int)modifer.Crit;
                proj.scale *= modifer.Size * scaleMul;
                proj.ArmorPenetration += (int)(Projectile.GetOwner().GetTotalArmorPenetration(Projectile.DamageType) + modifer.armorPenetration + bookItem.ArmorPenetration);

                if (proj.ModProjectile is EBookBaseProjectile bp)
                {
                    bp.shooter = Projectile.identity;
                    bp.mainProj = MainProjectile;
                    bp.ShooterModProjectile = this;
                    bp.homing += modifer.Homing;
                    bp.homingRange *= modifer.HomingRange;
                    bp.attackSpeed = modifer.attackSpeed;
                    bp.lifeSteal += modifer.lifeSteal;
                    bp.origProjType = origType;
                    // 书签效果只挂在熵书本体弹幕上;衍生/召唤类弹幕不再携带,自然也无法触发书签(2026-08-31 平衡案)
                    if (MainProjectile)
                    {
                        for (int i = 0; i < Math.Min(Main.LocalPlayer.GetMyMaxActiveBookMarks(bookItem), Projectile.GetOwner().Entropy().EBookStackItems.Count); i++)
                        {
                            Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                            if (BookMarkLoader.IsABookMark(it))
                            {
                                EBookProjectileEffect eff = BookMarkLoader.GetEffect(it);
                                if (eff != null)
                                {
                                    eff.FromBookmark = true;
                                    bp.ProjectileEffects.Add(eff);
                                }
                            }
                        }
                    }
                    if (this.getEffect() != null)
                    {
                        bp.ProjectileEffects.Add(this.getEffect());
                    }
                    if (colorMult != default)
                    {
                        bp.color = bp.baseColor.MultiplyRGBA(colorMult);
                        bp.initColor = false;
                    }
                }
                initAction?.Invoke(proj);
                CEUtils.SyncProj(proj);
            }
            bookItem.channel = true;
        }
        public bool mouseRightLast = false;
        public virtual bool CanShoot()
        {
            return true;
        }
        /// <summary>
        /// 根据武器useTime，射速倍率，书签计算射击冷却（帧）
        /// </summary>
        public virtual int GetShootCd()
        {
            int _shotCooldown = bookItem.useTime;

            EBookStatModifer m = getBaseModifer();
            for (int i = 0; i < Main.LocalPlayer.GetMyMaxActiveBookMarks(bookItem); i++)
            {
                Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(it))
                {
                    BookMarkLoader.ModifyStat(it, m);
                    if (this.canApplyShootCDModifer)
                    {
                        BookMarkLoader.modifyShootCooldown(it, ref _shotCooldown);
                    }
                }
            }
            return (int)((float)_shotCooldown / m.attackSpeed);
        }
        public virtual int frameChange => 4;
        public virtual Vector2 UIHeldOffset => Vector2.UnitY * -52;
        public override bool PreAI()
        {
            if (bookItem == null)
                bookItem = Projectile.GetOwner().HeldItem;
            return true;
        }
        public override void AI()
        {
            var player = Projectile.GetOwner();
            if (Projectile.Entropy().IndexOfTwistedTwinShootedThisProj >= 0 && !Projectile.Entropy().IndexOfTwistedTwinShootedThisProj.ToProj().active)
            {
                Projectile.Kill();
                return;
            }
            if (player.dead)
            {
                Projectile.Kill();
                return;
            }

            if (player.HeldItem.type != ItemType && !UIOpen)
            {
                Projectile.Kill();
                return;
            }
            if (EBookUI.active)
            {
                UIOpen = true;
            }
            if (!UIOpen)
            {
                EBookUI.bookItem = player.HeldItem;
            }
            Projectile.timeLeft++;
            if (UIOpen && player.HeldItem.ModItem is EntropyBook eb)
            {
                if (player.HeldItem.type != ItemType)
                {
                    Projectile.Kill();
                    UIOpen = false;
                    EBookUI.active = false;
                }
            }
            if (!player.mouseInterface && Main.myPlayer == Projectile.owner)
            {
                if (Main.mouseRight && !mouseRightLast && !active)
                {
                    UIOpen = !UIOpen;
                    EBookUI.active = UIOpen;
                    if (UIOpen)
                    {
                        UIOpenAnm = 0;
                        Main.playerInventory = true;
                    }
                }
            }
            if (UIOpen && !Main.playerInventory)
            {
                UIOpen = false;
            }
            mouseRightLast = Main.mouseRight;
            setVel();
            if (!UIOpen)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else
            {
                Projectile.rotation = 0;
            }
            shotCooldown--;
            SetPosision();
            if (Main.myPlayer == Projectile.owner)
            {
                bool flag = Main.mouseLeft && !Main.LocalPlayer.mouseInterface && !UIOpen && Projectile.GetOwner().CheckMana(GetManaCost(), false);
                if (flag != active)
                {
                    active = flag;
                    Projectile.netUpdate = true;
                    if (active)
                    {
                        for (int i = 0; i < Main.LocalPlayer.GetMyMaxActiveBookMarks(bookItem); i++)
                        {
                            Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                            if (BookMarkLoader.IsABookMark(it))
                            {
                                var e = BookMarkLoader.GetEffect(it);
                                if (e != null)
                                {
                                    e.OnActive(this);
                                }
                            }
                        }
                        if (this.getEffect() != null)
                            this.getEffect().OnActive(this);
                    }
                }
                if (active)
                {
                    player.heldProj = Projectile.whoAmI;
                    player.itemTime = 3;
                    player.itemAnimation = 3;
                    player.channel = true;
                }
                if (!UIOpen)
                {
                    if (Projectile.velocity.X > 0)
                    {
                        player.direction = 1;
                        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
                    }
                    else
                    {
                        player.direction = -1;
                        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
                    }
                }
                if (active && Opened)
                {
                    ManaNoRegen = 60;
                    if (shotCooldown <= 0 && CanShoot())
                    {
                        if (Projectile.GetOwner().CheckMana(GetManaCost(), true))
                        {
                            if (Main.myPlayer != Projectile.owner || Shoot())
                            {
                                if (active && pageTurnAnm == 0)
                                {
                                    playTurnPageAnimation();
                                }
                                shotCooldown = bookItem.useTime;

                                EBookStatModifer m = getBaseModifer();
                                for (int i = 0; i < Main.LocalPlayer.GetMyMaxActiveBookMarks(bookItem); i++)
                                {
                                    Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                                    if (BookMarkLoader.IsABookMark(it))
                                    {
                                        var e = BookMarkLoader.GetEffect(it);
                                        BookMarkLoader.ModifyStat(it, m);
                                        if (this.canApplyShootCDModifer)
                                        {
                                            BookMarkLoader.modifyShootCooldown(it, ref shotCooldown);
                                        }
                                        // 书签"攻击时"触发统一1秒内置CD(与命中触发分通道,避免空实现白占CD)
                                        if (e != null && CECooldowns.CheckBMProc("Shoot_" + e.RegisterName()))
                                        {
                                            e.OnShoot(this);
                                        }
                                    }
                                }
                                if (this.getEffect() != null)
                                {
                                    var e = this.getEffect();
                                    if (e != null)
                                    {
                                        e.OnShoot(this);
                                    }

                                }
                                SetShootCooldown((int)((float)shotCooldown / m.attackSpeed));
                            }
                        }
                        else
                        {
                            active = false;
                        }
                    }
                }
            }
            if (ManaNoRegen-- > 0 && player.manaRegenDelay < 20)
                player.manaRegenDelay = 20;
            if (active)
            {
                for (int i = 0; i < Projectile.GetOwner().GetMyMaxActiveBookMarks(bookItem); i++)
                {
                    Item item = Projectile.GetOwner().Entropy().EBookStackItems[i];
                    if (BookMarkLoader.IsABookMark(item))
                    {
                        if (BookMarkLoader.GetEffect(item) != null)
                        {
                            BookMarkLoader.GetEffect(item).BookUpdate(Projectile, Main.myPlayer == Projectile.owner);
                        }
                    }
                }
            }
            UpdateAnimations();
            Projectile.GetOwner().heldProj = Projectile.whoAmI;
        }
        public virtual void SetShootCooldown(int cd)
        {
            shotCooldown = cd;
        }
        public virtual void SetPosision()
        {
            Projectile.Center = Projectile.GetOwner().MountedCenter + (UIOpen ? UIHeldOffset : new Vector2(heldOffset.X, heldOffset.Y * (Projectile.velocity.X > 0 ? 1 : -1))).RotatedBy(Projectile.rotation);
        }
        public int ManaNoRegen = 0;
        public virtual bool Opened => openAnim >= 2;
        public virtual void UpdateAnimations()
        {
            Projectile.frameCounter++;
            if (!active && !UIOpen && openAnim == 0)
            {
                Projectile.frameCounter = 0;
            }
            if (Projectile.frameCounter >= frameChange)
            {
                Projectile.frameCounter = 0;
                if (pageTurnAnm > 0)
                {
                    pageTurnAnm--;
                }
                if (UIOpen)
                {
                    Projectile.rotation = 0;
                    if (UIOpenAnm < 3)
                    {
                        UIOpenAnm++;
                    }
                    if (openAnim > 0)
                    {
                        openAnim--;
                    }
                }
                else
                {
                    if (UIOpenAnm > 0)
                    {
                        UIOpenAnm--;
                    }
                    if (active)
                    {
                        if (openAnim < 2)
                        {
                            openAnim++;
                        }
                    }
                    else
                    {
                        if (openAnim > 0)
                        {
                            openAnim--;
                        }
                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = getTexture();
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2, Projectile.scale, (Projectile.velocity.X > 0 || UIOpen ? SpriteEffects.None : SpriteEffects.FlipVertically), 0);
            return false;
        }
    }
    public abstract class EBookBaseProjectile : ModProjectile
    {
        public int hitCount = 0;
        public float homing = 0;
        public float homingRange = 460;
        public bool init = true;
        public bool sync = false;
        public bool EffectInit = true;
        public float lifeSteal = 0;
        public float gravity = 0;
        public bool mainProj = false;
        public int origProjType = -1;
        public virtual Color baseColor => Color.White;
        public Color color;
        public bool initColor = true;
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            hitbox = Projectile.Center.getRectCentered(hitbox.Width * Projectile.scale, hitbox.Height * Projectile.scale);
        }
        public override bool PreAI()
        {
            if (ShooterModProjectile == null)
            {
                Projectile p = shooter.ToProj_Identity();
                if (p != null && p != default)
                {
                    if (p.ModProjectile != null)
                    {
                        ShooterModProjectile = p.ModProjectile;
                    }
                }
            }

            if (initColor)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                initColor = false;
                color = baseColor;
            }
            return true;
        }
        public override void SetDefaults()
        {
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1000;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(shooter);
            writer.Write(homing);
            writer.Write(homingRange);
            writer.Write(Projectile.penetrate);
            writer.Write(Projectile.scale);
            writer.Write(Projectile.CritChance);
            writer.Write(lifeSteal);
            writer.Write(gravity);
            writer.Write(mainProj);
            writer.Write(origProjType);

            writer.Write(ProjectileEffects.Count);
            foreach (var effect in ProjectileEffects)
            {
                writer.Write(effect.RegisterName());
                writer.Write(effect.BMOtherMod_Name);
            }
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            shooter = reader.ReadInt32();
            homing = reader.ReadSingle();
            homingRange = reader.ReadSingle();
            Projectile.penetrate = reader.ReadInt32();
            Projectile.scale = reader.ReadSingle();
            Projectile.CritChance = reader.ReadInt32();
            lifeSteal = reader.ReadInt32();
            gravity = reader.ReadSingle();
            mainProj = reader.ReadBoolean();
            origProjType = reader.ReadInt32();

            this.ProjectileEffects.Clear();
            int r = reader.ReadInt32();
            for (int i = 0; i < r; i++)
            {
                var bef = EBookProjectileEffect.findByName(reader.ReadString());
                string omN = reader.ReadString();
                if (omN != string.Empty)
                {
                    bef = new BookmarkEffect_OtherMod() { BMOtherMod_Name = omN };
                }
                this.ProjectileEffects.Add(bef);
            }
            sync = true;
        }
        public List<EBookProjectileEffect> ProjectileEffects = new List<EBookProjectileEffect>();
        public float attackSpeed = 1;
        public ModProjectile ShooterModProjectile = null;
        public int shooter = -1;

        public virtual void ApplyHoming()
        {
            if (homing <= 0)
            {
                return;
            }
            NPC homingTarget = Projectile.FindTargetWithinRange(this.homingRange, (Projectile.tileCollide ? true : false));
            if (homingTarget != null)
            {
                Projectile.velocity *= 1f - (homing * 0.075f);
                Projectile.velocity += (homingTarget.Center - Projectile.Center).normalize() * homing * 4.2f;
            }
        }
        public override void AI()
        {
            if (init)
            {
                init = false;
                bool ownerClient = Main.myPlayer == Projectile.owner;
                if (ownerClient)
                {
                    sync = true;
                }
                if (ownerClient)
                {
                    CEUtils.SyncProj(Projectile.whoAmI);
                }
            }
            if (sync)
            {
                if (EffectInit)
                {
                    EffectInit = false;
                    foreach (var effect in ProjectileEffects)
                    {
                        effect.OnProjectileSpawn(Projectile, Main.myPlayer == Projectile.owner);
                    }
                }
            }
            Projectile.velocity.Y += this.gravity;
            foreach (var effect in ProjectileEffects)
            {
                effect.UpdateProjectile(Projectile, Main.myPlayer == Projectile.owner);
            }
            this.ApplyHoming();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitCount++;
            foreach (var effect in this.ProjectileEffects)
            {
                // 书签命中触发统一1秒内置CD;书本体自带效果不受限(2026-08-31 平衡案)
                if (effect.FromBookmark && !CECooldowns.CheckBMProc("Hit_" + effect.RegisterName()))
                    continue;
                effect.OnHitNPC(Projectile, target, damageDone);
            }
            if (lifeSteal > 0 && StealLife)
            {
                if (mainProj || Main.rand.NextBool(5))
                    Projectile.GetOwner()?.Entropy().HealFloat(lifeSteal);
                StealLife = false;
            }
        }
        public bool StealLife = true;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            foreach (var effect in this.ProjectileEffects)
            {
                effect.ModifyHitNPC(Projectile, target, ref modifiers);
            }
        }
    }
    public abstract class EBookBaseLaser : EBookBaseProjectile
    {
        public int segLength = 30;
        public int segCounts = 100;
        public int penetrate = 1;
        public int quickTime = -1;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = hitCd;
        }
        public virtual float width => 32 * Projectile.scale;
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public List<Vector2> _points = new List<Vector2>();
        public override void ApplyHoming() { }
        public virtual List<Vector2> getSamplePoints()
        {
            return _points;
        }
        public virtual int OnHitEffectProb => 6;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(OnHitEffectProb))
            {
                base.OnHitNPC(target, hit, damageDone);
            }
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(quickTime);
            writer.Write(penetrate);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            quickTime = reader.ReadInt32();
            penetrate = reader.ReadInt32();
        }
        public List<Vector2> cauculatePoints()
        {
            var points = new List<Vector2>();
            bool laserHoming = homing > 0;
            Vector2 startPos = Projectile.Center;
            List<NPC> hited = new List<NPC>();
            Vector2 nowPos = startPos;
            Vector2 addVel = Projectile.velocity.SafeNormalize(Vector2.UnitX) * segLength;
            Vector2 lastPos = startPos;
            var activenpcs = new List<NPC>();
            foreach (var n in Main.ActiveNPCs)
            {
                if (!n.friendly && !n.dontTakeDamage && n.CanBeChasedBy(Projectile))
                {
                    activenpcs.Add(n);
                }
            }
            for (int i = 0; i < segCounts; i++)
            {
                NPC homingTarget = null;
                float dist = homingRange;
                foreach (NPC npc in activenpcs)
                {
                    if (!hited.Contains(npc))
                    {
                        float r = CEUtils.getDistance(nowPos, npc.Center);
                        if (r < dist)
                        {
                            dist = r;
                            homingTarget = npc;
                        }
                    }
                    if (hited.Contains(npc) || npc.dontTakeDamage)
                    {
                        continue;
                    }
                    if (CEUtils.LineThroughRect(lastPos, nowPos, npc.getRect(), (int)width))
                    {
                        hited.Add(npc);
                    }
                }
                if (hited.Count >= penetrate)
                {
                    points.Add(nowPos);
                    return points;
                }
                if (laserHoming)
                {
                    Vector2 oldPos = Projectile.Center;
                    Projectile.Center = nowPos;

                    if (homingTarget != null)
                    {
                        addVel += (homingTarget.Center - Projectile.Center).normalize() * homing * 2.3f;
                        addVel *= 1 - homing * 0.12f;
                    }
                    Projectile.Center = oldPos;
                }
                lastPos = nowPos;
                points.Add(nowPos);
                nowPos += addVel.SafeNormalize(Vector2.UnitX) * segLength;
            }
            return points;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var points = this.getSamplePoints();
            for (int i = 1; i < points.Count; i++)
            {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, (int)width))
                {
                    return true;
                }
            }
            return false;
        }
        public virtual int hitCd => 10;
        public override bool PreAI()
        {
            Projectile.localNPCHitCooldown = (int)((float)hitCd / this.attackSpeed);
            if (quickTime > 0)
            {
                quickTime--;
                if (quickTime == 0)
                {
                    Projectile.Kill();
                    return false;
                }
            }
            return base.PreAI();
        }
        public override void AI()
        {
            if (this.penetrate < Projectile.penetrate)
            {
                this.penetrate = Projectile.penetrate;
            }
            Projectile.penetrate = -1;

            base.AI();
        }
        public override void PostAI()
        {
            _points = cauculatePoints();
        }
    }
    public abstract class EBookProjectileEffect : ModType
    {
        public static List<EBookProjectileEffect> instances;
        public string BMOtherMod_Name = string.Empty;

        /// <summary>
        /// 本实例是否来自书签(而非书本体自带效果)。挂载时由 ShootSingleProjectile/OnShoot 站点标记;
        /// 书签触发受统一1秒内置CD约束,书本体效果不受。不参与网络同步(触发只在弹幕主人端结算)。
        /// </summary>
        public bool FromBookmark = false;

        /// <summary>
        /// 书签派生弹幕统一伤害口径:固定基伤×玩家伤害加成,不吃书面板(2026-08-31 平衡案)。
        /// </summary>
        public static int FixedDamage(Player player, int baseDamage, DamageClass damageClass = null)
        {
            return (int)player.GetTotalDamage(damageClass ?? DamageClass.Magic).ApplyTo(baseDamage);
        }
        protected sealed override void Register()
        {
            if (instances == null)
            {
                instances = new List<EBookProjectileEffect>();
            }
            instances.Add(this);
        }
        public override void Unload()
        {
            instances = null;
        }
        public virtual void BookUpdate(Projectile projectile, bool ownerClient)
        {

        }
        public static EBookProjectileEffect findByName(string name)
        {
            if (instances == null)
            {
                return null;
            }
            foreach (EBookProjectileEffect eff in instances)
            {
                if (eff.RegisterName() == name)
                {
                    return eff;
                }
            }
            return null;
        }
        public virtual string RegisterName()
        {
            return this.Name;
        }

        public virtual void OnShoot(EntropyBookHeldProjectile book)
        {

        }
        public virtual void OnActive(EntropyBookHeldProjectile book)
        {

        }
        public virtual void OnProjectileSpawn(Projectile projectile, bool ownerClient)
        {

        }
        public virtual void UpdateProjectile(Projectile projectile, bool ownerClient)
        {

        }

        public virtual void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {

        }

        public virtual void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {

        }

        /// <summary>
        /// 独立攻击钩子，当书签被外部系统触发时调用
        /// 仅当OnShoot/OnActive中有需要在独立模式下复现的逻辑时才需重写
        /// </summary>
        public virtual void OnStandaloneAttack(Player player, Vector2 position, Vector2 direction, int damage, float knockback)
        {

        }
    }
}