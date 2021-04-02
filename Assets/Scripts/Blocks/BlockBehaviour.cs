using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBehaviour : MonoBehaviour
{
	/// <summary>
	/// Anything that spawns a block should be responsible for setting this variable.
	/// Classes that inherit <code>BlockBehaviour</code> should check this variable to see if they should apply physics.
	/// </summary>
	[HideInInspector]
	public bool HasPhysics;
}