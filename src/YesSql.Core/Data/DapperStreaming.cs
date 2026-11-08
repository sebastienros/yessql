using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;

namespace YesSql.Data
{
    internal static class DapperStreaming
    {
        /// <summary>
        /// Streams the rows of a query, like Dapper's <c>QueryUnbufferedAsync</c>.
        /// </summary>
        /// <remarks>
        /// Dapper's <c>QueryUnbufferedAsync</c> cancels its command when it disposes a reader that is still open, which
        /// is the case even after all the rows have been read. Cancelling is expensive for most drivers: Npgsql opens a
        /// new connection to send a cancel request, and SqlClient sends an attention signal. Here the reader is only
        /// disposed, as with any other query.
        /// </remarks>
        public static async IAsyncEnumerable<T> QueryStreamAsync<T>(this DbConnection connection, CommandDefinition command, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var reader = await connection.ExecuteReaderAsync(command, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);

            var parser = reader.GetRowParser<T>();

            while (await reader.ReadAsync(cancellationToken))
            {
                yield return parser(reader);
            }
        }
    }
}
