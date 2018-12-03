using System;
using System.IO;
using System.Reflection;
using CfdiSAT;
using CfdiSAT.Dto;
using Xunit;

namespace TestSatClient
{
    public class ClientSatUnitTest
    {
        [Fact]
        public void TestAutentica()
        {
            var certificate = new Certificate("km33jc24k", File.ReadAllBytes(GetPath("Certificates/efirma.pfx")));
            var client = new CfdiRecoveryClient(certificate);
            var response = client.Autentica();

            response.Wait();
            Assert.NotNull(response.Result);
        }

        private static string GetPath(string relativePath)
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return Path.Combine(dirPath, relativePath);
        }
    }
}
