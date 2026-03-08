using System.Diagnostics;  
using System.Linq;  
using System.Net;  
using System.Net.Http;  
using System.Text;  
using System.Threading;  
using System.Threading.Tasks;  
using Content.Shared.Starlight.CCVar;  
using Prometheus;  
using Robust.Shared.Configuration;  
using Robust.Shared.Prototypes;  
using System;  
using System.Collections.Generic;  
using System.Security.Cryptography;  
using Content.Shared.Starlight.TextToSpeech;
  
namespace Content.Server.Starlight.TextToSpeech;  
  
public sealed class TTSManager : ITTSManager  
{  
    [Dependency] private readonly IConfigurationManager _cfg = default!;  
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;  
  
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(  
        "tts_req_timings",  
        "Timings of TTS API requests",  
        new HistogramConfiguration()  
        {  
            LabelNames = new[] {"type"},  
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),  
        });  
  
    private static readonly Counter WantedCount = Metrics.CreateCounter(  
        "tts_wanted_count",  
        "Amount of wanted TTS audio.");  
  
    private static readonly Counter ReusedCount = Metrics.CreateCounter(  
        "tts_reused_count",  
        "Amount of reused TTS audio from cache.");  
  
    private readonly HttpClient _httpClient = new();  
    private ISawmill _sawmill = default!;  
    private readonly Dictionary<string, byte[]> _cache = new();  
    private readonly Dictionary<string, SemaphoreSlim> _semaphores = new();  
    private readonly List<string> _cacheKeysSeq = new();  
    private int _maxCachedCount = 200;  
    private string _apiUrl = string.Empty;  
    private string _apiToken = string.Empty;  
  
    public void Initialize()  
    {  
        _sawmill = Logger.GetSawmill("tts");  
        _cfg.OnValueChanged(StarlightCCVars.TTSApiUrl, v => _apiUrl = v, true);  
        _cfg.OnValueChanged(StarlightCCVars.TTSApiToken, v => {  
            _httpClient.DefaultRequestHeaders.Authorization =  
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", v);  
            _apiToken = v;  
        }, true);  
    }  
  
    public async Task<byte[]?> ConvertTextToSpeechStandard(string voiceId, string text)  
    {  
        WantedCount.Inc();  
        return await ConvertTextToSpeech(voiceId, text, null);  
    }  
  
    public async Task<byte[]?> ConvertTextToSpeechRadio(string voiceId, string text)  
    {  
        WantedCount.Inc();  
        return await ConvertTextToSpeech(voiceId, text, "radio");  
    }  
  
    public async Task<byte[]?> ConvertTextToSpeechAnnounce(string voiceId, string text)  
    {  
        WantedCount.Inc();  
        return await ConvertTextToSpeech(voiceId, text, null);  
    }  
  
    private async Task<byte[]?> ConvertTextToSpeech(string voiceId, string text, string? effect)  
    {  
        // Проверяем существование прототипа голоса  
        if (!_prototypeManager.TryIndex<VoicePrototype>(voiceId, out var voiceProto))  
        {  
            _sawmill.Warning($"Voice prototype '{voiceId}' not found, using default 'scout'");  
            voiceId = "scout";  
        }  
  
        var cacheKey = GenerateCacheKey(voiceId, text, effect);  
        _sawmill.Verbose($"Cache key for '{text}' is '{cacheKey}'");  
        var semaphore = _semaphores.GetValueOrDefault(cacheKey, new SemaphoreSlim(1, 1));  
        _semaphores[cacheKey] = semaphore;  
          
        try  
        {  
            await semaphore.WaitAsync();  
            if (_cache.TryGetValue(cacheKey, out var data))  
            {  
                ReusedCount.Inc();  
                _sawmill.Verbose($"Use cached sound for '{text}' speech by '{voiceId}'({effect}) speaker");  
                return data;  
            }  
  
            _sawmill.Verbose($"Generate new audio for '{text}' speech by '{voiceId}'({effect}) speaker");  
  
            var reqTime = DateTime.UtcNow;  
            try  
            {  
                var timeout = _cfg.GetCVar(StarlightCCVars.TTSApiTimeout);  
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));  
                  
                if (effect == null)  
                    effect = "";  
                  
                // Используем ID прототипа как имя спикера для API  
                var requestUrl = $"{_apiUrl}/api/v1/tts?speaker={voiceId}&text={WebUtility.UrlEncode(text)}&ext=ogg";  
                var response = await _httpClient.GetAsync(requestUrl, cts.Token);  
                _sawmill.Debug($"Requested API URL: {requestUrl}");  
                  
                if (!response.IsSuccessStatusCode)  
                {  
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)  
                    {  
                        _sawmill.Warning($"TTS request for {text} was rate limited");  
                        return null;  
                    }  
  
                    _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}");  
                    return null;  
                }  
  
                var soundData = await response.Content.ReadAsByteArrayAsync();  
  
                _cache[cacheKey] = soundData;  
                _cacheKeysSeq.Add(cacheKey);  
                if (_cache.Count > _maxCachedCount)  
                {  
                    var firstKey = _cacheKeysSeq.First();  
                    _cache.Remove(firstKey);  
                    _cacheKeysSeq.Remove(firstKey);  
                }  
  
                _sawmill.Debug($"Generated new audio for '{text}' speech by '{voiceId}'({effect}) speaker ({soundData.Length} bytes)");  
                RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);  
  
                return soundData;  
            }  
            catch (TaskCanceledException)  
            {  
                RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);  
                _sawmill.Error($"Timeout of request generation new audio for '{text}' speech by '{voiceId}'({effect}) speaker");  
                return null;  
            }  
            catch (Exception e)  
            {  
                RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);  
                _sawmill.Error($"Failed of request generation new sound for '{text}' speech by '{voiceId}'({effect}) speaker\n{e}");  
                return null;  
            }  
        }  
        finally  
        {  
            _semaphores.Remove(cacheKey);  
            semaphore.Release();  
        }  
    }  
  
    public void ResetCache()  
    {  
        _cache.Clear();  
        _cacheKeysSeq.Clear();  
    }  
  
    private string GenerateCacheKey(string speaker, string text, string? effect)  
    {  
        var key = $"{speaker}[{effect}]/{text}";  
        byte[] keyData = Encoding.UTF8.GetBytes(key);  
        var sha256 = SHA256.Create();  
        var bytes = sha256.ComputeHash(keyData);  
        return Convert.ToHexString(bytes);  
    }  
}