﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.CoreMod.Weapons.Launcher;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public abstract class AbstractWeaponSpec
{
	// Choose one. Only one will be installed anyway.
	public ProjectileLauncherSpec ProjectileLauncher;
	public MissileLauncherSpec MissileLauncher;
	public BurstBeamLauncherSpec BurstBeamLauncher;

	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public WeaponBindingGroup DefaultBinding;
}

public abstract class AbstractWeapon :
	MonoBehaviour,
	IWeaponBlock,
	IWeaponLauncherRpcRelay,
	IResourceConsumer,
	IConfigComponent
{
	protected BlockCore Core;
	protected AbstractWeaponLauncher WeaponLauncher;

	private GameObject _rangePreview;

	protected bool Firing;
	protected Vector2? AimPoint;
	protected int? TargetPhotonId;
	protected Tuple<Vector2, Vector2> PointDefenseTarget;

	protected WeaponBindingGroup DefaultBinding;
	public WeaponBindingGroup WeaponBinding { get; protected set; }

	#region Init

	protected void Awake()
	{
		Core = GetComponent<BlockCore>();
	}

	protected void OnEnable()
	{
		GetComponentInParent<IWeaponBlockRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.RegisterBlock(this);
	}

	protected void OnDisable()
	{
		GetComponentInParent<IWeaponBlockRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.UnregisterBlock(this);
	}

	protected void LoadProjectileWeapon(ProjectileLauncherSpec spec, in BlockContext context)
	{
		var launcherObject = new GameObject("ProjectileLauncher");

		SetWeaponLauncherTransform(launcherObject, spec);

		var launcher = launcherObject.AddComponent<ProjectileLauncher>();
		launcher.LoadSpec(spec, context);

		WeaponLauncher = launcher;
	}

	protected void LoadBurstBeamWeapon(BurstBeamLauncherSpec spec, in BlockContext context)
	{
		var launcherObject = new GameObject("BurstBeamLauncher");

		SetWeaponLauncherTransform(launcherObject, spec);

		var launcher = launcherObject.AddComponent<BurstBeamLauncher>();
		launcher.LoadSpec(spec, context);

		WeaponLauncher = launcher;
	}

	protected void LoadMissileWeapon(MissileLauncherSpec spec, in BlockContext context)
	{
		var launcherObject = new GameObject("MissileLauncher");

		SetWeaponLauncherTransform(launcherObject, spec);

		var launcher = launcherObject.AddComponent<MissileLauncher>();
		launcher.LoadSpec(spec, context);

		WeaponLauncher = launcher;
	}

	protected abstract void SetWeaponLauncherTransform(GameObject weaponEffectObject, AbstractWeaponLauncherSpec spec);

	protected void ShowRange(Vector2 center, float range)
	{
		if (_rangePreview == null)
		{
			_rangePreview = new("RangePreview");
			_rangePreview.transform.SetParent(transform);
			_rangePreview.transform.localPosition = center;
			_rangePreview.transform.localRotation = Quaternion.identity;
			_rangePreview.transform.localScale = Vector3.one;

			_rangePreview.AddComponent<LineRenderer>();
			_rangePreview.AddComponent<WeaponRangePreview>().SetRadius(range);
		}

		_rangePreview.SetActive(true);
	}

	protected void HideRange()
	{
		if (_rangePreview != null) _rangePreview.SetActive(false);
	}

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
		WeaponBinding = DefaultBinding;
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
		return new()
		{
			new StringSwitchSelectConfigItem
			{
				Key = "WeaponBinding",
				Options = new[] { "Manual 1", "Manual 2", "Manual 3", "Manual 4", "Point Defense" },
				Label = "Group",
				Tooltip = string.Join(
					"\n",
					"Weapons bound to different groups have different keybinds to fire.",
					"  Manual 1: Fires on LMB",
					"  Manual 2: Fires on RMB",
					"  Point Defense: Automatically shoot at approaching projectiles"
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

	public void SetPointDefenseTargetList(IReadOnlyList<PointDefenseTargetData> targetData)
	{
		Vector3 position = transform.position;
		float rangeSqrLimit = GetMaxRange();
		rangeSqrLimit *= rangeSqrLimit;

		Vector2 localVelocity = GetComponentInParent<Rigidbody2D>().GetPointVelocity(position);
		int bestIndex = -1;
		float bestScore = float.NegativeInfinity;
		Vector2 bestAimPoint = Vector2.zero;

		for (int i = 0; i < targetData.Count; i++)
		{
			var target = targetData[i];
			float score = target.PriorityScore;
			if (score <= bestScore) continue;

			Vector2 relativePosition = target.Target.transform.position - position;
			Vector2 relativeVelocity = target.Target.GetComponent<Rigidbody2D>().velocity - localVelocity;

			Vector2? relativeIntercept = WeaponLauncher.GetInterceptPoint(relativePosition, relativeVelocity);
			if (relativeIntercept == null || relativeIntercept.Value.sqrMagnitude > rangeSqrLimit) continue;

			bestIndex = i;
			bestScore = score;
			bestAimPoint = (Vector2) position + relativeIntercept.Value;
		}

		if (bestIndex < 0) // No valid target
		{
			SetAimPoint(null);
			SetTargetPhotonId(null);
			SetFiring(false);
			return;
		}

		var bestTarget = targetData[bestIndex];
		SetAimPoint(bestAimPoint);
		SetTargetPhotonId(bestTarget.Target.photonView.ViewID);
		SetFiring(true);
	}

	public void SetAimPoint(Vector2? aimPoint)
	{
		AimPoint = aimPoint;
		WeaponLauncher.SetAimPoint(aimPoint);
	}

	public void SetTargetPhotonId(int? targetId)
	{
		TargetPhotonId = targetId;
		WeaponLauncher.TargetPhotonId = targetId;
	}

	public void SetFiring(bool firing)
	{
		Firing = firing;
	}

	public void InvokeWeaponLauncherRpc(
		string methodName, RpcTarget rpcTarget, params object[] parameters
	)
	{
		GetComponentInParent<IBlockRpcRelay>().InvokeBlockRpc(
			Core.RootPosition, GetType(), nameof(ReceiveWeaponLauncherRpc), rpcTarget,
			methodName, parameters
		);
	}

	public void ReceiveWeaponLauncherRpc(string methodName, object[] parameters)
	{
		var method = WeaponLauncher.GetType().GetMethod(
			methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
		);

		if (method == null)
		{
			Debug.LogError($"Method {methodName} does not exist for {WeaponLauncher.GetType()}");
			return;
		}

		method.Invoke(WeaponLauncher, parameters);
	}

	#endregion

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		return WeaponLauncher.GetResourceConsumptionRateRequest();
	}

	public float SatisfyResourceRequestAtLevel(float level)
	{
		WeaponLauncher.SatisfyResourceRequestAtLevel(level);
		return level;
	}

	public float GetMaxRange()
	{
		return WeaponLauncher.GetMaxRange();
	}

	public void GetMaxFirepower(IList<FirepowerEntry> entries)
	{
		WeaponLauncher.GetMaxFirepower(entries);
	}

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return WeaponLauncher.GetMaxResourceUseRate();
	}

	protected void AppendAggregateDamageInfo(StringBuilder builder, string indent)
	{
		var firepower = new List<FirepowerEntry>();
		GetMaxFirepower(firepower);
		float maxDps = FirepowerUtils.GetTotalDamage(firepower);
		float armorPierce = FirepowerUtils.GetMeanArmorPierce(firepower);
		IReadOnlyDictionary<string, float> resourceUse = GetMaxResourceUseRate();

		builder.AppendLine(
			$"{indent}<b>Expected DPS {maxDps:0.#} (<color=lightblue>{armorPierce:0.##} AP</color>)</b>"
		);

		Dictionary<string, float> resourcePerFirepower =
			resourceUse.ToDictionary(entry => entry.Key, entry => entry.Value / maxDps);
		string resourceCostPerFirepower = string.Join(
			", ", VehicleResourceDatabase.Instance.FormatResourceDict(resourcePerFirepower)
		);
		builder.AppendLine($"{indent}Resource cost per unit DPS {resourceCostPerFirepower}");
	}
}
}