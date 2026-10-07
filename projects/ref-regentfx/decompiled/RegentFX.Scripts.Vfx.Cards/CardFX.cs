using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

public abstract class CardFX : FX
{
	public enum HoldingModes
	{
		BorrowDefault,
		BorrowAll,
		Custom,
		None
	}

	protected static bool _registryInitialized;

	public static readonly Dictionary<Type, Type> Registry = new Dictionary<Type, Type>();

	public CardModel? card;

	public const string DEFAULT_REGENT_ATTACK_SFX = "event:/sfx/characters/regent/regent_attack";

	public bool Enabled => Setting.ToggleEnabled(GetToggleKey(GetType()));

	public static IEnumerable<Type> GetTypes => from t in typeof(CardFX).Assembly.GetTypes()
		where t.IsSubclassOf(typeof(CardFX)) && !t.IsAbstract
		select t;

	public bool IsCharacterFacingRight
	{
		get
		{
			CardModel? obj = card;
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				Player owner = obj.Owner;
				obj2 = ((owner != null) ? owner.Creature : null);
			}
			if (obj2 != null)
			{
				return VFXUtil.IsCharacterFacingRight(card.Owner.Creature);
			}
			return false;
		}
	}

	public virtual HoldingModes HoldingMode => HoldingModes.BorrowDefault;

	public virtual int StarCount => 0;

	public virtual Vector2 TargetOffset => new Vector2(-260f, -400f);

	public virtual float StarSpacing => 40f;

	public virtual float MoveDuration => 0.3f;

	public virtual (float min, float max) DurationRange => (min: 0.8f, max: 1.2f);

	public virtual float ShakeIntensity => 5f;

	public virtual float ShakeSpeed => 20f;

	public virtual string HoldSfxPath => "event:/RegentFx/sfx/common_hold_1";

	public virtual string? HitSfxPath => null;

	public virtual string? SecondarySfxPath => null;

	public virtual float VfxClearDelay => 2f;

	public virtual bool HasExposureEffect => false;

	public virtual float ExposurePeak => 1.2f;

	public virtual float ExposureInDuration => 0.1f;

	public virtual float ExposureOutDuration => 0.5f;

	public virtual bool UseV2Patch => false;

	public virtual bool ShouldDisableRegentWeaponAttack => true;

	public virtual bool ShouldDisableRegentWeaponSFX => true;

	public virtual bool HasOnBeforeExecute => false;

	public virtual bool HasOnBeforeDamage => false;

	public virtual bool HasOnBeforeDamageTargeted => false;

	public virtual bool HasAfterPlay => false;

	public virtual string? ChangeHitFx => null;

	public virtual bool RemoveHitFx => false;

	public static string GetToggleKey(Type type)
	{
		return "card_" + type.Name;
	}

	public static bool IsTypeEnabled<T>() where T : CardFX
	{
		return Setting.ToggleEnabled(GetToggleKey(typeof(T)));
	}

	public static void EnsureRegistry()
	{
		if (_registryInitialized)
		{
			return;
		}
		_registryInitialized = true;
		foreach (Type getType in GetTypes)
		{
			CardFxAttribute cardFxAttribute = getType.GetCustomAttributes(typeof(CardFxAttribute), inherit: false).Cast<CardFxAttribute>().FirstOrDefault();
			if (cardFxAttribute != null)
			{
				Registry[cardFxAttribute.CardType] = getType;
			}
		}
	}

	public static CardFX? FromCard(CardModel card)
	{
		EnsureRegistry();
		if (card == null)
		{
			return null;
		}
		Type type = ((object)card).GetType();
		if (Registry.TryGetValue(type, out Type value))
		{
			CardFX cardFX = (CardFX)Activator.CreateInstance(value);
			if (cardFX != null && cardFX.Enabled)
			{
				cardFX.card = card;
				return cardFX;
			}
		}
		return null;
	}

	public virtual void HoldingCustom()
	{
	}

	public virtual async Task OnBeforeExecute()
	{
	}

	public virtual async Task OnBeforeDamage(AttackCommand command)
	{
	}

	public virtual async Task OnBeforeDamage(AttackCommand command, IReadOnlyList<Creature> targets)
	{
	}

	public virtual void AfterPlay()
	{
	}

	public virtual void OnCancel()
	{
	}

	public virtual Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		Vector2 result = basePosition + TargetOffset;
		if (totalCount > 1)
		{
			float num = (0f - (float)(totalCount - 1) * StarSpacing) / 2f;
			result.X += num + (float)index * StarSpacing;
		}
		return result;
	}

	public virtual void OnStartHolding(Star star, int index)
	{
	}

	public void TryPlayHoldingSfx()
	{
		if (!string.IsNullOrEmpty(HoldSfxPath))
		{
			FmodLite.Play(HoldSfxPath);
		}
	}
}
