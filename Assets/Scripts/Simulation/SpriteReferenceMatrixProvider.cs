using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
[RequireComponent(typeof(Renderer))]
public class SpriteReferenceMatrixProvider : MonoBehaviour
{
	private static readonly int LocalProjectionMatrix = Shader.PropertyToID("_LocalProjectionMatrix");

	private Renderer _renderer;
	private MaterialPropertyBlock _block;
	private Camera _mainCamera;

	private void Awake()
	{
		_renderer = GetComponent<Renderer>();
		_block = new MaterialPropertyBlock();
		_mainCamera = Camera.main;
	}

	private void LateUpdate()
	{
		var localToWorldMatrix = _renderer.localToWorldMatrix;
		var localToViewportMatrix = _mainCamera.projectionMatrix * _mainCamera.worldToCameraMatrix * localToWorldMatrix;

		_renderer.GetPropertyBlock(_block);
		_block.SetMatrix(LocalProjectionMatrix, localToViewportMatrix);
		_renderer.SetPropertyBlock(_block);
	}
}
}