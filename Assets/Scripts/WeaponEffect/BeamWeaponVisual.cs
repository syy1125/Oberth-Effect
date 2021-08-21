using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class BeamWeaponVisual : MonoBehaviour
{
	private LineRenderer _line;
	private Transform _hitParticleParentTransform;
	private ParticleSystem[] _hitParticles;

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

			_hitParticles = new ParticleSystem[hitParticlesSpec.Length];

			for (int i = 0; i < hitParticlesSpec.Length; i++)
			{
				// TODO Code duplicated in AbstractPropulsionBase
				var spec = hitParticlesSpec[i];

				GameObject particleHolder = new GameObject("ParticleSystem");

				var holderTransform = particleHolder.transform;
				holderTransform.SetParent(_hitParticleParentTransform);
				holderTransform.localPosition = new Vector3(spec.Offset.x, spec.Offset.y, 1f);
				holderTransform.localRotation = Quaternion.LookRotation(spec.Direction);

				var particles = particleHolder.AddComponent<ParticleSystem>();
				particles.LoadSpec(spec);
				particles.Stop();

				_hitParticles[i] = particles;
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

					foreach (ParticleSystem particle in _hitParticles)
					{
						particle.Play();
					}
				}

				_hitParticleParentTransform.position = localEnd;
				_hitParticleParentTransform.rotation = Quaternion.LookRotation(Vector3.forward, -normal);
			}
			else
			{
				if (_hit)
				{
					_hit = false;

					foreach (ParticleSystem particle in _hitParticles)
					{
						particle.Stop();
					}
				}
			}
		}
	}
}
}