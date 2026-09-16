using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Projectiles.TwistedTwin;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class RailPulseBowProjectile : ModProjectile
    {
        public float back = 0;
        public float backspeed = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Default;
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            Projectile.ArmorPenetration = 86;
        }
        int ammoID = -1;
        public override void AI() {

            Player player = Projectile.owner.ToPlayer();
            if (player.dead) {
                Projectile.Kill();
            }
            if (Projectile.Entropy().IndexOfTwistedTwinShootedThisProj != -1 && !(Projectile.Entropy().IndexOfTwistedTwinShootedThisProj.ToProj().active)) {
                Projectile.Kill();
            }
            if (player.HasAmmo(player.HeldItem)) {
                player.PickAmmo(player.HeldItem, out int projID, out float shootSpeed, out int damage, out float kb, out ammoID, true);
            }
            Vector2 playerRotatedPoint = player.RotatedRelativePoint(player.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner) {
                HandleChannelMovement(player, playerRotatedPoint);
            }
            Projectile.Center = player.MountedCenter;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center += new Vector2(12, 6 * Projectile.direction).RotatedBy(Projectile.rotation);
            if (Projectile.velocity.X >= 0) {
                player.direction = 1;
                Projectile.direction = 1;
                player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + MathHelper.ToRadians(12 + Projectile.ai[1]));
            }
            else {
                player.direction = -1;
                Projectile.direction = -1;
                player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.Pi - MathHelper.ToRadians(12 + Projectile.ai[1]));
            }
            if (player.HeldItem.type == ModContent.ItemType<RailPulseBow>()) {
                Projectile.timeLeft = 3;
            }
            if (player.channel && Projectile.ai[1] < maxCharge) {
                player.itemAnimation = 3;
                player.itemTime = 3;
                if (Projectile.ai[1] < maxCharge) {
                    Projectile.ai[1] += 1.8f * player.GetTotalAttackSpeed(DamageClass.Ranged) * (1 + player.Entropy().WeaponBoost);
                    if (Projectile.ai[1] >= maxCharge) {
                    }
                }

            }
            else {
                if (Projectile.ai[1] > 16 && player.HasAmmo(player.HeldItem)) {
                    if (Main.myPlayer == Projectile.owner) {
                        if (player.HeldItem.ModItem is RailPulseBow gw) {
                            gw.cs = true;
                        }
                        player.PickAmmo(player.HeldItem, out int projID, out float shootSpeed, out int damage, out float kb, out ammoID, false);
                        if (player.HeldItem.ModItem is RailPulseBow gw2) {
                            gw2.cs = false;
                        }
                        int p = Projectile.NewProjectile(player.GetSource_ItemUse_WithPotentialAmmo(player.HeldItem, ammoID), Projectile.Center, new Vector2(shootSpeed, 0).RotatedBy(Projectile.rotation) * (Projectile.ai[1] / (float)maxCharge), projID, (int)(damage * (Projectile.ai[1] / (float)maxCharge) * (Projectile.ai[1] >= maxCharge ? 1.8f : 1) * (Projectile.Entropy().IndexOfTwistedTwinShootedThisProj == -1 ? 1 : TwistedTwinMinion.damageMul)), kb * (Projectile.ai[1] / (float)maxCharge), Projectile.owner);
                        p.ToProj().scale = 1.2f * Projectile.scale;
                        if (Projectile.ai[1] >= maxCharge) {
                            p.ToProj().CritChance = 100;
                            p.ToProj().OriginalCritChance = 100;
                            p.ToProj().Entropy().rpBow = true;
                        }
                        if (Main.netMode == NetmodeID.MultiplayerClient) {
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p);
                        }

                    }
                    backspeed = 1.2f * (Projectile.ai[1] / (float)maxCharge);
                    SoundStyle SwingSound = new SoundStyle("CalamityEntropy/Assets/Sounds/HellkiteSwing", 2);
                    SwingSound.Volume = 0.6f;
                    SwingSound.Pitch = 0.4f + 2f * (Projectile.ai[1] / (float)maxCharge);
                    if (Projectile.ai[1] >= maxCharge) {
                        SwingSound = new SoundStyle("CalamityEntropy/Assets/Sounds/energyImpact");
                        SwingSound.Pitch = 1.2f;
                        SwingSound.Volume = 0.46f;

                    }
                    SoundEngine.PlaySound(SwingSound, Projectile.Center);
                }
                Projectile.ai[1] = 0;
            }
            back += backspeed;
            backspeed *= 0.9f;
            back *= 0.9f;
            player.heldProj = Projectile.whoAmI;

            Projectile.ai[0]++;

        }
        public void HandleChannelMovement(Player player, Vector2 playerRotatedPoint) {
            float speed = 16f;
            Vector2 newVelocity = (Main.MouseWorld - playerRotatedPoint).SafeNormalize(Vector2.UnitX * player.direction) * speed;

            if (Projectile.velocity.X != newVelocity.X || Projectile.velocity.Y != newVelocity.Y) {
                Projectile.netUpdate = true;
            }
            Projectile.velocity = newVelocity;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public int maxCharge = 40;
        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            Vector2 up = Projectile.Center + new Vector2(-20, -24).RotatedBy(Projectile.rotation) + new Vector2(-back, 0).RotatedBy(Projectile.rotation);
            Vector2 down = Projectile.Center + new Vector2(-20, 24).RotatedBy(Projectile.rotation) + new Vector2(-back, 0).RotatedBy(Projectile.rotation);
            Vector2 middle = Projectile.Center + new Vector2(-21 - Projectile.ai[1] * 0.35f, 0).RotatedBy(Projectile.rotation) + new Vector2(-back, 0).RotatedBy(Projectile.rotation);
            CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, up, middle, Color.Red, 2, 2);
            CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, down, middle, Color.Red, 2, 2);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<Vector2> v2ss = new List<Vector2>() { new Vector2(-2, -2), new Vector2(-2, 2), new Vector2(2, -2), new Vector2(2, 2) };
            for (int i = 0; i < 6 * (Projectile.ai[1] / (float)maxCharge) * (Projectile.ai[1] >= maxCharge ? 1f : 0.8f); i++) {
                foreach (Vector2 v in v2ss) {
                    Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + v + new Vector2(-back, 0).RotatedBy(Projectile.rotation), null, Color.Red * (Projectile.ai[1] / (float)maxCharge) * (Projectile.ai[1] >= maxCharge ? 1f : 0.95f), Projectile.rotation, texture.Size() / 2, Projectile.scale, (Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically));
                }
            }

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(-back, 0).RotatedBy(Projectile.rotation), null, Color.White, Projectile.rotation, texture.Size() / 2, Projectile.scale, (Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically));



            if (Projectile.ai[1] > 0) {
                if (ammoID >= 0) {
                    Main.spriteBatch.End();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    List<Vector2> v2s = new List<Vector2>() { new Vector2(-2, -2), new Vector2(-2, 2), new Vector2(2, -2), new Vector2(2, 2) };
                    for (int i = 0; i < 6 * (Projectile.ai[1] / (float)maxCharge) * (Projectile.ai[1] >= maxCharge ? 1f : 0.8f); i++) {
                        foreach (Vector2 v in v2s) {
                            Main.EntitySpriteDraw(TextureAssets.Item[ammoID].Value, Projectile.Center + v + new Vector2(-back, 0).RotatedBy(Projectile.rotation) + new Vector2(-Projectile.ai[1] * 0.35f - 20, 0).RotatedBy(Projectile.rotation) - Main.screenPosition, null, Color.Red * (Projectile.ai[1] / (float)maxCharge) * (Projectile.ai[1] >= maxCharge ? 1f : 0.95f), Projectile.rotation - MathHelper.PiOver2, TextureAssets.Item[ammoID].Value.Size() / 2 * new Vector2(1, 0), Projectile.scale * 1.2f * new Vector2(1f, 1f), (Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
                        }
                    }

                    Main.spriteBatch.End();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                }
                Main.EntitySpriteDraw(TextureAssets.Item[ammoID].Value, Projectile.Center + new Vector2(-back, 0).RotatedBy(Projectile.rotation) + new Vector2(-Projectile.ai[1] * 0.35f - 20, 0).RotatedBy(Projectile.rotation) - Main.screenPosition, null, Color.White, Projectile.rotation - MathHelper.PiOver2, TextureAssets.Item[ammoID].Value.Size() / 2 * new Vector2(1, 0), Projectile.scale * 1.2f * new Vector2(1, 1f), (Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));

            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
    }

}