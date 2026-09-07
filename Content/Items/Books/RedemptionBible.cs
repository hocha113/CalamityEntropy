using CalamityEntropy.Common;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.UI.EntropyBookUI;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityEntropy.Content.Items.Books
{
    public class RedemptionBible : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 68;
            Item.useAnimation = Item.useTime = 29;
            Item.crit = 10;
            Item.mana = 18;
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BookMark3")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<RedemptionBibleHeld>();
        public override int SlotCount => 2;

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<UpdraftTome>()
                .AddIngredient(ItemID.HallowedBar, 12)
                .AddIngredient(ItemID.SoulofLight, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class RedemptionBibleHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/RedemptionBible/RedemptionBibleOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/RedemptionBible/RedemptionBiblePage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/RedemptionBible/RedemptionBibleUI";


        public override float randomShootRotMax => 0;
        public override int baseProjectileType => ModContent.ProjectileType<RedemptionSpear>();
        public float getBowShootCd()
        {
            int _shotCooldown = bookItem.useTime;

            EBookStatModifer m = getBaseModifer();
            for (int i = 0; i < Math.Min(EBookUI.getMaxSlots(Main.LocalPlayer, bookItem), Projectile.GetOwner().Entropy().EBookStackItems.Count); i++)
            {
                Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(it))
                {
                    var e = BookMarkLoader.GetEffect(it);
                    BookMarkLoader.ModifyStat(it, m);
                }
            }
            return ((float)_shotCooldown / m.attackSpeed) * 0.38f;
        }
        public float getscale()
        {
            EBookStatModifer m = getBaseModifer();
            for (int i = 0; i < Math.Min(EBookUI.getMaxSlots(Main.LocalPlayer, bookItem), Projectile.GetOwner().Entropy().EBookStackItems.Count); i++)
            {
                Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(it))
                {
                    var e = BookMarkLoader.GetEffect(it);
                    BookMarkLoader.ModifyStat(it, m);
                }
            }
            return m.Size;
        }
        public override void AI()
        {
            CEUtils.AddLight(Projectile.Center, Color.LightGoldenrodYellow);
            base.AI();
            Player player = Projectile.GetOwner();

            if (Main.myPlayer == Projectile.owner)
            {
                int rb = ModContent.ProjectileType<RedemptionBow>();
                if (player.ownedProjectileCounts[rb] < player.ownedProjectileCounts[Projectile.type])
                {
                    ShootSingleProjectile(rb, Projectile.Center, Vector2.Zero);
                }
            }
        }
    }
    public class RedemptionBow : EBookBaseProjectile
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.netImportant = true;
        }
        public override void ApplyHoming()
        {
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.WriteVector2(mouse);
            writer.Write(Projectile.scale);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            mouse = reader.ReadVector2();
            Projectile.scale = reader.ReadSingle();
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public Vector2 mouse = Vector2.Zero;
        public int Delay = 0;
        public override void AI()
        {
            CEUtils.AddLight(Projectile.Center, Color.LightGoldenrodYellow);
            base.AI();
            if (Projectile.GetOwner().dead)
            {
                Projectile.Kill();
                return;
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (mouse != Main.MouseWorld)
                {
                    mouse = Main.MouseWorld;
                    Projectile.netUpdate = true;
                }
            }
            float dist = 1000;
            NPC target = null;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.friendly && !npc.dontTakeDamage)
                {
                    float d = CEUtils.getDistance(npc.Center, mouse);
                    if (d < dist)
                    {
                        target = npc;
                        dist = d;
                    }
                }
            }
            if (target != null)
            {
                if (CEUtils.getDistance(Projectile.Center, target.Center) > 300)
                {
                    Projectile.velocity += (target.Center - Projectile.Center).normalize() * 1f;
                    Projectile.velocity *= 0.985f;
                }
                else
                {
                    Projectile.velocity -= (target.Center - Projectile.Center).normalize() * 1f;
                    Projectile.velocity *= 0.985f;
                }
                float targetRot = (target.Center - Projectile.Center).ToRotation();
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.1f, false);
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.1f, true);
            }
            else
            {
                Projectile.velocity *= 0.9f;
                Vector2 tpos = Projectile.GetOwner().Center + new Vector2(-100 * Projectile.GetOwner().direction, -80);
                Projectile.Center += (tpos - Projectile.Center) * 0.07f;
                Projectile.rotation = (mouse - Projectile.Center).ToRotation();
            }
            if (this.ShooterModProjectile is RedemptionBibleHeld eb)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.scale = eb.getscale();
                    Projectile.ai[1] = Projectile.ai[0] / eb.getBowShootCd();
                }
                else
                {
                    Projectile.netUpdate = true;
                    if (Projectile.netSpam >= 10)
                    {
                        Projectile.netSpam = 9;
                    }
                }

                if (eb.active)
                {
                    if (Delay-- < 0)
                    {
                        Projectile.ai[0]++;
                        float shootCd = eb.getBowShootCd();
                        if (Projectile.ai[0] > shootCd)
                        {
                            Projectile.ai[0] -= shootCd;
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Delay = 8;
                                CEUtils.PlaySound("portal_emerge", Main.rand.NextFloat(2.2f, 2.4f), Projectile.Center, volume: 0.4f);
                                eb.ShootSingleProjectile(ModContent.ProjectileType<RedemptionArrow>(), Projectile.Center, Projectile.rotation.ToRotationVector2(), 0.24f, 1, 1.6f, MainProjectile: true);
                            }
                        }
                    }
                }
                else
                {
                    Projectile.ai[0] = 0;
                }
            }
            if (Projectile.GetOwner().ownedProjectileCounts[ModContent.ProjectileType<RedemptionBibleHeld>()] > 0)
            {
                Projectile.timeLeft = 3;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 top = Projectile.Center + new Vector2(-13, -28).RotatedBy(Projectile.rotation) * Projectile.scale;
            Vector2 but = Projectile.Center + new Vector2(-13, 28).RotatedBy(Projectile.rotation) * Projectile.scale;
            Vector2 mid = Projectile.Center + new Vector2(-13 - (MathHelper.Max(0, Projectile.ai[1] - 0.25f)) * 32, 0).RotatedBy(Projectile.rotation) * Projectile.scale;

            CEUtils.drawLine(top, mid, new Color(255, 255, 161), 2 * Projectile.scale, 2);
            CEUtils.drawLine(but, mid, new Color(255, 255, 161), 2 * Projectile.scale, 2);

            Main.EntitySpriteDraw(Projectile.getDrawData(Color.White));
            return false;
        }
    }
    public class RedemptionArrow : EBookBaseProjectile
    {
        public override Color baseColor => Color.Goldenrod;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = Projectile.height = 36;
            Projectile.timeLeft = 300;
        }
        public override void AI()
        {
            CEUtils.AddLight(Projectile.Center, color);
            base.AI();
            Projectile.rotation = Projectile.velocity.ToRotation();
            for (float i = 0; i < 1; i += 0.05f)
            {
                //PRT_GlowLightParticle拖尾,lightColor/AlphaShrink spawn后赋再Configure
                var p = PRTLoader.NewParticle<PRT_GlowLightParticle>(Projectile.Center + Projectile.velocity * Main.rand.NextFloat(), Vector2.Zero, color * 1f, 0.6f);
                p.lightColor = color * 0.12f;
                p.AlphaShrink = false;
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 8);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Main.EntitySpriteDraw(Projectile.getDrawData(Color.White));
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(1.4f, 1.8f);
                dust.velocity = (new Vector2(25, 25).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.6f, 1f);
                dust.noGravity = false;
                dust.color = Color.Goldenrod;
                dust.fadeIn = 2f;
            }
        }
    }
}
