using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;
using Serilog;
using Newtonsoft.Json;
using GoogleDriveManager.Forms;

namespace GoogleDriveManager
{
    public partial class MainForm : Form
    {
        private readonly ILogger logger = OperateLoggerFactory.Get();
        private readonly MainFormMessanger messanger = new MainFormMessanger();
        private readonly MainFormExtention extention = new MainFormExtention(OperateLoggerFactory.Get());
        private readonly DriveManager DriveManager;
        private readonly FileManager FileManager = new FileManager();
        private readonly ILogger Logger = OperateLoggerFactory.Get();
        private delegate void update();
        
        
        public List<User> UserList = new List<User>();
        public List<LocalFile> IOFileList = new List<LocalFile>();
        private List<FileMarker> cbList = new List<FileMarker>();
        private DataTable dtDriveFiles;
        
        private readonly string savePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BackUpManager";
        private readonly string saveFile;

        public MainForm()
        {
            InitializeComponent();
            pnlDragAndDrop.AllowDrop = true;
            saveFile = savePath + "\\GDASaves.json";
            DriveManager = new DriveManager(this);
        }
        private void cbTypeInit()
        {
            cbList = extention.GetListOfType();
            SetComboBoxFileType();
        }
        private void SetComboBoxFileType()
        {
            cbFileType.DataSource = null;
            cbFileType.Items.Clear();
            cbFileType.DataSource = cbList;
            cbFileType.DisplayMember = "name";
            cbFileType.ValueMember = "type";
            cbFileType.SelectedIndex = 0;
            cbFileType.Text = "Select Type...";
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadUsers(savePath, saveFile);
            UIinit();
            cbUserInit();
            cbTypeInit();
            lblProgress.Text = "";
            txtJsonPath.Text = "Resources\\client_secret.json";
            dtDriveFiles = new DataTable();
            dgvFilesFromDrive.DataSource = dtDriveFiles;
            logger.Information("Create and init Main form.");
        }
        public void UpdateStatusBar(long bytes, string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<long, string>(UpdateStatusBar), new object[] { bytes, msg });
                return;
            }
            progressBar.Value = (int)bytes;
            lblProgress.Text = msg;
            lblProgress.Location = new Point(ClientRectangle.Width - lblProgress.Width - progressBar.Width - 20, 4);
        }
        private void UpdateDataGridView(string name = null, string type = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>(UpdateDataGridView), new object[] { name, type });
                return;
            }

            dtDriveFiles = null;
            dtDriveFiles = extention.ToDataTable(DriveManager.CreateDriveFiles(name, type));
            dgvFilesFromDrive.DataSource = dtDriveFiles;
        }

        private void UIinit()
        {
            if (chbAddUser.Checked)
            {
                MinimumSize = new Size(900, 660);
                pnlConnection.Height = 290;
                pnlUser.Visible = true;
                pnlClient.Visible = true;
                Height = 660;
                lblPanel.Location = new Point(4, 351);
                pnlDragAndDrop.Location = new Point(7, 371);
                pnlDragAndDrop.Height -= 190;
                return;
            }
            MinimumSize = new Size(900, 470);
            pnlConnection.Height = 100;
            pnlUser.Visible = false;
            pnlClient.Visible = false;
            lblPanel.Location = new Point(4, 161);
            pnlDragAndDrop.Location = new Point(7, 181);
            pnlDragAndDrop.Height += 190;
            Height =  470;
        }

        private void LoadUsers(string savePath, string saveFile)
        {
            Directory.CreateDirectory(savePath);
            try
            {
                if (File.Exists(saveFile))
                {
                    UserList.Clear();
                    UserList = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText(saveFile));
                    logger.Information("Load users from saves.");
                    return;
                }
                FileManager.CreateFile(saveFile, "[ ]");
                LoadUsers(savePath, saveFile);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        private void SaveUsers(string saveFile)
        {
            string contentsToWriteToFile = JsonConvert.SerializeObject(UserList.ToArray(), Formatting.Indented);
            File.WriteAllText(saveFile, contentsToWriteToFile);
        }

        private void pnlDragAndDrop_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? 
                DragDropEffects.All : DragDropEffects.None;
        }

        private void pnlDragAndDrop_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (chbUploadMultiple.Checked)
                {
                    lbFilesToUpload.Items.Add(file);
                    IOFileList.Add(new LocalFile(file));
                }
                else
                {
                    IOFileList.Clear();
                    txtFilePath.Text = file;
                    IOFileList.Add(new LocalFile(file));
                }
            } 
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            lbFilesToUpload.Items.Clear();
            IOFileList.Clear();
        }

        private void pnlConnection_DragDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void pnlConnection_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                txtJsonPath.Text = file;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (cbUser.SelectedIndex == -1)
            {
                messanger.HaveToSelectUser();
            }
            if (DriveManager.GoogleDriveConnection(UserList[cbUser.SelectedIndex].ClientSecretPath, 
                UserList[cbUser.SelectedIndex].UserName))
            {
                btnConnect.BackColor = Color.Green;
                Task.Run(() => { UpdateDataGridView(); });
                logger.Information("Connect to user account.");
                return;
            }
            btnConnect.BackColor = Color.Red;
            messanger.ConnectionError();
            logger.Error("Can't connect to user account.");
        }
        private void textBox_path_TextChanged(object sender, EventArgs e)
        {
            txtFileName.Text = Path.GetFileName(txtFilePath.Text);
            txtMd5.Text = FileManager.HashGenerator(txtFilePath.Text);
        }
        private void btnDirToUpload_Click(object sender, EventArgs e)
        {
            var result = fbdDirToUpload.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtFilePath.Text = fbdDirToUpload.SelectedPath;
            }
        }

        private void button_browse_Click(object sender, EventArgs e)
        {
            var result = ofgFileToUpload.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtFilePath.Text = ofgFileToUpload.FileName;
                txtMd5.Text = FileManager.HashGenerator(ofgFileToUpload.FileName);
            }
        }
        private void btnJsonBroswe_Click(object sender, EventArgs e)
        {
            var result = ofgJsonFile.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtJsonPath.Text = ofgJsonFile.FileName;
            }
        }
        private void DownloadFile(string fileName, string fileID, string mimeType)
        {
            var fbd = new FolderBrowserDialog();
            var result = fbd.ShowDialog();

            if (result == DialogResult.OK)
            {
                txtJsonPath.Text = ofgJsonFile.FileName;
                string path = fbd.SelectedPath;
                Task.Run(() => { DriveManager.DownloadFromDrive(fileName, fileID, path, mimeType); });
            }
        }
        private void btnUpload_Click(object sender, EventArgs e)
        {
            bool result = false;
            var parentID = (txtParentID.Text != string.Empty) ? txtParentID.Text : null;
            if (chbUploadMultiple.Checked)
            {
                result = messanger.WantToUploadExistFile();
                lbFilesToUpload.BeginUpdate();
                MultipleUpload(parentID, result);
                lbFilesToUpload.EndUpdate();
                return;
            }
            SingleUpload(parentID, result);    
        }
        private void MultipleUpload(string parentId, bool result)
        {
            for (int i = 0; i < lbFilesToUpload.Items.Count; i++)
            {
                var filePath = chbCompress.Checked ?
                    FileManager.CompressFile(lbFilesToUpload.Items[i].ToString()) :
                    lbFilesToUpload.Items[i].ToString();
                var fileName = chbCompress.Checked ?
                    Path.GetFileName(lbFilesToUpload.Items[i].ToString().Split('.').First()) + ".zip" :
                    Path.GetFileName(lbFilesToUpload.Items[i].ToString());
                Task.Run(() =>
                {
                    DriveManager.UploadToDrive(filePath, fileName, parentId, result);
                    UpdateDataGridView();
                });
                var newName = "DONE!  " + lbFilesToUpload.Items[i].ToString();

                lbFilesToUpload.Items[i] = newName;
                UpdateDataGridView();
            }
        }
        private void SingleUpload(string parentId, bool result)
        {
            var filePath = chbCompress.Checked ? FileManager.CompressFile(txtFilePath.Text) : txtFilePath.Text;
            var fileName = chbCompress.Checked ? txtFileName.Text.Split('.').First() + ".zip" : txtFileName.Text;
            if (DriveManager.CompareHash(FileManager.HashGenerator(filePath)))
            {
                result = messanger.AlreadyExistFile(fileName);
                Task.Run(() => {
                    DriveManager.UploadToDrive(filePath, fileName, parentId, !result);
                    UpdateDataGridView();
                });
            }
            Task.Run(() => {
                DriveManager.UploadToDrive(filePath, fileName, parentId, false);
                UpdateDataGridView();
            });
        }
        private void btnDownload_Click(object sender, EventArgs e)
        {
            string fileName, fileId, mimeType;
            if (dgvFilesFromDrive.SelectedRows.Count <= 0)
            {
                messanger.HaveToSelectRow();
                return;
            }
            foreach (DataGridViewRow row in dgvFilesFromDrive.SelectedRows)
            {
                fileName = dgvFilesFromDrive.Rows[row.Index].Cells[0].Value.ToString();
                mimeType = dgvFilesFromDrive.Rows[row.Index].Cells[3].Value.ToString();
                fileId = dgvFilesFromDrive.Rows[row.Index].Cells[4].Value.ToString();
                DownloadFile(fileName, fileId, mimeType);
            }
        }

        private void dgvFilesFromDrive_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            txtParentID.Text = dgvFilesFromDrive.Rows[e.RowIndex].Cells[4].Value.ToString();
        }
        private void btnRemove_Click(object sender, EventArgs e)
        {
            string  fileId, fileName;
            var result = messanger.WantToDeleteMessage(dgvFilesFromDrive.SelectedRows.Count);
            if (result == DialogResult.Yes)
            {
                foreach (DataGridViewRow row in dgvFilesFromDrive.SelectedRows)
                {
                    fileName = dgvFilesFromDrive.Rows[row.Index].Cells[0].Value.ToString();
                    fileId = dgvFilesFromDrive.Rows[row.Index].Cells[4].Value.ToString();
                    DriveManager.RemoveFile(fileId);

                }
                Task.Run(() => { UpdateDataGridView(); });
            }
        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            UserList.Add(new User(txtUserName.Text, txtJsonPath.Text));
            SaveUsers(saveFile);
            cbUserInit();
        }

        private void cbUserInit()
        {
            cbUser.DataSource = null;
            cbUser.Items.Clear();
            cbUser.DataSource = UserList;
            cbUser.DisplayMember = "userName";
            cbUser.ValueMember = "userName";
            cbUser.SelectedIndex = -1;
            cbUser.Text = "Select User...";
        }

        private void chbAddUser_CheckedChanged(object sender, EventArgs e)
        {
            UIinit();
        }

        private void cbUser_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtInit(cbUser.SelectedIndex);
        }

        private void txtInit(int i)
        {
            if (i >= 0)
            {
                txtUserName.Text = UserList[i].UserName;
                txtJsonPath.Text = UserList[i].ClientSecretPath;
                return;
            }
            txtUserName.Text = "";
            txtJsonPath.Text = "";
        }

        private void btnRemUser_Click(object sender, EventArgs e)
        {
            if (cbUser.SelectedIndex != -1)
            {
                var result = messanger.WantToRemoveUser(UserList[cbUser.SelectedIndex].UserName);
                if (result == DialogResult.Yes)
                { 
                    FileManager.DeleteCredFile(savePath, UserList[cbUser.SelectedIndex].UserName);
                    UserList.Remove(UserList[cbUser.SelectedIndex]);
                    SaveUsers(saveFile);
                    cbUserInit();
                }
            }
        }
        

        private void dgvFilesFromDrive_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) dgvFilesFromDrive.Rows[e.RowIndex].Selected = true ;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            UpdateDataGridView(); 
        }
        private void btnCreate_Click(object sender, EventArgs e)
        {
            Task.Run(() => { DriveManager.CreateFolderToDrive(txtCreateFolder.Text, null); UpdateDataGridView(); });
        }
        private void btnSearch_Click(object sender, EventArgs e)
        {
            UpdateDataGridView(txtSearchFile.Text, cbList[cbFileType.SelectedIndex].Type); 
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            lblProgress.Location = new Point(ClientRectangle.Width - lblProgress.Width - progressBar.Width - 20, 4);
        }
        private void chbUploadMultiple_CheckedChanged(object sender, EventArgs e)
        {
            pnlSF.Visible = !chbUploadMultiple.Checked;
            pnlListBox.Visible = chbUploadMultiple.Checked;
            txtBackUpName.Text = "Backup";
        }
        private void cmnuOpenFileOnBrowser_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvFilesFromDrive.SelectedRows)
            {
                string uri = dgvFilesFromDrive.Rows[row.Index].Cells[6].Value.ToString();
                System.Diagnostics.Process.Start(uri);
            }
        }

        private void cmnuCopyID_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvFilesFromDrive.SelectedRows)
            {
                string fileID = dgvFilesFromDrive.Rows[row.Index].Cells[4].Value.ToString();
                Clipboard.SetText(fileID);
            }
            
        }
        private void dgvFilesFromDrive_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != -1 && e.RowIndex != -1 && e.Button == MouseButtons.Right)
            {
                var c = (sender as DataGridView)[e.ColumnIndex, e.RowIndex];
                if (!c.Selected)
                {
                    c.DataGridView.ClearSelection();
                    c.DataGridView.CurrentCell = c;
                    c.Selected = true;
                }
            }
            if (e.RowIndex >= 0) dgvFilesFromDrive.Rows[e.RowIndex].Selected = true;
        }

        private void cmnuCopyHash_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvFilesFromDrive.SelectedRows)
            {
                string fileID = dgvFilesFromDrive.Rows[row.Index].Cells[5].Value.ToString();
                if (fileID != null)Clipboard.SetText(fileID);
            }
        }
    }
}
