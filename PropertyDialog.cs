// This sample code is courtesy of http://www.gotreportviewer.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RdlViewer
{
    public partial class PropertyDialog : Form
    {
        public PropertyDialog()
        {
            InitializeComponent();
        }

        public PropertyGrid PropertyGrid
        {
            get
            {
                return this.propertyGrid1;
            }
        }
    }
}