using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Chainsaw;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using static CalamityEntropy.CalamityEntropy;

namespace CalamityEntropy.Core.Graphics.Screen
{
    /// <summary>
    /// 虚空系全屏特效:遮罩先攒进交换缓冲,再由 cvoid / cvoid2 / cvoid3 / kscreen2 四个着色器合成回主屏。
    /// 这些通道整体依赖 RT,复古 / 迷幻光照下由 <see cref="CEScreenPipeline"/> 整段跳过。
    /// <para>
    /// 注意 <see cref="DrawVoidStarWake"/> 里那批实体绘制画的是喂给 cvoid2 的遮罩、不是可见精灵,
    /// 所以它不属于 <see cref="CEEntityOverlay"/>,不做兜底。
    /// </para>
    /// </summary>
    internal static class CEVoidScreen
    {
        /// <summary>
        /// 虚空遮罩:巡洋者刀光、黑洞弹、虚空怪与 PRT_Void 粒子的 cvmask 光斑一起画进交换缓冲,
        /// 交给 <see cref="DrawVoidParticles"/> 与 <see cref="ApplyVoidBackground"/> 当着色器输入
        /// </summary>
        public static void DrawVoidMasks(GraphicsDevice graphicsDevice) {
            if (CEScreenPipeline.Screen0 == null) {
                return;
            }

            CEScreenPipeline.CaptureScreenTo0(graphicsDevice);

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            int cruiserSlashType = ModContent.ProjectileType<CruiserSlash>();
            int cruiserBlackholeBulletType = ModContent.ProjectileType<CruiserBlackholeBullet>();
            int voidBulletType = ModContent.ProjectileType<VoidBullet>();
            int voidMonsterType = ModContent.ProjectileType<VoidMonster>();

            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == cruiserSlashType) {
                    if (p.ModProjectile is CruiserSlash cs && cs.ct > 60) {
                        Main.spriteBatch.Draw(EffectLoader.cruiserSlash.Value, p.Center - Main.screenPosition + new Vector2((p.ai[0] + p.ai[1]) / 2 - 300, 0).RotatedBy(p.rotation), null, Color.White, p.rotation, new Vector2(EffectLoader.cruiserSlash.Value.Width, EffectLoader.cruiserSlash.Value.Height) / 2, new Vector2((p.ai[0] - p.ai[1]) / EffectLoader.cruiserSlash.Value.Width, 1.2f), SpriteEffects.None, 0);
                    }
                }
                else if (p.type == cruiserBlackholeBulletType) {
                    Main.spriteBatch.Draw(EffectLoader.cruiserBlackholeBullet.Value, p.Center - Main.screenPosition, null, Color.White, p.rotation, new Vector2(EffectLoader.cruiserBlackholeBullet.Value.Width, EffectLoader.cruiserBlackholeBullet.Value.Height) / 2, p.scale, SpriteEffects.None, 0);
                }
                else if (p.type == voidBulletType) {
                    Main.spriteBatch.Draw(EffectLoader.cruiserBlackholeBullet.Value, p.Center - Main.screenPosition, null, Color.White, p.rotation, new Vector2(EffectLoader.cruiserBlackholeBullet.Value.Width, EffectLoader.cruiserBlackholeBullet.Value.Height) / 2, p.scale, SpriteEffects.None, 0);
                }
                else if (p.type == voidMonsterType) {
                    if (p.ModProjectile is VoidMonster vmnpc) {
                        vmnpc.draw();
                    }
                }
            }

