using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 湮灭主炮射线:起点每帧钉在本体核心,从 ai[2] 起始角以恒定角速度扫 100°,ai[1] = 扫射帧数 × 方向符号,ai[0] 本体。
    /// 一帧亮起(4 帧张满)、末 12 帧收拢;出手 3 帧后开判定;沿射线每 10 帧向两侧落一发虚空弹雨;持续低频震屏与暗角压场
    /// </summary>
    public class VDAnnihilationBeam : VDHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => VDDirector.CannonSweepFrames;

        public int SweepFrames => (int)Math.Max(Math.Abs(Projectile.ai[1]), 10f);
        public float SweepSign => Projectile.ai[1] < 0f ? -1f : 1f;
        public float StartAngle => Projectile.ai[2];
        public int Age => SweepFrames - Projectile.timeLeft;
        public float Progress => MathHelper.Clamp(Age / (float)SweepFrames, 0f, 1f);
        public float Angle => StartAngle + SweepSign * Progress * MathHelper.ToRadians(VDDirector.CannonSweepDegrees);
        public Vector2 Dir => Angle.ToRotationVector2();
        public float Width => VDDirector.CannonBeamWidth * (Main.getGoodWorld ? 1.3f : 1f);

        private VoidDestroyerNPC Owner
        {
            get
            {
                int idx = (int)Projectile.ai[0];
                if (idx < 0 || idx >= Main.maxNPCs || !Main.npc[idx].active || Main.npc[idx].ModNPC is not VoidDestroyerNPC boss)
                {
                    return null;
                }
                return boss;
            }
        }

        public override void SetExtraDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>宽度包络:4 帧张满,末 12 帧收拢</summary>
        public float Envelope()
        {
            float open = MathHelper.Clamp(Age / 4f, 0f, 1f);
            float close = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            return Math.Min(open, close);
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = SweepFrames;
                if (!Main.dedServ)
                {
                    CEUtils.PlaySound("VoidAttack", 0.55f, Projectile.Center, 2, 1.3f);
                    CEUtils.PlaySound("void_laser", 0.5f, Projectile.Center, 2, 1.1f);
                    CEUtils.SetShake(Projectile.Center, 12f, 4000f);
                }
            }
            VoidDestroyerNPC boss = Owner;
            if (boss == null)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = boss.CorePos;
            Projectile.rotation = Angle;
            Vector2 dir = Dir;
            Lighting.AddLight(Projectile.Center + dir * 400f, VDVfx.VoidPurple.ToVector3() * 1.5f);
            Lighting.AddLight(Projectile.Center + dir * 1200f, VDVfx.VoidPurple.ToVector3() * 1.2f);

            //压场:暗角 + 低频震屏 + 循环音
            VDScreenFx.ReportVignette(VDDirector.CannonVignette * Envelope());
            if (!Main.dedServ)
            {
                if (Age % 8 == 0)
                {
                    CEUtils.SetShake(boss.NPC.Center, 3f, 3200f);
                }
                if (Age % 20 == 10)
                {
                    CEUtils.PlaySound("void_laser", 0.6f, Projectile.Center, 3, 0.8f);
                }
                //沿射线飞散的火花
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = Projectile.Center + dir * Main.rand.NextFloat(80f, VDDirector.CannonBeamLength * 0.7f);
                    Vector2 v = dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-6f, 6f) + dir * Main.rand.NextFloat(4f, 12f);
                    VDVfx.SparkBurst(pos, VDVfx.VoidPink, 1, v.Length(), v.Length(), 18, 0.4f, 0.9f);
                }
            }

            //弹雨:沿射线随机一点向两侧各落一发(服务端)
            if (IsServer && Age % VDDirector.CannonRainInterval == 5 && Projectile.timeLeft > 14)
            {
                int dmg = (int)Math.Max(1f, Projectile.damage * VDDirector.DmgVoidBolt / (float)VDDirector.DmgAnnihilationBeam);
                Vector2 pos = Projectile.Center + dir * Main.rand.NextFloat(300f, 2000f);
                Vector2 side = dir.RotatedBy(MathHelper.PiOver2);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), pos, side * VDDirector.CannonRainSpeed, ModContent.ProjectileType<VDVoidBolt>(), dmg, 0f, Main.myPlayer, VDVoidBolt.ModeStraight);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), pos, -side * VDDirector.CannonRainSpeed, ModContent.ProjectileType<VDVoidBolt>(), dmg, 0f, Main.myPlayer, VDVoidBolt.ModeStraight);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Age < 3 || Envelope() < 0.5f)
            {
                return false;
            }
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Dir * VDDirector.CannonBeamLength, targetHitbox, (int)(Width * 0.8f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float env = Envelope();
            VDBeamDraw.Draw(Projectile.Center, Dir, VDDirector.CannonBeamLength, Width, VDVfx.VoidPurple, VDVfx.CannonCore, env, 1f, 0.77f);
            //炮口爆闪:出手前 6 帧最亮
            Main.spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            float flash = MathHelper.Clamp(1f - Age / 6f, 0f, 1f);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.White * (0.9f * flash), 0f, glow.Size() / 2f, 2.5f * flash + 0.6f * env, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
