namespace JsonApiToolkit.TypeGen.Tests;

/// <summary>
/// Exercises the tool against ContractApi.dll, a real assembly built from a
/// separate project (not the test's own in-process types). This is what
/// actually proves the load-context resolution and name-based attribute
/// matching in Cli.cs work, since ContractApi carries its own copy of
/// JsonApiToolkit.dll, a different Assembly instance than the one this test
/// project references directly.
/// </summary>
public class CliIntegrationTests
{
    [Fact]
    public void Run_resolves_JsonApiResource_types_from_a_separately_built_assembly()
    {
        var contractApiDll = typeof(ContractApi.Author).Assembly.Location;
        var outPath = Path.Join(Path.GetTempPath(), $"cli-test-{Guid.NewGuid():N}.ts");

        try
        {
            var exit = TypeGenCli.Run(["--assembly", contractApiDll, "--out", outPath]);
            Assert.Equal(0, exit);

            var generated = File.ReadAllText(outPath);
            Assert.Contains("export interface Article", generated);
            Assert.Contains("export interface Author", generated);
            Assert.Contains("export interface Comment", generated);
            Assert.Contains(
                "export const Article: JsonApiResourceDescriptor<Article> = {",
                generated
            );
            Assert.Contains("  toOne: [\"author\"],", generated);
            Assert.Contains("  toMany: [\"comments\"],", generated);

            // --check passes right after generation...
            Assert.Equal(
                0,
                TypeGenCli.Run(["--assembly", contractApiDll, "--out", outPath, "--check"])
            );

            // ...and fails once the file drifts from what generation would produce.
            File.AppendAllText(outPath, "// drift\n");
            Assert.Equal(
                1,
                TypeGenCli.Run(["--assembly", contractApiDll, "--out", outPath, "--check"])
            );
        }
        finally
        {
            File.Delete(outPath);
        }
    }
}
