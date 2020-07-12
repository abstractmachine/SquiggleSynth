using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InteractML;

public class SquiggleDataIn : MonoBehaviour
{
	SquiggleManager squiggleManager;

	[PullFromIMLController]
	public float labelValue = -1;
	
	void Start()
	{
		squiggleManager = GetComponent<SquiggleManager>(); 
	}

	void Update()
	{
		// Do something with data coming in here
	}
}
