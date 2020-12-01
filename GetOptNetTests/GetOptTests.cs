#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;

namespace GetOptNet.Tests {

    [TestClass]
    public class GetOptTests {

        private void AssertEqual(GetOpt.ParsedArgs expected, GetOpt.ParsedArgs actual) {
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(expected.Values, actual.Values);
            Assert.AreEqual(expected.Options.Count, actual.Options.Count);
            foreach (var expectedPair in expected.Options) {
                if (actual.Options.TryGetValue(expectedPair.Key, out var value)) {
                    if (expectedPair.Value is ICollection collection) {
                        CollectionAssert.AreEqual(collection, value as ICollection);
                    } else {
                        Assert.AreEqual(expectedPair.Value, value);
                    }
                } else {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parse_NullOptionsTest() {
            GetOpt.Parse(Array.Empty<string>(), null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parse_NullArgsTest() {
            GetOpt.Parse(null!, "a,b,c");
        }

        [TestMethod]
        public void Parse_InvalidOptionsTest() {
            var args = new[] { "" };
            var invalidOptionsStrings = new[] {
                "a;b;c",
                "a|b,b|c",
                "abc--def",
                "a++",
                "a:str"
            };
            foreach (var str in invalidOptionsStrings) {
                Assert.ThrowsException<FormatException>(
                    () => GetOpt.Parse(args, str),
                    $"Options string '{str}' is not valid."
                );
            }
        }

        [TestMethod]
        public void Parse_SimpleOptionsHelpTest() {
            AssertEqual(
                new GetOpt.ParsedArgs(new[] { ("help", (object?)true) }, Array.Empty<string>()),
                GetOpt.Parse(new[] { "-h" }, "n,e,help|h,version")
            );
        }

        [TestMethod]
        public void Parse_SimpleOptionsVersionTest() {
            AssertEqual(
                new GetOpt.ParsedArgs(new[] { ("version", (object?)true) }, Array.Empty<string>()),
                GetOpt.Parse(new[] { "--version" }, "n,e,help|h,version")
            );
        }

        [TestMethod]
        public void Parse_SimpleOptionsShortHelpTest() {
            AssertEqual(
                new GetOpt.ParsedArgs(new[] { ("help", (object?)true) }, Array.Empty<string>()),
                GetOpt.Parse(new[] { "-h" }, "n,e,help|h,version")
            );
        }

        [TestMethod]
        public void Parse_SimpleOptionsMergedTest() {
            AssertEqual(
                new GetOpt.ParsedArgs(
                    new (string, object?)[] { ("e", true), ("n", true) },
                    Array.Empty<string>()
                ),
                GetOpt.Parse(new[] { "-en" }, "n,e,help|h,version")
            );
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOptionsException))]
        public void Parse_SimpleOptionsDuplicateErrorTest() =>
            GetOpt.Parse(new[] { "-nen" }, "n,e,help|h,version");

        [TestMethod]
        public void Parse_SimpleOptionsMixedTest() {
            AssertEqual(
                new GetOpt.ParsedArgs(
                    new (string, object?)[] { ("all", true), ("escape", true) },
                    Array.Empty<string>()
                ),
                GetOpt.Parse(new[] { "-a", "--escape" }, "all|a,escape|b,help|h,version")
            );
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOptionsException))]
        public void Parse_SimpleOptionsMixedDuplicateErrorTest() =>
            GetOpt.Parse(new[] { "-nen" }, "all|a,almost-all|A,help|h,version");

        [TestMethod]
        public void Parse_RepeatableModifierTest() {
            AssertEqual(
                new GetOpt.ParsedArgs(new[] { ("verbose", (object?)3) }, Array.Empty<string>()),
                GetOpt.Parse(new[] { "-vv", "--verbose" }, "verbose|v+,help|h,version")
            );
        }

        [TestMethod]
        public void Parse_ValueModifierTest() {
            AssertEqual(
                new GetOpt.ParsedArgs(
                    new (string, object?)[] {
                        ("one", "1"), ("two", "2"), ("three", "3"), ("four", "4")
                    },
                    Array.Empty<string>()
                ),
                GetOpt.Parse(
                    new[] {
                        "-o=1",
                        "-t", "2",
                        "--three=3",
                        "--four", "4"
                    },
                    "one|o:,two|t:,three:,four:,help|h,version"
                )
            );
        }

        [TestMethod]
        public void Parse_RepeatableValueModifierTest() {
            AssertEqual(
                new GetOpt.ParsedArgs(
                    new (string, object?)[] {
                        ("file", new[] { "file1.txt", "file2.dat", "file3.jpg", "file4.png" })
                    },
                    Array.Empty<string>()
                ),
                GetOpt.Parse(
                    new[] {
                        "-f=file1.txt", "-f", "file2.dat", "--file=file3.jpg", "--file", "file4.png"
                    },
                    "file|f:+,help|h,version"
                )
            );
        }

        [TestMethod]
        public void Parse_RequiredModifierTest() {
            Assert.ThrowsException<InvalidOptionsException>(
                () => GetOpt.Parse(new[] { "-ne" }, "n,e,f!")
            );
            Assert.ThrowsException<InvalidOptionsException>(
                () => GetOpt.Parse(Array.Empty<string>(), "n,e,f!+")
            );
        }
    }
}
