using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class SimulationMenu : MonoBehaviourPunCallbacks
{
	public InputActionReference MenuAction;
	public SceneReference ReturnScene;

	private bool _open;

	public override void OnEnable()
	{
		base.OnEnable();
		_open = false;
		MenuAction.action.performed += ToggleMenu;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		MenuAction.action.performed -= ToggleMenu;
	}

	private void ToggleMenu(InputAction.CallbackContext context)
	{
		_open = !_open;


		foreach (Transform child in transform)
		{
			child.gameObject.SetActive(_open);
		}

		Debug.Log(_open ? "Simulation menu is now open" : "Simulation menu is now closed");
	}

	public void LeaveGame()
	{
		Debug.Log("Leaving room");
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		SceneManager.LoadScene(ReturnScene);
	}
}
}