using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BusinessLayer.ExtensionLibrary
{
    public static class ExtensionLibrary
    {
        public static KeyValuePair<string, string> GetFileNameAndExtension(this string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return new KeyValuePair<string, string>("", "");
            string fileName = Path.GetFileName(filePath);
            var paths = fileName.Split('.').ToList();
            string name = paths.First();
            paths.Remove(name);
            string extension = string.Join(".", paths);
            var nameValue = new KeyValuePair<string, string>(name, string.Concat(".", extension));
            return nameValue;
        }
    }
}
