using System.Collections.Generic;

namespace GoogleDriveManager.API
{
    public static class MimeTypes
    {
        public static List<MimeTypeMarker> GetList()
        {
            List<MimeTypeMarker> list = new List<MimeTypeMarker>();
            list.Add(new MimeTypeMarker(".xlsx", "application/vnd.google-apps.spreadsheet", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
            list.Add(new MimeTypeMarker(".doc", "application/vnd.google-apps.document", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
            list.Add(new MimeTypeMarker(".pptx", "application/vnd.google-apps.presentation", "application/vnd.openxmlformats-officedocument.presentationml.presentation"));
            list.Add(new MimeTypeMarker(".html", "application/vnd.google-apps.site", "text/html"));
            list.Add(new MimeTypeMarker(".zip", "application/vnd.google-apps.folder", "application/vnd.google-apps.folder"));

            return list;
        }
    }
}
