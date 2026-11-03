using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleCity
{
    // Opt-in device evidence from the actual player and GPU, not editor rendering.
    public static class StartupDiagnostics
    {
        static void SaveScreen(string path)
        {
            // CaptureScreenshot(filename) prepends persistentDataPath on iOS,
            // even for an absolute filename. Encode the captured frame directly.
            var screen = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, screen.EncodeToPNG()); }
            finally { Object.Destroy(screen); }
        }
        public static IEnumerator Capture(CityGame game, IList<RenderTexture> portraits)
        {
            string folder = Path.Combine(Application.persistentDataPath, "StartupValidation");
            Directory.CreateDirectory(folder);
            string request = Path.Combine(Application.persistentDataPath, "validate-ipad.request");
            if (File.Exists(request)) File.Delete(request);
            for (int i = 0; i < portraits.Count; i++)
            {
                RenderTexture previous = RenderTexture.active;
                var pixels = new Texture2D(portraits[i].width, portraits[i].height, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = portraits[i];
                    pixels.ReadPixels(new Rect(0, 0, pixels.width, pixels.height), 0, 0);
                    pixels.Apply();
                    int colored = 0;
                    foreach (Color32 p in pixels.GetPixels32())
                        if (p.a > 200 && Mathf.Max(p.r, Mathf.Max(p.g, p.b)) - Mathf.Min(p.r, Mathf.Min(p.g, p.b)) > 45) colored++;
                    File.WriteAllBytes(Path.Combine(folder, "portrait-" + i + ".png"), pixels.EncodeToPNG());
                    Debug.Log("LITTLE_CITY_PORTRAIT index=" + i + " coloredPixels=" + colored);
                }
                finally { RenderTexture.active = previous; Object.Destroy(pixels); }
            }
            yield return new WaitForEndOfFrame();
            SaveScreen(Path.Combine(folder, "character-selection.png"));
            yield return new WaitForSeconds(2);
            game.Hud.BeginExploring(); game.RecenterCamera();
            yield return new WaitForSeconds(2);
            yield return new WaitForEndOfFrame();
            SaveScreen(Path.Combine(folder, "city-ready.png"));
            yield return null;
            var controls = new List<string>();
            yield return TouchControlChecks.Run(game, controls);
            File.WriteAllLines(Path.Combine(folder, "touch-controls.txt"), controls);
            Debug.Log("LITTLE_CITY_TOUCH_CHECKS_PASSED cases=" + controls.Count);
            game.Hud.ShowCharacters();
            Debug.Log("LITTLE_CITY_DEVICE_VALIDATION_COMPLETE " + folder);
        }
    }
}
