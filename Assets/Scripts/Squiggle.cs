using System.Collections;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Squiggle is a component that draws a polyline using incoming points from pen or mouse pointers
/// Lines are drawn using Freya Holmér's Shapes package
/// At the end of the drawing period, path analysis is sent out to determine path type
/// </summary>
public class Squiggle : MonoBehaviour
{
	[Range(0.001f, 1.0f)]
	[SerializeField] float thickness = 0.05f;
	private PolylinePath path;

	float sinSpeed = 20.0f;
	float radius = 0.1f;
	// we use this to give an effect of progressively removing the oldest points
	int startingIndex = 0;

	public bool drawing = true;
	public bool dying = false;

	public void OnEnable()
	{
		// register the callback when enabling object
		Camera.onPostRender += OnRender;
	}

	public void OnDisable()
	{
		// remove the callback when disabling object
		Camera.onPostRender -= OnRender;
	}

	void Start()
	{
		Vector3 position = Pointer.current.position.ReadValue();
		position = ScreenToWorld(position);

		path = new PolylinePath();
		path.AddPoint(position.x, position.y);

		StartCoroutine(DrawLine());
	}

	IEnumerator DrawLine()
	{
		bool wasReleasedThisFrame = false;
		// keep looping while the pointer press in still active
		while (!wasReleasedThisFrame)
		{
			float magnitude = Pointer.current.delta.ReadValue().magnitude;
			// if we've moved the pointer
			if (magnitude > 0.0f)
			{
				// adapt pointer to screen space
				Vector3 position = Pointer.current.position.ReadValue();
				position = ScreenToWorld(position);
				path.AddPoint(position.x, position.y);
			}
			// wait for the next frame
			yield return new WaitForEndOfFrame();
			// check to see if we've finished drawing
			wasReleasedThisFrame = Pointer.current.press.wasReleasedThisFrame;
		}

		// ok we're done drawing
		drawing = false;
		// analyze the results
		AnalyzePolyline();
	}

	void AnalyzePolyline()
	{

	}

	Vector3 ScreenToWorld(Vector3 point)
	{
		point.z = Mathf.Abs(Camera.main.transform.position.z);
		return Camera.main.ScreenToWorldPoint(point);
	}

	public void OnRender(Camera cam)
	{
		// make sure we have a path and that it has at least 2 points
		if (path != null && path.Count > 1)
		{
			PolylinePath tempPath = new PolylinePath();
			for (int i = startingIndex; i < path.Count; ++i)
			{
				// extract this point
				Vector3 point = path[i].point;
				// if we're not drawing
				if (!drawing)
				{
					// animate the path
					float pct = i / (float) path.Count;
					float piOffset = pct * (Mathf.PI * 2);
					float sin = Mathf.Sin((Time.time * sinSpeed) + piOffset);
					float cos = Mathf.Cos((Time.time * sinSpeed) + piOffset);
					point.x += radius * sin;
					point.y += radius * cos;
				}
				tempPath.AddPoint(point);
			}
			// draw the line
			Draw.LineEndCaps = LineEndCap.Round;
			Draw.Polyline(tempPath, closed : false, thickness : thickness, Color.white);
		}
	}

	public void Die()
	{
		dying = true;

		StartCoroutine(Deathmarch());
	}

	IEnumerator Deathmarch()
	{
		// how long to die?
		float delay = 1.0f;
		// figure out the timeStep to remove all points within the delay
		float timeStep = delay / path.Count;

		// remove points
		while (startingIndex < path.Count)
		{
			// use time step to take exactly the amount of time necessary to remove all points
			yield return new WaitForSeconds(timeStep);
			// don't go out of bounds
			if (startingIndex >= path.Count - 1) break;
			else startingIndex++; // increment
		}
	}

	void OnDestroy()
	{
		if (path != null)
		{
			path.Dispose(); // Disposing of mesh data happens here
		}
	}
}