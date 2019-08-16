using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xutils.DynamicCode.Tests
{
    public interface IDynamicCode
    {
        string ParseString(string input);
    }

    public class TestClass : IDynamicCode
    {
        public string ParseString(string input)
        {
            return $"Input was: {input}";
        }
    }

    [TestClass]
    public class FunctionCompilerTests
    {
        [TestMethod]
        public void CompileTest()
        {
            IDynamicCode compiledCode = Compiler<IDynamicCode>.Compile("testAssembly", @"
namespace Xutils.DynamicCode.Tests
{
    public class TestClass : IDynamicCode
    {
        public string ParseString(string input)
        {
            return $""Input was: { input}"";
        }
    }
}");
            string output = compiledCode.ParseString("this is the input");
            Assert.AreEqual("Input was: this is the input", output);
        }
    }
}
