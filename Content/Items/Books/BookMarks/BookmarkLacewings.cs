using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkLacewings : BookMark, IPriceFromRecipe
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Pink;
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Lacewings");
        public override EBookProjectileEffect getEffect() {
            return new BookmarkLacewingsEffect();
        }

        public override Color tooltipColor => Color.Pink;
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<BookmarkFairy>()
                .AddIngredient<BookMarkLibra>()
                .AddIngredient(ItemID.EmpressButterfly)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        private static int projType = -1;
        public static int ProjType { get { if (projType == -1) { projType = ModContent.ProjectileType<LacewingBMMinion>(); } return projType; } }
    }

    public class BookmarkLacewingsEffect : EBookProjectileEffect
    {
    }
    public class LacewingBMMinion : BaseBookMinion
    {
        public int Delay = 0;
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.width = Projectile.height = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 5;
            Projectile.minion = true;
            Projectile.minionSlots = 0;
        }
        public int num = -1;
        public override float DamageMult => 0.75f;
        public override void AI() {
            base.AI();
            Player player = Projectile.GetOwner();
            if (Main.myPlayer != Projectile.owner || BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkLacewingsEffect>(player)) {
                Projectile.timeLeft = 3;
            }
            else {
                return;
            }

            float DelayMult = 1;
            if (ShooterModProjectile is EntropyBookHeldProjectile eb_) {
                DelayMult = eb_.CauculateAttackSpeed();
            }
            Projectile.MaxUpdates = 1;

            if (CEUtils.getDistance(Projectile.Center, player.Center) > 3000) {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(100);
                Projectile.velocity *= 0;
            }
            Delay--;
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Vector2.Lerp(player.Center, Projectile.Center, 0.32f), 3400);
            if (--num == 1)
                Projectile.velocity *= 0.2f;
            if (target == null) {
                Projectile.pushByOther(0.1f);
                if (CEUtils.getDistance(Projectile.Center, player.Center) > 100) {
                    Projectile.velocity += (player.Center - Projectile.Center).normalize() * 0.6f;
                    Projectile.velocity *= 0.98f;
                }
                dir = Math.Sign(Projectile.velocity.X);
            }
            else {
                dir = Math.Sign(target.Center.X - Projectile.Center.X);
                Projectile.pushByOther(0.05f);
                if (CEUtils.getDistance(Projectile.Center, target.Center) < 600 && Delay <= 0) {
                    CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.GetOwner(), target.Center + target.velocity, EBookProjectileEffect.FixedDamage(Projectile.GetOwner(), 30, Projectile.DamageType), 6, Projectile.DamageType).CritChance = Projectile.CritChance;
                    for (float i = 0.04f; i <= 1; i += 0.02f) {
                        Color rgbColor = Main.hslToRgb(i, 0.5f, 0.6f) * 0.25f;
                        //LineCal彩虹连线,CalamityPorts Configure(false,lifetime)仿Calamity LineParticle
                        PRTLoader.NewParticle<PRT_LineCal>(Vector2.Lerp(Projectile.Center, target.Center, i), (target.Center - Projectile.Center).normalize() * 0.01f, rgbColor, (1f - i) * 0.8f + 0.8f).Configure(false, 18);
                    }
                    if (Projectile.penetrate > 0)
                        Projectile.penetrate--;
                    Delay = (int)(30 / DelayMult);
                }
                else {
                    if (CEUtils.getDistance(Projectile.Center, target.Center) > 280) {
                        Projectile.velocity += (target.Center - Projectile.Center).normalize() * 0.8f;
                        Projectile.velocity *= 0.97f;
                    }
                    else {
                        Projectile.velocity -= (target.Center - Projectile.Center).normalize() * 0.8f;
                        Projectile.velocity *= 0.97f;
                    }
                }
                Projectile.rotation = Math.Sign(Projectile.velocity.X);
            }
            Vector3 rgb = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.33f % 1f, 1f, 0.5f).ToVector3() * 0.3f;
            rgb += Vector3.One * 0.1f;
            Lighting.AddLight(Projectile.Center, rgb);
            int num3 = 60;
            bool flag = false;
            int num4 = 50;
            Projectile.ai[2] = MathHelper.Clamp(Projectile.ai[2] + (float)flag.ToDirectionInt(), 0f, num4);
            Projectile.Opacity = Utils.GetLerpValue(num3, (float)num4 / 2f, Projectile.ai[2], clamped: true);
            int num5 = 1;
            for (int i = 0; i < num5; i++) {
                if (Main.rand.Next(5) == 0) {
                    float num6 = MathHelper.Lerp(0.9f, 0.6f, Projectile.Opacity);
                    colorDraw = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.3f % 1f, 1f, 0.5f) * 0.5f;
                    int num7 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 267, 0f, 0f, 0, colorDraw);
                    Main.dust[num7].position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height);
                    Main.dust[num7].velocity *= Main.rand.NextFloat() * 0.8f;
                    Main.dust[num7].velocity += Projectile.velocity * 0.6f;
                    Main.dust[num7].noGravity = true;
                    Main.dust[num7].fadeIn = 0.6f + Main.rand.NextFloat() * 0.7f * num6;
                    Main.dust[num7].scale = 0.35f;
                    if (num7 != 6000) {
                        Dust dust = Dust.CloneDust(num7);
                        dust.scale /= 2f;
                        dust.fadeIn *= 0.85f;
                        dust.color = new Color(255, 255, 255, 255) * 0.5f;
                    }
                }
            }

        }
        //草蛉贴图,加载期就位,不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Items/Books/BookMarks/Fairy/Lacewing")]
        internal static Texture2D LacewingTex;
        public Color colorDraw = Color.White;
        public int dir = 1;
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = LacewingTex;
            Rectangle frame = new Rectangle(0, 24 * (((int)Main.GameUpdateCount / 4) % 3), tex.Width, 24);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.Lerp(colorDraw, Color.White, 0.8f) * Projectile.Opacity, 0, new Vector2(12, 11), Projectile.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (float i = 0; i < 360; i += 60) {
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + (MathHelper.ToRadians(i) + Main.GlobalTimeWrappedHourly * 4).ToRotationVector2() * 4, frame, Color.Lerp(colorDraw, Color.White, 0.16f) * Projectile.Opacity * 1f, 0, new Vector2(12, 11), Projectile.scale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}