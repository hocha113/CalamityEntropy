using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.Constellation
{
    //运动走原版 aiStyle 99; 主球取同主人同类型中 identity 最小者, 只有主球召唤五芒星, 发星时所有同类型悠悠球同帧各发一颗
    public class ConstellationYoyo : ModProjectile
    {
        //时间单位帧, 距离单位 px, 速度单位 px/帧
        public const float MaxRange = 640f;
        public const float TopSpeed = 19f;
        public const int HitCooldown = 10;
        public const int StarInterval = 30;
        public const int StarIntervalFast = 20;
        public const int FastWindow = 60;
        public const int PentagramLife = 180;
        public const float PentagramDamageMult = 0.5f;
        public const float StarDamageMult = 0.5f;

        public static readonly Color TrailColor = new(110, 150, 255);
        public static readonly Color GlowColor = new(150, 190, 255);

        private int starTimer;
        private int fastTimer;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = -1f;
            ProjectileID.Sets.YoyosMaximumRange[Type] = MaxRange;
            ProjectileID.Sets.YoyosTopSpeed[Type] = TopSpeed;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
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
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = HitCooldown;
        }

        public override void PostAI() {
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.5f);
            if (Projectile.owner != Main.myPlayer || FindMain() != Projectile)
                return;
            if (fastTimer > 0)
                fastTimer--;
            if (FindPentagram(Projectile.owner, Projectile.identity) == null) {
                starTimer = 0;
                return;
            }
            if (++starTimer < (fastTimer > 0 ? StarIntervalFast : StarInterval))
                return;
            starTimer = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == Type)
                    FireStar(p);
            }
        }

        private void FireStar(Projectile from) {
            Vector2 velocity = Main.rand.NextVector2CircularEdge(ConstellationStar.LaunchSpeed, ConstellationStar.LaunchSpeed);
            Projectile.NewProjectile(from.GetSource_FromThis(), from.Center, velocity, ModContent.ProjectileType<ConstellationStar>(), (int)(Projectile.damage * StarDamageMult), 1f, Projectile.owner, -1f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Projectile main = FindMain();
            if (main?.ModProjectile is ConstellationYoyo mainYoyo)
                mainYoyo.fastTimer = FastWindow;
            if (main != Projectile)
                return;
            Projectile pentagram = FindPentagram(Projectile.owner, Projectile.identity);
            if (pentagram != null) {
                pentagram.ai[1] = PentagramLife;
                pentagram.netUpdate = true;
                return;
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ConstellationPentagram>(), (int)(Projectile.damage * PentagramDamageMult), 0f, Projectile.owner, Projectile.identity, PentagramLife);
        }

        private Projectile FindMain() {
            Projectile main = null;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == Type && (main == null || p.identity < main.identity))
                    main = p;
            }
            return main;
        }

        public static Projectile FindPentagram(int owner, int hostIdentity) {
            int type = ModContent.ProjectileType<ConstellationPentagram>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == owner && p.type == type && (int)p.ai[0] == hostIdentity)
                    return p;
            }
            return null;
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
            Main.EntitySpriteDraw(glow, pos, null, (GlowColor with { A = 0 }) * 0.4f, 0f, glow.Size() / 2f, 0.22f, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, Color.Lerp(lightColor, Color.White, 0.4f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
