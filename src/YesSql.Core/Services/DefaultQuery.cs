using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using YesSql.Data;
using YesSql.Indexes;
using YesSql.Utils;

namespace YesSql.Services
{
    public class QueryState
    {
        public QueryState(ISqlBuilder sqlBuilder, IStore store, string collection)
        {
            _collection = collection;
            _documentTable = store.Configuration.TableNameConvention.GetDocumentTable(collection);
            _sqlBuilder = sqlBuilder;
            _store = store;
            _bindings[_bindingName] = new List<Type>();

            _currentPredicate = new AndNode();
            _predicate = _currentPredicate;
        }

        public string _bindingName = "a1";
        public Dictionary<string, List<Type>> _bindings = new Dictionary<string, List<Type>>();
        public readonly string _documentTable;
        public string _lastParameterName;
        public ISqlBuilder _sqlBuilder;
        public List<Action<object, ISqlBuilder>> _parameterBindings;
        public string _collection;
        public IStore _store;
        internal CompositeNode _predicate; // the defaut root predicate is an AND expression
        internal CompositeNode _currentPredicate; // the current predicate when Any() or All() is called
        public bool _processed = false;

        public void FlushFilters()
        {
            if (_predicate != null)
            {
                var builder = new RentedStringBuilder(Store.SmallBufferSize);
                _predicate.Build(builder);

                if (builder.Length > 0)
                {
                    _sqlBuilder.WhereAnd(builder.ToString());
                }

                builder.Dispose();

                _predicate = null;
            }
        }

        public string GetTableAlias(string tableName)
        {
            return tableName + "_" + _bindingName;
        }

        public string GetBridgeAlias(Type t)
        {
            return GetTableAlias(t.Name + "_Document");
        }

        public string GetTypeAlias(Type t)
        {
            return GetTableAlias(t.Name);
        }

        public void AddBinding(Type t)
        {
            if (!_bindings.TryGetValue(_bindingName, out var bindings))
            {
                _bindings.Add(_bindingName, bindings = new List<Type>());
            }

            bindings.Add(t);
        }

        public void RemoveBinding()
        {
            if (!_bindings.TryGetValue(_bindingName, out var bindings))
            {
                return;
            }

            bindings.RemoveAt(bindings.Count - 1);
        }

        public List<Type> GetBindings()
        {
            return _bindings[_bindingName];
        }

        public QueryState Clone()
        {
            var clone = new QueryState(_sqlBuilder.Clone(), _store, _collection);

            clone._bindingName = _bindingName;
            clone._bindings = new Dictionary<string, List<Type>>();
            foreach (var binding in _bindings)
            {
                clone._bindings.Add(binding.Key, new List<Type>(binding.Value));
            }

            clone._currentPredicate = (CompositeNode) _predicate.Clone();
            clone._predicate = clone._currentPredicate;
            
            clone._lastParameterName = _lastParameterName;
            clone._parameterBindings = _parameterBindings == null ? null : new List<Action<object, ISqlBuilder>>(_parameterBindings);

            return clone;
        }
    }

    public class DefaultQuery : IQuery
    {
        internal QueryState _queryState;
        private readonly Session _session;
        private readonly ISqlDialect _dialect;
        private object _compiledQuery = null;
        private string _collection;

        public static Dictionary<MethodInfo, Action<DefaultQuery, IStringBuilder, ISqlDialect, MethodCallExpression>> MethodMappings =
            new Dictionary<MethodInfo, Action<DefaultQuery, IStringBuilder, ISqlDialect, MethodCallExpression>>();

