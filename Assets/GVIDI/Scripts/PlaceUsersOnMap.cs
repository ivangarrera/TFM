using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlaceUsersOnMap : MonoBehaviour
{
    #region editor properties

    /// <summary>
    /// It contains the reference to the map on which the polyline will be drawn, according to the characteristics of the map such as 
    /// rendered coordinates at a certain moment or the zoom on the terrain
    /// </summary>
    [SerializeField]
    private AbstractMap _abstractMap;

    /// <summary>
    /// Contains the reference to the material that will be used to fill the POI
    /// </summary>
    [SerializeField]
    private Material _material;

    [SerializeField]
    private Mesh _poiMesh;

    #endregion

    #region private fields

    /// <summary>
    /// It contains a reference to the GameObject that manages the aspects of the polyline (vertices, triangles, mesh, ...)
    /// </summary>
    private GameObject _gameObj;

    private List<string> coordinatesUser;

    #endregion

    #region public methods

    // Start is called before the first frame update
    private void Start()
    {
        if (_gameObj == null)
        {
            _gameObj = GetComponent<GameObject>();
        }

        if (_abstractMap == null)
        {
            _abstractMap = FindObjectOfType<AbstractMap>();
        }

        coordinatesUser = new List<string>
        {
            "39.5403411,-4.3398568", "39.5396466,-4.3422542",  "39.5417593,-4.3471313",  "39.5485987,-4.3584196",  "39.5480903,-4.3500898","39.546783,-4.3473726",  "39.5460341,-4.3438206",  "39.5454722,-4.3414698",  "39.5423526,-4.3378727", "39.5404643,-4.3391543"
        };

        _gameObj = CreateGameObject();
    }

    // Update is called once per frame
    private void Update()
    {
        if (_gameObj != null)
        {
            foreach (var poiObj in GameObject.FindGameObjectsWithTag("PoIUser"))
            {
                poiObj.Destroy();
            }

            foreach (var coord in coordinatesUser)
            {
                _gameObj = CreateGameObject();
                var coordVector2d = Conversions.StringToLatLon(coord);
                Vector3 worldPos = _abstractMap.GeoToWorldPosition(coordVector2d, true);
                _gameObj.transform.SetPositionAndRotation(worldPos, _abstractMap.transform.rotation);
                _gameObj.transform.Translate(new Vector3(0.0f, 0.05f, 0.0f));
                _gameObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            }
        }
    }

    #endregion

    #region private methods

    /// <summary>
    /// The first time the program is executed it is necessary to create the gameobject that will contain the users (as PoI)
    /// </summary>
    /// <param name="data">Information needed to create the POI </param>
    /// <returns>Reference to the gameobject created. The following times, it is not necessary to create the gameobject or its 
    /// sub-components, this reference can simply be used to update them</returns>
    private GameObject CreateGameObject()
    {
        // Create the game object which will represent the Users on the Map
        var directionsGameObject = new GameObject($"PoIUser_{System.Guid.NewGuid().ToString().Substring(0, 6)}")
        {
            tag = "PoIUser"
        };
        directionsGameObject.transform.SetParent(_abstractMap.transform);
        MeshFilter meshFilter = AddToGameObjectAndSetParent<MeshFilter>(directionsGameObject);
        meshFilter.mesh = _poiMesh;

        AddToGameObjectAndSetParent<MeshRenderer>(directionsGameObject).material = _material;
        return directionsGameObject;
    }

    /// <summary>
    /// Add a certain component as a sub-component of the indicated <see cref="GameObject"/>. 
    /// </summary>
    /// <typeparam name="T">Specific type of the component which is to be appended</typeparam>
    /// <param name="gameObject"><see cref="GameObject"/> reference to which the subcomponent is to be appended</param>
    /// <returns>Reference to the appended sub-component</returns>
    private T AddToGameObjectAndSetParent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.AddComponent<T>();
        return component;
    }

    #endregion
}
