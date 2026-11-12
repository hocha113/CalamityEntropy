using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.CelestialSlimeStaff
{
    /// <summary>
    /// 小型凝胶黑洞:跟着被打中的敌人走,圆形判定。
    /// 外观复用虚空史莱姆的 VDSingularity 旋流(VoidSlime.DrawSwirl),换成金色吸积盘;ai[0] 跟随的敌人下标
    /// </summary>
    public class CelestialSlimeStaffGelHole : ModProjectile
    {
        #region 调参旋钮
        //半径(像素,3格)、存在时间(帧,0.9秒)、局部无敌帧、穿甲,均为策划
        public const float Radius = 48f;
        public const int Lifetime = 54;
        public const int HitCooldown = 18;
        public const int ArmorPen = 40;
        //张开与收拢的帧数、旋流转速、黑盘占比
        public const int OpenFrames = 8;
        public const int CloseFrames = 12;
        public const float Spin = 1.8f;
        public const float CoreFraction = 0.5f;
        //被吸进来的金色尘:每几帧一颗、出生距离(像素)
        public const int DustEvery = 2;
        public const float DustMinDistance = 30f;
        public const float DustMaxDistance = 72f;
        //底光:大小(半径的倍数)与强度;旋流半宽(半径的倍数,与小奇点一致);张开度低于这个就不画
        public const float GlowSize = 5f;
        public const float GlowOpacity = 0.7f;
        public const float SwirlSize = 2f;
        public const float MinVisibleOpen = 0.01f;
        //照明强度;音效音高与随机幅度、音量与同时发声上限(每秒约 3 个黑洞)
        public const float LightStrength = 0.6f;
        public const float SoundPitch = 1.6f;
        public const float SoundPitchBand = 0.1f;
        public const float SoundVolume = 0.3f;
        public const int SoundMaxInstances = 3;
        #endregion

        //吸积盘金、热边浅金
        public static readonly Color Disk = new(255, 186, 60);
        public static readonly Color Hot = new(255, 238, 176);

        public override string Texture => CEUtils.WhiteTexPath;

        private int Age => Lifetime - Projectile.timeLeft;
        private float Open => GodDraw.EaseOut(Age / (float)OpenFrames) * GodDraw.Span(Projectile.timeLeft, 0f, CloseFrames);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = (int)(Radius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.ArmorPenetration = ArmorPen;
        }

        public override bool? CanCutTiles() => false;

        public override void AI() {
            int index = (int)Projectile.ai[0];
            NPC host = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
            Projectile.velocity = host != null && host.active ? host.velocity : Vector2.Zero;
            Lighting.AddLight(Projectile.Center, Disk.ToVector3() * LightStrength * Open);
            if (Main.dedServ) {
                return;
            }
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                VSlimeVfx.Cue("BlackholeSpawn", Projectile.Center, SoundPitch + Main.rand.NextFloat(-SoundPitchBand, SoundPitchBand), SoundVolume, SoundMaxInstances);
            }
            if (Age % DustEvery == 0) {
                VSlimeVfx.Converge(Projectile.Center, DustMinDistance, DustMaxDistance, DustID.GoldFlame);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= Radius * Radius;
        }

        //和虚空史莱姆的小奇点(MiniSingularity)同一套画法:加法底光,再画旋流与真遮挡的黑盘
        public override bool PreDraw(ref Color lightColor) {
            float open = Open;
            if (open <= MinVisibleOpen) {
                return false;
            }
            GodDraw.BeginAdditive();
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(Disk, GlowOpacity * open), 0f, Radius * GlowSize / GodDraw.Glow.Width);
            GodDraw.End();
            VoidSlimeNPC.DrawSwirl(Projectile.Center, Radius * SwirlSize * open, CoreFraction, Disk, Hot, Spin, open);
            return false;
        }
    }
}