        static DefaultQuery()
        {
            MethodMappings[typeof(String).GetMethod("StartsWith", new Type[] { typeof(string) })] = static (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Object);
                builder.Append(" like ");
                query.ConvertFragment(builder, expression.Arguments[0]);
                var parameter = query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName];
                query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName] = parameter.ToString() + "%";
                builder.Append(")");
            };

            MethodMappings[typeof(String).GetMethod("EndsWith", new Type[] { typeof(string) })] = static (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Object);
                builder.Append(" like ");
                query.ConvertFragment(builder, expression.Arguments[0]);
                var parameter = query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName];
                query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName] = "%" + parameter.ToString();
                builder.Append(")");

            };

            MethodMappings[typeof(String).GetMethod("Contains", new Type[] { typeof(string) })] = static (query, builder, dialect, expression) =>
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
                static (query, builder, dialect, expression) =>
            {
                var generators = new List<Action<IStringBuilder>>();

                foreach (var argument in expression.Arguments)
                {
                    generators.Add(sb => query.ConvertFragment(sb, argument));
                }

                dialect.Concat(builder, generators.ToArray());
            };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsLike")] = static (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Arguments[0]);
                builder.Append(" like ");
                query.ConvertFragment(builder, expression.Arguments[1]);
                var parameter = query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName];
                query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName] = parameter.ToString();
                builder.Append(")");
            };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsNotLike")] = static (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Arguments[0]);
                builder.Append(" not like ");
                query.ConvertFragment(builder, expression.Arguments[1]);
                var parameter = query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName];
                query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName] = parameter.ToString();
                builder.Append(")");
            };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("NotContains")] = static (query, builder, dialect, expression) =>
            {
                builder.Append("(");
                query.ConvertFragment(builder, expression.Arguments[0]);
                builder.Append(" not like ");
                query.ConvertFragment(builder, expression.Arguments[1]);
                var parameter = query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName];
                query._queryState._sqlBuilder.Parameters[query._queryState._lastParameterName] = "%" + parameter.ToString() + "%";
                builder.Append(")");
            };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsIn")] =
                static (query, builder, dialect, expression) =>
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
                        var elements = new RentedStringBuilder(128);
                        for (var i = 0; i < values.Count; i++)
                        {
                            query.ConvertFragment(elements, Expression.Constant(values[i]));
                            if (i < values.Count - 1)
                            {
                                elements.Append(", ");
                            }
                        }

                        builder.Append(dialect.InOperator(elements.ToString()));

                        elements.Dispose();
                    }
                };

            MethodMappings[typeof(DefaultQueryExtensions).GetMethod("IsNotIn")] =
                static (query, builder, dialect, expression) =>
                {
                    // Could be simplified if int[] could be casted to IEnumerable<object>
                    var objects = Expression.Lambda(expression.Arguments[1]).Compile().DynamicInvoke() as IEnumerable;
                    var values = new List<object>();

                    foreach (var o in objects)
                    {
                        values.Add(o);
                    }

                    if (values.Count == 0)
                    {
                        builder.Append(" 1 = 1");
                    }
                    else if (values.Count == 1)
                    {
                        query.ConvertFragment(builder, expression.Arguments[0]);
                        builder.Append(" <> ");
                        query.ConvertFragment(builder, Expression.Constant(values[0]));
                    }
                    else
                    {
                        query.ConvertFragment(builder, expression.Arguments[0]);
                        var elements = new RentedStringBuilder(128);
                        for (var i = 0; i < values.Count; i++)
                        {
                            query.ConvertFragment(elements, Expression.Constant(values[i]));
                            if (i < values.Count - 1)
                            {
                                elements.Append(", ");
                            }
                        }

                        builder.Append(dialect.NotInOperator(elements.ToString()));

                        elements.Dispose();
                    }
                };

            MethodMappings[typeof(DefaultQueryExtensionsIndex).GetMethod("IsIn")] =
                static (query, builder, dialect, expression) =>
                {
                    InFilter(query, builder, dialect, expression, false, expression.Arguments[1], expression.Arguments[2]);
                };

            MethodMappings[typeof(DefaultQueryExtensionsIndex).GetMethod("IsNotIn")] =
                static (query, builder, dialect, expression) =>
                {
                    InFilter(query, builder, dialect, expression, true, expression.Arguments[1], expression.Arguments[2]);
                };

            MethodMappings[typeof(DefaultQueryExtensionsIndex).GetMethod("IsInAny")] =
                static (query, builder, dialect, expression) =>
                {
                    InFilter(query, builder, dialect, expression, false, expression.Arguments[1], null);
                };

            MethodMappings[typeof(DefaultQueryExtensionsIndex).GetMethod("IsNotInAny")] =
                static (query, builder, dialect, expression) =>
                {
                    InFilter(query, builder, dialect, expression, true, expression.Arguments[1], null);
                };
        }

        private static void InFilter(DefaultQuery query, IStringBuilder builder, ISqlDialect dialect, MethodCallExpression expression, bool negate, Expression selector, Expression indexFilter)
        {
            // type of the index
            var tIndex = ((LambdaExpression)((UnaryExpression)selector).Operand).Parameters[0].Type;

            // create new query here as the inner join should be in the sub query

            var sqlBuilder = query._dialect.CreateBuilder(query._session._store.Configuration.TablePrefix);

            query._queryState.AddBinding(tIndex);

            // Build inner query
            var _builder = new RentedStringBuilder(Store.MediumBufferSize);

            sqlBuilder.Select();

            query.ConvertFragment(_builder, ((LambdaExpression)((UnaryExpression)selector).Operand).Body);
            sqlBuilder.Selector(_builder.ToString());
            _builder.Clear();

            // Get the current collection name from the query state.
            var tableName = query._session._store.Configuration.TableNameConvention.GetIndexTable(tIndex, query._queryState._collection);
            sqlBuilder.Table(tableName, query._queryState.GetTypeAlias(tIndex));

            if (indexFilter != null)
            {
                query.ConvertPredicate(_builder, ((LambdaExpression)((UnaryExpression)indexFilter).Operand).Body);
                sqlBuilder.WhereAnd(_builder.ToString());
            }

            _builder.Dispose();

            query._queryState.RemoveBinding();

            // Insert query
            query.ConvertFragment(builder, expression.Arguments[0]);

            if (negate)
            {
                builder.Append(dialect.NotInSelectOperator(sqlBuilder.ToSqlString()));
            }
            else
            {
                builder.Append(dialect.InSelectOperator(sqlBuilder.ToSqlString()));
            }
        }

        public DefaultQuery(Session session, string tablePrefix, string collection)
        {
            _collection = collection;
            _session = session;
            _dialect = session.Store.Configuration.SqlDialect;
            _queryState = new QueryState(_dialect.CreateBuilder(tablePrefix), session.Store, collection);
        }

        public DefaultQuery(Session session, QueryState queryState, object compiledQuery)
        {
            _queryState = queryState;
            _compiledQuery = compiledQuery;
            _session = session;
            _dialect = session.Store.Configuration.SqlDialect;
        }

        public override string ToString()
        {
            return _queryState._sqlBuilder.ToSqlString();
        }

        private void Bind<TIndex>() where TIndex : IIndex
        {
            Bind(typeof(TIndex));
        }

        private void Bind(Type tIndex)
        {
            if (_queryState.GetBindings().Contains(tIndex))
            {
                return;
            }

            var name = tIndex.Name;
            var indexTable = _queryState._store.Configuration.TableNameConvention.GetIndexTable(tIndex, _collection);
            var indexTableAlias = _queryState.GetTypeAlias(tIndex);

            _queryState.AddBinding(tIndex);

            if (typeof(MapIndex).IsAssignableFrom(tIndex))
            {
                // inner join [PersonByName] as [PersonByName_a1] on [PersonByName_a1].[Id] = [Document].[Id] 
                _queryState._sqlBuilder.InnerJoin(indexTable, indexTableAlias, "DocumentId", _queryState._documentTable, "Id", indexTableAlias);
            }
            else
            {
                var bridgeName = indexTable + "_" + _queryState._documentTable;
                var bridgeAlias = _queryState.GetBridgeAlias(tIndex);

                // inner join [ArticlesByDay_Document] as [ArticlesByDay_Document_a1] on [ArticlesByDay_Document].[DocumentId] = [Document].[Id]
                _queryState._sqlBuilder.InnerJoin(bridgeName, bridgeAlias, "DocumentId", _queryState._documentTable, "Id", bridgeAlias);

                // inner join [ArticlesByDay] as [ArticlesByDay_a1] on [ArticlesByDay_a1].[Id] = [ArticlesByDay_Document].[ArticlesByDayId]
                _queryState._sqlBuilder.InnerJoin(indexTable, indexTableAlias, "Id", bridgeName, name + "Id", indexTableAlias, bridgeAlias);
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
                var indexTable = _queryState._store.Configuration.TableNameConvention.GetIndexTable(typeof(TIndex), _collection);

                _queryState.GetBindings().Clear();
                _queryState.AddBinding(typeof(TIndex));

                _queryState._sqlBuilder.Select();
                _queryState._sqlBuilder.Table(indexTable);
                _queryState._sqlBuilder.Selector(typeof(TIndex).Name, "DocumentId");
            }

            var builder = new RentedStringBuilder(Store.SmallBufferSize);
            // if Filter is called, the Document type is implicit so there is no need to filter on TIndex
            ConvertPredicate(builder, predicate.Body);

            _queryState._currentPredicate.Children.Add(new FilterNode(builder.ToString()));
            builder.Dispose();
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

                        _queryState._parameterBindings.Add((o, sqlBuilder) =>
                        {
                            var localValue = ((FieldInfo)memberExpression.Member).GetValue(o);

                            sqlBuilder.Parameters[_parameterName] = _dialect.TryConvert(localValue);
                        });

                        value = ((FieldInfo)memberExpression.Member).GetValue(obj);

                        return Expression.Constant(_dialect.TryConvert(value));
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

                        _queryState._parameterBindings.Add((o, sqlBuilder) =>
                        {
                            var localValue = ((PropertyInfo)memberExpression.Member).GetValue(o);

                            sqlBuilder.Parameters[_parameterName] = _dialect.TryConvert(localValue);
                        });

                        value = ((PropertyInfo)memberExpression.Member).GetValue(obj);

                        return Expression.Constant(_dialect.TryConvert(value));
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

        public void ConvertFragment(IStringBuilder builder, Expression expression)
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
                            _queryState._lastParameterName = "@p" + _queryState._sqlBuilder.Parameters.Count.ToString();
                            _queryState._sqlBuilder.Parameters.Add(_queryState._lastParameterName, _dialect.TryConvert(left.Value));
                            builder.Append(_queryState._lastParameterName);
                            
                            builder.Append(GetBinaryOperator(expression));

                            _queryState._lastParameterName = "@p" + _queryState._sqlBuilder.Parameters.Count.ToString();
                            _queryState._sqlBuilder.Parameters.Add(_queryState._lastParameterName, _dialect.TryConvert(right.Value));
                            builder.Append(_queryState._lastParameterName);
                        
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
                    var bound = _queryState.GetBindings().Last();

                    if (bound == typeof(Document))
                    {
                        var boundTable = _queryState._store.Configuration.TableNameConvention.GetDocumentTable(_queryState._collection);
                        builder.Append(_queryState._sqlBuilder.FormatColumn(boundTable, memberExpression.Member.Name));
                    }
                    else
                    {
                        var boundTable = _queryState.GetTypeAlias(bound);
                        builder.Append(_queryState._sqlBuilder.FormatColumn(boundTable, memberExpression.Member.Name, true));
                    }
                    
                    break;
                case ExpressionType.Constant:
                    _queryState._lastParameterName = "@p" + _queryState._sqlBuilder.Parameters.Count.ToString();
                    var value = ((ConstantExpression)expression).Value;
                    _queryState._sqlBuilder.Parameters.Add(_queryState._lastParameterName, _dialect.TryConvert(value));
                    builder.Append(_queryState._lastParameterName);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)expression;
                    var methodInfo = methodCallExpression.Method;
                    Action<DefaultQuery, IStringBuilder, ISqlDialect, MethodCallExpression> action;
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
        public void ConvertPredicate(IStringBuilder builder, Expression expression)
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

        private void ConvertComparisonBinaryExpression(IStringBuilder builder, BinaryExpression expression, string operation)
        {
            if (operation == " = " || operation == " <> ")
            {
                // Checking for NULL comparison
                var leftIsNull = IsNull(expression.Left);
                var rightIsNull = IsNull(expression.Right);

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

            bool IsNull(Expression e)
            {
                switch (e.NodeType)
                {
                    case ExpressionType.Constant:
                        return (e as ConstantExpression).Value == null;
                    case ExpressionType.MemberAccess:
                        return !IsParameterBased(e) && Evaluate(e).Value == null;
                }

                return false;
            }
        }

        private void ConvertConcatenateBinaryExpression(IStringBuilder builder, BinaryExpression expression)
        {
            _dialect.Concat(builder, b => ConvertFragment((RentedStringBuilder)b, expression.Left), b => ConvertFragment((RentedStringBuilder)b, expression.Right));
        }

        private void ConvertEqualityBinaryExpression(IStringBuilder builder, BinaryExpression expression, string operation)
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
            var builder = new RentedStringBuilder(Store.SmallBufferSize);
            ConvertFragment(builder, RemoveUnboxing(keySelector.Body));
            _queryState._sqlBuilder.OrderBy(builder.ToString());
            builder.Dispose();
        }

        private void ThenBy<T>(Expression<Func<T, object>> keySelector)
        {
            var builder = new RentedStringBuilder(Store.SmallBufferSize);
            ConvertFragment(builder, RemoveUnboxing(keySelector.Body));
            _queryState._sqlBuilder.ThenOrderBy(builder.ToString());
            builder.Dispose();
        }

        private void OrderByDescending<T>(Expression<Func<T, object>> keySelector)
        {
            var builder = new RentedStringBuilder(Store.SmallBufferSize);
            ConvertFragment(builder, RemoveUnboxing(keySelector.Body));
            _queryState._sqlBuilder.OrderByDescending(builder.ToString());
            builder.Dispose();
        }

        private void OrderByRandom()
        {
            _queryState._sqlBuilder.OrderByRandom();
        }

        private void ThenByDescending<T>(Expression<Func<T, object>> keySelector)
        {
            var builder = new RentedStringBuilder(Store.SmallBufferSize);
            ConvertFragment(builder, RemoveUnboxing(keySelector.Body));
            _queryState._sqlBuilder.ThenOrderByDescending(builder.ToString());
            builder.Dispose();
        }

        private void ThenByRandom()
        {
            _queryState._sqlBuilder.ThenOrderByRandom();
        }

        public async Task<int> CountAsync()
        {
            // Commit any pending changes before doing a query (auto-flush)
            await _session.FlushAsync();

            var connection = await _session.CreateConnectionAsync();
            var transaction = _session.CurrentTransaction;

            _queryState.FlushFilters();

            var localBuilder = _queryState._sqlBuilder.Clone();

            if (localBuilder.HasJoin)
            {
                localBuilder.Selector($"count(distinct {_queryState._sqlBuilder.FormatColumn(_queryState._documentTable, "Id")})");
            }
            else
            {
                localBuilder.Selector("count(*)");
            }

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
            var parameters = localBuilder.Parameters;
            var key = new WorkerQueryKey(sql, localBuilder.Parameters);

            try
            {
                return await _session._store.ProduceAsync(key, static (state) =>
                {
                    var logger = state.Session._store.Configuration.Logger;

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(state.Sql);
                    }

                    return state.Connection.ExecuteScalarAsync<int>(state.Sql, state.Parameters, state.Transaction);
                },
                new { Session = _session, Sql = sql, Parameters = parameters, Connection = connection, Transaction = transaction });
            }
            catch
            {
                await _session.CancelAsync();

                throw;
            }
        }

        IQuery<T> IQuery.For<T>(bool filterType)
        {
            _queryState.GetBindings().Clear();
            _queryState.AddBinding(typeof(Document));

            _queryState._sqlBuilder.Select();
            _queryState._sqlBuilder.Table(_queryState._documentTable);

            if (filterType)
            {
                _queryState._sqlBuilder.WhereAnd(_queryState._sqlBuilder.FormatColumn(_queryState._documentTable, "Type") + " = @Type"); // TODO: investigate, this makes the query 3 times slower on sqlite
                _queryState._sqlBuilder.Parameters["@Type"] = _session.Store.TypeNames[typeof(T)];
            }

            return new Query<T>(this);
        }

        IQueryIndex<TIndex> IQuery.ForIndex<TIndex>()
        {
            _queryState.GetBindings().Clear();
            _queryState.AddBinding(typeof(TIndex));
            _queryState._sqlBuilder.Select();
            _queryState._sqlBuilder.Table(_queryState._store.Configuration.TableNameConvention.GetIndexTable(typeof(TIndex), _collection), _queryState.GetTypeAlias(typeof(TIndex)));

            return new QueryIndex<TIndex>(this);
        }

        IQuery<object> IQuery.Any()
        {
            _queryState.GetBindings().Clear();
            _queryState.AddBinding(typeof(Document));

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

            public string GetTypeAlias(Type type)
            {
                return _query._queryState.GetTypeAlias(type);
            }

            public Task<T> FirstOrDefaultAsync()
            {
                return FirstOrDefaultImpl();
            }

            protected async Task<T> FirstOrDefaultImpl()
            {
                // Flush any pending changes before doing a query (auto-flush)
                await _query._session.FlushAsync();

                var connection = await _query._session.CreateConnectionAsync();
                var transaction = _query._session.CurrentTransaction;

                _query._queryState.FlushFilters();

                if (_query._compiledQuery != null && _query._queryState._parameterBindings != null)
                {
                    foreach (var binding in _query._queryState._parameterBindings)
                    {
                        binding(_query._compiledQuery, _query._queryState._sqlBuilder);
                    }
                }

                _query.Page(1, 0);

                try
                {
                    if (typeof(IIndex).IsAssignableFrom(typeof(T)))
                    {
                        _query._queryState._sqlBuilder.Selector("*");
                        var sql = _query._queryState._sqlBuilder.ToSqlString();
                        var key = new WorkerQueryKey(sql, _query._queryState._sqlBuilder.Parameters);
                        return (await _query._session._store.ProduceAsync(key, static (state) =>
                        {
                            var logger = state.Query._session._store.Configuration.Logger;

                            if (logger.IsEnabled(LogLevel.Debug))
                            {
                                logger.LogDebug(state.Sql);
                            }

                            return state.Connection.QueryAsync<T>(state.Sql, state.Query._queryState._sqlBuilder.Parameters, state.Transaction);

                        }, new { Query = _query, Sql = sql, Connection = connection, Transaction = transaction })).FirstOrDefault();
                    }
                    else
                    {
                        _query._queryState._sqlBuilder.Selector(_query._queryState._documentTable, "*");
                        var sql = _query._queryState._sqlBuilder.ToSqlString();
                        var key = new WorkerQueryKey(sql, _query._queryState._sqlBuilder.Parameters);
                        var documents = (await _query._session._store.ProduceAsync(key, static (state) =>
                        {
                            var logger = state.Query._session._store.Configuration.Logger;

                            if (logger.IsEnabled(LogLevel.Debug))
                            {
                                logger.LogDebug(state.Sql);
                            }

                            return state.Connection.QueryAsync<Document>(state.Sql, state.Query._queryState._sqlBuilder.Parameters, state.Transaction);
                        }, new { Query = _query, Sql = sql, Connection = connection, Transaction = transaction })).ToArray();

                        if (documents.Length == 0)
                        {
                            return default(T);
                        }

                        return _query._session.Get<T>(documents, _query._collection).FirstOrDefault();
                    }
                }
                catch
                {
                    await _query._session.CancelAsync();
                    throw;
                }
            }

            Task<IEnumerable<T>> IQuery<T>.ListAsync()
            {
                return ListImpl();
            }

            async IAsyncEnumerable<T> IQuery<T>.ToAsyncEnumerable()
            {
                // TODO: [IAsyncEnumerable] Once Dapper supports IAsyncEnumerable we can replace this call by a non-buffered one
                foreach(var item in await ListImpl())
                {
                    yield return item;
                }
            }

            internal async Task<IEnumerable<T>> ListImpl()
            {
                // TODO: [IAsyncEnumerable] Once Dapper supports IAsyncEnumerable we can return it by default, and buffer it in ListAsync instead

                // Flush any pending changes before doing a query (auto-flush)
                await _query._session.FlushAsync();

                var connection = await _query._session.CreateConnectionAsync();
                var transaction = _query._session.CurrentTransaction;

                _query._queryState.FlushFilters();

                if (_query._compiledQuery != null && _query._queryState._parameterBindings != null)
                {
                    foreach (var binding in _query._queryState._parameterBindings)
                    {
                        binding(_query._compiledQuery, _query._queryState._sqlBuilder);
                    }
                }

                try
                {
                    if (typeof(IIndex).IsAssignableFrom(typeof(T)))
                    {
                        _query._queryState._sqlBuilder.Selector("*");

                        // If a page is requested without order add a default one
                        if (!_query._queryState._sqlBuilder.HasOrder && _query._queryState._sqlBuilder.HasPaging)
                        {
                            _query._queryState._sqlBuilder.OrderBy(_query._queryState._sqlBuilder.FormatColumn(_query._queryState.GetTypeAlias(typeof(T)), "Id", true));
                        }

                        var sql = _query._queryState._sqlBuilder.ToSqlString();
                        var key = new WorkerQueryKey(sql, _query._queryState._sqlBuilder.Parameters);
                        return await _query._session._store.ProduceAsync(key, static (state) =>
                        {
                            var logger = state.Query._session._store.Configuration.Logger;

                            if (logger.IsEnabled(LogLevel.Debug))
                            {
                                logger.LogDebug(state.Sql);
                            }

                            return state.Connection.QueryAsync<T>(state.Sql, state.Query._queryState._sqlBuilder.Parameters, state.Transaction);
                        }, new { Query = _query, Sql = sql, Connection = connection, Transaction = transaction });
                    }
                    else
                    {
                        // If a page is requested without order add a default one
                        if (!_query._queryState._sqlBuilder.HasOrder && _query._queryState._sqlBuilder.HasPaging)
                        {
                            _query._queryState._sqlBuilder.OrderBy(_query._queryState._sqlBuilder.FormatColumn(_query._queryState._documentTable, "Id"));
                        }

                        _query._queryState._sqlBuilder.Selector(_query._queryState._sqlBuilder.FormatColumn(_query._queryState._documentTable, "*"));
                        _query._queryState._sqlBuilder.Distinct();
                        var sql = _query._queryState._sqlBuilder.ToSqlString();
                        var key = new WorkerQueryKey(sql, _query._queryState._sqlBuilder.Parameters);
                        var documents = await _query._session._store.ProduceAsync(key, static (state) =>
                        {
                            var logger = state.Query._session._store.Configuration.Logger;

                            if (logger.IsEnabled(LogLevel.Debug))
                            {
                                logger.LogDebug(state.Sql);
                            }

                            return state.Connection.QueryAsync<Document>(state.Sql, state.Query._queryState._sqlBuilder.Parameters, state.Transaction);
                        }, new { Query = _query, Sql = sql, Connection = connection, Transaction = transaction });

                        return _query._session.Get<T>(documents.ToArray(), _query._collection);
                    }
                }
                catch
                {
                    await _query._session.CancelAsync();

                    throw;
                }
            }

            IQuery<T> IQuery<T>.Skip(int count)
            {
                if (!_query._queryState._sqlBuilder.HasOrder)
                {
                    _query._queryState._sqlBuilder.OrderBy(_query._queryState._sqlBuilder.FormatColumn(_query._queryState._documentTable, "Id"));
                }

                _query._queryState._sqlBuilder.Skip(count.ToString());
                return this;
            }

            IQuery<T> IQuery<T>.Take(int count)
            {
                if (!_query._queryState._sqlBuilder.HasOrder)
                {
                    _query._queryState._sqlBuilder.OrderBy(_query._queryState._sqlBuilder.FormatColumn(_query._queryState._documentTable, "Id"));
                }

                _query._queryState._sqlBuilder.Take(count.ToString());
                return this;
            }

            Task<int> IQuery<T>.CountAsync()
            {
                return _query.CountAsync();
            }

            IQuery<T> IQuery<T>.Any(params Func<IQuery<T>, IQuery<T>>[] predicates)
            {
                // Scope the currentPredicate so multiple calls will not act on the new predicate.
                var currentPredicate = _query._queryState._currentPredicate;
                var query = ComposeQuery(predicates, new OrNode());
                // Return the currentPredicate to it's previous value, so another method call will act on the previous predicate.
                _query._queryState._currentPredicate = currentPredicate;

                return query;
            }

            ValueTask<IQuery<T>> IQuery<T>.AnyAsync(params Func<IQuery<T>, ValueTask<IQuery<T>>>[] predicates)
            {
                // Scope the currentPredicate so multiple calls will not act on the new predicate.
                var currentPredicate = _query._queryState._currentPredicate;
                var query = ComposeQueryAsync(predicates, new OrNode());
                // Return the currentPredicate to it's previous value, so another method call will act on the previous predicate.
                _query._queryState._currentPredicate = currentPredicate;

                return query;
            }

            IQuery<T> IQuery<T>.All(params Func<IQuery<T>, IQuery<T>>[] predicates)
            {
                // Scope the currentPredicate so multiple calls will not act on the new predicate.
                var currentPredicate = _query._queryState._currentPredicate;
                var query = ComposeQuery(predicates, new AndNode());
                // Return the currentPredicate to it's previous value, so another method call will act on the previous predicate.
                _query._queryState._currentPredicate = currentPredicate;

                return query;
            }

            ValueTask<IQuery<T>> IQuery<T>.AllAsync(params Func<IQuery<T>, ValueTask<IQuery<T>>>[] predicates)
            {
                // Scope the currentPredicate so multiple calls will not act on the new predicate.
                var currentPredicate = _query._queryState._currentPredicate;
                var query = ComposeQueryAsync(predicates, new AndNode());
                // Return the currentPredicate to it's previous value, so another method call will act on the previous predicate.
                _query._queryState._currentPredicate = currentPredicate;

                return query;
            }

            private IQuery<T> ComposeQuery(Func<IQuery<T>, IQuery<T>>[] predicates, CompositeNode predicate)
            {
                _query._queryState._currentPredicate.Children.Add(predicate);

                _query._queryState._currentPredicate = predicate;

                foreach (var p in predicates)
                {
                    var name = "a" + (_query._queryState._bindings.Count + 1);
                    _query._queryState._bindingName = name;
                    _query._queryState._bindings.Add(name, new List<Type>());

                    p(this);
                }

                return new Query<T>(_query);
            }

            private async ValueTask<IQuery<T>> ComposeQueryAsync(Func<IQuery<T>, ValueTask<IQuery<T>>>[] predicates, CompositeNode predicate)
            {
                _query._queryState._currentPredicate.Children.Add(predicate);

                _query._queryState._currentPredicate = predicate;

                foreach (var p in predicates)
                {
                    var name = "a" + (_query._queryState._bindings.Count + 1);
                    _query._queryState._bindingName = name;
                    _query._queryState._bindings.Add(name, new List<Type>());

                    await p(this);
                }

                return new Query<T>(_query);
            }            

            IQuery<T, TIndex> IQuery<T>.With<TIndex>()
            {
                _query.Bind<TIndex>();
                return new Query<T, TIndex>(_query);
            }

            IQuery<T> IQuery<T>.With(Type indexType)
            {
                if (!typeof(IIndex).IsAssignableFrom(indexType))
                {
                    throw new ArgumentException("The type must implement IIndex.", nameof(indexType));
                }

                if (!indexType.IsClass)
                {
                    throw new ArgumentException("The type must be a class.", nameof(indexType));
                }

                _query.Bind(indexType);
                return new Query<T>(_query);
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

            async IAsyncEnumerable<T> IQueryIndex<T>.ToAsyncEnumerable()
            {
                // TODO: [IAsyncEnumerable] Once Dapper supports IAsyncEnumerable we can replace this call by a non-buffered one
                foreach (var item in await ListImpl())
                {
                    yield return item;
                }
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
                _query._queryState._currentPredicate.Children.Add(new FilterNode(sql));

                return this;
            }

            IQueryIndex<T> IQueryIndex<T>.Where(Func<ISqlDialect, string> sql)
            {
                _query._queryState._currentPredicate.Children.Add(new FilterNode(sql?.Invoke(_query._dialect)));

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
                _query.Bind<TIndex>();
                //_query._queryState._currentFilter.Add(sql);

                _query._queryState._currentPredicate.Children.Add(new FilterNode(sql));

                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(Func<ISqlDialect, string> sql)
            {
                _query.Bind<TIndex>();
                _query._queryState._currentPredicate.Children.Add(new FilterNode(sql?.Invoke(_query._dialect)));
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.WithParameter(string name, object value)
            {
                _query.Bind<TIndex>();
                _query._queryState._sqlBuilder.Parameters[name] = value;
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(Expression<Func<TIndex, bool>> predicate)
            {
                _query.Bind<TIndex>();
                _query.Filter<TIndex>(predicate);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderBy(Expression<Func<TIndex, object>> keySelector)
            {
                _query.Bind<TIndex>();
                _query.OrderBy(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderBy(string sql)
            {
                _query.Bind<TIndex>();
                _query._queryState._sqlBuilder.OrderBy(sql);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.ThenBy(Expression<Func<TIndex, object>> keySelector)
            {
                _query.Bind<TIndex>();
                _query.ThenBy(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.ThenBy(string sql)
            {
                _query.Bind<TIndex>();
                _query._queryState._sqlBuilder.ThenOrderBy(sql);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderByDescending(Expression<Func<TIndex, object>> keySelector)
            {
                _query.Bind<TIndex>();
                _query.OrderByDescending(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderByDescending(string sql)
            {
                _query.Bind<TIndex>();
                _query._queryState._sqlBuilder.OrderByDescending(sql);
                return this;
            }

            public IQuery<T, TIndex> OrderByRandom()
            {
                _query.Bind<TIndex>();
                _query.OrderByRandom();
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.ThenByDescending(Expression<Func<TIndex, object>> keySelector)
            {
                _query.Bind<TIndex>();
                _query.ThenByDescending(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.ThenByDescending(string sql)
            {
                _query.Bind<TIndex>();
                _query._queryState._sqlBuilder.ThenOrderByDescending(sql);
                return this;
            }

            public IQuery<T, TIndex> ThenByRandom()
            {
                _query.Bind<TIndex>();
                _query.ThenByRandom();
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

        /// <summary>
        /// Whether the value matches the specified SQL filter with `%`.
        /// </summary>
        public static bool IsLike(this string source, string filter)
        {
            return false;
        }

        /// <summary>
        /// Whether the value doesn't match the specified SQL filter with `%`.
        /// </summary>
        public static bool IsNotLike(this string source, string filter)
        {
            return false;
        }

        /// <summary>
        /// Whether the value doesn't contain the specified text.
        /// </summary>
        public static bool NotContains(this string source, string text)
        {
            return false;
        }
    }

    public static class DefaultQueryExtensionsIndex
    {
        /// <summary>
        /// Matches all values that are in the specified <see cref="TIndex"/> index, and the specified predicate.
        /// </summary>
        public static bool IsIn<TIndex>(this object source, Expression<Func<TIndex, object>> select, Expression<Func<TIndex, bool>> where)
        {
            return false;
        }

        /// <summary>
        /// Matches all values that are in the specified <see cref="TIndex"/> index.
        /// </summary>
        public static bool IsInAny<TIndex>(this object source, Expression<Func<TIndex, object>> select)
        {
            return false;
        }

        /// <summary>
        /// Matches all values that are not in the specified <see cref="TIndex"/> index, and the specified predicate.
        /// </summary>
        public static bool IsNotIn<TIndex>(this object source, Expression<Func<TIndex, object>> select, Expression<Func<TIndex, bool>> where)
        {
            return false;
        }

        /// <summary>
        /// Matches all values that are not in the specified <see cref="TIndex"/> index.
        /// </summary>
        public static bool IsNotInAny<TIndex>(this object source, Expression<Func<TIndex, object>> select)
        {
            return false;
        }
    }
}
