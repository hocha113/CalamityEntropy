using CalamityEntropy.Common;
using CalamityEntropy.Common.DrawLayers;
using CalamityEntropy.Content.Items.Armor.AzafureT3;
using CalamityEntropy.Content.Items.Atbm;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics.Screen;
using CalamityEntropy.Utilities;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Hooks
{
    /// <summary>
    /// 客户端绘制相关的 On_* 钩子:玩家本体绘制的隐藏与替位、世界层附加绘制的两个层位、
    /// 主菜单回收循环音、以及自定义准星时对原版光标的屏蔽。
    /// 具体画什么在 <see cref="CEWorldOverlayDraw"/>,这里只管钩子位置与 orig 的调用时机。
    /// </summary>
    internal sealed class CEDrawHooks : ICELoader
    {
        //替位绘制要认的弹幕。在 SetupData 里一次解析,不再在绘制路径上用 -1 哨兵懒解析
        private static int cbpSmashType = -1;

        void ICELoader.LoadData() {
            if (Main.dedServ) {
                return;
            }
            CEDetourRegistry.Add(() => On_LegacyPlayerRenderer.DrawPlayer += DrawPlayerHook, () => On_LegacyPlayerRenderer.DrawPlayer -= DrawPlayerHook);
            CEDetourRegistry.Add(() => On_Main.DrawProjectiles += DrawProjectilesHook, () => On_Main.DrawProjectiles -= DrawProjectilesHook);
            CEDetourRegistry.Add(() => On_Main.DrawInfernoRings += DrawInfernoRingsHook, () => On_Main.DrawInfernoRings -= DrawInfernoRingsHook);
            CEDetourRegistry.Add(() => On_Main.DrawMenu += DrawMenuHook, () => On_Main.DrawMenu -= DrawMenuHook);
            CEDetourRegistry.Add(() => On_Main.DrawCursor += DrawCursorHook, () => On_Main.DrawCursor -= DrawCursorHook);
            CEDetourRegistry.Add(() => On_Main.DrawThickCursor += DrawThickCursorHook, () => On_Main.DrawThickCursor -= DrawThickCursorHook);
        }

        void ICELoader.SetupData() {
            if (Main.dedServ) {
                return;
            }
            cbpSmashType = ModContent.ProjectileType<CBPSmash>();
            CEWorldOverlayDraw.ResolveTypes();
            //屏幕特效管线的类型 ID 也在这里解析,免得绘制路径上再用 -1 哨兵懒解析
            CEWarpScreen.ResolveTypes();
            CEEntityOverlay.ResolveTypes();
        }

        //隐身/演出期不画玩家本体;砸击期把绘制位置挪到弹幕上
        private static void DrawPlayerHook(On_LegacyPlayerRenderer.orig_DrawPlayer orig, LegacyPlayerRenderer self, Camera camera, Player drawPlayer, Vector2 position, float rotation, Vector2 rotationOrigin, float shadow, float scale) {
            bool hide = false;
            if (!Main.gameMenu) {
                if (Main.netMode == NetmodeID.MultiplayerClient && drawPlayer.GetModPlayer<AtbmPlayer>().Active && !drawPlayer.GetModPlayer<AtbmPlayer>().CanDraw)
                    hide = true;
                if (drawPlayer.Entropy().DontDrawTime > 0)
                    hide = true;
                if (drawPlayer.TryGetModPlayer<AcropolisArmorPlayer>(out var mp)) {
                    if (!mp.PlayerVisual)
                        hide = true;
                }
                scale *= drawPlayer.Entropy().Scale;
            }
            if (hide) {
                return;
            }
            if (cbpSmashType > 0 && drawPlayer.ownedProjectileCounts[cbpSmashType] > 0) {
                foreach (Projectile pj in Main.ActiveProjectiles) {
                    if (pj.owner == drawPlayer.whoAmI && pj.type == cbpSmashType) {
                        position = pj.Center;
                        break;
                    }
                }
            }
            orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, scale);
        }

        //弹幕之后、玩家之前:画转到玩家身后的那半圈护壳与护盾
        private static void DrawProjectilesHook(On_Main.orig_DrawProjectiles orig, Main self) {
            orig(self);
            Main.spriteBatch.begin_();
            foreach (Player player in Main.ActivePlayers) {
                CEWorldOverlayDraw.DrawOrbitingShells(player, backHalf: true);
            }
            Main.spriteBatch.End();
        }

        //orig 只能在方法末尾调一次。InnoVault 的 PRT 粒子层挂在同一个 DrawInfernoRings 钩子里(比本钩子先注册,
        //因而在 orig 链内),这里若在开头也调一次 orig,全部 PRT 粒子每帧就会被画两遍,加法粒子亮度直接翻倍。
        //旧 EParticle 时代粒子是在本钩子内、DrawMech 之后手动画一遍,所以保留末尾那次 orig 即可维持原先的层序。
        private static void DrawInfernoRingsHook(On_Main.orig_DrawInfernoRings orig, Main self) {
            CEWorldOverlayDraw.DrawAzafureChargeBar();
            foreach (Player player in Main.ActivePlayers) {
                CEWorldOverlayDraw.DrawLostHeirloomGlow(player);
                CEWorldOverlayDraw.DrawOrbitingShells(player, backHalf: false);
            }
            CEWorldOverlayDraw.DrawPermafrostRings();
            CEWorldOverlayDraw.DrawAcropolisMechs();
            orig(self);
        }

        private static void DrawMenuHook(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime) {
            orig(self, gameTime);
            EModSys.mi = false;
            //回到主菜单意味着世界已卸载,循环音的宿主实体都没了,必须逐个停掉再清表
            if (LoopSoundManager.sounds != null) {
                for (int i = 0; i < LoopSoundManager.sounds.Count; i++) {
                    LoopSoundManager.sounds[i].stop();
                }
                LoopSoundManager.sounds.Clear();
            }
        }

        private static void DrawCursorHook(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart) {
            if (!EModSys.mi) {
                orig(bonus, smart);
            }
        }

        private static Vector2 DrawThickCursorHook(On_Main.orig_DrawThickCursor orig, bool smart) {
            if (!EModSys.mi) {
                return orig(smart);
            }
            return Vector2.Zero;
        }
    }
}
