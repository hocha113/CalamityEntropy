using CalamityEntropy.Core.CalamityRef;
using Terraria;

namespace CalamityEntropy.Content.NPCs.Acropolis.Core
{
    /// <summary>
    /// 卫城机器的唯一数字出口。状态与宿主里不许出现裸数字(纯局部插值系数除外)。
    /// <para>
    /// 本文件是 2026-09-17 状态机重构时从原 <c>AcropolisMachine.AI()</c> / <c>AttackPlayer()</c>
    /// 逐个搬出来的,<b>数值一律照搬,没有一处调整</b>。注释写的是「这个数在原代码里干什么」,
    /// 不是「这个数为什么该是这样」,原作者没留依据的地方不替他编理由。
    /// 唯一例外是 2026-09-18 按手感反馈改过的腾空腿姿(<see cref="LegAirExtendFraction"/> 一组),注释里写明了改的理由。
    /// </para>
    /// </summary>
    internal static class AcropolisDirector
    {
        //==================== 难度系数 enrange ====================

        /// <summary>
        /// 难度系数。原代码每帧在 <c>AttackPlayer</c> 开头重算一次。
        /// 基数是<b>血量</b>:满血 1.0,濒死 2.0;随后专家 +0.07、大师 +0.07、复仇 +0.1、死亡 +0.1,
        /// 熵灾模式先 +0.4 再 ×1.15,getGood ×1.3,天顶 ×0.88。
        /// <para>
        /// 加法项先累完再乘乘法项,顺序不可换。它直接乘进速度、射速与冷却流速,
        /// 所以每个来源都必须在各端一致,这里全是世界级已同步量加上已同步的 life。
        /// </para>
        /// <para>装灾厄读复仇/死亡,缺席仍走专家/大师兜底。勿连带改熵灾那一项。</para>
        /// </summary>
        public static float Enrange(NPC npc) {
            float enrange = 1 + (1 - (float)npc.life / npc.lifeMax);
            if (Main.expertMode) {
                enrange += 0.07f;
            }
            if (Main.masterMode) {
                enrange += 0.07f;
            }
            if (CECal.IsRevengeance) {
                enrange += 0.1f;
            }
            if (CECal.IsDeathMode) {
                enrange += 0.1f;
            }
            if (CalamityEntropy.EntropyMode) {
                enrange += 0.4f;
                enrange *= 1.15f;
            }
            if (Main.getGoodWorld) {
                enrange *= 1.3f;
            }
            if (Main.zenithWorld) {
                enrange *= 0.88f;
            }
            return enrange;
        }

        //==================== 全局 / 形态 ====================

        /// <summary>血量低于这个比例就自我晋升成 Boss(原 <c>life / lifeMax &lt; 0.98f</c>)</summary>
        public const float BossPromoteLifeRatio = 0.98f;

        /// <summary>目标距离超过它就算脱战(原 <c>Distance(NPC.Center) &lt; 6000</c> 的补集)</summary>
        public const float DisengageDistance = 6000f;

        /// <summary>脱战累计帧数,到点直接 <c>NPC.active = false</c>(原 dcounter &gt; 295)</summary>
        public const int DespawnFrames = 295;

        /// <summary>脱战漂移:每帧向右加速、贴到实心块就上浮、否则下坠</summary>
        public const float DriftAccelX = 0.25f;
        public const float DriftUp = -0.4f;
        public const float DriftDown = 0.7f;

        /// <summary>
        /// 状态总龄上限。原代码<b>没有</b>任何超时兜底,这是重构时新加的纯安全网:
        /// 最长的实战状态是 200 帧的炮击,跳跃最长 180 帧,1200 帧在正常对局里到不了,
        /// 存在的意义只是不让状态机死在某个状态里、Boss 靠惯性飘走
        /// </summary>
        public const int StateTimeoutFrames = 1200;

        //==================== 身体物理(背景行为,任何状态下都跑)====================

        /// <summary>腾空时的重力(乘 scale),落地时改为整体阻尼</summary>
        public const float JumpGravity = 0.4f;
        public const float GroundDrag = 0.97f;

