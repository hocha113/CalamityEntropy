using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarBlessing
{
    /// <summary>
    /// 召唤物打中带本鞭标记的目标时冒出的紫色小星座:星点依次被细线连起,停一下后淡出;
    /// 只是画面,不造成伤害;图案以 ai[0] 为种子,各端画出来一样;ai[1] 为跟随的目标序号+1
    /// </summary>
    public class StarBlessingConstellation : ModProjectile
    {
        #region 调参旋钮
        //星点数量上下限(含)
        public const int MinStars = 4;
        public const int MaxStars = 6;
        //相邻星点距离(像素)与每一步最多拐的角度(弧度)
        public const float StepMin = 14f;
        public const float StepMax = 24f;
        public const float TurnMax = 1.3f;
        //连线全部画完、画完后停留、淡出各自的帧数;星点冒出来的帧数
        public const int DrawInFrames = 16;
        public const int HoldFrames = 10;
        public const int FadeFrames = 16;
        public const int PopFrames = 4;
        //连线外发光与实线宽度(像素)
        public const float LineGlowWidth = 5f;
        public const float LineWidth = 1.2f;
        //星点:星芒长短两轴缩放(Extra_98 高72像素)、中心光点缩放(Glow2 原图256像素)、大小随机范围
        public static readonly Vector2 FlareLong = new(0.1f, 0.32f);
        public static readonly Vector2 FlareShort = new(0.1f, 0.2f);
        public const float DotScale = 0.06f;
        public const float SizeMin = 0.7f;
        public const float SizeMax = 1.2f;
        //闪烁幅度与速度(弧度/帧)
        public const float Twinkle = 0.2f;
        public const float TwinkleSpeed = 0.5f;
        #endregion

        public const int Life = DrawInFrames + HoldFrames + FadeFrames;

        public override string Texture => CEUtils.WhiteTexPath;

        private Vector2[] stars;
        private int[] parent;
        private float[] sizes;
        private Vector2 offset;

        private NPC Host {
            get {
                int i = (int)Projectile.ai[1] - 1;
                if (i < 0 || i >= Main.maxNPCs)
                    return null;
                NPC npc = Main.npc[i];
                return npc.active ? npc : null;
            }
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Life;
        }

        public override bool? CanDamage() => false;

        public override bool? CanCutTiles() => false;

        public override void AI() {
            NPC host = Host;
            if (stars == null) {
                Build();
                if (host != null)
                    offset = Projectile.Center - host.Center;
            }
            if (host != null)
                Projectile.Center = host.Center + offset;
            Projectile.velocity = Vector2.Zero;
        }

        //随机走几步连成一串;五颗以上时最后一颗从中间某颗岔出去,像星座里的分叉
        private void Build() {
            UnifiedRandom rand = new((int)Projectile.ai[0]);
            int count = rand.Next(MinStars, MaxStars + 1);
            stars = new Vector2[count];
            parent = new int[count];
            sizes = new float[count];
            float dir = rand.NextFloat(MathHelper.TwoPi);
            parent[0] = -1;
            sizes[0] = rand.NextFloat(SizeMin, SizeMax);
            for (int i = 1; i < count; i++) {
                sizes[i] = rand.NextFloat(SizeMin, SizeMax);
                bool branch = i == count - 1 && count >= 5;
                parent[i] = branch ? rand.Next(1, count - 2) : i - 1;
                dir += branch ? MathHelper.PiOver2 * (rand.NextBool() ? 1f : -1f) : rand.NextFloat(-TurnMax, TurnMax);
                stars[i] = stars[parent[i]] + dir.ToRotationVector2() * rand.NextFloat(StepMin, StepMax);
            }
            Vector2 mid = Vector2.Zero;
            foreach (Vector2 s in stars)
                mid += s;
            mid /= count;
            for (int i = 0; i < count; i++)
                stars[i] -= mid;
        }

        public override bool PreDraw(ref Color lightColor) {
            if (stars == null)
                return false;
            int age = Life - Projectile.timeLeft;
            float alpha = Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);
            float per = DrawInFrames / (float)(stars.Length - 1);
            Vector2 center = Projectile.Center;
            //A为0的颜色在常规混合下等同叠加发光
            Color glow = StarBlessingProj.Violet;
            glow.A = 0;
            Color bright = StarBlessingProj.VioletLight;
            bright.A = 0;
            Color white = Color.White;
            white.A = 0;
            for (int i = 1; i < stars.Length; i++) {
                float t = Utils.GetLerpValue((i - 1) * per, i * per, age, true);
                if (t <= 0f)
                    continue;
                Vector2 a = center + stars[parent[i]];
                Vector2 b = Vector2.Lerp(a, center + stars[i], t);
                CEUtils.drawLineBetter(a, b, glow * (0.7f * alpha), LineGlowWidth);
                CEUtils.drawLine(a, b, StarBlessingProj.VioletLight * (0.8f * alpha), LineWidth);
            }
            Texture2D flare = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 flareOrigin = flare.Size() / 2f;
            Texture2D dot = CEExtraAssets.Glow2;
            Vector2 dotOrigin = dot.Size() / 2f;
            for (int i = 0; i < stars.Length; i++) {
                float appear = i * per;
                float pop = Utils.GetLerpValue(appear, appear + PopFrames, age, true);
                if (pop <= 0f)
                    continue;
                float s = sizes[i] * pop * (1f + Twinkle * MathF.Sin(age * TwinkleSpeed + i * 1.7f));
                Vector2 pos = center + stars[i] - Main.screenPosition;
                //亮背景上只有叠加层会看不见,底下垫一颗实色小点
                Main.EntitySpriteDraw(dot, pos, null, StarBlessingProj.Violet * alpha, 0f, dotOrigin, DotScale * 0.6f * s, SpriteEffects.None);
                Main.EntitySpriteDraw(flare, pos, null, bright * alpha, 0f, flareOrigin, FlareLong * s, SpriteEffects.None);
                Main.EntitySpriteDraw(flare, pos, null, bright * alpha, MathHelper.PiOver2, flareOrigin, FlareShort * s, SpriteEffects.None);
                Main.EntitySpriteDraw(dot, pos, null, white * alpha, 0f, dotOrigin, DotScale * s, SpriteEffects.None);
            }
            return false;
        }
    }
}
