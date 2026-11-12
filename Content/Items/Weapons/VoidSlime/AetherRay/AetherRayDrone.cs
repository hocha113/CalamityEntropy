using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.AetherRay
{
    /// <summary>
    /// 以太射线的浮游炮,HoldItem 在使用中生成,一人一架,不占召唤栏,不接触伤害;收起武器 2 秒后消失
    /// ai[0] 目标 NPC 序号,-1 为无,拥有者每 10 帧补找最近的,换了才同步;开火请求只在拥有者端,由物品 Shoot 置位
    /// 过载时停火飞回身边,光带接到枪顶能量仓;每帧逻辑只在最后一次更新跑,位移自己做
    /// </summary>
    public class AetherRayDrone : ModProjectile
    {
        #region 调参旋钮
        public const int NoTarget = -1;                 //ai[0] 无目标
        public const float TargetRange = 1600f;         //像素,策划 100 格,从玩家量
        public const int RetargetInterval = 10;         //帧
        public const float HoverDistance = 180f;        //像素,悬停点离目标边缘,朝玩家一侧
        public const float HoverLift = 60f;             //像素,悬停点上抬
        public const float IdleBack = 34f;              //像素,无目标时在肩后
        public const float IdleLift = 40f;
        public const float OverloadBack = 22f;          //像素,过载时贴在身边
        public const float OverloadLift = 54f;
        public const float FollowRate = 0.14f;          //每帧向停位靠拢的比例
        public const float MaxStep = 30f;               //像素每帧
        public const float SnapDistance = 2400f;        //像素,离太远直接闪回
        public const float BobAmplitude = 4f;           //像素,悬浮起伏
        public const float BobRate = 2.6f;              //弧度每秒
        public const float TurnRate = 0.3f;             //弧度每帧
        public const int LingerFrames = 120;            //帧,策划不再手持约 2 秒后消失
        public const int FadeFrames = 20;               //帧,消失前淡出
        public const int KeepAliveFrames = 3;
        public const int HitboxSize = 24;               //像素
        public const float OverloadBlendRate = 0.15f;   //过载外观每帧趋近比例
        public const float MinVisible = 0.01f;
        //以下在 AetherRayDrone.png(30×26,镜头朝 +X)上量得,相对贴图中心
        public const float MuzzleForward = 13f;         //镜头前缘,激光弹从这里出
        public const float LensX = 7f;
        public const float LensY = 1f;
        public const float ThrusterX = -14f;
        public const float ThrusterY = 0f;
        //供能光带
        public const float RibbonHalfWidth = 5f;        //像素
        public const float RibbonCoreRatio = 0.4f;      //白芯相对半宽
        public const float RibbonWave = 6f;             //像素,摆幅
        public const float RibbonWaveLength = 70f;      //像素
        public const float RibbonFlow = 9f;             //弧度每秒
        public const float RibbonSpacing = 8f;          //像素,折线点距
        public const float RibbonIntensity = 0.9f;
        public const float RibbonHueSpan = 1f;          //光带从头到尾跨过的色相圈数
        //绘制与粒子
        public const float BodySelfLight = 0.35f;       //机身至少保留的亮度比例
        public const float LensGlowSize = 24f;          //像素
        public const float LensGlowIdle = 0.5f;
        public const float FlashDecay = 0.8f;           //开火闪光每帧保留
        public const float FlashGrow = 0.8f;            //开火时镜头光晕放大比例
        public const float ThrusterGlowSize = 18f;      //像素
        public const float ThrusterGlowOpacity = 0.6f;
        public const float LightStrength = 0.4f;
        public const int ThrusterMoteInterval = 6;      //帧
        public const float ThrusterMoteSpeed = 1.5f;    //像素每帧
        public const float ThrusterMoteMinScale = 0.15f;
        public const float ThrusterMoteMaxScale = 0.25f;
        public const int ThrusterMoteLife = 16;         //帧
        public const float SpawnVolume = 0.45f;
        public const int SoundInstances = 2;
        public static readonly Color LensColor = new(85, 138, 255);
        public static readonly Color ThrusterColor = new(136, 35, 159);
        public static readonly Color RibbonCore = new(223, 248, 255);
        #endregion

        private static readonly List<Vector2> ribbonPoints = new();

        private int age;
        private int linger;         //各端自数,未手持武器的帧数
        private int retargetTimer;  //仅拥有者
        private bool pendingShot;   //仅拥有者,Shoot 置位,下一次逻辑消费
        private float flash;
        private float overloadVisual;

        private bool Flipped => Math.Abs(MathHelper.WrapAngle(Projectile.rotation)) > MathHelper.PiOver2;

        public static AetherRayDrone Find(int owner) {
            int type = ModContent.ProjectileType<AetherRayDrone>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == owner && p.type == type && p.ModProjectile is AetherRayDrone drone) {
                    return drone;
                }
            }
            return null;
        }

        /// <summary>物品 Shoot 调用,朋友盒同帧多调一次也只打一发</summary>
        public static void RequestShot(int owner) {
            AetherRayDrone drone = Find(owner);
            if (drone != null) {
                drone.pendingShot = true;
            }
        }

        /// <summary>激光弹在各端首次更新时调用,别的客户端也看得到镜头闪光</summary>
        public static void OnBoltFired(int owner) {
            AetherRayDrone drone = Find(owner);
            if (drone != null) {
                drone.flash = 1f;
            }
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = HitboxSize;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = KeepAliveFrames;
        }

        public override void OnSpawn(IEntitySource source) {
            Projectile.ai[0] = NoTarget;
        }

        public override void AI() {
            Projectile.timeLeft = KeepAliveFrames;
            if (!Projectile.FinalExtraUpdate()) {
                return;
            }
            age++;
            Player player = Main.player[Projectile.owner];
            bool owner = Projectile.owner == Main.myPlayer;
            bool held = player.active && !player.dead && player.HeldItem.type == ModContent.ItemType<AetherRay>();
            linger = held ? 0 : linger + 1;
            if (!player.active || (owner && (player.dead || linger >= LingerFrames))) {
                Projectile.Kill();
                return;
            }
            AetherRayHeld gun = held ? AetherRayHeld.Find(Projectile.owner) : null;
            bool overloaded = gun != null && gun.Overloaded;
            overloadVisual = MathHelper.Lerp(overloadVisual, overloaded ? 1f : 0f, OverloadBlendRate);
            NPC target = FindTarget(player, owner);
            Vector2 goal = overloaded ? Anchor(player, OverloadBack, OverloadLift)
                : target != null ? HoverPoint(player, target)
                : Anchor(player, IdleBack, IdleLift);
            Move(goal);
            float heading = overloaded ? Projectile.AngleTo(gun.CorePoint)
                : target != null ? Projectile.AngleTo(target.Center)
                : player.direction > 0 ? 0f : MathHelper.Pi;
            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, heading, TurnRate);
            if (owner && pendingShot) {
                pendingShot = false;
                if (!overloaded && target != null) {
                    Fire(player, target);
                }
            }
            flash *= FlashDecay;
            Projectile.Opacity = MathHelper.Clamp((LingerFrames - linger) / (float)FadeFrames, 0f, 1f);
            Lighting.AddLight(Projectile.Center, LensColor.ToVector3() * LightStrength * Projectile.Opacity);
            if (!Main.dedServ) {
                Visuals();
            }
        }

        private NPC FindTarget(Player player, bool owner) {
            NPC current = Valid((int)Projectile.ai[0], player);
            if (!owner) {
                return current;
            }
            retargetTimer++;
            if (current != null && retargetTimer < RetargetInterval) {
                return current;
            }
            retargetTimer = 0;
            NPC next = CEUtils.FindTarget_HomingProj(Projectile, player.Center, TargetRange);
            float index = next?.whoAmI ?? NoTarget;
            if (index != Projectile.ai[0]) {
                Projectile.ai[0] = index;
                Projectile.netUpdate = true;
            }
            return next;
        }

        private NPC Valid(int index, Player player) {
            if (index < 0 || index >= Main.maxNPCs) {
                return null;
            }
            NPC npc = Main.npc[index];
            return npc.CanBeChasedBy(Projectile) && player.DistanceSQ(npc.Center) <= TargetRange * TargetRange ? npc : null;
        }

        private static Vector2 HoverPoint(Player player, NPC target) {
            Vector2 away = (player.Center - target.Center).SafeNormalize(-Vector2.UnitY);
            float edge = Math.Max(target.width, target.height) * 0.5f;
            return target.Center + away * (HoverDistance + edge) - Vector2.UnitY * HoverLift;
        }

        private static Vector2 Anchor(Player player, float back, float lift) {
            return player.Center + new Vector2(-player.direction * back, -lift * player.gravDir);
        }

        private void Move(Vector2 goal) {
            goal.Y += MathF.Sin(Main.GlobalTimeWrappedHourly * BobRate + Projectile.identity) * BobAmplitude;
            Vector2 delta = goal - Projectile.Center;
            if (delta.LengthSquared() > SnapDistance * SnapDistance) {
                Projectile.Center = goal;
                Projectile.velocity = Vector2.Zero;
                return;
            }
            Vector2 step = delta * FollowRate;
            if (step.LengthSquared() > MaxStep * MaxStep) {
                step = step.SafeNormalize(Vector2.Zero) * MaxStep;
            }
            Projectile.velocity = step;
            Projectile.Center += step;
        }

        private void Fire(Player player, NPC target) {
            Item item = player.HeldItem;
            float travel = Projectile.Distance(target.Center) / (AetherRayBolt.Speed * AetherRayBolt.UpdatesPerFrame);
            Vector2 dir = (target.Center + target.velocity * travel - Projectile.Center).SafeNormalize(Vector2.UnitX * player.direction);
            //激光弹从这里继承暴击,开火前刷成当前武器暴击
            Projectile.CritChance = player.GetWeaponCrit(item);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + dir * MuzzleForward, dir * AetherRayBolt.Speed,
                ModContent.ProjectileType<AetherRayBolt>(), player.GetWeaponDamage(item), player.GetWeaponKnockback(item), Projectile.owner);
            Projectile.rotation = dir.ToRotation();
        }

        private Vector2 Part(float x, float y) {
            Vector2 forward = Projectile.rotation.ToRotationVector2();
            float flip = Flipped ? -1f : 1f;
            return Projectile.Center + forward * x + forward.RotatedBy(MathHelper.PiOver2) * (y * flip);
        }

        private void Visuals() {
            if (age == 1) {
                CEUtils.PlaySound("CogflyActive", 1f, Projectile.Center, SoundInstances, SpawnVolume);
            }
            if (age % ThrusterMoteInterval == 0) {
                Vector2 back = -Projectile.rotation.ToRotationVector2();
                PRTLoader.NewParticle<PRT_Light>(Part(ThrusterX, ThrusterY), back * ThrusterMoteSpeed, ThrusterColor,
                    Main.rand.NextFloat(ThrusterMoteMinScale, ThrusterMoteMaxScale)).Configure(Projectile.Opacity, lifetime: ThrusterMoteLife);
            }
        }

        public override bool? CanDamage() {
            return false;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            float opacity = Projectile.Opacity;
            if (overloadVisual > MinVisible && AetherRayHeld.Find(Projectile.owner) is AetherRayHeld gun) {
                DrawRibbon(Part(LensX, LensY), gun.CorePoint, opacity * overloadVisual);
            }
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Color body = Color.Lerp(lightColor, Color.White, BodySelfLight) * opacity;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, body, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, Flipped ? SpriteEffects.FlipVertically : SpriteEffects.None);

            Texture2D glow = GodDraw.Glow;
            Color lens = Color.Lerp(LensColor, AetherRayBeam.RainbowColor(0f), overloadVisual);
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, Part(LensX, LensY), GodDraw.Add(lens, (LensGlowIdle + flash) * opacity), 0f, LensGlowSize * (1f + FlashGrow * flash) / glow.Width);
            GodDraw.Centered(glow, Part(ThrusterX, ThrusterY), GodDraw.Add(ThrusterColor, ThrusterGlowOpacity * opacity), 0f, ThrusterGlowSize / glow.Width);
            GodDraw.End();
            return false;
        }

        //两端钉住、中段摆动的彩虹光带,外层色相沿线流动,里层白芯
        private static void DrawRibbon(Vector2 from, Vector2 to, float opacity) {
            Vector2 delta = to - from;
            float length = delta.Length();
            if (length < RibbonSpacing) {
                return;
            }
            Vector2 normal = (delta / length).RotatedBy(MathHelper.PiOver2);
            int count = (int)(length / RibbonSpacing) + 2;
            float time = Main.GlobalTimeWrappedHourly * RibbonFlow;
            ribbonPoints.Clear();
            for (int i = 0; i < count; i++) {
                float t = i / (float)(count - 1);
                float wave = MathF.Sin(t * length / RibbonWaveLength * MathHelper.TwoPi - time) * RibbonWave * MathF.Sin(t * MathHelper.Pi);
                ribbonPoints.Add(from + delta * t + normal * wave);
            }
            GodDraw.BeginAdditive();
            GodDraw.Strip(GodDraw.SoftLine, ribbonPoints, t => RibbonHalfWidth, t => GodDraw.Add(AetherRayBeam.RainbowColor(t * RibbonHueSpan), RibbonIntensity * opacity));
            GodDraw.Strip(GodDraw.SoftLine, ribbonPoints, t => RibbonHalfWidth * RibbonCoreRatio, t => GodDraw.Add(RibbonCore, RibbonIntensity * opacity));
            GodDraw.End();
        }
    }
}
