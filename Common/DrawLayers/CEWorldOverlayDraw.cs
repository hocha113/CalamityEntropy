using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Armor.AzafureT3;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Utilities;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    /// <summary>
    /// 世界层的附加绘制内容:环绕玩家的虚无护壳与玛瑞维护盾、阿扎弗护盾充能条、
    /// 永冻牢笼环、卫城机甲。这些原先整块塞在模组入口的两个绘制钩子体内。
    /// 钩子本身留在 <see cref="Core.Hooks.CEDrawHooks"/>,这里只负责画。
    /// </summary>
    internal static class CEWorldOverlayDraw
    {
        [VaultLoaden("CalamityEntropy/Assets/Extra/shell")]
        internal static Asset<Texture2D> ShellTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/MariviniumShield")]
        internal static Asset<Texture2D> MariviniumShieldTex;

        private static int permafrostCircleType = -1;
        //每个玩家至多画一个永冻牢笼环(原行为是找到第一个就 break),单趟扫描时用它记录已画过的 owner
        private static readonly bool[] permafrostDrawn = new bool[256];

        public static float AzShieldBarAlpha = 0;

        /// <summary>在 PostSetupContent 期一次解析,不再在绘制循环里用 -1 哨兵懒解析</summary>
        public static void ResolveTypes() {
            permafrostCircleType = ModContent.ProjectileType<PrisonOfPermafrostCircle>();
        }

        /// <summary>
        /// 环绕玩家的护壳与护盾。同一圈分前后两半绘制:
        /// <paramref name="backHalf"/> 为真时画转到玩家身后的那半(在弹幕层之后、玩家之前),
        /// 为假时画身前的那半。
        /// </summary>
        public static void DrawOrbitingShells(Player player, bool backHalf) {
            EModPlayer entropy = player.Entropy();
            int shellCount = entropy.nihShellCount;
            int shieldCount = entropy.MariviniumShieldCount;
            if (shellCount <= 0 && shieldCount <= 0) {
                return;
            }

            Vector2 anchor = player.Center + player.gfxOffY * Vector2.UnitY - Main.screenPosition;
            Texture2D shell = ShellTex.Value;

            if (shellCount > 0) {
                float rot = entropy.CasketSwordRot * 0.2f;
                for (int i = 0; i < shellCount; i++) {
                    float sin = rot.ToRotationVector2().Y;
                    if (backHalf ? (sin < 0) : (sin > 0)) {
                        Vector2 center = new Vector2(36, 0).RotatedBy(rot);
                        center.Y = 0;
                        float sizeX = Math.Abs(new Vector2(56, 0).RotatedBy(rot + 0.3f).X - new Vector2(56, 0).RotatedBy(rot - 0.3f).X);
                        Main.spriteBatch.Draw(shell, anchor + center, null, Color.White * 0.8f * (((sin + 1) * 0.5f) * 0.7f + 0.3f), 0, shell.Size() / 2, new Vector2(sizeX / shell.Width, 1), SpriteEffects.None, 0);
                    }
                    rot += MathHelper.TwoPi / shellCount;
                }
            }

            if (shieldCount > 0) {
                Texture2D crystalShield = MariviniumShieldTex.Value;
                float rot = entropy.CasketSwordRot * -0.2f;
                for (int i = 0; i < shieldCount; i++) {
                    float sin = rot.ToRotationVector2().Y;
                    if (backHalf ? (sin < 0) : (sin > 0)) {
                        Vector2 center = new Vector2(48, 0).RotatedBy(rot);
                        center.Y = 0;
                        float sizeX = Math.Abs(new Vector2(56, 0).RotatedBy(rot + 0.3f).X - new Vector2(56, 0).RotatedBy(rot - 0.3f).X);
                        //原点沿用护壳贴图的尺寸,与 3.33 表现一致,不改成护盾自身的
                        Main.spriteBatch.Draw(crystalShield, anchor + center, null, Color.White * 0.6f * (((sin + 1) * 0.5f) * 0.7f + 0.3f), 0, shell.Size() / 2, new Vector2(sizeX / shell.Width, 1), SpriteEffects.None, 0);
                    }
                    rot += MathHelper.TwoPi / shieldCount;
                }
            }
        }

        /// <summary>本地玩家头顶的阿扎弗护盾充能条</summary>
        public static void DrawAzafureChargeBar() {
            EModPlayer entropy = Main.LocalPlayer.Entropy();
            Item chargeShield = entropy.AzafureChargeShieldItem;
            Item driverShield = entropy.AzafureDriverShieldItem;
            if (chargeShield == null && driverShield == null) {
                AzShieldBarAlpha = float.Lerp(AzShieldBarAlpha, 0, 0.1f);
                return;
            }

            float charge = 0;
            float maxCharge = 0;
            if (chargeShield != null && chargeShield.ModItem is AzafureChargeShield mi) {
                charge = mi.charge;
                maxCharge = mi.maxCharge;
            }
            if (driverShield != null && driverShield.ModItem is AzafureDriverCore mi2) {
                charge = mi2.charge;
                maxCharge = mi2.maxCharge;
            }
            //充满了就淡出,没充满才显示
            AzShieldBarAlpha = float.Lerp(AzShieldBarAlpha, charge >= maxCharge ? 0 : 1, 0.1f);
            CEUtils.DrawChargeBar(1.5f, Main.LocalPlayer.Center + Main.LocalPlayer.gfxOffY * Vector2.UnitY - Main.screenPosition + new Vector2(0, -42),
                charge / maxCharge,
                ((charge > 1) ? Color.Lerp(Color.OrangeRed, Color.Orange, (float)Math.Cos(Main.GameUpdateCount * 0.2f) * 0.5f + 0.5f) : Color.Firebrick) * AzShieldBarAlpha);
        }

        /// <summary>遗珍的柔光</summary>
        public static void DrawLostHeirloomGlow(Player player) {
            if (player.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom)) {
                CEUtils.DrawGlow(player.Center, Color.White * 0.2f, 5.2f);
            }
        }

        /// <summary>
        /// 永冻牢笼环。原先这段嵌在玩家循环里、每个持有者都要全量扫一遍弹幕数组,
        /// 现在改成单趟扫描,每个 owner 只画第一个。
        /// </summary>
        public static void DrawPermafrostRings() {
            if (permafrostCircleType <= 0) {
                return;
            }
            Array.Clear(permafrostDrawn, 0, permafrostDrawn.Length);
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type != permafrostCircleType || p.owner < 0 || p.owner >= permafrostDrawn.Length || permafrostDrawn[p.owner]) {
                    continue;
                }
                if (p.ModProjectile is not PrisonOfPermafrostCircle poc) {
                    continue;
                }
                permafrostDrawn[p.owner] = true;
                float alpha = poc.usingTime / 60f;
                if (alpha > 1) {
                    alpha = 1;
                }
                Texture2D itemTex = poc.itemTex;
                Main.spriteBatch.Draw(itemTex, p.Center + p.rotation.ToRotationVector2() * 28 - Main.screenPosition, null, Color.White * alpha, p.rotation + MathHelper.PiOver2, itemTex.Size() / 2, p.scale * 0.5f, SpriteEffects.None, 0);
            }
        }

        /// <summary>卫城套装的机甲。自带一段独立的 SpriteBatch 区段</summary>
        public static void DrawAcropolisMechs() {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (Player player in Main.ActivePlayers) {
                if (player.TryGetModPlayer<AcropolisArmorPlayer>(out var mp)) {
                    mp.DrawMech();
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
    }
}
