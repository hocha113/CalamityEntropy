using InnoVault;
using InnoVault.Rigs2D.Runtime;

namespace CalamityEntropy.Assets.Register
{
    /// <summary>
    /// 骨架在客户端和服务端都加载,dedServ 只解析 JSON 不取贴图,骨骼落位在服务端也成立
    /// 这些字段在 PostSetupContent 之后才有值,宿主应惰性 CreateInstance 再 Bind,不要写进字段初始化器
    /// .rig.json 可以热重载,巡游者链长随难度变化,那条链走 CruiserChainRig,不在这里
    /// </summary>
    public static class CERigAssets
    {
        /// <summary>灭心者骨架含身体、12 节尾链和尾尖,跟随链与贝塞尔链按尾巴样式切换</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Apsychos")]
        public static Vault2DRig Apsychos;

        /// <summary>虚无噬菌体宿主骨架有三层触手对,另有通往细胞的 30 节绳</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Nihility")]
        public static Vault2DRig Nihility;

        /// <summary>混沌细胞骨架有八条 12 节的跟随触手带</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/ChaoticCell")]
        public static Vault2DRig ChaoticCell;

        /// <summary>小混沌细胞骨架有通往母体的 30 节绳</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/ChaoticCellSmall")]
        public static Vault2DRig ChaoticCellSmall;

        /// <summary>流明蛾骨架有 8 帧身体件和两条 10 节 verlet 尾带</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Luminaris")]
        public static Vault2DRig Luminaris;

        /// <summary>卫城机器骨架有四腿步态、两条双节瞄准臂和鱼叉链带,贴图朝右,靠 Mirrored 翻身</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Acropolis")]
        public static Vault2DRig Acropolis;

        /// <summary>先知骨架有四片翅骨、10 节尾带和尾环</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/Prophet")]
        public static Vault2DRig Prophet;

        /// <summary>星月贤者骨架有头、上下两对翼骨、10 节尾带和尾环,贴图朝下</summary>
        [VaultLoaden("CalamityEntropy/Assets/Rigs/StarmoonSage")]
        public static Vault2DRig StarmoonSage;
    }
}
