using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books
{
    public class LushGrassclassics : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 7;
            Item.shootSpeed = 24;
            Item.ArmorPenetration = 10;
            Item.mana = 8;
        }
        public override int HeldProjectileType => ModContent.ProjectileType<LushGrassclassicsHeld>();
        public override int SlotCount => 1;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.JungleSpores, 2)
                .AddIngredient(ItemID.Vine, 2)
                .AddIngredient(ItemID.Stinger, 3)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/LG")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
    }

    public class LushGrassclassicsHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/LushGrassclassics/lushGrassclassicsOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/LushGrassclassics/lushGrassclassicsPage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/LushGrassclassics/lushGrassclassicsUI";
        public override EBookProjectileEffect getEffect()
        {
            return new PoisionEBEffect();
        }
        public override int baseProjectileType => ModContent.ProjectileType<LushVine>();
    }
    public class PoisionEBEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BonePiercingToxin>(), 360);
        }
    }
    public class LushVine : EBookBaseProjectile
    {
        public List<Vector2> OldPos = new List<Vector2>();
        public List<float> OldRots = new List<float>();
        public int Counter = 0;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 10;
            Projectile.light = 0;
            Projectile.tileCollide = true;
            Projectile.width = Projectile.height = 8;
            Projectile.penetrate = 10;
        }
        public int Penet = 10;
        public List<int> hitedNPC = new();
        public override bool PreAI()
        {
            if (Projectile.penetrate > Penet)
            {
                Penet = Projectile.penetrate;
            }
            Projectile.penetrate = -1;
            return base.PreAI();
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Counter < 18)
            {
                Counter = 18;
            }
            SoundEngine.PlaySound(SoundID.Grass, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(Projectile.Center + Projectile.velocity, 1, 1, DustID.Grass);
            }
            Projectile.tileCollide = false;
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(target.Center, 1, 1, DustID.Grass);
            }
            if (!hitedNPC.Contains(target.whoAmI))
            {
                hitedNPC.Add(target.whoAmI);
            }
            if (hitedNPC.Count >= Penet)
            {
                if (Counter < 18)
                {
                    Counter = 18;
                }
            }

        }
        public override void AI()
        {
            base.AI();
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Counter++ < 18)
            {
                OldRots.Add(Projectile.rotation);
                OldPos.Add(Projectile.Center);
                Projectile.velocity = Projectile.velocity.RotatedByRandom(0.1f);
            }
            else
            {
                if (OldPos.Count > 0)
                {
                    Projectile.Center = OldPos[OldRots.Count - 1];
                    Projectile.rotation = OldRots[OldRots.Count - 1];
                    OldPos.RemoveAt(OldPos.Count - 1);
                    OldRots.RemoveAt(OldRots.Count - 1);
                }
                else
                {
                    Projectile.Kill();
                }
            }
            Projectile.Center += Projectile.velocity * Projectile.scale;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void ApplyHoming()
        {
            if (Counter < 18)
            {
                base.ApplyHoming();
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (OldPos.Count > 2)
            {
                for (int i = 1; i < OldPos.Count; i++)
                {
                    if (CEUtils.LineThroughRect(OldPos[i - 1], OldPos[i], targetHitbox, (int)(16 * Projectile.scale)))
                    { return true; }
                }
                if (CEUtils.LineThroughRect(OldPos[OldPos.Count - 1], OldPos[OldPos.Count - 1] + OldRots[OldRots.Count - 1].ToRotationVector2() * 32 * Projectile.scale, targetHitbox, (int)(16 * Projectile.scale)))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D vine = Projectile.GetTexture();
            Texture2D top = this.getTextureAlt();
            List<ColoredVertex> vertexs = new List<ColoredVertex>();
            float tc = 0;
            Vector2 lastPos = Projectile.Center;
            for (int i = 0; i < OldPos.Count; i++)
            {
                tc += CEUtils.getDistance(lastPos, OldPos[i]) / (24f * Projectile.scale);
                lastPos = OldPos[i];
                Color color = Lighting.GetColor((OldPos[i] / 16f).ToPoint());
                vertexs.Add(new ColoredVertex(OldPos[i] - Main.screenPosition + OldRots[i].ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 8 * Projectile.scale, new Vector3(tc, 0, 1), color));
                vertexs.Add(new ColoredVertex(OldPos[i] - Main.screenPosition + OldRots[i].ToRotationVector2().RotatedBy(MathHelper.PiOver2) * -8 * Projectile.scale, new Vector3(tc, 1, 1), color));
            }
            Color color_ = Lighting.GetColor((Projectile.Center / 16f).ToPoint());
            tc += CEUtils.getDistance(lastPos, Projectile.Center) / (24f * Projectile.scale);
            vertexs.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 8 * Projectile.scale, new Vector3(tc, 0, 1), color_));
            vertexs.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * -8 * Projectile.scale, new Vector3(tc, 1, 1), color_));

            if (vertexs.Count > 3)
            {
                Main.spriteBatch.UseSampleState(SamplerState.PointWrap);
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = vine;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertexs.ToArray(), 0, vertexs.Count - 2);
                Main.spriteBatch.ExitShaderRegion();
            }
            Main.spriteBatch.Draw(top, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(0, top.Height / 2f), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }

}
