using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common;

public class EPlayerDash : ModPlayer
{
    public const int DashDown = 0;
    public const int DashUp = 1;
    public const int DashRight = 2;
    public const int DashLeft = 3;
    public const int DashCooldown = 50; public const int DashDuration = 28;
    public const float DashVelocity = 24f;
    public int DashDir = -1;
    public int DashDirLast = -1;
    public bool DashAccessoryEquipped;
    public bool velt;
    public int DashDelay = 0;
    public int DashTimer = 0;
    public override void ResetEffects()
    {
        DashAccessoryEquipped = false;

        if (Player.controlDown && Player.releaseDown && Player.doubleTapCardinalTimer[DashDown] < 15)
        {
            DashDir = DashDown;
        }
        else if (Player.controlUp && Player.releaseUp && Player.doubleTapCardinalTimer[DashUp] < 15)
        {
            DashDir = DashUp;
        }
        else if (Player.controlRight && Player.releaseRight && Player.doubleTapCardinalTimer[DashRight] < 15)
        {
            DashDir = DashRight;
        }
        else if (Player.controlLeft && Player.releaseLeft && Player.doubleTapCardinalTimer[DashLeft] < 15)
        {
            DashDir = DashLeft;
        }
        else
        {
            DashDir = -1;
        }
    }

    public override void PreUpdateMovement()
    {
        if (!Main.dedServ && EModPlayer.DashHotkey.JustPressed)
        {
            if (Player.direction == 1)
            {
                DashDir = DashRight;
            }
            else
            {
                DashDir = DashLeft;
            }
            if (Player.controlLeft)
            {
                DashDir = DashLeft;
            }
            if (Player.controlRight)
            {
                DashDir = DashRight;
            }
        }
        if (!Main.dedServ && CanUseDash() && (DashDir != -1 || EModPlayer.DashHotkey.JustPressed) && DashDelay == 0)
        {
            DashDirLast = DashDir;
            Player.wingTime -= 20;

            Vector2 newVelocity = Player.velocity;

            switch (DashDir)
            {
                case DashUp when Player.velocity.Y > -DashVelocity:
                case DashDown when Player.velocity.Y < DashVelocity:
                    {
                        float dashDirection = DashDir == DashDown ? 1 : -1.3f;
                        newVelocity.Y = dashDirection * DashVelocity;
                        break;
                    }
                case DashLeft when Player.velocity.X > -DashVelocity:
                case DashRight when Player.velocity.X < DashVelocity:
                    {
                        float dashDirection = DashDir == DashRight ? 1 : -1;
                        newVelocity.X = dashDirection * DashVelocity;
                        break;
                    }
                default:
                    return;
            }

            DashDelay = DashCooldown;
            DashTimer = DashDuration;
            Player.velocity = newVelocity;
            Player.immuneTime = 30;
            for (int i = 0; i < 32; i++)
            {
                //PRT_Void dash残影,Opacity/vd spawn后赋,旧VoidParticles原值
                var p = PRTLoader.NewParticle<PRT_Void>(Player.Center, newVelocity + CEUtils.randomPointInCircle(6), Color.White, 1f);
                p.Opacity = 0.36f;
                p.vd = 0.9f;
            }
        }

        if (DashDelay > 0)
        {
            DashDelay--;
            if (DashDelay <= 0)
            {
                Player.dashDelay = 20;
                if (DashDir == DashUp)
                    Player.Entropy().gravAddTime = 30;
            }
        }

        if (DashTimer > 0)
        {
            Player.eocDash = DashTimer;
            Player.armorEffectDrawShadowEOCShield = true;
            if (DashDirLast == DashDown)
            {
                Player.velocity.Y = DashVelocity;
            }
            var p = PRTLoader.NewParticle<PRT_Void>(Player.Center, Vector2.Zero, Color.White, 1f);
            p.Opacity = 0.34f;
            p.vd = 0.9f;
            p = PRTLoader.NewParticle<PRT_Void>(Player.Center - Player.velocity / 2, Vector2.Zero, Color.White, 1f);
            p.Opacity = 0.34f;
            p.vd = 0.9f;
            DashTimer--;
            Player.dashDelay = -1;
        }
    }

    private bool CanUseDash()
    {
        // 暗影披风占用中禁用符文冲刺;冷却期间旗标为假,符文冲刺可正常触发
        return DashAccessoryEquipped
               && !Player.setSolar && !Player.mount.Active
               && !Player.Entropy().shadeDashExclusive
               && Player.wingTime > 20;
    }
}