using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetOptNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetOptNet.Tests {

    [TestClass]
    public class GetOptTests {

        [TestMethod, ExpectedException(typeof(ArgumentNullException)]
        public void Parse_NullOptionsTest() {
            GetOpt.Parse(Array.Empty<string>();
        }

        [TestMethod]
        public void Parse_NullArgsTest() {
            Assert.Fail();
        }

        [TestMethod]
        public void Parse_InvalidOptionsTest() {
            Assert.Fail();
        }
    }
}
