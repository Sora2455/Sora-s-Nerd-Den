using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Concurrency;
using Microsoft.Concurrency.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.Concurrency.TestTools.UnitTesting.Assert;

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

        [TestMethod, DataRaceTestMethod]
        public void ConcurrentAddDiffKeys()
        {
            ConcurrentDictionaryOfCollections<int, string> testDic =
                new ConcurrentDictionaryOfCollections<int, string>();

            Parallel.Invoke(() =>
            {
                testDic.Add(0, "Hi");
            }, () => {
                testDic.Add(1, "There");
            });

            List<string> finalValues = testDic.GetAll();

            Assert.AreEqual(testDic.Get(0).FirstOrDefault(), "Hi");
            Assert.AreEqual(testDic.Get(1).FirstOrDefault(), "There");
        }
    }
}