        /// <summary>未晋升形态:普通重力 + 贴地清零 + 地面横向阻尼</summary>
        public const float DummyGravity = 0.4f;
        public const float DummyGroundDragX = 0.96f;
        /// <summary>未晋升且腾空时,机体按横速微微倾斜</summary>
        public const float DummyTiltFactor = 0.04f;
        public const float DummyTiltRate = 0.16f;

        /// <summary>碰撞探测盒的高度倍率(原 <c>NPC.height * 1.4f</c>)</summary>
        public const float GroundProbeHeightScale = 1.4f;

        //==================== 朝向 ====================

        /// <summary>腾空时朝向直接由横速推出的倾角(乘 0.04)</summary>
        public const float AirRotationFactor = 0.04f;
        /// <summary>左右腿落点连线的最大倾角</summary>
        public const float MaxTerrainTiltDegrees = 60f;
        /// <summary>按地形倾角转向的速率</summary>
        public const float TerrainRotateRate = 0.12f;
        /// <summary>只有一侧腿着地时的原地旋转量</summary>
        public const float SingleSideSpinRate = 0.16f;
        /// <summary>两侧腿数相同时归正朝向的速率</summary>
        public const float FacingSnapRate = 0.3f;
        /// <summary>着地腿不足三条时归正朝向的速率</summary>
        public const float FacingFallbackRate = 0.12f;
        /// <summary>判定「站住了」所需的着地腿数</summary>
        public const int LegsOnTileForGround = 3;

        //==================== 跨招冷却 TeslaCD(背景行为)====================

        /// <summary>出场初值</summary>
        public const float TeslaCDInit = 120f;
        /// <summary>骰点:1/6 出炮击,再 1/6 出跳射,其余单发</summary>
        public const int BarrageRollDenominator = 6;
        public const int JumpRollDenominator = 6;
        /// <summary>抽中炮击或跳射之后的冷却</summary>
        public const float TeslaCDAfterSpecial = 360f;
        /// <summary>单发之后的冷却</summary>
        public const float TeslaCDAfterShot = 160f;

        //==================== 单发电球(Walk 自带的空档招)====================

        public const float SingleShotSpread = 0.1f;
        public const float SingleShotSpeed = 6f;
        /// <summary>单发的炮臂反冲(乘 dir)</summary>
        public const float SingleShotRecoil = 0.2f;

        //==================== 炮口常态瞄准(未出招时的默认声明)====================

        /// <summary>常态瞄点抬高量</summary>
        public const float IdleAimRise = -14f;
        /// <summary>超过这个距离开始按平方补抛物线落差</summary>
        public const float IdleAimDropDistance = 500f;
        public const float IdleAimDropFactor = 0.02f;

        //==================== 炮击 CannonBarrage(原 CannonUpAtk)====================

        /// <summary>本段总帧数(原 <c>CannonUpAtk = 200</c> 每帧自减)</summary>
        public const int BarrageFrames = 200;
        /// <summary>前 60 帧只抬炮不开火(原 <c>CannonUpAtk &lt; 140</c>)</summary>
        public const int BarrageFireStartFrame = 60;
        /// <summary>开火节拍,按 enrange 流逝</summary>
        public const float BarrageInterval = 12f;
        /// <summary>瞄点:玩家头顶 800(高抛)</summary>
        public const float BarrageAimRise = -800f;
        public const float BarrageSpread = 0.6f;
        public const float BarrageSpeed = 5f;
        /// <summary>炮臂反冲(乘 dir)</summary>
        public const float BarrageRecoil = 0.06f;
        /// <summary>弹幕 ai0:高抛弹标记</summary>
        public const float BarrageProjAi0 = -1f;

        //==================== 跳射 JumpShoot(原 Jumping + JumpAndShoot)====================

