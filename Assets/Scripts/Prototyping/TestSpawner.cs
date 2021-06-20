using System.Collections;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Designer;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TestSpawner : MonoBehaviour
{
	public InputActionReference Activation;
	public VehicleDesigner Designer;

	private void OnEnable()
	{
		Activation.action.performed += HandleActivation;
	}

	private void OnDisable()
	{
		Activation.action.performed -= HandleActivation;
	}

	private void HandleActivation(InputAction.CallbackContext context)
	{
		if (Application.isEditor)
		{
			StartCoroutine(RunLoadScene());
		}
	}

	private IEnumerator RunLoadScene()
	{
		DontDestroyOnLoad(gameObject);

		string blueprint = Designer.SaveVehicle();
		AsyncOperation op = SceneManager.LoadSceneAsync("Track Test");

		yield return new WaitUntil(() => op.isDone);

		GameObject vehicleObject = GameObject.Find("Vehicle");
		vehicleObject.GetComponent<VehicleLoader>().SpawnVehicle(JsonUtility.FromJson<VehicleBlueprint>(blueprint));

		Destroy(gameObject);
	}
}