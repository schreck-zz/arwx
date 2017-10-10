using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class radar_plain : MonoBehaviour {

    private Transform plane;
    private Texture newtexture;
    private AndroidJavaClass bmf;
    public consoler con;
    private float xx;
    private float zz;

    void Start () {
        con.add("start");
        zz = 0f;
        xx = 0f;
        StartCoroutine(world());
        StartCoroutine(img());
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

    IEnumerator world()
    {
        con.add("world file");
        WWW www = new WWW("http://radar.weather.gov/ridge/RadarImg/N0R/OKX_N0R_0.gfw");
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
            Debug.Log(x_scale + " " + y_scale + " " + x + " " + y);
            float x_loc = -73.925f;
            float y_loc =  40.662f;
            xx = 5*(.5f+(x_loc - x) / (x_scale * 600)); // in terms of scaled x world coords, not offset by the half x we need.
            // we are hard coding the width and starting location, this whole thing as to wait till the image loads, and the gps resolves
            // and handle when one of those three required things dont happen
            // observed from image in pixels:
            //  -93,-19
            zz = 5 * (.5f + (y_loc - y) / (y_scale * 550));

            Debug.Log("px offset (" + ((x_loc - x) / (x_scale) - 300) + "," + (275 - (y_loc - y) / (y_scale))+")");  // matches observed -112,-20
        }
        else
        {
            Debug.Log("wrong length in gfw? "+S_gfw.Length);
        }


    }

    IEnumerator img()
    {
        con.add("img");
        bmf = new AndroidJavaClass("android.graphics.BitmapFactory");
        AndroidJavaClass bm = new AndroidJavaClass("android.graphics.Bitmap");
        con.add("www send");
        WWW www = new WWW("http://radar.weather.gov/ridge/RadarImg/N0R/OKX_N0R_0.gif");
        yield return www;
        con.add("www recv " + www.bytesDownloaded);
        AndroidJavaObject bmo = bmf.CallStatic<AndroidJavaObject>("decodeByteArray",new object[] { www.bytes, 0, www.bytesDownloaded });
        int h = bmo.Call<int>("getHeight", new object[] { });
        con.add("height " + h);
        int w = bmo.Call<int>("getWidth", new object[] { });
        con.add("width " + w);
        System.IntPtr pixs = AndroidJNI.NewIntArray(h * w);
        var texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
        //bmo.Call("getPixels", new object[] { pixs, 0, w, 0, 0, w, h });
        jvalue[] gpargs;
        gpargs = new jvalue[7];
        gpargs[0].l = pixs;
        gpargs[1].i = 0;
        gpargs[2].i = w;
        gpargs[3].i = 0;
        gpargs[4].i = 0;
        gpargs[5].i = w;
        gpargs[6].i = h;
        AndroidJNI.CallVoidMethod(bmo.GetRawObject(), AndroidJNI.GetMethodID(bm.GetRawClass(), "getPixels","([IIIIIII)V"), gpargs);
        int[] apixs;
        apixs = AndroidJNI.FromIntArray(pixs);
        for (int i = 0; i < h; i++)
        {
            for(int j = 0; j < w; j++)
            {
                int pixel = apixs[j + w * i];
                Color32 pc = ConvertAndroidColor(pixel);
                texture.SetPixel(j,i,pc);
            }
        }
        con.add("pix2" + apixs.GetHashCode());
        texture.Apply();
        plane = transform.GetChild(0);
        plane.GetComponent<Renderer>().material.mainTexture = texture;
        yield return "good";
    }

    public void center_on_location(LocationInfo loc)
    {

    }

    // Update is called once per frame
    void Update () {
        transform.position = new Vector3(xx, 30f, zz);
    }
}
;