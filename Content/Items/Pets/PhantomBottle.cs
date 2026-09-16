using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class PhantomBottle : ModItem
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<CruiserPhantomPet>();
            Item.buffType = ModContent.BuffType<PhantomOfCruiser>();
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }

    }
    public class PhantomOfCruiser : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<CruiserPhantomPet>());
        }
    }
    public class CruiserPhantomPet : ModProjectile
    {
        //幻影巡游者的身体帧与头部贴图在加载期就位,绘制时不再逐帧请求
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
            Projectile.width = 156;
            Projectile.height = 156;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Main.projPet[Type] = true;
        }
        public Vector2 spawnPos;
        public float spawnRot = 0;
        public List<Vector2> bodies = new List<Vector2>();
        float counter = 0;
        public override void AI() {
            if (counter == 0) {
                for (int i = 0; i < 27; i++) {
                    bodies.Add(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * -20);
                }
                Projectile.Center += Projectile.velocity * 16;
                targetPos = Projectile.Center;
                spawnPos = Projectile.Center;
                spawnRot = Projectile.velocity.ToRotation();
                Projectile.Center += Projectile.velocity * 10;
            }
            counter++;
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

            Projectile.velocity += Projectile.rotation.ToRotationVector2() * 1;
            float r = Utils.Remap(Projectile.Distance(Projectile.GetOwner().Center), 100, 3600, 0, 0.16f);
            if (f-- > 0)
                r *= 0.1f;
            Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (Projectile.GetOwner().Center - Projectile.Center).ToRotation(), r, false));
            if (f < -5 && Main.rand.NextBool(80))
                f = Main.rand.Next(2, 32);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.Length() < Utils.Remap(Projectile.Distance(Projectile.GetOwner().Center), 1600, 3600, 2, 26))
                Projectile.velocity *= 1.006f;
            else
                Projectile.velocity *= 0.95f;
            if (Projectile.GetOwner().HasBuff<PhantomOfCruiser>()) {
                Projectile.timeLeft = 5;
            }
            if (Projectile.Distance(Projectile.GetOwner().Center) > 6000)
                Projectile.Center = Projectile.GetOwner().Center;
        }
        public float rt = 0;
        public int noChase = 0;
        Vector2 targetPos;
        public int f = 0;
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
                //PRT_Void Opacity/vd spawn后赋,旧VoidParticles原值
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.rotation.ToRotationVector2() * 60 + Projectile.velocity, new Vector2((float)((r.NextDouble() - 0.5) * .3), (float)((r.NextDouble() - 0.5) * 1.3)), Color.White, 1f);
                p.shape = 4;
                p.Opacity = 1.6f;
                p.ad = 0.013f;
            }
            for (int i = 0; i < 4; i++) {
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.rotation.ToRotationVector2() * 60 - Projectile.velocity * 0.5f + Projectile.velocity, new Vector2((float)((r.NextDouble() - 0.5) * .3), (float)((r.NextDouble() - 0.5) * 1.3)), Color.White, 1f);
                p.shape = 4;
                p.Opacity = 1.6f;
                p.ad = 0.013f;
            }
        }

        public override bool PreDraw(ref Color lightColor) {

            return false;
        }
        public void draw() {
            int bd = 0;
            Vector2 vtodraw = Projectile.Center;
            SpriteBatch spriteBatch = Main.spriteBatch;
            float alpha = 1;
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

                    Texture2D tx = BodyFrames[bd];

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

        }
    }
}