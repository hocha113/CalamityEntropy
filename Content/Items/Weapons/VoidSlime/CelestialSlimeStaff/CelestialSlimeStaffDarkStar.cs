using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.CelestialSlimeStaff
{
    /// <summary>
    /// 黑洞星:金边黑星,先散开再追踪,带金色残影;命中后在敌人身上放一个凝胶黑洞。
    /// ai[0] 目标 NPC 下标,ai[1] 史莱姆出手时的面板伤害(算黑洞伤害用)
    /// </summary>
    public class CelestialSlimeStaffDarkStar : ModProjectile
    {
        #region 调参旋钮
        //判定边长(像素)、寿命(更新次数,每帧2次更新)
        public const int Size = 20;
        public const int Lifetime = 300;
        //先减速散开的更新次数与每次的速度保留比例
        public const int DriftUpdates = 20;
        public const float DriftDrag = 0.95f;
        //追踪速度(像素/更新)与转向系数;目标没了以后重新找目标的距离(像素)、没有目标时的加速比例
        public const float HomingSpeed = 12f;
        public const float HomingLerp = 0.1f;
        public const float RetargetRange = 1200f;
        public const float IdleAccel = 1.04f;
        //自转速度(弧度/更新)、照明强度
        public const float SpinSpeed = 0.1f;
        public const float LightStrength = 0.4f;
        //光晕直径(像素)与强度;残影数、最亮一张的强度、最小一张的缩放
        public const float GlowDiameter = 44f;
        public const float GlowOpacity = 0.45f;
        public const int AfterimageCount = 7;
        public const float AfterimageOpacity = 0.5f;
        public const float AfterimageMinScale = 0.6f;
        //消失时的金色闪烁:缩放、寿命(帧)、光晕缩放
        public const float KillSparkleScale = 0.5f;
        public const int KillSparkleLife = 12;
        public const float KillSparkleBloom = 1.2f;
        //凝胶黑洞伤害占史莱姆面板的比例(策划 50%)
        public const float HoleMult = 0.5f;
        #endregion

        public static readonly Color Gold = new(255, 196, 70);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = AfterimageCount;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            float age = ++Projectile.localAI[0];
            NPC target = Target();
            if (age <= DriftUpdates) {
                Projectile.velocity *= DriftDrag;
            }
            else if (target != null) {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * HomingSpeed, HomingLerp);
            }
            else {
                if (Projectile.owner == Main.myPlayer) {
                    NPC next = Projectile.FindTargetWithinRange(RetargetRange);
                    if (next != null) {
                        Projectile.ai[0] = next.whoAmI;
                        Projectile.netUpdate = true;
                    }
                }
                if (Projectile.velocity.Length() < HomingSpeed) {
                    Projectile.velocity *= IdleAccel;
                }
            }
            Projectile.rotation += SpinSpeed;
            Lighting.AddLight(Projectile.Center, Gold.ToVector3() * LightStrength);
        }

        private NPC Target() {
            int index = (int)Projectile.ai[0];
            if (index < 0 || index >= Main.maxNPCs) {
                return null;
            }
            NPC npc = Main.npc[index];
            return npc.CanBeChasedBy(Projectile) ? npc : null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.owner != Main.myPlayer) {
                return;
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<CelestialSlimeStaffGelHole>(),
                (int)(Projectile.ai[1] * HoleMult), 0f, Projectile.owner, target.whoAmI);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            PRTLoader.NewParticle<PRT_SparkleCal>(Projectile.Center, Vector2.Zero, Gold, KillSparkleScale).Configure(Gold, KillSparkleLife, bloomScale: KillSparkleBloom);
        }

        //残影用加法画同一张贴图:黑色本体加不出亮度,只剩金边幽影;本体用普通混合
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            GodDraw.BeginAdditive();
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(Gold, GlowOpacity), 0f, GlowDiameter / GodDraw.Glow.Width);
            int count = Projectile.oldPos.Length;
            for (int i = count - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)count;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
                Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, GodDraw.Add(Gold, AfterimageOpacity * t), Projectile.oldRot[i], origin,
                    MathHelper.Lerp(AfterimageMinScale, 1f, t), SpriteEffects.None, 0f);
            }
            GodDraw.End();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
