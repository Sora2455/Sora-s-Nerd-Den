using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Concurrency;
//using Microsoft.Concurrency.TestTools.UnitTesting;
//using Microsoft.Concurrency.TestTools.UnitTesting.Chess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.Concurrency.TestTools.UnitTesting.Assert;

//**********************************************************************************************
// Instruct Chess to instrument the System.dll assembly so we can catch data races in CLR classes 
// such as LinkedList{T}
//**********************************************************************************************
//[assembly: ChessInstrumentAssembly("System")]
//[assembly: ChessInstrumentAssembly("Concurrency")]

//Run //.\/mcut.exe runAllTests [PathToRepo]\Testing\bin\Debug\Testing.dll
//From //[PathToRepo]\packages\Chess.1.0.1\tools
//Correct as needed

namespace Testing
{
    [TestClass]
    public class ConcurrentDictionaryOfCollectionsTests
    {
        [TestMethod]
        public void AddAndGetSingleValues()
        {
            ConcurrentDictionaryOfCollections<int, string> testDic = 
                new ConcurrentDictionaryOfCollections<int, string>();

            testDic.Add(0, "Hi");
            testDic.Add(1, "There");
            testDic.Add(2, "Hello");
            testDic.Add(3, "World");

            Assert.AreEqual(testDic.Get(0).FirstOrDefault(), "Hi");
            Assert.AreEqual(testDic.Get(1).FirstOrDefault(), "There");
            Assert.AreEqual(testDic.Get(2).FirstOrDefault(), "Hello");
            Assert.AreEqual(testDic.Get(3).FirstOrDefault(), "World");
        }

        [TestMethod]
        public void AddAndGetGroupedValues()
        {
            ConcurrentDictionaryOfCollections<int, string> testDic =
                new ConcurrentDictionaryOfCollections<int, string>();

            testDic.Add(0, "Hi");
            testDic.Add(0, "There");
            testDic.Add(0, "Hello");
            testDic.Add(0, "World");

            string[] finalValues = testDic.Get(0).ToArray();

            Assert.AreEqual(finalValues[0], "Hi");
            Assert.AreEqual(finalValues[1], "There");
            Assert.AreEqual(finalValues[2], "Hello");
            Assert.AreEqual(finalValues[3], "World");
        }

        [TestMethod]
        public void GetAll()
        {
            ConcurrentDictionaryOfCollections<int, string> testDic =
                new ConcurrentDictionaryOfCollections<int, string>();

            testDic.Add(0, "Hi");
            testDic.Add(0, "There");
            testDic.Add(1, "Hello");
            testDic.Add(1, "World");

            List<string> finalValues = testDic.GetAll();

            Assert.AreEqual(finalValues[0], "Hi");
            Assert.AreEqual(finalValues[1], "There");
            Assert.AreEqual(finalValues[2], "Hello");
            Assert.AreEqual(finalValues[3], "World");
        }

        [TestMethod]
        //[DataRaceTestMethod]
        public void ConcurrentAddDiffKeys()
        {
            for (int i = 0; i < 100; i++)
            {
                ConcurrentDictionaryOfCollections<int, string> testDic =
                    new ConcurrentDictionaryOfCollections<int, string>();

                Parallel.Invoke(() =>
                {
                    testDic.Add(0, "Hi");
                }, () => {
                    testDic.Add(1, "There");
                });

                Assert.AreEqual(testDic.Get(0).FirstOrDefault(), "Hi");
                Assert.AreEqual(testDic.Get(1).FirstOrDefault(), "There");
            }
        }

        [TestMethod]
        //[DataRaceTestMethod]
        public void ConcurrentAddSameKey()
        {
            for (int i = 0; i < 100; i++)
            {
                ConcurrentDictionaryOfCollections<int, string> testDic =
                new ConcurrentDictionaryOfCollections<int, string>();

                Parallel.Invoke(() =>
                {
                    testDic.Add(0, "Hi");
                }, () => {
                    testDic.Add(0, "There");
                });

                List<string> finalValues = testDic.Get(0).ToList();

                Assert.IsTrue(finalValues.Contains("Hi"));
                Assert.IsTrue(finalValues.Contains("There"));
            }
        }
    }
}
