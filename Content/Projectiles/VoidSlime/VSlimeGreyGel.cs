using CalamityEntropy.Content.NPCs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 灰色凝胶球,受重力,碰到物块反弹,弹够次数就碎
    /// 各端各自算碰撞,起始数据相同轨迹就相同
    /// localAI[0] 存活帧,localAI[1] 已反弹次数
    /// </summary>
    public class VSlimeGreyGel : VSlimeHostileProjectile
    {
        public override int DefaultTimeLeft => VSlimeDirector.GreyGelLife;

        private float Age => Projectile.localAI[0];
        /// <summary>落地瞬间的压扁量,逐帧回弹</summary>
        private float squash;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.tileCollide = true;
        }

        public override void AI() {
            Projectile.localAI[0]++;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + VSlimeDirector.GreyGelGravity, 15f);
            Projectile.rotation += Projectile.velocity.X * 0.025f;
            Projectile.Opacity = MathHelper.Clamp(Age / 6f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 14f, 0f, 1f);
            squash *= 0.8f;
            Lighting.AddLight(Projectile.Center, 0.3f, 0.3f, 0.34f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.localAI[1]++;
            if (Projectile.localAI[1] > VSlimeDirector.GreyGelMaxBounces) {
                return true;
            }
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon) {
                Projectile.velocity.X = -oldVelocity.X * VSlimeDirector.GreyGelBounce;
            }
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon) {
                Projectile.velocity.Y = -oldVelocity.Y * VSlimeDirector.GreyGelBounce;
            }
            squash = 0.35f;
            if (!Main.dedServ) {
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.3f, Pitch = 0.4f, MaxInstances = 3 }, Projectile.Center);
            }
            return false;
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f ? null : false;

        public override void OnKill(int timeLeft) {
            VSlimeVfx.Scatter(Projectile.Center, DustID.Silver, 8, 1.5f, 5f, 0.9f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            DrawBolt(tex, tex.Frame(), VSlimeVfx.PaleMid, Projectile.Opacity, Projectile.scale, 0.3f, new Vector2(1f + squash, 1f - squash));
            return false;
        }
    }
}
