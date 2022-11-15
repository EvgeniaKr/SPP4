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
            var generateTestsByFile = new TransformManyBlock<string, Files>// получаем на вход 1 стороку на выходе преобразуем её в несколлько обьектов классов
           (
             async data => await GenerateTestClasses(data),
             new ExecutionDataflowBlockOptions
             {
                 MaxDegreeOfParallelism = classgenerator.ClassesGenerateCount,
             }
             );
            var writeFile = new ActionBlock<Files>//1 получаем на вход 1 стороку на выходе ничего не отдаём
          (
              async data => await WriteFileAsync(data),
              new ExecutionDataflowBlockOptions
              {
                  MaxDegreeOfParallelism = classgenerator.FilesWriteCount,
              }
          );

            readFiles.LinkTo(generateTestsByFile, new DataflowLinkOptions { PropagateCompletion = true });
            generateTestsByFile.LinkTo(writeFile, new DataflowLinkOptions { PropagateCompletion = true });

            foreach (var path in classgenerator.FilesTestClasssesPaths)
            {
                readFiles.Post(path);
            }
            
            readFiles.Complete();

            return writeFile.Completion;
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
                var baseNamespace = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();//Возвращает коллекцию узлов-потомков для этого документа или элемента в порядке документа.

                var @namespace = SyntaxFactory.NamespaceDeclaration(baseNamespace is null ? SyntaxFactory.IdentifierName("Tests") : SyntaxFactory.IdentifierName($"{baseNamespace.Name}.Tests"));

                var @class = SyntaxFactory.ClassDeclaration(classDeclaration.Identifier.Text + "Tests")
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithAttributeLists(
                        SyntaxFactory.SingletonList
                        (
                            SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("TestClass"))))
                        )
                    );
                @class = @class.AddMembers(GenerateTestMethods(classDeclaration).ToArray());

                compilationUnit = compilationUnit.AddMembers(@namespace.AddMembers(@class));
                var testFile = new Files(@class.Identifier.Text, compilationUnit.NormalizeWhitespace("    ", "\r\n").ToString());
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
        private IEnumerable<MemberDeclarationSyntax> GenerateTestMethods(ClassDeclarationSyntax classDeclaration)
        {
            var result = new List<MemberDeclarationSyntax>();

            var methodsDeclarations = classDeclaration.Members.OfType<MethodDeclarationSyntax>().Where(syntax => syntax.Modifiers.Any(SyntaxKind.PublicKeyword));

            var uniqueMethodsNames = new List<string>();

            foreach (var methodDeclaration in methodsDeclarations)
            {
                var body = new List<StatementSyntax>();

                var baseUniqueName = methodDeclaration.Identifier.Text + "Test";
                string uniqueName;
                var i = 0;
                do
                {
                    if (i == 0)
                    {
                        uniqueName = baseUniqueName;
                    }
                    else
                        uniqueName = baseUniqueName + i.ToString();
                    i++;
                } while (uniqueMethodsNames.Contains(uniqueName));
                uniqueMethodsNames.Add(uniqueName);

                body.Add(GenerateAssertFailStatement());

                var method =
                    SyntaxFactory.MethodDeclaration(
                         SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        SyntaxFactory.Identifier(uniqueName)
                    )
                    .WithAttributeLists(
                        SyntaxFactory.SingletonList
                        (
                            SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("TestMethod"))))
                        )
                    )
                    .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                            )
                    )
                    .WithBody(SyntaxFactory.Block(body));

                result.Add(method);
            }

            return result;
        }
        private StatementSyntax GenerateAssertFailStatement()
        {
            var assert = SyntaxFactory.IdentifierName("Assert");
            var fail = SyntaxFactory.IdentifierName("Fail");
            var memberaccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, assert, fail);

            var argument = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("autogenerated")));
            var argumentList = SyntaxFactory.SeparatedList(new[] { argument });

            var statement =
                SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(memberaccess,
                SyntaxFactory.ArgumentList(argumentList)));

            return statement;
        }


        private async Task WriteFileAsync(Files data)
        {
            var filePath = Path.Combine(classgenerator.Path, $"{data.Name}.cs");
            await File.WriteAllTextAsync(filePath, data.Data);
        }

    }
}
