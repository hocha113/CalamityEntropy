using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.DustCarverBow
{
    public class CarverSpirit : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = Projectile.height = 32;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 5;
            Projectile.minion = true;
            Projectile.minionSlots = 0;
        }

        public enum Mode
        {
            Defending,
            Penetrate,
            Shooting
        }
        public override bool? CanHitNPC(NPC target) {
            return mode == Mode.Penetrate ? null : false;
        }
        public Mode mode { get { return (Mode)Projectile.ai[0]; } set { Projectile.ai[0] = (int)value; } }
        public int Delay = 0;

        public List<Vector2> OldPos = new List<Vector2>();
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 80;

        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (HealingCooldown <= 0) {
                HealingCooldown = 30.ApplyCdDec(Projectile.GetOwner());
                Projectile.GetOwner().Heal(1);
            }
        }
        public int HealingCooldown = 0;
        public override void AI() {
            HealingCooldown--;

            white--;
            Player player = Projectile.GetOwner();
            if (player.heldProj < 0) {
                foreach (var pr in Main.ActiveProjectiles) {
                    if (pr.owner == Projectile.owner && pr.ModProjectile is DustCarverHeld) {
                        player.heldProj = pr.whoAmI;
                        break;
                    }
                }
            }
            if (player.HeldItem.ModItem is DustCarver && !player.dead && player.heldProj >= 0) {
                Projectile.timeLeft = 4;
            }
            else {
                return;
            }

            float DelayMult = player.GetWeaponAttackSpeed(player.HeldItem);
            Projectile.CritChance = player.GetWeaponCrit(player.HeldItem);
            Projectile.damage = player.GetWeaponDamage(player.HeldItem) / 20;
            Projectile.MaxUpdates = 1;

            if (Main.myPlayer == Projectile.owner && Keyboard.GetState().IsKeyDown(Keys.LeftShift)) {
                Projectile.velocity *= 0;
                return;
            }
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 4000) {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(100);
                Projectile.velocity *= 0;
            }
            int count = 0;
            int id = 0;
            foreach (Projectile proj in Main.ActiveProjectiles) {
                if (proj.type == Projectile.type && proj.owner == Projectile.owner && proj.ModProjectile is CarverSpirit cs && cs.mode == mode) {
                    if (proj.whoAmI == Projectile.whoAmI) {
                        id = count;
                    }
                    count++;
                }
            }
            Delay--;
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Vector2.Lerp(player.Center, Projectile.Center, 0.32f), 3400);
            switch (mode) {
                case Mode.Penetrate: {
                    if (target == null) {
                        Projectile.pushByOther(0.6f);
                        if (CEUtils.getDistance(Projectile.Center, player.Center) > 120) {
                            Projectile.velocity += (player.Center - Projectile.Center).normalize();
                            Projectile.velocity *= 0.98f;
                        }
                        Projectile.rotation += Projectile.velocity.X * 0.02f;
                    }
                    else {
                        Projectile.pushByOther(3f);
                        if (Delay == 1)
                            Projectile.velocity *= 0.1f;
                        if (CEUtils.getDistance(Projectile.Center, target.Center) < 300) {
                            if (Delay <= 0) {
                                CEUtils.PlaySound("SwiftSlice", Main.rand.NextFloat(1.6f, 2), Projectile.Center);
                                //PRT_DOracleSlash 光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                                var p = PRTLoader.NewParticle<PRT_DOracleSlash>(target.Center + (Projectile.Center - target.Center).normalize() * 80, Vector2.Zero, Color.Crimson, 140);
                                p.centerColor = Color.White;
                                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, (target.Center - Projectile.Center).ToRotation(), 16);
                                Projectile.ResetLocalNPCHitImmunity();
                                Delay = (int)(8 / DelayMult);
                                Projectile.velocity = (target.Center + (target.Center - Projectile.Center).normalize() * 250 - Projectile.Center) / (int)(8 / DelayMult);
                            }
                        }
                        else {
                            Projectile.velocity += (target.Center - Projectile.Center).normalize() * 2;
                            Projectile.velocity *= 0.97f;
                        }
                        Projectile.rotation = (target.Center - Projectile.Center).ToRotation() + MathHelper.PiOver4;
                    }
                    break;
                }
                case Mode.Shooting: {
                    player.Entropy().MouseWorldListener = true;
                    Vector2 tpos = player.Center + (player.heldProj.ToProj().rotation.ToRotationVector2() * -200).RotatedBy((id - count / 2f) * 0.22f);

                    Projectile.velocity += (tpos - Projectile.Center) * float.Min(0.1f, CEUtils.getDistance(Projectile.Center, tpos) / 300f);
                    Projectile.velocity *= 0.6f;
                    Projectile.pushByOther(0.6f);

                    Projectile.rotation = ((target == null ? player.Entropy().MouseWorld : target.Center) - Projectile.Center).ToRotation();
                    if (target != null && Projectile.owner == Main.myPlayer) {
                        if (Delay <= 0) {
                            Delay = (int)(26 / DelayMult);
                            //旧对象初始化器拆成字段直赋+Configure,顺序别反
                            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 16, Vector2.Zero, new Color(255, 90, 90), 0.05f).Configure(new Vector2(0.25f, 1), Projectile.rotation, 0.36f, 24);
                            CEUtils.PlaySound("lasershoot", Main.rand.NextFloat(1f, 1.2f), Projectile.Center, 64);
                            CEUtils.PlaySound("lasershoot", Main.rand.NextFloat(1f, 1.2f), Projectile.Center, 64);

                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (target.Center - Projectile.Center).normalize() * 36, ModContent.ProjectileType<CarverBolt>(), (int)(Projectile.damage * 2.0f), 4, Projectile.owner).ToProj().scale *= 0.7f;
                            Projectile.velocity -= Projectile.rotation.ToRotationVector2() * 12;
                        }
                    }
                    break;
                }
                case Mode.Defending: {
                    bool hasproj = false;
                    Projectile tar = null;
                    float distance = 1200;
                    Projectile.rotation = (Projectile.Center - player.Center).ToRotation() + MathHelper.PiOver2;
                    foreach (Projectile p in Main.ActiveProjectiles) {
                        if (p.hostile && !p.friendly && p.damage > 0 && Math.Max(p.width, p.height) < 150 && CEUtils.getDistance(Projectile.Center, p.Center) < distance && CEUtils.getDistance(player.Center, p.Center) < 256) {
                            if (p.Colliding(p.getRect(), p.Center.getRectCentered(16, 16)) && !p.Colliding(p.getRect(), (p.Center + p.rotation.ToRotationVector2() * 320).getRectCentered(36, 36)) && !p.Colliding(p.getRect(), (p.Center + p.velocity.normalize() * 320).getRectCentered(36, 36))) {
                                if (Delay <= 0 && p.Colliding(p.getRect(), Projectile.getRect())) {
                                    p.Kill();
                                    Delay = (int)(600 / DelayMult);
                                    CEUtils.PlaySound("LightHit", 1, Projectile.Center);
                                }
                                else {
                                    tar = p;
                                    distance = CEUtils.getDistance(Projectile.Center, p.Center);
                                    hasproj = true;
                                }
                            }
                        }
                    }
                    if (hasproj && Delay <= 20) {
                        for (int i = 0; i < 4; i++) {
                            Projectile.velocity += (tar.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 4f;
                            Projectile.velocity *= 0.94f;
                        }
                    }
                    else {
                        Vector2 targetPosp;
                        int index = id;
                        int maxFlies = count;

                        targetPosp = player.Center + MathHelper.ToRadians(((float)(index) / (float)(maxFlies)) * 360).ToRotationVector2().RotatedBy(player.Entropy().CasketSwordRot * 0.4f) * 140;
                        Projectile.velocity += (targetPosp - Projectile.Center).SafeNormalize(Vector2.Zero) * 3f;
                        Projectile.velocity *= 0.9f;
                        if (CEUtils.getDistance(targetPosp, Projectile.Center) < Projectile.velocity.Length() + 32) {
                            Projectile.Center = targetPosp;
                            Projectile.velocity *= 0f;
                        }

                    }
                    break;
                }
            }
            OldPos.Add(Projectile.Center);
            if (OldPos.Count > 8)
                OldPos.RemoveAt(0);
        }
        public int white = 0;

        public override bool PreDraw(ref Color lightColor) {
            if (Main.myPlayer == Projectile.owner) {
                if (white > 0) {
                    Effect shader = CEEffectAssets.WhiteTrans;
                    Main.spriteBatch.EnterShaderRegion();
                    shader.CurrentTechnique.Passes[0].Apply();
                    shader.Parameters["strength"].SetValue(0.5f + 0.5f * ((float)Math.Cos(Main.GlobalTimeWrappedHourly * 18)));
                    lightColor = Color.White;
                }
            }
            var tex = (mode == Mode.Penetrate ? Projectile.GetTexture() : (mode == Mode.Shooting ? this.getTextureAlt() : this.getTextureAlt("AltB" + (((int)Main.GameUpdateCount / 4) % 3).ToString())));
            if (mode == Mode.Defending && Delay > 5)
                lightColor *= 0.4f;
            else {
                OldPos.Add(Projectile.Center);
                for (int i = 1; i < OldPos.Count; i++) {
                    for (float j = 0; j < 1; j += 0.1f) {
                        Main.EntitySpriteDraw(tex, Vector2.Lerp(OldPos[i - 1], OldPos[i], j) - Main.screenPosition, null, lightColor * 0.12f * ((float)i / OldPos.Count), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
                    }
                }
                OldPos.RemoveAt(OldPos.Count - 1);
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, tex));
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
