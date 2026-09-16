using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class CruiserShadow : ModProjectile
    {
        //巡洋者虚影贴图组:身体 P2b1~P2b7 按序号收进数组,头与上下颚单字段,加载期就位
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/P2b", 1, 7, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] BodyFrames;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/Head2")]
        internal static Asset<Texture2D> HeadTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawUp2")]
        internal static Asset<Texture2D> JawUpTex;
        [VaultLoaden("CalamityEntropy/Content/NPCs/Cruiser/CruiserJawDown2")]
        internal static Asset<Texture2D> JawDownTex;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        float mouthRot = 0;
        public bool bite = false;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 156;
            Projectile.height = 156;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ArmorPenetration = 60;
            Projectile.localNPCHitCooldown = 8;
        }
        public Vector2 spawnPos;
        public float spawnRot = 0;
        public float alphaPor = 1;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 90, 3.6f, 1000, 16);
            Projectile.netUpdate = true;
            bite = true;
            if (noChase < -10) {
                noChase = 10;
            }
        }
        public List<Vector2> bodies = new List<Vector2>();
        public override void OnSpawn(IEntitySource source) {
            for (int i = 0; i < 27; i++) {
                bodies.Add(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * -20);
            }
            Projectile.Center += Projectile.velocity * 16;
            targetPos = Projectile.Center;
            spawnPos = Projectile.Center;
            spawnRot = Projectile.velocity.ToRotation();
            Projectile.Center += Projectile.velocity * 10;
            Projectile.timeLeft += Projectile.owner.ToPlayer().Entropy().WeaponBoost * 120;
        }
        float counter = 0;
        public void DrawPortal(Vector2 pos, Color color, float rot, float size, float xmul = 0.3f, float aj = 0) {

            Texture2D tx = CEUtils.getExtraTex("SoulVortex");
            float angle = MathHelper.ToDegrees(counter * 0.2f + aj);
            Vector2 lu = new Vector2(size, 0).RotatedBy(MathHelper.ToRadians(angle - 135));
            Vector2 ru = new Vector2(size, 0).RotatedBy(MathHelper.ToRadians(angle - 45));
            Vector2 ld = new Vector2(size, 0).RotatedBy(MathHelper.ToRadians(angle + 135));
            Vector2 rd = new Vector2(size, 0).RotatedBy(MathHelper.ToRadians(angle + 45));

            lu.X *= xmul;
            ru.X *= xmul;
            ld.X *= xmul;
            rd.X *= xmul;

            Vector2 dp = pos - Main.screenPosition;
            float rangle = rot;
            lu = lu.RotatedBy(rangle);
            ru = ru.RotatedBy(rangle);
            ld = ld.RotatedBy(rangle);
            rd = rd.RotatedBy(rangle);

            CEUtils.drawTextureToPoint(Main.spriteBatch, tx, color, dp + lu, dp + ru, dp + ld, dp + rd);
        }
        public override void AI() {
            alphaPor *= 0.88f;
            counter++;
            if (counter % 20 == 0 && Main.myPlayer == Projectile.owner) {
                for (int i = 0; i < 6; i++) {
                    Projectile p = Main.projectile[Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity * 0.6f + new Vector2(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11)), ModContent.ProjectileType<VoidStarF>(), (int)(Projectile.damage * 0.16f), 5, Projectile.owner)];
                    p.DamageType = Projectile.DamageType;
                }
            }
            Player player = Projectile.owner.ToPlayer();
            updateBodies();
            if (bite) {
                mouthRot -= 12;
                if (mouthRot < -48) {
                    bite = false;
                }
            }
            else {
                mouthRot *= 0.9f;
            }
            spawnParticles();
            if (Main.myPlayer == Projectile.owner) {
                if (targetPos != Main.MouseWorld) {
                    Projectile.netUpdate = true;
                }
                targetPos = Main.MouseWorld;
            }

            Vector2 c = Projectile.Center;
            Projectile.Center = player.Center;
            NPC n = Projectile.FindMinionTarget();
            Projectile.Center = c;
            noChase--;

            if (Projectile.timeLeft < 40) {
                Projectile.velocity.Y -= 1;
                Projectile.velocity *= 0.98f;
                return;
            }
            if (n != null) {
                targetPos = n.Center;
                if (CEUtils.getDistance(targetPos, Projectile.Center) > 120) {
                    if (rt < 40) {
                        rt += Main.rand.NextFloat(1, 4);
                    }
                    Projectile.velocity *= 0.9f;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (targetPos - Projectile.Center).ToRotation(), rt.ToRadians());

                    Projectile.velocity = new Vector2(Projectile.velocity.Length() + 10, 0).RotatedBy(Projectile.rotation);
                }
                else {
                    rt = Main.rand.NextFloat(0, 10);
                    Projectile.velocity *= 1.01f;
                }
            }
            else {
                if (CEUtils.getDistance(Projectile.Center, targetPos) > 1000) {
                    Projectile.velocity += (targetPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 16f;
                    Projectile.velocity *= 0.8f;
                }
                else
                    if (CEUtils.getDistance(Projectile.Center, targetPos) > 100) {
                    Projectile.velocity += (targetPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.9f;
                    Projectile.velocity *= 0.996f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public float rt = 0;
        public int noChase = 0;
        public override void SendExtraAI(BinaryWriter writer) {
            writer.WriteVector2(targetPos);
            writer.Write(bite);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            targetPos = reader.ReadVector2();
            bite = reader.ReadBoolean();
        }
        Vector2 targetPos;

        public void updateBodies() {
            for (int i = 0; i < bodies.Count; i++) {
                Vector2 oPos;
                float oRot;

                if (i == 0) {
                    oPos = Projectile.Center;
                    oRot = Projectile.rotation;
                }
                else {
                    oPos = bodies[i - 1];
                    if (i == 1) {
                        oRot = (Projectile.Center - bodies[0]).ToRotation();
                    }
                    else {
                        oRot = (bodies[i - 2] - bodies[i - 1]).ToRotation();
                    }
                }
                float rot = (oPos - bodies[i]).ToRotation();
                rot = CEUtils.RotateTowardsAngle(rot, oRot, 0.12f, false);
                int spacing = 54;
                bodies[i] = oPos - rot.ToRotationVector2() * spacing * Projectile.scale;
            }
        }

        public void spawnParticles() {
            var r = Main.rand;
            for (int i = 0; i < 4; i++) {
                //PRT_Void字段直赋对齐旧VoidParticles,Opacity/ad/multShrink Configure管不了
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.rotation.ToRotationVector2() * 60, new Vector2((float)((r.NextDouble() - 0.5) * .3), (float)((r.NextDouble() - 0.5) * 1.3)), Color.White, 1f);
                p.shape = 4;
                p.Opacity = 1.6f;
                p.ad = 0.013f;
            }
            for (int i = 0; i < 4; i++) {
                //每帧拖尾Void,旧spawnNew也是AI里无脑刷
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.rotation.ToRotationVector2() * 60 - Projectile.velocity * 0.5f, new Vector2((float)((r.NextDouble() - 0.5) * .3), (float)((r.NextDouble() - 0.5) * 1.3)), Color.White, 1f);
                p.shape = 4;
                p.Opacity = 1.6f;
                p.ad = 0.013f;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            int bd = 0;
            Vector2 vtodraw = Projectile.Center;
            SpriteBatch spriteBatch = Main.spriteBatch;
            float alpha = 1;
            if (Projectile.timeLeft < 40) {
                alpha = (float)Projectile.timeLeft / 40f;
            }
            for (int d = 0; d < 9; d++) {
                if (d < bodies.Count) {
                    if (d == 0 || d == 2) {
                        continue;
                    }
                    float rot = 0;
                    if (bd == 0) {
                        rot = (vtodraw - bodies[d]).ToRotation();
                    }
                    else {
                        rot = (bodies[d - 1] - bodies[d]).ToRotation();
                    }
                    Vector2 pos = bodies[d];

                    Texture2D tx;
                    tx = BodyFrames[bd];

                    spriteBatch.Draw(tx, pos - Main.screenPosition, null, Color.White * alpha, rot, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0f);

                    bd += 1;

                }
            }
            Texture2D txd = HeadTex.Value;
            Texture2D j2 = JawUpTex.Value;
            Texture2D j1 = JawDownTex.Value;
            Vector2 joffset = new Vector2(60, 62);
            Vector2 ofs2 = joffset * new Vector2(1, -1);
            float roth = mouthRot * 0.8f;

            spriteBatch.Draw(j1, vtodraw - Main.screenPosition + joffset.RotatedBy(Projectile.rotation) * Projectile.scale, null, Color.White * alpha, Projectile.rotation + MathHelper.ToRadians(roth), new Vector2(40, 28), Projectile.scale, SpriteEffects.None, 0);

            spriteBatch.Draw(j2, vtodraw - Main.screenPosition + ofs2.RotatedBy(Projectile.rotation) * Projectile.scale, null, Color.White * alpha, Projectile.rotation - MathHelper.ToRadians(roth), new Vector2(40, j2.Height - 28), Projectile.scale, SpriteEffects.None, 0);

            spriteBatch.Draw(txd, vtodraw - Main.screenPosition, null, Color.White * alpha, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.None, 0f);

            return false;
        }
    }


}