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

		string blueprint = Designer.SaveVehicle();
		var op = SceneManager.LoadSceneAsync("Track Test");

		yield return new WaitUntil(() => op.isDone);

		var vehicle = JsonUtility.FromJson<VehicleBlueprint>(blueprint);

		var vehicleObject = GameObject.Find("Vehicle");
		vehicleObject.GetComponent<VehicleSpawner>().SpawnVehicle(vehicle);

		Destroy(gameObject);
	}
}