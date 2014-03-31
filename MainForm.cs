// This sample code is courtesy of http://www.gotreportviewer.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data;
using Microsoft.Reporting.WinForms;

namespace RdlViewer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.reportViewer1.Drillthrough += new DrillthroughEventHandler(DrillthroughEventHandler);
        }

        private void LoadReportData(string rdlPath, IList<string> dataSetNames,
            ReportParameterInfoCollection reportParams, ReportDataSourceCollection dataSources)
        {
            ReportInfo reportInfo = new ReportInfo(rdlPath);
            try
            {
                reportInfo.Initialize();

                foreach (string dataSetName in dataSetNames)
                {
                    DataTable dataTable = reportInfo.GetData(dataSetName, reportParams);
                    if (dataTable == null)
                        return;
                    dataSources.Add(new ReportDataSource(dataSetName, dataTable));
                }
            }
            finally
            {
                reportInfo.Cleanup();
            }
        }

        private void LoadDataIntoReport(LocalReport report)
        {
            LoadReportData(report.ReportPath, report.GetDataSourceNames(), report.GetParameters(), report.DataSources);
        }
        
        private void LoadReport(string rdlFilename)
        {
            this.reportViewer1.Reset();
            this.reportViewer1.ProcessingMode = Microsoft.Reporting.WinForms.ProcessingMode.Local;
            this.reportViewer1.LocalReport.EnableExternalImages = true;
            this.reportViewer1.LocalReport.EnableHyperlinks = true;
            this.reportViewer1.LocalReport.ReportPath = rdlFilename;

            this.reportViewer1.LocalReport.SubreportProcessing += new SubreportProcessingEventHandler(SubreportEventHandler);

            // Note about subreports:
            //
            // If the report has subreports ReportViewer will automatically load them, but only if the subreport has
            // the .rdlc extension.
            //
            // You could use LoadSubreportDefinition() to workaround this problem but in this case you have to know the 
            // name of the subreport. You could scan the main report's rdl to collect names of all subreports, but this
            // sample does not do that.

            ReportParameterInfoCollection parameters = this.reportViewer1.LocalReport.GetParameters();
            if (parameters.Count > 0)
            {
                using (ParameterDialog dialog = new ParameterDialog())
                {
                    dialog.ReportViewer = this.reportViewer1;
                    dialog.ShowDialog();
                }
            }

            LoadDataIntoReport(this.reportViewer1.LocalReport);

            this.reportViewer1.RefreshReport();
        }

        void SubreportEventHandler(object sender, SubreportProcessingEventArgs e)
        {
            LocalReport parentReport = (LocalReport)sender;
            string parentRdl = parentReport.ReportPath;
            string subreportPath = Path.Combine(Path.GetDirectoryName(parentRdl), e.ReportPath +".rdlc");
            LoadReportData(subreportPath, e.DataSourceNames, e.Parameters, e.DataSources);
        }

        void DrillthroughEventHandler(object sender, DrillthroughEventArgs e)
        {
            string parentRdl = this.reportViewer1.LocalReport.ReportPath;
            string drillthruReportPath = Path.Combine(Path.GetDirectoryName(parentRdl), e.ReportPath + ".rdlc");
            if (!File.Exists(drillthruReportPath))
                drillthruReportPath = Path.Combine(Path.GetDirectoryName(parentRdl), e.ReportPath + ".rdlc");
            LocalReport report = (LocalReport)e.Report;
            report.SubreportProcessing += new SubreportProcessingEventHandler(SubreportEventHandler);
            report.ReportPath = drillthruReportPath;
            LoadDataIntoReport(report);
        }

        private string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            if (ex.InnerException != null)
                message += ": " + GetExceptionMessage(ex.InnerException);
            return message;
        }

        private void openRDLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "RDLC Files (*.rdlc)|*.rdlc|All Files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    LoadReport(openFileDialog1.FileName);
                    this.Text = Path.GetFileName(openFileDialog1.FileName) + " - RDLC Viewer";
                }
                catch (Exception ex)
                {
                    string message = GetExceptionMessage(ex);

                    if (ex.InnerException != null && ex.InnerException.GetType().ToString() ==
                                 "Microsoft.Reporting.DefinitionInvalidException")
                    {
                        message = "RDL version may not be compatible with ReportViewer version. "
                                  + "To view 2008 RDLs you need Visual Studio 2010."
                                  + "\r\n\r\n" + message;
                    }

                    MessageBox.Show(message, "RDLC Viewer", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (PropertyDialog dlg = new PropertyDialog())
            {
                dlg.PropertyGrid.SelectedObject = this.reportViewer1;
                dlg.ShowDialog();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Size = new Size(800, 600);
        }
    }
}
