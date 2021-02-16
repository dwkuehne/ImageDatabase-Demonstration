<img src="https://github.com/dwkuehne/ImageDatabase-Demonstration/blob/master/server.png" width="100px" height="100px" alt="Server Logo"> | <h1>Image Database Demonstration</h1>
-------------------------|-----------------

This project is to store an image as a binary file and to retreive the image from the server and load it into a PictureBox control. 
The program will allow the user to browse for an image file from their computer and store it on a MS SQL Server database. The use
of this code is for use by students.

### Project Introduction
The purpose of this code is to demonstrate the code necessary to take a user provided image file and store it on a server. 
This project will allow for storing images, deleting images, and cycling through available images. 

#### Code Preview:
```csharp
byte[] image = File.ReadAllBytes(openFile.FileName); //Convert image into a byte array
try
{
   cntDatabase.Open();
   string insertQuery = @"INSERT INTO Images(Image) VALUES(@Image)"; // @Image is a parameter we will fill in later
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
```
This is not an exhaustive demonstration of storing/retrieving data nor is it a complete solution for any project.

---David Kuehne, CPT Instructor, February 2021

### Development Environment
Type | Description
-----|-------------
Language | C#
Development Environment | Visual Studio 2019 Community Edition
Target Environment | Windows 10
Target Audience | Students

### Contact
Contact | Information
--------|------
Name | David Kuehne
Email | dwkuehne@tstc.edu
Company | Texas State Technical College

### <a href="https://github.com/dwkuehne/ImageDatabase-Demonstration/blob/master/LICENSE">License</a>
 dwkuehne/ImageDatabase-Demonstration is licensed under the
GNU General Public License v3.0

Permissions of this strong copyleft license are conditioned on making available complete source code of licensed works and modifications, which include larger works using a licensed work, under the same license. Copyright and license notices must be preserved. Contributors provide an express grant of patent rights.
