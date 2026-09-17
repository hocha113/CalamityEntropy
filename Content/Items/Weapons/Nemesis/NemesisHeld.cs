using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
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

namespace CalamityEntropy.Content.Items.Weapons.Nemesis
{
    /// <summary>
    /// 天罚手持弹幕。三种模式共用一套「收-爆-停」骨架,刀光是 odr 旋转历史铺成的扫掠体,着色走 SwordTrail2。
    /// ai[0] = 模式(0 普通挥砍 / 1 天罚 / 2 蓄力旋斩),ai[1] = 挥砍方向(+1 下劈 / -1 上撩)。
    /// 结构镜像 TrueMoonlightSwordHeld:相位驱动、不依赖任何挥舞基类。
    /// </summary>
    internal class NemesisHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Nemesis/Nemesis";

        #region 调参旋钮
        //每帧逻辑更新次数,odr 采样密度、顿帧与冷却都以它为单位
        public const int Updates = 16;
        //模式 0:收势结束进度、爆发落点进度,余下为停势
        public const float GatherFrac = 0.22f;
        public const float BurstFrac = 0.45f;
        //模式 0:起手角(相对瞄准方向)、收势回拉角、落点过冲角;三者之和控制在 180 度内,读作切穿而不是绕圈
        public const float StartDeg = 80f;
        public const float PullDeg = 15f;
        public const float OvershootDeg = 5f;
        //过冲回坐所用帧数
        public const float SettleFrames = 2f;
        //模式 1:抡到头顶所占进度、头顶停顿结束进度、刀体弹放倍率
        public const float SmiteRaiseFrac = 0.2f;
        public const float SmiteHoldFrac = 0.4f;
        public const float SmiteScale = 1.24f;
        public const float SmiteStartDeg = 50f;
        //模式 2:充满帧数、警示音帧、旋转帧数、火焰斩间隔帧、基础角速度(度/帧)、旋转期同目标命中冷却帧
        public const int ChargeFrames = 140;
        public const int ChargeCueFrame = 130;
        public const int SpinFrames = 60;
        public const int SpinShotInterval = 4;
        public const float SpinDegPerFrame = 45f;
        public const int SpinHitCooldownFrames = 10;
        public const float ChargePoseDeg = 60f;
        //刀身可达长度(柄到尖)、碰撞线宽、割草线宽,均再乘 scale
        public const float BladeReach = 210f;
        public const float HitLineWidth = 56f;
        public const float CutTileWidth = 50f;
        //刀光:主层内缘占比、刃线内缘占比、历史采样上限、旋转期环形刀光覆盖的角度(留口不闭合,避免加法混合叠圈)、爆发后完全消散的进度、刃线白闪帧数
        public const float RibbonInnerFrac = 0.45f;
        public const float EdgeInnerFrac = 0.88f;
        public const int RibbonSamples = 96;
        public const float SpinRibbonDeg = 350f;
        public const float RibbonFadeEnd = 0.75f;
        public const int EdgeFlashFrames = 2;
        public const int BladeFlashFrames = 2;
        //身体倾斜(度):模式 1 起手后仰、模式 2 释放前扑
        public const float LeanSmiteDeg = -8f;
        public const float LeanSpinDeg = 10f;
        //顿帧帧数、震屏幅度
        public const int HitStopFrames = 3;
        public const float ShakeAmp = 8f;
        public const float SpinShakeAmp = 4f;
        //火焰调色:暗余烬 → 热金;刃线:浅金 → 白
        public static readonly Color EmberColor1 = new Color(140, 30, 20);
        public static readonly Color EmberColor2 = new Color(255, 210, 120);
        public static readonly Color EdgeColor1 = new Color(255, 230, 170);
        public static readonly Color EdgeColor2 = new Color(255, 255, 240);
        public static readonly Color MarginColor = new Color(255, 50, 50);
        #endregion

        private enum ChargePhase : byte
        {
            Charging = 0,
            Released = 1,
        }

