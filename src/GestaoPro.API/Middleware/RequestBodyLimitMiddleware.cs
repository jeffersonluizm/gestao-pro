using Microsoft.AspNetCore.Http.Features;

namespace GestaoPro.API.Middleware;

// Teto do corpo da requisição em toda a API (padrão 64 KB; nada aqui envia arquivo). O Kestrel aplica o mesmo
// limite (Program.cs); este middleware responde 413 com mensagem legível antes de o corpo ser lido.
public sealed class RequestBodyLimitMiddleware
{
    public const long DefaultMaxBytes = 64 * 1024;
    public const string TooLargeMessage = "Os dados enviados são grandes demais. Encurte os textos e tente de novo.";

    private readonly RequestDelegate _next;
    private readonly long _maxBytes;

    public RequestBodyLimitMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _maxBytes = MaxBytes(configuration);
    }

    public static long MaxBytes(IConfiguration configuration) =>
        Math.Max(1024, configuration.GetValue("Limites:CorpoMaximoBytes", DefaultMaxBytes));

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > _maxBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new { message = TooLargeMessage });
            return;
        }

        // Corpo sem Content-Length (chunked): o servidor corta no mesmo limite, e o corpo lido passa por um contador.
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false }) feature.MaxRequestBodySize = _maxBytes;
        if (context.Request.ContentLength is null && context.Request.Body != Stream.Null)
            context.Request.Body = new LimitedReadStream(context.Request.Body, _maxBytes);

        try
        {
            await _next(context);
        }
        catch (Exception error) when (!context.Response.HasStarted && FindBadRequest(error) is { } ex)
        {
            // O limite estourou no meio da leitura: responde com o status da exceção (413), sem virar 500.
            context.Response.Clear();
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new
            {
                message = ex.StatusCode == StatusCodes.Status413PayloadTooLarge ? TooLargeMessage : "Requisição inválida.",
            });
        }
    }

    // A leitura do corpo pode chegar embrulhada (por exemplo, pela leitura do JSON): procura na cadeia de exceções.
    private static BadHttpRequestException? FindBadRequest(Exception? error)
    {
        for (; error is not null; error = error.InnerException)
            if (error is BadHttpRequestException bad) return bad;
        return null;
    }

    // Lê o corpo contando os bytes; acima do limite, BadHttpRequestException com 413 (o mesmo que o Kestrel lança).
    private sealed class LimitedReadStream(Stream inner, long maxBytes) : Stream
    {
        private long _read;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Count(await inner.ReadAsync(buffer, cancellationToken));

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Count(await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken));

        private int Count(int read)
        {
            _read += read;
            if (_read > maxBytes)
                throw new BadHttpRequestException(TooLargeMessage, StatusCodes.Status413PayloadTooLarge);
            return read;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
