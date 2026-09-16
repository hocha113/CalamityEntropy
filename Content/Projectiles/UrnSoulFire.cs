using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class UrnSoulFire : ModProjectile
    {
        //薄雾贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Particles/MediumMist")]
        internal static Asset<Texture2D> MistTex;
        public override string Texture => "CalamityEntropy/Assets/Extra/Ports/FireProj";

        public static int Lifetime => 120;
        public static int Fadetime => 90;
        public ref float Time => ref Projectile.ai[0];
        public int MistType = -1;

        public override void SetDefaults() {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.ArmorPenetration = 10;
            Projectile.MaxUpdates = 3;
            Projectile.timeLeft = Lifetime;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 6;
            Projectile.tileCollide = false;
        }

        public override void AI() {
            Time++;
            if (Time < Fadetime && Main.rand.NextBool(6)) {
                Vector2 cinderPos = Projectile.Center + Main.rand.NextVector2Circular(60f, 60f) * Utils.Remap(Time, 0f, Lifetime, 0.5f, 1f);
                float cinderSize = Utils.GetLerpValue(6f, 12f, Time, true);
                // 原灾厄 BrimstoneFlame 尘埃改用原版红炬焰
                Dust cinder = Dust.NewDustDirect(cinderPos, 4, 4, Terraria.ID.DustID.RedTorch, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f);
                if (Main.rand.NextBool(3)) {
                    cinder.scale *= 2f;
                    cinder.velocity *= 2f;
                }
                cinder.noGravity = true;
                cinder.scale *= cinderSize * 1.2f;
                cinder.velocity += Projectile.velocity * Utils.Remap(Time, 0f, Fadetime * 0.75f, 1f, 0.1f) * Utils.Remap(Time, 0f, Fadetime * 0.1f, 0.1f, 1f);
            }

            if (MistType == -1)
                MistType = Main.rand.Next(3);

            Lighting.AddLight(Projectile.Center, 0.56f, 0.56f, 0.8f);
        }


        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            int size = (int)Utils.Remap(Time, 0f, Fadetime, 10f, 40f);

            if (Time > Fadetime)
                size = (int)Utils.Remap(Time, Fadetime, Lifetime, 40f, 0f);
            hitbox.Inflate(size, size);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 300);

            Projectile.GetOwner().statMana += 2;
            if (Projectile.GetOwner().statMana > Projectile.GetOwner().statManaMax2) {
                Projectile.GetOwner().statMana = Projectile.GetOwner().statManaMax2;
            }
            for (int i = 0; i < 4; i++) {
                //RuneParticleHoming追踪字段spawn后赋,Configure管不了entity引用
                var __prt = PRTLoader.NewParticle<PRT_RuneParticleHoming>(target.Center, CEUtils.randomPointInCircle(10), Color.White, 0.5f).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0);
                __prt.homingTarget = Projectile.GetOwner();
            }
            int smokeCount = 3 + (int)MathHelper.Clamp(target.width * 0.1f, 0f, 20f);
            for (int i = 0; i < smokeCount; i++) {
                bool Smoketype = Main.rand.NextBool();
                Vector2 smokePos = target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f);
                Vector2 smokeVel = Vector2.UnitY * (Smoketype ? Main.rand.NextFloat(-0.8f, -2f) : Main.rand.NextFloat(-1.2f, -0.2f)) * MathHelper.Clamp(target.height * 0.1f, 1f, 10f);
                //MediumMistCal CalamityPorts,Configure是mist原构造
                PRTLoader.NewParticle<PRT_MediumMistCal>(smokePos, smokeVel, new Color(210, 210, 255), Smoketype ? Main.rand.NextFloat(0.4f, 0.75f) : Main.rand.NextFloat(1.5f, 2f)).Configure(Color.SkyBlue, 220 - Main.rand.Next(50), 0.1f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D fire = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Texture2D mist = MistTex.Value;

            // The conga line of colors to sift through
            Color color1 = new Color(178, 170, 255, 200);
            Color color2 = new Color(144, 140, 255, 70);
            Color color3 = new Color(190, 190, 255, 100);
            Color color4 = new Color(126, 110, 220, 100);
            float length = ((Time > Fadetime - 10f) ? 0.1f : 0.15f);
            float vOffset = Math.Min(Time, 20f);
            float timeRatio = Utils.GetLerpValue(0f, Lifetime, Time);
            float fireSize = Utils.Remap(timeRatio, 0.2f, 0.5f, 0.25f, 1f);

            if (timeRatio >= 1f)
                return false;

            for (float j = 1f; j >= 0f; j -= length) {
                Color fireColor = ((timeRatio < 0.1f) ? Color.Lerp(Color.Transparent, color1, Utils.GetLerpValue(0f, 0.1f, timeRatio)) :
                ((timeRatio < 0.2f) ? Color.Lerp(color1, color2, Utils.GetLerpValue(0.1f, 0.2f, timeRatio)) :
                ((timeRatio < 0.35f) ? color2 :
                ((timeRatio < 0.7f) ? Color.Lerp(color2, color3, Utils.GetLerpValue(0.35f, 0.7f, timeRatio)) :
                ((timeRatio < 0.85f) ? Color.Lerp(color3, color4, Utils.GetLerpValue(0.7f, 0.85f, timeRatio)) :
                Color.Lerp(color4, Color.Transparent, Utils.GetLerpValue(0.85f, 1f, timeRatio)))))));
                fireColor *= (1f - j) * Utils.GetLerpValue(0f, 0.2f, timeRatio, true);
                Color innerColor = Color.Lerp(fireColor, Color.Black, 0.3f);

                Vector2 firePos = Projectile.Center - Main.screenPosition - Projectile.velocity * vOffset * j;
                float mainRot = -j * MathHelper.PiOver2 - Main.GlobalTimeWrappedHourly * (j + 1f) * 2f / length;
                float trailRot = MathHelper.PiOver4 - mainRot;

                Vector2 trailOffset = Projectile.velocity * vOffset * length * 0.5f;
                Main.EntitySpriteDraw(fire, firePos - trailOffset, null, innerColor * 0.25f, trailRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None);

                Main.EntitySpriteDraw(fire, firePos, null, innerColor, mainRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None);

                if (MistType > 2 || MistType < 0)
                    return false;
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Rectangle frame = mist.Frame(1, 3, 0, MistType);
                Main.EntitySpriteDraw(mist, firePos, frame, Color.Lerp(fireColor, Color.White, 0.3f), mainRot, frame.Size() * 0.5f, fireSize, SpriteEffects.None);
                Main.EntitySpriteDraw(mist, firePos, frame, fireColor, mainRot, frame.Size() * 0.5f, fireSize * 3f, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            }
            return false;
        }
    }
}
