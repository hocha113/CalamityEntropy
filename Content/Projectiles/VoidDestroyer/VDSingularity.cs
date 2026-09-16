using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 虚空奇点:飘行 30 帧减速停住 → 150 帧引力(本地玩家被拉向它、封顶可逃;全屏引力透镜;吸积盘边缘螺旋放弹;
    /// 视界接触伤害)→ 20 帧塌缩(盘缩到 40%,余弦闪烁,粒子先断)→ 24 发环爆 + 冲击环;P3 且整场未用过时点燃唯一一次冲击帧。
    /// ai[0] 本体,ai[1] 阶段,ai[2] 环爆/螺旋弹伤害(已折算)
    /// </summary>
    public class VDSingularity : VDHostileProjectile
    {
        public const int TravelFrames = 30;
        public const float TravelDecay = 0.94f;
        /// <summary>着色器四边形半边长:r=1 落在 260px,吸积盘外沿 0.55 ≈ 143px</summary>
        public const float QuadHalf = 260f;

        [InnoVault.VaultLoaden("CalamityEntropy/Assets/Extra/white")]
        private static Asset<Texture2D> quadTex = null;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => TravelFrames + VDDirector.SingActiveFrames + VDDirector.SingCollapseFrames;

        public int Phase => (int)Math.Max(Projectile.ai[1], 1f);
        public int BoltDamage => (int)Projectile.ai[2];
        public int Age => (int)Projectile.localAI[1];
        public bool Traveling => Age < TravelFrames;
        public bool Collapsing => Age >= TravelFrames + VDDirector.SingActiveFrames;
        public bool ActivePull => !Traveling && !Collapsing;

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

        /// <summary>盘体整体缩放:出现 12 帧长起,塌缩期缩到 40% 并闪烁</summary>
        public float DiskScale
        {
            get
            {
                float grow = MathHelper.Clamp(Age / 12f, 0f, 1f);
                grow = 1f - MathF.Pow(1f - grow, 3f);
                if (!Collapsing)
                {
                    return grow;
                }
                float c = MathHelper.Clamp((Age - TravelFrames - VDDirector.SingActiveFrames) / (float)VDDirector.SingCollapseFrames, 0f, 1f);
                float target = MathF.Cos(c * 9f) * 0.07f + 0.4f;
                return MathHelper.SmoothStep(1f, target, c);
            }
        }

        public override void SetExtraDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = DefaultTimeLeft;
                if (!Main.dedServ)
                {
                    CEUtils.PlaySound("VoidAnticipation", 0.6f, Projectile.Center, 3, 1.1f);
                }
            }
            Projectile.localAI[1]++;
            int age = Age;
            Projectile.rotation += 0.02f;

            if (Traveling)
            {
                Projectile.velocity *= TravelDecay;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
            }

            float scale = DiskScale;
            Lighting.AddLight(Projectile.Center, VDVfx.VoidPurple.ToVector3() * 1.5f * scale);

            if (ActivePull)
            {
                PullLocalPlayer();
                VDScreenFx.ReportLens(Projectile.Center, VDDirector.SingLensStrength * scale, VDDirector.SingLensRadius);
                //吸积盘边缘螺旋放弹(切向 + 少量径向,越飞越远)
                if (IsServer && BoltDamage > 0 && (age - TravelFrames) % VDDirector.SingOrbitBoltInterval == 0)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 radial = ang.ToRotationVector2();
                    Vector2 vel = radial.RotatedBy(MathHelper.PiOver2) * VDDirector.SingOrbitBoltSpeed + radial * 1.5f;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + radial * VDDirector.SingDiskRadius, vel, ModContent.ProjectileType<VDVoidBolt>(), BoltDamage, 0f, Main.myPlayer, VDVoidBolt.ModeStraight);
                }
                //被吸进来的碎屑(客户端)
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 from = Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(220f, 420f);
                    Vector2 v = (Projectile.Center - from).SafeNormalize(Vector2.Zero).RotatedBy(0.6f) * Main.rand.NextFloat(6f, 11f);
                    VDVfx.SparkBurst(from, VDVfx.VoidPink, 1, v.Length(), v.Length(), 26, 0.3f, 0.7f);
                }
            }
            else if (Collapsing)
            {
                //塌缩:透镜反而更猛(引力压实),粒子全断
                float c = (age - TravelFrames - VDDirector.SingActiveFrames) / (float)VDDirector.SingCollapseFrames;
                VDScreenFx.ReportLens(Projectile.Center, VDDirector.SingLensStrength * (1f + c * 1.2f), VDDirector.SingLensRadius * (1f - c * 0.4f));
                if (!Main.dedServ && age == TravelFrames + VDDirector.SingActiveFrames)
                {
                    CEUtils.PlaySound("VoidAnticipation", 0.5f, Projectile.Center, 3, 1.2f);
                }
            }
        }

        /// <summary>本地玩家引力:朝奇点方向加速,朝向分量封顶(翼/坐骑仍能逃);玩家速度归其自身客户端,服务端不碰</summary>
        private void PullLocalPlayer()
        {
            if (Main.dedServ)
            {
                return;
            }
            Player player = Main.LocalPlayer;
            if (!player.active || player.dead)
            {
                return;
            }
            Vector2 toSing = Projectile.Center - player.Center;
            float dist = toSing.Length();
            if (dist > VDDirector.SingPullRadius || dist < 1f)
            {
                return;
            }
            Vector2 dir = toSing / dist;
            //越近越强(线性),远端边缘几乎无感
            float falloff = 1f - dist / VDDirector.SingPullRadius;
            player.velocity += dir * VDDirector.SingPullAccel(Phase) * (0.35f + 0.65f * falloff);
            float along = Vector2.Dot(player.velocity, dir);
            if (along > VDDirector.SingPullMaxSpeed)
            {
                player.velocity -= dir * (along - VDDirector.SingPullMaxSpeed);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Traveling || Age < TravelFrames + 6)
            {
                return false;
            }
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float r = VDDirector.SingCoreRadius * DiskScale;
            return Vector2.DistanceSquared(closest, Projectile.Center) <= r * r;
        }

        public override void OnKill(int timeLeft)
        {
            //寿命走完 = 塌缩结束的环爆;阶段清场的 Kill(timeLeft > 1)不爆
            if (timeLeft > 1)
            {
                return;
            }
            VoidDestroyerNPC boss = Owner;
            if (IsServer && BoltDamage > 0)
            {
                for (int i = 0; i < VDDirector.SingBurstCount; i++)
                {
                    Vector2 dir = (MathHelper.TwoPi * i / VDDirector.SingBurstCount).ToRotationVector2();
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, dir * VDDirector.SingBurstSpeed, ModContent.ProjectileType<VDVoidBolt>(), BoltDamage, 0f, Main.myPlayer, VDVoidBolt.ModeStraight);
                }
            }
            //冲击帧单发闸:各端按本地已知的闸位裁决,权威端把闸位写进包
            bool impact = Phase >= 3 && boss != null && !boss.Context.ImpactFrameUsed;
            if (impact && boss != null)
            {
                boss.Context.ImpactFrameUsed = true;
                if (IsServer)
                {
                    boss.NPC.netUpdate = true;
                }
            }
            if (Main.dedServ)
            {
                return;
            }
            CEUtils.PlaySound("VoidBomb", 0.7f, Projectile.Center, 3, 1.3f);
            CEUtils.SetShake(Projectile.Center, impact ? 16f : 11f, 3200f);
            VDVfx.Explosion(Projectile.Center, 1.4f, 32);
            VDVfx.SparkBurst(Projectile.Center, VDVfx.VoidPurple, 70, 6f, 28f, 42, 0.8f, 1.6f);
            VDVfx.SparkBurst(Projectile.Center, Color.White, 20, 3f, 12f, 30, 0.5f, 1f);
            for (int i = 0; i < 30; i++)
            {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4f, 14f);
                VDVfx.VoidPuff(Projectile.Center, v, Main.rand.NextFloat(1.2f, 2.2f), 0.9f);
            }
            if (impact)
            {
                VDScreenFx.FireImpact(VDDirector.ImpactFrameFrames);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float scale = DiskScale;
            if (scale <= 0.01f)
            {
                return false;
            }
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Effect shader = CEEffectAssets.VDSingularity?.Value;
            Texture2D glow = CEUtils.getExtraTex("Glow");

            //外围引力晕(加法)
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glow, pos, null, VDVfx.VoidDeep * (0.55f * scale), 0f, glow.Size() / 2f, 2.6f * scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            if (shader != null && quadTex != null)
            {
                Texture2D quad = quadTex.Value;
                Texture2D noise = CEExtraAssets.TurbulentNoise ?? CEUtils.getExtraTex("TurbulentNoise");
                Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
                Main.instance.GraphicsDevice.Textures[1] = noise;
                Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                shader.Parameters["uColor"]?.SetValue(VDVfx.VoidPurple.ToVector3());
                shader.Parameters["uColor2"]?.SetValue(VDVfx.VoidPink.ToVector3());
                shader.Parameters["uCoreRadius"]?.SetValue(VDDirector.SingCoreRadius / QuadHalf);
                shader.Parameters["uOpacity"]?.SetValue(1f);
                shader.Parameters["uSpin"]?.SetValue(1.4f);
                shader.CurrentTechnique.Passes[0].Apply();
                float quadScale = QuadHalf * 2f * scale / quad.Width;
                Main.spriteBatch.Draw(quad, pos, null, Color.White, Projectile.rotation, quad.Size() / 2f, quadScale, SpriteEffects.None, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }
            else
            {
                //无着色器退化:加法环 + 黑盘
                Texture2D ring = CEUtils.getExtraTex("BloomRing");
                Texture2D circle = CEUtils.getExtraTex("Circle");
                Main.spriteBatch.UseAdditive();
                Main.spriteBatch.Draw(ring, pos, null, VDVfx.VoidPurple * scale, Projectile.rotation * 3f, ring.Size() / 2f, VDDirector.SingDiskRadius * 2f / ring.Width * scale, SpriteEffects.None, 0f);
                CEUtils.ReSetToEndShader();
                Main.spriteBatch.Draw(circle, pos, null, Color.Black, 0f, circle.Size() / 2f, VDDirector.SingCoreRadius * 2f / circle.Width * scale, SpriteEffects.None, 0f);
            }

            //牵引范围:极淡的一圈,告诉玩家哪里开始被拉
            if (ActivePull)
            {
                Main.spriteBatch.UseAdditive();
                Texture2D ring = CEUtils.getExtraTex("BloomRing");
                float pulse = 0.12f + 0.06f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
                Main.spriteBatch.Draw(ring, pos, null, VDVfx.VoidPurple * pulse, -Projectile.rotation, ring.Size() / 2f, VDDirector.SingPullRadius * 2f / ring.Width, SpriteEffects.None, 0f);
                CEUtils.ReSetToEndShader();
            }
            return false;
        }
    }
}
