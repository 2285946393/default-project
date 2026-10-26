using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.Common;

internal static class PredictionCloner
{
	private sealed record CloneTypePlan(FieldInfo[] EventFields);

	private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly MirrorMethodSpec[] ModelCloneMethods = new MirrorMethodSpec[2]
	{
		new MirrorMethodSpec(typeof(AbstractModel), "DeepCloneFields", BindingFlags.Instance | BindingFlags.NonPublic, Array.Empty<Type>()),
		new MirrorMethodSpec(typeof(AbstractModel), "AfterCloned", BindingFlags.Instance | BindingFlags.NonPublic, Array.Empty<Type>())
	};

	private static readonly HashSet<Type> KnownModelTypes = new HashSet<Type>
	{
		typeof(CardModel),
		typeof(EnchantmentModel),
		typeof(AfflictionModel),
		typeof(PotionModel),
		typeof(OrbModel),
		typeof(SovereignBlade)
	};

	private static readonly ConcurrentDictionary<Type, CloneTypePlan> TypePlans = new ConcurrentDictionary<Type, CloneTypePlan>();

	public static T ShallowClone<T>(T source) where T : class
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return (T)MemberwiseClone(source);
	}

	public static T CloneModel<T>(T source) where T : AbstractModel
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (!((AbstractModel)source).IsMutable)
		{
			return source;
		}
		Type type = ((object)source).GetType();
		CloneTypePlan orAdd = TypePlans.GetOrAdd(type, CreateTypePlan);
		T val = PredictionCloner.ShallowClone<T>(source);
		object obj = val;
		CardModel val2 = (CardModel)((obj is CardModel) ? obj : null);
		if (val2 == null)
		{
			object obj2 = val;
			EnchantmentModel val3 = (EnchantmentModel)((obj2 is EnchantmentModel) ? obj2 : null);
			if (val3 != null)
			{
				FinalizeCloneEnchantment(val3);
			}
		}
		else
		{
			FinalizeCloneCard(val2);
		}
		FieldInfo[] eventFields = orAdd.EventFields;
		for (int i = 0; i < eventFields.Length; i++)
		{
			eventFields[i].SetValue(val, null);
		}
		return val;
	}

	private static void FinalizeCloneCard(CardModel card)
	{
		((AbstractModel)card).AssertMutable();
		card._keywords = card._keywords?.ToHashSet();
		card._dynamicVars = CloneDynamicVars(card._dynamicVars, (AbstractModel)(object)card);
		CardEnergyCost energyCost = card._energyCost;
		card._energyCost = ((energyCost != null) ? energyCost.Clone(card) : null);
		card._temporaryStarCosts = card._temporaryStarCosts.ToList();
		EnchantmentModel enchantment = card.Enchantment;
		if (enchantment != null)
		{
			EnchantmentModel val = PredictionCloner.CloneModel<EnchantmentModel>(enchantment);
			((AbstractModel)val).AssertMutable();
			val._card = card;
			card.Enchantment = val;
		}
		AfflictionModel affliction = card.Affliction;
		if (affliction != null)
		{
			AfflictionModel val2 = PredictionCloner.CloneModel<AfflictionModel>(affliction);
			((AbstractModel)val2).AssertMutable();
			val2._card = card;
			card.Affliction = val2;
		}
		if (card._canonicalInstance == null)
		{
			card._canonicalInstance = ModelDb.GetById<CardModel>(((AbstractModel)card).Id);
		}
	}

	private static void FinalizeCloneEnchantment(EnchantmentModel enchantment)
	{
		((AbstractModel)enchantment).AssertMutable();
		enchantment._dynamicVars = CloneDynamicVars(enchantment._dynamicVars, (AbstractModel)(object)enchantment);
	}

	private static DynamicVarSet? CloneDynamicVars(DynamicVarSet? source, AbstractModel owner)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		if (source == null)
		{
			return null;
		}
		DynamicVarSet val = new DynamicVarSet(source.Values.Select(ShallowClone<DynamicVar>));
		foreach (DynamicVar value in val.Values)
		{
			value._owner = owner;
		}
		return val;
	}

	private static CloneTypePlan CreateTypePlan(Type type)
	{
		MirrorMethodSpec[] modelCloneMethods = ModelCloneMethods;
		foreach (MirrorMethodSpec mirrorMethodSpec in modelCloneMethods)
		{
			if (mirrorMethodSpec.TryGetOverride(type, out MethodInfo overrideMethod) && (!(overrideMethod.DeclaringType != null) || !KnownModelTypes.Contains(overrideMethod.DeclaringType)))
			{
				Entry.Logger.Warn($"Prediction cloning does not support {type.FullName}.{mirrorMethodSpec.Name} override in {overrideMethod.DeclaringType?.FullName}.{overrideMethod.Name}", 1);
			}
		}
		List<FieldInfo> list = new List<FieldInfo>();
		Type type2 = type;
		while (type2 != null)
		{
			EventInfo[] events = type2.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (EventInfo eventInfo in events)
			{
				FieldInfo field = type2.GetField(eventInfo.Name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field == null || field.FieldType != eventInfo.EventHandlerType)
				{
					Entry.Logger.Warn($"Prediction cloning does not support {type.FullName}.{eventInfo.Name} event declared in {type2.FullName}", 1);
				}
				else
				{
					list.Add(field);
				}
			}
			if (type2 == typeof(AbstractModel))
			{
				break;
			}
			type2 = type2.BaseType;
		}
		return new CloneTypePlan(list.ToArray());
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "MemberwiseClone")]
	private static extern object MemberwiseClone(object source);
}
