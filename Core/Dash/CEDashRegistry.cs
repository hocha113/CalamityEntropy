using InnoVault;
using System.Collections.Generic;

namespace CalamityEntropy.Core.Dash
{
    /// <summary>
    /// 冲刺效果与强化器的单例表。饰品用 <see cref="Get{T}"/> 取实例,联机同步用 ID 反查。
    /// 实现 ICELoader:实例由 GetUninitializedObject 创建,字段初始化器不会跑,一切都在 SetupData 里建。
    /// </summary>
    internal class CEDashRegistry : ICELoader
    {
        private static Dictionary<string, CEDashEffect> effectsById;
        private static Dictionary<System.Type, CEDashEffect> effectsByType;
        private static Dictionary<string, CEDashEnhancer> enhancersById;
        private static Dictionary<System.Type, CEDashEnhancer> enhancersByType;

        void ICELoader.SetupData()
        {
            effectsById = new Dictionary<string, CEDashEffect>();
            effectsByType = new Dictionary<System.Type, CEDashEffect>();
            enhancersById = new Dictionary<string, CEDashEnhancer>();
            enhancersByType = new Dictionary<System.Type, CEDashEnhancer>();

            foreach (CEDashEffect effect in VaultUtils.GetDerivedInstances<CEDashEffect>())
            {
                effectsById[effect.ID] = effect;
                effectsByType[effect.GetType()] = effect;
            }
            foreach (CEDashEnhancer enhancer in VaultUtils.GetDerivedInstances<CEDashEnhancer>())
            {
                enhancersById[enhancer.ID] = enhancer;
                enhancersByType[enhancer.GetType()] = enhancer;
            }
        }

        void ICELoader.UnLoadData()
        {
            effectsById = null;
            effectsByType = null;
            enhancersById = null;
            enhancersByType = null;
        }

        public static T Get<T>() where T : CEDashEffect
            => effectsByType != null && effectsByType.TryGetValue(typeof(T), out var effect) ? (T)effect : null;

        public static CEDashEffect GetEffect(string id)
            => effectsById != null && id != null && effectsById.TryGetValue(id, out var effect) ? effect : null;

        public static T GetEnhancer<T>() where T : CEDashEnhancer
            => enhancersByType != null && enhancersByType.TryGetValue(typeof(T), out var enhancer) ? (T)enhancer : null;

        public static CEDashEnhancer GetEnhancer(string id)
            => enhancersById != null && !string.IsNullOrEmpty(id) && enhancersById.TryGetValue(id, out var enhancer) ? enhancer : null;
    }
}
