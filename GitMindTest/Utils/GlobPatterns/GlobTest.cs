using GitMind.Utils.GlobPatterns;
using NUnit.Framework;


namespace GitMindTest.Utils.GlobPatterns
{
    [TestFixture]
    public class GlobTest
    {
        [Test]
        public void ShouldMatchFullGlob()
        {
            var glob = new Glob("Foo.txt");
            Assert.False(glob.IsMatch("SomeFoo.txt")); // this fails because IsMatch is true
        }

        [Test]
        public void Test()
        {
            Assert.IsTrue(Glob.IsMatch("som/folder/bin/SomeFoo.txt", "**/bin/**/*"));

            Assert.IsTrue(Glob.IsMatch("GitMind\\Dependencies\\Autofac.dllt", "GitMind/Dependencies/**/*"));

            //Assert.IsTrue(Glob.IsMatch("som/folder/bin/SomeFoo.txt", "**/[Dd]ebug//**/*"));
        }


        [Test]
        public void CanParseSimpleFilename()
        {
            var glob = new Glob("*.txt");
            Assert.True(glob.IsMatch("file.txt"));
            Assert.False(glob.IsMatch("file.zip"));
            Assert.True(glob.IsMatch(@"c:\windows\file.txt"));
        }

        [Test]
        public void CanParseDots()
        {
            var glob = new Glob("/some/dir/folder/foo.*");
            Assert.True(glob.IsMatch("/some/dir/folder/foo.txt"));
            Assert.True(glob.IsMatch("/some/dir/folder/foo.csv"));
        }

        [Test]
        public void CanMatchUnderscore()
        {
            var glob = new Glob("a_*file.txt");
            Assert.True(glob.IsMatch("a_bigfile.txt"));
            Assert.True(glob.IsMatch("a_file.txt"));
            Assert.False(glob.IsMatch("another_file.txt"));
        }

        [Test]
        public void CanMatchSingleFile()
        {
            var glob = new Glob("*file.txt");
            Assert.True(glob.IsMatch("bigfile.txt"));
            Assert.True(glob.IsMatch("smallfile.txt"));
        }


        [Test]
        public void CanMatchSingleFileOnExtension()
        {
            var glob = new Glob("folder/*.txt");
            Assert.True(glob.IsMatch("folder/bigfile.txt"));
            Assert.True(glob.IsMatch("folder/smallfile.txt"));
            Assert.False(glob.IsMatch("folder/smallfile.txt.min"));
        }

        [Test]
        public void CanMatchSingleFileWithAnyNameOrExtension()
        {
            var glob = new Glob("folder/*.*");
            Assert.True(glob.IsMatch("folder/bigfile.txt"));
            Assert.True(glob.IsMatch("folder/smallfile.txt"));
            Assert.True(glob.IsMatch("folder/smallfile.txt.min"));
        }

        [Test]
        public void CanMatchSingleFileUsingCharRange()
        {
            var glob = new Glob("*fil[e-z].txt");
            Assert.True(glob.IsMatch("bigfile.txt"));
            Assert.True(glob.IsMatch("smallfilf.txt"));
            Assert.False(glob.IsMatch("smallfila.txt"));
            Assert.False(glob.IsMatch("smallfilez.txt"));
        }

        [Test]
        public void CanMatchSingleFileUsingNumberRange()
        {
            var glob = new Glob("*file[1-9].txt");
            Assert.True(glob.IsMatch("bigfile1.txt"));
            Assert.True(glob.IsMatch("smallfile8.txt"));
            Assert.False(glob.IsMatch("smallfile0.txt"));
            Assert.False(glob.IsMatch("smallfilea.txt"));
        }

        [Test]
        public void CanMatchSingleFileUsingCharList()
        {
            var glob = new Glob("*file[abc].txt");
            Assert.True(glob.IsMatch("bigfilea.txt"));
            Assert.True(glob.IsMatch("smallfileb.txt"));
            Assert.False(glob.IsMatch("smallfiled.txt"));
            Assert.False(glob.IsMatch("smallfileaa.txt"));
        }

        [Test]
        public void CanMatchSingleFileUsingInvertedCharList()
        {
            var glob = new Glob("*file[!abc].txt");
            Assert.False(glob.IsMatch("bigfilea.txt"));
            Assert.False(glob.IsMatch("smallfileb.txt"));
            Assert.True(glob.IsMatch("smallfiled.txt"));
            Assert.True(glob.IsMatch("smallfile-.txt"));
            Assert.False(glob.IsMatch("smallfileaa.txt"));
        }

        [Test]
        public void CanMatchDirectoryWildcardInTopLevelDirectory()
        {
            const string globPattern = @"/**/somefile";
            var glob = new Glob(globPattern);
            Assert.True(glob.IsMatch("/somefile"));
        }

        [Test]
        ////[TestCase("C:\\sources\\x-y 1\\BIN\\DEBUG\\COMPILE\\**\\MSVC*120.DLL", false, "C:\\sources\\x-y 1\\BIN\\DEBUG\\COMPILE\\ANTLR3.RUNTIME.DLL")]      // Attempted repro for https://github.com/dazinator/DotNet.Glob/issues/37
        ////[TestCase("literal", false, "fliteral", "foo/literal", "literals", "literals/foo")]
        ////[TestCase("Shock* 12", false, "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
        ////[TestCase("*Shock* 12", false, "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
        ////[TestCase("*ave*2", false, "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
        ////[TestCase("*ave 12", false, "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
        ////[TestCase("*ave 12", false, "wave 12/")]

