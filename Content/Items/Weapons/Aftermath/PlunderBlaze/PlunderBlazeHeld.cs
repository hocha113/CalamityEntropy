using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PlunderBlaze
{
    /// <summary>
    /// 劫掠怒焰的持握弹幕,由物品的 Shoot 在拥有者端生成,贴图借物品图。按住时每 6 帧喷一发,10 发后歇 24 帧再喷
    /// 松手后也要歇完这一下才收起,点按快不过正常节奏
    /// ai[0] 当前阶段已过帧数,ai[1] 阶段,0 喷射 1 歇息,ai[2] 本轮已喷出的发数
    /// 各端按同一计时推进姿态与特效;开火、松手与消失只由拥有者裁决并同步
    /// </summary>
    public class PlunderBlazeHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Aftermath/PlunderBlaze/PlunderBlaze";

        private const float PhaseNone = -1f;            //还没看到过任何阶段,首帧从它进喷射算点火
        private const float PhaseFiring = 0f;           //ai[1] 取值: 喷射
        private const float PhaseResting = 1f;          //ai[1] 取值: 歇息
        private const int KeepAliveFrames = 3;          //每帧续上的 timeLeft,消失只由拥有者裁决
        private const int ItemLockFrames = 3;           //每帧续上的 itemTime 与 itemAnimation
        private const int FireInterval = 6;             //两发火焰的间隔,帧
        private const int BurstShots = 10;              //一轮喷出的火焰数
        private const int RestFrames = 24;              //两轮之间的歇息,帧,0.4 秒
        private const int AmmoCheckInterval = 5;        //每几发尝试扣一次凝胶,同原版喷火器一次使用喷 5 发只扣一次
        private const float AimSpread = 0.04f;          //火焰出膛方向的随机偏转上限,弧度
        private const float SpeedJitter = 0.06f;        //火焰初速的随机浮动比例
        private const float RecoilPerShot = 2f;         //每发火焰的枪身后坐,像素
        private const float RecoilDecay = 0.7f;         //后坐每帧保留的比例
        private const float HeatRise = 0.12f;           //喷射时储罐火光每帧上升量
        private const float HeatFall = 0.04f;           //歇息时储罐火光每帧下降量
        private const float HoldForward = 10f;          //握把顶端离玩家中心的距离,像素
        private const float HoldDropY = 2f;             //握把顶端相对玩家中心的下沉,像素
        //以下是在 PlunderBlaze.png(70×32,枪口朝 +X)上量出的像素坐标,除绘制原点外都相对握把顶端
        private const float GripX = 28f;                //握把顶端,绘制原点,也是手的落点
        private const float GripY = 21f;
        private const float MuzzleX = 40f;              //枪口
        private const float MuzzleY = -4f;
        private const float TankX = -1f;                //储罐玻璃中心
        private const float TankY = -13f;
        private const float VentX = 13f;                //泄压口,开始歇息时从这里冒汽
        private const float VentY = -11f;
        private const float PilotX = 37f;               //长明火
        private const float PilotY = -14f;
        private const float ForegripX = 16f;            //枪管下沿,后手托在这里
        private const float ForegripY = 0f;
        //以下取自原版 Player.GetBackHandPosition 与 GetFrontHandPosition,同金弓银弹的持弓弹幕
        private const float BackShoulderX = 6f;         //后肩相对绘制中心,像素,朝向为正
        private const float FrontShoulderX = -4f;       //前肩相对绘制中心,像素
        private const float ShoulderY = -2f;
        //特效
        private const float TankGlowSize = 30f;         //储罐火光直径,像素
        private const float TankGlowOpacity = 0.55f;    //储罐火光满热时的不透明度
        private const float PilotGlowSize = 12f;        //长明火光晕直径,像素
        private const float PilotGlowOpacity = 0.7f;
        private const float PilotFlicker = 0.25f;       //长明火闪烁幅度,比例
        private const float PilotFlickerSpeed = 54f;    //长明火闪烁角频率,弧度每秒
        private const float MuzzleGlowSize = 26f;       //枪口火光直径,像素
        private const float MuzzleFlashDecay = 0.75f;   //枪口火光每帧保留的比例
        private const float MinVisibleOpacity = 0.01f;  //低于此不透明度的火光不再画
        private const int VentPuffs = 5;                //开始歇息时冒出的烟团数
        private const float VentPuffSpeed = 2.5f;       //烟团初速上限,像素每帧
        private const float VentPuffScaleMin = 0.35f;   //烟团缩放
        private const float VentPuffScaleMax = 0.6f;
        private const int VentPuffLifetime = 40;        //烟团寿命,帧
        private const float VentPuffOpacity = 1f;       //烟团初始不透明度
        private const int SoundInstances = 2;           //同一声音同时最多几个
        private const float NormalPitch = 1f;           //CEUtils.PlaySound 的原调
        private const float StartVolume = 0.55f;        //点火声音量
        private const float VentPitch = 1.15f;          //泄压声音高
        private const float VentVolume = 0.4f;          //泄压声音量
        private const float EndVolume = 0.55f;          //熄火声音量
        private static readonly Color TankColor = new Color(170, 80, 255);
        private static readonly Color PilotColor = new Color(254, 110, 228);
        private static readonly Color MuzzleColor = new Color(120, 130, 255);
        private static readonly Color VentSmokeColor = new Color(96, 84, 120);   //泄压烟,先白后转成这个色

        private float recoil;                   //枪身后坐,像素
        private float heat;                     //0-1 储罐火光
        private float muzzleFlash;              //0-1 枪口火光
        private float seenPhase = PhaseNone;    //各端上一帧看到的阶段,阶段一变就放对应特效
        private bool firstShotDone;             //仅拥有者端,第一发火焰用的是使用物品时扣过的凝胶

        private ref float Timer => ref Projectile.ai[0];
        private ref float Phase => ref Projectile.ai[1];
        private ref float Shots => ref Projectile.ai[2];

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
            bool owner = Projectile.owner == Main.myPlayer;
            if (owner && ShouldDrop(player)) {
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

            recoil *= RecoilDecay;
            muzzleFlash *= MuzzleFlashDecay;
            Hold(player, center, aim);
            if (!Step(player, aim, owner)) {
                return;
            }
            if (Phase != seenPhase) {
                OnPhaseEntered(seenPhase);
                seenPhase = Phase;
            }
            heat = Phase == PhaseFiring ? Math.Min(heat + HeatRise, 1f) : Math.Max(heat - HeatFall, 0f);
            Timer++;
        }

        private static bool ShouldDrop(Player player) {
            return !player.active || player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<PlunderBlaze>();
        }

        //推进喷射与歇息,返回 false 表示已被拥有者收起
        private bool Step(Player player, Vector2 aim, bool owner) {
            if (Phase == PhaseResting) {
                if (Timer < RestFrames || !owner) {
                    return true;
                }
                if (!player.channel) {
                    Projectile.Kill();
                    return false;
                }
                EnterPhase(PhaseFiring);
            }
            if (Shots >= BurstShots) {
                if (Timer >= BurstShots * FireInterval) {
                    EnterPhase(PhaseResting);
                }
                return true;
            }
            if (Timer < Shots * FireInterval) {
                return true;
            }
            if (owner) {
                //松手后不再喷,照常歇完这一下才收起
                if (!player.channel) {
                    EnterPhase(PhaseResting);
                    return true;
                }
                if (!FireFlame(player, aim)) {
                    Projectile.Kill();
                    return false;
                }
            }
            Shots++;
            recoil += RecoilPerShot;
            muzzleFlash = 1f;
            return true;
        }

        //各端都按计时走阶段,只有拥有者的结果同步出去
        private void EnterPhase(float phase) {
            Phase = phase;
            Timer = 0f;
            Shots = 0f;
            if (Projectile.owner == Main.myPlayer) {
                Projectile.netUpdate = true;
            }
        }

        /// <summary>喷一发火焰。第一发用使用物品时扣过的凝胶,之后每 5 发尝试扣一次,其余只确认还有凝胶;没凝胶了返回 false</summary>
        private bool FireFlame(Player player, Vector2 aim) {
            int damage = Projectile.damage;
            float knockback = Projectile.knockBack;
            if (firstShotDone) {
                bool consume = (int)Shots % AmmoCheckInterval == 0;
                if (!player.PickAmmo(player.HeldItem, out _, out _, out damage, out knockback, out _, !consume)) {
                    return false;
                }
            }
            firstShotDone = true;
            Vector2 muzzle = PartPosition(MuzzleX, MuzzleY);
            //枪口被物块挡住时退回玩家中心,免得火焰生在墙里
            Vector2 origin = Collision.CanHitLine(player.MountedCenter, 0, 0, muzzle, 0, 0) ? muzzle : player.MountedCenter;
            float speed = PlunderBlazeFlame.Speed * (1f + Main.rand.NextFloat(-SpeedJitter, SpeedJitter));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, aim.RotatedByRandom(AimSpread) * speed, ModContent.ProjectileType<PlunderBlazeFlame>(), damage, knockback, Projectile.owner, Shots);
            return true;
        }

        //贴图上相对握把顶端的一点换到世界坐标;朝左时贴图竖直翻转,纵向偏移跟着反号
        private Vector2 PartPosition(float x, float y) {
            Vector2 aim = Projectile.rotation.ToRotationVector2();
            return Projectile.Center + aim * x + aim.RotatedBy(MathHelper.PiOver2) * (y * Projectile.direction);
        }

        private void Hold(Player player, Vector2 center, Vector2 aim) {
            float gravDir = player.gravDir;
            Projectile.Center = center + new Vector2(0f, HoldDropY * gravDir) + aim * (HoldForward - recoil);
            //前手握握把,后手托枪管下沿;两臂各自指向落点,不顺着枪管伸直
            int dir = Projectile.direction;
            Vector2 toGrip = Projectile.Center - (center + new Vector2(FrontShoulderX * dir, ShoulderY * gravDir));
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ArmRotation(toGrip, gravDir));
            Vector2 toForegrip = PartPosition(ForegripX, ForegripY) - (center + new Vector2(BackShoulderX * dir, ShoulderY * gravDir));
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, ArmRotation(toForegrip, gravDir));
        }

        //合成手臂的转角按正立姿态给,反重力时原版会再取反一次,所以先把纵轴翻回去
        private static float ArmRotation(Vector2 direction, float gravDir) {
            return new Vector2(direction.X, direction.Y * gravDir).ToRotation() - MathHelper.PiOver2;
        }

        private void OnPhaseEntered(float previous) {
            if (Main.dedServ) {
                return;
            }
            if (Phase == PhaseFiring) {
                if (previous == PhaseNone) {
                    CEUtils.PlaySound("flamethrower start", NormalPitch, Projectile.Center, SoundInstances, StartVolume);
                }
                //拥有者在歇完的同一帧就喷出第一发,别的客户端收到同步时已错过,在这里补上枪口火光与后坐
                else if (Projectile.owner != Main.myPlayer) {
                    recoil += RecoilPerShot;
                    muzzleFlash = 1f;
                }
                return;
            }
            CEUtils.PlaySound("steam", VentPitch, Projectile.Center, SoundInstances, VentVolume);
            Vector2 vent = PartPosition(VentX, VentY);
            for (int i = 0; i < VentPuffs; i++) {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-VentPuffSpeed, VentPuffSpeed), -Main.rand.NextFloat(VentPuffSpeed));
                PRTLoader.NewParticle<PRT_EMediumSmoke>(vent, velocity, VentSmokeColor, Main.rand.NextFloat(VentPuffScaleMin, VentPuffScaleMax)).Configure(VentPuffOpacity, false, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), VentPuffLifetime);
            }
        }

        public override void OnKill(int timeLeft) {
            Player player = Main.player[Projectile.owner];
            player.itemTime = 0;
            player.itemAnimation = 0;
            CEUtils.PlaySound("flamethrower end", NormalPitch, Projectile.Center, SoundInstances, EndVolume);
        }

        public override bool? CanDamage() {
            return false;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            bool flip = Projectile.direction < 0;
            Vector2 origin = new Vector2(GripX, flip ? texture.Height - GripY : GripY);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, flip ? SpriteEffects.FlipVertically : SpriteEffects.None);

            //火光都用 A=0 的颜色在默认批次里叠加,不切批次
            Texture2D glow = CEExtraAssets.Glow2;
            float flicker = 1f + PilotFlicker * MathF.Sin(Main.GlobalTimeWrappedHourly * PilotFlickerSpeed);
            DrawSoftGlow(glow, PartPosition(TankX, TankY), TankColor, TankGlowSize, TankGlowOpacity * heat);
            DrawSoftGlow(glow, PartPosition(PilotX, PilotY), PilotColor, PilotGlowSize * flicker, PilotGlowOpacity);
            DrawSoftGlow(glow, PartPosition(MuzzleX, MuzzleY), MuzzleColor, MuzzleGlowSize, muzzleFlash);
            return false;
        }

        private static void DrawSoftGlow(Texture2D glow, Vector2 position, Color color, float size, float opacity) {
            if (opacity < MinVisibleOpacity) {
                return;
            }
            Main.EntitySpriteDraw(glow, position - Main.screenPosition, null, color with { A = 0 } * opacity, 0f, glow.Size() * 0.5f, size / glow.Width, SpriteEffects.None);
        }
    }
}
