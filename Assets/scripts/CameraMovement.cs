namespace Mapbox.Examples
{
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using System;
	using UnityEngine;

	public class CameraMovement : MonoBehaviour
	{

		[SerializeField]
		public float _zoomSpeed = 50f;

		[SerializeField]
		Camera _referenceCamera;

		[HideInInspector]
		public Controller Controller;


		private Vector3 _origin;


		void Awake()
		{
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



		private void LateUpdate()
		{
			//development short cut: reset center to 0/0 with right click
			if (Input.GetMouseButton(1))
			{
				Controller._centerWebMerc.x = Controller._centerWebMerc.y = 0;
				return;
			}


			// zoom
			var y = Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
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
				float factor = Conversions.GetTileScaleInMeters(Controller._currentZoomLevel) * 256 / Controller._unityTileScale;
				xMove *= factor;
				zMove *= factor;
				Debug.LogFormat("xMove:{0} zMove:{1}", xMove, zMove);
				Controller._centerWebMerc.x += xMove;
				Controller._centerWebMerc.y += zMove;
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
						float factor = Conversions.GetTileScaleInMeters(Controller._currentZoomLevel) * 256 / Controller._unityTileScale;
						var centerOld = Controller._centerWebMerc;
						Controller._centerWebMerc.x += offset.x * factor;
						Controller._centerWebMerc.y += offset.z * factor;

						Debug.LogFormat("old center:{0} new center:{1} offset:{2}", centerOld, Controller._centerWebMerc, offset);
					}
				}
			}
		}



	}
}