            foreach (var pt in PRTLoader.PRT_InGame_World_Inds) {
                if (!pt.active || pt.Mod != Instance) {
                    continue;
                }
                //is PRT_Void && is not PRT_Abyssal:两套RT shader分流,条件写反就画错桶
                if (pt is not PRT_Void || pt is PRT_Abyssal) {
                    continue;
                }
                if (EffectLoader.cvmask == null) {
                    continue;
                }
                Main.spriteBatch.Draw(EffectLoader.cvmask.Value, pt.Position - Main.screenPosition, null, Color.White * 0.06f, pt.Rotation, EffectLoader.cvmask.Value.Size() / 2, (5.4f * pt.Opacity) * 0.05f, SpriteEffects.None, 0);
            }

            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile != null) {
                    if (p.ModProjectile is Pioneer1 p1) {
                        p1.drawVoid();
                    }
                }
            }

            Main.spriteBatch.End();
        }

        /// <summary>PRT_Void 的 shape4→Screen1(Additive)→kscreen2→Screen2,不进常规 PRT 桶</summary>
        public static void DrawVoidParticles(GraphicsDevice graphicsDevice) {
            graphicsDevice.SetRenderTarget(CEScreenPipeline.Screen1);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            foreach (var pt in PRTLoader.PRT_InGame_World_Inds) {
                if (!pt.active || pt.Mod != Instance) {
                    continue;
                }
                //过滤条件和 DrawNonPixVoid / CEAbyssScreen 镜像,is PRT_Abyssal 走另一套RT
                if (pt is not PRT_Void || pt is PRT_Abyssal) {
                    continue;
                }
                if (pt is PRT_Void voidPt && voidPt.shape != 4)   //只有shape4进kscreen2合成,别的走常规PRT桶
                {
                    continue;
                }
                Texture2D draw = EffectLoader.CvdtTex.Value;
                Main.spriteBatch.Draw(draw, pt.Position - Main.screenPosition, null, Color.White, pt.Rotation, draw.Size() / 2, 2.2f * pt.Opacity, SpriteEffects.None, 0);
            }

            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(CEScreenPipeline.Screen2);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            EffectLoader.kscreen2.CurrentTechnique = EffectLoader.kscreen2.Techniques["Technique1"];
            EffectLoader.kscreen2.CurrentTechnique.Passes[0].Apply();
            EffectLoader.kscreen2.Parameters["tex0"].SetValue(CEScreenPipeline.Screen1);
            EffectLoader.kscreen2.Parameters["tex1"].SetValue(CEExtraAssets.EternityStreak);
            EffectLoader.kscreen2.Parameters["offset"].SetValue(Main.screenPosition / Main.ScreenSize.ToVector2());
            EffectLoader.kscreen2.Parameters["i"].SetValue(0.04f);
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        /// <summary>把 Screen2 的虚空遮罩经 cvoid 合成回主屏,这一步决定了虚空背景的整体观感</summary>
        public static void ApplyVoidBackground(GraphicsDevice graphicsDevice) {
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            Main.spriteBatch.Draw(CEScreenPipeline.Screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            EffectLoader.cvoid.CurrentTechnique = EffectLoader.cvoid.Techniques["Technique1"];
            EffectLoader.cvoid.CurrentTechnique.Passes[0].Apply();
            EffectLoader.cvoid.Parameters["tex1"].SetValue(EffectLoader.planetarium_blue_base.Value);
            EffectLoader.cvoid.Parameters["tex2"].SetValue(CEExtraAssets.Empty);
            EffectLoader.cvoid.Parameters["tex3"].SetValue(CEExtraAssets.Empty);
            EffectLoader.cvoid.Parameters["tex4"].SetValue(CEExtraAssets.Empty);
            EffectLoader.cvoid.Parameters["tex5"].SetValue(CEExtraAssets.Empty);
            EffectLoader.cvoid.Parameters["tex6"].SetValue(CEExtraAssets.Empty);
            EffectLoader.cvoid.Parameters["time"].SetValue(Instance.cvcount / 50f);
            EffectLoader.cvoid.Parameters["scsize"].SetValue(Main.ScreenSize.ToVector2());
            EffectLoader.cvoid.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(-Instance.cvcount / 6f, Instance.cvcount / 6f)) / Main.ScreenSize.ToVector2());
            Main.spriteBatch.Draw(CEScreenPipeline.Screen2, Main.ScreenSize.ToVector2() * 0.5f, null, Color.White, 0, CEScreenPipeline.Screen2.Size() * 0.5f, 1, Main.LocalPlayer.gravDir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        /// <summary>寂静之翼专用的那条 cvoid3 通道,没有对应弹幕存活时整段不跑</summary>
        public static void DrawNonPixVoid(GraphicsDevice graphicsDevice) {
            if (CEScreenPipeline.Screen0 == null || !CEUtils.AnyActiveProj<WOHHeld>()) {
                return;
            }

            CEScreenPipeline.CaptureScreenTo0(graphicsDevice);

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);

            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile != null) {
                    if (p.ModProjectile is WOHHeld woh)
                        woh.DrawVoid();
                }
            }

            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            Main.spriteBatch.Draw(CEScreenPipeline.Screen0, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            EffectLoader.cvoid3.CurrentTechnique.Passes[0].Apply();
            EffectLoader.cvoid3.Parameters["tex1"].SetValue(EffectLoader.planetarium_blue_base.Value);
            EffectLoader.cvoid3.Parameters["time"].SetValue(Instance.cvcount / 50f);
            EffectLoader.cvoid3.Parameters["scsize"].SetValue(Main.ScreenSize.ToVector2());
            EffectLoader.cvoid3.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(-Instance.cvcount / 6f, Instance.cvcount / 6f)) / Main.ScreenSize.ToVector2());
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// 虚空星的尾迹与护盾遮罩,经 cvoid2 合成。历史函数名是 DrawRandomEffect,
        /// 实际内容是虚空星轨迹 + 光语火焰 + 魔能护盾 + 巡洋者残影传送门
        /// </summary>
        public static void DrawVoidStarWake(GraphicsDevice graphicsDevice) {
            CEScreenPipeline.CaptureScreenTo0(graphicsDevice);

            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile == null) {
                    continue;
                }
                if (p.ModProjectile is VoidStar) {
                    if (p.ai[0] >= 60 || p.ai[2] == 0) {
                        VoidStar mp = (VoidStar)p.ModProjectile;
                        mp.odp.Add(p.Center);
                        if (mp.odp.Count > 2) {
                            float size = 10;
                            float sizej = size / mp.odp.Count;
                            Color cl = new Color(200, 235, 255);
                            for (int i = mp.odp.Count - 1; i >= 1; i--) {
                                CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, mp.odp[i], mp.odp[i - 1], cl * ((255 - p.alpha) / 255f), size * 0.25f);
                                size -= sizej;
                            }
                        }
                        mp.odp.RemoveAt(mp.odp.Count - 1);
                    }

                }
                if (p.ModProjectile is VoidStarF) {
                    VoidStarF mp = (VoidStarF)p.ModProjectile;
                    mp.odp.Add(p.Center);
                    if (mp.odp.Count > 2) {
                        float size = 10;
                        float sizej = size / mp.odp.Count;
                        Color cl = new Color(200, 235, 255);
                        if (p.ai[2] > 0) {
                            cl = new Color(255, 160, 160);
                        }
                        for (int i = mp.odp.Count - 1; i >= 1; i--) {
                            CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, mp.odp[i], mp.odp[i - 1], cl * ((255 - p.alpha) / 255f), size * 0.25f);
                            size -= sizej;
                        }
                    }
                    mp.odp.RemoveAt(mp.odp.Count - 1);

                }
                if (p.ModProjectile is LightWisperFlame lwf) {
                    lwf.draw();
                }
            }
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (Player p in Main.ActivePlayers) {
                if (!p.dead && p.Entropy().MagiShield > 0 && p.Entropy().visualMagiShield) {
                    Texture2D shieldTexture = EffectLoader.ShieldTex.Value;
                    Main.spriteBatch.Draw(shieldTexture, p.Center - Main.screenPosition, null, new Color(186, 120, 255), 0, shieldTexture.Size() / 2, 0.47f, SpriteEffects.None, 0);
                }
            }
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile != null && p.ModProjectile is MoonlightShieldBreak) {
                    Texture2D shieldTexture = EffectLoader.ShieldTex.Value;
                    Main.spriteBatch.Draw(shieldTexture, p.Center - Main.screenPosition, null, new Color(186, 120, 255) * p.ai[2], 0, shieldTexture.Size() / 2, 0.47f * (1 + p.ai[1]), SpriteEffects.None, 0);
                }
                if (p.ModProjectile != null && p.ModProjectile is CruiserShadow aw) {
                    if (aw.alphaPor > 0) {
                        float s = 0;
                        float sj = 1;
                        for (int i = 0; i <= 30; i++) {
                            aw.DrawPortal(aw.spawnPos, new Color(50, 35, 240) * aw.alphaPor, aw.spawnRot, 270 * s, 0.3f, i * 3f);
                            s = s + (sj - s) * 0.05f;
                        }
                    }
                }
            }
            Main.spriteBatch.End();
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
            EffectLoader.cvoid2.CurrentTechnique = EffectLoader.cvoid2.Techniques["Technique1"];
            EffectLoader.cvoid2.CurrentTechnique.Passes[0].Apply();
            EffectLoader.cvoid2.Parameters["tex0"].SetValue(Main.screenTargetSwap);
            EffectLoader.cvoid2.Parameters["tex1"].SetValue(CEExtraAssets.VoidBack);
            EffectLoader.cvoid2.Parameters["time"].SetValue(Instance.cvcount / 50f);
            EffectLoader.cvoid2.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(Instance.cvcount * 1.4f, Instance.cvcount * 1.4f)) / new Vector2(Main.screenWidth, Main.screenHeight));
            Main.spriteBatch.Draw(CEScreenPipeline.Screen0, Main.ScreenSize.ToVector2() / 2, null, Color.White, 0, Main.ScreenSize.ToVector2() / 2, 1, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }
    }
}
