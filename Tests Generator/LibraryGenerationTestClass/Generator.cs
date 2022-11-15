using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LibraryGenerationTestClass
{
    public class Generator
    {
        private readonly ClassGenerator classgenerator;

        public Generator(ClassGenerator classgenerator)
        {
            this.classgenerator = classgenerator;

        }
        public Task Generate()//Представляет асинхронную операцию. Одна задача может запускать другую - вложенную задачу. При этом эти задачи выполняются независимо друг от друга. 
        {
            var readFiles = new TransformBlock<string, string>(async path => await File.ReadAllTextAsync(path),//1 получаем на вход 1 стороку на выходе её отдаём
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = classgenerator.FilesReadCount,
                }
                );
            return null;
        }
        private async Task<Files[]> GenerateTestClasses(string fileText)
        {
            var root = CSharpSyntaxTree.ParseText(fileText).GetCompilationUnitRoot();//CSharpSyntaxTree - Проанализированное представление исходного документа C#. ParseText - Создает синтаксическое дерево путем анализа исходного текста.GetCompilationUnitRoot - Получает корень синтаксического дерева
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();//DescendantNodes - Возвращает коллекцию узлов-потомков для этого документа или элемента в порядке документа.
            var result = new List<Files>();

            foreach (var clas in classes)
            {
                result.Add(await GenerateTestClass(clas, root));
            }

            return result.ToArray();
        }
        private async Task<Files> GenerateTestClass(ClassDeclarationSyntax classDeclaration, CompilationUnitSyntax root)
        {
            return await Task.Run(() =>
            {
                var compilationUnit = SyntaxFactory.CompilationUnit();//Создает новый экземпляр CompilationUnitSyntax.

                compilationUnit = compilationUnit.AddUsings(GenerateTestUsings(root).ToArray());

               
                return testFile;
            });
        }
        private IEnumerable<UsingDirectiveSyntax> GenerateTestUsings(CompilationUnitSyntax root)
        {
            var result = new Dictionary<string, UsingDirectiveSyntax>();//Класс словаря Dictionary<K, V> типизируется двумя типами: параметр K представляет тип ключей, а параметр V предоставляет тип значений.

            var @namespace = root.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();//@-ссылка

            if (@namespace is not null)
            {
                var selfUsing = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(@namespace.Name.ToString()));//Создает новый экземпляр UsingDirectiveSyntax. IdentifierName - Создает новый экземпляр IdentifierNameSyntax.Класс, представляющий синтаксический узел для имени идентификатора.

                result.TryAdd(selfUsing.Name.ToString(), selfUsing);
            }

            foreach (var rootUsing in root.Usings)
            {
                result.TryAdd(rootUsing.Name.ToString(), rootUsing);
            }

            return result.Values;
        }

    }
}
