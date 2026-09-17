using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Biomes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.CalamityRef;
using InnoVault.Rigs2D.Runtime;
using InnoVault.Rigs2D.Solvers;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.NihilityTwin
{
    /// <summary>
    /// 小混沌细胞。通往母体的绳是一副 Rigs2D 骨架(<c>Assets/Rigs/ChaoticCellSmall.rig.json</c>):
    /// 29 节 <c>VerletStrand</c>,末端每帧钉到母体中心,节长贴合两端距离 × 29/35,与迁移前的 <c>Utilities.Rope</c> 参数逐项对应。纯绘制
    /// </summary>
    public class ChaoticCellSmall : ModNPC
    {
        private Rig2DInstance rig;
        [Rig2DSolver("rope")]
        private VerletStrandSolver ropeSolver;

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 160);
        }
        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.CCellSmallBestiary")
            });
        }
        public override void SetDefaults() {
            NPC.width = 64;
            NPC.height = 64;
            NPC.damage = 80;
            if (Main.expertMode) {
                NPC.damage += 2;
            }
            if (Main.masterMode) {
                NPC.damage += 2;
            }
            NPC.lifeMax = 2200;
            //拆回 3.33 两条:死亡与复仇加成同值,无灾厄时兜底大师/专家
            if (CECal.IsDeathMode) {
                NPC.damage += 2;
            }
            else if (CECal.IsRevengeance) {
                NPC.damage += 2;
            }
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCHit1;
            NPC.value = Item.buyPrice(0, 0, 40, 0);
            NPC.knockBackResist = 0.7f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.5f;
            NPC.dontCountMe = true;
            NPC.netAlways = true;
            SpawnModBiomes = new int[] { ModContent.GetInstance<VoidDummyBoime>().Type };
        }
        public bool init = true;

        private void EnsureRopeRig() {
            if (rig != null) {
                return;
            }
            Vault2DRig asset = CERigAssets.ChaoticCellSmall;
            if (asset == null || !asset.IsValid) {
                return;
            }
            rig = asset.CreateInstance(NPC.whoAmI);
            rig.Bind(this, null);
        }

        /// <summary>绳骨架落地:锚在自身中心,末端钉到母体中心(两端都是已同步坐标)</summary>
        private void UpdateRopeRig() {
            EnsureRopeRig();
            if (rig == null || !rig.Bound) {
                return;
            }
            rig.Scale = NPC.scale;
            rig.SetRoot(NPC.Center, NPC.rotation);
            ropeSolver.EndTarget = owner.Center;
            rig.Step();
        }

        public override void AI() {
            NPC.velocity *= 0.98f;
            if (!owner.active) {
                NPC.active = false;
            }
            NPC.rotation += NPC.velocity.X * 0.002f;
            if (owner.HasValidTarget) {
                NPC.velocity += (owner.target.ToPlayer().Center - NPC.Center).SafeNormalize(Vector2.Zero) * 0.36f;
            }
            NPC.velocity += (owner.Center - NPC.Center) * 0.0022f;
            if (Main.netMode != NetmodeID.MultiplayerClient && (Main.GameUpdateCount % 10 == 0 && Main.rand.NextBool(16))) {
                float rot = (owner.target.ToPlayer().Center - NPC.Center).ToRotation();
                for (int i = 0; i < 2; i++) {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, rot.ToRotationVector2().RotatedBy(0.03f * i) * 22, ModContent.ProjectileType<CellBullet>(), NPC.damage / 7, 4);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, rot.ToRotationVector2().RotatedBy(-0.03f * i) * 22, ModContent.ProjectileType<CellBullet>(), NPC.damage / 7, 4);

                }
            }
            UpdateRopeRig();
            foreach (NPC n in Main.ActiveNPCs) {
                if (n.type == NPC.type && n.whoAmI != NPC.whoAmI) {
                    if (n.getRect().Intersects(NPC.getRect())) {
                        NPC.velocity += (NPC.Center - n.Center).SafeNormalize(Vector2.UnitX) * 1f;
                    }
                }
            }
        }
        public NPC owner { get { return ((int)NPC.ai[0]).ToNPC(); } }

        public override bool CheckActive() {
            return !owner.active;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (NPC.IsABestiaryIconDummy)
                return true;
            if (rig == null || !rig.Bound || !rig.Built) {
                Texture2D fallback = TextureAssets.Npc[NPC.type].Value;
                Main.EntitySpriteDraw(fallback, NPC.Center - Main.screenPosition, null, Color.White, NPC.rotation, fallback.Size() / 2, NPC.scale, SpriteEffects.None);
                return false;
            }
            //绳带在下、本体件在上;带状件会自己切一轮批次再回到 Deferred / AlphaBlend
            Rig2DDrawContext ctx = Rig2DDrawContext.World().Flat(Color.White);
            Rig2DRenderer.DrawAll(spriteBatch, rig, in ctx);
            return false;
        }

    }
}
