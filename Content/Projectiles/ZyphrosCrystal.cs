using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ZyphrosCrystal : ModProjectile
    {
        //拖尾与水晶变体贴图(c0~c5 按 ai[0] 取),加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/ZypCrystals/c", 0, 6, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] CrystalFrames;
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 34;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0.16f;
            Projectile.timeLeft = 450 * 4;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 160;
            Projectile.ArmorPenetration = 100;
        }
        public float alpha = 1;
        public int rotdir = 0;
        public override void AI() {
            if (Projectile.timeLeft < 40) {
                alpha -= 1f / 40f;
            }
            if (rotdir == 0) {
                rotdir = Main.rand.NextBool() ? 1 : -1;
            }
            var player = Projectile.GetOwner();
            Projectile.ai[1]--;

            if (Projectile.ai[1] == -20) {
                if (Main.myPlayer == Projectile.owner && player.Entropy().itemTime > 0) {
                    Projectile.velocity = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX) * 16;
                    Projectile.netUpdate = true;
                    Projectile.ai[1]--;
                    for (int i = 0; i < 6; i++) {
                        //PRT_CritSparkCal Calamity crit spark,Configure Ports签名
                        PRTLoader.NewParticle<PRT_CritSparkCal>(Projectile.Center, CEUtils.randomPointInCircle(8), Color.LightBlue, Main.rand.NextFloat(0.7f, 1.3f)).Configure(Color.Blue, 16);  //CritSparkCal Calamity crit spark,Configure Ports签名
                    }
                }
                else {
                    Projectile.ai[1]++;
                }
            }
            if (Projectile.ai[1] < -1 * 28 * 4) {
                Projectile.ai[1] = Main.rand.Next(200, 260);
            }
            if (Projectile.ai[1] > -20) {
                Projectile.velocity = ((player.Center + ((Projectile.Center - player.Center).ToRotation() + 0.2f * rotdir).ToRotationVector2() * 110) - Projectile.Center).SafeNormalize(Vector2.Zero) * (CEUtils.getDistance(Projectile.Center, player.Center) > 150 ? 12 : 4);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        internal Color ColorFunction(float completionRatio, Vector2 vertex) {
            float fadeToEnd = MathHelper.Lerp(0.65f, 1f, (float)Math.Cos(-Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
            float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio, true) * alpha;
            Color colorHue = Color.SkyBlue;

            Color endColor = Color.Lerp(colorHue, Color.Turquoise, (float)Math.Sin(completionRatio * MathHelper.Pi * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio, Vector2 vertex) {
            float expansionCompletion = (float)Math.Pow(1 - completionRatio, 3);
            return MathHelper.Lerp(0f, 8 * Projectile.scale * alpha, expansionCompletion);
        }
        public override bool PreDraw(ref Color lightColor) {
            // 暂替贴图：原灾厄 ScarletDevilStreak 条带，待 texture-map 定稿后按表回改
            GameShaders.Misc["CalamityEntropy:TrailStreak"].SetShaderTexture(CEExtraAssets.StreakFadedAsset);
            CEPrimitiveRenderer.RenderTrail(Projectile.oldPos, new CEPrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Projectile.Size * 0.5f, shader: GameShaders.Misc["CalamityEntropy:TrailStreak"]), 30);

            Texture2D t = CrystalFrames[(int)Projectile.ai[0]];
            Main.EntitySpriteDraw(t, Projectile.Center - Main.screenPosition, null, Color.White * alpha, Projectile.rotation, t.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            // 2026-08-31 平衡案:水晶无视目标护甲(所有变体)
            modifiers.ScalingArmorPenetration += 1f;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            // 2026-08-31 平衡案:水晶不再造成任何灾厄移植减益;保留回血与分裂等非减益特性
            if (Projectile.ai[0] == 2) {
                Projectile.GetOwner().Heal(5);
            }
            if (Projectile.ai[0] == 1) {
                Projectile.timeLeft -= 160 * 4;
                if (Projectile.timeLeft < 1) {
                    Projectile.timeLeft = 1;
                }
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ZyphrosCrystal>(), Projectile.damage / 12, Projectile.knockBack, Projectile.owner, Main.rand.Next(2, 6));
            }
        }
    }

}
