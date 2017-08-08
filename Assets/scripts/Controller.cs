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


	private GameObject _root;
	private Vector2dBounds _boundsWorld;
	private Vector2dBounds _boundsSmall;
	private Vector2dBounds _boundsMap;
	private bool _creatingTiles = false;
	private MapboxAccess _mbxAccess;

	// Use this for initialization
	void Start()
	{
		if (null == _referenceCamera)
		{
			Debug.LogError("no reference camera");
			return;
		}

		_mbxAccess = MapboxAccess.Instance;


		_maxZoomLevel = _maxZoomLevel == 0 ? 10 : _maxZoomLevel;
		//_minZoomLevel = _minZoomLevel == 0 ? 10 : _minZoomLevel;
		_currentZoomLevel = _currentZoomLevel == 0 ? 4 : _currentZoomLevel;

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
			new Vector2d(-5,-5)
			, new Vector2d(5, 5)
		);

		//_boundsMap = _boundsWorld;
		_boundsMap = _boundsSmall;
		loadTiles();
	}

	void Update()
	{

			float y = _referenceCamera.transform.localPosition.y;

		try
		{
			if (_creatingTiles) { return; }

			//camera moves within one zoom level, don't do anything
			if (y > 500 & y < 1000) { return; }

			Vector3 localPosition = _referenceCamera.transform.position;
			if (y <= 500)
			{
				//already at highest level, don't do anything
				if (_currentZoomLevel == _maxZoomLevel) { return; }
				_currentZoomLevel++;
				loadTiles();
				localPosition.y = 1000;
			}
			if (y >= 1000)
			{
				//already at lowest level, don't do anything
				if (_currentZoomLevel == _minZoomLevel) { return; }
				_currentZoomLevel--;
				loadTiles();
				localPosition.y = 500;
			}
			_referenceCamera.transform.localPosition = localPosition;


			//Hud.text = string.Format(
			//	"camera.y:{0:0.00} zoom:{1} viewport:{2}/{3}"
			//	, y
			//	, _currentZoomLevel
			//	, viewPortLL
			//	, viewPortUR
			//);
		}
		finally
		{
			Hud.text = string.Format(
				"camera.y:{0:0.00} zoom:{1}"
				, y
				, _currentZoomLevel
			);
		}
	}


	private void loadTiles()
	{
		if (_creatingTiles) { return; }

		try
		{
			_creatingTiles = true;

			Vector3 viewPortLL = _referenceCamera.ViewportToWorldPoint(new Vector3(0, 0, _referenceCamera.transform.localPosition.y));
			Vector3 viewPortUR = _referenceCamera.ViewportToWorldPoint(new Vector3(1, 1, _referenceCamera.transform.localPosition.y));

			//revert downscaling and get back to full WebMerc coords
			Vector2dBounds viewPortWebMerc = new Vector2dBounds(
				new Vector2d(viewPortLL.x *= 256, viewPortLL.z *= 256)
				, new Vector2d(viewPortUR.x *= 256, viewPortUR.z *= 256)
			);
			Vector2dBounds viewPortLatLng = new Vector2dBounds(
				Conversions.MetersToLatLon(viewPortWebMerc.SouthWest)
				, Conversions.MetersToLatLon(viewPortWebMerc.NorthEast)
			);

			Vector2dBounds requestBounds = _boundsMap;

			if (
				viewPortLatLng.West > requestBounds.West
				&& viewPortLatLng.East < requestBounds.East
				&& viewPortLatLng.South > requestBounds.South
				&& viewPortLatLng.North < requestBounds.North
			)
			{
				requestBounds = viewPortLatLng;
			}

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
				tile.Initialize(tileId, _mbxAccess);
				tile.SetActive(true);
			}
		}
		finally
		{
			_creatingTiles = false;
		}
	}


}
