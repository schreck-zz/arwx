using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*// given a location [loc] from gps or something, find the offset (x,z) to potision this radar plane
public Vector2 find_offset(LocationInfo loc)
{

    Vector2 offset = new Vector2(
          10 * (.5f + (loc.longitude - corner.x) / (scale.x * w))
        - 10 * (.5f - (loc.latitude  - corner.y) / (scale.y * h)));
    return offset;
}*/

public class WorldFile
{
    public Vector2 Corner;
    public Vector2 Scale;
    //public Vector2 Rotation; //TODO
    public WorldFile(float c_x, float c_y, float s_x, float s_y)
    {
        Corner = new Vector2(c_x, c_y);
        Scale  = new Vector2(s_x, s_y);
    }
}

public class radar_plain : MonoBehaviour {

    private Transform plane; // child object 
    public consoler con; // object specfic in-world debug log
    // offset objects
    private WorldFile gfw; // world file data
    private Vector2 gps; // gps data
    private Vector2Int wh; // width and height of image download pixels

    void Start () {
        con.add("start");
        //LoadRadarData("http://radar.weather.gov/ridge/RadarImg/N0R/OKX_N0R_0");
    }

    public string Status()
    {

        bool loc = (gps.x != 0f);
        bool img = (wh.x != 0);
        bool world = (gfw != null);
        return (loc?"L":".")+(img?"I":".")+(world?"W":".");
    }

    public void LoadRadarData(string url)
    {
        gps.x = 0f;
        wh.x = 0;
        gfw = null;
        StartCoroutine(world(url+".gfw"));
        StartCoroutine(img(url+".gif"));
    }

    public void SetGps(LocationInfo loc)
    {
        gps = new Vector2(loc.longitude, loc.latitude);
        con.add("seting gps "+gps);
        UpdateOffset();
    }

    public void SetGps(Vector2 loc)
    {
        gps = loc;
        con.add("seting gps " + gps);
        UpdateOffset();
    }

    public void MockWH(Vector2Int wh_)
    {
        wh = wh_;
        UpdateOffset();
    }

    private static Color32 ConvertAndroidColor(int aCol)
    {
        Color32 c;
        c.b = (byte)((aCol) & 0xFF);
        c.g = (byte)((aCol >> 8) & 0xFF);
        c.r = (byte)((aCol >> 16) & 0xFF);
        c.a = (byte)((aCol >> 24) & 0xFF);
        return c;
    }

    IEnumerator world(string url)
    {
        con.add("world file send");
        WWW www = new WWW(url);
        yield return www;
        con.add("world file rcv " + www.bytesDownloaded);
        string[] S_gfw = www.text.Split('\n'); // stringed gfw file
        if(S_gfw.Length == 7)
        {
            bool worked = true;
            float x_scale = 0f;
            float y_scale = 0f;
            float x = 0f;
            float y = 0f;
            worked = worked&&float.TryParse(S_gfw[0], out x_scale);
            //worked = worked && float.TryParse(S_gfw[1], out a);
            //worked = worked && float.TryParse(S_gfw[2], out b);
            worked = worked && float.TryParse(S_gfw[3], out y_scale);
            worked = worked && float.TryParse(S_gfw[4], out x);
            worked = worked && float.TryParse(S_gfw[5], out y);
            if (!worked)
            {
                Debug.Log("couldnt parse world file");
            }
            gfw = new WorldFile(x, y, x_scale, y_scale);
            UpdateOffset();
        }
        else
        {
            Debug.Log("wrong length in gfw? "+S_gfw.Length);
        }
    }

    IEnumerator img(string url)
    {
        con.add("img send");
        WWW www = new WWW(url);
        yield return www;
        con.add("img recv " + www.bytesDownloaded);
        AndroidJavaClass bmf = new AndroidJavaClass("android.graphics.BitmapFactory");
        AndroidJavaClass bm  = new AndroidJavaClass("android.graphics.Bitmap");
        // this bitmapfactory class method returns a Bitmap object
        AndroidJavaObject bmo = bmf.CallStatic<AndroidJavaObject>("decodeByteArray",new object[] { www.bytes, 0, www.bytesDownloaded });
        // we van figure out the width and height of the gif data
        int h = bmo.Call<int>("getHeight", new object[] { });
        int w = bmo.Call<int>("getWidth", new object[] { });
        wh = new Vector2Int(w, h); // set the global wh for offsetment
        // the trick is getting the pixels without calling the JNI to often i.e. _getPixel()_
        // setup java inputs for BitMap.getPixels
        System.IntPtr pixs = AndroidJNI.NewIntArray(h * w);
        jvalue[] gpargs;
        gpargs = new jvalue[7];
        gpargs[0].l = pixs;
        gpargs[1].i = 0;
        gpargs[2].i = w;
        gpargs[3].i = 0;
        gpargs[4].i = 0;
        gpargs[5].i = w;
        gpargs[6].i = h;
        // this is the same as `bmo.getPixels(pixs,0,w,0,0,w,h)` but in raw AndroidJNI calls because pixs is a pointer to an int[] buffer
        AndroidJNI.CallVoidMethod(bmo.GetRawObject(), AndroidJNI.GetMethodID(bm.GetRawClass(), "getPixels","([IIIIIII)V"), gpargs);
        int[] apixs;
        apixs = AndroidJNI.FromIntArray(pixs);
        //
        // paint a texture with the pixels
        var texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
        for (int i = 0; i < h; i++)
        {
            for(int j = 0; j < w; j++)
            {
                int pixel = apixs[j + w * i];
                Color32 pc = ConvertAndroidColor(pixel);
                texture.SetPixel(j,i,pc);
            }
        }
        con.add("apixs " + apixs.GetHashCode());
        texture.Apply();
        plane = transform.GetChild(0);
        plane.GetComponent<Renderer>().material.mainTexture = texture;
        UpdateOffset();
        yield return "good";
    }

    void UpdateOffset()
    {
        bool loaded = (gps.x != 0f) && (wh.x != 0) && (gfw != null); // gps loaded, image width height loaded, and world file loaded 
        // TODO promises?
        if (loaded)
        {
            // lat
            float plane_w = 10f * transform.localScale.x;
            float dx_dg = gps.x - gfw.Corner.x; // distance from top right corner to gps lon (dg)
            float dx_norm = dx_dg / (wh.x * gfw.Scale.x);
            //float dx_game = dx_norm * plane_w;
            // lon
            float plane_h = 10f * transform.localScale.y;
            float dy_dg = (-gps.y) + gfw.Corner.y;
            float dy_norm =  dy_dg / (wh.y * gfw.Scale.y);
            //float dy_game = dy_norm * plane_h;
            
            transform.position = new Vector3(
                 plane_w*(.5f - dx_norm),
                transform.position.y,
                plane_h*((-.5f)-dy_norm));

            con.add("updating offset " + transform.position);
        }
    }

    void Update () {
        //transform.position = new Vector3(xx, 5f, zz);
        // wont work on the meridian or with 0 width images

    }
}