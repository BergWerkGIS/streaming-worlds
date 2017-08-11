namespace Mapbox.Examples
{
	using Mapbox.Utils;
	using UnityEngine;

	public class CameraMovement : MonoBehaviour
	{
		[SerializeField]
		float _panSpeed = 20f;

		[SerializeField]
		float _zoomSpeed = 50f;

		[SerializeField]
		Camera _referenceCamera;

		[HideInInspector]
		public Controller Controller;


		Quaternion _originalRotation;
		Vector3 _origin;
		Vector3 _delta;
		bool _shouldDrag;

		void Awake()
		{
			_originalRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

			if (_referenceCamera == null)
			{
				_referenceCamera = GetComponent<Camera>();
				if (_referenceCamera == null)
				{
					throw new System.Exception("You must have a reference camera assigned!");
				}
			}
			transform.localPosition.Set(
				transform.localPosition.x
				, _referenceCamera.farClipPlane
				, transform.localPosition.z
			);
		}

		void LateUpdate_OLD()
		{
			//development short cut: reset center to 0/0 with right click
			if (Input.GetMouseButton(1))
			{
				Controller._center.x = Controller._center.y = 0;
				return;
			}

			var x = 0f;
			var y = 0f;
			var z = 0f;

			if (Input.GetMouseButton(0))
			{
				var mousePosition = Input.mousePosition;
				mousePosition.z = _referenceCamera.transform.localPosition.y;
				_delta = _referenceCamera.ScreenToWorldPoint(mousePosition) - _referenceCamera.transform.localPosition;
				_delta.y = 0f;
				if (_shouldDrag == false)
				{
					_shouldDrag = true;
					_origin = _referenceCamera.ScreenToWorldPoint(mousePosition);
				}
			}
			else
			{
				_shouldDrag = false;
			}

			if (_shouldDrag == true)
			{
				var offset = _origin - _delta;
				offset.y = transform.localPosition.y;

				if (null != Controller)
				{
					if (0 != offset.x && 0 != offset.z)
					{
						Controller._center.x = offset.x;
						Controller._center.y = offset.z;

						//Vector3 mapPos = new Vector3(-offset.x, 0, -offset.z);
						//Controller._root.transform.localPosition = mapPos;
						foreach (Transform child in Controller._root.transform)
						{
							Vector3 newPos = new Vector3(
								child.transform.localPosition.x - (float)Controller._center.x,// transform.localPosition.x,
								0,
								child.transform.localPosition.z - (float)Controller._center.y// transform.localPosition.z
							);
							child.transform.localPosition = newPos;
						}
					}
				}
			}
			else
			{
				x = Input.GetAxis("Horizontal");
				z = Input.GetAxis("Vertical");
				y = -Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
				x = 0;
				z = 0;


				Vector3 localPosition = transform.localPosition + transform.forward * y + (_originalRotation * new Vector3(x * _panSpeed, 0, z * _panSpeed));


				//allow move camera along Y only
				localPosition.x = 0;
				localPosition.z = 0;
				transform.localPosition = localPosition;

			}
		}


		private void LateUpdate()
		{
			//development short cut: reset center to 0/0 with right click
			if (Input.GetMouseButton(1))
			{
				Controller._center.x = Controller._center.y = 0;
				return;
			}


			// zoom
			var y = -Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
			//avoid unnecessary translation
			if (0 != y)
			{
				_referenceCamera.transform.Translate(new Vector3(0, y, 0), Space.World);
				// TODO:
				//current approach doesn't work nicely when camera is tilted
				//maybe move camera so that center of viewport is always at 0/0
				//_referenceCamera.transform.Translate(new Vector3(0, y, 0), Space.Self);
			}

			//pan keyboard
			float xMove = Input.GetAxis("Horizontal");
			float zMove = Input.GetAxis("Vertical");
			if (0 != xMove || 0 != zMove)
			{
				Debug.LogFormat("xMove:{0} zMove:{1}", xMove, zMove);
				Controller._center.x += xMove;
				Controller._center.y += zMove;
			}

			//pan mouse
			if (Input.GetMouseButtonDown(0))
			{
				var mouseDownPosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				mouseDownPosScreen.z = _referenceCamera.transform.localPosition.y;
				_origin = _referenceCamera.ScreenToWorldPoint(mouseDownPosScreen);
				Debug.LogFormat("button down, mousePosScreen:{0} mousePosWorld:{1}", mouseDownPosScreen, _origin);
			}

			if (Input.GetMouseButtonUp(0))
			{
				var mouseUpPosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				//http://answers.unity3d.com/answers/599100/view.html
				mouseUpPosScreen.z = _referenceCamera.transform.localPosition.y;
				var mouseUpPosWorld = _referenceCamera.ScreenToWorldPoint(mouseUpPosScreen);
				Debug.LogFormat("button up, mousePosScreen:{0} mousePosWorld:{1}", mouseUpPosScreen, mouseUpPosWorld);

				//has position changed?
				if (_origin != mouseUpPosWorld)
				{
					var offset = _origin - mouseUpPosWorld;
					if (null != Controller)
					{
						var centerOld = Controller._center;
						Controller._center.x += offset.x;
						Controller._center.y += offset.z;

						Debug.LogFormat("shifting tiles, old center:{0} new center:{1} offset:{2}", centerOld, Controller._center, offset);

						//Vector3 mapPos = new Vector3(-offset.x, 0, -offset.z);
						//Controller._root.transform.localPosition = mapPos;
						//foreach (Transform child in Controller._root.transform)
						//{
						//	Vector3 newPos = new Vector3(
						//		child.transform.localPosition.x - offset.x,
						//		0,
						//		child.transform.localPosition.z - offset.z
						//	);
						//	child.transform.localPosition = newPos;
						//}
					}
				}
			}
		}



	}
}