using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Indexes;
using YesSql.Serialization;

namespace YesSql.Services
{
    public class DefaultQuery : IQuery
    {
        private readonly Session _session;

        private List<Type> _bound = new List<Type>();
        private readonly string _documentTable;
        private readonly IDbConnection _connection;
        private readonly ISqlDialect _dialect;
        private readonly IDbTransaction _transaction;
        private string _lastParameterName;
        private ISqlBuilder _sqlBuilder;
        private StringBuilder _builder = new StringBuilder();

        public static Dictionary<MethodInfo, Action<DefaultQuery, StringBuilder, ISqlDialect, MethodCallExpression>> MethodMappings =
            new Dictionary<MethodInfo, Action<DefaultQuery, StringBuilder, ISqlDialect, MethodCallExpression>>();

        static DefaultQuery()
        {
            MethodMappings[typeof(String).GetMethod("StartsWith", new Type[] { typeof(string) })] = (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Object);
                builder.Append(" like ");
                query.ConvertFragment(builder, expression.Arguments[0]);
                var parameter = query._sqlBuilder.Parameters[query._lastParameterName];
                query._sqlBuilder.Parameters[query._lastParameterName] = parameter.ToString() + "%";
                builder.Append(")");
            };

            MethodMappings[typeof(String).GetMethod("EndsWith", new Type[] { typeof(string) })] = (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Object);
                builder.Append(" like ");
                query.ConvertFragment(builder, expression.Arguments[0]);
                var parameter = query._sqlBuilder.Parameters[query._lastParameterName];
                query._sqlBuilder.Parameters[query._lastParameterName] = "%" + parameter.ToString();
                builder.Append(")");

            };

