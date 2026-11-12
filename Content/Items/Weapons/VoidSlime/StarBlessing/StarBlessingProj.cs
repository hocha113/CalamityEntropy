using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarBlessing
{
    /// <summary>
    /// 群星之祝的鞭身:一串紫色星座,每隔几节一颗星,星与星之间拉细线;
    /// 甩响那一下从鞭梢放出星团;双子盒复制出来的那条不放,一次挥鞭只出一组
    /// </summary>
    public class StarBlessingProj : BaseWhip
    {
        #region 调参旋钮
        //鞭节数与长度倍率:鞭长约336像素(21格),万花筒约352
        public const int SegmentCount = 21;
        public const float LengthMult = 2.1f;
        //贴图自上而下分段的高度(像素):柄、中星、小星、鞭梢
        public const int HandlePx = 22;
        public const int StarPx = 14;
        public const int TipPx = 26;
        //每隔几个鞭节画一颗星,连线只连这些星
        public const int StarStride = 3;
        //连线实线宽与外发光宽(像素)
        public const float LineWidth = 2f;
        public const float LineGlowWidth = 6f;
        //自发光比例,0 完全随环境光、1 不受光照
        public const float SelfLight = 0.7f;
        //星点与鞭梢的光晕缩放(Glow2 原图256像素)与不透明度
        public const float StarGlowScale = 0.14f;
        public const float TipGlowScale = 0.3f;
        public const float GlowOpacity = 0.6f;
        //星团6发、33%面板(策划);出手速度(像素/更新)与扇形张角(弧度)
        public const int ClusterCount = 6;
        public const float ClusterDamageMult = 0.33f;
        public const float ClusterSpeed = 9f;
        public const float ClusterFan = MathHelper.Pi * 0.9f;
        //甩响时鞭梢星芒的缩放与寿命(帧)
        public const float CrackFlashScale = 0.6f;
        public const int CrackFlashLife = 16;
        //鞭身沿途的碎光:每帧1/N概率、寿命(帧)、缩放;星点处的光照强度
        public const int MoteChance = 3;
        public const int MoteLife = 20;
        public const float MoteScale = 0.25f;
        public const float LightStrength = 0.45f;
        #endregion

        //连线与星点用紫,鞭梢与碎光掺金
        public static readonly Color Violet = new(178, 96, 222);
        public static readonly Color VioletLight = new(226, 178, 255);
        public static readonly Color Gold = new(236, 190, 90);

        public override void SetDefaults() {
            base.SetDefaults();
            segments = SegmentCount;
            rangeMult = LengthMult;
        }

        public override void WhipAI() {
            if (Projectile.ai[0] == (int)(flyTime / 2f)) {
                Vector2 tip = Projectile.WhipPointsForCollision[^1];
                if (Projectile.owner == Main.myPlayer && Projectile.Entropy().IndexOfTwistedTwinShootedThisProj < 0
                    && Main.player[Projectile.owner].GetModPlayer<StarBlessingPlayer>().TryRelease())
                    ReleaseClusters(tip);
                if (!Main.dedServ)
                    PRTLoader.NewParticle<PRT_SparkleCal>(tip, Vector2.Zero, VioletLight, CrackFlashScale).Configure(Violet, CrackFlashLife, 0.05f, 1.2f);
            }
            if (Main.dedServ)
                return;
            List<Vector2> points = new();
            Projectile.FillWhipControlPoints(Projectile, points);
            foreach (int i in Anchors(points.Count))
                Lighting.AddLight(points[i], Violet.ToVector3() * LightStrength);
            if (Projectile.FinalExtraUpdate() && Main.rand.NextBool(MoteChance)) {
                Vector2 pos = points[Main.rand.Next(points.Count)];
                Color color = Main.rand.NextBool(3) ? Gold : VioletLight;
                PRTLoader.NewParticle<PRT_Light>(pos, Main.rand.NextVector2Circular(0.6f, 0.6f), color, MoteScale).Configure(1f, lifetime: MoteLife);
            }
        }

        //从鞭梢朝外撒成扇形;伤害按面板算,不受鞭子本身伤害变化影响
        private void ReleaseClusters(Vector2 tip) {
            Player owner = Main.player[Projectile.owner];
            Vector2 away = (tip - owner.Center).SafeNormalize(Vector2.UnitX * Projectile.direction);
            int panel = ownerItem != null ? owner.GetWeaponDamage(ownerItem) : Projectile.damage;
            int damage = (int)(panel * ClusterDamageMult);
            int type = ModContent.ProjectileType<StarBlessingCluster>();
            for (int i = 0; i < ClusterCount; i++) {
                float t = i / (float)(ClusterCount - 1) - 0.5f;
                Vector2 vel = away.RotatedBy(ClusterFan * t) * ClusterSpeed;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), tip, vel, type, damage, Projectile.knockBack * 0.5f, Projectile.owner, 0f, i);
            }
        }

        //画星的鞭节:柄、每隔 StarStride 一节、鞭梢
        private static List<int> Anchors(int count) {
            List<int> list = new() { 0 };
            for (int i = StarStride; i < count - 1; i += StarStride)
                list.Add(i);
            list.Add(count - 1);
            return list;
        }

        private static Color SelfLit(Vector2 worldPos, Color tint) {
            Color lit = Lighting.GetColor((int)(worldPos.X / 16f), (int)(worldPos.Y / 16f)).MultiplyRGBA(tint);
            return Color.Lerp(lit, tint, SelfLight);
        }

        public override void DrawStrings(List<Vector2> points) {
            List<int> anchors = Anchors(points.Count);
            Color glow = Violet;
            glow.A = 0;
            for (int k = 1; k < anchors.Count; k++) {
                Vector2 a = points[anchors[k - 1]];
                Vector2 b = points[anchors[k]];
                CEUtils.drawLineBetter(a, b, glow * 0.7f, LineGlowWidth * Projectile.scale);
                CEUtils.drawLine(a, b, SelfLit(Vector2.Lerp(a, b, 0.5f), VioletLight), LineWidth * Projectile.scale);
            }
        }

        //只在画星的鞭节上画贴图,中星、小星交替
        public override void DrawSegs(List<Vector2> points) {
            Texture2D tex = Projectile.GetTexture();
            List<int> anchors = Anchors(points.Count);
            SpriteEffects flip = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            for (int k = 0; k < anchors.Count; k++) {
                int i = anchors[k];
                Rectangle frame;
                if (k == 0)
                    frame = new Rectangle(0, 0, tex.Width, HandlePx);
                else if (k == anchors.Count - 1)
                    frame = new Rectangle(0, tex.Height - TipPx, tex.Width, TipPx);
                else
                    frame = new Rectangle(0, HandlePx + (k % 2 == 1 ? 0 : StarPx), tex.Width, StarPx);
                Vector2 dir = i == points.Count - 1 ? points[i] - points[i - 1] : points[i + 1] - points[i];
                float rot = dir.ToRotation() - MathHelper.PiOver2;
                Main.EntitySpriteDraw(tex, points[i] - Main.screenPosition, frame, SelfLit(points[i], Color.White), rot, frame.Size() / 2f, Projectile.scale, flip);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            List<Vector2> points = new();
            Projectile.FillWhipControlPoints(Projectile, points);
            List<int> anchors = Anchors(points.Count);
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 origin = glow.Size() / 2f;
            //A为0的颜色在常规混合下等同叠加发光
            for (int k = 1; k < anchors.Count; k++) {
                bool tip = k == anchors.Count - 1;
                Color color = tip ? Gold : Violet;
                color.A = 0;
                Main.EntitySpriteDraw(glow, points[anchors[k]] - Main.screenPosition, null, color * GlowOpacity, 0f, origin, (tip ? TipGlowScale : StarGlowScale) * Projectile.scale, SpriteEffects.None);
            }
            DrawStrings(points);
            DrawSegs(points);
            return false;
        }
    }
}
