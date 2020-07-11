using UnityEngine;
using UnityEngine.InputSystem;

public class SquiggleManager : MonoBehaviour
{
	// how do we build a new squiggle?
	public GameObject squigglePrefab;
	// the maximum amount of squiggles we allow
	public int maximumSquiggles = 5;
	// how long does it take to die?
	public static float deathDelay = 1.0f;

	void Update()
	{
		if (Pointer.current.press.wasPressedThisFrame)
		{
			Vector2 position = Pointer.current.position.ReadValue();
			// make sure we're inside the screen
			if (position.x > 0 && position.y > 0 &&  position.x < Screen.width &&  position.y < Screen.height)
			{
				OnPressed();
			}
		}
	}

	void OnPressed()
	{
		// Instantiate a new Squiggle (it will take care of Pointer/Pen values itself)
		GameObject newSquiggle = Instantiate(squigglePrefab);
		// make it a child of the Manager
		newSquiggle.transform.parent = this.transform;
		newSquiggle.name = "Squiggle";
		// fifo: remove when there are too many squiggles
		if (transform.childCount > maximumSquiggles)
		{
			Fifo();
		}

	}

	/// remove oldest Squiggle that is not already dying
	void Fifo()
	{
		for (int i = 0; i < transform.childCount; i++)
		{
			// if there is already an older dying sibling, move on to next oldest
			if (transform.GetChild(i).GetComponent<Squiggle>().dying) continue;
			// tell the oldest to die
			transform.GetChild(i).GetComponent<Squiggle>().Die();
			// send it to it's deathbed
			Destroy(transform.GetChild(i).gameObject, deathDelay);
			// all done
			break;
		}
	}

}