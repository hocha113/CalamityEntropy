using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Cruiser
{

    public class CruiserBlackhole : ModProjectile
    {
        //黑洞主体与吸积盘贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Cruiser/Blackhole_p1")]
        internal static Asset<Texture2D> BlackholeP1Tex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Cruiser/Blackhole_p2")]
        internal static Asset<Texture2D> BlackholeP2Tex;
        public List<Vector2> tp1 = new List<Vector2>();
        public List<Vector2> tp2 = new List<Vector2>();

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void SetDefaults() {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 460;
        }
        public bool projSpawn = true;
        public bool sp = true;
        public override void AI() {
            foreach (Player p in Main.player) {
                if (CEUtils.getDistance(Projectile.Center, p.Center) < 6000) {
                    p.velocity += (Projectile.Center - p.Center).SafeNormalize(Vector2.Zero) * 0.24f;
                    if (CEUtils.getDistance(Projectile.Center, p.Center) > 1000) {
                        p.velocity += (Projectile.Center - p.Center).SafeNormalize(Vector2.Zero) * 0.5f;
                    }
                    if (CEUtils.getDistance(Projectile.Center, p.Center) > 2000) {
                        p.velocity += (Projectile.Center - p.Center).SafeNormalize(Vector2.Zero) * 0.5f;
                    }
                }
            }
            if (sp) {
                sp = false;
                if (!Main.dedServ) {
                    SoundStyle s = new("CalamityEntropy/Assets/Sounds/blackholeCharge");
                    SoundEngine.PlaySound(s, Projectile.Center);
                }
            }
            if (Projectile.timeLeft < 100) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    if (projSpawn) {
                        projSpawn = false;
                        for (int i = 0; i < 40 + (Main.expertMode ? 16 : 0) + (Main.masterMode ? 16 : 0); i++) {
                            CEUtils.SyncProj(Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(Main.rand.Next(0, 35), 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2), ModContent.ProjectileType<VoidStar>(), Projectile.damage, 4, 0, 0, 0, 1));
                        }
                    }
                }
                scale2 -= 0.025f;
                if (scale2 <= 0) {
                    if (Main.netMode != NetmodeID.MultiplayerClient) {
                        CEUtils.SyncProj(Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), Projectile.damage, 0, -1, 0, 1));
                        CEUtils.SyncProj(Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BHExp>(), Projectile.damage, 0));


                    }
                    if (!Main.dedServ) {
                        SoundStyle s = new("CalamityEntropy/Assets/Sounds/blackholeEnd");
                        SoundEngine.PlaySound(s, Projectile.Center);
                        CEUtils.SetShake(Projectile.Center, 36, 1800);

                    }
                    for (int i = 0; i < 64; i++) {
                        //PRT_Void字段直赋对齐旧VoidParticles,Opacity/ad/multShrink Configure管不了
                        var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, new Vector2(Main.rand.Next(0, 6), 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2), Color.White, 1f);
                        p.Opacity = 1f;  //Opacity旧初始化器字段,Configure管不了
                    }

                    Projectile.Kill();
                }
            }
            if (Projectile.timeLeft > 250) {
                if (Projectile.timeLeft % 6 == 0) {
                    if (Main.netMode != NetmodeID.MultiplayerClient) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(1900, 0).RotatedByRandom(Math.PI), Vector2.Zero, ModContent.ProjectileType<CruiserBlackholeBullet>(), Projectile.damage, 0, -1, 0, Projectile.Center.X, Projectile.Center.Y);
                    }
                }
            }
            counter++;
            if (counter % 4 == 0 && Projectile.timeLeft > 100) {
                Vector2 xv = new Vector2(2600, 0).RotatedBy(Main.rand.NextDouble() * Math.PI * 2);
                tp1.Add(xv);
                tp2.Add(xv);
            }

            for (int i = 0; i < tp1.Count; i++) {
                tp1[i] *= 0.9f;
                tp2[i] = tp2[i] + (tp1[i] - tp2[i]) * 0.1f;
            }
        }
        public int counter = 0;
        public float scale2 = 1;
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 160);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D htx = CEExtraAssets.lightball;
            Texture2D tx = BlackholeP1Tex.Value;
            Texture2D tx2 = BlackholeP2Tex.Value;
            Projectile.ai[0] += 6;
            Main.spriteBatch.Draw(htx, Projectile.Center - Main.screenPosition, null, Color.Black, 0, htx.Size() / 2, 2f + (float)(1f + Math.Cos(Projectile.ai[0] / 60f) * 0.3f) * scale2, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(htx, Projectile.Center - Main.screenPosition, null, Color.Black, 0, htx.Size() / 2, 2f + (float)(1f + Math.Cos(Projectile.ai[0] / 60f) * 0.3f) * scale2, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White, MathHelper.ToRadians(Projectile.ai[0]), tx.Size() / 2, 2.4f * scale2, SpriteEffects.None, 0);

            for (int i = 0; i < tp1.Count; i++) {
                Main.spriteBatch.Draw(tx2, Projectile.Center + tp2[i] - Main.screenPosition, null, Color.White * 0.3f, (tp1[i] - tp2[i]).ToRotation(), new Vector2(0, tx2.Height / 2), new Vector2(CEUtils.getDistance(tp1[i], tp2[i]) / tx2.Width, 2), SpriteEffects.None, 0);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }

}