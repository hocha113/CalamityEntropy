using CalamityEntropy.Content.Dusts;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.monument
{
    public class MonumentGround : ModProjectile
    {
        //地裂贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/monument/MonumentGround")]
        internal static Asset<Texture2D> GroundTex;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 92;
            Projectile.height = 92;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1.6f;
            Projectile.timeLeft = 30;

        }
        public override void OnSpawn(IEntitySource source) {
            if (!CEUtils.isAir(Projectile.Center)) {
                for (int i = 0; i < 20; i++) {
                    Dust.NewDust(Projectile.Center, 8, 8, ModContent.DustType<Vmpiece>());
                }
                SoundEngine.PlaySound(SoundID.DD2_GoblinBomb, Projectile.Center);
            }
        }
        public override void AI() {
            Projectile.timeLeft--;
            if (Projectile.timeLeft == 25) {
                if (Projectile.ai[1] > 0 && Projectile.owner == Main.myPlayer) {
                    int pj = Projectile.NewProjectile(Projectile.owner.ToPlayer().GetSource_FromAI(), Projectile.Center + new Vector2(Projectile.ai[2] * 80, 0), Vector2.Zero, Projectile.type, Projectile.damage, Projectile.knockBack, Projectile.owner, 0, Projectile.ai[1] - 1, Projectile.ai[2]);
                    pj.ToProj().direction = Projectile.direction;
                    pj.ToProj().rotation = Projectile.rotation;
                    pj.ToProj().netUpdate = true;
                }
            }

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (CEUtils.isAir(Projectile.Center)) {
                return false;
            }
            return projHitbox.Intersects(targetHitbox) && Projectile.timeLeft < 28;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCsAndTiles.Add(index);
        }
        public override bool PreDraw(ref Color lightColor) {
            if (CEUtils.isAir(Projectile.Center)) {
                return false;
            }
            Texture2D tx = GroundTex.Value;
            float sc = (float)(30 - Projectile.timeLeft) / 15f;
            if (Projectile.timeLeft <= 15) {
                sc = (float)Projectile.timeLeft / 15f;
            }

            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(0, tx.Height / 2), sc, SpriteEffects.None, 0);
            return false;
        }

    }

}