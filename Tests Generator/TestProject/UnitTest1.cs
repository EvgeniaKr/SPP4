using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using LibraryGenerationTestClass;
namespace TestProject
{
    [TestClass]
    public class UnitTest
    {
        private readonly string _writePath = @"C:\mywork\SPPlabs\SPP_4\Tests Generator\resclass";
        private readonly string _testClass1Path = @"C:\mywork\SPPlabs\SPP_4\Tests Generator\TestClass\Class1.cs";

        [TestMethod]
        public void Analyze_GeneratingTests_UsingsCorrect()
        {

            var classgenerator = new ClassGenerator(3, 6, 3, @"C:\mywork\SPPlabs\SPP_4\Tests Generator\resclass",
                 new List<string>()
                {
                    @"C:\mywork\SPPlabs\SPP_4\Tests Generator\TestClass\Class1.cs",
                });
            var generator = new Generator(classgenerator);
            var expected = new List<string>()
            {
                "TestClass",
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks"
            };

            generator.Generate().Wait();
            var actual = CSharpSyntaxTree
                .ParseText(File.ReadAllText(Path.Combine(_writePath, "Class1Tests.cs")))
                .GetCompilationUnitRoot().Usings.Select(syntax => syntax.Name.ToString());

            CollectionAssert.AreEqual(expected, actual.ToList<string>());
        }

        [TestMethod]
        public void Analyze_GeneratingTests_NamespaceCorrect()
        {
            var classgenerator = new ClassGenerator(3, 6, 3, @"C:\mywork\SPPlabs\SPP_4\Tests Generator\resclass",
                new List<string>()
               {
                    @"C:\mywork\SPPlabs\SPP_4\Tests Generator\TestClass\Class1.cs",
               });
            var generator = new Generator(classgenerator);
            var expected = "TestClass.Tests";

            generator.Generate().Wait();
            var actual = CSharpSyntaxTree
                .ParseText(File.ReadAllText(Path.Combine(_writePath, "Class1Tests.cs")))
                .GetCompilationUnitRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>().First().Name.ToString();

            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void Analyze_GeneratingTests_ClassCorrect()
        {
            var classgenerator = new ClassGenerator(3, 6, 3, @"C:\mywork\SPPlabs\SPP_4\Tests Generator\resclass",
                new List<string>()
               {
                    @"C:\mywork\SPPlabs\SPP_4\Tests Generator\TestClass\Class1.cs",
               });
            var generator = new Generator(classgenerator);
            var expected = "Class1Tests";

            generator.Generate().Wait();
            var actual = CSharpSyntaxTree
                .ParseText(File.ReadAllText(Path.Combine(_writePath, "Class1Tests.cs")))
                .GetCompilationUnitRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

            //Assert.Single(actual);

            Assert.AreEqual(expected, actual.First().Identifier.Text);
        }

        [TestMethod]
        public void Analyze_GeneratingTests_MethodsNamesCorrect()
        {
            var classgenerator = new ClassGenerator(3, 6, 3, @"C:\mywork\SPPlabs\SPP_4\Tests Generator\resclass",
                new List<string>()
               {
                    @"C:\mywork\SPPlabs\SPP_4\Tests Generator\TestClass\Class1.cs",
               });
            var generator = new Generator(classgenerator);
            var expected = new List<string>()
            {
                "FirstMethodTest",
                "SecondMethodTest",
                "ThirdMethodTest",
                "ThirdMethodTest1",
            };

            generator.Generate().Wait();
            var actual = CSharpSyntaxTree
                .ParseText(File.ReadAllText(Path.Combine(_writePath, "Class1Tests.cs")))
                .GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Select(syntax => syntax.Identifier.Text);

            CollectionAssert.AreEqual(expected, actual.ToList<string>());
        }
    }
}