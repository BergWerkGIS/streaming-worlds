using Mapbox.Map;
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
	private Dictionary<CanonicalTileId, Tile> _tiles = new Dictionary<CanonicalTileId, Tile>();
	private GameObject _root;
	private Vector2dBounds _boundsWorld;
	private Vector2dBounds _boundsSmall;
	private Vector2dBounds _boundsMap;
	private object _lock = new object();


	// Use this for initialization
	void Start()
	{
		Hud.text = "Start";
		_root = new GameObject("root");

		_boundsWorld = new Vector2dBounds(
			new Vector2d(-180, -90)
			, new Vector2d(180, 90)
		);

		_boundsSmall = new Vector2dBounds(
			new Vector2d(-10, -10)
			, new Vector2d(10, 10)
		);

		//_boundsMap = _boundsWorld;
		_boundsMap = _boundsSmall;
	}

	void Update()
	{
		float y = _referenceCamera.transform.localPosition.y;
		int currentZoom = 20 - (int)Math.Floor(y);

		Hud.text = string.Format("camera.y:{0:0.00} zoom:{1}", y, currentZoom);

		if (_lastZoom == currentZoom) { return; }
		if (currentZoom > 9)
		{
			_lastZoom = currentZoom;
			Debug.LogWarning("level has too many tiles - not creating any");
			return;
		}


		Debug.LogFormat("new zoom[{0}]: adding/removing tiles", currentZoom);

		HashSet<CanonicalTileId> tilesNeeded = TileCover.Get(_boundsMap, currentZoom);
		Debug.Log("tiles needed:" + tilesNeeded.Count);

		lock (_lock)
		{
			var deactivate = _tiles.Where(t => t.Key.Z == _lastZoom).ToList();
			foreach (var d in deactivate)
			{
				d.Value.SetActive(false);
				Destroy(d.Value.gameObject);
				_tiles.Remove(d.Key);
			}

			foreach (var tileId in tilesNeeded)
			{
				Tile tile;
				if (!_tiles.TryGetValue(tileId, out tile))
				{

					GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
					quad.name = "tile-data";
					GameObject goTxt = new GameObject("text");
					//goTxt.transform.parent = plane.transform;
					var textMesh = goTxt.AddComponent<TextMesh>();
					//var meshRenderer = goTxt.AddComponent<MeshRenderer>();
					//tile = plane.AddComponent<Tile>();
					tile = new GameObject().AddComponent<Tile>();
					//tile = _root.AddComponent<Tile>();
					//goTxt.transform.parent = tile.transform;
					//tile.transform.parent = plane.transform;
					//plane.transform.parent = _root.transform;
					tile.transform.parent = _root.transform;
					quad.transform.parent = tile.transform;
					//goTxt.transform.parent = plane.transform;
					goTxt.transform.parent = tile.transform;

					float scale = 1f;
					if (0 != currentZoom)
					{
						//scale = (float)(1.0 / Math.Pow(2d, (double)currentZoom));
						//Debug.Log("scale:" + scale);
						//Vector3 s = new Vector3(scale, 1, scale);
						//tile.transform.localScale = s;
						//quad.transform.localScale = s;
						//textMesh.fontSize = 10;
					}

					tile.Initialize(currentZoom, tileId.X, tileId.Y);
					_tiles.Add(tileId, tile);
				}
				tile.SetActive(true);
			}
		}
		_lastZoom = currentZoom;
	}


}
