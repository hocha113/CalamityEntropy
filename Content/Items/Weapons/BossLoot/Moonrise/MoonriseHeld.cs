using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.Moonrise
{
    /// <summary>
    /// 月上梢头的持握弹幕,由物品的 Shoot 在拥有者端生成。按住时按模式喷火焰、发火球,松手即收起
    /// 出弹间隔与扣弹计时记在 MoonrisePlayer 的冷却里;贴图三帧依次是新月、弦月、满月,储月球显示当前月相
    /// ai[0] 模式,生成时写入;localAI[0] 已举枪帧数
    /// 各端按同一计时推进姿态与特效;开火与收起只由拥有者裁决
    /// </summary>
    public class MoonriseHeld : ModProjectile
    {
        public const int AmmoCheckFrames = 30;          //每隔多少帧尝试扣一次凝胶,同原版喷火器一次使用 30 帧只扣一次
        private const int KeepAliveFrames = 3;          //每帧续上的 timeLeft,消失只由拥有者裁决
        private const int ItemLockFrames = 3;           //每帧续上的 itemTime 与 itemAnimation
        private const int SheetFrames = 3;              //贴图竖排帧数,帧号等于模式
        private const int FlameInterval = 6;            //喷火间隔,帧,等于物品使用时间
        private const int NewBallInterval = 15;         //新月的火球间隔,帧,0.25 秒
        private const int HalfBallInterval = 20;        //弦月的火球间隔,帧,约 0.33 秒
        private const float WhiteReach = 38f * 16f;     //白焰射程,像素,38 格,从玩家中心量到伤害判定最远处
        private const float BlueReach = 50f * 16f;      //蓝焰射程,像素,50 格
        private const float FlameSpread = 0.04f;        //火焰出膛方向的随机偏转上限,弧度
        private const float FlameSpeedJitter = 0.04f;   //火焰初速的随机浮动比例
        private const float BallSpread = 0.1f;          //火球出膛方向的随机偏转上限,弧度
        private const float FlameRecoil = 1.2f;         //每团火焰的枪身后坐,像素
        private const float BallRecoil = 2.5f;          //每个火球的枪身后坐,像素
        private const float RecoilDecay = 0.7f;         //后坐每帧保留的比例
        private const float HoldForward = 8f;           //握把顶端离玩家中心的距离,像素
        private const float HoldDropY = 2f;             //握把顶端相对玩家中心的下沉,像素
        //以下是在 MoonriseHeld.png 单帧(88×36,枪口朝 +X)上量出的像素坐标,除绘制原点外都相对握把顶端
        private const float GripX = 27f;                //握把顶端,绘制原点,也是手的落点
        private const float GripY = 28f;
        private const float MuzzleX = 57f;              //两片月牙护角之间的出口
        private const float MuzzleY = -7f;
        private const float PilotX = 54f;               //膛口
        private const float PilotY = -7f;
        private const float OrbX = 13f;                 //储月球中心
        private const float OrbY = -18f;
        private const float ForegripX = 16f;            //凝胶罐,后手托在这里
        private const float ForegripY = 0f;
        //以下取自原版 Player.GetBackHandPosition 与 GetFrontHandPosition,同劫掠怒焰的持握弹幕
        private const float BackShoulderX = 6f;         //后肩相对绘制中心,像素,朝向为正
        private const float FrontShoulderX = -4f;       //前肩相对绘制中心,像素
        private const float ShoulderY = -2f;
        //特效
        private const float OrbGlowSize = 40f;          //储月球光晕直径,像素
        private const float OrbPulse = 0.08f;           //储月球光晕起伏幅度,比例
        private const float OrbPulseSpeed = 3f;         //储月球光晕起伏角频率,弧度每秒
        private const float PilotGlowSize = 12f;        //膛口长明火光晕直径,像素
        private const float PilotGlowOpacity = 0.7f;
        private const float PilotFlicker = 0.25f;       //长明火闪烁幅度,比例
        private const float PilotFlickerSpeed = 54f;    //长明火闪烁角频率,弧度每秒
        private const float MuzzleGlowSize = 34f;       //枪口火光直径,像素
        private const float MuzzleFlashDecay = 0.75f;   //枪口火光每帧保留的比例
        private const float MinVisibleOpacity = 0.01f;  //低于此不透明度的火光不再画
        private const int EndPuffs = 3;                 //收枪时枪口冒出的烟团数
        private const float EndPuffSpeed = 1.5f;        //烟团初速上限,像素每帧
        private const float EndPuffScaleMin = 0.3f;     //烟团缩放
        private const float EndPuffScaleMax = 0.5f;
        private const int EndPuffLifetime = 36;         //烟团寿命,帧
        private const float EndPuffOpacity = 0.8f;      //烟团初始不透明度
        private const int SoundInstances = 2;           //同一声音同时最多几个
        private const float NormalPitch = 1f;           //CEUtils.PlaySound 的原调
        private const float StartVolume = 0.55f;        //点火声音量
        private const float EndVolume = 0.55f;          //熄火声音量
        private static readonly float[] OrbGlowOpacity = { 0.2f, 0.4f, 0.6f };  //各模式的储月球光晕不透明度,月越满越亮
        private static readonly Color OrbColor = new Color(170, 204, 255);
        private static readonly Color[] ModeColor = { new Color(170, 204, 255), new Color(223, 248, 255), new Color(88, 104, 238) };   //各模式的膛口与枪口火光: 淡蓝、白、蓝
        private static readonly Color EndSmokeColor = new Color(150, 160, 185);

        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/BossLoot/Moonrise/MoonriseHeld_Glow")]
        internal static Asset<Texture2D> GlowTex;       //储月球的月相,不受环境光影响

        private float recoil;                           //枪身后坐,像素
        private float muzzleFlash;                      //0-1 枪口火光
        private int flames;                             //仅拥有者端,本次按住已喷出的火焰数,交给火焰定喷射声节奏

        private int Mode => Utils.Clamp((int)Projectile.ai[0], Moonrise.ModeNew, Moonrise.ModeFull);
        private bool SpraysFlame => Mode != Moonrise.ModeNew;
        private ref float Timer => ref Projectile.localAI[0];

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
            if (owner && (ShouldDrop(player) || !player.channel)) {
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
            if (Timer == 0f && SpraysFlame) {
                CEUtils.PlaySound("flamethrower start", NormalPitch, Projectile.Center, SoundInstances, StartVolume);
            }
            if (owner) {
                if (!Fire(player, aim)) {
                    Projectile.Kill();
                    return;
                }
            }
            else {
                MirrorShots();
            }
            Timer++;
        }

        private static bool ShouldDrop(Player player) {
            return !player.active || player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<Moonrise>();
        }

        /// <summary>冷却到了就喷火焰、发火球。每次出弹都确认还有凝胶,扣弹计时到了才真的尝试扣;没凝胶了返回 false</summary>
        private bool Fire(Player player, Vector2 aim) {
            MoonrisePlayer modPlayer = player.GetModPlayer<MoonrisePlayer>();
            bool flame = SpraysFlame && modPlayer.FlameCooldown <= 0;
            bool ball = Mode != Moonrise.ModeFull && modPlayer.BallCooldown <= 0;
            if (!flame && !ball) {
                return true;
            }
            bool consume = modPlayer.AmmoCooldown <= 0;
            if (!player.PickAmmo(player.HeldItem, out _, out _, out int damage, out float knockback, out _, !consume)) {
                return false;
            }
            if (consume) {
                modPlayer.AmmoCooldown = AmmoCheckFrames;
            }
            Vector2 muzzle = PartPosition(MuzzleX, MuzzleY);
            //枪口被物块挡住时退回玩家中心,免得火焰生在墙里
            Vector2 origin = Collision.CanHitLine(player.MountedCenter, 0, 0, muzzle, 0, 0) ? muzzle : player.MountedCenter;
            if (flame) {
                modPlayer.FlameCooldown = FlameInterval;
                FireFlame(player, origin, aim, damage, knockback);
            }
            if (ball) {
                modPlayer.BallCooldown = Mode == Moonrise.ModeNew ? NewBallInterval : HalfBallInterval;
                FireBall(origin, aim, damage, knockback);
            }
            return true;
        }

        private void FireFlame(Player player, Vector2 origin, Vector2 aim, int damage, float knockback) {
            bool blue = Mode == Moonrise.ModeFull;
            //射程从玩家中心量起,扣掉出膛点到玩家中心这段再反推初速
            float reach = (blue ? BlueReach : WhiteReach) - Vector2.Distance(player.Center, origin);
            float speed = MoonriseFlame.SpeedForReach(reach) * (1f + Main.rand.NextFloat(-FlameSpeedJitter, FlameSpeedJitter));
            int variant = blue ? MoonriseFlame.VariantBlue : MoonriseFlame.VariantWhite;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, aim.RotatedByRandom(FlameSpread) * speed, ModContent.ProjectileType<MoonriseFlame>(), damage, knockback, Projectile.owner, variant, flames);
            flames++;
            recoil += FlameRecoil;
            muzzleFlash = 1f;
        }

        private void FireBall(Vector2 origin, Vector2 aim, int damage, float knockback) {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, aim.RotatedByRandom(BallSpread) * MoonriseFireball.LaunchSpeed, ModContent.ProjectileType<MoonriseFireball>(), damage, knockback, Projectile.owner, MoonriseFireball.NoTarget);
            recoil += BallRecoil;
            muzzleFlash = 1f;
        }

        //别的客户端看不到拥有者的冷却,按同样的间隔补上枪口火光与后坐
        private void MirrorShots() {
            int interval = SpraysFlame ? FlameInterval : NewBallInterval;
            if (Timer % interval == 0f) {
                recoil += SpraysFlame ? FlameRecoil : BallRecoil;
                muzzleFlash = 1f;
            }
        }

        //贴图上相对握把顶端的一点换到世界坐标;朝左时贴图竖直翻转,纵向偏移跟着反号
        private Vector2 PartPosition(float x, float y) {
            Vector2 aim = Projectile.rotation.ToRotationVector2();
            return Projectile.Center + aim * x + aim.RotatedBy(MathHelper.PiOver2) * (y * Projectile.direction);
        }

        private void Hold(Player player, Vector2 center, Vector2 aim) {
            float gravDir = player.gravDir;
            Projectile.Center = center + new Vector2(0f, HoldDropY * gravDir) + aim * (HoldForward - recoil);
            //前手握握把,后手托凝胶罐;两臂各自指向落点,不顺着枪管伸直
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
            player.itemTime = 0;
            player.itemAnimation = 0;
            if (!SpraysFlame || Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("flamethrower end", NormalPitch, Projectile.Center, SoundInstances, EndVolume);
            Vector2 muzzle = PartPosition(MuzzleX, MuzzleY);
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
            Rectangle frame = texture.Frame(1, SheetFrames, 0, Mode);
            bool flip = Projectile.direction < 0;
            Vector2 origin = new Vector2(GripX, flip ? frame.Height - GripY : GripY);
            Vector2 position = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = flip ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, position, frame, lightColor, Projectile.rotation, origin, Projectile.scale, effects);
            Main.EntitySpriteDraw(GlowTex.Value, position, frame, Color.White, Projectile.rotation, origin, Projectile.scale, effects);

            //火光都用 A=0 的颜色在默认批次里叠加,不切批次
            Texture2D glow = CEExtraAssets.Glow2;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 1f + OrbPulse * MathF.Sin(time * OrbPulseSpeed);
            float flicker = 1f + PilotFlicker * MathF.Sin(time * PilotFlickerSpeed);
            Color modeColor = ModeColor[Mode];
            DrawSoftGlow(glow, PartPosition(OrbX, OrbY), OrbColor, OrbGlowSize * pulse, OrbGlowOpacity[Mode]);
            DrawSoftGlow(glow, PartPosition(PilotX, PilotY), modeColor, PilotGlowSize * flicker, PilotGlowOpacity);
            DrawSoftGlow(glow, PartPosition(MuzzleX, MuzzleY), modeColor, MuzzleGlowSize, muzzleFlash);
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
