using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class WhipOfServiceProjectile : ModProjectile
    {
        public int[] hitCd = new int[Main.player.Length];
        public override void SetStaticDefaults() {
            ProjectileID.Sets.IsAWhip[Type] = true;
        }
        public override void SetDefaults() {
            Projectile.DefaultToWhip();
            Projectile.MaxUpdates = 8;
            Projectile.WhipSettings.Segments = 48;
            Projectile.WhipSettings.RangeMultiplier = 2.2f;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.Entropy().serviceWhipDamageBonus += 0.07f;
        }
        public override bool CanHitPlayer(Player target) {
            return false;
        }
        public override bool PreAI() {
            Projectile.hostile = true;
            for (int i = 0; i < hitCd.Length; i++) {
                if (hitCd[i] > 0) {
                    hitCd[i]--;
                }
            }
            var owner = Projectile.owner.ToPlayer();
            return true;
        }

        private void DrawLine(List<Vector2> list) {
            /*Texture2D texture = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/white").Value;
			Rectangle frame = texture.Frame();
			Vector2 origin = new Vector2(0, 0.5f);

			Vector2 pos = list[0];
			for (int i = 0; i < list.Count - 1; i++) {
				Vector2 element = list[i];
				Vector2 diff = list[i + 1] - element;

				float rotation = diff.ToRotation();
				Color color = new Color(138, 76, 57);
				Vector2 scale = new Vector2(diff.Length() + 2, 2);
				if (i == list.Count - 2)
				{
					scale.X -= 8;
				}

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

				pos += diff;
			}*/
        }
        private float Timer {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public override bool PreDraw(ref Color lightColor) {
            List<Vector2> list_ = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, list_);
            List<Vector2> list = list_;
            DrawLine(list);


            SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Vector2 pos = list[0];

            for (int i = 0; i < list.Count - 1; i++) {
                Rectangle frame = new Rectangle(0, 0, 22, 30); Vector2 origin = new Vector2(7, 4); float scale = 1f;

                if (i == list.Count - 2) {
                    frame.Y = 36; frame.Height = 20;
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = 1f;
                    origin = new Vector2(7, 0);

                }
                else if (i > 0) {
                    frame.Y = 14;
                    frame.Height = 14;
                    origin = new Vector2(7, 0);
                }

                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;

                float rotation = diff.ToRotation() - MathHelper.PiOver2; Color color = Lighting.GetColor((int)(pos.X / 16), (int)(pos.Y / 16));

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, flip, 0);

                pos += diff;
            }
            return false;
        }
    }
}
