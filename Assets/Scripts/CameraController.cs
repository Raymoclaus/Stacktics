using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Define camera modes
public enum CameraMode {OrthoFreeMode, OrthoFollowMode, PerspFreeMode, PerspFollowMode, PerspTrackMode};

/* Controls actions that affect the camera */
public class CameraController : MonoBehaviour
{
	/* Fields */
	#region
	//reference to components and external objects
	public Camera cam;
	public CharController charTarget;
	public Transform orthoFreeTarget;

	//used to keep track of current camera mode
	[HideInInspector]
	public CameraMode mode = CameraMode.OrthoFreeMode;

	//ortho/persp transition-related stuff
	private bool transitioning;
	public float transitionTime;
	private float transitionCount = 0f;
	private Vector2 nearFarClippingLimits = new Vector2();

	//orthographic-related stuff
	public enum ScrollMode { EdgeScroll, DragScroll }
	public ScrollMode scrollMode;
	public float orthoScrollPadding, orthoScrollLimit;
	public Vector2 orthoZoomLimits;
	public float zoomSpeed;
	public bool smoothZooming;
	private float orthoZoomTo, orthoZoomCount = 0f, orthoZoomTime = 1f;
	[HideInInspector]
	public Vector3 mapCenter = new Vector3(0f, 45f, 0f);
	private Vector3 orthoRotation = new Vector3(30f, 45f, 0f);
	private Vector3 currentOrthoRotation;
	public float orthoRotationTime;
	private float orthoRotationCount = 0f;
	private Vector3 prevMouseHoldPos, dragOrigin, targetOrigin;
	private float dragDistance, dragTime, dragSpeed = 100f;
	public bool Dragging { get { return dragDistance > 0.05f || dragTime > 1f; } }

	//perspective-related stuff
	public float freeMoveSpeed, freeRotateSpeed;
	public bool invertedX, invertedY;
	public Vector2 rotationLimit;
	private Vector3 currentPerspRotation;
	public float perspFollowDistance;
	private float currentPerspFollowDistance;
	public Vector2 perspFollowDistanceLimits;
	private float perspFollowDistanceCount = 0f, perspFollowDistanceTime = 1f;
	private float transitionToPerspFollowCount = 0f, transitionToPerspFollowTime = 1f;
	public bool TransitioningToPerspFollow
	{
		get
		{
			return transitionToPerspFollowCount < transitionToPerspFollowTime;
		}
	}
	#endregion

	void Start()
	{
		//get references
		cam = GetComponent<Camera>();

		//variable initialising
		nearFarClippingLimits.x = cam.nearClipPlane;
		nearFarClippingLimits.y = cam.farClipPlane;
		orthoZoomTo = cam.orthographicSize;
		currentOrthoRotation = orthoRotation;
		currentPerspRotation = orthoRotation;
		currentPerspFollowDistance = perspFollowDistance;
	}

	private void EarlyUpdate()
	{
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

		//lock cursor if in persp free or persp follow mode
		Cursor.lockState = mode == CameraMode.PerspFreeMode || mode == CameraMode.PerspFollowMode ?
			CursorLockMode.Locked : CursorLockMode.None;

		//only run certain code based on current camera mode
		switch(mode)
		{
		case CameraMode.OrthoFreeMode:
			{
				UpdateOrthoFree();
				break;
			}
		case CameraMode.OrthoFollowMode:
			{
				UpdateOrthoFollow();
				break;
			}
		case CameraMode.PerspFreeMode:
			{
				UpdatePerspFree();
				break;
			}
		case CameraMode.PerspFollowMode:
			{
				UpdatePerspFollow();
				break;
			}
		case CameraMode.PerspTrackMode:
			{
				UpdatePerspTrack();
				break;
			}
		}

		//Update certain variables after everything else to be used in various contexts
		LateUpdate();
  	}

	private void LateUpdate()
	{
		if (Input.GetMouseButtonDown(0))
		{
			dragOrigin = Input.mousePosition;
			targetOrigin = orthoFreeTarget.position;
		}
		if (Input.GetMouseButton(0))
		{
			prevMouseHoldPos = Input.mousePosition;
		}
  	}

