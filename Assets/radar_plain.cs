using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using UnityEngine;

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

public class radar_history {
    private List<Texture2D> T;
    private List<DateTime> TS;

    public radar_history()
    {
        T = new List<Texture2D>();
        TS = new List<DateTime>();
    }

    public void add(Texture2D t,DateTime ts)
    {
        T.Add(t);
        TS.Add(ts);
    }

    public void add(Texture2D t, string fname)
    {
        var S = fname.Split('_');
        // wrong length? exception
        var ts = new DateTime(
            Int32.Parse(S[1].Substring(0, 4)),
            Int32.Parse(S[1].Substring(4, 2)),
            Int32.Parse(S[1].Substring(6, 2)),
            Int32.Parse(S[2].Substring(0, 2)),
            Int32.Parse(S[2].Substring(2, 2)),
            0);
        add(t, ts);
    }

    public Texture2D asof(DateTime now)
    {
        var ts_0 = new TimeSpan(0, 0, 0);
        var min = new TimeSpan(888, 8, 8, 8, 8); // HACK45 start from first interval?
        int i_min = -1;
        for(int i=0;i<TS.Count; i++)
        {
            // now - then
            var foo = now - TS[i];
            if (foo > ts_0) // i is in the past
            {
                if (foo < min) // closest without going over
                {
                    min = foo;
                    i_min = i;
                }
            }
        }
        return T[i_min];
    }

    public int length()
    {
        return TS.Count;
    }
}

public class radar_plain : MonoBehaviour {

    private Transform plane; // child object 
    private string station;
    private static string radar_base = "https://radar.weather.gov/ridge/RadarImg/N0R/";
    // offset objects
    private WorldFile gfw; // world file data
    private Vector2 gps; // gps data
    private Vector2Int wh; // width and height of image download pixels
    private radar_history rh;

    void Start () {
        rh = new radar_history();
    }

    private string RadarURL()
    {
        return radar_base + station + "_N0R_0";
    }

    private string RadarHistDir()
    {
        return radar_base + station+"/?F=0";
    }

    public string Status()
    {
        bool loc = (gps.x != 0f);
        bool img = (wh.x != 0);
        bool world = (gfw != null);
        return (loc?"L":".")+(img?"I":".")+(world?"W":".");
    }

    public void LoadRadarData(string station_)
    {
        station = station_;
        var url = RadarURL();
        gps.x = 0f;
        wh.x = 0;
        gfw = null;
        StartCoroutine(world(url+".gfw"));
        StartCoroutine(img(  url+".gif"));
        StartCoroutine(load_history(RadarHistDir()));
    }

    public void SetGps(LocationInfo loc)
    {
        gps = new Vector2(loc.longitude, loc.latitude);
        UpdateOffset();
    }

    public void SetGps(Vector2 loc)
    {
        gps = loc;
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

    IEnumerator load_history_element(string url,string fname)
    {
        WWW www = new WWW(url);
        yield return www;
        Debug.Log("rcv "+www.bytesDownloaded);
        rh.add(GifToTexture(www.bytes, www.bytesDownloaded), fname);
        Debug.Log(fname + " loaded " + rh.length());
    }

    IEnumerator load_history(string url)
    {
        WWW www = new WWW(url);
        yield return www;
        XmlDocument xmlDoc = new XmlDocument();
        var text = www.text.Insert(54, " \"\""); // HACK43 (https://stackoverflow.com/a/9225499) relax the xml parser?
        xmlDoc.LoadXml(text);
        foreach(XmlElement node in xmlDoc.GetElementsByTagName("a"))
        {
            StartCoroutine(load_history_element(url + "/" + node.Attributes["href"].Value, node.Attributes["href"].Value));
        }
    }

    IEnumerator world(string url)
    {
        WWW www = new WWW(url);
        yield return www;
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
        WWW www = new WWW(url);
        yield return www;
        plane = transform.GetChild(0);
#if UNITY_ANDROID
        plane.GetComponent<Renderer>().material.mainTexture = GifToTexture(www.bytes,www.bytesDownloaded);
#endif
        UpdateOffset();
        yield return "good";
    }

#if UNITY_ANDROID
    private Texture2D GifToTexture(byte[] bytes, int length) {
        // ANDROID ONLY
        AndroidJavaClass bmf = new AndroidJavaClass("android.graphics.BitmapFactory");
        AndroidJavaClass bm = new AndroidJavaClass("android.graphics.Bitmap");
        // this bitmapfactory class method returns a Bitmap object
        AndroidJavaObject bmo = bmf.CallStatic<AndroidJavaObject>("decodeByteArray", new object[] { bytes, 0, length });
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
        AndroidJNI.CallVoidMethod(bmo.GetRawObject(), AndroidJNI.GetMethodID(bm.GetRawClass(), "getPixels", "([IIIIIII)V"), gpargs);
        int[] apixs;
        apixs = AndroidJNI.FromIntArray(pixs);
        //
        // paint a texture with the pixels
        var texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                int pixel = apixs[j + w * i];
                Color32 pc = ConvertAndroidColor(pixel);
                texture.SetPixel(j, i, pc);
            }
        }
        texture.Apply();
        return texture;
    }
#endif

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
        }
    }

    void Update () {
        //transform.position = new Vector3(xx, 5f, zz);
        // wont work on the meridian or with 0 width images

    }
}