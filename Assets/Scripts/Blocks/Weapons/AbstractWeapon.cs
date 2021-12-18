using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
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
	protected IWeaponEffectEmitter WeaponEmitter;

	protected bool Firing;
	protected Vector2? AimPoint;
	protected Tuple<Vector2, Vector2> PointDefenseTarget;

	public WeaponBindingGroup WeaponBinding { get; protected set; }

	#region Init

	protected void Awake()
	{
		Core = GetComponent<BlockCore>();
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

		WeaponEmitter = weaponEmitter;
	}

	protected void LoadBurstBeamWeapon(BurstBeamWeaponEffectSpec spec)
	{
		var weaponEffectObject = new GameObject("BurstBeamWeaponEffect");

		SetWeaponEffectTransform(weaponEffectObject, spec);

		var weaponEmitter = weaponEffectObject.AddComponent<BurstBeamWeaponEffectEmitter>();
		weaponEmitter.LoadSpec(spec);

		WeaponEmitter = weaponEmitter;
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
				Options = new[] { "Manual 1", "Manual 2", "Point Defense" },
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

	public void SetPointDefenseTargetList(IReadOnlyList<PointDefenseTarget> targets)
	{
		Vector2 position = transform.position;
		float limit = GetMaxRange();
		limit *= limit;

		Vector2 localVelocity = GetComponentInParent<Rigidbody2D>().GetPointVelocity(position);
		int bestIndex = -1;
		float bestScore = float.NegativeInfinity;

		for (int i = 0; i < targets.Count; i++)
		{
			var target = targets[i];
			Vector2 relativePosition = (Vector2) target.transform.position - position;
			if (relativePosition.sqrMagnitude > limit) continue;

			Vector2 relativeVelocity = target.GetComponent<Rigidbody2D>().velocity - localVelocity;
			float score = Vector2.Dot(relativePosition, -relativeVelocity) / relativePosition.sqrMagnitude;

			if (score > bestScore)
			{
				bestIndex = i;
				bestScore = score;
			}
		}

		if (bestIndex < 0) // No valid target
		{
			SetAimPoint(null);
			SetFiring(false);
			return;
		}

		var bestTarget = targets[bestIndex];
		if (WeaponEmitter is ProjectileWeaponEffectEmitter projectileEmitter)
		{
			Vector2 relativePosition = (Vector2) bestTarget.transform.position - position;
			Vector2 relativeVelocity = bestTarget.GetComponent<Rigidbody2D>().velocity - localVelocity;

			InterceptSolver.ProjectileIntercept(
				relativePosition, relativeVelocity, projectileEmitter.GetMaxSpeed(),
				out Vector2 interceptVelocity, out float hitTime
			);

			SetAimPoint(position + interceptVelocity * hitTime);
		}
		else // Is some kind of beam weapon
		{
			SetAimPoint(bestTarget.transform.position);
		}

		SetFiring(true);
	}

	public void SetAimPoint(Vector2? aimPoint)
	{
		AimPoint = aimPoint;
		WeaponEmitter.SetAimPoint(aimPoint);
	}

	public void SetFiring(bool firing)
	{
		Firing = firing;
	}

	public void InvokeWeaponEffectRpc(
		string methodName, RpcTarget rpcTarget, params object[] parameters
	)
	{
		GetComponentInParent<IBlockRpcRelay>().InvokeBlockRpc(
			Core.RootPosition, typeof(TurretedWeapon), nameof(ReceiveWeaponEmitterRpc), rpcTarget,
			methodName, parameters
		);
	}

	public void ReceiveWeaponEmitterRpc(string methodName, object[] parameters)
	{
		var method = WeaponEmitter.GetType().GetMethod(
			methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
		);

		if (method == null)
		{
			Debug.LogError($"Method {methodName} does not exist for {WeaponEmitter.GetType()}");
			return;
		}

		method.Invoke(WeaponEmitter, parameters);
	}

	#endregion

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		return WeaponEmitter.GetResourceConsumptionRateRequest();
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		WeaponEmitter.SatisfyResourceRequestAtLevel(level);
	}

	public float GetMaxRange()
	{
		return WeaponEmitter.GetMaxRange();
	}

	public void GetMaxFirepower(IList<FirepowerEntry> entries)
	{
		WeaponEmitter.GetMaxFirepower(entries);
	}

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return WeaponEmitter.GetMaxResourceUseRate();
	}

	protected void AppendAggregateDamageInfo(StringBuilder builder)
	{
		List<FirepowerEntry> firepower = new List<FirepowerEntry>();
		GetMaxFirepower(firepower);
		float maxDps = FirepowerUtils.GetTotalDamage(firepower);
		float armorPierce = FirepowerUtils.GetMeanArmorPierce(firepower);
		IReadOnlyDictionary<string, float> resourceUse = GetMaxResourceUseRate();

		builder.AppendLine($"  <b>Maximum DPS {maxDps:0.#} (<color=\"lightblue\">{armorPierce:0.##} AP</color>)</b>");

		Dictionary<string, float> resourcePerFirepower =
			resourceUse.ToDictionary(entry => entry.Key, entry => entry.Value / maxDps);
		string resourceCostPerFirepower = string.Join(
			", ", VehicleResourceDatabase.Instance.FormatResourceDict(resourcePerFirepower)
		);
		builder.Append($"  Resource cost per unit DPS {resourceCostPerFirepower}");
	}
}
}