using LibraryGenerationTestClass;
namespace Program
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var classgenerator = new ClassGenerator(3, 6, 3, @"C:\mywork\SPPlabs\SPP_4\Tests Generator\resclass",
                 new List<string>()
                {
                    @"C:\mywork\SPPlabs\SPP_4\Tests Generator\TestClass\Class1.cs",
                });
            

            var generator = new Generator(classgenerator);

            generator.Generate().Wait();
        }
    }
}