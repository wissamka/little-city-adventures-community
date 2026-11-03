using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleCity
{
    // Explicit opt-in only; ordinary players never enter this automated tour.
    public sealed class IllustratedBenchmark : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--illustrated-benchmark") < 0) return;
            new GameObject("Illustrated benchmark").AddComponent<IllustratedBenchmark>();
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--benchmark-output");
            string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : Application.persistentDataPath;
            Directory.CreateDirectory(folder);
            var game = FindFirstObjectByType<CityGame>();
            float deadline = Time.realtimeSinceStartup + 120;
            while (game != null && !game.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            if (game == null || !game.Ready) { Debug.LogError("ILLUSTRATED_BENCHMARK_STARTUP_FAILED"); Application.Quit(2); yield break; }
            bool cityRun = Array.IndexOf(args,"--benchmark-city")>=0;
            game.Hud.BeginExploring();game.Worlds.VisitWorld(cityRun?0:4);game.Hud.SetTouchControls(false);
            if(!cityRun)game.SelectRide(Ride.Scooter);
            game.State.Place(cityRun?new Point(-300,0,-300):new Point(-133,0,34)); game.RefreshPlayer(); game.RecenterCamera();
            yield return new WaitForSecondsRealtime(3);
            var times = new List<float>(); var gpu = new List<double>(); var frames = new FrameTiming[1];
            for (int i = 0; i < 600; i++)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                times.Add(Time.unscaledDeltaTime * 1000);
                if (FrameTimingManager.GetLatestTimings(1, frames) > 0 && frames[0].gpuFrameTime > 0)
                    gpu.Add(frames[0].gpuFrameTime);
            }
            times.Sort(); gpu.Sort();
            double sum = 0; foreach (float t in times) sum += t;
            var lines = new List<string> {
                "Little City Skate City 0.8.2 standalone runtime measurement",
                "Device: " + SystemInfo.deviceModel,
                "CPU: " + SystemInfo.processorType,
                "GPU: " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType,
                "Resolution: " + Screen.width + " x " + Screen.height,
                "Target frame rate: " + Application.targetFrameRate,
                "Scene: " + (cityRun?"expanded city district":"Skatepark with scooter") + ", normal game update and HUD active",
                "Allocated Unity memory MB: " + (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576).ToString(),
                "Warmup: 3 seconds; samples: " + times.Count,
                "Mean frames/second (including frame cap): " + (1000 / (sum / times.Count)).ToString("F1"),
                "Frame milliseconds median / p95: " + times[times.Count/2].ToString("F2") + " / " + times[(int)(times.Count*.95)].ToString("F2"),
                "GPU timing samples: " + gpu.Count,
                "GPU milliseconds median: " + (gpu.Count > 0 ? gpu[gpu.Count/2].ToString("F2") : "unavailable"),
                "This Mac result does not establish A10 iPad performance."
            };
            File.WriteAllLines(Path.Combine(folder,cityRun?"standalone-city-performance.txt":"standalone-skate-performance.txt"),lines);
            yield return new WaitForEndOfFrame();
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder,cityRun?"standalone-city.png":"standalone-skatepark.png"),screenshot.EncodeToPNG()); Destroy(screenshot);
            Debug.Log("ILLUSTRATED_BENCHMARK_COMPLETE " + folder);
            Application.Quit(0);
        }
    }
}
