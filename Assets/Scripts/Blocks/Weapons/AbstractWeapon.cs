using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.WeaponEffect;
using Syy1125.OberthEffect.WeaponEffect.Emitter;
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

	protected void LoadMissileWeapon(MissileLauncherEffectSpec spec)
	{
		var weaponEffectObject = new GameObject("MissileLauncherWeaponEffect");

		SetWeaponEffectTransform(weaponEffectObject, spec);

		var weaponEmitter = weaponEffectObject.AddComponent<MissileLauncherEffectEmitter>();
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
		return new List<ConfigItemBase>
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
		Vector2 position = transform.position;
		float rangeSqrLimit = GetMaxRange();
		rangeSqrLimit *= rangeSqrLimit;

		Vector2 localVelocity = GetComponentInParent<Rigidbody2D>().GetPointVelocity(position);
		int bestIndex = -1;
		float bestScore = float.NegativeInfinity;
		Vector2 bestAimPoint = Vector2.zero;

		for (int i = 0; i < targetData.Count; i++)
		{
			var target = targetData[i];

			Vector2 interceptPoint = WeaponEmitter.GetInterceptPoint(
				position, localVelocity,
				target.Target.transform.position, target.Target.GetComponent<Rigidbody2D>().velocity
			);
			if ((interceptPoint - position).sqrMagnitude > rangeSqrLimit) continue;

			float score = target.PriorityScore;

			if (score > bestScore)
			{
				bestIndex = i;
				bestScore = targetData[i].PriorityScore;
				bestAimPoint = interceptPoint;
			}
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
		WeaponEmitter.SetAimPoint(aimPoint);
	}

	public void SetTargetPhotonId(int? targetId)
	{
		TargetPhotonId = targetId;
		WeaponEmitter.TargetPhotonId = targetId;
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
			Core.RootPosition, GetType(), nameof(ReceiveWeaponEmitterRpc), rpcTarget,
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

	public float SatisfyResourceRequestAtLevel(float level)
	{
		WeaponEmitter.SatisfyResourceRequestAtLevel(level);
		return level;
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

		builder.AppendLine($"  <b>Expected DPS {maxDps:0.#} (<color=\"lightblue\">{armorPierce:0.##} AP</color>)</b>");

		Dictionary<string, float> resourcePerFirepower =
			resourceUse.ToDictionary(entry => entry.Key, entry => entry.Value / maxDps);
		string resourceCostPerFirepower = string.Join(
			", ", VehicleResourceDatabase.Instance.FormatResourceDict(resourcePerFirepower)
		);
		builder.Append($"  Resource cost per unit DPS {resourceCostPerFirepower}");
	}
}
}