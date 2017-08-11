using Mapbox.Examples;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{


	[SerializeField]
	Camera _referenceCamera;

	[SerializeField]
	CameraMovement _CameraMovement;

	[SerializeField]
	public Text Hud;


	[SerializeField]
	public int _cameraZoomingRangeMinY;

	[SerializeField]
	public int _cameraZoomingRangeMaxY;


	[SerializeField]
	public int _maxZoomLevel;

	[SerializeField]
	public int _minZoomLevel;

	[SerializeField]
	public int _currentZoomLevel;

	[SerializeField]
	public float _unityTileScale;


	[HideInInspector]
	public GameObject _root;

	[HideInInspector]
	public Vector2d _center = Vector2d.zero;

	private Plane _groundPlane;
	private bool _creatingTiles = false;
	private MapboxAccess _mbxAccess;
	private float _previousY = float.MinValue;
	private Vector2dBounds _viewPortLatLngBounds;

	private GameObject _DEBUG_cameraCenterRayHitPnt;
	private GameObject _DEBUG_cameraLLRayHitPnt;
	private GameObject _DEBUG_cameraURRayHitPnt;
	private float _DEBUG_hitPointScale;

	// Use this for initialization
	void Start()
	{
		if (null == _referenceCamera) { Debug.LogError("reference camera not set"); return; }
		if (null == _CameraMovement) { Debug.LogError("CameraMovement not set"); return; }

		_mbxAccess = MapboxAccess.Instance;

		_unityTileScale = _unityTileScale == 0 ? 400 : _unityTileScale;
		_maxZoomLevel = _maxZoomLevel == 0 ? 10 : _maxZoomLevel;
		_cameraZoomingRangeMaxY = _cameraZoomingRangeMaxY == 0 ? 1000 : _cameraZoomingRangeMaxY;
		_cameraZoomingRangeMinY = _cameraZoomingRangeMinY == 0 ? 500 : _cameraZoomingRangeMinY;


		_DEBUG_hitPointScale = _unityTileScale * 0.05f;
		_DEBUG_cameraCenterRayHitPnt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		_DEBUG_cameraCenterRayHitPnt.transform.localScale = new Vector3(_DEBUG_hitPointScale, _DEBUG_hitPointScale, _DEBUG_hitPointScale);
		_DEBUG_cameraCenterRayHitPnt.name = "camera center ray hit point";
		_DEBUG_cameraLLRayHitPnt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		_DEBUG_cameraLLRayHitPnt.transform.localScale = new Vector3(_DEBUG_hitPointScale, _DEBUG_hitPointScale, _DEBUG_hitPointScale);
		_DEBUG_cameraLLRayHitPnt.name = "camera LL ray hit point";
		_DEBUG_cameraURRayHitPnt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		_DEBUG_cameraURRayHitPnt.transform.localScale = new Vector3(_DEBUG_hitPointScale, _DEBUG_hitPointScale, _DEBUG_hitPointScale);
		_DEBUG_cameraURRayHitPnt.name = "camera UR ray hit point";




		Vector3 localPosition = _referenceCamera.transform.position;
		localPosition.y = (_cameraZoomingRangeMaxY + _cameraZoomingRangeMinY) / 2;
		_referenceCamera.transform.localPosition = localPosition;

		_groundPlane = new Plane(Vector3.up, 0);

		Hud.text = "Start";
		_root = new GameObject("root");
		_CameraMovement.Controller = this;

		loadTiles(
			_viewPortLatLngBounds
			, _currentZoomLevel
		);
	}

	void Update()
	{
		// rays from camera to groundplane: center, lower left and upper right
		Ray rayCenter = _referenceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
		Ray rayLL = _referenceCamera.ViewportPointToRay(new Vector3(0, 0));
		Ray rayUR = _referenceCamera.ViewportPointToRay(new Vector3(1, 1));

		Vector3 hitPntCenter = getGroundPlaneHitPoint(rayCenter);
		Vector3 hitPntLL = getGroundPlaneHitPoint(rayLL);
		Vector3 hitPntUR = getGroundPlaneHitPoint(rayUR);

		//apply shift (panning)
		//work with Vector2d, double precision to avoid Unity floating point problems
		//Vector2d hitPntShiftedCenter = new Vector2d((hitPntCenter.x + _center.x) * Math.Pow(2, _currentZoomLevel), (hitPntCenter.z + _center.y) * Math.Pow(2, _currentZoomLevel));
		//Vector2d hitPntShiftedLL = new Vector2d((hitPntLL.x + _center.x) * Math.Pow(2, _currentZoomLevel), (hitPntLL.z + _center.y) * Math.Pow(2, _currentZoomLevel));
		//Vector2d hitPntShiftedUR = new Vector2d((hitPntUR.x + _center.x) * Math.Pow(2, _currentZoomLevel), (hitPntUR.z + _center.y) * Math.Pow(2, _currentZoomLevel));
		Vector2d hitPntShiftedCenter = new Vector2d(hitPntCenter.x + (_center.x * Math.Pow(2, _currentZoomLevel)), hitPntCenter.z + (_center.y * Math.Pow(2, _currentZoomLevel)));
		Vector2d hitPntShiftedLL = new Vector2d(hitPntLL.x + (_center.x * Math.Pow(2, _currentZoomLevel)), hitPntLL.z + (_center.y * Math.Pow(2, _currentZoomLevel)));
		Vector2d hitPntShiftedUR = new Vector2d(hitPntUR.x + (_center.x * Math.Pow(2, _currentZoomLevel)), hitPntUR.z + (_center.y * Math.Pow(2, _currentZoomLevel)));
		//Vector2d hitPntShiftedCenter = new Vector2d(hitPntCenter.x + _center.x, hitPntCenter.z + _center.y);
		//Vector2d hitPntShiftedLL = new Vector2d(hitPntLL.x + _center.x, hitPntLL.z + _center.y);
		//Vector2d hitPntShiftedUR = new Vector2d(hitPntUR.x + _center.x, hitPntUR.z + _center.y);

		//calculate factor to get from Unity units to WebMercator meters
		//tile size of 256
		double factor = Conversions.GetTileScaleInMeters(_currentZoomLevel) * 256 / _unityTileScale;

		//convert Unity units to WebMercator and LatLng to get bounding box
		Vector2d llWebMerc = new Vector2d(hitPntShiftedLL.x * factor, hitPntShiftedLL.y * factor);
		Vector2d urWebMerc = new Vector2d(hitPntShiftedUR.x * factor, hitPntShiftedUR.y * factor);
		Vector2d llLatLng = Conversions.MetersToLatLon(llWebMerc);
		Vector2d urLatLng = Conversions.MetersToLatLon(urWebMerc);

		Vector2dBounds currentViewPortLatLngBnds = new Vector2dBounds(
			llLatLng
			, urLatLng
		);

		float y = _referenceCamera.transform.localPosition.y;

		//HACK: using 'ToString()' as Vector2dBounds doesn't (yet?) have an equality operator
		bool bboxChanged = !(_viewPortLatLngBounds.ToString() == currentViewPortLatLngBnds.ToString());

		//no zoom, no pan -> don't do anyhting
		if (y == _previousY && !bboxChanged) { return; }
		_previousY = y;

		try
		{
			if (_creatingTiles) { return; }

			//camera moves within one zoom level, and no panning, don't do anything
			if (
				(y > _cameraZoomingRangeMinY && y < _cameraZoomingRangeMaxY)
				&& !bboxChanged
			)
			{
				Debug.Log("nothing's changed");
				return;
			}
			//if (bboxChanged) { Debug.LogFormat("bbox changed: {0} vs. {1}", _viewPortLatLngBounds, currentViewPortLatLngBnds); }

			_viewPortLatLngBounds = currentViewPortLatLngBnds;

			Vector3 localPosition = _referenceCamera.transform.position;
			//close to ground, zoom in
			if (y < _cameraZoomingRangeMinY)
			{
				//already at highest level, don't do anything
				if (_currentZoomLevel == _maxZoomLevel) { return; }
				_currentZoomLevel++;
				//if (0 != localPosition.x || 0 != localPosition.z) { shiftCamera(ref localPosition, true); }
				//use '_currentZoomLevel - 1' for extent to prevent disappearing tiles at the birder
				loadTiles(_viewPortLatLngBounds, _currentZoomLevel - 1);
				localPosition.y = _cameraZoomingRangeMaxY;
				_referenceCamera.transform.localPosition = localPosition;
			}
			//arrived at max distance, zoom out
			else if (y > _cameraZoomingRangeMaxY)
			{
				//already at lowest level, don't do anything
				if (_currentZoomLevel == _minZoomLevel) { return; }
				_currentZoomLevel--;
				//if (0 != localPosition.x || 0 != localPosition.z) { shiftCamera(ref localPosition, false); }
				loadTiles(_viewPortLatLngBounds, _currentZoomLevel);
				localPosition.y = _cameraZoomingRangeMinY;
				_referenceCamera.transform.localPosition = localPosition;
			}
			else if (bboxChanged)
			{
				loadTiles(_viewPortLatLngBounds, _currentZoomLevel);
			}

		}
		finally
		{
			//Debug.DrawRay(rayCenter.origin, rayCenter.direction * groundPlaneDistance, Color.green);

			_DEBUG_cameraCenterRayHitPnt.transform.position = hitPntCenter;
			_DEBUG_cameraLLRayHitPnt.transform.position = hitPntLL;
			_DEBUG_cameraURRayHitPnt.transform.position = hitPntUR;


			Hud.text = string.Format(
				"camera.y:{1:0.00} zoom:{2}{0}center:{3}{0}shifted:{4}{0}LL:{5}{0}shifted:{6}{0}UR:{7}{0}shifted:{8}{0}WebMerc:{9} / {10}{0}LatLng:{11} / {12}{0}center:{13}"
				, Environment.NewLine
				, y
				, _currentZoomLevel
				, hitPntCenter
				, hitPntShiftedCenter
				, hitPntLL
				, hitPntShiftedLL
				, hitPntUR
				, hitPntShiftedUR
				, string.Format("{0:0.0},{1:0.0}", llWebMerc.x, llWebMerc.y)
				, string.Format("{0:0.0},{1:0.0}", urWebMerc.x, llWebMerc.y)
				, llLatLng
				, urLatLng
				, _viewPortLatLngBounds.Center
			);
		}
	}


	private Vector3 getGroundPlaneHitPoint(Ray ray)
	{
		float distance;
		if (!_groundPlane.Raycast(ray, out distance)) { return Vector3.zero; }
		return ray.GetPoint(distance);
	}


	private void loadTiles(Vector2dBounds LatLngbounds, int zoom)
	{
		if (_creatingTiles) { return; }

		try
		{
			_creatingTiles = true;

			Debug.LogFormat("center:{0} bboxLatLng:{1}", _center, LatLngbounds);

			//TileCover.Get() crashes if there are too many tiles
			HashSet<CanonicalTileId> tilesNeeded = TileCover.Get(LatLngbounds, zoom);

			if (tilesNeeded.Count > 256)
			{
				Debug.LogWarningFormat("level[{0}] has too many tiles[{1}]: not creating any", zoom, tilesNeeded.Count);
				return;
			}

			//the hard way, just destroy all tiles
			foreach (Transform child in _root.transform) { Destroy(child.gameObject); }


			foreach (var tileId in tilesNeeded)
			{
				Tile tile = new GameObject().AddComponent<Tile>();
				tile.transform.parent = _root.transform;
				tile.Initialize(_mbxAccess, tileId, _unityTileScale, _center);
				tile.SetActive(true);
			}
		}
		finally
		{
			_creatingTiles = false;
		}
	}


}
