using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.MassPoint
{
    //运动走原版 aiStyle 99,命中不弹开;持续命中够久就在球心生成黑洞,每颗悠悠球同时只有一个
    public class MassPointYoyo : ModProjectile
    {
        //策划: 最远 45 格,单位像素;无限滞空;局部无敌帧 10
        public const float MaxRange = 720f;
        public const int HitCooldown = 10;
        //最高速度,单位像素每帧,策划没写,取星座球的 19 略高
        public const float TopSpeed = 20f;
        //策划: 持续命中 1.5 秒成形;两次命中间隔不超过约 0.5 秒算持续,单位帧
        public const int StreakFrames = 90;
        public const int StreakGapFrames = 30;
        //策划: 黑洞伤害为悠悠球伤害的 125%
        public const float SingularityDamageMult = 1.25f;
        //拖尾长度,单位次更新
        private const int TrailLength = 8;

        public static readonly Color TrailColor = new(88, 104, 238);
        public static readonly Color GlowColor = new(164, 92, 240);

        //按帧计的时钟,豆奶加更新时只在每帧最后一次更新推进
        private int clock;
        //以下只在拥有者端有效,OnHitNPC 只在拥有者端跑
        private int lastHitFrame = -StreakGapFrames - 1;
        private int streakStartFrame = -1;
        private int singularityIndex = -1;
        private Vector2 velocityBeforeHit;

        //距上次命中的帧数,黑洞在拥有者端按它判断何时塌缩
        public int FramesSinceHit => clock - lastHitFrame;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = -1f;
            ProjectileID.Sets.YoyosMaximumRange[Type] = MaxRange;
            ProjectileID.Sets.YoyosTopSpeed[Type] = TopSpeed;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = ProjAIStyleID.Yoyo;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
        }

        public override void PostAI() {
            if (Projectile.FinalExtraUpdate())
                clock++;
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.45f);
            if (Projectile.owner != Main.myPlayer)
                return;
            if (singularityIndex >= 0 && !IsOwnSingularity(Main.projectile[singularityIndex]))
                singularityIndex = -1;
            if (singularityIndex < 0 && streakStartFrame >= 0 && FramesSinceHit <= StreakGapFrames && clock - streakStartFrame >= StreakFrames) {
                singularityIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MassPointSingularity>(), (int)(Projectile.damage * SingularityDamageMult), 0f, Projectile.owner, Projectile.identity);
                streakStartFrame = -1;
            }
        }

        private bool IsOwnSingularity(Projectile p) => p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<MassPointSingularity>() && (int)p.ai[0] == Projectile.identity;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            velocityBeforeHit = Projectile.velocity;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            //原版悠悠球在两个钩子之间把速度反弹,这里还原成命中前的速度,贴着目标继续磨
            Projectile.velocity = velocityBeforeHit;
            if (FramesSinceHit > StreakGapFrames)
                streakStartFrame = clock;
            lastHitFrame = clock;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 origin = tex.Size() / 2f;
            Vector2 half = Projectile.Size / 2f;
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, null, (TrailColor with { A = 0 }) * 0.5f * t, Projectile.rotation, origin, Projectile.scale * (0.7f + 0.3f * t), SpriteEffects.None);
            }
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, pos, null, (GlowColor with { A = 0 }) * 0.4f, 0f, glow.Size() / 2f, 0.2f, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, Color.Lerp(lightColor, Color.White, 0.4f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
