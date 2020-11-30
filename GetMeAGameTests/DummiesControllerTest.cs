using COMP2084GetMeAGame.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace GetMeAGameTests
{
    [TestClass]
    public class DummiesControllerTest
    {
        [TestMethod]
        public void IndexReturnsSomething()
        {
            // arrange
            var controller = new DummiesController();

            // act - call the Index method and store the result that comes back
            var result = controller.Index();

            // assert
            Assert.IsNotNull(result);
        }
    }
}
