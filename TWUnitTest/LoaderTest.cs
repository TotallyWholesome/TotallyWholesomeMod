using WholesomeLoader;

namespace TWUnitTest
{

    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void LoaderCVRVersionCheck()
        {
            var oldVersion = new CVRVersion("2022r170p2");
            var currentVersion = new CVRVersion("2023r172ex1p2");
            var patchVersion = new CVRVersion("2023r172ex1p3");
            var expVersion = new CVRVersion("2023r172ex2p2");
            var nextVersion = new CVRVersion("2023r172");
            
            Assert.That(currentVersion.IsVersionNewer(currentVersion), Is.EqualTo(0));
            Assert.That(currentVersion.IsVersionNewer(patchVersion), Is.EqualTo(-1));
            Assert.That(currentVersion.IsVersionNewer(oldVersion), Is.EqualTo(1));
            Assert.That(currentVersion.IsVersionNewer(expVersion), Is.EqualTo(-1));
            Assert.That(currentVersion.IsVersionNewer(nextVersion), Is.EqualTo(-1));
        }
    }
}