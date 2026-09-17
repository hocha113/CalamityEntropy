using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CalamityEntropy.Core.AI
{
    /// <summary>
    /// Boss 联机的 DEBUG 诊断通道。<b>Release 构建下零开销零行为变化</b>:
    /// 全部入口都挂 <see cref="ConditionalAttribute"/>,编译器连实参求值一起删掉;
    /// 内部状态另外用 <c>#if DEBUG</c> 括起来,Release 下连那个字典都不存在。
    /// <para>
    /// 所以调用处可以无脑写,不必自己套 <c>#if DEBUG</c>,也不必担心把 <c>Timer</c>
    /// 这种虚属性的读取留在发布版里。
    /// </para>
    /// </summary>
    public static class CEBossNetDiag
    {
#if DEBUG
        /// <summary>定长块起始偏移。键是 <c>标签|W</c> / <c>标签|R</c>,写读两侧各记各的</summary>
        private static readonly Dictionary<string, long> blockMarks = new();
        /// <summary>没给期望字节数时,每个标签只报一次实测值,免得每包刷屏</summary>
        private static readonly HashSet<string> sizeReported = new();
        /// <summary>
        /// 收包在客户端跑的是 socket 读取线程,发包在服务端跑的是主线程。
        /// 实测两侧各自单线程、互不重叠,但字典被并发改写会直接死循环,诊断件不值得赌这个
        /// </summary>
        private static readonly object gate = new();

        private static void Log(string line, bool warn) {
            CalamityEntropy inst = CalamityEntropy.Instance;
            if (inst == null) {
                return;
            }
            if (warn) {
                inst.Logger.Warn(line);
            }
            else {
                inst.Logger.Debug(line);
            }
        }

        private static void Mark(string key, Stream stream) {
            if (stream == null || !stream.CanSeek) {
                return;
            }
            lock (gate) {
                blockMarks[key] = stream.Position;
            }
        }

        private static void Measure(string key, string tag, string side, Stream stream, int expectedBytes) {
            if (stream == null || !stream.CanSeek) {
                return;
            }
            long start;
            bool first;
            lock (gate) {
                if (!blockMarks.TryGetValue(key, out start)) {
                    return;
                }
                blockMarks.Remove(key);
                first = expectedBytes < 0 && sizeReported.Add(key);
            }
            int len = (int)(stream.Position - start);
            if (expectedBytes < 0) {
                if (first) {
                    Log($"[CEBossNet] {tag} {side} 定长块实测 {len} 字节,把它填进两端共用的常量里", false);
                }
                return;
            }
            if (len != expectedBytes) {
                Log($"[CEBossNet] {tag} {side} 定长块字节数不符:实测 {len},契约 {expectedBytes}。"
                    + "这一侧的读写顺序已经和契约脱节,整条共享流会从这里开始错位", true);
            }
        }
#endif

        /// <summary>
        /// 计时收养越过容差时打点。挂在 <see cref="CEBossStateBase{TCtx}.AdoptNetTiming"/> 里,
        /// 也就是<b>真正发生收养的那一刻</b>。
        /// <para>
        /// <b>不要把这种探针写在 <c>ReceiveExtraAI</c> 末尾。</b>
        /// <see cref="CEBossNetMotion.ReceiveTiming"/> 只把计时存进内部的待收养槽,并不落盘;
        /// 收养发生在此后的 <see cref="CEBossHost.AdoptTimingAtFrameStart"/> 或换态钩子里。
        /// 在 <c>ReceiveExtraAI</c> 里比较「收包前的本地计时」和「状态对象上的计时」,
        /// 两边读到的是同一个还没被动过的值,条件恒假 —— 那是个永远不会响的探针,
        /// 拿它确认「联机没失步」只会得到假阴性。Apsychos 上一版就是这么写的。
        /// </para>
        /// </summary>
        /// <param name="state">状态名,给日志用</param>
        /// <param name="stateId">状态号</param>
        /// <param name="localTimer">收养<b>之前</b>的本地计时</param>
        /// <param name="syncedTimer">包里的权威端计时</param>
        [Conditional("DEBUG")]
        public static void TimingAdopted(string state, int stateId, int localTimer, int syncedTimer) {
#if DEBUG
            int delta = syncedTimer - localTimer;
            if (Math.Abs(delta) > CEBossNetMotion.TimerTolerance) {
                Log($"[CEBossNet] {state}(#{stateId}) 计时收养越过容差:本地 {localTimer} ← 权威 {syncedTimer}(Δ{delta})", false);
            }
#endif
        }

        /// <summary>
        /// 定长块字节数自查(写侧)。与 <see cref="EndWrite"/> 成对,夹住 <c>SendExtraAI</c> 的块体。
        /// <para>
        /// <b>tML 自己那道检查不够用。</b><c>NPCLoader.ReceiveExtraAI</c> 在整块读完之后才查
        /// <c>stream.Position &lt; stream.Length</c>,报的是「整条共享流少读了 N 字节」,
        /// 而且异常信息只会列出一串 GlobalNPC 让你自己猜。本 Boss 的块体读少了 4 字节,
        /// 后面每个订阅者都跟着错位、一起读到垃圾,最后由某个无辜的 GlobalNPC 背锅;
        /// 要是错位量恰好被另一个变长订阅者抵消,整块长度还能对上,那就连异常都不会有。
        /// 本件把检查下沉到<b>每个订阅者自己的块</b>,当场指名道姓
        /// </para>
        /// </summary>
        [Conditional("DEBUG")]
        public static void BeginWrite(string tag, BinaryWriter writer) {
#if DEBUG
            Mark(tag + "|W", writer?.BaseStream);
#endif
        }

        /// <summary>
        /// 定长块字节数自查(写侧收尾)。
        /// <para>
        /// <b>为什么比的是常量而不是对端。</b>写在服务端、读在客户端,是两个进程,运行时比不了;
        /// 把长度塞进流里又等于改线格式,DEBUG 与 Release 的包互不兼容,反而更糟。
        /// 所以契约落在一个<b>两端共用的编译期常量</b>上:改了字段忘了改对侧,
        /// 哪一侧跑偏哪一侧当场自己打日志,不需要对两份日志。
        /// </para>
        /// <para>
        /// <paramref name="expectedBytes"/> 传负数则只报一次实测值,方便第一次接入时把数字抄进常量。
        /// </para>
        /// </summary>
        /// <param name="tag">Boss 标签,直接用 <c>nameof</c></param>
        /// <param name="writer">与 <see cref="BeginWrite"/> 同一个 writer</param>
        /// <param name="expectedBytes">两端共用的定长块字节数常量;负数表示只观测</param>
        [Conditional("DEBUG")]
        public static void EndWrite(string tag, BinaryWriter writer, int expectedBytes = -1) {
#if DEBUG
            Measure(tag + "|W", tag, "写侧", writer?.BaseStream, expectedBytes);
#endif
        }

        /// <summary>定长块字节数自查(读侧)。与 <see cref="EndRead"/> 成对,夹住 <c>ReceiveExtraAI</c> 的块体</summary>
        [Conditional("DEBUG")]
        public static void BeginRead(string tag, BinaryReader reader) {
#if DEBUG
            Mark(tag + "|R", reader?.BaseStream);
#endif
        }

        /// <summary>定长块字节数自查(读侧收尾)。判据与 <see cref="EndWrite"/> 完全一致,共用同一个常量</summary>
        [Conditional("DEBUG")]
        public static void EndRead(string tag, BinaryReader reader, int expectedBytes = -1) {
#if DEBUG
            Measure(tag + "|R", tag, "读侧", reader?.BaseStream, expectedBytes);
#endif
        }
    }
}
