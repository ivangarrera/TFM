using Assets.GVIDI.Scripts;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlaceRouteOnMap : MonoBehaviour
{
    #region editor properties

    /// <summary>
    /// They are used as a helper, to generate the polyline that will be placed on the map. In this case, it is preferable to use 
    /// <see cref="LineMeshModifier"/> so the polygons are automatically generated from a list of vertices. The triangles and tiled 
    /// UV are also automatically generated.
    /// </summary>
    [SerializeField]
    private MeshModifier[] _meshModifiers;

    /// <summary>
    /// 
    /// </summary>
    [SerializeField]
    private TextAsset _jsonFile;

    /// <summary>
    /// Contains the reference to the material that will be used to draw the polyline
    /// </summary>
    [SerializeField]
    private Material _material;

    /// <summary>
    /// It contains the reference to the map on which the polyline will be drawn, according to the characteristics of the map such as 
    /// rendered coordinates at a certain moment or the zoom on the terrain
    /// </summary>
    [SerializeField]
    private AbstractMap _abstractMap;

    #endregion

    #region private fields

    /// <summary>
    /// It contains a reference to the GameObject that manages the aspects of the polyline (vertices, triangles, mesh, ...)
    /// </summary>
    private GameObject _gameObj;

    private Expedition _expedition;

    #endregion

    #region public methods

    // Start is called before the first frame update
    private void Start()
    {
        if (_meshModifiers == null)
        {
            _meshModifiers = new MeshModifier[] { new LineMeshModifier() };
        }

        if (_gameObj == null)
        {
            _gameObj = GetComponent<GameObject>();
        }

        if (_abstractMap == null)
        {
            _abstractMap = FindObjectOfType<AbstractMap>();
        }

        try
        {
            _expedition = Newtonsoft.Json.JsonConvert.DeserializeObject<Expedition>(_jsonFile.text);
            _abstractMap.SetCenterLatitudeLongitude(Conversions.StringToLatLon($"{_expedition.guide.FirstOrDefault().lat},{_expedition.guide.FirstOrDefault().lon}"));
        }
        catch (Exception)
        {

        }

        MeshData meshData = CreateMeshData();
        _gameObj = CreateGameObject(meshData);
    }

    // Update is called once per frame
    private void Update()
    {
        if (_expedition == null || _expedition.guide == null || _expedition.participants == null) return;

        if (_gameObj != null)
        {
            foreach (var poiObj in GameObject.FindGameObjectsWithTag("PolyLine"))
            {
                poiObj.Destroy();
            }

            MeshData meshData = CreateMeshData();
            _gameObj = CreateGameObject(meshData);
            _gameObj.transform.Translate(new Vector3(0.0f, 0.05f, 0.0f));
        }
    }

    #endregion

    #region private methods

    /// <summary>
    /// The first time the program is executed it is necessary to create the gameobject that will contain the polyline as well as add as 
    /// sub-components the meshes needed to render the polyline
    /// </summary>
    /// <param name="data">Information needed to create the polyline (information about vertices, triangles and UV)</param>
    /// <returns>Reference to the gameobject created. The following times, it is not necessary to create the gameobject or its 
    /// sub-components, this reference can simply be used to update them</returns>
    private GameObject CreateGameObject(MeshData data)
    {
        // Create the game object which will represent the PolyLine
        var directionsGameObject = new GameObject("PathLine")
        {
            tag = "PolyLine"
        };
        directionsGameObject.transform.SetParent(_abstractMap.transform);

        MeshFilter meshFilter = AddToGameObjectAndSetParent<MeshFilter>(directionsGameObject);

        Mesh mesh = meshFilter.mesh;
        FillMeshWithData(mesh, data);
        RecalculateMeshParams(mesh);
        AddToGameObjectAndSetParent<MeshRenderer>(directionsGameObject).material = _material;
        return directionsGameObject;
    }

    /// <summary>
    /// It takes the information from the previously created <see cref="MeshData"/> and embeds it into a generic <see cref="Mesh"/>
    /// </summary>
    /// <param name="mesh">Reference to a <see cref="Mesh"/> in which the information will be embeded</param>
    /// <param name="data">Information which will be embeded</param>
    private void FillMeshWithData(Mesh mesh, MeshData data)
    {
        // Set the amount of "submeshes", the mesh has
        mesh.subMeshCount = data.Triangles.Count;

        // Set the vertices of the mesh
        mesh.SetVertices(data.Vertices);

        // Set the triangles of the mesh (for each submesh)
        int counter = data.Triangles.Count;
        for (int i = 0; i < counter; i++)
        {
            var triangle = data.Triangles[i];
            mesh.SetTriangles(triangle, i);
        }

        // Set the UV of the mesh (for each submesh)
        counter = data.UV.Count;
        for (int i = 0; i < counter; i++)
        {
            var uv = data.UV[i];
            mesh.SetUVs(i, uv);
        }
    }

    private MeshData CreateMeshData()
    {
        MeshData meshData = new MeshData();
        // Coordinates normalized to fit into the map (height included)
        List<Vector3> coordinatesData = new List<Vector3>();

        foreach (var data in _expedition.guide)
        {
            // Transform each coordinate from "lat, lon" format to 3D vector (and normalize the X,Y,Z values to fit the map scale)
            var coordVector2d = Conversions.StringToLatLon($"{data.lat},{data.lon}");
            Vector3 worldPos = _abstractMap.GeoToWorldPosition(coordVector2d, true);
            coordinatesData.Add(worldPos);
        }

        VectorFeatureUnity vectorFeature = new VectorFeatureUnity();
        vectorFeature.Points.Add(coordinatesData);

        // Forall active mesh modifiers, generate the polygon (triangles, vertices and UVs) using the coordinates data
        foreach (MeshModifier meshModifier in _meshModifiers.Where(x => x.Active))
        {
            // Execute the MeshModifier (LineMeshModifier) with the normalized coordinates and store the result into the meshData variable. In the end,
            // the meshData variable will contain the vertices, triangles and UV information
            if (((LineMeshModifier)meshModifier).NeedToInit)
            {
                meshModifier.Initialize();
            }
            meshModifier.Run(vectorFeature, meshData, _abstractMap.WorldRelativeScale * 10);
        }

        return meshData;
    }

    /// <summary>
    /// Recalculate the different parameters of the mesh when the vertices change
    /// </summary>
    /// <param name="mesh">Mesh for which the parameters are to be recalculated</param>
    private void RecalculateMeshParams(Mesh mesh)
    {
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }

    /// <summary>
    /// Add a certain component as a sub-component of the indicated <see cref="GameObject"/>. In addition, it sets it as a child of the 
    /// <see cref="AbstractMap"/> so that it inherits its transformation.
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
