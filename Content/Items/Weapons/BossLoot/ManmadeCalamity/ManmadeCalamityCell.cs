using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.ManmadeCalamity
{
    //先漂一小段再锁定追踪,结构照原版泡泡 aiStyle 70(Projectile.cs 27146)
    public class ManmadeCalamityCell : ModProjectile
    {
        public const int DriftFrames = 8;                       //出生后先漂,帧;原版泡泡 10
        private const int Lifetime = 180;                       //策划 3 秒
        private const float HomingRange = 800f;                 //策划 50 格
        private const int RetryFrames = 10;                     //没找到目标隔多久再找,帧;原版 15
        private const int TargetOffset = 1;                     //ai[0] 存下标加一,0 留给待找
        private const float HomingSpeed = 14f;                  //像素每帧
        private const float HomingInertia = 12f;                //越大转向越慢;原版 40
        private const float DriftDrag = 0.94f;                  //漂移段每帧速度保留
        private const float IdleDrag = 0.98f;                   //没有目标时每帧速度保留
        private const int HitboxSize = 20;                      //像素,胞体可见直径约 22
        private const int FadeInFrames = 6;
        private const int FadeOutFrames = 20;
        private const float SpinPerSpeed = 0.03f;               //每帧自转 = 横向速度 × 此值,弧度
        private const int Afterimages = 5;                      //拖影帧数
        private const float AfterimageAlpha = 0.45f;
        private const float AfterimageMinScale = 0.6f;
        private const float SelfLight = 0.4f;                   //本体向白色插值,暗处也看得见
        private const float GlowScale = 0.12f;                  //Glow2 边长 256,约 30 像素
        private const float GlowAlpha = 0.5f;
        private const int PopSparks = 4;
        private const float PopSpeedMin = 1.5f;                 //像素每帧
        private const float PopSpeedMax = 4f;
        private const float PopScale = 0.025f;                  //GlowSpark 边长 2048
        private const int PopLife = 10;                         //帧
        private static readonly Vector2 PopSquash = new(0.6f, 1f);
        private static readonly SoundStyle PopSound = SoundID.Item54 with { Volume = 0.3f, PitchVariance = 0.3f, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };   //原版泡泡破裂
        private static readonly Color GlowColor = new(216, 111, 225, 0);    //A=0 即叠加,取自 ChaoticCellMinion
        private static readonly Color PopColor = new(252, 67, 239);         //取自 ChaoticCellSmall
        private static readonly Vector3 CellLight = new(0.2f, 0.08f, 0.3f);

        private float Target { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }   //负数倒数,0 待找,正数是目标下标加一

        private int age;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = Afterimages;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI() {
            age++;
            if (Target > 0f)
                Home();
            else {
                Projectile.velocity *= age <= DriftFrames ? DriftDrag : IdleDrag;
                if (Target < 0f)
                    Target++;
                else if (Projectile.owner == Main.myPlayer)
                    FindTarget();
            }
            Projectile.rotation += Projectile.velocity.X * SpinPerSpeed;
            Projectile.Opacity = Math.Min(Utils.GetLerpValue(0f, FadeInFrames, age, true), Utils.GetLerpValue(0f, FadeOutFrames, Projectile.timeLeft, true));
            Lighting.AddLight(Projectile.Center, CellLight * Projectile.Opacity);
        }

        private void FindTarget() {
            NPC npc = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange);
            if (npc == null) {
                Target = -RetryFrames;
                return;
            }
            Target = npc.whoAmI + TargetOffset;
            Projectile.netUpdate = true;
        }

        private void Home() {
            NPC npc = Main.npc[(int)Target - TargetOffset];
            if (!npc.CanBeChasedBy(Projectile)) {
                Target = 0f;
                return;
            }
            Vector2 want = (npc.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * HomingSpeed;
            Projectile.velocity = (Projectile.velocity * HomingInertia + want) / (HomingInertia + 1f);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ || timeLeft <= 0)
                return;
            SoundEngine.PlaySound(PopSound, Projectile.Center);
            for (int i = 0; i < PopSparks; i++) {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(PopSpeedMin, PopSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, PopColor, PopScale).Configure(false, PopLife, PopSquash);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 origin = tex.Size() / 2f;
            Vector2 half = Projectile.Size / 2f;
            float opacity = Projectile.Opacity;
            Color body = Color.Lerp(lightColor, Color.White, SelfLight);
            for (int i = Afterimages - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float k = 1f - i / (float)Afterimages;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, null, body * (k * AfterimageAlpha * opacity), Projectile.rotation, origin,
                    Projectile.scale * MathHelper.Lerp(AfterimageMinScale, 1f, k), SpriteEffects.None);
            }
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, pos, null, GlowColor * (GlowAlpha * opacity), 0f, glow.Size() / 2f, GlowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, body * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
