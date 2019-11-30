using System;
using System.Linq;
using Common.Concurrency;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
