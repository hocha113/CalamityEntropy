using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 深空掠袭的护航幻影舰(纯演出,无伤害):跟着本体的深度与平面位置飞编队,表观偏移 (ai[1], ai[2]) 按深度换算成世界偏移。
    /// ai[0] 本体。本体不在掠袭状态时淡出消失;弹幕由状态从它的位置代射
    /// </summary>
    public class VDEscortShip : VDDepthProjectile
    {
        [InnoVault.VaultLoaden("CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyerP2")]
        private static Asset<Texture2D> p2Tex = null;

        public override string Texture => "CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyer";
        public override int DefaultTimeLeft => 400;
        public override bool WantsMarker => false;

        private float fade;

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
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = false;
        }

        public override bool? CanDamage() => false;

        /// <summary>护航舰所在的平面位置(本体平面位置 + 表观偏移按深度换算)</summary>
        public static Vector2 FormationPos(VoidDestroyerNPC boss, Vector2 apparentOffset) {
            return boss.NPC.Center + VDDepth.WorldOffset(apparentOffset, boss.DeclaredDepth);
        }

        protected override void DepthAI() {
            VoidDestroyerNPC boss = Owner;
            bool alive = boss != null && boss.CurrentStateIndex == VDStateIndex.DeepStrafe && !boss.Dying;
            fade = MathHelper.Clamp(fade + (alive ? 0.08f : -0.1f), 0f, 1f);
            if (!alive && fade <= 0f) {
                Projectile.Kill();
                return;
            }
            if (boss != null) {
                Z = boss.DeclaredDepth;
                ZVel = 0f;
                Vector2 apparent = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                Vector2 want = FormationPos(boss, apparent);
                //编队用弹性跟随,不是钉死:队形有一点点滞后才像三艘船
                Projectile.Center = Vector2.Lerp(Projectile.Center, want, 0.25f);
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, MathHelper.Clamp((want.X - Projectile.Center.X) * 0.01f, -0.25f, 0.25f), 0.2f);
            }
            if (!Main.dedServ && fade > 0.5f && Main.rand.NextBool(3)) {
                //引擎火花放在投影位置,尺寸随深度缩
                float sc = VDDepth.Scale(Z);
                Vector2 pos = ProjectedCenter + new Vector2(0f, 30f * sc);
                VDVfx.Spark(pos, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(1f, 2.5f)) * sc, VDVfx.VoidPink, 0.6f * sc, 0.8f, 14);
            }
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            VoidDestroyerNPC boss = Owner;
            Texture2D tex = boss != null && boss.Phase >= 2 && p2Tex != null ? p2Tex.Value : TextureAssets.Projectile[Type].Value;
            Vector2 pos = ProjectedCenter - Main.screenPosition;
            float alpha = 0.85f * fade * VDDepth.Alpha(Z);
            if (alpha <= 0.01f) {
                return;
            }
            float scale = DrawScale;
            spriteBatch.UseAdditive();
            Texture2D glow = CEUtils.getExtraTex("Glow");
            spriteBatch.Draw(glow, pos, null, VDDepth.Fog(VDVfx.VoidPurple, Z) * (0.35f * alpha), 0f, glow.Size() / 2f, 0.9f * scale, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            VDHologramDraw.Draw(tex, pos, null, VDDepth.Fog(VDVfx.VoidPurple, Z), alpha, Projectile.rotation, tex.Size() / 2f, scale, SpriteEffects.None);
        }
    }
}
