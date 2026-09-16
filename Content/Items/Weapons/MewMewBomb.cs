using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class MewMewBomb : ModItem, ICEChargeWeapon, IGetFromStarterBag
    {
        // 周期就绪 8 秒；原潜伏乘数 伤害2.5/弹速1 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.Periodic(8f, 2.5f, 1f);

        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.damage = 18;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 2f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<MewMewBombThrow>();
            Item.shootSpeed = 6.4f;
            Item.DamageType = DamageClass.Ranged;
            Item.crit = 8;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.LicenseCat)
                .AddIngredient(ItemID.Dynamite, 99)
                .AddTile(TileID.Anvils)
                .Register();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            SoundEngine.PlaySound(SoundID.Item1, position);
            SoundEngine.PlaySound(SoundID.Item1, position);
            if (CEChargeWeapon.TryConsume(player, Item)) {
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                if (p >= 0 && p < Main.maxProjectiles) {
                    CEChargeWeapon.Empower(p);
                }
                return false;
            }
            return true;
        }

        public bool OwnAble(Player player, ref int count) {
            if (player.Entropy().drCrystals == null) return false;
            return player.Entropy().drCrystals[4];
        }
    }
    public class MewMewBombThrow : ModProjectile
    {
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRots = new List<float>();
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 800;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = true;
            Projectile.MaxUpdates = 4;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Exploded);
            writer.WriteVector2(hitPos);
            writer.WriteVector2(ExplodeTargetPos);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            Exploded = reader.ReadBoolean();
            hitPos = reader.ReadVector2();
            ExplodeTargetPos = reader.ReadVector2();
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/MewMewBomb";
        public bool Hitted { get { return Projectile.ai[1] > 0; } set { Projectile.ai[1] = value ? 1 : 0; } }
        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (Projectile.velocity.X != oldVelocity.X) {
                Projectile.velocity.X = -oldVelocity.X * 1f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y) {
                Projectile.velocity.Y = -oldVelocity.Y * 1f;
            }
            if (Projectile.localAI[1] <= 0) {
                Projectile.localAI[1] = 4 * Projectile.MaxUpdates;
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Pink * 1.1f, 0.01f).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, Projectile.scale * 0.4f, 20);
                SoundEngine.PlaySound(SoundID.Item56 with { Volume = 1.5f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item58 with { Volume = 1f, Pitch = Main.rand.NextFloat(-0.4f, 0.4f) }, Projectile.Center);
            }

            return false;
        }
        public bool ExplodeEffect = true;
        public override void AI() {
            if (Hitted)
                Projectile.tileCollide = false;
            Projectile.localAI[1]--;
            if (Main.myPlayer != Projectile.owner && Exploded && ExplodeEffect) {
                EXPLODE();
            }
            if (Exploded && Projectile.timeLeft == 2 && Projectile.IsEmpowered()) {
                foreach (Player plr in Main.ActivePlayers) {
                    if (plr.Entropy().immune > 0)
                        continue;
                    if (Projectile.Colliding(Projectile.Hitbox, plr.getRect())) {
                        plr.Hurt(PlayerDeathReason.ByProjectile(Projectile.owner, Projectile.whoAmI), 20, 0, true, false);
                        CEUtils.PlaySound("firedeath hiss", 1.5f, plr.Center, 8, 0.4f);
                        plr.Entropy().immune = plr.Entropy().MewmewSmokeEffect = 150;
                    }
                }
            }
            float or = Projectile.rotation;
            if (Projectile.localAI[0]++ > 40 && !Hitted) {
                Projectile.velocity.Y += 0.07f;
            }
            if (!Hitted) {
                Projectile.velocity *= 0.998f;
                if (Projectile.timeLeft < 8) {
                    if (!Exploded) {
                        if (Main.myPlayer == Projectile.owner)
                            EXPLODE();
                    }
                }
            }
            else {
                Projectile.velocity *= 0;
                if (Projectile.ai[2] < 1f)
                    Projectile.ai[2] += 1 / 50f;
                else {
                    Projectile.localAI[2] += 0.01f;
                    Projectile.scale = 1f + Projectile.localAI[2] + (0.5f + 0.5f * (float)(Math.Sin(Projectile.localAI[2] * 18))) * 0.04f;
                    if (Projectile.localAI[2] > (Projectile.IsEmpowered() ? 1 : 0.5f)) {
                        if (!Exploded) {
                            if (Main.myPlayer == Projectile.owner)
                                EXPLODE();
                        }
                    }
                }
                Vector2 o = Projectile.Center;
                Projectile.Center = Vector2.Lerp(hitPos, ExplodeTargetPos, CEUtils.Parabola(Projectile.ai[2] * 0.5f, 1));
                Projectile.rotation += (Projectile.Center - o).X * 0.03f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.03f;
            for (float i = 0.2f; i <= 1; i += 0.5f) {
                oldPos.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity, i));
                oldRots.Add(CEUtils.RotateTowardsAngle(or, Projectile.rotation, i, false));
                if (oldPos.Count > 80) {
                    oldPos.RemoveAt(0);
                    oldRots.RemoveAt(0);
                }
            }
        }
        public bool Exploded = false;
        public void EXPLODE() {
            ExplodeEffect = false;
            CEUtils.PlaySound("PumpkinExplode" + Main.rand.Next(1, 3), Main.rand.NextFloat(1.65f, 1.85f), Projectile.Center);
            if (Projectile.IsEmpowered()) {
                CEUtils.PlaySound("WulfrumScrewdriverScrewHit", Main.rand.NextFloat(0.8f, 1.25f), Projectile.Center);
            }
            Exploded = true;
            Projectile.ResetLocalNPCHitImmunity();
            Projectile.timeLeft = 4;

            SoundEngine.PlaySound(SoundID.Item58 with { Volume = 1.6f, Pitch = Main.rand.NextFloat(0.2f, 0.5f) }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item58 with { Volume = 1.6f, Pitch = Main.rand.NextFloat(0.2f, 0.5f) }, Projectile.Center);
            float scale = Projectile.IsEmpowered() ? 1.3f : 1f;
            for (int i = 0; i < 46; i++) {
                var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Fireworks, Main.rand.NextFloat(-12, 12) * scale, Main.rand.NextFloat(-12, 12) * scale);
                d.scale = Main.rand.NextFloat(1.2f, 1.4f) * scale;
                d.noGravity = true;

                d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Fireworks);
                d.velocity = (Main.rand.NextBool() ? Vector2.UnitX : Vector2.UnitY) * Main.rand.NextFloat(-50, 50) * scale;
                d.scale = Main.rand.NextFloat(1.2f, 1.4f) * scale;
                d.noGravity = true;
            }
            int plt = Projectile.IsEmpowered() ? 15 : 12;
            Vector2 v = Vector2.UnitX;
            PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, v, Color.DeepPink, 0.04f * scale).Configure(false, plt, new Vector2(8, 4), true, false);
            PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, v, Color.White, 0.03f * scale).Configure(false, plt, new Vector2(8, 4), true, false);

            v = Vector2.UnitY;
            PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, v, Color.DeepPink, 0.04f * scale).Configure(false, plt, new Vector2(8, 4), true, false);
            PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, v, Color.White, 0.03f * scale).Configure(false, plt, new Vector2(8, 4), true, false);
            if (Main.myPlayer == Projectile.owner)
                CEUtils.SyncProj(Projectile.whoAmI);
        }
        public override void OnKill(int timeLeft) {

        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Hitted && !Exploded)
                return false;
            if (Exploded) {
                int wd = Projectile.IsEmpowered() ? 60 : 26;
                Rectangle h = Projectile.Center.getRectCentered(CrossBombDist * 2, wd);
                Rectangle w = Projectile.Center.getRectCentered(wd, CrossBombDist * 2);
                return targetHitbox.Intersects(h) || targetHitbox.Intersects(w);
            }
            return null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Hitted)
                return;
            CEUtils.PlaySound("HIT", 1.8f, Projectile.Center);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Pink * 1.25f, 0.01f).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, Projectile.scale * 0.4f, 20);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Pink * 1.25f, 0.01f).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, Projectile.scale * 1.4f, 20);
            SoundEngine.PlaySound(SoundID.Item56 with { Volume = 1.5f }, Projectile.Center);
            Hitted = true;
            List<NPC> targetNearby = CEUtils.FindSomeNearEnemies(Projectile.Center, 24, 900);
            Vector2 targetPos = Projectile.Center;
            Vector2 bestTarget = Projectile.Center;
            int HitCount = 0;
            for (int i = 0; i < 128; i++) {
                targetPos = CEUtils.randomPointInCircle(CrossBombDist * 1.2f) + Projectile.Center;
                int hitCountTemp = 0;
                Rectangle h = targetPos.getRectCentered(CrossBombDist * 2, Projectile.IsEmpowered() ? 60 : 26);
                Rectangle w = targetPos.getRectCentered(Projectile.IsEmpowered() ? 60 : 26, CrossBombDist * 2);
                foreach (NPC n in targetNearby) {
                    if (w.Intersects(n.Hitbox) || h.Intersects(n.Hitbox))
                        hitCountTemp++;
                }
                if (hitCountTemp > HitCount) {
                    HitCount = hitCountTemp;
                    bestTarget = targetPos;
                    if (HitCount >= targetNearby.Count)
                        break;
                }
            }
            hitPos = Projectile.Center;
            ExplodeTargetPos = bestTarget;
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public Vector2 ExplodeTargetPos = Vector2.Zero;
        public Vector2 hitPos = Vector2.Zero;
        public int CrossBombDist => Projectile.IsEmpowered() ? 500 : 320;
        public override bool PreDraw(ref Color lightColor) {
            if (Exploded)
                return false;
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            if (oldPos.Count > 1) {
                for (int i = 0; i < oldPos.Count; i++) {
                    float p = ((float)(1 + i) / oldPos.Count);
                    Color clr = Color.Pink * 0.44f * p;
                    Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, clr, oldRots[i], tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
                }
            }
            for (float r = 0; r < MathHelper.TwoPi; r += MathHelper.PiOver4) {
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + r.ToRotationVector2() * 2, null, Color.White * 0.5f, Projectile.rotation, tex.Size().Half(), Projectile.scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + r.ToRotationVector2() * 4, null, Color.White * 0.5f, Projectile.rotation, tex.Size().Half(), Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size().Half(), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
