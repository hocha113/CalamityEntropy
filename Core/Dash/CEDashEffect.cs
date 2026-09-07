using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Dash
{
    /// <summary>
    /// 冲刺撞击结算参数,由 <see cref="CEDashEffect.OnHit"/> 填写。Damage 为 0 时不结算伤害。
    /// </summary>
    public struct CEDashHit
    {
        /// <summary>基础伤害,由引擎套用职业伤害加成与暴击率。</summary>
        public int Damage;
        public float Knockback;
        /// <summary>命中后给玩家的无敌帧。</summary>
        public int PlayerImmuneFrames;
        /// <summary>伤害职业,默认无职业(Generic)。</summary>
        public DamageClass DamageClass;
    }

    /// <summary>
    /// 一次冲刺的运行态。效果对象是无状态单例,所有随冲刺变化的数据都放在这里。
    /// </summary>
    public sealed class CEDashState
    {
        public CEDashEffect Effect;
        public CEDashEnhancer Enhancer;
        /// <summary>单位方向向量。</summary>
        public Vector2 Direction;
        /// <summary>已进行帧数,首帧为 0。</summary>
        public int Timer;
        public int Duration;
        /// <summary>每帧位移(像素),由引擎按距离与曲线生成。</summary>
        public float[] Speeds = Array.Empty<float>();
        /// <summary>远端玩家的纯视觉复现:不写速度、不判撞击。</summary>
        public bool Remote;
        public int HitCount;
        public readonly HashSet<int> HitNPCs = new();
        /// <summary>效果私有数据槽(如拖尾粒子引用)。</summary>
        public object EffectData;
        /// <summary>强化器私有数据槽。</summary>
        public object EnhancerData;

        public bool Horizontal => Direction.Y == 0f;
        public float Progress => Duration <= 0 ? 1f : MathHelper.Clamp(Timer / (float)Duration, 0f, 1f);
        public float CurrentSpeed => Timer >= 0 && Timer < Speeds.Length ? Speeds[Timer] : 0f;
        /// <summary>冲刺期间是否全程无敌(含弹幕)。</summary>
        public bool Invincible => Effect.Invincible || (Enhancer != null && Enhancer.Invincible);
        public bool Enhanced => Enhancer != null;
        /// <summary>冲刺方向的水平符号,无水平分量时取玩家朝向。</summary>
        public int HorizontalSign(Player player) => Direction.X != 0f ? Math.Sign(Direction.X) : player.direction;
    }

    /// <summary>
    /// 冲刺效果定义。饰品每帧在 <c>UpdateAccessory</c> 里调用 <see cref="CEDashPlayer.Offer"/> 登记,
    /// 引擎负责输入、运动、撞击与冷却;效果只声明参数并在回调里做扣费、音效与视觉。
    /// 子类由 <see cref="CEDashRegistry"/> 反射实例化,必须有无参构造,且不得持有随冲刺变化的字段。
    /// </summary>
    public abstract class CEDashEffect
    {
        /// <summary>唯一标识,写入 <c>EModPlayer.LastUsedDashID</c> 并用于联机同步。</summary>
        public abstract string ID { get; }

        /// <summary>多件冲刺饰品同时登记时,数值高者接管;相同则后登记者接管。</summary>
        public virtual int Priority => 0;

        /// <summary>是否由双击方向键或冲刺键触发。为假的效果只能靠 <see cref="Hotkey"/> 起手。</summary>
        public virtual bool UsesDoubleTap => true;

        /// <summary>是否允许双击上下触发竖直冲刺。</summary>
        public virtual bool Omnidirectional => false;

        /// <summary>专用起手键;返回非空时引擎在 ProcessTriggers 里监听。</summary>
        public virtual ModKeybind Hotkey => null;

        /// <summary>热键起手时的方向,返回空表示本次不起手。</summary>
        public virtual Vector2? HotkeyDirection(Player player) => null;

        /// <summary>是否可以打断正在进行的其他冲刺。</summary>
        public virtual bool CanInterrupt => false;

        /// <summary>运动由原版驱动(如被暗影披风强化的原版冲刺),引擎不写速度、不判墙。</summary>
        public virtual bool ExternalMotion => false;

        /// <summary>冲刺持续帧数。</summary>
        public abstract int Duration { get; }

        /// <summary>自由空间中的总位移(像素)。</summary>
        public abstract float Distance { get; }

        /// <summary>冲刺结束后的锁定帧,期间任何冲刺都不能起手。</summary>
        public virtual int Cooldown => 30;

        /// <summary>缓出曲线指数:越大起手越猛、收尾越缓。1 为线性衰减。</summary>
        public virtual float Curve => 2f;

        /// <summary>冲刺期间重力倍率。</summary>
        public virtual float GravityMult => 0.3f;

        /// <summary>水平冲刺期间衰减竖直速度(不按跳跃时),让冲刺"接住"下落。</summary>
        public virtual bool DampVertical => true;

        /// <summary>冲刺全程无敌,可穿过敌方弹幕。</summary>
        public virtual bool Invincible => false;

        /// <summary>冲刺期间穿过敌怪并对其结算撞击(见 <see cref="OnHit"/>)。</summary>
        public virtual bool HitsEnemies => false;

        /// <summary>冲刺期间无视平台。</summary>
        public virtual bool IgnorePlatforms => false;

        /// <summary>是否接受暗影披风等强化器。</summary>
        public virtual bool CanBeEnhanced => true;

        /// <summary>冲刺结尾速度:水平冲刺衔接跑速,竖直冲刺留一点余速。</summary>
        public virtual float EndSpeed(Player player, Vector2 direction)
        {
            if (direction.Y != 0f)
                return 4f;
            return MathHelper.Clamp(Math.Max(player.accRunSpeed, player.maxRunSpeed), 3f, 9f);
        }

        /// <summary>资源门槛(充能、冷却等)。引擎已经排除坐骑、控制、死亡与锁定帧。</summary>
        public virtual bool CanStart(Player player) => true;

        /// <summary>起手瞬间(本地):扣费、音效、初始视觉。</summary>
        public virtual void OnStart(Player player, CEDashState state) { }

        /// <summary>每帧位移之后的视觉(本地与远端都会调用)。</summary>
        public virtual void OnVisuals(Player player, CEDashState state) { }

        /// <summary>撞击敌怪(本地):填写 hit 以结算伤害。每个敌怪每次冲刺只触发一次。</summary>
        public virtual void OnHit(Player player, NPC npc, CEDashState state, ref CEDashHit hit) { }

        /// <summary>冲刺结束(本地与远端)。</summary>
        public virtual void OnEnd(Player player, CEDashState state) { }
    }

    /// <summary>
    /// 冲刺强化器(暗影披风):常规冲刺起手时若 <see cref="TryConsume"/> 通过,本次冲刺被强化。
    /// 同样是无状态单例,由 <see cref="CEDashRegistry"/> 反射实例化。
    /// </summary>
    public abstract class CEDashEnhancer
    {
        public abstract string ID { get; }

        /// <summary>就绪则消耗(挂冷却)并返回真。只在本地调用。</summary>
        public abstract bool TryConsume(Player player);

        /// <summary>速度与加速度倍率;同帧数下位移同倍放大。</summary>
        public virtual float SpeedMult => 1.2f;

        /// <summary>强化期间全程无敌。</summary>
        public virtual bool Invincible => true;

        public virtual void OnStart(Player player, CEDashState state) { }
        public virtual void OnVisuals(Player player, CEDashState state) { }
        public virtual void OnEnd(Player player, CEDashState state) { }
    }

    /// <summary>
    /// 被强化器改造的原版冲刺(克苏鲁之盾、忍者大师装备等)的占位效果:
    /// 运动仍由原版 DashMovement 驱动,引擎只跟踪无敌与强化视觉。
    /// </summary>
    public sealed class CEVanillaDashEffect : CEDashEffect
    {
        public override string ID => "VanillaDash";
        public override bool UsesDoubleTap => false;
        public override bool ExternalMotion => true;
        /// <summary>仅供远端视觉复现使用,本地以 dashDelay 回到非负为结束。</summary>
        public override int Duration => 30;
        public override float Distance => 0f;
        public override int Cooldown => 0;
        public override bool CanBeEnhanced => true;
    }
}
