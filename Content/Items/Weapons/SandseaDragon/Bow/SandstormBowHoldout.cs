using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Bow
{
    /// <summary>
    /// 沙尘暴的持弓弹幕,贴图用物品图,作为 heldProj 画在玩家手里。每次使用由物品的 Shoot 生成或重新上弦,先拉弦再放箭,放完留在手里一小会儿
    /// ai[0] 本轮要射的弹幕类型,ai[1] 本轮总帧数,ai[2] 上弦后已过帧数。三者由拥有者写入并同步,姿态全由它们算出,各端同帧同形
    /// </summary>
    public class SandstormBowHoldout : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/SandseaDragon/Bow/SandstormBow";

        private const float DrawFraction = 0.38f;       //拉弦段占一轮的比例,其余是放箭后的余势
        private const float MinDrawFrames = 3f;         //拉弦最少帧数,攻速再高也看得见拉弦
        private const float MinCycle = 2f;              //一轮总帧数的下限
        private const int LingerFrames = 20;            //最后一轮结束后弓留在手里的帧数
        private const int KeepAliveFrames = 3;          //每次更新续上的 timeLeft,消失只由拥有者裁决
        private const float StringPull = 9f;            //满弦时箭扣后拉的距离,像素
        private const float StringVibeAmplitude = 4f;   //放箭瞬间弦的前冲幅度,像素
        private const float StringVibeDecay = 0.72f;    //弦颤幅度每帧保留的比例
        private const float StringVibeFrequency = 2.4f; //弦颤角频率,弧度每帧
        private const float KickPeak = 3.5f;            //放普通箭时弓身后坐的峰值,像素
        private const float GiantKickPeak = 5.5f;       //放巨箭时的后坐峰值,像素
        private const float KickRiseFraction = 0.19f;   //后坐到峰值的时间占余势段的比例
        private const float KickRiseMin = 1.5f;         //后坐到峰值的最少帧数
        private const float HandReturnFraction = 0.75f; //拉弦手回到弦上的时间占余势段的比例
        private const float HandReturnMin = 4f;         //拉弦手回位的最少帧数
        private const float HandDrop = 3f;              //拉弦手落在箭下方的距离,像素
        private const float LimbFlexX = 0.02f;          //满弦时弓身沿瞄准轴拉长的比例
        private const float LimbFlexY = 0.04f;          //满弦时上下弓臂向内收的比例
        private const float GiantNockScale = 0.7f;      //巨箭刚搭上弦时的缩放,拉满长到 1
        private const float GiantHeadForward = 22f;     //满弦时巨箭箭头中心在握把前方的距离,像素,巨箭的出箭点(弦位 -15 拉距 -9 箭身 +46)
        private const float NockInset = 2f;             //箭尾压进弦的深度,像素
        private const float StringWidth = 2f;           //弦的粗细,像素,同贴图里的一格
        private const int StringOverlap = 1;            //两段弦在箭扣处多画的长度,像素,补住折角的缺口
        //以下是在 SandstormBow.png(48×76,弦在左、朝 +X 发射)上量出的像素坐标
        private const float GripX = 34f;                //握把中心,绘制原点,也是后手落点
        private const float GripY = 38f;
        private const float StringAnchorX = -15f;       //弦的上下挂点相对握把,贴图 (19,30) 与 (19,46)
        private const float StringAnchorY = 8f;
        private const int BakedStringRight = 20;        //贴图里烘进去的弦占 x<20、y 30..45,画弓身时跳过
        private const int BakedStringTop = 30;
        private const int BakedStringBottom = 46;
        //以下取自原版 Player.GetBackHandPosition 与 GetFrontHandPosition
        private const float BackShoulderX = 6f;         //后肩相对绘制中心,像素,朝向为正
        private const float FrontShoulderX = -4f;       //前肩相对绘制中心,像素
        private const float ShoulderY = -2f;
        private const float BackArmReachX = 10f;        //后臂全伸的臂长,横纵两轴,像素
        private const float BackArmReachY = 12f;
        private const float ArmReachNone = 4f;          //前臂四档伸展的臂长,像素
        private const float ArmReachQuarter = 6f;
        private const float ArmReachThreeQuarters = 8f;
        private const float ArmReachFull = 10f;
        private static readonly Color StringColor = new Color(37, 20, 29);  //弦色,取自贴图

        private float lastCounter = -1f;    //上一帧进 AI 时的 Counter,据此判断是否刚跨过放箭帧
        private float pull;                 //0-1 拉弦进度
        private float nockShift;            //箭扣相对静止弦位沿瞄准轴的位移,像素,负为向后
        private float handPull;             //拉弦手相对静止弦位后撤的距离,像素
        private float kick;                 //弓身后坐,像素
        private bool nocked;                //弦上是否搭着箭
        private IEntitySource shotSource;   //仅拥有者端,本轮箭的生成来源
        private float shotSpeed;            //仅拥有者端,本轮普通箭的初速,像素每帧

        private int ArrowType => (int)Projectile.ai[0];
        private float Cycle => Math.Max(Projectile.ai[1], MinCycle);
        private ref float Counter => ref Projectile.ai[2];
        private bool Giant => ArrowType == ModContent.ProjectileType<SandstormBowGiantArrow>();
        private float ReleaseFrame => Math.Min(Math.Max(Cycle * DrawFraction, MinDrawFrames), Cycle - 1f);

        public override void SetDefaults() {
            Projectile.HeldProjSetDefaults(DamageClass.Ranged);
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.noEnchantmentVisuals = true;
            Projectile.timeLeft = KeepAliveFrames;
        }

        /// <summary>物品每次使用时调用,只在拥有者端。手里已有弓就重新上弦,没有就生成</summary>
        internal static void Arm(Player player, IEntitySource source, Vector2 position, Vector2 aim, int arrowType, float speed, int damage, float knockback) {
            int type = ModContent.ProjectileType<SandstormBowHoldout>();
            int cycle = player.itemTimeMax;
            foreach (Projectile other in Main.ActiveProjectiles) {
                if (other.owner == player.whoAmI && other.type == type && other.ModProjectile is SandstormBowHoldout held) {
                    held.Rearm(player, source, arrowType, cycle, speed, damage, knockback);
                    return;
                }
            }
            int index = Projectile.NewProjectile(source, position, aim, type, damage, knockback, player.whoAmI, arrowType, cycle);
            if (Main.projectile[index].ModProjectile is SandstormBowHoldout spawned) {
                spawned.shotSource = source;
                spawned.shotSpeed = speed;
            }
        }

        private void Rearm(Player player, IEntitySource source, int arrowType, int cycle, float speed, int damage, float knockback) {
            //攻速高到上一轮还没放箭就又触发时,先把欠的那一轮放掉
            if (lastCounter < ReleaseFrame) {
                Release(player, Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction));
            }
            Projectile.ai[0] = arrowType;
            Projectile.ai[1] = cycle;
            Counter = 0f;
            Projectile.damage = damage;
            Projectile.knockBack = knockback;
            shotSource = source;
            shotSpeed = speed;
            //这里在玩家更新阶段,netUpdate 会被弹幕自己的 Update 开头清掉,直接发包
            CEUtils.SyncProj(Projectile.whoAmI);
        }

        public override void AI() {
            Projectile.timeLeft = KeepAliveFrames;
            //更新次数被豆奶之类翻倍时,仍按帧推进
            if (!Projectile.FinalExtraUpdate()) {
                return;
            }
            Player player = Main.player[Projectile.owner];
            if (Projectile.owner == Main.myPlayer && ShouldDrop(player)) {
                Projectile.Kill();
                return;
            }

            Vector2 center = player.GetDrawCenter();
            Vector2 aim = (player.mouseWorld() - center).SafeNormalize(Vector2.UnitX * player.direction);
            int dir = aim.X < 0f ? -1 : 1;
            Projectile.rotation = aim.ToRotation();
            Projectile.velocity = aim;
            Projectile.direction = dir;
            Projectile.spriteDirection = dir;
            player.ChangeDir(dir);
            player.heldProj = Projectile.whoAmI;

            float counter = Counter;
            float releaseAt = ReleaseFrame;
            bool release = lastCounter < releaseAt && counter >= releaseAt;
            UpdatePose(counter, releaseAt);
            lastCounter = counter;
            Counter = counter + 1f;

            Hold(player, center, aim, dir);
            if (release) {
                Release(player, aim);
            }
            if (nocked && Giant) {
                SandstormBowVfx.GatherSand(Projectile.Center + aim * (StringAnchorX + nockShift), aim);
            }
        }

        private bool ShouldDrop(Player player) {
            if (!player.active || player.dead || player.CCed || player.noItems) {
                return true;
            }
            if (player.HeldItem.type != ModContent.ItemType<SandstormBow>()) {
                return true;
            }
            return Counter > Cycle + LingerFrames;
        }

        /// <summary>按上弦后已过帧数算本帧姿态,不累积状态</summary>
        private void UpdatePose(float counter, float releaseAt) {
            if (counter < releaseAt) {
                float left = 1f - counter / releaseAt;
                pull = 1f - left * left * left;
                nockShift = -StringPull * pull;
                handPull = StringPull * pull;
                kick = 0f;
                nocked = true;
                return;
            }
            float since = counter - releaseAt;
            float tail = Cycle - releaseAt;
            pull = 0f;
            nocked = false;
            nockShift = StringVibeAmplitude * MathF.Pow(StringVibeDecay, since) * MathF.Cos(since * StringVibeFrequency);
            //手松弦后停在满弦位,再缓缓回到弦上
            handPull = StringPull * (1f - MathHelper.SmoothStep(0f, 1f, since / Math.Max(tail * HandReturnFraction, HandReturnMin)));
            //后坐先冲到峰值再衰减,phase 为 1 时正好到峰
            float phase = since / Math.Max(tail * KickRiseFraction, KickRiseMin);
            kick = (Giant ? GiantKickPeak : KickPeak) * phase * MathF.Exp(1f - phase);
        }

        private void Hold(Player player, Vector2 center, Vector2 aim, int dir) {
            float gravDir = player.gravDir;
            Vector2 backShoulder = center + new Vector2(BackShoulderX * dir, ShoulderY * gravDir);
            Vector2 grip = backShoulder + new Vector2(aim.X * BackArmReachX, aim.Y * BackArmReachY);
            Projectile.Center = grip - aim * kick;
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, ArmRotation(aim, gravDir));

            Vector2 normal = aim.RotatedBy(MathHelper.PiOver2);
            Vector2 hand = grip + aim * (StringAnchorX - handPull) + normal * (HandDrop * dir * gravDir);
            Vector2 toHand = hand - (center + new Vector2(FrontShoulderX * dir, ShoulderY * gravDir));
            player.SetCompositeArmFront(true, StretchFor(toHand.Length()), ArmRotation(toHand, gravDir));
        }

        //合成手臂的转角按正立姿态给,反重力时原版会再取反一次,所以先把纵轴翻回去
        private static float ArmRotation(Vector2 direction, float gravDir) {
            return new Vector2(direction.X, direction.Y * gravDir).ToRotation() - MathHelper.PiOver2;
        }

        private static Player.CompositeArmStretchAmount StretchFor(float reach) {
            if (reach < (ArmReachNone + ArmReachQuarter) * 0.5f) {
                return Player.CompositeArmStretchAmount.None;
            }
            if (reach < (ArmReachQuarter + ArmReachThreeQuarters) * 0.5f) {
                return Player.CompositeArmStretchAmount.Quarter;
            }
            if (reach < (ArmReachThreeQuarters + ArmReachFull) * 0.5f) {
                return Player.CompositeArmStretchAmount.ThreeQuarters;
            }
            return Player.CompositeArmStretchAmount.Full;
        }

        /// <summary>放箭。声音与沙尘各端都播,箭只由拥有者生成</summary>
        private void Release(Player player, Vector2 aim) {
            bool giant = Giant;
            SandstormBowVfx.Release(Projectile.Center, aim, giant);
            if (Projectile.owner != Main.myPlayer || shotSource == null) {
                return;
            }
            Vector2 muzzle = Projectile.Center + aim * (giant ? GiantHeadForward : 0f);
            //出箭点被物块挡住时退回玩家中心,免得箭生在墙里
            Vector2 origin = Collision.CanHitLine(player.MountedCenter, 0, 0, muzzle, 0, 0) ? muzzle : player.MountedCenter;
            SandstormBow.FireVolley(shotSource, player, origin, aim, shotSpeed, ArrowType, Projectile.damage, Projectile.knockBack);
        }

        public override bool? CanDamage() {
            return false;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float rotation = Projectile.rotation;
            Vector2 aim = rotation.ToRotationVector2();
            Vector2 normal = aim.RotatedBy(MathHelper.PiOver2);
            Vector2 grip = Projectile.Center;
            Vector2 flex = new Vector2(1f + LimbFlexX * pull, 1f - LimbFlexY * pull) * Projectile.scale;

            Vector2 anchorBack = aim * (StringAnchorX * flex.X);
            Vector2 anchorSide = normal * (StringAnchorY * flex.Y);
            Vector2 nock = grip + anchorBack + aim * nockShift;
            Color stringColor = lightColor.MultiplyRGB(StringColor);
            CEUtils.drawLine(grip + anchorBack - anchorSide, nock, stringColor, StringWidth, StringOverlap);
            CEUtils.drawLine(grip + anchorBack + anchorSide, nock, stringColor, StringWidth, StringOverlap);

            //弓身分上、中、下三段画,中段只取握把一侧,跳过贴图里烘进去的那根弦
            bool flip = Projectile.direction < 0;
            DrawBowPiece(texture, new Rectangle(0, 0, texture.Width, BakedStringTop), grip, rotation, flex, flip, lightColor);
            DrawBowPiece(texture, new Rectangle(BakedStringRight, BakedStringTop, texture.Width - BakedStringRight, BakedStringBottom - BakedStringTop), grip, rotation, flex, flip, lightColor);
            DrawBowPiece(texture, new Rectangle(0, BakedStringBottom, texture.Width, texture.Height - BakedStringBottom), grip, rotation, flex, flip, lightColor);

            if (nocked) {
                DrawNockedArrows(nock, normal, lightColor);
            }
            return false;
        }

        private static void DrawBowPiece(Texture2D texture, Rectangle piece, Vector2 grip, float rotation, Vector2 scale, bool flip, Color color) {
            //竖直翻转时各段在整图里上下对调,原点的纵坐标跟着从段底量起
            Vector2 origin = new Vector2(GripX - piece.X, flip ? piece.Bottom - GripY : GripY - piece.Y);
            Main.EntitySpriteDraw(texture, grip - Main.screenPosition, piece, color, rotation, origin, scale, flip ? SpriteEffects.FlipVertically : SpriteEffects.None);
        }

        private void DrawNockedArrows(Vector2 nock, Vector2 normal, Color color) {
            int arrowType = ArrowType;
            if (arrowType <= ProjectileID.None || arrowType >= TextureAssets.Projectile.Length) {
                return;
            }
            if (arrowType < ProjectileID.Count) {
                Main.instance.LoadProjectile(arrowType);
            }
            Texture2D texture = TextureAssets.Projectile[arrowType].Value;
            Rectangle frame = texture.Frame(1, Math.Max(1, Main.projFrames[arrowType]));
            Vector2 origin = new Vector2(frame.Width * 0.5f, frame.Height - NockInset);
            bool giant = Giant;
            float offset = giant ? SandstormBow.GiantPairOffset : SandstormBow.ArrowPairOffset;
            float scale = giant ? MathHelper.Lerp(GiantNockScale, 1f, pull) : 1f;
            //箭的贴图箭头朝上,转到瞄准方向要补四分之一圈
            float rotation = Projectile.rotation + MathHelper.PiOver2;
            for (int i = 0; i < SandstormBow.ArrowsPerShot; i++) {
                Vector2 position = nock + normal * (offset * (2 * i - (SandstormBow.ArrowsPerShot - 1)));
                Main.EntitySpriteDraw(texture, position - Main.screenPosition, frame, color, rotation, origin, scale * Projectile.scale, SpriteEffects.None);
            }
        }
    }
}
