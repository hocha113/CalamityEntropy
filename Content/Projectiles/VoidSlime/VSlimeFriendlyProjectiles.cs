using CalamityEntropy.Content.Buffs.VoidSlime;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>友方小辉星;ai[0] 颜色帧 0 蓝 1 紫 2 橙,ai[1] 开始追踪前的帧数,ai[2] 伤害类型 0 近战 1 远程 2 召唤</summary>
    public class VSlimeFriendlyStarlet : ModProjectile
    {
        public const float SeekRange = 720f;
        public const float Turn = 0.09f;

        public override string Texture => "CalamityEntropy/Content/Projectiles/VoidSlime/VSlimeStarlet";

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Generic;
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                int kind = (int)Projectile.ai[2];
                Projectile.DamageType = kind == 0 ? DamageClass.Melee : kind == 1 ? DamageClass.Ranged : DamageClass.Summon;
                Projectile.frame = (int)MathHelper.Clamp(Projectile.ai[0], 0f, 2f);
            }
            Projectile.localAI[1]++;
            Projectile.rotation += 0.12f;
            if (Projectile.localAI[1] >= Projectile.ai[1]) {
                NPC target = Projectile.FindTargetWithinRange(SeekRange);
                if (target != null) {
                    float speed = Math.Max(Projectile.velocity.Length(), 10f);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * speed, Turn);
                }
            }
            Lighting.AddLight(Projectile.Center, VSlimeVfx.StarletColor(Projectile.frame).ToVector3() * 0.4f);
        }

        public override void OnKill(int timeLeft) {
            for (int i = 0; i < 6; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(3f, 3f), 0, VSlimeVfx.StarletColor(Projectile.frame), 1.1f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() / 2f;
            Color glow = VSlimeVfx.StarletColor(Projectile.frame);
            GodDraw.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.spriteBatch.Draw(tex, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, frame, GodDraw.Add(glow, 0.45f * t), Projectile.rotation, origin, 0.55f + 0.4f * t, SpriteEffects.None, 0f);
            }
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(glow, 0.5f), 0f, 0.3f);
            GodDraw.End();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }

    /// <summary>凝星刃甩出的胶团:受重力,弹跳三次,消失时炸成三颗追踪小辉星;ai[0] 已弹跳次数</summary>
    public class GelstarBlob : ModProjectile
    {
        public const int MaxBounces = 3;
        public const int Starlets = 3;

        public override string Texture => "CalamityEntropy/Content/Projectiles/VoidSlime/VSlimeDarkGel";

        public override void SetDefaults() {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI() {
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.3f, 16f);
            Projectile.rotation += Projectile.velocity.X * 0.03f;
            if (Main.rand.NextBool(3)) {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, 0f, 0f, 0, default, 1.2f);
                dust.noGravity = true;
                dust.velocity *= 0.3f;
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.16f, 0.48f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Projectile.ai[0]++;
            if (Projectile.ai[0] > MaxBounces) {
                return true;
            }
            if (Projectile.velocity.X != oldVelocity.X) {
                Projectile.velocity.X = -oldVelocity.X * 0.8f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y) {
                Projectile.velocity.Y = -oldVelocity.Y * 0.8f;
            }
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = 0.3f }, Projectile.Center);
            return false;
        }

        public override void OnKill(int timeLeft) {
            SoundEngine.PlaySound(SoundID.Item110 with { Volume = 0.7f }, Projectile.Center);
            for (int i = 0; i < 14; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.5f);
                dust.noGravity = true;
            }
            if (Projectile.owner != Main.myPlayer) {
                return;
            }
            float start = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < Starlets; i++) {
                Vector2 velocity = (start + MathHelper.TwoPi * i / Starlets).ToRotationVector2() * 9f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<VSlimeFriendlyStarlet>(),
                    Projectile.damage / 2, Projectile.knockBack * 0.5f, Projectile.owner, 1f, 10f, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            GodDraw.BeginAdditive();
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(VSlimeVfx.VoidBright, 0.5f), 0f, 0.34f);
            GodDraw.End();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }

    /// <summary>脉冲星杖的射线:瞬发直线,穿墙穿敌,每个敌人只吃一次;velocity 只存方向</summary>
    public class PulsarBeam : ModProjectile
    {
        public const float Length = 1500f;
        public const float HalfWidth = 14f;
        public const int Life = 18;

        public override string Texture => CEUtils.WhiteTexPath;

        private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);

        private Vector2 End => Projectile.Center + Direction * Length;

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Life;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item72 with { Pitch = 0.2f }, Projectile.Center);
            }
            Projectile.rotation = Direction.ToRotation();
            for (float along = 0f; along < Length; along += 150f) {
                Lighting.AddLight(Projectile.Center + Direction * along, 0.3f, 0.5f, 0.8f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float unused = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, End, HalfWidth * 2f, ref unused);
        }

        public override bool PreDraw(ref Color lightColor) {
            float k = Projectile.timeLeft / (float)Life;
            GodDraw.BeginAdditive();
            GodDraw.Line(Projectile.Center, End, HalfWidth * 2.4f * k, GodDraw.Add(VSlimeVfx.StarBlue, 0.9f * k), 0.04f);
            GodDraw.Line(Projectile.Center, End, HalfWidth * 0.9f * k, GodDraw.Add(Color.White, k), 0.04f);
            GodDraw.Centered(GodDraw.Flash, Projectile.Center, GodDraw.Add(VSlimeVfx.StarIce, k), 0f, 0.5f * k);
            GodDraw.End();
            return false;
        }
    }

    /// <summary>奇点杖的哨兵:原地悬停的小黑洞,圈内接触伤害,定时朝最近的敌人吐追踪小辉星;ai[0] 计时</summary>
    public class MiniSingularity : ModProjectile
    {
        public const float Radius = 56f;
        public const float Range = 900f;
        public const int FireInterval = 40;

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.sentry = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI() {
            Projectile.ai[0]++;
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, 0.7f, 0.25f, 0.2f);
            if (Projectile.owner != Main.myPlayer || (int)Projectile.ai[0] % FireInterval != 0) {
                return;
            }
            NPC target = Projectile.OwnerMinionAttackTargetNPC;
            if (target == null || !target.CanBeChasedBy(Projectile) || Projectile.Distance(target.Center) > Range) {
                target = Projectile.FindTargetWithinRange(Range);
            }
            if (target == null) {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.5f }, Projectile.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.DirectionTo(target.Center) * 12f,
                ModContent.ProjectileType<VSlimeFriendlyStarlet>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 2f, 0f, 2f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= Radius * Radius;
        }

        public override bool PreDraw(ref Color lightColor) {
            float open = MathHelper.Clamp(Projectile.ai[0] / 20f, 0f, 1f);
            GodDraw.BeginAdditive();
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(VSlimeVfx.HoleDark, 0.8f * open), 0f, Radius * 5f / GodDraw.Glow.Width);
            GodDraw.End();
            VoidSlimeNPC.DrawSwirl(Projectile.Center, Radius * 2f * open, 0.5f, VSlimeVfx.HoleRed, VSlimeVfx.HoleEmber, 1.8f, open);
            return false;
        }
    }

    /// <summary>虚空星辰史莱姆宠物:飘在玩家肩后,两帧呼吸</summary>
    public class VoidStarSlimePet : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 2;
            Main.projPet[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 28;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft *= 5;
        }

        public override bool? CanCutTiles() => false;

        public override bool MinionContactDamage() => false;

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (!player.dead && player.HasBuff(ModContent.BuffType<VoidStarSlimeBuff>())) {
                Projectile.timeLeft = 2;
            }
            float bob = MathF.Sin(Main.GlobalTimeWrappedHourly * 2f + Projectile.whoAmI) * 6f;
            Vector2 diff = player.Center + new Vector2(-48f * player.direction, -54f + bob) - Projectile.Center;
            if (diff.Length() > 2000f) {
                Projectile.Center += diff;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
            else {
                Vector2 want = diff * 0.12f;
                if (want.Length() > 18f) {
                    want *= 18f / want.Length();
                }
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.15f);
            }
            Projectile.spriteDirection = Projectile.velocity.X > 0.5f ? 1 : Projectile.velocity.X < -0.5f ? -1 : player.direction;
            Projectile.rotation = Projectile.velocity.X * 0.03f;
            if (++Projectile.frameCounter >= 24) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.16f, 0.48f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.6f);
    }
}
