using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace FlashFortune.Application.Tests.TestHelpers;

/// <summary>
/// NSubstitute-based helpers for creating async-queryable DbSet stubs from in-memory lists.
/// Supports FindAsync, AnyAsync, and ToListAsync patterns used by MediatR handlers.
/// </summary>
public static class MockDbSetExtensions
{
    /// <summary>
    /// Creates a substituted DbSet backed by an in-memory list.
    /// Supports LINQ async operators (ToListAsync, AnyAsync, FirstOrDefaultAsync, etc.)
    /// and FindAsync via primary key lookup.
    /// </summary>
    public static DbSet<T> CreateMockDbSet<T>(this IEnumerable<T> source) where T : class
    {
        var data = source.ToList();
        var queryable = data.AsQueryable();

        var mockSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();

        // Wire up IQueryable so LINQ operators work synchronously (for unit tests)
        ((IQueryable<T>)mockSet).Provider.Returns(
            new TestAsyncQueryProvider<T>(queryable.Provider));
        ((IQueryable<T>)mockSet).Expression.Returns(queryable.Expression);
        ((IQueryable<T>)mockSet).ElementType.Returns(queryable.ElementType);
        ((IQueryable<T>)mockSet).GetEnumerator().Returns(_ => queryable.GetEnumerator());

        // Wire up IAsyncEnumerable so ToListAsync works
        ((IAsyncEnumerable<T>)mockSet)
            .GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(_ => new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        // Wire up FindAsync: delegates to the in-memory list via key lookup
        mockSet.FindAsync(Arg.Any<object?[]?>())
            .Returns(callInfo =>
            {
                var keys = callInfo.Arg<object?[]?>();
                if (keys is null || keys.Length == 0)
                    return ValueTask.FromResult<T?>(null);

                // Find by matching key property on entity type
                var keyValue = keys[0];
                var idProp = typeof(T).GetProperty("Id");
                var match = idProp is null
                    ? null
                    : data.FirstOrDefault(e => Equals(idProp.GetValue(e), keyValue));
                return ValueTask.FromResult(match);
            });

        mockSet.FindAsync(Arg.Any<object?[]?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var keys = callInfo.ArgAt<object?[]?>(0);
                if (keys is null || keys.Length == 0)
                    return ValueTask.FromResult<T?>(null);

                var keyValue = keys[0];
                var idProp = typeof(T).GetProperty("Id");
                var match = idProp is null
                    ? null
                    : data.FirstOrDefault(e => Equals(idProp.GetValue(e), keyValue));
                return ValueTask.FromResult(match);
            });

        return mockSet;
    }

    /// <summary>
    /// Creates a mock IApplicationDbContext where each DbSet property is backed by the
    /// provided seed collections. Pass an empty list to simulate an empty table.
    /// </summary>
    public static FlashFortune.Application.Interfaces.IApplicationDbContext CreateMockContext(
        IEnumerable<FlashFortune.Domain.Entities.User>? users = null,
        IEnumerable<FlashFortune.Domain.Entities.BusinessUnit>? businessUnits = null,
        IEnumerable<FlashFortune.Domain.Entities.UserUnitRole>? userUnitRoles = null,
        IEnumerable<FlashFortune.Domain.Entities.Raffle>? raffles = null,
        IEnumerable<FlashFortune.Domain.Entities.Prize>? prizes = null,
        IEnumerable<FlashFortune.Domain.Entities.Participant>? participants = null,
        IEnumerable<FlashFortune.Domain.Entities.Winner>? winners = null)
    {
        var ctx = Substitute.For<FlashFortune.Application.Interfaces.IApplicationDbContext>();

        ctx.Users.Returns((users ?? []).CreateMockDbSet());
        ctx.BusinessUnits.Returns((businessUnits ?? []).CreateMockDbSet());
        ctx.UserUnitRoles.Returns((userUnitRoles ?? []).CreateMockDbSet());
        ctx.Raffles.Returns((raffles ?? []).CreateMockDbSet());
        ctx.Prizes.Returns((prizes ?? []).CreateMockDbSet());
        ctx.Participants.Returns((participants ?? []).CreateMockDbSet());
        ctx.Winners.Returns((winners ?? []).CreateMockDbSet());

        ctx.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        return ctx;
    }
}

/// <summary>
/// Async query provider that wraps a synchronous IQueryProvider.
/// Enables EF Core async LINQ operators (ToListAsync, AnyAsync, etc.) in tests.
/// </summary>
internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner)
    : IQueryProvider, IAsyncQueryProvider
{
    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(System.Linq.Expressions.Expression expression)
        => inner.Execute(expression);

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        => inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(
        System.Linq.Expressions.Expression expression,
        CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments().First();
        var executeMethod = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute))!
            .MakeGenericMethod(resultType);

        var result = executeMethod.Invoke(inner, [expression]);

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [result])!;
    }
}

internal sealed class TestAsyncEnumerable<T>(System.Linq.Expressions.Expression expression)
    : EnumerableQuery<T>(expression), IAsyncEnumerable<T>, IQueryable<T>
{
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask<bool> MoveNextAsync()
        => ValueTask.FromResult(inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
