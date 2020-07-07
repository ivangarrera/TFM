using Assets.GVIDI.Scripts;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CalculateExpeditionPath : MonoBehaviour
{
    #region private properties

    /// <summary>
    /// JSON file that contains the information about the expedition
    /// </summary>
    [SerializeField]
    public TextAsset jsonFile;

    /// <summary>
    /// Reference to the location provider, to obtain the current position of the user
    /// </summary>
    [SerializeField]
    public LocationProviderFactory locationProviderFactory;

    /// <summary>
    /// Contains the reference to the material that will be used to draw the polyline
    /// </summary>
    [SerializeField]
    private Material _material;

    /// <summary>
    /// Reference to the game object containing this script
    /// </summary>
    [SerializeField]
    private GameObject myGameObject;

    /// <summary>
    /// They are used as a helper, to generate the polyline that will be placed on the screen. In this case, it is preferable to use 
    /// <see cref="LineMeshModifier"/> so the polygons are automatically generated from a list of vertices. The triangles and tiled 
    /// UV are also automatically generated.
    /// </summary>
    private MeshModifier[] _meshModifiers;

    [SerializeField]
    private AbstractMap _abstractMap;

    private Expedition expedition;
    private ILocationProvider locationProvider;

    #endregion

    #region public methods

    // Start is called before the first frame update
    public void Start()
    {
        try
        {
            if (_meshModifiers == null)
            {
                _meshModifiers = new MeshModifier[] { new LineMeshModifier() };
            }

            if (_abstractMap == null)
            {
                _abstractMap = FindObjectOfType<AbstractMap>();
            }
        
            expedition = Newtonsoft.Json.JsonConvert.DeserializeObject<Expedition>(jsonFile.text);

            // Fill the guide coordinates in radians
            foreach (var data in expedition.guide)
            {
                if (float.TryParse(data.lat, out float lat) && float.TryParse(data.lon, out float lon))
                {
                    data.CoordinatesRadians = new System.Numerics.Vector2(ConvertToRadians(lat), ConvertToRadians(lon));
                }

            }

            // Fill the participants coordinates in radians
            foreach (var participant in expedition.participants)
            {
                foreach (var data in participant)
                {
                    if (float.TryParse(data.lat, out float lat) && float.TryParse(data.lon, out float lon))
                    {
                        data.CoordinatesRadians = new System.Numerics.Vector2(ConvertToRadians(lat), ConvertToRadians(lon));
                    }
                }
            }

            // Get the location provider
            if (locationProviderFactory != null)
            {
                locationProvider = locationProviderFactory.DefaultLocationProvider;
            }
            myGameObject.transform.localPosition = new Vector3(myGameObject.transform.localPosition.x, myGameObject.transform.localPosition.y - 20f, myGameObject.transform.localPosition.z);
        }
        catch (Exception)
        {

        }
    }

    // Update is called once per frame
    public void Update()
    {
        if (expedition == null || expedition.guide == null || expedition.participants == null) return;

        Mapbox.Utils.Vector2d? currentLocation = locationProvider?.CurrentLocation.LatitudeLongitude;
        if (currentLocation != null)
        {
            System.Numerics.Vector2 locationRadians = new System.Numerics.Vector2(ConvertToRadians((float)currentLocation.Value.x),
                ConvertToRadians((float)currentLocation.Value.y));
            // Get the nearest guide position
            double nearestDistance = double.PositiveInfinity;
            ExpeditionUserData nearestGuidePosition = null;
            foreach (var data in expedition.guide)
            {
                double distance = CalculateDistanceBetweenPoints(data.CoordinatesRadians, locationRadians);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGuidePosition = data;
                }
            }
            var coordGuide = Conversions.StringToLatLon($"{nearestGuidePosition.lat},{nearestGuidePosition.lon}");
            var myCoord = Conversions.StringToLatLon($"{currentLocation.Value.x},{currentLocation.Value.y}");
            Vector3 worldGuide = _abstractMap.GeoToWorldPosition(coordGuide, false);
            Vector3 myWorld = _abstractMap.GeoToWorldPosition(myCoord, false);

            MeshData meshData = CreateMeshData(worldGuide, myWorld);
            CreateGameObject(meshData);
        }
    }

    #endregion

    #region private methods

    private MeshData CreateMeshData(Vector3 guide, Vector3 position)
    {
        MeshData meshData = new MeshData();
        // Coordinates normalized to fit into the map (height included)
        List<Vector3> coordinatesData = new List<Vector3>
        {
            guide,
            position
        };

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
            meshModifier.Run(vectorFeature, meshData, _abstractMap.WorldRelativeScale);
        }

        return meshData;
    }
    
    private double CalculateDistanceBetweenPoints(System.Numerics.Vector2 coordinatesFirst, System.Numerics.Vector2 coordinatesSecond)
    {
        double latitudeDistance = coordinatesFirst.X - coordinatesSecond.X;
        double longitudeDistance = coordinatesFirst.Y - coordinatesSecond.Y;

        double a = Math.Pow(Math.Sin(latitudeDistance / 2), 2) + Math.Cos(coordinatesSecond.X) * Math.Cos(coordinatesFirst.X) * Math.Pow(Math.Sin(longitudeDistance / 2), 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        // 6373.0 is the earth radius
        return 6373.0 * c;
    }

    private void CreateGameObject(MeshData data)
    {
        myGameObject.AddComponent(typeof(MeshFilter));

        Mesh mesh = myGameObject.GetComponent<MeshFilter>().mesh;
        FillMeshWithData(mesh, data);
        RecalculateMeshParams(mesh);
        myGameObject.AddComponent(typeof(MeshRenderer));
        myGameObject.GetComponent<MeshRenderer>().material = _material;
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

    private float ConvertToRadians(float angle) => (float)Math.PI / 180 * angle;

    #endregion
}
