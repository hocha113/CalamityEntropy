using Microsoft.Xna.Framework.Graphics;

namespace CalamityEntropy.Core.Integrations.BossLog
{
    /// <summary>
    /// 图鉴整本书接管时的域主题:一套色板 + 两层演出(书后全屏氛围、书缘饰件)。
    /// 书骨架(封 / 脊 / 纸 / 标题块 / 场景框)由 <see cref="CEBossLogSkin"/> 按色板统一画,
    /// 各 Boss 共用同一骨架,主题只换材质故事与氛围细节
    /// </summary>
    internal abstract class CEBossLogTheme
    {
        /// <summary>书封主体</summary>
        public abstract Color Cover { get; }
        /// <summary>封边亮线 / 书脊高光</summary>
        public abstract Color CoverEdge { get; }
        /// <summary>书脊</summary>
        public abstract Color Spine { get; }
        /// <summary>纸面。取暗纸:BossChecklist 右页自带的白描边文字、金色标题与物品格要在它上面可读</summary>
        public abstract Color Paper { get; }
        /// <summary>纸面纤维亮部 / 页缘叠层线</summary>
        public abstract Color PaperLight { get; }
        /// <summary>页内细线</summary>
        public abstract Color Rule { get; }
        /// <summary>强调光:标题、场景框角、书后背光</summary>
        public abstract Color Accent { get; }
        /// <summary>次要文字(模组名)</summary>
        public abstract Color Muted { get; }
        /// <summary>全屏压暗底色</summary>
        public abstract Color Dim { get; }
        /// <summary>书后天幕渐变上端色</summary>
        public abstract Color SkyTop { get; }
        /// <summary>书后天幕渐变下端色</summary>
        public abstract Color SkyBottom { get; }

        /// <summary>左页顶部标题带最小高度(UI px),实际取值见 <see cref="CEBossLogSkin.TitleBandOf"/>(随字号阶梯撑开)</summary>
        public virtual int TitleBand => 58;

        /// <summary>
        /// 书后全屏氛围的域细节(星尘 / 孢雾 / 余烬…)。
        /// 基础压暗、天幕渐变与书后背光已由骨架先画好,这里只补域签名。
        /// <paramref name="blend"/> 为缓入缓出后的混合量,所有 alpha 乘它
        /// </summary>
        public abstract void DrawAmbienceDetail(SpriteBatch sb, Rectangle screen, Rectangle book, float time, float blend);

        /// <summary>
        /// 书缘饰件:画在整本书之上,只许落在书脊与底封边,
        /// 不得压页面内容与书签标签(标签在书左右两侧外突,左右封边不可用)
        /// </summary>
        public virtual void DrawOrnament(SpriteBatch sb, Rectangle book, Rectangle spine, Rectangle bottomMargin, float time, float blend) { }
    }
}
