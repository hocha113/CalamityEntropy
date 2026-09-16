using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 幻影舰:驱逐舰的全息复制体,在门口待机 ai[1] 帧(与真身同拍瞄准)→ 服务端一帧定速朝目标齐冲 → 冲刺结束碎成全息碎片。
    /// ai[0] 本体,ai[2] 目标玩家。只有冲刺中有判定;P3 沿路留加速虚空弹。伤害是真身接触的 60%
    /// </summary>
    public class VDPhantomShip : VDHostileProjectile
    {
        [InnoVault.VaultLoaden("CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyerP2")]
        private static Asset<Texture2D> p2Tex = null;

        public override string Texture => "CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyer";
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => 200;

        public int AimFrames => (int)Math.Max(Projectile.ai[1], 6f);
        public int Age => (int)Projectile.localAI[1];
        public bool Dashing => Projectile.velocity.LengthSquared() > 400f;

        private VoidDestroyerNPC Owner {
            get {
                int idx = (int)Projectile.ai[0];
                if (idx < 0 || idx >= Main.maxNPCs || !Main.npc[idx].active || Main.npc[idx].ModNPC is not VoidDestroyerNPC boss) {
                    return null;
                }
                return boss;
            }
        }

        public override void SetExtraDefaults() {
            Projectile.width = 120;
            Projectile.height = 70;
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = AimFrames + VDDirector.FleetDashFrames + VDDirector.FleetEndFade;
                VDVfx.HoloBurst(Projectile.Center, VDVfx.VoidPurple);
            }
            Projectile.localAI[1]++;
            int age = Age;
            Player target = TargetPlayer(2);

            if (age == AimFrames && IsServer) {
                Vector2 aim = target != null ? target.Center : Projectile.Center + Vector2.UnitY;
                Projectile.velocity = (aim - Projectile.Center).SafeNormalize(Vector2.UnitY) * VDDirector.FleetDashSpeed;
                Projectile.netUpdate = true;
            }
            if (age == AimFrames && !Main.dedServ) {
                CEUtils.PlaySound("CruiserDash", 1.1f, Projectile.Center, 4, 0.8f);
            }
            if (age >= AimFrames + VDDirector.FleetDashFrames) {
                Projectile.velocity *= 0.8f;
            }

            //倾斜跟随横向速度(与真身同一套)
            Projectile.rotation = MathHelper.Lerp(Projectile.rotation, MathHelper.Clamp(Projectile.velocity.X * 0.012f, -0.25f, 0.25f), 0.2f);
            Lighting.AddLight(Projectile.Center, VDVfx.VoidPurple.ToVector3() * 0.7f);

            VoidDestroyerNPC boss = Owner;
            if (Dashing && boss != null && boss.Phase >= 3 && IsServer && age % VDDirector.FleetTrailInterval == 3) {
                int dmg = boss.ProjDamage(VDDirector.DmgVoidBolt);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY) * VDDirector.PhantomTrailSpeed, ModContent.ProjectileType<VDVoidBolt>(), dmg, 0f, Main.myPlayer, VDVoidBolt.ModeDashTrail);
            }
            if (!Main.dedServ && Dashing && Main.rand.NextBool(2)) {
                Vector2 v = -Projectile.velocity * 0.1f + CEUtils.randomPointInCircle(2f);
                VDVfx.SparkBurst(Projectile.Center + CEUtils.randomPointInCircle(40f), VDVfx.VoidPurple, 1, v.Length(), v.Length(), 18, 0.5f, 1f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Dashing) {
                return false;
            }
            return projHitbox.Intersects(targetHitbox);
        }

        public override void OnKill(int timeLeft) {
            VDVfx.HoloBurst(Projectile.Center, VDVfx.VoidPurple);
            VDVfx.HoloBurst(Projectile.Center, Color.White);
        }

        public override bool PreDraw(ref Color lightColor) {
            VoidDestroyerNPC boss = Owner;
            Texture2D tex = boss != null && boss.Phase >= 2 && p2Tex != null ? p2Tex.Value : TextureAssets.Projectile[Type].Value;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float fadeIn = MathHelper.Clamp(Age / 8f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)VDDirector.FleetEndFade, 0f, 1f);
            float opacity = 0.85f * Math.Min(fadeIn, fadeOut);
            //瞄准期越接近出手越亮(全息闪烁加剧 = 预告)
            if (Age < AimFrames) {
                float p = Age / (float)AimFrames;
                opacity *= 0.6f + 0.4f * p;
            }

            VDHologramDraw.Begin();
            if (Dashing) {
                for (int i = 1; i <= 4; i++) {
                    Vector2 ghost = pos - Projectile.velocity * i * 1.2f;
                    VDHologramDraw.DrawPart(tex, ghost, null, VDVfx.VoidPurple, opacity * (0.3f - i * 0.06f), Projectile.rotation, tex.Size() / 2f, 1f, SpriteEffects.None);
                }
            }
            VDHologramDraw.DrawPart(tex, pos, null, VDVfx.VoidPurple, opacity, Projectile.rotation, tex.Size() / 2f, 1f, SpriteEffects.None);
            VDHologramDraw.End();
            return false;
        }
    }
}
