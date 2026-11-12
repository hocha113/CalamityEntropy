using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.AetherRay
{
    /// <summary>
    /// 以太射线的伽玛射线,Shoot 每次使用生成上下两道;不飞行,每次更新按同步鼠标重算发射口与方向,撞墙截断
    /// ai[0] 发射口,-1 上 1 下;ai[1] 1 为接上一对、不再展开;localAI[0] 已过更新数,各端自数
    /// 判伤 36 次更新,同侧来了更年轻的一道就当场让位,停火则不判伤、收窄淡出;寿命与无敌帧都按更新计,豆奶下仍无缝
    /// </summary>
    public class AetherRayBeam : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        #region 调参旋钮
        public static readonly int[] Sides = { -1, 1 };    //ai[0] 取值,上、下发射口
        public const int NoTwin = -1;                   //twinSpawnIndex 缺省,不是朋友盒复制
        public const int DamageUpdates = 36;            //更新,策划射线存在 36 帧
        public const int HitCooldown = 12;              //更新,策划 12 点无敌帧
        public const int FadeUpdates = 8;               //更新,停火后收窄淡出
        public const int OpenUpdates = 4;               //更新,头一对射线展开
        public const int KeepAlive = 2;                 //每次更新续上的 timeLeft,寿命由逻辑裁决
        public const int HitboxSize = 16;               //像素,只占位,判定走 Colliding
        public const float MaxLength = 1600f;           //像素,任务约 100 格
        public const float ScanStep = 8f;               //像素,撞墙探测步长
        public const float ScanRefine = 2f;             //像素,探到墙后回退细化
        public const float BeamWidth = 16f;             //像素,可见宽度
        public const float HitWidth = 22f;              //像素,判定宽度,比可见略宽
        public const float OverloadDamageMult = 1.2f;   //策划过载 120%
        //束形与暗鞘同虚空电场
        public const float StartCap = 0.02f;            //起点渐隐占全长
        public const float OpenEndCap = 0.24f;          //打空时末端渐隐占全长
        public const float OpenEndTaper = 0.3f;         //打空时末端宽度比
        public const float BlockedEndCap = 0.015f;
        public const float BlockedEndTaper = 0.85f;
        public const float UnderlayWidthMult = 1.9f;
        public const float UnderlayOpacity = 0.45f;
        public const float SeedSpread = 0.37f;          //两侧湍流种子错开
        public const float MinVisible = 0.01f;          //低于此不透明度不画
        //彩虹配色取原版最终棱镜 GetLastPrismHue 默认分支,六道各取色相 k/6
        public const int StrandCount = 6;
        public const float PrismSaturation = 1f;
        public const float PrismLuminance = 0.5f;
        public const float HueFlowRate = 0.5f;          //色相每秒流过的圈数
        public const float StrandAmplitude = 0.55f;     //绞绕摆幅占束宽
        public const float StrandWave = 150f;           //像素,一个绞绕周期的长度
        public const float StrandSpin = 7f;             //弧度每秒,绞绕流动
        public const float StrandHalfWidth = 2.5f;      //像素
        public const float StrandSpacing = 16f;         //像素,折线点距
        public const float StrandIntensity = 0.9f;
        public const float StrandBackDim = 0.3f;        //转到背面的股剩余亮度
        public const float StrandCap = 0.04f;           //起点渐隐占全长
        //光点
        public const float FlareSize = 44f;             //发射口光晕直径,像素,随束宽缩放
        public const float EndFlareSize = 56f;          //撞墙光晕直径,像素
        public const float FlareIntensity = 0.8f;
        public const float CoreFlareRatio = 0.45f;      //白核相对外晕
        public const float LightStrength = 0.5f;
        //撞墙火花与沿线光尘,每帧一次
        public const int WallSparkChance = 3;           //每帧 1/3
        public const float WallSparkSpread = 1.1f;      //弧度
        public const float WallSparkMinSpeed = 2f;      //像素每帧
        public const float WallSparkMaxSpeed = 6f;
        public const float WallSparkScale = 0.45f;
        public const int WallSparkLife = 12;            //帧
        public const int MoteChance = 4;                //每帧 1/4
        public const float MoteDrift = 1.2f;            //像素每帧,横向漂开
        public const float MoteMinScale = 0.25f;
        public const float MoteMaxScale = 0.4f;
        public const int MoteLife = 14;                 //帧
        public const float MoteSpin = 0.1f;             //弧度每帧
        public const float MoteBloom = 1.3f;
        public const int HitSparks = 3;
        public const float HitSparkSpread = 0.6f;       //弧度,顺射线方向散开
        public const float HitSparkMinSpeed = 3f;       //像素每帧
        public const float HitSparkMaxSpeed = 8f;
        public const float HitSparkScale = 0.5f;
        public const int HitSparkLife = 12;             //帧
        public static readonly Vector2 SparkSquash = new(0.3f, 1f);
        public static readonly Color BeamColor = new(85, 138, 255);
        public static readonly Color CoreColor = new(223, 248, 255);
        public static readonly Color UnderlayColor = new(18, 4, 36);
        #endregion

        private static readonly List<Vector2> strandPoints = new();

        private Vector2 start;
        private Vector2 end;
        private float length;
        private bool blocked;
        private bool overloaded;

        private int Side => SideOf(Projectile);
        private bool Chained => Projectile.ai[1] > 0f;
        private ref float Age => ref Projectile.localAI[0];

        private static int SideOf(Projectile p) => p.ai[0] < 0f ? -1 : 1;

        public static Color RainbowColor(float offset) {
            float hue = (Main.GlobalTimeWrappedHourly * HueFlowRate + offset) % 1f;
            return Main.hslToRgb(hue, PrismSaturation, PrismLuminance);
        }

        /// <summary>Shoot 生成前调用,同侧还有在判伤的旧射线时新射线直接满宽接上</summary>
        public static bool CanChain(int owner, int side, int twin) {
            int type = ModContent.ProjectileType<AetherRayBeam>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == type && p.owner == owner && SideOf(p) == side && p.localAI[0] <= DamageUpdates && p.Entropy().IndexOfTwistedTwinShootedThisProj == twin) {
                    return true;
                }
            }
            return false;
        }

        public static bool AnyAlive(int owner) {
            int type = ModContent.ProjectileType<AetherRayBeam>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == type && p.owner == owner && p.Entropy().IndexOfTwistedTwinShootedThisProj == NoTwin) {
                    return true;
                }
            }
            return false;
        }

        private static bool HasYounger(int owner, int side, int twin, float age) {
            int type = ModContent.ProjectileType<AetherRayBeam>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == type && p.owner == owner && SideOf(p) == side && p.localAI[0] < age && p.Entropy().IndexOfTwistedTwinShootedThisProj == twin) {
                    return true;
                }
            }
            return false;
        }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)MaxLength;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.timeLeft = KeepAlive;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = KeepAlive;
            Age++;
            //在 AI 里销毁的这次更新不再判伤,交接当帧不叠打
            int twin = Projectile.Entropy().IndexOfTwistedTwinShootedThisProj;
            if (Age > DamageUpdates + FadeUpdates || HasYounger(Projectile.owner, Side, twin, Age)) {
                Projectile.Kill();
                return;
            }
            UpdateGeometry(player);
            overloaded = AetherRayHeld.IsOverloaded(Projectile.owner);
            if (!Main.dedServ && length > 0f && Projectile.FinalExtraUpdate()) {
                Visuals();
            }
        }

        private void UpdateGeometry(Player player) {
            Vector2 aim = AetherRayHeld.AimOf(player);
            start = AetherRayHeld.EmitterPoint(player, aim, Side);
            //发射口戳进墙里就不出光
            blocked = true;
            length = Collision.CanHitLine(player.MountedCenter, 0, 0, start, 0, 0) ? Scan(start, aim, out blocked) : 0f;
            end = start + aim * length;
            Projectile.Center = start;
            Projectile.velocity = aim;
            Projectile.rotation = aim.ToRotation();
        }

        private static float Scan(Vector2 from, Vector2 dir, out bool hitWall) {
            for (float d = 0f; d < MaxLength; d += ScanStep) {
                float next = Math.Min(d + ScanStep, MaxLength);
                if (Solid(from + dir * next)) {
                    while (d + ScanRefine < next && !Solid(from + dir * (d + ScanRefine))) {
                        d += ScanRefine;
                    }
                    hitWall = true;
                    return d;
                }
            }
            hitWall = false;
            return MaxLength;
        }

        private static bool Solid(Vector2 world) {
            Point p = world.ToTileCoordinates();
            return WorldGen.InWorld(p.X, p.Y) && WorldGen.SolidOrSlopedTile(p.X, p.Y);
        }

        private float Envelope() {
            float open = Chained ? 1f : Math.Min(Age / OpenUpdates, 1f);
            float fade = Age <= DamageUpdates ? 1f : 1f - (Age - DamageUpdates) / FadeUpdates;
            return MathHelper.Clamp(Math.Min(open, fade), 0f, 1f);
        }

        private Color CurrentColor() => Color.Lerp(BeamColor, RainbowColor(0f), AetherRayHeld.OverloadBlend(Projectile.owner));

        private void Visuals() {
            Color color = CurrentColor();
            DelegateMethods.v3_1 = color.ToVector3() * LightStrength * Envelope();
            Utils.PlotTileLine(start, end, BeamWidth, DelegateMethods.CastLight);
            Vector2 dir = Projectile.velocity;
            if (blocked && Main.rand.NextBool(WallSparkChance)) {
                Vector2 vel = (-dir).RotatedByRandom(WallSparkSpread) * Main.rand.NextFloat(WallSparkMinSpeed, WallSparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(end, vel, color, WallSparkScale).Configure(true, WallSparkLife, SparkSquash, true);
            }
            if (Main.rand.NextBool(MoteChance)) {
                Vector2 at = start + dir * Main.rand.NextFloat(length);
                Vector2 drift = dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-MoteDrift, MoteDrift);
                PRTLoader.NewParticle<PRT_SparkleCal>(at, drift, CoreColor, Main.rand.NextFloat(MoteMinScale, MoteMaxScale)).Configure(color, MoteLife, MoteSpin, MoteBloom);
            }
        }

        public override bool? CanDamage() => Age <= DamageUpdates && length > 0f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(start, end, targetHitbox, (int)(HitWidth * Projectile.scale));
        }

        public override void CutTiles() {
            if (length <= 0f) {
                return;
            }
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(start, end, BeamWidth * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (overloaded) {
                modifiers.SourceDamage *= OverloadDamageMult;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Main.dedServ) {
                return;
            }
            Vector2 dir = Projectile.velocity;
            Vector2 at = start + dir * MathHelper.Clamp(Vector2.Dot(target.Center - start, dir), 0f, length);
            Color color = CurrentColor();
            for (int i = 0; i < HitSparks; i++) {
                Vector2 vel = dir.RotatedByRandom(HitSparkSpread) * Main.rand.NextFloat(HitSparkMinSpeed, HitSparkMaxSpeed);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(at, vel, color, HitSparkScale).Configure(false, HitSparkLife, SparkSquash, true);
            }
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            float env = Envelope();
            if (length <= 1f || env < MinVisible) {
                return false;
            }
            float blend = AetherRayHeld.OverloadBlend(Projectile.owner);
            Color main = Color.Lerp(BeamColor, RainbowColor(0f), blend);
            float width = BeamWidth * Projectile.scale * env;
            float widthEnd = width * (blocked ? BlockedEndTaper : OpenEndTaper);
            float capEnd = blocked ? BlockedEndCap : OpenEndCap;
            float seed = Side * SeedSpread;
            //亮背景上先压一层暗鞘,加法束才不被冲成白线;着色器缺失时不画
            if (VDBeamDraw.BeginTapered(UnderlayColor, UnderlayColor, UnderlayOpacity, seed, BlendState.AlphaBlend)) {
                VDBeamDraw.TaperedQuad(start, end, width * UnderlayWidthMult, widthEnd * UnderlayWidthMult, 1f, capStart: StartCap, capEnd: capEnd);
                VDBeamDraw.EndTapered();
            }
            VDBeamDraw.DrawTapered(start, end, width, widthEnd, main, CoreColor, 1f, 1f, seed, endGlow: false, capStart: StartCap, capEnd: capEnd);
            GodDraw.BeginAdditive();
            if (blend > MinVisible) {
                DrawStrands(width, capEnd, blend);
            }
            DrawFlares(main, Projectile.scale * env, env);
            GodDraw.End();
            return false;
        }

        private void DrawStrands(float width, float capEnd, float blend) {
            Vector2 dir = Projectile.velocity;
            Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);
            int count = Math.Max(2, (int)(length / StrandSpacing) + 1);
            float time = Main.GlobalTimeWrappedHourly * StrandSpin;
            float hueShift = Main.GlobalTimeWrappedHourly * HueFlowRate;
            float amplitude = width * StrandAmplitude;
            float halfWidth = StrandHalfWidth * Projectile.scale;
            float len = length;
            for (int k = 0; k < StrandCount; k++) {
                float phase = MathHelper.TwoPi * k / StrandCount;
                strandPoints.Clear();
                for (int i = 0; i < count; i++) {
                    float d = len * i / (count - 1);
                    strandPoints.Add(start + dir * d + normal * (MathF.Sin(Twist(d, time, phase)) * amplitude));
                }
                Color hue = Main.hslToRgb(((float)k / StrandCount + hueShift) % 1f, PrismSaturation, PrismLuminance);
                GodDraw.Strip(GodDraw.SoftLine, strandPoints, t => halfWidth,
                    t => GodDraw.Add(hue, StrandIntensity * blend * Edge(t, capEnd) * Depth(t * len, time, phase)));
            }
        }

        private static float Twist(float distance, float time, float phase) => distance / StrandWave * MathHelper.TwoPi - time + phase;

        //股转到主束背后时变暗,看起来是绕着主束绞
        private static float Depth(float distance, float time, float phase) {
            return MathHelper.Lerp(StrandBackDim, 1f, (MathF.Cos(Twist(distance, time, phase)) + 1f) * 0.5f);
        }

        private static float Edge(float t, float capEnd) => Math.Min(GodDraw.Span(t, 0f, StrandCap), GodDraw.Span(1f - t, 0f, capEnd));

        private void DrawFlares(Color main, float scale, float env) {
            Texture2D glow = GodDraw.Glow;
            Color outer = GodDraw.Add(main, FlareIntensity * env);
            Color inner = GodDraw.Add(CoreColor, FlareIntensity * env);
            GodDraw.Centered(glow, start, outer, 0f, FlareSize * scale / glow.Width);
            GodDraw.Centered(glow, start, inner, 0f, FlareSize * CoreFlareRatio * scale / glow.Width);
            if (blocked) {
                GodDraw.Centered(glow, end, outer, 0f, EndFlareSize * scale / glow.Width);
                GodDraw.Centered(glow, end, inner, 0f, EndFlareSize * CoreFlareRatio * scale / glow.Width);
            }
        }
    }
}
