using System.Windows.Forms;

namespace GoogleDriveManager.Forms
{
    public class MainFormMessanger
    {
        public void HaveToSelectRow() => MessageBox.Show("You have to select a row in order to download.");
        public void HaveToSelectUser() => MessageBox.Show("You have to select a User in order to connect.", 
            "Attention!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        public void ConnectionError() => MessageBox.Show("Connection Error", "Attention!",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

        public bool WantToUploadExistFile() =>
            MessageBox.Show("Do you want to upload only new/changed files on Google Drive ?",
                "Upload existing files?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        public bool AlreadyExistFile(string fileName) =>
             MessageBox.Show("The file : \"" + fileName +
                "\" \nAlready exists on Google Drive!! \nDo you want to uploaded anyway?",
                "File already exist on Google Drive",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        public DialogResult WantToDeleteMessage(int count) =>
            MessageBox.Show("Do you want to delete the " + count + " Selected Files?",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        public DialogResult WantToRemoveUser(string userName) => MessageBox.Show("Do you want to delete User: " +
            userName, "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        public DialogResult WantUploadNewChangedFiles(string userName) =>
            MessageBox.Show("Do you want to Upload only \"new/changed\" files to Google Drive?" +
                userName, "Uploading Arguments",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    }
}
