using System.Linq.Expressions;
using System.Reflection;

namespace JsonApiToolkit.Extensions.Projection;

/// <summary>
/// Builds LINQ MemberInitExpression trees for EF Core Select() projections.
/// </summary>
internal static class ProjectionExpressionBuilder
{
    /// <summary>
    /// Builds a lambda expression: <c>entity => new ProjectionType { Prop1 = entity.Prop1, ... }</c>.
    /// Returns a <see cref="LambdaExpression"/> typed as <c>Func&lt;TSource, ProjectionType&gt;</c> at runtime.
    /// </summary>
    internal static LambdaExpression Build(
        Type sourceType,
        Type projectionType,
        IReadOnlyList<PropertyInfo> sourceProperties
    )
    {
        ParameterExpression param = Expression.Parameter(sourceType, "e");

        MemberBinding[] bindings = sourceProperties
            .Select(sourceProp =>
            {
                PropertyInfo projProp =
                    projectionType.GetProperty(sourceProp.Name)
                    ?? throw new InvalidOperationException(
                        $"Projection type '{projectionType.Name}' is missing property '{sourceProp.Name}'. "
                            + "This indicates a bug in DynamicTypeBuilder — the generated type should have all requested properties."
                    );

                return (MemberBinding)
                    Expression.Bind(projProp, Expression.Property(param, sourceProp));
            })
            .ToArray();

        MemberInitExpression memberInit = Expression.MemberInit(
            Expression.New(projectionType),
            bindings
        );

        return Expression.Lambda(memberInit, param);
    }
}
