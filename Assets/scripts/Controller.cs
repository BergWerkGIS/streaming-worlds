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
	public int _maxZoomLevel;

	[SerializeField]
	public int _minZoomLevel;

	[SerializeField]
	public int _currentZoomLevel;

	[SerializeField]
	public float _unityTileScale;


	private GameObject _root;
	private Vector2dBounds _boundsWorld;
	private Vector2dBounds _boundsSmall;
	private Vector2dBounds _boundsMap;
	private bool _creatingTiles = false;
	private MapboxAccess _mbxAccess;
	private float _previousY = float.MinValue;

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
		//_minZoomLevel = _minZoomLevel == 0 ? 10 : _minZoomLevel;
		//_currentZoomLevel = _currentZoomLevel == 0 ? 4 : _currentZoomLevel;

		Vector3 localPosition = _referenceCamera.transform.position;
		localPosition.y = 750;
		_referenceCamera.transform.localPosition = localPosition;


		Hud.text = "Start";
		_root = new GameObject("root");

		//_boundsWorld = new Vector2dBounds(
		//	new Vector2d(-180, -90)
		//	, new Vector2d(180, 90)
		//);

		_boundsSmall = new Vector2dBounds(
			new Vector2d(-5, -5)
			, new Vector2d(5, 5)
		);

		//_boundsMap = _boundsWorld;
		_boundsMap = _boundsSmall;
		loadTiles();
	}

	void Update()
	{

		float y = _referenceCamera.transform.localPosition.y;
		//no camera movement
		if (y == _previousY) { return; }
		_previousY = y;

		try
		{
			if (_creatingTiles) { return; }

			//camera moves within one zoom level, don't do anything
			if (y > 500 & y < 1000) { return; }

			Vector3 localPosition = _referenceCamera.transform.position;
			if (y < 500)
			{
				//already at highest level, don't do anything
				if (_currentZoomLevel == _maxZoomLevel) { return; }
				_currentZoomLevel++;
				loadTiles();
				localPosition.y = 1000;
			}
			if (y > 1000)
			{
				//already at lowest level, don't do anything
				if (_currentZoomLevel == _minZoomLevel) { return; }
				_currentZoomLevel--;
				loadTiles();
				localPosition.y = 500;
			}
			_referenceCamera.transform.localPosition = localPosition;
		}
		finally
		{
			Vector3 viewPortLL = _referenceCamera.ViewportToWorldPoint(new Vector3(0, 0, _referenceCamera.transform.localPosition.y));
			Vector3 viewPortUR = _referenceCamera.ViewportToWorldPoint(new Vector3(1, 1, _referenceCamera.transform.localPosition.y));

			Ray LLray = _referenceCamera.ViewportPointToRay(new Vector3(0, 0));
			Ray URray = _referenceCamera.ViewportPointToRay(new Vector3(1, 1));
			string raysInfo = "hitpoint[LL]:";
			RaycastHit LLhit;
			if (Physics.Raycast(LLray, out LLhit))
			{
				raysInfo += LLhit.point.ToString() + LLhit.transform.name;
			}
			else { raysInfo += "NADA"; }
			raysInfo += Environment.NewLine + "hitpoint[UR]:";
			RaycastHit URhit;
			if (Physics.Raycast(URray, out URhit))
			{
				raysInfo += URhit.point.ToString() + URhit.transform.name;
			}
			else { raysInfo += "NADA"; }

			Hud.text = string.Format(
				"camera.y:{1:0.00} zoom:{2}{0}viewport:{3}/{4}{0}{5}"
				, Environment.NewLine
				, y
				, _currentZoomLevel
				, viewPortLL
				, viewPortUR
				, raysInfo
			);
		}
	}


	private CanonicalTileId getTileId(Vector3 unityPoint, int zoom)
	{
		int maxTileCount = (int)Math.Pow(2, _currentZoomLevel);
		int shift = maxTileCount / 2;

		int tileIdx = (int)(((unityPoint.x - (_unityTileScale / 2)) / _unityTileScale) + shift);
		int tileIdy = -(int)(((unityPoint.y - (_unityTileScale / 2)) / _unityTileScale) + (shift + 1));
		tileIdy += maxTileCount;

		return new CanonicalTileId(zoom, tileIdx, tileIdy);
	}

	private void loadTiles()
	{
		if (_creatingTiles) { return; }

		try
		{
			_creatingTiles = true;

			Vector3 viewPortLL = _referenceCamera.ViewportToWorldPoint(new Vector3(0, 0, _referenceCamera.transform.localPosition.y));
			Vector3 viewPortUR = _referenceCamera.ViewportToWorldPoint(new Vector3(1, 1, _referenceCamera.transform.localPosition.y));

			CanonicalTileId LLTileId = getTileId(viewPortLL, _currentZoomLevel);
			CanonicalTileId URTileId = getTileId(viewPortUR, _currentZoomLevel);

			//Vector2d sw = Conversions.TileIdToBounds(LLTileId.X, LLTileId.Y, _currentZoomLevel).SouthWest;
			//Vector2d ne = Conversions.TileIdToBounds(URTileId.X, URTileId.Y, _currentZoomLevel).NorthEast;
			Vector2d sw = Conversions.TileIdToBounds(LLTileId).SouthWest;
			Vector2d ne = Conversions.TileIdToBounds(URTileId).NorthEast;

			Vector2dBounds requestBounds = new Vector2dBounds(
				//Conversions.MetersToLatLon(sw)
				//, Conversions.MetersToLatLon(ne)
				sw
				, ne
			);

			Debug.LogFormat("z[{0}] ll tileId:{1}/{2} ur tile id:{3}/{4} requestBounds:{5}"
				, _currentZoomLevel
				, LLTileId.X
				, LLTileId.Y
				, URTileId.X
				, URTileId.Y
				, requestBounds
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
