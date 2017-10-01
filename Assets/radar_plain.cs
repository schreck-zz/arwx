using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class radar_plain : MonoBehaviour {

    private Transform plane;
    private Texture newtexture;
    private AndroidJavaClass bmf;
    public consoler con;

    void Start () {
        con.add("start");
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

    IEnumerator img()
    {
        con.add("img");
        bmf = new AndroidJavaClass("android.graphics.BitmapFactory");
        con.add("bmf");
        byte[] stream = new byte[0];
        con.add("byt");
        AndroidJavaClass bm = new AndroidJavaClass("android.graphics.Bitmap");
        con.add("bm");
    
        con.add("www send");
        WWW www = new WWW("http://radar.weather.gov/ridge/RadarImg/N0R/OKX_N0R_0.gif");
        yield return www;
        con.add("www recv " + www.bytesDownloaded);
        AndroidJavaObject bmo = bmf.CallStatic<AndroidJavaObject>("decodeByteArray",new object[] { www.bytes, 0, www.bytesDownloaded });
        con.add("huh");
        int h = bmo.Call<int>("getHeight", new object[] { });
        con.add("height " + h);
        int w = bmo.Call<int>("getWidth", new object[] { });
        con.add("width " + w);
        int[] pixs;
        var texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
        for (int i = 0; i < h; i++)
        {
            for(int j = 0; j < w; j++)
            {
                //con.add("(" + j + "," + i + ")");
                int pixel = bmo.Call<int>("getPixel", new object[] { j, i });
                //con.add("pixel " + pixel);
                Color32 pc = ConvertAndroidColor(pixel);
                //con.add("color");
                texture.SetPixel(j,i,pc);
                //con.add("Set pizxel");
            }
        }
        con.add("pix");
        texture.Apply();
        con.add("txt");
        plane = transform.GetChild(0);
        plane.GetComponent<Renderer>().material.mainTexture = texture;
        con.add("mesh");
        yield return "good";
    }

    // Update is called once per frame
    void Update () {
    }
}
;