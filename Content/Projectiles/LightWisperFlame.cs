using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class LightWisperFlame : ModProjectile
    {
        //薄雾贴图,加载期就位,绘制时不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Particles/MediumMist")]
        internal static Asset<Texture2D> MistTex;
        public int MistType = -1;

        public override string Texture => "CalamityEntropy/Assets/Extra/Ports/FireProj";

        public static int Lifetime => 192;

        public static int Fadetime => 160;

        public ref float Time => ref base.Projectile.ai[0];

        public override void SetDefaults() {
            base.Projectile.width = (base.Projectile.height = 10);
            base.Projectile.friendly = true;
            base.Projectile.ignoreWater = true;
            base.Projectile.tileCollide = false;
            base.Projectile.DamageType = DamageClass.Ranged;
            base.Projectile.penetrate = 20;
            base.Projectile.MaxUpdates = 4;
            base.Projectile.timeLeft = Lifetime;
            base.Projectile.usesIDStaticNPCImmunity = true;
            base.Projectile.idStaticNPCHitCooldown = 3;
            Projectile.ArmorPenetration = 20;
        }

        public override void AI() {
            Time += 1f;
            if (MistType == -1) {
                MistType = Main.rand.Next(3);
            }

            if (Time > (float)Fadetime) {
                base.Projectile.velocity *= 0.95f;
            }

            if (Time > 6f && Time < (float)Fadetime) {
                if (Main.rand.NextBool(16)) {
                    Dust dust = Dust.NewDustDirect(base.Projectile.Center + Main.rand.NextVector2Circular(120f, 120f) * Utils.Remap(Time, 0f, Fadetime, 0.5f, 1f), 4, 4, DustID.CorruptTorch, base.Projectile.velocity.X * 0.2f, base.Projectile.velocity.Y * 0.2f, 100);
                    if (Main.rand.NextBool(5)) {
                        dust.noGravity = true;
                        dust.scale *= 2f;
                        dust.velocity *= 0.8f;
                    }

                    dust.velocity *= 1.1f;
                    dust.velocity += base.Projectile.velocity * Utils.Remap(Time, 0f, (float)Fadetime * 0.75f, 1f, 0.1f) * Utils.Remap(Time, 0f, (float)Fadetime * 0.1f, 0.1f, 1f);
                }

                if (Main.rand.NextBool(17)) {
                    bool flag = !Main.rand.NextBool();
                    //PRT_FlameCal Calamity flame trail,Configure照Ports
                    PRTLoader.NewParticle<PRT_FlameCal>(base.Projectile.Center, new Vector2(base.Projectile.velocity.X * 0.8f, -10f).RotatedByRandom(0.004999999888241291) * (flag ? Main.rand.NextFloat(0.4f, 0.65f) : Main.rand.NextFloat(0.8f, 1f)), Color.BlueViolet * (flag ? 1.2f : 0.5f), 0.05f).Configure(20, MathHelper.Clamp(Time * 0.05f, 0.15f, 1.75f), Color.DarkBlue * (flag ? 1.2f : 0.5f));  //FlameCal Calamity flame trail,Configure照Ports
                }
            }
            else if (Time == 5f) {
                Dust dust2 = Dust.NewDustPerfect(base.Projectile.Center, Main.rand.NextBool(3) ? 295 : 181, base.Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30f)) * Main.rand.NextFloat(0.5f, 1f));
                dust2.scale = Main.rand.NextFloat(0.8f, 1.8f);
                dust2.noGravity = true;
                dust2.fadeIn = 0.5f;
            }
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            int num = (int)Utils.Remap(Time, 0f, Fadetime, 20f, 80f);
            if (Time > (float)Fadetime) {
                num = (int)Utils.Remap(Time, Fadetime, Lifetime, 80f, 0f);
            }

            hitbox.Inflate(num, num);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 360);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (base.Projectile.damage < 1) {
                base.Projectile.damage = 1;
            }
        }

        public override bool PreDraw(ref Color lightColor) {


            return false;
        }

        public bool draw() {
            Texture2D fire = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Texture2D mist = MistTex.Value;

            Color color1 = new Color(160, 100, 255, 200);
            Color color2 = new Color(160, 50, 255, 70);
            Color color3 = new Color(120, 100, 255, 100);
            Color color4 = new Color(30, 50, 200, 100);
            float length = ((Time > Fadetime - 10f) ? 0.1f : 0.15f);
            float vOffset = Math.Min(Time, 40f);
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

                Vector2 firePos = Projectile.Center - Main.screenPosition - Projectile.velocity * vOffset * j;
                float mainRot = (-j * MathHelper.PiOver2 - Main.GlobalTimeWrappedHourly * (j + 1f) * 2f / length) * Math.Sign(Projectile.velocity.X);
                float trailRot = MathHelper.PiOver4 - mainRot;

                Vector2 trailOffset = Projectile.velocity * vOffset * length * 0.5f;
                Main.EntitySpriteDraw(fire, firePos - trailOffset, null, fireColor * 0.25f, trailRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None);

                Main.EntitySpriteDraw(fire, firePos, null, fireColor, mainRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None);

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
