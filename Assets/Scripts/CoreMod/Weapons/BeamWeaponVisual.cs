using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public class BeamAfterimageSpec
{
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Duration;

	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateRangeFloat(0f, 1f)]
	public float StartAlpha;

	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateRangeFloat(0f, 1f)]
	public float EndAlpha;
}

public class BeamWeaponVisual : MonoBehaviour
{
	private LineRenderer _line;
	private Transform _hitParticleParentTransform;
	private ParticleSystemWrapper[] _hitParticles;

	private Color _beamColor;
	private BeamAfterimageSpec _afterimageSpec;
	private bool _firing;
	private bool _hit;
	private float? _afterimageDuration;

	public void Init(
		float beamWidth, Color beamColor, BeamAfterimageSpec afterimageSpec, ParticleSystemSpec[] hitParticlesSpec
	)
	{
		_beamColor = beamColor;
		_afterimageSpec = afterimageSpec;

		_line = gameObject.AddComponent<LineRenderer>();
		_line.startColor = _beamColor;
		_line.endColor = _beamColor;
		_line.widthCurve = AnimationCurve.Constant(0f, 1f, beamWidth);
		_line.useWorldSpace = false;
		_line.enabled = false;
		_line.sharedMaterial = TextureDatabase.Instance.DefaultLineMaterial;

		if (hitParticlesSpec != null)
		{
			GameObject hitParticleParentObject = new GameObject("HitParticleParent");
			_hitParticleParentTransform = hitParticleParentObject.transform;
			_hitParticleParentTransform.SetParent(transform);

			_hitParticles = new ParticleSystemWrapper[hitParticlesSpec.Length];

			for (int i = 0; i < hitParticlesSpec.Length; i++)
			{
				_hitParticles[i] = RendererHelper.CreateParticleSystem(
					_hitParticleParentTransform, hitParticlesSpec[i]
				);
			}
		}
	}

	private void Update()
	{
		if (_afterimageSpec == null || _afterimageDuration == null) return;

		_afterimageDuration -= Time.deltaTime;

		if (_afterimageDuration.Value < 0f)
		{
			_afterimageDuration = null;
			_line.enabled = false;
			return;
		}

		Color afterimageColor = _beamColor;
		afterimageColor.a = MathUtils.Remap(
			_afterimageDuration.Value,
			_afterimageSpec.Duration, 0f,
			_afterimageSpec.StartAlpha, _afterimageSpec.EndAlpha
		);
		_line.startColor = afterimageColor;
		_line.endColor = afterimageColor;
	}

	public void UpdateState(bool firing, Vector3 localEnd, Vector3? hitNormal)
	{
		if (firing)
		{
			if (!_firing)
			{
				_firing = true;
				_line.enabled = true;
				_line.startColor = _beamColor;
				_line.endColor = _beamColor;
				_afterimageDuration = null;
			}

			localEnd.z = -1;
			_line.SetPositions(new[] { Vector3.back, localEnd });
		}
		else
		{
			if (_firing)
			{
				_firing = false;

				if (_afterimageSpec != null)
				{
					_afterimageDuration = _afterimageSpec.Duration;
				}
			}
		}

		if (_hitParticles != null)
		{
			UpdateHitParticles(localEnd, hitNormal);
		}
	}

	private void UpdateHitParticles(Vector3 localEnd, Vector3? hitNormal)
	{
		if (_firing && hitNormal.HasValue)
		{
			Vector3 normal = hitNormal.Value;

			if (!_hit)
			{
				_hit = true;

				foreach (ParticleSystemWrapper wrapper in _hitParticles)
				{
					wrapper.Play();
				}
			}

			_hitParticleParentTransform.localPosition = localEnd;
			_hitParticleParentTransform.rotation = Quaternion.LookRotation(Vector3.forward, -normal);
		}
		else
		{
			if (_hit)
			{
				_hit = false;

				foreach (ParticleSystemWrapper wrapper in _hitParticles)
				{
					wrapper.Stop();
				}
			}
		}
	}
}
}