using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.SharkSquad
{
    /// <summary>快乐小鲨喷的禁戒碎片: 直飞、不穿透敌怪、不受物块阻挡;首帧在各端放喷吐声并让小鲨张嘴后坐</summary>
    public class SharkSquadShard : ModProjectile
    {
        public const int HitboxSize = 12;          //碰撞箱边长(像素),贴图 16×24
        public const int Lifetime = 50;            //帧,初速 17 时约 850 像素
        public const int TrailLength = 6;          //残影缓存的帧数
        public const int TrailMode = 2;            //原版拖尾缓存模式: 位置、转角都记
        public const float TrailAlpha = 0.5f;      //最近一道残影的不透明度
        public const float TrailMinScale = 0.6f;   //最远一道残影的缩放,不小于本体一半
        public const float BodyGlow = 0.6f;        //本体向全亮靠拢的比例
        public const float LightStrength = 0.3f;   //照明强度

        //以下只管表现
        public const int MuzzleDust = 4;
        public const float MuzzleSpread = 0.5f;    //出膛尘的散角(弧度)
        public const float MuzzleSpeedMin = 2f;    //像素/帧
        public const float MuzzleSpeedMax = 5f;
        public const int TrailDustChance = 3;      //每帧有三分之一的机会掉一粒沙
        public const float TrailDustDrag = 0.15f;  //沙尘带走的速度比例
        public const int BreakDust = 8;
        public const float BreakSpread = 1.2f;     //碎裂尘的散角(弧度)
        public const float BreakSpeedMin = 1.5f;   //像素/帧
        public const float BreakSpeedMax = 6f;
        public const float BreakScaleMin = 0.9f;
        public const float BreakScaleMax = 1.3f;
        public const float SpitVolume = 0.4f;
        public const float SpitPitch = 0.35f;      //SoundStyle.Pitch,0 为原调
        public const float SpitPitchVariance = 0.15f;
        public const float BreakVolume = 0.35f;
        public const float BreakPitch = 0.4f;
        public const float BreakPitchVariance = 0.2f;
        public const int SoundCap = 4;             //同一音效的并发上限
        public static readonly Color LightColor = new Color(255, 214, 96);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            //小鲨自己穿物块,对齐点常落在墙里,碎片不穿就会出膛即碎
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.aiStyle = -1;
        }

        public override void AI() {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                SharkSquadMinionBase.Find<SharkSquadHappy>(Projectile.owner)?.OnShot(dir, SharkSquadHappy.SpitRecoil);
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = SpitVolume, Pitch = SpitPitch, PitchVariance = SpitPitchVariance, MaxInstances = SoundCap }, Projectile.Center);
                    for (int i = 0; i < MuzzleDust; i++) {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Sandnado, dir.RotatedByRandom(MuzzleSpread) * Main.rand.NextFloat(MuzzleSpeedMin, MuzzleSpeedMax));
                        dust.noGravity = true;
                    }
                }
            }
            //贴图尖端朝上
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, LightColor.ToVector3() * LightStrength);
            if (!Main.dedServ && Main.rand.NextBool(TrailDustChance)) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Sand, -Projectile.velocity * TrailDustDrag);
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < BreakDust; i++) {
                bool glint = i % 2 == 0;
                Vector2 velocity = dir.RotatedByRandom(BreakSpread) * Main.rand.NextFloat(BreakSpeedMin, BreakSpeedMax);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, glint ? DustID.Sandnado : DustID.Sand, velocity, 0, default, Main.rand.NextFloat(BreakScaleMin, BreakScaleMax));
                dust.noGravity = glint;
            }
            //飞到头散掉不出声
            if (timeLeft > 0) {
                SoundEngine.PlaySound(SoundID.Dig with { Volume = BreakVolume, Pitch = BreakPitch, PitchVariance = BreakPitchVariance, MaxInstances = SoundCap }, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Color body = Color.Lerp(lightColor, Color.White, BodyGlow);
            int length = Projectile.oldPos.Length;
            for (int i = length - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float near = 1f - i / (float)length;
                Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, at, null, body * (near * TrailAlpha), Projectile.oldRot[i], origin, MathHelper.Lerp(TrailMinScale, 1f, near), SpriteEffects.None);
            }
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, body, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    /// <summary>
    /// 懒散小鲨的诅咒火球: 追着目标飞,穿物块不穿透,命中施加诅咒狱火
    /// ai[0] 目标敌怪索引加 1(0 无);目标没了由拥有者端就近换一个并同步,其余端只照 ai[0] 转向
    /// </summary>
    public class SharkSquadCursedFireball : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flamelash;

        public const int VanillaFrames = 6;        //原版烈焰火鞭贴图 6 帧竖排,每帧 48×64,火尖朝上
        public const int FrameTicks = 4;           //每张帧停留的帧数
        public const int HitboxSize = 16;          //碰撞箱边长(像素)
        public const int Lifetime = 150;           //帧
        public const float MaxSpeed = 13f;         //像素/帧
        public const float Accel = 0.35f;          //每帧提速(像素/帧)
        public const float BaseHoming = 0.08f;     //方向每帧向目标靠拢的比例
        public const float HomingGrowth = 0.002f;  //靠拢比例每帧增长,飞得越久拐得越急,不会一直绕圈
        public const float MaxHoming = 0.3f;
        public const float RetargetRange = 700f;   //换目标的搜索半径(像素)
        public const int CurseTime = 240;          //策划: 诅咒狱火 4 秒
        public const int TrailLength = 8;          //残影缓存的帧数
        public const int TrailMode = 2;            //原版拖尾缓存模式: 位置、转角都记
        public const float TrailAlpha = 0.5f;      //最近一道残影的不透明度
        public const float TrailMinScale = 0.55f;  //最远一道残影的缩放,不小于本体一半
        public const float LightStrength = 0.5f;   //照明强度

        //以下只管表现
        public const int TrailDustChance = 2;      //每帧有一半的机会掉一粒火星
        public const float TrailDustRadius = 5f;   //像素
        public const float TrailDustDrag = 0.2f;   //火星带走的速度比例
        public const float TrailDustScaleMin = 1f;
        public const float TrailDustScaleMax = 1.5f;
        public const int BurstDust = 10;
        public const float BurstSpeed = 5f;        //像素/帧
        public const float BurstScaleMin = 1.2f;
        public const float BurstScaleMax = 1.8f;
        public const float BurstRingStart = 0.05f; //命中环的起始缩放
        public const float BurstRingScale = 0.35f; //命中环的终值缩放,环贴图 156 像素,即约 55 像素
        public const int BurstRingLife = 10;       //帧
        public const float CastVolume = 0.35f;
        public const float CastPitchVariance = 0.15f;
        public const float BurstVolume = 0.35f;
        public const float BurstPitchVariance = 0.2f;
        public const int SoundCap = 3;             //同一音效的并发上限
        /// <summary>火团圆心在帧内的像素坐标,火尖在它上方</summary>
        public static readonly Vector2 FlameOrigin = new Vector2(24f, 40f);
        public static readonly Color BodyColor = new Color(150, 255, 60);
        /// <summary>叠在本体上的加色层,A 为 0</summary>
        public static readonly Color GlowColor = new Color(40, 120, 10, 0);
        public static readonly Color LightColor = new Color(120, 220, 40);

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = VanillaFrames;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.aiStyle = -1;
        }

        public override void AI() {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            if (Projectile.localAI[0] == 0f) {
                SharkSquadMinionBase.Find<SharkSquadLazy>(Projectile.owner)?.OnShot(dir, SharkSquadLazy.CastRecoil);
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = CastVolume, PitchVariance = CastPitchVariance, MaxInstances = SoundCap }, Projectile.Center);
                }
            }
            //localAI[0] 已飞帧数
            Projectile.localAI[0]++;

            NPC target = HomingTarget();
            float speed = Math.Min(MaxSpeed, Projectile.velocity.Length() + Accel);
            if (target != null) {
                Vector2 want = (target.Center - Projectile.Center).SafeNormalize(dir);
                float turn = Math.Min(MaxHoming, BaseHoming + Projectile.localAI[0] * HomingGrowth);
                dir = Vector2.Lerp(dir, want, turn).SafeNormalize(want);
            }
            Projectile.velocity = dir * speed;
            //贴图火尖朝上,转成火尖拖在身后
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            if (++Projectile.frameCounter >= FrameTicks) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % VanillaFrames;
            }
            Lighting.AddLight(Projectile.Center, LightColor.ToVector3() * LightStrength);
            if (!Main.dedServ && Main.rand.NextBool(TrailDustChance)) {
                Vector2 at = Projectile.Center + Main.rand.NextVector2Circular(TrailDustRadius, TrailDustRadius);
                Dust dust = Dust.NewDustPerfect(at, DustID.CursedTorch, -Projectile.velocity * TrailDustDrag, 0, default, Main.rand.NextFloat(TrailDustScaleMin, TrailDustScaleMax));
                dust.noGravity = true;
            }
        }

        private NPC HomingTarget() {
            int index = (int)Projectile.ai[0] - 1;
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].CanBeChasedBy(Projectile)) {
                return Main.npc[index];
            }
            if (Projectile.owner != Main.myPlayer) {
                return null;
            }
            NPC best = null;
            float bestDist = RetargetRange;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile)) {
                    continue;
                }
                float dist = npc.Distance(Projectile.Center);
                if (dist < bestDist) {
                    bestDist = dist;
                    best = npc;
                }
            }
            int slot = best == null ? 0 : best.whoAmI + 1;
            if (slot != (int)Projectile.ai[0]) {
                Projectile.ai[0] = slot;
                Projectile.netUpdate = true;
            }
            return best;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.CursedInferno, CurseTime);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < BurstDust; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.CursedTorch, Main.rand.NextVector2Circular(BurstSpeed, BurstSpeed), 0, default, Main.rand.NextFloat(BurstScaleMin, BurstScaleMax));
                dust.noGravity = true;
            }
            //飞到头灭掉不放环不出声
            if (timeLeft <= 0) {
                return;
            }
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, LightColor, BurstRingStart).Configure(BurstRingScale, BurstRingLife);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = BurstVolume, PitchVariance = BurstPitchVariance, MaxInstances = SoundCap }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, VanillaFrames, 0, Projectile.frame);
            int length = Projectile.oldPos.Length;
            for (int i = length - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float near = 1f - i / (float)length;
                Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, at, frame, BodyColor * (near * TrailAlpha), Projectile.oldRot[i], FlameOrigin, MathHelper.Lerp(TrailMinScale, 1f, near), SpriteEffects.None);
            }
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, center, frame, BodyColor, Projectile.rotation, FlameOrigin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, frame, GlowColor, Projectile.rotation, FlameOrigin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    /// <summary>
    /// 勇敢小鲨的神圣光剑: 直飞,一帧两次更新,穿物块不穿透,无视 20 点护甲
    /// ai[1] 是拥有者端掷出的剑形(原版贴图的第几帧)
    /// </summary>
    public class SharkSquadLightSword : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SkyFracture;

        public const int Variants = 14;            //原版裂天剑贴图 14 帧横排,每帧 38×38,剑尖朝右上
        public const int ArmorIgnore = 20;         //策划: 无视 20 点护甲
        public const int HitboxSize = 14;          //碰撞箱边长(像素)
        public const int ExtraUpdates = 1;         //每帧多跑一次更新
        public const int Lifetime = 44;            //更新次数,即 22 帧;每次更新 14 像素,约 610 像素
        public const int FadeInUpdates = 4;
        public const int FadeOutUpdates = 8;
        public const int TrailLength = 8;          //残影缓存的更新次数
        public const int TrailMode = 2;            //原版拖尾缓存模式: 位置、转角都记
        public const float TrailAlpha = 0.55f;     //最近一道残影的不透明度
        public const float TrailMinScale = 0.6f;   //最远一道残影的缩放,不小于本体一半
        public const float LightStrength = 0.45f;  //照明强度

        //以下只管表现
        public const int TrailDustChance = 3;      //每次更新有三分之一的机会掉一粒光尘
        public const float TrailDustRadius = 4f;   //像素
        public const float TrailDustDrag = 0.1f;   //光尘带走的速度比例
        public const int BurstDust = 8;
        public const float BurstSpread = 1.3f;     //命中尘朝来向溅开的散角(弧度)
        public const float BurstSpeedMin = 2f;     //像素/帧
        public const float BurstSpeedMax = 6f;
        public const float BurstScaleMin = 1f;
        public const float BurstScaleMax = 1.5f;
        public const float ShineScale = 0.5f;      //命中闪光的缩放,光晕贴图 256 像素、亮芯约四分之一
        public const int ShineLife = 10;           //帧
        public const float CastVolume = 0.3f;
        public const float CastPitch = 0.25f;      //SoundStyle.Pitch,0 为原调
        public const float CastPitchVariance = 0.15f;
        public const float BurstVolume = 0.3f;
        public const float BurstPitch = 0.4f;
        public const float BurstPitchVariance = 0.2f;
        public const int SoundCap = 3;             //同一音效的并发上限
        public static readonly Color BodyColor = new Color(255, 150, 215);
        /// <summary>叠在本体上的加色层,A 为 0</summary>
        public static readonly Color GlowColor = new Color(100, 15, 65, 0);
        public static readonly Color LightColor = new Color(255, 120, 200);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.ArmorPenetration = ArmorIgnore;
            Projectile.aiStyle = -1;
        }

        public override void AI() {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            if (Projectile.localAI[0] == 0f) {
                SharkSquadMinionBase.Find<SharkSquadBrave>(Projectile.owner)?.OnShot(dir, SharkSquadBrave.FireRecoil);
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Item9 with { Volume = CastVolume, Pitch = CastPitch, PitchVariance = CastPitchVariance, MaxInstances = SoundCap }, Projectile.Center);
                }
            }
            //localAI[0] 已跑的更新次数
            Projectile.localAI[0]++;
            //贴图剑尖朝右上
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.frame = Math.Clamp((int)Projectile.ai[1], 0, Variants - 1);
            Lighting.AddLight(Projectile.Center, LightColor.ToVector3() * LightStrength);
            if (!Main.dedServ && Main.rand.NextBool(TrailDustChance)) {
                Vector2 at = Projectile.Center + Main.rand.NextVector2Circular(TrailDustRadius, TrailDustRadius);
                Dust dust = Dust.NewDustPerfect(at, DustID.PinkTorch, -Projectile.velocity * TrailDustDrag);
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) {
            //飞到头自己淡出,不放命中特效
            if (Main.dedServ || timeLeft <= 0) {
                return;
            }
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < BurstDust; i++) {
                Vector2 velocity = (-dir).RotatedByRandom(BurstSpread) * Main.rand.NextFloat(BurstSpeedMin, BurstSpeedMax);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, velocity, 0, default, Main.rand.NextFloat(BurstScaleMin, BurstScaleMax));
                dust.noGravity = true;
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, LightColor, ShineScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, ShineLife);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = BurstVolume, Pitch = BurstPitch, PitchVariance = BurstPitchVariance, MaxInstances = SoundCap }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(Variants, 1, Projectile.frame, 0);
            Vector2 origin = frame.Size() / 2f;
            float fade = MathHelper.Clamp(Projectile.localAI[0] / FadeInUpdates, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / (float)FadeOutUpdates, 0f, 1f);
            int length = Projectile.oldPos.Length;
            for (int i = length - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float near = 1f - i / (float)length;
                Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, at, frame, BodyColor * (near * TrailAlpha * fade), Projectile.oldRot[i], origin, MathHelper.Lerp(TrailMinScale, 1f, near), SpriteEffects.None);
            }
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, center, frame, BodyColor * fade, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, frame, GlowColor * fade, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
