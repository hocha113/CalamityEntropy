using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Conqueror
{
    /// <summary>
    /// 沙海征服者的刺击,贴图用物品图,作为 heldProj 画在玩家手里
    /// ai[0] 已过帧数,ai[1] 总帧数(物品 Shoot 写入),ai[2] 已命中次数(拥有者累加,其余端据此补命中火花)
    /// </summary>
    public class SandseaConquerorStab : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/SandseaDragon/Conqueror/SandseaConqueror";

        public const int MinLife = 8;                   //总帧数下限,攻速再高也留出刺出与收回
        private const int BodySize = 18;                //碰撞箱边长,像素,只管相机裁剪与附魔粒子的范围
        private const float ThrustEnd = 0.25f;          //刺出段结束的进度
        private const float HoldEnd = 0.5f;             //停顿段结束的进度
        private const float RetractKeep = 0.3f;         //收回到底时剩余的伸出量,0-1
        private const float DamageEnd = 0.6f;           //判定窗口结束的进度,往回收时不再伤人
        private const float FadeStart = 0.85f;          //开始淡出的进度,连刺时只在换手的两三帧变淡
        private const float FadeInFrames = 2f;          //淡入帧数
        private const float GripBack = -12f;            //起手时握点沿刺向相对手部的偏移,像素
        private const float GripFront = 2f;             //刺到头时的偏移,像素
        private const int HitWidth = 26;                //判定线宽,像素,约刃宽的 1.5 倍
        private const float HitTipExtra = 8f;           //判定线越过刃尖的余量,像素
        private const float CutWidthRatio = 0.5f;       //割草线宽占判定线宽的比例
        private const float EnchantAlong = 0.6f;        //附魔粒子出在刃长上的位置,0-1
        private const float OrbLaunchAt = 0.18f;        //发射能量球的进度,约刺到头
        private const float OrbTipInset = 6f;           //能量球起点从刃尖往回收的距离,像素
        private const float OrbKnockbackMult = 0.5f;    //能量球击退占本体的比例
        private const int MaxFangTargets = 2;           //每刺最多几个目标触发毒牙
        private const int SparkExtraIndex = 98;         //原版星芒贴图 Extra_98,竖直纺锤形,72 像素
        private const float GlintAlong = 0.62f;         //刺出闪光中心在刃长上的位置,0-1
        private const float GlintWidth = 0.45f;         //闪光横向缩放
        private const float GlintLength = 1.15f;        //闪光纵向缩放,拉到约 83 像素
        private static readonly Color GlintColor = new Color(255, 190, 110, 0);     //A 为 0 即加色
        private static readonly Vector2 GripPx = new Vector2(6f, 36f);              //物品图上的握点,像素,量自 40×42 贴图
        private static readonly Vector2 TipPx = new Vector2(38f, 1f);               //物品图上的刃尖
        private static readonly float BladeLength = Vector2.Distance(GripPx, TipPx);
        private static readonly float SpriteAngle = (TipPx - GripPx).ToRotation();  //贴图里握点指向刃尖的角度

        private Vector2 tip;        //本帧刃尖世界坐标
        private Vector2 hitStart;   //本帧判定线起点,只在 AI 里取,认得扭曲孪生替身的位置
        private int seenHits;       //本端已播过火花的命中次数
        private int fangTargets;    //本次刺击已触发毒牙的目标数,只在拥有者端累加
        private bool launched;      //本端是否已过发射那一拍
        private bool sized;         //是否已按物品缩放定过大小

        private int Age {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        private int Life => Math.Max(MinLife, (int)Projectile.ai[1]);
        private float Progress => MathHelper.Clamp(Age / (float)Life, 0f, 1f);
        private int LaunchFrame => Math.Max(1, (int)MathF.Ceiling(Life * OrbLaunchAt));
        private Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);

        public override void SetDefaults() {
            Projectile.width = BodySize;
            Projectile.height = BodySize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.noEnchantmentVisuals = true;
            Projectile.hide = true;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.CCed) {
                Projectile.Kill();
                return;
            }
            Age++;
            if (Age > Life) {
                Projectile.Kill();
                return;
            }
            if (!sized) {
                sized = true;
                Projectile.scale *= player.GetAdjustedItemScale(player.HeldItem);
            }

            Vector2 dir = Dir;
            float ext = Extension(Progress);
            Projectile.rotation = dir.ToRotation();
            Projectile.spriteDirection = dir.X < 0f ? -1 : 1;
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeInFrames, Age, true) * Utils.GetLerpValue(1f, FadeStart, Progress, true);

            //扭曲孪生替身发出的那一份不占玩家的手,按普通弹幕画;其 AI 期间玩家中心已被全局钩子换到替身处
            bool twinCopy = Projectile.Entropy().IndexOfTwistedTwinShootedThisProj >= 0;
            Projectile.hide = !twinCopy;
            Projectile.ownerHitCheck = !twinCopy;
            if (!twinCopy) {
                player.heldProj = Projectile.whoAmI;
                player.SetHandRot(Projectile.rotation, ext.ToStretchAmount());
            }
            Vector2 grip = player.GetFrontHandPositionImproved(player.compositeFrontArm) + dir * MathHelper.Lerp(GripBack, GripFront, ext);
            Projectile.Center = grip;
            tip = grip + dir * (BladeLength * Projectile.scale);
            hitStart = player.MountedCenter;
            Projectile.EmitEnchantmentVisualsAt(Vector2.Lerp(grip, tip, EnchantAlong) - new Vector2(BodySize / 2f), BodySize, BodySize);

            if (!launched && Age >= LaunchFrame) {
                launched = true;
                LaunchOrbs(dir);
            }
            int hits = (int)Projectile.ai[2];
            if (hits > seenHits) {
                seenHits = hits;
                SandseaConquerorVfx.BladeHit(tip, dir);
            }
        }

        /// <summary>进度转伸出量,快出、停顿、慢收</summary>
        private static float Extension(float progress) {
            if (progress < ThrustEnd) {
                float left = 1f - progress / ThrustEnd;
                return 1f - left * left * left;
            }
            if (progress < HoldEnd) {
                return 1f;
            }
            return MathHelper.SmoothStep(1f, RetractKeep, (progress - HoldEnd) / (1f - HoldEnd));
        }

        private void LaunchOrbs(Vector2 dir) {
            Vector2 muzzle = tip - dir * OrbTipInset;
            if (Projectile.owner == Main.myPlayer) {
                int type = ModContent.ProjectileType<SandseaConquerorOrb>();
                int damage = Math.Max(1, (int)MathF.Round(Projectile.damage * SandseaConquerorOrb.DamageMult));
                float knockback = Projectile.knockBack * OrbKnockbackMult;
                for (int side = -1; side <= 1; side += 2) {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), muzzle, dir * SandseaConquerorOrb.Speed, type, damage, knockback, Projectile.owner, side);
                }
            }
            SandseaConquerorVfx.OrbLaunch(muzzle, dir);
        }

        public override bool? CanDamage() {
            return Progress <= DamageEnd ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            //从持剑者中心量起,贴脸的目标也算刺中
            return CEUtils.LineThroughRect(hitStart, tip + Dir * HitTipExtra, targetHitbox, HitWidth);
        }

        public override void CutTiles() {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, tip, HitWidth * CutWidthRatio, DelegateMethods.CutTiles);
        }

        //命中钩子只在拥有者端跑,ai[2] 过网后其余端在 AI 里补火花
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Vector2 point = HitPoint(target);
            Projectile.ai[2]++;
            seenHits = (int)Projectile.ai[2];
            Projectile.netUpdate = true;
            SandseaConquerorVfx.BladeHit(point, Dir);
            if (fangTargets < MaxFangTargets) {
                fangTargets++;
                SandseaConquerorFang.SpawnVolley(Projectile, target, point);
            }
        }

        /// <summary>刃线上离目标中心最近的点,再收进目标的碰撞箱</summary>
        private Vector2 HitPoint(NPC target) {
            Vector2 blade = tip - Projectile.Center;
            float along = MathHelper.Clamp(Vector2.Dot(target.Center - Projectile.Center, blade) / Math.Max(blade.LengthSquared(), 1f), 0f, 1f);
            Vector2 point = Projectile.Center + blade * along;
            Rectangle box = target.Hitbox;
            return new Vector2(MathHelper.Clamp(point.X, box.Left, box.Right), MathHelper.Clamp(point.Y, box.Top, box.Bottom));
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 dir = Dir;
            bool flip = dir.X < 0f;
            Vector2 origin = flip ? new Vector2(texture.Width - GripPx.X, GripPx.Y) : GripPx;
            float rotation = Projectile.rotation - (flip ? MathHelper.Pi - SpriteAngle : SpriteAngle);
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(texture, drawPos, null, lightColor * Projectile.Opacity, rotation, origin, Projectile.scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            //刺出与停顿段沿刃身的一道闪光,不切批次
            float flash = Utils.GetLerpValue(HoldEnd, 0f, Progress, true) * Projectile.Opacity;
            if (flash > 0f) {
                Texture2D spark = TextureAssets.Extra[SparkExtraIndex].Value;
                Vector2 glintPos = drawPos + dir * (BladeLength * Projectile.scale * GlintAlong);
                Main.EntitySpriteDraw(spark, glintPos, null, GlintColor * flash, Projectile.rotation + MathHelper.PiOver2, spark.Size() / 2f, new Vector2(GlintWidth, GlintLength) * Projectile.scale, SpriteEffects.None);
            }
            return false;
        }
    }
}
