using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace CssCombiner.Test
{
    /// <summary>
    /// This test class tests the functionality of the CssCombiner class.
    /// </summary>
    [TestFixture]
    public class CssCombinerTests : NAntTask.CssCombiner
    {
        private CssCombinerTests cssCombiner;
        private const string testFilesDirectory = "../../test files/";
       
        [TestFixtureSetUp()]
        public void TestInitialize()
        {
            cssCombiner = new CssCombinerTests { Verbose = true, AssemblyVersion = "1234" };
        }

        private IEnumerable<string> GetFileNames()
        {
            var directory = new DirectoryInfo(testFilesDirectory + "expected");
            var files = directory.GetFiles();

            var fileNames = new string[files.Length];
            for(var i = 0; i < files.Length; i++)
            {
                fileNames[i] = files[i].Name;
            }

            return fileNames;
        }

        [Test, TestCaseSource("GetFileNames")]
        public void TestAppendBuildVersionToImagesUrls(string fileName)
        {
            var expected = ReadContent(testFilesDirectory + "expected/" + fileName);
            var input = ReadContent(testFilesDirectory + "actual/" + fileName);
            var actual = cssCombiner.AppendBuildVersionToImagesUrls(input);

            Assert.AreEqual(expected, actual, "The actual and expected output for {0} differ", fileName);
        }

        private static string ReadContent(string filePath)
        {
            using (var tr = new StreamReader(filePath))
            {
                return tr.ReadToEnd();
            }
        }
    }
}
