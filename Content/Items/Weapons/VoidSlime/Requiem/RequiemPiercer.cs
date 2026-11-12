using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    public class RequiemPiercer : ModProjectile
    {
        public const int ArmorPierce = 50;          //无视的防御点数
        public const int MaxHits = 5;               //可命中的敌人数
        public const int HitboxSize = 8;            //像素
        public const int ExtraUpdates = 1;
        public const int Lifetime = 120;            //更新次数,约 60 帧 1800 像素
        public const int TrailLength = 5;           //拖尾记录的更新次数
        public const int TrailMode = 0;             //只记录位置
        public const float TrailWidth = 4f;         //像素,与弹体同宽
        public const float LightStrength = 0.45f;
        public const float ShotPitchMin = 1.1f;     //CEUtils.PlaySound 音高,1 为原调
        public const float ShotPitchMax = 1.2f;
        public const float ShotVolume = 0.3f;
        public const int ShotMaxInstances = 6;
        public const int HitSparks = 3;
        public const float HitSparkSpread = 0.6f;   //弧度
        public const float HitSparkSpeedMin = 4f;   //像素/帧
        public const float HitSparkSpeedMax = 9f;
        public const int HitSparkLife = 10;         //帧
        public const float HitPitchMin = 1.5f;
        public const float HitPitchMax = 1.7f;
        public const float HitVolume = 0.22f;
        public const int HitMaxInstances = 4;
        public const int DeathSparks = 4;
        public const float DeathSparkSpread = 1.2f; //弧度
        public const int DeathSparkLife = 12;       //帧

        private bool fired;
        private readonly bool[] remoteFx = new bool[Main.maxNPCs];  //远端命中火花去重

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = MaxHits;
            Projectile.ArmorPenetration = ArmorPierce;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, RequiemVfx.Gold.ToVector3() * LightStrength);
            if (!fired) {
                fired = true;
                OnFire();
            }
            if (Projectile.owner != Main.myPlayer)
                RemoteHitFx();
        }

        private void OnFire() {
            Player owner = Main.player[Projectile.owner];
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            RequiemVfx.MuzzleFlash(Projectile.Center, aim, RequiemVfx.Gold, owner.velocity);
            if (Projectile.Entropy().IndexOfTwistedTwinShootedThisProj >= 0)
                return;
            CEUtils.PlaySound("gunshot_large", Main.rand.NextFloat(ShotPitchMin, ShotPitchMax), Projectile.Center, ShotMaxInstances, ShotVolume);
            RequiemVfx.Shell(owner, Projectile.Center, aim);
        }

        private void RemoteHitFx() {
            if (Main.dedServ)
                return;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (remoteFx[npc.whoAmI] || !npc.CanBeChasedBy() || !Projectile.Hitbox.Intersects(npc.Hitbox))
                    continue;
                remoteFx[npc.whoAmI] = true;
                HitFx();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            HitFx();
        }

        private void HitFx() {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            RequiemVfx.Sparks(Projectile.Center, dir, HitSparks, HitSparkSpread, HitSparkSpeedMin, HitSparkSpeedMax, RequiemVfx.GoldDeep, RequiemVfx.GoldWhite, HitSparkLife);
            CEUtils.PlaySound("metalhit", Main.rand.NextFloat(HitPitchMin, HitPitchMax), Projectile.Center, HitMaxInstances, HitVolume);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            return true;
        }

        public override void OnKill(int timeLeft) {
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            RequiemVfx.Sparks(Projectile.Center, back, DeathSparks, DeathSparkSpread, HitSparkSpeedMin, HitSparkSpeedMax, RequiemVfx.GoldDeep, RequiemVfx.Gold, DeathSparkLife);
        }

        public override bool PreDraw(ref Color lightColor) {
            RequiemVfx.DrawTrail(Projectile, TrailLength, RequiemVfx.GoldWhite, RequiemVfx.GoldDeep, TrailWidth);
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