        /// <summary>起跳初速:横向 12(与 scale 无关,原式先除后乘抵消)、纵向 -24 乘 scale</summary>
        public const float JumpLaunchSpeedX = 12f;
        public const float JumpLaunchSpeedY = -24f;
        /// <summary>起跳时写入的跳跃冷却</summary>
        public const int JumpShootJumpCD = 200;
        /// <summary>跳射计数初值,每帧自减,归零前一直开火</summary>
        public const int JumpShootFrames = 200;
        /// <summary>起跳时把炮口节拍设成 30,所以第一发比常规慢</summary>
        public const float JumpShootTeslaUpInit = 30f;
        /// <summary>跳射的开火节拍</summary>
        public const float JumpShootInterval = 23f;
        /// <summary>瞄点:本体炮臂根部正下方 220(对地倾泻)</summary>
        public const float JumpShootAimDrop = 220f;
        public const float JumpShootSpread = 0.03f;
        public const float JumpShootSpeed = 3f;
        /// <summary>弹幕 ai0:对地弹标记</summary>
        public const float JumpShootProjAi0 = 1f;
        /// <summary>原代码对这一发连调两次 PointAPos,等于转向速率翻倍,照搬</summary>
        public const int JumpShootAimTimes = 2;

        //==================== 追高跳 Leap(原 JumpCD &lt;= -260 那一支)====================

        /// <summary>玩家高出本体这么多才考虑追高</summary>
        public const float LeapHeightGate = 200f;
        /// <summary>跳跃冷却降到这个值以下才允许追高(从 200 数到 -260 要 460 帧)</summary>
        public const int LeapReadyJumpCD = -260;
        /// <summary>追高初速:横向按水平差 0.01 比例,纵向按垂直差 0.08 比例但不超过 -30</summary>
        public const float LeapSpeedXFactor = 0.01f;
        public const float LeapSpeedYFactor = 0.08f;
        public const float LeapSpeedYMax = -30f;
        /// <summary>追高之后写入的跳跃冷却</summary>
        public const int LeapJumpCD = 160;

        //==================== 落地判定(背景行为)====================

        /// <summary>跳射计数降到这个值以下才允许落地,等于起跳后 50 帧内不许收</summary>
        public const int JumpEndCounterGate = 150;
        /// <summary>跳跃冷却降到这个值以下无条件收招</summary>
        public const int JumpEndJumpCD = 20;
        /// <summary>踩到实心/平台且下坠速度超过它也收招</summary>
        public const float JumpEndFallSpeed = 8f;

        //==================== 地面悬停与行走(背景行为)====================

        /// <summary>悬停参考点相对本体的抬高量</summary>
        public const float HoverYOffset = -90f;
        /// <summary>本体已经压到玩家下方时的下坠限速与额外阻尼</summary>
        public const float HoverFallClamp = 2f;
        public const float HoverFallDamp = 0.84f;
        /// <summary>高度差超过它用满推力,低于死区则停推并额外阻尼</summary>
        public const float HoverFarDistance = 150f;
        public const float HoverDeadZone = 14f;
        public const float HoverDeadZoneDamp = 0.8f;
        public const float HoverThrustNear = 0.2f;
        public const float HoverThrustFar = 1f;
        /// <summary>高度差小于这个值就不再调整(悬停闸)</summary>
        public const float HoverGate = 20f;
        /// <summary>玩家在下方时的下压加速度</summary>
        public const float HoverDownAccel = 0.4f;
        /// <summary>玩家在上方且腿没被地形卡住时的上抬加速度</summary>
        public const float HoverUpAccel = -0.6f;
        /// <summary>腿垂得过低时反而下压,避免把自己吊在半空</summary>
        public const float HoverPushDownAccel = 2f;
        /// <summary>腿相对本体的「垂得低」与「垂得过低」两条线</summary>
        public const float LegLowThreshold = 110f;
        public const float LegVeryLowThreshold = 130f;
        /// <summary>完全没落脚点时的自由落体</summary>
        public const float FreeFallAccel = 0.42f;
        public const float FreeFallMaxSpeed = 12f;
        /// <summary>离玩家超过它才继续横向推进</summary>
        public const float WalkKeepDistance = 100f;
        public const float WalkAccel = 0.1f;

        //==================== 鱼叉(背景行为,与任何招式并行)====================

