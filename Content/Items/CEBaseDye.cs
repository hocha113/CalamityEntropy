using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    /// <summary>染料物品基类（原生移植，替代灾厄 BaseDye）：自动绑定护甲着色器并克隆凝胶染料默认值。</summary>
    public abstract class CEBaseDye : ModItem
    {
        public abstract ArmorShaderData ShaderDataToBind { get; }

        public sealed override void SetStaticDefaults() {
            if (!Main.dedServ) {
                GameShaders.Armor.BindShader(Item.type, ShaderDataToBind);
            }
            SafeSetStaticDefaults();
        }

        public sealed override void SetDefaults() {
            int dye = Item.dye;
            Item.CloneDefaults(ItemID.GelDye);
            Item.dye = dye;
            SafeSetDefaults();
        }

        /// <summary>等价 SetDefaults，染料 ID 克隆已由基类处理。</summary>
        public virtual void SafeSetDefaults() {
        }

        /// <summary>等价 SetStaticDefaults，着色器绑定已由基类处理。</summary>
        public virtual void SafeSetStaticDefaults() {
        }
    }
}
