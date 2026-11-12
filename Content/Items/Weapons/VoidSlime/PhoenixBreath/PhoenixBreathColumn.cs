using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.PhoenixBreath
{
    /// <summary>
    /// 凤凰吐息的火柱。持握弹幕蓄满时在拥有者端点燃,每帧贴着喷口沿瞄准方向重新铺开,持握弹幕消失即熄灭
    /// 长度按射线逐点探测,碰到第一块实心物块或液面就截断,喷口在水里时喷不出;岩浆不熄灭火焰,同原版火焰
    /// 无限穿透,同一敌人隔一段时间烧一次,间隔乘攻速;贴图借原版火焰 Projectile_85,外层红焰半透明叠色,内芯金白 A=0 叠加
    /// ai[0] 持握弹幕的 identity;localAI[0] 已点燃帧数;localAI[1] 当前长度,像素,各端各自探测
    /// </summary>
    public class PhoenixBreathColumn : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flames;

        private const int Frames = 7;                   //贴图竖排帧数
        private const int Size = 2;                     //判定框边长,像素;实际判定见 Colliding 的线段
        private const int KeepAliveFrames = 3;          //每帧续上的 timeLeft,消失只由拥有者裁决
        private const int HitFrames = 6;                //同一敌人的命中间隔,帧,再乘攻速
        private const float HitWidth = 48f;             //伤害判定的粗细,像素,3 格
        private const float ScanStep = 4f;              //射线探测步长,像素
        private const float TileSize = 16f;             //物块格边长,像素
        private const float FullLiquid = 255f;          //一格装满时的液量
        private const float ExtendFrames = 6f;          //点燃后伸到最长所需帧数
        private const int DrawFluff = 640;              //绘制裁剪放宽,像素,大于最长长度
        //绘制,沿火柱等距铺一串旋转的火团,从喷口往外流动
        private const float PuffSpacing = 14f;          //火团间距,像素
        private const float FlowSpeed = 280f;           //火团往外流动的速度,像素每秒
        private const float PuffScaleNear = 0.32f;      //喷口处火团缩放,单帧 98 像素
        private const float PuffScaleFar = 0.72f;       //最远处火团缩放
        private const float CoreScale = 0.5f;           //内芯相对外层的缩放
        private const float CoreUntil = 0.85f;          //内芯画到最长长度的这个比例
        private const float FirstFrame = 2f;            //喷口处用的帧,往外渐变到下一帧
        private const float LastFrame = 5f;
        private const float SpinRate = 2.5f;            //火团自转,弧度每秒,相邻火团反向
        private const float SpinStagger = 1.7f;         //相邻火团的初始转角差,弧度
        private const float FadeInLength = 24f;         //喷口前这段长度里火团淡入,像素
        private const float FadeOutLength = 56f;        //末端这段长度里火团淡出,像素
        private const float SplashSize = 96f;           //撞墙处火光直径,像素
        private const float SplashOpacity = 0.7f;
        //光与粉尘
        private const float LightStep = 32f;            //沿火柱每隔多远点一盏光,像素
        private const int FlameDust = 3;                //每帧沿火柱出的火焰粉尘数
        private const float FlameDustSpread = 16f;      //火焰粉尘离中轴的散布,像素
        private const float FlameDustSpeedMin = 2f;     //火焰粉尘顺着火柱的速度,像素每帧
        private const float FlameDustSpeedMax = 6f;
        private const float FlameDustScaleMin = 1.3f;
        private const float FlameDustScaleMax = 2f;
        private const float SplashDustChance = 0.5f;    //撞墙时每帧出一颗火星的概率
        private const float SplashDustSpeed = 5f;       //火星初速上限,像素每帧
        private const float SteamChance = 0.3f;         //喷口在水里时每帧冒一团烟的概率
        private const float SteamRise = 1.5f;           //烟上飘的速度,像素每帧
        private const int SteamAlpha = 120;
        private static readonly Color OuterNear = new Color(244, 42, 10, 200);  //外层红焰,喷口处偏橙,取自凤凰色阶
        private static readonly Color OuterFar = new Color(200, 18, 34, 170);   //外层红焰,远处深红
        private static readonly Color CoreNear = new Color(250, 237, 184);      //内芯,喷口处金白
        private static readonly Color CoreFar = new Color(255, 215, 70);        //内芯,远处金黄
        private static readonly Color SplashColor = new Color(252, 145, 28);
        private static readonly Color SteamColor = new Color(200, 200, 210);
        private static readonly Vector3 LightRGB = new Vector3(1f, 0.45f, 0.15f);   //照明,RGB 0-1

        private ref float Age => ref Projectile.localAI[0];
        private ref float Length => ref Projectile.localAI[1];

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = DrawFluff;
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = KeepAliveFrames;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitFrames;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI() {
            Projectile.timeLeft = KeepAliveFrames;
            int index = Projectile.GetByUUID(Projectile.owner, Projectile.ai[0]);
            if (index < 0 || Main.projectile[index].ModProjectile is not PhoenixBreathHeld held) {
                //别的客户端可能先收到火柱后收到持握弹幕,只有拥有者把它当作熄灭
                if (Projectile.owner == Main.myPlayer) {
                    Projectile.Kill();
                }
                Length = 0f;
                return;
            }
            Player player = Main.player[Projectile.owner];
            Vector2 aim = Main.projectile[index].velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = held.Muzzle;
            Projectile.Center = start;
            Projectile.velocity = aim;
            Projectile.rotation = aim.ToRotation();
            Age++;
            float reach = PhoenixBreath.ColumnReach(player) * Math.Min(Age / ExtendFrames, 1f);
            Length = CastLength(start, aim, reach, PhoenixBreath.ColumnPassesWalls(player));
            if (Projectile.owner == Main.myPlayer) {
                Projectile.localNPCHitCooldown = CombinedHooks.TotalUseTime(HitFrames, player, player.HeldItem);
            }
            float length = Length;
            for (float d = 0f; d < length; d += LightStep) {
                Lighting.AddLight(start + aim * d, LightRGB);
            }
            if (!Main.dedServ) {
                SpawnDust(start, aim, length, length < reach);
            }
        }

        /// <summary>从起点沿方向逐点探,碰到实心物块或非岩浆的液体就停,返回能喷到的长度</summary>
        public static float CastLength(Vector2 start, Vector2 direction, float reach, bool passWalls) {
            for (float d = 0f; d < reach; d += ScanStep) {
                Vector2 point = start + direction * d;
                if (!passWalls && !CEUtils.isAir(point) || InLiquid(point)) {
                    return d;
                }
            }
            return reach;
        }

        //液体从物块格底部往上填,液量 255 为满格
        private static bool InLiquid(Vector2 point) {
            Point tilePoint = point.ToTileCoordinates();
            if (!WorldGen.InWorld(tilePoint.X, tilePoint.Y)) {
                return false;
            }
            Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
            if (tile.LiquidAmount == 0 || tile.LiquidType == LiquidID.Lava) {
                return false;
            }
            float surface = (tilePoint.Y + 1) * TileSize - tile.LiquidAmount / FullLiquid * TileSize;
            return point.Y >= surface;
        }

        private void SpawnDust(Vector2 start, Vector2 aim, float length, bool blocked) {
            if (length <= 0f) {
                if (Main.rand.NextFloat() < SteamChance) {
                    Dust steam = Dust.NewDustPerfect(start, DustID.Smoke, -Vector2.UnitY * SteamRise, SteamAlpha, SteamColor);
                    steam.noGravity = true;
                }
                return;
            }
            Vector2 normal = aim.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < FlameDust; i++) {
                Vector2 position = start + aim * Main.rand.NextFloat(length) + normal * Main.rand.NextFloat(-FlameDustSpread, FlameDustSpread);
                Dust dust = Dust.NewDustPerfect(position, Main.rand.NextBool() ? DustID.Torch : DustID.SolarFlare, aim * Main.rand.NextFloat(FlameDustSpeedMin, FlameDustSpeedMax));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(FlameDustScaleMin, FlameDustScaleMax);
            }
            if (blocked && Main.rand.NextFloat() < SplashDustChance) {
                Dust spark = Dust.NewDustPerfect(start + aim * length, DustID.GoldFlame, Main.rand.NextVector2Circular(SplashDustSpeed, SplashDustSpeed));
                spark.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float length = Length;
            if (length <= 0f) {
                return false;
            }
            Vector2 start = Projectile.Center;
            return CEUtils.LineThroughRect(start, start + Projectile.velocity * length, targetHitbox, (int)(HitWidth * Projectile.scale));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            PhoenixBreath.ApplyHitDebuffs(Main.player[Projectile.owner], target);
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            float length = Length;
            if (length <= 0f) {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 aim = Projectile.velocity;
            Vector2 start = Projectile.Center - Main.screenPosition;
            float reach = PhoenixBreath.ColumnReach(Main.player[Projectile.owner]);
            float time = Main.GlobalTimeWrappedHourly;
            float flow = time * FlowSpeed % PuffSpacing;
            float scale = Projectile.scale;
            //从远到近画,喷口处的火团压在上面;比例按最长长度算,被墙截短时火团不跟着缩
            for (int k = (int)(length / PuffSpacing); k >= 0; k--) {
                float distance = k * PuffSpacing + flow;
                if (distance > length) {
                    continue;
                }
                float progress = distance / reach;
                float opacity = Utils.GetLerpValue(0f, FadeInLength, distance, true) * Utils.GetLerpValue(length, length - FadeOutLength, distance, true);
                if (opacity <= 0f) {
                    continue;
                }
                Rectangle frame = texture.Frame(1, Frames, 0, Math.Min((int)MathHelper.Lerp(FirstFrame, LastFrame + 1f, progress), (int)LastFrame));
                Vector2 origin = frame.Size() * 0.5f;
                Vector2 position = start + aim * distance;
                float spin = (k % 2 == 0 ? 1f : -1f) * time * SpinRate + k * SpinStagger;
                float puffScale = MathHelper.Lerp(PuffScaleNear, PuffScaleFar, progress) * scale;
                Main.EntitySpriteDraw(texture, position, frame, Color.Lerp(OuterNear, OuterFar, progress) * opacity, spin, origin, puffScale, SpriteEffects.None);
                if (progress < CoreUntil) {
                    Color core = Color.Lerp(CoreNear, CoreFar, progress / CoreUntil) with { A = 0 };
                    Main.EntitySpriteDraw(texture, position, frame, core * opacity, -spin, origin, puffScale * CoreScale, SpriteEffects.None);
                }
            }
            if (length < reach - ScanStep) {
                Texture2D glow = CEExtraAssets.Glow2;
                Main.EntitySpriteDraw(glow, start + aim * length, null, SplashColor with { A = 0 } * SplashOpacity, 0f, glow.Size() * 0.5f, SplashSize * scale / glow.Width, SpriteEffects.None);
            }
            return false;
        }
    }
}
