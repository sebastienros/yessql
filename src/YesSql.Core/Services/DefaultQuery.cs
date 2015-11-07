using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YesSql.Core.Indexes;
using YesSql.Core.Query;
using YesSql.Core.Serialization;
using YesSql.Core.Sql;

namespace YesSql.Core.Services
{
    public class DefaultQuery : IQuery {
        private readonly Session _session;

        private List<Type> _bound = new List<Type>();
        private readonly DbConnection _connection;
        private readonly ISqlDialect _dialect;
        private readonly DbTransaction _transaction;
        private string _lastParameterName;
        private SqlBuilder _sqlBuilder;

        public static Dictionary<MethodInfo, Action<DefaultQuery, StringBuilder, MethodCallExpression>> MethodMappings = 
            new Dictionary<MethodInfo, Action<DefaultQuery, StringBuilder, MethodCallExpression>>();

        static DefaultQuery()
        {
            MethodMappings[typeof(String).GetMethod("StartsWith", new Type[] { typeof(string) })] = (query, builder, expression) =>
            {
                builder.Append("(");
                query.Convert(builder, expression.Object);
                builder.Append(" like ");
                query.Convert(builder, expression.Arguments[0]);
                var parameter = query._sqlBuilder.Parameters[query._lastParameterName];
                query._sqlBuilder.Parameters[query._lastParameterName] = parameter.ToString() + "%";
                builder.Append(")");
            };

            MethodMappings[typeof(String).GetMethod("EndsWith", new Type[] { typeof(string) })] = (query, builder, expression) =>
            {
                builder.Append("(");
                query.Convert(builder, expression.Object);
                builder.Append(" like ");
                query.Convert(builder, expression.Arguments[0]);
                var parameter = query._sqlBuilder.Parameters[query._lastParameterName];
                query._sqlBuilder.Parameters[query._lastParameterName] = "%" + parameter.ToString();
                builder.Append(")");

            };

            MethodMappings[typeof(String).GetMethod("Contains", new Type[] { typeof(string) })] = (query, builder, expression) =>
            {
                builder.Append("(");
                query.Convert(builder, expression.Object);
                builder.Append(" like ");
                query.Convert(builder, expression.Arguments[0]);
                var parameter = query._sqlBuilder.Parameters[query._lastParameterName];
                query._sqlBuilder.Parameters[query._lastParameterName] = "%" + parameter.ToString() + "%";
                builder.Append(")");
            };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsIn", new Type[] { typeof(string), typeof(IEnumerable<string>) })] =
            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsIn", new Type[] { typeof(int), typeof(IEnumerable<int>) })] =
                (query, builder, expression) =>
            {
                query.Convert(builder, expression.Arguments[0]);
                builder.Append(" in (");
                var values = (Expression.Lambda(expression.Arguments[1]).Compile().DynamicInvoke() as IEnumerable<object>).ToArray();
                for(var i=0; i<values.Length; i++)
                {
                    query.Convert(builder, Expression.Constant(values[i]));
                    if (i < values.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }
                builder.Append(")");
            };
        }

        public DefaultQuery(DbConnection connection, DbTransaction transaction, Session session)
        {
            _connection = connection;
            _transaction = transaction;
            _session = session;
            _dialect = SqlDialectFactory.For(connection);
            _sqlBuilder = new SqlBuilder();
        }
        
        private void Bind<TIndex>() where TIndex : Index
        {
            if(_bound.Contains(typeof(TIndex)))
            {
                return;
            }

            var name = typeof(TIndex).Name;
            _bound.Add(typeof(TIndex));

            if (typeof(MapIndex).IsAssignableFrom(typeof(TIndex)))
            {
                // inner join [PersonByName] on [PersonByName].[Id] = [Document].[Id]
                _sqlBuilder.InnerJoin(name, name, "Id", "Document", "Id");
            }
            else
            {
                var bridgeName = name + "_Document";

                // inner join [ArticlesByDay_Document] on [Document].[Id] = [ArticlesByDay_Document].[DocumentId]
                _sqlBuilder.InnerJoin(bridgeName, "Document", "Id", bridgeName, "DocumentId");

                // inner join [ArticlesByDay] on [ArticlesByDay_Document].[ArticlesByDayId] = [ArticlesByDay].[Id]
                _sqlBuilder.InnerJoin(name, bridgeName, name + "Id", name, "Id");
            }
        }

        private void Page(int count, int skip)
        {
            _sqlBuilder.Skip(skip);
            _sqlBuilder.Take(count);
        }

        private void Filter<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : Index
        {
            // For<T> hasn't been called already
            if (String.IsNullOrEmpty(_sqlBuilder.Clause))
            {
                _bound.Clear();
                _bound.Add(typeof(TIndex));

                _sqlBuilder.Select();
                _sqlBuilder.Table(typeof(TIndex).Name);
                _sqlBuilder.Selector(typeof(TIndex).Name, "DocumentId");
            }

            var builder = new StringBuilder();
            // if Filter is called, the Document type is implicit so there is no need to filter on TIndex 
            Convert(builder, predicate.Body);
            _sqlBuilder.WhereAlso(builder.ToString());
        }

        public void Convert(StringBuilder builder, Expression expression)
        {
            if (!IsParameterBased(expression))
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                Convert(builder, Expression.Constant(value));
                return;
            }

            switch (expression.NodeType)
            {
                case ExpressionType.LessThan:
                    ConvertBinaryExpression(builder, (BinaryExpression)expression, " < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    ConvertBinaryExpression(builder, (BinaryExpression)expression, " <= ");
                    break;
                case ExpressionType.GreaterThan:
                    ConvertBinaryExpression(builder, (BinaryExpression)expression, " > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    ConvertBinaryExpression(builder, (BinaryExpression)expression, " >= ");
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    ConvertBinaryExpression(builder, (BinaryExpression)expression, " and ");
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    ConvertBinaryExpression(builder, (BinaryExpression)expression, " or ");
                    break;
                case ExpressionType.Equal:
                    ConvertBinaryExpression(builder, (BinaryExpression)expression, " = ");
                    break;
                case ExpressionType.NotEqual:
                    ConvertBinaryExpression(builder, (BinaryExpression)expression, " <> ");
                    break;
                case ExpressionType.IsTrue:
                    Convert(builder, ((UnaryExpression)expression).Operand);
                    break;
                case ExpressionType.IsFalse:
                    builder.Append(" not ");
                    Convert(builder, ((UnaryExpression)expression).Operand);
                    break;
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;
                    builder.Append(_bound.Last().Name + "." +  memberExpression.Member.Name);
                    break;
                case ExpressionType.Constant:
                    _lastParameterName = "@p" + _sqlBuilder.Parameters.Count.ToString();
                    _sqlBuilder.Parameters.Add(_lastParameterName, ((ConstantExpression)expression).Value);
                    builder.Append(_lastParameterName);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)expression;
                    var methodInfo = methodCallExpression.Method;
                    Action<DefaultQuery, StringBuilder, MethodCallExpression> action;
                    if (MethodMappings.TryGetValue(methodInfo, out action))
                    {
                        action(this, builder, methodCallExpression);
                    }
                    else
                    {
                        throw new ArgumentException("Not supported method: " + methodInfo.Name);
                    }
                    break;
                default:
                    throw new ArgumentException("Not supported expression: " + expression);
            }
        }

        /// <summary>
        /// Return true if an expression path is based on the parameter of the predicate.
        /// If false it means the expression should be evaluated, otherwise converted to 
        /// its sql equivalent.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool IsParameterBased(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                    return true;
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    var binaryExpression = (BinaryExpression)expression;
                    return IsParameterBased(binaryExpression.Left) || IsParameterBased(binaryExpression.Right);
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.Constant:
                    return true;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)expression;

