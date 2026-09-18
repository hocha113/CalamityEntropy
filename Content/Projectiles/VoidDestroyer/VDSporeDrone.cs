using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 孢子无人机:出现后沿固定方向亮起淡紫预警线,ai[1] 帧后瞬发一道 VDSporeLaser 并消失(自身无伤害)。
    /// ai[0] 模式:0 向右射,1 向下射,2 纯装饰(绕 ai[2] 号 NPC 转,ai[1] 为相位;从 Z 2 的背景降入环阵,装饰即深度预告);
    /// ai[2] 在射击模式下为激光长度。激光的伤害值由本弹幕的 damage 承接
    /// </summary>
    public class VDSporeDrone : ModProjectile, IVoidDestroyerProjectile
    {
        public const int ModeFireRight = 0;
        public const int ModeFireDown = 1;
        public const int ModeDecor = 2;
        public const int FadeOutTime = 12;
        public const int DecorLife = 40;
        public static readonly Color WarnColor = new Color(210, 140, 255);

        public int Mode => (int)Projectile.ai[0];
        public int WarnTime => (int)Math.Max(Projectile.ai[1], 10f);
        public float BeamLength => Projectile.ai[2] > 0 ? Projectile.ai[2] : 2400f;
        public float FireRotation => Mode == ModeFireDown ? MathHelper.PiOver2 : 0f;
        public int Age => (int)Projectile.localAI[1];

        public override void SetDefaults() {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 120;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = Mode == ModeDecor ? DecorLife : WarnTime + FadeOutTime;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("vbapear", Main.rand.NextFloat(1.1f, 1.3f), Projectile.Center, 8, 0.5f);
                }
            }
            Projectile.localAI[1]++;

            if (Mode == ModeDecor) {
                int idx = (int)Projectile.ai[2];
                if (idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active) {
                    float ang = Projectile.ai[1] + Age * 0.08f;
                    Projectile.Center = Main.npc[idx].Center + ang.ToRotationVector2() * 130f;
                }
                float inOut = Math.Min(Age / 8f, Projectile.timeLeft / 8f);
                Projectile.scale = MathHelper.Clamp(inOut, 0f, 1f);
                return;
            }

            //出现 8 帧放大,预警期轻微上下浮动,发射后 12 帧收缩消失
            if (Age <= WarnTime) {
                Projectile.scale = MathHelper.Clamp(Age / 8f, 0f, 1f);
            }
            else {
                Projectile.scale = MathHelper.Clamp(Projectile.timeLeft / (float)FadeOutTime, 0f, 1f);
            }
            Projectile.rotation = (float)Math.Sin(Age * 0.15f) * 0.08f;
            Lighting.AddLight(Projectile.Center, WarnColor.ToVector3() * 0.4f * Projectile.scale);

            if (Age == WarnTime) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    float width = 24f * (Main.getGoodWorld ? 1.5f : 1f);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, FireRotation.ToRotationVector2(), ModContent.ProjectileType<VDSporeLaser>(), Projectile.damage, 0f, Main.myPlayer, BeamLength, width);
                }
                if (!Main.dedServ) {
                    for (int i = 0; i < 8; i++) {
                        Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
                        VDVfx.Spark(Projectile.Center, v, WarnColor, Main.rand.NextFloat(0.5f, 0.9f), 1f, 18, gravity: true);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D glow = CEUtils.getExtraTex("Glow");
            float drawScale = Projectile.scale;
            if (Mode == ModeDecor) {
                //装饰无人机从 Z 2 的背景降入环阵:纯绘制的假深度(它本来就没有判定),装饰即深度预告
                float z = VDDirector.LaserDecorDepth * (1f - VDVfx.EaseOut(Age / (float)VDDirector.LaserDecorDescend));
                drawPos = VDDepth.Project(Projectile.Center, z) - Main.screenPosition;
                drawScale *= VDDepth.Scale(z);
            }

            Main.spriteBatch.UseAdditive();
            if (Mode != ModeDecor && Age < WarnTime) {
                //预警线:随蓄力变亮变实,末尾 15 帧闪一下
                float charge = Age / (float)WarnTime;
                float alpha = 0.25f + 0.55f * charge;
                if (WarnTime - Age < 15) {
                    alpha += 0.3f * (float)Math.Sin(Age * 1.2f);
                }
                Vector2 dir = FireRotation.ToRotationVector2();
                Vector2 end = Projectile.Center + dir * BeamLength;
                CEUtils.drawLineBetter(Projectile.Center, end, WarnColor * MathHelper.Clamp(alpha, 0f, 1f), 4f + 6f * charge);
                CEUtils.drawLine(Projectile.Center, end, Color.White * (0.35f * charge), 1.5f);
                Main.spriteBatch.Draw(glow, drawPos, null, WarnColor * (0.5f + 0.5f * charge), 0f, glow.Size() / 2f, (0.18f + 0.12f * charge) * Projectile.scale, SpriteEffects.None, 0f);
            }
            else {
                Main.spriteBatch.Draw(glow, drawPos, null, WarnColor * 0.5f, 0f, glow.Size() / 2f, 0.18f * Projectile.scale, SpriteEffects.None, 0f);
            }
            CEUtils.ReSetToEndShader();

            Main.spriteBatch.Draw(tex, drawPos, null, Color.White, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>
    /// 孢子激光:瞬发定长射线,前 6 帧判伤,随后收窄消失。velocity 只作朝向;ai[0] 长度,ai[1] 宽度。
    /// 命中 372 + 带电 3 秒
    /// </summary>
    public class VDSporeLaser : VDHostileProjectile
    {
        public const int Lifetime = 24;
        public const int DamageFrames = 6;
        public static readonly Color BeamColor = new Color(190, 90, 255);

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => BuffID.Electrified;
        public override int DefaultTimeLeft => Lifetime;

        public float Length => Projectile.ai[0] > 0 ? Projectile.ai[0] : 2400f;
        public float Width => Projectile.ai[1] > 0 ? Projectile.ai[1] : 24f;
        public Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);

        public override void SetExtraDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("void_laser", Main.rand.NextFloat(1.2f, 1.4f), Projectile.Center, 8, 0.55f);
                    Vector2 mid = Projectile.Center + Dir * Math.Min(Length * 0.5f, 600f);
                    CEUtils.SetShake(mid, 2.5f, 1500f);
                }
            }
            Projectile.rotation = Dir.ToRotation();
            Lighting.AddLight(Projectile.Center + Dir * 200f, BeamColor.ToVector3() * 0.6f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.timeLeft < Lifetime - DamageFrames) {
                return false;
            }
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Dir * Length, targetHitbox, (int)Width);
        }

        public override bool PreDraw(ref Color lightColor) {
            float life = Projectile.timeLeft / (float)Lifetime;
            //前 6 帧全宽,之后按剩余寿命收窄淡出
            float shrink = Projectile.timeLeft >= Lifetime - DamageFrames ? 1f : life / ((Lifetime - DamageFrames) / (float)Lifetime);
            shrink = MathHelper.Clamp(shrink, 0f, 1f);
            Texture2D beam = CEUtils.getExtraTex("VoidLaser");
            Texture2D glowBeam = CEUtils.getExtraTex("BasicTrail");
            Vector2 start = Projectile.Center - Main.screenPosition;
            float rot = Projectile.rotation;
            Vector2 mid = start + Dir * Length * 0.5f;

            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glowBeam, mid, null, BeamColor * (0.7f * shrink), rot, glowBeam.Size() / 2f, new Vector2(Length / glowBeam.Width, Width * 3f * shrink / glowBeam.Height), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(beam, start, null, BeamColor * shrink, rot, new Vector2(0, beam.Height / 2f), new Vector2(Length / beam.Width, Width * 1.4f * shrink / beam.Height), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(beam, start, null, Color.White * shrink, rot, new Vector2(0, beam.Height / 2f), new Vector2(Length / beam.Width, Width * 0.55f * shrink / beam.Height), SpriteEffects.None, 0f);
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, start, null, BeamColor * shrink, 0f, glow.Size() / 2f, 0.35f * shrink, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
