using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xutils.DynamicCode.Tests
{
    public interface DynamicCode
    {
        string ParseString(string input);
    }

    public class TestClass : DynamicCode
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
            DynamicCode compiledCode = Compiler<DynamicCode>.Compile("testAssembly", @"
namespace Xutils.DynamicCode.Tests
{
    public class TestClass : IXutils.DynamicCode
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
