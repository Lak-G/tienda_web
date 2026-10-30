using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Session;

namespace TiendaNube.Tests.Apoyo;

public sealed class SesionEnMemoria : ISession
{
    private readonly Dictionary<string, byte[]> _datos = [];

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _datos.Keys;

    public void Clear() => _datos.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _datos.Remove(key);
    public void Set(string key, byte[] value) => _datos[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _datos.TryGetValue(key, out value);

    public IHttpContextAccessor ComoAccesor()
    {
        var contexto = new DefaultHttpContext();
        contexto.Features.Set<ISessionFeature>(new SessionFeature { Session = this });
        return new AccesorFijo(contexto);
    }

    // HttpContextAccessor guarda el contexto en un AsyncLocal y se perdería entre InitializeAsync y la prueba
    private sealed class AccesorFijo(HttpContext contexto) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = contexto;
    }
}
