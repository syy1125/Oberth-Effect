using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Match;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Math;
using Syy1125.OberthEffect.Simulation.UserInterface;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Game
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(RoomViewTeamProvider))]
public class Shipyard : MonoBehaviourPun, IDirectDamageable, IPunObservable, ITargetLockInfoProvider
{
	[Header("Orbit")]
	public CelestialBody ParentBody;
	public float OrbitRadius;
	public float TrueAnomalyAtEpoch;

	[Header("Configuration")]
	public float BaseMaxHealth;
	public Transform[] SpawnPoints;
	public Bounds[] ExplosionBounds;

	[Header("References")]
	public Transform IndicatorCanvas;
	public GameObject ProtectIndicatorPrefab;
	public GameObject DestroyIndicatorPrefab;

	public static readonly Dictionary<int, Shipyard> ActiveShipyards = new Dictionary<int, Shipyard>();

	public bool IsMine => PhotonNetwork.LocalPlayer.IsMasterClient;
	public int OwnerId => -1;
	public int TeamIndex => _teamProvider.TeamIndex;

	private Rigidbody2D _body;
	private RoomViewTeamProvider _teamProvider;
	private Orbit2D _orbit;
	private float _referenceTime;

	private GameMode _gameMode;
	private bool _damageable;
	private GameObject _indicator;

	private Bounds _explosionHull;

	public float MaxHealth { get; private set; }

	public float Health { get; private set; }

	private void Awake()
	{
		_body = GetComponent<Rigidbody2D>();
		_teamProvider = GetComponent<RoomViewTeamProvider>();

		ActiveShipyards.Add(TeamIndex, this);
		_explosionHull = new Bounds();
		foreach (Bounds bounds in ExplosionBounds) _explosionHull.Encapsulate(bounds);

		if (PhotonHelper.GetRoomGameMode().CanDamageShipyards())
		{
			_indicator = Instantiate(
				TeamIndex == PhotonTeamHelper.GetPlayerTeamIndex(PhotonNetwork.LocalPlayer)
					? ProtectIndicatorPrefab
					: DestroyIndicatorPrefab,
				IndicatorCanvas
			);
			_indicator.GetComponent<HighlightTarget>().Target = transform;
		}
	}

	private void OnEnable()
	{
		if (ParentBody != null)
		{
			ParentBody.OnOrbitUpdate += UpdateOrbit;

			_orbit = new Orbit2D
			{
				ParentGravitationalParameter = ParentBody.GravitationalParameter,
				SemiLatusRectum = OrbitRadius,
				Eccentricity = 0f,
				ArgumentOfPeriapsis = 0f,
				TrueAnomalyAtEpoch = TrueAnomalyAtEpoch * Mathf.Deg2Rad
			};
			_referenceTime = (float) PhotonNetwork.Time;

			_body.isKinematic = true;
		}
	}

	public static Shipyard GetShipyardForTeam(int teamIndex)
	{
		return ActiveShipyards.TryGetValue(teamIndex, out Shipyard shipyard) ? shipyard : null;
	}

	private void Start()
	{
		_gameMode = PhotonHelper.GetRoomGameMode();
		_damageable = _gameMode.CanDamageShipyards();
		Health = MaxHealth = BaseMaxHealth * PhotonHelper.GetShipyardHealthMultiplier();
		Debug.Log($"Shipyard for team index {TeamIndex} starts with {Health}/{MaxHealth} health");

		Color teamColor = PhotonTeamHelper.GetTeamColors(TeamIndex).PrimaryColor;
		foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
		{
			sprite.color = teamColor;
		}
	}

	private void OnDisable()
	{
		if (ParentBody != null)
		{
			ParentBody.OnOrbitUpdate -= UpdateOrbit;
		}
	}

	private void OnDestroy()
	{
		ActiveShipyards.Remove(TeamIndex);
	}

	private void UpdateOrbit(Vector2 parentPosition, bool init)
	{
		(Vector2 localPosition, Vector2 _) = _orbit.GetStateVectorAt((float) PhotonNetwork.Time - _referenceTime);
		Vector2 position = parentPosition + localPosition;

		if (init)
		{
			transform.position = position;
		}
		else
		{
			_body.MovePosition(position);
		}
	}

