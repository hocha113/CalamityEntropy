using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Uhrwerk;

public class UhrwerkHammerCallout : ModProjectile
{
    public override string Texture => "CalamityEntropy/Content/Projectiles/Uhrwerk/uhrwerkHammer_body";

    public override void SetStaticDefaults() {
        Main.projFrames[Projectile.type] = 1;

    }
    public override void SetDefaults() {
        Projectile.DamageType = DamageClass.Melee;
        Projectile.width = 96;
        Projectile.height = 96;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.light = 0f;
        Projectile.scale = 1.6f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 4;
        Projectile.ArmorPenetration = 5;
    }

    public override void AI() {
        Player player = Main.player[Projectile.owner];
        Projectile.ai[0]++;

        if (Projectile.ai[0] % 8 == 0) {
            SoundEngine.PlaySound(SoundID.Item22, Projectile.Center);
        }

        Vector2 playerRotatedPoint = player.RotatedRelativePoint(player.MountedCenter, true);
        if (Main.myPlayer == Projectile.owner) {
            HandleChannelMovement(player, playerRotatedPoint);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = player.Center + Projectile.rotation.ToRotationVector2() * 32 * Projectile.scale;
        if (Projectile.Entropy().OnProj != -1) {
            Projectile.Center = Projectile.Entropy().OnProj.ToProj().Center + Projectile.rotation.ToRotationVector2() * 32 * Projectile.scale;
        }
        if (Projectile.velocity.X > 0) {
            player.direction = 1;

        }
        else {
            player.direction = -1;
        }
        player.itemRotation = (Projectile.velocity * player.direction).ToRotation();
        player.heldProj = Projectile.whoAmI;
        player.itemTime = 2;
        player.itemAnimation = 2;
        if (!player.channel) {
            Projectile.timeLeft = 1;
        }
        soundCd--;
    }
    public void HandleChannelMovement(Player player, Vector2 playerRotatedPoint) {
        float speed = 1f;
        Vector2 newVelocity = (Main.MouseWorld - playerRotatedPoint).SafeNormalize(Vector2.UnitX * player.direction) * speed;

        if (Projectile.velocity.X != newVelocity.X || Projectile.velocity.Y != newVelocity.Y) {
            Projectile.netUpdate = true;
        }
        Projectile.velocity = newVelocity;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
        Player player = Main.player[Projectile.owner];
        int bsize = ((int)(80 * Projectile.scale));
        Vector2 c = player.Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
        if (Projectile.Entropy().OnProj != -1) {
            c = Projectile.Entropy().OnProj.ToProj().Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
        }
        return new Rectangle((int)c.X - bsize / 2, (int)c.Y - bsize / 2, bsize, bsize).Intersects(targetHitbox);

    }
    public int soundCd = 0;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        Player Owner = Main.player[Projectile.owner];
        SoundStyle hitSound = new SoundStyle("CalamityEntropy/Assets/Sounds/chainsaw", SoundType.Ambient);
        if (soundCd <= 0) {
            SoundEngine.PlaySound(hitSound, Projectile.Center);
            soundCd = 16;
        }
        float sparkCount = 1;
        for (int i = 0; i < sparkCount; i++) {
            Vector2 sparkVelocity2 = new Vector2(16, 0).RotatedByRandom(3.14159f) * Main.rand.NextFloat(0.5f, 1.8f);
            int sparkLifetime2 = Main.rand.Next(23, 35);
            float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
            Color sparkColor2 = Color.DarkRed;

            float velc = 0.7f;
            if (Main.rand.NextBool()) {
                //PRT_AltSpark跟LineCal随机混用,旧Calamity spark/Lines二选一
                PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * velc, sparkColor2, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
            }
            else {
                //LineCal Configure(false,lifetime)对齐Calamity LineParticle
                PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * velc, Main.rand.NextBool() ? Color.Red : Color.Firebrick, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
            }
        }
    }
    public override bool PreDraw(ref Color dc) {
        Texture2D tx = ModContent.Request<Texture2D>(Texture).Value;

        SpriteEffects ef = SpriteEffects.None;
        if (Projectile.velocity.X < 0) {
            ef = SpriteEffects.FlipVertically;
        }

        var origin = tx.Size() / 2 + new Vector2(8, 10) * (Projectile.velocity.X > 0).ToDirectionInt();
        Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, dc, Projectile.rotation, origin, new Vector2(Projectile.scale, Projectile.scale), ef, 0);
        return false;
    }
    public override bool? CanCutTiles() {
        return false;
    }
}