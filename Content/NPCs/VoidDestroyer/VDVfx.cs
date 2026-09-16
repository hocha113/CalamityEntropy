using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.VoidDestroyer;
using InnoVault;
using InnoVault.PRT;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 表现门面(纯本地,不回写 gameplay):配色常量、闪现/爆闪粒子、震屏、找地面,
    /// 以及权威端的清自家弹幕。状态与弹幕都从这里取共用动作,不各自散写
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

        /// <summary>闪现消失/出现的粒子(旧位置与新位置各一次)</summary>
        public static void BlinkBurst(Vector2 pos)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 24; i++)
            {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 9f);
                var p = PRTLoader.NewParticle<PRT_Void>(pos + v * 4f, v, Color.White, Main.rand.NextFloat(0.8f, 1.6f));
                p.Opacity = Main.rand.NextFloat(0.4f, 0.9f);
                p.ad = 0.03f;
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4f, 12f);
                PRTLoader.NewParticle<PRT_GlowSpark>(pos, v, VoidPurple, Main.rand.NextFloat(0.6f, 1.1f))
                    .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 24);
            }
        }

        /// <summary>径向火花爆闪(转阶段/爆炸/收束释放)</summary>
        public static void SparkBurst(Vector2 pos, Color color, int count, float minSpeed, float maxSpeed, int life = 36, float scaleMin = 0.8f, float scaleMax = 1.5f)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(minSpeed, maxSpeed);
                PRTLoader.NewParticle<PRT_GlowSpark>(pos, v, color, Main.rand.NextFloat(scaleMin, scaleMax))
                    .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), life);
            }
        }

        /// <summary>虚空烟团(像素管线)</summary>
        public static void VoidPuff(Vector2 pos, Vector2 vel, float scale = 1.2f, float opacity = 0.7f)
        {
            if (Main.dedServ)
            {
                return;
            }
            var p = PRTLoader.NewParticle<PRT_Void>(pos, vel, Color.White, scale);
            p.Opacity = opacity;
            p.ad = 0.03f;
        }

        /// <summary>爆炸闪光(加法)</summary>
        public static void Explosion(Vector2 pos, float scale, int life = 30)
        {
            if (Main.dedServ)
            {
                return;
            }
            PRTLoader.NewParticle<PRT_EXPLOSION>(pos, Vector2.Zero, Color.White, scale)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, life);
        }

        /// <summary>全息碎片爆闪(全息生物出现/传送/消散)</summary>
        public static void HoloBurst(Vector2 pos, Color color)
        {
            SparkBurst(pos, color, 14, 3f, 9f, 22, 0.5f, 1f);
        }

        /// <summary>一次性震屏(带距离衰减)</summary>
        public static void Shake(Vector2 center, float strength, float maxDist = 4000f)
        {
            if (Main.dedServ)
            {
                return;
            }
            CEUtils.SetShake(center, strength, maxDist);
        }

        public static void Sound(string name, float pitch, Vector2 pos, int maxInstances = 4, float volume = 1f)
        {
            if (Main.dedServ)
            {
                return;
            }
            CEUtils.PlaySound(name, pitch, pos, maxInstances, volume);
        }

        /// <summary>清掉本 Boss 自己的敌对弹幕(阶段切换/死亡时的公平阀),只在权威端做,Kill 自带同步</summary>
        public static void ClearOwnProjectiles()
        {
            if (VaultUtils.isClient)
            {
                return;
            }
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.hostile && p.ModProjectile is IVoidDestroyerProjectile)
                {
                    p.Kill();
                }
            }
        }

        /// <summary>从起点向下扫最多 60 格找实心方块顶面,找不到则退回 fallbackY</summary>
        public static Vector2 FindGround(Vector2 start, float fallbackY)
        {
            if (TryFindGround(start, out Vector2 ground))
            {
                return ground;
            }
            return new Vector2(start.X, fallbackY);
        }

        /// <summary>玩家脚下 60 格内是否有可站地面(支援投送的门槛)</summary>
        public static bool HasGroundBelow(Vector2 start)
        {
            return TryFindGround(start, out _);
        }

        private static bool TryFindGround(Vector2 start, out Vector2 ground)
        {
            ground = start;
            int tx = (int)(start.X / 16f);
            int ty = (int)(start.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return false;
            }
            for (int y = Math.Max(ty - 6, 10); y < Math.Min(ty + VDDirector.GroundScanTiles, Main.maxTilesY - 10); y++)
            {
                Tile t = Main.tile[tx, y];
                if (t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType])
                {
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
        public static float EaseOut(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>三次缓入(入场/俯冲的「朝镜头飞来」曲线)</summary>
        public static float EaseInCubic(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * t;
        }
    }
}
