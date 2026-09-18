using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 表现门面(纯本地,不回写 gameplay):配色常量、闪现/爆闪粒子、震屏、找地面,纵深的呼啸/俯冲落地/落点标记,
    /// 以及权威端的清自家弹幕。状态与弹幕都从这里取共用动作,不各自散写。
    /// 贴图铁律(实机反馈 2026-09-17):BloomRing / Circle / StreakSolid / BasicTrail 这类底为不透明黑的灰度贴图
    /// 只能加法画,且环形与实心带状贴图不许非等比缩放:环压成椭圆会得到两侧薄、上下厚的歪光圈,
    /// 实心带拉成粗药丸叠在亮背景上就是一串粉紫椭圆。要椭圆就用等宽折线(见 DrawPortalAt)或 Glow 这类径向渐变
    /// </summary>
    public static class VDVfx
    {
        public static readonly Color VoidPurple = new Color(190, 60, 255);
        public static readonly Color VoidPink = new Color(255, 120, 255);
        public static readonly Color VoidWhite = new Color(230, 200, 255);
        public static readonly Color VoidDeep = new Color(110, 30, 190);
        public static readonly Color HellRed = new Color(255, 60, 60);
        public static readonly Color JungleGreen = new Color(80, 255, 120);
        public static readonly Color SkyBlue = new Color(80, 160, 255);
        public static readonly Color RiftWhite = new Color(240, 230, 255);
        public static readonly Color CannonCore = new Color(255, 200, 255);
        /// <summary>三阶段护盾的淡紫,天幕网格 P3 换成它</summary>
        public static readonly Color ShieldLavender = new Color(210, 160, 255);
        /// <summary>纵深雾色:退入深处的东西向它插值(冷蓝紫,比天幕地平线亮一档,远物暗而不透)</summary>
        public static readonly Color FarFog = new Color(70, 60, 130);
        /// <summary>纵深落点标记的基色:比虚空紫冷,与平面弹幕拉开</summary>
        public static readonly Color DepthMarker = new Color(150, 200, 255);

        //天幕「轨道封锁」配色:底幕暗、饱和度低,弹幕永远比天亮
        public static readonly Color SkyTop = new Color(8, 4, 18);
        public static readonly Color SkyHorizon = new Color(34, 16, 62);
        public static readonly Color SkyNebula = new Color(70, 30, 120);
        /// <summary>被侵蚀星球的挖口热边与外圈微晕</summary>
        public static readonly Color SkyErosion = new Color(190, 90, 255);
        /// <summary>星球边缘的冷紫背光</summary>
        public static readonly Color SkyPlanetRim = new Color(120, 80, 200);
        /// <summary>星球碎屑环的物质色(比背光暗、比底幕亮)</summary>
        public static readonly Color SkyRing = new Color(120, 60, 170);
        /// <summary>地表环境光被拉向的虚空暮色</summary>
        public static readonly Color SkyTileTint = new Color(90, 70, 130);

        /// <summary>
        /// GlowSpark 贴图是 256px 画布上一枚 230px 长的纺锤,仓库惯例 scale 0.06~0.16(15~40px 的火花)。
        /// 驱逐舰这批代码按「1 ≈ 一颗火花」书写,直接喂给 PRT 就把每粒火花画成巴掌大的紫椭圆(实机反馈 2026-09-18),
        /// 所以驱逐舰的火花只许经 <see cref="Spark"/> 生成,在这里统一折算:size 1 → 贴图 scale 0.12(约 28px)
        /// </summary>
        public const float SparkUnit = 0.12f;

        /// <summary>驱逐舰火花的唯一入口:size 以「一颗火花 ≈ 1」计,内部折算贴图 scale;gravity=false 只淡出不下坠、朝向钉在初速</summary>
        public static PRT_GlowSpark Spark(Vector2 pos, Vector2 vel, Color color, float size, float opacity, int life, bool gravity = false) {
            if (Main.dedServ) {
                return null;
            }
            var s = PRTLoader.NewParticle<PRT_GlowSpark>(pos, vel, color, size * SparkUnit)
                .Configure(opacity, true, PRTDrawModeEnum.AdditiveBlend, vel.ToRotation(), life);
            s.grav = gravity;
            return s;
        }

        /// <summary>闪现消失/出现的粒子(旧位置与新位置各一次)</summary>
        public static void BlinkBurst(Vector2 pos) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 24; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 9f);
                var p = PRTLoader.NewParticle<PRT_Void>(pos + v * 4f, v, Color.White, Main.rand.NextFloat(0.8f, 1.6f));
                p.Opacity = Main.rand.NextFloat(0.4f, 0.9f);
                p.ad = 0.03f;
            }
            for (int i = 0; i < 10; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4f, 12f);
                Spark(pos, v, VoidPurple, Main.rand.NextFloat(0.6f, 1.1f), 1f, 24, gravity: true);
            }
        }

        /// <summary>径向火花爆闪(转阶段/爆炸/收束释放);scale 以「一颗火花 ≈ 1」计</summary>
        public static void SparkBurst(Vector2 pos, Color color, int count, float minSpeed, float maxSpeed, int life = 36, float scaleMin = 0.8f, float scaleMax = 1.5f) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < count; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(minSpeed, maxSpeed);
                Spark(pos, v, color, Main.rand.NextFloat(scaleMin, scaleMax), 1f, life, gravity: true);
            }
        }

        /// <summary>虚空烟团(像素管线)</summary>
        public static void VoidPuff(Vector2 pos, Vector2 vel, float scale = 1.2f, float opacity = 0.7f) {
            if (Main.dedServ) {
                return;
            }
            var p = PRTLoader.NewParticle<PRT_Void>(pos, vel, Color.White, scale);
            p.Opacity = opacity;
            p.ad = 0.03f;
        }

        /// <summary>爆炸闪光(加法)</summary>
        public static void Explosion(Vector2 pos, float scale, int life = 30) {
            if (Main.dedServ) {
                return;
            }
            PRTLoader.NewParticle<PRT_EXPLOSION>(pos, Vector2.Zero, Color.White, scale)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, life);
        }

        /// <summary>全息碎片爆闪(全息生物出现/传送/消散)</summary>
        public static void HoloBurst(Vector2 pos, Color color) {
            SparkBurst(pos, color, 14, 3f, 9f, 22, 0.5f, 1f);
        }

        /// <summary>
        /// 掠过镜头的一瞬(深度实体的 Z 穿过 <see cref="VDDirector.DepthWhooshZ"/>):呼啸音 + 轻震屏 + 滤镜从投影点向外的径向拖影。
        /// 投影点是世界坐标(已投影),滤镜自己折成 UV
        /// </summary>
        public static void PassBy(Vector2 projectedWorld, float strength = 1f) {
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("vbdisapear", 0.6f, projectedWorld, 4, 0.7f * strength);
            CEUtils.SetShake(projectedWorld, VDDirector.WhooshShake * strength, 2400f);
            VDScreenFx.ReportWhoosh(projectedWorld, VDDirector.WhooshStreak * strength);
        }

        /// <summary>俯冲落地的一记:冲击环粒子 + 震屏 + 落地音</summary>
        public static void DiveShock(Vector2 pos, float strength = 1f) {
            if (Main.dedServ) {
                return;
            }
            Sound("VoidAttack", 0.9f, pos, 2);
            Shake(pos, VDDirector.DiveShake * strength);
            Explosion(pos, 0.8f * strength, 18);
            SparkBurst(pos, VoidPurple, (int)(30 * strength), 5f, 16f, 30);
            for (int i = 0; i < 12; i++) {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                VoidPuff(pos + v * 3f, v, 1.3f, 0.6f);
            }
        }

        /// <summary>
        /// 平面落点标记(所有 Z 轴弹幕的公平阀):收缩环随逼近变亮,末段闪白。progress 0 = 刚出现,1 = 即将命中。
        /// 只能加法画,环贴图等比缩放。<paramref name="ownBatch"/> 为假时调用方已开加法批次(点阵一次画几十枚,不逐枚切批次)
        /// </summary>
        public static void DrawDepthMarker(Vector2 worldPos, float progress, float radius, Color color, float opacity = 1f, bool ownBatch = true) {
            if (Main.dedServ || opacity <= 0.01f) {
                return;
            }
            progress = MathHelper.Clamp(progress, 0f, 1f);
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 pos = worldPos - Main.screenPosition;
            float flicker = progress > 0.8f ? 0.6f + 0.4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 50f) : 1f;
            Color c = Color.Lerp(color, Color.White, progress * progress) * (opacity * flicker);
            if (ownBatch) {
                Main.spriteBatch.UseAdditive();
            }
            //外环从 2.4 倍收缩到落点半径,内环反向慢转
            float outer = MathHelper.Lerp(2.4f, 1f, EaseOut(progress)) * radius * 2f / ring.Width;
            Main.spriteBatch.Draw(ring, pos, null, c * (0.35f + 0.55f * progress), Main.GlobalTimeWrappedHourly * 1.5f, ring.Size() / 2f, outer, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, pos, null, c * (0.25f + 0.35f * progress), -Main.GlobalTimeWrappedHourly * 2.2f, ring.Size() / 2f, outer * 0.6f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, pos, null, c * (0.25f + 0.45f * progress), 0f, glow.Size() / 2f, radius * 1.6f / glow.Width * (0.6f + 0.4f * progress), SpriteEffects.None, 0f);
            if (ownBatch) {
                CEUtils.ReSetToEndShader();
            }
        }

        /// <summary>一次性震屏(带距离衰减)</summary>
        public static void Shake(Vector2 center, float strength, float maxDist = 4000f) {
            if (Main.dedServ) {
                return;
            }
            CEUtils.SetShake(center, strength, maxDist);
        }

        public static void Sound(string name, float pitch, Vector2 pos, int maxInstances = 4, float volume = 1f) {
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound(name, pitch, pos, maxInstances, volume);
        }

        /// <summary>清掉本 Boss 自己的敌对弹幕(阶段切换/死亡时的公平阀),只在权威端做,Kill 自带同步</summary>
        public static void ClearOwnProjectiles() {
            if (VaultUtils.isClient) {
                return;
            }
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.hostile && p.ModProjectile is IVoidDestroyerProjectile) {
                    p.Kill();
                }
            }
        }

        /// <summary>从起点向下扫最多 60 格找实心方块顶面,找不到则退回 fallbackY</summary>
        public static Vector2 FindGround(Vector2 start, float fallbackY) {
            if (TryFindGround(start, out Vector2 ground)) {
                return ground;
            }
            return new Vector2(start.X, fallbackY);
        }

        /// <summary>玩家脚下 60 格内是否有可站地面(支援投送的门槛)</summary>
        public static bool HasGroundBelow(Vector2 start) {
            return TryFindGround(start, out _);
        }

        private static bool TryFindGround(Vector2 start, out Vector2 ground) {
            ground = start;
            int tx = (int)(start.X / 16f);
            int ty = (int)(start.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5) {
                return false;
            }
            for (int y = Math.Max(ty - 6, 10); y < Math.Min(ty + VDDirector.GroundScanTiles, Main.maxTilesY - 10); y++) {
                Tile t = Main.tile[tx, y];
                if (t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType]) {
                    ground = new Vector2(tx * 16f + 8f, y * 16f);
                    return true;
                }
            }
            return false;
        }

        /// <summary>四角方向:0 左上 1 右上 2 左下 3 右下</summary>
        public static readonly Vector2[] CornerDirs =
        {
            new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1)
        };

        /// <summary>二次缓出</summary>
        public static float EaseOut(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>三次缓入(入场/俯冲的「朝镜头飞来」曲线)</summary>
        public static float EaseInCubic(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * t;
        }
    }
}
