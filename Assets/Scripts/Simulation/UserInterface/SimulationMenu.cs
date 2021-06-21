using Photon.Pun;
using Syy1125.OberthEffect.Utils;
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
		MenuAction.action.Enable();
		MenuAction.action.performed += ToggleMenu;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		MenuAction.action.performed -= ToggleMenu;
		MenuAction.action.Disable();
	}

	private void ToggleMenu(InputAction.CallbackContext context)
	{
		_open = !_open;

		foreach (Transform child in transform)
		{
			child.gameObject.SetActive(_open);
		}
	}

	public void LeaveGame()
	{
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		PhotonHelper.ClearPhotonPlayerProperties();
		SceneManager.LoadScene(ReturnScene);
	}
}
}