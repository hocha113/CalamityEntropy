using CalamityEntropy.Core.AI;

namespace CalamityEntropy.Content.NPCs.VoidSlime.Core
{
    /// <summary>形态号写在 ai[2],和设计文档的 0 到 3 阶段错开一位</summary>
    public static class VSlimeForm
    {
        /// <summary>苍白史莱姆,落地的壳</summary>
        public const int Pale = 1;
        /// <summary>虚空史莱姆</summary>
        public const int Void = 2;
        /// <summary>奇点史莱姆</summary>
        public const int Singularity = 3;
        /// <summary>死兆星,尾杀演出</summary>
        public const int DeathStar = 4;
    }

    /// <summary>状态声明意图,宿主统一落地</summary>
    public enum VSlimeMoveMode
    {
        /// <summary>未声明,指数刹停</summary>
        Hold,
        /// <summary>朝目标点飞,带减速带</summary>
        HoverTo,
        /// <summary>与目标保持相对静止</summary>
        HoldRelative,
        /// <summary>状态自己写速度</summary>
        Direct,
    }

    /// <summary>本体的外形,宿主在相邻两种之间做过渡</summary>
    public enum VSlimeShape
    {
        /// <summary>史莱姆本体</summary>
        Slime,
        /// <summary>飞行时团成的球</summary>
        Orb,
        /// <summary>虫洞</summary>
        Wormhole,
        /// <summary>黑洞,半径读 HoleRadius</summary>
        BlackHole,
        /// <summary>恒星,半径读 StarRadius</summary>
        Star,
    }

    /// <summary>事实字段要过线,声明每帧清回默认,漏声明就回到无害值</summary>
    public class VSlimeStateContext : CEBossStateContext
    {
        public VoidSlime Owner { get; set; }

        #region 事实:过线
        /// <summary>连接段开头选定的下一招,-1 为无</summary>
        public int PendingState { get; set; } = -1;
        /// <summary>侧向 ±1,每手翻转</summary>
        public int SideDir { get; set; } = 1;
        /// <summary>闪现和跃迁的落点</summary>
        public Vector2 AnchorPos { get; set; }
        /// <summary>跃迁的起点</summary>
        public Vector2 LeapFrom { get; set; }
        /// <summary>出手前锁死的点</summary>
        public Vector2 LockedPoint { get; set; }
        /// <summary>第二个锁定点,连段招式存上一拍的落点</summary>
        public Vector2 LockedPoint2 { get; set; }
        /// <summary>出手前锁死的角度,单位是弧度</summary>
        public float LockedAngle { get; set; }
        /// <summary>本招的随机种子,各端用它算同一组偏移</summary>
        public int RollSeed { get; set; }
        /// <summary>招内计数,跳跃次数这类由物理结果决定的次序存这里</summary>
        public int Combo { get; set; }
        /// <summary>终幕开始时奇点黑洞的半径,收缩从它算起,单位是像素</summary>
        public float HeldRadius { get; set; }
        /// <summary>大于 0 正在闪现</summary>
        public int BlinkTimer { get; set; }
        public int NoContactTimer { get; set; }
        /// <summary>引力场已展开</summary>
        public bool ArenaActive { get; set; }
        public Vector2 ArenaCenter { get; set; }
        /// <summary>引力场当前半径,单位是像素</summary>
        public float ArenaRadius { get; set; }
        /// <summary>死亡演出已开始,CheckDead 拦住真死</summary>
        public bool Dying { get; set; }
        public bool DeathFinished { get; set; }
        #endregion

        #region 声明:运动
        public VSlimeMoveMode Mode { get; set; }
        public Vector2 MoveTarget { get; set; }
        public float MoveSpeed { get; set; }
        public float Accel { get; set; } = 0.1f;
        public float SlowRadius { get; set; } = 120f;
        public Vector2 HoldOffset { get; set; }
        public float Stiffness { get; set; } = 0.12f;
        /// <summary>本帧走重力和物块碰撞,只有苍白形态会声明</summary>
        public bool GroundPhysics { get; set; }
        /// <summary>本帧引力场跟随本体</summary>
        public bool ArenaFollow { get; set; } = true;
        /// <summary>引力场半径的目标值,单位是像素</summary>
        public float ArenaRadiusTarget { get; set; } = VSlimeDirector.ArenaRadius;
        #endregion

        #region 声明:判定
        /// <summary>本帧没有接触伤害</summary>
        public bool NoContact { get; set; }
        /// <summary>演出态声明,本帧不可受伤</summary>
        public bool Invulnerable { get; set; }
        /// <summary>圆形判定的半径,0 用本体判定框,单位是像素</summary>
        public float HazardRadius { get; set; }
        /// <summary>本帧接触伤害的普通档基值,0 用形态默认值</summary>
        public int ContactBase { get; set; }
        /// <summary>第二个圆形判定的圆心,半径同 HazardRadius,零向量为无</summary>
        public Vector2 Hazard2Center { get; set; }
        /// <summary>受击判定框的目标边长,零向量用形态默认值</summary>
        public Vector2 HitboxTarget { get; set; }
        /// <summary>对本地玩家的吸力,单位是像素每帧</summary>
        public float PullStrength { get; set; }
        /// <summary>吸力的作用半径,单位是像素</summary>
        public float PullRange { get; set; }
        /// <summary>圆形判定内的玩家持续获得衰变</summary>
        public bool HazardDecays { get; set; }
        /// <summary>横排凝胶球本帧仍挂在本体上</summary>
        public bool RowHeld { get; set; }
        #endregion

