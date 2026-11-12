using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.DimNova
{
    /// <summary>
    /// 大爆炸后散开、停顿、再收紧的凝胶碎片,只由主人端生成
    /// ai[0]=收紧时追的 NPC 序号(-1 就收回爆炸中心) ai[1]、ai[2]=爆炸中心 velocity=散开方向(不参与移动)
    /// localAI[0]=已跑过的 AI 次数,位置每帧按它算出来,不靠同步;只在收紧段造成伤害
    /// </summary>
    public class DimNovaShard : ModProjectile
    {
        //散开、停顿、收紧三段的时长(帧)
        private const int SpreadFrames = 12;
        private const int HoldFrames = 8;
        private const int ContractFrames = 10;
        private const int Lifetime = SpreadFrames + HoldFrames + ContractFrames;
        //timeLeft 只是兜底,比动画多留的帧数
        private const int KillMargin = 10;
        //散开距离(像素);停顿时沿散开方向抖动的幅度(像素)与快慢(弧度/帧)
        private const float SpreadDistance = 120f;
        private const float HoldWobble = 3f;
        private const float HoldWobbleSpeed = 1.2f;
        //收紧目标离爆炸中心超过这个距离(像素)就改收回爆炸中心
        private const float GoalLeash = 480f;
        //本体半自发光;光晕(Glow2 原图 256 像素)的大小与不透明度,停顿段逐渐蓄亮
        private const float SelfLight = 0.5f;
        private const float GlowScale = 0.14f;
        private const float GlowOpacity = 0.4f;
        private const float GlowOpacityCharged = 0.9f;
        //收紧拖影: 残影数量、起始不透明度、最末一个残影的缩放
        private const int TrailLength = 6;
        private const float TrailOpacity = 0.6f;
        private const float TrailMinScale = 0.5f;
        //收拢瞬间的白热闪光(Glow2)大小与存活帧数,5 发叠在一起更亮
        private const float FlashScale = 0.32f;
        private const int FlashLife = 8;
        //收拢声用虚空史莱姆奇点转移的闪光声,同一刻 5 发只留一声
        private const float SoundVolume = 0.4f;
        private const float SoundPitch = 1.3f;
        private const int SoundInstances = 1;

        private static readonly Color GlowBlue = new(110, 170, 255);
        private static readonly Color HotWhite = new(255, 244, 214);
        private static readonly Vector3 ShardLight = new(0.2f, 0.35f, 0.7f);

        private float Age => Projectile.localAI[0] - 1f;

        private bool Contracting => Age >= SpreadFrames + HoldFrames;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime + KillMargin;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Contracting ? null : false;

        public override bool? CanCutTiles() => false;

        public override void AI() {
            Projectile.localAI[0]++;
            float age = Age;
            if (age > Lifetime) {
                Projectile.Kill();
                return;
            }
            Vector2 center = new(Projectile.ai[1], Projectile.ai[2]);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 hold = center + direction * SpreadDistance;
            float outward = direction.ToRotation() + MathHelper.PiOver2;
            if (age < SpreadFrames) {
                float t = age / SpreadFrames;
                Projectile.Center = center + direction * (SpreadDistance * (1f - (1f - t) * (1f - t)));
                Projectile.rotation = outward;
            }
            else if (!Contracting) {
                float t = (age - SpreadFrames) / HoldFrames;
                Projectile.Center = hold + direction * (MathF.Sin((age - SpreadFrames) * HoldWobbleSpeed) * HoldWobble);
                Projectile.rotation = outward + MathHelper.Pi * t;
            }
            else {
                float t = (age - SpreadFrames - HoldFrames) / ContractFrames;
                Vector2 goal = Goal(center);
                Projectile.Center = Vector2.Lerp(hold, goal, t * t);
                if (goal != hold)
                    Projectile.rotation = (goal - hold).ToRotation() + MathHelper.PiOver2;
            }
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, ShardLight);
        }

        private Vector2 Goal(Vector2 center) {
            int index = (int)Projectile.ai[0];
            if (index >= 0 && index < Main.maxNPCs) {
                NPC npc = Main.npc[index];
                if (npc.active && !npc.friendly && npc.life > 0 && npc.Distance(center) < GoalLeash)
                    return npc.Center;
            }
            return center;
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, HotWhite, FlashScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FlashLife);
            CEUtils.PlaySound("soulshine", SoundPitch, Projectile.Center, SoundInstances, SoundVolume);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Color body = Color.Lerp(lightColor, Color.White, SelfLight);
            if (Contracting) {
                for (int i = TrailLength - 1; i > 0; i--) {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float fade = 1f - i / (float)TrailLength;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Main.EntitySpriteDraw(tex, pos, null, body * (fade * TrailOpacity), Projectile.oldRot[i], origin, Projectile.scale * MathHelper.Lerp(TrailMinScale, 1f, fade), SpriteEffects.None);
                }
            }
            float charge = Utils.GetLerpValue(SpreadFrames, SpreadFrames + HoldFrames, Age, true);
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, center, null, (GlowBlue with { A = 0 }) * MathHelper.Lerp(GlowOpacity, GlowOpacityCharged, charge), 0f, glow.Size() / 2f, GlowScale * Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, null, body, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
