using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.ChurchDefender
{
    public class ChurchDefender : ModItem
    {
        public const int BaseDamage = 160;          //任务面板
        public const int UseFrames = 30;            //帧/发,每秒2发
        public const float ShootSpeed = 14f;        //像素/帧
        public const float Knockback = 4f;
        public const float MuzzleLength = 36f;      //像素,握点到炮口
        public const int SunlightCost = 10;         //配方烈阳之光
        public const int SellGold = 25;

        public override void SetStaticDefaults() {
            AmmoID.Sets.SpecificLauncherAmmoProjectileFallback[Type] = ItemID.GrenadeLauncher;
        }

        public override void SetDefaults() {
            Item.width = 58;
            Item.height = 32;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.UseSound = SoundID.Item61 with { Pitch = -0.3f, PitchVariance = 0.1f };
            Item.shoot = ModContent.ProjectileType<ChurchDefenderBomb>();
            Item.shootSpeed = ShootSpeed;
            Item.useAmmo = AmmoID.Rocket;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override void HoldItem(Player player) {
            player.CheckAndSpawnHeldProj(ModContent.ProjectileType<ChurchDefenderHeld>());
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Vector2 muzzle = position + velocity.SafeNormalize(Vector2.UnitX) * MuzzleLength;
            Projectile.NewProjectile(source, muzzle, velocity, ModContent.ProjectileType<ChurchDefenderBomb>(), damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.GrenadeLauncher)
                .AddIngredient<BlazingSunlight>(SunlightCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class ChurchDefenderHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Aftermath/ChurchDefender/ChurchDefender";

        public static readonly Vector2 GripOrigin = new(22f, 13f);   //像素,贴图握把与炮管轴线
        public const float MuzzleDistance = 36f;    //像素,握点到炮口
        public const float RearDistance = 20f;      //像素,握点到尾喷口
        public const float RecoilKick = 9f;         //像素
        public const float RecoilDecay = 0.8f;      //每帧
        public const float LiftKick = 0.2f;         //弧度,炮口上扬
        public const float LiftDecay = 0.84f;       //每帧
        public const float ArmDrop = 0.6f;          //手臂下压,同轻机枪
        public const float FireShake = 2.5f;        //比开拓者炮轻
        public const int SmokeCount = 4;            //炮口与尾喷各此数
        public const int SparkCount = 6;

        private float recoil;
        private float lift;

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 3;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Player player = Projectile.GetOwner();
            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<ChurchDefender>()) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 3;
            Projectile.Center = player.GetDrawCenter();
            Vector2 aim = (player.mouseWorld() - Projectile.Center).SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.rotation = aim.ToRotation();
            Projectile.velocity = aim;
            if (player.ItemAnimationJustStarted)
                FireEffects(aim);
            recoil *= RecoilDecay;
            lift *= LiftDecay;
            player.heldProj = Projectile.whoAmI;
            player.SetHandRot((aim + new Vector2(0f, ArmDrop)).ToRotation());
        }

        private void FireEffects(Vector2 aim) {
            recoil = RecoilKick;
            lift = LiftKick;
            if (Main.dedServ)
                return;
            Vector2 muzzle = Projectile.Center + aim * MuzzleDistance;
            Vector2 rear = Projectile.Center - aim * RearDistance;
            CEUtils.SetShake(Projectile.Center, FireShake);
            PRTLoader.NewParticle<PRT_ImpactParticle>(muzzle, Vector2.Zero, new Color(255, 220, 150), 0.08f)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation);
            for (int i = 0; i < SmokeCount; i++) {
                Color smoke = Color.Lerp(Color.Gray, Color.LightGray, Main.rand.NextFloat()) * 0.6f;
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(muzzle, aim.RotatedByRandom(0.5f) * Main.rand.NextFloat(3f, 7f), smoke, 0.45f)
                    .Configure(1f, 34, 0.03f, false, 0f, false, true);
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(rear, (-aim).RotatedByRandom(0.6f) * Main.rand.NextFloat(2f, 5f), smoke, 0.35f)
                    .Configure(0.8f, 28, 0.03f, false, 0f, false, true);
            }
            for (int i = 0; i < SparkCount; i++) {
                Color spark = Color.Lerp(Color.Goldenrod, Color.Orange, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_LineCal>(muzzle, aim.RotateRandom(0.35f) * Main.rand.NextFloat(8f, 18f), spark, Main.rand.NextFloat(0.5f, 0.9f))
                    .Configure(false, Main.rand.Next(6, 10));
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            bool left = Projectile.velocity.X < 0f;
            int dir = left ? -1 : 1;
            Vector2 origin = new(GripOrigin.X, left ? tex.Height - GripOrigin.Y : GripOrigin.Y);
            Vector2 pos = Projectile.Center - Projectile.velocity * recoil - Main.screenPosition;
            Main.EntitySpriteDraw(tex, pos, null, lightColor, Projectile.rotation - dir * lift, origin, Projectile.scale, left ? SpriteEffects.FlipVertically : SpriteEffects.None);
            return false;
        }
    }

    public class ChurchDefenderBomb : ModProjectile
    {
        public const int FlightFrames = 120;        //未粘住约2秒自爆
        public const int StickFrames = 480;         //粘住后8秒爆炸
        public const int MaxPerTarget = 10;         //每目标上限
        public const int DefensePerBomb = 5;        //每枚降防
        public const int FlareBurnFrames = 180;     //耀斑灼烧3秒
        public const int FlareRefreshFrames = 60;   //粘附期间每秒刷新
        public const int ExplosionRadius = 80;      //像素,5格
        public const float ExplosionMult = 2f;      //爆炸200%
        public const float ExplosionShake = 3f;
        public const float OffsetScale = 2f;        //中心=目标中心-速度×此值
        public const float BackOutStep = 2f;        //像素,退出命中框
        public const int BackOutSteps = 12;
        public const float HeadOffset = 10f;        //像素,弹头离中心
        public const float HeadGlowSize = 26f;      //像素
        public const int BlastDust = 18;
        public const int BlastSmoke = 5;
        public const int BlastSparks = 8;
        public const int TrailLength = 6;

        //ai[0]=1 已粘附
        //ai[1] 粘附的NPC
        //粘附时velocity存偏移
        private bool Stuck => Projectile.ai[0] == 1f;
        private bool exploding;
        private float blink;

        private float Pulse => Stuck ? 0.5f + 0.5f * MathF.Sin(blink) : 0.8f;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = FlightFrames;
        }

        public override bool ShouldUpdatePosition() => !Stuck;

        public override bool? CanDamage() => Stuck && !exploding ? false : null;

        public override bool CanHitPvp(Player target) => false;

        public override void AI() {
            Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.25f, 0.08f) * Pulse);
            if (!Stuck) {
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (!Main.dedServ && Main.rand.NextBool(3)) {
                    Dust d = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, DustID.SolarFlare, -Projectile.velocity * 0.1f, 100, default, 0.9f);
                    d.noGravity = true;
                }
                return;
            }
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = StickFrames;
            }
            NPC npc = Main.npc[(int)Projectile.ai[1]];
            if (!npc.active || npc.life <= 0) {
                Projectile.Kill();
                return;
            }
            Projectile.Center = npc.Center - Projectile.velocity * OffsetScale;
            Projectile.rotation = Projectile.velocity.ToRotation();
            npc.GetGlobalNPC<ChurchDefenderGlobalNPC>().BombAccum++;
            float urgency = 1f - Projectile.timeLeft / (float)StickFrames;
            blink += MathHelper.Lerp(0.06f, 0.5f, urgency * urgency);
            if (Main.myPlayer == Projectile.owner && ++Projectile.localAI[1] >= FlareRefreshFrames) {
                Projectile.localAI[1] = 0f;
                npc.AddBuff(ModContent.BuffType<FlareBurn>(), FlareBurnFrames);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (exploding || Stuck)
                return;
            target.AddBuff(ModContent.BuffType<FlareBurn>(), FlareBurnFrames);
            Vector2 back = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < BackOutSteps && target.Hitbox.Contains(Projectile.Center.ToPoint()); i++)
                Projectile.Center -= back * BackOutStep;
            Projectile.ai[0] = 1f;
            Projectile.ai[1] = target.whoAmI;
            Projectile.velocity = (target.Center - Projectile.Center) / OffsetScale;
            Projectile.localAI[0] = 1f;
            Projectile.timeLeft = StickFrames;
            Projectile.netUpdate = true;
            KillOldest(target.whoAmI);
        }

        private void KillOldest(int npcIndex) {
            int count = 0;
            Projectile oldest = null;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.whoAmI == Projectile.whoAmI || p.type != Type || p.owner != Projectile.owner || p.ai[0] != 1f || (int)p.ai[1] != npcIndex)
                    continue;
                count++;
                if (oldest == null || p.timeLeft < oldest.timeLeft)
                    oldest = p;
            }
            if (count >= MaxPerTarget)
                oldest.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!exploding)
                return null;
            return targetHitbox.ClosestPointInRect(Projectile.Center).Distance(Projectile.Center) <= ExplosionRadius;
        }

        public override void OnKill(int timeLeft) {
            BlastEffects();
            if (Main.myPlayer != Projectile.owner)
                return;
            exploding = true;
            for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                Projectile.localNPCImmunity[i] = 0;
            Projectile.Resize(ExplosionRadius * 2, ExplosionRadius * 2);
            Projectile.damage = (int)(Projectile.damage * ExplosionMult);
            Projectile.Damage();
        }

        private void BlastEffects() {
            if (Main.dedServ)
                return;
            Vector2 c = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, PitchVariance = 0.2f, MaxInstances = 4 }, c);
            CEUtils.SetShake(c, ExplosionShake);
            PRTLoader.NewParticle<PRT_ShineParticle>(c, Vector2.Zero, new Color(255, 200, 110), ExplosionRadius * 2.4f / 256f)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 16);
            PRTLoader.NewParticle<PRT_PulseRing>(c, Vector2.Zero, new Color(255, 150, 50), 0.2f)
                .Configure(ExplosionRadius * 2f / 156f, 18);
            for (int i = 0; i < BlastDust; i++) {
                Dust d = Dust.NewDustPerfect(c, DustID.SolarFlare, CEUtils.randomPointInCircle(9f), 0, default, Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
            }
            for (int i = 0; i < BlastSmoke; i++) {
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(c + CEUtils.randomPointInCircle(24f), CEUtils.randomPointInCircle(4f), Color.Gray * 0.5f, 0.6f)
                    .Configure(1f, 40, 0.02f, false, 0f, false, true);
            }
            for (int i = 0; i < BlastSparks; i++) {
                Color spark = Color.Lerp(Color.Gold, Color.OrangeRed, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_LineCal>(c, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(10f, 20f), spark, Main.rand.NextFloat(0.6f, 1f))
                    .Configure(false, Main.rand.Next(8, 13));
            }
            Lighting.AddLight(c, 1.2f, 0.7f, 0.25f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = tex.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color glow = new(255, 150, 60, 0);
            if (!Stuck) {
                for (int i = 1; i < Projectile.oldPos.Length; i++) {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float k = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Main.EntitySpriteDraw(tex, at, null, glow * (0.5f * k), Projectile.rotation, origin, Projectile.scale * (0.6f + 0.4f * k), SpriteEffects.None);
                }
            }
            Main.EntitySpriteDraw(tex, pos, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Texture2D bloom = CEExtraAssets.Glow2;
            Vector2 head = pos + Projectile.rotation.ToRotationVector2() * HeadOffset;
            Main.EntitySpriteDraw(bloom, head, null, glow * (0.35f + 0.65f * Pulse), 0f, bloom.Size() / 2f, HeadGlowSize / bloom.Width, SpriteEffects.None);
            return false;
        }
    }

    public class ChurchDefenderGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        //上一帧粘附的炸弹数,弹幕每帧累加
        public int BombCount;
        public int BombAccum;

        public override void ResetEffects(NPC npc) {
            BombCount = BombAccum;
            BombAccum = 0;
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) {
            if (BombCount > 0)
                modifiers.Defense.Flat -= ChurchDefenderBomb.DefensePerBomb * Math.Min(BombCount, ChurchDefenderBomb.MaxPerTarget);
        }
    }
}
