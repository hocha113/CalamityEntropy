using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    /// <summary>右键召出的黑色大口径手枪,握在后手,抬枪后射出一发安息弹再垂下消失</summary>
    public class RequiemBlackGun : ModProjectile
    {
        public const int RaiseTime = 12;            //帧,从下垂抬到瞄准
        public const int FireTime = 14;             //帧,生成后第几帧开火
        public const int DroopDelay = 12;           //帧,开火后保持瞄准的时长
        public const int Lifetime = 46;             //帧
        public const int FadeTime = 14;             //帧,末尾淡出
        public const float RaiseDropAngle = 1.1f;   //弧度,抬枪起点相对瞄准方向下压
        public const float RecoilKickAngle = 0.55f; //弧度,开火瞬间枪口上扬
        public const float RecoilKickBack = 10f;    //像素,开火瞬间枪身后退
        public const float RecoilDecay = 0.8f;      //每帧保留比例
        public const float DroopRate = 0.12f;       //每帧向正下方插值的比例
        public static readonly Vector2 GripOrigin = new(12f, 16f);      //像素,贴图握把点
        public static readonly Vector2 MuzzleFromGrip = new(52f, -7f);  //像素,朝右时枪口相对握把点
        public const int HitboxSize = 16;           //像素,不造成伤害
        public const float RoundSpeed = 14f;        //像素/更新,安息弹自带 4 次额外更新
        public const float PlayerKick = 3f;         //像素/帧,开火把持有者向后推
        public const float ShakeStrength = 14f;

        public const float ShotPitch = 0.95f;       //CEUtils.PlaySound 音高
        public const float ShotVolume = 0.9f;
        public const float CrackPitch = 0.75f;
        public const float CrackVolume = 0.8f;
        public const int ShotMaxInstances = 2;
        public const float FlashScale = 0.3f;       //Glow2 缩放
        public static readonly Vector2 FlashStretch = new(2.6f, 1f);
        public const int FlashLife = 8;             //帧
        public const float RingAhead = 10f;         //像素,波环在枪口前方
        public const float RingDrift = 2f;          //像素/帧
        public const float RingStart = 0.05f;       //HollowCircleHardEdge 156 像素的缩放
        public const float RingEnd = 0.4f;
        public const int RingLife = 14;             //帧
        public static readonly Vector2 RingSquish = new(0.35f, 1f);
        public const int LineSparks = 6;
        public const float LineSpread = 0.35f;      //弧度
        public const float LineSpeedMin = 10f;      //像素/帧
        public const float LineSpeedMax = 20f;
        public const float LineScaleMin = 0.8f;     //DrainLineBloom 10×32 像素的缩放
        public const float LineScaleMax = 1.2f;
        public const int LineLife = 12;             //帧
        public const int SmokePuffs = 4;
        public const float SmokeSpeedMin = 0.5f;    //像素/帧
        public const float SmokeSpeedMax = 2.5f;
        public const float SmokeScaleMin = 0.4f;    //HeavySmoke 单帧 80 像素的缩放
        public const float SmokeScaleMax = 0.7f;
        public const int SmokeLife = 36;            //帧
        public const float SmokeSpin = 0.03f;       //弧度/帧
        public const float SmokeOpacity = 0.6f;
        public const float ShellKick = 1.4f;        //相对左键抛壳力度
        public const float FireLight = 1.5f;

        public const float SummonPitch = 0.8f;      //比 AzafurePioneerCannon 的同名上膛声低一档
        public const float SummonVolume = 0.7f;
        public const int SummonSmoke = 3;
        public const float SummonSmokeScale = 0.35f;
        public const int SummonSmokeLife = 24;      //帧
        public const int SummonGlints = 5;
        public const float SummonGlintSpeed = 3f;   //像素/帧
        public const int SummonGlintLife = 14;      //帧

        public const int RimTaps = 4;
        public const float RimOffset = 2f;          //像素
        public static readonly Color Rim = new(120, 60, 200, 0);
        public const float RimOpacity = 0.35f;
        public const float SelfLight = 0.35f;       //本体向白色插值比例,暗处也看得见轮廓

        private int timer;                          //帧
        private float gunRot;
        private float aimRot;
        private float recoilAngle;
        private float recoilBack;

        public override void SetDefaults() {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = Lifetime;
            //豆奶翻倍更新次数时只在每帧最后一次更新推进
            if (!Projectile.FinalExtraUpdate())
                return;
            if (timer >= Lifetime) {
                Projectile.Kill();
                return;
            }
            int dir = owner.direction;
            if (timer <= FireTime) {
                aimRot = (owner.mouseWorld() - owner.MountedCenter).ToRotation();
                if (Projectile.owner == Main.myPlayer)
                    owner.ChangeDir(Math.Cos(aimRot) >= 0 ? 1 : -1);
                dir = owner.direction;
            }
            if (timer < RaiseTime) {
                float t = timer / (float)RaiseTime;
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);
                gunRot = (aimRot + RaiseDropAngle * dir).AngleLerp(aimRot, ease);
            }
            else if (timer <= FireTime + DroopDelay) {
                gunRot = aimRot;
            }
            else {
                gunRot = gunRot.AngleLerp(MathHelper.PiOver2, DroopRate);
            }
            recoilAngle *= RecoilDecay;
            recoilBack *= RecoilDecay;
            float drawRot = gunRot - recoilAngle * dir;
            float armRot = drawRot - MathHelper.PiOver2;
            owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRot);
            Vector2 hand = owner.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, armRot) + Vector2.UnitY * owner.gfxOffY;
            Projectile.Center = hand - drawRot.ToRotationVector2() * recoilBack;
            Projectile.rotation = drawRot;
            Projectile.spriteDirection = dir;
            int fadeLeft = Lifetime - timer;
            Projectile.Opacity = fadeLeft < FadeTime ? fadeLeft / (float)FadeTime : 1f;
            if (timer == 0)
                SummonFx(owner);
            if (timer == FireTime)
                Fire(owner, dir);
            timer++;
        }

        private Vector2 Muzzle(float rot, int dir) {
            Vector2 local = new(MuzzleFromGrip.X, MuzzleFromGrip.Y * dir);
            return Projectile.Center + local.RotatedBy(rot);
        }

        private void Fire(Player owner, int dir) {
            Vector2 aim = aimRot.ToRotationVector2();
            Vector2 muzzle = Muzzle(aimRot, dir);
            if (!Collision.CanHit(owner.MountedCenter, 0, 0, muzzle, 0, 0))
                muzzle = owner.MountedCenter;
            if (Projectile.owner == Main.myPlayer) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), muzzle, aim * RoundSpeed, ModContent.ProjectileType<RequiemRound>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                owner.velocity -= aim * PlayerKick;
            }
            recoilAngle = RecoilKickAngle;
            recoilBack = RecoilKickBack;
            FireFx(owner, muzzle, aim);
        }

        private static void FireFx(Player owner, Vector2 muzzle, Vector2 aim) {
            if (Main.dedServ)
                return;
            CEUtils.PlaySound("sniper_rifle", ShotPitch, muzzle, ShotMaxInstances, ShotVolume);
            CEUtils.PlaySound("crack", CrackPitch, muzzle, ShotMaxInstances, CrackVolume);
            PRT_ShineParticle flash = PRTLoader.NewParticle<PRT_ShineParticle>(muzzle, Vector2.Zero, RequiemVfx.GoldWhite, FlashScale);
            flash.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, aim.ToRotation(), FlashLife);
            flash.drawScale = FlashStretch;
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(muzzle + aim * RingAhead, aim * RingDrift, RequiemVfx.Gold, RingStart).Configure(RingSquish, aim.ToRotation(), RingEnd, RingLife);
            for (int i = 0; i < LineSparks; i++) {
                Vector2 v = aim.RotatedByRandom(LineSpread) * Main.rand.NextFloat(LineSpeedMin, LineSpeedMax);
                PRTLoader.NewParticle<PRT_LineCal>(muzzle, v, Color.Lerp(RequiemVfx.Gold, RequiemVfx.GoldWhite, Main.rand.NextFloat()), Main.rand.NextFloat(LineScaleMin, LineScaleMax)).Configure(false, LineLife);
            }
            for (int i = 0; i < SmokePuffs; i++) {
                Vector2 v = aim.RotatedByRandom(LineSpread) * Main.rand.NextFloat(SmokeSpeedMin, SmokeSpeedMax);
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(muzzle, v, RequiemVfx.Soot * SmokeOpacity, Main.rand.NextFloat(SmokeScaleMin, SmokeScaleMax)).Configure(1f, SmokeLife, SmokeSpin);
            }
            RequiemVfx.Shell(owner, muzzle, aim, ShellKick);
            CEUtils.SetShake(muzzle, ShakeStrength);
            Lighting.AddLight(muzzle, RequiemVfx.Gold.ToVector3() * FireLight);
        }

        private static void SummonFx(Player owner) {
            if (Main.dedServ)
                return;
            Vector2 at = owner.MountedCenter;
            CEUtils.PlaySound("Reload", SummonPitch, at, 1, SummonVolume);
            for (int i = 0; i < SummonSmoke; i++)
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(at, Main.rand.NextVector2Circular(1f, 1f), RequiemVfx.Soot * SmokeOpacity, SummonSmokeScale).Configure(1f, SummonSmokeLife, SmokeSpin);
            for (int i = 0; i < SummonGlints; i++) {
                Vector2 v = Main.rand.NextVector2Unit() * SummonGlintSpeed;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(at, v, RequiemVfx.Gold, RequiemVfx.SparkScaleMin).Configure(false, SummonGlintLife, RequiemVfx.SparkSquash, true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            bool left = Projectile.spriteDirection == -1;
            SpriteEffects fx = left ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Vector2 origin = left ? new Vector2(GripOrigin.X, tex.Height - GripOrigin.Y) : GripOrigin;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float alpha = Projectile.Opacity;
            for (int i = 0; i < RimTaps; i++) {
                Vector2 off = (MathHelper.TwoPi * i / RimTaps).ToRotationVector2() * RimOffset;
                Main.EntitySpriteDraw(tex, pos + off, null, Rim * (RimOpacity * alpha), Projectile.rotation, origin, Projectile.scale, fx);
            }
            Color body = Color.Lerp(lightColor, Color.White, SelfLight) * alpha;
            Main.EntitySpriteDraw(tex, pos, null, body, Projectile.rotation, origin, Projectile.scale, fx);
            return false;
        }
    }
}
