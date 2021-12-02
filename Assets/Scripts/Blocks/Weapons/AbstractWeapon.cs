using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public abstract class AbstractWeapon :
	MonoBehaviour,
	IWeaponSystem, IWeaponEffectRpcRelay,
	IResourceConsumer, IConfigComponent
{
	protected BlockCore Core;
	protected List<IWeaponEffectEmitter> WeaponEmitters;

	protected Dictionary<DamageType, float> Firepower;
	protected Dictionary<string, float> MaxResourceUseRate;

	protected bool Firing;
	protected Vector2? AimPoint;
	protected Dictionary<string, float> ResourceRequests;

	public WeaponBindingGroup WeaponBinding { get; protected set; }

	#region Init

	protected void Awake()
	{
		Core = GetComponent<BlockCore>();

		WeaponEmitters = new List<IWeaponEffectEmitter>();
		ResourceRequests = new Dictionary<string, float>();
	}

	protected void OnEnable()
	{
		GetComponentInParent<IWeaponSystemRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.RegisterBlock(this);
	}

	protected void OnDisable()
	{
		GetComponentInParent<IWeaponSystemRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.UnregisterBlock(this);
	}

	protected void LoadProjectileWeapon(ProjectileWeaponEffectSpec spec)
	{
		var weaponEffectObject = new GameObject("ProjectileWeaponEffect");

		SetWeaponEffectTransform(weaponEffectObject, spec);

		var weaponEmitter = weaponEffectObject.AddComponent<ProjectileWeaponEffectEmitter>();
		weaponEmitter.LoadSpec(spec);
		WeaponEmitters.Add(weaponEmitter);
	}

	protected void LoadBurstBeamWeapon(BurstBeamWeaponEffectSpec spec)
	{
		var weaponEffectObject = new GameObject("BurstBeamWeaponEffect");

		SetWeaponEffectTransform(weaponEffectObject, spec);

		var weaponEmitter = weaponEffectObject.AddComponent<BurstBeamWeaponEffectEmitter>();
		weaponEmitter.LoadSpec(spec);
		WeaponEmitters.Add(weaponEmitter);
	}

	protected abstract void SetWeaponEffectTransform(GameObject weaponEffectObject, AbstractWeaponEffectSpec spec);

	#endregion

	#region Config

	public JObject ExportConfig()
	{
		return new JObject
		{
			{ "WeaponBinding", new JValue(WeaponBinding.ToString()) }
		};
	}

	public void InitDefaultConfig()
	{
		WeaponBinding = WeaponBindingGroup.Manual1;
	}

	public void ImportConfig(JObject config)
	{
		if (
			config.ContainsKey("WeaponBinding")
			&& Enum.TryParse(config.Value<string>("WeaponBinding"), out WeaponBindingGroup weaponBinding)
		)
		{
			WeaponBinding = weaponBinding;
		}
	}

	public List<ConfigItemBase> GetConfigItems()
	{
		return new List<ConfigItemBase>
		{
			new StringSwitchSelectConfigItem
			{
				Key = "WeaponBinding",
				Options = new[] { "Manual 1", "Manual 2" },
				Label = "Group",
				Tooltip = string.Join(
					"\n",
					"Weapons bound to different groups have different keybinds to fire.",
					"  Manual 1: Fires on LMB",
					"  Manual 2: Fires on RMB"
				),
				Serialize = SerializeWeaponBinding,
				Deserialize = DeserializeWeaponBinding
			}
		};
	}

	private static string SerializeWeaponBinding(int index)
	{
		return ((WeaponBindingGroup) index).ToString();
	}

	private static int DeserializeWeaponBinding(string binding)
	{
		return Enum.TryParse(binding, out WeaponBindingGroup group) ? (int) group : 0;
	}

	#endregion

	#region Weapon Logic

	public void SetAimPoint(Vector2? aimPoint)
	{
		AimPoint = aimPoint;
	}

	public void SetFiring(bool firing)
	{
		Firing = firing;
	}

	public void InvokeWeaponEffectRpc(
		IWeaponEffectEmitter self, string methodName, RpcTarget rpcTarget, params object[] parameters
	)
	{
		int index = WeaponEmitters.IndexOf(self);
		if (index < 0)
		{
			Debug.LogError($"Failed to find index of {self} in weapon emitter list");
		}

		GetComponentInParent<IBlockRpcRelay>().InvokeBlockRpc(
			Core.RootPosition, typeof(TurretedWeapon), nameof(ReceiveBlockRpc), rpcTarget,
			index, methodName, parameters
		);
	}

	public void ReceiveBlockRpc(int index, string methodName, object[] parameters)
	{
		if (index < 0 || index >= WeaponEmitters.Count)
		{
			Debug.LogError($"Weapon emitter index {index} outside permitted range");
			return;
		}


		IWeaponEffectEmitter emitter = WeaponEmitters[index];
		var method = emitter.GetType().GetMethod(
			methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
		);

		if (method == null)
		{
			Debug.LogError($"Method {methodName} does not exist for {emitter.GetType()}");
			return;
		}

		method.Invoke(emitter, parameters);
	}

	#endregion

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		ResourceRequests.Clear();
		DictionaryUtils.SumDictionaries(
			WeaponEmitters
				.Select(emitter => emitter.GetResourceConsumptionRateRequest())
				.Where(dict => dict != null),
			ResourceRequests
		);
		return ResourceRequests;
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		foreach (IWeaponEffectEmitter emitter in WeaponEmitters)
		{
			emitter.SatisfyResourceRequestAtLevel(level);
		}
	}

	public IReadOnlyDictionary<DamageType, float> GetMaxFirepower()
	{
		if (Firepower == null)
		{
			Firepower = new Dictionary<DamageType, float>();
			DictionaryUtils.SumDictionaries(
				WeaponEmitters.Select(emitter => emitter.GetMaxFirepower()),
				Firepower
			);
		}

		return Firepower;
	}

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		if (MaxResourceUseRate == null)
		{
			MaxResourceUseRate = new Dictionary<string, float>();

			DictionaryUtils.SumDictionaries(
				WeaponEmitters.Select(emitter => emitter.GetMaxResourceUseRate()), MaxResourceUseRate
			);
		}

		return MaxResourceUseRate;
	}
}
}