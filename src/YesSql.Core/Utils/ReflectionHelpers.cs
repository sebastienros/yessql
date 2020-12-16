using System;
using System.Linq.Expressions;
using System.Reflection;

namespace YesSql.Utils
{
    public class ReflectionHelpers
    {
        public static PropertyInfo FromExpression<T>(Expression<Func<T, object>> expression)
        {
            MemberExpression Exp = null;

            // this line is necessary, because sometimes the expression comes in as Convert(originalexpression)
            if (expression.Body is UnaryExpression)
            {
                var UnExp = (UnaryExpression)expression.Body;
                if (!(UnExp.Operand is MemberExpression))
                {
                    throw new ArgumentException();
                }

                Exp = (MemberExpression)UnExp.Operand;
            }
            else if (expression.Body is MemberExpression)
            {
                Exp = (MemberExpression)expression.Body;
            }

            if (Exp == null)
            {
                throw new ArgumentException();
            }

            return (PropertyInfo)Exp.Member;
        }
    }
}
