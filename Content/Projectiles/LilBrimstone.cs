using CalamityEntropy.Content.Buffs;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class LilBrimstone : ModProjectile
    {
        //小硫火帧图集,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/LilBrimstone")]
        internal static Asset<Texture2D> SheetTex;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.light = 0.5f;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public int direction = 0;
        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 1800) {
                Projectile.Center = player.Center;
            }
            if (player.HasBuff(ModContent.BuffType<LilBrimstoneBuff>())) {
                Projectile.timeLeft = 3;
            }
            int index = -1;
            int pos = 1;
            foreach (Projectile p in Main.projectile) {
                if (p.whoAmI == Projectile.whoAmI) {
                    break;
                }
                if (p.type == Projectile.type && p.whoAmI != Projectile.whoAmI && p.active && p.owner == Projectile.owner) {
                    index = p.whoAmI;
                    pos++;
                }
            }
            Vector2 targetPos;
            float rot = (player.Center - Projectile.Center).ToRotation();
            float spacing = 110;
            if (index == -1) {
                targetPos = player.Center;
            }
            else {
                targetPos = index.ToProj().Center;
            }
            if (CEUtils.getDistance(Projectile.Center, targetPos) > spacing) {
                Projectile.velocity += (targetPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 4f;
                Projectile.velocity *= 0.8f;
            }
            else {
                Projectile.velocity *= 0.8f;
            }
            NPC target = null;
            if (player.HasMinionAttackTargetNPC) {
                target = Main.npc[player.MinionAttackTargetNPC];
                float betw = Vector2.Distance(target.Center, Projectile.Center);
                if (betw > 2000f) {
                    target = null;
                }

            }
            if (target == null || !target.active) {
                int t = Projectile.FindTargetWithLineOfSight(2000);
                if (t > -1) {
                    target = Main.npc[t];
                }
            }
            if (target == null) {
                Projectile.ai[0] = 0;
                Projectile.ai[1] = 0;
                direction = 1;
                if (player.Center.X < Projectile.Center.X) {
                    direction = -1;
                }
            }
            else {
                direction = 1;
                if (target.Center.X < Projectile.Center.X) {
                    direction = -1;
                }
                if (Projectile.ai[2] <= 0) {
                    Projectile.ai[0]++;

                    if (Projectile.ai[0] > 10) {
                        Projectile.ai[0] = 0;
                        Projectile.ai[1]++;

                        if (Projectile.ai[1] == 7) {
                            Projectile.ai[2] = 120;
                            direction = 1;

                            if (Projectile.owner == Main.myPlayer) {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + (target.Center + target.velocity * 20 - Projectile.Center).SafeNormalize(Vector2.UnitX) * 16, (target.Center + target.velocity * 20 - Projectile.Center).SafeNormalize(Vector2.UnitX), ModContent.ProjectileType<Brimstone>(), (int)(Projectile.damage * (1 + player.Entropy().WeaponBoost * 0.35f)), 5, Projectile.owner, 0, Projectile.scale * 0.4f + player.Entropy().WeaponBoost * 0.4f, Projectile.whoAmI);

                            }
                            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/blood laser weak 1"), Projectile.Center);
                        }
                    }

                }
            }
            Projectile.ai[2]--;
            if (Projectile.ai[2] == 0) {
                Projectile.ai[1] = 0;

            }

        }

        public override bool PreDraw(ref Color lightColor) {
            SpriteEffects ef = SpriteEffects.None;
            if (direction < 0) {
                ef = SpriteEffects.FlipHorizontally;
            }
            Texture2D tx;
            tx = SheetTex.Value;

            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tx, 8, (int)Projectile.ai[1]), Color.White, 0, new Vector2(46, 52) / 2, Projectile.scale, ef, 0);
            return false;
        }
    }
}

