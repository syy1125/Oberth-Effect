using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public class BeamWeaponVisual : MonoBehaviour
{
	private LineRenderer _line;
	private Transform _hitParticleParentTransform;
	private ParticleSystemWrapper[] _hitParticles;

	private bool _firing;
	private bool _hit;

	public void Init(float beamWidth, Color beamColor, ParticleSystemSpec[] hitParticlesSpec)
	{
		_line = gameObject.AddComponent<LineRenderer>();
		_line.startColor = beamColor;
		_line.endColor = beamColor;
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

	public void UpdateState(bool firing, Vector3 localEnd, Vector3? hitNormal)
	{
		if (firing)
		{
			if (!_firing)
			{
				_firing = true;
				_line.enabled = true;
			}

			localEnd.z = -1;
			_line.SetPositions(new[] { Vector3.back, localEnd });
		}
		else
		{
			if (_firing)
			{
				_firing = false;
				_line.enabled = false;
			}
		}

		if (_hitParticles != null)
		{
			if (hitNormal.HasValue)
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
}