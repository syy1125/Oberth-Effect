using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Lib
{
[Serializable]
public struct PidConfig
{
	public float Response;
	public float DerivativeTime;
	public float IntegralTime;
}

public class Pid<T>
{
	public T Output { get; private set; }

	private PidConfig _config;

	private Func<T, T, T> _addOp;
	private Func<T, T, T> _subOp;
	private Func<T, float, T> _multOp;

	private bool _hasPrev;
	private T _prev;
	private T _integral;

	public Pid(PidConfig config, Func<T, T, T> addOp, Func<T, T, T> subOp, Func<T, float, T> multOp)
	{
		_config = config;
		_hasPrev = false;

		_addOp = addOp;
		_subOp = subOp;
		_multOp = multOp;
	}

	public void Update(T value, float deltaTime)
	{
		T baseResponse = value;

		if (_hasPrev && !Mathf.Approximately(_config.DerivativeTime, 0f))
		{
			T derivative = _multOp(_subOp(value, _prev), 1f / deltaTime);
			baseResponse = _addOp(baseResponse, _multOp(derivative, _config.DerivativeTime));
		}

		if (!Mathf.Approximately(_config.IntegralTime, 0f))
		{
			_integral = _addOp(_integral, _multOp(value, deltaTime));
			baseResponse = _addOp(baseResponse, _multOp(_integral, 1f / _config.IntegralTime));
		}

		_prev = value;
		_hasPrev = true;

		Output = _multOp(baseResponse, _config.Response);
	}

	public void Reset()
	{
		_hasPrev = false;
		_integral = default;
	}
}
}