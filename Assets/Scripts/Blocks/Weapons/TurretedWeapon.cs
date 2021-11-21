using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public class TurretedWeapon :
	MonoBehaviour,
	IWeaponSystem, IWeaponEffectRpcRelay, IConfigComponent, IResourceConsumerBlock, IHasDebrisLogic, ITooltipProvider
{
	public const string CLASS_KEY = "TurretedWeapon";

	private BlockCore _core;

	// From spec
	private float _rotationSpeed;
	private Transform _turretTransform;
	private List<IWeaponEffectEmitter> _weaponEmitters;

	// Cache
	private Dictionary<DamageType, float> _firepower;
	private Dictionary<string, float> _maxResourceUseRate;

	// Config
	public WeaponBindingGroup WeaponBinding { get; set; }

	// State
	private bool _isDebris;
	private bool _firing;
	private Vector2? _aimPoint;
	private float _turretAngle;
	private Dictionary<string, float> _resourceRequests;

	private void Awake()
	{
		_core = GetComponent<BlockCore>();

		_weaponEmitters = new List<IWeaponEffectEmitter>();
		_resourceRequests = new Dictionary<string, float>();
	}

	private void OnEnable()
	{
		GetComponentInParent<IWeaponSystemRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IResourceConsumerBlockRegistry>()?.RegisterBlock(this);
	}

	public void LoadSpec(TurretedWeaponSpec spec)
	{
		_rotationSpeed = spec.Turret.RotationSpeed;

		var turretObject = new GameObject("Turret");
		_turretTransform = turretObject.transform;
		_turretTransform.SetParent(transform);
		_turretTransform.localScale = Vector3.one;
		_turretTransform.localPosition = new Vector3(
			spec.Turret.TurretPivotOffset.x, spec.Turret.TurretPivotOffset.y, -1f
		);

		RendererHelper.AttachRenderers(_turretTransform, spec.Turret.Renderers);

		if (spec.ProjectileWeaponEffect != null)
		{
			var weaponEffectObject = new GameObject("ProjectileWeaponEffect");

			SetWeaponEffectTransform(weaponEffectObject, spec.ProjectileWeaponEffect);

			var weaponEmitter = weaponEffectObject.AddComponent<ProjectileWeaponEffectEmitter>();
			weaponEmitter.LoadSpec(spec.ProjectileWeaponEffect);
			_weaponEmitters.Add(weaponEmitter);
		}

		if (spec.BurstBeamWeaponEffect != null)
		{
			var weaponEffectObject = new GameObject("BurstBeamWeaponEffect");

			SetWeaponEffectTransform(weaponEffectObject, spec.BurstBeamWeaponEffect);

			var weaponEmitter = weaponEffectObject.AddComponent<BurstBeamWeaponEffectEmitter>();
			weaponEmitter.LoadSpec(spec.BurstBeamWeaponEffect);
			_weaponEmitters.Add(weaponEmitter);
		}
	}

	private void SetWeaponEffectTransform(GameObject weaponEffectObject, AbstractWeaponEffectSpec spec)
	{
		var weaponEffectTransform = weaponEffectObject.transform;
		weaponEffectTransform.SetParent(_turretTransform);
		weaponEffectTransform.localPosition = spec.FiringPortOffset;
		weaponEffectTransform.localRotation = Quaternion.identity;
	}

	private void Start()
	{
		_turretAngle = 0;
		ApplyTurretRotation();

		StartCoroutine(LateFixedUpdate());
	}

	private void OnDisable()
	{
		GetComponentInParent<IWeaponSystemRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IResourceConsumerBlockRegistry>()?.UnregisterBlock(this);
	}

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

	public void SetAimPoint(Vector2? aimPoint)
	{
		_aimPoint = aimPoint;
	}

	public void SetFiring(bool firing)
	{
		_firing = firing;
	}

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		_resourceRequests.Clear();
		DictionaryUtils.SumDictionaries(
			_weaponEmitters
				.Select(emitter => emitter.GetResourceConsumptionRateRequest())
				.Where(dict => dict != null),
			_resourceRequests
		);
		return _resourceRequests;
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		foreach (IWeaponEffectEmitter emitter in _weaponEmitters)
		{
			emitter.SatisfyResourceRequestAtLevel(level);
		}
	}

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (enabled)
		{
			UpdateTurretRotationState();
			ApplyTurretRotation();

			foreach (IWeaponEffectEmitter emitter in _weaponEmitters)
			{
				emitter.EmitterFixedUpdate(_firing, _core.IsMine);
			}

			yield return new WaitForFixedUpdate();
		}
	}

	private void UpdateTurretRotationState()
	{
		float targetAngle = _aimPoint == null
			? 0f
			: Vector3.SignedAngle(Vector3.up, transform.InverseTransformPoint(_aimPoint.Value), Vector3.forward);
		_turretAngle = Mathf.MoveTowardsAngle(_turretAngle, targetAngle, _rotationSpeed * Time.fixedDeltaTime);
	}

	private void ApplyTurretRotation()
	{
		_turretTransform.localRotation = Quaternion.AngleAxis(_turretAngle, Vector3.forward);
	}

	public void EnterDebrisMode()
	{
		enabled = false;
	}

	public void InvokeWeaponEffectRpc(
		IWeaponEffectEmitter self, string methodName, RpcTarget rpcTarget, params object[] parameters
	)
	{
		int index = _weaponEmitters.IndexOf(self);
		if (index < 0)
		{
			Debug.LogError($"Failed to find index of {self} in weapon emitter list");
		}

		GetComponentInParent<IBlockRpcRelay>().InvokeBlockRpc(
			_core.RootPosition, typeof(TurretedWeapon), nameof(ReceiveBlockRpc), rpcTarget,
			index, methodName, parameters
		);
	}

	public void ReceiveBlockRpc(int index, string methodName, object[] parameters)
	{
		if (index < 0 || index >= _weaponEmitters.Count)
		{
			Debug.LogError($"Weapon emitter index {index} outside permitted range");
			return;
		}


		IWeaponEffectEmitter emitter = _weaponEmitters[index];
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

	#region Info

	public IReadOnlyDictionary<DamageType, float> GetMaxFirepower()
	{
		if (_firepower == null)
		{
			_firepower = new Dictionary<DamageType, float>();
			DictionaryUtils.SumDictionaries(
				_weaponEmitters.Select(emitter => emitter.GetMaxFirepower()),
				_firepower
			);
		}

		return _firepower;
	}

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		if (_maxResourceUseRate == null)
		{
			_maxResourceUseRate = new Dictionary<string, float>();

			DictionaryUtils.SumDictionaries(
				_weaponEmitters.Select(emitter => emitter.GetMaxResourceUseRate()), _maxResourceUseRate
			);
		}

		return _maxResourceUseRate;
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder
			.AppendLine("Turreted Weapon")
			.AppendLine("  Turret")
			.AppendLine($"    Rotation speed {_rotationSpeed}°/s");

		foreach (IWeaponEffectEmitter emitter in _weaponEmitters)
		{
			builder.Append(emitter.GetEmitterTooltip());
		}

		IReadOnlyDictionary<DamageType, float> firepower = GetMaxFirepower();
		IReadOnlyDictionary<string, float> resourceUse = GetMaxResourceUseRate();
		float maxDps = firepower.Values.Sum();

		builder.AppendLine($"  Theoretical maximum DPS {maxDps:F1}");

		Dictionary<string, float> resourcePerFirepower =
			resourceUse.ToDictionary(entry => entry.Key, entry => entry.Value / maxDps);
		string resourceCostPerFirepower = string.Join(
			", ", VehicleResourceDatabase.Instance.FormatResourceDict(resourcePerFirepower)
		);
		builder.Append($"  Resource cost per unit firepower {resourceCostPerFirepower}");

		return builder.ToString();
	}

	#endregion
}
}