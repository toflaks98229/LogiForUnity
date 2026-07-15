namespace Loupedeck.LogiForUnityPlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    // Unity 에디터 컴패니언과 통신하는 서버.
    //
    // 프로토콜: 127.0.0.1 TCP, UTF-8, 줄바꿈으로 구분되는 JSON 한 줄.
    // 플러그인이 서버인 이유는, Unity가 도메인 리로드로 수시로 죽었다 살아나기 때문이다.
    // 재접속하는 쪽이 클라이언트인 편이 훨씬 단순하다.
    //
    // 컴패니언은 핸드셰이크 파일(bridge.json)에서 포트와 토큰을 읽어 접속한다.

    internal sealed class UnityBridgeServer : IDisposable
    {
        // 컴패니언이 접속 정보를 찾는 위치. Unity 쪽 LogiBridgeClient.cs와 반드시 일치해야 한다.
        public static String HandshakeFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Logi", "LogiForUnity", "bridge.json");

        private readonly Object _lock = new Object();
        private readonly String _token = Guid.NewGuid().ToString("N");

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile Boolean _running;

        private TcpClient _client;
        private StreamWriter _writer;

        // 컴패니언이 접속해 핸드셰이크를 마쳤는지 여부.
        public Boolean IsConnected
        {
            get
            {
                lock (this._lock)
                {
                    return this._client != null && this._client.Connected;
                }
            }
        }

        // 컴패니언이 보고한 Unity 버전과 프로젝트 경로. 진단용.
        public String UnityVersion { get; private set; }

        public String ProjectPath { get; private set; }

        // 컴패니언이 보고한 에디터 상태. 이 상태에서 프로젝트에 파일을 쓰면 재컴파일이 걸려 플레이 모드가 튕긴다.
        // 컴패니언이 연결되어 있지 않으면 알 수 없으므로 false로 남는다.
        public Boolean IsEditorBusy { get; private set; }

        // 에디터가 플레이/컴파일을 벗어났을 때, 미뤄둔 작업을 다시 시도할 수 있도록 알린다.
        public event EventHandler EditorStateChanged;

        // 컴패니언이 다이얼 값을 보고했다. 인자는 대상 이름(예: "move.x").
        public event EventHandler<String> AdjustmentValueReported;

        // 컴패니언이 마지막으로 보고한 다이얼 값. 값이 없으면(선택된 오브젝트 없음 등) 항목이 없다.
        private readonly Dictionary<String, String> _adjustmentValues = new Dictionary<String, String>();

        public String TryGetAdjustmentValue(String target)
        {
            lock (this._lock)
            {
                return this._adjustmentValues.TryGetValue(target ?? String.Empty, out var value) ? value : null;
            }
        }

        public void Start()
        {
            // 포트 0을 요청해 OS가 빈 포트를 고르게 한다. 고정 포트는 충돌하고, 여러 인스턴스를 막는다.
            this._listener = new TcpListener(IPAddress.Loopback, 0);
            this._listener.Start();

            var port = ((IPEndPoint)this._listener.LocalEndpoint).Port;
            this.WriteHandshakeFile(port);

            this._running = true;
            this._acceptThread = new Thread(this.AcceptLoop) { IsBackground = true, Name = "UnityBridgeAccept" };
            this._acceptThread.Start();

            PluginLog.Info($"Unity bridge listening on 127.0.0.1:{port}");
        }

        private void WriteHandshakeFile(Int32 port)
        {
            var path = HandshakeFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, $"{{\"port\":{port},\"token\":\"{this._token}\"}}", new UTF8Encoding(false));
        }

        private void AcceptLoop()
        {
            while (this._running)
            {
                try
                {
                    var client = this._listener.AcceptTcpClient();

                    // 클라이언트마다 별도 스레드를 준다. accept 루프에서 직접 처리하면, 컴패니언이 소켓을 닫지 않고
                    // 죽었을 때(에디터 강제 종료 등) 다음 접속을 영영 받지 못한다.
                    new Thread(() => this.HandleClient(client)) { IsBackground = true, Name = "UnityBridgeClient" }.Start();
                }
                catch (Exception ex) when (!this._running)
                {
                    // Stop() 중 리스너가 닫히면서 나는 예외. 정상 종료 경로다.
                    PluginLog.Verbose(ex, "Accept loop stopped");
                    return;
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Unity bridge accept failed");
                    Thread.Sleep(500);
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            client.NoDelay = true;

            var stream = client.GetStream();
            var reader = new StreamReader(stream, new UTF8Encoding(false));
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            // 첫 줄은 반드시 핸드셰이크여야 한다. 토큰이 틀리면 즉시 끊는다.
            var hello = reader.ReadLine();
            if (hello == null || !hello.Contains(this._token))
            {
                PluginLog.Warning("Unity bridge rejected a client with a bad handshake");
                client.Close();
                return;
            }

            this.UnityVersion = ExtractJsonString(hello, "unity");
            this.ProjectPath = ExtractJsonString(hello, "project");

            lock (this._lock)
            {
                // 도메인 리로드 후 새 연결이 오면 이전 연결은 이미 죽었다. 최신 연결이 이긴다.
                this.CloseClient();
                this._client = client;
                this._writer = writer;
            }

            PluginLog.Info($"Unity bridge connected: Unity {this.UnityVersion}, project '{this.ProjectPath}'");
            writer.WriteLine("{\"cmd\":\"welcome\"}");

            this.ReadLoop(client, reader);
        }

        private void ReadLoop(TcpClient client, StreamReader reader)
        {
            try
            {
                String line;
                while ((line = reader.ReadLine()) != null)
                {
                    PluginLog.Verbose($"Unity bridge received: {line}");
                    this.HandleInboundMessage(line);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Verbose(ex, "Unity bridge read loop ended");
            }

            lock (this._lock)
            {
                // 그 사이 새 연결로 교체되었다면 건드리지 않는다.
                if (ReferenceEquals(this._client, client))
                {
                    this.CloseClient();
                }
            }

            PluginLog.Info("Unity bridge disconnected");

            // 연결이 끊기면 에디터가 바쁜지 알 수 없게 되고, IsEditorBusy 는 false 로 되돌아간다.
            // 이것도 상태 변화다. 미뤄둔 설치가 있다면 사용자가 Unity 로 포커스를 옮길 때까지 기다릴 필요가 없다.
            this.EditorStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void HandleInboundMessage(String line)
        {
            var command = ExtractJsonString(line, "cmd");

            if (command == "value")
            {
                this.HandleAdjustmentValue(line);
                return;
            }

            if (command != "state")
            {
                return;
            }

            var busy = ExtractJsonBoolean(line, "playing") || ExtractJsonBoolean(line, "compiling");
            if (busy == this.IsEditorBusy)
            {
                return;
            }

            this.IsEditorBusy = busy;
            PluginLog.Info($"Unity editor is now {(busy ? "busy (playing or compiling)" : "idle")}");

            this.EditorStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void HandleAdjustmentValue(String line)
        {
            var target = ExtractJsonString(line, "target");
            if (target == null)
            {
                return;
            }

            // text 는 값이 없을 때 JSON null 로 온다. 선택된 오브젝트가 없다는 뜻이다.
            var text = ExtractJsonString(line, "text");

            lock (this._lock)
            {
                if (text == null)
                {
                    this._adjustmentValues.Remove(target);
                }
                else
                {
                    this._adjustmentValues[target] = text;
                }
            }

            this.AdjustmentValueReported?.Invoke(this, target);
        }

        // 컴패니언에 명령 한 줄을 보낸다. 연결이 없으면 false를 돌려준다.
        public Boolean TrySend(String json)
        {
            lock (this._lock)
            {
                if (this._writer == null || this._client == null || !this._client.Connected)
                {
                    return false;
                }

                try
                {
                    this._writer.WriteLine(json);
                    return true;
                }
                catch (Exception ex)
                {
                    PluginLog.Warning(ex, "Unity bridge send failed");
                    this.CloseClient();
                    return false;
                }
            }
        }

        private void CloseClient()
        {
            try { this._writer?.Dispose(); } catch { }
            try { this._client?.Close(); } catch { }
            this._writer = null;
            this._client = null;

            // 컴패니언이 없으면 에디터 상태를 알 수 없다. 마지막으로 본 값을 계속 믿으면 안 된다.
            this.IsEditorBusy = false;
        }

        // 의존성 없이 쓰는 최소 JSON 문자열 추출. 핸드셰이크 한 줄만 읽으면 되므로 파서는 과하다.
        private static String ExtractJsonString(String json, String key)
        {
            var marker = $"\"{key}\"";
            var i = json.IndexOf(marker, StringComparison.Ordinal);
            if (i < 0)
            {
                return null;
            }

            var colon = json.IndexOf(':', i + marker.Length);
            if (colon < 0)
            {
                return null;
            }

            // 값이 문자열이 아닐 수 있다("text":null). 이때 다음 따옴표를 그냥 찾으면 엉뚱한 키를 값으로 읽는다.
            var start = colon + 1;
            while (start < json.Length && Char.IsWhiteSpace(json[start]))
            {
                start++;
            }

            if (start >= json.Length || json[start] != '"')
            {
                return null;
            }

            var end = json.IndexOf('"', start + 1);
            return end < 0 ? null : json.Substring(start + 1, end - start - 1);
        }

        private static Boolean ExtractJsonBoolean(String json, String key)
        {
            var marker = $"\"{key}\"";
            var i = json.IndexOf(marker, StringComparison.Ordinal);
            if (i < 0)
            {
                return false;
            }

            var colon = json.IndexOf(':', i + marker.Length);
            if (colon < 0)
            {
                return false;
            }

            // 다음 구분자까지가 값이다. "playing":true 뒤에는 콤마나 닫는 중괄호가 온다.
            var end = json.IndexOfAny(new[] { ',', '}' }, colon + 1);
            var value = (end < 0 ? json.Substring(colon + 1) : json.Substring(colon + 1, end - colon - 1)).Trim();
            return value == "true";
        }

        public void Dispose()
        {
            this._running = false;

            try { this._listener?.Stop(); } catch { }

            lock (this._lock)
            {
                this.CloseClient();
            }

            try { File.Delete(HandshakeFilePath); } catch { }

            PluginLog.Info("Unity bridge stopped");
        }
    }
}
