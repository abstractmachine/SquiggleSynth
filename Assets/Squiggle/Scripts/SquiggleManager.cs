using UnityEngine;

public class SquiggleManager : MonoBehaviour
{
	// how do we build a new squiggle?
	public GameObject squigglePrefab;
	// the maximum amount of squiggles we allow
	public int maximumSquiggles = 5;
	// how long does it take to die?
	public static float deathDelay = 1.0f;

	void Start()
	{
		Input.simulateMouseWithTouches = false;
	}

	void Update()
	{
		if (!CheckMouse())
		{
			CheckTouch();
		}
	}

	bool CheckTouch()
	{
		// go through all possible touches
		for (int i = 0; i < Input.touchCount; i++)
		{
			Touch touch = Input.GetTouch(i);

			if (touch.phase == TouchPhase.Began && touch.type == TouchType.Stylus)
			{
				// make sure we're inside the screen
				if (touch.position.x > 0 && touch.position.y > 0 && touch.position.x < Screen.width && touch.position.y < Screen.height)
				{
					OnPressed(touch.fingerId, false);
					return true;
				}
			}
			// if (TouchPhase)
		}
		// for(touchCount)
		return false;
	}

	bool CheckMouse()
	{
		if (Input.GetMouseButtonDown(0))
		{
			OnPressed(-1, true);
			return true;
		}
		return false;
	}

	void OnPressed(int fingerId, bool mouseClick)
	{
		// Instantiate a new Squiggle (it will take care of Pointer/Pen values itself)
		GameObject newSquiggle = Instantiate(squigglePrefab);
		// make it a child of the Manager
		newSquiggle.transform.parent = this.transform;
		newSquiggle.name = "Squiggle";
		// set the fingerId that started this polyline
		newSquiggle.GetComponent<Squiggle>().fingerId = fingerId;
		// if this is not a touch, it's a mouseClick
		newSquiggle.GetComponent<Squiggle>().mouseClick = mouseClick;
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