using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons.Launcher
{
public class AbstractWeaponLauncherSpec : ICustomValidation
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Damage;
	public DamageType DamageType;
	[ValidateRangeFloat(1f, 10f)]
	public float ArmorPierce = 1f;
	public float ExplosionRadius; // Only relevant for explosive damage

	public Vector2 FiringPortOffset;

	[ValidateRangeFloat(0f, 180f)]
	public float AimCorrection;

	public Dictionary<string, float> MaxResourceUse;

	public SoundReferenceSpec FireSound;

	public virtual void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);
		path.Add(nameof(MaxResourceUse));
		ValidationHelper.ValidateResourceDictionary(path, MaxResourceUse, errors);
		path.RemoveAt(path.Count - 1);
	}
}

/// <summary>
/// A weapon launcher is the part of the weapon actually responsible for creating the effect of the weapon, like launching a projectile or firing a beam.
/// </summary>
public abstract class AbstractWeaponLauncher : MonoBehaviour
{
	protected float MaxRange;
	protected float AimCorrection;

	public int? TargetPhotonId { get; set; }
	protected Vector2? AimPoint;
	protected Dictionary<string, float> ReloadResourceUse;
	protected float ResourceSatisfaction;

	private AudioSource _audioSource;
	private string _fireSoundId;
	private AudioClip _fireSound;
	private float _fireSoundVolume;

	protected void LoadSpec(AbstractWeaponLauncherSpec spec, in BlockContext context)
	{
		AimCorrection = spec.AimCorrection;
		ReloadResourceUse = spec.MaxResourceUse;

		if (spec.FireSound != null)
		{
			_audioSource = SoundDatabase.Instance.CreateBlockAudioSource(gameObject, !context.IsMainVehicle);
			_fireSoundId = spec.FireSound.SoundId;
			_fireSound = SoundDatabase.Instance.GetAudioClip(_fireSoundId);
			_fireSoundVolume = spec.FireSound.Volume;
		}
	}

	public void SetAimPoint(Vector2? aimPoint)
	{
		AimPoint = aimPoint;
	}

	public abstract IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();

	public void SatisfyResourceRequestAtLevel(float level)
	{
		ResourceSatisfaction = level;
	}

	public abstract Vector2? GetInterceptPoint(
		Vector2 ownPosition, Vector2 ownVelocity, Vector2 targetPosition, Vector2 targetVelocity
	);

	protected float GetCorrectionAngle()
	{
		if (Mathf.Approximately(AimCorrection, 0f)) return 0f;

		var correctionAngle = AimPoint != null
			? Vector2.SignedAngle(transform.InverseTransformPoint(AimPoint.Value), Vector2.up)
			: 0f;
		return Mathf.Clamp(correctionAngle, -AimCorrection, AimCorrection);
	}

	public abstract void LauncherFixedUpdate(bool isMine, bool firing);

	public float GetMaxRange()
	{
		return MaxRange;
	}

	public abstract void GetMaxFirepower(IList<FirepowerEntry> entries);

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return ReloadResourceUse;
	}

	public abstract string GetLauncherTooltip();

	protected void ExecuteWeaponSideEffects()
	{
		if (_fireSound != null)
		{
			PlayFireSound();
			GetComponentInParent<IWeaponLauncherRpcRelay>()
				.InvokeWeaponLauncherRpc(nameof(PlayFireSound), RpcTarget.Others);
		}
	}

	public void PlayFireSound()
	{
		float volume = GetComponentInParent<IBlockSoundAttenuator>()
			.AttenuateOneShotSound(_fireSoundId, _fireSoundVolume);
		_audioSource.PlayOneShot(_fireSound, volume);
	}
}
}