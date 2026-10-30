using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TiendaNube.Tests.Apoyo;

/// <summary>
/// Retiene a cada participante hasta que llegan todos. Sirve para forzar que dos compras lean
/// el mismo stock antes de que cualquiera de las dos lo guarde, sin depender del azar del planificador.
/// </summary>
public sealed class PuntoDeEncuentro(int participantes)
{
    private int _llegados;
    private readonly TaskCompletionSource _todos = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task EsperarAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _llegados) == participantes)
            _todos.TrySetResult();

        return _todos.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
    }
}

/// <summary>
/// Detiene el primer SaveChanges de su contexto en el punto de encuentro y cuenta los
/// conflictos de concurrencia (UPDATE ... WHERE xmin = ... que afectó 0 filas).
/// </summary>
public sealed class EsperaAntesDeGuardar(PuntoDeEncuentro punto) : SaveChangesInterceptor
{
    private bool _yaEspero;

    public int Conflictos { get; private set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!_yaEspero)
        {
            _yaEspero = true;
            await punto.EsperarAsync(cancellationToken);
        }

        return result;
    }

    public override ValueTask<InterceptionResult> ThrowingConcurrencyExceptionAsync(
        ConcurrencyExceptionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        Conflictos++;
        return ValueTask.FromResult(result);
    }
}
