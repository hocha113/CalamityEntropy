using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkGoozma : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(platinum: 1, gold: 75);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Goozma");
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Gel, 999)
                .AddIngredient(ItemID.PinkGel, 99)
                .AddIngredient(ItemID.RainbowBrick, 99)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
        public override void modifyShootCooldown(ref int shootCooldown) {
            shootCooldown = (int)(shootCooldown * 0.64f);
        }
        public override int modifyProjectile(int pNow) {
            if (pNow == ModContent.ProjectileType<AstralBullet>()) {
                return -1;
            }
            return ModContent.ProjectileType<GoozmaStarShot>();
        }
        public override Color tooltipColor => Main.DiscoColor;
        public override EBookProjectileEffect getEffect() {
            return new GZMBMEffect();
        }
    }
    public class GZMBMEffect : EBookProjectileEffect
    {

    }
    public class GRainbowRocket : ModProjectile
    {
        public enum RockColorType
        {
            Pink,
            Orange,
            Yellow,
            White,
            SkyBlue,
            Purple,
            PalePink,
            Count
        }

        public const float SwerveAngle = 0.02f;

        public const float SwerveAngleOffsetMax = 0.04f;

        public const float SwerveTime = 60f;

        public const float HomingAcceleration = 0.4f;

        public ref float Time => ref base.Projectile.ai[0];

        public RockColorType RocketType {
            get {
                return (RockColorType)base.Projectile.ai[1];
            }
            set {
                base.Projectile.ai[1] = (float)value;
            }
        }

        public override void SetStaticDefaults() {
            Main.projFrames[base.Type] = 3;
            ProjectileID.Sets.CultistIsResistantTo[base.Type] = true;
            ProjectileID.Sets.TrailCacheLength[base.Type] = 8;
            ProjectileID.Sets.TrailingMode[base.Type] = 2;
        }

        public override void SetDefaults() {
            base.Projectile.width = 28;
            base.Projectile.height = 52;
            base.Projectile.friendly = true;
            base.Projectile.penetrate = 1;
            base.Projectile.timeLeft = 180;
            base.Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI() {
            Time += 1f;
            Lighting.AddLight(base.Projectile.Center, Main.hslToRgb((float)Math.Sin(Time / 20f) * 0.5f + 0.5f, 0.9f, 0.9f).ToVector3());
            base.Projectile.tileCollide = Time > 60f;
            base.Projectile.frameCounter++;
            if (base.Projectile.frameCounter % 4 == 3) {
                base.Projectile.frame = (base.Projectile.frame + 1) % Main.projFrames[base.Type];
            }

            NPC nPC = base.Projectile.Center.ClosestNPCAt(2600f, ignoreTiles: true, bossPriority: true);
            if (Time < 16f) {
                DoMovement_IdleSwerveFly();
            }
            else if (nPC != null) {
                DoMovement_FlyToTarget(nPC);
            }

            base.Projectile.rotation = base.Projectile.velocity.ToRotation() + MathF.PI / 2f;
        }

        private void DoMovement_IdleSwerveFly() {
            float num = MathHelper.Lerp(-0.04f, 0.04f, (float)RocketType / 7f);
            base.Projectile.velocity = base.Projectile.velocity.RotatedBy(num + 0.02f);
        }

        private void DoMovement_FlyToTarget(NPC target) {
            Projectile.velocity *= 0.96f;
            Projectile.velocity += (target.Center - Projectile.Center).normalize() * 4;
        }

        internal Color GetRocketColor() {
            return RocketType switch {
                RockColorType.Pink => Color.Pink,
                RockColorType.Orange => Color.Orange,
                RockColorType.Yellow => Color.LightGoldenrodYellow * 0.8f,
                RockColorType.White => Color.LightGray,
                RockColorType.SkyBlue => Color.LightSkyBlue,
                RockColorType.Purple => Color.Magenta,
                RockColorType.PalePink => Color.Pink * 0.77f,
                _ => Color.White,
            };
        }

        internal Color ColorFunction(float completionRatio, Vector2 vertexPos) {
            Color value = Main.hslToRgb(((float)base.Projectile.identity * 0.33f + completionRatio + Main.GlobalTimeWrappedHourly * 2f) % 1f, 1f, 0.54f);
            return Color.Lerp(GetRocketColor(), value, MathHelper.Clamp(completionRatio * 0.8f, 0f, 1f)) * base.Projectile.Opacity;
        }

        internal float WidthFunction(float completionRatio, Vector2 vertexPos) {
            float num = 8f;
            float num2 = ((!(completionRatio < 0.1f)) ? MathHelper.Lerp(num, 0f, Utils.GetLerpValue(0.1f, 1f, completionRatio, clamped: true)) : ((float)Math.Sin(completionRatio / 0.1f * (MathF.PI / 2f)) * num + 0.1f));
            return num2 * base.Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor) {
            base.Projectile.oldPos[0] = base.Projectile.position + base.Projectile.velocity.SafeNormalize(Vector2.Zero) * 50f;
            CEPrimitiveRenderer.RenderTrail(base.Projectile.oldPos, new CEPrimitiveSettings(WidthFunction, ColorFunction, (float _, Vector2 _) => base.Projectile.Size * 0.5f), null);
            Texture2D value = TextureAssets.Projectile[base.Type].Value;
            Main.EntitySpriteDraw(value, base.Projectile.Center - Main.screenPosition, value.Frame(1, Main.projFrames[base.Type], 0, base.Projectile.frame), GetRocketColor(), base.Projectile.rotation, value.Size() * 0.5f, base.Projectile.scale, SpriteEffects.None);
            return false;
        }

        public override void OnKill(int timeLeft) {
            if (base.Projectile.owner == Main.myPlayer) {
                for (int i = 1; i < base.Projectile.oldPos.Length; i++) {
                    if (Main.rand.NextBool(3)) {
                        float num = MathHelper.Lerp(-MathF.PI / 4f, MathF.PI / 4f, (float)i / (float)base.Projectile.oldPos.Length);
                        Vector2 position = base.Projectile.oldPos[i] + base.Projectile.Size * 0.5f;
                        Vector2 spinningpoint = (base.Projectile.oldPos[i - 1] - base.Projectile.oldPos[i]).SafeNormalize(Vector2.Zero);
                        spinningpoint = spinningpoint.RotatedBy(num);
                        spinningpoint *= Main.rand.NextFloat(12f, 18f);
                        Projectile.NewProjectile(base.Projectile.GetSource_FromThis(), position, spinningpoint, ModContent.ProjectileType<PartySparkle>(), base.Projectile.damage, 2f, base.Projectile.owner);
                    }
                }
            }

            base.Projectile.ExpandHitboxBy(350);
            base.Projectile.Damage();
            SoundEngine.PlaySound(in SoundID.Item14, base.Projectile.Center);
            if (!Main.dedServ) {
                //dedServ守卫:批量spawn时孤儿实例语义,别用null判断
                for (int i = 0; i < 32; i++)
                    PRTLoader.NewParticle<PRT_GlowLightParticle>(Projectile.Center, CEUtils.randomPointInCircle(16), Color.LightBlue, Main.rand.NextFloat(0.6f, 1f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 22);
                float scale = 3f;
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Main.DiscoColor * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.06f, 16);
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Main.DiscoColor, 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.034f, 12);

            }
        }
    }
}