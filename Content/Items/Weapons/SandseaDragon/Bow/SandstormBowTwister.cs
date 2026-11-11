using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Bow
{
    /// <summary>
    /// 沙龙卷,巨箭命中时左右各生成一道。横向起步慢、匀加速,每个敌人只打一次。外观与纵向范围照原版沙尘龙卷(aiStyle 127 与 Main.DrawProj 的 656 段)
    /// ai[0] 行进方向 ±1,ai[1] 已存在帧数。两者随生成包同步,之后的运动各端按同一套算式自己推
    /// </summary>
    public class SandstormBowTwister : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SandnadoFriendly;

        public const int Lifetime = 300;                //存在帧数,策划 5 秒
        private const int TimeLeftSlack = Lifetime * 2; //寿命看 ai[1],timeLeft 只留余量
        private const int FormFrames = 12;              //淡入成形的帧数,期间不判定
        private const int FadeFrames = 40;              //末尾淡出的帧数,期间不判定
        private const float StartSpeed = 0.8f;          //起步横向速度,像素每帧
        private const float Acceleration = 0.03f;       //横向加速度,像素每帧平方
        private const float MaxSpeed = 9f;              //横向速度上限,像素每帧
        private const int SpawnSize = 10;               //生成时的碰撞箱边长,同原版
        private const int CritterLifeMax = 5;           //原版以 lifeMax 5 分界小动物
        //以下照原版 aiStyle 127 取纵向范围与判定宽高
        private const int ExpandUpTiles = 15;           //自中心向上探空的格数
        private const int ExpandDownTiles = 15;         //向下探空的格数
        private const float WidthRatio = 0.13f;         //判定宽占高的比例,原版 0.2 × 0.65
        private const float TileSize = 16f;             //一格的边长,像素
        //以下是横移时才会遇到的情况,原版不动所以没有
        private const int MinWidth = 40;                //判定宽下限,像素,低矮处别窄得打不着
        private const float MinHeight = 48f;            //柱高低于它算被埋住,像素
        private const int ClimbTiles = 8;               //中心陷进实心物块时最多向上找空位的格数
        private const int BuriedKillFrames = 45;        //连续被埋这么多帧后消散
        private const float PresenceStep = 0.1f;        //被埋与脱出时显隐每帧的变化量
        //以下照 Main.DrawProj 的 656 段
        private const float RingSpacing = 5.1f;         //环片竖向间距,像素
        private const float RingSpin = MathHelper.Pi / 50f;             //整体自转,弧度每帧
        private const float RingTwistPerPixel = MathHelper.TwoPi / 20f; //沿高度每像素的扭转,弧度
        private const float RingScaleBottom = 0.85f;    //最底一片的缩放,原图 60 像素
        private const float RingScaleGrowth = 1f;       //从底到顶缩放的增量
        private const float RingAlphaScale = 0.5f;      //环片 A 通道折半
        //以下是在原版画法上加的动势
        private const float LeanPerSpeed = 2.5f;        //顶端落后根部的距离,每 1 像素/帧的速度对应的像素数
        private const float SwayAmplitude = 6f;         //顶端左右摆幅,像素
        private const float SwaySpeed = 0.09f;          //摆动角速度,弧度每帧
        private const float SwayWaves = 3.5f;           //从根到顶的摆动相位差,弧度
        private const float FormScaleStart = 0.6f;      //刚开始成形时环片的缩放比例

        private readonly bool[] puffed = new bool[Main.maxNPCs];    //各端自己记,已对哪些 NPC 扬过沙
        private bool buried;            //中心陷在实心物块里,或上下空间不够成柱
        private int buriedFrames;       //已连续被埋的帧数
        private float presence = 1f;    //0-1,被埋时淡出、脱出后淡回

        private int Direction => Projectile.ai[0] < 0f ? -1 : 1;
        private ref float Age => ref Projectile.ai[1];
        private float Fade => Utils.GetLerpValue(0f, FormFrames, Age, true) * Utils.GetLerpValue(Lifetime, Lifetime - FadeFrames, Age, true) * presence;
        //判定只在成形期间开
        private bool Formed => Age >= FormFrames && Age <= Lifetime - FadeFrames && !buried;

        public override void SetDefaults() {
            Projectile.width = SpawnSize;
            Projectile.height = SpawnSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.noEnchantmentVisuals = true;
            Projectile.timeLeft = TimeLeftSlack;
        }

        public override void AI() {
            if (Age >= Lifetime) {
                Projectile.Kill();
                return;
            }
            //一次命中生成左右两道,成形声只由向右的那道响
            if (Age == 0f && Direction > 0) {
                SandstormBowVfx.TwisterSpawn(Projectile.Center);
            }
            float speed = Math.Min(StartSpeed + Acceleration * Age, MaxSpeed);
            Projectile.velocity = new Vector2(Direction * speed, 0f);
            UpdateColumn();

            buriedFrames = buried ? buriedFrames + 1 : 0;
            presence = MathHelper.Clamp(presence + (buried ? -PresenceStep : PresenceStep), 0f, 1f);
            if (buriedFrames > BuriedKillFrames && Projectile.owner == Main.myPlayer) {
                Projectile.Kill();
                return;
            }
            if (Formed) {
                SandstormBowVfx.TwisterDust(Projectile.Center, Projectile.height);
                PuffOnContact();
            }
            Age++;
        }

        /// <summary>
        /// 照原版从中心所在的格子上下探空,把碰撞箱摆成那一段空柱。每帧中心向空柱中点靠一次,几帧内长到站在地上的全高
        /// 横坐标保持不动,原版把它吸到格子中线的做法在慢速横移时会吸住走不动
        /// </summary>
        private void UpdateColumn() {
            Vector2 center = Projectile.Center;
            Point tile = center.ToTileCoordinates();
            int row = tile.Y;
            //中心进了坡或台阶时往上找空位,爬上去
            for (int climbed = 0; climbed < ClimbTiles && WorldGen.SolidTile3(tile.X, row); climbed++) {
                row--;
            }
            buried = WorldGen.SolidTile3(tile.X, row);
            if (buried) {
                return;
            }
            Collision.ExpandVertically(tile.X, row, out int topRow, out int bottomRow, ExpandUpTiles, ExpandDownTiles);
            //探到的是挡住的那一格,各退一格才是空的;柱的两端落在这两格的中心
            float top = (topRow + 1) * TileSize + TileSize * 0.5f;
            float bottom = (bottomRow - 1) * TileSize + TileSize * 0.5f;
            float height = bottom - top;
            if (height < MinHeight) {
                buried = true;
                return;
            }
            Projectile.width = Math.Max(MinWidth, (int)(height * WidthRatio));
            Projectile.height = (int)height;
            Projectile.Center = new Vector2(center.X, (top + bottom) * 0.5f);
        }

        //卷到敌人时的扬沙各端自己判,命中钩子只在拥有者端跑,队友看不到
        private void PuffOnContact() {
            if (Main.dedServ) {
                return;
            }
            Rectangle box = Projectile.Hitbox;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (puffed[npc.whoAmI] || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= CritterLifeMax || !box.Intersects(npc.Hitbox)) {
                    continue;
                }
                puffed[npc.whoAmI] = true;
                SandstormBowVfx.TwisterHit(npc.Hitbox, Direction);
            }
        }

        public override bool? CanDamage() {
            return Formed ? null : false;
        }

        //龙卷穿墙横扫上百格,不让它一路割草破罐
        public override bool? CanCutTiles() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            float fade = Fade;
            float height = Projectile.height;
            if (fade <= 0f || height < MinHeight) {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bottom = Projectile.Bottom - Main.screenPosition;
            float age = Age;
            float formScale = MathHelper.Lerp(FormScaleStart, 1f, Utils.GetLerpValue(0f, FormFrames, age, true));
            float lag = -Direction * Math.Abs(Projectile.velocity.X) * LeanPerSpeed;
            for (float rise = RingSpacing; rise < height; rise += RingSpacing) {
                float along = rise / height;    //0 根部,1 顶端
                //两端透明、中段最实,颜色从根部的橙过渡到沙黄
                Color color = Color.Lerp(SandstormBowVfx.SandWarm, SandstormBowVfx.Sand, along);
                color = Color.Lerp(Color.Transparent, color, along < 0.5f ? along * 2f : 2f - along * 2f);
                color.A = (byte)(color.A * RingAlphaScale);
                color *= fade;
                float offset = (lag + MathF.Sin(age * SwaySpeed + along * SwayWaves) * SwayAmplitude) * along;
                Vector2 position = bottom + new Vector2(offset, RingSpacing - rise);
                float scale = (RingScaleBottom + RingScaleGrowth * along) * formScale;
                Main.EntitySpriteDraw(texture, position, null, color, -RingSpin * age - RingTwistPerPixel * rise, origin, scale, SpriteEffects.None);
            }
            return false;
        }
    }
}
