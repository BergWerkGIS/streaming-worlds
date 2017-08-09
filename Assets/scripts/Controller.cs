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


	private Plane _groundPlane;
	private GameObject _root;
	private bool _creatingTiles = false;
	private MapboxAccess _mbxAccess;
	private float _previousY = float.MinValue;

	private GameObject _DEBUG_cameraCenterRayHitPnt;
	private GameObject _DEBUG_cameraLLRayHitPnt;
	private GameObject _DEBUG_cameraURRayHitPnt;
	private float _DEBUG_hitPointScale = 20f;

	// Use this for initialization
	void Start()
	{
		if (null == _referenceCamera)
		{
			Debug.LogError("no reference camera");
			return;
		}

		_mbxAccess = MapboxAccess.Instance;

		_unityTileScale = _unityTileScale == 0 ? 400 : _unityTileScale;
		_maxZoomLevel = _maxZoomLevel == 0 ? 10 : _maxZoomLevel;
		_cameraZoomingRangeMaxY = _cameraZoomingRangeMaxY == 0 ? 1000 : _cameraZoomingRangeMaxY;
		_cameraZoomingRangeMinY = _cameraZoomingRangeMinY == 0 ? 500 : _cameraZoomingRangeMinY;


		Vector3 localPosition = _referenceCamera.transform.position;
		localPosition.y = (_cameraZoomingRangeMaxY + _cameraZoomingRangeMinY) / 2;
		_referenceCamera.transform.localPosition = localPosition;

		_groundPlane = new Plane(Vector3.up, 0);

		Hud.text = "Start";
		_root = new GameObject("root");

		loadTiles(
			new Vector3(-100, 0, -100)
			, new Vector3(100, 0, 100)
			, _currentZoomLevel
		);
	}

	void Update()
	{

		Ray rayLL = _referenceCamera.ViewportPointToRay(new Vector3(0, 0));
		Ray rayUR = _referenceCamera.ViewportPointToRay(new Vector3(1, 1));
		Ray rayCenter = _referenceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

		Vector3 hitPntLL = getGroundPlaneHitPoint(rayLL);
		Vector3 hitPntUR = getGroundPlaneHitPoint(rayUR);
		Vector3 hitPntCenter = getGroundPlaneHitPoint(rayCenter);

		float y = _referenceCamera.transform.localPosition.y;
		//no camera movement
		if (y == _previousY) { return; }
		_previousY = y;

		try
		{
			if (_creatingTiles) { return; }

			//camera moves within one zoom level, don't do anything
			if (y > _cameraZoomingRangeMinY & y < _cameraZoomingRangeMaxY) { return; }

			Vector3 localPosition = _referenceCamera.transform.position;
			//close to ground, zoom in
			if (y < _cameraZoomingRangeMinY)
			{
				//already at highest level, don't do anything
				if (_currentZoomLevel == _maxZoomLevel) { return; }
				_currentZoomLevel++;
				if (0 != localPosition.x || 0 != localPosition.z) { shiftCamera(ref localPosition, true); }
				//use '_currentZoomLevel - 1' for extent to prevent disappearing tiles at the birder
				loadTiles(hitPntLL, hitPntUR, _currentZoomLevel - 1);
				localPosition.y = _cameraZoomingRangeMaxY;
			}
			//arrived at max distance, zoom out
			if (y > _cameraZoomingRangeMaxY)
			{
				//already at lowest level, don't do anything
				if (_currentZoomLevel == _minZoomLevel) { return; }
				_currentZoomLevel--;
				if (0 != localPosition.x || 0 != localPosition.z) { shiftCamera(ref localPosition, false); }
				loadTiles(hitPntLL, hitPntUR, _currentZoomLevel);
				localPosition.y = _cameraZoomingRangeMinY;
			}
			_referenceCamera.transform.localPosition = localPosition;
		}
		finally
		{
			//Debug.DrawRay(rayCenter.origin, rayCenter.direction * groundPlaneDistance, Color.green);

			if (null == _DEBUG_cameraCenterRayHitPnt)
			{
				_DEBUG_cameraCenterRayHitPnt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_DEBUG_cameraCenterRayHitPnt.transform.localScale = new Vector3(_DEBUG_hitPointScale, _DEBUG_hitPointScale, _DEBUG_hitPointScale);
				_DEBUG_cameraCenterRayHitPnt.name = "camera center ray hit point";
			}
			_DEBUG_cameraCenterRayHitPnt.transform.position = hitPntCenter;

			if (null == _DEBUG_cameraLLRayHitPnt)
			{
				_DEBUG_cameraLLRayHitPnt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_DEBUG_cameraLLRayHitPnt.transform.localScale = new Vector3(_DEBUG_hitPointScale, _DEBUG_hitPointScale, _DEBUG_hitPointScale);
				_DEBUG_cameraLLRayHitPnt.name = "camera LL ray hit point";
			}
			_DEBUG_cameraLLRayHitPnt.transform.position = hitPntLL;

			if (null == _DEBUG_cameraURRayHitPnt)
			{
				_DEBUG_cameraURRayHitPnt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_DEBUG_cameraURRayHitPnt.transform.localScale = new Vector3(_DEBUG_hitPointScale, _DEBUG_hitPointScale, _DEBUG_hitPointScale);
				_DEBUG_cameraURRayHitPnt.name = "camera UR ray hit point";
			}
			_DEBUG_cameraURRayHitPnt.transform.position = hitPntUR;


			Hud.text = string.Format(
				"camera.y:{1:0.00} zoom:{2}{0}center:{3}{0}LL:{4}{0}UR:{5}"
				, Environment.NewLine
				, y
				, _currentZoomLevel
				, hitPntCenter
				, hitPntLL
				, hitPntUR
			);
		}
	}


	private void shiftCamera(ref Vector3 localPosition, bool zoomIn)
	{
		if (zoomIn)
		{
			localPosition.x = localPosition.x * 2;
			localPosition.z = localPosition.z * 2;
		}
		else
		{
			localPosition.x = localPosition.x / 2;
			localPosition.z = localPosition.z / 2;
		}
	}


	private Vector3 getGroundPlaneHitPoint(Ray ray)
	{
		float distance;
		if (!_groundPlane.Raycast(ray, out distance)) { return Vector3.zero; }
		return ray.GetPoint(distance);
	}


	private void loadTiles(Vector3 ll, Vector3 ur, int zoom)
	{
		if (_creatingTiles) { return; }

		try
		{
			_creatingTiles = true;

			double factor = Conversions.GetTileScaleInMeters(zoom) * 256 / _unityTileScale;
			Vector2d llWebMerc = new Vector2d(ll.x * factor, ll.z * factor);
			Vector2d llLatLng = Conversions.MetersToLatLon(llWebMerc);
			Vector2d urWebMerc = new Vector2d(ur.x * factor, ur.z * factor);
			Vector2d urLatLng = Conversions.MetersToLatLon(urWebMerc);

			Vector2dBounds requestBounds = new Vector2dBounds(
				llLatLng
				, urLatLng
			);

			Debug.LogFormat("z[{0}] requestBounds:{1} wmLL:{2} latLngLL:{3} wmUR:{4} latLngUR:{5}"
				, _currentZoomLevel
				, requestBounds
				, llWebMerc
				, llLatLng
				, urWebMerc
				, urLatLng
			);

			//TileCover.Get() crashes if there are too many tiles
			HashSet<CanonicalTileId> tilesNeeded = TileCover.Get(requestBounds, _currentZoomLevel);

			if (tilesNeeded.Count > 256)
			{
				Debug.LogWarningFormat("level[{0}] has too many tiles[{1}]: not creating any", _currentZoomLevel, tilesNeeded.Count);
				return;
			}

			//the hard way, just destroy all tiles
			foreach (Transform child in _root.transform) { Destroy(child.gameObject); }


			foreach (var tileId in tilesNeeded)
			{
				Tile tile = new GameObject().AddComponent<Tile>();
				tile.transform.parent = _root.transform;
				tile.Initialize(_mbxAccess, tileId, _unityTileScale);
				tile.SetActive(true);
			}
		}
		finally
		{
			_creatingTiles = false;
		}
	}


}
