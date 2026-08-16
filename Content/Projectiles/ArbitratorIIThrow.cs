using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class ArbitratorIIThrow : ModProjectile, IJavelin
    {
        List<Vector2> odp = new List<Vector2>();
        List<float> odr = new List<float>();
        public bool SetHandRot { get; set; }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = CEUtils.RogueDC;
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 260;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.ArmorPenetration = 6;
            SetHandRot = true;
        }
        public float handrot = 0;
        public float handrotspeed = 0;
        public Vector2 ownerMouse = Vector2.Zero;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.rotation);
            writer.Write(handrot);
            writer.Write(stick);
            writer.Write(stickNpc);
            writer.WriteVector2(offset);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.rotation = reader.ReadSingle();
            handrot = reader.ReadSingle();
            stick = reader.ReadBoolean();
            stickNpc = reader.ReadInt32();
            offset = reader.ReadVector2();
        }
        public override void OnSpawn(IEntitySource source)
        {
            foreach (Projectile p in Main.projectile)
            {
                if (p.whoAmI != Projectile.whoAmI)
                {
                    if (p.ModProjectile is IJavelin jv)
                    {
                        jv.SetHandRot = false;
                    }
                }
            }
        }
        public override void AI()
        {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            frameChange--;
            if (frameChange <= 0)
            {
                frame++;
                frameChange = 4;
            }
            if (odp.Count > 16)
            {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
            if (Projectile.ai[0] == 0)
            {
                handrotspeed = -0.3f;
            }
            else if (Projectile.ai[0] < 12)
            {
                handrotspeed += 0.056f;
            }
            if (Projectile.ai[0] < 12)
            {

                var owner = Projectile.owner.ToPlayer();

                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.rotation = (Main.MouseWorld - Projectile.Center).ToRotation();
                    Projectile.netUpdate = true;
                }
                if (this.SetHandRot)
                {
                    Projectile.owner.ToPlayer().heldProj = Projectile.whoAmI;
                    if (owner.direction == 1)
                    {
                        Projectile.Center = owner.MountedCenter + new Vector2(26, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver2 - handrot);
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - handrot - MathHelper.Pi);
                    }
                    else
                    {
                        Projectile.Center = owner.MountedCenter + new Vector2(26, 0).RotatedBy(Projectile.rotation + MathHelper.PiOver2 + handrot);
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + handrot);
                    }
                }
                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(Projectile.rotation);
            }
            else if (Projectile.ai[0] < 36)
            {
                handrotspeed *= 0.84f;
                var owner = Projectile.owner.ToPlayer();
                if (this.SetHandRot)
                {
                    if (owner.direction == 1)
                    {
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - handrot - MathHelper.Pi);
                    }
                    else
                    {
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + handrot);

                    }
                    Projectile.owner.ToPlayer().heldProj = -1;
                }

            }
            if (Projectile.ai[0] > 12)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                for (int i = 0; i < 10; i++)
                {
                    EclipseMetaball.SpawnParticle(Projectile.Center + (i * 0.1f * Projectile.velocity), CEUtils.randomPointInCircle(2.5f), Main.rand.NextFloat(12f, 29f));
                }
            }
            handrot -= handrotspeed;
            if (Projectile.ai[0] == 10)
            {

                SoundStyle SwingSound = SoundID.Item1;
                SwingSound.Pitch = 0f;
                if (Projectile.Calamity().stealthStrike)
                {
                    SwingSound.Pitch = 1f;
                }

                SoundEngine.PlaySound(SwingSound, Projectile.Center);
            }
            if (stick)
            {
                if (stickNpc.ToNPC().active)
                {
                    Projectile.Center = stickNpc.ToNPC().Center + offset;
                }
                else
                {
                    stick = false;
                    Projectile.Kill();
                }
            }
            Projectile.ai[0]++;
            hitcd--;
        }
        public int hitcd = 0;
        public bool spawnShard = true;
        public bool stick = false;
        public int stickNpc = -1;
        public Vector2 offset;
        public int hitc = 0;
        public bool exp = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 26; i++)
            {
                EclipseMetaball.SpawnParticle(target.Center, CEUtils.randomPointInCircle(10), Main.rand.NextFloat(18f, 36f));
            }
            if (exp)
            {
                SoundEngine.PlaySound(new("CalamityMod/Sounds/NPCKilled/DevourerSegmentBreak1") { Volume = 0.3f }, Projectile.Center);
                float sparkCount = Projectile.Calamity().stealthStrike ? 26 : 16;
                for (int i = 0; i < sparkCount; i++)
                {
                    Vector2 sparkVelocity2 = new Vector2(16, 0).RotatedBy(Projectile.rotation).RotatedByRandom(MathHelper.ToRadians(40)) * Main.rand.NextFloat(0.5f, 1.8f);
                    int sparkLifetime2 = Main.rand.Next(20, 24);
                    float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                    Color sparkColor2 = Color.Black;

                    float velc = Projectile.Calamity().stealthStrike ? 1.5f : 0.9f;
                    if (Main.rand.NextBool())
                    {
                        AltSparkParticle spark = new AltSparkParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * velc, false, (int)(sparkLifetime2 * 1), sparkScale2 * 1, sparkColor2);
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                    else
                    {
                        LineParticle spark = new LineParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * velc * 1.2f, false, (int)(sparkLifetime2 * 1), sparkScale2 * 1, Main.rand.NextBool() ? Color.Yellow
                            : Color.Orange);
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }
                if (!Projectile.Calamity().stealthStrike)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        Projectile.localNPCImmunity[target.whoAmI] = 0;
                        AltSparkParticle spark = new AltSparkParticle(target.Center, CEUtils.randomPointInCircle(12), false, 60, Main.rand.NextFloat(1.4f, 2.4f), Color.Black);
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                    for (int j = 0; j < 32; j++)
                    {
                        Projectile.localNPCImmunity[target.whoAmI] = 0;
                        AltSparkParticle spark = new AltSparkParticle(target.Center, CEUtils.randomPointInCircle(12), false, 60, Main.rand.NextFloat(1.4f, 2.4f), Color.OrangeRed);
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }
            }
            if (exp)
            {
                if (Projectile.Calamity().stealthStrike)
                {
                    if (!stick)
                    {
                        offset = Projectile.Center - target.Center;
                        stick = true;
                        stickNpc = target.whoAmI;
                    }
                    if (hitc == 1)
                    {
                        SoundEngine.PlaySound(in SoundID.NPCDeath56, Projectile.Center);
                        for (int i = 0; i < 4; i++)
                        {
                            for (int j = 0; j < 20; j++)
                            {
                                Projectile.localNPCImmunity[target.whoAmI] = 0;
                                AltSparkParticle spark = new AltSparkParticle(target.Center + new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)), new Vector2(Main.rand.NextFloat(0, 16)).RotatedBy(MathHelper.ToRadians(i * 90)), false, 60, Main.rand.NextFloat(1, 1.4f), Color.Black);
                                GeneralParticleHandler.SpawnParticle(spark);
                                AltSparkParticle spark2 = new AltSparkParticle(target.Center + new Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)), new Vector2(Main.rand.NextFloat(0, 16)).RotatedBy(MathHelper.ToRadians(i * 90)), false, 60, Main.rand.NextFloat(1, 1.4f), Color.OrangeRed);
                                GeneralParticleHandler.SpawnParticle(spark2);

                            }
                        }
                        for (int j = 0; j < 46; j++)
                        {
                            AltSparkParticle spark = new AltSparkParticle(target.Center, CEUtils.randomPointInCircle(12), false, 60, Main.rand.NextFloat(1.4f, 2.4f), Color.Black);
                            GeneralParticleHandler.SpawnParticle(spark);
                        }
                        for (int j = 0; j < 46; j++)
                        {
                            AltSparkParticle spark = new AltSparkParticle(target.Center, CEUtils.randomPointInCircle(12), false, 60, Main.rand.NextFloat(1.4f, 2.4f), Color.OrangeRed);
                            GeneralParticleHandler.SpawnParticle(spark);
                        }
                        for (int i = 0; i < 64; i++)
                        {
                            EclipseMetaball.SpawnParticle(target.Center, CEUtils.randomPointInCircle(21), Main.rand.NextFloat(30f, 64f));
                        }
                        exp = false;
                        if(Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ArbitratorStealthImpactSpawner>(), (int)(Projectile.damage * 0.2f), 0, Projectile.owner);
                        }
                        Projectile.Kill();
                    }
                    else
                    {
                        hitcd = 60;
                    }
                    hitc++;
                }
                else
                {
                    Projectile.localNPCImmunity[target.whoAmI] = 0;
                    Projectile.velocity *= 0.01f;
                    Projectile.Resize(300, 300);
                    Projectile.timeLeft = 2;
                    CalamityMod.Particles.Particle explosion2 = new DetailedExplosion(target.Center, Vector2.Zero, Color.Orange, Vector2.One, Main.rand.NextFloat(-5, 5), 0f, 1.4f, 24);
                    GeneralParticleHandler.SpawnParticle(explosion2);
                    exp = false;
                }
            }
            Projectile.netUpdate = true;
        }
        public override bool ShouldUpdatePosition()
        {
            return Projectile.ai[0] >= 12 && !stick && exp;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (hitcd > 0)
            {
                return false;
            }
            if (Projectile.ai[0] <= 10)
            {
                return false;
            }
            return null;
        }
        public bool eff = true;
        public int frame = 0;
        public int frameChange = 4;
        public bool breakHandle = false;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D handle = CEUtils.getExtraTex("Arbitrator/Arb0");
            Texture2D tx1 = CEUtils.getExtraTex("Arbitrator/Arb2");
            Texture2D tx2 = CEUtils.getExtraTex("Arbitrator/Arb3");
            Texture2D tx3 = CEUtils.getExtraTex("Arbitrator/Arb4");
            List<Texture2D> tx = new List<Texture2D>() { tx1, tx2, tx3 };
            float rj = 0;
            if (Projectile.ai[0] < 12)
            {
                rj = -handrot * Projectile.owner.ToPlayer().direction;
            }

            Main.EntitySpriteDraw(handle, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4 + rj, handle.Size() / 2, Projectile.scale, SpriteEffects.None);

            Main.EntitySpriteDraw(tx[frame % tx.Count], Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4 + rj, tx[0].Size() / 2, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
    public class ArbitratorStealthImpactSpawner : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.timeLeft = 28;
            Projectile.width = Projectile.height = 32;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public override void AI()
        {
            if(Main.myPlayer == Projectile.owner && Projectile.timeLeft % 3 == 0)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ArbitratorStealthImpact>(), Projectile.damage, 0, Projectile.owner);
            }
        }
    }
    public class ArbitratorStealthImpact : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 12;
            Projectile.width = Projectile.height = 32;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(Projectile.Center, 500 * Projectile.scale).Intersects(targetHitbox);
        }
        public List<List<Vector2>> arcs = new List<List<Vector2>>();
        public override void AI()
        {
            if(Projectile.Entropy().FirstFrames)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(in SoundID.NPCDeath56, Projectile.Center);
                    CEUtils.PlaySound("ApoctosisShoot", Main.rand.NextFloat(3f, 3.4f), Projectile.Center, 60, 0.95f);
                    for (int i = 0; i < 12; i++)
                    {
                        List<Vector2> points = new List<Vector2>();
                        float rot = CEUtils.randomRot();

                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, rot.ToRotationVector2() * Main.rand.NextFloat(24, 28), false, 8, 0.06f * Main.rand.NextFloat(0.9f, 1f), Main.rand.NextBool() ? Color.OrangeRed : Color.DarkOrange, new Vector2(2.4f, 0.6f), true));

                        Vector2 pos = Projectile.Center;
                        points.Add(pos);
                        for (float j = 0; j <= 1; j += 0.05f)
                        {
                            rot += Main.rand.NextFloat(-0.2f, 0.2f);
                            pos += rot.ToRotationVector2() * 25;
                            points.Add(pos);
                        }
                        arcs.Add(points);
                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Color color1 = Color.White;
            Color color2 = Color.Orange;
            float w = Projectile.timeLeft / 6f;
            if (w > 1)
                w = 1;
            for(int i = 0; i < arcs.Count; i++)
            {
                List<Vector2> arc = arcs[i];
                List<CEUtils.VertexPointSets> ve = new List<CEUtils.VertexPointSets>();
                for(int j = 0; j < arc.Count; j++)
                {
                    float maxl = Utils.Remap(Projectile.timeLeft, 6f, 12f, 1f, 0.11f);
                    float num = j / (arc.Count - 1f);
                    float p =  (maxl - (j / (float)arc.Count)) / maxl;
                    float pw = 1 - p;
                    pw = 1 - pw * pw;
                    if (num > maxl)
                        break;
                    ve.Add(new CEUtils.VertexPointSets(arc[j], Color.White, 36 * w * pw, 0));
                }
                ThalassianWaterBolt.DrawTrail(ve, color1, color2, CEUtils.getExtraTex("Streak1"), CEUtils.getExtraTex("Streak2"), false, 0.2f);
            }
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 32;
        }
    }
}