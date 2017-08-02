using System.ComponentModel.Design;
namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;

	public class TileCoverTileProvider : AbstractTileProvider
	{
		[SerializeField]
		Camera _camera;

		// TODO: change to Vector4 to optimize for different aspect ratios.
		[SerializeField]
		int _visibleBuffer;

		[SerializeField]
		int _disposeBuffer;

		[SerializeField]
		float _updateInterval;

		Plane _groundPlane;
		Ray _ray;

		float _hitDistance;
		Vector2 _ne;
		Vector3 _sw;
		float _elapsedTime;
		bool _shouldUpdate;

		Vector2d _currentLatitudeLongitude;
		UnwrappedTileId _cachedTile;
		UnwrappedTileId _currentTile;

		float _distance;

		float _distance2;

		[SerializeField]

		float _d;






		internal override void OnInitialized()
		{
			_groundPlane = new Plane(Vector3.up, Mapbox.Unity.Constants.Math.Vector3Zero);
			_sw = Vector2.zero;
			_ne = Vector2.one;
			Build();
			//_shouldUpdate = true;
		}

		void Update()
		{
			_ray = _camera.ViewportPointToRay(_sw);
			if (_groundPlane.Raycast(_ray, out _hitDistance))
			{
				var p1 = _ray.GetPoint(_hitDistance);
				p1.y = 0;
				_ray = _camera.ViewportPointToRay(_ne);
				if (_groundPlane.Raycast(_ray, out _hitDistance))
				{

					var p2 = _ray.GetPoint(_hitDistance);
					p2.y = 0;
					_distance2 = (p1 - p2).magnitude;
				}
			}

			_d = _distance2 / _distance;
			if (_d < .9f)
			{
				_map.Zoom++;
			}
			else if (_d > 1.1f)
			{
				_map.Zoom--;
			}
		}

		void Build()
		{
			//length = distance between point and camera
			//d = width or depth of quad
			//C = constant
			//(length/d) < C

			//if (!_shouldUpdate)
			//{
			//	return;
			//}

			//_elapsedTime += Time.deltaTime;
			//if (_elapsedTime >= _updateInterval)
			{
				_elapsedTime = 0f;

				_ray = _camera.ViewportPointToRay(_sw);
				Vector2d latLonSW;
				if (_groundPlane.Raycast(_ray, out _hitDistance))
				{
					latLonSW = _ray.GetPoint(_hitDistance).GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
					//Debug.Log("TileCoverTileProvider: " + latLonSW);
					_ray = _camera.ViewportPointToRay(_ne);
					Vector2d latLonNE;
					if (_groundPlane.Raycast(_ray, out _hitDistance))
					{

						latLonNE = _ray.GetPoint(_hitDistance).GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
						//Debug.Log("TileCoverTileProvider: " + latLonNE);
						var cover = TileCover.Get(new Vector2dBounds(latLonSW, latLonNE), _map.Zoom);
						foreach (var tile in cover)
						{
							AddTile(new UnwrappedTileId(tile.Z, tile.X, tile.Y));
						}
						_distance = (float)(Conversions.LatLonToMeters(latLonNE) - Conversions.LatLonToMeters(latLonSW)).magnitude;
						Debug.Log("TileCoverTileProvider: 1 " + _distance);
					}
				}


				//_currentTile = TileCover.CoordinateToTileId(_currentLatitudeLongitude, _map.Zoom);

				//if (!_currentTile.Equals(_cachedTile))
				//{
				//	// FIXME: this results in bugs at world boundaries! Does not cleanly wrap. Negative tileIds are bad.
				//	for (int x = _currentTile.X - _visibleBuffer; x <= (_currentTile.X + _visibleBuffer); x++)
				//	{
				//		for (int y = _currentTile.Y - _visibleBuffer; y <= (_currentTile.Y + _visibleBuffer); y++)
				//		{
				//			AddTile(new UnwrappedTileId(_map.Zoom, x, y));
				//		}
				//	}
				//	_cachedTile = _currentTile;
				//	Cleanup(_currentTile);
				//}
			}
		}
	}

	//void Cleanup(UnwrappedTileId currentTile)
	//{
	//	var count = _activeTiles.Count;
	//	for (int i = count - 1; i >= 0; i--)
	//	{
	//		var tile = _activeTiles[i];
	//		bool dispose = false;
	//		dispose = tile.X > currentTile.X + _disposeBuffer || tile.X < _currentTile.X - _disposeBuffer;
	//		dispose = dispose || tile.Y > _currentTile.Y + _disposeBuffer || tile.Y < _currentTile.Y - _disposeBuffer;

	//		if (dispose)
	//		{
	//			RemoveTile(tile);
	//		}
	//	}
	//}
}

