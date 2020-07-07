using Assets.GVIDI.Scripts;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using System;
using System.Linq;
using UnityEngine;

public class CalculateUsersHolograms : MonoBehaviour
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
    /// Reference to the game object containing this script
    /// </summary>
    [SerializeField]
    private GameObject myGameObject;

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
            if (_abstractMap == null)
            {
                _abstractMap = FindObjectOfType<AbstractMap>();
            }

            expedition = Newtonsoft.Json.JsonConvert.DeserializeObject<Expedition>(jsonFile.text);
            expedition.guide.OrderByDescending(g => DateTime.Parse(g.time));
            _abstractMap.SetCenterLatitudeLongitude(Conversions.StringToLatLon($"{expedition.guide.FirstOrDefault().lat},{expedition.guide.FirstOrDefault().lon}"));


            foreach (var participant in expedition.participants)
            {
                participant.OrderByDescending(p => DateTime.Parse(p.time));
            }

            // Get the location provider
            if (locationProviderFactory != null)
            {
                locationProvider = locationProviderFactory.DefaultLocationProvider;
            }
            myGameObject.transform.localScale = new Vector3(2f, 2f, 2f);
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
            // Get location of the guide
            var coordinatesGuide = expedition.guide.FirstOrDefault();

            var coordVector2d = Conversions.StringToLatLon($"{coordinatesGuide.lat},{coordinatesGuide.lon}");
            Vector3 worldPos = _abstractMap.GeoToWorldPosition(coordVector2d, false);
            myGameObject.transform.SetPositionAndRotation(worldPos, _abstractMap.transform.rotation);
            //myGameObject.transform.rotation = new Quaternion(0f, 250f, 0f, 0f);
            while (Math.Abs(transform.position.x) > 20f || Math.Abs(transform.position.y) > 20f || Math.Abs(transform.position.z) > 20f)
            {
                myGameObject.transform.position = new Vector3(transform.position.x / 2f, transform.position.y / 2f, transform.position.z / 2f);
            }
            myGameObject.transform.localPosition = new Vector3(myGameObject.transform.localPosition.x, myGameObject.transform.localPosition.y + 1f, myGameObject.transform.localPosition.z);
        }
    }

    #endregion
}
