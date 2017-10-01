using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class playercontroller : MonoBehaviour {

    private Quaternion initRot;
    private LocationInfo loc;
    private string loc_status;

	// Use this for initialization
	void Start () {
        if (SystemInfo.supportsGyroscope) {
            Input.gyro.enabled = true;
        }
        else
        {
            Input.gyro.enabled = false;
        }
        Input.compass.enabled = true;
        initRot = new Quaternion(0, 0, 1, 0);
        loc_status = "looking";
        StartCoroutine(check_location());
    }
    
    IEnumerator check_location()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
            yield break;

        // Start service before querying location
        Input.location.Start();

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            loc_status = "timeout";
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            loc_status = "failed";
        }
        else
        {
            // Access granted and location value could be retrieved
            loc_status = "found";
            loc = Input.location.lastData;
        }

        // Stop service if there is no need to query location updates continuously
        Input.location.Stop();
    }

/*protected void OnGUI()
{
    GUI.skin.label.fontSize = Screen.width / 40;

    GUILayout.Label("Orientation: " + Screen.orientation);
    GUILayout.Label("input.gyro.attitude: " + Input.gyro.attitude);
    GUILayout.Label("iphone width/font: " + Screen.width + " : " + GUI.skin.label.fontSize);
}*/

private static Quaternion GetGyro()
    {
        if (Input.gyro.enabled)
        {
            return new Quaternion(Input.gyro.attitude.x, Input.gyro.attitude.y, Input.gyro.attitude.z, Input.gyro.attitude.w);
        } else
        { // personal computer gyro mock init
            Quaternion a = Quaternion.Euler(355f, 274f, 270f);
            return a;
        }
    }

    // Update is called once per frame
    void Update () {

        transform.localRotation = (Quaternion.Euler(90f,90f,-90f))*GetGyro()*initRot;
        transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "<b>gyro:</b>" +
            "\neuler: " + GetGyro().eulerAngles +
            "\n<b>compass:</b>\nmag: " + Input.compass.magneticHeading.ToString() +
            "\ntrue: " + Input.compass.trueHeading.ToString() +
            "\n<b>gps:</b>\nstatus:" + loc_status +
            "\nlon:" + loc.longitude +
            "\nlat:" + loc.latitude +
            "\nalt:" + loc.altitude;
    }
}
