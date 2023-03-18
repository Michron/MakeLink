using Xunit;

namespace MakeLink.UnitTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Platform compatibility is asserted in the constructor.")]
    public class JunctionLinkTests
    {
        private readonly DirectoryInfo _workspace;

        public JunctionLinkTests()
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
            {
                Assert.Fail("Unable to run JunctionLink tests on non-windows platforms.");
            }

            string workspace = Path.Combine(Environment.CurrentDirectory, "TestWorkspace");
            _workspace = new DirectoryInfo(workspace);

            ClearWorkspace();
        }

        [Fact]
        public void Create_JunctionLink()
        {
            (DirectoryInfo target, FileInfo file) = CreateTestDirectory();

            string link = Path.Combine(_workspace.FullName, "Link");
            string linkedFile = Path.Combine(link, file.Name);

            JunctionLink.Create(link, target.FullName, overwrite: false);

            Assert.True(Directory.Exists(link));
            Assert.True(File.Exists(linkedFile));
        }

        [Fact]
        public void Create_Overwrites_Empty_Folder()
        {
            (DirectoryInfo target, FileInfo file) = CreateTestDirectory();

            string link = Path.Combine(_workspace.FullName, "Link");
            string linkedFile = Path.Combine(link, file.Name);

            Directory.CreateDirectory(link);

            JunctionLink.Create(link, target.FullName, overwrite: true);

            Assert.True(Directory.Exists(link));
            Assert.True(File.Exists(linkedFile));
        }

        [Fact]
        public void Create_Overwrites_Existing_Junction()
        {
            (DirectoryInfo target1, FileInfo file1) = CreateTestDirectory("Target1", "TestFile1");
            (DirectoryInfo target2, FileInfo file2) = CreateTestDirectory("Target2", "TestFile2");

            string link = Path.Combine(_workspace.FullName, "Link");
            string linkedFile1 = Path.Combine(link, file1.Name);
            string linkedFile2 = Path.Combine(link, file2.Name);

            JunctionLink.Create(link, target1.FullName, overwrite: false);

            Assert.True(Directory.Exists(link));
            Assert.True(File.Exists(linkedFile1));
            Assert.False(File.Exists(linkedFile2));

            JunctionLink.Create(link, target2.FullName, overwrite: true);

            Assert.False(File.Exists(linkedFile1));
            Assert.True(File.Exists(linkedFile2));
            Assert.True(Directory.Exists(link));
        }

        [Fact]
        public void Create_Throws_If_Existing_Folder()
        {
            (DirectoryInfo target, FileInfo file) = CreateTestDirectory();

            string link = Path.Combine(_workspace.FullName, "Link");

            Directory.CreateDirectory(link);

            Assert.Throws<IOException>(() => JunctionLink.Create(link, target.FullName, overwrite: false));
        }

        [Fact]
        public void Create_Throws_If_Existing_Junction()
        {
            (DirectoryInfo target, FileInfo file) = CreateTestDirectory();

            string link = Path.Combine(_workspace.FullName, "Link");

            JunctionLink.Create(link, target.FullName, overwrite: false);

            Assert.Throws<IOException>(() => JunctionLink.Create(link, target.FullName, overwrite: false));
        }

        [Fact]
        public void Create_Throws_If_Target_Does_Not_Exist()
        {
            DirectoryInfo target = new(Path.Combine(_workspace.FullName, "Target"));
            string link = Path.Combine(_workspace.FullName, "Link");

            Assert.False(target.Exists);
            Assert.Throws<DirectoryNotFoundException>(() => JunctionLink.Create(link, target.FullName, overwrite: false));
        }

        [Fact]
        public void Create_Throws_If_Overwrite_Non_Empty_Folder()
        {
            (DirectoryInfo target, FileInfo file) = CreateTestDirectory();
            CreateTestDirectory("Link");

            string link = Path.Combine(_workspace.FullName, "Link");
            string linkedFile = Path.Combine(link, file.Name);

            Assert.True(File.Exists(linkedFile));
            Assert.Throws<IOException>(() => JunctionLink.Create(link, target.FullName, overwrite: true));
        }

        [Fact]
        public void Delete_JunctionLink()
        {
            (DirectoryInfo target, FileInfo file) = CreateTestDirectory();

            string link = Path.Combine(_workspace.FullName, "Link");
            string linkedFile = Path.Combine(link, file.Name);

            JunctionLink.Create(link, target.FullName, overwrite: false);

            Assert.True(Directory.Exists(link));
            Assert.True(File.Exists(linkedFile));

            JunctionLink.Delete(link);

            Assert.False(File.Exists(linkedFile));
            Assert.False(Directory.Exists(link));
        }

        [Fact]
        public void Delete_NonExisting_JunctionLink()
        {
            string nonExistingPath = Path.Combine(_workspace.FullName, "NonExistingPath");

            Assert.False(Directory.Exists(nonExistingPath));

            JunctionLink.Delete(nonExistingPath);
        }

        [Fact]
        public void Delete_Throws_If_Not_JunctionLink()
        {
            (DirectoryInfo target, FileInfo file) = CreateTestDirectory();

            Assert.Throws<IOException>(() => JunctionLink.Delete(target.FullName));
            Assert.Throws<IOException>(() => JunctionLink.Delete(file.FullName));
        }

        [Fact]
        public void Exists_JunctionLink()
        {
            (DirectoryInfo target, FileInfo _) = CreateTestDirectory();

            string link = Path.Combine(_workspace.FullName, "Link");

            JunctionLink.Create(link, target.FullName, overwrite: false);

            Assert.True(JunctionLink.Exists(link));
        }

        [Fact]
        public void Exists_NonExisting_Path()
        {
            string nonExistingPath = Path.Combine(_workspace.FullName, "NonExistingPath");

            Assert.False(JunctionLink.Exists(nonExistingPath));
        }

        [Fact]
        public void Exists_Directory()
        {
            (DirectoryInfo target, FileInfo _) = CreateTestDirectory();

            Assert.False(JunctionLink.Exists(target.FullName));
        }

        [Fact]
        public void GetTarget_JunctionLink()
        {
            (DirectoryInfo target, FileInfo _) = CreateTestDirectory();

            string link = Path.Combine(_workspace.FullName, "Link");

            JunctionLink.Create(link, target.FullName, overwrite: false);

            string? result = JunctionLink.GetTarget(link);

            Assert.NotNull(result);
            Assert.Equal(target.FullName, Path.GetFullPath(result));
        }

        [Fact]
        public void GetTarget_NonExisting_Path()
        {
            string nonExistingPath = Path.Combine(_workspace.FullName, "NonExistingPath");

            string? result = JunctionLink.GetTarget(nonExistingPath);

            Assert.Null(result);
        }

        [Fact]
        public void GetTarget_Directory()
        {
            (DirectoryInfo target, FileInfo file) = CreateTestDirectory();

            string? result = JunctionLink.GetTarget(target.FullName);

            Assert.Null(result);
        }

        [Fact]
        public void GetTarget_File()
        {
            (DirectoryInfo _, FileInfo file) = CreateTestDirectory();

            string? result = JunctionLink.GetTarget(file.FullName);

            Assert.Null(result);
        }

        private (DirectoryInfo directory, FileInfo file) CreateTestDirectory(string directoryName = "Target", string fileName = "TestFile")
        {
            DirectoryInfo directory = new(Path.Combine(_workspace.FullName, directoryName));
            FileInfo file = new(Path.Combine(directory.FullName, $"{fileName}.txt"));

            directory.Create();
            using var stream = file.CreateText();
            stream.WriteLine($"Test File {fileName}");

            return (directory, file);
        }

        private void ClearWorkspace()
        {
            if (_workspace.Exists)
            {
                // _workspace.Delete(recursive: true) throws an UnauthorizedAccessException in some cases.
                // Manually deleting the directory avoids this somehow.
                DeleteDirectoryRecursively(_workspace);
            }

            _workspace.Create();
        }

        private void DeleteDirectoryRecursively(DirectoryInfo directory)
        {
            IEnumerable<DirectoryInfo> directories = directory.EnumerateDirectories();

            foreach (DirectoryInfo d in directories)
            {
                DeleteDirectoryRecursively(d);
            }

            IEnumerable<FileInfo> files = directory.EnumerateFiles();

            foreach (FileInfo file in files)
            {
                file.Delete();
            }

            directory.Delete();
        }
    }
}
