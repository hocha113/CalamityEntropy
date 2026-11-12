using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    public class RequiemCharger : ModProjectile
    {
        public const int HealAmount = 1;            //生命值,冷却见 RequiemPlayer
        public const int HealMinLifeMax = 5;        //目标最大生命不高于此不回血(小动物)
        public const int HitboxSize = 8;            //像素
        public const int ExtraUpdates = 1;
        public const int Lifetime = 120;            //更新次数
        public const int TrailLength = 5;           //拖尾记录的更新次数
        public const int TrailMode = 0;             //只记录位置
        public const float TrailWidth = 4f;         //像素,与弹体同宽
        public const float LightStrength = 0.5f;
        public const float CoreScale = 0.09f;       //Glow2 缩放
        public const float CorePulse = 0.25f;       //核心光呼吸幅度,相对缩放
        public const float CorePulseSpeed = 0.5f;   //弧度/更新
        public const float CoreBack = 4f;           //像素,核心光在弹体中心后方
        public const float ShotPitchMin = 1.32f;    //CEUtils.PlaySound 音高,比穿甲弹高一档
        public const float ShotPitchMax = 1.42f;
        public const float ShotVolume = 0.26f;
        public const int ShotMaxInstances = 6;
        public const int DeathSparks = 4;
        public const float DeathSparkSpread = 1.2f; //弧度
        public const float DeathSparkSpeedMin = 4f; //像素/帧
        public const float DeathSparkSpeedMax = 9f;
        public const int DeathSparkLife = 12;       //帧

        private bool fired;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, RequiemVfx.Ice.ToVector3() * LightStrength);
            if (fired)
                return;
            fired = true;
            Player owner = Main.player[Projectile.owner];
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            RequiemVfx.MuzzleFlash(Projectile.Center, aim, RequiemVfx.Ice, owner.velocity);
            if (Projectile.Entropy().IndexOfTwistedTwinShootedThisProj >= 0)
                return;
            CEUtils.PlaySound("gunshot_large", Main.rand.NextFloat(ShotPitchMin, ShotPitchMax), Projectile.Center, ShotMaxInstances, ShotVolume);
            RequiemVfx.Shell(owner, Projectile.Center, aim);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.owner != Main.myPlayer)
                return;
            int damage = (int)(Projectile.damage * RequiemShrapnel.DamageMult);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<RequiemShrapnel>(), damage, Projectile.knockBack, Projectile.owner, Projectile.velocity.ToRotation());
            TryHeal(Main.player[Projectile.owner], target, damageDone);
        }

        private static void TryHeal(Player owner, NPC target, int damageDone) {
            if (damageDone <= 0 || target.immortal || target.lifeMax <= HealMinLifeMax || owner.moonLeech || owner.statLife >= owner.statLifeMax2)
                return;
            if (!owner.GetModPlayer<RequiemPlayer>().TryStartHealCooldown())
                return;
            owner.Heal(HealAmount);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            return true;
        }

        public override void OnKill(int timeLeft) {
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            RequiemVfx.Sparks(Projectile.Center, back, DeathSparks, DeathSparkSpread, DeathSparkSpeedMin, DeathSparkSpeedMax, RequiemVfx.Ice, RequiemVfx.IceWhite, DeathSparkLife);
        }

        public override bool PreDraw(ref Color lightColor) {
            RequiemVfx.DrawTrail(Projectile, TrailLength, RequiemVfx.IceWhite, RequiemVfx.Silver, TrailWidth);
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, pos, null, Color.White * Projectile.Opacity, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            Texture2D glow = CEExtraAssets.Glow2;
            float pulse = 1f + CorePulse * MathF.Sin(Projectile.timeLeft * CorePulseSpeed);
            Color core = RequiemVfx.VoidBlue;
            core.A = 0;
            Vector2 corePos = pos - Projectile.rotation.ToRotationVector2() * CoreBack * Projectile.scale;
            Main.EntitySpriteDraw(glow, corePos, null, core * Projectile.Opacity, 0f, glow.Size() / 2f, CoreScale * pulse * Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
