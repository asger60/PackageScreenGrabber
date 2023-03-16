using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FarmandScreenGrabber : EditorWindow
{
    string folder = "Screenshots";
    string filenamePrefix = "screenshot";

    private string baseShotsFolder = "base";
    private Vector2 _scrollPosition;

    [SerializeField] private GUIStyle _boxStyle;
    [SerializeField] private GUIStyle _headerStyle;
    [SerializeField] private GUIStyle _labelStyle;


    struct ResolutionBlob
    {
        public string Name;
        public Vector2Int Resolution;

        public ResolutionBlob(string name, Vector2Int resolution)
        {
            Name = name;
            Resolution = resolution;
        }
    }

    //todo: maybe at some point put this stuff into a scriptable object to let the user define capture settings
    struct CaptureGroup
    {
        public string name;
        public bool captureEnabled;
        public List<ResolutionBlob> _ResolutionBlobs;

        public CaptureGroup(string name, List<ResolutionBlob> resolutionBlobs)
        {
            this.name = name;
            _ResolutionBlobs = resolutionBlobs;
            captureEnabled = false;
        }
    }

    private List<CaptureGroup> _captureGroups = new()
    {
        new CaptureGroup("base", new()
        {
            new ResolutionBlob("", new Vector2Int(Screen.width, Screen.height)),
        }),
        new CaptureGroup("iPhone", new()
        {
            new ResolutionBlob("6.5Display", new Vector2Int(1284, 2778)),
            new ResolutionBlob("5.5Display", new Vector2Int(1242, 2208)),
        }),
        new CaptureGroup("iPad", new()
        {
            new ResolutionBlob("12.9Display", new Vector2Int(2048, 2732)),
        }),
    };


    [MenuItem("Farmand/ScreenGrabber")]
    public static void ShowWindow()
    {
        var win = GetWindow<FarmandScreenGrabber>(false, "ScreenGrabber");
    }

    void OnGUI()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel);
        }

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
            };
        }

        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
            };
            var margin = _boxStyle.margin;
            margin.top += 10;
            _boxStyle.margin = margin;
        }

        using (new EditorGUILayout.VerticalScope(_boxStyle))
        {
            Section("Capture Settings");
            for (var i = 0; i < _captureGroups.Count; i++)
            {
                var group = _captureGroups[i];
                if (i == 0)
                {
                    group.captureEnabled = true;
                    group._ResolutionBlobs = new()
                    {
                        new ResolutionBlob("", new Vector2Int(Screen.width, Screen.height)),
                    };
                    _captureGroups[i] = group;
                    continue;
                }

                group.captureEnabled = EditorGUILayout.ToggleLeft(group.name, group.captureEnabled);
                _captureGroups[i] = group;
            }


            GUI.color = Color.green;
            if (GUILayout.Button("Capture", GUILayout.Height(60)))
            {
                string captureFileName = DateTime.Now.ToString("yyMMdd_HHmmss") + ".png";

                foreach (var captureGroup in _captureGroups)
                {
                    if (captureGroup.captureEnabled)
                    {
                        TakeScreenshot(captureGroup, captureFileName);
                    }
                }


                AssetDatabase.Refresh();
            }
        }

        GUI.color = Color.white;
        using (new EditorGUILayout.VerticalScope(_boxStyle))
        {

            
            Section("Screengrabs");


            string dir = "Assets/" + folder + "/" + baseShotsFolder + "/";
            string[] files = Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);


            foreach (var file in files)
            {
                using (new EditorGUILayout.VerticalScope(_boxStyle))
                {
                    EditorGUILayout.BeginHorizontal();

                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(file);
                    Texture2D myTexture = AssetPreview.GetAssetPreview(t);

                    GUILayout.Label(myTexture, GUILayout.Width(80));


                    EditorGUILayout.BeginVertical();
                    foreach (var filesList in GetCaptureGroupFiles(file))
                    {
                        if (EditorGUILayout.LinkButton(filesList))
                        {
                            EditorUtility.RevealInFinder(filesList);
                        }
                    }


                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                    if (GUILayout.Button("X", GUILayout.Width(40)))
                    {
                        DeleteDeviceFiles(file);
                        AssetDatabase.DeleteAsset(file);
                        AssetDatabase.Refresh();
                    }

                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }


    private void Section(string section, string submsg = null, bool topSpace = true)
    {
        if (topSpace) GUILayout.Space(10);
        GUILayout.Label(section, _headerStyle);
        if (submsg != null)
            GUILayout.Label(submsg, _labelStyle);
        GUILayout.Space(5);
    }

    void DeleteDeviceFiles(string file)
    {
        foreach (var captureGroup in _captureGroups)
        {
            foreach (var resolutionBlob in captureGroup._ResolutionBlobs)
            {
                string deleteFile = "Assets/" + folder + "/" + captureGroup.name + "/" + resolutionBlob.Name + "/" +
                                    resolutionBlob.Name +
                                    Path.GetFileName(file);
                AssetDatabase.DeleteAsset(deleteFile);
            }
        }
    }

    List<string> GetCaptureGroupFiles(string file)
    {
        List<string> returnList = new List<string>();
        foreach (var captureGroup in _captureGroups)
        {
            foreach (var resolutionBlob in captureGroup._ResolutionBlobs)
            {
                var filePath = "Assets/" + folder + "/" + captureGroup.name + "/" + resolutionBlob.Name + "/" +
                               resolutionBlob.Name + Path.GetFileName(file);
                if (File.Exists(filePath))
                    returnList.Add(filePath);
            }
        }

        return returnList;
    }

    private void TakeScreenshot(CaptureGroup group, string fileName)
    {
        foreach (var blob in group._ResolutionBlobs)
        {
            folder = GetSafePath(folder.Trim('/'));
            filenamePrefix = GetSafeFilename(blob.Name);

            string dir = Application.dataPath + "/" + folder + "/" + group.name + "/" + blob.Name + "/";
            string filename = filenamePrefix + fileName;
            string path = dir + filename;

            Camera cam = Camera.main;

            // Create Render Texture with width and height.
            RenderTexture rt = new RenderTexture(blob.Resolution.x, blob.Resolution.y, 0, RenderTextureFormat.RGB565);

            // Assign Render Texture to camera.
            cam.targetTexture = rt;


            // Render the camera's view to the Target Texture.
            cam.Render();


            // Save the currently active Render Texture so we can override it.
            RenderTexture currentRT = RenderTexture.active;

            // ReadPixels reads from the active Render Texture.
            RenderTexture.active = cam.targetTexture;

            // Make a new texture and read the active Render Texture into it.
            Texture2D screenshot = new Texture2D(blob.Resolution.x, blob.Resolution.y, TextureFormat.RGB565, false);
            screenshot.ReadPixels(new Rect(0, 0, blob.Resolution.x, blob.Resolution.y), 0, 0, false);

            // PNGs should be sRGB so convert to sRGB color space when rendering in linear.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                Color[] pixels = screenshot.GetPixels();
                for (int p = 0; p < pixels.Length; p++)
                {
                    pixels[p] = pixels[p].gamma;
                }

                screenshot.SetPixels(pixels);
            }

            // Apply the changes to the screenshot texture.
            screenshot.Apply(false);

            // Save the screnshot.
            Directory.CreateDirectory(dir);
            byte[] png = screenshot.EncodeToPNG();
            File.WriteAllBytes(path, png);

            // Remove the reference to the Target Texture so our Render Texture is garbage collected.
            cam.targetTexture = null;

            // Replace the original active Render Texture.
            RenderTexture.active = currentRT;
        }
    }

    private string GetSafePath(string path)
    {
        return string.Join("_", path.Split(Path.GetInvalidPathChars()));
    }

    private string GetSafeFilename(string filename)
    {
        return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
    }
}