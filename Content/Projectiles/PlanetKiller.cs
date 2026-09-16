using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Weapons;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PlanetKiller : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.light = 0.5f;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
            Projectile.ArmorPenetration = 20;
        }
        public override bool? CanCutTiles() {
            return false;
        }

        void MoveToTarget(Vector2 targetPos, float MaxSpeed = 20f, float accSpeed = 0.5f) {
            Vector2 v = targetPos - Projectile.Center;
            v.Normalize();
            Projectile.velocity += v * accSpeed * 5;
            Projectile.velocity *= 0.95f;
            if (Projectile.ai[0] % 100 == 0) {
                Projectile.ai[1] = Main.rand.Next(160, 240);
            }
        }
        void AttackShooting(NPC target) {
            if (!(Projectile.owner == Main.myPlayer)) {
                return;
            }
            Player player = Main.player[Projectile.owner];
            Projectile.ai[0]++;
            if (Projectile.ai[0] % 52 == 0 || Projectile.ai[0] % 52 == 16) {

                int projID = ProjectileID.Bullet;
                float shootSpeed = FalseGun.shootSpeed;
                int damage = Projectile.originalDamage;
                float kb = FalseGun.knockBack;

                if (player.HasAmmo(FalseGun)) {
                    player.PickAmmo(FalseGun, out projID, out shootSpeed, out damage, out kb, out _, true);
                }
                Vector2 velocity = Projectile.SafeDirectionTo(target.Center) * shootSpeed;

                int p = Projectile.NewProjectile(player.GetSource_FromAI(), Projectile.Center, velocity.RotatedByRandom(MathHelper.ToRadians(2)), projID, damage, kb, Main.myPlayer);
                p.ToProj().usesLocalNPCImmunity = true;
                p.ToProj().localNPCHitCooldown = 10;
                if (Main.rand.NextBool(3)) {
                    p = Projectile.NewProjectile(player.GetSource_FromAI(), Projectile.Center, velocity.RotatedByRandom(MathHelper.ToRadians(2)), projID, damage, kb, Main.myPlayer);
                    p.ToProj().usesLocalNPCImmunity = true;
                    p.ToProj().localNPCHitCooldown = 10;
                }

                p.ToProj().DamageType = Projectile.DamageType;
            }
            if (Projectile.ai[0] % (int)(210 / (1 + Projectile.owner.ToPlayer().Entropy().WeaponBoost)) == 0) {
                Vector2 velocity = Projectile.SafeDirectionTo(target.Center) * 12;

                int p = Projectile.NewProjectile(player.GetSource_FromAI(), Projectile.Center, velocity, ModContent.ProjectileType<WohLaser>(), Projectile.originalDamage, 4, Main.myPlayer, ai2: 1);
                p.ToProj().scale = 0.6f;
                p.ToProj().DamageType = Projectile.DamageType;
                p.ToProj().netUpdate = true;
                p.ToProj().tileCollide = false;
                p.ToProj().usesLocalNPCImmunity = true;
                p.ToProj().localNPCHitCooldown = 16;
            }
        }

        public float counter = 0;
        public Item FalseGun = null;
        public Item Pk = null;
        private void DefineFalseGun(int baseDamage) {
            // 模板枪只取"消耗子弹"的弹药口径，其余属性全部被下方覆写，用原版乌兹等效
            int p90ID = ItemID.Uzi;
            int CVEID = ModContent.ItemType<PhantomPlanetKillerEngine>();
            FalseGun = new Item();
            Pk = new Item();
            FalseGun.SetDefaults(p90ID, true);
            Pk.SetDefaults(CVEID, true);
            FalseGun.damage = baseDamage;
            FalseGun.knockBack = Pk.knockBack;
            FalseGun.shootSpeed = Pk.shootSpeed * 1.25f;
            FalseGun.consumeAmmoOnFirstShotOnly = false;
            FalseGun.consumeAmmoOnLastShotOnly = false;
            FalseGun.ArmorPenetration = Projectile.ArmorPenetration;
            FalseGun.DamageType = DamageClass.Summon;
        }
        public override void AI() {
            foreach (Projectile p in Main.projectile) {
                if (p.type == Projectile.type && p.active && p.whoAmI != Projectile.whoAmI) {
                    if (p.owner == Projectile.owner && p.Hitbox.Intersects(Projectile.Hitbox)) {

                        for (int i = 0; i < 6; i++) {
                            if (p.Hitbox.Intersects(Projectile.Hitbox)) {
                                Projectile.Center += (Projectile.Center - p.Center) * 0.05f;
                            }
                        }
                        Projectile.velocity += (Projectile.Center - p.Center).SafeNormalize(Vector2.One) * 2f;
                    }
                }
            }
            if (FalseGun is null) {
                DefineFalseGun(Projectile.originalDamage);
            }
            if (Main.GameUpdateCount % 5 == 0) {
                Projectile.frame++;
                if (Projectile.frame >= 4) {
                    Projectile.frame = 0;
                }
            }
            counter += 0.18f;
            if (counter > 360) {
                counter -= 360;
            }
            Player player = Main.player[Projectile.owner];
            if (player.HasBuff(ModContent.BuffType<PlanetDestroyer>())) {
                Projectile.timeLeft = 3;
            }
            NPC target = null;
            if (player.HasMinionAttackTargetNPC) {
                target = Main.npc[player.MinionAttackTargetNPC];
                float betw = Vector2.Distance(target.Center, Projectile.Center);
                if (betw > 2000f) {
                    target = null;
                }

            }
            if (target == null || !target.active) {
                NPC t = Projectile.FindTargetWithinRange(2400, false);
                if (t != null) {
                    target = t;
                }
            }
            if (target != null) {
                if (target.active) {
                    if (Vector2.Distance(player.Center, target.Center) > 3000) {
                        Projectile.position = player.Center + new Vector2(0, -100);
                    }

                    Vector2 Position = target.Center + (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * Projectile.ai[1] * 1.4f;
                    if (CEUtils.getDistance(Projectile.Center, Projectile.owner.ToPlayer().Center) > 600) {
                        Position = target.Center + (Projectile.owner.ToPlayer().Center - target.Center).SafeNormalize(Vector2.Zero) * Projectile.ai[1] * 1.4f;
                    }
                    MoveToTarget(Position, 24, 0.3f);
                    AttackShooting(target);
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 8f.ToRadians(), true);
                }
            }
            else {
                Vector2 mypos = player.Center + new Vector2(0, -200);
                float dis = Projectile.Distance(mypos);
                if (dis > 2500) {
                    Projectile.position = player.Center + new Vector2(0, -100);
                }
                else if (dis > Projectile.ai[1]) {
                    MoveToTarget(mypos, 20, 0.4f);
                }
                else {
                    SimpleStandBy(mypos, 0.3f);
                }
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.3f, false);
            }
            if (Projectile.owner == Main.myPlayer) {
                Projectile.netUpdate = true;
            }


        }
        public void SimpleStandBy(Vector2 pos, float speed = 0.2f) {
            if (Projectile.localAI[1] < 3) {
                Projectile.position = pos;
                Projectile.localAI[1]++;
                return;
            }

        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
            Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tx.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}

