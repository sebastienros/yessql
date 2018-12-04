using Dapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Data;
using YesSql.Indexes;
using YesSql.Serialization;

namespace YesSql.Services
{
    public class QueryState
    {
        public QueryState(ISqlBuilder sqlBuilder)
        {
            _documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            _sqlBuilder = sqlBuilder;
        }

        public List<Type> _bound = new List<Type>();
        public readonly string _documentTable;
        public string _lastParameterName;
        public ISqlBuilder _sqlBuilder;
        public List<Action<object, ISqlBuilder>> _parameterBindings;
        public StringBuilder _builder = new StringBuilder();

        public QueryState Clone()
        {
            var clone = new QueryState(_sqlBuilder.Clone());

            clone._bound = new List<Type>(_bound);
            clone._lastParameterName = _lastParameterName;
            clone._parameterBindings = _parameterBindings == null ? null : new List<Action<object, ISqlBuilder>>(_parameterBindings);
            clone._builder = new StringBuilder(_builder.ToString());

            // Clear previous paging if any
            clone._sqlBuilder.ClearTrail();
            clone._sqlBuilder.Skip(null);
            clone._sqlBuilder.Take(null);

            return clone;
        }
    }

    public class DefaultQuery : IQuery
    {
        internal QueryState _queryState;
        private readonly Session _session;
        private readonly ISqlDialect _dialect;
        private object _compiledQuery = null;

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
                var parameter = query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName];
                query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName] = parameter.ToString() + "%";
                builder.Append(")");
            };

            MethodMappings[typeof(String).GetMethod("EndsWith", new Type[] { typeof(string) })] = (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Object);
                builder.Append(" like ");
                query.ConvertFragment(builder, expression.Arguments[0]);
                var parameter = query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName];
                query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName] = "%" + parameter.ToString();
                builder.Append(")");

            };

            MethodMappings[typeof(String).GetMethod("Contains", new Type[] { typeof(string) })] = (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Object);
                builder.Append(" like ");
                query.ConvertFragment(builder, expression.Arguments[0]);
                var parameter = query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName];
                query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName] = "%" + parameter.ToString() + "%";
                builder.Append(")");
            };

            MethodMappings[typeof(String).GetMethod(nameof(String.Concat), new Type[] { typeof(string[]) })] =
            MethodMappings[typeof(String).GetMethod(nameof(String.Concat), new Type[] { typeof(string), typeof(string) })] =
            MethodMappings[typeof(String).GetMethod(nameof(String.Concat), new Type[] { typeof(string), typeof(string), typeof(string) })] =
            MethodMappings[typeof(String).GetMethod(nameof(String.Concat), new Type[] { typeof(string), typeof(string), typeof(string), typeof(string) })] =
                (query, builder, dialect, expression) =>
            {
                var generators = new List<Action<StringBuilder>>();

                foreach (var argument in expression.Arguments)
                {
                    generators.Add(sb => query.ConvertFragment(sb, argument));
                }

                dialect.Concat(builder, generators.ToArray());
            };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsIn")] =
                (query, builder, dialect, expression) =>
                {
                    // Could be simplified if int[] could be casted to IEnumerable<object>
                    var objects = Expression.Lambda(expression.Arguments[1]).Compile().DynamicInvoke() as IEnumerable;
                    var values = new List<object>();

                    foreach(var o in objects)
                    {
                        values.Add(o);
                    }

                    if (values.Count == 0)
                    {
                        builder.Append(" 1 = 0");
                    }
                    else if (values.Count == 1)
                    {
                        query.ConvertFragment(builder, expression.Arguments[0]);
                        builder.Append(" = " );
                        query.ConvertFragment(builder, Expression.Constant(values[0]));
                    }
                    else
                    {
                        query.ConvertFragment(builder, expression.Arguments[0]);
                        var elements = new StringBuilder();
                        for (var i = 0; i < values.Count; i++)
                        {
                            query.ConvertFragment(elements, Expression.Constant(values[i]));
                            if (i < values.Count - 1)
                            {
                                elements.Append(", ");
                            }
                        }

                        builder.Append(dialect.InOperator(elements.ToString()));
                    }
                };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsNotIn")] =
                (query, builder, dialect, expression) =>
                {
                    var values = (Expression.Lambda(expression.Arguments[1]).Compile().DynamicInvoke() as IEnumerable<object>).ToArray();

                    if (values.Length == 0)
                    {
                        builder.Append(" 1 = 1");
                    }
                    else if (values.Length == 1)
                    {
                        query.ConvertFragment(builder, expression.Arguments[0]);
                        builder.Append(" <> ");
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

                        builder.Append(dialect.NotInOperator(elements.ToString()));
                    }
                };

            MethodMappings[typeof(DefaultQueryExtensionsIndex).GetMethod("IsIn")] =
                (query, builder, dialect, expression) =>
                {
                    var selector = expression.Arguments[1];
                    var predicate = expression.Arguments[2];

                    var tIndex = ((LambdaExpression)((UnaryExpression)selector).Operand).Parameters[0].Type;
                    query._queryState._bound.Add(tIndex);

                    var sqlBuilder = query._dialect.CreateBuilder(query._session._store.Configuration.TablePrefix);

                    // Build inner query
                    var _builder = new StringBuilder();

                    sqlBuilder.Select();

                    query.ConvertFragment(_builder, ((LambdaExpression)((UnaryExpression)selector).Operand).Body);
                    sqlBuilder.Selector(_builder.ToString());
                    _builder.Clear();

                    sqlBuilder.Table(((LambdaExpression)((UnaryExpression)selector).Operand).Parameters[0].Type.Name);
                    query.ConvertPredicate(_builder, ((LambdaExpression)((UnaryExpression)predicate).Operand).Body);
                    sqlBuilder.WhereAlso(_builder.ToString());

                    query._queryState._bound.RemoveAt(query._queryState._bound.Count - 1);

                    // Insert query
                    query.ConvertFragment(builder, expression.Arguments[0]);
                    builder.Append(dialect.InSelectOperator(sqlBuilder.ToSqlString()));
                };

            MethodMappings[typeof(DefaultQueryExtensionsIndex).GetMethod("IsNotIn")] =
                (query, builder, dialect, expression) =>
                {
                    var selector = expression.Arguments[1];
                    var predicate = expression.Arguments[2];

                    var tIndex = ((LambdaExpression)((UnaryExpression)selector).Operand).Parameters[0].Type;
                    query._queryState._bound.Add(tIndex);

                    var sqlBuilder = query._dialect.CreateBuilder(query._session._store.Configuration.TablePrefix);

                    // Build inner query
                    var _builder = new StringBuilder();

                    sqlBuilder.Select();

                    query.ConvertFragment(_builder, ((LambdaExpression)((UnaryExpression)selector).Operand).Body);
                    sqlBuilder.Selector(_builder.ToString());
                    _builder.Clear();

                    sqlBuilder.Table(query._queryState._bound.Last().Name);
                    query.ConvertPredicate(_builder, ((LambdaExpression)((UnaryExpression)predicate).Operand).Body);
                    sqlBuilder.WhereAlso(_builder.ToString());

                    query._queryState._bound.RemoveAt(query._queryState._bound.Count - 1);

                    // Insert query
                    query.ConvertFragment(builder, expression.Arguments[0]);
                    builder.Append(dialect.NotInSelectOperator(sqlBuilder.ToSqlString()));
                };
        }

        public DefaultQuery(IDbConnection connection, IDbTransaction transaction, Session session, string tablePrefix)
        {
            _session = session;
            _dialect = session.Store.Dialect;
            _queryState = new QueryState(_dialect.CreateBuilder(tablePrefix));
        }

        public DefaultQuery(IDbConnection connection, IDbTransaction transaction, Session session, string tablePrefix, QueryState queryState, object compiledQuery)
        {
            _queryState = queryState;
            _compiledQuery = compiledQuery;
            _session = session;
            _dialect = session.Store.Dialect;
        }

        public override string ToString()
        {
            return _queryState._sqlBuilder.ToSqlString();
        }

        private void Bind<TIndex>() where TIndex : IIndex
        {
            if (_queryState._bound.Contains(typeof(TIndex)))
            {
                return;
            }

            var name = typeof(TIndex).Name;
            _queryState._bound.Add(typeof(TIndex));

            if (typeof(MapIndex).IsAssignableFrom(typeof(TIndex)))
            {
                // inner join [PersonByName] on [PersonByName].[Id] = [Document].[Id]
                _queryState._sqlBuilder.InnerJoin(name, name, "DocumentId", _queryState._documentTable, "Id");
            }
            else
            {
                var bridgeName = name + "_" + _queryState._documentTable;

                // inner join [ArticlesByDay_Document] on [Document].[Id] = [ArticlesByDay_Document].[DocumentId]
                _queryState._sqlBuilder.InnerJoin(bridgeName, _queryState._documentTable, "Id", bridgeName, "DocumentId");

                // inner join [ArticlesByDay] on [ArticlesByDay_Document].[ArticlesByDayId] = [ArticlesByDay].[Id]
                _queryState._sqlBuilder.InnerJoin(name, bridgeName, name + "Id", name, "Id");
            }
        }

        private void Page(int count, int skip)
        {
            if (skip > 0)
            {
                _queryState._sqlBuilder.Skip(skip.ToString());
            }

            if (count > 0)
            {
                _queryState._sqlBuilder.Take(count.ToString());
            }
        }

        private void Filter<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : IIndex
        {
            // For<T> hasn't been called already
            if (String.IsNullOrEmpty(_queryState._sqlBuilder.Clause))
            {
                _queryState._bound.Clear();
                _queryState._bound.Add(typeof(TIndex));

                _queryState._sqlBuilder.Select();
                _queryState._sqlBuilder.Table(typeof(TIndex).Name);
                _queryState._sqlBuilder.Selector(typeof(TIndex).Name, "DocumentId");
            }

            _queryState._builder.Clear();
            // if Filter is called, the Document type is implicit so there is no need to filter on TIndex
            ConvertPredicate(_queryState._builder, predicate.Body);
            _queryState._sqlBuilder.WhereAlso(_queryState._builder.ToString());
        }

        /// <summary>
        /// Converts an expression that is not based on a lambda parameter to its atomic constant value.
        /// </summary>
        private ConstantExpression Evaluate(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return (ConstantExpression)expression;

                case ExpressionType.Convert:
                    return Evaluate(((UnaryExpression)expression).Operand);

                case ExpressionType.New:
                    var newExpression = (NewExpression)expression;
                    var arguments = newExpression.Arguments.Select(a => Evaluate(a).Value).ToArray();
                    var value = newExpression.Constructor.Invoke(arguments);
                    return Expression.Constant(value);

                case ExpressionType.Call:
                    var methodExpression = (MethodCallExpression)expression;
                    arguments = methodExpression.Arguments.Select(a => Evaluate(a).Value).ToArray();
                    object obj = null;

                    // Static method?
                    if (methodExpression.Object != null)
                    {
                        obj = Evaluate(methodExpression.Object).Value;
                    }

                    return Expression.Constant(methodExpression.Method.Invoke(obj, arguments));

                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;

                    if (memberExpression.Member.MemberType == MemberTypes.Field)
                    {
                        if (memberExpression.Expression != null)
                        {
                            obj = Evaluate(memberExpression.Expression).Value;

                            if (obj == null)
                            {
                                return Expression.Constant(null);
                            }
                        }
                        else
                        {
                            // Static members
                            obj = null;
                        }

                        _queryState._parameterBindings = _queryState._parameterBindings ?? new List<Action<object, ISqlBuilder>>();

                        // Create a delegate that will be invoked every time a compiled query is reused,
                        // which will re-evaluate the current node, for the current parameter.
                        var _parameterName = "@p" + _queryState._sqlBuilder.Parameters.Count.ToString();
                        _queryState._parameterBindings.Add((o, sqlBuilder) => sqlBuilder.Parameters[_parameterName] = ((PropertyInfo)memberExpression.Member).GetValue(o));

                        value = ((FieldInfo)memberExpression.Member).GetValue(obj);
                        return Expression.Constant(value);
                    }
                    else if (memberExpression.Member.MemberType == MemberTypes.Property)
                    {
                        if (memberExpression.Expression != null)
                        {
                            obj = Evaluate(memberExpression.Expression).Value;

                            if (obj == null)
                            {
                                return Expression.Constant(null);
                            }
                        }
                        else
                        {
                            // Static members
                            obj = null;
                        }

                        _queryState._parameterBindings = _queryState._parameterBindings ?? new List<Action<object, ISqlBuilder>>();

                        // Create a delegate that will be invoked every time a compiled query is reused,
                        // which will re-evaluate the current node, for the current parameter.
                        var _parameterName = "@p" + _queryState._sqlBuilder.Parameters.Count.ToString();
                        _queryState._parameterBindings.Add((o, sqlBuilder) => sqlBuilder.Parameters[_parameterName] = ((PropertyInfo)memberExpression.Member).GetValue(o));

                        value = ((PropertyInfo)memberExpression.Member).GetValue(obj);
                        return Expression.Constant(value);
                    }
                    break;
            }

            // TODO: Detect the code paths that can reach this point, but testing various expression or
            // logging, then enhance this method to take the case into account. This is critical
            // to not have to compile as the performance difference is 100x.
            return Expression.Constant(Expression.Lambda(expression).Compile().DynamicInvoke());
        }

        private string GetBinaryOperator(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.LessThan:
                    return " < ";
                case ExpressionType.LessThanOrEqual:
                    return " <= ";
                case ExpressionType.GreaterThan:
                    return " > ";
                case ExpressionType.GreaterThanOrEqual:
                    return " >= ";
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return " and ";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return " or ";
                case ExpressionType.Equal:
                    return " = ";
                case ExpressionType.NotEqual:
                    return " <> ";
                case ExpressionType.Add:
                    return " + ";
                case ExpressionType.Subtract:
                    return " - ";
                case ExpressionType.Multiply:
                    return " * ";
                case ExpressionType.Divide:
                    return " / ";
            }

            throw new ArgumentException(nameof(expression));
        }

        public void ConvertFragment(StringBuilder builder, Expression expression)
        {
            if (!IsParameterBased(expression))
            {
                switch (expression.NodeType)
                {
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
                    case ExpressionType.Add:
                    case ExpressionType.Multiply:
                    case ExpressionType.Divide:
                    case ExpressionType.Subtract:
                        // Don't reduce to a single value, just render both ends

                        var binaryExpression = (BinaryExpression)expression;
                        if (binaryExpression.Left is ConstantExpression left && binaryExpression.Right is ConstantExpression right)
                        {
                            _queryState._builder.Append(_dialect.GetSqlValue(left.Value));
                            _queryState._builder.Append(GetBinaryOperator(expression));
                            _queryState._builder.Append(_dialect.GetSqlValue(right.Value));
                            return;
                        }

                        break;
                }

                expression = Evaluate(expression);
            }

            switch (expression.NodeType)
            {
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                    ConvertComparisonBinaryExpression(builder, (BinaryExpression)expression, GetBinaryOperator(expression));
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    ConvertEqualityBinaryExpression(builder, (BinaryExpression)expression, GetBinaryOperator(expression));
                    break;
                case ExpressionType.Add:
                    var binaryExpression = (BinaryExpression)expression;

                    // Is it supposed to be a concatenation?
                    if (binaryExpression.Left.Type == typeof(string) || binaryExpression.Right.Type == typeof(string))
                    {
                        ConvertConcatenateBinaryExpression(builder, binaryExpression);
                    }
                    else
                    {
                        ConvertComparisonBinaryExpression(builder, binaryExpression, " + ");
                    }
                    break;
                case ExpressionType.Convert:
                    ConvertFragment(builder, ((UnaryExpression)expression).Operand);
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
                    builder.Append(_queryState._sqlBuilder.FormatColumn(_queryState._bound.Last().Name, memberExpression.Member.Name));
                    break;
                case ExpressionType.Constant:
                    _queryState._lastParameterName = "@p" + _queryState._sqlBuilder.Parameters.Count.ToString();
                    _queryState._sqlBuilder.Parameters.Add(_queryState._lastParameterName, ((ConstantExpression)expression).Value);
                    builder.Append(_queryState._lastParameterName);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)expression;
                    var methodInfo = methodCallExpression.Method;
                    Action<DefaultQuery, StringBuilder, ISqlDialect, MethodCallExpression> action;
                    if (MethodMappings.TryGetValue(methodInfo, out action) 
                        || MethodMappings.TryGetValue(methodInfo.GetGenericMethodDefinition(), out action))
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
                case ExpressionType.Convert:
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
                case ExpressionType.Add:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Subtract:
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
            if (operation == " = " || operation == " <> ")
            {
                // Checking for NULL comparison
                var leftIsNull = expression.Left.NodeType == ExpressionType.Constant && (expression.Left as ConstantExpression).Value == null;
                var rightIsNull = expression.Right.NodeType == ExpressionType.Constant && (expression.Right as ConstantExpression).Value == null;

                if (leftIsNull && rightIsNull)
                {
                    builder.Append("(");
                    builder.Append(_dialect.GetSqlValue(true));
                    builder.Append(operation);
                    builder.Append(_dialect.GetSqlValue(true));
                    builder.Append(")");
                    return;
                }
                else if (leftIsNull)
                {
                    builder.Append("(");
                    ConvertFragment(builder, expression.Right);
                    builder.Append(operation == " = " ? " IS NULL" : " IS NOT NULL");
                    builder.Append(")");
                    return;
                }
                else if (rightIsNull)
                {
                    builder.Append("(");
                    ConvertFragment(builder, expression.Left);
                    builder.Append(operation == " = " ? " IS NULL" : " IS NOT NULL");
                    builder.Append(")");
                    return;
                }

            }

            builder.Append("(");
            ConvertFragment(builder, expression.Left);
            builder.Append(operation);
            ConvertFragment(builder, expression.Right);
            builder.Append(")");
        }

        private void ConvertConcatenateBinaryExpression(StringBuilder builder, BinaryExpression expression)
        {
            _dialect.Concat(builder, b => ConvertFragment(b, expression.Left), b => ConvertFragment(b, expression.Right));
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
            _queryState._builder.Clear();
            ConvertFragment(_queryState._builder, RemoveUnboxing(keySelector.Body));
            _queryState._sqlBuilder.OrderBy(_queryState._builder.ToString());
        }

        private void ThenBy<T>(Expression<Func<T, object>> keySelector)
        {
            _queryState._builder.Clear();
            ConvertFragment(_queryState._builder, RemoveUnboxing(keySelector.Body));
            _queryState._sqlBuilder.ThenOrderBy(_queryState._builder.ToString());
        }

        private void OrderByDescending<T>(Expression<Func<T, object>> keySelector)
        {
            _queryState._builder.Clear();
            ConvertFragment(_queryState._builder, RemoveUnboxing(keySelector.Body));
            _queryState._sqlBuilder.OrderByDescending(_queryState._builder.ToString());
        }

        private void ThenByDescending<T>(Expression<Func<T, object>> keySelector)
        {
            _queryState._builder.Clear();
            ConvertFragment(_queryState._builder, RemoveUnboxing(keySelector.Body));
            _queryState._sqlBuilder.ThenOrderByDescending(_queryState._builder.ToString());
        }

        public async Task<int> CountAsync()
        {
            // Commit any pending changes before doing a query (auto-flush)
            await _session.CommitAsync();

            var transaction = await _session.DemandAsync();

            var localBuilder = _queryState._sqlBuilder.Clone();
            localBuilder.Selector("count(*)");

            // Clear paging and order when counting 
            localBuilder.ClearOrder();
            localBuilder.Skip(null);
            localBuilder.Take(null);

            if (_compiledQuery != null && _queryState._parameterBindings != null)
            {
                foreach (var binding in _queryState._parameterBindings)
                {
                    binding(_compiledQuery, localBuilder);
                }
            }

            var sql = localBuilder.ToSqlString();

            var key = new WorkerQueryKey(sql, localBuilder.Parameters);
            return await _session._store.ProduceAsync(key, () =>
            {
                return transaction.Connection.ExecuteScalarAsync<int>(sql, localBuilder.Parameters, transaction);
            });
        }

        IQuery<T> IQuery.For<T>(bool filterType)
        {
            _queryState._bound.Clear();
            _queryState._bound.Add(typeof(Document));

            _queryState._sqlBuilder.Select();
            _queryState._sqlBuilder.Table(_queryState._documentTable);

            if (filterType)
            {
                _queryState._sqlBuilder.WhereAlso(_queryState._sqlBuilder.FormatColumn(_queryState._documentTable, "Type") + " = @Type"); // TODO: investigate, this makes the query 3 times slower on sqlite
                _queryState._sqlBuilder.Parameters["@Type"] = typeof(T).SimplifiedTypeName();
            }

            return new Query<T>(this);
        }

        IQueryIndex<TIndex> IQuery.ForIndex<TIndex>()
        {
            _queryState._bound.Clear();
            _queryState._bound.Add(typeof(TIndex));
            _queryState._sqlBuilder.Select();
            _queryState._sqlBuilder.Table(typeof(TIndex).Name);

            return new QueryIndex<TIndex>(this);
        }

        IQuery<object> IQuery.Any()
        {
            _queryState._bound.Clear();
            _queryState._bound.Add(typeof(Document));

            _queryState._sqlBuilder.Select();
            _queryState._sqlBuilder.Table(_queryState._documentTable);
            _queryState._sqlBuilder.Selector("*");
            return new Query<object>(this);
        }

        public class Query<T> : IQuery<T> where T : class
        {
            internal readonly DefaultQuery _query;

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

                var transaction = await _query._session.DemandAsync();

                if (_query._compiledQuery != null && _query._queryState._parameterBindings != null)
                {
                    foreach (var binding in _query._queryState._parameterBindings)
                    {
                        binding(_query._compiledQuery, _query._queryState._sqlBuilder);
                    }
                }

                _query.Page(1, 0);

                if (typeof(IIndex).IsAssignableFrom(typeof(T)))
                {
                    _query._queryState._sqlBuilder.Selector("*");
                    var sql = _query._queryState._sqlBuilder.ToSqlString();
                    var key = new WorkerQueryKey(sql, _query._queryState._sqlBuilder.Parameters);
                    return (await _query._session._store.ProduceAsync(key, () =>
                    {
                        return transaction.Connection.QueryAsync<T>(sql, _query._queryState._sqlBuilder.Parameters, transaction);
                    })).FirstOrDefault();
                }
                else
                {
                    _query._queryState._sqlBuilder.Selector(_query._queryState._documentTable, "*");
                    var sql = _query._queryState._sqlBuilder.ToSqlString();
                    var key = new WorkerQueryKey(sql, _query._queryState._sqlBuilder.Parameters);
                    var documents = (await _query._session._store.ProduceAsync(key, () =>
                    {
                        return transaction.Connection.QueryAsync<Document>(sql, _query._queryState._sqlBuilder.Parameters, transaction);
                    })).ToArray();

                    if (documents.Length == 0)
                    {
                        return default(T);
                    }

                    return _query._session.Get<T>(documents).FirstOrDefault();
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

                var transaction = await _query._session.DemandAsync();

                if (_query._compiledQuery != null && _query._queryState._parameterBindings != null)
                {
                    foreach (var binding in _query._queryState._parameterBindings)
                    {
                        binding(_query._compiledQuery, _query._queryState._sqlBuilder);
                    }
                }

                if (typeof(IIndex).IsAssignableFrom(typeof(T)))
                {
                    _query._queryState._sqlBuilder.Selector("*");

                    // If a page is requested without order add a default one
                    if (!_query._queryState._sqlBuilder.HasOrder && _query._queryState._sqlBuilder.HasPaging)
                    {
                        _query._queryState._sqlBuilder.OrderBy(_query._queryState._sqlBuilder.FormatColumn(typeof(T).Name, "Id"));
                    }

                    var sql = _query._queryState._sqlBuilder.ToSqlString();
                    var key = new WorkerQueryKey(sql, _query._queryState._sqlBuilder.Parameters);
                    return await _query._session._store.ProduceAsync(key, () =>
                    {
                        return transaction.Connection.QueryAsync<T>(sql, _query._queryState._sqlBuilder.Parameters, transaction);
                    });
                }
                else
                {
                    // If a page is requested without order add a default one
                    if (!_query._queryState._sqlBuilder.HasOrder && _query._queryState._sqlBuilder.HasPaging)
                    {
                        _query._queryState._sqlBuilder.OrderBy(_query._queryState._sqlBuilder.FormatColumn(_query._queryState._documentTable, "Id"));
                    }

                    _query._queryState._sqlBuilder.Selector(_query._queryState._sqlBuilder.FormatColumn(_query._queryState._documentTable, "*"));
                    var sql = _query._queryState._sqlBuilder.ToSqlString();
                    var key = new WorkerQueryKey(sql, _query._queryState._sqlBuilder.Parameters);
                    var documents = await _query._session._store.ProduceAsync(key, () =>
                    {
                        return transaction.Connection.QueryAsync<Document>(sql, _query._queryState._sqlBuilder.Parameters, transaction);
                    });

                    return _query._session.Get<T>(documents.ToArray());
                }
            }

            IQuery<T> IQuery<T>.Skip(int count)
            {
                if (!_query._queryState._sqlBuilder.HasOrder)
                {
                    _query._queryState._sqlBuilder.OrderBy(_query._dialect.QuoteForColumnName("Id"));
                }

                _query._queryState._sqlBuilder.Skip(count.ToString());
                return this;
            }

            IQuery<T> IQuery<T>.Take(int count)
            {
                if (!_query._queryState._sqlBuilder.HasOrder)
                {
                    _query._queryState._sqlBuilder.OrderBy(_query._dialect.QuoteForColumnName("Id"));
                }

                _query._queryState._sqlBuilder.Take(count.ToString());
                return this;
            }

            Task<int> IQuery<T>.CountAsync()
            {
                return _query.CountAsync();
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
                if (count > 0)
                {
                    _query._queryState._sqlBuilder.Skip(count.ToString());
                }
                else
                {
                    _query._queryState._sqlBuilder.Skip(null);
                }

                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.Take(int count)
            {
                if (count > 0)
                {
                    _query._queryState._sqlBuilder.Take(count.ToString());
                }
                else
                {
                    _query._queryState._sqlBuilder.Take(null);
                }

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
                _query._queryState._sqlBuilder.WhereAlso(sql);
                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.Where(Func<ISqlDialect, string> sql)
            {
                _query._queryState._sqlBuilder.WhereAlso(sql?.Invoke(_query._dialect));
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
                _query._queryState._sqlBuilder.Parameters[name] = value;
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
                _query._queryState._sqlBuilder.WhereAlso(sql);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(Func<ISqlDialect, string> sql)
            {
                _query._queryState._sqlBuilder.WhereAlso(sql?.Invoke(_query._dialect));
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.WithParameter(string name, object value)
            {
                _query._queryState._sqlBuilder.Parameters[name] = value;
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
        public static bool IsIn(this object source, IEnumerable values)
        {
            return false;
        }

        public static bool IsNotIn(this object source, IEnumerable values)
        {
            return false;
        }
    }

    public static class DefaultQueryExtensionsIndex
    {
        public static bool IsIn<TIndex>(this object source, Expression<Func<TIndex, object>> select, Expression<Func<TIndex, bool>> where)
        {
            return false;
        }

        public static bool IsNotIn<TIndex>(this object source, Expression<Func<TIndex, object>> select, Expression<Func<TIndex, bool>> where)
        {
            return false;
        }
    }
}
