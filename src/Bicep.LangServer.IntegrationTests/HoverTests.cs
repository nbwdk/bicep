// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Bicep.Core.Samples;
using Bicep.LangServer.IntegrationTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Bicep.LangServer.IntegrationTests
{
    [TestClass]
    public class HoverTests
    {
        [DataTestMethod]
        [DynamicData(nameof(GetData), DynamicDataSourceType.Method, DynamicDataDisplayNameDeclaringType = typeof(DataSet), DynamicDataDisplayName = nameof(DataSet.GetDisplayName))]
        public async Task HoversShouldHover(DataSet dataSet)
        {
            var diagnosticsPublished = new TaskCompletionSource<PublishDiagnosticsParams>();
            var client = await IntegrationTestHelper.StartServerWithClientConnection(options =>
            {
                options.OnPublishDiagnostics(p => diagnosticsPublished.SetResult(p));
            });
            var uri = DocumentUri.From(dataSet.Name);

            // send open document notification
            client.DidOpenTextDocument(TextDocumentParamHelper.CreateDidOpenDocumentParams(uri, dataSet.Bicep, 0));

            // notifications don't produce responses,
            // but our server should send us diagnostics when it receives the notification
            await IntegrationTestHelper.WithTimeout(diagnosticsPublished.Task);

            // find positions to request hovers
            

            var hover = await client.RequestHover(new HoverParams
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Position = new Position(0, 0)
            });
        }

        private static IEnumerable<object[]> GetData()
        {
            return DataSets.AllDataSets.ToDynamicTestData();
        }
    }
}
