using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    /// <summary>安息弹,按目标当前生命的一成结算并无视防御与减伤,命中后永久削减防御</summary>
    public class RequiemRound : ModProjectile
    {
        public const float LifeRatio = 0.1f;        //伤害占目标当前生命,蠕虫按头部共享生命算
        public const int MinDamage = 1;
        public const int DamageCap = 1000000;
        public const int HitboxSize = 10;           //像素
        public const int ExtraUpdates = 4;
        public const int Lifetime = 200;            //更新次数,约 40 帧 2800 像素
        public const int TrailLength = 16;          //拖尾记录的更新次数
        public const int TrailMode = 0;             //只记录位置
        public const float TrailWidth = 8f;         //像素,与弹体同宽
        public static readonly Color TrailTail = new(120, 60, 200);
        public const float LightStrength = 0.8f;
        public const int WispInterval = 4;          //更新次数,每隔几次飘一缕烟
        public const float WispScale = 0.2f;        //HeavySmoke 单帧 80 像素的缩放
        public const int WispLife = 20;             //帧
        public const float WispOpacity = 0.35f;
        public const int DeathSparks = 8;
        public const float DeathSparkSpread = 1.4f; //弧度
        public const float DeathSparkSpeedMin = 5f; //像素/帧
        public const float DeathSparkSpeedMax = 12f;
        public const int DeathSparkLife = 14;       //帧

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
            Lighting.AddLight(Projectile.Center, RequiemVfx.Gold.ToVector3() * LightStrength);
            if (Main.dedServ || Projectile.timeLeft % WispInterval != 0)
                return;
            PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, Vector2.Zero, RequiemVfx.Soot * WispOpacity, WispScale).Configure(1f, WispLife);
        }

        private static NPC Body(NPC target) {
            int head = target.realLife;
            return head >= 0 && head < Main.maxNPCs && Main.npc[head].active ? Main.npc[head] : target;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            int damage = (int)Math.Clamp(Body(target).life * (double)LifeRatio, MinDamage, DamageCap);
            modifiers.DisableCrit();
            modifiers.ModifyHitInfo += (ref NPC.HitInfo info) => {
                info.Damage = damage;
                info.Crit = false;
            };
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            int shatter = ModContent.BuffType<RequiemShatter>();
            target.AddBuff(shatter, RequiemShatter.PinTime);
            NPC body = Body(target);
            if (body != target)
                body.AddBuff(shatter, RequiemShatter.PinTime);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, -Vector2.UnitY * RequiemCrossBlast.RiseSpeed, ModContent.ProjectileType<RequiemCrossBlast>(), 0, 0f, Projectile.owner);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            return true;
        }

        public override void OnKill(int timeLeft) {
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            RequiemVfx.Sparks(Projectile.Center, back, DeathSparks, DeathSparkSpread, DeathSparkSpeedMin, DeathSparkSpeedMax, RequiemVfx.GoldDeep, RequiemVfx.GoldWhite, DeathSparkLife);
        }

        public override bool PreDraw(ref Color lightColor) {
            RequiemVfx.DrawTrail(Projectile, TrailLength, RequiemVfx.GoldWhite, TrailTail, TrailWidth);
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
