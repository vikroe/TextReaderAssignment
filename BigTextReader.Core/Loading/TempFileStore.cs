namespace BigTextReader.Core.Loading
{
    public sealed class TempFileStore: IDisposable
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "BigTextReader");
        private readonly List<string> _paths = [];

        public TempFileStore()
        {
            Directory.CreateDirectory(Root);
            SweepOrphans();
        }

        public string NewFile(string extension = ".txt")
        {
            string path = Path.Combine(Root, $"{Guid.NewGuid():N}{extension}");
            _paths.Add(path);
            return path;
        }

        private static void SweepOrphans()
        {
            foreach(string f in Directory.EnumerateFiles(Root))
                try { File.Delete(f); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
        }
        
        public void Dispose()
        {
            foreach (string f in _paths)
                try { File.Delete(f); } 
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            _paths.Clear();
        }
    }
}
