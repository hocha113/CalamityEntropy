using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Rarities;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books
{
    public class AshTranscript : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 140;
            Item.useAnimation = Item.useTime = 25;
            Item.crit = 10;
            Item.mana = 20;
            Item.rare = CECal.RarityTurquoise(ModContent.RarityType<NihilityBlue>());
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BookMark5")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<AshTranscriptHeld>();
        public override int SlotCount => 4;

        // 2026-08-31 平衡案:改为拜月邪教徒50%直接掉落,原配方删除
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DivineGeode, CEID.Tile_ProfanedCrucible))
            {
                CreateRecipe().AddIngredient<NightEpic>()
                    .AddIngredient(CEID.Item_DivineGeode, 6)
                    .AddIngredient(ItemID.Ectoplasm, 6)
                    .AddTile(CEID.Tile_ProfanedCrucible)
                    .Register();
            }
        }
    }

    /// <summary>灰烬笔录掉落:拜月邪教徒 50%。装灾厄时改由上面那条 3.33 配方产出,这里让位</summary>
    public class AshTranscriptDropGNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (!CERef.Has && npc.type == NPCID.CultistBoss)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AshTranscript>(), 2));
            }
        }
    }

    public class AshTranscriptHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/AshTranscript/AshTranscriptOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/AshTranscript/AshTranscriptPage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/AshTranscript/AshTranscriptUI";

        public override float randomShootRotMax => 0.02f;
        public override int baseProjectileType => ModContent.ProjectileType<AshAncientLight>();

        public override int frameChange => 3;
        public override EBookProjectileEffect getEffect()
        {
            return new HolyFireDebuffEffect();
        }

        // 2026-08-31 平衡案:重做为发射5发扇形的远古光明妖
        public override bool Shoot()
        {
            int type = getShootProjectileType();
            for (int i = 0; i < Main.LocalPlayer.GetMyMaxActiveBookMarks(bookItem); i++)
            {
                var bm = Projectile.owner.ToPlayer().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(bm))
                {
                    int pn = BookMarkLoader.ModifyProjectile(bm, type);
                    if (pn >= 0)
                    {
                        type = pn;
                    }
                }
            }
            for (int i = -2; i <= 2; i++)
            {
                ShootSingleProjectile(type, Projectile.Center, Projectile.velocity.RotatedBy(i * 0.14f), MainProjectile: true);
            }
            return true;
        }
    }

    public class HolyFireDebuffEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 600);
        }
    }

    /// <summary>友方"远古光明妖":摇曳飞行+轻微追踪,拖曳远古光明尘。</summary>
    public class AshAncientLight : EBookBaseProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override Color baseColor => new Color(255, 230, 160);
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 1;
            homing = 0.9f;
        }
        public override void AI()
        {
            base.AI();
            // 光明妖式摇曳
            Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sin(Projectile.timeLeft * 0.11f) * 0.02f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.8f, 0.7f, 0.4f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, -Projectile.velocity * 0.2f);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1f, 1.5f);
            }
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, CEUtils.randomVec(4));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.2f, 1.8f);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glow = CEExtraAssets.lightball;
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < Projectile.oldPos.Length && i < 6; i++)
            {
                float fade = 1f - i / 6f;
                Vector2 pos = (i == 0 ? Projectile.Center : Projectile.oldPos[i] + Projectile.Size / 2f);
                Main.spriteBatch.Draw(glow, pos - Main.screenPosition, null, color * 0.5f * fade, 0, glow.Size() / 2f, 0.24f * fade * Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.White * 0.9f, 0, glow.Size() / 2f, 0.13f * Projectile.scale, SpriteEffects.None, 0);
            CEUtils.ReSetToEndShader();
            return false;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }
    }
}
