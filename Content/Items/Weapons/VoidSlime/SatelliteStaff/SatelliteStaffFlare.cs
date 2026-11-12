using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SatelliteStaff
{
    /// <summary>橙色耀斑射线:快速直线飞行,最多命中5个目标、每个敌人一次;第一次命中时在目标处炸一次破晓同款的爆炸</summary>
    public class SatelliteStaffFlare : ModProjectile
    {
        #region 调参旋钮
        //判定边长(像素)、寿命(更新次数,每帧5次更新)、最多命中数(策划)
        public const int Size = 16;
        public const int Lifetime = 200;
        public const int ExtraUpdates = 4;
        public const int MaxHits = 5;
        //光带:长度(像素)、半宽(像素)、头部光晕直径(像素)
        public const float StreakLength = 150f;
        public const float HalfWidth = 7f;
        public const float HeadGlow = 48f;
        //爆炸伤害相对射线本身(射线 100%、爆炸 150%,策划)
        public const float ExplosionMult = 1.5f;
        //烈日灼烧时长(帧,3秒,策划)
        public const int BurnFrames = 180;
        //光带形状:中间两个节点的位置(尾 0 到头 1)、尾端宽度比例、短于这个长度(像素)不画
        public const float MidKnot = 0.5f;
        public const float HeadKnot = 0.85f;
        public const float TailWidth = 0.3f;
        public const float MinVisibleLength = 2f;
        //外晕宽度倍率与强度、头部光晕强度、亮芯宽度比例
        public const float OuterWidth = 2f;
        public const float OuterOpacity = 0.6f;
        public const float HeadGlowOpacity = 0.8f;
        public const float CoreWidth = 0.4f;
        //照明强度;音效音高与随机幅度、音量与同时发声上限
        public const float LightStrength = 0.8f;
        public const float SoundPitch = 1.2f;
        public const float SoundPitchBand = 0.1f;
        public const float SoundVolume = 0.4f;
        public const int SoundMaxInstances = 3;
        #endregion

        public static readonly Color Outer = new(255, 104, 28);
        public static readonly Color Body = new(255, 170, 60);
        public static readonly Color Core = new(255, 240, 170);

        //飞行过的更新次数(光带从出手点长出来);爆炸只在主人端触发一次
        private int age;
        private bool exploded;

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = MaxHits;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI() {
            age++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Body.ToVector3() * LightStrength);
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (!Main.dedServ) {
                    VSlimeVfx.Cue("lasershoot", Projectile.Center, SoundPitch + Main.rand.NextFloat(-SoundPitchBand, SoundPitchBand), SoundVolume, SoundMaxInstances);
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<SunblazeBurn>(), BurnFrames);
            if (exploded || Projectile.owner != Main.myPlayer) {
                return;
            }
            exploded = true;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<SatelliteStaffExplosion>(),
                (int)(Projectile.damage * ExplosionMult), Projectile.knockBack, Projectile.owner);
        }

        //加法两层光带加头部光晕;直通道再画一条实色亮芯
        public override bool PreDraw(ref Color lightColor) {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float length = Math.Min(StreakLength, age * Projectile.velocity.Length());
            if (length < MinVisibleLength) {
                return false;
            }
            Vector2 head = Projectile.Center;
            Vector2 tail = head - dir * length;
            Vector2[] line = { tail, Vector2.Lerp(tail, head, MidKnot), Vector2.Lerp(tail, head, HeadKnot), head };
            GodDraw.BeginAdditive();
            GodDraw.Strip(GodDraw.SoftLine, line, t => HalfWidth * OuterWidth * MathHelper.Lerp(TailWidth, 1f, t), t => GodDraw.Add(Outer, OuterOpacity * t));
            GodDraw.Strip(GodDraw.SoftLine, line, t => HalfWidth * MathHelper.Lerp(TailWidth, 1f, t), t => GodDraw.Add(Body, t));
            GodDraw.Centered(GodDraw.Glow, head, GodDraw.Add(Body, HeadGlowOpacity), 0f, HeadGlow / GodDraw.Glow.Width);
            GodDraw.End();
            GodDraw.BeginStraightAlpha();
            GodDraw.Strip(CEUtils.getExtraTex("white"), line, t => HalfWidth * CoreWidth * t, t => GodDraw.Straight(Core, t));
            GodDraw.End();
            return false;
        }
    }
}
