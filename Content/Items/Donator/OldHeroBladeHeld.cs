using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    /// <summary>
    /// 昔日的英雄之刃手持弹幕。收-爆-停三段挥砍,刀光是 odr 旋转历史铺成的扫掠体,着色走 SwordTrail2。
    /// ai[0] = 1 为昔日之斩(第四击:更大的弧、放大的刀体、身体前倾),ai[1] = 挥砍方向(+1 下劈 / -1 上撩)。
    /// 结构镜像 NemesisHeld 模式 0,不依赖任何挥舞基类。
    /// </summary>
    public class OldHeroBladeHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/OldHeroBlade";

        #region 调参旋钮
        //每帧逻辑更新次数,odr 采样密度、顿帧与冷却都以它为单位
        public const int Updates = 16;
        //收势结束进度、爆发落点进度,余下为停势
        public const float GatherFrac = 0.22f;
        public const float BurstFrac = 0.46f;
        //起手角(相对瞄准方向)、收势回拉角、落点过冲角;大剑语言,刀体全程可见,弧度比天罚略大
        public const float StartDeg = 92f;
        public const float GreatStartDeg = 108f;
        public const float PullDeg = 14f;
        public const float OvershootDeg = 6f;
        public const float SettleFrames = 2f;
        //昔日之斩的刀体放大倍率与身体前倾(度)
        public const float GreatScale = 1.22f;
        public const float GreatLeanDeg = 9f;
        //刀身可达长度(柄到尖)、碰撞线宽、割草线宽,均再乘 scale
        //122x106 贴图以左下角为原点,刀尖像素 (115,12) 距原点 148.6,刀光只多留 3 像素余量(2026-09-18 实测)
        public const float BladeReach = 152f;
        public const float HitLineWidth = 60f;
        public const float CutTileWidth = 50f;
        //刀光:主层内缘占比、刃线内缘占比、历史采样上限、爆发后完全消散的进度、刃线白闪帧数
        public const float RibbonInnerFrac = 0.42f;
        public const float EdgeInnerFrac = 0.88f;
        public const int RibbonSamples = 96;
        public const float RibbonFadeEnd = 0.78f;
        public const int EdgeFlashFrames = 2;
        //顿帧帧数与震屏幅度(普通 / 昔日之斩)
        public const int HitStopFrames = 3;
        public const int GreatHitStopFrames = 4;
        public const float ShakeAmp = 7f;
        public const float GreatShakeAmp = 11f;
        //钢色刀光:深钢蓝 → 冷白;刃线:淡蓝白 → 白
        public static readonly Color SteelColor1 = new Color(28, 60, 140);
        public static readonly Color SteelColor2 = new Color(190, 225, 255);
        public static readonly Color EdgeColor1 = new Color(225, 240, 255);
        public static readonly Color EdgeColor2 = new Color(255, 255, 255);
        #endregion

        private Player Owner => Main.player[Projectile.owner];
        private bool Great => Projectile.ai[0] > 0.5f;
        private int SwingSign => Projectile.ai[1] < 0 ? -1 : 1;
        //逻辑帧(顿帧时冻结)
        private float Frame => counter / Updates;
        private float Reach => BladeReach * Projectile.scale * bladeScaleMul;
        private Vector2 TipPos => Projectile.Center + Projectile.rotation.ToRotationVector2() * Reach;

        private bool initialized;
        private int facing = 1;
        private float aim;
        private float atkSpeed = 1f;
        private float swingFrames = 24f;
        private float counter;
        private int stopTicks;
        private bool hitStopUsed;
        private bool burstEntered;
        private bool shotFired;
        private float bladeScaleMul = 1f;

        //身体倾斜的写入记录,用于仲裁 fullRotation 的其他写入者
        private bool leanOwned;
        private float leanWritten;

        //刀光旋转历史与包络
        private readonly List<float> odr = new List<float>();
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

        private void Initialize(Player owner) {
            initialized = true;
            facing = Projectile.velocity.X != 0 ? Math.Sign(Projectile.velocity.X) : owner.direction;
            aim = Projectile.velocity.LengthSquared() > 0.01f ? Projectile.velocity.ToRotation() : (facing > 0 ? 0f : MathHelper.Pi);
            atkSpeed = MathHelper.Clamp(owner.GetWeaponAttackSpeed(owner.HeldItem), 0.2f, 5f);
            int baseUse = owner.HeldItem.useTime > 0 ? owner.HeldItem.useTime : 24;
            swingFrames = baseUse / atkSpeed;
            Projectile.scale = owner.GetAdjustedItemScale(owner.HeldItem);
            if (Great) {
                bladeScaleMul = GreatScale;
            }
        }

        public override void AI() {
            Player owner = Owner;
            if (!owner.active || owner.dead || owner.HeldItem.ModItem is not OldHeroBlade) {
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

            if (!UpdateSwing(owner)) {
                return;
            }

            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            if (counter % Updates == 0 && ribbonAlpha > 0.05f) {
                Lighting.AddLight(TipPos, 0.35f, 0.6f, 1f);
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
        }

        #region 挥砍
        private bool UpdateSwing(Player owner) {
            float p = Frame / swingFrames;
            int sense = SwingSign * facing;
            ribbonSense = sense;
            float startDeg = Great ? GreatStartDeg : StartDeg;
            float offDeg;
            float lean = 0f;
            if (p < GatherFrac) {
                //收势:回拉后近乎静止,只留呼吸颤动;昔日之斩顺势后仰
                float t = p / GatherFrac;
                float ease = MathHelper.SmoothStep(0f, 1f, t);
                offDeg = -startDeg - PullDeg * ease + (float)Math.Sin(counter * 0.7f) * 0.6f * t;
                ribbonAlpha = 0f;
                lean = Great ? -GreatLeanDeg * 0.6f * ease : 0f;
            }
            else if (p < BurstFrac) {
                //爆发:加速曲线,前段几乎不动、末两帧跨过大半弧
                if (!burstEntered) {
                    burstEntered = true;
                    odr.Clear();
                    SoundEngine.PlaySound(Great ? SoundID.Item71 with { Pitch = -0.2f } : SoundID.Item1 with { Pitch = -0.1f }, owner.position);
                    CEUtils.PlaySound(Great ? "swing3" : "swing" + Main.rand.Next(1, 3), Great ? 0.8f : Main.rand.NextFloat(0.95f, 1.1f), owner.Center, 6, 0.6f * CEUtils.WeapSound);
                }
                float t = (p - GatherFrac) / (BurstFrac - GatherFrac);
                float acc = (float)Math.Pow(t, 2.4);
                offDeg = -startDeg - PullDeg + (2f * startDeg + PullDeg + OvershootDeg) * acc;
                ribbonAlpha = 1f;
                lean = Great ? MathHelper.Lerp(-GreatLeanDeg * 0.6f, GreatLeanDeg, acc) : 0f;
                if (!Main.dedServ && counter % 2 == 0) {
                    SpawnTipMote(sense);
                }
            }
            else {
                //落点:放出残影,过冲两帧回坐后几何冻结
                if (!shotFired) {
                    shotFired = true;
                    edgeFlashTicks = EdgeFlashFrames * Updates;
                    if (Main.myPlayer == Projectile.owner) {
                        FirePhantom(owner);
                    }
                }
                float sinceLand = Frame - BurstFrac * swingFrames;
                float settle = MathHelper.Clamp(sinceLand / SettleFrames, 0f, 1f);
                offDeg = startDeg + OvershootDeg * (1f - settle);
                float fade = MathHelper.Clamp((p - BurstFrac) / (RibbonFadeEnd - BurstFrac), 0f, 1f);
                ribbonAlpha = 1f - fade;
                lean = Great ? GreatLeanDeg * (1f - fade) : 0f;
            }

            Projectile.rotation = aim + sense * MathHelper.ToRadians(offDeg);
            if (p >= GatherFrac) {
                RecordRibbon(sense);
            }
            if (Great) {
                ApplyLean(owner, lean);
            }
            if (p >= 1f) {
                Finish(owner);
                return false;
            }
            return true;
        }

        private void FirePhantom(Player owner) {
            Vector2 aimUnit = aim.ToRotationVector2();
            Vector2 spawnPos = owner.MountedCenter + aimUnit * 40f;
            float mult = Great ? OldHeroBlade.GreatSlashDamageMult : OldHeroBlade.PhantomDamageMult;
            Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), spawnPos, aimUnit * owner.HeldItem.shootSpeed, ModContent.ProjectileType<HeroPhantomSlash>(),
                (int)(Projectile.damage * mult), Projectile.knockBack, owner.whoAmI, Great ? 1f : 0f);
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
            while (odr.Count > RibbonSamples) {
                odr.RemoveAt(0);
            }
        }

        private void SpawnTipMote(int sense) {
            Vector2 tangent = (Projectile.rotation + sense * MathHelper.PiOver2).ToRotationVector2();
            Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * Reach * Main.rand.NextFloat(0.7f, 1f);
            Vector2 vel = tangent * Main.rand.NextFloat(2f, 5f) + CEUtils.randomPointInCircle(1.2f);
            PRTLoader.NewParticle<PRT_Light>(pos, vel, HeroPhantomSlash.SteelColor, Main.rand.NextFloat(0.3f, 0.5f)).Configure(0.8f, lifetime: 12);
        }
        #endregion

        #region 碰撞与命中
        public override bool? CanDamage() {
            float p = Frame / swingFrames;
            return p >= GatherFrac && Frame <= BurstFrac * swingFrames + SettleFrames;
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
            OldHeroBlade.TrySummonSpirits(owner, target, owner.HeldItem);
            if (!hitStopUsed) {
                hitStopUsed = true;
                stopTicks = (Great ? GreatHitStopFrames : HitStopFrames) * Updates;
            }
            if (Main.dedServ) {
                return;
            }
            Vector2 tangent = (Projectile.rotation + ribbonSense * MathHelper.PiOver2).ToRotationVector2();
            ScreenShaker.AddShake(new ScreenShaker.ScreenShake(tangent, Great ? GreatShakeAmp : ShakeAmp));
            CEUtils.PlaySound("SwordHit" + Main.rand.Next(2), Main.rand.NextFloat(0.85f, 1.05f), target.Center, 6, 0.6f * CEUtils.WeapSound);
            for (int i = 0; i < 14; i++) {
                Vector2 vel = tangent.RotatedByRandom(0.45f) * Main.rand.NextFloat(14f, 28f);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, HeroPhantomSlash.SteelColor * 1.4f, Projectile.scale * 0.055f).Configure(false, 14, new Vector2(0.3f, 1f));
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, HeroPhantomSlash.SteelColor, Great ? 2.6f : 1.8f).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 8);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White, Great ? 1.5f : 1f).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 6);
            if (Great) {
                PRTLoader.NewParticle<PRT_PulseRing>(target.Center, Vector2.Zero, HeroPhantomSlash.SteelColor, 0.1f).Configure(1.6f, 10);
            }
        }
        #endregion

        #region 绘制
        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor) {
            Player owner = Owner;
            //主刀光:深钢蓝到冷白的扫掠体,噪声随挥舞方向滚动
            DrawRibbon(RibbonInnerFrac, 1f, ribbonAlpha, CEExtraAssets.MotionTrail2, CEExtraAssets.TurbulentNoise, SteelColor1, SteelColor2, 12f * ribbonSense);
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

            if (Great) {
                //昔日之斩:刀体外一圈缓转的冷白描边,标记这一击不同
                VaultUtils.DrawRotatingMarginEffect(Main.spriteBatch, tex, (int)counter, drawPos, null, SteelColor2 * 0.8f, rot, origin, drawScale, effect);
            }
            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, rot, origin, drawScale, effect);
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
