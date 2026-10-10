using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace MukaDownloadManager.Core;

/// <summary>
/// Original HTTP/HTTPS transfer engine. No IDM code or assets are used.
/// Uses persistent per-segment files for recovery after pause or restart.
/// </summary>
public sealed class DownloadEngine : IDisposable
{
    private readonly HttpClient _http;
    public DownloadEngine()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.None,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(20),
            MaxConnectionsPerServer = 24
        };
        _http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MukaDownloadManager/0.1 (+https://github.com/MukaSanches/Muka-Download-Manager)");
    }

    public async Task TransferAsync(DownloadItem item, int segments, int limitKiB,
        Action<long,long,double> report, CancellationToken ct)
    {
        var uri = new Uri(item.Url, UriKind.Absolute);
        if (uri.Scheme is not ("http" or "https"))
            throw new NotSupportedException("Only HTTP and HTTPS links are supported.");

        var directory = Path.GetDirectoryName(item.FilePath);
        if (string.IsNullOrWhiteSpace(directory)) throw new IOException("Invalid destination path.");
        Directory.CreateDirectory(directory);
        var metadata = await ProbeAsync(uri, ct);
        if (segments > 1 && metadata.Length is > 2_097_152 && metadata.Ranges)
            await SegmentedAsync(item, metadata.Length.Value, Math.Clamp(segments, 1, 16), limitKiB, report, ct);
        else
            await SingleAsync(item, metadata.Length ?? 0, limitKiB, report, ct);
    }

    private sealed record Metadata(long? Length, bool Ranges);

    private async Task<Metadata> ProbeAsync(Uri uri, CancellationToken ct)
    {
        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, uri);
            using var resp = await _http.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.IsSuccessStatusCode && resp.Content.Headers.ContentLength is long size)
                return new(size, resp.Headers.AcceptRanges.Contains("bytes"));
        }
        catch (HttpRequestException) { }
        using var probe = new HttpRequestMessage(HttpMethod.Get, uri);
        probe.Headers.Range = new RangeHeaderValue(0, 0);
        using var result = await _http.SendAsync(probe, HttpCompletionOption.ResponseHeadersRead, ct);
        result.EnsureSuccessStatusCode();
        return new(result.Content.Headers.ContentRange?.Length ?? result.Content.Headers.ContentLength,
            result.StatusCode == HttpStatusCode.PartialContent && result.Content.Headers.ContentRange?.From == 0);
    }

    private async Task SingleAsync(DownloadItem item, long knownLength, int limitKiB,
        Action<long,long,double> report, CancellationToken ct)
    {
        string part = item.FilePath + ".muka-part";
        long previous = File.Exists(part) ? new FileInfo(part).Length : 0;
        using var request = new HttpRequestMessage(HttpMethod.Get, item.Url);
        if (previous > 0) request.Headers.Range = new RangeHeaderValue(previous, null);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable && knownLength == previous)
        {
            File.Move(part, item.FilePath, true);
            report(previous, previous, 0);
            return;
        }
        response.EnsureSuccessStatusCode();
        bool append = previous > 0 && response.StatusCode == HttpStatusCode.PartialContent
            && response.Content.Headers.ContentRange?.From == previous;
        if (!append) previous = 0;
        long total = append
            ? (response.Content.Headers.ContentRange?.Length ?? knownLength)
            : (response.Content.Headers.ContentLength ?? knownLength);
        var monitor = new ProgressMonitor(previous, total, report);
        var throttle = new ByteThrottle(limitKiB);
        await using var output = new FileStream(part, append ? FileMode.Append : FileMode.Create,
            FileAccess.Write, FileShare.Read, 131072, useAsync: true);
        await using var input = await response.Content.ReadAsStreamAsync(ct);
        byte[] buffer = new byte[131072];
        int count;
        while ((count = await input.ReadAsync(buffer, ct)) > 0)
        {
            await throttle.WaitAsync(count, ct);
            await output.WriteAsync(buffer.AsMemory(0, count), ct);
            monitor.Advance(count);
        }
        await output.FlushAsync(ct);
        if (total > 0 && output.Length != total)
            throw new IOException($"Incomplete HTTP transfer: {output.Length} of {total} bytes.");
        output.Close();
        File.Move(part, item.FilePath, true);
        monitor.Finish();
    }

    private async Task SegmentedAsync(DownloadItem item, long length, int segments, int limitKiB,
        Action<long,long,double> report, CancellationToken ct)
    {
        string partsDir = item.FilePath + ".muka-parts-" + item.Id.ToString("N");
        Directory.CreateDirectory(partsDir);
        var ranges = Enumerable.Range(0, segments).Select(i =>
        {
            long start = i * length / segments;
            long end = (i + 1) * length / segments - 1;
            return (start, end, path: Path.Combine(partsDir, $"{i:D2}.part"));
        }).ToArray();
        long existing = 0;
        foreach (var (start, end, path) in ranges)
        {
            long size = File.Exists(path) ? new FileInfo(path).Length : 0;
            if (size > end - start + 1)
            {
                File.Delete(path); size = 0;
            }
            existing += size;
        }
        var monitor = new ProgressMonitor(existing, length, report);
        var throttle = new ByteThrottle(limitKiB);
        await Task.WhenAll(ranges.Select(async range =>
        {
            var (start, end, path) = range;
            long resumed = File.Exists(path) ? new FileInfo(path).Length : 0;
            if (resumed == end - start + 1) return;
            using var request = new HttpRequestMessage(HttpMethod.Get, item.Url);
            request.Headers.Range = new RangeHeaderValue(start + resumed, end);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode != HttpStatusCode.PartialContent
                || response.Content.Headers.ContentRange?.From != start + resumed
                || response.Content.Headers.ContentRange?.To != end
                || response.Content.Headers.ContentRange?.Length != length)
                throw new IOException("Server returned an invalid byte range; the download was not assembled.");
            await using var output = new FileStream(path, FileMode.Append, FileAccess.Write,
                FileShare.Read, 131072, useAsync: true);
            await using var input = await response.Content.ReadAsStreamAsync(ct);
            byte[] buffer = new byte[131072];
            int count;
            while ((count = await input.ReadAsync(buffer, ct)) > 0)
            {
                if (output.Length + count > end - start + 1)
                    throw new IOException("Server returned more bytes than requested.");
                await throttle.WaitAsync(count, ct);
                await output.WriteAsync(buffer.AsMemory(0, count), ct);
                monitor.Advance(count);
            }
            await output.FlushAsync(ct);
            if (output.Length != end - start + 1)
                throw new IOException("Segment ended prematurely.");
        }));

        string assembly = item.FilePath + ".muka-assembling";
        try
        {
            await using (var destination = new FileStream(assembly, FileMode.Create, FileAccess.Write,
                FileShare.None, 131072, useAsync: true))
            {
                foreach (var (_, _, path) in ranges)
                {
                    await using var source = new FileStream(path, FileMode.Open, FileAccess.Read,
                        FileShare.Read, 131072, useAsync: true);
                    await source.CopyToAsync(destination, ct);
                }
                await destination.FlushAsync(ct);
                if (destination.Length != length)
                    throw new IOException("Assembled file length does not match server metadata.");
            }
            File.Move(assembly, item.FilePath, true);
            Directory.Delete(partsDir, recursive: true);
            monitor.Finish();
        }
        finally
        {
            if (File.Exists(assembly)) File.Delete(assembly);
        }
    }

    private sealed class ProgressMonitor(long initial, long total, Action<long,long,double> report)
    {
        private long _bytes = initial;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _lastBytes = initial;
        private long _lastTicks;
        private readonly object _gate = new();
        public void Advance(int count)
        {
            Interlocked.Add(ref _bytes, count);
            lock (_gate)
            {
                long now = _clock.ElapsedMilliseconds;
                if (now - _lastTicks < 220) return;
                long current = Interlocked.Read(ref _bytes);
                double speed = (current - _lastBytes) * 1000.0 / Math.Max(1, now - _lastTicks);
                report(current, total, speed);
                _lastBytes = current;
                _lastTicks = now;
            }
        }
        public void Finish() => report(Interlocked.Read(ref _bytes), total, 0);
    }

    private sealed class ByteThrottle(int limitKiB)
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly object _lock = new();
        private long _bytes;
        public async Task WaitAsync(int bytes, CancellationToken ct)
        {
            if (limitKiB <= 0) return;
            double delay;
            lock (_lock)
            {
                _bytes += bytes;
                delay = _bytes * 1000.0 / (limitKiB * 1024.0) - _clock.ElapsedMilliseconds;
            }
            if (delay > 1) await Task.Delay(TimeSpan.FromMilliseconds(delay), ct);
        }
    }

    public void Dispose() => _http.Dispose();
}
