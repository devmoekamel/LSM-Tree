using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.SSTable;
public class SStableManager
{
    public List<SSTableMetadata> SSTablesMetadata { get; private set; }

    const int _metadataSatartOffset = -12;
    public string _folder { get; set; }
     object _lock = new object();
    public SStableManager(string Folder)
    {
        _folder = Folder;
        RefreshTables();
    }


    public void RefreshTables()
    {
        var files = Directory.GetFiles(_folder, "*.sst")
            .OrderByDescending(f => File.GetCreationTime(f));

        SSTablesMetadata = files.Select(f => new SSTableMetadata(f, _metadataSatartOffset)).ToList();
    }

   public (bool found, byte[]? value) Search(string key)
    {

        List<SSTableMetadata> tablesSnapshot;
        lock (_lock)
        {
            tablesSnapshot = new List<SSTableMetadata>(SSTablesMetadata);
        }

        foreach (var metadata in tablesSnapshot)
        {
            if (metadata.IsKeyInRange(key))
            {
                var stable = new SSTable(metadata.Path);
                var (found, value) = stable.Searchkey(key);

                if (found)
                {
                    return (found, value);
                }
            }
        }
      


        return (false,null);
    }

    public void RegisterNewTable(string path)
    {
        SSTablesMetadata.Insert(0, new SSTableMetadata(path, _metadataSatartOffset));
    }

    public void ReplaceTables(List<string> oldFiles, string newFile)
    {
        lock (_lock)
        {
            SSTablesMetadata.RemoveAll(t => oldFiles.Contains(t.Path));
            RegisterNewTable(newFile);

            foreach (var file in oldFiles)
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }
    }

}
