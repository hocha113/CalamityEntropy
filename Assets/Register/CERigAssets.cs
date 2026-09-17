using InnoVault;
using InnoVault.Rigs2D.Runtime;

namespace CalamityEntropy.Assets.Register
{
    /// <summary>
    /// Boss 骨架资产(InnoVault Rigs2D),定义文件在 <c>Assets/Rigs/*.rig.json</c>。
    /// <para>
    /// 与贴图类资产不同,骨架<b>两端都加载</b>(<c>Rig2DLoadenHandle.LoadOnServer</c>):
    /// 专用服务器只解析 JSON、不取贴图,所以体节落位、触地判定一类要读骨骼的 gameplay 逻辑在服务端也成立。
    /// 字段在 <c>PostSetupContent</c> 之后才有值,而 ModNPC 模板实例在那之前就构造好了,
    /// 所以宿主一律<b>惰性</b>创建 <see cref="Rig2DInstance"/>(首帧 AI 里 <c>CreateInstance</c> + <c>Bind</c>),不要在字段初始化器里碰这些
    /// </para>
    /// <para>
    /// 开发机上改 <c>.rig.json</c> 可热重载(<c>/vaultdebug</c> → Rig2D 页,轮询 ModSources 副本);
    /// 巡游者的链长随难度变化,走代码直建(<c>CruiserChainRig</c>),不在这里
    /// </para>
    /// </summary>
    public static class CERigAssets
    {
        /// <summary>灭心者:身体 + 12 节尾链 + 尾尖,跟随链与贝塞尔链按尾巴样式切换</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Apsychos")]
        public static Vault2DRig Apsychos;

        /// <summary>虚无噬菌体宿主:三层触手对 + 通往细胞的 30 节绳</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Nihility")]
        public static Vault2DRig Nihility;

        /// <summary>混沌细胞:八条 12 节跟随触手带</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/ChaoticCell")]
        public static Vault2DRig ChaoticCell;

        /// <summary>小混沌细胞:通往母体的 30 节绳</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/ChaoticCellSmall")]
        public static Vault2DRig ChaoticCellSmall;

        /// <summary>流明蛾:8 帧身体件 + 两条 10 节 verlet 尾带</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Luminaris")]
        public static Vault2DRig Luminaris;

        /// <summary>卫城机器:四腿步态 + 两条双节瞄准臂 + 鱼叉链带,朝右作图、靠 Mirrored 翻身</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Acropolis")]
        public static Vault2DRig Acropolis;

        /// <summary>先知:四片翅骨 + 10 节尾带 + 尾环</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Prophet")]
        public static Vault2DRig Prophet;
    }
}
