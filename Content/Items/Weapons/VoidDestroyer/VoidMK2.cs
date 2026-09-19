using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidDestroyer
{
    /// <summary>
    /// 虚空 MK2:虚空驱逐舰掉落的机枪。消耗任意子弹弹药,射出的一律是专用虚空弹头;
    /// 持续开火时每 <see cref="DroneInterval"/> 帧放出一架追踪无人机,命中后爆炸并施加 3 秒虚空之火
    /// </summary>
    public class VoidMK2 : ModItem
    {
        public const int DroneInterval = 60;
        public const float DroneDamageMult = 2.5f;
        /// <summary>开火累计帧数,只在拥有者本端累加;松手不清零,断续点射也按累计满一秒放机</summary>
        private int fireTimer;

        public override bool RangedPrefix() => true;

        public override void SetDefaults() {
            Item.width = 204;
            Item.height = 60;
            Item.scale = 0.75f;
            Item.damage = 105;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 6;
            Item.useAnimation = 6;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(platinum: 2, gold: 50);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 14f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 6;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-24, 0);

        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextBool(2);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 dir = velocity.SafeNormalize(Vector2.UnitX);
            Vector2 muzzle = position + dir * 110f;
            if (Collision.CanHit(position, 0, 0, muzzle, 0, 0)) {
                position = muzzle;
            }
            CEUtils.PlaySound("gunshot_small" + Main.rand.Next(1, 4), Main.rand.NextFloat(0.75f, 1.05f), position, 10, 0.45f);
            if (!Main.dedServ) {
                for (int i = 0; i < 3; i++) {
                    VDVfx.Spark(position, dir.RotatedByRandom(0.5f) * Main.rand.NextFloat(3f, 7f), VoidMK2Bullet.GlowColor, Main.rand.NextFloat(0.4f, 0.7f), 0.9f, 12, gravity: true);
                }
            }
            Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.035f), ModContent.ProjectileType<VoidMK2Bullet>(), damage, knockback, player.whoAmI);
            return false;
        }

        #region 无人机
        public override void HoldItem(Player player) {
            player.Entropy().MouseWorldListener = true;
            if (player.whoAmI != Main.myPlayer) {
                return;
            }
            if (player.ItemAnimationActive) {
                fireTimer++;
                if (fireTimer >= DroneInterval) {
                    fireTimer = 0;
                    LaunchDrone(player);
                }
            }
        }

        /// <summary>从枪身上方放出一架无人机,先向前上方漂一下再开始追踪</summary>
        private void LaunchDrone(Player player) {
            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 pos = player.MountedCenter + aim * 40f;
            int damage = (int)(player.GetWeaponDamage(Item) * DroneDamageMult);
            Vector2 vel = aim.RotatedBy(-0.45f * player.direction) * 9f;
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), pos, vel, ModContent.ProjectileType<VoidMK2Drone>(), damage, Item.knockBack * 2f, player.whoAmI);
            CEUtils.PlaySound("vbapear", Main.rand.NextFloat(1.1f, 1.3f), pos, 6, 0.55f);
        }
        #endregion

        #region 手持姿态
        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 64f;
            Vector2 itemSize = new Vector2(Item.width, Item.height) * Item.scale;
            Vector2 itemOrigin = new Vector2(-24, 0);

            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5) {
                rotation += (-0.04f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            }
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);

            float backArmRotation = rotation + 0.52f * player.direction;
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, backArmRotation);
        }
        #endregion
    }

    /// <summary>虚空 MK2 弹头:直飞,贴图朝右,尾部一道紫色速度线</summary>
    public class VoidMK2Bullet : ModProjectile
    {
        public static readonly Color GlowColor = new Color(215, 95, 255);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = -1;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.3f);
            if (!Main.dedServ && Main.rand.NextBool(8)) {
                VDVfx.Spark(Projectile.Center, -Projectile.velocity * 0.08f + CEUtils.randomPointInCircle(0.6f), GlowColor, Main.rand.NextFloat(0.3f, 0.55f), 0.8f, 12);
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 5; i++) {
                Vector2 v = -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.9f) * Main.rand.NextFloat(2f, 6f);
                VDVfx.Spark(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.4f, 0.7f), 1f, 16, gravity: true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            Main.spriteBatch.UseAdditive();
            Texture2D streak = CEUtils.getExtraTex("StreakSolid");
            float len = 46f;
            Vector2 streakScale = new Vector2(len / streak.Width, 6f / streak.Height);
            Main.spriteBatch.Draw(streak, drawPos - dir * (len * 0.5f + 4f), null, GlowColor * 0.55f, Projectile.rotation, streak.Size() / 2f, streakScale, SpriteEffects.None, 0f);
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float a = (1f - i / (float)Projectile.oldPos.Length) * 0.4f;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.spriteBatch.Draw(tex, pos, null, GlowColor * a, Projectile.oldRot[i], origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, GlowColor * 0.5f, 0f, glow.Size() / 2f, 0.07f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            Main.spriteBatch.Draw(tex, drawPos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>
    /// 虚空 MK2 追踪无人机:放出后先漂 12 帧,再锁定最近敌怪加速扑去;贴到敌怪、撞物块或超时都爆炸。
    /// 机体本身没有判定,伤害全由爆炸(<see cref="VoidMK2Explosion"/>)结算,并给范围内敌怪 3 秒虚空之火。贴图借用驱逐舰的孢子无人机
    /// </summary>
    public class VoidMK2Drone : ModProjectile
    {
        public const int ExplosionSize = 150;
        public static readonly Color GlowColor = new Color(210, 140, 255);

        public override string Texture => "CalamityEntropy/Content/Projectiles/VoidDestroyer/VDSporeDrone";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true;
            Projectile.aiStyle = -1;
        }

        public int Age => (int)Projectile.localAI[0];

        public override bool? CanDamage() => false;

        public override void AI() {
            Projectile.localAI[0]++;
            if (Age < 12) {
                Projectile.velocity *= 0.96f;
            }
            else {
                //HomeInOnNPC 锁定目标后会给 extraUpdates +1,实际每帧位移约为此值两倍
                CEUtils.HomeInOnNPC(Projectile, true, 1400f, 13f, 16f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.4f);

            //贴到敌怪就引爆(机体自身不判伤,免得直击 + 爆炸对同一目标算两次)
            if (Age >= 6) {
                Rectangle hitbox = Projectile.Hitbox;
                foreach (NPC npc in Main.ActiveNPCs) {
                    if (npc.CanBeChasedBy(Projectile) && hitbox.Intersects(npc.Hitbox)) {
                        Projectile.Kill();
                        return;
                    }
                }
            }

            if (Main.dedServ) {
                return;
            }
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            if (Age % 3 == 0) {
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center + back * 12f + CEUtils.randomPointInCircle(4f), back * Main.rand.NextFloat(0.5f, 1.5f), Color.White, Main.rand.NextFloat(0.6f, 0.9f));
                p.Opacity = 0.6f;
                p.ad = 0.05f;
            }
            if (Main.rand.NextBool(3)) {
                VDVfx.Spark(Projectile.Center + back * 10f, back * Main.rand.NextFloat(1f, 3f) + CEUtils.randomPointInCircle(1f), GlowColor, Main.rand.NextFloat(0.35f, 0.6f), 0.9f, 14);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnKill(int timeLeft) {
            if (Projectile.owner == Main.myPlayer) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<VoidMK2Explosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("VoidBomb", Main.rand.NextFloat(1.0f, 1.2f), Projectile.Center, 6, 0.5f);
            VDVfx.Explosion(Projectile.Center, 0.55f, 22);
            VDVfx.SparkBurst(Projectile.Center, VoidMK2Bullet.GlowColor, 16, 4f, 11f, 26, 0.5f, 1f);
            for (int i = 0; i < 8; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
                VDVfx.VoidPuff(Projectile.Center + v * 2f, v, 1.1f, 0.6f);
            }
            CEUtils.SetShake(Projectile.Center, 1.5f, 1200f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.UseAdditive();
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float a = (1f - i / (float)Projectile.oldPos.Length) * 0.3f;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.spriteBatch.Draw(tex, pos, null, GlowColor * a, Projectile.oldRot[i], origin, Projectile.scale * (1f - i * 0.03f), SpriteEffects.None, 0f);
            }
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, GlowColor * 0.6f, 0f, glow.Size() / 2f, 0.16f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            Main.spriteBatch.Draw(tex, drawPos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>无人机爆炸的范围判定(6 帧),命中施加 3 秒虚空之火;演出全在无人机 OnKill 里</summary>
    public class VoidMK2Explosion : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetDefaults() {
            Projectile.width = VoidMK2Drone.ExplosionSize;
            Projectile.height = VoidMK2Drone.ExplosionSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<VoidFire>(), 180);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
