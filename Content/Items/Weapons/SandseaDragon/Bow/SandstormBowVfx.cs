using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Bow
{
    /// <summary>沙尘暴一族的表现,纯本地,各端各自播放,不回写 gameplay</summary>
    internal static class SandstormBowVfx
    {
        public static readonly Color Sand = new Color(212, 192, 100);       //禁戒沙黄,原版沙尘龙卷的颜色
        public static readonly Color SandWarm = new Color(226, 150, 70);    //禁戒橙,龙卷根部偏向它
        private static readonly Color PuffDark = new Color(131, 95, 57);    //沙烟暗部,取自 Boss 调色板

        private const float DustScaleMin = 0.9f;        //沙粒缩放范围
        private const float DustScaleMax = 1.4f;
        private const int GlowDustChance = 3;           //每这么多粒沙里有一粒换成发光的沙尘龙卷尘
        private const float PuffOpacity = 0.55f;        //沙烟不透明度
        private const float PuffSpin = 0.03f;           //沙烟自转上限,弧度每帧

        //放箭
        private const float ShotPitch = -0.1f;          //普通箭的弓弦音高偏移
        private const float GiantShotPitch = -0.35f;    //巨箭的弓弦音高偏移,更沉
        private const float ShotPitchVariance = 0.12f;
        private const float ShotVolume = 0.9f;
        private const int ShotMaxInstances = 3;         //同时发声上限,26 帧一发
        private const float WhooshPitchMin = 0.72f;     //巨箭破风声的音高范围,1 为原速
        private const float WhooshPitchMax = 0.86f;
        private const float WhooshVolume = 0.45f;
        private const int ReleaseDustCount = 8;         //放巨箭时弓口喷出的沙粒数
        private const float ReleaseDustSpread = 0.28f;  //沙粒相对瞄准方向的散角,弧度
        private const float ReleaseDustSpeedMin = 3f;   //沙粒初速范围,像素每帧
        private const float ReleaseDustSpeedMax = 9f;
        private const float ReleaseDustRadius = 6f;     //沙粒起点散布半径,像素
        private const float ReleasePuffSpeed = 2.2f;    //弓口沙烟初速,像素每帧
        private const float ReleasePuffScale = 0.4f;    //弓口沙烟缩放,原图 80 像素
        private const int ReleasePuffLife = 22;         //弓口沙烟寿命,帧

        //拉弦时沙尘往巨箭上聚
        private const int GatherChance = 2;             //每帧出尘的概率分母
        private const float GatherAlongMin = 6f;        //聚拢点沿箭杆离箭扣的距离范围,像素
        private const float GatherAlongMax = 50f;
        private const float GatherRadius = 16f;         //尘的起点离箭杆的距离,像素
        private const float GatherPull = 0.14f;         //每帧朝箭杆靠拢的比例
        private const float GatherScale = 0.8f;

        //巨箭飞行
        private const float ArrowLight = 0.35f;         //巨箭自带的微光强度,暗处也看得见箭身
        private const int FlightDustChance = 2;         //每次更新出尘的概率分母
        private const float FlightDustBackMin = 10f;    //尘落在箭头后方的距离范围,像素
        private const float FlightDustBackMax = 44f;
        private const float FlightDustRadius = 7f;      //尘离箭杆的散布半径,像素
        private const float FlightDustCarry = 0.3f;     //尘继承箭速的比例

        //巨箭命中
        private const float BurstPitchMin = 1.5f;       //碎裂声的音高范围,1 为原速
        private const float BurstPitchMax = 1.9f;
        private const float BurstVolume = 0.3f;
        private const int BurstMaxInstances = 4;
        private const int BurstDustCount = 14;          //溅出的沙粒数
        private const float BurstSpread = 0.5f;         //打中敌怪时顺箭势的散角,弧度
        private const float BurstSpreadBounce = 1.1f;   //撞物块时往回溅的散角,弧度
        private const float BurstSpeedMin = 2f;         //沙粒初速范围,像素每帧
        private const float BurstSpeedMax = 7f;
        private const int BurstPuffCount = 3;           //沙烟团数
        private const float BurstPuffRadius = 8f;       //沙烟起点散布半径,像素
        private const float BurstPuffSpeedMin = 0.8f;   //沙烟初速范围,像素每帧
        private const float BurstPuffSpeedMax = 2.6f;
        private const float BurstPuffScaleMin = 0.4f;   //沙烟缩放范围,原图 80 像素
        private const float BurstPuffScaleMax = 0.65f;
        private const int BurstPuffLife = 30;           //沙烟寿命,帧
        private const float DissolveRadius = 5f;        //尾迹散成尘时的散布半径,像素
        private const float DissolveSpeed = 1.2f;       //散尘初速上限,像素每帧
        private const float DissolveScale = 1f;

        //沙龙卷
        private const float TwisterVolume = 0.45f;      //成形声音量,一次命中两道龙卷只响一声
        private const float TwisterPitchVariance = 0.2f;
        private const int TwisterMaxInstances = 3;
        private const int HitDustCount = 8;             //卷到敌人时扬起的沙粒数
        private const float HitDustSpeedMin = 2f;       //沙粒顺龙卷行进方向的初速范围,像素每帧
        private const float HitDustSpeedMax = 6f;
        private const float HitDustLiftMin = 1f;        //沙粒向上的初速范围,像素每帧
        private const float HitDustLiftMax = 5f;
        //以下照原版 aiStyle 127 的扬尘,横纵取值都是占半宽、半高的比例
        private const float TwisterDustXMin = 0.1f;
        private const float TwisterDustXMax = 1f;
        private const float TwisterDustYTop = -0.5f;
        private const float TwisterDustYBottom = 0.9f;
        private const float TwisterDustFlareTop = 2.2f; //越靠上散得越开
        private const float TwisterDustFlareBottom = 0.6f;
        private const float TwisterDustWidthRatio = 0.2f;   //扬尘范围宽占柱高的比例
        private const float TwisterDustInner = -1.2f;   //比它更靠里的尘才带横向初速
        private const float TwisterDustFadeIn = 1f;
        private const float TwisterDustScale = 0.3f;
        private const float TwisterDustDriftMin = 1f;   //横向初速范围,像素每帧
        private const float TwisterDustDriftMax = 2f;
        private const float TwisterDustRiseMin = 1f;    //上升初速范围,像素每帧
        private const float TwisterDustRiseMax = 1.5f;

        /// <summary>弓弦声,巨箭另加破风声与弓口的沙</summary>
        public static void Release(Vector2 muzzle, Vector2 aim, bool giant) {
            if (Main.dedServ) {
                return;
            }
            float pitch = giant ? GiantShotPitch : ShotPitch;
            SoundEngine.PlaySound(SoundID.Item5 with { Pitch = pitch, PitchVariance = ShotPitchVariance, Volume = ShotVolume, MaxInstances = ShotMaxInstances }, muzzle);
            if (!giant) {
                return;
            }
            CEUtils.PlaySound("throw", Main.rand.NextFloat(WhooshPitchMin, WhooshPitchMax), muzzle, ShotMaxInstances, WhooshVolume);
            for (int i = 0; i < ReleaseDustCount; i++) {
                Vector2 velocity = aim.RotatedByRandom(ReleaseDustSpread) * Main.rand.NextFloat(ReleaseDustSpeedMin, ReleaseDustSpeedMax);
                Vector2 start = muzzle + Main.rand.NextVector2Circular(ReleaseDustRadius, ReleaseDustRadius);
                Dust dust = Dust.NewDustPerfect(start, DustID.Sand, velocity, 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
                dust.noGravity = true;
            }
            SandPuff(muzzle, aim * ReleasePuffSpeed, ReleasePuffScale, ReleasePuffLife);
        }

        /// <summary>拉弦期间每帧调用,沙尘朝弦上的巨箭聚拢</summary>
        public static void GatherSand(Vector2 nock, Vector2 aim) {
            if (Main.dedServ || !Main.rand.NextBool(GatherChance)) {
                return;
            }
            Vector2 onShaft = nock + aim * Main.rand.NextFloat(GatherAlongMin, GatherAlongMax);
            Vector2 start = onShaft + Main.rand.NextVector2CircularEdge(GatherRadius, GatherRadius);
            Dust dust = Dust.NewDustPerfect(start, DustID.Sandnado, (onShaft - start) * GatherPull, 0, default, GatherScale);
            dust.noGravity = true;
        }

        /// <summary>巨箭每次更新调用,微光与箭杆后的散沙</summary>
        public static void ArrowFlight(Projectile arrow) {
            if (Main.dedServ) {
                return;
            }
            Lighting.AddLight(arrow.Center, Sand.ToVector3() * ArrowLight);
            if (!Main.rand.NextBool(FlightDustChance)) {
                return;
            }
            Vector2 back = -arrow.velocity.SafeNormalize(Vector2.Zero);
            Vector2 start = arrow.Center + back * Main.rand.NextFloat(FlightDustBackMin, FlightDustBackMax) + Main.rand.NextVector2Circular(FlightDustRadius, FlightDustRadius);
            int type = Main.rand.NextBool(GlowDustChance) ? DustID.Sandnado : DustID.Sand;
            Dust dust = Dust.NewDustPerfect(start, type, arrow.velocity * FlightDustCarry, 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
            dust.noGravity = true;
        }

        /// <summary>巨箭碎成沙。撞物块时沙往回溅,打中敌怪时顺着箭势穿过去</summary>
        public static void ArrowBurst(Vector2 position, Vector2 travel, bool bounce) {
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(BurstPitchMin, BurstPitchMax), position, BurstMaxInstances, BurstVolume);
            Vector2 spray = bounce ? -travel : travel;
            float spread = bounce ? BurstSpreadBounce : BurstSpread;
            for (int i = 0; i < BurstDustCount; i++) {
                Vector2 velocity = spray.RotatedByRandom(spread) * Main.rand.NextFloat(BurstSpeedMin, BurstSpeedMax);
                int type = Main.rand.NextBool(GlowDustChance) ? DustID.Sandnado : DustID.Sand;
                Dust.NewDustPerfect(position, type, velocity, 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
            }
            for (int i = 0; i < BurstPuffCount; i++) {
                Vector2 velocity = spray.RotatedByRandom(spread) * Main.rand.NextFloat(BurstPuffSpeedMin, BurstPuffSpeedMax);
                Vector2 start = position + Main.rand.NextVector2Circular(BurstPuffRadius, BurstPuffRadius);
                SandPuff(start, velocity, Main.rand.NextFloat(BurstPuffScaleMin, BurstPuffScaleMax), BurstPuffLife);
            }
        }

        /// <summary>巨箭消失时把尾迹上的每个位置散成尘,尾迹不跟着箭一起凭空没掉</summary>
        public static void TrailDissolve(Vector2[] oldPositions, Vector2 half) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 1; i < oldPositions.Length; i++) {
                if (oldPositions[i] == Vector2.Zero) {
                    continue;
                }
                Vector2 start = oldPositions[i] + half + Main.rand.NextVector2Circular(DissolveRadius, DissolveRadius);
                Dust dust = Dust.NewDustPerfect(start, DustID.Sandnado, Main.rand.NextVector2Circular(DissolveSpeed, DissolveSpeed), 0, default, DissolveScale);
                dust.noGravity = true;
            }
        }

        /// <summary>龙卷成形声,原版沙尘龙卷生成时响的就是 Item82</summary>
        public static void TwisterSpawn(Vector2 position) {
            if (Main.dedServ) {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item82 with { Volume = TwisterVolume, PitchVariance = TwisterPitchVariance, MaxInstances = TwisterMaxInstances }, position);
        }

        /// <summary>龙卷成形期间每帧调用,绕柱心旋的扬尘</summary>
        public static void TwisterDust(Vector2 center, float height) {
            if (Main.dedServ) {
                return;
            }
            float along = Main.rand.NextFloat();
            Vector2 unit = new Vector2(MathHelper.Lerp(TwisterDustXMin, TwisterDustXMax, Main.rand.NextFloat()), MathHelper.Lerp(TwisterDustYTop, TwisterDustYBottom, along));
            unit.X *= -MathHelper.Lerp(TwisterDustFlareTop, TwisterDustFlareBottom, along);
            Vector2 half = new Vector2(height * TwisterDustWidthRatio, height) * 0.5f;
            Dust dust = Dust.NewDustPerfect(center + half * unit, DustID.Sandnado);
            //customData 给柱心,原版这种尘会朝它横向加速
            dust.customData = center;
            dust.fadeIn = TwisterDustFadeIn;
            dust.scale = TwisterDustScale;
            if (unit.X > TwisterDustInner) {
                dust.velocity.X = Main.rand.NextFloat(TwisterDustDriftMin, TwisterDustDriftMax);
            }
            dust.velocity.Y = -Main.rand.NextFloat(TwisterDustRiseMin, TwisterDustRiseMax);
        }

        /// <summary>龙卷卷到敌人,沙从它身上顺着龙卷的行进方向扬起</summary>
        public static void TwisterHit(Rectangle target, int direction) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < HitDustCount; i++) {
                Vector2 start = new Vector2(Main.rand.NextFloat(target.Left, target.Right), Main.rand.NextFloat(target.Top, target.Bottom));
                Vector2 velocity = new Vector2(direction * Main.rand.NextFloat(HitDustSpeedMin, HitDustSpeedMax), -Main.rand.NextFloat(HitDustLiftMin, HitDustLiftMax));
                Dust.NewDustPerfect(start, DustID.Sand, velocity, 0, default, Main.rand.NextFloat(DustScaleMin, DustScaleMax));
            }
        }

        //沙烟走 Alpha 混合并受环境光,不发光
        private static void SandPuff(Vector2 position, Vector2 velocity, float scale, int life) {
            Color color = Color.Lerp(PuffDark, Sand, Main.rand.NextFloat());
            PRTLoader.NewParticle<PRT_HeavySmokeCal>(position, velocity, color, scale)
                .Configure(PuffOpacity, life, Main.rand.NextFloat(-PuffSpin, PuffSpin), false, 0f, false, true);
        }
    }
}
