using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Workflow.Tests
{
   [TestClass]
   public class UnitTest1
   {
      [TestMethod]
      public void Test1()
      {
         var class1 = new Class1();

         Assert.AreEqual("Foo", class1.Value);
      }
   }
}
