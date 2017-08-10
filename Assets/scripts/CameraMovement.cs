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

		void LateUpdate()
		{
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
						Controller._center.x += offset.x;
						Controller._center.y += offset.z;

						//Vector3 mapPos = new Vector3(-offset.x, 0, -offset.z);
						//Controller._root.transform.localPosition = mapPos;
						//foreach (Transform child in Controller._root.transform)
						//{
						//	Vector3 newPos = new Vector3(
						//		child.transform.localPosition.x - transform.localPosition.x,
						//		0,
						//		child.transform.localPosition.z - transform.localPosition.z
						//	);
						//	child.transform.localPosition = newPos;
						//}
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
	}
}