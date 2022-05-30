using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect.Emitter
{
public abstract class AbstractWeaponEffectEmitter : MonoBehaviour, IWeaponEffectEmitter
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

	protected void LoadSpec(AbstractWeaponEffectSpec spec)
	{
		AimCorrection = spec.AimCorrection;
		ReloadResourceUse = spec.MaxResourceUse;

		if (spec.FireSound != null)
		{
			_audioSource = SoundDatabase.Instance.CreateBlockAudioSource(gameObject);
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
		var correctionAngle = AimPoint != null
			? Vector2.SignedAngle(transform.InverseTransformPoint(AimPoint.Value), Vector2.up)
			: 0f;
		correctionAngle = Mathf.Clamp(correctionAngle, -AimCorrection, AimCorrection);
		return correctionAngle;
	}

	public abstract void EmitterFixedUpdate(bool isMine, bool firing);

	public float GetMaxRange()
	{
		return MaxRange;
	}

	public abstract void GetMaxFirepower(IList<FirepowerEntry> entries);

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return ReloadResourceUse;
	}

	public abstract string GetEmitterTooltip();

	protected void ExecuteWeaponSideEffects()
	{
		if (_fireSound != null)
		{
			PlayFireSound();
			GetComponentInParent<IWeaponEffectRpcRelay>()
				.InvokeWeaponEffectRpc(nameof(PlayFireSound), RpcTarget.Others);
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