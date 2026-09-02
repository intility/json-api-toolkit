using System.Reflection;
using System.Runtime.Loader;

namespace JsonApiToolkit.TypeGen;

/// <summary>
/// The tool's actual logic, split out of Program.cs so it is callable
/// in-process from tests (exercising the real assembly-loading path against
/// a separately-built DLL, not just the pure string emission).
/// </summary>
public static class TypeGenCli
{
    public static int Run(string[] args)
    {
        string? assemblyPath = null;
        string? outPath = null;
        string clientImport = "@intility/json-api-client";
        bool check = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--assembly":
                    assemblyPath = args[++i];
                    break;
                case "--out":
                    outPath = args[++i];
                    break;
                case "--client-import":
                    clientImport = args[++i];
                    break;
                case "--check":
                    check = true;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    return 1;
            }
        }

        if (assemblyPath is null || outPath is null)
        {
            Console.Error.WriteLine(
                "Usage: jsonapi-typegen --assembly <path/to/Api.dll> --out <path/to/api-types.gen.ts> "
                    + "[--client-import <specifier>] [--check]"
            );
            return 1;
        }

        assemblyPath = Path.GetFullPath(assemblyPath);
        var loadContext = new PluginLoadContext(assemblyPath);
        Assembly assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

        var resources = new List<(Type Type, string WireType)>();
        foreach (Type type in GetLoadableTypes(assembly))
        {
            string? wireType = GetJsonApiResourceTypeName(type);
            if (wireType != null)
                resources.Add((type, wireType));
        }

        if (resources.Count == 0)
        {
            Console.Error.WriteLine($"No [JsonApiResource] types found in {assemblyPath}.");
            return 1;
        }

        string generated = TypeScriptEmitter.Generate(resources, clientImport);

        if (check)
        {
            string? existing = File.Exists(outPath) ? File.ReadAllText(outPath) : null;
            if (existing == generated)
            {
                Console.WriteLine($"{outPath} is up to date ({resources.Count} resources).");
                return 0;
            }

            Console.Error.WriteLine(
                $"{outPath} is out of date. Run without --check to regenerate."
            );
            return 1;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, generated);
        Console.WriteLine($"Wrote {resources.Count} resources to {outPath}.");
        return 0;
    }

    private static string? GetJsonApiResourceTypeName(Type type)
    {
        // Matched by name, not by typeof(...), so the tool works regardless of
        // which JsonApiToolkit.dll copy the target assembly loaded against.
        CustomAttributeData? attributeData = type.GetCustomAttributesData()
            .FirstOrDefault(a => a.AttributeType.Name == "JsonApiResourceAttribute");

        return attributeData?.ConstructorArguments is [{ Value: string wireType }]
            ? wireType
            : null;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}

/// <summary>
/// Resolves the target assembly's own dependencies (e.g. EF Core) from its
/// output directory, the same trick `dotnet` plugin hosts use.
/// </summary>
internal sealed class PluginLoadContext(string mainAssemblyPath) : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver = new(mainAssemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}
