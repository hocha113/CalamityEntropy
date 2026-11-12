using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.PhoenixBreath
{
    /// <summary>
    /// 凤凰吐息的火焰弹,火球命中后散出。先穿过火球打中的那个敌人飞出一小段,再追踪附近看得见的敌人,别的敌人优先,命中即灭
    /// ai[0] 先穿过的敌人序号;ai[1] 追踪目标的 NPC 序号,NoTarget 为无,只由拥有者选定并同步;localAI[0] 已飞行帧数
    /// </summary>
    public class PhoenixBreathEmber : ModProjectile
    {
        public const float LaunchSpeed = 10f;           //散出速度,像素每帧
        public const float NoTarget = -1f;              //ai[1] 取值: 没有目标
        private const int Frames = 2;                   //贴图竖排帧数
        private const int FrameTicks = 3;               //每帧停留,帧
        private const int Size = 12;                    //判定边长,像素
        private const int LifeFrames = 150;             //寿命,帧,2.5 秒
        private const int FadeFrames = 12;              //寿命最后这些帧里淡出
        private const int TrailRecordMode = 2;          //ProjectileID.Sets.TrailingMode 取值,逐帧记下旧位置与朝向
        private const float PassThroughFrames = 12f;    //出生后这么多帧里不打先穿过的敌人,也不撞物块
        private const float HomingDelay = 6f;           //出生后这么多帧才开始追踪
        private const float HomingRange = 560f;         //索敌半径,像素,35 格
        private const float RetargetInterval = 10f;     //有目标时重新索敌的间隔,帧;没有目标时每帧都找
        private const float HomingSpeed = 16f;          //追踪时趋近的速度,像素每帧
        private const float HomingInertia = 10f;        //转向惯性,每帧朝目标方向修正 1/10
        private const float HeadX = 11f;                //火苗头部在单帧(16×12,朝 +X)上的像素坐标,绘制原点
        private const float HeadY = 6f;
        private const int TrailLength = 8;              //拖尾段数,也是记下的旧位置数
        private const float TrailOpacity = 0.6f;        //拖尾首段不透明度,往后线性降到 0
        private const float TrailScaleEnd = 0.5f;       //拖尾末段缩放,不窄于弹体的一半
        private const float GlowSize = 30f;             //光晕直径,像素
        private const float GlowOpacity = 0.5f;
        private const float TrailDustChance = 0.35f;    //每帧出拖尾粉尘的概率
        private const float TrailDustScaleMin = 0.9f;
        private const float TrailDustScaleMax = 1.3f;
        private const int BurstDust = 8;                //熄灭时的粉尘数
        private const float BurstDustSpeed = 3f;        //熄灭粉尘初速上限,像素每帧
        private static readonly Color GlowColor = new Color(252, 102, 25);
        private static readonly Color TrailColor = new Color(255, 145, 28);
        private static readonly Vector3 LightRGB = new Vector3(0.7f, 0.35f, 0.1f);  //照明,RGB 0-1

        private ref float Age => ref Projectile.localAI[0];

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailRecordMode;
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = LifeFrames;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            Age++;
            Projectile.tileCollide = Age >= PassThroughFrames;
            if (++Projectile.frameCounter >= FrameTicks) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Frames;
            }
            if (Age >= HomingDelay) {
                if (Projectile.owner == Main.myPlayer && (!HasTarget(out _) || (Age - HomingDelay) % RetargetInterval == 0f)) {
                    Retarget();
                }
                if (HasTarget(out NPC target)) {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * HomingSpeed;
                    Projectile.velocity = (Projectile.velocity * (HomingInertia - 1f) + desired) / HomingInertia;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, LightRGB * Projectile.Opacity);
            if (!Main.dedServ && Main.rand.NextFloat() < TrailDustChance) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Vector2.Zero);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(TrailDustScaleMin, TrailDustScaleMax);
            }
        }

        //目标只在拥有者端选,换了才同步;先找先穿过的那个以外的敌人,找不到再回头打它
        private void Retarget() {
            int passed = (int)Projectile.ai[0];
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange, (proj, npc) => npc != passed && CEUtils.HomingWithTileBlockingFilter(proj, npc))
                ?? CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange, CEUtils.HomingWithTileBlockingFilter);
            float index = target == null ? NoTarget : target.whoAmI;
            if (index != Projectile.ai[1]) {
                Projectile.ai[1] = index;
                Projectile.netUpdate = true;
            }
        }

        private bool HasTarget(out NPC target) {
            int index = (int)Projectile.ai[1];
            target = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
            return target != null && target.CanBeChasedBy(Projectile);
        }

        public override bool? CanHitNPC(NPC target) {
            if (Age < PassThroughFrames && target.whoAmI == (int)Projectile.ai[0]) {
                return false;
            }
            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            PhoenixBreath.ApplyHitDebuffs(Main.player[Projectile.owner], target);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < BurstDust; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame, Main.rand.NextVector2Circular(BurstDustSpeed, BurstDustSpeed));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Frames, 0, Projectile.frame);
            Vector2 origin = new Vector2(HeadX, HeadY);
            float opacity = Projectile.Opacity;
            //拖尾用同一张图的 A=0 叠加,越旧越淡越小;从最旧的画起
            for (int i = TrailLength - 1; i >= 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float progress = i / (float)TrailLength;
                Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color color = TrailColor with { A = 0 } * (TrailOpacity * (1f - progress) * opacity);
                float scale = MathHelper.Lerp(1f, TrailScaleEnd, progress) * Projectile.scale;
                Main.EntitySpriteDraw(texture, position, frame, color, Projectile.oldRot[i], origin, scale, SpriteEffects.None);
            }
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, center, null, GlowColor with { A = 0 } * (GlowOpacity * opacity), 0f, glow.Size() * 0.5f, GlowSize * Projectile.scale / glow.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, frame, Color.White * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
