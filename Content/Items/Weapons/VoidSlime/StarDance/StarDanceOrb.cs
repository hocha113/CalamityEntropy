using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarDance
{
    /// <summary>
    /// 星舞五色能量球的公共部分:同色拖尾与残影、消失时快速缩小;
    /// ai[2] 为 1 是命中后消失,为 2 是被挤出圆轨、不出命中光效;命中只在主人端判定,靠 netUpdate 告诉其他端
    /// </summary>
    public abstract class StarDanceOrb : ModProjectile
    {
        #region 调参旋钮
        //判定边长(像素),与贴图同大
        public const int Size = 18;
        //消失时缩到无的帧数(任务文件约8帧)、刚出现时由小变大的帧数与起始缩放
        public const int ShrinkFrames = 8;
        public const int GrowFrames = 5;
        public const float GrowFrom = 0.4f;
        //直飞的球缩小期间每次更新的减速
        public const float DyingDrag = 0.8f;
        //拖尾:记录的旧位置数、带子半宽(像素,BasicTrail 亮带只占中间一半,可见宽度约等于本体)、内芯宽度比
        public const int TrailLength = 14;
        public const float TrailHalfWidth = 14f;
        public const float TrailCoreRatio = 0.4f;
        //残影:隔几个旧位置画一个、画几个、不透明度、尾端缩放
        public const int AfterimageStep = 2;
        public const int AfterimageCount = 4;
        public const float AfterimageOpacity = 0.45f;
        public const float AfterimageMinScale = 0.55f;
        //光晕:Glow2 原图256像素,0.22倍约56像素;光照强度
        public const float GlowScale = 0.22f;
        public const float GlowOpacity = 0.7f;
        public const float LightStrength = 0.6f;
        //飞行中的碎光:每帧1/N概率、散布(像素)、寿命(帧)、缩放
        public const int MoteChance = 6;
        public const float MoteSpread = 6f;
        public const int MoteLife = 16;
        public const float MoteScale = 0.22f;
        //命中消失的星芒:缩放、寿命(帧);同色火星数量、偏离来向的最大角(弧度)、速度(像素/帧)、缩放、寿命(帧)
        public const float BurstScale = 0.45f;
        public const int BurstLife = 12;
        public const int BurstSparks = 3;
        public const float BurstSparkSpread = 0.7f;
        public const float BurstSparkSpeed = 2.5f;
        public const float BurstSparkScale = 0.25f;
        public const int BurstSparkLife = 16;
        #endregion

        //球体主色与浅色边框色;拖尾外层用主色、内芯用边框色
        protected abstract Color Body { get; }
        protected abstract Color Rim { get; }
        //总寿命(更新次数)与能命中几次
        protected abstract int Lifetime { get; }
        protected virtual int HitCount => 1;
        //寿命按真实帧算,豆奶多出的更新不扣寿命
        protected virtual bool RealFrameLife => false;

        //出现后经过的真实帧数
        protected int age;
        private int hitsLeft;
        private bool burstDone;

        protected bool Dying => Projectile.ai[2] > 0f || Projectile.timeLeft <= ShrinkFrames;
        protected Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
            hitsLeft = HitCount;
        }

        public override bool? CanDamage() => Dying ? false : null;

        public override bool? CanHitNPC(NPC target) => Dying ? false : null;

        public override bool? CanCutTiles() => false;

        public override void AI() {
            bool lastUpdate = Projectile.FinalExtraUpdate();
            if (lastUpdate)
                age++;
            else if (RealFrameLife)
                Projectile.timeLeft++;
            if (Projectile.ai[2] > 0f && Projectile.timeLeft > ShrinkFrames)
                Projectile.timeLeft = ShrinkFrames;
            Move();
            if (!Projectile.active)
                return;
            Lighting.AddLight(Projectile.Center, Body.ToVector3() * (LightStrength * DrawScale()));
            if (Main.dedServ)
                return;
            if (Projectile.ai[2] == 1f && !burstDone) {
                burstDone = true;
                Burst();
            }
            if (lastUpdate && !Dying && Main.rand.NextBool(MoteChance)) {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(MoteSpread, MoteSpread);
                PRTLoader.NewParticle<PRT_Light>(pos, Projectile.velocity * -0.1f, Body, MoteScale).Configure(1f, lifetime: MoteLife);
            }
        }

        protected abstract void Move();

        //其他端也可以先行标记,以主人端同步来的为准
        protected void StartDying(bool hit) {
            if (Projectile.ai[2] > 0f)
                return;
            Projectile.ai[2] = hit ? 1f : 2f;
            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (--hitsLeft <= 0)
                StartDying(true);
        }

        //一次施法最多九颗球同时命中,音量压低、同时最多响两声;火星顺着球的来向散开
        protected void Burst() {
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.35f, PitchVariance = 0.3f, MaxInstances = 2, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, Projectile.Center);
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Rim, BurstScale).Configure(Body, BurstLife);
            Vector2 heading = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < BurstSparks; i++) {
                Vector2 vel = heading.RotatedByRandom(BurstSparkSpread) * (BurstSparkSpeed * Main.rand.NextFloat(0.6f, 1f));
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center, vel, Body, BurstSparkScale).Configure(1f, lifetime: BurstSparkLife);
            }
        }

        //出现时由小变大,消失时快速缩小
        protected float DrawScale() {
            float grow = MathHelper.Lerp(GrowFrom, 1f, Utils.GetLerpValue(0f, GrowFrames, age, true));
            float shrink = Dying ? Utils.GetLerpValue(0f, ShrinkFrames, Projectile.timeLeft, true) : 1f;
            return Projectile.scale * grow * shrink;
        }

        public override bool PreDraw(ref Color lightColor) {
            float scale = DrawScale();
            if (scale <= 0f)
                return false;
            DrawTrail(scale);
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            for (int k = 1; k <= AfterimageCount; k++) {
                int i = k * AfterimageStep;
                if (i >= Projectile.oldPos.Length || Projectile.oldPos[i] == Vector2.Zero)
                    break;
                float fade = 1f - k / (float)(AfterimageCount + 1);
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, null, Color.White * (AfterimageOpacity * fade), 0f, origin, scale * MathHelper.Lerp(AfterimageMinScale, 1f, fade), SpriteEffects.None);
            }
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 center = Projectile.Center - Main.screenPosition;
            //A为0的颜色在常规混合下等同叠加发光
            Main.EntitySpriteDraw(glow, center, null, (Body with { A = 0 }) * GlowOpacity, 0f, glow.Size() / 2f, GlowScale * scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, null, Color.White, 0f, origin, scale, SpriteEffects.None);
            return false;
        }

        //两层叠加带子:外层主色、内芯边框色,从头到尾变窄变淡
        //GetVertexesList 把第一个点宽度当成0,所以从尾端往头部排;一帧多次更新会记下重合的旧位置,跳过以免带子提前收窄
        private void DrawTrail(float scale) {
            int count = 0;
            while (count < Projectile.oldPos.Length && Projectile.oldPos[count] != Vector2.Zero)
                count++;
            if (count < 3)
                return;
            List<CEUtils.VertexPointSets> outer = new();
            List<CEUtils.VertexPointSets> core = new();
            Vector2 last = new(float.NaN);
            for (int i = count - 1; i >= 0; i--) {
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
                if (Vector2.DistanceSquared(pos, last) < 0.25f)
                    continue;
                last = pos;
                float k = 1f - i / (float)(count - 1);
                outer.Add(new CEUtils.VertexPointSets(pos, Body * k, TrailHalfWidth * scale * k, k));
                core.Add(new CEUtils.VertexPointSets(pos, Rim * k, TrailHalfWidth * TrailCoreRatio * scale * k, k));
            }
            if (outer.Count < 3)
                return;
            Main.spriteBatch.UseAdditive();
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            gd.Textures[0] = CEExtraAssets.BasicTrail;
            List<ColoredVertex> verts = outer.GetVertexesList(false);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, verts.ToArray(), 0, verts.Count - 2);
            verts = core.GetVertexesList(false);
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, verts.ToArray(), 0, verts.Count - 2);
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
