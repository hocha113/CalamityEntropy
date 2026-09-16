using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkMarivium : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.value = Item.buyPrice(platinum: 2, gold: 80);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Marivium");
        public override EBookProjectileEffect getEffect() {
            return new BookmarkMariviumEffect();
        }

        public override Color tooltipColor => Color.LightSkyBlue;
        private static int projType = -1;
        public static int ProjType { get { if (projType == -1) { projType = ModContent.ProjectileType<MiniWyrm>(); } return projType; } }
    }

    public class BookmarkMariviumEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            target.AddBuff<LifeOppress>(5 * 60);
        }
    }
    public class MiniWyrm : BaseBookMinion, iWyrmSeg
    {
        public float rot { get { return Projectile.rotation; } set { Projectile.rotation = value; } }
        public Vector2 Center { get { return Projectile.Center; } set { Projectile.Center = value; } }
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.penetrate = -1;
            Projectile.localNPCHitCooldown = 25;
            Projectile.width = Projectile.height = 64;
            Projectile.tileCollide = false;
        }
        public List<WyrmSeg> segs;
        public override bool PreDraw(ref Color lightColor) {
            Color color = lightColor * (BlockTimes >= 1 ? 1 : 0.5f);
            Texture2D tex = Projectile.GetTexture();
            Rectangle head = new Rectangle(76, 0, 30, tex.Height);
            Rectangle seg1 = new Rectangle(42, 0, 32, tex.Height);
            Rectangle seg2 = new Rectangle(0, 0, 40, tex.Height);
            Vector2 rectOrigin(Rectangle rect) {
                return new Vector2(rect.Width / 2f, rect.Height / 2f);
            }
            Main.EntitySpriteDraw(tex, Center - Main.screenPosition, head, color, rot, rectOrigin(head), Projectile.scale, SpriteEffects.None);
            if (segs != null) {
                for (int i = 0; i < segs.Count; i++) {
                    iWyrmSeg s = segs[i];
                    Rectangle rect = i == segs.Count - 1 ? seg2 : seg1;
                    Main.EntitySpriteDraw(tex, s.Center - Main.screenPosition, rect, color, s.rot, rectOrigin(rect), Projectile.scale, SpriteEffects.None);
                }
            }
            return false;
        }

        public override float DamageMult => 1f;
        public float BlockTimes { get { return Projectile.ai[1]; } set { Projectile.ai[1] = value; } }
        public override bool PreAI() {
            if (segs == null) {
                segs = new List<WyrmSeg>();
                segs.Add(new WyrmSeg() { Center = Projectile.Center, follow = this, spacing = 18 });
                segs.Add(new WyrmSeg() { Center = Projectile.Center, follow = segs[0], spacing = 24 });
                segs.Add(new WyrmSeg() { Center = Projectile.Center, follow = segs[1], spacing = 24 });
                segs.Add(new WyrmSeg() { Center = Projectile.Center, follow = segs[2], spacing = 24 });
                segs.Add(new WyrmSeg() { Center = Projectile.Center, follow = segs[3], spacing = 32 });
            }
            if (BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkMariviumEffect>(Projectile.GetOwner()))
                Projectile.timeLeft = 5;
            return true;
        }
        public override void AI() {
            base.AI();
            var player = Projectile.GetOwner();
            if (BlockTimes < 5)
                BlockTimes += 1 / 70f;

            bool roundPlr = false;
            if (BlockTimes >= 1) {
                Projectile tar = null;
                bool b = false;
                float distance = 999999;
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (p.hostile && !p.friendly && p.damage > 0 && Math.Max(p.width, p.height) < 150 && CEUtils.getDistance(player.Center, p.Center) < 800) {
                        if (p.Colliding(p.getRect(), p.Center.getRectCentered(16, 16)) && !p.Colliding(p.getRect(), (p.Center + p.rotation.ToRotationVector2() * 320).getRectCentered(36, 36)) && !p.Colliding(p.getRect(), (p.Center + p.velocity.normalize() * 320).getRectCentered(36, 36))) {
                            float dst = CEUtils.getDistance(p.Center, Projectile.Center);
                            if (dst < distance) {
                                tar = p;
                                distance = dst;
                            }
                            if (p.Colliding(p.getRect(), Projectile.getRect())) {
                                p.Kill();
                                CEUtils.PlaySound("LightHit", 1, Projectile.Center);
                                if (!b) {
                                    b = true;
                                    BlockTimes--;
                                    if (Main.myPlayer == Projectile.owner)
                                        CEUtils.SyncProj(Projectile.whoAmI);
                                }
                            }
                        }
                    }
                }
                if (tar != null) {
                    Projectile.velocity *= 0.7f;
                    Projectile.velocity += (tar.Center - Projectile.Center).normalize() * 10;
                }
                else {
                    roundPlr = true;
                }
            }
            else { roundPlr = true; }

            if (roundPlr) {
                if (CEUtils.getDistance(Projectile.Center, player.Center) > 250) {
                    Projectile.velocity *= 0.96f;
                    Projectile.velocity += (player.Center - Projectile.Center).normalize() * 1.4f;
                }
                else {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.One) * float.Lerp(Projectile.velocity.Length(), 16, 0.05f);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center += Projectile.velocity;
            foreach (WyrmSeg seg in segs)
                seg.update();
            Projectile.Center -= Projectile.velocity;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff<LifeOppress>(5 * 60);
        }
    }
}