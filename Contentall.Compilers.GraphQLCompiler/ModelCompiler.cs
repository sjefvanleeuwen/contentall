
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace Contentall.Compilers.GraphQLCompiler
{
    public class ModelCompiler
    {
        public CompilerResults result;

        public ModelCompiler(string modelSource)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            string outputExe = "Out.exe";
            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = true,
                OutputAssembly = outputExe,
                GenerateInMemory = true
            };
           // parameters.ReferencedAssemblies = new System.Collections.Specialized.StringCollection()
            var compiler = codeProvider.CreateCompiler(); //CompileAssemblyFromSource(parameters, File.ReadAllText(modelSource));
           // result = compiler.CompileAssemblyFromFile(parameters, modelSource);


        }
    }
}