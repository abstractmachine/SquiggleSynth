using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;
using TMPro;

/// <summary>
/// Squiggle is a component that draws a polyline using incoming points from pen or mouse pointers
/// Lines are drawn using Freya Holmér's Shapes package
/// At the end of the drawing period, path analysis is sent out to InteractML to determine path type
/// </summary>
public class Squiggle : MonoBehaviour
{
	// line thickness
	[Range(0.001f, 1.0f)]
	[SerializeField] float thickness = 0.05f;

	public TMP_Text labelTextMesh;

	public float label = -1.0f;

	SquiggleAudio squiggleAudio;
	SquiggleManager squiggleManager;

	// the actual path
	private PolylinePath path;
	List<Vector3> points = new List<Vector3>();
	List<Vector3> deltas = new List<Vector3>();

	// used for animating line
	private float sinSpeed = 20.0f;
	private float radius = 0.1f;

	Bounds bounds;
	Vector3 center = Vector3.zero;

	// we use this to give an effect of progressively removing the oldest points at death
	private int startingIndex = 0;

	[HideInInspector]
	public int fingerId = -1;
	[HideInInspector]
	public bool mouseClick = false;
	[HideInInspector]
	public bool drawing = true;
	[HideInInspector]
	public bool playing = false;
	[HideInInspector]
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
		squiggleAudio = GetComponent<SquiggleAudio>();
		squiggleManager = transform.parent.GetComponent<SquiggleManager>();

		Vector3 position = FindStartingPoint();

		path = new PolylinePath();
		path.AddPoint(position.x, position.y);
		points.Add(position);
		deltas.Add(Vector3.zero);

		StartCoroutine(DrawLine());
	}

	public void OnRender(Camera cam)
	{
		// make sure we have a path and that it has at least 2 points
		if (path != null && path.Count > 1)
		{
			PolylinePath tempPath = new PolylinePath();
			for (int i = Mathf.Max(1,startingIndex); i < path.Count; ++i)
			{
				// extract this point
				Vector3 previousPoint = path[i-1].point;
				Vector3 currentPoint = path[i].point;
				// if we're not drawing
				if (!drawing)
				{
					float value = 0.0f;
					// animate the path
					float pct = i / (float) path.Count;
					// calculate arc-tangent from delta
					// FIXME: I would have assumed the inverse current-previous for the ATAN2, hmmm....
					Vector3 deltaPoint = previousPoint - currentPoint;
					float atan = Mathf.Atan2(deltaPoint.y, deltaPoint.x);
					float sin = Mathf.Sin(atan);
					float cos = Mathf.Cos(atan);
					// figture out the index
					int spectrumIndex = (int)(pct * squiggleAudio.logSpectrum.Length);
					// use spectrum for radius
					value = squiggleAudio.logSpectrum[spectrumIndex];
					// FIXME: do a proper FFT normalization
					float attenuation = 0.025f;
					// FIXME: replace random with a proper FFT using spectrum value
					float radius = Random.Range(-value*attenuation, value*attenuation);
					// float radius = value * attenuation;
					// apply to point
					currentPoint.x += radius * sin;
					currentPoint.y += radius * cos;
				}
				tempPath.AddPoint(currentPoint);

				// draw the label
			}
			// make sure we have more than one point
			if (tempPath.Count > 1)
			{
				// draw the line
				Draw.LineEndCaps = LineEndCap.Round;
				Draw.Polyline(tempPath, closed : false, thickness : thickness, Color.white);
			}
		}
	}


	Vector3 FindStartingPoint()
	{
		// if this was a mousePress
		if (mouseClick)
		{
			return ScreenToWorld(Input.mousePosition);
		}

		// if this was a pen press
		for (int i = 0; i < Input.touchCount; i++)
		{
			// if this is the right index
			if (Input.touches[i].fingerId == fingerId)
			{ // return this position in world space
				return ScreenToWorld(Input.touches[i].position);
			}
		}

		// we found neither the mousePosition (not mouse click), nor the fingerId
		Debug.LogError("Error creating next Squiggle (fingerId not found");
		return Vector3.zero;
	}

	IEnumerator DrawLine()
	{
		bool movementDone = false;
		// keep looping while the pointer press in still active
		while (!movementDone)
		{
			movementDone = CheckForMovementDone();
			// wait for the next frame
			yield return new WaitForEndOfFrame();
		}

		// ok we're done drawing
		drawing = false;
		// analyze the results
		AnalyzePolyline();
		// show onscreen
		PositionLabel();
	}

	bool CheckForMovementDone()
	{
		// if this is a mouse click
		if (mouseClick)
		{
			// get the last position
			Vector3 lastPoint = path.LastPoint.point;
			// get the new position
			Vector3 position = ScreenToWorld(Input.mousePosition);
			// compare the two
			Vector3 delta = new Vector3(position.x - lastPoint.x, position.y - lastPoint.y, 0.0f);
			// check magnitude
			if (delta.magnitude > 0.0f)
			{ // add point
				path.AddPoint(position.x, position.y);
				points.Add(position);
				deltas.Add(delta);
			}
			// is there a mouse click up event? (can't use Up because this is in an IFrame cycle)
			if (!Input.GetMouseButton(0))
			{
				return true;
			}
			// otherwise, no movement
			return false;
		}

		for (int i = 0; i < Input.touchCount; i++)
		{
			// if this isn't the right index
			if (Input.touches[i].fingerId != fingerId) continue;
			// get the last position
			Vector3 lastPoint = path.LastPoint.point;
			// get the position of this touch, adapt point to screen space
			Vector3 position = ScreenToWorld(Input.touches[i].position);
			// compare the two
			Vector3 delta = new Vector3(position.x - lastPoint.x, position.y - lastPoint.y, 0.0f);
			// if we've moved the pointer
			if (Input.touches[i].deltaPosition.magnitude > 0.0f)
			{
				path.AddPoint(position.x, position.y);
				points.Add(position);
				deltas.Add(delta);
			}
			// check to see if we've finished drawing
			if (Input.touches[i].phase == TouchPhase.Ended ||  Input.touches[i].phase == TouchPhase.Canceled)
			{
				return true;
			}
		}

		return false;
	}

	void AnalyzePolyline()
	{
		CalculateCenter();

		SendPositions();

		// FIXME: for now this is just randomly generated, without waiting for the response from InteractML
		ReceivedLabel((int)Random.Range(0,3));

	}


	/// <summary>
	/// Send the positions InteractML
	/// </summary>
	void SendPositions()
	{
		// find the data out object
		SquiggleDataOut squiggleDataOut = squiggleManager.GetComponent<SquiggleDataOut>();

		squiggleDataOut.SendPositions(deltas.ToArray());
	}


	public void ReceivedLabel(float newLabel)
	{
		label = newLabel;

		// now that we have a label, we can create the audio (with it's type)
		CreateAudio();
	}


	void CreateAudio()
	{
		float frequency = 220.0f + (220.0f * label);

		squiggleAudio.Create(frequency);

		playing = true;
	}

	void CalculateCenter()
	{
		bounds = GeometryUtility.CalculateBounds(points.ToArray(), transform.localToWorldMatrix);
		center = bounds.center;
	}

	void PositionLabel()
	{
		Transform labelTransform = transform.GetChild(0);
		labelTransform.position = center;
		labelTextMesh.text = label.ToString();
	}

	Vector3 ScreenToWorld(Vector3 point)
	{
		point.z = Mathf.Abs(Camera.main.transform.position.z);
		return Camera.main.ScreenToWorldPoint(point);
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
// class Squiggle