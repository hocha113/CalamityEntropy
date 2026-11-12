using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.AncientPaladinWhip
{
    public class AncientPaladinWhipProj : BaseWhip
    {
        //鞭节数、长度倍率、每帧更新次数;鞭长约4×60×2/3×2.0≈320像素(20格)
        private const int SegmentCount = 24;
        private const float LengthMult = 2.0f;
        private const int UpdatesPerFrame = 6;
        //贴图分段高度(像素):柄、竖环与横环、锤头,与AncientPaladinWhipProj.png对应
        private const int HandlePx = 24;
        private const int SegPx = 12;
        private const int TipPx = 22;
        //金属反光,鞭身向白色插值的比例
        private const float Shine = 0.35f;
        //鞭响时锤头闪光:Glow2原图256像素,0.4倍约100像素
        private const float GlintScale = 0.4f;
        private const int GlintLife = 12;

        private static readonly Color Gold = new(233, 207, 137);
        private static readonly Color Glint = new(255, 240, 190);
        private static readonly Vector3 TipLight = new(0.6f, 0.5f, 0.25f);

        public override Color StringColor => Gold;
        public override int handleHeight => HandlePx;
        public override int segHeight => SegPx;
        public override int endHeight => TipPx;

        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = UpdatesPerFrame;
            segments = SegmentCount;
            rangeMult = LengthMult;
        }

        public override Color modifySegColor(int segCount, Color color) => Color.Lerp(color, Color.White, Shine);

        public override void WhipAI() {
            if (Main.dedServ || Projectile.WhipPointsForCollision.Count == 0)
                return;
            Vector2 tip = Projectile.WhipPointsForCollision[^1];
            if (Projectile.ai[0] == (int)(flyTime / 2f)) {
                PRTLoader.NewParticle<PRT_ShineParticle>(tip, Vector2.Zero, Glint, GlintScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, GlintLife);
            }
            if (Projectile.numUpdates == -1 && HitboxActive()) {
                Lighting.AddLight(tip, TipLight);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            target.GetGlobalNPC<AncientPaladinWhipNPC>().Mark(Projectile.owner, Projectile.damage);
        }
    }
}
