using System;
using System.Linq.Expressions;
using System.Reflection;

namespace DeltaComparer
{
    /// <summary>
    /// Contains extensions for help with expressions
    /// There is no claim to copyright on any of this and licenses belong to original authors
    /// </summary>
    public static class ExpressionHelpers
    {
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left, right), parameter);
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _oldValue)
                    return _newValue;
                return base.Visit(node);
            }
        }

        public static string MemberName<T, V>(this Expression<Func<T, V>> expression)
        {
            MemberExpression body = expression.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)expression.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }

        public static MemberExpression GetFinalMemberOfExpression<T, V>(this Expression<Func<T, V>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;

            if (memberExpression == null)
            {
                var mce = expression.Body as MethodCallExpression;

                if (mce == null)
                {
                    var unaryExpression = expression.Body as UnaryExpression;

                    if (unaryExpression.Operand.IsMemberExpression())
                        memberExpression = unaryExpression.Operand as MemberExpression;

                    if (memberExpression != null)
                        return memberExpression;
                }

                LambdaExpression lambdaExpression = null;

                var loopCount = 0; // do this JUST in case.

                do
                {
                    if (loopCount > 1000)
                        break;

                    lambdaExpression = (LambdaExpression)mce.Arguments[1];

                    if (lambdaExpression.Body.IsMethodCallExpression())
                    {
                        mce = lambdaExpression.Body as MethodCallExpression;
                    }
                    else
                    {
                        mce = null;
                    }
                    loopCount++;
                } while (mce != null);

                memberExpression = lambdaExpression.Body as MemberExpression;

                if (memberExpression == null)
                    throw new InvalidOperationException("Expression must be a member expression");
            }

            return memberExpression;
        }

        public static bool IsMethodCallExpression(this Expression expression)
        {
            try
            {
                var memberExpression = expression as MethodCallExpression;

                if (memberExpression == null)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMemberExpression(this Expression expression)
        {
            var memberExpression = expression as MemberExpression;

            if (memberExpression == null)
                return false;
            else
                return true;
        }

        public static T SetPropertyValue<T>(this T target, Expression<Func<T, object>> memberLamda, object value)
        {
            var memberSelectorExpression = memberLamda.Body as MemberExpression;

            if (memberSelectorExpression == null)
            {
                UnaryExpression ubody = (UnaryExpression)memberLamda.Body;
                memberSelectorExpression = ubody.Operand as MemberExpression;
            }

            if (memberSelectorExpression != null)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null)
                {
                    property.SetValue(target, value, null);
                }
            }

            return target;
        }

        public static void SetPropertyValue<T, TKey>(this T target, Expression<Func<T, TKey>> memberLamda, object value)
        {
            var memberSelectorExpression = memberLamda.Body as MemberExpression;

            if (memberSelectorExpression == null)
            {
                UnaryExpression ubody = (UnaryExpression)memberLamda.Body;
                memberSelectorExpression = ubody.Operand as MemberExpression;
            }

            if (memberSelectorExpression != null)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null)
                {
                    property.SetValue(target, value, null);
                }
            }
        }
    }
}
