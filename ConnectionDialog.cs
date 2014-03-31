// This sample code is courtesy of http://www.gotreportviewer.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;

// todo: use SqlConnectionStringBuilder class
// (This code was originally written using .NET v1.x which didn't have SqlConnectionStringBuilder class.)

namespace RdlViewer
{
    public partial class ConnectionDialog : Form
    {
        private string m_connectString;
        private string m_dataSourceName;

        public SqlConnection connection;

        public ConnectionDialog(string dataSourceName, string connectString)
        {
            this.m_dataSourceName = dataSourceName;
            this.m_connectString = connectString;
            InitializeComponent();
        }

        private void ConnectionDialog_Load(object sender, EventArgs e)
        {
            this.dataSourceNameTextBox.Text = m_dataSourceName;

            string modifiedConnectString = "";

            string[] nameValuePairs = m_connectString.Split(';');
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] s = nameValuePair.Split('=');
                if (s.Length == 2)
                {
                    string name = s[0];
                    string value = s[1];
                    if (name.Equals("data source", StringComparison.OrdinalIgnoreCase))
                    {
                        this.hostTextBox.Text = value;
                    }
                    else if (name.Equals("integrated security", StringComparison.OrdinalIgnoreCase))
                    {
                        this.integratedSecurityCheckBox.Checked = true;
                    }
                    else if (name.Equals("persist security info", StringComparison.OrdinalIgnoreCase))
                    {
                        // discard
                    }
                    else
                    {
                        modifiedConnectString += name + "=" + value + ";";
                    }
                }
            }

            this.m_connectString = modifiedConnectString;
            UpdateConnectStringTextBox();
            SetUsernamePasswordTextBoxStates();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            try
            {
                connection = new SqlConnection(connectStringTextBox.Text);
                connection.Open();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Connection Error");
            }
        }

        private void SetUsernamePasswordTextBoxStates()
        {
            bool enable = !integratedSecurityCheckBox.Checked;
            this.usernameTextBox.Enabled = enable;
            this.passwordTextBox.Enabled = enable;
        }

        private void integratedSecurityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SetUsernamePasswordTextBoxStates();
            UpdateConnectStringTextBox();
        }

        private void UpdateConnectStringTextBox()
        {
            if (integratedSecurityCheckBox.Checked)
            {
                connectStringTextBox.Text =
                              "data source=" + this.hostTextBox.Text +
                              ";Integrated Security=SSPI" +
                              ";" + m_connectString;
            }
            else
            {
                connectStringTextBox.Text =
                              "data source=" + this.hostTextBox.Text +
                              ";user=" + this.usernameTextBox.Text +
                              ";password=" + this.passwordTextBox.Text +
                              ";" + m_connectString;
            }
        }

        private void usernameTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateConnectStringTextBox();
        }

        private void passwordTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateConnectStringTextBox();
        }

        private void hostTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateConnectStringTextBox();
        }
    }
}
