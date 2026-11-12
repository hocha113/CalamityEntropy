using CalamityEntropy.Content.Items.Weapons.VoidDestroyer;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PrayerlightLance
{
    /// <summary>
    /// 手持长矛,每次使用一支。ai[0] 模式(0 刺击,1 洗礼);ai[1] 出手时的光之能量进度,其他端的发光层用;
    /// ai[2] 命中标记(序号 × 256 + NPC 下标),拥有者命中时写入并同步,其他端据此补放命中火花
    /// </summary>
    public class PrayerlightLanceHeld : ModProjectile
    {
        public const float ModeStab = 0f;
        public const float ModeBaptism = 1f;

        //刺击时间轴,占总时长的比例;伸缩量单位像素
        public const float GatherEnd = 0.2f;
        public const float BurstEnd = 0.34f;
        public const float RestEnd = 0.6f;
        public const float PullBack = 12f;
        public const float Overshoot = 46f;
        public const float Hold = 38f;
        public const int HitLineWidth = 26;

        //洗礼时间轴,帧:举矛、朝天颤抖(时长同洗礼)、转一圈回到朝鼠标
        public const int RaiseFrames = 16;
        public const int SpinFrames = 26;
        //颤抖幅度,弧度 / 像素
        public const float TrembleAngle = 0.035f;
        public const float TrembleShift = 1.6f;
        //从矛尖射向天空的那道光:长度像素,升起与淡出帧数
        public const float SkyShotLength = 1100f;
        public const int SkyShotRiseFrames = 6;
        public const int SkyShotFrames = 24;

        public const int HitMarkerStride = 256;
        private const float Up = -MathHelper.PiOver2;

        public override string Texture => PrayerlightLanceGeometry.TexturePath;

        [VaultLoaden(PrayerlightLanceGeometry.TexturePath + "_Glow")]
        internal static Asset<Texture2D> GlowMask;

        private int total;
        private int timeline;
        private float extension;
        private float prevExtension;
        private int hitSerial;
        private int seenMarker;

        private int baptismAge;
        private float startAim;
        private float aim;
        private int spinSign;
        private Vector2 tremble;
        private int skyShotAge = -1;

        private bool Baptism => Projectile.ai[0] == ModeBaptism;
        private Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        private float Progress => total > 0 ? MathHelper.Clamp(timeline / (float)total, 0f, 1f) : 0f;
        private bool Live => !Baptism && Progress >= GatherEnd && Progress < RestEnd;
        private float AimRotation => Baptism ? aim : Dir.ToRotation();
        private float Reach => Baptism ? 0f : extension;
        private int HoldEnd => RaiseFrames + PrayerlightLance.BaptismFrames;

        public override void SetDefaults() {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.timeLeft = 120;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = PrayerlightLance.StabHitCooldown;
        }

        public override bool ShouldUpdatePosition() => false;

        public static float ExtensionAt(float p) {
            if (p < GatherEnd) {
                float k = p / GatherEnd;
                return -PullBack * k * k * (3f - 2f * k);
            }
            if (p < BurstEnd) {
                float k = (p - GatherEnd) / (BurstEnd - GatherEnd);
                return MathHelper.Lerp(-PullBack, Overshoot, 1f - MathF.Pow(1f - k, 4f));
            }
            if (p < RestEnd) {
                float k = (p - BurstEnd) / (RestEnd - BurstEnd);
                return Hold + (Overshoot - Hold) * (1f - k) * (1f - k);
            }
            float r = (p - RestEnd) / (1f - RestEnd);
            return Hold * (1f - r * r * (3f - 2f * r));
        }

        private Player.CompositeArmStretchAmount Stretch {
            get {
                if (Baptism) {
                    return Player.CompositeArmStretchAmount.Full;
                }
                float p = Progress;
                if (p < GatherEnd) {
                    return Player.CompositeArmStretchAmount.Quarter;
                }
                return p < RestEnd ? Player.CompositeArmStretchAmount.Full : Player.CompositeArmStretchAmount.ThreeQuarters;
            }
        }

        private Vector2 HandPos(Player owner) {
            return owner.GetFrontHandPosition(Stretch, AimRotation - MathHelper.PiOver2) + new Vector2(0f, owner.gfxOffY);
        }

        private Vector2 TipPos(Player owner) => HandPos(owner) + tremble + AimRotation.ToRotationVector2() * (Reach + PrayerlightLanceGeometry.TipFromPivot);

        private HeldSprite Sprite(Player owner, float reach) {
            float rot = AimRotation;
            Vector2 world = HandPos(owner) + tremble + rot.ToRotationVector2() * reach;
            return HeldSprite.Along(TextureAssets.Projectile[Type].Value, PrayerlightLanceGeometry.Pivot, PrayerlightLanceGeometry.AxisAngle, world, rot, Projectile.direction);
        }

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.CCed || owner.HeldItem.type != ModContent.ItemType<PrayerlightLance>()) {
                Projectile.Kill();
                return;
            }
            owner.heldProj = Projectile.whoAmI;
            Projectile.timeLeft = 2;
            ReadHitMarker(owner);
            if (Baptism) {
                BaptismAI(owner);
            }
            else {
                StabAI(owner);
            }
        }

        private void StabAI(Player owner) {
            if (total == 0) {
                //各端用同一公式算时长,远端玩家的 itemAnimationMax 不一定可靠
                total = Math.Max(8, CombinedHooks.TotalAnimationTime(PrayerlightLance.UseTime, owner, owner.HeldItem));
                Projectile.direction = Dir.X >= 0f ? 1 : -1;
            }
            owner.ChangeDir(Projectile.direction);
            float before = Progress;
            timeline++;
            if (timeline >= total) {
                Projectile.Kill();
                return;
            }
            owner.itemTime = owner.itemAnimation = Math.Max(2, total - timeline + 1);

            prevExtension = extension;
            extension = ExtensionAt(Progress);
            if (before < GatherEnd && Progress >= GatherEnd) {
                OnBurst(owner);
            }
            Pose(owner);
            Vector2 tip = TipPos(owner);
            Projectile.Center = Vector2.Lerp(HandPos(owner), tip, 0.5f);
            Lighting.AddLight(tip, SunVfx.Gold.ToVector3() * (Live ? 0.6f : 0.3f));
        }

        /// <summary>爆发帧:矛尖处射出光枪,出手音与尖端火花</summary>
        private void OnBurst(Player owner) {
            Vector2 tip = HandPos(owner) + Dir * (Overshoot + PrayerlightLanceGeometry.TipFromPivot);
            if (Projectile.owner == Main.myPlayer) {
                int damage = Math.Max(1, (int)(Projectile.damage * PrayerlightLance.SpearDamageMult));
                Vector2 velocity = Dir * (PrayerlightLance.SpearSpeed / (PrayerlightLanceSpear.ExtraUpdates + 1));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), tip, velocity, ModContent.ProjectileType<PrayerlightLanceSpear>(), damage, Projectile.knockBack * PrayerlightLance.SpearKnockbackMult, Projectile.owner, Projectile.damage);
            }
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("sf_spear_attak", Main.rand.NextFloat(0.95f, 1.15f), tip, 3, 0.5f * CEUtils.WeapSound);
            CEUtils.PlaySound("light_bolt", Main.rand.NextFloat(1.2f, 1.4f), tip, 3, 0.25f);
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(tip, Dir * 2f, SunVfx.Gold, 0.1f).Configure(new Vector2(0.45f, 1f), Dir.ToRotation(), 0.55f, 12);
            for (int i = 0; i < 5; i++) {
                Color c = i % 2 == 0 ? SunVfx.Core : SunVfx.Gold;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(tip, Dir.RotatedByRandom(0.2f) * Main.rand.NextFloat(10f, 20f), c, Main.rand.NextFloat(0.035f, 0.05f)).Configure(false, 9, new Vector2(2.4f, 0.8f), true);
            }
        }

        private void BaptismAI(Player owner) {
            if (baptismAge == 0) {
                startAim = Dir.ToRotation();
                aim = startAim;
                Projectile.direction = Dir.X >= 0f ? 1 : -1;
            }
            baptismAge++;
            if (baptismAge > HoldEnd + SpinFrames) {
                Projectile.Kill();
                return;
            }
            //整个洗礼期间占住使用,不能刺击
            owner.itemTime = owner.itemAnimation = 2;
            tremble = Vector2.Zero;

            if (baptismAge <= RaiseFrames) {
                float k = baptismAge / (float)RaiseFrames;
                aim = startAim + MathHelper.WrapAngle(Up - startAim) * (1f - (1f - k) * (1f - k) * (1f - k));
                if (baptismAge == RaiseFrames) {
                    OnRaise(owner);
                }
            }
            else if (baptismAge <= HoldEnd) {
                aim = Up + Main.rand.NextFloat(-TrembleAngle, TrembleAngle);
                tremble = Main.rand.NextVector2Circular(TrembleShift, TrembleShift);
            }
            else {
                //转一圈后停在鼠标方向;转向在起转时定下,鼠标在正下方晃动也不会反转
                owner.Entropy().MouseWorldListener = true;
                Vector2 toMouse = owner.Entropy().MouseWorld - owner.MountedCenter;
                if (spinSign == 0) {
                    spinSign = toMouse.X >= 0f ? 1 : -1;
                }
                float delta = MathHelper.WrapAngle(toMouse.ToRotation() - Up);
                if (spinSign > 0 && delta < 0f) {
                    delta += MathHelper.TwoPi;
                }
                else if (spinSign < 0 && delta > 0f) {
                    delta -= MathHelper.TwoPi;
                }
                float k = (baptismAge - HoldEnd) / (float)SpinFrames;
                aim = Up + (delta + MathHelper.TwoPi * spinSign) * k * k * (3f - 2f * k);
                Projectile.direction = toMouse.X >= 0f ? 1 : -1;
            }
            if (skyShotAge >= 0) {
                skyShotAge++;
            }
            owner.ChangeDir(Projectile.direction);
            Pose(owner);
            Vector2 tip = TipPos(owner);
            Projectile.Center = Vector2.Lerp(HandPos(owner), tip, 0.5f);
            Lighting.AddLight(tip, SunVfx.Gold.ToVector3() * 0.6f);
        }

        /// <summary>举到正上方那一帧:能量射向天空,拥有者在鼠标处放下洗礼区</summary>
        private void OnRaise(Player owner) {
            skyShotAge = 0;
            Vector2 tip = TipPos(owner);
            if (Projectile.owner == Main.myPlayer) {
                int damage = Math.Max(1, (int)(Projectile.damage * PrayerlightLance.LaserDamageMult));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Entropy().MouseWorld, Vector2.Zero, ModContent.ProjectileType<PrayerlightLanceBaptism>(), damage, 0f, Projectile.owner);
            }
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("angel_blast1", 1f, tip, 2, 0.7f);
            CEUtils.SetShake(owner.Center, 3f, 1600f);
            PrayerlightLanceFx.Strike(tip, 1.2f);
        }

        private void Pose(Player owner) {
            float rot = AimRotation;
            owner.SetCompositeArmFront(true, Stretch, rot - MathHelper.PiOver2);
            owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, rot - MathHelper.PiOver2 + 0.5f * Projectile.direction);
            owner.itemRotation = MathHelper.WrapAngle(rot + (Projectile.direction < 0 ? MathHelper.Pi : 0f));
        }

        public override bool? CanDamage() => Live ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Player owner = Main.player[Projectile.owner];
            return CEUtils.LineThroughRect(HandPos(owner), TipPos(owner), targetHitbox, HitLineWidth);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.HitDirectionOverride = Projectile.direction;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            hitSerial++;
            seenMarker = hitSerial * HitMarkerStride + target.whoAmI;
            Projectile.ai[2] = seenMarker;
            Projectile.netUpdate = true;
            HitFx(Main.player[Projectile.owner], target);
        }

        private void ReadHitMarker(Player owner) {
            int marker = (int)Projectile.ai[2];
            if (marker == seenMarker) {
                return;
            }
            seenMarker = marker;
            int index = marker % HitMarkerStride;
            if (marker > 0 && index < Main.maxNPCs && Main.npc[index].active) {
                HitFx(owner, Main.npc[index]);
            }
        }

        /// <summary>命中点取目标中心在刺击线上的投影,火花顺着刺击方向溅出</summary>
        private void HitFx(Player owner, NPC target) {
            if (Main.dedServ) {
                return;
            }
            Vector2 hand = HandPos(owner);
            Vector2 onLine = hand + Dir * Vector2.Dot(target.Center - hand, Dir);
            Vector2 contact = Vector2.Clamp(onLine, target.TopLeft, target.BottomRight);
            CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(0.95f, 1.1f), contact, 3, 0.55f * CEUtils.WeapSound);
            for (int i = 0; i < 6; i++) {
                Color c = i % 3 == 0 ? SunVfx.Core : SunVfx.Gold;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(contact, Dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(8f, 18f), c, Main.rand.NextFloat(0.035f, 0.055f)).Configure(false, 10, new Vector2(2.4f, 0.8f), true);
            }
        }

        private float GlowOpacity(Player owner) {
            if (Baptism) {
                if (baptismAge <= RaiseFrames) {
                    return 1f;
                }
                return MathHelper.Clamp(1f - (baptismAge - RaiseFrames) / (float)PrayerlightLance.BaptismFrames, 0f, 1f);
            }
            return Projectile.owner == Main.myPlayer ? owner.GetModPlayer<PrayerlightLancePlayer>().Progress : Projectile.ai[1];
        }

        public override bool PreDraw(ref Color lightColor) {
            if (total == 0 && baptismAge == 0) {
                return false;
            }
            Player owner = Main.player[Projectile.owner];
            Texture2D glowTex = GlowMask.Value;
            HeldSprite spear = Sprite(owner, Reach);
            float glow = GlowOpacity(owner);
            Vector2 tip = TipPos(owner);

            //爆发这几帧沿轴拖两道虚影
            float travel = extension - prevExtension;
            if (!Baptism && travel > 6f) {
                VDWeaponFx.BeginAdditive();
                for (int i = 1; i <= 2; i++) {
                    Sprite(owner, extension - travel * i / 3f).Draw(glowTex, SunVfx.Gold * (0.4f - i * 0.1f));
                }
                VDWeaponFx.End();
            }

            spear.Draw(lightColor);
            if (glow > 0.01f) {
                spear.Draw(glowTex, Color.White * glow);
            }

            VDWeaponFx.BeginAdditive();
            //积满后日徽缓慢脉动
            float pulse = glow >= 1f ? 0.75f + 0.25f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f) : 1f;
            VDWeaponFx.Glow(spear.ToWorld(PrayerlightLanceGeometry.Emblem), SunVfx.Gold * (0.45f * glow * pulse), 22f + 10f * glow);
            if (Live) {
                VDWeaponFx.GlowStretched(tip, SunVfx.Core * 0.6f, AimRotation, 46f, 14f);
            }
            if (skyShotAge >= 0 && skyShotAge < SkyShotFrames) {
                float k = skyShotAge / (float)SkyShotFrames;
                float rise = MathHelper.Clamp(skyShotAge / (float)SkyShotRiseFrames, 0f, 1f);
                PrayerlightLanceFx.Beam(tip - Vector2.UnitY * SkyShotLength * rise, tip, 20f * (1f - k * 0.5f), 1f - k);
            }
            VDWeaponFx.End();
            return false;
        }
    }
}
