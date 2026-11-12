using CalamityEntropy.Content.Items.Weapons.VoidDestroyer;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PrayerlightLance
{
    /// <summary>
    /// 神圣激光。ai[0] 目标 NPC 下标,-1 为空打只有表现;ai[1] 目标类型,防下标被新生成的怪复用。
    /// 光柱从屏幕顶端以上竖直落到目标,落地起判定 3 帧且只打这个目标,随后留下逐渐消散的金色十字架
    /// </summary>
    public class PrayerlightLanceLaser : ModProjectile
    {
        public const int FallFrames = 6;
        public const int StrikeFrames = 3;
        public const int BeamFadeFrames = 14;
        //十字架停留约 0.5 秒
        public const int CrossFrames = 30;
        public const int CrossPopFrames = 6;
        //十字架消散时上浮,像素
        public const float CrossRise = 12f;
        //像素
        public const float BeamWidth = 24f;
        //光柱起点在本地屏幕顶端以上,像素
        public const float SkyMargin = 160f;

        private int age;
        private bool struck;

        private int Since => age - FallFrames;

        private NPC Target {
            get {
                int index = (int)Projectile.ai[0];
                if (index < 0 || index >= Main.maxNPCs) {
                    return null;
                }
                NPC npc = Main.npc[index];
                return npc.active && npc.type == (int)Projectile.ai[1] ? npc : null;
            }
        }

        public override void SetDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = FallFrames + CrossFrames + 2;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            age++;
            NPC target = Target;
            if (target != null && age <= FallFrames + StrikeFrames) {
                Projectile.Center = target.Center;
            }
            if (!struck && age >= FallFrames) {
                struck = true;
                OnStrike();
            }
            float light = struck ? CrossOpacity : 0.5f;
            Lighting.AddLight(Projectile.Center, SunVfx.Gold.ToVector3() * light);
        }

        private void OnStrike() {
            if (Main.dedServ) {
                return;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.3f, 1.6f), Projectile.Center, 4, 0.45f);
            PrayerlightLanceFx.Strike(Projectile.Center, 1f);
        }

        public override bool? CanDamage() => struck && age < FallFrames + StrikeFrames ? null : false;

        public override bool? CanHitNPC(NPC target) {
            NPC locked = Target;
            return locked != null && target.whoAmI == locked.whoAmI ? null : false;
        }

        private float CrossOpacity => MathHelper.Clamp(1f - Since / (float)CrossFrames, 0f, 1f);

        public override bool PreDraw(ref Color lightColor) {
            Vector2 at = Projectile.Center;
            Vector2 sky = new Vector2(at.X, Math.Min(Main.screenPosition.Y, at.Y) - SkyMargin);
            VDWeaponFx.BeginAdditive();
            if (!struck) {
                float k = age / (float)FallFrames;
                Vector2 head = Vector2.Lerp(sky, at, k * k);
                PrayerlightLanceFx.Beam(sky, head, BeamWidth * 0.7f, 0.9f);
                VDWeaponFx.Glow(head, SunVfx.Core * 0.7f, 28f);
            }
            else {
                float beam = MathHelper.Clamp(1f - Since / (float)BeamFadeFrames, 0f, 1f);
                PrayerlightLanceFx.Beam(sky, at, BeamWidth * (0.6f + 0.6f * beam), beam);
                VDWeaponFx.Glow(at, SunVfx.Gold * (0.55f * CrossOpacity), 48f);
            }
            VDWeaponFx.End();

            if (struck) {
                Texture2D tex = TextureAssets.Projectile[Type].Value;
                float k = MathHelper.Clamp(Since / (float)CrossPopFrames, 0f, 1f);
                float scale = MathHelper.Lerp(1.4f, 1f, 1f - (1f - k) * (1f - k));
                Vector2 pos = at - new Vector2(0f, CrossRise * Since / CrossFrames);
                Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, Color.White * CrossOpacity, 0f, tex.Size() / 2f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
