using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Pets.Abyss
{
    public class VoidPalProj : ModProjectile
    {
        public float counter = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            base.SetStaticDefaults();

        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 24;
            Projectile.height = 44;
        }
        public Vector2 bodyP = Vector2.Zero;
        public Vector2 tailP = Vector2.Zero;
        public override bool PreDraw(ref Color lightColor) {

            if (Main.gameMenu) {
                Texture2D txd = AbyssPetTextures.Menu.Value;
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            Player player = Main.player[Projectile.owner];
            if (counter > 36) {
                counter -= 36;
            }
            Texture2D head = AbyssPetTextures.Head.Value;   //贴图在AbyssPet.cs的static VaultLoaden表,别改回实例字段
            Texture2D body = AbyssPetTextures.Body.Value;
            Texture2D tail = AbyssPetTextures.Tail.Value;
            Main.EntitySpriteDraw(head, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(head.Width, head.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(body, bodyP - Main.screenPosition, null, lightColor, (Projectile.Center - bodyP).ToRotation(), new Vector2(body.Width, body.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tail, tailP - Main.screenPosition, null, lightColor, (bodyP - tailP).ToRotation(), new Vector2(body.Width, body.Height) / 2, Projectile.scale, SpriteEffects.None, 0);




            return false;

        }
        void MoveToTarget(Vector2 targetPos) {
            Lighting.AddLight(Projectile.Center, 1.2f, 1.2f, 1.2f);
            bodyP = Projectile.Center + (bodyP - Projectile.Center).SafeNormalize(Vector2.Zero) * 32;
            tailP = bodyP + (tailP - bodyP).SafeNormalize(Vector2.Zero) * 32;
            float br = (Projectile.Center - bodyP).ToRotation();
            float tr = (bodyP - tailP).ToRotation();
            br = CEUtils.RotateTowardsAngle(br, Projectile.rotation, 0.1f, false);
            tr = CEUtils.RotateTowardsAngle(tr, br, 0.1f, false);
            bodyP = Projectile.Center - br.ToRotationVector2() * 32;
            tailP = bodyP - tr.ToRotationVector2() * 32;
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 1800) {
                Projectile.Center = Main.player[Projectile.owner].Center - new Vector2(0, 50);
            }
            counter++;
            Projectile.tileCollide = false;
            Projectile.rotation = Projectile.velocity.ToRotation();
            for (int i = 0; i < 2; i++) {
                //PRT_Void字段直赋对齐旧VoidParticles,Opacity/ad/multShrink Configure管不了
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, new Vector2(0.3f, 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2), Color.White, 1f);
                p.Opacity = 0.4f;  //Opacity旧初始化器字段,Configure管不了
            }
            for (int i = 0; i < 2; i++) {
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.velocity / 2, new Vector2(0.3f, 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2), Color.White, 1f);
                p.Opacity = 0.4f;
            }

            if (CEUtils.getDistance(Projectile.Center, targetPos) > 140) {
                Vector2 px = targetPos - Projectile.Center;
                px.Normalize();
                Projectile.velocity += px * 0.36f;

                Projectile.velocity *= 0.996f;

            }
            if (Projectile.velocity.X > 0) {
                Projectile.direction = 1;
            }
            else {
                Projectile.direction = -1;
            }
        }



        public override bool PreAI() {
            Player player = Main.player[Projectile.owner];

            player.zephyrfish = false;
            return true;
        }

        public override void AI() {

            Player player = Main.player[Projectile.owner];
            MoveToTarget(player.Center + new Vector2(0, 0));
            if (!player.dead && player.HasBuff(ModContent.BuffType<VoidPal>())) {
                Projectile.timeLeft = 2;
            }

        }


    }
}
