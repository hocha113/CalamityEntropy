using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.NurglePot
{
    /// <summary>
    /// 纳垢锅手持弹幕。ai[0] = 模式(0 双手甩锅撒粘液 / 1 持锅加热喷滚烫粘液)。
    /// 模式 0 完全由计数器驱动,各端演出一致,只有所有者在释放帧生成粘液;
    /// 模式 1 由所有者决定加热进度与松手,进度经 SendExtraAI 下发。
    /// </summary>
    public class NurglePotHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/NurglePot/KevinsNurglePot";

        #region 调参旋钮
        public const float SwingFrames = 30f;
        //甩锅三段:蓄力结束进度、爆发结束进度、释放进度
        public const float GatherFrac = 0.35f;
        public const float BurstFrac = 0.55f;
        public const float ReleaseFrac = 0.48f;
        //甩锅角度(相对瞄准方向,朝向已归一):起手举到身后上方,爆发甩到前下方,收势回一点
        public const float StartDeg = -110f;
        public const float PullDeg = -150f;
        public const float EndDeg = 30f;
        public const float SettleDeg = 12f;
        //锅心距玩家中心、锅口距锅心
        public const float HoldDist = 30f;
        public const float MouthDist = 16f;
        #endregion

        private Player Owner => Main.player[Projectile.owner];
        private int Mode => (int)Projectile.ai[0];

        private bool initialized;
        private int facing = 1;
        private float aim;
        private float atkSpeed = 1f;
        private float counter;
        private bool released;
        private float potAngle;

        //模式 1 状态,所有者决定
        private int heat;
        private bool boiling;
        private int shotTimer;
        private bool steamCuePlayed;

        private Vector2 PotCenter => Projectile.Center + potAngle.ToRotationVector2() * HoldDist;
        private Vector2 MouthPos => PotCenter + potAngle.ToRotationVector2() * MouthDist;

        public override void SetDefaults() {
            Projectile.HeldProjSetDefaults(DamageClass.Magic);
            Projectile.timeLeft = 100000;
        }

        public override bool? CanDamage() => false;

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(heat);
            writer.Write(boiling);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            heat = reader.ReadInt32();
            boiling = reader.ReadBoolean();
        }

        private void Initialize(Player owner) {
            initialized = true;
            facing = Projectile.velocity.X != 0 ? Math.Sign(Projectile.velocity.X) : owner.direction;
            aim = Projectile.velocity.LengthSquared() > 0.01f ? Projectile.velocity.ToRotation() : (facing > 0 ? 0f : MathHelper.Pi);
            atkSpeed = MathHelper.Clamp(owner.GetWeaponAttackSpeed(owner.HeldItem), 0.2f, 5f);
        }

        public override void AI() {
            Player owner = Owner;
            if (!owner.active || owner.dead || owner.HeldItem.ModItem is not KevinsNurglePot) {
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

            if (Mode == 0) {
                UpdateSwing(owner);
            }
            else {
                UpdateHeat(owner);
            }
        }

        #region 模式 0:双手甩锅
        private void UpdateSwing(Player owner) {
            float total = SwingFrames / atkSpeed;
            float p = counter / total;
            float offDeg;
            if (p < GatherFrac) {
                float t = MathHelper.SmoothStep(0f, 1f, p / GatherFrac);
                offDeg = MathHelper.Lerp(StartDeg, PullDeg, t);
            }
            else if (p < BurstFrac) {
                float t = (p - GatherFrac) / (BurstFrac - GatherFrac);
                offDeg = MathHelper.Lerp(PullDeg, EndDeg, t * t);
                if (!released && p >= ReleaseFrac) {
                    released = true;
                    Fling(owner);
                }
            }
            else {
                float t = MathHelper.SmoothStep(0f, 1f, (p - BurstFrac) / (1f - BurstFrac));
                offDeg = MathHelper.Lerp(EndDeg, SettleDeg, t);
            }
            potAngle = aim + facing * MathHelper.ToRadians(offDeg);
            Projectile.rotation = potAngle + MathHelper.PiOver2;
            //双手都握着锅
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, potAngle - MathHelper.PiOver2);
            owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, potAngle - MathHelper.PiOver2);

            //爆发段锅口甩出零星绿滴
            if (!Main.dedServ && p >= GatherFrac && p < BurstFrac && counter % 2 == 0) {
                Dust d = Dust.NewDustPerfect(MouthPos, DustID.Poisoned, potAngle.ToRotationVector2() * 3f + CEUtils.randomPointInCircle(1f), 100, default, 1.1f);
                d.noGravity = false;
            }

            counter++;
            if (p >= 1f) {
                Finish(owner);
            }
        }

        private void Fling(Player owner) {
            CEUtils.PlaySound("poop_itemthrow", Main.rand.NextFloat(0.9f, 1.1f), PotCenter, 6, 0.9f);
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.5f, Pitch = -0.2f }, PotCenter);
            if (!Main.dedServ) {
                for (int i = 0; i < 12; i++) {
                    Dust d = Dust.NewDustPerfect(MouthPos, DustID.Poisoned, potAngle.ToRotationVector2().RotatedByRandom(0.6f) * Main.rand.NextFloat(3f, 9f), 80, default, Main.rand.NextFloat(1f, 1.6f));
                    d.noGravity = false;
                }
            }
            if (Main.myPlayer != Projectile.owner) {
                return;
            }
            Vector2 mouth = MouthPos;
            Vector2 toMouse = (owner.mouseWorld() - mouth).SafeNormalize(aim.ToRotationVector2());
            //稍微抬一点,让粘液画出抛物线撒开
            toMouse = (toMouse + new Vector2(0, -0.18f)).SafeNormalize(toMouse);
            int type = ModContent.ProjectileType<NurgleSlimeGlob>();
            for (int i = 0; i < KevinsNurglePot.SplashCount; i++) {
                float spread = MathHelper.Lerp(-KevinsNurglePot.SplashSpread, KevinsNurglePot.SplashSpread, i / (KevinsNurglePot.SplashCount - 1f)) + Main.rand.NextFloat(-0.06f, 0.06f);
                Vector2 vel = toMouse.RotatedBy(spread) * Main.rand.NextFloat(KevinsNurglePot.SplashSpeedMin, KevinsNurglePot.SplashSpeedMax);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), mouth, vel, type, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
        }
        #endregion

        #region 模式 1:持锅加热
        private void UpdateHeat(Player owner) {
            //瞄准随鼠标(各端读同步过的鼠标世界坐标)
            Vector2 mouse = owner.mouseWorld();
            Vector2 toMouse = mouse - owner.MountedCenter;
            if (toMouse.LengthSquared() > 1f) {
                aim = toMouse.ToRotation();
            }
            facing = Math.Cos(aim) >= 0 ? 1 : -1;
            owner.direction = facing;
            potAngle = aim;
            Projectile.rotation = aim + MathHelper.PiOver2;
            //后手托锅,前手在锅底施火
            owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, aim - MathHelper.PiOver2 + facing * 0.55f);

            float progress = MathHelper.Clamp(heat / (float)KevinsNurglePot.HeatFrames, 0f, 1f);
            if (!Main.dedServ) {
                SpawnHeatVisuals(progress);
            }

            if (Main.myPlayer == Projectile.owner) {
                if (!Main.mouseRight) {
                    Finish(owner);
                    return;
                }
                if (!boiling) {
                    heat++;
                    if (heat >= KevinsNurglePot.HeatFrames) {
                        boiling = true;
                        shotTimer = KevinsNurglePot.BoilShotInterval;
                        Projectile.netUpdate = true;
                    }
                    else if (heat % 15 == 0) {
                        Projectile.netUpdate = true;
                    }
                }
                else {
                    shotTimer++;
                    if (shotTimer >= KevinsNurglePot.BoilShotInterval) {
                        shotTimer = 0;
                        if (!owner.CheckMana(KevinsNurglePot.ManaPerBoilShot, true)) {
                            Finish(owner);
                            return;
                        }
                        owner.manaRegenDelay = (int)owner.maxRegenDelay;
                        FireBoilShot();
                    }
                }
            }
            else if (!boiling && heat < KevinsNurglePot.HeatFrames) {
                heat++;
            }

            if (boiling && !steamCuePlayed) {
                steamCuePlayed = true;
                CEUtils.PlaySound("steam", 1f, PotCenter, 4, 0.7f);
                if (!Main.dedServ) {
                    for (int i = 0; i < 10; i++) {
                        PRTLoader.NewParticle<PRT_HeavySmokeCal>(MouthPos, potAngle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 4f), new Color(150, 210, 70), Main.rand.NextFloat(0.8f, 1.2f)).Configure(0.5f, 34, 0.03f, true);
                    }
                }
            }
            counter++;
        }

        private void SpawnHeatVisuals(float progress) {
            Vector2 potCenter = PotCenter;
            Vector2 bottom = potCenter - potAngle.ToRotationVector2() * 12f;
            //锅底火魔法:火焰随加热进度变旺
            int flames = 1 + (int)(progress * 2);
            for (int i = 0; i < flames; i++) {
                Dust d = Dust.NewDustPerfect(bottom + CEUtils.randomPointInCircle(6f), DustID.Torch, new Vector2(0, -1.5f) + CEUtils.randomPointInCircle(0.6f), 0, default, Main.rand.NextFloat(1f, 1.5f + progress * 0.5f));
                d.noGravity = true;
            }
            //锅口:加热过半开始冒泡,沸腾后持续蒸汽
            if (progress > 0.5f && Main.rand.NextFloat() < (progress - 0.5f) * 1.4f) {
                Dust d = Dust.NewDustPerfect(MouthPos + CEUtils.randomPointInCircle(6f), DustID.Poisoned, potAngle.ToRotationVector2() * Main.rand.NextFloat(0.5f, 2f), 120, default, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }
            if (boiling && counter % 3 == 0) {
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(MouthPos + CEUtils.randomPointInCircle(5f), potAngle.ToRotationVector2() * Main.rand.NextFloat(0.6f, 1.8f) + new Vector2(0, -0.6f), new Color(160, 200, 80), Main.rand.NextFloat(0.5f, 0.8f)).Configure(0.35f, 30, 0.02f);
            }
            Lighting.AddLight(bottom, 0.9f * (0.4f + progress * 0.6f), 0.5f * (0.4f + progress * 0.6f), 0.1f);
        }

        private void FireBoilShot() {
            Vector2 mouth = MouthPos;
            Vector2 vel = (potAngle.ToRotationVector2() * KevinsNurglePot.BoilShotSpeed).RotatedByRandom(0.08f) + new Vector2(0, -1.2f);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), mouth, vel, ModContent.ProjectileType<NurgleBoilingGlob>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            CEUtils.PlaySound("poop_itemthrow", Main.rand.NextFloat(1.1f, 1.3f), mouth, 6, 0.6f);
            Projectile.netUpdate = true;
        }
        #endregion

        private void Finish(Player owner) {
            owner.itemTime = 1;
            owner.itemAnimation = 1;
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 pos = PotCenter + Owner.gfxOffY * Vector2.UnitY - Main.screenPosition;
            Color col = Lighting.GetColor(PotCenter.ToTileCoordinates());
            Main.EntitySpriteDraw(tex, pos, null, col, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            if (Mode == 1 && boiling) {
                //沸腾时锅体透出一层热光
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                float pulse = 0.5f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f);
                Main.EntitySpriteDraw(tex, pos, null, new Color(255, 120, 40) * pulse, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }
    }
}
