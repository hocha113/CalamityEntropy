using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.EventHorizon
{
    //断钢剑式挥砍弧光: 锥形碰撞、砍草和命中朝向走原版 aiStyle 190,运动与绘制自己写
    //ai0 挥向(朝向乘重力方向),ai1 挥砍总帧数,ai2 物品缩放,localAI0 已过帧数
    public class EventHorizonSlash : ModProjectile
    {
        //原版断钢剑弧光 982 的贴图,竖排 4 帧,0 帧实心月牙,3 帧细边
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Excalibur;

        //照原版 AI_190: 弧光随进度放大 60%,绘制再放大 10%
        private const float ScaleGrowth = 0.6f;
        private const float DrawScale = 1.1f;
        //原版 982: 只判定离主人 300 像素内的目标
        private const float OwnerHitDistance = 300f;
        //粉尘离弧心的半径,单位像素,原版星尘落在 20 到 100,外沿烟尘在 84
        private const float DustInner = 20f;
        private const float DustSpread = 80f;
        private const float DustEdge = 84f;
        //贴图帧数与细边帧号;弧边星光数,原版 8
        private const int FrameCount = 4;
        private const int EdgeFrame = 3;
        private const int SparkleCount = 8;

        private static readonly Color BodyDark = new(14, 12, 24);
        private static readonly Color BodyMid = new(38, 40, 54);
        private static readonly Color BodyRim = new(59, 63, 76);
        private static readonly Color Shine = new(170, 204, 255);

        private float Progress => Projectile.localAI[0] / Projectile.ai[1];

        public override void SetDefaults() {
            //照原版 982
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = ProjAIStyleID.NightsEdge;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.ownerHitCheckDistance = OwnerHitDistance;
            Projectile.usesOwnerMeleeHitCD = true;
            Projectile.stopsDealingDamageAfterPenetrateHits = true;
        }

        public override bool PreAI() {
            Player owner = Main.player[Projectile.owner];
            //豆奶让每帧多跑几次更新,进度只在每帧最后一次更新里推进,才跟得上挥剑
            bool frameTick = Projectile.FinalExtraUpdate();
            if (frameTick)
                Projectile.localAI[0]++;
            float progress = Progress;
            float dir = Projectile.ai[0];
            Projectile.rotation = MathHelper.Pi * dir * progress + Projectile.velocity.ToRotation() + dir * MathHelper.Pi + owner.fullRotation;
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter) - Projectile.velocity;
            Projectile.scale = 1f + progress * ScaleGrowth;
            if (frameTick && !Main.dedServ)
                SpawnDust();
            Projectile.scale *= Projectile.ai[2];
            if (Projectile.localAI[0] >= Projectile.ai[1])
                Projectile.Kill();
            return false;
        }

        //位置与概率照原版 982,金色星尘换成冰白,外沿白尘换成黑烟
        private void SpawnDust() {
            float dir = Projectile.ai[0];
            float angle = Projectile.rotation + Main.rand.NextFloatDirection() * MathHelper.PiOver2 * 0.7f;
            Vector2 radial = angle.ToRotationVector2();
            Vector2 along = (angle + dir * MathHelper.PiOver2).ToRotationVector2();
            if (Main.rand.NextFloat() * 2f < Projectile.Opacity) {
                Vector2 pos = Projectile.Center + radial * (Main.rand.NextFloat() * DustSpread + DustInner) * Projectile.scale;
                Dust spark = Dust.NewDustPerfect(pos, DustID.FireworksRGB, along, 100, Color.Lerp(Shine, Color.White, Main.rand.NextFloat() * 0.3f), 0.4f);
                spark.fadeIn = 0.4f + Main.rand.NextFloat() * 0.15f;
                spark.noGravity = true;
            }
            if (Main.rand.NextFloat() * 1.5f < Projectile.Opacity) {
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + radial * DustEdge * Projectile.scale, DustID.Wraith, along, 100, default, 1.2f * Projectile.Opacity);
                smoke.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EventHorizon.SpawnTerraBladeHit(target, Projectile.velocity, Projectile.owner);
        }

        //层次照原版 Main.DrawProj_Excalibur: 三层实心底、三层细边、沿弧边的星光;金色底换成暗色,细边保持白
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle body = tex.Frame(1, FrameCount);
            Rectangle edge = tex.Frame(1, FrameCount, 0, EdgeFrame);
            Vector2 origin = body.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float scale = Projectile.scale * DrawScale;
            float dir = Projectile.ai[0];
            float rot = Projectile.rotation;
            SpriteEffects fx = dir >= 0f ? SpriteEffects.None : SpriteEffects.FlipVertically;
            float progress = Progress;
            float fade = Utils.Remap(progress, 0f, 0.6f, 0f, 1f) * Utils.Remap(progress, 0.6f, 1f, 1f, 0f);

            Main.EntitySpriteDraw(tex, pos, body, BodyDark * (0.9f * fade), rot + dir * MathHelper.PiOver4 * -1f * (1f - progress), origin, scale, fx);
            Main.EntitySpriteDraw(tex, pos, body, BodyMid * (0.55f * fade), rot, origin, scale, fx);
            Main.EntitySpriteDraw(tex, pos, body, BodyRim * (0.45f * fade), rot, origin, scale * 0.975f, fx);
            Main.EntitySpriteDraw(tex, pos, edge, Color.White * (0.6f * fade), rot + dir * 0.01f, origin, scale, fx);
            Main.EntitySpriteDraw(tex, pos, edge, Color.White * (0.5f * fade), rot + dir * -0.05f, origin, scale * 0.8f, fx);
            Main.EntitySpriteDraw(tex, pos, edge, Color.White * (0.4f * fade), rot + dir * -0.1f, origin, scale * 0.6f, fx);

            float sweep = Utils.Remap(progress, 0f, 1f, 0f, MathHelper.PiOver4) * dir;
            for (int i = 0; i < SparkleCount; i++) {
                float angle = rot + dir * i * MathHelper.TwoPi * -0.025f + sweep;
                Vector2 at = pos + angle.ToRotationVector2() * (tex.Width * 0.5f - 6f) * scale;
                DrawPrettyStarSparkle(Projectile.Opacity, SpriteEffects.None, at, new Color(255, 255, 255, 0) * fade * (i / 9f), Shine, progress, 0f, 0.5f, 0.5f, 1f, angle, new Vector2(0f, Utils.Remap(progress, 0f, 1f, 3f, 0f)) * scale, Vector2.One * scale);
            }
            Vector2 tip = pos + (rot + sweep).ToRotationVector2() * (tex.Width * 0.5f - 4f) * scale;
            DrawPrettyStarSparkle(Projectile.Opacity, SpriteEffects.None, tip, new Color(255, 255, 255, 0) * fade * 0.5f, Shine, progress, 0f, 0.5f, 0.5f, 1f, 0f, new Vector2(2f, Utils.Remap(progress, 0f, 1f, 4f, 1f)) * scale, Vector2.One * scale);
            return false;
        }

        //原版 Main.DrawPrettyStarSparkle 是私有的,照抄
        private static void DrawPrettyStarSparkle(float opacity, SpriteEffects dir, Vector2 drawpos, Color drawColor, Color shineColor, float flareCounter, float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd, float rotation, Vector2 scale, Vector2 fatness) {
            Texture2D tex = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Color shine = shineColor * opacity * 0.5f;
            shine.A = 0;
            Vector2 origin = tex.Size() / 2f;
            Color core = drawColor * 0.5f;
            float t = Utils.GetLerpValue(fadeInStart, fadeInEnd, flareCounter, true) * Utils.GetLerpValue(fadeOutEnd, fadeOutStart, flareCounter, true);
            Vector2 across = new Vector2(fatness.X * 0.5f, scale.X) * t;
            Vector2 along = new Vector2(fatness.Y * 0.5f, scale.Y) * t;
            shine *= t;
            core *= t;
            Main.EntitySpriteDraw(tex, drawpos, null, shine, MathHelper.PiOver2 + rotation, origin, across, dir);
            Main.EntitySpriteDraw(tex, drawpos, null, shine, rotation, origin, along, dir);
            Main.EntitySpriteDraw(tex, drawpos, null, core, MathHelper.PiOver2 + rotation, origin, across * 0.6f, dir);
            Main.EntitySpriteDraw(tex, drawpos, null, core, rotation, origin, along * 0.6f, dir);
        }
    }
}
