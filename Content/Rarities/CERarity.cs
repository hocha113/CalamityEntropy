using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Rarities
{
    /// <summary>
    /// 本模组自有稀有度基类,11 档全部继承它,不再有任何一档借灾厄的 ModRarity。
    /// <see cref="BaseColor"/> 给拾取飘字、[i:] 标签、物品栏名字等只读颜色的位置;
    /// 提示框名称行由 <see cref="Common.EGlobalItem"/> 经 <see cref="CERarityNameEffects.TryDrawItemName"/> 转到
    /// <see cref="DrawName"/> 自绘,全程在当前 SpriteBatch 直绘,不 End/Begin、不切混合态、不碰渲染目标。
    /// 每档把可调参数放在类顶部常量里,视觉结论在没进游戏看过之前都算未验证。
    /// </summary>
    public abstract class CERarity : ModRarity
    {
        /// <summary>静态主色。各档必须彼此可辨,拾取飘字只看这一个值</summary>
        public abstract Color BaseColor { get; }

        public sealed override Color RarityColor => BaseColor;

        /// <summary>
        /// 名称行自绘。<paramref name="lineColor"/> 是 tML 传入的名称行颜色,已含 mouseTextColor 呼吸衰减
        /// (可能被他模 OverrideColor 换色),各档用 <see cref="CERarityNameEffects.FadeOf"/> 取衰减系数同步自己的调色板;
        /// <paramref name="scale"/> 为行基准缩放;默认只画原版口径的阴影字
        /// </summary>
        public virtual void DrawName(SpriteBatch sb, Item item, string text, Vector2 pos, Color lineColor, Vector2 scale, float time) {
            CERarityNameEffects.DrawPlain(sb, text, pos, lineColor, scale);
        }
    }
}