        #region 声明:表现(纯本地,不过线)
        public VSlimeShape Shape { get; set; }
        /// <summary>纵向压扁量,正值压扁,负值拉长,约 -0.4 到 0.5</summary>
        public float Squash { get; set; }
        /// <summary>整体放大量,0 为原大</summary>
        public float Swell { get; set; }
        /// <summary>紫色描边强度 0..1</summary>
        public float RimGlow { get; set; }
        /// <summary>红色蓄力光 0..1</summary>
        public float ChargeGlow { get; set; }
        /// <summary>全身白热叠层 0..1</summary>
        public float BodyGlow { get; set; }
        /// <summary>爆闪脉冲 0..1,自衰减</summary>
        public float Pulse { get; set; }
        /// <summary>透明度声明,NaN 时宿主拉回 1</summary>
        public float AlphaDeclared { get; set; } = float.NaN;
        /// <summary>全身抖动 0..1,只是绘制偏移,自衰减</summary>
        public float ShakeStrength { get; set; }
        /// <summary>黑洞的可见半径,单位是像素</summary>
        public float HoleRadius { get; set; }
        /// <summary>黑洞色调,0 红 1 蓝</summary>
        public float HoleTint { get; set; }
        /// <summary>恒星的可见半径,单位是像素</summary>
        public float StarRadius { get; set; }
        /// <summary>恒星色温,0 蓝主序星,1 红巨星,2 白超新星</summary>
        public float StarHeat { get; set; }
        /// <summary>退入背景的程度 0..1</summary>
        public float Depth { get; set; }
        /// <summary>头顶虹光环 0..1</summary>
        public float Halo { get; set; }
        /// <summary>壳体裂纹 0..1,苍白形态爆开前用</summary>
        public float Cracks { get; set; }
        /// <summary>身体溅散成团块的程度 0..1,转奇点形态时用</summary>
        public float Splatter { get; set; }
        /// <summary>环绕本体的大辉星数量</summary>
        public int OrbitStars { get; set; }
        /// <summary>预警圈半径,强度为 0 不画</summary>
        public float WarnRadius { get; set; }
        public float WarnStrength { get; set; }
        /// <summary>航道预告,LaneStrength 为 0 不画</summary>
        public Vector2 LaneStart { get; set; }
        public Vector2 LaneEnd { get; set; }
        public float LaneWidth { get; set; }
        public float LaneStrength { get; set; }
        /// <summary>落点标记,强度为 0 不画</summary>
        public Vector2 MarkerPos { get; set; }
        public float MarkerStrength { get; set; }
        /// <summary>五角星轨迹已画出的进度,0 到 5 条边</summary>
        public float PentagramDrawn { get; set; }
        /// <summary>五角星整体亮度 0..1</summary>
        public float PentagramStrength { get; set; }
        /// <summary>图腾亮度 0..1</summary>
        public float TotemStrength { get; set; }
        /// <summary>图腾公转的基准角,单位是弧度</summary>
        public float TotemAngle { get; set; }
        /// <summary>天幕存在强度 0..1</summary>
        public float SkyIntensity { get; set; } = 1f;
        /// <summary>天幕压黑程度 0..1,死兆星阶段拉满</summary>
        public float SkyDarkness { get; set; }
        #endregion

        public override void BeginFrameDefaults() {
            base.BeginFrameDefaults();
            Mode = VSlimeMoveMode.Hold;
            MoveSpeed = 0f;
            Accel = 0.1f;
            SlowRadius = 120f;
            Stiffness = 0.12f;
            GroundPhysics = false;
            //死兆星阶段本体钉在圈心,场地不再移动
            ArenaFollow = Phase < VSlimeForm.DeathStar;
            ArenaRadiusTarget = VSlimeDirector.ArenaRadius;

            NoContact = false;
            Invulnerable = false;
            HazardRadius = 0f;
            ContactBase = 0;
            Hazard2Center = Vector2.Zero;
            HitboxTarget = Vector2.Zero;
            PullStrength = 0f;
            PullRange = 0f;
            HazardDecays = false;
            RowHeld = false;

            Shape = VSlimeShape.Slime;
            Squash = 0f;
            Swell = 0f;
            RimGlow = 0f;
            ChargeGlow = 0f;
            BodyGlow = 0f;
            AlphaDeclared = float.NaN;
            HoleRadius = 0f;
            HoleTint = 0f;
            StarRadius = 0f;
            StarHeat = 0f;
            Depth = 0f;
            Halo = 0f;
            Cracks = 0f;
            Splatter = 0f;
            OrbitStars = 0;
            WarnStrength = 0f;
            LaneStrength = 0f;
            MarkerStrength = 0f;
            PentagramDrawn = 0f;
            PentagramStrength = 0f;
            TotemStrength = 0f;
            SkyIntensity = Phase >= VSlimeForm.Void ? 1f : 0f;
            SkyDarkness = Phase >= VSlimeForm.DeathStar ? 1f : 0f;

            Pulse *= 0.9f;
            if (Pulse < 0.02f) {
                Pulse = 0f;
            }
            ShakeStrength *= 0.84f;
            if (ShakeStrength < 0.02f) {
                ShakeStrength = 0f;
            }
        }
    }
}
