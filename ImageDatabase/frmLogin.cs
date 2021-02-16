using System;
using System.Windows.Forms;

namespace ImageDatabase
{
    public partial class frmLogin : Form
    {
        public frmLogin()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The tbxUsername and tbxPassword TextBox controls have their 
        /// Modifiers property set to Public. These controls will be accessed
        /// from frmMain by referencing this form.
        /// 
        /// Example: 
        /// frmLogin login = new frmLogin();
        /// login.ShowDialog();
        /// 
        /// username = login.tbxUsername.Text
        /// password = login.tbxPassword.Text
        /// </summary>        
        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (tbxUsername.Text == String.Empty)
            {
                MessageBox.Show("Please enter your username.", "No Username Entered", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tbxUsername.Focus();
                return;
            }

            if (tbxPassword.Text == String.Empty)
            {
                MessageBox.Show("Please enter your password.", "No Password Entered", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tbxPassword.Focus();
                return;
            }
            this.Close();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            tbxPassword.Text = String.Empty;
            tbxUsername.Text = String.Empty;
            tbxUsername.Focus();
        }
    }
}