using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books
{
    public class TabooVolume : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 270;
            Item.useAnimation = Item.useTime = 100;
            Item.crit = 10;
            Item.mana = 42;
            Item.shootSpeed = 29;
            Item.rare = CECal.RarityCalamityRed(ModContent.RarityType<VoidPurple>());
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BookMark8")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<TabooVolumeHeld>();
        public override int SlotCount => 5;

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Heresy, CEID.Item_AshesofAnnihilation, CEID.Tile_DraedonsForge))
            {

                CreateRecipe().AddIngredient<BurntLostClassics>()
                .AddIngredient(CEID.Item_Heresy)
                .AddIngredient(CEID.Item_AshesofAnnihilation, 6)
                .AddTile(CEID.Tile_DraedonsForge)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient<ControlTerminal>()
                .AddIngredient<VoidOde>()
                .AddIngredient<FadingRunestone>()
                .AddTile(ModContent.TileType<Tiles.VoidWellTile>())
                .Register();
        }
    }

    public class TabooVolumeHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/TabooVolume/TabooVolumeOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/TabooVolume/TabooVolumePage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/TabooVolume/TabooVolumeUI";

        public float seekerRot = 0;
        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            // 2026-08-31 平衡案:去除吸血特性
            m.armorPenetration += 40;
            return m;
        }
        public override float randomShootRotMax => 0.01f;
        public bool SeekerShoot = false;
        public override int baseProjectileType => SeekerShoot ? ModContent.ProjectileType<BrimstoneHellblastFriendly>() : ModContent.ProjectileType<BrimstoneGigaBlastFriendly>();
        public override EBookProjectileEffect getEffect()
        {
            return new TabooVolumeBookBaseEffect();
        }
        public int seekerCd = 0;
        /// <summary>硫磺暴弹内置CD(3秒固定,不吃攻速;2026-08-31 平衡案)。</summary>
        public int gigaCd = 0;
        public override int frameChange => 3;
        public float seekerRotTarget = 0;
        public override void AI()
        {
            base.AI();
            if (gigaCd > 0)
                gigaCd--;
            seekerRot += (seekerRotTarget - seekerRot) * 0.1f;
            if (!active)
            {
                seekerRotTarget += 0.04f;
            }
            else
            {
                if (seekerCd-- <= 0)
                {
                    seekerCd = this.GetShootCd() / 6;
                    SeekerShoot = true;
                    this.Shoot();
                    SeekerShoot = false;
                }
            }
            if (Main.GameUpdateCount % 15 == 0 && active)
            {
                base.playTurnPageAnimation();
            }
        }
        public override void playTurnPageAnimation()
        {

        }
        public override bool CanShoot()
        {
            // 硫磺暴弹冷却期间不允许主动射击(顺带不耗蓝);索魂者射击不走此闸门
            return gigaCd <= 0 && base.CanShoot();
        }
        public override bool Shoot()
        {
            if (SeekerShoot)
            {
                seekerRotTarget += MathHelper.ToRadians(60);
                var seekers = getSeekerPos();
                Vector2 opos = Projectile.Center;
                float oRot = Projectile.rotation;
                Vector2 oVel = Projectile.velocity;
                foreach (var sp in seekers)
                {
                    Projectile.Center = sp;
                    Projectile.rotation = (Main.MouseWorld - Projectile.Center).ToRotation();
                    Projectile.velocity = Projectile.rotation.ToRotationVector2() * Projectile.velocity.Length();
                    base.Shoot();
                    //PRT_HadCircle2 scale2链式赋值,Configure返回this所以能点.scale2
                    PRTLoader.NewParticle<PRT_HadCircle2>(Projectile.Center, Vector2.Zero, Color.OrangeRed, 1)
                        .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0).scale2 = 0.4f;
                }
                Projectile.rotation = oRot;
                Projectile.Center = opos;
                Projectile.velocity = oVel;
                Projectile.localAI[0]++;
                return true;
            }
            gigaCd = 180;
            return base.Shoot();
        }
        //环绕灵魂索魂者贴图,加载期就位,不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Items/Books/SoulSeekerSupreme")]
        internal static Asset<Texture2D> SeekerTex;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = SeekerTex.Value;
            foreach (Vector2 pos in getSeekerPos())
            {
                Main.EntitySpriteDraw(tex, pos - Main.screenPosition, CEUtils.GetCutTexRect(tex, 6, ((int)Main.GameUpdateCount / 4) % 6, false), lightColor, 0, new Vector2(48, 65), Projectile.scale, (Projectile.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            }
            return base.PreDraw(ref lightColor);
        }
        public List<Vector2> getSeekerPos()
        {
            int dist = 120;
            List<Vector2> pos = new List<Vector2>();
            int count = 3;
            for (int i = 0; i < count; i++)
            {
                float rot = MathHelper.ToRadians(360f / count) * i + seekerRot;
                pos.Add(Projectile.Center + rot.ToRotationVector2() * dist);
            }
            return pos;
        }
    }
    public class TabooVolumeBookBaseEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 6 * 60);
        }
    }
}
