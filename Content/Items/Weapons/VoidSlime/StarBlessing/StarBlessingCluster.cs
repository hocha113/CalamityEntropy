using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarBlessing
{
    /// <summary>
    /// 紫色星团:夜光射弹的紫色换色版,动作照原版,先打旋缓行再追踪;
    /// ai[0] 为目标序号+1,由主人端写入并同步;ai[1] 为一组里的序号
    /// </summary>
    public class StarBlessingCluster : ModProjectile
    {
        #region 调参旋钮
        //以下时长都按更新次数,每帧2次更新,同原版夜光:总寿命、剩余多少之前打旋缓行、剩余多少之后不再转向
        public const int Life = 240;
        public const int DriftUntil = 180;
        public const int HomeUntil = 20;
        //缓行阶段每次更新的减速与最大偏转(弧度/更新),同原版
        public const float DriftDrag = 0.97f;
        public const float DriftTurn = MathHelper.TwoPi * 0.125f / 30f;
        //追踪速度(像素/更新)与转向插值上下限,同原版
        public const float HomingSpeed = 30f;
        public const float HomingLerpMin = 0.075f;
        public const float HomingLerpMax = 0.125f;
        //索敌半径(像素);原版只找视线内的,这里不看视线,先找鞭子点名的目标
        public const float SeekRange = 900f;
        //判定边长(像素),同原版
        public const int Size = 30;
        //残影记录数与尾端缩放,同原版
        public const int TrailLength = 20;
        public const float TrailEndScale = 0.7f;
        //星芒长短两轴的缩放(Extra_98 竖直纺锤形),同原版
        public static readonly Vector2 FlareLong = new(0.2f, 2f);
        public static readonly Vector2 FlareShort = new(0.2f, 0.8f);
        //本体缩放与中心亮芯缩放
        public const float BodyScale = 0.9f;
        public const float CoreScale = 0.5f;
        //命中时的星芒缩放与寿命(帧);碎光数量、偏离来向的最大角(弧度)、速度(像素/帧)、缩放与寿命(帧)
        public const float HitFlashScale = 0.4f;
        public const int HitFlashLife = 12;
        public const int HitMotes = 3;
        public const float HitMoteSpread = 0.7f;
        public const float HitMoteSpeed = 2f;
        public const float HitMoteScale = 0.25f;
        public const int HitMoteLife = 16;
        #endregion

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FairyQueenMagicItemShot;

        //一组六发从深紫到浅紫略有不同
        private Color Tint => Color.Lerp(StarBlessingProj.Violet, StarBlessingProj.VioletLight, Projectile.ai[1] / (StarBlessingProj.ClusterCount - 1f) * 0.5f);

        private NPC Target {
            get {
                int i = (int)Projectile.ai[0] - 1;
                if (i < 0 || i >= Main.maxNPCs)
                    return null;
                NPC npc = Main.npc[i];
                return npc.active && npc.life > 0 ? npc : null;
            }
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = Life;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (!Main.dedServ && Projectile.ai[1] == 0f)
                    SoundEngine.PlaySound(SoundID.Item82 with { Volume = 0.55f, Pitch = 0.25f, MaxInstances = 2 }, Projectile.Center);
            }
            if (Projectile.timeLeft > DriftUntil) {
                float wave = MathF.Cos(Projectile.ai[1] / StarBlessingProj.ClusterCount + Projectile.position.X / 320f + Projectile.position.Y / 160f);
                Projectile.velocity *= DriftDrag;
                Projectile.velocity = Projectile.velocity.RotatedBy(wave * DriftTurn);
            }
            if (Projectile.owner == Main.myPlayer)
                Retarget();
            if (Projectile.timeLeft <= DriftUntil && Projectile.timeLeft > HomeUntil) {
                NPC target = Target;
                Vector2 want = Projectile.velocity;
                if (target != null)
                    want = Projectile.DirectionTo(target.Center) * HomingSpeed;
                else
                    Projectile.timeLeft--;
                float amount = MathHelper.Lerp(HomingLerpMin, HomingLerpMax, Utils.GetLerpValue(DriftUntil, 30f, Projectile.timeLeft, true));
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, want, amount);
                Projectile.velocity *= MathHelper.Lerp(0.85f, 1f, Utils.GetLerpValue(0f, 90f, Projectile.timeLeft, true));
            }
            Projectile.Opacity = Utils.GetLerpValue(Life, Life - 20f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, Tint.ToVector3() * (0.5f * Projectile.Opacity));
        }

        private void Retarget() {
            NPC current = Target;
            if (current != null && current.CanBeChasedBy(Projectile))
                return;
            Player owner = Main.player[Projectile.owner];
            NPC found = null;
            if (owner.HasMinionAttackTargetNPC) {
                NPC marked = Main.npc[owner.MinionAttackTargetNPC];
                if (marked.CanBeChasedBy(Projectile) && Projectile.Distance(marked.Center) < SeekRange)
                    found = marked;
            }
            found ??= CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, SeekRange);
            float next = found == null ? 0f : found.whoAmI + 1;
            if (next != Projectile.ai[0]) {
                Projectile.ai[0] = next;
                Projectile.netUpdate = true;
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ || timeLeft <= 0)
                return;
            Color tint = Tint;
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, StarBlessingProj.VioletLight, HitFlashScale).Configure(tint, HitFlashLife, 0.08f);
            Vector2 heading = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < HitMotes; i++) {
                Vector2 vel = heading.RotatedByRandom(HitMoteSpread) * (HitMoteSpeed * Main.rand.NextFloat(0.6f, 1f));
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center, vel, tint, HitMoteScale).Configure(1f, lifetime: HitMoteLife);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            float fade = Projectile.Opacity * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            if (fade <= 0f)
                return false;
            Color tint = Tint;
            Vector2 offset = Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            //残影半透明半叠加,同原版
            for (int i = TrailLength - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float t = i / (float)TrailLength;
                Color c = tint * ((1f - t) * fade);
                c.A /= 2;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + offset, null, c, Projectile.oldRot[i], origin, Projectile.scale * MathHelper.Lerp(1f, TrailEndScale, t), SpriteEffects.None);
            }
            Vector2 head = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(tex, head, null, tint * fade, Projectile.rotation, origin, Projectile.scale * BodyScale, SpriteEffects.None);
            Color core = Color.White;
            core.A = 0;
            Main.EntitySpriteDraw(tex, head, null, core * (0.6f * fade), Projectile.rotation, origin, Projectile.scale * CoreScale, SpriteEffects.None);
            Texture2D flare = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 flareOrigin = flare.Size() / 2f;
            float pulse = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(Life, Life - 40f, Projectile.timeLeft, true)
                * (1f + 0.2f * MathF.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * MathHelper.TwoPi * 3f)) * 0.8f;
            Color glow = StarBlessingProj.VioletLight;
            glow.A = 0;
            Main.EntitySpriteDraw(flare, head, null, glow * pulse, MathHelper.PiOver2, flareOrigin, FlareLong * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(flare, head, null, glow * pulse, 0f, flareOrigin, FlareShort * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(flare, head, null, glow * (0.5f * pulse), MathHelper.PiOver2, flareOrigin, FlareLong * (0.6f * pulse), SpriteEffects.None);
            Main.EntitySpriteDraw(flare, head, null, glow * (0.5f * pulse), 0f, flareOrigin, FlareShort * (0.6f * pulse), SpriteEffects.None);
            return false;
        }
    }
}
