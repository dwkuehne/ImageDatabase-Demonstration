using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageDatabase
{
    public partial class frmMain : Form
    {
        // Global variables to keep track of things like 
        // selected image, if connected, storing in a list, etc.
        private SqlConnection cntDatabase = new SqlConnection();        
        private bool blnConnected = false;

        // A list is used to query the server once and store the 
        // results to be processed locally instead of querying 
        // the server over and over again. 
        private List<Images> lstImages = new List<Images>();
        
        // Keeping track of images
        private int intCurrent = 0;
        private int intSelectedImageID = 0;

        // Storing credentials from frmLogin
        private string strUsername = String.Empty;
        private string strPassword = String.Empty;

        //TODO: Replace String.Empty with the name of your table.
        private string strTableName = String.Empty; // (e.g. "Images")        

        public frmMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Each Images object will hold the ImageID (pictureID column in SQL)
        /// and the byte array of the actual Image (Image column in SQL)
        /// 
        /// SQL Data
        /// pictureID: int (auto increment)
        /// Image: varbinary(MAX)
        /// 
        /// Each object will be stored in a list.
        /// </summary>
        public class Images
        {
            public int ImageID { get; set; }
            public byte[] Image { get; set; }

            public Images()
            {
                //Default constructor
            }

            // Allow for two parameters (Coding preference)
            // Not used in this demonstration.
            public Images(int intNumber, byte[] imageArray)
            {
                ImageID = intNumber;
                Image = imageArray;
            }
        }

        /// <summary>
        /// The form load event will check for credentials for logging into the SQL Server.
        /// If the credentials are correct then if available, load the first image into 
        /// the PictureBox control.
        /// 
        /// ReloadImageList() is used to grab any data from the SQL Server hosting your
        /// images and storing the actual image and it's ID as an object in a list. 
        /// If the list is not empty, then load the first image in the list to the PictureBox.
        /// 
        /// intSelectedImageID is used in the event you want to delete an image from the server.
        /// </summary>
        private void frmMain_Load(object sender, EventArgs e)
        {
            if (!blnConnected)            
                LoginPrompt();
            
            try
            {
                cntDatabase.Open();
                statusLabel.Text = "Connection Status: Connected!";
                blnConnected = true;
                cntDatabase.Close();

                ReloadImageList();
                if (lstImages.Count > 0)
                {
                    using (MemoryStream ms = new MemoryStream(lstImages[0].Image))
                    {
                        Image image = Image.FromStream(ms);
                        pbxImage.Image = image;
                    }
                    intSelectedImageID = lstImages[0].ImageID;
                }
            }
            catch (Exception)
            {
                statusLabel.Text = "Connection Status: Offline";
                blnConnected = false;
            }
        }

        private void pbxClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void pbxNext_Click(object sender, EventArgs e)
        {
            if (blnConnected && lstImages.Count == 0)
                return;

            if (!blnConnected)
                blnConnected = CheckConnection();
            else
            {
                intCurrent++;                
                LoadNext();
            }
        }

        private void pbxPrevious_Click(object sender, EventArgs e)
        {
            if (blnConnected && lstImages.Count == 0)
                return;

            if (!blnConnected)
                blnConnected = CheckConnection();
            else
            {
                intCurrent--;                
                LoadPrevious();
            }
        }

        /// <summary>
        /// Clicking the "Add" button will open a file dialog prompt for the user. The 
        /// user can browse for .png and .jpg files (with a little validation). The 
        /// selected file is converted into a byte array for storing in a varbinary(MAX)
        /// data type on a SQL Server. 
        /// </summary>
        private void pbxAdd_Click(object sender, EventArgs e)
        {
            if (!blnConnected)
                blnConnected = CheckConnection();
            else
            {
                //OpenFileDialog Properties------------------------------------------
                OpenFileDialog openFile = new OpenFileDialog(); //New instance
                openFile.ValidateNames = true; //Prevent illegal characters
                openFile.AddExtension = false; //Auto fixes file extension problems
                openFile.Filter = "Image File|*.png|Image File|*.jpg"; //Allow types
                openFile.Title = "File to Upload"; //Title of dialog box
                //-------------------------------------------------------------------
                
                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    //TODO: Add some validation to make sure the file is an image.

                    byte[] image = File.ReadAllBytes(openFile.FileName); //Convert image into a byte array
                    try
                    {
                        cntDatabase.Open();
                        //TODO: Change (Image) to the name of your image column [e.g (ProductImages)]
                        string insertQuery = $"INSERT INTO {strTableName}(Image) VALUES(@Image)"; // @Image is a parameter we will fill in later                        
                        SqlCommand insertCmd = new SqlCommand(insertQuery, cntDatabase);
                        SqlParameter sqlParams = insertCmd.Parameters.AddWithValue("@Image", image); // The parameter will be the image as a byte array
                        sqlParams.DbType = System.Data.DbType.Binary; // The type of data we are sending to the server will be a binary file
                        insertCmd.ExecuteNonQuery();
                        cntDatabase.Close();

                        MessageBox.Show("File was successfully added to the database.", "File Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ReloadImageList(); // Repopulate the list of images locally

                        // We use the MemoryStream to convert our image stored in the list to something the 
                        // PictureBox control can understand.
                        using (MemoryStream ms = new MemoryStream(lstImages[lstImages.Count - 1].Image))
                        {
                            Image loadImage = Image.FromStream(ms);
                            pbxImage.Image = loadImage;
                        }

                        intCurrent = lstImages.Count - 1; // Keep track of where we are in our list of images
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message, "Error During Upload", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// To remove an image, first check to see if there are any images on the server. 
        /// Checking for the images happens during Form Load and when a new image is added.
        /// To check with this click event, we look at the list of images. If it is empty, 
        /// produce an error message. Otherwise, prompt the user if they are sure to delete
        /// the selected image. If they select "Yes" then run the delete query.
        /// </summary>
        private void pbxRemove_Click(object sender, EventArgs e)
        {
            if (blnConnected && lstImages.Count == 0)
            {
                pbxImage.Image = Properties.Resources.question_mark5;
                MessageBox.Show("There are no images to delete on the database!", "Nothing to delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!blnConnected)
                blnConnected = CheckConnection();
            else
            {
                if (MessageBox.Show("Are you sure you want to delete the selected picture?", "About to Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        cntDatabase.Open();
                        //TODO: Change pictureID to the name of your Primary Key column
                        string deleteQuery = $"DELETE FROM {strTableName} WHERE pictureID={intSelectedImageID}";
                        SqlCommand deleteCmd = new SqlCommand(deleteQuery, cntDatabase);
                        deleteCmd.ExecuteNonQuery();
                        cntDatabase.Close();

                        MessageBox.Show("File was successfully removed from the database.", "File Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ReloadImageList();

                        if (lstImages.Count > 0)
                        {
                            using (MemoryStream ms = new MemoryStream(lstImages[lstImages.Count - 1].Image))
                            {
                                Image loadImage = Image.FromStream(ms);
                                pbxImage.Image = loadImage;
                            }
                            intCurrent = lstImages.Count - 1;
                        }
                        else
                        {
                            intCurrent = 0;
                            pbxImage.Image = Properties.Resources.question_mark5;
                        }
                                                    
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message, "Error During Deletion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                if (lstImages.Count == 0)
                {
                    pbxImage.Image = Properties.Resources.question_mark5;
                }
            }
        }

        /// <summary>
        /// CheckConnection() is used to determine if the server can be reached. 
        /// Produce a Login page to obtain the credentials from the user. Update
        /// the boolean value to reflect if a connection was achieved. If there 
        /// was a sucessful connection, query the server for any images. Load the images
        /// into a list with the ReloadImageList() method then load the first image
        /// in the list into the PictureBox control. 
        /// 
        /// Store the image ID to be used later if we decide to delete the image. 
        /// </summary>
        /// <returns>blnConnected as true or false</returns>
        public bool CheckConnection()
        {
            if (MessageBox.Show("Not connected to the Internet.", "Connection Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
            {
                try
                {
                    LoginPrompt();

                    cntDatabase.Open();
                    statusLabel.Text = "Connection Status: Connected!";
                    blnConnected = true;
                    cntDatabase.Close();

                    ReloadImageList();
                    using (MemoryStream ms = new MemoryStream(lstImages[0].Image))
                    {
                        Image image = Image.FromStream(ms);
                        pbxImage.Image = image;
                    }
                    intSelectedImageID = lstImages[0].ImageID;

                    return blnConnected;
                }
                catch (Exception)
                {
                    statusLabel.Text = "Connection Status: Offline";
                    blnConnected = false;
                    CheckConnection();

                    return blnConnected;
                }
            }

            return false;
        }

        /// <summary>
        /// LoginPrompt() loads an instance of frmLogin and will retrieve
        /// the credentials from two TextBox controls on that form. They will
        /// be used to build the connection string to query the database.
        /// 
        /// Server: The Server name or it's IP address
        /// Database: Your database name here
        /// User Id: You don't want to hard-code this in production code
        /// Password: This either
        /// </summary>
        public void LoginPrompt()
        {
            frmLogin login = new frmLogin();            
            login.ShowDialog();
            //TODO: Change "IPaddressOfServer" to the fully qualified server name or IP address
            //TODO: Change "databaseOnServer" to the server database name
            string CONNECT_STRING = $"Server=IPaddressOfServer;Database=databaseOnServer;User Id={login.tbxUsername.Text};password={login.tbxPassword.Text}";            
            // Example String: $"Server=192.168.1.1;Database=MyAwesomeApp;User Id=dave;password=codemonkey"
            cntDatabase = new SqlConnection(CONNECT_STRING);
        }

        /// <summary>
        /// ReloadImageList() is used to query the server for the images
        /// and their associated IDs. The SqlDataRead will look at each 
        /// column returned which we will store in an object that will
        /// be placed inside of a list. 
        /// </summary>
        public void ReloadImageList()
        {
            //TODO: Change the SELECT statement to the column names you are trying to use.
            string strCommand = $"SELECT pictureID, Image FROM {strTableName};"; // Query to pull two columns of data from Images table            
            SqlCommand SelectCommand = new SqlCommand(strCommand, cntDatabase);
            SqlDataReader sqlReader;

            lstImages.Clear(); // Empty the list before loading new images to prevent duplications
            try
            {
                cntDatabase.Open();
                sqlReader = SelectCommand.ExecuteReader();

                while (sqlReader.Read())
                {
                    Images image = new Images();
                    image.ImageID = sqlReader.GetInt32(0); // MS SQL Datatype int
                    image.Image = (byte[])sqlReader[1]; // MS SQL Datatype varbinary(MAX)
                    lstImages.Add(image); // Add image object to list

                    // You can use a constructor for this class to accept two parameters
                    // and add it all at the same time. Just for demo purposes

                    // lstImages.Add(new Images(sqlReader.GetInt32(0), (byte[])sqlReader[1]));
                }                
                cntDatabase.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Error reloading images.", "Error with Loading", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load next image and keep track of it's ID. 
        /// Update PictureBox control with the new image.
        /// If the end of the list is reached, loop to first image.
        /// </summary>
        public void LoadNext()
        {
            if (intCurrent >= lstImages.Count)
            {
                intCurrent = 0;
            }

            using (MemoryStream ms = new MemoryStream(lstImages[intCurrent].Image))
            {
                Image image = Image.FromStream(ms);
                pbxImage.Image = image;                
            }

            intSelectedImageID = lstImages[intCurrent].ImageID;
        }

        /// <summary>
        /// Load previous image and keep track of it's ID. 
        /// Update PictureBox control with the new image.
        /// If the beginning of the list is reached, loop to the last image.
        /// </summary>
        public void LoadPrevious()
        {
            if (intCurrent < 0 & lstImages.Count > 0)
            {
                intCurrent = lstImages.Count - 1;
            }

            using (MemoryStream ms = new MemoryStream(lstImages[intCurrent].Image))
            {
                Image image = Image.FromStream(ms);
                pbxImage.Image = image;
            }

            intSelectedImageID = lstImages[intCurrent].ImageID;
        }
    }
}