using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

using RNG = UnityEngine.Random;

public class PrideBatteriesScript : MonoBehaviour
{
    [SerializeField]
    private GameObject _prefabD, _prefabAA;
    [SerializeField]
    private Renderer[] _renderersD, _renderersAA;

    private int _count;

    private const string DefaultSettings = @"[{""Name"":""Transgender"",""Colors"":[""55CDFC"",""F7A8B8"",""FFFFFF"",""F7A8B8"",""55CDFC""],""Probability"":1.0},{""Name"":""Rainbow"",""Colors"":[""E40303"",""FF8C00"",""FFED00"",""008026"",""24408E"",""732982""],""Probability"":1.0},{""Name"":""Progress"",""Colors"":[""FFFFFF"",""F7A8B8"",""55CDFC"",""613915"",""000000"",""E40303"",""FF8C00"",""FFED00"",""008026"",""24408E"",""732982""],""Probability"":1.0},{""Name"":""IntersexProgress"",""Colors"":[""FFD800"",""7902AA"",""FFD800"",""FFFFFF"",""F7A8B8"",""55CDFC"",""613915"",""000000"",""E40303"",""FF8C00"",""FFED00"",""008026"",""24408E"",""732982""],""Widths"":[0.4,0.2,0.4,1,1,1,1,1,1,1,1,1,1,1],""Probability"":1.0},{""Name"":""Bisexual"",""Colors"":[""D60270"",""9B4F96"",""0038A8""],""Widths"":[2,1,2],""Probability"":1.0},{""Name"":""Pansexual"",""Colors"":[""FF1c8D"",""FFd700"",""1AB3FF""],""Probability"":1.0},{""Name"":""Nonbinary"",""Colors"":[""FCF431"",""FCFCFC"",""9D59D2"",""282828""],""Probability"":1.0},{""Name"":""Lesbian"",""Colors"":[""D62800"",""FF9B56"",""FFFFFF"",""D462A6"",""A40062""],""Probability"":1.0},{""Name"":""Agender"",""Colors"":[""000000"",""BABABA"",""FFFFFF"",""BAF484"",""FFFFFF"",""BABABA"",""000000""],""Probability"":1.0},{""Name"":""Asexual"",""Colors"":[""000000"",""A4A4A4"",""FFFFFF"",""810081""],""Probability"":1.0},{""Name"":""Demisexual"",""Colors"":[""000000"",""FFFFFF"",""6E0071"",""D3D3D3""],""Widths"":[2.5,2.5,1,2.5],""Probability"":1.0},{""Name"":""Genderqueer"",""Colors"":[""B57FDD"",""FFFFFF"",""49821E""],""Probability"":1.0},{""Name"":""Genderfluid"",""Colors"":[""FE76A2"",""FFFFFF"",""BF12D7"",""000000"",""303CBE""],""Probability"":1.0},{""Name"":""Intersex"",""Colors"":[""FFD800"",""7902AA"",""FFD800""],""Widths"":[5,1,5],""Probability"":1.0},{""Name"":""Aromantic"",""Colors"":[""3BA740"",""A8D47A"",""FFFFFF"",""ABABAB"",""000000""],""Probability"":1.0},{""Name"":""Polyamory"",""Colors"":[""0000FF"",""FF0000"",""FFFF00"",""FF0000"",""000000""],""Widths"":[1,0.7,0.1,0.7,1],""Probability"":1.0}]";

    private class FlagSettings
    {
        public string Name;
        public string[] Colors;
        public float[] Widths;
        public float Probability = 1f;
    }

