using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
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
    /// 物质解离矛:虚空驱逐舰掉落的长矛。
    /// 左键戳击,命中后引发暗影爆炸(<see cref="BurstMult"/>)并在目标周围生成 3 发慢速追踪虚空弹(<see cref="BoltMult"/>);
    /// 右键投掷,飞行途中每隔 <see cref="MatterDissociationSpearThrow.BoltSpacing"/> 像素留下一发虚空弹(最多 7 发),
    /// 命中目标时从目标下方立刻生成 4 发瞄准目标的极速投影三叉戟(<see cref="TridentMult"/>)。
    /// 虚空弹生成 30 帧后才开始追踪
    /// </summary>
    public class MatterDissociationSpear : ModItem
    {
        public const float BoltMult = 0.10f;
        public const float BurstMult = 0.75f;
        public const float TridentMult = 0.25f;
        public const int StabUseTime = 22;
        public const int ThrowUseTime = 30;

        public override void SetDefaults() {
            Item.width = 70;
            Item.height = 68;
            Item.damage = 500;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = StabUseTime;
            Item.useAnimation = StabUseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 6f;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(platinum: 2, gold: 50);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<MatterDissociationSpearStab>();
            Item.shootSpeed = 1f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2) {
                Item.useTime = ThrowUseTime;
                Item.useAnimation = ThrowUseTime;
                Item.shoot = ModContent.ProjectileType<MatterDissociationSpearThrow>();
                Item.shootSpeed = 26f;
                Item.noUseGraphic = true;
                //同时只允许一支掷出的矛在场
                return player.ownedProjectileCounts[Item.shoot] == 0;
            }
            Item.useTime = StabUseTime;
            Item.useAnimation = StabUseTime;
            Item.shoot = ModContent.ProjectileType<MatterDissociationSpearStab>();
            Item.shootSpeed = 1f;
            return player.ownedProjectileCounts[Item.shoot] == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (player.altFunctionUse == 2) {
                CEUtils.PlaySound("throw", Main.rand.NextFloat(0.9f, 1.1f), position, 6, 0.8f);
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                return false;
            }
            CEUtils.PlaySound("sf_spear_attak", Main.rand.NextFloat(0.9f, 1.1f), position, 6, 0.6f);
            Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI);
            return false;
        }
    }

    /// <summary>
    /// 戳击手持弹幕:速度只作方向,贴图中心沿方向从手边推出再收回(正弦包络),判定为手到矛尖的一条线。
    /// 每次戳击只触发一次暗影爆炸与虚空弹,不因扎穿多个目标叠爆
    /// </summary>
    public class MatterDissociationSpearStab : ModProjectile
    {
        /// <summary>贴图对角线半长(140×134 的对角),矛尖 = 中心 + 方向 × 半长 × 缩放</summary>
        public const float HalfLength = 97f;
        public const float MinOffset = 46f;
        public const float MaxOffset = 138f;

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/VoidDestroyer/MatterDissociationSpear";

        private bool triggered;

        public override void SetDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.timeLeft = 90;
            Projectile.aiStyle = -1;
            Projectile.scale = 0.9f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        private Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        private Vector2 HandPos(Player owner) => owner.RotatedRelativePoint(owner.MountedCenter);
        private Vector2 TipPos => Projectile.Center + Dir * HalfLength * Projectile.scale;

        /// <summary>0 → 1 → 0 的推刺包络</summary>
        private float Reach(Player owner) {
            if (owner.itemAnimationMax <= 0) {
                return 0f;
            }
            float progress = 1f - owner.itemAnimation / (float)owner.itemAnimationMax;
            return MathF.Sin(MathHelper.Clamp(progress, 0f, 1f) * MathHelper.Pi);
        }

        public override void AI() {
            Player owner = Projectile.GetOwner();
            if (!owner.active || owner.dead || owner.itemAnimation <= 0 || owner.HeldItem.type != ModContent.ItemType<MatterDissociationSpear>()) {
                Projectile.Kill();
                return;
            }
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = owner.itemAnimation;
            Projectile.timeLeft = 2;

            float reach = Reach(owner);
            Vector2 dir = Dir;
            Projectile.direction = Projectile.spriteDirection = dir.X >= 0f ? 1 : -1;
            owner.ChangeDir(Projectile.direction);
            Projectile.Center = HandPos(owner) + dir * MathHelper.Lerp(MinOffset, MaxOffset, reach) * Projectile.scale;
            Projectile.rotation = dir.ToRotation() + MathHelper.PiOver4;
            owner.itemRotation = MathHelper.WrapAngle(dir.ToRotation() + (Projectile.direction < 0 ? MathHelper.Pi : 0f));
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, dir.ToRotation() - MathHelper.PiOver2);

            Lighting.AddLight(TipPos, VDVfx.VoidPurple.ToVector3() * 0.5f);
            if (!Main.dedServ && reach > 0.3f && Main.rand.NextBool(2)) {
                VDVfx.Spark(TipPos + CEUtils.randomPointInCircle(6f), dir * Main.rand.NextFloat(2f, 6f) + CEUtils.randomPointInCircle(1f), VDVfx.VoidPurple, Main.rand.NextFloat(0.35f, 0.65f), 0.9f, 12);
            }
        }

        public override bool? CanDamage() => Reach(Projectile.GetOwner()) > 0.25f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Player owner = Projectile.GetOwner();
            return CEUtils.LineThroughRect(HandPos(owner), TipPos, targetHitbox, 26);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(0.9f, 1.1f), target.Center, 6, 0.7f);
            if (triggered) {
                return;
            }
            triggered = true;
            if (Projectile.owner != Main.myPlayer) {
                return;
            }
            IEntitySource src = Projectile.GetSource_FromThis();
            int burstDamage = (int)(Projectile.damage * MatterDissociationSpear.BurstMult);
            Projectile.NewProjectile(src, target.Center, Vector2.Zero, ModContent.ProjectileType<MatterDissociationBurst>(), burstDamage, Projectile.knockBack, Projectile.owner);
            int boltDamage = Math.Max(1, (int)(Projectile.damage * MatterDissociationSpear.BoltMult));
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 3; i++) {
                Vector2 v = (baseAngle + i * MathHelper.TwoPi / 3f).ToRotationVector2() * Main.rand.NextFloat(4f, 6f);
                Projectile.NewProjectile(src, target.Center + v * 4f, v, ModContent.ProjectileType<MatterDissociationBolt>(), boltDamage, Projectile.knockBack * 0.3f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float reach = Reach(Projectile.GetOwner());

            if (reach > 0.2f) {
                Main.spriteBatch.UseAdditive();
                Vector2 dir = Dir;
                for (int i = 1; i <= 3; i++) {
                    Main.spriteBatch.Draw(tex, drawPos - dir * (i * 9f * reach), null, VDVfx.VoidPurple * (0.22f * reach * (1f - i * 0.25f)), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
                }
                Texture2D glow = CEUtils.getExtraTex("Glow");
                Main.spriteBatch.Draw(glow, TipPos - Main.screenPosition, null, VDVfx.VoidPink * (0.55f * reach), 0f, glow.Size() / 2f, 0.14f * reach, SpriteEffects.None, 0f);
                CEUtils.ReSetToEndShader();
            }
            Main.spriteBatch.Draw(tex, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>
    /// 掷出的矛:直飞,每飞过 <see cref="BoltSpacing"/> 像素留下一发原地起步的虚空弹(最多 <see cref="MaxBolts"/> 发);
    /// 命中目标时从目标脚下 4 个点各射出一发极速投影三叉戟,矛本身随即消散
    /// </summary>
    public class MatterDissociationSpearThrow : ModProjectile
    {
        public const float BoltSpacing = 44f;
        public const int MaxBolts = 7;

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/VoidDestroyer/MatterDissociationSpear";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 150;
            Projectile.aiStyle = -1;
            Projectile.scale = 0.9f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        /// <summary>自上一发虚空弹起累计飞行距离</summary>
        private float Travelled { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }
        private int BoltsLeft { get => (int)Projectile.localAI[1]; set => Projectile.localAI[1] = value; }
        private Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, VDVfx.VoidPurple.ToVector3() * 0.5f);

            Travelled += Projectile.velocity.Length();
            if (Travelled >= BoltSpacing && BoltsLeft < MaxBolts) {
                Travelled -= BoltSpacing;
                BoltsLeft++;
                if (Projectile.owner == Main.myPlayer) {
                    int boltDamage = Math.Max(1, (int)(Projectile.damage * MatterDissociationSpear.BoltMult));
                    //留在路径上的弹只带一点侧向漂移,30 帧后开始追踪
                    Vector2 drift = Dir.RotatedBy(MathHelper.PiOver2) * (BoltsLeft % 2 == 0 ? 1f : -1f) * 1.2f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, drift, ModContent.ProjectileType<MatterDissociationBolt>(), boltDamage, Projectile.knockBack * 0.3f, Projectile.owner);
                }
                if (!Main.dedServ) {
                    VDVfx.SparkBurst(Projectile.Center, VDVfx.VoidPurple, 5, 1.5f, 4f, 16, 0.4f, 0.8f);
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(2)) {
                Vector2 tip = Projectile.Center + Dir * 60f * Projectile.scale;
                VDVfx.Spark(tip, -Projectile.velocity * 0.1f + CEUtils.randomPointInCircle(1f), VDVfx.VoidPink, Main.rand.NextFloat(0.35f, 0.6f), 0.9f, 12);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(0.8f, 1f), target.Center, 6, 0.8f);
            if (!Main.dedServ) {
                VDVfx.HoloBurst(target.Bottom + Vector2.UnitY * 40f, VDVfx.VoidPink);
            }
            if (Projectile.owner != Main.myPlayer) {
                return;
            }
            int tridentDamage = Math.Max(1, (int)(Projectile.damage * MatterDissociationSpear.TridentMult));
            for (int i = 0; i < 4; i++) {
                Vector2 spawn = new Vector2(target.Center.X + (i - 1.5f) * 36f, target.Bottom.Y + 64f + Math.Abs(i - 1.5f) * 12f);
                Vector2 vel = (target.Center - spawn).SafeNormalize(-Vector2.UnitY) * 38f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel, ModContent.ProjectileType<MatterDissociationTrident>(), tridentDamage, Projectile.knockBack * 0.5f, Projectile.owner);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            CEUtils.PlaySound("metalhit", Main.rand.NextFloat(0.9f, 1.1f), Projectile.Center, 6, 0.5f);
            return true;
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            VDVfx.SparkBurst(Projectile.Center + Dir * 40f, VDVfx.VoidPurple, 12, 2f, 7f, 20, 0.5f, 1f);
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
                float a = (1f - i / (float)Projectile.oldPos.Length) * 0.35f;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.spriteBatch.Draw(tex, pos, null, VDVfx.VoidPurple * a, Projectile.oldRot[i], origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos + Dir * 60f * Projectile.scale, null, VDVfx.VoidPink * 0.5f, 0f, glow.Size() / 2f, 0.12f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            Main.spriteBatch.Draw(tex, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>
    /// 友方虚空弹(贴图借驱逐舰的紫水晶弹):生成后前 30 帧只按初速漂移并减速,之后以低速追踪最近敌怪。
    /// 戳击版从目标周围散出,投掷版原地留在矛的路径上
    /// </summary>
    public class MatterDissociationBolt : ModProjectile
    {
        public const int HomingDelay = 30;
        /// <summary>HomeInOnNPC 锁定后会给 extraUpdates +1,实际每帧位移约为此值两倍,所以这里给得很低</summary>
        public const float HomingSpeed = 5.5f;
        public static readonly Color GlowColor = new Color(190, 60, 255);

        public override string Texture => "CalamityEntropy/Content/Projectiles/VoidDestroyer/VDVoidBolt";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 420;
            Projectile.aiStyle = -1;
            Projectile.scale = 0.8f;
        }

        public int Age => (int)Projectile.localAI[0];

        public override void AI() {
            Projectile.localAI[0]++;
            if (Age < HomingDelay) {
                Projectile.velocity *= 0.94f;
                //待机期原地慢转,起追后再按速度定向
                Projectile.rotation += 0.12f;
            }
            else {
                CEUtils.HomeInOnNPC(Projectile, true, 1500f, HomingSpeed, 28f);
                if (Projectile.velocity.LengthSquared() > 0.25f) {
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                }
                else {
                    Projectile.rotation += 0.06f;
                }
            }
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.4f);

            if (Main.dedServ) {
                return;
            }
            if (Age % 4 == 0) {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.Zero);
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center + back * 8f + CEUtils.randomPointInCircle(4f), back * Main.rand.NextFloat(0.3f, 1.2f), Color.White, Main.rand.NextFloat(0.5f, 0.8f));
                p.Opacity = 0.6f;
                p.ad = 0.05f;
            }
            if (Age % 6 == 0) {
                VDVfx.Spark(Projectile.Center, -Projectile.velocity * 0.15f + CEUtils.randomPointInCircle(1.2f), GlowColor, Main.rand.NextFloat(0.3f, 0.6f), 0.9f, 16);
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("vb_hit", Main.rand.NextFloat(1.1f, 1.4f), Projectile.Center, 8, 0.35f);
            for (int i = 0; i < 6; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
                VDVfx.Spark(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.4f, 0.7f), 1f, 18, gravity: true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float pulse = 1f + 0.08f * MathF.Sin(Age * 0.2f);

            Main.spriteBatch.UseAdditive();
            float speed = Projectile.velocity.Length();
            if (speed > 5f) {
                Texture2D streak = CEUtils.getExtraTex("StreakSolid");
                Vector2 dir = Projectile.velocity / speed;
                float len = MathHelper.Clamp(speed * 3f, 20f, 48f);
                Vector2 streakScale = new Vector2(len / streak.Width, 6f / streak.Height);
                Main.spriteBatch.Draw(streak, drawPos - dir * (len * 0.5f + 5f), null, GlowColor * 0.4f, dir.ToRotation(), streak.Size() / 2f, streakScale, SpriteEffects.None, 0f);
            }
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float a = (1f - i / (float)Projectile.oldPos.Length) * 0.3f;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.spriteBatch.Draw(tex, pos, null, GlowColor * a, Projectile.oldRot[i], origin, Projectile.scale * (1f - i * 0.04f), SpriteEffects.None, 0f);
            }
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, GlowColor * 0.55f, 0f, glow.Size() / 2f, 0.13f * pulse, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            Main.spriteBatch.Draw(tex, drawPos, null, Color.White, Projectile.rotation, origin, Projectile.scale * pulse, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>暗影爆炸:170 像素方框、8 帧判定,演出是一团向外扩的虚空烟与一圈收缩的暗环</summary>
    public class MatterDissociationBurst : ModProjectile
    {
        public const int Lifetime = 8;
        public const int Size = 170;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (Main.dedServ) {
                    return;
                }
                CEUtils.PlaySound("VoidBomb", Main.rand.NextFloat(0.75f, 0.9f), Projectile.Center, 6, 0.7f);
                CEUtils.SetShake(Projectile.Center, 2f, 1400f);
                for (int i = 0; i < 16; i++) {
                    Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 9f);
                    VDVfx.VoidPuff(Projectile.Center + v * 2f, v, Main.rand.NextFloat(1.2f, 1.8f), 0.75f);
                }
                VDVfx.SparkBurst(Projectile.Center, VDVfx.VoidDeep, 18, 4f, 12f, 28, 0.6f, 1.2f);
                VDVfx.SparkBurst(Projectile.Center, VDVfx.VoidPink, 8, 2f, 6f, 20, 0.4f, 0.8f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            float life = 1f - Projectile.timeLeft / (float)Lifetime;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.UseAdditive();
            float ringScale = MathHelper.Lerp(0.3f, 1.1f, VDVfx.EaseOut(life)) * Size / ring.Width * 1.4f;
            Main.spriteBatch.Draw(ring, drawPos, null, VDVfx.VoidDeep * (1f - life), 0f, ring.Size() / 2f, ringScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, drawPos, null, VDVfx.VoidPurple * (0.8f * (1f - life)), 0f, glow.Size() / 2f, 0.7f * (1f - life * 0.5f), SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }

    /// <summary>投影三叉戟(近战):从目标脚下射出的极速全息三叉戟,借原版邪恶三叉戟贴图经全息着色器画成品红投影</summary>
    public class MatterDissociationTrident : ModProjectile
    {
        public static readonly Color HoloColor = new Color(255, 120, 255);

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 70;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = -1;
        }

        public override void AI() {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, HoloColor.ToVector3() * 0.4f);
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                VDVfx.Spark(Projectile.Center, -Projectile.velocity * 0.05f + CEUtils.randomPointInCircle(1f), HoloColor, Main.rand.NextFloat(0.3f, 0.6f), 0.9f, 12);
            }
        }

        public override void OnKill(int timeLeft) {
            if (!Main.dedServ) {
                VDVfx.HoloBurst(Projectile.Center, HoloColor);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            HoloTridentDraw.Draw(Projectile, HoloColor, MathHelper.Clamp(Projectile.localAI[0] / 6f, 0f, 1f) * 0.9f);
            return false;
        }
    }

    /// <summary>全息三叉戟绘制:近战版与召唤版共用,原版邪恶三叉戟贴图 + 残影经 <see cref="VDHologramDraw"/> 着色</summary>
    public static class HoloTridentDraw
    {
        public static void Draw(Projectile projectile, Color color, float opacity, float scale = 1.1f) {
            if (opacity <= 0.01f) {
                return;
            }
            Main.instance.LoadProjectile(ProjectileID.UnholyTridentHostile);
            Texture2D tex = TextureAssets.Projectile[ProjectileID.UnholyTridentHostile].Value;
            Vector2 origin = tex.Size() / 2f;
            VDHologramDraw.Begin();
            for (int i = projectile.oldPos.Length - 1; i >= 1; i--) {
                Vector2 old = projectile.oldPos[i];
                if (old == Vector2.Zero) {
                    continue;
                }
                float a = (1f - i / (float)projectile.oldPos.Length) * 0.35f;
                Vector2 pos = old + projectile.Size / 2f - Main.screenPosition;
                VDHologramDraw.DrawPart(tex, pos, null, color, opacity * a, projectile.rotation, origin, scale, SpriteEffects.None);
            }
            VDHologramDraw.DrawPart(tex, projectile.Center - Main.screenPosition, null, color, opacity, projectile.rotation, origin, scale, SpriteEffects.None);
            VDHologramDraw.End();
        }
    }
}