        private Player Owner => Main.player[Projectile.owner];
        private int Mode => (int)Projectile.ai[0];
        private int SwingSign => Projectile.ai[1] < 0 ? -1 : 1;
        //逻辑帧(顿帧时冻结)
        private float Frame => counter / Updates;
        //刀尖可达距离,含天罚弹放与释放爆增
        private float Reach => BladeReach * Projectile.scale * bladeScaleMul * reachMul;
        private Vector2 TipPos => Projectile.Center + Projectile.rotation.ToRotationVector2() * Reach;

        private bool initialized;
        private int facing = 1;
        private float aim;
        private float atkSpeed = 1f;
        private float swingFrames = 18f;
        private float counter;
        private int stopTicks;
        private bool hitStopUsed;
        private bool healOrbsSpawned;
        private bool shotFired;
        private bool burstEntered;
        private float bladeScaleMul = 1f;
        private float reachMul = 1f;
        private int bladeFlashTicks;

        //模式 2 相位,所有者决定,SendExtraAI 下发
        private ChargePhase phase = ChargePhase.Charging;
        private int chargeTicks;
        private int spinTicks;
        private bool cuePlayed;

        //身体倾斜的写入记录,用于仲裁 fullRotation 的其他写入者
        private bool leanOwned;
        private float leanWritten;

