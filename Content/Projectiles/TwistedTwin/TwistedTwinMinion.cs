using CalamityEntropy.Content.Buffs;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.TwistedTwin
{
    public class TwistedTwinMinion : ModProjectile
    {
        //双子四形态贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/TwistedTwin/TwistedTwinMinion")]
        internal static Asset<Texture2D> BodyTex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/TwistedTwin/a1")]
        internal static Asset<Texture2D> A1Tex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/TwistedTwin/b1")]
        internal static Asset<Texture2D> B1Tex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/TwistedTwin/b2")]
        internal static Asset<Texture2D> B2Tex;
        public static float damageMul { get { return 1.0f + Main.LocalPlayer.Entropy().WeaponBoost * 0.05f; } }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.light = 0.5f;
            Projectile.minion = true;
            Projectile.minionSlots = 2;
            Projectile.netImportant = true;
        }
        public override bool? CanCutTiles() {
            return false;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (player.HasBuff(ModContent.BuffType<TwistedTwinBuff>())) {
                Projectile.timeLeft = 3;
            }
            int index = -1;
            int pos = 1;
            foreach (Projectile p in Main.projectile) {
                if (p.whoAmI == Projectile.whoAmI) {
                    break;
                }
                if (p.type == Projectile.type && p.ai[1] == Projectile.ai[1] && p.whoAmI != Projectile.whoAmI && p.active && p.owner == Projectile.owner) {
                    index = p.whoAmI;
                    pos++;
                }
            }
            Vector2 targetPos;
            float rot;
            if (Projectile.owner == Main.myPlayer) {
                rot = (Main.MouseWorld - Main.myPlayer.ToPlayer().Center).ToRotation();
                Projectile.ai[2] = rot;

            }
            else {
                rot = Projectile.ai[2];
            }
            rot += (float)(Math.PI * 0.5);
            float spacing = 56;
            if (index == -1) {
                targetPos = player.Center + (rot.ToRotationVector2() * Projectile.ai[1]) * spacing;
            }
            else {
                targetPos = index.ToProj().Center + (rot.ToRotationVector2() * Projectile.ai[1]) * spacing;
            }
            Projectile.netUpdate = true;
            Projectile.Center = Projectile.Center + (targetPos - Projectile.Center) * 0.24f;
            Projectile.ai[0]--;
            if (Projectile.owner.ToPlayer().channel) {
                Projectile.ai[0] = 6;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            SpriteEffects ef = SpriteEffects.None;
            if (Projectile.owner.ToPlayer().direction < 0) {
                ef = SpriteEffects.FlipHorizontally;
            }
            Texture2D tx;
            if (Projectile.ai[1] > 0) {
                tx = BodyTex.Value;
                if (Projectile.ai[0] > 0) {
                    tx = A1Tex.Value;
                }
            }
            else {
                tx = B1Tex.Value;
                if (Projectile.ai[0] > 0) {
                    tx = B2Tex.Value;
                }
            }
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White, 0, tx.Size() / 2, Projectile.scale, ef, 0);
            return false;
        }
    }
}