	private void FixedUpdate()
	{
		if (_orbit != null)
		{
			(Vector2 localPosition, Vector2 localVelocity) =
				_orbit.GetStateVectorAt(SynchronizedTimer.Instance.SynchronizedTime);
			transform.position = ParentBody.Body.position + localPosition;
			_body.velocity = ParentBody.Body.velocity + localVelocity;
		}
	}

	public Transform GetBestSpawnPoint(int playerIndex)
	{
		// Decreasing radius to try to ensure maximum free space before spawning
		foreach (var radius in new[] { 15f, 10f, 5f })
		{
			for (int i = 0; i < SpawnPoints.Length; i++)
			{
				Transform t = SpawnPoints[(playerIndex + i) % SpawnPoints.Length];
				// If there are no other crafts obstructing the spawn point, then it's viable.
				var overlapCollider = Physics2D.OverlapCircle(t.position, radius, LayerConstants.DAMAGEABLE_LAYER_MASK);
				if (overlapCollider == null)
				{
					return t;
				}
			}
		}

		// Fallback
		return SpawnPoints[playerIndex % SpawnPoints.Length];
	}

	public Vector2 GetPosition()
	{
		return transform.position;
	}

	public Vector2 GetVelocity()
	{
		if (_orbit == null) return Vector2.zero;

		float time = (float) PhotonNetwork.Time - _referenceTime;
		return ParentBody.GetEffectiveVelocity(time) + _orbit.GetStateVectorAt(time).Item2;
	}

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return new Tuple<Vector2, Vector2>(_explosionHull.min, _explosionHull.max);
	}

	public int GetExplosionGridResolution()
	{
		return 50;
	}

	public Predicate<Vector2> GetPointInBoundPredicate()
	{
		return point => ExplosionBounds.Any(bounds => Contains2D(bounds, point));
	}

	private static bool Contains2D(Bounds bounds, Vector2 point)
	{
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		return min.x <= point.x && min.y <= point.y && max.x >= point.x && max.y >= point.y;
	}

	public void TakeDamage(DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted)
	{
		if (!_damageable)
		{
			damageExhausted = true;
			return;
		}

		if (damage < Health)
		{
			Health -= damage;
			damage = 0f;
			damageExhausted = true;
		}
		else if (Health > 0f) // Shipyard isn't destroyed yet but will be by this attack
		{
			damage -= Health;
			Health = 0f;
			damageExhausted = false;

			if (_indicator != null) Destroy(_indicator);
			AbstractGameManager.GameManager.OnShipyardDestroyed(TeamIndex);
		}
		else
		{
			damageExhausted = false;
		}
	}

	public void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		photonView.RPC(nameof(TakeBeamDamageRpc), photonView.Owner, damageType, damage, armorPierce);
	}

	// Shipyard is stationary, no need to re-do raycast on this end.
	[PunRPC]
	private void TakeBeamDamageRpc(DamageType damageType, float damage, float armorPierce)
	{
		// Shipyard's dead and that's the most important part. In a 2-team game that's game over.
		// TODO in the future we could do overpenetration, especially if there are more than 2 teams.
		TakeDamage(damageType, ref damage, armorPierce, out bool _);
	}

	public void RequestDirectDamage(DamageType damageType, float damage, float armorPierce)
	{
		photonView.RPC(nameof(TakeDirectDamageRpc), photonView.Owner, damageType, damage, armorPierce);
	}

	[PunRPC]
	private void TakeDirectDamageRpc(DamageType damageType, float damage, float armorPierce)
	{
		if (!IsMine) return;
		TakeDamage(damageType, ref damage, armorPierce, out bool _);
	}

	public string GetName()
	{
		return $"Team {TeamIndex + 1} Shipyard";
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(Health);
		}
		else
		{
			Health = (float) stream.ReceiveNext();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.color = Color.yellow;
		foreach (Bounds bound in ExplosionBounds)
		{
			Gizmos.DrawWireCube(bound.center, bound.size);
		}
	}
}
}