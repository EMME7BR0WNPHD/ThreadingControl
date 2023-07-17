using System.IO;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using ThreadingControl.Analyzer;

namespace ThreadingControl.Test
{
    [TestClass]
    public class ThreadingControlUnitTest
    {
        [TestMethod]
        public async Task SameThreadInvocation()
        {
            await TestCase(nameof(SourceCode.SameThreadInvocation));
        }

        [TestMethod]
        public async Task CrossThreadInvocation()
        {
            var expected = new DiagnosticResult(MyAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithLocation(16, 13);
            await TestCase(nameof(SourceCode.CrossThreadInvocation), expected);
        }

        //[TestMethod]
        //public async Task PropertyAccess()
        //{
        //    var expected = new DiagnosticResult(MyAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithLocation(16, 13);
        //    await TestCase(nameof(SourceCode.PropertyAccess), expected);
        //}

        [TestMethod]
        public async Task NestedCallInvocation()
        {
            var expected = new DiagnosticResult(MyAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithLocation(16, 13);
            await TestCase(nameof(SourceCode.NestedInvocation), expected);

        }

        [TestMethod]
        public async Task ChainedCallInvocation()
        {
            var expected = new DiagnosticResult(MyAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithLocation(18, 17);
            await TestCase(nameof(SourceCode.ChainedInvocation), expected);
        }

        [TestMethod]
        public async Task PipelineInvocation()
        {
            await TestCase(nameof(SourceCode.PipelineInvocation));
        }

        private async Task TestCase(string sourceName, params DiagnosticResult[] expectedDiagnosticResults)
        {
            var source = await File.ReadAllTextAsync(@$"SourceCode\{sourceName}.cs");

            var test = new CSharpAnalyzerTest<MyAnalyzer, MSTestVerifier>
            {
                TestCode = source,
                TestState =
                {
                    AdditionalReferences =
                    {
                        MetadataReference.CreateFromFile(typeof(PipelineAttribute).Assembly.Location)
                    }
                }
            };

            test.ExpectedDiagnostics.AddRange(expectedDiagnosticResults);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