        public const float HarpoonCDInit = 120f;
        /// <summary>发射后的再装填冷却</summary>
        public const float HarpoonCDAfterLaunch = 160f;
        /// <summary>冷却归零后每帧的蓄力增量,满 1 即发</summary>
        public const float HarpoonChargeRate = 0.01f;
        /// <summary>蓄力超过它就停止追瞄,等于「锁定即承诺」的预告窗</summary>
        public const float HarpoonAimChargeCap = 0.8f;
        /// <summary>发射初速(乘 scale)</summary>
        public const float HarpoonLaunchSpeed = 36f;
        /// <summary>发射时写给鱼叉的「不回收」帧数</summary>
        public const int HarpoonBackFrames = 40;
        /// <summary>发射时的鱼叉臂反冲(乘 dir)</summary>
        public const float HarpoonRecoil = 0.3f;
        /// <summary>鱼叉绘制与碰撞的枪口前伸与侧向偏移(侧向乘 dir)。骨架里 <c>harpoonMuzzle</c> 骨的偏移,rig.json 与这里要一致</summary>
        public const float HarpoonMuzzleReach = 150f;
        public const float HarpoonMuzzleSide = 10f;

        //==================== 鱼叉拽拉(背景行为,由鱼叉实体触发)====================

        /// <summary>被拽时的固定速度</summary>
        public const float PullSpeed = 40f;
        /// <summary>鱼叉每帧把它刷成 2,所以只要卡着不放就一直拽</summary>
        public const int PullTimerRefill = 2;
        /// <summary>拽拉与解拽时写入的跳跃冷却</summary>
        public const int PullJumpCD = 100;
        /// <summary>拽到这个距离以内就松钩</summary>
        public const float PullReleaseDistance = 400f;
        /// <summary>再次扎墙的冷却</summary>
        public const int PullCooldownFrames = 12 * 60;
        /// <summary>扎墙判定:本体离玩家、本体离鱼叉、鱼叉离玩家的三道门槛</summary>
        public const float StickOwnerToTargetMin = 500f;
        public const float StickOwnerToHarpoonMin = 600f;
        public const float StickHarpoonToTargetMax = 400f;
        /// <summary>扎墙瞬间顺着来速再插进去一段</summary>
        public const float StickPenetration = 40f;
        /// <summary>回收时每帧朝发射口的加速度与阻尼</summary>
        public const float HarpoonReturnAccel = 8f;
        public const float HarpoonReturnDrag = 0.9f;
        public const float HarpoonReturnCatchPad = 6f;
        /// <summary>锁链尾端相对发射口的回退量(骨架里 <c>chainTail</c> 骨的偏移 = 前伸 − 回退)</summary>
        public const float HarpoonChainTail = 72f;

        //==================== 肢体常数(骨架:Rigs2D,骨长 / 挂点 / 步态 / 瞄准参数以 Assets/Rigs/Acropolis.rig.json 为准)====================

        /// <summary>
        /// 四条腿的落点挂点,<b>按骨架腿序</b>:0 内左、1 外左、2 内右、3 外右(原顺序是内左、内右、外左、外右)。
        /// 迁移后只用于未晋升贴体的 Hold 目标;步态的休息位由 rig.json 里髋骨的朝向与 <c>restReach</c> 指向这四个点
        /// </summary>
        public static readonly Vector2[] LegMounts =
        {
            new Vector2(-100f, 120f),
            new Vector2(-140f, 120f),
            new Vector2(100f, 120f),
            new Vector2(140f, 120f),
        };

        /// <summary>焦痕着色器逐件摇随机数的腿序(原绘制顺序:内左、内右、外左、外右),映射到骨架腿序</summary>
        public static readonly int[] LegCharredOrder = { 0, 2, 1, 3 };

        /// <summary>脚掌贴图相对胫节的附加倾角:右腿顺时针、左腿逆时针(原 Draw 里的 ±24°)</summary>
        public const float FootTiltDegrees = 24f;

        /// <summary>炮臂挂点 (-80,-32),x 乘 dir。第一节长 76、第二节(枪口)60 在 rig.json</summary>
        public const float CannonMountX = -80f;
        public const float CannonMountY = -32f;
        /// <summary>鱼叉臂挂点 (60,-18),x 乘 dir。第一节长 66 在 rig.json</summary>
        public const float HarpoonMountX = 60f;
        public const float HarpoonMountY = -18f;

