using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 驱逐舰的传送门演出弹幕(无伤害):ai[0] 模式 0 冲刺门(朝向 = 生成时 rotation,寿命 ai[1]),1 支援投送地面门;
    /// ai[2] 门环亮度倍率(0 = 1 倍;幻影舰队里真身的门更亮,是可读的破绽)。开合曲线由寿命推导,全端一致。
    /// 经 <see cref="VDDepthSource"/> 生成时带深度:门画在按 Z 投影的位置、按 Z 缩放,并按 Z 分层(远门进远景层、近门压在玩家之上),
    /// 立体舰队 / 三维幻影冲刺的远门与近门就是它;Center 仍是门在平面坐标系里的位置
    /// </summary>
    public class VDPortal : ModProjectile, IVoidDestroyerProjectile, IVDDepthDrawable
    {
        public const int ModeDash = 0;
        public const int ModeReinforce = 1;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public int Mode => (int)Projectile.ai[0];
        public int TotalLife => (int)Math.Max(Projectile.ai[1], 20f);
        public float GlowMult => Projectile.ai[2] > 0f ? Projectile.ai[2] : 1f;
        /// <summary>门所在深度(0 平面);随生成包过线</summary>
        public float Depth;
        float IVDDepthDrawable.DepthZ => Depth;
        public Vector2 ProjectedCenter => VDDepth.Project(Projectile.Center, Depth);

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 60;
        }

        public override void OnSpawn(IEntitySource source) {
            if (source is VDDepthSource depth) {
                Depth = depth.Z;
            }
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Depth);

        public override void ReceiveExtraAI(BinaryReader reader) => Depth = reader.ReadSingle();

        //velocity 只当朝向用(随生成包过线),门本身不动
        public override bool ShouldUpdatePosition() => false;

        /// <summary>开合程度:前 25% 展开,最后 25% 收拢</summary>
        public float Openness() {
            float total = TotalLife;
            float age = total - Projectile.timeLeft;
            float open = MathHelper.Clamp(age / (total * 0.25f), 0f, 1f);
            float close = MathHelper.Clamp(Projectile.timeLeft / (total * 0.25f), 0f, 1f);
            return Math.Min(open, close);
        }

        public override void AI() {
            //首帧同步寿命(客户端拿到的 timeLeft 是 SetDefaults 的 60,按 ai[1] 重定)
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = TotalLife;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("portal_emerge", (Mode == ModeDash ? 1.2f : 0.9f) * (Depth != 0f ? VDDepth.DopplerPitch(Depth) : 1f), ProjectedCenter, 4, 0.8f);
                }
            }
            //支援门开在地面上:短轴竖直,门面是横椭圆;冲刺门短轴指向通过方向
            if (Mode == ModeReinforce) {
                Projectile.rotation = MathHelper.PiOver2;
            }
            else if (Projectile.velocity != Vector2.Zero) {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            float scale = VDDepth.Scale(Depth);
            if (Math.Abs(Depth) < 0.5f) {
                Lighting.AddLight(Projectile.Center, VoidDestroyerNPC.VoidPurple.ToVector3() * 0.8f * Openness());
            }
            if (!Main.dedServ && Main.rand.NextBool(2)) {
                //门缘粒子放在投影位置(粒子系统不分层),尺寸随深度缩
                float ang = Projectile.rotation + MathHelper.PiOver2;
                float extent = (Mode == ModeDash ? 90f : 70f) * scale;
                Vector2 pos = ProjectedCenter + ang.ToRotationVector2() * Main.rand.NextFloat(-extent, extent) * Openness();
                Vector2 v = CEUtils.randomPointInCircle(2f) * scale;
                var p = PRTLoader.NewParticle<PRT_Void>(pos, v, Color.White, Main.rand.NextFloat(0.8f, 1.3f) * Math.Max(scale, 0.4f));
                p.Opacity = 0.6f * VDDepth.Alpha(Depth);
                p.ad = 0.03f;
            }
        }

        public override bool? CanDamage() => false;

        #region 分层与绘制
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            VDDepthLayer layer = Depth == 0f ? VDDepthLayer.Plane : VDDepth.LayerOf(Depth);
            if (layer == VDDepthLayer.Far && VDDepth.FarLayerUsable(ProjectedCenter)) {
                Projectile.hide = true;
                VDDepthStage.RegisterFar(this);
            }
            else if (layer == VDDepthLayer.Near) {
                Projectile.hide = true;
                overPlayers.Add(index);
            }
            else {
                Projectile.hide = false;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            DrawPortal();
            return false;
        }

        void IVDDepthDrawable.DrawDepthFar(SpriteBatch spriteBatch) => DrawPortal();

        void IVDDepthDrawable.DrawDepthMarker(SpriteBatch spriteBatch) {
        }

        /// <summary>门按深度投影缩放;近门的白环按剪影透明度压暗,别把屏幕边缘整块糊白</summary>
        private void DrawPortal() {
            float size = (Mode == ModeDash ? 120f : 90f) * VDDepth.Scale(Depth);
            float glow = GlowMult * (Depth < 0f ? VDDepth.Alpha(Depth) / VDDirector.DepthNearAlphaMax : 1f);
            VoidDestroyerNPC.DrawPortalAt(ProjectedCenter, Openness(), Projectile.rotation, size, Main.screenPosition, glow);
        }
        #endregion
    }
}
