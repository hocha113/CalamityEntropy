using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class BottledStarlightCocoon : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 3;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.IntegrateHotkey(CEKeybinds.CommandMinions);
        }
        public override void SetDefaults() {
            Item.damage = 30;
            Item.DamageType = DamageClass.Summon;
            Item.width = 22;
            Item.height = 26;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<StarlightMothMinion>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(0, 20);
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.noMelee = true;
            Item.mana = 10;
            Item.buffType = ModContent.BuffType<StarlightMoth>();
            Item.rare = ModContent.RarityType<Lunarblight>();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;

            return false;
        }
    }
    public class StarlightMoth : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<StarlightMothMinion>();
    }
    public class StarlightMothMinion : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.light = 0.8f;
            Projectile.minion = true;
            Projectile.minionSlots = 3;
            Projectile.ArmorPenetration = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 80;
        }
        public override bool? CanHitNPC(NPC target) {
            if (ai != AIStyle.Dashing)
                return false;
            return null;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public enum AIStyle
        {
            AroundOwner,
            Shooting,
            Dashing
        }
        private AIStyle _ai = AIStyle.AroundOwner;
        public AIStyle ai { get { return _ai; } set { SetAI(value); } }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write((byte)ai);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            ai = (AIStyle)reader.ReadByte();
        }
        public Vector2 vec = Vector2.Zero;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
            PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, Projectile.velocity.normalize(), Color.White, 2).Configure(false, 10, new Vector2(0.02f, 0.02f), true, false);
            PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, Projectile.velocity.normalize(), Color.Blue, 2).Configure(false, 10, new Vector2(0.016f, 0.016f), true, false);
        }
        public override void AI() {
            Player player = Projectile.GetOwner();
            Projectile.MinionCheck<StarlightMoth>();
            if (Projectile.localAI[0] == 0) {
                for (int i = 0; i < 16; i++)
                    PRTLoader.NewParticle<PRT_GlowLightParticle>(Projectile.Center, CEUtils.randomPointInCircle(9), Color.LightBlue, Main.rand.NextFloat(0.6f, 1f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 22);
                float scale = 1f;
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(140, 40, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.06f, 8);
            }
            if (Projectile.localAI[0]++ < 3) {
                Projectile.timeLeft++;
                return;
            }
            if (Projectile.Distance(player.Center) > 8000) {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(128);
            }
            bool f = false;
            if (player.whoAmI == Main.myPlayer && CEKeybinds.CommandMinions.Current && Projectile.owner == Main.myPlayer) {
                f = true;
                if (ai != AIStyle.AroundOwner) {
                    ai = AIStyle.AroundOwner;
                    CEUtils.SyncProj(Projectile.whoAmI);
                }
            }
            NPC target = Projectile.FindMinionTarget();
            if (target != null) {
                if (!f && ai == AIStyle.AroundOwner) {
                    ai = AIStyle.Shooting;
                }
            }
            else {
                ai = AIStyle.AroundOwner;
            }
            Projectile.ai[1] = (ai == AIStyle.AroundOwner) ? 1 : 0;
            if (ai == AIStyle.AroundOwner) {
                if (Main.rand.NextBool(16) || vec == Vector2.Zero) {
                    vec = CEUtils.randomRot().ToRotationVector2() * 0.34f;
                }
                Projectile.velocity += vec;
                Projectile.velocity += (player.MountedCenter - Projectile.Center) * 0.0064f;
                Projectile.pushByOther(1);
                Projectile.spriteDirection = (Math.Sign(player.Center.X - Projectile.Center.X));
                Projectile.velocity *= 0.98f;
                if (player.statLife < player.statLifeMax2) {
                    for (int i = 0; i < 16; i++) {
                        var d = Dust.NewDustDirect(Projectile.Center + CEUtils.randomPointInCircle(22), 0, 0, DustID.GemEmerald);
                        d.noGravity = true;
                        d.velocity = (player.Center - Projectile.Center).normalize() * Main.rand.NextFloat(5, 9);
                    }
                }
            }
            else {
                if (target != null)
                    Projectile.spriteDirection = (Math.Sign(target.Center.X - Projectile.Center.X));
            }
            if (ai == AIStyle.Shooting) {
                Projectile.pushByOther(1);
                Projectile.localAI[1]++;
                if (Projectile.localAI[1] >= 0) {
                    if (Projectile.Distance(target.Center) > 500) {
                        Projectile.localAI[1] = 0;
                        Projectile.velocity *= 0.94f;
                        Projectile.velocity += (target.Center + vec.normalize() * 400 - Projectile.Center).normalize() * 1.6f;
                    }
                }
                else {
                    if (Main.rand.NextBool(16) || vec == Vector2.Zero) {
                        vec = CEUtils.randomRot().ToRotationVector2() * 0.34f;
                    }
                    Projectile.velocity *= 0.96f;
                    Projectile.velocity += vec * 1.2f;
                    Projectile.velocity += (player.MountedCenter + new Vector2(-256 * player.direction, -160) - Projectile.Center) * 0.004f;
                }
                if (Projectile.localAI[1] > 0) {
                    ai = AIStyle.Dashing;
                    CEUtils.PlaySound("SwordHit0", 1.4f, Projectile.Center, 4, 0.8f);
                    Projectile.velocity = (target.Center + (target.Center - Projectile.Center).normalize() * 460 - Projectile.Center) / 16f;
                }
                if (Projectile.localAI[2]++ % 16 == 4 && Main.myPlayer == Projectile.owner) {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, CEUtils.randomPointInCircle(28), ModContent.ProjectileType<LuminarisMinionAstralShoot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }

            }
            if (ai == AIStyle.Dashing) {
                if (Projectile.localAI[1]++ > 16) {
                    Projectile.localAI[1] = -180;
                    ai = AIStyle.Shooting;
                    Projectile.velocity *= 0.45f;
                }
            }
            if (trail == null || trail.Lifetime <= 0) {
                //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                trail = PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, Vector2.Zero, Color.White, 1.2f);
                trail.addPoint = false;
                trail.maxLength = 12;
                trail.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);
            }
            trail.AddPoint(Projectile.Center + Projectile.velocity);
            trail.Position = Projectile.Center + Projectile.velocity;
            trail.Lifetime = 12;

        }
        public PRT_StarTrailParticle trail = null;
        public void SetAI(AIStyle t) {
            if (_ai != t && Main.myPlayer == Projectile.owner) {
                _ai = t;
                CEUtils.SyncProj(Projectile.whoAmI);
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (CEUtils.LineThroughRect(Projectile.Center, Projectile.Center - Projectile.velocity, targetHitbox, 46))
                return true;
            return null;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            //冲撞攻击造成10倍的伤害
            modifiers.SourceDamage *= 10;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Rectangle frame = CEUtils.GetCutTexRect(tex, 9, ((int)Main.GameUpdateCount / 4) % 9, false);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, new Vector2(Projectile.spriteDirection > 0 ? 57 : (tex.Width - 57), 42), Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            return false;
        }
    }
}
