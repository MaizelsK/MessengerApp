using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    public static class FileHelper
    {
        // Получение данных о файле
        public static FileMetadata GetFileMetadata(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            FileMetadata metadata = new FileMetadata
            {
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                FilePath = filePath
            };

            return metadata;
        }

        // Сохранение файла
        public static void SaveFile(FileMetadata metadata, byte[] fileData)
        {
            Task.Run(() =>
            {
                string savedFilesPath = Directory.GetCurrentDirectory() + "\\Saved Files";
                Directory.CreateDirectory(savedFilesPath);

                File.WriteAllBytes(savedFilesPath + "\\" + metadata.FileName, fileData);
            });
        }
    }
}
