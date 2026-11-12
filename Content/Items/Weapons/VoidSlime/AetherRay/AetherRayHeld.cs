using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.AetherRay
{
    /// <summary>
    /// 以太射线的持握弹幕,HoldItem 在使用中生成,贴图借物品图;停火后等射线淡完才收起
    /// ai[0] 过载标记,拥有者每帧读右键,变了才同步;射线与浮游炮在各端读它
    /// 枪身几何是静态函数,射线按同一套算发射口,朋友盒复制的射线随双子平移
    /// </summary>
    public class AetherRayHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/VoidSlime/AetherRay/AetherRay";

        #region 调参旋钮
        public const int KeepAliveFrames = 3;           //每帧续上的 timeLeft,消失只由拥有者裁决
        public const float HoldForward = 10f;           //握把离玩家中心,像素,同劫掠怒焰
        public const float HoldDropY = 2f;              //握把下沉,像素
        //以下在 AetherRay.png(76×34,朝 +X)上量得,除绘制原点外都相对握把
        public const float GripX = 25f;                 //握把,绘制原点
        public const float GripY = 26f;
        public const float EmitterX = 48f;              //发射口,上下相距 12 像素
        public const float UpperEmitterY = -17f;
        public const float LowerEmitterY = -5f;
        public const float CoreX = 4f;                  //顶部能量仓
        public const float CoreY = -22f;
        public const float ForegripX = 19f;             //前托,后手托在这里
        public const float ForegripY = -3f;
        //肩位取自原版 GetFrontHandPosition 一族,同劫掠怒焰
        public const float BackShoulderX = 6f;
        public const float FrontShoulderX = -4f;
        public const float ShoulderY = -2f;
        public const float OverloadBlendRate = 0.15f;   //过载外观每帧趋近比例
        public const float EmitterGlowSize = 20f;       //发射口光晕直径,像素
        public const float EmitterGlowOpacity = 0.8f;
        public const float CoreGlowSize = 30f;          //能量仓光晕直径,像素
        public const float CoreGlowIdle = 0.35f;        //能量仓平时亮度,过载升到 1
        public const float LightStrength = 0.6f;        //发射口照明
        public const float LoopVolume = 0.28f;          //射线循环音
        public const float LoopMinDist = 200f;          //像素,满音量距离
        public const float LoopMaxDist = 1400f;         //像素,静音距离
        public const float LoopPitch = -0.05f;          //循环音音高偏移
        public const float OverloadPitch = 0.2f;        //过载时循环音再升
        public const float StartVolume = 0.5f;          //起射音量
        public const float ToggleVolume = 0.55f;        //过载开关音量
        public const int SoundInstances = 2;
        public static readonly Color CoreViolet = new(136, 35, 159);
        #endregion

        private float overloadVisual;   //0-1 过载外观,射线与浮游炮也读它
        private bool started;
        private bool seenOverload;
        private LoopSound hum;

        public bool Overloaded => Projectile.ai[0] > 0f;

        /// <summary>枪顶能量仓的世界坐标,浮游炮的供能光带接在这里</summary>
        public Vector2 CorePoint => PartPoint(Projectile.Center, Projectile.rotation.ToRotationVector2(), CoreX, CoreY);

        public static Vector2 AimOf(Player player) {
            return (player.mouseWorld() - player.GetDrawCenter()).SafeNormalize(Vector2.UnitX * player.direction);
        }

        public static Vector2 GripPoint(Player player, Vector2 aim) {
            return player.GetDrawCenter() + new Vector2(0f, HoldDropY * player.gravDir) + aim * HoldForward;
        }

        //贴图上相对握把的一点换到世界坐标;朝左时贴图竖直翻转,纵向偏移跟着反号
        public static Vector2 PartPoint(Vector2 grip, Vector2 aim, float x, float y) {
            float flip = aim.X < 0f ? -1f : 1f;
            return grip + aim * x + aim.RotatedBy(MathHelper.PiOver2) * (y * flip);
        }

        public static Vector2 EmitterPoint(Player player, Vector2 aim, int side) {
            return PartPoint(GripPoint(player, aim), aim, EmitterX, side < 0 ? UpperEmitterY : LowerEmitterY);
        }

        public static AetherRayHeld Find(int owner) {
            int type = ModContent.ProjectileType<AetherRayHeld>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == owner && p.type == type && p.ModProjectile is AetherRayHeld held) {
                    return held;
                }
            }
            return null;
        }

        public static bool IsOverloaded(int owner) => Find(owner)?.Overloaded ?? false;

        public static float OverloadBlend(int owner) => Find(owner)?.overloadVisual ?? 0f;

        public override void SetDefaults() {
            Projectile.HeldProjSetDefaults(DamageClass.Magic);
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.noEnchantmentVisuals = true;
            Projectile.timeLeft = KeepAliveFrames;
        }

        public override void AI() {
            Projectile.timeLeft = KeepAliveFrames;
            //更新次数被豆奶翻倍时仍按帧推进
            if (!Projectile.FinalExtraUpdate()) {
                return;
            }
            Player player = Main.player[Projectile.owner];
            if (!player.active) {
                Projectile.Kill();
                return;
            }
            if (Projectile.owner == Main.myPlayer) {
                if (ShouldDrop(player)) {
                    Projectile.Kill();
                    return;
                }
                float overload = Main.mouseRight && !player.mouseInterface ? 1f : 0f;
                if (overload != Projectile.ai[0]) {
                    Projectile.ai[0] = overload;
                    Projectile.netUpdate = true;
                }
            }
            Vector2 aim = AimOf(player);
            Vector2 grip = GripPoint(player, aim);
            int dir = aim.X < 0f ? -1 : 1;
            Projectile.Center = grip;
            Projectile.rotation = aim.ToRotation();
            Projectile.velocity = aim;
            Projectile.direction = dir;
            Projectile.spriteDirection = dir;
            player.ChangeDir(dir);
            player.heldProj = Projectile.whoAmI;
            HoldArms(player, grip, aim, dir);

            overloadVisual = MathHelper.Lerp(overloadVisual, Overloaded ? 1f : 0f, OverloadBlendRate);
            Lighting.AddLight(PartPoint(grip, aim, EmitterX, LowerEmitterY), BeamTint().ToVector3() * LightStrength);
            if (!Main.dedServ) {
                Sounds();
            }
        }

        private static bool ShouldDrop(Player player) {
            if (player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<AetherRay>()) {
                return true;
            }
            return player.itemAnimation <= 0 && !AetherRayBeam.AnyAlive(player.whoAmI);
        }

        //前手握握把,后手托前托;两臂各自指向落点,不顺着枪管伸直
        private static void HoldArms(Player player, Vector2 grip, Vector2 aim, int dir) {
            Vector2 center = player.GetDrawCenter();
            float gravDir = player.gravDir;
            Vector2 toGrip = grip - (center + new Vector2(FrontShoulderX * dir, ShoulderY * gravDir));
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ArmRotation(toGrip, gravDir));
            Vector2 toForegrip = PartPoint(grip, aim, ForegripX, ForegripY) - (center + new Vector2(BackShoulderX * dir, ShoulderY * gravDir));
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, ArmRotation(toForegrip, gravDir));
        }

        //合成手臂的转角按正立姿态给,反重力时原版会再取反一次
        private static float ArmRotation(Vector2 direction, float gravDir) {
            return new Vector2(direction.X, direction.Y * gravDir).ToRotation() - MathHelper.PiOver2;
        }

        private void Sounds() {
            Vector2 at = Projectile.Center;
            if (!started) {
                started = true;
                seenOverload = Overloaded;
                CEUtils.PlaySound("VividClarityBeamAppear", 1f, at, SoundInstances, StartVolume);
            }
            if (Overloaded != seenOverload) {
                seenOverload = Overloaded;
                CEUtils.PlaySound(Overloaded ? "AscendantActivate" : "AscendantOff", 1f, at, SoundInstances, ToggleVolume);
            }
            bool firing = AetherRayBeam.AnyAlive(Projectile.owner);
            if (hum == null && firing && CalamityEntropy.ealaserSound != null) {
                hum = new LoopSound(CalamityEntropy.ealaserSound);
                hum.setVolume(0f);
                hum.play();
            }
            if (hum != null) {
                hum.timeleft = KeepAliveFrames;
                hum.setVolume_Dist(at, LoopMinDist, LoopMaxDist, firing ? LoopVolume : 0f);
                if (hum.instance != null) {
                    hum.instance.Pitch = MathHelper.Clamp(LoopPitch + OverloadPitch * overloadVisual, -1f, 1f);
                }
            }
        }

        private Color BeamTint() => Color.Lerp(AetherRayBeam.BeamColor, AetherRayBeam.RainbowColor(0f), overloadVisual);

        public override void OnKill(int timeLeft) {
            hum?.stop();
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

            //光晕用 A=0 的颜色在默认批次里叠加,不切批次
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 aim = Projectile.rotation.ToRotationVector2();
            Color beam = BeamTint();
            DrawSoftGlow(glow, PartPoint(Projectile.Center, aim, EmitterX, UpperEmitterY), beam, EmitterGlowSize, EmitterGlowOpacity);
            DrawSoftGlow(glow, PartPoint(Projectile.Center, aim, EmitterX, LowerEmitterY), beam, EmitterGlowSize, EmitterGlowOpacity);
            DrawSoftGlow(glow, CorePoint, Color.Lerp(CoreViolet, beam, overloadVisual), CoreGlowSize, MathHelper.Lerp(CoreGlowIdle, 1f, overloadVisual));
            return false;
        }

        private static void DrawSoftGlow(Texture2D glow, Vector2 position, Color color, float size, float opacity) {
            Main.EntitySpriteDraw(glow, position - Main.screenPosition, null, color with { A = 0 } * opacity, 0f, glow.Size() * 0.5f, size / glow.Width, SpriteEffects.None);
        }
    }
}
