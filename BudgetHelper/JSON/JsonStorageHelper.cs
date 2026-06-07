using System;
using System.Collections.Generic;
using System.IO;
using BudgetHelper.Models;
using Newtonsoft.Json;

namespace BudgetHelper.Storage
{
    public class ActData
    {
        public DocumentHeader Header { get; set; }
        public List<DocumentContent> Contents { get; set; }
    }

    public static class JsonStorageHelper
    {
        /// <summary>
        /// Сохраняет заголовок и список операций в JSON-файл.
        /// </summary>
        public static void SaveAct(string filePath, DocumentHeader header, List<DocumentContent> contents)
        {
            var data = new ActData { Header = header, Contents = contents };
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Загружает заголовок и список операций из JSON-файла.
        /// </summary>
        public static (DocumentHeader header, List<DocumentContent> contents) LoadAct(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"JSON-файл не найден: {filePath}");

            string json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<ActData>(json);
            return (data.Header, data.Contents);
        }

        /// <summary>
        /// Удаляет JSON-файлы (если нужно очистить хранилище).
        /// </summary>
        public static void ClearStorage(params string[] filePaths)
        {
            foreach (var path in filePaths)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}