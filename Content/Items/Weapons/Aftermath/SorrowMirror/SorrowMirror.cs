using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.SorrowMirror
{
    public class SorrowMirror : ModItem
    {
        public const int BaseDamage = 105;          //任务面板
        public const int UseFrames = 20;            //帧/次,每秒3次
        public const int ManaCost = 14;
        public const float ShootSpeed = 10f;
        public const float Knockback = 4f;
        public const float MirrorDistance = 48f;    //像素,镜子离玩家3格
        public const int EssenceCost = 12;          //配方怨灵精华
        public const int SellGold = 25;

        public const int ModeFlare = 0;             //月耀
        public const int ModeTyphoon = 1;           //利刃台风
        public const int ModeStar = 2;              //星星吉他
        public const int ModeWrath = 3;             //双足翼龙怒气
        public const int ModeBoulder = 4;           //巨石法杖
        public const int ModeCount = 5;

        public const int FlareCount = 6;
        public const float FlareMult = 0.75f;       //全中折合4.5倍
        public const float FlareSpeed = 5f;         //像素/更新,每帧6更新
        public const float FlareSpreadX = 160f;     //像素,10格
        public const float FlareHeight = 640f;      //像素,40格
        public const float FlareStagger = 60f;      //像素,逐发抬高
        public const float FlareAimJitter = 24f;    //像素
        public const int TyphoonCount = 3;
        public const float TyphoonMult = 0.5f;      //每个最多3段
        public const float TyphoonSpeed = 6f;       //像素/更新,同原版
        public const float TyphoonSpread = 0.25f;   //弧度
        public const int StarCount = 5;
        public const float StarMult = 0.85f;
        public const float StarSearch = 700f;       //像素,同原版
        public const float StarLead = 20f;          //帧,预判目标位移
        public const float StarMissRadius = 24f;    //像素
        public const float StarArc = 0.8f;          //控制向量长/距离
        public const float StarMinArc = 64f;        //像素
        public const int WrathCount = 3;
        public const float WrathMult = 1f;
        public const float WrathSpeed = 14f;        //像素/更新,同原版
        public const float WrathSpread = 0.05f;     //弧度,约3度
        public const float WrathSpeedStep = 0.08f;  //逐发减速排成一串
        public const int BoulderCount = 2;
        public const float BoulderMult = 1.5f;
        public const float BoulderSpeed = 12f;      //像素/更新,同原版
        public const float BoulderSpread = 0.07f;   //弧度,约4度

        public static readonly int[] ModeIcons = { ItemID.LunarFlareBook, ItemID.RazorbladeTyphoon, ItemID.SparkleGuitar, ItemID.ApprenticeStaffT3, ItemID.StaffofEarth };
        public static readonly Color[] ModeColors = { new(150, 235, 255), new(110, 190, 255), new(200, 225, 255), new(130, 160, 255), new(185, 205, 230) };
        public static readonly SoundStyle[] ModeSounds = { SoundID.Item88, SoundID.Item84, SoundID.Item9, SoundID.DD2_BetsysWrathShot, SoundID.Item69 };

        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.knockBack = Knockback;
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<SorrowMirrorHeld>();
            Item.shootSpeed = ShootSpeed;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            SorrowMirrorPlayer mp = player.GetModPlayer<SorrowMirrorPlayer>();
            int mode = mp.Mode;
            mp.Mode = (mode + 1) % ModeCount;
            Vector2 mouse = Main.MouseWorld;
            Projectile mirror = FindMirror(player, type);
            if (mirror == null)
                mirror = Projectile.NewProjectileDirect(source, SorrowMirrorHeld.RestPosition(player, mouse), Vector2.Zero, type, 0, 0f, player.whoAmI, mode, 1f);
            else {
                mirror.ai[0] = mode;
                mirror.ai[1]++;
                mirror.ai[2] = 0f;
                mirror.netUpdate = true;
            }
            Vector2 from = mirror.Center;
            Vector2 aim = (mouse - from).SafeNormalize(Vector2.UnitX * player.direction);
            switch (mode) {
                case ModeFlare:
                    ShootFlares(player, source, mouse, (int)(damage * FlareMult), knockback);
                    break;
                case ModeTyphoon:
                    ShootFan(player, source, from, aim, ModContent.ProjectileType<SorrowMirrorTyphoon>(), TyphoonCount, TyphoonSpread, TyphoonSpeed, 0f, (int)(damage * TyphoonMult), knockback);
                    break;
                case ModeStar:
                    ShootStars(player, source, from, mouse, (int)(damage * StarMult), knockback);
                    break;
                case ModeWrath:
                    ShootFan(player, source, from, aim, ModContent.ProjectileType<SorrowMirrorWrath>(), WrathCount, WrathSpread, WrathSpeed, WrathSpeedStep, (int)(damage * WrathMult), knockback);
                    break;
                default:
                    ShootFan(player, source, from, aim, ModContent.ProjectileType<SorrowMirrorBoulder>(), BoulderCount, BoulderSpread, BoulderSpeed, 0f, (int)(damage * BoulderMult), knockback);
                    break;
            }
            return false;
        }

        private static Projectile FindMirror(Player player, int type) {
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == type && p.owner == player.whoAmI)
                    return p;
            }
            return null;
        }

        private static void ShootFan(Player player, IEntitySource source, Vector2 from, Vector2 aim, int type, int count, float spread, float speed, float speedStep, int damage, float knockback) {
            for (int i = 0; i < count; i++) {
                Vector2 v = aim.RotatedBy((i - (count - 1) / 2f) * spread) * speed * (1f - speedStep * i);
                Projectile.NewProjectile(source, from, v, type, damage, knockback, player.whoAmI);
            }
        }

        private static void ShootFlares(Player player, IEntitySource source, Vector2 mouse, int damage, float knockback) {
            int type = ModContent.ProjectileType<SorrowMirrorFlare>();
            float top = Math.Min(player.Center.Y, mouse.Y) - FlareHeight;
            for (int i = 0; i < FlareCount; i++) {
                Vector2 spawn = new(mouse.X + Main.rand.NextFloat(-FlareSpreadX, FlareSpreadX), top - FlareStagger * i);
                Vector2 aimAt = mouse + Main.rand.NextVector2Circular(FlareAimJitter, FlareAimJitter);
                Vector2 v = (aimAt - spawn).SafeNormalize(Vector2.UnitY) * FlareSpeed;
                Projectile.NewProjectile(source, spawn, v, type, damage, knockback, player.whoAmI, 0f, mouse.Y);
            }
        }

        private static void ShootStars(Player player, IEntitySource source, Vector2 from, Vector2 mouse, int damage, float knockback) {
            int type = ModContent.ProjectileType<SorrowMirrorStar>();
            List<NPC> pool = new();
            foreach (NPC n in Main.ActiveNPCs) {
                if (n.CanBeChasedBy() && n.Distance(mouse) < StarSearch)
                    pool.Add(n);
            }
            for (int i = 0; i < StarCount; i++) {
                Vector2 goal;
                if (pool.Count > 0) {
                    NPC target = pool[Main.rand.Next(pool.Count)];
                    goal = target.Center + target.velocity * StarLead;
                }
                else
                    goal = mouse + Main.rand.NextVector2Circular(StarMissRadius, StarMissRadius);
                float arc = Math.Max(from.Distance(goal), StarMinArc) * StarArc;
                Vector2 control = new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(0.3f, 1f)).SafeNormalize(-Vector2.UnitY) * arc;
                Projectile.NewProjectile(source, from, control, type, damage, knockback, player.whoAmI, goal.X, goal.Y);
            }
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.MagicMirror)
                .AddIngredient<WraithSoulEssence>(EssenceCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class SorrowMirrorPlayer : ModPlayer
    {
        //下一次攻击的模式
        public int Mode;
    }

    public class SorrowMirrorHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Aftermath/SorrowMirror/SorrowMirror";

        public const float FollowLerp = 0.3f;       //每帧追随比例
        public const float BobAmplitude = 4f;       //像素
        public const float BobSpeed = 0.06f;        //弧度/帧
        public const float TiltMax = 0.12f;         //弧度
        public const float TiltLerp = 0.2f;
        public const float FadeIn = 0.12f;          //每帧
        public const float FadeOut = 0.07f;
        public const int FlashFrames = 18;
        public const float IconBox = 20f;           //像素,镜中剪影最长边
        public static readonly Vector2 GlassOffset = new(0f, -3f);   //像素,镜面中心偏移
        public const float BackGlowSize = 90f;      //像素
        public const float FlashRing = 0.55f;       //脉冲环终值缩放
        public const int FlashDust = 6;

        //ai[0] 当前模式
        //ai[1] 攻击计数,变即闪光
        //ai[2]=1 淡出
        private float opacity;
        private float bob;
        private float seenCount;
        private int flashTimer;

        private int Mode => Math.Clamp((int)Projectile.ai[0], 0, SorrowMirror.ModeCount - 1);

        private Color ModeColor => SorrowMirror.ModeColors[Mode];

        public static Vector2 RestPosition(Player player, Vector2 mouse) {
            Vector2 center = player.GetDrawCenter();
            return center + (mouse - center).SafeNormalize(Vector2.UnitX * player.direction) * SorrowMirror.MirrorDistance;
        }

        public override void SetDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
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
            if (!player.active || player.dead) {
                Projectile.Kill();
                return;
            }
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.Center = RestPosition(player, player.mouseWorld());
                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item6 with { Volume = 0.5f }, Projectile.Center);
            }
            if (Main.myPlayer == Projectile.owner && Projectile.ai[2] == 0f) {
                bool inUse = player.HeldItem.type == ModContent.ItemType<SorrowMirror>() && (player.itemAnimation > 0 || player.controlUseItem) && !player.CCed && !player.noItems;
                if (!inUse) {
                    Projectile.ai[2] = 1f;
                    Projectile.netUpdate = true;
                }
            }
            bool fading = Projectile.ai[2] != 0f;
            opacity = MathHelper.Clamp(opacity + (fading ? -FadeOut : FadeIn), 0f, 1f);
            if (fading && opacity <= 0f) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 3;
            bob += BobSpeed;
            Vector2 rest = RestPosition(player, player.mouseWorld()) + new Vector2(0f, MathF.Sin(bob) * BobAmplitude);
            Projectile.Center = Vector2.Lerp(Projectile.Center, rest, FollowLerp);
            Vector2 toMirror = Projectile.Center - player.GetDrawCenter();
            float tilt = MathHelper.Clamp(toMirror.X / SorrowMirror.MirrorDistance, -1f, 1f) * TiltMax;
            Projectile.rotation = MathHelper.Lerp(Projectile.rotation, tilt, TiltLerp);
            if (!fading)
                player.SetHandRot(toMirror.ToRotation());
            if (Projectile.ai[1] != seenCount) {
                seenCount = Projectile.ai[1];
                flashTimer = FlashFrames;
                Flash();
            }
            if (flashTimer > 0)
                flashTimer--;
            Lighting.AddLight(Projectile.Center, ModeColor.ToVector3() * ((0.3f + 0.5f * flashTimer / FlashFrames) * opacity));
        }

        private void Flash() {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SorrowMirror.ModeSounds[Mode] with { Volume = 0.8f, MaxInstances = 3 }, Projectile.Center);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, ModeColor, 0.15f)
                .Configure(FlashRing, 16);
            for (int i = 0; i < FlashDust; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 18f), DustID.MagicMirror, Main.rand.NextVector2Circular(1.5f, 1.5f), 150, default, 1.1f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float flash = flashTimer / (float)FlashFrames;
            Color glow = Additive(ModeColor);
            Texture2D bloom = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(bloom, pos, null, glow * ((0.25f + 0.45f * flash) * opacity), 0f, bloom.Size() / 2f, BackGlowSize / bloom.Width, SpriteEffects.None);
            Color body = Color.Lerp(lightColor, Color.White, 0.6f) * opacity;
            Main.EntitySpriteDraw(tex, pos, null, body, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            if (flash <= 0f)
                return false;
            Texture2D glass = this.getTextureAlt("Glass");
            Main.EntitySpriteDraw(glass, pos, null, glow * (0.8f * flash * opacity), Projectile.rotation, glass.Size() / 2f, Projectile.scale, SpriteEffects.None);
            int id = SorrowMirror.ModeIcons[Mode];
            Main.instance.LoadItem(id);
            Texture2D icon = TextureAssets.Item[id].Value;
            Rectangle frame = Main.itemAnimations[id] != null ? Main.itemAnimations[id].GetFrame(icon) : icon.Frame();
            float s = IconBox / Math.Max(frame.Width, frame.Height);
            Vector2 at = pos + GlassOffset.RotatedBy(Projectile.rotation);
            Main.EntitySpriteDraw(icon, at, frame, new Color(255, 255, 255, 0) * (flash * opacity), Projectile.rotation, frame.Size() / 2f, s, SpriteEffects.None);
            return false;
        }

        private static Color Additive(Color c) => new(c.R, c.G, c.B, 0);
    }

    public class SorrowMirrorFlare : ModProjectile
    {
        public const int Frames = 7;                //原版月耀爆炸7帧
        public const int FrameUpdates = 18;         //更新/帧,同原版
        public const int ExtraUpdates = 5;          //同原版
        public const int LifeUpdates = 600;         //约100帧
        public const int BlastSize = 140;           //像素,同原版
        public const float FuseRadius = 60f;        //像素,近炸
        public const float HeadScale = 0.35f;       //飞行取首帧缩小
        public const float TrailGlowSize = 34f;     //像素
        public const float BlastGlowSize = 190f;    //像素
        public const int TrailLength = 18;          //更新数,约3帧
        public const int BlastDust = 10;

        //ai[0]=1 爆炸中
        //ai[1] 鼠标高度,过后才撞墙
        private bool Blasting => Projectile.ai[0] != 0f;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifeUpdates;
        }

        public override void AI() {
            if (!Blasting) {
                if (Projectile.Center.Y > Projectile.ai[1])
                    Projectile.tileCollide = true;
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (Main.myPlayer == Projectile.owner && NearTarget())
                    Detonate();
                if (!Main.dedServ && Main.rand.NextBool(4)) {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex, -Projectile.velocity * 0.2f, 0, default, 0.9f);
                    d.noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, 0.15f, 0.35f, 0.5f);
                return;
            }
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                BlastEffects();
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.6f, 0.9f);
            if (++Projectile.frameCounter >= FrameUpdates) {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Frames)
                    Projectile.Kill();
            }
        }

        private bool NearTarget() {
            foreach (NPC n in Main.ActiveNPCs) {
                if (n.CanBeChasedBy(Projectile) && n.Hitbox.ClosestPointInRect(Projectile.Center).Distance(Projectile.Center) < FuseRadius && Collision.CanHitLine(Projectile.Center, 1, 1, n.Center, 1, 1))
                    return true;
            }
            return false;
        }

        private void Detonate() {
            Projectile.ai[0] = 1f;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Blasting)
                return null;
            return targetHitbox.Intersects(Utils.CenteredRectangle(Projectile.Center, new Vector2(BlastSize)));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (!Blasting)
                Detonate();
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Detonate();
            return false;
        }

        private void BlastEffects() {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.35f, Pitch = 0.4f, MaxInstances = 3 }, Projectile.Center);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, new Color(120, 210, 255), 0.2f)
                .Configure(BlastSize / 156f, 16);
            for (int i = 0; i < BlastDust; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex, CEUtils.randomPointInCircle(6f), 0, default, 1.2f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D bloom = CEExtraAssets.Glow2;
            int fh = tex.Height / Frames;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            if (!Blasting) {
                for (int i = 0; i < Projectile.oldPos.Length; i += 2) {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float k = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Main.EntitySpriteDraw(bloom, at, null, new Color(110, 190, 255, 0) * (0.5f * k), 0f, bloom.Size() / 2f, TrailGlowSize * k / bloom.Width, SpriteEffects.None);
                }
                Rectangle head = new(0, 0, tex.Width, fh);
                Main.EntitySpriteDraw(tex, pos, head, Color.White, Projectile.rotation, head.Size() / 2f, HeadScale, SpriteEffects.None);
                return false;
            }
            Rectangle frame = new(0, fh * Projectile.frame, tex.Width, fh);
            float life = 1f - Projectile.frame / (float)Frames;
            Main.EntitySpriteDraw(bloom, pos, null, new Color(120, 200, 255, 0) * (0.5f * life), 0f, bloom.Size() / 2f, BlastGlowSize / bloom.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, frame, Color.White * 0.9f, 0f, frame.Size() / 2f, 1f, SpriteEffects.None);
            return false;
        }
    }

    public class SorrowMirrorTyphoon : ModProjectile
    {
        public const int Frames = 3;
        public const int FrameUpdates = 3;          //更新/帧
        public const int ExtraUpdates = 2;          //同原版
        public const int LifeUpdates = 300;         //约100帧
        public const int FadeUpdates = 30;
        public const int Hits = 3;                  //每个最多3段
        public const int HitCooldown = 30;          //更新,约10帧
        public const float HomingRange = 500f;      //像素,同原版
        public const float HomingTurn = 0.05f;      //弧度/更新
        public const float SpeedGain = 0.02f;       //像素/更新²
        public const float MaxSpeed = 8f;           //像素/更新
        public const float SpinSpeed = 0.12f;       //弧度/更新
        public const int TrailLength = 10;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = Hits;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifeUpdates;
        }

        public override void AI() {
            Projectile.rotation += SpinSpeed * (Projectile.velocity.X < 0f ? -1f : 1f);
            if (++Projectile.frameCounter >= FrameUpdates) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Frames;
            }
            float speed = Math.Min(Projectile.velocity.Length() + SpeedGain, MaxSpeed);
            float rot = Projectile.velocity.ToRotation();
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange);
            if (target != null)
                rot = CEUtils.RotateTowardsAngle(rot, (target.Center - Projectile.Center).ToRotation(), HomingTurn);
            Projectile.velocity = rot.ToRotationVector2() * speed;
            if (!Main.dedServ && Main.rand.NextBool(6)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.RazorbladeTyphoon, Projectile.velocity * 0.3f, 100, default, 1f);
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.1f, 0.25f, 0.45f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            int fh = tex.Height / Frames;
            Rectangle frame = new(0, fh * Projectile.frame, tex.Width, fh);
            Vector2 origin = frame.Size() / 2f;
            float fade = Utils.GetLerpValue(0f, FadeUpdates, Projectile.timeLeft, true);
            for (int i = Projectile.oldPos.Length - 1; i > 0; i -= 2) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float k = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, at, frame, new Color(120, 200, 255, 0) * (0.45f * k * fade), Projectile.rotation - i * SpinSpeed, origin, Projectile.scale * (0.6f + 0.4f * k), SpriteEffects.None);
            }
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * (0.85f * fade), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    public class SorrowMirrorStar : ModProjectile
    {
        public const int FlightUpdates = 90;        //同原版,约30帧
        public const int ExtraUpdates = 2;
        public const int BlastSize = 96;            //像素,6格
        public const float SpinPerUpdate = 0.12f;   //弧度
        public const float GlowSize = 54f;          //像素
        public const int TrailLength = 14;
        public const int BlastDust = 10;

        //ai[0]/ai[1] 落点
        //velocity存弧线控制向量
        private Vector2 start;
        private bool exploding;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = FlightUpdates * 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => exploding ? null : false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                start = Projectile.Center;
            }
            float t = Math.Min(++Projectile.localAI[1] / FlightUpdates, 1f);
            Vector2 goal = new(Projectile.ai[0], Projectile.ai[1]);
            Vector2 control = start + Projectile.velocity;
            float u = 1f - t;
            Projectile.Center = u * u * start + 2f * u * t * control + t * t * goal;
            Projectile.rotation += SpinPerUpdate;
            Lighting.AddLight(Projectile.Center, 0.25f, 0.35f, 0.55f);
            if (!Main.dedServ && Main.rand.NextBool(8)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.MagicMirror, Vector2.Zero, 150, default, 0.9f);
                d.noGravity = true;
            }
            if (t >= 1f)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft) {
            BlastEffects();
            if (Main.myPlayer != Projectile.owner)
                return;
            exploding = true;
            Projectile.Resize(BlastSize, BlastSize);
            Projectile.Damage();
        }

        private void BlastEffects() {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.5f, Pitch = 0.2f, MaxInstances = 4 }, Projectile.Center);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, new Color(170, 210, 255), 0.15f)
                .Configure(BlastSize / 156f, 14);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(150, 200, 255), BlastSize * 1.5f / 256f)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 14);
            for (int i = 0; i < BlastDust; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, CEUtils.randomPointInCircle(5f), 0, default, 1.3f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D bloom = CEExtraAssets.Glow2;
            Vector2 origin = tex.Size() / 2f;
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float k = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, at, null, new Color(150, 200, 255, 0) * (0.6f * k), Projectile.rotation - i * SpinPerUpdate, origin, Projectile.scale * (0.4f + 0.6f * k), SpriteEffects.None);
            }
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, pos, null, new Color(120, 180, 255, 0) * 0.5f, 0f, bloom.Size() / 2f, GlowSize / bloom.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    public class SorrowMirrorWrath : ModProjectile
    {
        public const int ExtraUpdates = 1;          //同原版
        public const int LifeUpdates = 240;         //约2秒
        public const int Hits = 2;
        public const float DrawScale = 0.7f;        //同原版
        public static readonly Vector2 HeadOrigin = new(17f, 20f);   //像素,火球头部
        public const int TrailLength = 12;
        public const int ImpactDust = 14;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = Hits;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifeUpdates;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.3f, 0.6f);
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.IceTorch, -Projectile.velocity * 0.2f, 0, default, 1.2f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.DD2_BetsysWrathImpact with { Volume = 0.6f, MaxInstances = 3 }, Projectile.Center);
            for (int i = 0; i < ImpactDust; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, CEUtils.randomPointInCircle(5f), 0, default, 1.5f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            for (int i = Projectile.oldPos.Length - 1; i > 0; i -= 2) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float k = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, at, null, new Color(120, 160, 255, 0) * (0.5f * k), Projectile.rotation, HeadOrigin, DrawScale * (0.7f + 0.3f * k), SpriteEffects.None);
            }
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, HeadOrigin, DrawScale, SpriteEffects.None);
            return false;
        }
    }

    public class SorrowMirrorBoulder : ModProjectile
    {
        public const int ExtraUpdates = 1;          //同原版
        public const int LifeUpdates = 360;         //约3秒
        public const int Hits = 2;
        public const float Gravity = 0.2f;          //像素/更新²
        public const float MaxFall = 12f;           //像素/更新
        public const float BounceX = 0.6f;          //撞墙速度保留
        public const float BounceY = 0.45f;         //落地速度保留
        public const float MinBounce = 2f;          //像素/更新,低于则贴地
        public const float RollFriction = 0.985f;   //每次触地
        public const float MinDamageSpeed = 1.5f;   //像素/更新
        public const float SpinFactor = 0.03f;      //弧度/像素
        public const float GlowSize = 48f;          //像素
        public const int BreakDust = 16;

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = Hits;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifeUpdates;
        }

        public override bool? CanDamage() => Projectile.velocity.Length() < MinDamageSpeed ? false : null;

        public override void AI() {
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + Gravity, MaxFall);
            Projectile.rotation += Projectile.velocity.X * SpinFactor;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * BounceX;
            if (Projectile.velocity.Y != oldVelocity.Y) {
                Projectile.velocity.Y = Math.Abs(oldVelocity.Y) > MinBounce ? -oldVelocity.Y * BounceY : 0f;
                Projectile.velocity.X *= RollFriction;
            }
            return false;
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.6f, MaxInstances = 3 }, Projectile.Center);
            for (int i = 0; i < BreakDust; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.Frost, CEUtils.randomPointInCircle(4f), 0, default, 1.2f);
                d.noGravity = Main.rand.NextBool();
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D bloom = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(bloom, pos, null, new Color(140, 180, 255, 0) * 0.25f, 0f, bloom.Size() / 2f, GlowSize / bloom.Width, SpriteEffects.None);
            Color body = Color.Lerp(lightColor, new Color(210, 230, 255), 0.4f);
            Main.EntitySpriteDraw(tex, pos, null, body, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
