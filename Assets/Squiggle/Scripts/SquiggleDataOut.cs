using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InteractML;

public class SquiggleDataOut : MonoBehaviour
{
	SquiggleManager squiggleManager;

	[SendToIMLController]
	public Vector3[] positions;

	void Start()
	{
		squiggleManager = GetComponent<SquiggleManager>();
	}


	public void SendPositions(Vector3[] newPositions)
	{
		positions = newPositions;
		
		Debug.Log("Send out array here");
		Debug.Log(newPositions);
	}

}
