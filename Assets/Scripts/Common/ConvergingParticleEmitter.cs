using Unity.Collections;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

namespace Syy1125.OberthEffect.Common
{
[RequireComponent(typeof(ParticleSystem))]
public class ConvergingParticleEmitter : MonoBehaviour
{
	private struct ConvergeParticlesJob : IJobParticleSystem
	{
		[ReadOnly]
		public Vector2 TargetLocalPosition;
		[ReadOnly]
		public float Dispersion;

		public void Execute(ParticleSystemJobData jobData)
		{
			Vector2 normal = new Vector2(-TargetLocalPosition.y, TargetLocalPosition.x) * Dispersion;

			var positionsX = jobData.positions.x;
			var positionsY = jobData.positions.y;
			var yScales = jobData.customData1.x;

			for (int i = 0; i < jobData.count; i++)
			{
				float x = jobData.aliveTimePercent[i] / 100f;
				float y = x * (1 - x) * yScales[i];
				Vector2 targetPosition = TargetLocalPosition * x + normal * y;
				positionsX[i] = targetPosition.x;
				positionsY[i] = targetPosition.y;
			}
		}
	}

	public Vector3 Target;
	public float Dispersion = 1f;

	private ParticleSystem _particles;
	private ConvergeParticlesJob _job;

	private void Awake()
	{
		_particles = GetComponent<ParticleSystem>();
		_job = new ConvergeParticlesJob();
	}

	private void LateUpdate()
	{
		_job.TargetLocalPosition = transform.InverseTransformPoint(Target);
		_job.Dispersion = Dispersion;
	}

	private void OnParticleUpdateJobScheduled()
	{
		_job.Schedule(_particles);
	}
}
}