        [TestCase("literal", false, "fliteral", "literals", "literals/foo")]
        [TestCase("path/hats*nd", false, "path/hatsblahn", "path/hatsblahndt")]
        [TestCase("path/?atstand", false, "path/moatstand", "path/batstands")]
        [TestCase("/**/file.csv", false, "/file.txt")]
        [TestCase("/*file.txt", false, "/folder")]
        [TestCase("C:\\THIS_IS_A_DIR\\**\\somefile.txt", false, "C:\\THIS_IS_A_DIR\\awesomefile.txt")] // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/27
        [TestCase("C:\\name\\**", false, "C:\\name.ext", "C:\\name_longer.ext")] // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/29
        [TestCase("Bumpy/**/AssemblyInfo.cs", false, "Bumpy.Test/Properties/AssemblyInfo.cs")]      // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/33

        [TestCase("literal1", false, "LITERAL1")] // Regression tests for https://github.com/dazinator/DotNet.Glob/issues/41
        [TestCase("*ral*", false, "LITERAL1")] // Regression tests for https://github.com/dazinator/DotNet.Glob/issues/41
        [TestCase("[list]s", false, "LS", "iS", "Is")] // Regression tests for https://github.com/dazinator/DotNet.Glob/issues/41
        [TestCase("range/[a-b][C-D]", false, "range/ac", "range/Ad", "range/BD")] // Regression tests for https://github.com/dazinator/DotNet.Glob/issues/41
        public void Does_Not_Match(string pattern, bool allowInvalidPathCharcters, params string[] testStrings)
        {
            var glob = new Glob(pattern);
            foreach (string testString in testStrings)
            {
                Assert.IsFalse(glob.IsMatch(testString));
            }
        }

        [Test]
        [TestCase("literal", "literal")]
        [TestCase("a/literal", "a/literal")]
        [TestCase("path/*atstand", "path/fooatstand")]
        [TestCase("path/hats*nd", "path/hatsforstand")]
        [TestCase("path/?atstand", "path/hatstand")]
        [TestCase("path/?atstand?", "path/hatstands")]
        [TestCase("p?th/*a[bcd]", "pAth/fooooac")]
        [TestCase("p?th/*a[bcd]b[e-g]a[1-4]", "pAth/fooooacbfa2")]
        [TestCase("p?th/*a[bcd]b[e-g]a[1-4][!wxyz]", "pAth/fooooacbfa2v")]
        [TestCase("p?th/*a[bcd]b[e-g]a[1-4][!wxyz][!a-c][!1-3].*", "pAth/fooooacbfa2vd4.txt")]
        [TestCase("path/**/somefile.txt", "path/foo/bar/baz/somefile.txt")]
        [TestCase("p?th/*a[bcd]b[e-g]a[1-4][!wxyz][!a-c][!1-3].*", "pGth/yGKNY6acbea3rm8.")]
        [TestCase("/**/file.*", "/folder/file.csv")]
        [TestCase("/**/file.*", "/file.txt")]
        [TestCase("/**/file.*", "/file.txt")]
        [TestCase("**/file.*", "/file.txt")]
        [TestCase("/*file.txt", "/file.txt")]
        [TestCase("C:\\THIS_IS_A_DIR\\*", "C:\\THIS_IS_A_DIR\\somefile")] // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/20
        [TestCase("/DIR1/*/*", "/DIR1/DIR2/file.txt")]  // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/21
                                                        //[TestCase("~/*~3", "~/abc123~3")]  // Regression Test for https://github.com/dazinator/DotNet.Glob/pull/15
                                                        //[TestCase("**\\Shock* 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
        [TestCase("**\\*ave*2", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
        [TestCase("**", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
        [TestCase("**", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12.txt")]
        //[TestCase("Stuff, *", "Stuff, x")]      // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/31
        //[TestCase("\"Stuff*", "\"Stuff")]      // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/32
        [TestCase("path/**/somefile.txt", "path//somefile.txt")]
        [TestCase("**/app*.js", "dist/app.js", "dist/app.a72ka8234.js")]      // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/34
        [TestCase("**/y", "y")]      // Regression Test for https://github.com/dazinator/DotNet.Glob/issues/44       
        public void IsMatch(string pattern, params string[] testStrings)
        {
            var glob = new Glob(pattern);
            foreach (string testString in testStrings)
            {
                Assert.IsTrue(glob.IsMatch(testString));
            }
        }

        [Test]
        [TestCase("literal1", "LITERAL1", "literal1")]
        [TestCase("*ral*", "LITERAL1", "literal1")]
        [TestCase("[list]s", "LS", "ls", "iS", "Is")]
        [TestCase("range/[a-b][C-D]", "range/ac", "range/Ad", "range/bC", "range/BD")]
        public void IsMatchCaseInsensitive(string pattern, params string[] testStrings)
        {
            var glob = new Glob(pattern);
            foreach (string testString in testStrings)
            {
                Assert.IsTrue(glob.IsMatch(testString));
            }
        }
    }
}