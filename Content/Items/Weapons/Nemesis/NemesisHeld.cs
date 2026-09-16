using CalamityEntropy.Common;
using CalamityEntropy.Core;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Nemesis
{
    internal class NemesisHeld : BaseKnife
    {
        //蓄力条贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/GenericBarBack")]
        internal static Asset<Texture2D> BarBackTex;
        [VaultLoaden("CalamityEntropy/Assets/GenericBarFront")]
        internal static Asset<Texture2D> BarFrontTex;
        public override int TargetID => ModContent.ItemType<Nemesis>();
        public override string trailTexturePath => EffectLoader.AssetPath + "MotionTrail3";
        public override string gradientTexturePath => EffectLoader.AssetPath + "NemesisBar";
        public override void SetKnifeProperty() {
            Projectile.width = Projectile.height = 182;
            overOffsetCachesRoting = MathHelper.ToRadians(6);
            IgnoreImpactBoxSize = true;
            drawTrailHighlight = false;
            canDrawSlashTrail = true;
            Incandescence = true;
            drawTrailBtommWidth = 60;
            drawTrailTopWidth = 130;
            distanceToOwner = 120;
            OtherMeleeSize = 1f;
            unitOffsetDrawZkMode = -8;
            SwingData.starArg = 60;
            SwingData.baseSwingSpeed = 4;
            ShootSpeed = 20;
            Length = 124;
        }
        public override void Shoot() {
            int type = ModContent.ProjectileType<NemesisProj>();
            if (Projectile.ai[0] == 2) {
                if (Time < 140 * updateCount) {
                    return;
                }
                SoundEngine.PlaySound(SoundID.Item71, Owner.position);
                type = ModContent.ProjectileType<NemesisAlt>();

                Projectile.NewProjectile(Source, ShootSpanPos, ShootVelocity.RotatedByRandom(0.66f)
                    , type, Projectile.damage * 10, Projectile.knockBack, Owner.whoAmI, 0f, 0);

                return;
            }
            if (Projectile.ai[0] == 1) {
                SoundEngine.PlaySound(SoundID.Item69, Owner.position);
                type = ModContent.ProjectileType<EXNemesisProj>();
                Vector2 pos = InMousePos;
                pos.Y -= 800;
                pos.X -= Owner.direction * 320;
                Vector2 ver = new Vector2(Owner.direction * 2, 6);
                Projectile.NewProjectile(Source, pos, ver, type, Projectile.damage * 5
                    , Projectile.knockBack, Owner.whoAmI, 0f, 0);
                return;
            }
            Vector2 orig = ShootSpanPos + new Vector2(0, -800);
            Vector2 toMou = orig.To(InMousePos);
            for (int i = 0; i < 3; i++) {
                Vector2 spwanPos = orig + toMou.UnitVector() * 600;
                spwanPos.X += Main.rand.Next(-260, 260);
                spwanPos.Y -= 660;
                Vector2 ver = spwanPos.To(InMousePos).UnitVector() * 26;
                ver = ver.RotatedByRandom(0.2f);
                ver *= Main.rand.NextFloat(0.6f, 1.33f);
                Projectile.NewProjectile(Source, spwanPos, ver, type, Projectile.damage / 4
                    , Projectile.knockBack, Owner.whoAmI, 0f, 0);
            }
        }

        public override bool PreInOwnerUpdate() {
            if (Projectile.ai[0] == 2 && Time == 140 * updateCount && Main.myPlayer == Projectile.owner && Main.mouseRight) {
                Time--;
            }
            if (Time == 0 && Projectile.ai[0] == 0) {
                SoundEngine.PlaySound(SoundID.Item71, Owner.position);
            }

            if (Projectile.ai[0] == 1) {
                shootSengs = 0.95f;
                maxSwingTime = 22;
                canDrawSlashTrail = false;
                SwingData.starArg = 13;
                SwingData.baseSwingSpeed = 2;
                SwingData.ler1_UpLengthSengs = 0.1f;
                SwingData.ler1_UpSpeedSengs = 0.1f;
                SwingData.ler1_UpSizeSengs = 0.062f;
                SwingData.ler2_DownLengthSengs = 0.01f;
                SwingData.ler2_DownSpeedSengs = 0.14f;
                SwingData.ler2_DownSizeSengs = 0;
                SwingData.minClampLength = 160;
                SwingData.maxClampLength = 200;
                SwingData.maxSwingTime = 20;
                SwingData.ler1Time = 8;
                OtherMeleeSize = 1.24f;
                return true;
            }
            else if (Projectile.ai[0] == 2) {
                if (DownRight && Time < 140 * updateCount) {
                    canDrawSlashTrail = false;
                    SwingData.maxSwingTime = maxSwingTime = (int)(200 / SetSwingSpeed(1));
                    SwingData.starArg = 60;
                    SwingData.baseSwingSpeed = 0;
                    SwingData.minClampLength = 160;
                    SwingData.maxClampLength = 160;
                    SwingData.ler1_UpSizeSengs = 0;
                    if (Time == 130 * updateCount) {
                        SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
                    }
                }
                else {
                    canDrawSlashTrail = true;
                    if (Time < 140 * updateCount) {
                        Projectile.Kill();
                    }

                    speed = MathHelper.ToRadians(SwingData.baseSwingSpeed) / SetSwingSpeed(1);
                    SwingData.baseSwingSpeed = 9;
                    SwingData.minClampLength = 160;
                    SwingData.maxClampLength = 200;
                    SwingData.ler1_UpSizeSengs = 0;
                    if (Time % 20 == 0) {
                        canShoot = true;
                    }

                }
                return true;
            }

            if (Time == 10 * updateCount) {
                canShoot = true;
            }
            return base.PreInOwnerUpdate();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.numHits == 0) {
                int type = ModContent.ProjectileType<NemesisProj>();
                for (int i = 0; i < 6; i++) {
                    Vector2 spwanPos = target.Center;
                    Vector2 ver = CEUtils.randVr(3, 18);
                    Projectile.NewProjectile(Source, spwanPos, ver, type, Projectile.damage
                        , Projectile.knockBack, Owner.whoAmI, 1f, 0);
                }
            }
        }
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.ai[0] == 2 && Time <= 140 * updateCount) {
                return false;
            }
            return base.CanHitNPC(target);
        }
        public override void DrawSwing(SpriteBatch spriteBatch, Color lightColor) {
            float newCharge = Time;
            float maxCharge = 140 * updateCount;
            if (Projectile.ai[0] == 2 && newCharge <= maxCharge) {
                {
                    Texture2D barBG = BarBackTex.Value;
                    Texture2D barFG = BarFrontTex.Value;
                    float barScale = 2f;
                    Vector2 barOrigin = barBG.Size() * 0.5f;
                    Vector2 drawPos = Owner.MountedCenter + new Vector2(0, 60) - Main.screenPosition;
                    float sengs = 1 - (maxCharge - newCharge) / maxCharge;
                    Rectangle frameCrop = new Rectangle(0, 0, (int)(sengs * barFG.Width), barFG.Height);
                    Color color = Color.OrangeRed;
                    spriteBatch.Draw(barBG, drawPos, null, color, 0f, barOrigin, barScale, 0, 0f);
                    spriteBatch.Draw(barFG, drawPos, frameCrop, color * 0.8f, 0f, barOrigin, barScale, 0, 0f);
                }
            }
            if (Projectile.ai[0] != 0) {
                {
                    Texture2D texture = TextureValue;
                    Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
                    Vector2 drawOrigin = new Vector2(texture.Width / 2, texture.Height / 2);
                    SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

                    Vector2 toOwner = Projectile.Center - Owner.GetPlayerStabilityCenter();
                    Vector2 offsetOwnerPos = toOwner.GetNormalVector() * -6 * Projectile.spriteDirection;

                    Vector2 drawPosValue = Projectile.Center - RodingToVer(toProjCoreMode, (Projectile.Center - Owner.Center).ToRotation()) + offsetOwnerPos;
                    Vector2 trueDrawPos = drawPosValue - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
                    float drawRoting = Projectile.rotation;
                    if (Projectile.spriteDirection == -1) {
                        drawRoting += MathHelper.Pi;
                    }
                    if (Projectile.ai[0] != 0) {
                        VaultUtils.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, trueDrawPos
                        , null, new Color(255, 50, 50), drawRoting, drawOrigin, Projectile.scale, effects);
                    }
                }
            }
            base.DrawSwing(spriteBatch, lightColor);
        }
    }
}
