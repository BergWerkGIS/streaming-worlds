﻿using Mapbox.Map;
using Mapbox.Unity;
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


	private int _lastZoom = int.MinValue;
	private GameObject _root;
	private Vector2dBounds _boundsWorld;
	private Vector2dBounds _boundsSmall;
	private Vector2dBounds _boundsMap;
	private bool _creatingTiles = false;
	private MapboxAccess _mbxAccess;

	// Use this for initialization
	void Start()
	{
		_mbxAccess = MapboxAccess.Instance;

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
	}

	void Update()
	{

		float y = _referenceCamera.transform.localPosition.y;
		//int currentZoom = 20 - (int)(Math.Floor(y) / 500 * 1.5);
		int currentZoom = 20 - (10 + (int)(y / 10000 * 10));
		Hud.text = string.Format(
			"camera.y:{0:0.00} zoom:{1} viewport:{2}/{3}"
			, y
			, currentZoom
			, _referenceCamera.ViewportToWorldPoint(new Vector3(0, 0, _referenceCamera.transform.localPosition.y))
			, _referenceCamera.ViewportToWorldPoint(new Vector3(1, 1, _referenceCamera.transform.localPosition.y))
		);

		if (_creatingTiles) { return; }

		try
		{
			_creatingTiles = true;


			if (_lastZoom == currentZoom) { return; }

			if (currentZoom > 10)
			{
				Debug.LogWarningFormat("new zoom[{0}] too high, TileCover.Get() will crash", currentZoom);
				return;
			}

			//TileCover.Get() crashes if there are too many tiles
			HashSet<CanonicalTileId> tilesNeeded = TileCover.Get(_boundsMap, currentZoom);

			if (tilesNeeded.Count > 256)
			{
				_lastZoom = currentZoom;
				Debug.LogWarningFormat("level[{0}] has too many tiles[{1}]: not creating any", currentZoom, tilesNeeded.Count);
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
			_lastZoom = currentZoom;
			_creatingTiles = false;
		}
	}


}