        //刀光旋转历史与包络
        private readonly List<float> odr = new List<float>();
        private int ribbonCap = RibbonSamples;
        private float ribbonAlpha;
        private int edgeFlashTicks;
        private int ribbonSense = 1;

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.MaxUpdates = Updates;
        }

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write((byte)phase);
            writer.Write(chargeTicks);
            writer.Write(spinTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            ChargePhase incoming = (ChargePhase)reader.ReadByte();
            chargeTicks = reader.ReadInt32();
            spinTicks = reader.ReadInt32();
            if (incoming == ChargePhase.Released && phase != ChargePhase.Released) {
                //旁观者也要看到释放帧:白闪一次并从零铺环形刀光
                bladeFlashTicks = BladeFlashFrames * Updates;
                odr.Clear();
            }
            phase = incoming;
        }

        private void Initialize(Player owner) {
            initialized = true;
            facing = Projectile.velocity.X != 0 ? Math.Sign(Projectile.velocity.X) : owner.direction;
            aim = Projectile.velocity.LengthSquared() > 0.01f ? Projectile.velocity.ToRotation() : (facing > 0 ? 0f : MathHelper.Pi);
            atkSpeed = MathHelper.Clamp(owner.GetWeaponAttackSpeed(owner.HeldItem), 0.2f, 5f);
            int baseUse = owner.HeldItem.useTime > 0 ? owner.HeldItem.useTime : 18;
            swingFrames = baseUse / atkSpeed;
            Projectile.scale = owner.GetAdjustedItemScale(owner.HeldItem);
            if (Mode == 2) {
                Projectile.localNPCHitCooldown = SpinHitCooldownFrames * Updates;
                //按实际角速度算采样数,让环覆盖 SpinRibbonDeg 度后正好收口
                float degPerUpdate = SpinDegPerFrame * atkSpeed / Updates;
                ribbonCap = Math.Max(8, (int)(SpinRibbonDeg / degPerUpdate));
            }
        }

        public override void AI() {
            Player owner = Owner;
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<Nemesis>()) {
                ReleaseLean(owner);
                Projectile.Kill();
                return;
            }
            if (!initialized) {
                Initialize(owner);
            }

            Projectile.timeLeft = 3;
            Projectile.Center = owner.MountedCenter;
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            owner.direction = facing;

            bool alive = Mode switch {
                1 => UpdateSmite(owner),
                2 => UpdateCharge(owner),
                _ => UpdateSwing(owner),
            };
            if (!alive) {
                return;
            }

            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            if (counter % Updates == 0) {
                //刀光亮着时满光,蓄力期只给刀尖一点余烬光
                float glow = ribbonAlpha > 0.05f ? 1f : (Mode == 2 ? 0.35f : 0f);
                if (glow > 0f) {
                    Lighting.AddLight(TipPos, 1f * glow, 0.55f * glow, 0.2f * glow);
                }
            }

            if (stopTicks > 0) {
                stopTicks--;
            }
            else {
                counter++;
            }
            if (edgeFlashTicks > 0) {
                edgeFlashTicks--;
            }
            if (bladeFlashTicks > 0) {
                bladeFlashTicks--;
            }
        }

        #region 模式 0:普通挥砍
        private bool UpdateSwing(Player owner) {
            float p = Frame / swingFrames;
            int sense = SwingSign * facing;
            ribbonSense = sense;
            float offDeg;
            if (p < GatherFrac) {
                //收势:回拉 15 度后近乎静止,只留呼吸颤动
                float t = p / GatherFrac;
                float ease = MathHelper.SmoothStep(0f, 1f, t);
                offDeg = -StartDeg - PullDeg * ease + (float)Math.Sin(counter * 0.7f) * 0.6f * t;
                ribbonAlpha = 0f;
            }
            else if (p < BurstFrac) {
                //爆发:加速曲线,前段几乎不动、末两帧跨过大半弧
                if (!burstEntered) {
                    burstEntered = true;
                    odr.Clear();
                }
                float t = (p - GatherFrac) / (BurstFrac - GatherFrac);
                float acc = (float)Math.Pow(t, 2.4);
                offDeg = -StartDeg - PullDeg + (2f * StartDeg + PullDeg + OvershootDeg) * acc;
                ribbonAlpha = 1f;
                if (!Main.dedServ && counter % 2 == 0) {
                    SpawnTipEmber(sense);
                }
            }
            else {
                //落点:发射龙陨星,过冲两帧回坐后几何冻结
                if (!shotFired) {
                    shotFired = true;
                    edgeFlashTicks = EdgeFlashFrames * Updates;
                    SoundEngine.PlaySound(SoundID.Item71, owner.position);
                    if (Main.myPlayer == Projectile.owner) {
                        FireMeteors(owner);
                    }
                }
                float sinceLand = Frame - BurstFrac * swingFrames;
                float settle = MathHelper.Clamp(sinceLand / SettleFrames, 0f, 1f);
                offDeg = StartDeg + OvershootDeg * (1f - settle);
                ribbonAlpha = 1f - MathHelper.Clamp((p - BurstFrac) / (RibbonFadeEnd - BurstFrac), 0f, 1f);
            }

            Projectile.rotation = aim + sense * MathHelper.ToRadians(offDeg);
            if (p >= GatherFrac) {
                RecordRibbon(sense);
            }
            if (p >= 1f) {
                Finish(owner);
                return false;
            }
            return true;
        }

        private void FireMeteors(Player owner) {
            Vector2 mouse = Main.MouseWorld;
            Vector2 aimUnit = (mouse - owner.MountedCenter).SafeNormalize(Vector2.UnitX * facing);
            Vector2 orig = owner.MountedCenter + aimUnit * 80f + new Vector2(0, -800);
            Vector2 toMouse = (mouse - orig).SafeNormalize(Vector2.UnitY);
            IEntitySource src = owner.GetSource_ItemUse(owner.HeldItem);
            int type = ModContent.ProjectileType<NemesisProj>();
            for (int i = 0; i < 3; i++) {
                Vector2 spawnPos = orig + toMouse * 600f;
                spawnPos.X += Main.rand.Next(-260, 260);
                spawnPos.Y -= 660;
                Vector2 vel = (mouse - spawnPos).SafeNormalize(Vector2.UnitY) * 26f;
                vel = vel.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.6f, 1.33f);
                Projectile.NewProjectile(src, spawnPos, vel, type, Projectile.damage / 4, Projectile.knockBack, owner.whoAmI, 0f, 0f);
            }
        }
        #endregion

        #region 模式 1:天罚
        private bool UpdateSmite(Player owner) {
            float total = 20f / atkSpeed;
            float p = Frame / total;
            //起手方向按朝向水平线取,不跟鼠标,抡起方向为逆朝向(面朝右时逆时针向上)
            int sense = -facing;
            ribbonSense = sense;
            float baseDir = facing > 0 ? 0f : MathHelper.Pi;
            float thetaDeg;
            float lean = 0f;
            if (p < SmiteRaiseFrac) {
                //起手:加速抡到头顶,刀体同步弹放
                if (!burstEntered) {
                    burstEntered = true;
                    odr.Clear();
                }
                float t = p / SmiteRaiseFrac;
                float acc = t * t;
                thetaDeg = SmiteStartDeg - (SmiteStartDeg + 90f + OvershootDeg) * acc;
                bladeScaleMul = 1f + (SmiteScale - 1f) * acc;
                ribbonAlpha = 1f;
                lean = LeanSmiteDeg * t;
                if (!Main.dedServ && counter % 2 == 0) {
                    SpawnTipEmber(sense);
                }
            }
            else if (p < SmiteHoldFrac) {
                //头顶停顿:落点帧降下天罚,过冲回坐,身体缓缓回正
                if (!shotFired) {
                    shotFired = true;
                    bladeFlashTicks = BladeFlashFrames * Updates;
                    edgeFlashTicks = EdgeFlashFrames * Updates;
                    SoundEngine.PlaySound(SoundID.Item69, owner.position);
                    if (Main.myPlayer == Projectile.owner) {
                        FireSmite(owner);
                    }
                }
                float sinceLand = Frame - SmiteRaiseFrac * total;
                float settle = MathHelper.Clamp(sinceLand / SettleFrames, 0f, 1f);
                thetaDeg = -90f - OvershootDeg * (1f - settle);
                bladeScaleMul = SmiteScale;
                float holdT = (p - SmiteRaiseFrac) / (SmiteHoldFrac - SmiteRaiseFrac);
                ribbonAlpha = 1f - holdT;
                lean = LeanSmiteDeg * (1f - holdT);
            }
            else {
                //缓落:头顶回到前方,刀体缩回原尺寸
                float t = (p - SmiteHoldFrac) / (1f - SmiteHoldFrac);
                float ease = MathHelper.SmoothStep(0f, 1f, t);
                thetaDeg = -90f + (SmiteStartDeg - 20f + 90f) * ease;
                bladeScaleMul = MathHelper.Lerp(SmiteScale, 1f, ease);
                ribbonAlpha = 0f;
            }

            Projectile.rotation = baseDir + facing * MathHelper.ToRadians(thetaDeg);
            if (p < SmiteHoldFrac) {
                RecordRibbon(sense);
            }
            ApplyLean(owner, lean);
            if (p >= 1f) {
                Finish(owner);
                return false;
            }
            return true;
        }

        private void FireSmite(Player owner) {
            Vector2 pos = Main.MouseWorld;
            pos.Y -= 800;
            pos.X -= facing * 320;
            Vector2 vel = new Vector2(facing * 2, 6);
            Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), pos, vel, ModContent.ProjectileType<EXNemesisProj>(),
                Projectile.damage * 5, Projectile.knockBack, owner.whoAmI, 0f, 0f);
        }
        #endregion

        #region 模式 2:蓄力旋斩
        private bool UpdateCharge(Player owner) {
            int sense = facing;
            ribbonSense = sense;
            float baseDir = facing > 0 ? 0f : MathHelper.Pi;
            if (phase == ChargePhase.Charging) {
                int chargeFrame = chargeTicks / Updates;
                float progress = MathHelper.Clamp(chargeFrame / (float)ChargeFrames, 0f, 1f);
                //蓄力姿态锁在前下方,临近充满时轻微颤动
                float tremble = progress > 0.8f ? (float)Math.Sin(counter * 1.3f) * 0.8f * (progress - 0.8f) / 0.2f : 0f;
                Projectile.rotation = baseDir + facing * MathHelper.ToRadians(ChargePoseDeg + tremble);
                ribbonAlpha = 0f;
                if (!cuePlayed && chargeFrame >= ChargeCueFrame) {
                    cuePlayed = true;
                    edgeFlashTicks = EdgeFlashFrames * Updates;
                    SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
                }
                if (!Main.dedServ && chargeTicks % 2 == 0) {
                    SpawnChargeMote(progress);
                }
                if (Main.myPlayer == Projectile.owner && !Main.mouseRight) {
                    if (chargeFrame >= ChargeFrames) {
                        phase = ChargePhase.Released;
                        spinTicks = 0;
                        bladeFlashTicks = BladeFlashFrames * Updates;
                        odr.Clear();
                        Projectile.netUpdate = true;
                    }
                    else {
                        //未满松手:真取消,不留任何下限
                        Finish(owner);
                        return false;
                    }
                }
                chargeTicks++;
                return true;
            }

            //释放:一帧半径爆增后高速旋转,火焰斩按固定间隔射出
            float envelope = MathHelper.Clamp(1f - spinTicks / (10f * Updates), 0f, 1f);
            reachMul = 1.1f + 0.15f * envelope;
            bladeScaleMul = 1f + 0.1f * envelope;
            Projectile.rotation += sense * MathHelper.ToRadians(SpinDegPerFrame * atkSpeed) / Updates;
            int fadeStart = (SpinFrames - 10) * Updates;
            ribbonAlpha = spinTicks > fadeStart ? 1f - (spinTicks - fadeStart) / (10f * Updates) : 1f;
            if (spinTicks % (SpinShotInterval * Updates) == 0) {
                SoundEngine.PlaySound(SoundID.Item71, owner.position);
                if (Main.myPlayer == Projectile.owner) {
                    FireCrescent(owner);
                }
            }
            if (!Main.dedServ && spinTicks % 4 == 0) {
                SpawnTipEmber(sense);
            }
            ApplyLean(owner, LeanSpinDeg * envelope);
            RecordRibbon(sense);
            spinTicks++;
            if (spinTicks >= SpinFrames * Updates) {
                Finish(owner);
                return false;
            }
            return true;
        }

        private void FireCrescent(Player owner) {
            Vector2 aimUnit = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * facing);
            Vector2 spawnPos = owner.MountedCenter + aimUnit * 80f;
            Vector2 vel = (aimUnit * 20f * atkSpeed).RotatedByRandom(0.66f);
            Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), spawnPos, vel, ModContent.ProjectileType<NemesisAlt>(),
                Projectile.damage * 10, Projectile.knockBack, owner.whoAmI, 0f, 0f);
        }

        private void SpawnChargeMote(float progress) {
            //火星从递减半径处向刀尖汇聚,能量可见地在装填
            float radius = 120f * (1f - progress) + 12f;
            Vector2 tip = TipPos;
            Vector2 pos = tip + CEUtils.randomRot().ToRotationVector2() * radius;
            Vector2 vel = (tip - pos) / 8f;
            Color color = Main.rand.NextBool() ? Color.OrangeRed : Color.Gold;
            PRTLoader.NewParticle<PRT_Light>(pos, vel, color, Main.rand.NextFloat(0.5f, 0.9f)).Configure(0.8f, lifetime: 12);
        }
        #endregion

        #region 收尾与仲裁
        private void Finish(Player owner) {
            owner.itemTime = 1;
            owner.itemAnimation = 1;
            ReleaseLean(owner);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft) {
            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers) {
                ReleaseLean(Main.player[Projectile.owner]);
            }
        }

        //前倾正值为面朝方向;骑乘或已有其他写入者时放弃本帧
        private void ApplyLean(Player owner, float deg) {
            float target = MathHelper.ToRadians(deg) * facing;
            if (owner.mount.Active) {
                ReleaseLean(owner);
                return;
            }
            if (leanOwned && owner.fullRotation != leanWritten) {
                leanOwned = false;
                return;
            }
            if (!leanOwned && owner.fullRotation != 0f) {
                return;
            }
            if (Math.Abs(target) < 0.0005f) {
                ReleaseLean(owner);
                return;
            }
            owner.fullRotationOrigin = new Vector2(owner.width / 2f, owner.height);
            owner.fullRotation = target;
            leanWritten = target;
            leanOwned = true;
        }

        private void ReleaseLean(Player owner) {
            if (leanOwned && owner != null && owner.fullRotation == leanWritten) {
                owner.fullRotation = 0f;
            }
            leanOwned = false;
            leanWritten = 0f;
        }
        #endregion

        #region 刀光采样
        //刀光头永远钉在当前刀角:前进则追加,回坐则先裁掉超前于刀体的采样再补当前角,静止不动
        private void RecordRibbon(int sense) {
            float rot = Projectile.rotation;
            if (odr.Count == 0) {
                odr.Add(rot);
                return;
            }
            float d = sense * MathHelper.WrapAngle(rot - odr[odr.Count - 1]);
            if (d > 0.0001f) {
                odr.Add(rot);
            }
            else if (d < -0.0001f) {
                while (odr.Count > 0 && sense * MathHelper.WrapAngle(odr[odr.Count - 1] - rot) > 0.0005f) {
                    odr.RemoveAt(odr.Count - 1);
                }
                odr.Add(rot);
            }
            while (odr.Count > ribbonCap) {
                odr.RemoveAt(0);
            }
        }

        private void SpawnTipEmber(int sense) {
            Vector2 tangent = (Projectile.rotation + sense * MathHelper.PiOver2).ToRotationVector2();
            Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * Reach * Main.rand.NextFloat(0.75f, 1f);
            Vector2 vel = tangent * Main.rand.NextFloat(2f, 5f) + CEUtils.randomPointInCircle(1.5f);
            PRTLoader.NewParticle<PRT_FlameCal>(pos, vel, Color.Gold, 0.05f).Configure(18, Main.rand.NextFloat(0.3f, 0.5f), Color.Firebrick);
        }
        #endregion

        #region 碰撞与命中
        public override bool? CanDamage() {
            switch (Mode) {
                case 1: {
                        float total = 20f / atkSpeed;
                        return Frame <= SmiteRaiseFrac * total + SettleFrames;
                    }
                case 2:
                    return phase == ChargePhase.Released;
                default: {
                        float p = Frame / swingFrames;
                        return p >= GatherFrac && Frame <= BurstFrac * swingFrames + SettleFrames;
                    }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            //从手心起算到刀尖,贴脸与擦边都算
            return CEUtils.LineThroughRect(Projectile.Center, TipPos, targetHitbox, (int)(HitLineWidth * Projectile.scale));
        }

        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, TipPos, CutTileWidth * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            int dir = Math.Sign(target.Center.X - Owner.Center.X);
            modifiers.HitDirectionOverride = dir == 0 ? facing : dir;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Player owner = Owner;
            if (!healOrbsSpawned) {
                healOrbsSpawned = true;
                if (Main.myPlayer == Projectile.owner) {
                    IEntitySource src = owner.GetSource_ItemUse(owner.HeldItem);
                    int type = ModContent.ProjectileType<NemesisProj>();
                    for (int i = 0; i < 6; i++) {
                        Projectile.NewProjectile(src, target.Center, CEUtils.randVr(3, 18), type, Projectile.damage
                            , Projectile.knockBack, owner.whoAmI, 1f, 0f);
                    }
                }
            }
            if (Mode == 0 && !hitStopUsed) {
                hitStopUsed = true;
                stopTicks = HitStopFrames * Updates;
            }
            if (Main.dedServ) {
                return;
            }

            Vector2 tangent = (Projectile.rotation + ribbonSense * MathHelper.PiOver2).ToRotationVector2();
            ScreenShaker.AddShake(new ScreenShaker.ScreenShake(tangent, Mode == 2 ? SpinShakeAmp : ShakeAmp));
            CEUtils.PlaySound("SwordHit" + Main.rand.Next(2), Main.rand.NextFloat(0.85f, 1.05f), target.Center, 6, 0.6f * CEUtils.WeapSound);

            //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造
            for (int i = 0; i < 16; i++) {
                Vector2 vel = tangent.RotatedByRandom(0.46f) * Main.rand.NextFloat(16f, 30f);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, Color.Gold * 1.5f, Projectile.scale * 0.06f)
                    .Configure(false, 14, new Vector2(0.3f, 1f));
            }
            for (int i = 0; i < 12; i++) {
                Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(8f, 14f);
                Color color = Main.rand.NextBool() ? Color.OrangeRed : Color.Gold;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, color, 0.06f * Main.rand.NextFloat(0.65f, 1f))
                    .Configure(false, 11, new Vector2(3f, 0.4f), true);
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(255, 200, 120), 2.2f)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 8);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White, 1.2f)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 6);
        }
        #endregion

        #region 绘制
        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor) {
            Player owner = Owner;
            //主刀光:暗余烬到热金的扫掠体,噪声随挥舞方向滚动
            DrawRibbon(RibbonInnerFrac, 1f, ribbonAlpha, CEExtraAssets.MotionTrail2, CEExtraAssets.TurbulentNoise, EmberColor1, EmberColor2, 12f * ribbonSense);
            //刃线白:只在落点后两帧出现的结构性白
            if (edgeFlashTicks > 0) {
                float edgeAlpha = edgeFlashTicks / (float)(EdgeFlashFrames * Updates);
                DrawRibbon(EdgeInnerFrac, 1.02f, edgeAlpha * Math.Max(ribbonAlpha, 0.35f), CEExtraAssets.MotionTrail2, CEExtraAssets.white, EdgeColor1, EdgeColor2, 0f);
            }

            Texture2D tex = Projectile.GetTexture();
            int drawDir = ribbonSense;
            Vector2 origin = drawDir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = drawDir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = drawDir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;
            Vector2 drawPos = Projectile.Center + owner.gfxOffY * Vector2.UnitY - Main.screenPosition;
            float drawScale = Projectile.scale * bladeScaleMul;

            //模式 1/2 的红色旋转描边是「已充能」身份,蓄力期随进度浮现
            if (Mode != 0) {
                float marginMul = 1f;
                if (Mode == 2 && phase == ChargePhase.Charging) {
                    marginMul = 0.4f + 0.6f * MathHelper.Clamp(chargeTicks / (float)(ChargeFrames * Updates), 0f, 1f);
                }
                VaultUtils.DrawRotatingMarginEffect(Main.spriteBatch, tex, (int)counter, drawPos, null, MarginColor * marginMul, rot, origin, drawScale, effect);
            }

            if (bladeFlashTicks > 0) {
                //整刀白化只留给天罚落点与旋斩释放
                float strength = bladeFlashTicks / (float)(BladeFlashFrames * Updates);
                Effect white = CEEffectAssets.WhiteTrans;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, white, Main.GameViewMatrix.TransformationMatrix);
                white.Parameters["strength"].SetValue(strength);
                white.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(tex, drawPos, null, Color.White, rot, origin, drawScale, effect);
                Main.spriteBatch.ExitShaderRegion();
            }
            else {
                Main.EntitySpriteDraw(tex, drawPos, null, Color.White, rot, origin, drawScale, effect);
            }

            if (Mode == 2 && phase == ChargePhase.Charging) {
                int chargeFrame = chargeTicks / Updates;
                if (chargeFrame < ChargeFrames) {
                    CEUtils.DrawChargeBar(2f, owner.MountedCenter + new Vector2(0, 60) - Main.screenPosition, chargeFrame / (float)ChargeFrames, Color.OrangeRed);
                }
            }
            return false;
        }

        private void DrawRibbon(float innerFrac, float outerMul, float alpha, Texture2D shape, Texture2D noise, Color color1, Color color2, float scroll) {
            if (odr.Count < 2 || alpha <= 0.01f || shape == null || noise == null) {
                return;
            }
            float reach = Reach;
            Vector2 center = Projectile.Center - Main.screenPosition;
            List<ColoredVertex> ve = new List<ColoredVertex>(odr.Count * 2);
            for (int i = 0; i < odr.Count; i++) {
                float u = i / (float)(odr.Count - 1);
                Vector2 dir = odr[i].ToRotationVector2();
                ve.Add(new ColoredVertex(center + dir * reach * outerMul, new Vector3(u, 1f, 1f), Color.White));
                ve.Add(new ColoredVertex(center + dir * reach * innerFrac, new Vector3(u, 0f, 1f), Color.White));
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            Effect shader = CEEffectAssets.SwordTrail2;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            shader.Parameters["color1"].SetValue(color1.ToVector4());
            shader.Parameters["color2"].SetValue(color2.ToVector4());
            shader.Parameters["alpha"].SetValue(alpha);
            shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * scroll);
            shader.CurrentTechnique.Passes["EffectPass"].Apply();
            gd.Textures[0] = shape;
            gd.Textures[1] = noise;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            sb.ExitShaderRegion();
        }
        #endregion
    }
}