            MethodMappings[typeof(String).GetMethod("Contains", new Type[] { typeof(string) })] = (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Object);
                builder.Append(" like ");
                query.ConvertFragment(builder, expression.Arguments[0]);
                var parameter = query._sqlBuilder.Parameters[query._lastParameterName];
                query._sqlBuilder.Parameters[query._lastParameterName] = "%" + parameter.ToString() + "%";
                builder.Append(")");
            };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsIn", new Type[] { typeof(string), typeof(IEnumerable<string>) })] =
            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsIn", new Type[] { typeof(int), typeof(IEnumerable<int>) })] =
                (query, builder, dialect, expression) =>
                {
                    var values = (Expression.Lambda(expression.Arguments[1]).Compile().DynamicInvoke() as IEnumerable<object>).ToArray();

                    if (values.Length == 0)
                    {
                        builder.Append(" 1 = 0");
                    }
                    else if (values.Length == 1)
                    {
                        query.ConvertFragment(builder, expression.Arguments[0]);
                        builder.Append(" = " );
                        query.ConvertFragment(builder, Expression.Constant(values[0]));
                    }
                    else
                    {
                        query.ConvertFragment(builder, expression.Arguments[0]);
                        var elements = new StringBuilder();
                        for (var i = 0; i < values.Length; i++)
                        {
                            query.ConvertFragment(elements, Expression.Constant(values[i]));
                            if (i < values.Length - 1)
                            {
                                elements.Append(", ");
                            }
                        }

                        builder.Append(dialect.InOperator(elements.ToString()));
                    }
                };
        }

        public DefaultQuery(IDbConnection connection, IDbTransaction transaction, Session session, string tablePrefix)
        {
            _documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            _connection = connection;
            _transaction = transaction;
            _session = session;
            _dialect = SqlDialectFactory.For(connection);
            _sqlBuilder = _dialect.CreateBuilder(tablePrefix);
        }

        private void Bind<TIndex>() where TIndex : IIndex
        {
            if (_bound.Contains(typeof(TIndex)))
            {
                return;
            }

            var name = typeof(TIndex).Name;
            _bound.Add(typeof(TIndex));

            if (typeof(MapIndex).IsAssignableFrom(typeof(TIndex)))
            {
                // inner join [PersonByName] on [PersonByName].[Id] = [Document].[Id]
                _sqlBuilder.InnerJoin(name, name, "DocumentId", _documentTable, "Id");
            }
            else
            {
                var bridgeName = name + "_" + _documentTable;

                // inner join [ArticlesByDay_Document] on [Document].[Id] = [ArticlesByDay_Document].[DocumentId]
                _sqlBuilder.InnerJoin(bridgeName, _documentTable, "Id", bridgeName, "DocumentId");

                // inner join [ArticlesByDay] on [ArticlesByDay_Document].[ArticlesByDayId] = [ArticlesByDay].[Id]
                _sqlBuilder.InnerJoin(name, bridgeName, name + "Id", name, "Id");
            }
        }

        private void Page(int count, int skip)
        {
            _sqlBuilder.Skip(skip);
            _sqlBuilder.Take(count);
        }

        private void Filter<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : IIndex
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

            _builder.Clear();
            // if Filter is called, the Document type is implicit so there is no need to filter on TIndex
            ConvertPredicate(_builder, predicate.Body);
            _sqlBuilder.WhereAlso(_builder.ToString());
        }

        public void ConvertFragment(StringBuilder builder, Expression expression)
        {
            if (!IsParameterBased(expression))
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                expression = Expression.Constant(value);
            }

            switch (expression.NodeType)
            {
                case ExpressionType.LessThan:
                    ConvertComparisonBinaryExpression(builder, (BinaryExpression)expression, " < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    ConvertComparisonBinaryExpression(builder, (BinaryExpression)expression, " <= ");
                    break;
                case ExpressionType.GreaterThan:
                    ConvertComparisonBinaryExpression(builder, (BinaryExpression)expression, " > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    ConvertComparisonBinaryExpression(builder, (BinaryExpression)expression, " >= ");
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    ConvertEqualityBinaryExpression(builder, (BinaryExpression)expression, " and ");
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    ConvertEqualityBinaryExpression(builder, (BinaryExpression)expression, " or ");
                    break;
                case ExpressionType.Equal:
                    ConvertComparisonBinaryExpression(builder, (BinaryExpression)expression, " = ");
                    break;
                case ExpressionType.NotEqual:
                    ConvertComparisonBinaryExpression(builder, (BinaryExpression)expression, " <> ");
                    break;
                case ExpressionType.IsTrue:
                    ConvertFragment(builder, ((UnaryExpression)expression).Operand);
                    break;
                case ExpressionType.IsFalse:
                    builder.Append(" not ");
                    ConvertFragment(builder, ((UnaryExpression)expression).Operand);
                    break;
                case ExpressionType.Not:
                    ConvertFragment(builder, Expression.MakeBinary(ExpressionType.NotEqual, ((UnaryExpression)expression).Operand, Expression.Constant(true)));
                    break;
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;
                    builder.Append(_sqlBuilder.FormatColumn(_bound.Last().Name, memberExpression.Member.Name));
                    break;
                case ExpressionType.Constant:
                    _lastParameterName = "@p" + _sqlBuilder.Parameters.Count.ToString();
                    _sqlBuilder.Parameters.Add(_lastParameterName, ((ConstantExpression)expression).Value);
                    builder.Append(_lastParameterName);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)expression;
                    var methodInfo = methodCallExpression.Method;
                    Action<DefaultQuery, StringBuilder, ISqlDialect, MethodCallExpression> action;
                    if (MethodMappings.TryGetValue(methodInfo, out action))
                    {
                        action(this, builder, _dialect, methodCallExpression);
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
        /// Converts an expression that has to be a binary expression. Unary expressions
        /// are converted to a binary expression and evaluated.
        /// </summary>
        public void ConvertPredicate(StringBuilder builder, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Equal:
                case ExpressionType.Not:
                case ExpressionType.NotEqual:
                    ConvertFragment(builder, expression);
                    break;
                case ExpressionType.Call:
                    // A method call is something like x.Name.StartsWith("B") thus it is supposed
                    // to be part of the method mappings
                    ConvertFragment(builder, expression);
                    break;
                case ExpressionType.MemberAccess:
                    ConvertFragment(builder, Expression.Equal(expression, Expression.Constant(true, typeof(bool))));
                    break;
                case ExpressionType.Constant:
                    ConvertFragment(builder, Expression.Equal(expression, Expression.Constant(true, typeof(bool))));
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
                    if (!typeof(IIndex).IsAssignableFrom(expression.Type))
                    {
                        return false;
                    }

                    return true;
                case ExpressionType.Not:
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
                    return false;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)expression;

                    if (methodCallExpression.Object == null)
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

        private void ConvertComparisonBinaryExpression(StringBuilder builder, BinaryExpression expression, string operation)
        {
            builder.Append("(");
            ConvertFragment(builder, expression.Left);
            builder.Append(operation);
            ConvertFragment(builder, expression.Right);
            builder.Append(")");
        }

        private void ConvertEqualityBinaryExpression(StringBuilder builder, BinaryExpression expression, string operation)
        {
            builder.Append("(");
            ConvertPredicate(builder, expression.Left);
            builder.Append(operation);
            ConvertPredicate(builder, expression.Right);
            builder.Append(")");
        }

        private Expression RemoveUnboxing(Expression e)
        {
            // If an expression is a conversion, extract its body.
            // This is used when an OrderBy expression uses a ValueType but
            // it's automatically using unboxing conversion (to object)

            if (e.NodeType == ExpressionType.Convert)
            {
                return ((UnaryExpression)e).Operand;
            }

            return e;
        }

        private void OrderBy<T>(Expression<Func<T, object>> keySelector)
        {
            _builder.Clear();
            ConvertFragment(_builder, RemoveUnboxing(keySelector.Body));
            _sqlBuilder.OrderBy(_builder.ToString());
        }

        private void ThenBy<T>(Expression<Func<T, object>> keySelector)
        {
            _builder.Clear();
            ConvertFragment(_builder, RemoveUnboxing(keySelector.Body));
            _sqlBuilder.ThenOrderBy(_builder.ToString());
        }

        private void OrderByDescending<T>(Expression<Func<T, object>> keySelector)
        {
            _builder.Clear();
            ConvertFragment(_builder, RemoveUnboxing(keySelector.Body));
            _sqlBuilder.OrderByDescending(_builder.ToString());
        }

        private void ThenByDescending<T>(Expression<Func<T, object>> keySelector)
        {
            _builder.Clear();
            ConvertFragment(_builder, RemoveUnboxing(keySelector.Body));
            _sqlBuilder.ThenOrderByDescending(_builder.ToString());
        }

        public async Task<int> CountAsync()
        {
            // Commit any pending changes before doing a query (auto-flush)
            await _session.CommitAsync();

            _sqlBuilder.Selector("count(*)");
            var sql = _sqlBuilder.ToSqlString(_dialect, true);
            return await _connection.ExecuteScalarAsync<int>(sql, _sqlBuilder.Parameters, _transaction);
        }

        IQuery<T> IQuery.For<T>(bool filterType)
        {
            _bound.Clear();
            _bound.Add(typeof(Document));

            _sqlBuilder.Select();
            _sqlBuilder.Table(_documentTable);

            if (filterType)
            {
                _sqlBuilder.WhereAlso(_sqlBuilder.FormatColumn(_documentTable, "Type") + " = @Type"); // TODO: investigate, this makes the query 3 times slower on sqlite
                _sqlBuilder.Parameters["@Type"] = typeof(T).SimplifiedTypeName();
            }

            return new Query<T>(this);
        }

        IQueryIndex<TIndex> IQuery.ForIndex<TIndex>()
        {
            _bound.Clear();
            _bound.Add(typeof(TIndex));
            _sqlBuilder.Select();
            _sqlBuilder.Table(typeof(TIndex).Name);

            return new QueryIndex<TIndex>(this);
        }

        IQuery<object> IQuery.Any()
        {
            _bound.Clear();
            _bound.Add(typeof(Document));

            _sqlBuilder.Select();
            _sqlBuilder.Table(_documentTable);
            _sqlBuilder.Selector("*");
            return new Query<object>(this);
        }

        class Query<T> : IQuery<T> where T : class
        {
            protected readonly DefaultQuery _query;

            public Query(DefaultQuery query)
            {
                _query = query;
            }

            public Task<T> FirstOrDefaultAsync()
            {
                return FirstOrDefaultImpl();
            }

            protected async Task<T> FirstOrDefaultImpl()
            {
                // Commit any pending changes before doing a query (auto-flush)
                await _query._session.CommitAsync();

                _query.Page(1, 0);

                if (typeof(IIndex).IsAssignableFrom(typeof(T)))
                {
                    _query._sqlBuilder.Selector("*");
                    var sql = _query._sqlBuilder.ToSqlString(_query._dialect);
                    return (await _query._connection.QueryAsync<T>(sql, _query._sqlBuilder.Parameters, _query._transaction)).FirstOrDefault();
                }
                else
                {
                    _query._sqlBuilder.Selector(_query._documentTable, "*");
                    var sql = _query._sqlBuilder.ToSqlString(_query._dialect);
                    var documents = (await _query._connection.QueryAsync<Document>(sql, _query._sqlBuilder.Parameters, _query._transaction)).ToArray();

                    if (documents.Length == 0)
                    {
                        return default(T);
                    }

                    if (typeof(T) == typeof(object))
                    {
                        return _query._session.Store.Configuration.ContentSerializer.DeserializeDynamic(documents[0].Content);
                    }
                    else
                    {
                        return (T)_query._session.Store.Configuration.ContentSerializer.Deserialize(documents[0].Content, typeof(T));
                    }
                }
            }

            Task<IEnumerable<T>> IQuery<T>.ListAsync()
            {
                return ListImpl();
            }

            public async Task<IEnumerable<T>> ListImpl()
            {
                // Commit any pending changes before doing a query (auto-flush)
                await _query._session.CommitAsync();

                if (typeof(IIndex).IsAssignableFrom(typeof(T)))
                {
                    _query._sqlBuilder.Selector("*");
                    var sql = _query._sqlBuilder.ToSqlString(_query._dialect);
                    return await _query._connection.QueryAsync<T>(sql, _query._sqlBuilder.Parameters, _query._transaction);
                }
                else
                {
                    _query._sqlBuilder.Selector(_query._sqlBuilder.FormatColumn(_query._documentTable, "*"));
                    var sql = _query._sqlBuilder.ToSqlString(_query._dialect);
                    var documents = await _query._connection.QueryAsync<Document>(sql, _query._sqlBuilder.Parameters, _query._transaction);
                    return _query._session.Get<T>(documents.ToArray());
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

            async Task<int> IQuery<T>.CountAsync()
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
        }

        class QueryIndex<T> : Query<T>, IQueryIndex<T> where T : class, IIndex
        {
            public QueryIndex(DefaultQuery query) : base(query)
            { }

            Task<T> IQueryIndex<T>.FirstOrDefaultAsync()
            {
                return FirstOrDefaultImpl();
            }

            Task<IEnumerable<T>> IQueryIndex<T>.ListAsync()
            {
                return ListImpl();
            }

            IQueryIndex<T> IQueryIndex<T>.Skip(int count)
            {
                _query._sqlBuilder.Skip(count);
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.Take(int count)
            {
                _query._sqlBuilder.Take(count);
                return this;
            }

            async Task<int> IQueryIndex<T>.CountAsync()
            {
                return await _query.CountAsync();
            }

            IQueryIndex<TIndex> IQueryIndex<T>.With<TIndex>()
            {
                _query.Bind<TIndex>();
                return new QueryIndex<TIndex>(_query);
            }

            IQueryIndex<TIndex> IQueryIndex<T>.With<TIndex>(Expression<Func<TIndex, bool>> predicate)
            {
                _query.Bind<TIndex>();
                _query.Filter(predicate);
                return new QueryIndex<TIndex>(_query);
            }

            IQueryIndex<T> IQueryIndex<T>.Where(string sql)
            {
                _query._sqlBuilder.WhereAlso(sql);
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.Where(Func<ISqlDialect, string> sql)
            {
                _query._sqlBuilder.WhereAlso(sql?.Invoke(_query._dialect));
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.Where(Expression<Func<T, bool>> predicate)
            {
                _query.Filter<T>(predicate);
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.OrderBy(Expression<Func<T, object>> keySelector)
            {
                _query.OrderBy(keySelector);
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.OrderByDescending(Expression<Func<T, object>> keySelector)
            {
                _query.OrderByDescending(keySelector);
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.ThenBy(Expression<Func<T, object>> keySelector)
            {
                _query.ThenBy(keySelector);
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.ThenByDescending(Expression<Func<T, object>> keySelector)
            {
                _query.ThenByDescending(keySelector);
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.WithParameter(string name, object value)
            {
                _query._sqlBuilder.Parameters[name] = value;
                return this;
            }
        }

        class Query<T, TIndex> : Query<T>, IQuery<T, TIndex>
            where T : class
            where TIndex : IIndex
        {
            public Query(DefaultQuery query)
                : base(query)
            {
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(string sql)
            {
                _query._sqlBuilder.WhereAlso(sql);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(Func<ISqlDialect, string> sql)
            {
                _query._sqlBuilder.WhereAlso(sql?.Invoke(_query._dialect));
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.WithParameter(string name, object value)
            {
                _query._sqlBuilder.Parameters[name] = value;
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(Expression<Func<TIndex, bool>> predicate)
            {
                _query.Filter<TIndex>(predicate);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderBy(Expression<Func<TIndex, object>> keySelector)
            {
                _query.OrderBy(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.ThenBy(Expression<Func<TIndex, object>> keySelector)
            {
                _query.ThenBy(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderByDescending(Expression<Func<TIndex, object>> keySelector)
            {
                _query.OrderByDescending(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.ThenByDescending(Expression<Func<TIndex, object>> keySelector)
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
