using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.SweetRain
{
    /// <summary>
    /// 甘霖法杖的雷雨云,按住期间的状态都在它身上
    /// <br/>ai[0] 状态 0 下雨 / 1 消散,只由拥有者端改写并 netUpdate
    /// <br/>扣魔与生成雨滴只在拥有者端;位置各端按同步的鼠标各自平滑跟随,不逐帧同步
    /// </summary>
    public class SweetRainCloud : ModProjectile
    {
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/SandseaDragon/SweetRain/SweetRainCloud_Bolt")]
        internal static Asset<Texture2D> BoltTex;

        public const float StateRaining = 0f;           //ai[0] 下雨
        public const float StateDissipating = 1f;       //ai[0] 消散

        //云体与雨幕,策划给 24×10 格的云、18 格的雨
        public const int FrameWidth = 384;              //云图宽,像素
        public const int FrameHeight = 160;             //云图单帧高,像素
        public const int FrameCount = 4;                //云图竖排帧数
        public const int FrameTicks = 10;               //每帧停留,帧
        public const float RainWidth = 288f;            //雨幕宽,像素
        public const float RainLineY = 64f;             //雨线在云心下方多远,像素,落在云底垂包里
        public const float RainLineJitter = 6f;         //雨线上下抖动,像素
        public const float MouseBelowRainLine = 24f;    //鼠标在雨线下方多远,像素;云悬在鼠标上方
        public const int HitboxSize = 64;               //屏幕裁剪的占位框,像素,不打人

        //下雨
        public const float DropsPerSecond = 36f;        //每秒雨滴数
        public const float DropsPerFrame = DropsPerSecond / 60f;    //按 60 帧每秒折算
        public const float DropSpeed = 11f;             //雨滴落速,像素/帧
        public const float DropSpeedJitter = 1f;        //落速随机幅度,像素/帧
        public const float GoldenStep = 0.618034f;      //横向落点的黄金分割步长,雨不扎堆
        public const float DriftInherit = 0.3f;         //雨滴继承云横移的比例
        public const float DriftMax = 4f;               //雨滴横向速度上限,像素/帧
        public const int RainDelay = 8;                 //成形后多少帧开始下雨
        public const float RainProbeStep = 16f;         //雨线被物块挡住时每步下探,像素,一格
        public const int RainProbeCount = 3;            //最多探几步(含原位),最远到鼠标下方 8 像素

        //跟随、淡入淡出与绘制
        public const float FollowLerp = 0.14f;          //每帧向目标靠拢的比例
        public const int FadeInFrames = 14;             //淡入帧数
        public const int FadeOutFrames = 22;            //淡出帧数
        public const int HoldFrames = 2;                //每帧续给玩家的使用帧
        public const float BodyMinLight = 0.65f;        //云体亮度下限 0-1
        public const float BobAmplitude = 2f;           //上下浮动幅度,像素
        public const float BobSpeed = 0.045f;           //浮动角速度,弧度/帧

        //闪电,纯表现不造成伤害
        public const int FlashIntervalMin = 110;        //两次闪电最短间隔,帧
        public const int FlashIntervalMax = 280;        //最长间隔,帧
        public const int FirstFlashMin = 40;            //成形后首次闪电最早,帧
        public const int FirstFlashMax = 140;           //首次闪电最晚,帧
        public const int FlashFrames = 16;              //一次闪电总帧数
        public const int FlashFirstEnd = 3;             //第一下亮到第几帧
        public const int FlashGapEnd = 6;               //两下之间暗到第几帧
        public const int FlashSecondEnd = 9;            //第二下亮到第几帧,之后线性熄灭
        public const float FlashGapLevel = 0.25f;       //两下之间的亮度 0-1
        public const float FlashSecondLevel = 0.8f;     //第二下的亮度 0-1
        public const float FlashOverlay = 0.8f;         //云体加色叠层的强度 0-1
        public const int BoltFrameCount = 3;            //闪电图竖排帧数
        public const float BoltRangeX = 60f;            //闪电中心的横向范围,像素,保证整道闪电落在云的轮廓里
        public const float BoltMinY = 0f;               //闪电中心相对云心的纵向下限,像素
        public const float BoltMaxY = 12f;              //纵向上限,像素
        public const int BoltSparks = 5;                //闪电处的电火花数
        public const float BoltSparkRadius = 24f;       //电火花散布半径,像素
        public const float BoltSparkSpeed = 2f;         //电火花初速上限,像素/帧
        public const float BoltSparkScale = 0.6f;       //电火花缩放
        public const float ThunderVolume = 0.3f;        //雷声音量 0-1
        public const int ThunderInstances = 2;          //雷声同时实例上限

        //云絮尘
        public const int WispInterval = 7;              //下雨时每几帧飘一粒
        public const int FadeWispChance = 2;            //消散时每帧 1/N 概率飘一粒
        public const float WispRangeX = 170f;           //云絮出生的横向范围,像素
        public const float WispMinY = 10f;              //云絮出生相对云心的纵向下限,像素
        public const float WispMaxY = 66f;              //纵向上限,像素
        public const float WispSpeed = 0.8f;            //云絮横向初速上限,像素/帧
        public const float WispRise = 0.5f;             //云絮上飘初速上限,像素/帧
        public const int FormPuffs = 16;                //成形时聚拢的尘数
        public const int DissipatePuffs = 22;           //消散时散开的尘数
        public const float PuffRingX = 160f;            //聚散尘的横向分布半径,像素
        public const float PuffRingY = 58f;             //纵向分布半径,像素
        public const float PuffRingInner = 0.5f;        //分布内圈占半径的比例 0-1
        public const float PuffSpeedMin = 1.5f;         //聚散尘初速下限,像素/帧
        public const float PuffSpeedMax = 3.5f;         //初速上限,像素/帧
        public const float PuffScaleMin = 1.2f;         //尘缩放下限
        public const float PuffScaleMax = 1.8f;         //尘缩放上限
        public const int PuffAlpha = 110;               //尘透明度 0-255

        //持杖
        public const float FaceDeadzone = 8f;           //云在头顶正上方这么宽以内不改朝向,像素,防来回翻面
        public const float StaffTipDistance = 66f;      //杖头离玩家中心,像素,从贴图量得
        public const float StaffTipAngle = 0.28f;       //杖头偏离瞄准线的角度,弧度,从贴图量得
        public const int TipDustInterval = 5;           //杖头每几帧冒一粒光尘
        public const float TipDustRise = 1.2f;          //光尘上飘初速,像素/帧
        public const int TipDustAlpha = 100;            //光尘透明度 0-255
        public const float TipDustScale = 0.9f;         //光尘缩放

        private static readonly Vector3 CloudGlow = new(0.07f, 0.2f, 0.22f);    //云底常亮微光
        private static readonly Vector3 FlashGlow = new(0.5f, 0.95f, 1f);       //闪电照明
        private static readonly Color FlashTint = new(190, 255, 255, 0);        //A 为 0 即加色
        private static readonly SoundStyle ThunderSound = SoundID.Thunder with { Volume = ThunderVolume, MaxInstances = ThunderInstances, Type = SoundType.Sound };

        private float fade;             //不透明度 0-1,各端本地推进
        private bool fading;            //本端已进入消散
        private int age;                //本端存活帧
        private int manaTimer;          //距上次扣魔帧数,仅拥有者端
        private float dropCarry;        //不足一滴的累积,仅拥有者端
        private float rainPhase;        //横向落点序列相位 0-1,仅拥有者端
        private Vector2 drift;          //本帧位移,像素
        private int flashTimer;         //闪电剩余帧
        private int nextFlash;          //距下次闪电帧数
        private int boltFrame;          //本次闪电用第几帧图
        private Vector2 boltOffset;     //本次闪电相对云心的位置
        private bool boltFlip;          //本次闪电是否水平翻转

        /// <summary>鼠标世界坐标对应的云心,雨线在鼠标上方 <see cref="MouseBelowRainLine"/></summary>
        public static Vector2 CenterForMouse(Vector2 mouseWorld) => mouseWorld - new Vector2(0f, RainLineY + MouseBelowRainLine);

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = FrameCount;
            ProjectileID.Sets.DontAttachHideToAlpha[Projectile.type] = true;    //hide 的弹幕默认按玩家处取光,云要按自己的位置
        }

        public override void SetDefaults() {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;     //全局钩子按 friendly 认孪生复制件,伤害由 CanDamage 关掉
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.timeLeft = HoldFrames;
        }

        public override bool? CanDamage() => false;

        public override bool? CanCutTiles() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCs.Add(index);  //压在物块上,让在敌怪、弹幕与玩家后面
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead) {
                Projectile.Kill();
                return;
            }
            bool owned = Projectile.owner == Main.myPlayer;
            if (owned && Projectile.ai[0] == StateRaining && !KeepRaining(player)) {
                Projectile.ai[0] = StateDissipating;
                Projectile.netUpdate = true;
            }

            if (Projectile.ai[0] == StateRaining) {
                Projectile.timeLeft = HoldFrames;
                fade = Math.Min(1f, fade + 1f / FadeInFrames);
                Follow(player);
                HoldStaff(player);
                if (owned && age >= RainDelay)
                    SpawnRain();
            }
            else {
                if (!fading) {
                    fading = true;
                    Projectile.timeLeft = FadeOutFrames;
                    if (!Main.dedServ)
                        PuffBurst(DissipatePuffs, true);
                }
                fade = Math.Max(0f, fade - 1f / FadeOutFrames);
                drift = Vector2.Zero;
            }

            if (!Main.dedServ)
                UpdateVisuals(player);
            age++;
        }

        //仅拥有者端;松手、换物品、被控或魔力不足时返回 false
        private bool KeepRaining(Player player) {
            if (!player.channel || player.noItems || player.CCed || player.HeldItem.type != ModContent.ItemType<SweetRainStaff>())
                return false;
            if (++manaTimer < SweetRainStaff.ManaTickFrames)
                return true;
            manaTimer = 0;
            if (!player.CheckMana(player.HeldItem, -1, true))
                return false;
            player.manaRegenDelay = (int)player.maxRegenDelay;
            return true;
        }

        private void Follow(Player player) {
            Vector2 before = Projectile.Center;
            Projectile.Center = Vector2.Lerp(before, CenterForMouse(player.mouseWorld()), FollowLerp);
            drift = Projectile.Center - before;
        }

        //各端都跑,远端玩家的持杖姿势也靠这里
        private void HoldStaff(Player player) {
            Vector2 aim = Projectile.Center - player.MountedCenter;
            if (Math.Abs(aim.X) > FaceDeadzone)
                player.ChangeDir(aim.X > 0f ? 1 : -1);
            player.SetDummyItemTime(HoldFrames);
            player.itemRotation = MathF.Atan2(aim.Y * player.direction, aim.X * player.direction);
        }

        private void SpawnRain() {
            dropCarry += DropsPerFrame;
            while (dropCarry >= 1f) {
                dropCarry -= 1f;
                rainPhase = (rainPhase + GoldenStep) % 1f;
                Vector2 pos = Projectile.Center + new Vector2((rainPhase - 0.5f) * RainWidth, RainLineY + Main.rand.NextFloat(-RainLineJitter, RainLineJitter));
                //雨线埋在实心物块里时往下探几步找空处,矮顶下也能淋到鼠标处的敌怪;探不到就不出这一滴
                Vector2 half = new Vector2(SweetRainDrop.HitWidth, SweetRainDrop.HitHeight) * 0.5f;
                int probe = 0;
                while (probe < RainProbeCount && Collision.SolidCollision(pos - half, SweetRainDrop.HitWidth, SweetRainDrop.HitHeight)) {
                    pos.Y += RainProbeStep;
                    probe++;
                }
                if (probe >= RainProbeCount)
                    continue;
                Vector2 vel = new Vector2(MathHelper.Clamp(drift.X * DriftInherit, -DriftMax, DriftMax), DropSpeed + Main.rand.NextFloat(-DropSpeedJitter, DropSpeedJitter));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, vel, ModContent.ProjectileType<SweetRainDrop>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
        }

        private void UpdateVisuals(Player player) {
            if (age == 0) {
                nextFlash = Main.rand.Next(FirstFlashMin, FirstFlashMax);
                PuffBurst(FormPuffs, false);
            }
            if (++Projectile.frameCounter >= FrameTicks) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            bool raining = Projectile.ai[0] == StateRaining;
            if (flashTimer > 0)
                flashTimer--;
            if (raining && fade >= 1f && --nextFlash <= 0)
                StartFlash();
            Lighting.AddLight(Projectile.Center + new Vector2(0f, RainLineY), CloudGlow * fade + FlashGlow * (FlashLevel() * fade));

            if (raining) {
                if (age % WispInterval == 0)
                    SpawnWisp(new Vector2(Main.rand.NextFloat(-WispSpeed, WispSpeed) + drift.X * DriftInherit, -Main.rand.NextFloat(WispRise)));
                if (age % TipDustInterval == 0) {
                    Vector2 hand = player.MountedCenter;
                    float angle = (Projectile.Center - hand).ToRotation() - StaffTipAngle * player.direction;
                    Dust tip = Dust.NewDustPerfect(hand + angle.ToRotationVector2() * StaffTipDistance, DustID.MagicMirror, new Vector2(0f, -TipDustRise), TipDustAlpha, default, TipDustScale);
                    tip.noGravity = true;
                }
            }
            else if (fade > 0f && Main.rand.NextBool(FadeWispChance)) {
                SpawnWisp(new Vector2(Main.rand.NextFloat(-WispSpeed, WispSpeed), -Main.rand.NextFloat(WispRise, PuffSpeedMin)));
            }
        }

        private void StartFlash() {
            flashTimer = FlashFrames;
            nextFlash = Main.rand.Next(FlashIntervalMin, FlashIntervalMax);
            boltFrame = Main.rand.Next(BoltFrameCount);
            boltOffset = new Vector2(Main.rand.NextFloat(-BoltRangeX, BoltRangeX), Main.rand.NextFloat(BoltMinY, BoltMaxY));
            boltFlip = Main.rand.NextBool();
            SoundEngine.PlaySound(ThunderSound, Projectile.Center);
            for (int i = 0; i < BoltSparks; i++) {
                Dust spark = Dust.NewDustPerfect(Projectile.Center + boltOffset + Main.rand.NextVector2Circular(BoltSparkRadius, BoltSparkRadius), DustID.Electric, Main.rand.NextVector2Circular(BoltSparkSpeed, BoltSparkSpeed), 0, default, BoltSparkScale);
                spark.noGravity = true;
            }
        }

        //闪电包络 0-1,亮、暗、再亮、线性熄灭
        private float FlashLevel() {
            if (flashTimer <= 0)
                return 0f;
            int elapsed = FlashFrames - flashTimer;
            if (elapsed < FlashFirstEnd)
                return 1f;
            if (elapsed < FlashGapEnd)
                return FlashGapLevel;
            if (elapsed < FlashSecondEnd)
                return FlashSecondLevel;
            return FlashSecondLevel * flashTimer / (FlashFrames - FlashSecondEnd);
        }

        private bool BoltVisible() {
            int elapsed = FlashFrames - flashTimer;
            return flashTimer > 0 && (elapsed < FlashFirstEnd || (elapsed >= FlashGapEnd && elapsed < FlashSecondEnd));
        }

        private void SpawnWisp(Vector2 velocity) {
            Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-WispRangeX, WispRangeX), Main.rand.NextFloat(WispMinY, WispMaxY));
            Dust wisp = Dust.NewDustPerfect(pos, DustID.RainCloud, velocity, PuffAlpha, default, Main.rand.NextFloat(PuffScaleMin, PuffScaleMax));
            wisp.noGravity = true;
        }

        //成形时从外圈朝云心聚拢,消散时朝外散开
        private void PuffBurst(int count, bool outward) {
            for (int i = 0; i < count; i++) {
                Vector2 unit = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 offset = new Vector2(unit.X * PuffRingX, unit.Y * PuffRingY) * Main.rand.NextFloat(PuffRingInner, 1f);
                Vector2 velocity = (outward ? unit : -unit) * Main.rand.NextFloat(PuffSpeedMin, PuffSpeedMax);
                Dust puff = Dust.NewDustPerfect(Projectile.Center + offset, DustID.RainCloud, velocity, PuffAlpha, default, Main.rand.NextFloat(PuffScaleMin, PuffScaleMax));
                puff.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            if (fade <= 0f)
                return false;
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = new Rectangle(0, FrameHeight * Projectile.frame, FrameWidth, FrameHeight);
            Vector2 origin = new Vector2(FrameWidth, FrameHeight) * 0.5f;
            float bob = MathF.Sin(age * BobSpeed) * BobAmplitude;
            Vector2 pos = (Projectile.Center - Main.screenPosition + new Vector2(0f, bob)).Floor();
            Color body = Color.Lerp(lightColor, Color.White, BodyMinLight) * fade;
            Main.EntitySpriteDraw(tex, pos, frame, body, 0f, origin, Projectile.scale, SpriteEffects.None, 0);

            float flash = FlashLevel() * fade;
            if (flash <= 0f)
                return false;
            Main.EntitySpriteDraw(tex, pos, frame, FlashTint * (flash * FlashOverlay), 0f, origin, Projectile.scale, SpriteEffects.None, 0);
            if (BoltVisible() && BoltTex != null) {
                Texture2D bolt = BoltTex.Value;
                int boltHeight = bolt.Height / BoltFrameCount;
                Rectangle boltRect = new Rectangle(0, boltHeight * boltFrame, bolt.Width, boltHeight);
                SpriteEffects flip = boltFlip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Main.EntitySpriteDraw(bolt, pos + boltOffset * Projectile.scale, boltRect, Color.White * fade, 0f, boltRect.Size() * 0.5f, Projectile.scale, flip, 0);
            }
            return false;
        }
    }
}
