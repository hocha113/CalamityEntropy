using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.SweetRain
{
    /// <summary>
    /// 甘霖法杖的雨滴,由 <see cref="SweetRainCloud"/> 在拥有者端生成
    /// <br/>草药催熟只在服务器(单人为本机)改物块,同步照原版 WorldGen.GrowAlch 用 SendTileSquare
    /// </summary>
    public class SweetRainDrop : ModProjectile
    {
        public const int HitWidth = 8;              //判定宽,像素
        public const int HitHeight = 22;            //判定高,像素,罩住雨滴的头,长尾在框外
        public const int MaxHits = 2;               //可命中次数,即穿透一次敌怪
        public const int LocalHitCooldown = 30;     //局部无敌帧
        public const int LifeFrames = 120;          //最长存活帧,约 1300 像素落程
        public const int FadeInFrames = 4;          //出云淡入帧数
        public const int FadeOutFrames = 10;        //寿命将尽的淡出帧数
        public const float MinLight = 0.5f;         //亮度下限 0-1
        public const float TileSize = 16f;          //一格物块的像素

        //草药,frameX 取自原版 WorldGen.GrowAlch
        public const int BlinkrootFrameX = 36;      //闪耀根
        public const int ShiverthornFrameX = 108;   //寒颤棘
        public static readonly bool BloomStoredHerbs = true;    //开花态存成物块类型的这两种是否直接催到开花
        public const int HerbDustCount = 6;         //催熟时的草叶尘数
        public const float HerbDustRadius = 6f;     //草叶尘散布半径,像素
        public const float HerbDustSpread = 1.2f;   //草叶尘横向初速上限,像素/帧
        public const float HerbDustRiseMin = 0.8f;  //草叶尘上弹初速下限,像素/帧
        public const float HerbDustRiseMax = 2.2f;  //上弹初速上限,像素/帧
        public const float HerbVolume = 0.4f;       //催熟音量 0-1
        public const float HerbPitch = 0.35f;       //催熟音高偏移
        public const int HerbInstances = 2;         //催熟声同时实例上限

        //水花
        public const int SplashDustHit = 3;         //命中敌怪溅起的水尘数
        public const int SplashDustTile = 4;        //落地溅起的水尘数
        public const float SplashSpread = 1.6f;     //水尘横向初速上限,像素/帧
        public const float SplashRiseMin = 0.8f;    //水尘上弹初速下限,像素/帧
        public const float SplashRiseMax = 2.4f;    //上弹初速上限,像素/帧
        public const float SplashInherit = 0.5f;    //水尘继承雨滴横向速度的比例
        public const float SplashScaleMin = 0.9f;   //水尘缩放下限
        public const float SplashScaleMax = 1.3f;   //水尘缩放上限
        public const int SplashAlpha = 60;          //水尘透明度 0-255
        public const int TileSoundChance = 6;       //落地每几滴响一声
        public const int HitSoundChance = 2;        //命中每几滴响一声
        public const float SplashVolume = 0.28f;    //滴水声音量 0-1
        public const float SplashPitchVar = 0.5f;   //滴水声音高浮动
        public const int SplashInstances = 4;       //滴水声同时实例上限,每秒 36 滴不堆音

        private static readonly SoundStyle SplashSound = SoundID.Drip with { Volume = SplashVolume, PitchVariance = SplashPitchVar, MaxInstances = SplashInstances, Type = SoundType.Sound };
        private static readonly SoundStyle HerbSound = SoundID.Grass with { Volume = HerbVolume, Pitch = HerbPitch, MaxInstances = HerbInstances };

        private bool hitTile;           //本端这滴是撞物块死的
        private int splashedNPC = -1;   //非拥有者端上次补过水花的敌怪下标

        public override void SetDefaults() {
            Projectile.width = HitWidth;
            Projectile.height = HitHeight;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.penetrate = MaxHits;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = LocalHitCooldown;
            Projectile.timeLeft = LifeFrames;
            Projectile.alpha = byte.MaxValue;
        }

        //友方弹幕默认会割掉草药与花草(tileCut),原版雨滴靠 aiStyle 45 豁免,这里手动关掉
        public override bool? CanCutTiles() => false;

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            if (Projectile.timeLeft <= FadeOutFrames)
                Projectile.alpha = Math.Min(byte.MaxValue, Projectile.alpha + byte.MaxValue / FadeOutFrames);
            else if (Projectile.alpha > 0)
                Projectile.alpha = Math.Max(0, Projectile.alpha - byte.MaxValue / FadeInFrames);
            WaterHerbs();
            if (!Main.dedServ && Projectile.owner != Main.myPlayer)
                PredictHitSplash();
        }

        //雨滴本帧扫过的格子里有没长成的草药就催熟;物块只在服务器或单人端改,客户端只出表现
        private void WaterHerbs() {
            int left = (int)(Projectile.position.X / TileSize);
            int right = (int)((Projectile.position.X + Projectile.width) / TileSize);
            int top = (int)(Projectile.position.Y / TileSize);
            int bottom = (int)((Projectile.position.Y + Projectile.height + Projectile.velocity.Y) / TileSize);
            for (int x = left; x <= right; x++) {
                for (int y = top; y <= bottom; y++) {
                    if (!WorldGen.InWorld(x, y))
                        continue;
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile)
                        continue;
                    ushort grown = GrownHerbType(tile.TileType, tile.TileFrameX);
                    if (grown == tile.TileType)
                        continue;
                    if (Main.netMode != NetmodeID.MultiplayerClient) {
                        tile.TileType = grown;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendTileSquare(-1, x, y);
                        WorldGen.SquareTileFrame(x, y);
                    }
                    if (!Main.dedServ)
                        HerbEffect(x, y);
                }
            }
        }

        /// <summary>这格被浇后该有的物块类型;不是草药或已长成则原样返回</summary>
        private static ushort GrownHerbType(ushort type, short frameX) {
            if (type != TileID.ImmatureHerbs && type != TileID.MatureHerbs)
                return type;
            if (BloomStoredHerbs && (frameX == BlinkrootFrameX || frameX == ShiverthornFrameX))
                return TileID.BloomingHerbs;
            return TileID.MatureHerbs;
        }

        private static void HerbEffect(int x, int y) {
            Vector2 center = new Vector2(x + 0.5f, y + 0.5f) * TileSize;
            for (int i = 0; i < HerbDustCount; i++) {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-HerbDustSpread, HerbDustSpread), -Main.rand.NextFloat(HerbDustRiseMin, HerbDustRiseMax));
                Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(HerbDustRadius, HerbDustRadius), DustID.GrassBlades, velocity);
            }
            SoundEngine.PlaySound(HerbSound, center);
        }

        //命中钩子只在拥有者端跑,别的客户端按自己这份雨滴与敌怪的重叠补水花
        private void PredictHitSplash() {
            Rectangle box = Projectile.Hitbox;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.whoAmI == splashedNPC || npc.friendly || npc.dontTakeDamage || !box.Intersects(npc.Hitbox))
                    continue;
                splashedNPC = npc.whoAmI;
                Splash(SplashDustHit, HitSoundChance);
                return;
            }
        }

        //水尘从雨滴头部往上往两侧弹开,带一点雨滴的横向速度
        private void Splash(int count, int soundChance) {
            if (Main.dedServ)
                return;
            Vector2 at = Projectile.Bottom;
            for (int i = 0; i < count; i++) {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-SplashSpread, SplashSpread) + Projectile.velocity.X * SplashInherit, -Main.rand.NextFloat(SplashRiseMin, SplashRiseMax));
                Dust.NewDustPerfect(at, DustID.Rain, velocity, SplashAlpha, default, Main.rand.NextFloat(SplashScaleMin, SplashScaleMax));
            }
            if (Main.rand.NextBool(soundChance))
                SoundEngine.PlaySound(SplashSound, at);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Splash(SplashDustHit, HitSoundChance);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            hitTile = true;
            return true;
        }

        public override void OnKill(int timeLeft) {
            if (hitTile)
                Splash(SplashDustTile, TileSoundChance);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height - HitHeight * 0.5f);
            Color color = Color.Lerp(lightColor, Color.White, MinLight) * Projectile.Opacity;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
