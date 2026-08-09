using System.Data;
using System.Data.Common;

namespace KnowledgeTracker.Data.Database;

internal static class SqlServerCommandExtensions
{
    public static DbParameter AddParameter(
        this DbCommand command,
        string name,
        DbType type,
        object value
    )
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
        return parameter;
    }
}
