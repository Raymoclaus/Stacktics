using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	/* Fields */
	#region
	//reference to the camera component
	private Camera cam;

	//Define camera modes
	public enum CameraMode {Orthographic, LockedPerspective, FreePerspective};
	private CameraMode camMode = CameraMode.Orthographic;
	public CameraMode CamMode
	{
		get { return camMode; }
		private set { camMode = value; }
	}

	//transition-related stuff
	private bool IsTransitioning
	{
		get { return fadingIn || fadingOut; }
	}
	private bool fadingOut, fadingIn;
	public float fadeTime;
	private float fadeCounter = 0f;
	private Vector2 nearFarClippingLimits = new Vector2();

	//reference to camera target
	public Transform target;

	//orthographic scrolling
	public enum ScrollMode { EdgeScroll, DragScroll, InputScroll }
	public ScrollMode scrollMode;
	public float orthoScrollPadding, orthoScrollLimit;
	public Vector2 orthoZoomLimits;
	public bool smoothZooming;
	private float zoomTo, zoomCount = 0f, zoomTime = 1f;
	public Vector3 orthographicCenter = new Vector3(0f, 45f, 0f);
	private Vector2 prevMouseHoldPos;
	private float dragDistance, dragTime;
	public bool Dragging { get { return dragDistance > 20f || dragTime > 1f; } }
	#endregion

	void Start()
	{
		cam = GetComponent<Camera>();
		//get near and far clipping planes
		nearFarClippingLimits.x = cam.nearClipPlane;
		nearFarClippingLimits.y = cam.farClipPlane;
		zoomTo = cam.orthographicSize;
	}

	private void EarlyUpdate()
	{
		
	}

	void Update()
	{
		//Update certain variables before everything else to be used in various contexts
		EarlyUpdate();

		//only run update method if not in the middle of a transition
		if (CheckTransition())
		{
			return;
		}

		switch(camMode)
		{
		case CameraMode.Orthographic:
			{
				UpdateOrthographic();
				break;
			}
		case CameraMode.LockedPerspective:
			{
				UpdateLocked();
				break;
			}
		case CameraMode.FreePerspective:
			{
				UpdateFree();
				break;
			}
		}

		//Update certain variables after everything else to be used in various contexts
		LateUpdate();
  	}

	private void LateUpdate()
	{
		if (Input.GetMouseButton(0))
		{
			prevMouseHoldPos = Input.mousePosition;
		}
		else
		{
			prevMouseHoldPos = Vector2.zero;
		}
	}

	private bool CheckTransition()
	{
		//if already in a transition then return true
		if (IsTransitioning)
		{
			return true;
		}
		//if player is trying to start a transition then start it and return true
		if (Input.GetKeyDown(KeyCode.Space))
		{
			StartCoroutine(FadeOut());
			return true;
		}
		//otherwise return false
		return false;
	}

	/* Orthographic Updates */
	#region
	private void UpdateOrthographic()
	{
		CheckOrthographicScrolling();
		CheckOrthographicZooming();
	}

	/* Scroll Input related */
	#region
	//Based on the current scroll mode, check certain kinds of input
	private void CheckOrthographicScrolling()
	{
		switch (scrollMode)
		{
		case ScrollMode.EdgeScroll:
			{
				CheckEdgeScrollInput();
				break;
			}
		case ScrollMode.DragScroll:
			{
				CheckDragScrollInput();
				break;
			}
		case ScrollMode.InputScroll:
			{
				CheckInputScrollInput();
				break;
			}
		}
	}

	public void CheckEdgeScrollInput()
	{
		//check left side of the screen
		if (Input.mousePosition.x < orthoScrollPadding)
		{
			Scroll(270f, 1f);
		}
		//check right side of the screen
		if (Input.mousePosition.x > Screen.width - orthoScrollPadding)
		{
			Scroll(90f, 1f);
		}
		//check top side of the screen
		if (Input.mousePosition.y > Screen.height - orthoScrollPadding)
		{
			Scroll(0f, 1f);
		}
		//check bottom side of the screen
		if (Input.mousePosition.y < orthoScrollPadding)
		{
			Scroll(180f, 1f);
		}
	}

	public void CheckDragScrollInput()
	{
		float dragDistanceThisFrame = 0f;
		if (Input.GetMouseButton(0))
		{
			if (prevMouseHoldPos != Vector2.zero)
			{
				dragDistanceThisFrame = Vector3.Distance(Input.mousePosition, prevMouseHoldPos);
				dragDistance += dragDistanceThisFrame;
				dragTime += Time.deltaTime;
			}
		}
		else
		{
			dragDistance = 0;
			dragTime = 0;
		}

		if (Dragging)
		{
			float angle = Vector2.Angle(Vector2.up, (Vector2)Input.mousePosition - prevMouseHoldPos + Vector2.up);
			//alter to 360 degree rotation
			if (Input.mousePosition.x < prevMouseHoldPos.x)
			{
				angle = 180f + (180f - angle);
			}
			Scroll(angle + 180f, dragDistanceThisFrame / 18f);
		}
	}

	public void CheckInputScrollInput()
	{
		//check left key
		if (Input.GetKey(KeyCode.LeftArrow))
		{
			Scroll(270f, 1f);
		}
		//check right key
		if (Input.GetKey(KeyCode.RightArrow))
		{
			Scroll(90f, 1f);
		}
		//check up key
		if (Input.GetKey(KeyCode.UpArrow))
		{
			Scroll(0f, 1f);
		}
		//check down key
		if (Input.GetKey(KeyCode.DownArrow))
		{
			Scroll(180f, 1f);
		}
	}

	//moves the camera in a given direction altered by the camera's rotation
	private void Scroll(float angle, float speed)
	{
		angle += transform.eulerAngles.y;
		//determine direction to scroll in
		Vector3 scroll = new Vector3(Mathf.Sin(Mathf.Deg2Rad * (angle)), 0f,  Mathf.Cos(Mathf.Deg2Rad * (angle)));
		scroll.Normalize();
		scroll *= speed;
		//apply direction
		transform.position += (Vector3)scroll * cam.orthographicSize / 30F;
		//check to see if camera has reached scroll limit
		while (Vector3.Distance(orthographicCenter, transform.position) > orthoScrollLimit)
		{
			transform.position = Vector3.MoveTowards(transform.position, orthographicCenter, 0.1f);
		}
	}
	#endregion

	/* Zoom Input related */
	#region
	private void CheckOrthographicZooming()
	{
		//check to see if the mouse wheel is being used
		float mouseScroll = -Input.mouseScrollDelta.y;
		if (mouseScroll != 0)
		{
			//reset zoom Count to allow for smooth scrolling
			zoomCount = 0;
			//only apply the scroll if the zoom would remain within limits
			if (zoomTo + mouseScroll >= orthoZoomLimits.x &&
				zoomTo + mouseScroll <= orthoZoomLimits.y)
			{
				zoomTo += mouseScroll;
			}
		}
		//increment zoom counter for smooth scrolling
		zoomCount += Time.deltaTime;
		//if smooth zooming is enabled then gradually zoom to calculated orthographic size
		if (smoothZooming)
		{
			cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomTo, zoomCount / zoomTime);
		}
		//otherwise just set the zoom to what it should be
		else
		{
			cam.orthographicSize = zoomTo;
		}
	}
	#endregion
	#endregion

	/* Locked Perspective Updates */
	#region
	private void UpdateLocked()
	{
		
	}
	#endregion

	/* Free Perspective Updates */
	#region
	private void UpdateFree()
	{
		
	}
	#endregion

	//changes the camera mode
	public void SetMode(CameraMode mode)
	{
		camMode = mode;
		switch (camMode)
		{
		case CameraMode.Orthographic:
			{
				cam.orthographic = true;
				break;
			}
		case CameraMode.LockedPerspective:
			{
				cam.orthographic = false;
				break;
			}
		case CameraMode.FreePerspective:
			{
				cam.orthographic = false;
				break;
			}
		}
	}

	/* Transition Effect */
	#region
	private IEnumerator FadeOut()
	{
		float halfway = (nearFarClippingLimits.x + nearFarClippingLimits.y) / 2f;
		//start fading out
		fadingOut = true;
		fadeCounter = 0;
		//loop until the clipping planes match after a certain amount of time
		while (fadeCounter < fadeTime / 2f)
		{
			//increment counter
			fadeCounter += Time.deltaTime;
			//adjust camera's far clip plane
			cam.nearClipPlane = Mathf.Lerp(cam.nearClipPlane, halfway, fadeCounter / (fadeTime / 2f));
			cam.farClipPlane = Mathf.Lerp(cam.farClipPlane, halfway + 0.01f, fadeCounter / (fadeTime / 2f));
			//wait until end of frame before continuing loop
			yield return 0;
		}
		//reset counter, change camera mode and start fading back in
		fadingOut = false;
		if (cam.orthographic)
		{
			if (target != null)
			{
				SetMode(CameraMode.LockedPerspective);
			}
			else
			{
				SetMode(CameraMode.FreePerspective);
			}
		}
		else
		{
			SetMode(CameraMode.Orthographic);
		}
		StartCoroutine(FadeIn());
	}

	private IEnumerator FadeIn()
	{
		//start fading in
		fadingIn = true;
		fadeCounter = 0;
		//loop until the clipping planes match after a certain amount of time
		while (fadeCounter < fadeTime / 2f)
		{
			//increment counter
			fadeCounter += Time.deltaTime;
			//adjust camera's far clip plane
			cam.nearClipPlane = Mathf.Lerp(cam.nearClipPlane, nearFarClippingLimits.x, fadeCounter / (fadeTime / 2f));
			cam.farClipPlane = Mathf.Lerp(cam.farClipPlane, nearFarClippingLimits.y, fadeCounter / (fadeTime / 2f));
			//wait until end of frame before continuing loop
			yield return 0;
		}
		//reset counter and start fading back in
		fadingIn = false;
	}
	#endregion
}
