using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Starpiercer
{
    /// <summary>
    /// 穿天星点的持握弹幕,由物品的 Shoot 在拥有者端生成。按住拉皮筋蓄力,松开时蓄满就射出天体弹丸,没蓄满什么都不发
    /// ai[0] 拿出后已过帧数,每个真实帧加一;ai[1] 松手那一帧的 ai[0],0 为仍在蓄力;ai[2] 模式,生成时写入
    /// 姿态只由 ai 算出,各端同帧同形;松手、射出与消失只由拥有者裁决并同步
    /// 脉冲星的蓄力乘攻速;引力星不受豆奶影响,蓄力不乘攻速,朋友盒的复制品在引力星模式下一出现就收起
    /// </summary>
    public class StarpiercerHeld : ModProjectile
    {
        private const int KeepAliveFrames = 3;          //每帧续上的 timeLeft,消失只由拥有者裁决
        private const int ItemLockFrames = 3;           //每帧续上的 itemTime 与 itemAnimation
        private const float GravitonChargeFrames = 180f;    //引力星蓄满所需帧数,3 秒
        private const float LingerFrames = 6f;          //射出后弹弓留在手里的帧数,期间不能再拉
        private const float KickPeak = 4f;              //射出时弹弓的后坐峰值,像素
        private const float KickRiseFrames = 2f;        //后坐冲到峰值的帧数
        private const float SnapAmplitude = 10f;        //松手瞬间皮兜弹过叉尖的距离,像素
        private const float SnapDecay = 0.6f;           //皮兜晃动幅度每帧保留的比例
        private const float SnapFrequency = 2.2f;       //皮兜晃动角频率,弧度每帧
        private const float HandDrop = 2f;              //拉皮筋的手落在皮兜下方的距离,像素
        private const float BandWidth = 2f;             //皮筋粗细,像素,同物品图里的一格
        private const int BandOverlap = 1;              //两段皮筋在皮兜处多画的长度,像素,补住折角的缺口
        //以下是在 StarpiercerHeld.png(54×40,叉口朝 +X)上量出的像素坐标,除绘制原点外都相对握把中心
        private const float GripX = 10f;                //握把中心,绘制原点,也是持弓手落点
        private const float GripY = 20f;
        private const float TipX = 39f;                 //上下两根叉尖,皮筋挂点;皮兜静止时就在两尖中间
        private const float TipY = 15f;
        private const float PouchFullX = -6f;           //满弦时皮兜的位置,已拉过握把
        //以下取自原版 Player.GetBackHandPosition 与 GetFrontHandPosition,同金弓银弹的持弓弹幕
        private const float BackShoulderX = 6f;         //后肩相对绘制中心,像素,朝向为正
        private const float FrontShoulderX = -4f;       //前肩相对绘制中心,像素
        private const float ShoulderY = -2f;
        private const float BackArmReachX = 10f;        //后臂全伸的臂长,横纵两轴,像素
        private const float BackArmReachY = 12f;
        private const float ArmReachNone = 4f;          //前臂四档伸展的臂长,像素
        private const float ArmReachQuarter = 6f;
        private const float ArmReachThreeQuarters = 8f;
        private const float ArmReachFull = 10f;
        //蓄力光与粉尘,各端都画
        private const float GlowMinSize = 10f;          //皮兜处光晕直径,像素,随蓄力变大
        private const float GlowMaxSize = 44f;
        private const float GlowOpacity = 0.7f;         //蓄满时光晕不透明度
        private const float GlowPulse = 0.15f;          //蓄满后光晕呼吸的幅度,比例
        private const float GlowPulseSpeed = 0.25f;     //光晕呼吸角频率,弧度每帧
        private const float ChargeDustChance = 0.5f;    //蓄力时每帧出一颗粉尘的概率
        private const float ChargeDustReach = 28f;      //粉尘生成处离皮兜的距离,像素
        private const float ChargeDustPull = 0.12f;     //粉尘初速取离皮兜距离的比例,朝皮兜
        private const float ChargeDustSwirl = 0.6f;     //粉尘绕皮兜旋进的偏角,弧度
        private const float ChargeDustScaleMin = 0.6f;  //粉尘缩放,随蓄力从下限涨到上限
        private const float ChargeDustScaleMax = 1.1f;
        //声音
        private const float CuePitch = 1.1f;            //蓄满提示音音高,CEUtils.PlaySound 以 1 为原调
        private const float CueVolume = 0.8f;
        private const int CueInstances = 2;
        private const float PulsarSnapPitch = 0.3f;     //松弦声的音高偏移,原版弓声
        private const float GravitonSnapPitch = -0.3f;
        private const float PulsarLaunchPitch = 1.2f;   //脉冲星出膛声音高
        private const float PulsarLaunchVolume = 0.5f;
        private const int LaunchInstances = 2;
        private static readonly Color BandColor = new Color(170, 204, 255);     //皮筋,同物品图与金弓银弹的弦
        private static readonly Color PulsarGlow = new Color(170, 204, 255);
        private static readonly Color GravitonGlow = new Color(255, 194, 11);

        private float pouchX;       //皮兜相对握把中心沿瞄准轴的位置,像素
        private float handX;        //拉皮筋的手相对握把中心沿瞄准轴的位置,像素
        private float kick;         //弹弓后坐,像素
        private float charge;       //0-1 蓄力进度
        private bool cuePlayed;     //各端各记,蓄满提示音是否已播
        private bool snapPlayed;    //各端各记,松弦声是否已播

        private ref float Counter => ref Projectile.ai[0];
        private ref float Marker => ref Projectile.ai[1];
        private bool Graviton => Projectile.ai[2] == Starpiercer.ModeGraviton;
        private bool IsCopy => Projectile.Entropy().IndexOfTwistedTwinShootedThisProj >= 0;

        public override void SetDefaults() {
            Projectile.HeldProjSetDefaults(DamageClass.Ranged);
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.noEnchantmentVisuals = true;
            Projectile.timeLeft = KeepAliveFrames;
        }

        public override void AI() {
            Projectile.timeLeft = KeepAliveFrames;
            //更新次数被豆奶翻倍时仍按真实帧推进
            if (!Projectile.FinalExtraUpdate()) {
                return;
            }
            Player player = Main.player[Projectile.owner];
            bool owner = Projectile.owner == Main.myPlayer;
            bool copy = IsCopy;
            if (owner && (ShouldDrop(player) || copy && (Graviton || TwinGone()))) {
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
            Projectile.hide = !copy;
            UpdatePose(player);
            if (copy) {
                Projectile.Center = center + aim * (BackArmReachX - kick);
            }
            else {
                player.itemTime = ItemLockFrames;
                player.itemAnimation = ItemLockFrames;
                player.ChangeDir(dir);
                player.heldProj = Projectile.whoAmI;
                Hold(player, center, aim, dir);
            }
            Cues(player);
            if (owner) {
                Decide(player, aim);
            }
            Counter++;
        }

        private static bool ShouldDrop(Player player) {
            return !player.active || player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<Starpiercer>();
        }

        private bool TwinGone() {
            Projectile twin = Projectile.Entropy().IndexOfTwistedTwinShootedThisProj.ToProj_Identity();
            return twin == null || !twin.active;
        }

        //脉冲星的 45 帧乘攻速;引力星固定 3 秒
        private float ChargeNeeded(Player player) {
            return Graviton ? GravitonChargeFrames : CombinedHooks.TotalUseTime(Starpiercer.UseFrames, player, player.HeldItem);
        }

        //拉皮筋先快后慢
        private static float EaseOut(float x) {
            float left = 1f - x;
            return 1f - left * left * left;
        }

        private void UpdatePose(Player player) {
            float released = Marker;
            if (released <= 0f) {
                charge = Math.Min(Counter / ChargeNeeded(player), 1f);
                pouchX = MathHelper.Lerp(TipX, PouchFullX, EaseOut(charge));
                handX = pouchX;
                kick = 0f;
                return;
            }
            //松手后皮兜弹过叉尖再来回晃,手停在松开处;后坐先冲到峰值再衰减
            float since = Counter - released;
            charge = 0f;
            pouchX = TipX + SnapAmplitude * MathF.Pow(SnapDecay, since) * MathF.Cos(since * SnapFrequency);
            handX = PouchFullX;
            float phase = since / KickRiseFrames;
            kick = KickPeak * phase * MathF.Exp(1f - phase);
        }

        private void Cues(Player player) {
            if (Main.dedServ) {
                return;
            }
            if (Marker <= 0f) {
                if (charge >= 1f && !cuePlayed) {
                    cuePlayed = true;
                    CEUtils.PlaySound("BaitReady", CuePitch, Projectile.Center, CueInstances, CueVolume);
                }
                if (Main.rand.NextFloat() < ChargeDustChance) {
                    Vector2 pouch = PartPosition(pouchX, 0f);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(ChargeDustReach, ChargeDustReach);
                    Dust dust = Dust.NewDustPerfect(pouch + offset, Graviton ? DustID.GoldFlame : DustID.Electric, (-offset * ChargeDustPull).RotatedBy(ChargeDustSwirl));
                    dust.noGravity = true;
                    dust.scale = MathHelper.Lerp(ChargeDustScaleMin, ChargeDustScaleMax, charge);
                }
                return;
            }
            if (!snapPlayed) {
                snapPlayed = true;
                SoundEngine.PlaySound(SoundID.Item5 with { Pitch = Graviton ? GravitonSnapPitch : PulsarSnapPitch }, Projectile.Center);
                if (!Graviton) {
                    CEUtils.PlaySound("pulseBlast", PulsarLaunchPitch, Projectile.Center, LaunchInstances, PulsarLaunchVolume);
                }
            }
        }

        private void Decide(Player player, Vector2 aim) {
            float released = Marker;
            if (released > 0f) {
                if (Counter - released >= LingerFrames) {
                    Projectile.Kill();
                }
                return;
            }
            if (player.channel) {
                return;
            }
            if (Counter < ChargeNeeded(player)) {
                Projectile.Kill();
                return;
            }
            Marker = Math.Max(Counter, 1f);
            Projectile.netUpdate = true;
            Vector2 muzzle = PartPosition(TipX, 0f);
            //出膛点被物块挡住时退回玩家中心,免得弹丸生在墙里
            if (!Collision.CanHitLine(player.MountedCenter, 0, 0, muzzle, 0, 0)) {
                muzzle = player.MountedCenter;
            }
            if (Graviton) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), muzzle, aim * StarpiercerGraviton.LaunchSpeed, ModContent.ProjectileType<StarpiercerGraviton>(), 0, 0f, Projectile.owner);
                return;
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), muzzle, aim * StarpiercerPulsar.Speed, ModContent.ProjectileType<StarpiercerPulsar>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }

        //贴图上相对握把中心的一点换到世界坐标;朝左时贴图竖直翻转,纵向偏移跟着反号
        private Vector2 PartPosition(float x, float y) {
            Vector2 aim = Projectile.rotation.ToRotationVector2();
            return Projectile.Center + (aim * x + aim.RotatedBy(MathHelper.PiOver2) * (y * Projectile.direction)) * Projectile.scale;
        }

        private void Hold(Player player, Vector2 center, Vector2 aim, int dir) {
            float gravDir = player.gravDir;
            Vector2 backShoulder = center + new Vector2(BackShoulderX * dir, ShoulderY * gravDir);
            Vector2 grip = backShoulder + new Vector2(aim.X * BackArmReachX, aim.Y * BackArmReachY);
            Projectile.Center = grip - aim * kick;
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, ArmRotation(aim, gravDir));

            Vector2 normal = aim.RotatedBy(MathHelper.PiOver2);
            Vector2 hand = grip + aim * handX + normal * (HandDrop * dir * gravDir);
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
            if (IsCopy) {
                return;
            }
            Player player = Main.player[Projectile.owner];
            player.itemTime = 0;
            player.itemAnimation = 0;
        }

        public override bool? CanDamage() {
            return false;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            //朝左时竖直翻转,原点纵坐标跟着从底边量起
            bool flip = Projectile.direction < 0;
            Vector2 origin = new Vector2(GripX, flip ? texture.Height - GripY : GripY);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, flip ? SpriteEffects.FlipVertically : SpriteEffects.None);

            Vector2 pouch = PartPosition(pouchX, 0f);
            Color bandColor = lightColor.MultiplyRGB(BandColor);
            float width = BandWidth * Projectile.scale;
            CEUtils.drawLine(PartPosition(TipX, -TipY), pouch, bandColor, width, BandOverlap);
            CEUtils.drawLine(PartPosition(TipX, TipY), pouch, bandColor, width, BandOverlap);
            if (Marker <= 0f) {
                DrawNocked(pouch, lightColor);
            }
            return false;
        }

        private void DrawNocked(Vector2 pouch, Color lightColor) {
            bool graviton = Graviton;
            //蓄力光用 A=0 的颜色在默认批次里叠加,不切批次
            float size = MathHelper.Lerp(GlowMinSize, GlowMaxSize, charge);
            if (charge >= 1f) {
                size *= 1f + GlowPulse * MathF.Sin(Counter * GlowPulseSpeed);
            }
            Texture2D glow = CEExtraAssets.Glow2;
            Color glowColor = (graviton ? GravitonGlow : PulsarGlow) with { A = 0 };
            Main.EntitySpriteDraw(glow, pouch - Main.screenPosition, null, glowColor * (GlowOpacity * charge), 0f, glow.Size() * 0.5f, size * Projectile.scale / glow.Width, SpriteEffects.None);

            int type = graviton ? ModContent.ProjectileType<StarpiercerGraviton>() : ModContent.ProjectileType<StarpiercerPulsar>();
            Texture2D pellet = TextureAssets.Projectile[type].Value;
            Color pelletColor = Color.Lerp(lightColor, Color.White, charge);
            Main.EntitySpriteDraw(pellet, pouch - Main.screenPosition, null, pelletColor, Projectile.rotation, pellet.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
        }
    }
}
