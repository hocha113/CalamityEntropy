using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Nemesis
{
    internal class EXNemesisProj : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Nemesis/Nemesis";
        bool isFs;
        int alp;
        Vector2 origVer;
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 354;
            Projectile.timeLeft = 180;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 3;
            Projectile.scale = 5;
            Projectile.tileCollide = false;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            alp = 0;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (Projectile.ai[0] == 0) {
                origVer = Projectile.velocity;
                isFs = player.direction > 0;
            }
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3());
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (Projectile.ai[0] < 30) {
                Projectile.velocity = origVer.UnitVector() * 0.3f;
                if (alp < 300) {
                    alp += 10;
                }
            }
            else if (Projectile.ai[0] < 80) {
                alp = 300;
                Projectile.velocity = origVer.UnitVector() * 32;
                //dedServ时PRTLoader.NewParticle给孤儿实例,Configure照常
                if (!Main.dedServ) {
                    for (int i = 0; i < 6; i++) {
                        Vector2 ver = Projectile.velocity.GetNormalVector() * Main.rand.NextFloat(-116, 116);
                        //PRT_Light拖尾,Configure尾参lifetime对齐旧timeLeft
                        PRTLoader.NewParticle<PRT_Light>(Projectile.Center + Projectile.velocity * 10, ver, Color.OrangeRed, Main.rand.NextFloat(1.3f, 1.7f))
                            .Configure(0.2f, lifetime: 32);
                    }
                }
                PunchCameraModifier modifier = new PunchCameraModifier(Projectile.Center
                        , (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 20f, 6f, 20, 1000f, FullName);
                Main.instance.CameraModifiers.Add(modifier);
            }
            else {
                Projectile.velocity = origVer.UnitVector() * 0.3f;
                if (alp > 0) {
                    alp -= 10;
                }
            }

            Projectile.ai[0]++;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.BetsysCurse, 150);
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 180);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawOrigin = texture.Size() / 2;
            float rot = Projectile.rotation;
            float newAlp = alp / 300f;

            if (!isFs) {
                rot += MathHelper.PiOver2;
            }

            SpriteEffects spriteEffects = isFs ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int k = 0; k < Projectile.oldPos.Length; k++) {
                Vector2 offsetPos = Projectile.oldPos[k].To(Projectile.position);
                Vector2 drawPos = Projectile.Center - Main.screenPosition - offsetPos;
                Color color = Projectile.GetAlpha(Color.Pink) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color * newAlp, rot, drawOrigin, Projectile.scale, spriteEffects, 0);
            }

            if (alp > 155) {
                VaultUtils.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, Projectile.Center - Main.screenPosition
                , null, Color.Red, rot, drawOrigin, Projectile.scale, spriteEffects);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White * newAlp
                , rot, drawOrigin, Projectile.scale, spriteEffects, 0);
            return false;
        }
    }
}
