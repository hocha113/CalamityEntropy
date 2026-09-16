using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.OblivionThresher
{
    public class OblivionCruiserDash : ModProjectile
    {
        //巡洋者头部与颚部贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/Head2")]
        internal static Asset<Texture2D> CruiserHeadTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawUp2")]
        internal static Asset<Texture2D> CruiserJawUpTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawDown2")]
        internal static Asset<Texture2D> CruiserJawDownTex;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        float mouthRot = 60f;
        public bool bite = false;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 24;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ArmorPenetration = 60;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 2;
        }
        public Vector2 spawnPos;
        public float spawnRot = 0;
        public float alphaPor = 1;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return Projectile.Center.getRectCentered(200 * Projectile.scale, 200 * Projectile.scale).Intersects(targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 90, 3.6f, 1000, 16);
        }
        float counter = 0;
        public override string Texture => CEUtils.WhiteTexPath;
        public override void AI() {
            counter++;
            Player player = Projectile.owner.ToPlayer();
            Projectile.rotation = Projectile.velocity.ToRotation();
            player.Center = Projectile.Center;
            player.velocity = Projectile.velocity * 0.2f;
            player.Entropy().immune = 10;
            mouthRot *= 0.88f;
            spawnParticles();
            int t = ModContent.ProjectileType<OblivionThresherShoot>();
            foreach (var p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == t) {
                    if (p.ModProjectile is OblivionThresherShoot ots) {
                        if (p.ai[0] >= 1f && p.localAI[1] > 80 && p.Colliding(p.Hitbox, Projectile.Hitbox)) {
                            player.velocity = -Projectile.velocity * 0.2f;
                            p.Kill();
                            foreach (var kh in Main.ActiveProjectiles) {
                                if (kh.owner == Projectile.owner && kh.ModProjectile is OblivionThresherHoldout) {
                                    kh.Kill();
                                }
                            }
                            if (Projectile.owner == Main.myPlayer) {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, Projectile.velocity, ModContent.ProjectileType<OblivionThresherHoldout>(), (int)(Projectile.damage * 2.5f), Projectile.knockBack, Projectile.owner);
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, Projectile.velocity, ModContent.ProjectileType<OblivionThresherShootAlt>(), (int)(Projectile.damage * 0.75f), Projectile.knockBack, Projectile.owner);
                            }
                            Projectile.Kill();
                            CEUtils.PlaySound("CastTriangles", 1, Projectile.Center);
                            return;
                        }
                    }
                }
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= 1;
            modifiers.FinalDamage *= 2;
        }
        public void spawnParticles() {
            var r = Main.rand;
            for (int i = 0; i < 2; i++) {
                //PRT_Void走EffectLoader void RT,vd/ad字段Configure前直赋
                //EParticle VoidParticles→PRT_Void,数值迁移一个不改
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.rotation.ToRotationVector2() * 60, new Vector2((float)((r.NextDouble() - 0.5) * .3), (float)((r.NextDouble() - 0.5) * 1.3)), Color.White, 1f);
                p.shape = 4;
                p.Opacity = 0.9f * (1 - Projectile.timeLeft / 24f);
                p.ad = 0.013f;
            }
            for (int i = 0; i < 2; i++) {
                //第二组Void带velocity偏移,vd/ad直赋+Configure对齐旧VoidParticles
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.rotation.ToRotationVector2() * 60 - Projectile.velocity * 0.5f, new Vector2((float)((r.NextDouble() - 0.5) * .3), (float)((r.NextDouble() - 0.5) * 1.3)), Color.White, 1f);
                p.shape = 4;
                p.Opacity = 0.9f * (1 - Projectile.timeLeft / 24f);
                p.ad = 0.013f;
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            lightColor = Color.White;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Vector2 vtodraw = Projectile.Center;
            SpriteBatch spriteBatch = Main.spriteBatch;
            float alpha = 1;
            if (Projectile.timeLeft < 10) {
                alpha = (float)Projectile.timeLeft / 10f;
            }
            Texture2D txd = CruiserHeadTex.Value;
            Texture2D j2 = CruiserJawUpTex.Value;
            Texture2D j1 = CruiserJawDownTex.Value;
            Vector2 joffset = new Vector2(60, 62);
            Vector2 ofs2 = joffset * new Vector2(1, -1);
            float roth = mouthRot * 0.8f;

            spriteBatch.Draw(j1, vtodraw - Main.screenPosition + joffset.RotatedBy(Projectile.rotation) * Projectile.scale, null, Color.White * alpha, Projectile.rotation + MathHelper.ToRadians(roth), new Vector2(40, 28), Projectile.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(j2, vtodraw - Main.screenPosition + ofs2.RotatedBy(Projectile.rotation) * Projectile.scale, null, Color.White * alpha, Projectile.rotation - MathHelper.ToRadians(roth), new Vector2(40, j2.Height - 28), Projectile.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(txd, vtodraw - Main.screenPosition, null, Color.White * alpha, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(j1, vtodraw - Main.screenPosition + joffset.RotatedBy(Projectile.rotation) * Projectile.scale, null, Color.White * alpha, Projectile.rotation + MathHelper.ToRadians(roth), new Vector2(40, 28), Projectile.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(j2, vtodraw - Main.screenPosition + ofs2.RotatedBy(Projectile.rotation) * Projectile.scale, null, Color.White * alpha, Projectile.rotation - MathHelper.ToRadians(roth), new Vector2(40, j2.Height - 28), Projectile.scale, SpriteEffects.None, 0);
            spriteBatch.Draw(txd, vtodraw - Main.screenPosition, null, Color.White * alpha, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.None, 0f);

            spriteBatch.ExitShaderRegion();
            return false;
        }
    }


}
