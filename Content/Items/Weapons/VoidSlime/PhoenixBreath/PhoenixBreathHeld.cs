using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.PhoenixBreath
{
    /// <summary>
    /// 凤凰吐息的持握弹幕,由物品的 Shoot 在拥有者端生成。左键先蓄力再点燃火柱,松开即熄灭;右键按住按攻速连发火球
    /// ai[0] 模式,生成时写入;ai[1] 已发出的火球数,拥有者写入并同步;localAI[0] 已举枪帧数;localAI[1] 仅拥有者端,距下次扣弹或发火球的帧数
    /// 各端按同一计时推进蓄力、姿态与特效;开火、扣弹与收起只由拥有者裁决
    /// 朋友盒的复制品照常喷火、发火球,但画在双子身上,不占玩家的手,也不扣凝胶
    /// </summary>
    public class PhoenixBreathHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/VoidSlime/PhoenixBreath/PhoenixBreath";

        public const float ModeColumn = 0f;             //ai[0] 取值: 左键火柱
        public const float ModeFireball = 1f;           //ai[0] 取值: 右键火球
        private const int KeepAliveFrames = 3;          //每帧续上的 timeLeft,消失只由拥有者裁决
        private const int ItemLockFrames = 3;           //每帧续上的 itemTime 与 itemAnimation
        private const float ChargeFrames = 18f;         //火柱蓄力,帧,0.3 秒
        private const float BallSpread = 0.04f;         //火球出膛方向的随机偏转上限,弧度
        private const float FlameShake = 1f;            //火柱喷射时枪身的抖动上限,像素
        private const float BallRecoil = 5f;            //每个火球的枪身后坐,像素
        private const float RecoilDecay = 0.75f;        //后坐每帧保留的比例
        private const float HoldForward = 8f;           //握把顶端离玩家中心的距离,像素
        private const float HoldDropY = 2f;             //握把顶端相对玩家中心的下沉,像素
        //以下是在 PhoenixBreath.png(96×48,喷口朝 +X)上量出的像素坐标,除绘制原点外都相对握把顶端
        private const float GripX = 26f;                //握把顶端,绘制原点,也是前手落点
        private const float GripY = 37f;
        private const float MuzzleX = 68f;              //喙形喷口
        private const float MuzzleY = -8f;
        private const float WindowX = 31f;              //机身上的火窗
        private const float WindowY = -8f;
        private const float ForegripX = 24f;            //机身下沿,后手托在这里
        private const float ForegripY = -3f;
        //以下取自原版 Player.GetBackHandPosition 与 GetFrontHandPosition,同月上梢头的持握弹幕
        private const float BackShoulderX = 6f;         //后肩相对绘制中心,像素,朝向为正
        private const float FrontShoulderX = -4f;       //前肩相对绘制中心,像素
        private const float ShoulderY = -2f;
        //蓄力时火星从四周聚向喷口
        private const int ChargeDust = 2;               //每帧出的火星数
        private const float ChargeDustFar = 40f;        //火星生成处离喷口的距离,像素,随蓄力收到下一个值
        private const float ChargeDustNear = 12f;
        private const float ChargeDustPull = 0.15f;     //火星初速取离喷口距离的比例,朝喷口
        private const float ChargeDustScaleMin = 1.1f;
        private const float ChargeDustScaleMax = 1.6f;
        //火光
        private const float ChargeGlowMin = 8f;         //喷口蓄力光晕直径,像素,随蓄力变大
        private const float ChargeGlowMax = 44f;
        private const float ChargeGlowOpacity = 0.8f;
        private const float ChargeFlicker = 0.12f;      //点燃后喷口光晕闪烁幅度,比例
        private const float ChargeFlickerSpeed = 40f;   //喷口光晕闪烁角频率,弧度每秒
        private const float WindowGlowSize = 18f;       //火窗光晕直径,像素
        private const float WindowGlowOpacity = 0.6f;
        private const float WindowPulse = 0.15f;        //火窗光晕起伏幅度,比例
        private const float WindowPulseSpeed = 5f;      //火窗光晕起伏角频率,弧度每秒
        private const float FlashGlowSize = 52f;        //点火与发火球时喷口火光的直径,像素
        private const float FlashDecay = 0.75f;         //喷口火光每帧保留的比例
        private const float MinVisibleOpacity = 0.01f;  //低于此不透明度的火光不再画
        //熄火时喷口冒的烟,同月上梢头
        private const int EndPuffs = 3;
        private const float EndPuffSpeed = 1.5f;        //烟团初速上限,像素每帧
        private const float EndPuffScaleMin = 0.3f;
        private const float EndPuffScaleMax = 0.5f;
        private const int EndPuffLifetime = 36;         //烟团寿命,帧
        private const float EndPuffOpacity = 0.8f;
        //声音
        private const int RoarInterval = 12;            //喷射声间隔,帧
        private const float RoarPitch = -0.25f;         //原版喷射声的音高偏移,取低一些显得厚重
        private const float RoarPitchBand = 0.15f;
        private const float RoarVolume = 0.8f;
        private const int RoarInstances = 3;
        private const int SoundInstances = 2;           //点火与熄火声同时最多几个
        private const float NormalPitch = 1f;           //CEUtils.PlaySound 的原调
        private const float StartVolume = 0.6f;
        private const float EndVolume = 0.6f;
        private static readonly Color HotColor = new Color(255, 145, 28);       //蓄力光,取自凤凰色阶
        private static readonly Color FlashColor = new Color(255, 215, 70);     //喷口火光
        private static readonly Color WindowColor = new Color(252, 102, 25);    //火窗
        private static readonly Color EndSmokeColor = new Color(120, 100, 96);

        private float recoil;       //枪身后坐,像素
        private float flash;        //0-1 喷口火光
        private float shotsSeen;    //各端各记,已经补过后坐的火球数

        private bool FireballMode => Projectile.ai[0] == ModeFireball;
        private ref float Shots => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float NextUse => ref Projectile.localAI[1];
        private bool IsCopy => Projectile.Entropy().IndexOfTwistedTwinShootedThisProj >= 0;

        /// <summary>火柱模式已蓄满点燃</summary>
        public bool Ignited => !FireballMode && Timer > ChargeFrames;

        /// <summary>喷口的世界坐标,火柱从这里铺开</summary>
        public Vector2 Muzzle => PartPosition(MuzzleX, MuzzleY);

        private float Charge => FireballMode ? 0f : Math.Min(Timer / ChargeFrames, 1f);

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
            bool copy = IsCopy;
            if (owner && (ShouldDrop(player) || !StillPressed(player) || copy && TwinGone())) {
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
            recoil *= RecoilDecay;
            flash *= FlashDecay;
            if (Shots > shotsSeen) {
                shotsSeen = Shots;
                recoil += BallRecoil;
                flash = 1f;
            }
            if (Ignited) {
                recoil = Math.Max(recoil, Main.rand.NextFloat(FlameShake));
            }
            //复制品运行时拥有者中心已挪到双子身上,只摆自己的位置
            if (copy) {
                Projectile.Center = center + aim * (HoldForward - recoil);
            }
            else {
                player.itemTime = ItemLockFrames;
                player.itemAnimation = ItemLockFrames;
                player.ChangeDir(dir);
                player.heldProj = Projectile.whoAmI;
                Hold(player, center, aim);
            }

            Cues();
            if (owner && !(FireballMode ? FireballTick(player, aim, copy) : ColumnTick(player, copy))) {
                Projectile.Kill();
                return;
            }
            Timer++;
        }

        private static bool ShouldDrop(Player player) {
            return !player.active || player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<PhoenixBreath>();
        }

        //右键使用时 channel 只维持一帧,火球模式改看右键是否还按着
        private bool StillPressed(Player player) {
            return FireballMode ? player.controlUseTile : player.channel;
        }

        private bool TwinGone() {
            Projectile twin = Projectile.Entropy().IndexOfTwistedTwinShootedThisProj.ToProj_Identity();
            return twin == null || !twin.active;
        }

        private static int UseInterval(Player player) {
            return CombinedHooks.TotalUseTime(PhoenixBreath.UseFrames, player, player.HeldItem);
        }

        /// <summary>火柱: 蓄满那一帧点燃;每隔一次使用时间判定一次扣弹,第一次在使用物品时已判定;没凝胶了返回 false</summary>
        private bool ColumnTick(Player player, bool copy) {
            if (Timer == 0f) {
                NextUse = UseInterval(player);
            }
            if (Timer == ChargeFrames) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Muzzle, Projectile.velocity, ModContent.ProjectileType<PhoenixBreathColumn>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.identity);
            }
            if (--NextUse > 0f) {
                return true;
            }
            NextUse = UseInterval(player);
            return player.PickAmmo(player.HeldItem, out _, out _, out _, out _, out _, copy);
        }

        /// <summary>火球: 举枪当帧发第一个,它的凝胶在使用物品时已判定;之后每隔一次使用时间扣弹再发,没凝胶了返回 false</summary>
        private bool FireballTick(Player player, Vector2 aim, bool copy) {
            if (Timer > 0f) {
                if (--NextUse > 0f) {
                    return true;
                }
                if (!player.PickAmmo(player.HeldItem, out _, out _, out _, out _, out _, copy)) {
                    return false;
                }
            }
            NextUse = UseInterval(player);
            Vector2 muzzle = Muzzle;
            //喷口被物块挡住时退回玩家中心,免得火球生在墙里
            Vector2 origin = Collision.CanHitLine(player.MountedCenter, 0, 0, muzzle, 0, 0) ? muzzle : player.MountedCenter;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, aim.RotatedByRandom(BallSpread) * PhoenixBreathFireball.LaunchSpeed, ModContent.ProjectileType<PhoenixBreathFireball>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            Shots++;
            Projectile.netUpdate = true;
            return true;
        }

        //声音与蓄力火星,各端按同一计时播
        private void Cues() {
            if (FireballMode || Main.dedServ) {
                return;
            }
            if (Timer == 0f) {
                CEUtils.PlaySound("flamethrower start", NormalPitch, Projectile.Center, SoundInstances, StartVolume);
            }
            if (Timer < ChargeFrames) {
                Vector2 muzzle = Muzzle;
                float reach = MathHelper.Lerp(ChargeDustFar, ChargeDustNear, Charge);
                for (int i = 0; i < ChargeDust; i++) {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(reach, reach);
                    Dust dust = Dust.NewDustPerfect(muzzle + offset, Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame, -offset * ChargeDustPull);
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(ChargeDustScaleMin, ChargeDustScaleMax);
                }
                return;
            }
            if (Timer == ChargeFrames) {
                flash = 1f;
            }
            if ((Timer - ChargeFrames) % RoarInterval == 0f) {
                SoundEngine.PlaySound(SoundID.Item34 with { Pitch = RoarPitch, PitchVariance = RoarPitchBand, Volume = RoarVolume, MaxInstances = RoarInstances }, Projectile.Center);
            }
        }

        //贴图上相对握把顶端的一点换到世界坐标;朝左时贴图竖直翻转,纵向偏移跟着反号
        private Vector2 PartPosition(float x, float y) {
            Vector2 aim = Projectile.rotation.ToRotationVector2();
            return Projectile.Center + (aim * x + aim.RotatedBy(MathHelper.PiOver2) * (y * Projectile.direction)) * Projectile.scale;
        }

        private void Hold(Player player, Vector2 center, Vector2 aim) {
            float gravDir = player.gravDir;
            Projectile.Center = center + new Vector2(0f, HoldDropY * gravDir) + aim * (HoldForward - recoil);
            //前手握握把,后手托机身;两臂各自指向落点
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

        public override void OnKill(int timeLeft) {
            Player player = Main.player[Projectile.owner];
            bool copy = IsCopy;
            if (!copy) {
                player.itemTime = 0;
                player.itemAnimation = 0;
            }
            //下一个火球本该发出之前不许再举枪,连点也守住攻速
            if (Projectile.owner == Main.myPlayer && FireballMode && !copy) {
                PhoenixBreathPlayer modPlayer = player.GetModPlayer<PhoenixBreathPlayer>();
                modPlayer.FireballCooldown = Math.Max(modPlayer.FireballCooldown, (int)NextUse);
            }
            if (!Ignited || Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("flamethrower end", NormalPitch, Projectile.Center, SoundInstances, EndVolume);
            Vector2 muzzle = Muzzle;
            for (int i = 0; i < EndPuffs; i++) {
                Vector2 velocity = Projectile.velocity * Main.rand.NextFloat(EndPuffSpeed) - Vector2.UnitY * Main.rand.NextFloat(EndPuffSpeed);
                PRTLoader.NewParticle<PRT_EMediumSmoke>(muzzle, velocity, EndSmokeColor, Main.rand.NextFloat(EndPuffScaleMin, EndPuffScaleMax)).Configure(EndPuffOpacity, false, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), EndPuffLifetime);
            }
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
            SpriteEffects effects = flip ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects);

            //火光都用 A=0 的颜色在默认批次里叠加,不切批次
            Texture2D glow = CEExtraAssets.Glow2;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 1f + WindowPulse * MathF.Sin(time * WindowPulseSpeed);
            DrawSoftGlow(glow, PartPosition(WindowX, WindowY), WindowColor, WindowGlowSize * pulse, WindowGlowOpacity);
            float charge = Charge;
            float flicker = Ignited ? 1f + ChargeFlicker * MathF.Sin(time * ChargeFlickerSpeed) : 1f;
            DrawSoftGlow(glow, Muzzle, HotColor, MathHelper.Lerp(ChargeGlowMin, ChargeGlowMax, charge) * flicker, ChargeGlowOpacity * charge);
            DrawSoftGlow(glow, Muzzle, FlashColor, FlashGlowSize, flash);
            return false;
        }

        private void DrawSoftGlow(Texture2D glow, Vector2 position, Color color, float size, float opacity) {
            if (opacity < MinVisibleOpacity) {
                return;
            }
            Main.EntitySpriteDraw(glow, position - Main.screenPosition, null, color with { A = 0 } * opacity, 0f, glow.Size() * 0.5f, size * Projectile.scale / glow.Width, SpriteEffects.None);
        }
    }
}
