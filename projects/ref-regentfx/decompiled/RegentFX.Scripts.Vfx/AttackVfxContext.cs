using System.Threading;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;

namespace RegentFX.Scripts.Vfx;

public static class AttackVfxContext
{
	public static bool ShouldDisableRegentWeaponAttack = false;

	public static bool ShouldDisableRegentWeaponSFX = false;

	public static CardModel? CurrentModelSource { get; set; }

	public static AsyncLocal<AttackCommand?> CurrentAttackCommand { get; } = new AsyncLocal<AttackCommand>();

}
