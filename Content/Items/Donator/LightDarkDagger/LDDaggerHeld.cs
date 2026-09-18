using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.LightDarkDagger
{
    /// <summary>
    /// 光暗龙匕手持弹幕:每次使用生成一枚,负责投掷动作与在释放帧生成飞刃。
    /// ai[0] = 模式(0 耀光三连 / 1 黯影三连 / 2 螺旋刃 / 3 闪烁收势)。
    /// 耀光从肩后上方甩出走上半弧,黯影从腰后下方撩出走下半弧;动作各端由计数器复现,飞刃只由所有者生成。
    /// </summary>
    public class LDDaggerHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/LightDarkDagger/LDKnifeLight";

        #region 调参旋钮
        //三段进度:蓄力结束、甩出结束(余下为收势)、释放帧
        public const float WindFrac = 0.32f;
        public const float FlickFrac = 0.46f;
        public const float ReleaseFrac = 0.42f;
        //螺旋刃蓄力更长
        public const float HelixWindFrac = 0.48f;
        public const float HelixFlickFrac = 0.6f;
        public const float HelixReleaseFrac = 0.56f;
        //手臂相对瞄准线的角度(度):起手、拉满、甩出落点、收势
        public const float StartDeg = 35f;
        public const float PullDeg = 125f;
        public const float EndDeg = 40f;
        public const float SettleDeg = 12f;
        //手心距玩家中心
        public const float HandDist = 20f;
        //收势后半段把下一把刃拿出来的进度
        public const float NextKnifeFrac = 0.78f;
        #endregion

        private Player Owner => Main.player[Projectile.owner];
        private int Mode => (int)Projectile.ai[0];
        private bool IsHelix => Mode == 2;
        private bool IsBlink => Mode == 3;

        private bool initialized;
        private int facing = 1;
        private float aim;
        private float total = 22f;
        private float counter;
        private bool released;
        private float handAngle;
        //当前手里的刃:-1 空手,0 耀光,1 黯影,2 双刃
        private int knifeInHand;
        private float knifeAlpha = 1f;

        private Vector2 HandPos => Projectile.Center + handAngle.ToRotationVector2() * HandDist;

        public override void SetDefaults() {
            Projectile.HeldProjSetDefaults(DamageClass.Ranged);
            Projectile.timeLeft = 100000;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        private void Initialize(Player owner) {
            initialized = true;
            facing = Projectile.velocity.X != 0 ? Math.Sign(Projectile.velocity.X) : owner.direction;
            aim = Projectile.velocity.LengthSquared() > 0.01f ? Projectile.velocity.ToRotation() : (facing > 0 ? 0f : MathHelper.Pi);
            total = Math.Max(owner.HeldItem.useTime > 0 ? owner.HeldItem.useTime : 22, 6);
            knifeInHand = IsHelix || IsBlink ? 2 : Mode;
        }

        public override void AI() {
            Player owner = Owner;
            if (!owner.active || owner.dead || owner.HeldItem.ModItem is not LightDarkDragonDagger) {
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
            owner.direction = facing;

            float p = counter / total;
            if (IsBlink) {
                UpdateBlinkPose(p);
            }
            else {
                UpdateThrow(owner, p);
            }
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, handAngle - MathHelper.PiOver2);

            counter++;
            if (p >= 1f) {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
        }

        /// <summary>耀光走上半弧:手从肩后上方甩到前下方;黯影走下半弧:手从腰后下方撩到前上方。</summary>
        private void UpdateThrow(Player owner, float p) {
            //arcSign:-1 为上方(屏幕负 Y 侧),+1 为下方;螺旋刃从上方甩出
            float arcSign = Mode == 1 ? 1f : -1f;
            float wind = IsHelix ? HelixWindFrac : WindFrac;
            float flick = IsHelix ? HelixFlickFrac : FlickFrac;
            float release = IsHelix ? HelixReleaseFrac : ReleaseFrac;
            float offDeg;
            if (p < wind) {
                float t = MathHelper.SmoothStep(0f, 1f, p / wind);
                offDeg = MathHelper.Lerp(StartDeg, PullDeg, t);
                if (IsHelix && !Main.dedServ && counter % 2 == 0) {
                    //螺旋刃蓄力:光暗两色光尘向手心汇聚
                    bool light = counter % 4 == 0;
                    Vector2 pos = HandPos + CEUtils.randomRot().ToRotationVector2() * MathHelper.Lerp(70f, 12f, t);
                    PRTLoader.NewParticle<PRT_Light>(pos, (HandPos - pos) / 8f, LDPalette.Tone(light), Main.rand.NextFloat(0.3f, 0.5f)).Configure(0.8f, lifetime: 10);
                }
            }
            else if (p < flick) {
                float t = (p - wind) / (flick - wind);
                offDeg = MathHelper.Lerp(PullDeg, -EndDeg, t * t);
                if (!released && p >= release) {
                    released = true;
                    Release(owner);
                }
                if (!Main.dedServ && knifeInHand >= 0) {
                    //甩出段:刃尖拖出一缕光
                    bool light = Mode != 1;
                    PRTLoader.NewParticle<PRT_Light>(HandPos + handAngle.ToRotationVector2() * 14f, handAngle.ToRotationVector2().RotatedBy(-arcSign * facing * MathHelper.PiOver2) * 4f, LDPalette.Tone(light), 0.35f).Configure(0.8f, lifetime: 8);
                }
            }
            else {
                float t = MathHelper.SmoothStep(0f, 1f, (p - flick) / (1f - flick));
                offDeg = MathHelper.Lerp(-EndDeg, -SettleDeg, t);
                //收势后半段把下一把刃拿到手里,自然衔接下一次投掷
                if (!IsHelix && p >= NextKnifeFrac) {
                    knifeInHand = 1 - Mode;
                    knifeAlpha = MathHelper.Clamp((p - NextKnifeFrac) / (1f - NextKnifeFrac), 0f, 1f);
                }
            }
            //负号:上半弧对应屏幕上方,右手系里正向旋转朝下,所以 -arcSign 为「后上方」
            handAngle = aim + facing * arcSign * MathHelper.ToRadians(offDeg);
        }

        private void UpdateBlinkPose(float p) {
            //闪烁收势:双刃前指定格,再缓缓放下
            float offDeg = p < 0.15f ? MathHelper.Lerp(-40f, 0f, p / 0.15f) : (p < 0.55f ? 0f : MathHelper.Lerp(0f, 30f, MathHelper.SmoothStep(0f, 1f, (p - 0.55f) / 0.45f)));
            handAngle = aim + facing * MathHelper.ToRadians(offDeg);
            knifeAlpha = p < 0.55f ? 1f : 1f - (p - 0.55f) / 0.45f;
        }

        private void Release(Player owner) {
            knifeInHand = -1;
            knifeAlpha = 0f;
            CEUtils.PlaySound("throw", IsHelix ? 0.7f : (Mode == 1 ? 0.85f : 1.1f), owner.Center, 6, 0.8f);
            if (IsHelix) {
                CEUtils.PlaySound("soulshine", 1.2f, owner.Center, 4, 0.6f);
            }
            if (Main.myPlayer != Projectile.owner) {
                return;
            }
            Vector2 hand = HandPos;
            Vector2 toMouse = (owner.mouseWorld() - hand).SafeNormalize(aim.ToRotationVector2());
            if (IsHelix) {
                SpawnHelix(owner, hand, toMouse);
            }
            else {
                SpawnVolley(owner, hand, toMouse, Mode == 1);
            }
        }

        private void SpawnVolley(Player owner, Vector2 hand, Vector2 dir, bool dark) {
            //正向旋转是否朝屏幕上方:据此决定耀光走上半弧、黯影走下半弧
            float upSign = dir.RotatedBy(0.1f).Y < dir.Y ? 1f : -1f;
            float sideSign = dark ? -upSign : upSign;
            int type = dark ? ModContent.ProjectileType<LDKnifeDark>() : ModContent.ProjectileType<LDKnifeLight>();
            float speed = owner.HeldItem.shootSpeed;
            for (int i = 0; i < LightDarkDragonDagger.VolleyOffsetsDeg.Length; i++) {
                float off = MathHelper.ToRadians(LightDarkDragonDagger.VolleyOffsetsDeg[i]) * sideSign;
                Vector2 vel = dir.RotatedBy(off) * speed;
                //ai[0] = 剩余回转总角(反向并略过冲),ai[1] = 剩余回转帧数
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), hand, vel, type, Projectile.damage, Projectile.knockBack, Projectile.owner, -off * LightDarkDragonDagger.ArcOvershoot, LightDarkDragonDagger.ArcTurnFrames);
            }
        }

        private void SpawnHelix(Player owner, Vector2 hand, Vector2 dir) {
            Vector2 vel = dir * LightDarkDragonDagger.HelixSpeed;
            int light = ModContent.ProjectileType<LDHelixKnifeLight>();
            int dark = ModContent.ProjectileType<LDHelixKnifeDark>();
            int lag = LightDarkDragonDagger.HelixLagFrames;
            //光股相位 0、暗股相位 π,同股第二把刃落后 lag 帧
            SpawnHelixKnife(hand, vel, light, 0f, 0);
            SpawnHelixKnife(hand, vel, light, 0f, lag);
            SpawnHelixKnife(hand, vel, dark, MathHelper.Pi, 0);
            SpawnHelixKnife(hand, vel, dark, MathHelper.Pi, lag);
        }

        private void SpawnHelixKnife(Vector2 pos, Vector2 vel, int type, float phase, int lag) {
            int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), pos, vel, type, Projectile.damage, Projectile.knockBack, Projectile.owner, phase, lag);
            CEChargeWeapon.Empower(p);
        }

        public override bool PreDraw(ref Color lightColor) {
            if (knifeInHand < 0 || knifeAlpha <= 0.02f) {
                return false;
            }
            Vector2 drawPos = HandPos + Owner.gfxOffY * Vector2.UnitY - Main.screenPosition;
            if (knifeInHand == 2) {
                //双刃交叉握在手里
                DrawKnife(true, drawPos, handAngle - facing * 0.28f, lightColor);
                DrawKnife(false, drawPos, handAngle + facing * 0.28f, lightColor);
            }
            else {
                DrawKnife(knifeInHand == 0, drawPos, handAngle, lightColor);
            }
            return false;
        }

        private void DrawKnife(bool light, Vector2 drawPos, float angle, Color lightColor) {
            Texture2D tex = light ? Projectile.GetTexture() : CEUtils.RequestTex("CalamityEntropy/Content/Items/Donator/LightDarkDagger/LDKnifeDark");
            //刃身贴图右上朝向,柄在左下;朝左时水平镜像,柄换到右下
            Vector2 origin = facing > 0 ? new Vector2(4f, tex.Height - 4f) : new Vector2(tex.Width - 4f, tex.Height - 4f);
            SpriteEffects effect = facing > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = facing > 0 ? angle + MathHelper.PiOver4 : angle + MathHelper.Pi * 0.75f;
            Color glow = LDPalette.Tone(light) * (0.35f * knifeAlpha);
            glow.A = 0;
            Texture2D glowTex = CEExtraAssets.Glow2;
            Vector2 bladeMid = drawPos + angle.ToRotationVector2() * 18f;
            Main.EntitySpriteDraw(glowTex, bladeMid, null, glow, 0f, glowTex.Size() / 2f, 0.18f, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, drawPos, null, lightColor * knifeAlpha, rot, origin, Projectile.scale, effect);
        }
    }
}