    private void Awake()
    {
        _count = RNG.value < 0.5f ? 1 : 2;
        Debug.Log("[Pride Battery] There " + (_count == 1 ? "is 1 battery" : "are 2 batteries") + " in this holder.");
        GetComponent<KMWidget>().OnQueryRequest += HandleRequest;

        Destroy(_count == 1 ? _prefabAA : _prefabD);

        GetComponent<KMModSettings>().RefreshSettings();
        var set = GetComponent<KMModSettings>().Settings;
        if (string.IsNullOrEmpty(set))
            set = DefaultSettings;

        var flags = JsonConvert.DeserializeObject<FlagSettings[]>(set);

        foreach (var rend in _count == 1 ? _renderersD : _renderersAA)
        {
            var choice = RNG.Range(0f, flags.Select(s => s.Probability).Sum());
            int ix = -1;
            while (choice > 0)
                choice -= flags[++ix].Probability;
            var flag = flags[ix];

            var name = flag.Name;
            if (string.IsNullOrEmpty(name) || !flag.Colors.All(IsValidColor))
                goto bad;
            var colors = flag.Colors.Select(ParseColor).ToArray();
            if (colors == null || colors.Length == 0)
                goto bad;
            var stops = flag.Widths == null
                ? Enumerable.Range(1, colors.Length).Select(x => (float)x / colors.Length).ToArray()
                : Enumerable.Range(1, colors.Length).Select(x => flag.Widths.Take(x).Sum() / flag.Widths.Sum()).ToArray();

            Debug.Log("[Pride Battery] Using gradient: " + name);
            goto good;

        bad:
            Debug.LogFormat("[Pride Battery] Bad custom gradient: {0}. Using the default: Trans", string.IsNullOrEmpty(name) ? "Unknown" : name);

            colors = new Color[]
            {
            new Color32(0x55, 0xcd, 0xfc, 0),
            new Color32(0xf7, 0xa8, 0xb8, 0),
            Color.white,
            new Color32(0xf7, 0xa8, 0xb8, 0),
            new Color32(0x55, 0xcd, 0xfc, 0)
            };
            stops = new float[]
            {
            0.2f, 0.4f, 0.6f, 0.8f, 1f
            };

        good:
            rend.material.SetColorArray("_colors", colors);
            rend.material.SetFloatArray("_colors_positions", stops);
            rend.material.SetInt("_stop_count", colors.Length);

            StartCoroutine(AnimateBattery(rend.material));
        }
    }

    private bool IsValidColor(string arg)
    {
        return arg.Length == 6 && arg.ToLowerInvariant().All("0123456789abcdef".Contains);
    }

    private Color ParseColor(string arg)
    {
        arg = arg.ToLowerInvariant();
        return new Color32(
            (byte)("0123456789abcdef".IndexOf(arg[0]) * 16 + "0123456789abcdef".IndexOf(arg[1])),
            (byte)("0123456789abcdef".IndexOf(arg[2]) * 16 + "0123456789abcdef".IndexOf(arg[3])),
            (byte)("0123456789abcdef".IndexOf(arg[4]) * 16 + "0123456789abcdef".IndexOf(arg[4])),
            0);
    }

    private IEnumerator AnimateBattery(Material material)
    {
        float
            scale = RNG.Range(0.8f, 1.2f),
            angle = RNG.Range(-10f, 10f),
            xfrequency = -RNG.Range(-0.4f, 0.4f),
            yfrequency = RNG.Range(-0.4f, 0.4f),
            scale_variance = RNG.Range(0.2f, 0.5f),
            angle_variance = RNG.Range(20f, 40f),
            xfrequency_variance = RNG.Range(0.1f, 0.4f),
            yfrequency_variance = RNG.Range(0.1f, 0.4f),
            scale_frequency = RNG.Range(0.1f, 0.5f),
            angle_frequency = RNG.Range(0.1f, 0.5f),
            xfrequency_frequency = RNG.Range(0.01f, 0.1f),
            yfrequency_frequency = RNG.Range(0.01f, 0.1f),
            time = RNG.Range(-100000f, -10000f);

        while (true)
        {
            time += Time.deltaTime;

            material.SetVector("_params", new Vector4(
                scale + scale_variance * Mathf.Sin(time * scale_frequency),
                (angle + angle_variance * Mathf.Sin(time * angle_frequency)) * Mathf.PI / 180f,
                xfrequency + xfrequency_variance * Mathf.Sin(time * xfrequency_frequency),
                yfrequency + yfrequency_variance * Mathf.Sin(time * yfrequency_frequency)
                ));
            yield return null;
        }
    }

    private string HandleRequest(string queryKey, string _)
    {
        if (queryKey != KMBombInfo.QUERYKEY_GET_BATTERIES)
            return null;

        return JsonConvert.SerializeObject(new Dictionary<string, int>() { { "numbatteries", _count } });
    }
}
