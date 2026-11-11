using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Conqueror
{
    /// <summary>
    /// 剧毒鲨牙,短剑本体命中后在命中点上下左右依次现身再合拢,贴图借 Boss 毒针
    /// ai[0] 目标 NPC 下标,ai[1] 出场序号 0-3,决定现身前等几帧;velocity 只存去向,位移自己推
    /// </summary>
    public class SandseaConquerorFang : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Projectiles/SandseaDragon/SandseaVenomNeedle";

        public const float DamageMult = 0.7f;           //伤害占面板的比例,策划原文
        private const int VenomFrames = 300;            //酸性毒液 5 秒,策划原文
        private const float Knockback = 0f;             //四面合击不带击退,免得把目标推来推去
        private const int VolleySize = 4;               //一组四发,策划原文
        private const float SpawnRadius = 56f;          //现身点到命中点的距离,像素
        private const int Stagger = 2;                  //相邻两发错开的帧数
        private const int AppearFrames = 3;             //现身蓄势帧数,期间不伤人
        private const int DashFrames = 9;               //冲刺最长帧数
        private const int FadeFrames = 3;               //没打中时末段淡出帧数,期间不伤人
        private const int LifeMargin = 2;               //timeLeft 比自管寿命多留的帧数
        private const int MaxLife = (VolleySize - 1) * Stagger + AppearFrames + DashFrames + LifeMargin;
        private const float DashStartSpeed = 8f;        //像素/帧
        private const float DashAccel = 1.3f;           //每帧倍率,约第 5 帧走完 56 像素
        private const float DashMaxSpeed = 20f;         //小于判定盒 14 加最窄敌人 18,不会一帧穿过去
        private const float AppearScale = 1.35f;        //现身时从大缩回 1
        private const float AppearPullback = 6f;        //蓄势时沿去向往后缩的距离,像素,只影响绘制
        private const float FollowMax = 40f;            //目标单帧位移超过它视为瞬移,不跟
        private const int BodySize = 14;                //判定盒边长,像素
        private const int AfterimageCount = 3;
        private const float AfterimageGap = 0.55f;      //残影间距占当帧速度的比例
        private const float AfterimageAlpha = 0.45f;
        private const float AfterimageShrink = 0.08f;   //每道残影缩小的比例,最小仍有本体的七成多
        private static readonly Vector3 LightColor = new Vector3(0.3f, 0.22f, 0.4f);
        //上下左右,策划写的顺序
        private static readonly Vector2[] Offsets = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };

        private int age;                    //本端已活帧数
        private float dashSpeed;            //当帧冲刺速度,残影间距按它算
        private float appear;               //现身进度 0-1
        private bool expired;               //自然到期,OnKill 据此不播命中火花
        private bool hasLast;               //lastTargetCenter 是否有效
        private Vector2 lastTargetCenter;   //上一帧目标中心

        private int Order => (int)Projectile.ai[1];
        private int DashFrame => age - Order * Stagger - AppearFrames;     //大于 0 即在冲刺
        private Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitY);

        /// <summary>在命中点四周生成一组四发,只由拥有者端的命中钩子调用</summary>
        public static void SpawnVolley(Projectile parent, NPC target, Vector2 point) {
            int type = ModContent.ProjectileType<SandseaConquerorFang>();
            int damage = Math.Max(1, (int)MathF.Round(parent.damage * DamageMult));
            for (int i = 0; i < VolleySize; i++) {
                Projectile.NewProjectile(parent.GetSource_OnHit(target), point + Offsets[i] * SpawnRadius, -Offsets[i], type, damage, Knockback, parent.owner, target.whoAmI, i);
            }
        }

        public override void SetDefaults() {
            Projectile.width = BodySize;
            Projectile.height = BodySize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = MaxLife;
            Projectile.Opacity = 0f;
        }

        public override void AI() {
            age++;
            FollowTarget();
            Vector2 dir = Dir;
            Projectile.rotation = dir.ToRotation();

            int local = age - Order * Stagger;
            if (local <= 0) {
                Projectile.Opacity = 0f;
                return;
            }
            if (local <= AppearFrames) {
                appear = local / (float)AppearFrames;
                Projectile.Opacity = appear;
                Projectile.scale = MathHelper.Lerp(AppearScale, 1f, appear);
                if (local == 1) {
                    SandseaConquerorVfx.FangAppear(Projectile.Center);
                }
                return;
            }

            int dash = local - AppearFrames;
            if (dash > DashFrames) {
                expired = true;
                Projectile.Kill();
                return;
            }
            appear = 1f;
            Projectile.scale = 1f;
            Projectile.Opacity = Utils.GetLerpValue(DashFrames + 1, DashFrames + 1 - FadeFrames, dash, true);
            dashSpeed = Math.Min(DashMaxSpeed, DashStartSpeed * MathF.Pow(DashAccel, dash - 1));
            Projectile.position += dir * dashSpeed;
            Lighting.AddLight(Projectile.Center, LightColor * Projectile.Opacity);
            if (dash == 1 && Order == 0) {
                SandseaConquerorVfx.FangDart(Projectile.Center);
            }
        }

        /// <summary>整组跟着目标平移,目标在动也能合拢到同一处;位移各端按本地看到的目标自己算</summary>
        private void FollowTarget() {
            int index = (int)Projectile.ai[0];
            if (index < 0 || index >= Main.maxNPCs) {
                return;
            }
            NPC target = Main.npc[index];
            if (!target.active) {
                hasLast = false;
                return;
            }
            if (hasLast) {
                Vector2 delta = target.Center - lastTargetCenter;
                if (delta.LengthSquared() <= FollowMax * FollowMax) {
                    Projectile.position += delta;
                }
            }
            lastTargetCenter = target.Center;
            hasLast = true;
        }

        public override bool? CanDamage() {
            int dash = DashFrame;
            return dash >= 1 && dash <= DashFrames - FadeFrames ? null : false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Venom, VenomFrames);
        }

        //命中即消失,消失会过网,所以白色火花放在这里各端都看得到
        public override void OnKill(int timeLeft) {
            if (expired || timeLeft <= 0) {
                return;
            }
            SandseaConquerorVfx.FangHit(Projectile.Center, Dir);
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            float opacity = Projectile.Opacity;
            if (opacity <= 0f) {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 dir = Dir;
            //毒针贴图沿对角线摆,尖端在右上
            float rotation = Projectile.rotation + MathF.Atan2(texture.Height, texture.Width);
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition - dir * (AppearPullback * MathF.Sin(appear * MathHelper.PiOver2) * (DashFrame > 0 ? 0f : 1f));

            for (int i = AfterimageCount; i >= 1; i--) {
                if (dashSpeed <= 0f) {
                    break;
                }
                float fade = 1f - i / (float)(AfterimageCount + 1);
                Main.EntitySpriteDraw(texture, drawPos - dir * (dashSpeed * AfterimageGap * i), null, Color.White * (fade * AfterimageAlpha * opacity), rotation, origin, Projectile.scale * (1f - AfterimageShrink * i), SpriteEffects.None);
            }
            Main.EntitySpriteDraw(texture, drawPos, null, Color.White * opacity, rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
