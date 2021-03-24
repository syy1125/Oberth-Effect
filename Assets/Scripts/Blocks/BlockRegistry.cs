using System;
using UnityEngine;

public class BlockRegistry : MonoBehaviour
{
	public static BlockRegistry Instance { get; private set; }

	public GameObject[] Blocks;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
		}
	}
}