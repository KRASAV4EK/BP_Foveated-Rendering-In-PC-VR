using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class GazepointGP3Client : MonoBehaviour
{
    [Header("Connection")]
    [Tooltip("IP address where Gazepoint Control is running.")]
    public string serverIP = "127.0.0.1";

    [Tooltip("Port used by Gazepoint Control API server (default 4242).")]
    public int serverPort = 4242;

    [Header("Debug")]
    [Tooltip("Print some debug info to the Unity Console.")]
    public bool logDebug = true;

    [Header("Gaze Position")]
    [Tooltip("Current position of user's gaze.")]
    public Vector2 _bestGazeViewport = new Vector2(0.5f, 0.5f); // (x,y) in Unity viewport coords

    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;
    private Thread _workerThread;
    private bool _stopRequested;

    // Shared gaze data (protected by lock)
    private readonly object _gazeLock = new object();
    private bool _bestGazeValid = false;

    // Public read-only accessors
    public Vector2 BestGazeViewport
    {
        get
        {
            lock (_gazeLock)
                return _bestGazeViewport;
        }
    }

    public bool BestGazeValid
    {
        get
        {
            lock (_gazeLock)
                return _bestGazeValid;
        }
    }

    private void Start()
    {
        _stopRequested = false;
        _workerThread = new Thread(WorkerLoop) { IsBackground = true };
        _workerThread.Start();
    }

    private void OnDestroy()
    {
        _stopRequested = true;

        try
        {
            _client?.Close();
        }
        catch { /* ignore */ }

        if (_workerThread != null && _workerThread.IsAlive)
        {
            try { _workerThread.Join(200); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Returns a gaze ray from the given camera through the current gaze point.
    /// Use only when BestGazeValid == true.
    /// </summary>
    public Ray GetGazeRay(Camera cam)
    {
        Vector2 vp;
        lock (_gazeLock)
            vp = _bestGazeViewport;

        return cam.ViewportPointToRay(new Vector3(vp.x, vp.y, 0f));
    }

    // ----------------- Worker thread -----------------

    private void WorkerLoop()
    {
        try
        {
            if (logDebug) Debug.Log("[Gazepoint] Connecting...");

            _client = new TcpClient();
            _client.Connect(serverIP, serverPort);

            NetworkStream stream = _client.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };

            if (logDebug) Debug.Log("[Gazepoint] Connected.");

            // Enable data streaming and Best POG (BPOGX, BPOGY, BPOGV)
            SendCommand("<SET ID=\"ENABLE_SEND_DATA\" STATE=\"1\" />");
            SendCommand("<SET ID=\"ENABLE_SEND_POG_FIX\" STATE=\"1\" />");

            // Main receive loop
            while (!_stopRequested && _client.Connected)
            {
                string line = _reader.ReadLine();
                if (line == null)
                    break;

                if (line.StartsWith("<REC"))
                    ParseRecLine(line);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[Gazepoint] Connection/worker error: " + ex.Message);
        }
        finally
        {
            if (logDebug) Debug.Log("[Gazepoint] Worker loop ended.");
        }
    }

    private void SendCommand(string xml)
    {
        // Gazepoint expects "\r\n" after each XML command.
        _writer.Write(xml);
        _writer.Write("\r\n");
        _writer.Flush();
        if (logDebug) Debug.Log("[Gazepoint ->] " + xml);
    }

    /// <summary>
    /// Parse a single <REC ... /> line and extract FPOGX / FPOGY / FPOGV.
    /// FPOGX/FPOGY are in [0..1] where (0,0)=top-left, (1,1)=bottom-right.
    /// We convert them into Unity viewport coords (0,0=bottom-left, 1,1=top-right).
    /// </summary>
    private void ParseRecLine(string line)
    {
        // Check validity flag first
        bool valid = ExtractBool(line, "FPOGV", defaultValue: false);

        if (!valid)
        {
            lock (_gazeLock)
            {
                _bestGazeValid = false;
            }
            return;
        }

        float gx = ExtractFloat(line, "FPOGX", float.NaN);
        float gy = ExtractFloat(line, "FPOGY", float.NaN);

        if (float.IsNaN(gx) || float.IsNaN(gy))
        {
            lock (_gazeLock)
            {
                _bestGazeValid = false;
            }
            return;
        }

        // Convert from Gazepoint coordinates (0,0 top-left, 1,1 bottom-right)
        // to Unity viewport coordinates (0,0 bottom-left, 1,1 top-right).
        float unityX = gx;
        float unityY = 1f - gy;

        if (logDebug) Debug.Log("\nX: " + unityX + " Y: " + unityY);

        lock (_gazeLock)
        {
            _bestGazeViewport = new Vector2(unityX, unityY);
            FoveatedVrsFeature.currentGazeUV = _bestGazeViewport;
            GazeMarker.currentGazeUV = _bestGazeViewport;
            _bestGazeValid = true;
        }
    }

    // ----------------- Small XML helpers -----------------

    private static float ExtractFloat(string line, string id, float defaultValue)
    {
        string pattern = id + "=\"";
        int idx = line.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return defaultValue;

        idx += pattern.Length;
        int end = line.IndexOf('"', idx);
        if (end < 0) return defaultValue;

        string number = line.Substring(idx, end - idx);
        if (float.TryParse(number, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture,
                           out float value))
        {
            return value;
        }

        return defaultValue;
    }

    private static bool ExtractBool(string line, string id, bool defaultValue)
    {
        string pattern = id + "=\"";
        int idx = line.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return defaultValue;

        idx += pattern.Length;
        int end = line.IndexOf('"', idx);
        if (end < 0) return defaultValue;

        string value = line.Substring(idx, end - idx);
        return value == "1";
    }
}
