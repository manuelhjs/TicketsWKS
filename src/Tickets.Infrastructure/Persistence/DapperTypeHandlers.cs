using System.Data;
using Dapper;

namespace Tickets.Infrastructure.Persistence;

/// <summary>Mapea DateOnly (C#) &lt;-&gt; date (SQL Server).</summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}

/// <summary>Mapea TimeOnly (C#) &lt;-&gt; time (SQL Server).</summary>
public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override TimeOnly Parse(object value) => value switch
    {
        TimeSpan ts => TimeOnly.FromTimeSpan(ts),
        DateTime dt => TimeOnly.FromDateTime(dt),
        string s => TimeOnly.Parse(s),
        _ => throw new DataException($"No se puede convertir {value.GetType()} a TimeOnly.")
    };

    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }
}

public static class DapperConfig
{
    private static bool _configured;
    private static readonly object Gate = new();

    public static void Register()
    {
        if (_configured) return;
        lock (Gate)
        {
            if (_configured) return;
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());
            _configured = true;
        }
    }
}
