using System.Reflection;
using System.Reflection.Emit;

namespace JsonApiToolkit.Extensions.Projection;

/// <summary>
/// Generates concrete projection types at runtime using Reflection.Emit.
/// EF Core requires a concrete type to translate Select() expressions to SQL.
/// </summary>
internal static class DynamicTypeBuilder
{
    private static readonly AssemblyBuilder s_assemblyBuilder;
    private static readonly ModuleBuilder s_moduleBuilder;
    private static readonly object s_buildLock = new();
    private static int s_typeCounter;

    static DynamicTypeBuilder()
    {
        var assemblyName = new AssemblyName("JsonApiToolkit.Projections");
        s_assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run
        );
        s_moduleBuilder = s_assemblyBuilder.DefineDynamicModule("MainModule");
    }

    /// <summary>
    /// Builds a new public class with the specified properties (each with a public getter and setter).
    /// </summary>
    internal static Type Build(IEnumerable<(string Name, Type PropertyType)> properties)
    {
        // ModuleBuilder is not thread-safe; serialize type creation
        lock (s_buildLock)
        {
            int id = Interlocked.Increment(ref s_typeCounter);
            string typeName = $"JsonApiProjection_{id}";

            TypeBuilder typeBuilder = s_moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public
                    | TypeAttributes.Class
                    | TypeAttributes.AutoLayout
                    | TypeAttributes.AnsiClass
                    | TypeAttributes.BeforeFieldInit
            );

            foreach (var (name, propertyType) in properties)
            {
                FieldBuilder field = typeBuilder.DefineField(
                    $"_backingField_{name}",
                    propertyType,
                    FieldAttributes.Private
                );

                PropertyBuilder property = typeBuilder.DefineProperty(
                    name,
                    PropertyAttributes.None,
                    propertyType,
                    null
                );

                MethodAttributes accessorAttributes =
                    MethodAttributes.Public
                    | MethodAttributes.SpecialName
                    | MethodAttributes.HideBySig;

                MethodBuilder getter = typeBuilder.DefineMethod(
                    $"get_{name}",
                    accessorAttributes,
                    propertyType,
                    Type.EmptyTypes
                );
                ILGenerator getIL = getter.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, field);
                getIL.Emit(OpCodes.Ret);
                property.SetGetMethod(getter);

                MethodBuilder setter = typeBuilder.DefineMethod(
                    $"set_{name}",
                    accessorAttributes,
                    null,
                    [propertyType]
                );
                ILGenerator setIL = setter.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Stfld, field);
                setIL.Emit(OpCodes.Ret);
                property.SetSetMethod(setter);
            }

            return typeBuilder.CreateType()!;
        }
    }
}
