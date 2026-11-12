using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.EventHorizon
{
    //黑洞传送起点与终点各一个短暂的黑洞,纯视觉
    //ai0 哪一端(Entry 或 Exit),localAI0 开场标记,localAI1 已过帧数
    public class EventHorizonHole : ModProjectile
    {
        public const float Entry = 0f;
        public const float Exit = 1f;
        //时间单位帧: 6 帧张开,第 22 帧起合拢,第 36 帧消失
        private const int OpenFrames = 6;
        private const int CloseStart = 22;
        private const int LifeFrames = 36;
        //保险寿命,单位次更新,豆奶让每帧多跑更新时也够用
        private const int SafetyLifetime = LifeFrames * 3;
        //黑盘半径,单位像素;黑盘占旋流盘的比例同虚空史莱姆的黑洞;旋流转速
        private const float CoreRadius = 40f;
        private const float CoreFraction = 0.5f;
        private const float Spin = 1.8f;
        //扭曲走仓库奇点的 VDScreenFx 透镜,Boss 用 0.09 / 520;同一帧只留最强的一个,镜头所在的终点更强
        private const float LensStrengthExit = 0.07f;
        private const float LensStrengthEntry = 0.05f;
        private const float LensRadius = 150f;
        //每帧吸入的火花数与寿命帧数
        private const int InwardSparks = 2;
        private const int SparkLife = 12;
        //传送音效只在终点放,同时实例上限与音量
        private const int SoundInstances = 2;
        private const float SoundVolume = 0.5f;

        private static readonly Color DiskColor = new(150, 170, 220);
        private static readonly Color HotColor = new(235, 245, 255);
        private static readonly Color HaloColor = new(40, 50, 90);

        private bool IsExit => Projectile.ai[0] == Exit;

        private float Size {
            get {
                float t = Projectile.localAI[1];
                float open = GodDraw.EaseOut(t / OpenFrames);
                float close = 1f - GodDraw.EaseIn((t - CloseStart) / (LifeFrames - CloseStart));
                return open * close;
            }
        }

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = SafetyLifetime;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (IsExit)
                    CEUtils.PlaySound("teleport", Main.rand.NextFloat(0.95f, 1.05f), Projectile.Center, SoundInstances, CEUtils.WeapSound * SoundVolume);
            }
            //物品生成的弹幕会被豆奶加更新,计时只在每帧最后一次更新推进
            bool frameTick = Projectile.FinalExtraUpdate();
            if (frameTick)
                Projectile.localAI[1]++;
            if (Projectile.localAI[1] >= LifeFrames) {
                Projectile.Kill();
                return;
            }
            if (Main.dedServ)
                return;
            float size = Size;
            VDScreenFx.ReportLens(Projectile.Center, (IsExit ? LensStrengthExit : LensStrengthEntry) * size, LensRadius);
            if (!frameTick)
                return;
            float reach = CoreRadius / CoreFraction * size;
            for (int i = 0; i < InwardSparks; i++) {
                Vector2 dir = Main.rand.NextVector2Unit();
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + dir * reach * Main.rand.NextFloat(0.9f, 1.4f), (-dir).RotatedBy(0.6f) * Main.rand.NextFloat(2.5f, 4.5f), HotColor * 0.8f, 0.03f)
                    .Configure(false, SparkLife, new Vector2(0.5f, 1.3f));
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            float size = Size;
            if (size <= 0.01f)
                return false;
            float core = CoreRadius * size;
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(HaloColor, 0.8f * size), 0f, core * 5.4f / glow.Width);
            GodDraw.End();
            DrawSwirl(Projectile.Center, core / CoreFraction, CoreFraction, DiskColor, HotColor, Spin, size);
            GodDraw.BeginAdditive();
            GodDraw.RingStrip(GodDraw.SoftLine, Projectile.Center, core, Math.Max(3f, core * 0.035f), GodDraw.Add(HotColor, 0.85f * size), 0f, MathHelper.TwoPi, 48);
            GodDraw.End();
            return false;
        }

        //旋流盘走 VDSingularity,预乘输出配 AlphaBlend,黑盘是真遮挡;着色器缺失时退回光环加黑盘
        private static void DrawSwirl(Vector2 center, float half, float coreFraction, Color disk, Color hot, float spin, float opacity) {
            Effect shader = CEEffectAssets.VDSingularity?.Value;
            if (shader == null) {
                Texture2D disc = GodDraw.Disc;
                GodDraw.BeginAdditive();
                GodDraw.Centered(GodDraw.BloomRing, center, GodDraw.Add(disk, opacity), 0f, GodDraw.RingScale(half * 0.75f));
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
            shader.Parameters["uColor"]?.SetValue(disk.ToVector3());
            shader.Parameters["uColor2"]?.SetValue(hot.ToVector3());
            shader.Parameters["uCoreRadius"]?.SetValue(coreFraction);
            shader.Parameters["uOpacity"]?.SetValue(opacity);
            shader.Parameters["uSpin"]?.SetValue(spin);
            shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(quad, center - Main.screenPosition, null, Color.White, 0f, quad.Size() / 2f, half * 2f / quad.Width, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            gd.Textures[1] = null;
        }
    }
}
