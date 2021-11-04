using System.IO;
using System.Linq;
using Syy1125.OberthEffect.Common.UserInterface;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu
{
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Tooltip))]
public class PlayButtonGuard : MonoBehaviour
{
	private void OnEnable()
	{
		bool hasVehicle = Directory.Exists(VehicleList.SaveDir)
		                  && Directory.GetFiles(VehicleList.SaveDir)
			                  .Any(file => Path.GetExtension(file).Equals(VehicleList.VEHICLE_EXTENSION));

		if (!hasVehicle)
		{
			GetComponent<Button>().interactable = false;
			GetComponent<Tooltip>().SetTooltip(
				string.Join(
					"\n",
					"You do not have any saved vehicle designs. As such, games you join will not be able to start.",
					"If you are new to the game, it is recommended that you first go through the guides.",
					"Otherwise, head to the designer and create a vehicle design"
				)
			);
		}
	}
}
}