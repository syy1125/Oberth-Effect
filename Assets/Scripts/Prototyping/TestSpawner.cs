using System.Collections;
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
		StartCoroutine(RunLoadScene());
	}

	private IEnumerator RunLoadScene()
	{
		DontDestroyOnLoad(gameObject);

		VehicleBlueprint blueprint = Designer.SaveVehicle();
		AsyncOperation op = SceneManager.LoadSceneAsync("Track Test");

		yield return new WaitUntil(() => op.isDone);

		GameObject vehicleObject = GameObject.Find("Vehicle");
		vehicleObject.GetComponent<VehicleSpawner>().SpawnVehicle(blueprint);

		Destroy(gameObject);
	}
}