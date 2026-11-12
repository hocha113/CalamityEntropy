using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.GoldbowSilvershot
{
    /// <summary>
    /// 金弓银弹的持弓弹幕,由物品的 Shoot 在拥有者端生成,弓在手里时物品不能再用
    /// ai[0] 拿出后已过帧数;ai[1] 穿山碎峦时是松手那一帧的 ai[0](0 为仍在蓄力),梨花暴雨时是已射出的轮数;ai[2] 模式,生成时写入
    /// 姿态只由 ai 算出,各端同帧同形;松手、射击与消失只由拥有者裁决并同步
    /// </summary>
    public class GoldbowSilvershotHeld : ModProjectile
    {
        public const float ModeGold = 0f;               //ai[2] 取值: 穿山碎峦
        public const float ModeSilver = 1f;             //ai[2] 取值: 梨花暴雨

        private const int KeepAliveFrames = 3;          //每帧续上的 timeLeft,消失只由拥有者裁决
        private const int ItemLockFrames = 3;           //每帧续上的 itemTime 与 itemAnimation
        //穿山碎峦
        private const float MaxChargeFrames = 150f;     //蓄满所需帧数,2.5 秒
        private const float MinChargeFrames = 18f;      //不足此帧数松手按最低档发射,0.3 秒
        private const float MaxDamageMultiplier = 12f;  //蓄满时金弹的伤害倍率
        private const int MaxTargets = 10;              //蓄满时金弹可命中的目标数
        private const float GoldLingerFrames = 12f;     //金弹射出后弓留在手里的帧数,期间不能再拉弓
        private const float GoldMuzzleForward = 8f;     //金弹出膛点在握把前方的距离,像素
        private const float GoldKickPeak = 6f;          //满蓄金弹的后坐峰值,像素
        private const float GoldMinKick = 0.35f;        //最低档金弹的后坐占峰值的比例
        private const float FullChargeCuePitch = 1.1f;  //蓄满提示音音高,1 为原调
        private const float FullChargeCueVolume = 0.8f; //蓄满提示音音量
        private const int FullChargeCueInstances = 2;   //蓄满提示音同时最多几个
        //瞄准线与准星,只画给拥有者
        private const float AimLineMinLength = 160f;    //瞄准线最短长度,像素,鼠标更近时也画这么长
        private const float AimLineStep = 8f;           //瞄准线探墙的步长,像素
        private const float AimLineMinWidth = 1f;       //瞄准线粗细,像素,随蓄力加粗
        private const float AimLineMaxWidth = 3f;
        private const float AimLineMinOpacity = 0.2f;   //瞄准线不透明度,随蓄力变亮
        private const float AimLineMaxOpacity = 0.8f;
        private const float CrosshairCloseFrames = 12f; //蓄满后准星收拢所需帧数
        private const float CrosshairOpenRadius = 30f;  //准星四根短线收拢前离中心的距离,像素
        private const float CrosshairClosedRadius = 8f; //收拢后的距离,像素
        private const float CrosshairTickLength = 8f;   //准星每根短线的长度,像素
        private const float CrosshairTickWidth = 2f;    //准星短线粗细,像素
        private const float CrosshairPulse = 1.5f;      //收拢后准星呼吸的幅度,像素
        private const float CrosshairPulseSpeed = 0.2f; //准星呼吸角频率,弧度每帧
        //梨花暴雨
        private const float SilverIntervalFrames = 60f; //两轮银弹的间隔,帧
        private const float SilverDrawFrames = 12f;     //每轮放箭前拉弦的帧数,第一轮在拿出后这么多帧射出
        private const float SilverLingerFrames = 10f;   //松手后弓至少留到上一轮射出后这么多帧
        private const int SilverPellets = 10;           //每轮银弹颗数
        private const float SilverHalfSpread = MathHelper.Pi / 9f;  //散射半角,弧度,即 ±20°
        private const float SilverSpeedJitter = 0.15f;  //银弹初速的随机浮动比例
        private const float SilverMuzzleForward = 6f;   //银弹出膛点在握把前方的距离,像素
        private const float SilverKickPeak = 3f;        //一轮银弹的后坐峰值,像素
        private const int SilverNockCount = 3;          //拉弦时搭在弦上画出的银弹数
        private const float SilverNockGap = 5f;         //搭在弦上的银弹间距,像素
        //拉弦与后坐
        private const float StringPull = 7f;            //满弦时箭扣后拉的距离,像素,再大前手就够不着弦
        private const float StringVibeAmplitude = 4f;   //放箭瞬间弦的前冲幅度,像素
        private const float StringVibeDecay = 0.72f;    //弦颤幅度每帧保留的比例
        private const float StringVibeFrequency = 2.4f; //弦颤角频率,弧度每帧
        private const float KickRiseFrames = 2f;        //后坐冲到峰值的帧数
        private const float HandReturnFrames = 8f;      //拉弦手松弦后回到弦上的帧数
        private const float HandDrop = 3f;              //拉弦手落在箭下方的距离,像素
        private const float LimbFlexX = 0.02f;          //满弦时弓身沿瞄准轴拉长的比例
        private const float LimbFlexY = 0.04f;          //满弦时上下弓臂向内收的比例
        private const float StringWidth = 2f;           //弦的粗细,像素,同物品图里的一格
        private const int StringOverlap = 1;            //两段弦在箭扣处多画的长度,像素,补住折角的缺口
        private const float NockInset = 2f;             //弹丸压进弦的深度,像素
        //以下是在 GoldbowSilvershotHeld.png(34×86,无弦,朝 +X 发射)上量出的像素坐标
        private const float GripX = 24f;                //握把中心,绘制原点,也是后手落点
        private const float GripY = 43f;
        private const float StringAnchorX = -23f;       //弦的上下挂点相对握把,物品图里的弦在 (1,8) 到 (1,78)
        private const float StringAnchorY = 35f;
        //以下取自原版 Player.GetBackHandPosition 与 GetFrontHandPosition,同 SandstormBowHoldout
        private const float BackShoulderX = 6f;         //后肩相对绘制中心,像素,朝向为正
        private const float FrontShoulderX = -4f;       //前肩相对绘制中心,像素
        private const float ShoulderY = -2f;
        private const float BackArmReachX = 10f;        //后臂全伸的臂长,横纵两轴,像素
        private const float BackArmReachY = 12f;
        private const float ArmReachNone = 4f;          //前臂四档伸展的臂长,像素
        private const float ArmReachQuarter = 6f;
        private const float ArmReachThreeQuarters = 8f;
        private const float ArmReachFull = 10f;
        //蓄力光,各端都画
        private const float GlowMinSize = 10f;          //弦上弹丸处光晕直径,像素,随蓄力变大
        private const float GlowMaxSize = 40f;
        private const float GlowOpacity = 0.6f;         //蓄满时光晕不透明度
        private const float GlowPulse = 0.15f;          //蓄满后光晕呼吸的幅度,比例
        private const float GlowPulseSpeed = 0.25f;     //光晕呼吸角频率,弧度每帧
        private static readonly Color StringColor = new Color(170, 204, 255);   //弦色,同物品图
        private static readonly Color GoldColor = new Color(255, 206, 92);      //瞄准线、准星与金弹蓄力光
        private static readonly Color SilverColor = new Color(190, 215, 255);   //银弹拉弦时的光

        private float pull;         //0-1 拉弦进度
        private float nockShift;    //箭扣相对静止弦位沿瞄准轴的位移,像素,负为向后
        private float handPull;     //拉弦手相对静止弦位后撤的距离,像素
        private float kick;         //弓身后坐,像素
        private bool nocked;        //弦上是否搭着弹丸
        private bool cuePlayed;     //各端各记,蓄满提示音是否已播

        private ref float Counter => ref Projectile.ai[0];
        private ref float Marker => ref Projectile.ai[1];
        private bool Silver => Projectile.ai[2] == ModeSilver;

        public override void SetDefaults() {
            Projectile.HeldProjSetDefaults(DamageClass.Ranged);
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.noEnchantmentVisuals = true;
            Projectile.timeLeft = KeepAliveFrames;
        }

        public override void AI() {
            Projectile.timeLeft = KeepAliveFrames;
            //更新次数被外力翻倍时仍按帧推进
            if (!Projectile.FinalExtraUpdate()) {
                return;
            }
            Player player = Main.player[Projectile.owner];
            if (Projectile.owner == Main.myPlayer && ShouldDrop(player)) {
                Projectile.Kill();
                return;
            }
            player.itemTime = ItemLockFrames;
            player.itemAnimation = ItemLockFrames;

            Vector2 center = player.GetDrawCenter();
            Vector2 aim = (player.mouseWorld() - center).SafeNormalize(Vector2.UnitX * player.direction);
            int dir = aim.X < 0f ? -1 : 1;
            Projectile.rotation = aim.ToRotation();
            Projectile.velocity = aim;
            Projectile.direction = dir;
            Projectile.spriteDirection = dir;
            player.ChangeDir(dir);
            player.heldProj = Projectile.whoAmI;

            if (Silver) {
                UpdateSilverPose();
            }
            else {
                UpdateGoldPose();
            }
            Hold(player, center, aim, dir);
            if (!Silver && Marker <= 0f && Counter >= MaxChargeFrames && !cuePlayed) {
                cuePlayed = true;
                CEUtils.PlaySound("BaitReady", FullChargeCuePitch, Projectile.Center, FullChargeCueInstances, FullChargeCueVolume);
            }
            if (Projectile.owner == Main.myPlayer) {
                if (Silver) {
                    SilverDecide(player, aim);
                }
                else {
                    GoldDecide(player, aim);
                }
            }
            Counter++;
        }

        private static bool ShouldDrop(Player player) {
            return !player.active || player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<GoldbowSilvershot>();
        }

        private static float ChargeOf(float frames) => Math.Min(frames / MaxChargeFrames, 1f);

        //不足最低帧数松手一律按最低档
        private static float TierOf(float frames) => frames < MinChargeFrames ? 0f : ChargeOf(frames);

        //拉弦先快后慢
        private static float EaseOut(float x) {
            float left = 1f - x;
            return 1f - left * left * left;
        }

        private void UpdateGoldPose() {
            float released = Marker;
            if (released <= 0f) {
                SetDrawingPose(EaseOut(ChargeOf(Counter)));
                return;
            }
            float kickPeak = GoldKickPeak * MathHelper.Lerp(GoldMinKick, 1f, TierOf(released));
            SetReleasedPose(Counter - released, EaseOut(ChargeOf(released)), kickPeak);
        }

        private void UpdateSilverPose() {
            //第 k 轮在 k*间隔 开始拉弦,拉满即射出
            float fired = Marker;
            float drawStart = fired * SilverIntervalFrames;
            if (fired < 1f || Counter >= drawStart) {
                SetDrawingPose(EaseOut(MathHelper.Clamp((Counter - drawStart) / SilverDrawFrames, 0f, 1f)));
                return;
            }
            float lastFire = drawStart - SilverIntervalFrames + SilverDrawFrames;
            SetReleasedPose(Counter - lastFire, 1f, SilverKickPeak);
        }

        private void SetDrawingPose(float progress) {
            pull = progress;
            nockShift = -StringPull * progress;
            handPull = StringPull * progress;
            kick = 0f;
            nocked = true;
        }

        private void SetReleasedPose(float since, float pullAtRelease, float kickPeak) {
            pull = 0f;
            nocked = false;
            nockShift = StringVibeAmplitude * MathF.Pow(StringVibeDecay, since) * MathF.Cos(since * StringVibeFrequency);
            //手松弦后停在松开处,再缓缓回到弦上
            handPull = StringPull * pullAtRelease * (1f - MathHelper.SmoothStep(0f, 1f, since / HandReturnFrames));
            //后坐先冲到峰值再衰减,phase 为 1 时正好到峰
            float phase = since / KickRiseFrames;
            kick = kickPeak * phase * MathF.Exp(1f - phase);
        }

        private void GoldDecide(Player player, Vector2 aim) {
            float released = Marker;
            if (released > 0f) {
                if (Counter - released >= GoldLingerFrames) {
                    Projectile.Kill();
                }
                return;
            }
            if (player.channel) {
                return;
            }
            Marker = Math.Max(Counter, 1f);
            Projectile.netUpdate = true;
            //箭在使用物品时已扣,伤害用生成时带来的武器加弹药伤害
            float tier = TierOf(Counter);
            int damage = (int)(Projectile.damage * MathHelper.Lerp(1f, MaxDamageMultiplier, tier));
            int targets = 1 + (int)((MaxTargets - 1) * tier);
            Vector2 muzzle = MuzzleOf(player, aim, GoldMuzzleForward);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), muzzle, aim * GoldbowSilvershotGoldShot.Speed, ModContent.ProjectileType<GoldbowSilvershotGoldShot>(), damage, Projectile.knockBack, Projectile.owner, targets, tier);
        }

        private void SilverDecide(Player player, Vector2 aim) {
            float fired = Marker;
            float nextFire = fired * SilverIntervalFrames + SilverDrawFrames;
            if (Counter >= nextFire) {
                if (!FireSilverRound(player, aim, fired >= 1f)) {
                    Projectile.Kill();
                    return;
                }
                Marker = fired + 1f;
                Projectile.netUpdate = true;
                return;
            }
            //第一轮的箭在使用物品时已扣,松手也照射;之后松手就收弓,至少让上一轮的弦颤播完
            if (!player.channel && fired >= 1f && Counter >= nextFire - SilverIntervalFrames + SilverLingerFrames) {
                Projectile.Kill();
            }
        }

        /// <summary>射一轮银弹。第一轮的箭在使用物品时已扣,之后每轮扣一支,没箭了返回 false</summary>
        private bool FireSilverRound(Player player, Vector2 aim, bool consume) {
            int damage = Projectile.damage;
            float knockback = Projectile.knockBack;
            if (consume && !player.PickAmmo(player.HeldItem, out _, out _, out damage, out knockback, out _)) {
                return false;
            }
            Vector2 muzzle = MuzzleOf(player, aim, SilverMuzzleForward);
            IEntitySource source = Projectile.GetSource_FromThis();
            int type = ModContent.ProjectileType<GoldbowSilvershotSilverShot>();
            for (int i = 0; i < SilverPellets; i++) {
                float speed = GoldbowSilvershotSilverShot.Speed * (1f + Main.rand.NextFloat(-SilverSpeedJitter, SilverSpeedJitter));
                Vector2 velocity = aim.RotatedBy(Main.rand.NextFloat(-SilverHalfSpread, SilverHalfSpread)) * speed;
                Projectile.NewProjectile(source, muzzle, velocity, type, damage, knockback, Projectile.owner, i);
            }
            return true;
        }

        private Vector2 MuzzleOf(Player player, Vector2 aim, float forward) {
            Vector2 muzzle = Projectile.Center + aim * forward;
            //出膛点被物块挡住时退回玩家中心,免得弹丸生在墙里
            return Collision.CanHitLine(player.MountedCenter, 0, 0, muzzle, 0, 0) ? muzzle : player.MountedCenter;
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

        public override void OnKill(int timeLeft) {
            Player player = Main.player[Projectile.owner];
            player.itemTime = 0;
            player.itemAnimation = 0;
            if (Projectile.owner != Main.myPlayer || !Silver || Marker < 1f) {
                return;
            }
            //下一轮本该开始拉弦之前不许再拿出弓,点按也守住每秒一轮
            GoldbowSilvershotPlayer modPlayer = player.GetModPlayer<GoldbowSilvershotPlayer>();
            modPlayer.Cooldown = Math.Max(modPlayer.Cooldown, (int)(Marker * SilverIntervalFrames - Counter));
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

            if (!Silver && Marker <= 0f && Projectile.owner == Main.myPlayer) {
                DrawAimLine(grip + aim * GoldMuzzleForward, aim);
            }

            Color stringColor = lightColor.MultiplyRGB(StringColor);
            CEUtils.drawLine(grip + anchorBack - anchorSide, nock, stringColor, StringWidth, StringOverlap);
            CEUtils.drawLine(grip + anchorBack + anchorSide, nock, stringColor, StringWidth, StringOverlap);

            //朝左时竖直翻转,上弓臂的高光始终在上;原点纵坐标跟着从底边量起
            bool flip = Projectile.direction < 0;
            Vector2 origin = new Vector2(GripX, flip ? texture.Height - GripY : GripY);
            Main.EntitySpriteDraw(texture, grip - Main.screenPosition, null, lightColor, rotation, origin, flex, flip ? SpriteEffects.FlipVertically : SpriteEffects.None);

            if (nocked) {
                DrawNocked(nock, aim, normal, lightColor);
            }
            return false;
        }

        private void DrawNocked(Vector2 nock, Vector2 aim, Vector2 normal, Color lightColor) {
            bool silver = Silver;
            float glow = silver ? pull : ChargeOf(Counter);
            int type = silver ? ModContent.ProjectileType<GoldbowSilvershotSilverShot>() : ModContent.ProjectileType<GoldbowSilvershotGoldShot>();
            Texture2D pellet = TextureAssets.Projectile[type].Value;
            Vector2 center = nock + aim * (pellet.Width * 0.5f - NockInset);

            //蓄力光用 A=0 的颜色在默认批次里叠加,不切批次
            float size = MathHelper.Lerp(GlowMinSize, GlowMaxSize, glow);
            if (!silver && Counter >= MaxChargeFrames) {
                size *= 1f + GlowPulse * MathF.Sin(Counter * GlowPulseSpeed);
            }
            Texture2D glowTexture = CEExtraAssets.Glow2;
            Color glowColor = (silver ? SilverColor : GoldColor) with { A = 0 };
            Main.EntitySpriteDraw(glowTexture, center - Main.screenPosition, null, glowColor * (GlowOpacity * glow), 0f, glowTexture.Size() * 0.5f, size / glowTexture.Width, SpriteEffects.None);

            Color pelletColor = Color.Lerp(lightColor, Color.White, glow);
            int count = silver ? SilverNockCount : 1;
            for (int i = 0; i < count; i++) {
                Vector2 position = center + normal * (SilverNockGap * (i - (count - 1) * 0.5f));
                Main.EntitySpriteDraw(pellet, position - Main.screenPosition, null, pelletColor, Projectile.rotation, pellet.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            }
        }

        private void DrawAimLine(Vector2 start, Vector2 aim) {
            float charge = ChargeOf(Counter);
            float wanted = Vector2.Distance(start, Main.player[Projectile.owner].mouseWorld());
            float length = MathHelper.Clamp(wanted, AimLineMinLength, GoldbowSilvershotGoldShot.Range);
            float reach = 0f;
            while (reach < length) {
                float next = Math.Min(reach + AimLineStep, length);
                if (!CEUtils.isAir(start + aim * next)) {
                    break;
                }
                reach = next;
            }
            Vector2 end = start + aim * reach;
            Color color = GoldColor with { A = 0 };
            color *= MathHelper.Lerp(AimLineMinOpacity, AimLineMaxOpacity, charge);
            CEUtils.drawLine(start, end, color, MathHelper.Lerp(AimLineMinWidth, AimLineMaxWidth, charge));
            if (Counter >= MaxChargeFrames) {
                DrawCrosshair(end, Counter - MaxChargeFrames, color);
            }
        }

        //四根短线从斜向的大圈转正并收拢成十字
        private static void DrawCrosshair(Vector2 center, float sinceFull, Color color) {
            float close = MathHelper.SmoothStep(0f, 1f, sinceFull / CrosshairCloseFrames);
            float radius = MathHelper.Lerp(CrosshairOpenRadius, CrosshairClosedRadius, close) + CrosshairPulse * close * MathF.Sin(sinceFull * CrosshairPulseSpeed);
            float spin = MathHelper.PiOver4 * (1f - close);
            for (int i = 0; i < 4; i++) {
                Vector2 direction = (MathHelper.PiOver2 * i + spin).ToRotationVector2();
                CEUtils.drawLine(center + direction * radius, center + direction * (radius + CrosshairTickLength), color, CrosshairTickWidth);
            }
        }
    }
}
