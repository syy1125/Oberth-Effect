using System;
using System.IO;
using Syy1125.OberthEffect.Components.UserInterface;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Components
{
public class ScreenshotHandler : MonoBehaviour
{
	private static ScreenshotHandler _instance;
	private static string _screenshotPath;

	public InputActionReference Screenshot;

	private void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (_instance != this)
		{
			Destroy(gameObject);
			return;
		}

		_screenshotPath = Path.Combine(Application.persistentDataPath, "Screenshots");
	}

	private void OnEnable()
	{
		Screenshot.action.performed += TakeScreenshot;
	}

	private void Start()
	{
		if (!Directory.Exists(_screenshotPath))
		{
			Directory.CreateDirectory(_screenshotPath);
		}
	}

	private void OnDisable()
	{
		Screenshot.action.performed -= TakeScreenshot;
	}

	private void TakeScreenshot(InputAction.CallbackContext context)
	{
		string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fff");
		string filename = $"screenshot_{timestamp}-{Screen.width}x{Screen.height}.png";
		string path = Path.Combine(_screenshotPath, filename);
		Debug.Log($"Taking screenshot {path}");

		ScreenCapture.CaptureScreenshot(path);

		if (ToastManager.Instance != null)
		{
			ToastManager.Instance.CreateToast($"Screenshot saved to {path}");
		}
	}
}
}