                    if(methodCallExpression.Object == null)
                    {
                        // Static call
                        return IsParameterBased(methodCallExpression.Arguments[0]);
                    }

                    return IsParameterBased(methodCallExpression.Object);
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;

                    if (memberExpression.Expression == null)
                    {
                        // Static method
                        return false;
                    }

                    return IsParameterBased(memberExpression.Expression);
                default:
                    return false;
            }
        }

        private void ConvertBinaryExpression(StringBuilder builder, BinaryExpression expression, string operation)
        {
            builder.Append("(");
            Convert(builder, expression.Left);
            builder.Append(operation);
            Convert(builder, expression.Right);
            builder.Append(")");
        }

        private void OrderBy<T>(Expression<Func<T, object>> keySelector)
        {
            var builder = new StringBuilder();
            Convert(builder, keySelector.Body);
            _sqlBuilder.OrderBy(builder.ToString());
        }

        private void ThenBy<T>(Expression<Func<T, object>> keySelector)
        {
            var builder = new StringBuilder();
            Convert(builder, keySelector.Body);
            _sqlBuilder.ThenOrderBy(builder.ToString());
        }

        private void OrderByDescending<T>(Expression<Func<T, object>> keySelector)
        {
            var builder = new StringBuilder();
            Convert(builder, keySelector.Body);
            _sqlBuilder.OrderByDescending(builder.ToString());
        }

        private void ThenByDescending<T>(Expression<Func<T, object>> keySelector)
        {
            var builder = new StringBuilder();
            Convert(builder, keySelector.Body);
            _sqlBuilder.ThenOrderByDescending(builder.ToString());
        }

        public async Task<int> CountAsync()
        {
            _sqlBuilder.Selector("count(*)");
            var sql = _sqlBuilder.ToSqlString(_dialect);
            return await _connection.ExecuteScalarAsync<int>(sql, _sqlBuilder.Parameters, _transaction);
        }

        IQuery<T> IQuery.For<T>()
        {
            _bound.Clear();
            _bound.Add(typeof(Document));

            _sqlBuilder.Select();
            _sqlBuilder.Table("Document");
            _sqlBuilder.WhereAlso("Document.Type = @Type"); // TODO: investigate, this makes the query 3 times slower on sqlite
            _sqlBuilder.Parameters["@Type"] = typeof(T).SimplifiedTypeName();

            return new Query<T>(this);
        }

        IQuery<TIndex> IQuery.ForIndex<TIndex>()
        {
            _bound.Clear();
            _bound.Add(typeof(TIndex));
            _sqlBuilder.Select();
            _sqlBuilder.Table(typeof(TIndex).Name);
            
            return new Query<TIndex>(this);
        }

        IQuery<object> IQuery.Any()
        {
            _bound.Clear();
            _bound.Add(typeof(Document));

            _sqlBuilder.Select();
            _sqlBuilder.Table("Document");
            _sqlBuilder.Selector("*");
            return new Query<object>(this);
        }

        class Query<T> : IQuery<T> where T : class
        {
            protected readonly DefaultQuery _query;

            public Query(DefaultQuery query) {
                _query = query;
            }
                        
            public async Task<T> FirstOrDefault()
            {
                _query.Page(1, 0);

                if (typeof(Index).IsAssignableFrom(typeof(T)))
                {
                    _query._sqlBuilder.Selector("*");
                    var sql = _query._sqlBuilder.ToSqlString(_query._dialect);
                    return (await _query._connection.QueryAsync<T>(sql, _query._sqlBuilder.Parameters, _query._transaction)).FirstOrDefault();
                }
                else
                {
                    _query._sqlBuilder.Selector("Document", "Id");
                    var sql = _query._sqlBuilder.ToSqlString(_query._dialect);
                    var ids = (await _query._connection.QueryAsync<int>(sql, _query._sqlBuilder.Parameters, _query._transaction)).ToArray();
                    
                    if(ids.Length == 0)
                    {
                        return default(T);
                    }

                    return await _query._session.GetAsync<T>(ids[0]);
                }
            }

            async Task<IEnumerable<T>> IQuery<T>.List()
            {
                if (typeof(Index).IsAssignableFrom(typeof(T)))
                {
                    _query._sqlBuilder.Selector("*");
                    var sql = _query._sqlBuilder.ToSqlString(_query._dialect);
                    return await _query._connection.QueryAsync<T>(sql, _query._sqlBuilder.Parameters, _query._transaction);
                }
                else
                {
                    _query._sqlBuilder.Selector("Document.*");
                    var sql = _query._sqlBuilder.ToSqlString(_query._dialect);
                    var documents = await _query._connection.QueryAsync<Document>(sql, _query._sqlBuilder.Parameters, _query._transaction);
                    return await _query._session.GetAsync<T>(documents.Select(x => x.Id));
                }
            }

            IQuery<T> IQuery<T>.Skip(int count)
            {
                _query._sqlBuilder.Skip(count);
                return this;
            }

            IQuery<T> IQuery<T>.Take(int count)
            {
                _query._sqlBuilder.Take(count);
                return this;
            }

            async Task<int> IQuery<T>.Count() 
            {
                return await _query.CountAsync();
            }

            IQuery<T, TIndex> IQuery<T>.With<TIndex>() 
            {
                _query.Bind<TIndex>();
                return new Query<T, TIndex>(_query);
            }

            IQuery<T, TIndex> IQuery<T>.With<TIndex>(Expression<Func<TIndex, bool>> predicate) 
            {
                _query.Bind<TIndex>();
                _query.Filter(predicate);
                return new Query<T, TIndex>(_query);
            }

            public IQuery<T> Where(string sql)
            {
                _query._sqlBuilder.WhereAlso(sql);
                return this;
            }

            IQuery<T> IQuery<T>.OrderBy(Expression<Func<T, object>> keySelector) {
                _query.OrderBy(keySelector);
                return this;
            }

            IQuery<T> IQuery<T>.OrderByDescending(Expression<Func<T, object>> keySelector) {
                _query.OrderByDescending(keySelector);
                return this;
            }

            IQuery<T> IQuery<T>.ThenBy(Expression<Func<T, object>> keySelector)
            {
                _query.ThenBy(keySelector);
                return this;
            }

            IQuery<T> IQuery<T>.ThenByDescending(Expression<Func<T, object>> keySelector)
            {
                _query.ThenByDescending(keySelector);
                return this;
            }
        }

        class Query<T, TIndex> : Query<T>, IQuery<T, TIndex> 
            where T : class 
            where TIndex : Index
        {
            public Query(DefaultQuery query)
                : base(query) {
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(Expression<Func<TIndex, bool>> predicate) 
            {
                _query.Filter<TIndex>(predicate);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderBy<TKey>(Expression<Func<TIndex, object>> keySelector)
            {
                _query.OrderBy(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.ThenBy<TKey>(Expression<Func<TIndex, object>> keySelector)
            {
                _query.ThenBy(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderByDescending<TKey>(Expression<Func<TIndex, object>> keySelector)
            {
                _query.OrderByDescending(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.ThenByDescending<TKey>(Expression<Func<TIndex, object>> keySelector)
            {
                _query.ThenByDescending(keySelector);
                return this;
            }
        }
    }

    public static class DefaultQueryExtensions
    {
        public static bool IsIn(this string source, IEnumerable<string> values)
        {
            return false;
        }
        public static bool IsIn(this int source, IEnumerable<int> values)
        {
            return false;
        }
    }

}
