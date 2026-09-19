using CalamityEntropy.Common;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidDestroyer
{
    /// <summary>
    /// 虚空电场:虚空驱逐舰掉落的法杖。长按左键持续从杖尖放出 5 道扇形摆动的旋转激光,
    /// 同时在杖尖积蓄一颗越来越大的闪电球(最多 3 秒);松开左键把闪电球掷出,命中后爆炸并给范围内敌怪带电。
    /// 蓄得越久闪电球伤害越高(<see cref="VoidElectricFieldHoldout.BallMinMult"/> → <see cref="VoidElectricFieldHoldout.BallMaxMult"/>)
    /// </summary>
    public class VoidElectricField : ModItem
    {
        public override void SetDefaults() {
            Item.width = 110;
            Item.height = 110;
            Item.damage = 160;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.knockBack = 5f;
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.value = Item.buyPrice(platinum: 2, gold: 50);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<VoidElectricFieldHoldout>();
            Item.shootSpeed = 1f;
        }

        public override bool MagicPrefix() => true;

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
    }

    /// <summary>
    /// 手持弹幕:速度只作朝向。ai[0] 为蓄力帧数(0..<see cref="MaxCharge"/>),随周期同步。
    /// 5 道激光的角度是 (帧数, 序号, 蓄力) 的确定函数,各端同算;激光判定由本弹幕承担,每目标每 <see cref="LaserHitCooldown"/> 帧一次。
    /// 松手或法力耗尽即释放闪电球(蓄力不足 <see cref="MinReleaseCharge"/> 帧则直接收杖)
    /// </summary>
    public class VoidElectricFieldHoldout : ModProjectile
    {
        public const int MaxCharge = 180;
        public const int MinReleaseCharge = 15;
        public const int LaserCount = 5;
        public const float LaserLength = 900f;
        public const int LaserHitCooldown = 12;
        public const int ManaInterval = 10;
        public const float TipOffset = 82f;
        public const float BallMinMult = 1.5f;
        public const float BallMaxMult = 8f;
        public static readonly Color BeamColor = new Color(170, 80, 255);
        public static readonly Color CoreColor = new Color(235, 215, 255);
        public static readonly Color ArcColor = new Color(200, 170, 255);

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/VoidDestroyer/VoidElectricField";

        private LoopSound chargeSnd;
        private bool manaOut;

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 5;
            Projectile.aiStyle = -1;
            Projectile.scale = 0.9f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = LaserHitCooldown;
        }

        public float Charge { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        public float ChargeRatio => MathHelper.Clamp(Charge / MaxCharge, 0f, 1f);
        public int Age => (int)Projectile.localAI[0];
        public Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        public Vector2 Tip => Projectile.Center + Dir * TipOffset * Projectile.scale;
        /// <summary>激光出现的前 10 帧从零展宽</summary>
        public float LaserEnvelope => MathHelper.Clamp(Age / 10f, 0f, 1f);

        /// <summary>第 i 道激光的方向:围绕瞄准轴按正弦摆动,五道相位错开即「旋转扇面」;蓄力越满扇面越收</summary>
        public Vector2 LaserDir(int i) {
            float spread = MathHelper.Lerp(0.55f, 0.2f, ChargeRatio);
            float phase = Age * 0.05f + i * MathHelper.TwoPi / LaserCount;
            return Dir.RotatedBy(MathF.Sin(phase) * spread);
        }

        public override void AI() {
            Player owner = Projectile.GetOwner();
            if (!owner.active || owner.dead || owner.CCed || owner.HeldItem.type != ModContent.ItemType<VoidElectricField>()) {
                Projectile.Kill();
                return;
            }
            Projectile.localAI[0]++;
            Projectile.timeLeft = 5;
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;

            if (Projectile.owner == Main.myPlayer) {
                Vector2 nv = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
                if (nv != Projectile.velocity) {
                    Projectile.netUpdate = true;
                }
                Projectile.velocity = nv;
                if (Age % 20 == 0) {
                    Projectile.netUpdate = true;
                }
            }
            Vector2 dir = Dir;
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter);
            Projectile.rotation = dir.ToRotation();
            Projectile.direction = dir.X >= 0f ? 1 : -1;
            owner.ChangeDir(Projectile.direction);
            owner.itemRotation = MathHelper.WrapAngle(Projectile.rotation + (Projectile.direction < 0 ? MathHelper.Pi : 0f));
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);

            //法力:拥有者按周期扣,扣不动就视作松手
            if (Projectile.owner == Main.myPlayer && Age % ManaInterval == ManaInterval - 1) {
                int cost = Math.Max(1, (int)(owner.GetManaCost(owner.HeldItem) * 0.4f));
                if (!owner.CheckMana(cost, true, false)) {
                    manaOut = true;
                }
            }
            bool channeling = owner.channel && !manaOut;
            if (!channeling) {
                Release(owner);
                return;
            }

            if (Age == 1 && !Main.dedServ) {
                CEUtils.PlaySound("lasercharge", 1.1f, Tip, 4, 0.5f);
            }
            Charge = Math.Min(Charge + 1f, MaxCharge);
            if ((int)Charge == MaxCharge && Age > 1 && !Main.dedServ && Projectile.localAI[1] == 0f) {
                //刚蓄满:提示音 + 一圈火花
                Projectile.localAI[1] = 1f;
                CEUtils.PlaySound("spark", 0.8f, Tip, 4, 0.8f);
                VDVfx.SparkBurst(Tip, CoreColor, 20, 3f, 9f, 24, 0.5f, 1f);
            }

            Lighting.AddLight(Tip, BeamColor.ToVector3() * (0.6f + 0.6f * ChargeRatio));
            UpdateSoundAndParticles(owner);
        }

        private void UpdateSoundAndParticles(Player owner) {
            if (Main.dedServ) {
                return;
            }
            if (chargeSnd == null) {
                chargeSnd = new LoopSound(CalamityEntropy.ofCharge);
                if (chargeSnd.instance != null) {
                    chargeSnd.instance.Pitch = 0f;
                    chargeSnd.instance.Volume = 0f;
                }
                chargeSnd.play();
            }
            chargeSnd.timeleft = 3;
            chargeSnd.setVolume_Dist(Projectile.Center, 100f, 700f, 0.25f + 0.3f * ChargeRatio);
            if (chargeSnd.instance != null) {
                chargeSnd.instance.Pitch = -0.2f + 0.7f * ChargeRatio;
            }

            Vector2 tip = Tip;
            //向杖尖汇聚的电线:蓄力越满越密
            if (Main.rand.NextFloat() < 0.35f + 0.5f * ChargeRatio) {
                Vector2 from = tip + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(50f, 110f);
                var line = PRTLoader.NewParticle<PRT_ULineParticle>(from, (tip - from) * 0.16f, Color.Lerp(ArcColor, Color.White, Main.rand.NextFloat(0.5f)), 0.35f).Configure(1f, true, PRTDrawModeEnum.AlphaBlend, 0f);
                line.spd = 0.06f;
                line.w1 = 0.8f;
                line.w2 = 0.85f;
                line.len = 4;
            }
            //球面电火花
            if (Main.rand.NextBool(3)) {
                float r = BallRadius;
                Vector2 on = tip + CEUtils.randomRot().ToRotationVector2() * r;
                VDVfx.Spark(on, (on - tip).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1f, 3f), ArcColor, Main.rand.NextFloat(0.3f, 0.6f), 0.9f, 12);
            }
            //激光根部的火花
            if (Main.rand.NextBool(2)) {
                int i = Main.rand.Next(LaserCount);
                VDVfx.Spark(tip + LaserDir(i) * Main.rand.NextFloat(10f, 40f), LaserDir(i) * Main.rand.NextFloat(4f, 9f), BeamColor, Main.rand.NextFloat(0.3f, 0.55f), 0.9f, 10);
            }
        }

        /// <summary>闪电球半径(像素),按蓄力从 8 长到 44</summary>
        public float BallRadius => MathHelper.Lerp(8f, 44f, ChargeRatio);

        private void Release(Player owner) {
            if (Charge >= MinReleaseCharge) {
                Vector2 tip = Tip;
                if (Projectile.owner == Main.myPlayer) {
                    float ratio = ChargeRatio;
                    int damage = (int)(Projectile.damage * MathHelper.Lerp(BallMinMult, BallMaxMult, ratio));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), tip, Dir * 11f, ModContent.ProjectileType<VoidElectricBall>(), damage, Projectile.knockBack * (1f + ratio), Projectile.owner, ratio);
                }
                if (!Main.dedServ) {
                    CEUtils.PlaySound("ThunderStrike", 0.9f + 0.3f * ChargeRatio, tip, 4, 0.5f + 0.4f * ChargeRatio);
                    CEUtils.SetShake(tip, 2f + 4f * ChargeRatio, 1200f);
                    VDVfx.SparkBurst(tip, CoreColor, 10 + (int)(14 * ChargeRatio), 3f, 9f, 20, 0.4f, 0.9f);
                }
                owner.velocity -= Dir * (1.5f + 3f * ChargeRatio);
            }
            Projectile.Kill();
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Age > 4 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 tip = Tip;
            int width = (int)(12f + 6f * ChargeRatio);
            for (int i = 0; i < LaserCount; i++) {
                if (CEUtils.LineThroughRect(tip, tip + LaserDir(i) * LaserLength, targetHitbox, width)) {
                    return true;
                }
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (!Main.dedServ) {
                VDVfx.Spark(target.Center, CEUtils.randomPointInCircle(3f), BeamColor, Main.rand.NextFloat(0.4f, 0.7f), 1f, 14, gravity: true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Vector2 tip = Tip;
            float envelope = LaserEnvelope;
            float ratio = ChargeRatio;

            //五道激光
            float width = 10f + 8f * ratio;
            for (int i = 0; i < LaserCount; i++) {
                VDBeamDraw.Draw(tip, LaserDir(i), LaserLength, width, BeamColor, CoreColor, envelope, 0.85f, i * 0.37f);
            }

            //杖尖闪电球:紫色实心球 + 白热核心 + 随机电弧
            float r = BallRadius;
            Texture2D circle = CEUtils.getExtraTex("a_circle");
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 tipScreen = tip - Main.screenPosition;
            float flicker = 0.9f + 0.1f * MathF.Sin(Age * 0.6f);
            Main.spriteBatch.UseAdditive();
            Main.spriteBatch.Draw(glow, tipScreen, null, BeamColor * (0.5f + 0.4f * ratio), 0f, glow.Size() / 2f, r * 5f / glow.Width * flicker, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(circle, tipScreen, null, BeamColor * 0.9f, 0f, circle.Size() / 2f, r * 2f / circle.Width * flicker, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(circle, tipScreen, null, CoreColor, 0f, circle.Size() / 2f, r * 1.1f / circle.Width * flicker, SpriteEffects.None, 0f);
            int arcs = 2 + (int)(4 * ratio);
            for (int k = 0; k < arcs; k++) {
                DrawArc(tip, r, Main.rand.NextFloat(MathHelper.TwoPi), r * Main.rand.NextFloat(0.8f, 1.8f));
            }
            CEUtils.ReSetToEndShader();

            //杖身:柄在手上,杖头指向瞄准方向
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 staffCenter = Projectile.Center + Dir * 36f * Projectile.scale - Main.screenPosition;
            Main.spriteBatch.Draw(tex, staffCenter, null, lightColor, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        /// <summary>从球面某点向外抖出一段锯齿电弧(纯绘制,逐帧随机即闪烁)</summary>
        public static void DrawArc(Vector2 center, float radius, float angle, float length) {
            Vector2 p = center + angle.ToRotationVector2() * radius;
            Vector2 dir = angle.ToRotationVector2();
            int segs = 4;
            for (int i = 0; i < segs; i++) {
                Vector2 next = p + dir.RotatedByRandom(0.9f) * (length / segs);
                CEUtils.drawLineBetter(p, next, ArcColor * 0.9f, 2.2f);
                CEUtils.drawLine(p, next, Color.White * 0.7f, 1f);
                p = next;
            }
        }
    }

    /// <summary>
    /// 闪电球:直飞,ai[0] 为蓄力比 0..1,半径与判定框按它缩放;贴到敌怪或撞墙即爆炸(<see cref="VoidElectricBurst"/>),
    /// 并放出若干链状闪电(<see cref="Lightning"/>)。球体自身不判伤,伤害全由爆炸结算,免得直击 + 爆炸对同一目标算两次;爆炸命中的敌怪带电
    /// </summary>
    public class VoidElectricBall : ModProjectile
    {
        public const int ElectrifyTime = 240;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 420;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = -1;
        }

        public float Ratio => MathHelper.Clamp(Projectile.ai[0], 0f, 1f);
        public float Radius => MathHelper.Lerp(10f, 46f, Ratio);

        public override bool? CanDamage() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                int size = (int)(Radius * 1.6f);
                Projectile.Resize(size, size);
            }
            Projectile.localAI[1]++;
            Projectile.rotation += 0.1f;
            Lighting.AddLight(Projectile.Center, VoidElectricFieldHoldout.BeamColor.ToVector3() * (0.6f + 0.6f * Ratio));

            //贴到敌怪即引爆
            Rectangle hitbox = Projectile.Hitbox;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.CanBeChasedBy(Projectile) && hitbox.Intersects(npc.Hitbox)) {
                    Projectile.Kill();
                    return;
                }
            }

            if (Main.dedServ) {
                return;
            }
            if (Main.rand.NextBool(2)) {
                Vector2 on = Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Radius;
                VDVfx.Spark(on, (on - Projectile.Center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1f, 3f) - Projectile.velocity * 0.1f, VoidElectricFieldHoldout.ArcColor, Main.rand.NextFloat(0.3f, 0.6f), 0.9f, 12);
            }
        }

        public override void OnKill(int timeLeft) {
            float ratio = Ratio;
            if (Projectile.owner == Main.myPlayer) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<VoidElectricBurst>(), Projectile.damage, Projectile.knockBack, Projectile.owner, ratio);
                int arcs = 3 + (int)(4 * ratio);
                for (int i = 0; i < arcs; i++) {
                    Vector2 v = new Vector2(30f, 0f).RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, v, ModContent.ProjectileType<Lightning>(), (int)(Projectile.damage * 0.3f), 3f, Projectile.owner);
                    if (p >= 0 && p < Main.maxProjectiles) {
                        Main.projectile[p].DamageType = DamageClass.Magic;
                    }
                }
            }
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("shockBlast", 0.9f + 0.2f * ratio, Projectile.Center, 4, 0.6f + 0.4f * ratio);
            CEUtils.SetShake(Projectile.Center, 3f + 5f * ratio, 1600f);
            VDVfx.Explosion(Projectile.Center, 0.5f + 0.7f * ratio, 24);
            VDVfx.SparkBurst(Projectile.Center, VoidElectricFieldHoldout.CoreColor, 14 + (int)(20 * ratio), 4f, 14f, 28, 0.5f, 1.1f);
            VDVfx.SparkBurst(Projectile.Center, VoidElectricFieldHoldout.BeamColor, 10 + (int)(14 * ratio), 2f, 8f, 24, 0.5f, 1f);
        }

        public override bool PreDraw(ref Color lightColor) {
            float r = Radius;
            Texture2D circle = CEUtils.getExtraTex("a_circle");
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float flicker = 0.9f + 0.1f * MathF.Sin(Projectile.localAI[1] * 0.5f);

            Main.spriteBatch.UseAdditive();
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float a = (1f - i / (float)Projectile.oldPos.Length);
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.spriteBatch.Draw(circle, pos, null, VoidElectricFieldHoldout.BeamColor * (0.35f * a), 0f, circle.Size() / 2f, r * 1.6f * a / circle.Width, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(glow, drawPos, null, VoidElectricFieldHoldout.BeamColor * 0.8f, 0f, glow.Size() / 2f, r * 5f / glow.Width * flicker, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(circle, drawPos, null, VoidElectricFieldHoldout.BeamColor, 0f, circle.Size() / 2f, r * 2f / circle.Width * flicker, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(circle, drawPos, null, VoidElectricFieldHoldout.CoreColor, 0f, circle.Size() / 2f, r * 1.1f / circle.Width * flicker, SpriteEffects.None, 0f);
            int arcs = 3 + (int)(4 * Ratio);
            for (int k = 0; k < arcs; k++) {
                VoidElectricFieldHoldout.DrawArc(Projectile.Center, r, Main.rand.NextFloat(MathHelper.TwoPi), r * Main.rand.NextFloat(0.8f, 1.8f));
            }
            CEUtils.ReSetToEndShader();
            return false;
        }
    }

    /// <summary>闪电球爆炸:方框边长随蓄力比 160 → 300,8 帧判定,命中带电;演出是一圈扩散的电环</summary>
    public class VoidElectricBurst : ModProjectile
    {
        public const int Lifetime = 8;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetDefaults() {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public float Ratio => MathHelper.Clamp(Projectile.ai[0], 0f, 1f);
        public int Size => (int)MathHelper.Lerp(160f, 300f, Ratio);

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.Resize(Size, Size);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Electrified, VoidElectricBall.ElectrifyTime);
        }

        public override bool PreDraw(ref Color lightColor) {
            float life = 1f - Projectile.timeLeft / (float)Lifetime;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.UseAdditive();
            float ringScale = MathHelper.Lerp(0.2f, 1f, VDVfx.EaseOut(life)) * Size / ring.Width * 1.5f;
            Main.spriteBatch.Draw(ring, drawPos, null, VoidElectricFieldHoldout.ArcColor * (1f - life), 0f, ring.Size() / 2f, ringScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, drawPos, null, Color.White * (0.6f * (1f - life)), 0f, ring.Size() / 2f, ringScale * 0.7f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, drawPos, null, VoidElectricFieldHoldout.BeamColor * (0.9f * (1f - life)), 0f, glow.Size() / 2f, Size / (float)glow.Width * 1.2f * (1f - life * 0.4f), SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