	/* OrthoFreeMode Updates */
	#region
	private void UpdateOrthoFree()
	{
		//check input for scrolling, zooming or rotating the camera
		CheckOrthoFreeScroll();
		CheckOrthoFreeZoom();
		CheckOrthoFreeRotate();
	}

	/* Scroll Input related */
	#region
	//Based on the current scroll mode, check certain kinds of input
	private void CheckOrthoFreeScroll()
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
		}
	}

	public void CheckEdgeScrollInput()
	{
		//check left side of the screen
		if (Input.mousePosition.x < orthoScrollPadding)
		{
			Scroll(-orthoFreeTarget.right);
		}
		//check right side of the screen
		if (Input.mousePosition.x > Screen.width - orthoScrollPadding)
		{
			Scroll(orthoFreeTarget.right);
		}
		//check top side of the screen
		if (Input.mousePosition.y > Screen.height - orthoScrollPadding)
		{
			Scroll(orthoFreeTarget.forward);
		}
		//check bottom side of the screen
		if (Input.mousePosition.y < orthoScrollPadding)
		{
			Scroll(-orthoFreeTarget.forward);
		}
	}

	public void CheckDragScrollInput()
	{
		if (Input.GetMouseButton(0) && prevMouseHoldPos != Vector3.zero)
		{
			dragDistance += Vector3.Distance(
				cam.ScreenToViewportPoint(Input.mousePosition), cam.ScreenToViewportPoint(prevMouseHoldPos));
			dragTime += Time.deltaTime;
		}

		if (Dragging)
		{
			orthoFreeTarget.position = targetOrigin;
			Vector3 dir = (cam.ScreenToViewportPoint(Input.mousePosition) - cam.ScreenToViewportPoint(dragOrigin))
				* dragSpeed;
			Vector3 move = Vector3.zero;
			move = orthoFreeTarget.right * -dir.x;
			move += orthoFreeTarget.forward * -dir.y;
			Scroll(move);
		}
	}

	private void Scroll(Vector3 direction)
	{
		//move the camera target
		orthoFreeTarget.position += direction * cam.orthographicSize / 30f;
		//keep it within bounds
		Vector3 distance = orthoFreeTarget.position - mapCenter;
		if (Mathf.Abs(distance.x) > orthoScrollLimit)
		{
			orthoFreeTarget.position -= Vector3.right * (Mathf.MoveTowards(distance.x, 0f, orthoScrollLimit));
		}
		if (Mathf.Abs(distance.z) > orthoScrollLimit)
		{
			orthoFreeTarget.position -= Vector3.forward * (Mathf.MoveTowards(distance.z, 0f, orthoScrollLimit));
		}
		Recenter();
	}
	#endregion

	/* Zoom Input related */
	#region
	private void CheckOrthoFreeZoom()
	{
		//check to see if the mouse wheel is being used
		float mouseScroll = -Input.mouseScrollDelta.y;
		if (mouseScroll != 0)
		{
			//reset zoom Count to allow for smooth scrolling
			orthoZoomCount = 0;
			//apply zoom
			orthoZoomTo += mouseScroll * zoomSpeed;
			//if zoom surpassed the limits then bring it back in line
			if (orthoZoomTo < orthoZoomLimits.x)
			{
				orthoZoomTo = orthoZoomLimits.x;
			}
			if (orthoZoomTo > orthoZoomLimits.y)
			{
				orthoZoomTo = orthoZoomLimits.y;
			}
		}
		//increment zoom counter for smooth scrolling
		orthoZoomCount += Time.deltaTime;
		//if smooth zooming is enabled then gradually zoom to calculated orthographic size
		if (smoothZooming)
		{
			cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthoZoomTo, orthoZoomCount / orthoZoomTime);
		}
		//otherwise just set the zoom to what it should be
		else
		{
			cam.orthographicSize = orthoZoomTo;
		}
	}
	#endregion

	/* Rotation Input related */
	#region
	private void CheckOrthoFreeRotate()
	{
		//increment counter
		orthoRotationCount += Time.deltaTime;
		//check input
		if (!transitioning)
		{
			if (Input.GetKeyDown(KeyCode.A))
			{
				orthoRotation.y += 90f;
				orthoRotationCount = 0f;
			}
			if (Input.GetKeyDown(KeyCode.D))
			{
				orthoRotation.y -= 90f;
				orthoRotationCount = 0f;
			}
		}
		//apply rotation and readjust position to always look at center after rotating
		if (transform.eulerAngles != orthoRotation)
		{
			currentOrthoRotation.y = Mathf.Lerp(currentOrthoRotation.y, orthoRotation.y, orthoRotationCount / orthoRotationTime);
			if (currentOrthoRotation.y < -135f)
			{
				currentOrthoRotation.y += 360f;
				orthoRotation.y += 360f;
				orthoFreeTarget.eulerAngles += Vector3.up * 360F;
			}
			if (currentOrthoRotation.y >= 225f)
			{
				currentOrthoRotation.y -= 360f;
				orthoRotation.y -= 360f;
				orthoFreeTarget.eulerAngles -= Vector3.up * 360F;
			}
			orthoFreeTarget.eulerAngles = Vector3.up * currentOrthoRotation.y;
			Recenter();
		}
	}
	#endregion
	#endregion

	/* OrthoFollowMode Updates */
	#region
	private void UpdateOrthoFollow()
	{
		
	}
	#endregion

	/* PerspFreeMode Updates */
	#region
	private void UpdatePerspFree()
	{
		//check input for moving, rotating and zooming the camera
		CheckPerspFreeMove();
		CheckPerspFreeRotate();
	}

	/* Move Input related */
	#region
	private void CheckPerspFreeMove()
	{
		if (Input.GetKey(KeyCode.W))
		{
			transform.position += transform.forward * Time.deltaTime * freeMoveSpeed;
		}
		if (Input.GetKey(KeyCode.A))
		{
			transform.position -= transform.right * Time.deltaTime * freeMoveSpeed;
		}
		if (Input.GetKey(KeyCode.S))
		{
			transform.position -= transform.forward * Time.deltaTime * freeMoveSpeed;
		}
		if (Input.GetKey(KeyCode.D))
		{
			transform.position += transform.right * Time.deltaTime * freeMoveSpeed;
		}
	}
	#endregion

	/* Rotate Input related */
	#region
	private void CheckPerspFreeRotate()
	{
		//check to see how far the mouse has moved
		Vector2 mouseMove = cam.ScreenToViewportPoint(new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y")));

		//flip rotation based on inverted settings
		mouseMove.x *= invertedX ? -1f : 1f;
		mouseMove.y *= invertedY ? -1f : 1f;

		//adjust rotation speed
		mouseMove *= freeRotateSpeed;

		//add horizontal mouse movements to Y rotation and vertical mouse movements to X rotation
		currentPerspRotation.x += mouseMove.y;
		currentPerspRotation.y += mouseMove.x;

		//limit the vertical rotation angle
		if (currentPerspRotation.x < rotationLimit.x)
		{
			currentPerspRotation.x = rotationLimit.x;
		}
		if (currentPerspRotation.x > rotationLimit.y)
		{
			currentPerspRotation.x = rotationLimit.y;
		}

		//apply rotation
		transform.eulerAngles = currentPerspRotation;
	}
           	#endregion
	#endregion

	/* PerspFollowMode Updates */
	#region
	private void UpdatePerspFollow()
	{
		//get position to begin manipulating
		Vector3 pos = transform.position;

		//check if still transitioning
		if (TransitioningToPerspFollow)
		{
			//get rotation
			currentPerspRotation = transform.eulerAngles;
			if (currentPerspRotation.x > rotationLimit.y)
			{
				currentPerspRotation.x -= 360f;
			}
			//increment counter for transition
			transitionToPerspFollowCount += Time.deltaTime;
			//lerp rotation
			currentPerspRotation.y = Mathf.Lerp(currentPerspRotation.y, charTarget.rotation.y, transitionToPerspFollowCount / transitionToPerspFollowTime);
			//apply calculated rotation
			transform.eulerAngles = currentPerspRotation;
			//lerp position
			pos = Vector3.Lerp(pos, charTarget.camLookAt.position - transform.forward * perspFollowDistance,
				transitionToPerspFollowCount / transitionToPerspFollowTime);
			//apply calculated position
			transform.position = pos;
		}
		else
		{
			//camera can only start rotating left/right if the transition is finished
			charTarget.cameraFollowing = true;
			//check for rotation input
			CheckPerspFollowRotationInput();
			//have y rotation match the target's
			currentPerspRotation.y = charTarget.rotation.y;
			//apply calculated rotation
			transform.eulerAngles = currentPerspRotation;
			//check for follow distance changes
			CheckPerspFollowZoomInput();
		}
	}

	/* Persp Follow Distance related */
	#region
	private void CheckPerspFollowZoomInput()
	{
		//check to see if the mouse wheel is being used
		float mouseScroll = -Input.mouseScrollDelta.y;
		if (mouseScroll != 0)
		{
			//reset zoom Count to allow for smooth scrolling
			perspFollowDistanceCount = 0;
			//apply zoom
			perspFollowDistance += mouseScroll * zoomSpeed;
			//if zoom surpassed the limits then bring it back in line
			if (perspFollowDistance < perspFollowDistanceLimits.x)
			{
				perspFollowDistance = perspFollowDistanceLimits.x;
			}
			if (perspFollowDistance > perspFollowDistanceLimits.y)
			{
				perspFollowDistance = perspFollowDistanceLimits.y;
			}
		}
		//increment zoom counter for smooth scrolling
		perspFollowDistanceCount += Time.deltaTime;
		//if smooth zooming is enabled then gradually zoom to calculated orthographic size
		if (smoothZooming)
		{
			currentPerspFollowDistance = Mathf.Lerp(
				currentPerspFollowDistance, perspFollowDistance, perspFollowDistanceCount / perspFollowDistanceTime);
		}
		//otherwise just set the zoom to what it should be
		else
		{
			currentPerspFollowDistance = perspFollowDistance;
		}

		//move to calculated position
		transform.position = charTarget.camLookAt.position - transform.forward * currentPerspFollowDistance;
	}
	#endregion

	/* Persp Follow Rotation related */
	#region
	private void CheckPerspFollowRotationInput()
	{
		//get vertical mouse input
		Vector2 mouseMove = cam.ScreenToViewportPoint(new Vector2(0f, Input.GetAxis("Mouse Y")));

		//invert input based on settings
		mouseMove.y *= invertedY ? -1f : 1f;

		//adjust rotation speed based on settings
		mouseMove.y *= freeRotateSpeed;

		//calculate rotation
		currentPerspRotation.x -= mouseMove.y;

		//limit the vertical rotation angle
		if (currentPerspRotation.x < rotationLimit.x)
		{
			currentPerspRotation.x = rotationLimit.x;
		}
		if (currentPerspRotation.x > rotationLimit.y)
		{
			currentPerspRotation.x = rotationLimit.y;
		}

		//apply calculated rotation
		transform.eulerAngles = currentPerspRotation;
	}
	#endregion
	#endregion

	/* PerspTrackMode Updates */
	#region
	private void UpdatePerspTrack()
	{
		
	}
	#endregion

	//changes the camera mode
	public void SetMode(CameraMode camMode)
	{
		//sets the mode
		mode = camMode;
		//changes camera's ortho or persp property
		cam.orthographic = mode == CameraMode.OrthoFreeMode || mode == CameraMode.OrthoFollowMode;
		//adjust position and rotation to suit the mode
		if (cam.orthographic)
		{
			currentOrthoRotation = orthoRotation;
			orthoFreeTarget.position = mapCenter;
			Recenter();
		}
	}

	//checks whether user is trying to initiate a transition
	private bool CheckTransition()
	{
		//if already in a transition then return true
		if (transitioning)
		{
			return true;
		}
		//if player is trying to start a transition then start it and return true
		if (Input.GetKeyDown(KeyCode.Alpha1) && mode != CameraMode.OrthoFreeMode)
		{
			if (cam.orthographic)
			{
				SetMode(CameraMode.OrthoFreeMode);
			}
			else
			{
				StartCoroutine(FadeOut(CameraMode.OrthoFreeMode));
				currentPerspRotation = orthoRotation;
				return true;
			}
		}
		if (Input.GetKeyDown(KeyCode.Alpha2) && mode != CameraMode.OrthoFollowMode && charTarget != null)
		{
			if (cam.orthographic)
			{
				SetMode(CameraMode.OrthoFollowMode);
			}
			else
			{
				StartCoroutine(FadeOut(CameraMode.OrthoFollowMode));
				currentPerspRotation = orthoRotation;
				return true;
			}
		}
		if (Input.GetKeyDown(KeyCode.Alpha3) && mode != CameraMode.PerspFreeMode)
		{
			if (charTarget != null)
			{
				charTarget.mode = CharCtrlMode.Waiting;
				charTarget.cameraFollowing = false;
			}

			if (cam.orthographic)
			{
				StartCoroutine(FadeOut(CameraMode.PerspFreeMode));
				orthoRotationCount = orthoRotationTime;
				orthoZoomCount = orthoZoomTime;
				return true;
			}
			else
			{
				SetMode(CameraMode.PerspFreeMode);
				currentPerspRotation = transform.eulerAngles;
				if (currentPerspRotation.x > rotationLimit.y)
				{
					currentPerspRotation.x -= 360f;
				}
			}
		}
		if (Input.GetKeyDown(KeyCode.Alpha4) && mode != CameraMode.PerspFollowMode && charTarget != null)
		{
			transitionToPerspFollowCount = 0f;
			//make sure the target can move
			charTarget.mode = CharCtrlMode.FreeMovement;

			if (cam.orthographic)
			{
				StartCoroutine(FadeOut(CameraMode.PerspFollowMode));
				orthoRotationCount = orthoRotationTime;
				orthoZoomCount = orthoZoomTime;
				return true;
			}
			else
			{
				SetMode(CameraMode.PerspFollowMode);
			}
		}
		if (Input.GetKeyDown(KeyCode.Alpha5) && mode != CameraMode.PerspTrackMode && charTarget != null)
		{
			if (cam.orthographic)
			{
				StartCoroutine(FadeOut(CameraMode.PerspTrackMode));
				orthoRotationCount = orthoRotationTime;
				orthoZoomCount = orthoZoomTime;
				return true;
			}
			else
			{
				SetMode(CameraMode.PerspTrackMode);
			}
		}

		//otherwise return false
		return false;
	}

	/* Transition Effect */
	#region
	private IEnumerator FadeOut(CameraMode mode)
	{
		float halfway = (nearFarClippingLimits.x + nearFarClippingLimits.y) / 2f;
		//start fading out
		transitioning = true;
		transitionCount = 0;
		//loop until the clipping planes match after a certain amount of time
		while (transitionCount < transitionTime * 0.45f)
		{
			//increment counter
			transitionCount += Time.deltaTime;
			//adjust camera's far clip plane
			cam.nearClipPlane = Mathf.Lerp(cam.nearClipPlane, halfway, transitionCount / (transitionTime * 0.45f));
			cam.farClipPlane = Mathf.Lerp(cam.farClipPlane, halfway + 0.001f, transitionCount / (transitionTime * 0.45f));
			//wait until end of frame before continuing loop
			yield return 0;
		}
		//reset counter, change camera mode and start fading back in
		SetMode(mode);

		while (transitionCount < transitionTime * 0.55f)
		{
			transitionCount += Time.deltaTime;
			yield return 0;
		}

		StartCoroutine(FadeIn());
	}

	private IEnumerator FadeIn()
	{
		//start fading in
		transitionCount = 0;
		//loop until the clipping planes match after a certain amount of time
		while (transitionCount < transitionTime * 0.45f)
		{
			//increment counter
			transitionCount += Time.deltaTime;
			//adjust camera's far clip plane
			cam.nearClipPlane = Mathf.Lerp(cam.nearClipPlane, nearFarClippingLimits.x, transitionCount / (transitionTime * 0.45f));
			cam.farClipPlane = Mathf.Lerp(cam.farClipPlane, nearFarClippingLimits.y, transitionCount / (transitionTime * 0.45f));
			//wait until end of frame before continuing loop
			yield return 0;
		}
		//reset counter and start fading back in
		transitioning = false;
	}
	#endregion

	private void Recenter()
	{
		transform.eulerAngles = currentOrthoRotation;
		Vector3 rot = transform.eulerAngles;
		rot.y = orthoFreeTarget.eulerAngles.y;
		transform.eulerAngles = rot;
		transform.position = orthoFreeTarget.position - transform.forward * nearFarClippingLimits.y / 2f;
	}
}
