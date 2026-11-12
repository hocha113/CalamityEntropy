using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.TwinDragonsPearl
{
    /// <summary>
    /// 金珍珠:手持双龙戏珠时存在,不造成伤害。ai[0] = 1 时正在使用,缓动跟到鼠标处;为 0 时悬在玩家身后与头同高、上下浮动
    /// </summary>
    public class TwinDragonsPearlOrb : ModProjectile
    {
        #region 调参旋钮
        //判定边长(像素),与贴图同大;每帧续命的帧数,换掉武器后自然消失
        public const int Size = 28;
        public const int KeepAlive = 2;
        //使用中跟随鼠标、闲置时回到身后的缓动(每帧插值比例)
        public const float MouseLerp = 0.3f;
        public const float IdleLerp = 0.18f;
        //自动重用的两次使用之间 itemAnimation 可能有一帧为 0,松手后再保持这么多帧的使用状态,珠子不至于每发一只就回弹一下
        public const int UseGrace = 4;
        //使用状态过渡的每帧增减量,进入快、退出慢
        public const float UseBlendIn = 0.15f;
        public const float UseBlendOut = 0.08f;
        //闲置位置相对玩家中心(像素):背对朝向一侧、与头同高
        public const float IdleBack = 30f;
        public const float IdleHeight = -20f;
        //闲置浮动幅度(像素)与角速度(弧度/帧)
        public const float BobAmplitude = 4f;
        public const float BobSpeed = 0.06f;
        //光晕半径(像素):闲置与使用中;呼吸幅度(倍率)与角速度(弧度/帧)
        public const float IdleGlowRadius = 34f;
        public const float UseGlowRadius = 52f;
        public const float PulseAmount = 0.12f;
        public const float PulseSpeed = 0.1f;
        //Glow2 可见半径占边长的比例;外晕与内核不透明度;内核相对外晕的半径比
        public const float GlowVisibleFrac = 0.4f;
        public const float HaloOpacity = 0.55f;
        public const float CoreOpacity = 0.5f;
        public const float CoreRatio = 0.45f;
        //使用中的拖尾:记录帧数、记录模式(0 = 只记位置)、不透明度、尾端最小缩放
        public const int TrailLength = 8;
        public const int TrailMode = 0;
        public const float TrailOpacity = 0.35f;
        public const float TrailMinScale = 0.5f;
        //光照强度;闲置时光照与使用中的比
        public const float LightStrength = 0.9f;
        public const float IdleLightRatio = 0.6f;
        //使用中绕珠火星:每帧出现概率、环绕半径范围(像素)、切向速度(像素/帧)
        public const float OrbitSparkChance = 0.5f;
        public const float OrbitSparkMinRadius = 14f;
        public const float OrbitSparkMaxRadius = 22f;
        public const float OrbitSparkSpeed = 2.2f;
        //闲置金尘:每帧出现概率、散布半径(像素)、上飘速度(像素/帧)
        public const float IdleDustChance = 0.1f;
        public const float IdleDustRadius = 12f;
        public const float IdleDustRise = 0.6f;
        //绕珠火星与闲置金尘:缩放范围、不透明度、寿命(帧)
        public const float OrbitSparkMinScale = 0.18f;
        public const float OrbitSparkMaxScale = 0.3f;
        public const float OrbitSparkOpacity = 0.9f;
        public const int OrbitSparkLife = 16;
        public const float IdleDustMinScale = 0.15f;
        public const float IdleDustMaxScale = 0.25f;
        public const float IdleDustOpacity = 0.8f;
        public const int IdleDustLife = 30;
        #endregion

        public static readonly Color PearlGold = new Color(255, 196, 64);
        public static readonly Color PearlCore = new Color(255, 244, 200);

        private int age;
        private float useBlend;
        //只在所有者端计数
        private int useGrace;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = KeepAlive;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override bool? CanCutTiles() => false;

        /// <summary>该玩家的金珍珠当前位置;没有则返回 false</summary>
        public static bool TryGetPearl(Player player, out Vector2 center) {
            int type = ModContent.ProjectileType<TwinDragonsPearlOrb>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == player.whoAmI && p.type == type) {
                    center = p.Center;
                    return true;
                }
            }
            center = Vector2.Zero;
            return false;
        }

        private static Vector2 IdleSpot(Player owner, int age) {
            float bob = MathF.Sin(age * BobSpeed) * BobAmplitude;
            return owner.MountedCenter + new Vector2(-owner.direction * IdleBack, IdleHeight + bob + owner.gfxOffY);
        }

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<TwinDragonsPearl>()) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = KeepAlive;
            age++;
            if (Projectile.owner == Main.myPlayer) {
                owner.Entropy().MouseWorldListener = true;
                useGrace = owner.itemAnimation > 0 ? UseGrace : Math.Max(useGrace - 1, 0);
                float state = useGrace > 0 ? 1f : 0f;
                if (state != Projectile.ai[0]) {
                    Projectile.ai[0] = state;
                    Projectile.netUpdate = true;
                }
            }
            bool inUse = Projectile.ai[0] > 0f;
            useBlend = MathHelper.Clamp(useBlend + (inUse ? UseBlendIn : -UseBlendOut), 0f, 1f);
            Vector2 goal = inUse ? owner.Entropy().MouseWorld : IdleSpot(owner, age);
            //刚生成时直接落位,免得从玩家中心飞出来
            Projectile.Center = age == 1 ? goal : Vector2.Lerp(Projectile.Center, goal, inUse ? MouseLerp : IdleLerp);
            Projectile.velocity = Vector2.Zero;

            float pulse = 1f + MathF.Sin(age * PulseSpeed) * PulseAmount;
            Lighting.AddLight(Projectile.Center, PearlGold.ToVector3() * LightStrength * pulse * MathHelper.Lerp(IdleLightRatio, 1f, useBlend));
            if (Main.dedServ) {
                return;
            }
            if (inUse) {
                if (Main.rand.NextFloat() < OrbitSparkChance) {
                    float a = CEUtils.randomRot();
                    Vector2 pos = Projectile.Center + a.ToRotationVector2() * Main.rand.NextFloat(OrbitSparkMinRadius, OrbitSparkMaxRadius);
                    Vector2 vel = (a + MathHelper.PiOver2).ToRotationVector2() * OrbitSparkSpeed;
                    PRTLoader.NewParticle<PRT_Light>(pos, vel, PearlGold, Main.rand.NextFloat(OrbitSparkMinScale, OrbitSparkMaxScale)).Configure(OrbitSparkOpacity, lifetime: OrbitSparkLife);
                }
            }
            else if (Main.rand.NextFloat() < IdleDustChance) {
                Vector2 pos = Projectile.Center + CEUtils.randomPointInCircle(IdleDustRadius);
                PRTLoader.NewParticle<PRT_Light>(pos, new Vector2(0f, -IdleDustRise), PearlGold, Main.rand.NextFloat(IdleDustMinScale, IdleDustMaxScale)).Configure(IdleDustOpacity, lifetime: IdleDustLife);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 origin = tex.Size() / 2f;
            Vector2 c = Projectile.Center - Main.screenPosition;
            float pulse = 1f + MathF.Sin(age * PulseSpeed) * PulseAmount;
            float radius = MathHelper.Lerp(IdleGlowRadius, UseGlowRadius, useBlend) * pulse;

            Main.spriteBatch.UseBlendState(BlendState.Additive);
            float glowScale = radius / (glow.Width * GlowVisibleFrac);
            Main.EntitySpriteDraw(glow, c, null, PearlGold * HaloOpacity, 0f, glow.Size() / 2f, glowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, c, null, PearlCore * CoreOpacity, 0f, glow.Size() / 2f, glowScale * CoreRatio, SpriteEffects.None);
            //使用中的短拖尾:同一张珠子贴图沿旧位置渐淡渐小
            if (useBlend > 0f) {
                for (int i = 1; i < TrailLength; i++) {
                    if (Projectile.oldPos[i] == Vector2.Zero) {
                        continue;
                    }
                    float fade = 1f - i / (float)TrailLength;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Main.EntitySpriteDraw(tex, pos, null, PearlGold * (TrailOpacity * fade * useBlend), 0f, origin, MathHelper.Lerp(TrailMinScale, 1f, fade), SpriteEffects.None);
                }
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(tex, c, null, Color.White, 0f, origin, 1f, SpriteEffects.None);
            return false;
        }
    }
}
