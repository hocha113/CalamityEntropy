using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StellarFury
{
    /// <summary>
    /// 星之怒的迷你黑洞,导弹命中时由拥有者在命中点生成,滞留原地;半径 5 格圆形判定,同类型共用静态无敌帧
    /// 旋流盘照虚空史莱姆 DrawSwirl 调 VDSingularity(那份文件在别处改动中,这里抄同一段调用);扭曲走 VDScreenFx.ReportLens,各端每帧上报
    /// </summary>
    public class StellarFuryHole : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        #region 调参旋钮
        public const float Radius = 80f;                //像素,策划半径 5 格
        public const int Life = 90;                     //帧,策划 1.5 秒
        public const int HitCooldown = 10;              //帧,策划 10 静态无敌帧
        public const int GrowFrames = 8;                //帧,出现时缓出放大
        public const int CollapseFrames = 10;           //帧,最后坍缩
        public const float HorizonRadius = 24f;         //像素,视界黑盘;吸积盘外缘对齐判定边界
        public const float SwirlSpin = 1.4f;            //旋流角速度,同虚空史莱姆黑洞
        public const float HazeScale = 3.4f;            //外晕直径与判定半径之比,同虚空史莱姆虫洞
        public const float HazeIntensity = 0.8f;
        public const float RimWidthRatio = 0.035f;      //边线半宽占半径,同虚空史莱姆黑洞
        public const float RimMinWidth = 3f;            //像素
        public const float RimIntensity = 0.85f;
        public const int RimSegments = 64;
        public const float FallbackRingRatio = 0.75f;   //着色器缺失时环半径占半宽
        public const float LensStrength = 0.05f;        //VDScreenFx 强度,0.05 是可读下限,策划要微弱
        public const float LensRadiusRatio = 2.1f;      //扭曲半径与判定半径之比
        public const float LightStrength = 0.6f;
        public const int MoteInterval = 2;              //帧
        public const float MoteSpawnRatio = 1.25f;      //吸入光点出生半径与判定半径之比
        public const float MoteInward = 2.2f;           //像素每帧,向心
        public const float MoteSwirl = 3.2f;            //像素每帧,切向
        public const float MoteMinScale = 0.2f;
        public const float MoteMaxScale = 0.34f;
        public const int MoteLife = 24;                 //帧
        public const float RingStartScale = 0.1f;       //出生冲击环,环贴图 156 像素
        public const float RingEndScale = 1.2f;
        public const int RingLife = 18;                 //帧
        public const float FlashScale = 1.4f;
        public const int FlashLife = 14;                //帧
        public const float FlashBloom = 1.6f;
        public const float SoundPitchMin = 0.95f;       //音高区间,黑洞每秒可能生成好几个
        public const float SoundPitchMax = 1.15f;
        public const float SpawnVolume = 0.4f;
        public const float EndVolume = 0.3f;
        public const int SoundInstances = 3;
        public static readonly Color DiskColor = new(91, 35, 159);
        public static readonly Color HotColor = new(170, 204, 255);
        public static readonly Color DeepColor = new(53, 11, 94);
        #endregion

        private int age;

        public override void SetDefaults() {
            Projectile.width = Projectile.height = (int)(Radius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = HitCooldown;
            Projectile.timeLeft = Life;
        }

        public override void AI() {
            age++;
            float env = Envelope();
            Lighting.AddLight(Projectile.Center, HotColor.ToVector3() * LightStrength * env);
            if (Main.dedServ) {
                return;
            }
            VDScreenFx.ReportLens(Projectile.Center, LensStrength * env, Radius * LensRadiusRatio * env);
            if (age == 1) {
                CEUtils.PlaySound("BlackholeSpawn", Main.rand.NextFloat(SoundPitchMin, SoundPitchMax), Projectile.Center, SoundInstances, SpawnVolume);
                PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, HotColor, RingStartScale).Configure(RingEndScale, RingLife);
                PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Color.White, FlashScale).Configure(DiskColor, FlashLife, 0f, FlashBloom);
            }
            if (Projectile.timeLeft == CollapseFrames) {
                CEUtils.PlaySound("blackholeEnd", Main.rand.NextFloat(SoundPitchMin, SoundPitchMax), Projectile.Center, SoundInstances, EndVolume);
            }
            if (age % MoteInterval == 0) {
                SpawnMote(env);
            }
        }

        private float Envelope() {
            float grow = GodDraw.EaseOut(age / (float)GrowFrames);
            float collapse = GodDraw.Smooth(Projectile.timeLeft / (float)CollapseFrames);
            return Math.Min(grow, collapse);
        }

        //边缘外生成,边转边往里吸
        private void SpawnMote(float env) {
            Vector2 dir = CEUtils.randomRot().ToRotationVector2();
            Vector2 position = Projectile.Center + dir * Radius * MoteSpawnRatio * env;
            Vector2 velocity = -dir * MoteInward + dir.RotatedBy(MathHelper.PiOver2) * MoteSwirl;
            Color color = Main.rand.NextBool() ? HotColor : DiskColor;
            PRTLoader.NewParticle<PRT_Light>(position, velocity, color, Main.rand.NextFloat(MoteMinScale, MoteMaxScale)).Configure(env, lifetime: MoteLife);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            //判定半径跟着出现与坍缩缩放,伤害不超出看得见的范围
            float radius = Radius * Envelope();
            Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(nearest, Projectile.Center) <= radius * radius;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            float env = Envelope();
            if (env <= 0f) {
                return false;
            }
            float radius = Radius * env;
            Vector2 center = Projectile.Center;
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, center, GodDraw.Add(DeepColor, HazeIntensity * env), 0f, radius * HazeScale / glow.Width);
            GodDraw.End();
            DrawSwirl(center, radius, HorizonRadius / Radius, env);
            //判定边界描一圈亮线,一眼可读
            GodDraw.BeginAdditive();
            GodDraw.RingStrip(GodDraw.SoftLine, center, radius, Math.Max(RimMinWidth, radius * RimWidthRatio), GodDraw.Add(HotColor, RimIntensity * env), 0f, MathHelper.TwoPi, RimSegments);
            GodDraw.End();
            return false;
        }

        //预乘输出配 AlphaBlend,黑盘是真遮挡;着色器缺失时退回环加黑盘
        private static void DrawSwirl(Vector2 center, float half, float coreFraction, float opacity) {
            Effect shader = CEEffectAssets.VDSingularity?.Value;
            if (shader == null) {
                Texture2D disc = GodDraw.Disc;
                GodDraw.BeginAdditive();
                GodDraw.Centered(GodDraw.BloomRing, center, GodDraw.Add(DiskColor, opacity), 0f, GodDraw.RingScale(half * FallbackRingRatio));
                GodDraw.End();
                Main.spriteBatch.Draw(disc, center - Main.screenPosition, null, Color.Black * opacity, 0f, disc.Size() / 2f, half * coreFraction * 2f / disc.Width, SpriteEffects.None, 0f);
                return;
            }
            Texture2D quad = CEUtils.getExtraTex("white");
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
            gd.Textures[1] = CEUtils.getExtraTex("TurbulentNoise");
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uColor"]?.SetValue(DiskColor.ToVector3());
            shader.Parameters["uColor2"]?.SetValue(HotColor.ToVector3());
            shader.Parameters["uCoreRadius"]?.SetValue(coreFraction);
            shader.Parameters["uOpacity"]?.SetValue(opacity);
            shader.Parameters["uSpin"]?.SetValue(SwirlSpin);
            shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(quad, center - Main.screenPosition, null, Color.White, 0f, quad.Size() / 2f, half * 2f / quad.Width, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            gd.Textures[1] = null;
        }
    }
}
