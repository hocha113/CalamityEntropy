using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class PrisonOfPermafrostCircle : ModProjectile
    {
        //法阵贴图组(火焰/冰晶/符环按序号收数组),加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/POP/flame", 1, 4, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FlameFrames;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/POP/ice", 1, 3, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] IceFrames;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/POP/c", 1, 6, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] RuneFrames;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/POP/triangle")]
        internal static Asset<Texture2D> TriangleTex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/POP/circle")]
        internal static Asset<Texture2D> CircleTex;
        public int usingTime = 0;
        public int counter = 0;
        public float a1 = 0;
        public float a2 = 0;
        public float a3 = 0;

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 60;
            usingTime = 0;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public float windVolume = 0;
        LoopSound windsound = null;
        public override void AI() {
            if (windsound == null) {
                windsound = new LoopSound(ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/wind_loop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);
                windsound.play();
            }
            windsound.timeleft = 2;
            windsound.setVolume_Dist(Projectile.Center, 200, 1200, windVolume);
            Player player = Main.player[Projectile.owner];
            Projectile.netImportant = true;
            player.manaRegenDelay = 80;
            Projectile.Center = player.Center + player.gfxOffY * Vector2.UnitY;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (player.channel && !player.HeldItem.IsAir && player.HeldItem.type == ModContent.ItemType<PrisonOfPermafrost>()) {
                if (Projectile.owner == Main.myPlayer) {
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (Main.MouseScreen + Main.screenPosition - player.Center).ToRotation(), 0.16f, false);
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (Main.MouseScreen + Main.screenPosition - player.Center).ToRotation(), 1.2f.ToRadians(), true);

                    Projectile.velocity = Projectile.rotation.ToRotationVector2();
                    Projectile.netUpdate = true;
                }

                usingTime++;
                Projectile.timeLeft = 60;
                if (usingTime > 60) {
                    for (int i = 0; i < 16; i++) {
                        //Smoke vd/ad字段spawn后赋,旧PRT/EParticle Smoke初始化器
                        var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 84 + CEUtils.randomVec(6), (Projectile.rotation + Main.rand.NextFloat(-0.6f, 0.6f)).ToRotationVector2() * Main.rand.NextFloat(34, 54), new Color(190, 226, 255) * 0.2f, 0.4f);  //Smoke vd/ad字段spawn后赋,旧EParticle Smoke初始化器
                        p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0f);
                    }
                    if (Projectile.owner == Main.myPlayer) {

                        if (usingTime < 100) {
                            if (usingTime % 5 == 0) {
                                int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 80 + new Vector2(12 + Main.rand.Next(-6, 7), 12 + Main.rand.Next(-60, 61)).RotatedBy(Projectile.rotation), Projectile.rotation.ToRotationVector2() * 40, ModContent.ProjectileType<IceSpike>(), (int)(Projectile.damage * 0.6f), Projectile.knockBack * 0.3f, Projectile.owner);
                            }
                        }
                        else {
                            if (usingTime < 160) {
                                if (usingTime % 4 == 0) {
                                    int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 80 + new Vector2(12 + Main.rand.Next(-6, 7), 12 + Main.rand.Next(-60, 61)).RotatedBy(Projectile.rotation), Projectile.rotation.ToRotationVector2() * 40, ModContent.ProjectileType<IceSpike>(), (int)(Projectile.damage * 0.6f), Projectile.knockBack * 0.3f, Projectile.owner);
                                }
                            }
                            else {
                                if (usingTime < 250) {
                                    if (usingTime % 3 == 0) {
                                        int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 80 + new Vector2(12 + Main.rand.Next(-6, 7), 12 + Main.rand.Next(-60, 61)).RotatedBy(Projectile.rotation), Projectile.rotation.ToRotationVector2() * 40, ModContent.ProjectileType<IceSpike>(), (int)(Projectile.damage * 0.6f), Projectile.knockBack * 0.3f, Projectile.owner);
                                    }
                                }
                                else {
                                    if (usingTime % 2 == 0) {
                                        int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 80 + new Vector2(12 + Main.rand.Next(-6, 7), 12 + Main.rand.Next(-60, 61)).RotatedBy(Projectile.rotation), Projectile.rotation.ToRotationVector2() * 40, ModContent.ProjectileType<IceSpike>(), (int)(Projectile.damage * 0.6f), Projectile.knockBack * 0.3f, Projectile.owner);
                                    }
                                }
                            }
                        }
                        if (usingTime % Math.Max(1, 65 - player.Entropy().WeaponBoost * 20) == 0 && usingTime > 120) {
                            Vector2 ofs;
                            float ag = (float)(Main.rand.NextDouble() * Math.PI * 2);
                            int projCount = 1 + (int)Math.Sqrt((usingTime + 500) / 110);
                            for (int i = 0; i < projCount; i++) {
                                ofs = Main.screenPosition + Main.MouseScreen + ag.ToRotationVector2() * 450 + new Vector2(0, -40);
                                int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 80 + new Vector2(Main.rand.Next(-17, 18), Main.rand.Next(-17, 18)), Vector2.Zero, ModContent.ProjectileType<Icicle>(), Projectile.damage, Projectile.knockBack * 3f, Projectile.owner, ofs.X, ofs.Y);
                                if (i == 0) {
                                    Main.projectile[p].ai[2] = 1;
                                }
                                ag += MathHelper.ToRadians(360f / (float)projCount);
                            }
                        }
                        if (usingTime % Math.Max(20, 180 - player.Entropy().WeaponBoost * 50) == 0) {
                            float anglep = MathHelper.ToRadians(5);
                            for (int i = 0; i < 6; i++) {
                                int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<IceEdge>(), Projectile.damage, 6);
                                Main.projectile[p].rotation = Projectile.rotation + anglep;
                                p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<IceEdge>(), Projectile.damage, 6);
                                Main.projectile[p].rotation = Projectile.rotation - anglep;

                                anglep += MathHelper.ToRadians(10);
                            }
                        }
                    }
                }
            }
            else {
                if (usingTime > 60) {
                    usingTime = 58;
                    Projectile.timeLeft = 60;
                }
                else {
                    usingTime -= 3;
                    Projectile.timeLeft = usingTime;
                }
            }
            if (counter % 12 == 0) {
                if (Projectile.damage < 1000) {
                    if (counter % 32 == 0) {
                        Projectile.damage += 1;

                    }
                }
                int cost = 2 + usingTime / 300;
                if (player.CheckMana(player.HeldItem, cost, true, false)) {
                    player.manaRegenDelay = 80;
                }
                else {
                    Projectile.Kill();
                }
            }
            counter++;
            if (usingTime > 60) {
                a1 += 0.02f;
                if (usingTime > 100) {
                    a2 += 0.02f;
                }
                else {
                    a2 -= 0.02f;
                }
                if (usingTime > 140) {
                    a3 += 0.02f;
                }
                else {
                    a3 -= 0.02f;
                }
                if (a1 > 1) {
                    a1 = 1;
                }
                if (a2 > 1) {
                    a2 = 1;
                }
                if (a3 > 1) {
                    a3 = 1;
                }
                if (a1 < 0) {
                    a1 = 0;
                }
                if (a2 < 0) {
                    a2 = 0;
                }
                if (a3 < 0) {
                    a3 = 0;
                }
            }
            else {
                a1 -= 0.02f;
                a2 -= 0.02f;
                a3 -= 0.02f;
                if (a1 < 0) {
                    a1 = 0;
                }
                if (a2 < 0) {
                    a2 = 0;
                }
                if (a3 < 0) {
                    a3 = 0;
                }
            }
            player.itemTime = 2;
            player.itemAnimation = 2;
            if (Projectile.velocity.X > 0) {
                player.direction = 1;
            }
            else {
                player.direction = -1;
            }
            if (Projectile.velocity.X > 0) {
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else {
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            windVolume = MathHelper.Clamp(usingTime / 60f, 0, 1);
            windsound.instance.Pitch = MathHelper.Clamp(usingTime / 60f, 0, 1) - 1;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.Entropy().OnProj != -1) {
                Projectile.Center = Projectile.Entropy().OnProj.ToProj().Center;
            }
            Texture2D[] flames = FlameFrames;
            Texture2D triangle = TriangleTex.Value;
            float alpha = (float)usingTime / 60f;
            if (alpha > 1) {
                alpha = 1;
            }
            Texture2D flameDraw = flames[(counter / 3) % 4];
            Main.spriteBatch.Draw(flameDraw, Projectile.Center - Main.screenPosition - new Vector2(0, 20), null, Color.White * alpha * 0.7f, 0, new Vector2(flameDraw.Width, flameDraw.Height) / 2, 1.3f, SpriteEffects.None, 0);
            int rg = (int)(220 + 35 * Math.Cos((float)counter / 10));
            Color triC = new Color(rg, rg, 255);

            Main.spriteBatch.Draw(triangle, Projectile.Center - Main.screenPosition + new Vector2(0, 12), null, triC * alpha, 0, new Vector2(triangle.Width, triangle.Height) / 2, 1, SpriteEffects.None, 0);

            Texture2D[] ices = IceFrames;
            float angle = ((float)counter) / 180f * (float)Math.PI * 2f;
            int size = (int)((alpha * 8.3f) * (alpha * 8.3f));
            Vector2 ofs = Vector2.Zero;

            for (int i = 0; i < 6; i++) {
                Texture2D itx = ices[i % 3];
                Main.spriteBatch.Draw(itx, Projectile.Center - Main.screenPosition + ofs + (new Vector2(size, 0).RotatedBy(angle + MathHelper.ToRadians(angle + i * 60))) * new Vector2(1, 0.8f), null, Color.White * alpha, 0, new Vector2(itx.Width, itx.Height) / 2, 1, SpriteEffects.None, 0);
            }

            Texture2D[] cs = RuneFrames;
            angle = -((float)counter) / 180f * (float)Math.PI * 2f;
            size = (int)((alpha * 12f) * (alpha * 12f)) + (int)(Math.Cos((float)counter / 10) * 30);
            int size2 = (int)((alpha * 12f) * (alpha * 12f)) - (int)(Math.Cos((float)counter / 10) * 30); ;
            ofs = Vector2.Zero;

            for (int i = 0; i < 6; i++) {
                Texture2D itx = cs[i];
                float alpha2 = alpha;
                if (i == 0 || i == 3) {
                    alpha2 = a1;
                }
                if (i == 1 || i == 4) {
                    alpha2 = a2;
                }
                if (i == 2 || i == 5) {
                    alpha2 = a3;
                }


                Main.spriteBatch.Draw(itx, Projectile.Center - Main.screenPosition + ofs + (new Vector2(size, 0).RotatedBy(angle + MathHelper.ToRadians(angle + i * 60))) * new Vector2(1, 0.8f), null, new Color(160, 180, 215) * alpha2 * 0.6f, 0, new Vector2(itx.Width, itx.Height) / 2, new Vector2(1.4f + 0.2f * (float)Math.Cos((float)counter / 20), 1.4f + 0.2f * (float)Math.Cos((float)counter / 20)), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(itx, Projectile.Center - Main.screenPosition + ofs + (new Vector2(size, 0).RotatedBy(angle + MathHelper.ToRadians(angle + i * 60))) * new Vector2(1, 0.8f), null, triC * alpha2, 0, new Vector2(itx.Width, itx.Height) / 2, 1, SpriteEffects.None, 0);


            }

            Texture2D circle = CircleTex.Value;
            angle = MathHelper.ToDegrees(counter);
            size = (int)(alpha * 100);
            Vector2 lu = new Vector2(size, 0).RotatedBy(angle - MathHelper.ToRadians(angle - 135));
            Vector2 ru = new Vector2(size, 0).RotatedBy(angle - MathHelper.ToRadians(angle - 45));
            Vector2 ld = new Vector2(size, 0).RotatedBy(angle - MathHelper.ToRadians(angle + 135));
            Vector2 rd = new Vector2(size, 0).RotatedBy(angle - MathHelper.ToRadians(angle + 45));

            lu.X *= 0.3f;
            ru.X *= 0.3f;
            ld.X *= 0.3f;
            rd.X *= 0.3f;

            Vector2 dp = Projectile.Center - Main.screenPosition;
            Vector2 offset = new Vector2(100, 0);

            lu += offset;
            ru += offset;
            ld += offset;
            rd += offset;

            lu = lu.RotatedBy(Projectile.rotation);
            ru = ru.RotatedBy(Projectile.rotation);
            ld = ld.RotatedBy(Projectile.rotation);
            rd = rd.RotatedBy(Projectile.rotation);

            CEUtils.drawTextureToPoint(Main.spriteBatch, circle, Color.White * alpha, dp + lu, dp + ru, dp + ld, dp + rd);
            Main.spriteBatch.ExitShaderRegion();
            return false;

        }
        public Texture2D itemTex => TextureAssets.Item[ModContent.ItemType<PrisonOfPermafrost>()].Value;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }

        public override bool? CanCutTiles() {
            return false;
        }
    }

}