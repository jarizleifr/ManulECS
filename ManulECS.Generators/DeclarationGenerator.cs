using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Text;

namespace ManulECS.Generators {
  [Generator]
  public class DeclarationGenerator : ISourceGenerator {
    public void Execute(GeneratorExecutionContext context) {

      // Find all structs that have a IComponent or ITag marker interface
      var componentTypes = context.Compilation.SyntaxTrees
        .SelectMany(syntaxTree => {
          var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
          return syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<StructDeclarationSyntax>()
            .Select(s => (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(s))
            .Where(s => s.AllInterfaces.Any(i => i.Name == "IComponent" || i.Name == "ITag"))
            .Select(s => s.Name);
        }).ToList();

      // Build up the source code
      var sb = new StringBuilder();
      sb.AppendLine("// Auto-generated code");
      sb.AppendLine($"namespace {context.Compilation.AssemblyName} {{");
      sb.AppendLine("public static class ManulECSExtensions {");
      sb.AppendLine("public static void DeclareAll(this ManulECS.World world) {");
      componentTypes.ForEach(c => sb.AppendLine($"world.Declare<{c}>();"));
      sb.AppendLine("}");
      sb.AppendLine("}");
      sb.AppendLine("}");

      // Format code and save it as a source file
      context.AddSource("World.Declarations.g.cs", 
        SyntaxFactory.ParseCompilationUnit(sb.ToString())
          .NormalizeWhitespace()
          .GetText(Encoding.Unicode));
    }

    public void Initialize(GeneratorInitializationContext context) { }
  }
}
