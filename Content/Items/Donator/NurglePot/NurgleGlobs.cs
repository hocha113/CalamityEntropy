using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.NurglePot
{
    /// <summary>
    /// 纳垢锅粘液基类:飞行 → 沾在敌怪上按间隔结算 / 沾在物块上成为伤害经过者的粘液坑。
    /// 状态、附着目标与偏移经 SendExtraAI 同步,命中判定只在所有者端发生。
    /// </summary>
    public abstract class NurgleGlobBase : ModProjectile
    {
        protected enum GlobState : byte
        {
            Flying = 0,
            StuckOnNPC = 1,
            Puddle = 2,
        }

        protected abstract int StickDuration { get; }
        protected abstract int PuddleDuration { get; }
        protected abstract int StickHitCooldown { get; }
        protected abstract int PuddleHitCooldown { get; }
        protected abstract float Gravity { get; }
        //沾在敌怪上的每次结算 / 粘液坑的结算分别施加什么减益
        protected abstract void ApplyStuckDebuffs(NPC target);
        protected virtual void ApplyPuddleDebuffs(NPC target) { }
        //粘液到期(沾附或坑)时的收尾,所有者端调用
        protected virtual void OnExpire() { }

        protected GlobState state = GlobState.Flying;
        protected int stickIndex = -1;
        protected Vector2 stickOffset;
        protected bool puddleVertical;

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, true, -1);
            Projectile.width = Projectile.height = 14;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 300;
        }

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write((byte)state);
            writer.Write(stickIndex);
            writer.WriteVector2(stickOffset);
            writer.Write(puddleVertical);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            state = (GlobState)reader.ReadByte();
            stickIndex = reader.ReadInt32();
            stickOffset = reader.ReadVector2();
            puddleVertical = reader.ReadBoolean();
        }

        public override void AI() {
            switch (state) {
                case GlobState.Flying:
                    Projectile.velocity.Y += Gravity;
                    Projectile.velocity.X *= 0.995f;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    FlyingVisuals();
                    break;
                case GlobState.StuckOnNPC: {
                        NPC host = stickIndex >= 0 && stickIndex < Main.maxNPCs ? Main.npc[stickIndex] : null;
                        if (host == null || !host.active) {
                            Projectile.Kill();
                            return;
                        }
                        Projectile.Center = host.Center + stickOffset.RotatedBy(host.rotation);
                        Projectile.velocity = Vector2.Zero;
                        StuckVisuals();
                        break;
                    }
                case GlobState.Puddle:
                    Projectile.velocity = Vector2.Zero;
                    PuddleVisuals();
                    break;
            }
        }

        protected virtual void FlyingVisuals() {
            if (Main.rand.NextBool(3)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Poisoned, -Projectile.velocity * 0.1f, 120, default, 0.9f);
                d.noGravity = true;
            }
        }

        protected virtual void StuckVisuals() {
            if (Main.rand.NextBool(8)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + CEUtils.randomPointInCircle(6f), DustID.Poisoned, new Vector2(0, Main.rand.NextFloat(0.5f, 1.5f)), 120, default, 0.9f);
                d.noGravity = false;
            }
        }

        protected virtual void PuddleVisuals() {
            if (Main.rand.NextBool(12)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + CEUtils.randomPointInCircle(8f), DustID.Poisoned, new Vector2(0, -Main.rand.NextFloat(0.3f, 0.9f)), 150, default, 0.8f);
                d.noGravity = true;
            }
        }

        public override bool? CanHitNPC(NPC target) {
            if (state == GlobState.StuckOnNPC) {
                return target.whoAmI == stickIndex ? null : false;
            }
            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (state == GlobState.Flying) {
                state = GlobState.StuckOnNPC;
                stickIndex = target.whoAmI;
                //接触点向目标中心收一些,保证之后的周期判定始终落在碰撞箱内
                stickOffset = ((Projectile.Center - target.Center) * 0.6f).RotatedBy(-target.rotation);
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.timeLeft = StickDuration;
                Projectile.localNPCHitCooldown = StickHitCooldown;
                Projectile.netUpdate = true;
                SplatEffects(target.Center);
            }
            if (state == GlobState.Puddle) {
                ApplyPuddleDebuffs(target);
            }
            else {
                ApplyStuckDebuffs(target);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (state != GlobState.Flying) {
                return false;
            }
            state = GlobState.Puddle;
            puddleVertical = Projectile.velocity.X != oldVelocity.X;
            //此时 velocity 已是碰撞裁剪后允许的位移,先走完它让粘液贴面
            Projectile.position += Projectile.velocity;
            //贴着碰到的面摊开:落地/顶天摊成横条,撞墙摊成竖条,并保持接触边不动
            Rectangle old = Projectile.Hitbox;
            int w = puddleVertical ? 10 : 34;
            int h = puddleVertical ? 34 : 10;
            Projectile.width = w;
            Projectile.height = h;
            Projectile.Center = old.Center.ToVector2();
            if (!puddleVertical) {
                Projectile.position.Y = oldVelocity.Y > 0 ? old.Bottom - h : old.Top;
            }
            else {
                Projectile.position.X = oldVelocity.X > 0 ? old.Right - w : old.Left;
            }
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = PuddleDuration;
            Projectile.localNPCHitCooldown = PuddleHitCooldown;
            SplatEffects(Projectile.Center);
            if (Main.myPlayer == Projectile.owner) {
                Projectile.netUpdate = true;
            }
            return false;
        }

        protected virtual void SplatEffects(Vector2 pos) {
            CEUtils.PlaySound("poopland", Main.rand.NextFloat(0.9f, 1.2f), pos, 8, 0.5f);
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 8; i++) {
                Dust d = Dust.NewDustPerfect(pos, DustID.Poisoned, CEUtils.randomPointInCircle(3f) + new Vector2(0, -1.5f), 100, default, Main.rand.NextFloat(1f, 1.5f));
                d.noGravity = false;
            }
        }

        public override void OnKill(int timeLeft) {
            if (state != GlobState.Flying) {
                SplatEffects(Projectile.Center);
                if (Main.myPlayer == Projectile.owner) {
                    OnExpire();
                }
            }
        }

        /// <summary>粘液坑贴图:与本体同名加 Puddle 后缀,竖坑用横坑旋转 90 度。</summary>
        protected Texture2D PuddleTexture => CEUtils.RequestTex(Texture + "Puddle");

        public override bool PreDraw(ref Color lightColor) {
            //像素画本体走默认批次与环境光,不换混合模式;发光层用 A=0 的颜色在同一批次里做加法
            Vector2 pos = Projectile.Center - Main.screenPosition;
            if (state == GlobState.Puddle) {
                Texture2D puddle = PuddleTexture;
                float rot = puddleVertical ? MathHelper.PiOver2 : 0f;
                Main.EntitySpriteDraw(puddle, pos, null, lightColor, rot, puddle.Size() / 2f, Projectile.scale, SpriteEffects.None);
                return false;
            }
            Texture2D tex = Projectile.GetTexture();
            float wobble = 1f + 0.06f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + Projectile.whoAmI);
            Vector2 scale = new Vector2(wobble, 1f / wobble) * Projectile.scale;
            float rot2 = state == GlobState.Flying ? Projectile.rotation : 0f;
            Main.EntitySpriteDraw(tex, pos, null, lightColor, rot2, tex.Size() / 2f, scale, SpriteEffects.None);
            return false;
        }
    }

    /// <summary>侵蚀性粘液:沾敌怪 10 秒,每秒结算伤害并中毒;沾物块成 5 秒粘液坑,每 0.5 秒伤害经过者。</summary>
    public class NurgleSlimeGlob : NurgleGlobBase
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/NurglePot/NurgleSlimeGlob";
        protected override int StickDuration => 600;
        protected override int PuddleDuration => 300;
        protected override int StickHitCooldown => 60;
        protected override int PuddleHitCooldown => 30;
        protected override float Gravity => 0.32f;

        protected override void ApplyStuckDebuffs(NPC target) {
            target.AddBuff(BuffID.Poisoned, 120);
        }
    }

    /// <summary>
    /// 滚烫粘液:命中附加中毒与酸性中毒,附着 5 秒每秒结算伤害,到期汽化为瘟疫毒雾;撞物块成为同样会汽化的热坑。
    /// </summary>
    public class NurgleBoilingGlob : NurgleGlobBase
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/NurglePot/NurgleBoilingGlob";
        protected override int StickDuration => 300;
        protected override int PuddleDuration => 300;
        protected override int StickHitCooldown => 60;
        protected override int PuddleHitCooldown => 60;
        protected override float Gravity => 0.18f;

        protected override void ApplyStuckDebuffs(NPC target) {
            target.AddBuff(BuffID.Poisoned, 300);
            target.AddBuff(BuffID.Venom, 300);
        }

        protected override void ApplyPuddleDebuffs(NPC target) {
            ApplyStuckDebuffs(target);
        }

        protected override void FlyingVisuals() {
            Lighting.AddLight(Projectile.Center, 0.8f, 0.5f, 0.1f);
            if (Main.rand.NextBool(2)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.15f, 0, default, 1.1f);
                d.noGravity = true;
            }
            if (Main.rand.NextBool(4)) {
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, -Projectile.velocity * 0.1f + new Vector2(0, -0.5f), new Color(170, 200, 90), Main.rand.NextFloat(0.35f, 0.55f)).Configure(0.3f, 26, 0.02f);
            }
        }

        protected override void StuckVisuals() {
            Lighting.AddLight(Projectile.Center, 0.6f, 0.35f, 0.05f);
            if (Main.rand.NextBool(3)) {
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center + CEUtils.randomPointInCircle(5f), new Vector2(0, -Main.rand.NextFloat(0.6f, 1.4f)), new Color(170, 200, 90), Main.rand.NextFloat(0.4f, 0.6f)).Configure(0.3f, 30, 0.02f);
            }
        }

        protected override void PuddleVisuals() {
            StuckVisuals();
        }

        protected override void OnExpire() {
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(0, -0.4f), ModContent.ProjectileType<NurglePlagueMist>(), 0, 0f, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor) {
            //热光垫在本体下面:A=0 的颜色在默认批次里等价加法混合,不用切混合状态
            Texture2D glow = CEExtraAssets.Glow2;
            float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f + Projectile.whoAmI);
            Color heat = new Color(255, 140, 40) * (0.45f * pulse);
            heat.A = 0;
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, heat, 0f, glow.Size() / 2f, 0.16f, SpriteEffects.None);
            //本体自带热光,不吃环境光压暗
            Color lit = Color.Lerp(lightColor, Color.White, 0.6f);
            return base.PreDraw(ref lit);
        }
    }

    /// <summary>瘟疫毒雾:滚烫粘液汽化后的残留,不造成直接伤害,让经过的敌怪染上瘟疫。</summary>
    public class NurglePlagueMist : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const int Life = 240;
        public const int FadeFrames = 60;
        public const int PlagueDuration = 120;

        public override void SetDefaults() {
            Projectile.width = Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Life;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            Projectile.velocity *= 0.97f;
            Projectile.velocity.X += (Main.windSpeedCurrent * 1.2f - Projectile.velocity.X) * 0.01f;
            Projectile.Opacity = Math.Min(1f, Projectile.timeLeft / (float)FadeFrames);
            if (!Main.dedServ) {
                if (Projectile.timeLeft % 3 == 0) {
                    Vector2 pos = Projectile.Center + CEUtils.randomPointInCircle(Projectile.width * 0.4f);
                    PRTLoader.NewParticle<PRT_HeavySmokeCal>(pos, CEUtils.randomPointInCircle(0.5f) + new Vector2(0, -0.3f), new Color(70, 120, 30), Main.rand.NextFloat(1f, 1.5f) * Projectile.Opacity).Configure(0.38f * Projectile.Opacity, 44, 0.02f);
                }
                if (Projectile.timeLeft % 9 == 0) {
                    Vector2 pos = Projectile.Center + CEUtils.randomPointInCircle(Projectile.width * 0.3f);
                    PRTLoader.NewParticle<PRT_MediumMistCal>(pos, CEUtils.randomPointInCircle(0.8f), new Color(110, 170, 50), Main.rand.NextFloat(0.9f, 1.3f)).Configure(new Color(40, 70, 20), 190f * Projectile.Opacity, 0.01f);
                }
            }
            if (Main.myPlayer == Projectile.owner && Projectile.timeLeft % 10 == 0) {
                Rectangle box = Projectile.Hitbox;
                foreach (NPC npc in Main.ActiveNPCs) {
                    if (!npc.friendly && !npc.dontTakeDamage && npc.life > 0 && npc.Hitbox.Intersects(box)) {
                        npc.AddBuff(ModContent.BuffType<Plague>(), PlagueDuration);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    /// <summary>手持臭雾:随风飘散,不伤生物,只给沾到的敌怪、生物和玩家挂上臭味。</summary>
    public class NurgleStinkMist : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const int Life = 150;
        public const int StinkyDuration = 120;

        public override void SetDefaults() {
            Projectile.width = Projectile.height = 40;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Life;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            Projectile.velocity.X += (Main.windSpeedCurrent * 3f - Projectile.velocity.X) * 0.02f;
            Projectile.velocity.Y *= 0.99f;
            Projectile.Opacity = Math.Min(1f, Projectile.timeLeft / 40f);
            if (!Main.dedServ && Projectile.timeLeft % 4 == 0) {
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center + CEUtils.randomPointInCircle(8f), Projectile.velocity * 0.3f + CEUtils.randomPointInCircle(0.3f), new Color(150, 170, 60), Main.rand.NextFloat(0.55f, 0.85f) * Projectile.Opacity).Configure(0.2f * Projectile.Opacity, 36, 0.015f);
            }
            if (Projectile.timeLeft % 10 != 0) {
                return;
            }
            Rectangle box = Projectile.Hitbox;
            if (Main.myPlayer == Projectile.owner) {
                foreach (NPC npc in Main.ActiveNPCs) {
                    if (npc.life > 0 && npc.Hitbox.Intersects(box)) {
                        npc.AddBuff(BuffID.Stinky, StinkyDuration);
                    }
                }
            }
            if (!Main.dedServ && Main.LocalPlayer.active && !Main.LocalPlayer.dead && Main.LocalPlayer.Hitbox.Intersects(box)) {
                Main.LocalPlayer.AddBuff(BuffID.Stinky, StinkyDuration);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
