using Mapbox.Examples;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
	public Vector2d _centerWebMerc = Vector2d.zero;


	private int _cameraZoomingRangeMinY;
	private int _cameraZoomingRangeMaxY;
	private Plane _groundPlane;
	private bool _initializingTiles = false;
	private HashSet<CanonicalTileId> _tilesNeeded = new HashSet<CanonicalTileId>();
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
		//don't hardcode? maybe also link to _referenceCamera.fieldOfView ???
		_cameraZoomingRangeMaxY = (int)(_unityTileScale * 2.5f);
		_cameraZoomingRangeMinY = (int)(_unityTileScale * 1.25f);

		//vienna austria
		_centerWebMerc.x = 1827980.66;
		_centerWebMerc.y = 6141386.74;
		//gibraltar
		//_centerWebMerc.x = -626272;
		//_centerWebMerc.y = 4277965;



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



		//put camera into the middle of the allowed y movement range
		Vector3 localPosition = _referenceCamera.transform.position;
		localPosition.y = (_cameraZoomingRangeMaxY + _cameraZoomingRangeMinY) / 2;
		_referenceCamera.transform.localPosition = localPosition;

		_groundPlane = new Plane(Vector3.up, 0);

		Hud.text = "Start";
		_root = new GameObject("root");
		_CameraMovement.Controller = this;
	}


	void Update()
	{

		Vector2dBounds currentViewPortLatLngBnds = getcurrentViewPortInLatLng();

		bool bboxChanged = !(_viewPortLatLngBounds.ToString() == currentViewPortLatLngBnds.ToString());

		float cameraY = _referenceCamera.transform.localPosition.y;

		//no zoom, no pan -> don't change tiles
		if (cameraY == _previousY && !bboxChanged) { return; }
		_previousY = cameraY;

		try
		{

			//camera moves within one zoom level, and no panning, don't do anything
			if (
				(cameraY > _cameraZoomingRangeMinY && cameraY < _cameraZoomingRangeMaxY)
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
			if (cameraY < _cameraZoomingRangeMinY)
			{
				//already at highest level, don't do anything -> camera free to move closer
				if (_currentZoomLevel == _maxZoomLevel) { return; }
				_currentZoomLevel++;
				//reposition camera at max distance
				localPosition.y = _cameraZoomingRangeMaxY;
				_referenceCamera.transform.localPosition = localPosition;
			}
			//arrived at max distance, zoom out
			else if (cameraY > _cameraZoomingRangeMaxY)
			{
				//already at lowest level, don't do anything -> camera free to move further away
				if (_currentZoomLevel == _minZoomLevel) { return; }
				_currentZoomLevel--;
				//reposition camera at min distance
				localPosition.y = _cameraZoomingRangeMinY;
				_referenceCamera.transform.localPosition = localPosition;
			}
			//else if (bboxChanged)
			//{
			//	loadTiles(_viewPortLatLngBounds, _currentZoomLevel);
			//}

			if (_initializingTiles) { return; }

			//update viewport in case it was changed by switching zoom level
			_viewPortLatLngBounds = getcurrentViewPortInLatLng();
			loadTiles(_viewPortLatLngBounds, _currentZoomLevel);
		}
		finally
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(string.Format("camera.y:{0:0.00} zoom:{1}", cameraY, _currentZoomLevel));
			//sb.AppendLine(string.Format("zoom:{0}", _currentZoomLevel));
			//sb.AppendLine(string.Format("center[Unity]:{0}", _centerUnity));
			//sb.AppendLine(string.Format("center[hitPnt]:{0}", hitPntCenter));
			//sb.AppendLine(string.Format("shifted:{0}", hitPntShiftedCenter));
			//sb.AppendLine(string.Format("LL[hitPnt]:{0}", hitPntLL));
			//sb.AppendLine(string.Format("shifted:{0}", hitPntShiftedLL));
			//sb.AppendLine(string.Format("UR[hitPnt]:{0}", hitPntUR));
			//sb.AppendLine(string.Format("shifted:{0}", hitPntShiftedUR));
			//sb.AppendLine(string.Format("WebMerc:{0:0.0},{1:0.0} - {2:0.0},{3:0.0}", llWebMerc.x, llWebMerc.y, urWebMerc.x, llWebMerc.y));
			//sb.AppendLine(string.Format("center[WebMerc]:{0:0.0} / {1:0.0}"
			//	, (llWebMerc.x + urWebMerc.x) / 2d
			//	, (llWebMerc.y + urWebMerc.y) / 2d
			//));
			//sb.AppendLine(string.Format("viewPort[LatLng]:{0}", _viewPortLatLngBounds));
			//sb.AppendLine(string.Format("center[LatLng]:{0:0.0000} / {1:0.0000}", _viewPortLatLngBounds.Center.x, _viewPortLatLngBounds.Center.y));
			sb.AppendLine(string.Format("center[WebMerc]:{0:0.0000} / {1:0.0000}", _centerWebMerc.x, _centerWebMerc.y));

			Hud.text = sb.ToString();
		}
	}


	private Vector3 getGroundPlaneHitPoint(Ray ray)
	{
		float distance;
		if (!_groundPlane.Raycast(ray, out distance)) { return Vector3.zero; }
		return ray.GetPoint(distance);
	}


	private Vector2dBounds getcurrentViewPortInLatLng(bool useGroundPlane = true)
	{
		Vector3 hitPntLL;
		Vector3 hitPntUR;

		if (useGroundPlane)
		{
			// rays from camera to groundplane: lower left and upper right
			Ray rayLL = _referenceCamera.ViewportPointToRay(new Vector3(0, 0));
			Ray rayUR = _referenceCamera.ViewportPointToRay(new Vector3(1, 1));
			hitPntLL = getGroundPlaneHitPoint(rayLL);
			hitPntUR = getGroundPlaneHitPoint(rayUR);
		}
		else
		{
			hitPntLL = _referenceCamera.ViewportToWorldPoint(new Vector3(0, 0, _referenceCamera.transform.localPosition.y));
			hitPntUR = _referenceCamera.ViewportToWorldPoint(new Vector3(1, 1, _referenceCamera.transform.localPosition.y));
		}

		_DEBUG_cameraLLRayHitPnt.transform.position = hitPntLL;
		_DEBUG_cameraURRayHitPnt.transform.position = hitPntUR;

		Vector2d centerLatLng = Conversions.MetersToLatLon(_centerWebMerc);
		//calculate factor to get from Unity units to WebMercator meters, tile size of 256
		double factor = Conversions.GetTileScaleInMeters((float)centerLatLng.y, _currentZoomLevel) * 256 / _unityTileScale;
		//double factor = Conversions.GetTileScaleInMeters((float)_viewPortLatLngBounds.Center.y, _currentZoomLevel);

		//convert Unity units to WebMercator and LatLng to get real world bounding box
		Vector2d llWebMerc = new Vector2d(_centerWebMerc.x + hitPntLL.x * factor, _centerWebMerc.y + hitPntLL.z * factor);
		Vector2d urWebMerc = new Vector2d(_centerWebMerc.x + hitPntUR.x * factor, _centerWebMerc.y + hitPntUR.z * factor);


		return new Vector2dBounds(
			llWebMerc
			, urWebMerc
		);
	}



	private void loadTiles(Vector2dBounds latLngbounds, int zoom)
	{
		if (_initializingTiles) { return; }
		_initializingTiles = true;

		try
		{
			//HashSet<CanonicalTileId> tilesNeeded = TileCover.Get(latLngbounds, zoom);
			HashSet<CanonicalTileId> tilesNeeded = TileCover.GetWithWebMerc(latLngbounds, zoom);

			if (tilesNeeded.Count > 256)
			{
				Debug.LogWarningFormat("level[{0}] has too many tiles[{1}]: not creating any", zoom, tilesNeeded.Count);
				return;
			}

			//check if current viewport needs differnt tiles than the previouse one
			if (
				_tilesNeeded.Except(tilesNeeded).Count() == 0
				&& tilesNeeded.Except(_tilesNeeded).Count() == 0
			)
			{
				return;
			}

			_tilesNeeded = tilesNeeded;


			//quick'n'dirty, just destroy all existing tiles
			foreach (Transform child in _root.transform) { Destroy(child.gameObject); }


			//calculate tile at current center of viewport
			//calculate distance (shift) between center of center tile and center of viewport
			//Vector2d centerLatLng = Conversions.MetersToLatLon(_centerWebMerc);
			//UnwrappedTileId centerTile = TileCover.CoordinateToTileId(centerLatLng, zoom);

			//HACK: switch COORDINATES - there's a bug somewhere with switched x<->y
			Vector2d centerWebMercDUMMY = new Vector2d(_centerWebMerc.y, _centerWebMerc.x);
			UnwrappedTileId centerTile = TileCover.WebMercatorToTileId(centerWebMercDUMMY, zoom);
			//Vector2 centerTileDummy = Conversions.MetersToTile(_centerWebMerc, zoom);
			//UnwrappedTileId centerTile = new UnwrappedTileId(zoom, (int)centerTileDummy.x, (int)centerTileDummy.y);
			Vector2d centerTileCenter = Conversions.TileIdToCenterWebMercator(centerTile.X, centerTile.Y, zoom);
			Vector2d shift = _centerWebMerc - centerTileCenter;
			float factor = Conversions.GetTileScaleInMeters(zoom) * 256 / _unityTileScale;


			foreach (var tileId in tilesNeeded)
			{
				Tile tile = new GameObject().AddComponent<Tile>();
				tile.transform.parent = _root.transform;
				tile.Initialize(_mbxAccess, tileId, _unityTileScale, centerTile, shift, factor);
				tile.SetActive(true);
			}
		}
		finally
		{
			_initializingTiles = false;
		}
	}


}
