using CalamityEntropy.Content.Projectiles.Pets.Abyss;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    /// <summary>
    /// 虚空唤灵盔职业套装奖励:迷你虚空吞噬者(2026-08-31 平衡案)。
    /// 形象取虚空珍珠伙伴的头/身/尾三段贴图,AI 参考沧溟龙契的冲撞打法(简化为单头三段)。
    /// 由 EModPlayer 在 VFHelmSummoner 生效时自动召唤,不占仆从栏。
    /// </summary>
    public class MiniVoidDevourer : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Projectiles/Pets/Abyss/Head";

        public override void SetDefaults() {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 24;
            Projectile.netImportant = true;
        }

        public Vector2 bodyP = Vector2.Zero;
        public Vector2 tailP = Vector2.Zero;
        private int chargeCd = 0;

        public override void AI() {
            Player owner = Projectile.GetOwner();
            if (owner == null || !owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            if (owner.Entropy().VFHelmSummoner) {
                Projectile.timeLeft = 2;
            }
            if (chargeCd > 0) {
                chargeCd--;
            }
            NPC target = owner.HasMinionAttackTargetNPC && Main.npc[owner.MinionAttackTargetNPC].active
                ? Main.npc[owner.MinionAttackTargetNPC]
                : Projectile.FindTargetWithinRange(1100, false);
            if (target != null && target.active) {
                Vector2 toTarget = target.Center - Projectile.Center;
                if (chargeCd <= 0 && toTarget.Length() < 900) {
                    // 俯冲冲撞,冲过后短暂盘旋再入(沧溟龙契式节奏)
                    Projectile.velocity = toTarget.SafeNormalize(Vector2.UnitX) * 21f;
                    chargeCd = 40;
                }
                else {
                    Projectile.velocity *= 0.97f;
                    Projectile.velocity += toTarget.SafeNormalize(Vector2.Zero) * 0.5f;
                }
            }
            else {
                Vector2 idle = owner.Center + new Vector2(-owner.direction * 60, -60);
                Projectile.velocity = (Projectile.velocity + (idle - Projectile.Center) * 0.02f) * 0.94f;
                if (CEUtils.getDistance(Projectile.Center, owner.Center) > 1400) {
                    Projectile.Center = idle;
                    bodyP = tailP = idle;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (bodyP == Vector2.Zero) {
                bodyP = tailP = Projectile.Center;
            }
            ChainFollow(ref bodyP, Projectile.Center, 20);
            ChainFollow(ref tailP, bodyP, 18);

            if (Main.rand.NextBool(5)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.1f);
                d.noGravity = true;
                d.scale = 1.1f;
            }
        }

        private static void ChainFollow(ref Vector2 seg, Vector2 ahead, float dist) {
            Vector2 diff = seg - ahead;
            if (diff.Length() > dist) {
                seg = ahead + diff.SafeNormalize(Vector2.UnitY) * dist;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            // 头身尾三段整条判定
            return CEUtils.LineThroughRect(Projectile.Center, tailP, targetHitbox, 24);
        }

        public override bool PreDraw(ref Color lightColor) {
            float scale = Projectile.scale * 0.8f;
            Texture2D head = AbyssPetTextures.Head.Value;
            Texture2D body = AbyssPetTextures.Body.Value;
            Texture2D tail = AbyssPetTextures.Tail.Value;
            float tailRot = (bodyP - tailP).ToRotation() + MathHelper.PiOver2;
            float bodyRot = (Projectile.Center - bodyP).ToRotation() + MathHelper.PiOver2;
            Main.EntitySpriteDraw(tail, tailP - Main.screenPosition, null, lightColor, tailRot, tail.Size() / 2f, scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(body, bodyP - Main.screenPosition, null, lightColor, bodyRot, body.Size() / 2f, scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(head, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, head.Size() / 2f, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