        //手臂追瞄速率 0.06、后坐衰减 0.96 是 rig.json 里四个 PointAt 的 turnRate / angularDamping;
        //原代码第一节相对正下方还有一道 ±50° 的两段式钳制(只在偏离正上方超过 100° 时触发),PointAt 的 maxDeviation 表达不了这种
        //两区间规则,迁移后默认关闭(maxDeviation 0),需要时在 rig.json 里开
        /// <summary>未晋升形态时手臂垂向的虚拟目标与跟随率</summary>
        public const float HandDummyLerp = 0.3f;

        /// <summary>
        /// 腾空姿态(步态 Hold 目标,跟随率 = rig.json holdRate 0.2):每条腿从髋沿「竖直向下、略向体外张开」的方向
        /// 伸到自己全肢触及的这个比例,膝微弯、脚尖朝下,像悬空蜘蛛自然垂下的腿。
        /// 原代码是四足一起收到本体正下方 200、横向只留 0.2 倍挂点偏移;那个点对外腿只有七成多触及,
        /// 大腿被逼成近乎水平的蹲姿。2026-09-18 手感反馈「下落像蹲着,应自然下伸」后改成随各腿触及缩放
        /// </summary>
        public const float LegAirExtendFraction = 0.9f;
        /// <summary>
        /// 腾空张开角:内腿 / 外腿相对竖直向下向体外偏的度数。不能为零:腿正下方伸直时两个膝解左右对称,
        /// 选边只剩浮点噪声决定;有了外偏,膝朝上的偏好才会稳定落在体外那一侧
        /// </summary>
        public const float LegAirSplayInnerDegrees = 10f;
        public const float LegAirSplayOuterDegrees = 18f;
        /// <summary>
        /// 探地起点已没入实心时向上找地表的最大距离,找不到就按原样返回起点。
        /// 原落点搜索里「命中实心后沿 Y 上抬到贴地」那一步的上限;步态自己的探地起点固定在目标上方 probeLift(46),
        /// 迈向陡坡、或行进向带竖直分量时起点会埋进地里,没有这一步足端就落在地下,下一帧落差超过 stepDown 又被迫补步
        /// </summary>
        public const float LegProbePopUp = 64f;
        /// <summary>未晋升且腾空时腿贴着本体的偏移倍率(随本体倾角旋转;跟随率同上)</summary>
        public const float LegDummySpreadX = 0.36f;
        public const float LegDummySpreadY = 2.2f;

        //以下原逐腿落点搜索的参数已由步态求解器接管,只作对照:迈步阈值 100(stepThreshold)、同侧互锁(节律窗相邻两腿为一对)、
        //落点收敛 0.2(holdRate / 摆越 swingMin~swingMax 5~12 帧)、探地含平台(自定义 Probe)

        //==================== 弹幕折算 ====================

        /// <summary>弹幕基础伤害 = <c>NPC.damage / 6.2</c>,击退固定 4,owner 传 -1</summary>
        public const float ProjDamageDivisor = 6.2f;
        public const float ProjKnockback = 4f;

        //==================== 死亡演出 ====================

        /// <summary>死亡倒计时;死亡难度下按帧奇偶多扣 1,天顶强制单速</summary>
        public const int DeathCounterInit = 240;
        /// <summary>充电音的音高按进度线性涨到 3</summary>
        public const float DeathPitchScale = 3f;
        /// <summary>震屏按距离重映射</summary>
        public const float DeathShakeFarDistance = 4000f;
        public const float DeathShakeNearDistance = 1000f;
        public const float DeathShakeMaxPower = 5f;
        /// <summary>充电粒子尺寸(乘 scale)</summary>
        public const float DeathParticleScale = 0.1f;
        /// <summary>死亡爆炸:基础 40,专家 ×2,大师或死亡难度再 ×2,半径 500(乘 scale)</summary>
        public const int DeathBlastDamage = 40;
        public const float DeathBlastRadius = 500f;

        //==================== 掉落与生成 ====================

        /// <summary>地狱层自然生成频率:月后 0.04,困难 0.07,其余 0.18</summary>
        public const float SpawnChancePostMoonlord = 0.04f;
        public const float SpawnChanceHardmode = 0.07f;
        public const float SpawnChancePreHardmode = 0.18f;
    }
}
