using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Items.Books
{
    public abstract class EntropyBookDrawingAlt : EntropyBookHeldProjectile
    {
        public sealed override Texture2D[] OpenAnimations() {
            return new Texture2D[] { CEUtils.RequestTex(OpenAnimationPath) };
        }
        public sealed override Texture2D[] PageAnimations() {
            return new Texture2D[] { CEUtils.RequestTex(PageAnimationPath) };
        }
        public sealed override Texture2D[] UIOpenAnimations() {
            return new Texture2D[] { CEUtils.RequestTex(UIOpenAnimationPath) };
        }
        public virtual int OpenAnmCount => 3;
        public virtual int PageAnmCount => 5;
        public virtual int UIOpenAnmCount => 4;

        public override void playTurnPageAnimation() {
            playPageSound();
            pageTurnAnm = PageAnmCount - 1;
            Projectile.frameCounter = 0;
        }
        public override Texture2D getTexture() {
            if (UIOpen) {
                return UIOpenAnimations()[0];
            }
            else {
                if (openAnim < OpenAnmCount - 1) {
                    return OpenAnimations()[0];
                }
                else {
                    return PageAnimations()[0];
                }
            }
        }
        public virtual Rectangle GetFrame() {
            Rectangle frame = new Rectangle();
            Rectangle GetRect(Texture2D tex, int frame, int totalFrame) {
                int h = tex.Height / totalFrame;
                return new Rectangle(0, h * frame, tex.Width, h - 2);
            }
            Texture2D tex = null;
            switch (Style) {
                case 0: tex = OpenAnimations()[0]; break;
                case 1: tex = PageAnimations()[0]; break;
                case 2: tex = UIOpenAnimations()[0]; break;
                default: break;
            }
            switch (Style) {
                case 0: frame = GetRect(tex, OpenAnmCount - 1 - openAnim, OpenAnmCount); break;
                case 1: frame = GetRect(tex, pageTurnAnm, PageAnmCount); break;
                case 2: frame = GetRect(tex, UIOpenAnm, UIOpenAnmCount); break;
                default: break;
            }
            return frame;
        }
        public virtual Vector2 GetOrigin() {
            var f = GetFrame();
            return new Vector2(f.Width, f.Height) / 2f;
        }
        public int Style => UIOpen ? 2 : ((openAnim < OpenAnmCount - 1) ? 0 : 1); // 2 - UIOpen;  1 - Opened;  0 - Opening

        public override bool Opened => openAnim >= OpenAnmCount - 1;
        public override void UpdateAnimations() {
            Projectile.frameCounter++;
            if (!active && !UIOpen && openAnim == 0) {
                Projectile.frameCounter = 0;
            }
            if (Projectile.frameCounter >= frameChange) {
                Projectile.frameCounter = 0;
                if (pageTurnAnm > 0) {
                    pageTurnAnm--;
                }
                if (UIOpen) {
                    Projectile.rotation = 0;
                    if (UIOpenAnm < UIOpenAnmCount - 1) {
                        UIOpenAnm++;
                    }
                    if (openAnim > 0) {
                        openAnim--;
                    }
                }
                else {
                    if (UIOpenAnm > 0) {
                        UIOpenAnm--;
                    }
                    if (active) {
                        if (openAnim < OpenAnmCount - 1) {
                            openAnim++;
                        }
                    }
                    else {
                        if (openAnim > 0) {
                            openAnim--;
                        }
                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = getTexture();
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, GetFrame(), lightColor, Projectile.rotation, GetOrigin(), Projectile.scale, (Projectile.velocity.X > 0 || UIOpen ? SpriteEffects.None : SpriteEffects.FlipVertically), 0);
            return false;
        }
    }
}