using Serilog;
using System;
using System.Data;
using System.Reflection;
using System.Collections.Generic;

namespace GoogleDriveManager.Forms
{
    public class MainFormExtention
    {
        private readonly ILogger Logger;
        public MainFormExtention(ILogger logger)
        {
            Logger = logger;
        }
        public DataTable ToDataTable<T>(List<T> items)
        {
            var dataTable = new DataTable(typeof(T).Name);
            try
            {

                var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo prop in props)
                {
                    var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                    dataTable.Columns.Add(prop.Name, type);
                }
                foreach (T item in items)
                {
                    var values = new object[props.Length];
                    for (int i = 0; i < props.Length; i++)
                    {
                        values[i] = props[i].GetValue(item, null);
                    }
                    dataTable.Rows.Add(values);
                }
                return dataTable;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return null;
            }
        }
        public List<FileMarker> GetListOfType()
        {
            return new List<FileMarker>
            {
                new FileMarker("All", null),
                new FileMarker("folder", "application/vnd.google-apps.folder"),
                new FileMarker("document", "application/vnd.google-apps.document"),
                new FileMarker("msword", "application/msword"),
                new FileMarker("spreadsheet", "application/vnd.google-apps.spreadsheet"),
                new FileMarker("pdf", "application/pdf"),
                new FileMarker("video", "video/"),
                new FileMarker("image", "image/"),
                new FileMarker("music", "audio/"),
                new FileMarker("html", "application/vnd.jgraph.mxfile.realtime"),
                new FileMarker("text", "text/"),
                new FileMarker("epub", "application/epub+zip"),
                new FileMarker("zip", "application/zip"),
                new FileMarker("ZIP", "application/x-zip-compressed"),
                new FileMarker("rar", "application/x-rar"),
                new FileMarker("code", "application/octet-stream"),
                new FileMarker("bat", "application/x-msdos-program"),
                new FileMarker("exe", "application/x-dosexec"),
                new FileMarker("unknown", "document/unknown")
            };
        }
    }
}
