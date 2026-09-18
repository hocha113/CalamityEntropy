using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    /// <summary>
    /// 暗夜灵杖手持弹幕。ai[0] = 模式(0 持杖连续施放夜灵 / 1 举杖降下夜幕)。
    /// 模式 0 是长按型:杖尖跟随鼠标,按物品使用间隔从杖尖放出夜灵并扣魔力,松手结束;
    /// 模式 1 是一次性演出:举杖蓄光、落杖那帧在光标处生成夜幕。施放与魔力都只由所有者决定。
    /// </summary>
    public class NightSpiritStaffHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/NightSpiritStaff";

        #region 调参旋钮
        //握点距玩家中心、握点到晶石尖的长度(再乘 scale)
        public const float GripDist = 8f;
        public const float TipLength = 132f;
        //杖身转向鼠标的跟随速率、施放后坐距离与回弹衰减
        public const float AimLerp = 0.35f;
        public const float CastRecoil = 12f;
        public const float RecoilDecay = 0.82f;
        //模式 0 首发前的举杖帧
        public const int FirstCastDelay = 5;
        //模式 1 总帧数、举杖结束帧、落杖帧
        public const int VeilTotalFrames = 36;
        public const int VeilRaiseEnd = 12;
        public const int VeilSlamFrame = 16;
        public const float VeilRaiseDeg = 118f;
        public const float VeilSlamDeg = 28f;
        #endregion

        private Player Owner => Main.player[Projectile.owner];
        private int Mode => (int)Projectile.ai[0];

        private bool initialized;
        private int facing = 1;
        private float aim;
        private float staffAngle;
        private float recoil;
        private int counter;
        private int castTimer;
        private bool veilCast;
        private float tipGlow = 0.6f;

        private Vector2 GripPos => Projectile.Center + staffAngle.ToRotationVector2() * (GripDist - recoil);
        private Vector2 TipPos => GripPos + staffAngle.ToRotationVector2() * TipLength * Projectile.scale;

        public override void SetDefaults() {
            Projectile.HeldProjSetDefaults(DamageClass.Magic);
            Projectile.timeLeft = 100000;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(veilCast);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            veilCast = reader.ReadBoolean();
        }

        private void Initialize(Player owner) {
            initialized = true;
            facing = Projectile.velocity.X != 0 ? Math.Sign(Projectile.velocity.X) : owner.direction;
            aim = Projectile.velocity.LengthSquared() > 0.01f ? Projectile.velocity.ToRotation() : (facing > 0 ? 0f : MathHelper.Pi);
            staffAngle = aim;
            Projectile.scale = owner.GetAdjustedItemScale(owner.HeldItem);
            castTimer = -FirstCastDelay;
        }

        public override void AI() {
            Player owner = Owner;
            if (!owner.active || owner.dead || owner.HeldItem.ModItem is not NightSpiritStaff) {
                Projectile.Kill();
                return;
            }
            if (!initialized) {
                Initialize(owner);
            }
            Projectile.timeLeft = 3;
            Projectile.Center = owner.MountedCenter;
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;

            //瞄准随鼠标(各端读同步过的鼠标世界坐标)
            Vector2 toMouse = owner.mouseWorld() - owner.MountedCenter;
            if (toMouse.LengthSquared() > 1f) {
                aim = toMouse.ToRotation();
            }
            facing = Math.Cos(aim) >= 0 ? 1 : -1;
            owner.direction = facing;

            if (Mode == 0) {
                UpdateChannel(owner);
            }
            else {
                UpdateVeil(owner);
            }

            recoil *= RecoilDecay;
            tipGlow = MathHelper.Lerp(tipGlow, 0.6f, 0.1f);
            //前手握杖,后手托在杖身更低处
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, staffAngle - MathHelper.PiOver2);
            owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, staffAngle - MathHelper.PiOver2 + facing * 0.4f);
            Lighting.AddLight(TipPos, NightPalette.Violet.ToVector3() * (0.4f + tipGlow * 0.6f));
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                PRTLoader.NewParticle<PRT_Light>(TipPos + CEUtils.randomPointInCircle(6f), new Vector2(0, -0.6f) + CEUtils.randomPointInCircle(0.3f), NightPalette.Violet, Main.rand.NextFloat(0.2f, 0.35f)).Configure(0.7f, lifetime: 18);
            }
            counter++;
        }

        #region 模式 0:持杖连发
        private void UpdateChannel(Player owner) {
            staffAngle = CEUtils.RotateTowardsAngle(staffAngle, aim, AimLerp, false);
            if (Main.myPlayer != Projectile.owner) {
                return;
            }
            if (!owner.channel) {
                Finish(owner);
                return;
            }
            castTimer++;
            int interval = Math.Max(owner.HeldItem.useTime, 4);
            if (castTimer >= interval || castTimer == 0) {
                //首发已由物品使用扣过魔力,之后每发自付
                if (castTimer >= interval && !owner.CheckMana(owner.HeldItem, -1, true)) {
                    Finish(owner);
                    return;
                }
                castTimer = 0;
                owner.manaRegenDelay = (int)owner.maxRegenDelay;
                CastWisps(owner);
            }
        }

        private void CastWisps(Player owner) {
            Vector2 tip = TipPos;
            Vector2 dir = (owner.mouseWorld() - tip).SafeNormalize(aim.ToRotationVector2());
            float speed = owner.HeldItem.shootSpeed;
            int damage = owner.GetWeaponDamage(owner.HeldItem);
            float kb = owner.GetWeaponKnockback(owner.HeldItem);
            int type = ModContent.ProjectileType<NightWisp>();
            for (int i = 0; i < NightSpiritStaff.WispsPerCast; i++) {
                float off = MathHelper.Lerp(-NightSpiritStaff.WispSpread, NightSpiritStaff.WispSpread, NightSpiritStaff.WispsPerCast == 1 ? 0.5f : i / (NightSpiritStaff.WispsPerCast - 1f));
                Vector2 vel = dir.RotatedBy(off) * speed * Main.rand.NextFloat(0.9f, 1.1f);
                //ai[0] = 蛇行相位,ai[1] = 蛇行方向
                Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), tip, vel, type, damage, kb, owner.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi), i % 2 == 0 ? 1f : -1f);
            }
            recoil = CastRecoil;
            tipGlow = 1.6f;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.1f }, tip);
            PRTLoader.NewParticle<PRT_ShineParticle>(tip, Vector2.Zero, NightPalette.Violet, 1.4f).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 10);
            for (int i = 0; i < 6; i++) {
                PRTLoader.NewParticle<PRT_GlowSparkCal>(tip, dir.RotatedByRandom(0.8f) * Main.rand.NextFloat(3f, 8f), NightPalette.Pale, 0.04f).Configure(false, 12, new Vector2(0.4f, 1f), true);
            }
            Projectile.netUpdate = true;
        }
        #endregion

        #region 模式 1:举杖降幕
        private void UpdateVeil(Player owner) {
            float baseDir = facing > 0 ? 0f : MathHelper.Pi;
            float thetaDeg;
            if (counter < VeilRaiseEnd) {
                //举杖:加速抡到头顶后方,晶石聚光
                float t = counter / (float)VeilRaiseEnd;
                float acc = t * t;
                thetaDeg = MathHelper.Lerp(20f, -VeilRaiseDeg, acc);
                if (!Main.dedServ && counter % 2 == 0) {
                    Vector2 tip = TipPos;
                    Vector2 pos = tip + CEUtils.randomRot().ToRotationVector2() * MathHelper.Lerp(90f, 16f, t);
                    PRTLoader.NewParticle<PRT_Light>(pos, (tip - pos) / 7f, NightPalette.Violet, Main.rand.NextFloat(0.35f, 0.6f)).Configure(0.9f, lifetime: 9);
                }
                tipGlow = 0.6f + t;
            }
            else if (counter < VeilSlamFrame) {
                //落杖:四帧扫到前下方
                float t = (counter - VeilRaiseEnd) / (float)(VeilSlamFrame - VeilRaiseEnd);
                thetaDeg = MathHelper.Lerp(-VeilRaiseDeg, VeilSlamDeg, t * t);
                tipGlow = 1.6f;
            }
            else {
                if (!veilCast) {
                    veilCast = true;
                    recoil = CastRecoil;
                    SlamEffects(owner);
                    if (Main.myPlayer == Projectile.owner) {
                        Vector2 pos = owner.mouseWorld();
                        int damage = (int)(owner.GetWeaponDamage(owner.HeldItem) * NightSpiritStaff.VeilDamageMult);
                        Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), pos, Vector2.Zero, ModContent.ProjectileType<NightVeil>(), damage, owner.GetWeaponKnockback(owner.HeldItem) * 0.5f, owner.whoAmI);
                        Projectile.netUpdate = true;
                    }
                }
                float t = MathHelper.SmoothStep(0f, 1f, (counter - VeilSlamFrame) / (float)(VeilTotalFrames - VeilSlamFrame));
                thetaDeg = MathHelper.Lerp(VeilSlamDeg, 8f, t);
            }
            staffAngle = baseDir + facing * MathHelper.ToRadians(thetaDeg);
            if (counter >= VeilTotalFrames) {
                Finish(owner);
            }
        }

        private void SlamEffects(Player owner) {
            Vector2 tip = TipPos;
            CEUtils.PlaySound("VoidAnticipation", 0.9f, tip, 4, 0.7f);
            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = -0.3f }, tip);
            if (Main.dedServ) {
                return;
            }
            ScreenShaker.AddShake(new ScreenShaker.ScreenShake(staffAngle.ToRotationVector2(), 4f));
            PRTLoader.NewParticle<PRT_PulseRing>(tip, Vector2.Zero, NightPalette.Violet, 0.1f).Configure(1.4f, 12);
            PRTLoader.NewParticle<PRT_ShineParticle>(tip, Vector2.Zero, NightPalette.Pale, 2.2f).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 12);
            for (int i = 0; i < 12; i++) {
                Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4f, 10f);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(tip, vel, NightPalette.Violet, 0.05f).Configure(false, 14, new Vector2(0.4f, 1f), true);
            }
        }
        #endregion

        private void Finish(Player owner) {
            owner.itemTime = 1;
            owner.itemAnimation = 1;
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            //杖身贴图右上朝向,柄在左下;朝左时水平镜像
            Vector2 origin = facing > 0 ? new Vector2(12f, tex.Height - 14f) : new Vector2(tex.Width - 12f, tex.Height - 14f);
            SpriteEffects effect = facing > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = facing > 0 ? staffAngle + MathHelper.PiOver4 : staffAngle + MathHelper.Pi * 0.75f;
            Vector2 drawPos = GripPos + Owner.gfxOffY * Vector2.UnitY - Main.screenPosition;
            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, rot, origin, Projectile.scale, effect);

            //晶石辉光:A=0 走同批次加法,施放瞬间提亮
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 tip = TipPos + Owner.gfxOffY * Vector2.UnitY - Main.screenPosition;
            Color deep = NightPalette.Deep * (0.7f * tipGlow);
            deep.A = 0;
            Color violet = NightPalette.Violet * (0.55f * tipGlow);
            violet.A = 0;
            Main.EntitySpriteDraw(glow, tip, null, deep, 0f, glow.Size() / 2f, 0.36f * tipGlow, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, tip, null, violet, 0f, glow.Size() / 2f, 0.2f * tipGlow, SpriteEffects.None);
            return false;
        }
    }
}
