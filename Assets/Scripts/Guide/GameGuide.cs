using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Designer;
using Syy1125.OberthEffect.Designer.Config;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Simulation;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Guide
{
public enum GuideSelection
{
	None,
	DesignerBasic,
	VehicleBasic,
	Gameplay
}

public class GameGuide : MonoBehaviour
{
	#region Unity Fields

	[Header("Common")]
	public RectTransform HighlightFrame;
	public Text GuideTitle;
	public Text GuideText;
	public Button SkipStepButton;
	public Button NextGuideButton;
	public Button ExitGuideButton;
	public float FadeTime = 0.2f;
	public float HighlightZoomTime = 0.5f;
	public float HighlightScale = 5f;
	public float SkipDelay = 10f;
	public TextAsset GuideVehicle;
	public SceneReference GameplayGuide;

	[Header("Designer")]
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;

	public RectTransform Toolbar;
	public RectTransform BlockCategories;
	public RectTransform AnalyzerButton;

	public ToolWindows Tools;
	public DesignerConfig Config;
	public GameObject DesignerMenu;
	public GameObject HelpScreen;

	[Header("Simulation")]
	public GameObject PauseIndicator;
	public PlayerVehicleSpawner VehicleSpawner;
	public RectTransform ShipyardFrame;
	public RectTransform StatusPanelFrame;
	public TargetDummy TargetDummy;
	public GameObject TargetDummyHighlight;
	public GameObject MissilePrefab;
	public string MissileTextureId;
	public RectTransform MinimapFrame;

	#endregion

	private RectTransform _canvasTransform;
	private CanvasGroup _canvasGroup;
	private RectTransform _highlightTarget;
	private Coroutine _zoomHighlight;
	private bool _skip;
	private bool _next;

	public static GuideSelection ActiveGuide = GuideSelection.None;
	private GuideSelection _currentGuide;
	public static GameGuide Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(this);
			return;
		}

		_canvasTransform = GetComponent<RectTransform>();
		_canvasGroup = GetComponent<CanvasGroup>();
	}

	private void OnEnable()
	{
		SkipStepButton.onClick.AddListener(SkipStep);
		NextGuideButton.onClick.AddListener(NextGuide);
		ExitGuideButton.onClick.AddListener(SkipStep);
	}

	private void Start()
	{
		_currentGuide = ActiveGuide;
		ActiveGuide = GuideSelection.None;
		switch (_currentGuide)
		{
			case GuideSelection.None:
				gameObject.SetActive(false);
				break;
			case GuideSelection.DesignerBasic:
				StartCoroutine(PlayDesignerGuide());
				break;
			case GuideSelection.VehicleBasic:
				StartCoroutine(PlayVehicleGuide());
				break;
			case GuideSelection.Gameplay:
				StartCoroutine(PlayGameplayGuide());
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void Update()
	{
		if (_highlightTarget == null)
		{
			HighlightFrame.gameObject.SetActive(false);
		}
		else
		{
			HighlightFrame.gameObject.SetActive(true);
			Vector3[] corners = new Vector3[4];
			_highlightTarget.GetWorldCorners(corners);
			// Reference: https://docs.unity3d.com/ScriptReference/RectTransform.GetWorldCorners.html
			// Corners are in clockwise order starting from bottom left
			HighlightFrame.offsetMin = _canvasTransform.InverseTransformPoint(corners[0]) - new Vector3(5f, 5f, 0);
			HighlightFrame.offsetMax = _canvasTransform.InverseTransformPoint(corners[2]) + new Vector3(5f, 5f, 0);
		}
	}

	private void OnDisable()
	{
		SkipStepButton.onClick.RemoveListener(SkipStep);
		NextGuideButton.onClick.RemoveListener(NextGuide);
		ExitGuideButton.onClick.RemoveListener(SkipStep);
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	#region Guide Utility

	private void SkipStep()
	{
		_skip = true;
	}

	private void NextGuide()
	{
		_next = true;
	}

	private void PauseSimulation()
	{
		Time.timeScale = 0f;
		PauseIndicator.SetActive(true);
	}

	private void UnpauseSimulation()
	{
		Time.timeScale = 1f;
		PauseIndicator.SetActive(false);
	}

	private void Highlight(RectTransform target)
	{
		_highlightTarget = target;

		if (target != null)
		{
			if (_zoomHighlight != null)
			{
				StopCoroutine(_zoomHighlight);
			}

			_zoomHighlight = StartCoroutine(ZoomHighlight());
		}
	}

	private IEnumerator ZoomHighlight()
	{
		float startTime = Time.unscaledTime;
		float endTime = startTime + HighlightZoomTime;

		while (Time.unscaledTime < endTime)
		{
			float scale = Mathf.Lerp(HighlightScale, 1f, Mathf.InverseLerp(startTime, endTime, Time.unscaledTime));
			HighlightFrame.localScale = Vector3.one * scale;

			yield return null;
		}

		HighlightFrame.localScale = Vector3.one;
	}

	private IEnumerator FadeGuideBox(float endAlpha)
	{
		float startAlpha = _canvasGroup.alpha;
		float startTime = Time.unscaledTime;
		float endTime = startTime + FadeTime;
		do
		{
			yield return null;
			float t = Mathf.InverseLerp(startTime, endTime, Time.unscaledTime);
			_canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
		}
		while (Time.unscaledTime < endTime);
	}

	private IEnumerator Step(string title, string text, Func<bool> condition, string skipText = null)
	{
		skipText ??= condition == null ? "Continue" : "Skip";
		SkipStepButton.GetComponentInChildren<Text>().text = skipText;

		SkipStepButton.gameObject.SetActive(true);
		NextGuideButton.gameObject.SetActive(false);
		ExitGuideButton.gameObject.SetActive(false);
		SkipStepButton.interactable = condition == null;

		GuideTitle.text = title;
		GuideText.text = text;
		yield return StartCoroutine(FadeGuideBox(1f));

		if (condition == null)
		{
			yield return new WaitUntil(() => _skip);
		}
		else
		{
			var delayedEnableSkip = StartCoroutine(DelayedEnableSkip(skipText));
			yield return new WaitUntil(() => _skip || condition());
			StopCoroutine(delayedEnableSkip);
			yield return new WaitForSecondsRealtime(0.5f);
		}

		_skip = false;
		SkipStepButton.interactable = false;
		yield return StartCoroutine(FadeGuideBox(0f));
	}

	private IEnumerator FinalStep(string title, string text, bool hasNext)
	{
		SkipStepButton.gameObject.SetActive(false);
		NextGuideButton.gameObject.SetActive(hasNext);
		ExitGuideButton.gameObject.SetActive(true);

		GuideTitle.text = title;
		GuideText.text = text;
		yield return StartCoroutine(FadeGuideBox(1f));

		yield return new WaitUntil(() => _skip || _next);

		bool next = _next;
		_skip = false;
		_next = false;

		yield return StartCoroutine(FadeGuideBox(0f));

		if (next)
		{
			switch (_currentGuide)
			{
				case GuideSelection.DesignerBasic:
					_currentGuide = GuideSelection.VehicleBasic;
					StartCoroutine(PlayVehicleGuide());
					break;
				case GuideSelection.VehicleBasic:
					VehicleSelection.SerializedVehicle = GuideVehicle.text;
					ActiveGuide = GuideSelection.Gameplay;
					SceneManager.LoadScene(GameplayGuide);
					break;
				default:
					Debug.LogError($"Current guide is {_currentGuide} which does not have a next guide!");
					break;
			}
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

	private IEnumerator DelayedEnableSkip(string skipText)
	{
		float startTime = Time.time;
		while (Time.time - startTime < SkipDelay)
		{
			float remainingTime = startTime + SkipDelay - Time.time;
			SkipStepButton.GetComponentInChildren<Text>().text = $"{skipText} ({Mathf.CeilToInt(remainingTime)})";
			yield return null;
		}

		SkipStepButton.interactable = true;
		SkipStepButton.GetComponentInChildren<Text>().text = skipText;
	}

	private void EndGuide()
	{
		gameObject.SetActive(false);
	}

	private IEnumerable<T> GetAllBlockComponents<T>()
	{
		return Designer.Blueprint.Blocks
			.Select(instance => Builder.GetBlockObject(instance))
			.SelectMany(blockObject => blockObject.GetComponents<T>());
	}

	#endregion

	#region Designer Guide

	private IEnumerator PlayDesignerGuide()
	{
		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Introduction to vehicle designer",
				string.Join(
					"\n",
					"Welcome to the vehicle designer! Here you will create your very own designs to your specifications.",
					"This short guide aims to get you familiar with the basic functionalities of the vehicle designer."
				),
				null
			)
		);

		Vector2 startPosition = Designer.transform.localPosition;
		yield return StartCoroutine(
			Step(
				"Designer controls",
				"Use WASD to move around the designer. Hold shift to pan faster. You can also right-click and drag.",
				() => ((Vector2) Designer.transform.localPosition - startPosition).sqrMagnitude > 4f
			)
		);

		yield return StartCoroutine(
			Step(
				"Designer controls",
				"Use scroll wheel to zoom in and out.",
				() => Mathf.Abs(Mathf.Log(Designer.transform.localScale.x)) > 0.5f
			)
		);

		yield return StartCoroutine(
			Step(
				"Placing blocks (1/2)",
				string.Join(
					"\n",
					"To add a block to your design, first click on it from the block palette on the right to select it, then use left click to place blocks. Use R and Shift+R for rotation.",
					"You can click and drag with the cursor to quickly place multiple blocks.",
					"Make something with 10 blocks total to complete this step."
				),
				() => Designer.Blueprint.Blocks.Count >= 10
			)
		);

		yield return StartCoroutine(
			Step(
				"Placing blocks (2/2)",
				"Use Q to deselect blocks and go back to cursor mode. E selects eraser mode for removing blocks. M toggles mirror mode, and period and comma keys move the mirror left and right.\nNote that you can never remove the control core, as a vehicle requires it to function.",
				null
			)
		);

		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Saving and loading",
				"Press esc to bring up the vehicle menu, where you can save your design or load previous ones.",
				() => DesignerMenu.activeSelf
			)
		);

		yield return StartCoroutine(
			Step(
				"Help screen",
				"Press F1 to bring up the help screen, where you can review control schemes.\nThere's also some explanation of game mechanics which you may find useful.",
				() => HelpScreen.activeSelf
			)
		);

		yield return StartCoroutine(
			Step(
				"Tooltips",
				string.Join(
					"\n",
					"Oberth Effect comes with an extensive tooltip system to provide you with detailed information.",
					"If you ever want to know what a block does, hover over it and see what the tooltip says!"
				),
				null
			)
		);

		Highlight(BlockCategories);
		yield return StartCoroutine(
			Step(
				"Block categories",
				"Blocks are grouped into categories. If you are looking for a specific block, filtering by block categories might help.",
				null
			)
		);

		Highlight(Toolbar);
		yield return StartCoroutine(
			Step(
				"Designer toolbox",
				string.Join(
					"\n",
					"So far, you have been working with the block palette. There are two other tool screens: the configuration tool and the analysis tool.",
					"The config tool lets you adjust your vehicle's behaviour and aesthetics settings, and the analysis tool is used to review expected performance of your vehicle.",
					"We will cover those in a future guide, when you're more familiar with other aspects of the game."
				),
				null
			)
		);

		yield return StartCoroutine(
			FinalStep(
				"Conclusion",
				string.Join(
					"\n",
					"That concludes the basic features of the vehicle designer.",
					"When you are ready, you can continue to the Vehicle Essentials guide.",
					"Alternatively, you're welcome to experiment with the designer and see what you can come up with."
				),
				true
			)
		);
	}

	#endregion

	#region Ship Guide

	private IEnumerator PlayVehicleGuide()
	{
		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Vehicle Essentials",
				"Now that you're familiar with the basic designer controls, let's look at what a functioning vehicle needs.",
				null
			)
		);

		yield return StartCoroutine(
			Step(
				"Resources",
				string.Join(
					"\n",
					"Most systems on the vehicle requires resources to function. The vehicle's control core comes with a bit of storage and basic energy generation, but that won't be enough for larger vehicles.",
					"Bring energy generation up to over 200 per second to complete this step."
				),
				() => GetAllBlockComponents<IResourceGenerator>()
					      .Select(generator => generator.GetMaxGenerationRate())
					      .Where(rate => rate != null && rate.ContainsKey("OberthEffect/Energy"))
					      .Sum(rate => rate["OberthEffect/Energy"])
				      >= 200
			)
		);

		yield return StartCoroutine(
			Step(
				"Propulsion (1/3)",
				string.Join(
					"\n",
					"Vehicles need propulsion components to move and maneuver. The more thrusters and engines you equip, the faster you can accelerate or decelerate.",
					"When applicable, propulsion components will also be used to rotate the vehicle. So if you want your vehicle to rotate quickly, place thrusters far away from the center of mass!"
				),
				null
			)
		);

		yield return StartCoroutine(
			Step(
				"Propulsion (2/3)", string.Join(
					"\n",
					"Engines have powerful thrust in a single direction, but they generally require time to ramp up thrust.",
					"Thrusters, on the other hand, are designed to be versatile. They are weaker and less efficient, but have fast response time and often can provide thrust in more than one direction."
				), null
			)
		);

		yield return StartCoroutine(
			Step(
				"Propulsion (3/3)",
				string.Join(
					"\n",
					"A mix of engines and thrusters is recommended to bring out the best of both worlds.",
					"To complete this step, have the vehicle able to generate thrust in all 4 cardinal directions."
				),
				() =>
				{
					float up = 0, down = 0, left = 0, right = 0;

					foreach (VehicleBlueprint.BlockInstance instance in Designer.Blueprint.Blocks)
					{
						GameObject blockObject = Builder.GetBlockObject(instance);
						foreach (IPropulsionBlock propulsion in blockObject.GetComponents<IPropulsionBlock>())
						{
							up += propulsion.GetMaxPropulsionForce(
								CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, instance.Rotation)
							);
							down += propulsion.GetMaxPropulsionForce(
								CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, instance.Rotation)
							);
							left += propulsion.GetMaxPropulsionForce(
								CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, instance.Rotation)
							);
							right += propulsion.GetMaxPropulsionForce(
								CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, instance.Rotation)
							);
						}
					}

					return !Mathf.Approximately(up, 0)
					       && !Mathf.Approximately(down, 0)
					       && !Mathf.Approximately(left, 0)
					       && !Mathf.Approximately(right, 0);
				}
			)
		);

		yield return StartCoroutine(
			Step(
				"Weapons",
				string.Join(
					"\n",
					"Mounting weapons lets you shoot back when encountering hostiles. This guide won't cover then in detail, but you should experiment and see what kinds of weapons work well for your playstyle.",
					"A detailed description of damage mechanics can be found in the help (F1) screen."
				),
				null
			)
		);

		Highlight(AnalyzerButton);
		yield return StartCoroutine(
			Step(
				"Vehicle analyzer",
				string.Join(
					"\n",
					"The vehicle analyzer lets you see the expected performance of your vehicle design, like its resource statistics or maneuverability.",
					"Be sure to check the analyzer every now and then to make sure the vehicle's performance is within expectations."
				),
				() => Tools.SelectedIndex == 2
			)
		);

		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Save your vehicle!",
				"The next tutorial will load a stock vehicle to demonstrate gameplay features. If you made a custom vehicle design, save it before entering the next tutorial!",
				null
			)
		);

		yield return StartCoroutine(
			FinalStep(
				"Conclusion",
				"That concludes the vehicle essentials guide. Next we will cover gameplay controls.\nOr, if you prefer, you can start designing vehicles; the tutorials will always be accessible from the main menu.",
				true
			)
		);
	}

	#endregion

	#region Gameplay Guide

	private IEnumerator PlayGameplayGuide()
	{
		yield return null;

		Highlight(null);
		PauseSimulation();
		yield return StartCoroutine(
			Step(
				"Gameplay Guide",
				string.Join(
					"\n", "Welcome to the gameplay guide!",
					"This guide aims to prepare you for facing off against other players in Oberth Effect."
				),
				null
			)
		);

		Camera.main.GetComponent<CameraZoom>().TargetZoomExponent = 1.5f;
		yield return new WaitForSecondsRealtime(0.5f);

		Highlight(ShipyardFrame);
		yield return StartCoroutine(
			Step(
				"Shipyard",
				string.Join(
					"\n",
					"This is your shipyard. When you're near a friendly shipyard, it will resupply you with resources, indicated by colored particles flowing from the shipyard to your vehicle.",
					"Protect it! If the opposing team destroys your shipyard, you will lose the game. A ring around the shipyard will indicate its remaining health."
				),
				null
			)
		);

		Highlight(null);
		Camera.main.GetComponent<CameraZoom>().TargetZoomExponent = 0.5f;
		UnpauseSimulation();
		yield return StartCoroutine(
			Step(
				"Maneuvering",
				string.Join(
					"\n",
					"You can accelerate your vehicle using WASD. Your vehicle will try to rotate to point in the direction of your mouse.",
					$"Accelerate your vehicle to {PhysicsUnitUtils.FormatSpeed(2)} to complete this step. It shouldn't take too long."
				),
				() => VehicleSpawner.Vehicle.GetComponent<Rigidbody2D>().velocity.sqrMagnitude > 4f
			)
		);

		yield return StartCoroutine(
			Step(
				"Control Schemes",
				string.Join(
					"\n",
					"There are several control modes available for your vehicle, which affects how your key presses translate to thruster actions.",
					"The vehicle is currently on <color=\"lime\">mouse</color> mode. There are two more modes, <color=\"lightblue\">relative</color> and <color=\"yellow\">cruise</color>.",
					"You can cycle between them during gameplay using 'R', or change your vehicle's default control mode in the vehicle config in the designer. Experiment and find out which mode suits your vehicle's playstyle the most!",
					"Cycle to a different control mode to complete this step."
				),
				() => PlayerControlConfig.Instance.ControlMode != VehicleControlMode.Mouse
			)
		);

		PauseSimulation();
		yield return StartCoroutine(
			Step(
				"Acceleration",
				string.Join(
					"\n",
					"A word of caution: there is no \"space drag\" in this game. It takes time to speed up and slow down. If you simply point toward the target and hold W, you will have a hard time slowing down when you overshoot!",
					"A common technique is to use a flip-and-burn trajectory (also known as a brachistochrone trajectory), where you accelerate toward the target until the halfway mark, then spend the other half of the trip accelerating in the opposite direction to slow down."
				),
				null
			)
		);

		yield return StartCoroutine(
			Step(
				"Inertia Dampener",
				"You can press 'X' to toggle inertia dampener, which will try to stop your velocity relative to the global reference frame.",
				null
			)
		);

		yield return StartCoroutine(
			Step(
				"Self Destruct",
				"If you ever find yourself hopelessly out of position, or if your vehicle has been damaged beyond repair, you can hold backspace to self-destruct and respawn at a friendly shipyard.",
				null
			)
		);

		Highlight(StatusPanelFrame);
		yield return StartCoroutine(
			Step(
				"Status Panel",
				string.Join(
					"\n",
					"The left side of your HUD contains a picture-in-picture (pip) frame showing your vehicle, as well as status of the vehicle.",
					"Some blocks can be turned on and off during gameplay. The status of those blocks will also show up on the status panel."
				),
				null
			)
		);

		Highlight(null);
		UnpauseSimulation();
		TargetDummyHighlight.SetActive(true);
		yield return StartCoroutine(
			Step(
				"Weapon Testing",
				"A target dummy has been set up for you to test your weapons. Head there and we will do some live-fire training.",
				() => (VehicleSpawner.Vehicle.transform.position - TargetDummy.transform.position).sqrMagnitude < 2500f
			)
		);

		TargetDummyHighlight.SetActive(false);
		yield return StartCoroutine(
			Step(
				"Weapon Testing",
				"Try hitting it with your flak cannons. On this vehicle, they are assigned to the \"Manual 1\" group. Press and hold LMB to fire them.",
				() => TargetDummy.GetDamageHistory().Any(item => item.Type == DamageType.Explosive)
			)
		);

		yield return StartCoroutine(
			Step(
				"Targeting Lock",
				string.Join(
					"\n",
					"Some of the perceptive players may have already noticed the targeting indicator floating over the target dummy.",
					"All vehicles have the ability to acquire targets. When you acquire a target, it is marked by a targeting indicator, and a pip frame of it shows up on the upper right corner of your HUD.",
					"Press spacebar to \"lock\" the target. This prevents your targeting system from switching targets when you move your cursor away.",
					"To complete this step, lock onto the dummy as your target."
				),
				() => VehicleSpawner.Vehicle.GetComponent<VehicleWeaponControl>().TargetLock
				      && (VehicleSpawner.Vehicle.GetComponent<VehicleWeaponControl>().TargetPhotonViewId
				          == TargetDummy.GetComponent<PhotonView>().ViewID)
			)
		);

		yield return StartCoroutine(
			Step(
				"Weapon Testing",
				string.Join(
					"\n",
					"Guided weapons, like the torpedo mounted on this vehicle, require a target (acquired, not necessarily locked) to fire.",
					"Now that you have the target dummy in your sights, press RMB to fire your torpedo."
				),
				() => TargetDummy.GetDamageHistory().Any(item => item.Type == DamageType.Kinetic)
			)
		);

		PauseSimulation();
		yield return StartCoroutine(
			Step(
				"Point Defense",
				string.Join(
					"\n",
					"You may have noticed that some of your flak cannons weren't firing at all. This is because they are bound to the point defense (PD) weapon group.",
					"PD weapons aren't under the control of any manual weapon groups. In the game, certain projectiles can be vulnerable to PD. When they enter the proximity of your vehicle, PD weapons will automatically track and fire on those projectiles.",
					"For demonstration purposes, when you press continue, this tutorial will spawn a torpedo to the right and heading toward your vehicle."
				), null
			)
		);

		UnpauseSimulation();

		var vehicle = VehicleSpawner.Vehicle.GetComponent<Rigidbody2D>();

		// Reflection hell as this part cannot reference CoreMod in compile time.
		var guidanceSpecType =
			Type.GetType("Syy1125.OberthEffect.CoreMod.Weapons.GuidanceSystem.PredictiveGuidanceSystemSpec")!;
		var predictiveGuidance = Activator.CreateInstance(guidanceSpecType);

		guidanceSpecType.GetField("MaxAcceleration").SetValue(predictiveGuidance, 10f);
		guidanceSpecType.GetField("MaxAngularAcceleration").SetValue(predictiveGuidance, 45f);
		guidanceSpecType.GetField("ThrustActivationDelay").SetValue(predictiveGuidance, 0.1f);
		guidanceSpecType.GetField("GuidanceActivationDelay").SetValue(predictiveGuidance, 0f);
		guidanceSpecType.GetField("PropulsionParticles").SetValue(
			predictiveGuidance, new[]
			{
				new ParticleSystemSpec
				{
					Offset = new Vector2(0f, -0.7f),
					Direction = Vector2.down,
					Size = 0.16f,
					MaxSpeed = 10f,
					EmissionRateOverTime = 200f,
					Lifetime = 0.2f,
					Color = "orange"
				}
			}
		);

		GameObject missile = NetworkedProjectileManager.Instance.CreateProjectile(
			vehicle.worldCenterOfMass + new Vector2(80f, 0f),
			Quaternion.AngleAxis(90f, Vector3.forward),
			vehicle.velocity,
			new()
			{
				ColliderSize = new(0.16f, 1.4f),
				Damage = 10f,
				DamageType = DamageType.Kinetic,
				ArmorPierce = 1f,
				Lifetime = 60f,

				PointDefenseTarget = new()
				{
					MaxHealth = 10f,
					ArmorValue = 1f,
				},
				HealthDamageScaling = 0.75f,

				ProjectileComponents = new() { { "PredictiveGuidance", predictiveGuidance } },

				Renderers = new[]
				{
					new RendererSpec
					{
						TextureId = MissileTextureId,
						Scale = Vector2.one
					}
				},
			}
		);

		var guidanceSystemType =
			Type.GetType("Syy1125.OberthEffect.CoreMod.Weapons.GuidanceSystem.PredictiveGuidanceSystem");
		guidanceSystemType.GetMethod("SetTargetId", BindingFlags.NonPublic).Invoke(
			missile.GetComponent(guidanceSystemType), new object[] { vehicle.GetComponent<PhotonView>().ViewID }
		);
		missile.GetComponent<PointDefenseTarget>().TutorialSetOwnerOverride(-1);

		yield return StartCoroutine(
			Step(
				"Point Defense",
				"Now watch as your point defense cannons destroy the incoming torpedo.",
				() => missile == null || !missile.gameObject.activeSelf
			)
		);

		Highlight(MinimapFrame);
		yield return StartCoroutine(
			Step(
				"Minimap",
				"The lower right corner of your HUD contains the minimap, to help you keep track of the strategic situation.\nYou can use the numpad plus and minus keys to zoom in and out the minimap.",
				null
			)
		);

		Highlight(null);
		yield return StartCoroutine(
			FinalStep(
				"Conclusion",
				"That concludes the gameplay tutorial. Armed with this knowledge, you are now ready to explore the rest of Oberth Effect!",
				false
			)
		);
	}

	#endregion
}
}