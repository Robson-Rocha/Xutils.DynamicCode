using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace DynamicCode
{

    public static class Compiler<I> where I : class
    {
        //HACK: Instância de uma classe do assembly que deverá estar carregado no AppDomain
        private static readonly DummyForLoading dummy = new DummyForLoading();
        private static readonly RuntimeBinderException exHack = new RuntimeBinderException();
        private static readonly Dictionary<string, I> Cache = new Dictionary<string, I>();

        public static void ReloadAll()
            => Cache.Clear();

        public static bool TryCompile(string assemblyName, string sourceCode, out I compiledClass, out IEnumerable<string> errors)
        {
            try
            {
                compiledClass = Compile(assemblyName, sourceCode);
                errors = new string[0];
                return true;
            }
            catch (CompilationFailedException cfe)
            {
                compiledClass = default;
                errors = cfe.Errors;
                return false;
            }
        }

        public static I Compile(string assemblyName, string sourceCode)
        {
            if (Cache.ContainsKey(assemblyName))
                return Cache[assemblyName];

            dynamic hack = JsonConvert.SerializeObject("");

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            List<MetadataReference> references = new List<MetadataReference>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (!assembly.IsDynamic && File.Exists(assembly.Location))
                        references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch (NotSupportedException) { }
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    List<string> errorsLog = new List<string>();

                    foreach (Diagnostic diagnostic in failures)
                    {
                        errorsLog.Add($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                    }
                    File.WriteAllLines(assemblyName + ".compilerErrors.txt", errorsLog);
                    throw new CompilationFailedException("The compilation failed", errorsLog);
                }

                ms.Seek(0, SeekOrigin.Begin);
                Assembly generatedAssembly = AssemblyLoadContext.Default.LoadFromStream(ms);

                System.Reflection.TypeInfo[] types = generatedAssembly.GetTypes().Cast<System.Reflection.TypeInfo>().ToArray();
                System.Reflection.TypeInfo iType = (System.Reflection.TypeInfo)typeof(I);

                Type generatedType = types.FirstOrDefault(t => t == iType || t.ImplementedInterfaces.Any(ti => ti == iType));
                if (generatedType == null)
                    throw new Exception($"No class named {nameof(I)} was found into the loaded assembly");

                I compiledClass = (I)Activator.CreateInstance(generatedType);

                Cache.Add(assemblyName, compiledClass);

                return compiledClass;
            }
        }
    }
}
