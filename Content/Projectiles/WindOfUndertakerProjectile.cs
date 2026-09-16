using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Projectiles.Cruiser;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class WindOfUndertakerProjectile : ModProjectile
    {
        public override void SetStaticDefaults() {
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.timeLeft = 10000;
            Projectile.WhipSettings.Segments = 14;
            Projectile.WhipSettings.RangeMultiplier = 2.4f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        bool st = true;

        public override void AI() {
            Player player = Projectile.owner.ToPlayer();
            if (Projectile.ai[0] == 0) {
                Projectile.timeLeft = (Projectile.owner.ToPlayer().itemAnimation + 4) * (Projectile.MaxUpdates);
            }
            if (st && (float)(player.itemAnimationMax - player.itemAnimation) / (float)player.itemAnimationMax >= 0.66f) {
                st = false;
                SoundStyle sound = new SoundStyle("CalamityEntropy/Assets/Sounds/clap");
                sound.Pitch = 1.4f;
                SoundEngine.PlaySound(sound);
                SoundEngine.PlaySound(SoundID.Item9);
                int num = 3;
                int counts = 2;
                float speed = 16;
                if (Projectile.owner == Main.myPlayer) {

                    float angle = 0;
                    for (int i = 0; i < counts; i++) {

                        for (int j = 0; j < num; j++) {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 270 * Projectile.WhipSettings.RangeMultiplier * Projectile.GetOwner().whipRangeMultiplier, angle.ToRotationVector2() * speed, ModContent.ProjectileType<VoidStarF>(), (int)(Projectile.damage / 6f), 1).ToProj().DamageType = Projectile.DamageType;
                            angle += ((float)Math.PI * 2 / (float)num);
                        }
                        angle += ((float)Math.PI * 2 / (float)num) / (float)counts;
                        speed *= 0.7f;
                    }
                }
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 270 * Projectile.WhipSettings.RangeMultiplier * Projectile.GetOwner().whipRangeMultiplier, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), 0, 0, Projectile.owner, 0, -0.4f);

            }
            player.heldProj = Projectile.whoAmI;
            Projectile.ai[0]++;
            Projectile.Center = Projectile.owner.ToPlayer().MountedCenter + Vector2.UnitY * Projectile.owner.ToPlayer().gfxOffY + Projectile.velocity.SafeNormalize(Vector2.Zero) * 10;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.RemoveAllTags(target);
            target.AddBuff(ModContent.BuffType<CruiserWhipDebuff>(), 240);
            target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 150);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
            Projectile.damage = (int)(Projectile.damage * 0.9f);

        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Player player = Projectile.owner.ToPlayer();
            List<Vector2> p1 = getPoints((float)(player.itemAnimationMax - player.itemAnimation) / (float)player.itemAnimationMax, false);
            List<Vector2> p2 = getPoints((float)(player.itemAnimationMax - player.itemAnimation) / (float)player.itemAnimationMax, true);
            for (int i = 1; i < p1.Count; i++) {
                if (CEUtils.LineThroughRect(p1[i - 1], p1[i], targetHitbox, 100)) {
                    return true;
                }
            }
            for (int i = 1; i < p2.Count; i++) {
                if (CEUtils.LineThroughRect(p2[i - 1], p2[i], targetHitbox, 100)) {
                    return true;
                }
            }
            return false;
        }
        private void DrawLine(List<Vector2> list) {
            Texture2D texture = CEExtraAssets.white;
            Rectangle frame = texture.Frame();
            Vector2 origin = new Vector2(0, 0.5f);

            Vector2 pos = list[0];
            for (int i = 0; i < list.Count - 1; i++) {
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;

                float rotation = diff.ToRotation();
                Color color = Color.LightBlue;
                Vector2 scale = new Vector2(diff.Length() + 2, 2);
                if (i == list.Count - 2) {
                    scale.X -= 8;
                }

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

                pos += diff;
            }
        }
        private void drawSegs(List<Vector2> list, bool flip) {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 1; i < list.Count; i++) {
                float rot = (list[i] - list[i - 1]).ToRotation();
                Rectangle frame = new Rectangle(38, 0, 14, 30);
                if (i % 2 == 0) {
                    frame = new Rectangle(52, 0, 14, 30);
                }
                if (i == list.Count - 1) {
                    frame = new Rectangle(66, 0, 18, 30);
                }
                Vector2 origin = new Vector2(0, 15);
                Main.EntitySpriteDraw(tex, list[i] - Main.screenPosition, frame, Color.White, rot, origin, Projectile.scale * 1.6f, flip ? SpriteEffects.FlipVertically : SpriteEffects.None);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            var player = Projectile.owner.ToPlayer();
            List<Vector2> p1 = getPoints((float)(player.itemAnimationMax - player.itemAnimation) / (float)player.itemAnimationMax, false);
            List<Vector2> p2 = getPoints((float)(player.itemAnimationMax - player.itemAnimation) / (float)player.itemAnimationMax, true);
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            DrawLine(p1);
            DrawLine(p2);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 38, 30), Color.White, Projectile.velocity.ToRotation(), new Vector2(20, 15), Projectile.scale * 1.6f, SpriteEffects.None);

            drawSegs(p1, true);
            drawSegs(p2, false);
            return false;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public List<Vector2> getPoints(float p, bool flip) {
            List<Vector2> points = new List<Vector2>();
            if (p <= 0.66f) {
                float pc = p / 0.66f;
                pc *= pc;
                Vector2 start = Projectile.Center;
                Vector2 mid = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * pc * 30 * Projectile.WhipSettings.RangeMultiplier * Projectile.GetOwner().whipRangeMultiplier;
                Vector2 end = mid + (Projectile.velocity.SafeNormalize(Vector2.UnitX) * pc * 240 * Projectile.WhipSettings.RangeMultiplier * Projectile.GetOwner().whipRangeMultiplier).RotatedBy((1 - pc) * MathHelper.PiOver2 * (flip ? -1 : 1));
                mid = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * pc * 180 * Projectile.WhipSettings.RangeMultiplier * Projectile.GetOwner().whipRangeMultiplier;

                for (int i = 0; i <= Projectile.WhipSettings.Segments; i++) {
                    points.Add(CEUtils.Bezier(new List<Vector2> { start, mid, end }, ((float)i / (float)Projectile.WhipSettings.Segments)));
                }

            }
            else {
                float c = 1 - (p - 0.66f) / 0.34f;
                Vector2 start = Projectile.Center;
                Vector2 mid = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 30 * c * Projectile.WhipSettings.RangeMultiplier * Projectile.GetOwner().whipRangeMultiplier;
                Vector2 end = mid + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 240 * c * Projectile.WhipSettings.RangeMultiplier * Projectile.GetOwner().whipRangeMultiplier;

                for (int i = 0; i <= Projectile.WhipSettings.Segments; i++) {
                    points.Add(CEUtils.Bezier(new List<Vector2> { start, mid, end }, ((float)i / (float)Projectile.WhipSettings.Segments)));
                }

            }
            return points;
        }
    }
}
