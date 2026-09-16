using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
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
using static CalamityEntropy.Content.NPCs.Apsychos.Apsychos;

namespace CalamityEntropy.Content.Items.Armor.Smoldering
{
    [AutoloadEquip(EquipType.Head)]
    public class SmolderingHelmet : ModItem
    {
        public static int MaxCells = 8;
        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.buyPrice(gold: 10);
            Item.defense = 5;
            Item.rare = ItemRarityID.LightRed;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) {
            return body.type == ModContent.ItemType<SmolderingBreastplate>() && legs.type == ModContent.ItemType<SmolderingGreaves>();
        }


        public override void UpdateArmorSet(Player player) {
            player.setBonus = Mod.GetLocalization("SmolderingSetBonus").Value;
            player.Entropy().smolderingSet = true;
            player.maxMinions += 1;
            // 潜行体系退役:原潜行条(上限0.8)按容量×10%换算为大招充能速度
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.08f;
        }
        public override void UpdateEquip(Player player) {
            player.GetCritChance(DamageClass.Generic) += 5;
            player.GetDamage(DamageClass.Generic) += 0.08f;
        }


        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<TectonicShard>(6)
                .AddIngredient(ItemID.MoltenHelmet)
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }
    public class SmolderingTail : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Generic, false, -1);
        }
        public override bool? CanCutTiles() => false;
        public List<TailSeg> segs = null;
        public void UpdateSegs(Player player, bool addVel = true) {
            if (segs == null) {
                segs = new List<TailSeg>();
                for (int i = 0; i < 9; i++) {
                    segs.Add(new TailSeg() { Center = Projectile.Center, rotation = 0 });
                }
            }
            Vector2 p1 = player.GetDrawCenter();
            Vector2 pm = player.GetDrawCenter() + new Vector2(player.direction * -90, 0);
            Vector2 p2 = Projectile.Center - Projectile.rotation.ToRotationVector2() * 90 * Projectile.scale;
            for (int i = 0; i < segs.Count; i++) {
                segs[i].Center += player.velocity;
                float pg = i / ((float)segs.Count);
                List<Vector2> lt = new List<Vector2> { p1, pm, p2, Projectile.Center + Projectile.velocity };
                Vector2 p = CEUtils.Bezier(lt, pg);
                segs[i].Center = p;

                Vector2 fp = i == 0 ? player.GetDrawCenter() : segs[i - 1].Center;
                segs[i].rotation = (segs[i].Center - fp).ToRotation();
            }
            if (addVel)
                Projectile.Center += player.velocity;
        }
        public int ShootDelay = 0;
        public override bool? CanDamage() {
            return Projectile.damage > 0;
        }
        bool t = true;
        public override void AI() {
            if (Projectile.localAI[1]++ == 0)
                t = Projectile.GetOwner().Entropy().smolderingSet;
            if (ShootDelay > 0)
                ShootDelay--;
            Player player = Projectile.GetOwner();
            if (player.dead) {
                Projectile.Kill();
                return;
            }
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 900)
                Projectile.Center = player.Center;
            if ((!player.Entropy().smolderingSet && t) || (player.Entropy().smolderingSet && !t) || !(player.Entropy().smolderingSet || player.Entropy().smdVisual)) {
                Projectile.Kill();
            }
            else {
                Projectile.timeLeft = 3;
            }
            player.Entropy().MouseWorldListener = true;
            float targetRot = 0;
            int delayMax = (int)(60 - 30 * (1f - (player.statLife / (float)player.statLifeMax2)));

            NPC target = Projectile.damage > 0 ? CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1600) : null;
            if (target == null) {
                Vector2 targetPos = player.Center + new Vector2(player.direction * -110, (float)(Math.Sin(Main.GameUpdateCount * 0.05f) * 56));
                Projectile.velocity += (targetPos - Projectile.Center) * 0.01f;
                Projectile.velocity *= 0.94f;

                targetRot = (Projectile.Center - player.Center).ToRotation();
                ShootDelay = delayMax;
                UpdateSegs(player, false);
            }
            else {
                Vector2 targetPos = player.Center + (target.Center - player.Center).normalize() * 60;

                Projectile.velocity += (targetPos - Projectile.Center) * 0.007f;
                Projectile.velocity *= 0.96f;
                targetRot = (target.Center - Projectile.Center).ToRotation();
                if (ShootDelay <= 0) {
                    ShootDelay = delayMax;
                    Vector2 shootVel = (target.Center - Projectile.Center).normalize() * 5;
                    Projectile.velocity -= shootVel * 2;
                    if (Main.myPlayer == Projectile.owner)
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, shootVel, ModContent.ProjectileType<SmolderingTailShoot>(), (int)player.GetTotalDamage(player.GetBestClass()).ApplyTo(60), 6, Projectile.owner);
                    var snd = SoundID.DD2_BetsyFireballShot with { Pitch = 0.4f };
                    SoundEngine.PlaySound(snd, Projectile.Center);

                }
                UpdateSegs(player);
            }
            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.14f, false);

        }
        //尾巴体节贴图,加载期就位,不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Items/Armor/Smoldering/SmolderingSeg")]
        internal static Asset<Texture2D> SegTex;
        public override bool PreDraw(ref Color lightColor) {
            lightColor = Color.White;
            Texture2D seg = SegTex.Value;
            foreach (var s in segs) {
                Main.EntitySpriteDraw(seg, s.Center - Main.screenPosition, null, lightColor, s.rotation, seg.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            }
            Texture2D t = Projectile.GetTexture();
            Main.EntitySpriteDraw(t, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(14, t.Height / 2), Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
    public class SmolderingTailShoot : ModProjectile
    {
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.hostile = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200 * 8;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 8;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.OnFire3, 180);
        }
        public override void AI() {
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 60) {
                oldPos.RemoveAt(0);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = Projectile.ai[0];
        }
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(2.6f, 3f), Projectile.Center, 8, 0.4f);
            float scale = 60 / 40f;
            //Smoldering重击爆炸,CustomPulse贴图路径现传走PRTPathTextures
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Red * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseAdditive();
            float scale = 1;

            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            for (int i = 0; i < oldPos.Count; i++) {
                float c = (i + 1f) / oldPos.Count;
                DrawEnergyBall(oldPos[i], scale * c, Projectile.Opacity * c);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.Draw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, Projectile.GetTexture().Size() * 0.5f, 1, SpriteEffects.None, 0);
            return false;
        }
        public void DrawEnergyBall(Vector2 pos, float size, float alpha) {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 230, 140) * alpha, 0, tex.Size() * 0.5f, size * 0.12f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(120, 90, 0) * alpha, 0, tex.Size() * 0.5f, size * 0.2f, SpriteEffects.None, 0);
        }
    }
}
