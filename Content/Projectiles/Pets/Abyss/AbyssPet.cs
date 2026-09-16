using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Pets.Abyss
{
    //head/body/tail旧写法是射弹实例字段Request,每条宠物实例化都拉贴图,加载/进世界能挂
    internal static class AbyssPetTextures
    {
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Abyss/Head")] internal static Asset<Texture2D> Head;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Abyss/Body")] internal static Asset<Texture2D> Body;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Abyss/Tail")] internal static Asset<Texture2D> Tail;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Abyss/AbyssPet")] internal static Asset<Texture2D> Menu;   //宠物栏预览那张
        //地面待机/行走帧动画,加载期一次就位,不再在 PreDraw 里每帧建表逐张请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Abyss/Afk", 1, 3, AssetMode = AssetMode.TextureValueArray)] internal static Texture2D[] AfkFrames;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Abyss/Walk", 1, 4, AssetMode = AssetMode.TextureValueArray)] internal static Texture2D[] WalkFrames;
    }

    public class AbyssPet : ModProjectile
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
            if (Projectile.ai[1] == 0) {
                Texture2D[] frames = Projectile.velocity.Length() < 1
                    ? AbyssPetTextures.AfkFrames
                    : AbyssPetTextures.WalkFrames;
                Texture2D tx = frames[(((int)counter / 6) % frames.Length)];
                if (Projectile.velocity.X > -2 && Projectile.velocity.X < 2f) {
                    if (player.Center.X > Projectile.Center.X) {
                        Projectile.direction = 1;
                    }
                    else {
                        Projectile.direction = -1;
                    }
                }
                if (Projectile.direction == -1) {
                    Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                }
                else {
                    Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
                }
            }
            else {
                Texture2D head = AbyssPetTextures.Head.Value;
                Texture2D body = AbyssPetTextures.Body.Value;
                Texture2D tail = AbyssPetTextures.Tail.Value;
                Main.EntitySpriteDraw(head, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(head.Width, head.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(body, bodyP - Main.screenPosition, null, lightColor, (Projectile.Center - bodyP).ToRotation(), new Vector2(body.Width, body.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tail, tailP - Main.screenPosition, null, lightColor, (bodyP - tailP).ToRotation(), new Vector2(body.Width, body.Height) / 2, Projectile.scale, SpriteEffects.None, 0);


            }

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
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 1400) {
                Projectile.Center = Main.player[Projectile.owner].Center - new Vector2(0, 50);
            }
            if (Projectile.ai[1] == 1) {
                counter++;
                Projectile.tileCollide = false;
                Projectile.rotation = Projectile.velocity.ToRotation();
                for (int i = 0; i < 2; i++) {
                    //宠物拖尾PRT_Void,Opacity直赋对齐旧VoidParticles
                    var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, new Vector2(0.3f, 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2), Color.White, 1f);
                    p.Opacity = 0.4f;  //Opacity旧初始化器值
                }
                for (int i = 0; i < 2; i++) {
                    //velocity/2偏移那圈也是旧VoidParticles双环spawn
                    var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.velocity / 2, new Vector2(0.3f, 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2), Color.White, 1f);
                    p.Opacity = 0.4f;
                }

                if (CEUtils.getDistance(Projectile.Center, targetPos) > 140) {
                    Vector2 px = targetPos - Projectile.Center;
                    px.Normalize();
                    Projectile.velocity += px * 1f;

                    Projectile.velocity *= 0.98f;

                }
                if (Projectile.Center.Y < targetPos.Y - 16 && CEUtils.getDistance(Projectile.Center, targetPos) < 100 && !(CEUtils.isAir(Projectile.owner.ToPlayer().Center + new Vector2(0, Projectile.owner.ToPlayer().height / 2 + 2), true)) && CEUtils.isAir(Projectile.Center)) {
                    Projectile.ai[1] = 0;
                }
                if (Projectile.velocity.X > 0) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }
            }
            else {
                if (Projectile.velocity.Length() < 1) {
                    counter += 1;
                }
                else {
                    counter += Math.Abs(Projectile.velocity.X / 1.5f);
                }
                Projectile.tileCollide = true;
                Projectile.rotation = 0;
                Projectile.velocity.Y += 0.5f;
                if (CEUtils.getDistance(targetPos, Projectile.Center) > 340 || (Math.Abs(targetPos.Y - Projectile.Center.Y) > 60 && Projectile.owner.ToPlayer().velocity.Y == 0)) {
                    Projectile.ai[1] = 1;
                }
                else if (CEUtils.getDistance(targetPos * new Vector2(1, 0), Projectile.Center * new Vector2(1, 0)) > 120) {
                    if (targetPos.X > Projectile.Center.X) {
                        Projectile.velocity.X += 0.6f;
                    }
                    else {
                        Projectile.velocity.X -= 0.6f;
                    }
                    Projectile.velocity.X *= 0.98f;
                }
                else {
                    Projectile.velocity.X *= 0.93f;
                }
                if (targetPos.X > Projectile.Center.X) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }

                if (Math.Abs(Projectile.velocity.X) > 0.5f && !CEUtils.isAir(Projectile.Center + (Projectile.velocity * new Vector2(1, 0)).SafeNormalize(Vector2.Zero) * 13 + new Vector2(0, 18))) {
                    Projectile.velocity.Y -= 1.5f;
                }
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
            if (!player.dead && player.HasBuff(ModContent.BuffType<AbyssBuff>())) {
                Projectile.timeLeft = 2;
            }

        }


    }
}
