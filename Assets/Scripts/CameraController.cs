using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	/* Fields */
	#region
	//reference to components and external objects
	public Camera cam;
	public Transform target;

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

	//orthographic scrolling
	public enum ScrollMode { EdgeScroll, DragScroll, InputScroll }
	public ScrollMode scrollMode;
	public float orthoScrollPadding, orthoScrollLimit;
	public Vector2 orthoZoomLimits;
	public float zoomSpeed;
	public bool smoothZooming;
	private float zoomTo, zoomCount = 0f, zoomTime = 1f;
	public Vector3 orthographicCenter = new Vector3(0f, 45f, 0f);
	public Vector3 CameraCenter
	{
		get
		{
			return orthographicCenter - transform.forward * nearFarClippingLimits.y / 2f;
		}
	}
	private Vector3 prevMouseHoldPos, dragOrigin, cameraDragOrigin;
	private float dragDistance, dragTime, dragSpeed = 100f;
	public bool Dragging { get { return dragDistance > 0.05f || dragTime > 1f; } }
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
		if (Input.GetMouseButtonDown(0))
		{
			dragOrigin = Input.mousePosition;
			cameraDragOrigin = transform.position;
		}
		if (Input.GetMouseButtonUp(0))
		{
			dragDistance = 0;
			dragTime = 0;
			prevMouseHoldPos = Vector3.zero;
		}
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
		if (Input.GetMouseButton(0))
		{
			dragDistance += Vector3.Distance(
				cam.ScreenToViewportPoint(Input.mousePosition), cam.ScreenToViewportPoint(prevMouseHoldPos));
			dragTime += Time.deltaTime;
		}

		if (Dragging)
		{
			float angle = Vector3.Angle(Vector3.up, Input.mousePosition - dragOrigin + Vector3.up);
			//alter to 360 degree rotation
			if (Input.mousePosition.x < dragOrigin.x)
			{
				angle = 180f + (180f - angle);
			}
			transform.position = cameraDragOrigin;
			Scroll(angle + 180f, dragSpeed * Vector3.Distance(cam.ScreenToViewportPoint(Input.mousePosition), cam.ScreenToViewportPoint(dragOrigin)));
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
		Vector3 pos = transform.position;
		if (Mathf.Abs(pos.x - CameraCenter.x) > orthoScrollLimit && pos.x != 0)
		{
			pos.x = CameraCenter.x + orthoScrollLimit * Mathf.Abs(pos.x) / pos.x;
		}
		if (Mathf.Abs(pos.z - CameraCenter.z) > orthoScrollLimit && pos.z != 0)
		{
			pos.z = CameraCenter.z + orthoScrollLimit * Mathf.Abs(pos.z) / pos.z;
		}
		transform.position = pos;
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
			if (zoomTo + mouseScroll * zoomSpeed >= orthoZoomLimits.x &&
				zoomTo + mouseScroll * zoomSpeed <= orthoZoomLimits.y)
			{
				zoomTo += mouseScroll * zoomSpeed;
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

		transform.position = orthographicCenter + transform.forward * -nearFarClippingLimits.y / 2f;
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
